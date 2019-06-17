using Orleans.Indexing.Facet;

namespace Orleans.Indexing.Tests
{
    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    public class PlayerChain_FT_TI_EG_PK : PlayerGrainNonFaultTolerant<PlayerGrainState>, IPlayerChain_FT_TI_EG_PK
    {
        public PlayerChain_FT_TI_EG_PK(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }
    }
}
