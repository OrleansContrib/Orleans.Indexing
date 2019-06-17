using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Orleans.Indexing.Facet;
using System.Collections.Generic;

namespace Orleans.Indexing.Tests.MultiInterface
{
    // These are the only supported combinations; Active Indexes cannot be FT or TXN, or partitioned PerKey or SingleBucket;
    // Total Indexes cannot be partitioned PerSilo; and FT and NFT cannot be mixed on a single grain.

    public class NFT_Props_Person_XI_LZ : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Person_XI_LZ>), IsEager = false, IsUnique = false)]
        public string Name { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Person_XI_LZ>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class NFT_Props_Job_XI_LZ : IJobProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_XI_LZ>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Job_XI_LZ>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public class NFT_Props_Employee_XI_LZ : IEmployeeProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Employee_XI_LZ>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public interface INFT_Grain_Person_XI_LZ : IIndexableGrain<NFT_Props_Person_XI_LZ>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_XI_LZ : IIndexableGrain<NFT_Props_Job_XI_LZ>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_XI_LZ : IIndexableGrain<NFT_Props_Employee_XI_LZ>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public class NFT_Grain_Employee_XI_LZ : TestEmployeeGrain<EmployeeGrainState>,
                                               INFT_Grain_Person_XI_LZ, INFT_Grain_Job_XI_LZ, INFT_Grain_Employee_XI_LZ
    {
        public NFT_Grain_Employee_XI_LZ(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }

    public abstract class MultiInterface_XI_LZ_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_XI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_XI_LZ_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_XI_LZ, NFT_Props_Person_XI_LZ,
                                                            INFT_Grain_Job_XI_LZ, NFT_Props_Job_XI_LZ,
                                                            INFT_Grain_Employee_XI_LZ, NFT_Props_Employee_XI_LZ>();
        }

        internal static IEnumerable<Func<IndexingTestRunnerBase, int, Task>> GetAllTestTasks(TestIndexPartitionType testIndexTypes)
        {
            yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                        INFT_Grain_Person_XI_LZ, NFT_Props_Person_XI_LZ,
                                                        INFT_Grain_Job_XI_LZ, NFT_Props_Job_XI_LZ,
                                                        INFT_Grain_Employee_XI_LZ, NFT_Props_Employee_XI_LZ>(intAdjust);
        }
    }
}
