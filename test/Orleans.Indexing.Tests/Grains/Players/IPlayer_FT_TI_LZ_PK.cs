namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_FT_TI_LZ_PK : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex(TotalIndexType.HashIndexPartitionedPerKeyHash)]
        public string Location { get; set; }
    }

    public interface IPlayer_FT_TI_LZ_PK : IPlayerGrain, IIndexableGrain<PlayerProperties_FT_TI_LZ_PK>
    {
    }
}
