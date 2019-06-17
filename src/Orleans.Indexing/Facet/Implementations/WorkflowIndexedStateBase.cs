using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    internal abstract class WorkflowIndexedStateBase<TGrainState, TWrappedState> : IndexedStateBase<TGrainState>
        where TGrainState : class, new()
        where TWrappedState: IndexedGrainStateWrapper<TGrainState>, new()
    {
        private protected NonTransactionalState<TWrappedState> nonTransactionalState;

        public WorkflowIndexedStateBase(IServiceProvider sp, IIndexedStateConfiguration config, IGrainActivationContext context)
            : base(sp, config, context)
        {
        }

        // A cache for the workflow queues, one for each grain interface type that the current IndexableGrain implements
        internal virtual IDictionary<Type, IIndexWorkflowQueue> WorkflowQueues { get; set; }

        internal override Task OnDeactivateAsync(CancellationToken ct)
        {
            base.Logger.Trace($"Deactivating indexable grain of type {base.grain.GetType().Name} in silo {this.SiloIndexManager.SiloAddress}.");
            return this.RemoveFromActiveIndexes();
        }

        #region public API

        public override Task<TResult> PerformRead<TResult>(Func<TGrainState, TResult> readFunction)
            => this.nonTransactionalState.PerformRead(wrappedState => readFunction(wrappedState.UserState));

        public async override Task<TResult> PerformUpdate<TResult>(Func<TGrainState, TResult> updateFunction)
        {
            // NonTransactionalState only does the grain-state update here; we then incorporate its write into the
            // index-update workflow via WriteStateAsync().
            var result = await this.nonTransactionalState.PerformUpdate(wrappedState => updateFunction(wrappedState.UserState));
            this._grainIndexes.MapStateToProperties(this.nonTransactionalState.State.UserState);
            await base.UpdateIndexes(IndexUpdateReason.WriteState, onlyUpdateActiveIndexes: false, writeStateIfConstraintsAreNotViolated: true);
            return result;
        }

        #endregion public API

        private protected Task WriteStateAsync() => this.nonTransactionalState.PerformUpdate();

        private protected async Task InitializeState()
        {
            var storage = base.SiloIndexManager.GetStorageBridge<TWrappedState>(base.grain, base.IndexedStateConfig.StorageName);
            this.nonTransactionalState = await NonTransactionalState<TWrappedState>.CreateAsync(storage);
            await this.PerformRead();

            this.nonTransactionalState.State.EnsureNullValues(base._grainIndexes.PropertyNullValues);
            base._grainIndexes.AddMissingBeforeImages(this.nonTransactionalState.State.UserState);
        }

        private protected Task FinishActivateAsync()
        {
            Debug.Assert(this.grain != null, "Initialize() not called");
            return this.InsertIntoActiveIndexes();
        }

        /// <summary>
        /// Inserts the current grain to the active indexes only if it already has a persisted state
        /// </summary>
        protected Task InsertIntoActiveIndexes()
        {
            // Check if it contains anything to be indexed
            return this._grainIndexes.HasIndexImages
                ? this.UpdateIndexes(IndexUpdateReason.OnActivate, onlyUpdateActiveIndexes: true, writeStateIfConstraintsAreNotViolated: false)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Removes the current grain from active indexes
        /// </summary>
        protected Task RemoveFromActiveIndexes()
        {
            // Check if it has anything indexed
            return this._grainIndexes.HasIndexImages
                ? this.UpdateIndexes(IndexUpdateReason.OnDeactivate, onlyUpdateActiveIndexes: true, writeStateIfConstraintsAreNotViolated: false)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Eagerly Applies updates to the indexes defined on this grain
        /// </summary>
        /// <param name="interfaceToUpdatesMap">the dictionary of updates for each index of each interface</param>
        /// <param name="updateIndexTypes">indicates whether unique and/or non-unique indexes should be updated</param>
        /// <param name="isTentative">indicates whether updates to indexes should be tentatively done. That is, the update
        ///     won't be visible to readers, but prevents writers from overwriting them and violating constraints</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected Task ApplyIndexUpdatesEagerly(InterfaceToUpdatesMap interfaceToUpdatesMap,
                                                    UpdateIndexType updateIndexTypes, bool isTentative = false)
            => Task.WhenAll(interfaceToUpdatesMap.Select(kvp => this.ApplyIndexUpdatesEagerly(kvp.Key, kvp.Value, updateIndexTypes, isTentative)));

        /// <summary>
        /// Eagerly Applies updates to the indexes defined on this grain for a single grain interface type implemented by this grain
        /// </summary>
        /// <param name="grainInterfaceType">a single indexable grain interface type implemented by this grain</param>
        /// <param name="updates">the dictionary of updates for each index</param>
        /// <param name="updateIndexTypes">indicates whether unique and/or non-unique indexes should be updated</param>
        /// <param name="isTentative">indicates whether updates to indexes should be tentatively done. That is, the update
        ///     won't be visible to readers, but prevents writers from overwriting them and violating constraints</param>
        /// <returns></returns>
        private protected Task ApplyIndexUpdatesEagerly(Type grainInterfaceType, IReadOnlyDictionary<string, IMemberUpdate> updates,
                                                        UpdateIndexType updateIndexTypes, bool isTentative)
        {
            var indexInterfaces = this._grainIndexes[grainInterfaceType];
            IEnumerable<Task<bool>> getUpdateTasks()
            {
                foreach (var (indexName, mu) in updates.Where(kvp => kvp.Value.OperationType != IndexOperationType.None))
                {
                    var indexInfo = indexInterfaces.NamedIndexes[indexName];
                    if (updateIndexTypes.HasFlag(indexInfo.MetaData.IsUniqueIndex ? UpdateIndexType.Unique : UpdateIndexType.NonUnique))
                    {
                        // If the caller asks for the update to be tentative, then it will be wrapped inside a MemberUpdateTentative
                        var updateToIndex = isTentative ? new MemberUpdateOverriddenMode(mu, IndexUpdateMode.Tentative) : mu;
                        yield return indexInfo.IndexInterface.ApplyIndexUpdate(this.SiloIndexManager,
                                             this.iIndexableGrain, updateToIndex.AsImmutable(), indexInfo.MetaData, this.BaseSiloAddress);
                    }
                }
            }

            // At the end, because the index update should be eager, we wait for all index update tasks to finish
            return Task.WhenAll(getUpdateTasks());
        }

        /// <summary>
        /// Lazily applies updates to the indexes defined on this grain
        /// 
        /// The lazy update involves adding a workflow record to the corresponding IIndexWorkflowQueue for this grain.
        /// </summary>
        /// <param name="interfaceToUpdatesMap">the dictionary of updates for each index by interface</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected Task ApplyIndexUpdatesLazily(InterfaceToUpdatesMap interfaceToUpdatesMap)
            => Task.WhenAll(interfaceToUpdatesMap.Select(kvp => this.GetWorkflowQueue(kvp.Key).AddToQueue(new IndexWorkflowRecord(interfaceToUpdatesMap.WorkflowIds[kvp.Key],
                                                                                                       base.iIndexableGrain, kvp.Value).AsImmutable())));

        private protected void UpdateBeforeImages(InterfaceToUpdatesMap interfaceToUpdatesMap)
            => this._grainIndexes.UpdateBeforeImages(interfaceToUpdatesMap);

        /// <summary>
        /// Find the corresponding workflow queue for a given grain interface type that the current IndexableGrain implements
        /// </summary>
        /// <param name="grainInterfaceType">the given indexable grain interface type</param>
        /// <returns>the workflow queue corresponding to the <paramref name="grainInterfaceType"/></returns>
        internal IIndexWorkflowQueue GetWorkflowQueue(Type grainInterfaceType)
        {
            if (this.WorkflowQueues == null)
            {
                this.WorkflowQueues = new Dictionary<Type, IIndexWorkflowQueue>();
            }

            return this.WorkflowQueues.GetOrAdd(grainInterfaceType,
                () => IndexWorkflowQueueBase.GetIndexWorkflowQueueFromGrainHashCode(this.SiloIndexManager, grainInterfaceType,
                        this.grain.AsReference<IIndexableGrain>(this.SiloIndexManager, grainInterfaceType).GetHashCode(), this.BaseSiloAddress));
        }
    }
}
