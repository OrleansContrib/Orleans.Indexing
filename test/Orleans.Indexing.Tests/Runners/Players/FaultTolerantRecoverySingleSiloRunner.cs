using System.Threading.Tasks;
using Orleans.Indexing.Facet;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests.Recovery
{
    using ITC = IndexingTestConstants;

    public interface IRecoveryPlayer_FT_TI_LZ_PK : IPlayerGrain, IIndexableGrain<PlayerProperties_FT_TI_LZ_PK>
    {
        Task DoDeactivate();
    }

    public class RecoveryPlayerGrain : PlayerGrain<PlayerGrainState>, IRecoveryPlayer_FT_TI_LZ_PK
    {
        public RecoveryPlayerGrain(
            [FaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<PlayerGrainState> indexedState)
            : base(indexedState) { }

        public Task DoDeactivate()
        {
            base.DeactivateOnIdle();
            return Task.CompletedTask;
        }
    }

    public abstract class FaultTolerantGrainRecoverySingleSiloRunner : IndexingTestRunnerBase
    {
        protected FaultTolerantGrainRecoverySingleSiloRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests recovery of a deactivated fault-tolerant grain
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("IndexingRecovery")]
        public async Task Test_GrainRecovery_FT_TI_LZ_PK()
        {
            var grainId = 1;
            var p1 = base.GetGrain<IRecoveryPlayer_FT_TI_LZ_PK>(grainId);

            var locIdx = await base.GetAndWaitForIndex<string, IRecoveryPlayer_FT_TI_LZ_PK>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IRecoveryPlayer_FT_TI_LZ_PK, PlayerProperties_FT_TI_LZ_PK>(location);

            // The configuration causes the grain to be deactivated prior to the update; the queue handler
            // then causes the grain to be reactivated by calling its GetActiveWorkflowIdsSet().
            await p1.SetLocation(ITC.Seattle);
            var idsSet = await p1.GetActiveWorkflowIdsSet();
            Assert.NotEmpty(idsSet.Value);

            Assert.Equal(0, await getLocationCount(ITC.Seattle));

            await p1.DoDeactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

            p1 = base.GetGrain<IRecoveryPlayer_FT_TI_LZ_PK>(grainId);
            Assert.Equal(ITC.Seattle, await p1.GetLocation());      // This propget actually causes the activation
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(1, await getLocationCount(ITC.Seattle));
        }
    }

    public abstract class FaultTolerantQueueRecoverySingleSiloRunner : IndexingTestRunnerBase
    {
        protected FaultTolerantQueueRecoverySingleSiloRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests recovery of a deactivated fault-tolerant grain when a reincarnated queue is forced.
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("IndexingRecovery")]
        public async Task Test_QueueRecovery_FT_TI_LZ_PK()
        {
            var grainId = 100;
            var p1 = base.GetGrain<IRecoveryPlayer_FT_TI_LZ_PK>(grainId);

            var locIdx = await base.GetAndWaitForIndex<string, IRecoveryPlayer_FT_TI_LZ_PK>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IRecoveryPlayer_FT_TI_LZ_PK, PlayerProperties_FT_TI_LZ_PK>(location);

            // The configuration causes the queue handler to not be invoked for the update; it does not stop the Silo.
            await p1.SetLocation(ITC.Seattle);
            var idsSet = await p1.GetActiveWorkflowIdsSet();
            Assert.NotEmpty(idsSet.Value);

            Assert.Equal(0, await getLocationCount(ITC.Seattle));

            // This cannot actually shut down the silo because that will lose the MemoryStorage state, so the
            // test fixture forces creation of the reincarnated queue when the grain is reactivated with in-flight workflows.
            await p1.DoDeactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

            p1 = base.GetGrain<IRecoveryPlayer_FT_TI_LZ_PK>(grainId);
            Assert.Equal(ITC.Seattle, await p1.GetLocation());      // This propget actually causes the activation
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(1, await getLocationCount(ITC.Seattle));
        }
    }
}
