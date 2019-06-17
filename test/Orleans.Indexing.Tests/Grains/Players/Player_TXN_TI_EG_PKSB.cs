using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player_TXN_TI_EG_PKSB : PlayerGrainTransactional<PlayerGrainState>, IPlayer_TXN_TI_EG_PKSB
    {
        public Player_TXN_TI_EG_PKSB(
            [TransactionalIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
