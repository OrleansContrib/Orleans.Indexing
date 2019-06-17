using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class Player_FT_TI_LZ_PK : PlayerGrainFaultTolerant<PlayerGrainState>, IPlayer_FT_TI_LZ_PK
    {
        public Player_FT_TI_LZ_PK(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingTestConstants.GrainStore)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
