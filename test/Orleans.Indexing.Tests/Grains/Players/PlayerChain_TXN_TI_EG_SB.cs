using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class PlayerChain_TXN_TI_EG_SB : PlayerGrainTransactional<PlayerGrainState>, IPlayerChain_TXN_TI_EG_SB
    {
        public PlayerChain_TXN_TI_EG_SB(
            [TransactionalIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
