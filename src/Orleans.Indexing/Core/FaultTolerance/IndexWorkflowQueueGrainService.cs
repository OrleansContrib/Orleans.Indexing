using Orleans.Concurrency;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// To minimize the number of RPCs, we process index updates for each grain on the silo where the grain is active. To do this processing, each silo
    /// has one or more <see cref="IndexWorkflowQueueGrainService"/>s for each grain class, up to the number of hardware threads. A GrainService is a grain that
    /// belongs to a specific silo.
    /// + Each of these GrainServices has a queue of workflowRecords, which describe updates that must be propagated to indexes. Each workflowRecord contains
    ///   the following information:
    ///    - workflowID: grainID + a sequence number
    ///    - memberUpdates: the updated values of indexed fields
    ///  
    ///   Ordinarily, these workflowRecords are for grains that are active on <see cref="IndexWorkflowQueueGrainService"/>'s silo. (This may not be true for
    ///   short periods when a grain migrates to another silo or after the silo recovers from failure).
    /// 
    /// + The <see cref="IndexWorkflowQueueGrainService"/> grain Q has a dictionary updatesOnWait is an in-memory dictionary that maps each grain G to the
    ///   workflowRecords for G that are waiting for be updated.
    /// </summary>
    [StorageProvider(ProviderName = IndexingConstants.INDEXING_WORKFLOWQUEUE_STORAGE_PROVIDER_NAME)]
    [Reentrant]
    internal class IndexWorkflowQueueGrainService : GrainService, IIndexWorkflowQueue
    {
        private IndexWorkflowQueueBase _base;

        internal IndexWorkflowQueueGrainService(SiloIndexManager siloIndexManager, Type grainInterfaceType, int queueSequenceNumber, bool isDefinedAsFaultTolerantGrain)
            : base(IndexWorkflowQueueBase.CreateIndexWorkflowQueueGrainReference(siloIndexManager, grainInterfaceType, queueSequenceNumber, siloIndexManager.SiloAddress).GrainIdentity,
                                                                                 siloIndexManager.Silo, siloIndexManager.LoggerFactory)
        {
            _base = new IndexWorkflowQueueBase(siloIndexManager, grainInterfaceType, queueSequenceNumber, siloIndexManager.SiloAddress, isDefinedAsFaultTolerantGrain,
                                               () => base.GetGrainReference()); // lazy is needed because the runtime isn't attached until Registered
        }

        public Task AddAllToQueue(Immutable<List<IndexWorkflowRecord>> workflowRecords)
            => _base.AddAllToQueue(workflowRecords);

        public Task AddToQueue(Immutable<IndexWorkflowRecord> workflowRecord)
            => _base.AddToQueue(workflowRecord);

        public Task<Immutable<List<IndexWorkflowRecord>>> GetRemainingWorkflowsIn(HashSet<Guid> activeWorkflowsSet)
            =>_base.GetRemainingWorkflowsIn(activeWorkflowsSet);

        public Task<Immutable<IndexWorkflowRecordNode>> GiveMoreWorkflowsOrSetAsIdle()
            =>_base.GiveMoreWorkflowsOrSetAsIdle();

        public Task RemoveAllFromQueue(Immutable<List<IndexWorkflowRecord>> workflowRecords)
            => _base.RemoveAllFromQueue(workflowRecords);

        public Task Initialize(IIndexWorkflowQueue oldParentGrainService)
            => throw new NotSupportedException();
    }
}
