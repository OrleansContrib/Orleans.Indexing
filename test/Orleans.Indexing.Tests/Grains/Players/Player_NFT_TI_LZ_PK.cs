using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player_NFT_TI_LZ_PK : PlayerGrainNonFaultTolerant<PlayerGrainState>, IPlayer_NFT_TI_LZ_PK
    {
        public Player_NFT_TI_LZ_PK(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
