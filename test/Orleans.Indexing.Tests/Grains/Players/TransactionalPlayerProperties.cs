namespace Orleans.Indexing.Tests
{
    public class TransactionalPlayerProperties : IPlayerProperties
    {
        [TotalIndex(TotalIndexType.HashIndexSingleBucket, IsEager = true, NullValue = "0")]
        public int Score { get; set; }

        [TotalIndex(TotalIndexType.HashIndexPartitionedPerKeyHash, IsEager = true)]
        public string Location { get; set; }
    }
}
