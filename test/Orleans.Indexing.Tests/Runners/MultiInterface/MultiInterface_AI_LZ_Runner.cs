using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Orleans.Indexing.Facet;
using System.Collections.Generic;

namespace Orleans.Indexing.Tests.MultiInterface
{
    #region PartitionedPerKey

    // Active Indexes cannot be FT
    // Active Indexes cannot be partitioned PerKey

    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // Active Indexes cannot be FT

    public class NFT_Props_Person_AI_LZ_PS : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Person_AI_LZ_PS>), IsEager = false, IsUnique = false)]  // PerSilo cannot be Unique
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_Person_AI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class NFT_Props_Job_AI_LZ_PS : IJobProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Job_AI_LZ_PS>), IsEager = false, IsUnique = false)] // PerSilo cannot be Unique
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Job_AI_LZ_PS>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public class NFT_Props_Employee_AI_LZ_PS : IEmployeeProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_Employee_AI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "-1")] // PerSilo cannot be Unique
        public int EmployeeId { get; set; }
    }

    public interface INFT_Grain_Person_AI_LZ_PS : IIndexableGrain<NFT_Props_Person_AI_LZ_PS>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_AI_LZ_PS : IIndexableGrain<NFT_Props_Job_AI_LZ_PS>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_AI_LZ_PS : IIndexableGrain<NFT_Props_Employee_AI_LZ_PS>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public class NFT_Grain_Employee_AI_LZ_PS : TestEmployeeGrain<EmployeeGrainState>,
                                               INFT_Grain_Person_AI_LZ_PS, INFT_Grain_Job_AI_LZ_PS, INFT_Grain_Employee_AI_LZ_PS
    {
        public NFT_Grain_Employee_AI_LZ_PS(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerSilo

    #region SingleBucket

    // Active Indexes cannot be FT
    // Active Indexes cannot be partitioned SingleBucket

    #endregion // SingleBucket

    public abstract class MultiInterface_AI_LZ_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_AI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_AI_LZ_PS()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_AI_LZ_PS, NFT_Props_Person_AI_LZ_PS,
                                                            INFT_Grain_Job_AI_LZ_PS, NFT_Props_Job_AI_LZ_PS,
                                                            INFT_Grain_Employee_AI_LZ_PS, NFT_Props_Employee_AI_LZ_PS>();
        }

        internal static IEnumerable<Func<IndexingTestRunnerBase, int, Task>> GetAllTestTasks(TestIndexPartitionType testIndexTypes)
        {
            if (testIndexTypes.HasFlag(TestIndexPartitionType.PerSilo))
            {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_AI_LZ_PS, NFT_Props_Person_AI_LZ_PS,
                                                            INFT_Grain_Job_AI_LZ_PS, NFT_Props_Job_AI_LZ_PS,
                                                            INFT_Grain_Employee_AI_LZ_PS, NFT_Props_Employee_AI_LZ_PS>(intAdjust);
            }
        }
    }
}
