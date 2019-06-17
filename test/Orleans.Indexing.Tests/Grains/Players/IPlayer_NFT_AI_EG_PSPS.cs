namespace Orleans.Indexing.Tests
{
    public class PlayerProperties_NFT_AI_EG_PSPS : IPlayerProperties
    {
        [ActiveIndex(ActiveIndexType.HashIndexPartitionedPerSilo, IsEager = true, IsUnique = false, NullValue = "0")]
        public int Score { get; set; }

        [ActiveIndex(ActiveIndexType.HashIndexPartitionedPerSilo, IsEager = true, IsUnique = false)]
        public string Location { get; set; }
    }

    public interface IPlayer_NFT_AI_EG_PSPS : IPlayerGrain, IIndexableGrain<PlayerProperties_NFT_AI_EG_PSPS>
    {
    }
}
