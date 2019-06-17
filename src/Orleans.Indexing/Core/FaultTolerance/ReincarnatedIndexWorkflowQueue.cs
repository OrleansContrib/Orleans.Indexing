using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    [Reentrant]
    internal class ReincarnatedIndexWorkflowQueue : Grain, IIndexWorkflowQueue
    {
        internal static TimeSpan ACTIVE_FOR_A_DAY = TimeSpan.FromDays(1);
        private IndexWorkflowQueueBase _base;

        internal SiloIndexManager SiloIndexManager => IndexManager.GetSiloIndexManager(ref __siloIndexManager, base.ServiceProvider);
        private SiloIndexManager __siloIndexManager;

        public override Task OnActivateAsync()
        {
            DelayDeactivation(ACTIVE_FOR_A_DAY);
            return base.OnActivateAsync();
        }

        public Task Initialize(IIndexWorkflowQueue oldParentGrainService)
        {
            if (_base == null)
            {
                GrainReference oldParentGrainServiceRef = oldParentGrainService.AsWeaklyTypedReference();
                string[] parts = oldParentGrainServiceRef.GetPrimaryKeyString().Split('-');
                if (parts.Length != 2)
                {
                    throw new WorkflowIndexException("The primary key for IndexWorkflowQueueGrainService should only contain a single special character '-', while it contains multiple." +
                                                     " The primary key is '" + oldParentGrainServiceRef.GetPrimaryKeyString() + "'");
                }

                Type grainInterfaceType = this.SiloIndexManager.CachedTypeResolver.ResolveType(parts[0]);
                int queueSequenceNumber = int.Parse(parts[1]);

                _base = new IndexWorkflowQueueBase(this.SiloIndexManager, grainInterfaceType, queueSequenceNumber,
                                                   oldParentGrainServiceRef.GrainServiceSiloAddress,
                                                   isDefinedAsFaultTolerantGrain: true /*otherwise it shouldn't have reached here!*/,
                                                   parentFunc: () => this.AsWeaklyTypedReference(), recoveryGrainReference:oldParentGrainServiceRef);
            }
            return Task.CompletedTask;
        }

        public Task AddAllToQueue(Immutable<List<IndexWorkflowRecord>> workflowRecords)
            => _base.AddAllToQueue(workflowRecords);

        public Task AddToQueue(Immutable<IndexWorkflowRecord> workflowRecord)
            => _base.AddToQueue(workflowRecord);

        public Task<Immutable<List<IndexWorkflowRecord>>> GetRemainingWorkflowsIn(HashSet<Guid> activeWorkflowsSet)
            => _base.GetRemainingWorkflowsIn(activeWorkflowsSet);

        public Task<Immutable<IndexWorkflowRecordNode>> GiveMoreWorkflowsOrSetAsIdle()
            => _base.GiveMoreWorkflowsOrSetAsIdle();

        public Task RemoveAllFromQueue(Immutable<List<IndexWorkflowRecord>> workflowRecords)
            => _base.RemoveAllFromQueue(workflowRecords);
    }
}
