namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_AI_LZ_PS : IPlayerProperties
    {
        public int Score { get; set; }

        [ActiveIndex(ActiveIndexType.HashIndexPartitionedPerSilo/*, IsEager = false*/)]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_AI_LZ_PS : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_AI_LZ_PS>
    {
    }
}
