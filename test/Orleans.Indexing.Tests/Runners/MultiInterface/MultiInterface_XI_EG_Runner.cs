using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Orleans.Indexing.Facet;
using System.Collections.Generic;

namespace Orleans.Indexing.Tests.MultiInterface
{
    #region PartitionedPerKey

    // NFT only; FT cannot be configured to be Eager.
    // These are the only supported combinations; Active Indexes cannot be FT or TXN, or partitioned PerKey or SingleBucket;
    // Total Indexes cannot be partitioned PerSilo; and FT and NFT cannot be mixed on a single grain.

    public class NFT_Props_Person_XI_EG : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Person_XI_EG>), IsEager = true, IsUnique = false)]
        public string Name { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Person_XI_EG>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class NFT_Props_Job_XI_EG_PK : IJobProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_XI_EG>), IsEager = true, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Job_XI_EG>), IsEager = true, IsUnique = false)]
        public string Department { get; set; }
    }

    public class NFT_Props_Employee_XI_EG_PK : IEmployeeProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Employee_XI_EG>), IsEager = true, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public interface INFT_Grain_Person_XI_EG : IIndexableGrain<NFT_Props_Person_XI_EG>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_XI_EG : IIndexableGrain<NFT_Props_Job_XI_EG_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_XI_EG : IIndexableGrain<NFT_Props_Employee_XI_EG_PK>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public class NFT_Grain_Employee_XI_EG : TestEmployeeGrain<EmployeeGrainState>,
                                               INFT_Grain_Person_XI_EG, INFT_Grain_Job_XI_EG, INFT_Grain_Employee_XI_EG
    {
        public NFT_Grain_Employee_XI_EG(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerKey

    public abstract class MultiInterface_XI_EG_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_XI_EG_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_XI_EG_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_XI_EG, NFT_Props_Person_XI_EG,
                                                            INFT_Grain_Job_XI_EG, NFT_Props_Job_XI_EG_PK,
                                                            INFT_Grain_Employee_XI_EG, NFT_Props_Employee_XI_EG_PK>();
        }

        internal static IEnumerable<Func<IndexingTestRunnerBase, int, Task>> GetAllTestTasks(TestIndexPartitionType testIndexTypes)
        {
            yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_XI_EG, NFT_Props_Person_XI_EG,
                                                            INFT_Grain_Job_XI_EG, NFT_Props_Job_XI_EG_PK,
                                                            INFT_Grain_Employee_XI_EG, NFT_Props_Employee_XI_EG_PK>(intAdjust);
        }
    }
}
