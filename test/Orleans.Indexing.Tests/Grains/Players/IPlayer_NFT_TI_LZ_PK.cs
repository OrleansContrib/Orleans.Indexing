namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_TI_LZ_PK : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex(TotalIndexType.HashIndexPartitionedPerKeyHash/*, IsEager = false*/)]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_TI_LZ_PK : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_TI_LZ_PK>
    {
    }
}
