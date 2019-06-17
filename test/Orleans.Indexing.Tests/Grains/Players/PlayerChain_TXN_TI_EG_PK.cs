using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class PlayerChain_TXN_TI_EG_PK : PlayerGrainTransactional<PlayerGrainState>, IPlayerChain_TXN_TI_EG_PK
    {
        public PlayerChain_TXN_TI_EG_PK(
            [TransactionalIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
