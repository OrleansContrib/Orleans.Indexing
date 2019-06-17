namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_TXN_TI_EG_SB : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex(TotalIndexType.HashIndexSingleBucket, IsEager = true)]
        public string Location { get; set; }
    }

    public interface IPlayer_TXN_TI_EG_SB : IPlayerGrainTransactional, IIndexableGrain<PlayerProperties_TXN_TI_EG_SB>
    {
    }
}
