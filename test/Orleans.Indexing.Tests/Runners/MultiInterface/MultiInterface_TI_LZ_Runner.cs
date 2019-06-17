using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Orleans.Indexing.Facet;
using System.Collections.Generic;

namespace Orleans.Indexing.Tests.MultiInterface
{
    #region PartitionedPerKey

    public class FT_Props_Person_TI_LZ_PK : IPersonProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, IFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class NFT_Props_Person_TI_LZ_PK : IPersonProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class FT_Props_Job_TI_LZ_PK : IJobProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public class NFT_Props_Job_TI_LZ_PK : IJobProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public class FT_Props_Employee_TI_LZ_PK : IEmployeeProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<int, IFT_Grain_Employee_TI_LZ_PK>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public class NFT_Props_Employee_TI_LZ_PK : IEmployeeProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Employee_TI_LZ_PK>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public interface IFT_Grain_Person_TI_LZ_PK : IIndexableGrain<FT_Props_Person_TI_LZ_PK>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Person_TI_LZ_PK : IIndexableGrain<NFT_Props_Person_TI_LZ_PK>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Job_TI_LZ_PK : IIndexableGrain<FT_Props_Job_TI_LZ_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_TI_LZ_PK : IIndexableGrain<NFT_Props_Job_TI_LZ_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Employee_TI_LZ_PK : IIndexableGrain<FT_Props_Employee_TI_LZ_PK>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_TI_LZ_PK : IIndexableGrain<NFT_Props_Employee_TI_LZ_PK>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public class FT_Grain_Employee_TI_LZ_PK : TestEmployeeGrain<EmployeeGrainState>,
                                              IFT_Grain_Person_TI_LZ_PK, IFT_Grain_Job_TI_LZ_PK, IFT_Grain_Employee_TI_LZ_PK
    {
        public FT_Grain_Employee_TI_LZ_PK(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }

    public class NFT_Grain_Employee_TI_LZ_PK : TestEmployeeGrain<EmployeeGrainState>,
                                               INFT_Grain_Person_TI_LZ_PK, INFT_Grain_Job_TI_LZ_PK, INFT_Grain_Employee_TI_LZ_PK
    {
        public NFT_Grain_Employee_TI_LZ_PK(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // None; Total indexes cannot be specified as partitioned per silo.

    #endregion // PartitionedPerSilo

    #region SingleBucket

    public class FT_Props_Person_TI_LZ_SB : IPersonProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, IFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class NFT_Props_Person_TI_LZ_SB : IPersonProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class FT_Props_Job_TI_LZ_SB : IJobProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public class NFT_Props_Job_TI_LZ_SB : IJobProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public class FT_Props_Employee_TI_LZ_SB : IEmployeeProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<int, IFT_Grain_Employee_TI_LZ_SB>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public class NFT_Props_Employee_TI_LZ_SB : IEmployeeProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_Employee_TI_LZ_SB>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public interface IFT_Grain_Person_TI_LZ_SB : IIndexableGrain<FT_Props_Person_TI_LZ_SB>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Person_TI_LZ_SB : IIndexableGrain<NFT_Props_Person_TI_LZ_SB>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Job_TI_LZ_SB : IIndexableGrain<FT_Props_Job_TI_LZ_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_TI_LZ_SB : IIndexableGrain<NFT_Props_Job_TI_LZ_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Employee_TI_LZ_SB : IIndexableGrain<FT_Props_Employee_TI_LZ_SB>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_TI_LZ_SB : IIndexableGrain<NFT_Props_Employee_TI_LZ_SB>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public class FT_Grain_Employee_TI_LZ_SB : TestEmployeeGrain<EmployeeGrainState>,
                                              IFT_Grain_Person_TI_LZ_SB, IFT_Grain_Job_TI_LZ_SB, IFT_Grain_Employee_TI_LZ_SB
    {
        public FT_Grain_Employee_TI_LZ_SB(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }

    public class NFT_Grain_Employee_TI_LZ_SB : TestEmployeeGrain<EmployeeGrainState>,
                                               INFT_Grain_Person_TI_LZ_SB, INFT_Grain_Job_TI_LZ_SB, INFT_Grain_Employee_TI_LZ_SB
    {
        public NFT_Grain_Employee_TI_LZ_SB(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }
    #endregion // SingleBucket

    public abstract class MultiInterface_TI_LZ_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_TI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_Employee_TI_LZ_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_TI_LZ_PK, FT_Props_Person_TI_LZ_PK,
                                                            IFT_Grain_Job_TI_LZ_PK, FT_Props_Job_TI_LZ_PK,
                                                            IFT_Grain_Employee_TI_LZ_PK, FT_Props_Employee_TI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_TI_LZ_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_LZ_PK, NFT_Props_Person_TI_LZ_PK,
                                                            INFT_Grain_Job_TI_LZ_PK, NFT_Props_Job_TI_LZ_PK,
                                                            INFT_Grain_Employee_TI_LZ_PK, NFT_Props_Employee_TI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_Employee_TI_LZ_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_TI_LZ_SB, FT_Props_Person_TI_LZ_SB,
                                                            IFT_Grain_Job_TI_LZ_SB, FT_Props_Job_TI_LZ_SB,
                                                            IFT_Grain_Employee_TI_LZ_SB, FT_Props_Employee_TI_LZ_SB>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_TI_LZ_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_LZ_SB, NFT_Props_Person_TI_LZ_SB,
                                                            INFT_Grain_Job_TI_LZ_SB, NFT_Props_Job_TI_LZ_SB,
                                                            INFT_Grain_Employee_TI_LZ_SB, NFT_Props_Employee_TI_LZ_SB>();
        }

        internal static IEnumerable<Func<IndexingTestRunnerBase, int, Task>> GetAllTestTasks(TestIndexPartitionType testIndexTypes)
        {
            if (testIndexTypes.HasFlag(TestIndexPartitionType.PerKeyHash))
            {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            IFT_Grain_Person_TI_LZ_PK, FT_Props_Person_TI_LZ_PK,
                                                            IFT_Grain_Job_TI_LZ_PK, FT_Props_Job_TI_LZ_PK,
                                                            IFT_Grain_Employee_TI_LZ_PK, FT_Props_Employee_TI_LZ_PK>(intAdjust);
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_TI_LZ_PK, NFT_Props_Person_TI_LZ_PK,
                                                            INFT_Grain_Job_TI_LZ_PK, NFT_Props_Job_TI_LZ_PK,
                                                            INFT_Grain_Employee_TI_LZ_PK, NFT_Props_Employee_TI_LZ_PK>(intAdjust);
            }
            if (testIndexTypes.HasFlag(TestIndexPartitionType.SingleBucket)) {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            IFT_Grain_Person_TI_LZ_SB, FT_Props_Person_TI_LZ_SB,
                                                            IFT_Grain_Job_TI_LZ_SB, FT_Props_Job_TI_LZ_SB,
                                                            IFT_Grain_Employee_TI_LZ_SB, FT_Props_Employee_TI_LZ_SB>(intAdjust);
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_TI_LZ_SB, NFT_Props_Person_TI_LZ_SB,
                                                            INFT_Grain_Job_TI_LZ_SB, NFT_Props_Job_TI_LZ_SB,
                                                            INFT_Grain_Employee_TI_LZ_SB, NFT_Props_Employee_TI_LZ_SB>(intAdjust);
            }
        }
    }
}
