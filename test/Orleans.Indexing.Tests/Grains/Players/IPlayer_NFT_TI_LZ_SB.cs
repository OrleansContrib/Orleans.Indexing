namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_TI_LZ_SB : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex/*(IsEager = false)*/]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_TI_LZ_SB : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_TI_LZ_SB>
    {
    }
}
