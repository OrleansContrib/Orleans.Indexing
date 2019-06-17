using Xunit.Abstractions;
using Xunit;

namespace Orleans.Indexing.Tests.Recovery
{
    [TestCategory("BVT"), TestCategory("Indexing"), TestCategory("IndexingRecovery")]
    public class FaultTolerantGrainRecoverySingleSiloTests : FaultTolerantGrainRecoverySingleSiloRunner, IClassFixture<IndexingGrainRecoveryTestFixture>
    {
        public FaultTolerantGrainRecoverySingleSiloTests(IndexingGrainRecoveryTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing"), TestCategory("IndexingRecovery")]
    public class FaultTolerantQueueRecoverySingleSiloTests : FaultTolerantQueueRecoverySingleSiloRunner, IClassFixture<IndexingQueueRecoveryTestFixture>
    {
        public FaultTolerantQueueRecoverySingleSiloTests(IndexingQueueRecoveryTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }
}
