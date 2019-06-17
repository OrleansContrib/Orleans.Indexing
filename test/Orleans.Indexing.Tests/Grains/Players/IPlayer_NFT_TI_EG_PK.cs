namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_TI_EG_PK : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex(TotalIndexType.HashIndexPartitionedPerKeyHash, IsEager = true)]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_TI_EG_PK : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_TI_EG_PK>
    {
    }
}
