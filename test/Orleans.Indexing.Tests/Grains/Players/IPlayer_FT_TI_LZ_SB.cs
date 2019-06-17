namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_FT_TI_LZ_SB : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex]
        public string Location { get; set; }
    }

    public interface IPlayer_FT_TI_LZ_SB : IPlayerGrain, IIndexableGrain<PlayerProperties_FT_TI_LZ_SB>
    {
    }
}
