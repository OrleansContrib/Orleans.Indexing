using Xunit.Abstractions;
using Xunit;
using Orleans.Indexing.Tests.MultiInterface;
using Orleans.Indexing.Tests.SharedGrainInterfaces;

namespace Orleans.Indexing.Tests
{
    #region Players
    [TestCategory("BVT"), TestCategory("Indexing")]
    public class SimpleIndexingSingleSiloTests : SimpleIndexingSingleSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public SimpleIndexingSingleSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class SimpleIndexingTwoSiloTests : SimpleIndexingTwoSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public SimpleIndexingTwoSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class ChainedBucketIndexingSingleSiloTests : ChainedBucketIndexingSingleSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public ChainedBucketIndexingSingleSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class ChainedBucketIndexingTwoSiloTests : ChainedBucketIndexingTwoSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public ChainedBucketIndexingTwoSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class FaultTolerantIndexingSingleSiloTests : FaultTolerantIndexingSingleSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public FaultTolerantIndexingSingleSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class FaultTolerantIndexingTwoSiloTests : FaultTolerantIndexingTwoSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public FaultTolerantIndexingTwoSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class LazyIndexingSingleSiloTests : LazyIndexingSingleSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public LazyIndexingSingleSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class LazyIndexingTwoSiloTests : LazyIndexingTwoSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public LazyIndexingTwoSiloTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class NoIndexingTests : NoIndexingRunner, IClassFixture<IndexingTestFixture>
    {
        public NoIndexingTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class TransactionalPlayerTests : TransactionalPlayerRunner, IClassFixture<IndexingTestFixture>
    {
        public TransactionalPlayerTests(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    #endregion Players

    #region MultiIndex

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_AI_EG : MultiIndex_AI_EG_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiIndex_AI_EG(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_AI_LZ : MultiIndex_AI_LZ_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiIndex_AI_LZ(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_TI_EG : MultiIndex_TI_EG_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiIndex_TI_EG(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_TI_LZ : MultiIndex_TI_LZ_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiIndex_TI_LZ(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_XI_EG : MultiIndex_XI_EG_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiIndex_XI_EG(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_XI_LZ : MultiIndex_XI_LZ_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiIndex_XI_LZ(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_All : MultiIndex_All_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiIndex_All(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    #endregion MultiIndex

    #region MultiInterface

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiInterface_AI_EG : MultiInterface_AI_EG_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiInterface_AI_EG(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiInterface_AI_LZ : MultiInterface_AI_LZ_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiInterface_AI_LZ(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiInterface_TI_EG : MultiInterface_TI_EG_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiInterface_TI_EG(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiInterface_TI_LZ : MultiInterface_TI_LZ_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiInterface_TI_LZ(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiInterface_XI_EG : MultiInterface_XI_EG_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiInterface_XI_EG(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiInterface_XI_LZ : MultiInterface_XI_LZ_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiInterface_XI_LZ(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiInterface_All : MultiInterface_All_Runner, IClassFixture<IndexingTestFixture>
    {
        public MultiInterface_All(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    #endregion MultiInterface

    #region DirectStorageManagedIndexes

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_DSMI_EG : MultiIndex_DSMI_EG_Runner, IClassFixture<DSMI_EG_IndexingTestFixture>
    {
        public MultiIndex_DSMI_EG(DSMI_EG_IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class MultiIndex_DSMI_LZ : MultiIndex_DSMI_LZ_Runner, IClassFixture<DSMI_LZ_IndexingTestFixture>
    {
        public MultiIndex_DSMI_LZ(DSMI_LZ_IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }

    #endregion // DirectStorageManagedIndexes

    [TestCategory("BVT"), TestCategory("Indexing")]
    public class SharedGrainInterface : SharedGrainInterfaceRunner, IClassFixture<IndexingTestFixture>
    {
        public SharedGrainInterface(IndexingTestFixture fixture, ITestOutputHelper output) : base(fixture, output) { }
    }
}
