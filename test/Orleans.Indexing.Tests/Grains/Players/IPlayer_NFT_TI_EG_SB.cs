namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_TI_EG_SB : IPlayerProperties
    {
        public int Score { get; set; }

        [TotalIndex(IsEager = true)]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_TI_EG_SB : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_TI_EG_SB>
    {
    }
}
