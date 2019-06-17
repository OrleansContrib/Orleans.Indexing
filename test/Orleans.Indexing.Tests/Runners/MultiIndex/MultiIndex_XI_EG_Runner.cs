using Orleans.Indexing.Facet;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    // NFT only; FT cannot be configured to be Eager.
    // These are the only supported combinations; Active Indexes cannot be FT or TXN, or partitioned PerKey or SingleBucket;
    // Total Indexes cannot be partitioned PerSilo; and FT and NFT cannot be mixed on a single grain.

    public class NFT_Props_UIUSNINS_XI_EG : ITestMultiIndexProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_UIUSNINS_XI_EG>), IsEager = true, IsUnique = false, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_UIUSNINS_XI_EG>), IsEager = true, IsUnique = true)]
        public string UniqueString { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_UIUSNINS_XI_EG>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_UIUSNINS_XI_EG>), IsEager = true, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface INFT_Grain_UIUSNINS_XI_EG : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_XI_EG>
    {
    }

    public class NFT_Grain_UIUSNINS_XI_EG : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_XI_EG
    {
        public NFT_Grain_UIUSNINS_XI_EG(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }

    public abstract class MultiIndex_XI_EG_Runner : IndexingTestRunnerBase
    {
        protected MultiIndex_XI_EG_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }


        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_XI_EG_PK()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_XI_EG, NFT_Props_UIUSNINS_XI_EG>();
        }

        internal static Func<IndexingTestRunnerBase, int, Task>[] GetAllTestTasks()
        {
            return new Func<IndexingTestRunnerBase, int, Task>[]
            {
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_XI_EG, NFT_Props_UIUSNINS_XI_EG>(intAdjust)
            };
        }
    }
}
