namespace Orleans.Indexing.Tests
{
    public class PlayerChainProperties_FT_TI_EG_SB : IPlayerProperties
    {
        [TotalIndex(IsEager = true, NullValue = "0")]
        public int Score { get; set; }
        
        [TotalIndex(TotalIndexType.HashIndexSingleBucket, IsEager = true, MaxEntriesPerBucket = 5)]
        public string Location { get; set; }
    }

    public interface IPlayerChain_FT_TI_EG_SB : IPlayerGrain, IIndexableGrain<PlayerChainProperties_FT_TI_EG_SB>
    {
    }
}
