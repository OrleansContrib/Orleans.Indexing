using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Transactions.Abstractions;

namespace Orleans.Indexing.Facet
{
    class TransactionalIndexedState<TGrainState> : IndexedStateBase<TGrainState>,
                                                   ITransactionalIndexedState<TGrainState>,
                                                   ILifecycleParticipant<IGrainLifecycle>
                                                   where TGrainState : class, new()
    {
        ITransactionalState<IndexedGrainStateWrapper<TGrainState>> transactionalState;

        public TransactionalIndexedState(
                IServiceProvider sp,
                IIndexedStateConfiguration config,
                IGrainActivationContext context,
                ITransactionalStateFactory transactionalStateFactory
            ) : base(sp, config, context)
        {
            var indexingTransactionalStateConfig = new IndexingTransactionalStateConfiguration(config.StateName, config.StorageName);
            var transactionalStateConfig = new TransactionalStateConfiguration(indexingTransactionalStateConfig);
            this.transactionalState = transactionalStateFactory.Create<IndexedGrainStateWrapper<TGrainState>>(transactionalStateConfig);
        }

        public void Participate(IGrainLifecycle lifecycle) => base.Participate<TransactionalIndexedState<TGrainState>>(lifecycle);

        internal override Task OnActivateAsync(CancellationToken ct)
        {
            base.Logger.Trace($"Activating indexable grain of type {grain.GetType().Name} in silo {this.SiloIndexManager.SiloAddress}.");
            if (this.transactionalState == null)
            {
                throw new IndexOperationException("Transactional Indexed State requires calling Attach() with an additional ITransactionalState<> facet on the grain's constructor.");
            }

            // Our state is "created" via Attach(). State initialization is deferred as we must be in a transaction context to access it.
            // Transactional indexes cannot be active and thus do not call InsertIntoActiveIndexes or RemoveFromActiveIndexes.
            return Task.CompletedTask;
        }

        internal override Task OnDeactivateAsync(CancellationToken ct)
        {
            base.Logger.Trace($"Deactivating indexable grain of type {this.grain.GetType().Name} in silo {this.SiloIndexManager.SiloAddress}.");

            // Transactional indexes cannot be active and thus do not call InsertIntoActiveIndexes or RemoveFromActiveIndexes.
            return Task.CompletedTask;
        }

        #region public API

        public override Task<TResult> PerformRead<TResult>(Func<TGrainState, TResult> readFunction)
        {
            return this.transactionalState.PerformRead(wrappedState =>
            {
                this.EnsureStateInitialized(wrappedState, forUpdate:false);
                return readFunction(wrappedState.UserState);
            });
        }

        public async override Task<TResult> PerformUpdate<TResult>(Func<TGrainState, TResult> updateFunction)
        {
            // TransactionalState does the grain-state write here as well as the update, then we do athe index updates.
            var result = await this.transactionalState.PerformUpdate(wrappedState =>
            {
                this.EnsureStateInitialized(wrappedState, forUpdate:true);
                var res = updateFunction(wrappedState.UserState);

                // The property values here are ephemeral; they are re-initialized by UpdateBeforeImages in EnsureStateInitialized.
                this._grainIndexes.MapStateToProperties(wrappedState.UserState);
                return res;
            });

            var interfaceToUpdatesMap = await base.UpdateIndexes(IndexUpdateReason.WriteState, onlyUpdateActiveIndexes: false, writeStateIfConstraintsAreNotViolated: true);
            // BeforeImage update is deferred, so we don't have potentially stale values if the transaction is rolled back, e.g. if a different grain's update fails

            return result;
        }

        #endregion public API

        void EnsureStateInitialized(IndexedGrainStateWrapper<TGrainState> wrappedState, bool forUpdate)
        {
            // State initialization is deferred as we must be in a transaction context to access it.
            wrappedState.EnsureNullValues(base._grainIndexes.PropertyNullValues);
            if (forUpdate)
            {
                // Apply the deferred BeforeImage update.
                _grainIndexes.UpdateBeforeImages(wrappedState.UserState, force:true);
            }
        }

        /// <summary>
        /// Applies a set of updates to the indexes defined on the grain
        /// </summary>
        /// <param name="interfaceToUpdatesMap">the dictionary of indexes to their corresponding updates</param>
        /// <param name="updateIndexesEagerly">whether indexes should be updated eagerly or lazily; must always be true for transactional indexes</param>
        /// <param name="onlyUniqueIndexesWereUpdated">a flag to determine whether only unique indexes were updated; unused for transactional indexes</param>
        /// <param name="numberOfUniqueIndexesUpdated">determine the number of updated unique indexes; unused for transactional indexes</param>
        /// <param name="writeStateIfConstraintsAreNotViolated">whether the state should be written to storage if no constraint is violated;
        ///                                                     must always be true for transactional indexes</param>
        private protected override async Task ApplyIndexUpdates(InterfaceToUpdatesMap interfaceToUpdatesMap,
                                                                bool updateIndexesEagerly,
                                                                bool onlyUniqueIndexesWereUpdated,
                                                                int numberOfUniqueIndexesUpdated,
                                                                bool writeStateIfConstraintsAreNotViolated)
        {
            Debug.Assert(writeStateIfConstraintsAreNotViolated, "Transactional index writes must only be called when updating the grain state (not on activation change).");

            // For Transactional, the grain-state write has already been done by the time we get here.
            if (!interfaceToUpdatesMap.IsEmpty)
            {
                Debug.Assert(updateIndexesEagerly, "Transactional indexes cannot be configured to be lazy; this misconfiguration should have been caught in ValidateSingleIndex.");
                IEnumerable<Task> getIndexUpdateTasks(Type grainInterfaceType, IReadOnlyDictionary<string, IMemberUpdate> updates)
                {
                    var indexInterfaces = this._grainIndexes[grainInterfaceType];
                    foreach (var (indexName, mu) in updates.Where(kvp => kvp.Value.OperationType != IndexOperationType.None).OrderBy(kvp => kvp.Key))
                    {
                        var indexInfo = indexInterfaces.NamedIndexes[indexName];
                        var updateToIndex = new MemberUpdateOverriddenMode(mu, IndexUpdateMode.Transactional) as IMemberUpdate;
                        yield return indexInfo.IndexInterface.ApplyIndexUpdate(this.SiloIndexManager,
                                                this.iIndexableGrain, updateToIndex.AsImmutable(), indexInfo.MetaData, base.BaseSiloAddress);
                    }
                }

                // Execute each index update individually, in an invariant sequence, to avoid deadlocks when locking multiple index buckets.
                // The invariant sequence must apply across all grains, since a single indexed interface may be on multiple grains.
                // Therefore, order by interface name and within that by index name.
                // TODO performance: safely execute multiple index updates in parallel.
                foreach (var updateTask in interfaceToUpdatesMap.OrderBy(kvp => kvp.Key.FullName).SelectMany(kvp => getIndexUpdateTasks(kvp.Key, kvp.Value)))
                {
                    await updateTask;
                }
            }
        }
    }
}
