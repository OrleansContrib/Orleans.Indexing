namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_AI_LZ_PSPS : IPlayerProperties
    {
        [ActiveIndex(ActiveIndexType.HashIndexPartitionedPerSilo/*, IsEager = false*/, IsUnique = false, NullValue = "0")]
        public int Score { get; set; }

        [ActiveIndex(ActiveIndexType.HashIndexPartitionedPerSilo/*, IsEager = false*/, IsUnique = false)]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_AI_LZ_PSPS : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_AI_LZ_PSPS>
    {
    }
}
