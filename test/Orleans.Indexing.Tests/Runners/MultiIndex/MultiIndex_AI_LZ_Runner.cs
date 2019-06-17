using Orleans.Indexing.Facet;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    #region PartitionedPerKey

    // Active Indexes cannot be FT
    // Active Indexes cannot be partitioned PerKey

    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // Active Indexes cannot be FT

    public class NFT_Props_UIUSNINS_AI_LZ_PS : ITestMultiIndexProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_UIUSNINS_AI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "-1")] // PerSilo cannot be Unique
        public int UniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_UIUSNINS_AI_LZ_PS>), IsEager = false, IsUnique = false)]    // PerSilo cannot be Unique
        public string UniqueString { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_UIUSNINS_AI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_UIUSNINS_AI_LZ_PS>), IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface INFT_Grain_UIUSNINS_AI_LZ_PS : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_AI_LZ_PS>
    {
    }

    public class NFT_Grain_UIUSNINS_AI_LZ_PS : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_AI_LZ_PS
    {
        public NFT_Grain_UIUSNINS_AI_LZ_PS(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerSilo

    #region SingleBucket

    // Active Indexes cannot be FT
    // Active Indexes cannot be partitioned SingleBucket

    #endregion // SingleBucket

    public abstract class MultiIndex_AI_LZ_Runner: IndexingTestRunnerBase
    {
        protected MultiIndex_AI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_AI_LZ_PS()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_AI_LZ_PS, NFT_Props_UIUSNINS_AI_LZ_PS>();
        }

        internal static Func<IndexingTestRunnerBase, int, Task>[] GetAllTestTasks()
        {
            return new Func<IndexingTestRunnerBase, int, Task>[]
            {
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_AI_LZ_PS, NFT_Props_UIUSNINS_AI_LZ_PS>(intAdjust)
            };
        }
    }
}
