namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_TXN_TI_EG_PK : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex(TotalIndexType.HashIndexPartitionedPerKeyHash, IsEager = true)]
        public string Location { get; set; }
    }

    public interface IPlayer_TXN_TI_EG_PK : IPlayerGrainTransactional, IIndexableGrain<PlayerProperties_TXN_TI_EG_PK>
    {
    }
}
