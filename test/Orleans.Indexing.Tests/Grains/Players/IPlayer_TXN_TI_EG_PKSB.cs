namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_TXN_TI_EG_PKSB : IPlayerProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<int, IPlayer_TXN_TI_EG_PKSB>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int Score { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IPlayer_TXN_TI_EG_PKSB>), IsEager = true, IsUnique = false)]
        public string Location { get; set; }
    }

    public interface IPlayer_TXN_TI_EG_PKSB : IPlayerGrainTransactional, IIndexableGrain<PlayerProperties_TXN_TI_EG_PKSB>
    {
    }
}
