using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Orleans.Indexing.Facet;
using System.Collections.Generic;
using Orleans.Transactions.Abstractions;

namespace Orleans.Indexing.Tests.MultiInterface
{
    #region PartitionedPerKey

    // NFT and TXN only; FT cannot be configured to be Eager.

    public class NFT_Props_Person_TI_EG_PK : IPersonProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Person_TI_EG_PK>), IsEager = true, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Person_TI_EG_PK>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class NFT_Props_Job_TI_EG_PK : IJobProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Job_TI_EG_PK>), IsEager = true, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Job_TI_EG_PK>), IsEager = true, IsUnique = false)]
        public string Department { get; set; }
    }

    public class NFT_Props_Employee_TI_EG_PK : IEmployeeProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Employee_TI_EG_PK>), IsEager = true, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public class TXN_Props_Person_TI_EG_PK : NFT_Props_Person_TI_EG_PK
    {
    }

    public class TXN_Props_Job_TI_EG_PK : NFT_Props_Job_TI_EG_PK
    {
    }

    public class TXN_Props_Employee_TI_EG_PK : NFT_Props_Employee_TI_EG_PK
    {
    }

    public interface INFT_Grain_Person_TI_EG_PK : IIndexableGrain<NFT_Props_Person_TI_EG_PK>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_TI_EG_PK : IIndexableGrain<NFT_Props_Job_TI_EG_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_TI_EG_PK : IIndexableGrain<NFT_Props_Employee_TI_EG_PK>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public interface ITXN_Grain_Person_TI_EG_PK : IIndexableGrain<NFT_Props_Person_TI_EG_PK>, IPersonGrain, IGrainWithIntegerKey, ITestTransactionalPersistence
    {
    }

    public interface ITXN_Grain_Job_TI_EG_PK : IIndexableGrain<NFT_Props_Job_TI_EG_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface ITXN_Grain_Employee_TI_EG_PK : IIndexableGrain<NFT_Props_Employee_TI_EG_PK>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public class NFT_Grain_Employee_TI_EG_PK : TestEmployeeGrain<EmployeeGrainState>,
                                               INFT_Grain_Person_TI_EG_PK, INFT_Grain_Job_TI_EG_PK, INFT_Grain_Employee_TI_EG_PK
    {
        public NFT_Grain_Employee_TI_EG_PK(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }

    public class TXN_Grain_Employee_TI_EG_PK : TestEmployeeGrain<EmployeeGrainState>,
                                               ITXN_Grain_Person_TI_EG_PK, ITXN_Grain_Job_TI_EG_PK, ITXN_Grain_Employee_TI_EG_PK
    {
        public TXN_Grain_Employee_TI_EG_PK(
            [TransactionalIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // None; Total indexes cannot be specified as partitioned per silo.

    #endregion // PartitionedPerSilo

    #region SingleBucket

    // NFT and TXN only; FT cannot be configured to be Eager.

    public class NFT_Props_Person_TI_EG_SB : IPersonProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Person_TI_EG_SB>), IsEager = true, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_Person_TI_EG_SB>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    public class NFT_Props_Job_TI_EG_SB : IJobProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_TI_EG_SB>), IsEager = true, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_TI_EG_SB>), IsEager = true, IsUnique = false)]
        public string Department { get; set; }
    }

    public class NFT_Props_Employee_TI_EG_SB : IEmployeeProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_Employee_TI_EG_SB>), IsEager = true, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public class TXN_Props_Person_TI_EG_SB : NFT_Props_Person_TI_EG_SB
    {
    }

    public class TXN_Props_Job_TI_EG_SB : NFT_Props_Job_TI_EG_SB
    {
    }

    public class TXN_Props_Employee_TI_EG_SB : NFT_Props_Employee_TI_EG_SB
    {
    }

    public interface INFT_Grain_Person_TI_EG_SB : IIndexableGrain<NFT_Props_Person_TI_EG_SB>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_TI_EG_SB : IIndexableGrain<NFT_Props_Job_TI_EG_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_TI_EG_SB : IIndexableGrain<NFT_Props_Employee_TI_EG_SB>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public interface ITXN_Grain_Person_TI_EG_SB : IIndexableGrain<NFT_Props_Person_TI_EG_SB>, IPersonGrain, IGrainWithIntegerKey, ITestTransactionalPersistence
    {
    }

    public interface ITXN_Grain_Job_TI_EG_SB : IIndexableGrain<NFT_Props_Job_TI_EG_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface ITXN_Grain_Employee_TI_EG_SB : IIndexableGrain<NFT_Props_Employee_TI_EG_SB>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public class NFT_Grain_Employee_TI_EG_SB : TestEmployeeGrain<EmployeeGrainState>,
                                               INFT_Grain_Person_TI_EG_SB, INFT_Grain_Job_TI_EG_SB, INFT_Grain_Employee_TI_EG_SB
    {
        public NFT_Grain_Employee_TI_EG_SB(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }

    public class TXN_Grain_Employee_TI_EG_SB : TestEmployeeGrain<EmployeeGrainState>,
                                               ITXN_Grain_Person_TI_EG_SB, ITXN_Grain_Job_TI_EG_SB, ITXN_Grain_Employee_TI_EG_SB
    {
        public TXN_Grain_Employee_TI_EG_SB(
            [TransactionalIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<EmployeeGrainState> indexedState)
            : base(indexedState) { }
    }
    #endregion // SingleBucket

    public abstract class MultiInterface_TI_EG_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_TI_EG_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_TI_EG_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_EG_PK, NFT_Props_Person_TI_EG_PK,
                                                            INFT_Grain_Job_TI_EG_PK, NFT_Props_Job_TI_EG_PK,
                                                            INFT_Grain_Employee_TI_EG_PK, NFT_Props_Employee_TI_EG_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_TXN_Grain_Employee_TI_EG_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<ITXN_Grain_Person_TI_EG_PK, TXN_Props_Person_TI_EG_PK,
                                                            ITXN_Grain_Job_TI_EG_PK, TXN_Props_Job_TI_EG_PK,
                                                            ITXN_Grain_Employee_TI_EG_PK, TXN_Props_Employee_TI_EG_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_TI_EG_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_EG_SB, NFT_Props_Person_TI_EG_SB,
                                                            INFT_Grain_Job_TI_EG_SB, NFT_Props_Job_TI_EG_SB,
                                                            INFT_Grain_Employee_TI_EG_SB, NFT_Props_Employee_TI_EG_SB>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_TXN_Grain_Employee_TI_EG_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<ITXN_Grain_Person_TI_EG_SB, TXN_Props_Person_TI_EG_SB,
                                                            ITXN_Grain_Job_TI_EG_SB, TXN_Props_Job_TI_EG_SB,
                                                            ITXN_Grain_Employee_TI_EG_SB, TXN_Props_Employee_TI_EG_SB>();
        }

        internal static IEnumerable<Func<IndexingTestRunnerBase, int, Task>> GetAllTestTasks(TestIndexPartitionType testIndexTypes)
        {
            if (testIndexTypes.HasFlag(TestIndexPartitionType.PerKeyHash))
            {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_TI_EG_PK, NFT_Props_Person_TI_EG_PK,
                                                            INFT_Grain_Job_TI_EG_PK, NFT_Props_Job_TI_EG_PK,
                                                            INFT_Grain_Employee_TI_EG_PK, NFT_Props_Employee_TI_EG_PK>(intAdjust);
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            ITXN_Grain_Person_TI_EG_PK, TXN_Props_Person_TI_EG_PK,
                                                            ITXN_Grain_Job_TI_EG_PK, TXN_Props_Job_TI_EG_PK,
                                                            ITXN_Grain_Employee_TI_EG_PK, TXN_Props_Employee_TI_EG_PK>(intAdjust);
            }
            if (testIndexTypes.HasFlag(TestIndexPartitionType.SingleBucket))
            {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_TI_EG_SB, NFT_Props_Person_TI_EG_SB,
                                                            INFT_Grain_Job_TI_EG_SB, NFT_Props_Job_TI_EG_SB,
                                                            INFT_Grain_Employee_TI_EG_SB, NFT_Props_Employee_TI_EG_SB>(intAdjust);
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            ITXN_Grain_Person_TI_EG_SB, TXN_Props_Person_TI_EG_SB,
                                                            ITXN_Grain_Job_TI_EG_SB, TXN_Props_Job_TI_EG_SB,
                                                            ITXN_Grain_Employee_TI_EG_SB, TXN_Props_Employee_TI_EG_SB>(intAdjust);
            }
        }
    }
}
