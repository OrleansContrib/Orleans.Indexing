namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_TI_LZ_PKSB : IPlayerProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<int, IPlayer_NFT_TI_LZ_PKSB>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int Score { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IPlayer_NFT_TI_LZ_PKSB>), IsEager = true, IsUnique = false)]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_TI_LZ_PKSB : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_TI_LZ_PKSB>
    {
    }
}
