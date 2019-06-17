using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    // This requires TWrappedState because it is subclassed by FaultTolerantWorkflowIndexedState.
    internal class NonFaultTolerantWorkflowIndexedState<TGrainState, TWrappedState> : WorkflowIndexedStateBase<TGrainState, TWrappedState>,
                                                                                      INonFaultTolerantWorkflowIndexedState<TGrainState>,
                                                                                      ILifecycleParticipant<IGrainLifecycle>
                                                                                      where TGrainState : class, new()
                                                                                      where TWrappedState: IndexedGrainStateWrapper<TGrainState>, new()
    {
        public NonFaultTolerantWorkflowIndexedState(
                IServiceProvider sp,
                IIndexedStateConfiguration config,
                IGrainActivationContext context
            ) : base(sp, config, context)
        {
            base.getWorkflowIdFunc = () => Guid.NewGuid();
        }

        public void Participate(IGrainLifecycle lifecycle) => base.Participate<NonFaultTolerantWorkflowIndexedState<TGrainState, TWrappedState>>(lifecycle);

        internal override async Task OnActivateAsync(CancellationToken ct)
        {
            Debug.Assert(!(this is FaultTolerantWorkflowIndexedState<TGrainState>));    // Ensure this is overridden
            base.Logger.Trace($"Activating indexable grain of type {grain.GetType().Name} in silo {this.SiloIndexManager.SiloAddress}.");
            await base.InitializeState();
            await base.FinishActivateAsync();
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
                                                                bool updateIndexesEagerly,
                                                                bool onlyUniqueIndexesWereUpdated,
                                                                int numberOfUniqueIndexesUpdated,
                                                                bool writeStateIfConstraintsAreNotViolated)
        {
            // If there is no update to the indexes, we should only write back the state of the grain, if requested.
            if (interfaceToUpdatesMap.IsEmpty)
            {
                if (writeStateIfConstraintsAreNotViolated)
                {
                    await this.WriteStateAsync();
                }
                return;
            }

            // HashIndexBucketState will not actually perform an index removal (Delete) if the index is not marked tentative.
            // Therefore we must do a two-step approach here; mark a tentative Delete, then do the non-tentative Delete.
            var updateEagerUniqueIndexesTentatively = numberOfUniqueIndexesUpdated > 1 || interfaceToUpdatesMap.HasAnyDeletes;

            // Apply any unique index updates eagerly.
            if (numberOfUniqueIndexesUpdated > 0)
            {
                try
                {
                    // If there is more than one unique index to update, then updates to the unique indexes should be tentative
                    // so they are not visible to readers before making sure that all uniqueness constraints are satisfied.
                    await this.ApplyIndexUpdatesEagerly(interfaceToUpdatesMap, UpdateIndexType.Unique, updateEagerUniqueIndexesTentatively);
                }
                catch (UniquenessConstraintViolatedException ex)
                {
                    // If any uniqueness constraint is violated and we have more than one unique index defined, then all tentative
                    // updates must be undone, then the exception is thrown back to the user code.
                    if (updateEagerUniqueIndexesTentatively)
                    {
                        await this.UndoTentativeChangesToUniqueIndexesEagerly(interfaceToUpdatesMap);
                    }
                    throw ex;
                }
            }

            if (updateIndexesEagerly)
            {
                var updateIndexTypes = UpdateIndexType.None;
                if (updateEagerUniqueIndexesTentatively) updateIndexTypes |= UpdateIndexType.Unique;
                if (!onlyUniqueIndexesWereUpdated) updateIndexTypes |= UpdateIndexType.NonUnique;

                if (updateIndexTypes != UpdateIndexType.None || writeStateIfConstraintsAreNotViolated)
                {
                    await Task.WhenAll(new[]
                    {
                        updateIndexTypes != UpdateIndexType.None ? base.ApplyIndexUpdatesEagerly(interfaceToUpdatesMap, updateIndexTypes, isTentative: false) : null,
                        writeStateIfConstraintsAreNotViolated ? this.WriteStateAsync() : null
                    }.Coalesce());
                }
            }
            else // !updateIndexesEagerly
            {
                this.ApplyIndexUpdatesLazilyWithoutWait(interfaceToUpdatesMap);
                if (writeStateIfConstraintsAreNotViolated)
                {
                    await this.WriteStateAsync();
                }
            }

            // If everything was successful, the before images are updated
            this.UpdateBeforeImages(interfaceToUpdatesMap);
        }

        private Task UndoTentativeChangesToUniqueIndexesEagerly(InterfaceToUpdatesMap interfaceToUpdatesMap)
            => Task.WhenAll(interfaceToUpdatesMap.Select(kvp => base.ApplyIndexUpdatesEagerly(kvp.Key, MemberUpdateReverseTentative.Reverse(kvp.Value),
                                                                                              UpdateIndexType.Unique, isTentative: false)));

        /// <summary>
        /// Lazily Applies updates to the indexes defined on this grain
        /// 
        /// The lazy update involves adding a workflow record to the corresponding IIndexWorkflowQueue for this grain.
        /// </summary>
        /// <param name="updatesByInterface">the dictionary of updates for each index by interface</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyIndexUpdatesLazilyWithoutWait(InterfaceToUpdatesMap updatesByInterface)
            => base.ApplyIndexUpdatesLazily(updatesByInterface).Ignore();
    }
}
