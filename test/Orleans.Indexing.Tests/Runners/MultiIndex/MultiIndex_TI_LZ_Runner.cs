using Orleans.Indexing.Facet;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    #region PartitionedPerKey
    public class FT_Props_UIUSNINS_TI_LZ_PK : ITestMultiIndexProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<int, IFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, IFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public class NFT_Props_UIUSNINS_TI_LZ_PK : ITestMultiIndexProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_UIUSNINS_TI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface IFT_Grain_UIUSNINS_TI_LZ_PK : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_TI_LZ_PK>
    {
    }

    public interface INFT_Grain_UIUSNINS_TI_LZ_PK : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_TI_LZ_PK>
    {
    }

    public class FT_Grain_UIUSNINS_TI_LZ_PK : TestMultiIndexGrainFaultTolerant<TestMultiIndexState>, IFT_Grain_UIUSNINS_TI_LZ_PK
    {
        public FT_Grain_UIUSNINS_TI_LZ_PK(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }

    public class NFT_Grain_UIUSNINS_TI_LZ_PK : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_TI_LZ_PK
    {
        public NFT_Grain_UIUSNINS_TI_LZ_PK(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // None; Total indexes cannot be specified as partitioned per silo.

    #endregion // PartitionedPerSilo

    #region SingleBucket
    public class FT_Props_UIUSNINS_TI_LZ_SB : ITestMultiIndexProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<int, IFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, IFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public class NFT_Props_UIUSNINS_TI_LZ_SB : ITestMultiIndexProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_UIUSNINS_TI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface IFT_Grain_UIUSNINS_TI_LZ_SB : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_TI_LZ_SB>
    {
    }

    public interface INFT_Grain_UIUSNINS_TI_LZ_SB : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_TI_LZ_SB>
    {
    }

    public class FT_Grain_UIUSNINS_TI_LZ_SB : TestMultiIndexGrainFaultTolerant<TestMultiIndexState>, IFT_Grain_UIUSNINS_TI_LZ_SB
    {
        public FT_Grain_UIUSNINS_TI_LZ_SB(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }

    public class NFT_Grain_UIUSNINS_TI_LZ_SB : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_TI_LZ_SB
    {
        public NFT_Grain_UIUSNINS_TI_LZ_SB(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // SingleBucket

    public abstract class MultiIndex_TI_LZ_Runner: IndexingTestRunnerBase
    {
        protected MultiIndex_TI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_TI_LZ_PK()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_TI_LZ_PK, FT_Props_UIUSNINS_TI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_TI_LZ_PK()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_TI_LZ_PK, NFT_Props_UIUSNINS_TI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_TI_LZ_SB()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_TI_LZ_SB, FT_Props_UIUSNINS_TI_LZ_SB>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_TI_LZ_SB()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_TI_LZ_SB, NFT_Props_UIUSNINS_TI_LZ_SB>();
        }

        internal static Func<IndexingTestRunnerBase, int, Task>[] GetAllTestTasks()
        {
            return new Func<IndexingTestRunnerBase, int, Task>[]
            {
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_TI_LZ_PK, FT_Props_UIUSNINS_TI_LZ_PK>(intAdjust),
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_TI_LZ_PK, NFT_Props_UIUSNINS_TI_LZ_PK>(intAdjust),
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_TI_LZ_SB, FT_Props_UIUSNINS_TI_LZ_SB>(intAdjust),
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_TI_LZ_SB, NFT_Props_UIUSNINS_TI_LZ_SB>(intAdjust)
            };
        }
    }
}
