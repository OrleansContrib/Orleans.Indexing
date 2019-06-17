namespace Orleans.Indexing.Tests
{
    public class PlayerChainProperties_TXN_TI_EG_SB : IPlayerProperties
    {
        [TotalIndex(IsEager = true, NullValue = "0")]
        public int Score { get; set; }
        
        [TotalIndex(TotalIndexType.HashIndexSingleBucket, IsEager = true, MaxEntriesPerBucket = 5)]
        public string Location { get; set; }
    }

    public interface IPlayerChain_TXN_TI_EG_SB : IPlayerGrainTransactional, IIndexableGrain<PlayerChainProperties_TXN_TI_EG_SB>
    {
    }
}
