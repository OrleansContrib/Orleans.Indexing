using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represents a player in a game
    /// </summary>
    public class Player_FT_TI_LZ_SB : PlayerGrainFaultTolerant<PlayerGrainState>, IPlayer_FT_TI_LZ_SB
    {
        public Player_FT_TI_LZ_SB(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
