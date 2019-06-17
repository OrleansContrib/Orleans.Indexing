using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    public class TransactionalPlayerGrain : PlayerGrainTransactional<PlayerGrainState>, ITransactionalPlayerGrain
    {
        public TransactionalPlayerGrain(
            [TransactionalIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
