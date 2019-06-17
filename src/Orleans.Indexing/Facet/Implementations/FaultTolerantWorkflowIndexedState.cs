using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    internal class FaultTolerantWorkflowIndexedState<TGrainState> : NonFaultTolerantWorkflowIndexedState<TGrainState, FaultTolerantIndexedGrainStateWrapper<TGrainState>>,
                                                                    IFaultTolerantWorkflowIndexedState<TGrainState>,
                                                                    ILifecycleParticipant<IGrainLifecycle>
                                                                    where TGrainState : class, new()
    {
        private readonly IGrainFactory _grainFactory;    // TODO: standardize leading _ or not; and don't do this._

        public FaultTolerantWorkflowIndexedState(
                IServiceProvider sp,
                IIndexedStateConfiguration config,
                IGrainActivationContext context,
                IGrainFactory grainFactory
            ) : base(sp, config, context)
        {
            this._grainFactory = grainFactory;
            base.getWorkflowIdFunc = () => this.GenerateUniqueWorkflowId();
        }

        private bool _hasAnyTotalIndex;

        private FaultTolerantIndexedGrainStateWrapper<TGrainState> ftWrappedState => base.nonTransactionalState.State;

        internal override IDictionary<Type, IIndexWorkflowQueue> WorkflowQueues
        {
            get => this.ftWrappedState.WorkflowQueues;
            set => this.ftWrappedState.WorkflowQueues = value;
        }

        private HashSet<Guid> ActiveWorkflowsSet
        {
            get => this.ftWrappedState.ActiveWorkflowsSet;
            set => this.ftWrappedState.ActiveWorkflowsSet = value;
        }

        public new void Participate(IGrainLifecycle lifecycle) => base.Participate<FaultTolerantWorkflowIndexedState<TGrainState>>(lifecycle);

        internal async override Task OnActivateAsync(CancellationToken ct)
        {
            base.Logger.Trace($"Activating indexable grain of type {grain.GetType().Name} in silo {this.SiloIndexManager.SiloAddress}.");
            await base.InitializeState();

            // If the list of active workflows is null or empty we can assume that we were not previously activated
            // or did not have any incomplete workflow queue items in a prior activation.
            if (this.ActiveWorkflowsSet == null || this.ActiveWorkflowsSet.Count == 0)
            {
                this.WorkflowQueues = null;
                await base.FinishActivateAsync();
            }
            else
            {
                // There are some remaining active workflows so they should be handled first.
                this.PruneWorkflowQueuesForMissingInterfaceTypes();
                await this.HandleRemainingWorkflows()
                          .ContinueWith(t => Task.WhenAll(this.PruneActiveWorkflowsSetFromAlreadyHandledWorkflows(t.Result),
                                                          base.FinishActivateAsync()));
            }
            this._hasAnyTotalIndex = base._grainIndexes.HasAnyTotalIndex;
        }

        /// <summary>
        /// Applies a set of updates to the indexes defined on the grain
        /// </summary>
        /// <param name="interfaceToUpdatesMap">the dictionary of indexes to their corresponding updates</param>
        /// <param name="updateIndexesEagerly">whether indexes should be updated eagerly or lazily</param>
        /// <param name="onlyUniqueIndexesWereUpdated">a flag to determine whether only unique indexes were updated</param>
        /// <param name="numberOfUniqueIndexesUpdated">determine the number of updated unique indexes</param>
        /// <param name="writeStateIfConstraintsAreNotViolated">whether the state should be written to storage if no constraint is violated</param>
        private protected override async Task ApplyIndexUpdates(InterfaceToUpdatesMap interfaceToUpdatesMap,
                                                                bool updateIndexesEagerly, bool onlyUniqueIndexesWereUpdated,
                                                                int numberOfUniqueIndexesUpdated, bool writeStateIfConstraintsAreNotViolated)
        {
            if (interfaceToUpdatesMap.IsEmpty || !this._hasAnyTotalIndex)
            {
                // Drop down to non-fault-tolerant
                await base.ApplyIndexUpdates(interfaceToUpdatesMap, updateIndexesEagerly, onlyUniqueIndexesWereUpdated,
                                             numberOfUniqueIndexesUpdated, writeStateIfConstraintsAreNotViolated);
                return;
            }

            if (interfaceToUpdatesMap.UpdateReason.IsActivationChange())
            {
                throw new InvalidOperationException("Active indexes cannot be fault-tolerant. This misconfiguration should have" +
                                                    " been detected on silo startup. Check ApplicationPartsIndexableGrainLoader for the reason.");
            }

            if (updateIndexesEagerly)
            {
                throw new InvalidOperationException("Fault tolerant indexes cannot be updated eagerly. This misconfiguration should have" +
                                                    " been detected on silo startup. Check ApplicationPartsIndexableGrainLoader for the reason.");
            }

            // Update the indexes lazily. This is the first step, because its workflow record should be persisted in the workflow-queue first.
            // The reason for waiting here is to make sure that the workflow record in the workflow queue is correctly persisted.
            await base.ApplyIndexUpdatesLazily(interfaceToUpdatesMap);

            // Apply any unique index updates eagerly. This will always finish before the Lazy updates start (see "interleaving" below).
            if (numberOfUniqueIndexesUpdated > 0)
            {
                // Updates to the unique indexes should be tentative so they are not visible to readers before making sure
                // that all uniqueness constraints are satisfied (and that the grain state persistence completes successfully).
                // UniquenessConstraintViolatedExceptions propagate; any tentative records will be removed by WorkflowQueueHandler.
                await base.ApplyIndexUpdatesEagerly(interfaceToUpdatesMap, UpdateIndexType.Unique, isTentative: true);
            }

            // Finally, the grain state is persisted if requested.
            if (writeStateIfConstraintsAreNotViolated)
            {
                // There is no constraint violation, so add the workflow ID to the list of active (committed/in-flight) workflows.
                // Note that there is no race condition allowing the lazy update to sneak in before we add these, because grain access
                // is single-threaded unless the method is marked as interleaved; this method is called from this.WriteStateAsync, which
                // is not marked as interleaved, so the queue handler call to this.GetActiveWorkflowIdsSet blocks until this method exits.
                this.AddWorkflowIdsToActiveWorkflows(interfaceToUpdatesMap.Select(kvp => interfaceToUpdatesMap.WorkflowIds[kvp.Key]).ToArray());
                await this.WriteStateAsync();
            }

            // If everything was successful, the before images are updated
            base.UpdateBeforeImages(interfaceToUpdatesMap);
        }

        /// <summary>
        /// Handles the remaining workflows of the grain 
        /// </summary>
        /// <returns>the actual list of workflow record IDs that were available in the queue(s)</returns>
        private Task<IEnumerable<Guid>> HandleRemainingWorkflows()
        {
            // A copy of WorkflowQueues is required, because we want to iterate over it and add/remove elements from/to it.
            var copyOfWorkflowQueues = new Dictionary<Type, IIndexWorkflowQueue>(this.WorkflowQueues);
            var tasks = copyOfWorkflowQueues.Select(wfqEntry => this.HandleRemainingWorkflows(wfqEntry.Key, wfqEntry.Value));
            return Task.WhenAll(tasks).ContinueWith(t => t.Result.SelectMany(res => res));
        }

        /// <summary>
        /// Handles the remaining workflows of a specific grain interface of the grain
        /// </summary>
        /// <param name="grainInterfaceType">the grain interface type being indexed</param>
        /// <param name="oldWorkflowQ">the previous workflow queue responsible for handling the updates</param>
        /// <returns>the actual list of workflow record IDs that were available in this queue</returns>
        private async Task<IEnumerable<Guid>> HandleRemainingWorkflows(Type grainInterfaceType, IIndexWorkflowQueue oldWorkflowQ)
        {
            // Keeps the reference to the reincarnated workflow queue, if the original workflow queue (GrainService) did not respond.
            IIndexWorkflowQueue reincarnatedOldWorkflowQ = null;

            // Keeps the list of workflow records from the old workflow queue.
            Immutable<List<IndexWorkflowRecord>> remainingWorkflows;

            // First, we remove the workflow queue associated with grainInterfaceType (i.e., oldWorkflowQ) so that another call to get the
            // workflow queue for grainInterfaceType gets the new workflow queue responsible for grainInterfaceType (otherwise oldWorkflowQ is returned).
            this.WorkflowQueues.Remove(grainInterfaceType);
            var newWorkflowQ = this.GetWorkflowQueue(grainInterfaceType);

            // If the same workflow queue is responsible we just check what workflow records are still in process
            if (this.SiloIndexManager.InjectableCode.AreQueuesEqual(() => newWorkflowQ.Equals(oldWorkflowQ)))
            {
                remainingWorkflows = await oldWorkflowQ.GetRemainingWorkflowsIn(this.ActiveWorkflowsSet);
                if (remainingWorkflows.Value != null && remainingWorkflows.Value.Count > 0)
                {
                    // Add an empty enumeration to make sure the queue thread is running.
                    await oldWorkflowQ.AddAllToQueue(new Immutable<List<IndexWorkflowRecord>>(new List<IndexWorkflowRecord>()));
                    return remainingWorkflows.Value.Select(w => w.WorkflowId);
                }
            }
            else // The workflow queue responsible for grainInterfaceType has changed
            {
                try
                {
                    // Get the list of remaining workflow records from oldWorkflowQ.
                    remainingWorkflows = await this.SiloIndexManager.InjectableCode
                                                   .GetRemainingWorkflowsIn(() => oldWorkflowQ.GetRemainingWorkflowsIn(this.ActiveWorkflowsSet));
                }
                catch
                {
                    // An exception means that oldWorkflowQ is not reachable. Create a reincarnatedOldWorkflowQ grain
                    // to read the state of the oldWorkflowQ to get the list of remaining workflow records.
                    reincarnatedOldWorkflowQ = await this.GetReincarnatedWorkflowQueue(oldWorkflowQ);
                    remainingWorkflows = await reincarnatedOldWorkflowQ.GetRemainingWorkflowsIn(this.ActiveWorkflowsSet);
                }

                // If any workflow is remaining unprocessed, pass their responsibility to newWorkflowQ.
                if (remainingWorkflows.Value != null && remainingWorkflows.Value.Count > 0)
                {
                    // Give the responsibility of handling the remaining workflow records to the newWorkflowQ.
                    await newWorkflowQ.AddAllToQueue(remainingWorkflows);

                    // Remove workflows from the target old workflow queue that responded to our request.
                    // We don't need to await this; worst-case, they will be processed again by the old-queue.
                    var targetOldWorkflowQueue = reincarnatedOldWorkflowQ ?? oldWorkflowQ;
                    targetOldWorkflowQueue.RemoveAllFromQueue(remainingWorkflows).Ignore();
                    return remainingWorkflows.Value.Select(w => w.WorkflowId);
                }
            }
            // If there are no remaining workflow records, an empty Enumerable is returned.
            return Enumerable.Empty<Guid>();
        }

        private async Task<IIndexWorkflowQueue> GetReincarnatedWorkflowQueue(IIndexWorkflowQueue workflowQ)
        {
            var primaryKey = workflowQ.GetPrimaryKeyString();
            var reincarnatedQ = this._grainFactory.GetGrain<IIndexWorkflowQueue>(primaryKey);
            var reincarnatedQHandler = this._grainFactory.GetGrain<IIndexWorkflowQueueHandler>(primaryKey);

            // This is called during OnActivateAsync(), so workflowQ's may be on a different silo than the
            // current grain activation.
            await Task.WhenAll(reincarnatedQ.Initialize(workflowQ), reincarnatedQHandler.Initialize(workflowQ));
            return reincarnatedQ;
        }

        private Task PruneActiveWorkflowsSetFromAlreadyHandledWorkflows(IEnumerable<Guid> workflowsInProgress)
        {
            var initialSize = this.ActiveWorkflowsSet.Count;
            this.ActiveWorkflowsSet.Clear();
            this.ActiveWorkflowsSet.UnionWith(workflowsInProgress);
            return (this.ActiveWorkflowsSet.Count != initialSize) ? this.WriteStateAsync() : Task.CompletedTask;
        }

        private void PruneWorkflowQueuesForMissingInterfaceTypes()
        {
            // Interface types may be missing if the grain definition was updated.
            var oldQueues = this.WorkflowQueues;
            this.WorkflowQueues = oldQueues.Where(kvp => base._grainIndexes.ContainsInterface(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public override Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet()
        {
            var workflowIds = this.ActiveWorkflowsSet;

            // Immutable does not prevent items from being added to the hashset; there was a race condition where
            // IndexableGrain.ApplyIndexUpdates adds to the list after IndexWorkflowQueueHandlerBase.HandleWorkflowsUntilPunctuation
            // obtains grainsToActiveWorkflows and thus IndexWorkflowQueueHandlerBase.RemoveFromActiveWorkflowsInGrainsTasks
            // removes the added workflowId, which means that workflowId is not processed. Therefore deep-copy workflows.
            var result = (workflowIds == null) ? new HashSet<Guid>() : new HashSet<Guid>(workflowIds);
            return Task.FromResult(result.AsImmutable());
        }

        public override Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowIds)
        {
            if (this.ActiveWorkflowsSet != null && this.ActiveWorkflowsSet.RemoveWhere(removedWorkflowIds.Contains) > 0)
            {
                // TODO: decide whether we need to actually write the state back to the storage or we can leave it for the next WriteStateAsync
                // on the grain itself.
                //return WriteBaseStateAsync();
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a workflow ID to the list of active workflows for this fault-tolerant indexable grain
        /// </summary>
        /// <param name="workflowIds">the workflow IDs to be added</param>
        private void AddWorkflowIdsToActiveWorkflows(Guid[] workflowIds)
        {
            if (this.ActiveWorkflowsSet == null)
            {
                this.ActiveWorkflowsSet = new HashSet<Guid>(workflowIds);
                return;
            }
            this.ActiveWorkflowsSet.AddRange(workflowIds);
        }

        /// <summary>
        /// Generates a unique Guid that does not exist in the list of active workflows.
        /// 
        /// Actually, there is a very unlikely possibility that we end up with a duplicate workflow ID in the following scenario:
        /// 1- IndexableGrain G is updated and assigned workflow ID = A
        /// 2- workflow record with ID = A is added to the index workflow queue
        /// 3- G fails and its state (including its active workflow list) is thrown away
        /// 4- G is re-activated and reads it state from storage (which does not include A in its active workflow list)
        /// 5- G gets updated and a new workflow with ID = A is generated for it.
        ///    This ID is assumed to be unique, while it actually is not unique and already exists in the workflow queue.
        /// 
        /// The only way to avoid it is using a centralized unique workflow ID generator, which can be added if necessary.
        /// </summary>
        /// <returns>a new unique workflow ID</returns>
        private Guid GenerateUniqueWorkflowId()
        {
            var workflowId = Guid.NewGuid();
            while (this.ActiveWorkflowsSet != null && this.ActiveWorkflowsSet.Contains(workflowId))
            {
                workflowId = Guid.NewGuid();
            }
            return workflowId;
        }
    }
}
