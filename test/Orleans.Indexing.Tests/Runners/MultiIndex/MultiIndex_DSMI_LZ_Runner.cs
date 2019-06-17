using Orleans.Indexing.Facet;
using Orleans.Providers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    #region PartitionedPerKey
    public class FT_Props_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public class NFT_Props_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface IFT_Grain_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_DSMI_LZ_PK>
    {
    }

    public interface INFT_Grain_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_LZ_PK>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class FT_Grain_UIUSNINS_DSMI_LZ_PK : TestMultiIndexGrainFaultTolerant<TestMultiIndexState>, IFT_Grain_UIUSNINS_DSMI_LZ_PK
    {
        public FT_Grain_UIUSNINS_DSMI_LZ_PK(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingTestConstants.CosmosDBGrainStorage)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_LZ_PK : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_DSMI_LZ_PK
    {
        public NFT_Grain_UIUSNINS_DSMI_LZ_PK(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingTestConstants.CosmosDBGrainStorage)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo
    public class FT_Props_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public class NFT_Props_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]    // PerSilo cannot be Unique
        public int UniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false)]  // PerSilo cannot be Unique
        public string UniqueString { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface IFT_Grain_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_DSMI_LZ_PS>
    {
    }

    public interface INFT_Grain_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_LZ_PS>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class FT_Grain_UIUSNINS_DSMI_LZ_PS : TestMultiIndexGrainFaultTolerant<TestMultiIndexState>, IFT_Grain_UIUSNINS_DSMI_LZ_PS
    {
        public FT_Grain_UIUSNINS_DSMI_LZ_PS(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingTestConstants.CosmosDBGrainStorage)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_LZ_PS : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_DSMI_LZ_PS
    {
        public NFT_Grain_UIUSNINS_DSMI_LZ_PS(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingTestConstants.CosmosDBGrainStorage)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerSilo

    #region SingleBucket
    public class FT_Props_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public class NFT_Props_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface IFT_Grain_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_DSMI_LZ_SB>
    {
    }

    public interface INFT_Grain_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_LZ_SB>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class FT_Grain_UIUSNINS_DSMI_LZ_SB : TestMultiIndexGrainFaultTolerant<TestMultiIndexState>, IFT_Grain_UIUSNINS_DSMI_LZ_SB
    {
        public FT_Grain_UIUSNINS_DSMI_LZ_SB(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingTestConstants.CosmosDBGrainStorage)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_LZ_SB : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_DSMI_LZ_SB
    {
        public NFT_Grain_UIUSNINS_DSMI_LZ_SB(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingTestConstants.CosmosDBGrainStorage)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // SingleBucket

    public abstract class MultiIndex_DSMI_LZ_Runner : IndexingTestRunnerBase
    {
        protected MultiIndex_DSMI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_DSMI_LZ_PK()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_DSMI_LZ_PK, FT_Props_UIUSNINS_DSMI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_LZ_PK()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_LZ_PK, NFT_Props_UIUSNINS_DSMI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_DSMI_LZ_PS()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_DSMI_LZ_PS, FT_Props_UIUSNINS_DSMI_LZ_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_LZ_PS()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_LZ_PS, NFT_Props_UIUSNINS_DSMI_LZ_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_DSMI_LZ_SB()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_DSMI_LZ_SB, FT_Props_UIUSNINS_DSMI_LZ_SB>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_LZ_SB()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_LZ_SB, NFT_Props_UIUSNINS_DSMI_LZ_SB>();
        }
    }
}
