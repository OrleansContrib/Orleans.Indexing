using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public abstract class SimpleIndexingSingleSiloRunner : IndexingTestRunnerBase
    {
        protected SimpleIndexingSingleSiloRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucket
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_NFT_TI_EG_SB()
        {
            var p1 = base.GetGrain<IPlayer_NFT_TI_EG_SB>(1);
            await p1.SetLocation(ITC.Seattle);

            var p2 = base.GetGrain<IPlayer_NFT_TI_EG_SB>(2);
            var p3 = base.GetGrain<IPlayer_NFT_TI_EG_SB>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_NFT_TI_EG_SB>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_NFT_TI_EG_SB, PlayerProperties_NFT_TI_EG_SB>(location);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            Thread.Sleep(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Deactivation does not affect Total indexes

            p2 = base.GetGrain<IPlayer_NFT_TI_EG_SB>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        /// <summary>
        /// Tests basic functionality of Transactional HashIndexSingleBucket
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Lookup_3Grains_TXN_TI_EG_SB()
        {
            var p1 = base.GetGrain<IPlayer_TXN_TI_EG_SB>(1);
            await p1.SetLocation(ITC.Seattle);

            var p2 = base.GetGrain<IPlayer_TXN_TI_EG_SB>(2);
            var p3 = base.GetGrain<IPlayer_TXN_TI_EG_SB>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_TXN_TI_EG_SB>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCountTxn<IPlayer_TXN_TI_EG_SB, PlayerProperties_TXN_TI_EG_SB>(location);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            Thread.Sleep(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Transactional indexes are always Total, so the count remains 2

            p2 = base.GetGrain<IPlayer_TXN_TI_EG_SB>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        /// <summary>
        /// Tests basic functionality of ActiveHashIndexPartitionedPerSiloImpl with 1 Silo
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_NFT_AI_EG_PS()
        {
            IPlayer_NFT_AI_EG_PS p1 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(1);
            await p1.SetLocation(ITC.Tehran);

            IPlayer_NFT_AI_EG_PS p2 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(2);
            IPlayer_NFT_AI_EG_PS p3 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(3);

            await p2.SetLocation(ITC.Tehran);
            await p3.SetLocation(ITC.Yazd);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_NFT_AI_EG_PS>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_NFT_AI_EG_PS, PlayerProperties_NFT_AI_EG_PS>(location);

            Assert.Equal(2, await getLocationCount(ITC.Tehran));

            await p2.Deactivate();
            Thread.Sleep(1000);
            Assert.Equal(1, await getLocationCount(ITC.Tehran));

            p2 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(2);
            Assert.Equal(ITC.Tehran, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Tehran));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_NFT_TI_EG_PK()
        {
            var p1 = base.GetGrain<IPlayer_NFT_TI_EG_PK>(1);
            await p1.SetLocation(ITC.Seattle);

            var p2 = base.GetGrain<IPlayer_NFT_TI_EG_PK>(2);
            var p3 = base.GetGrain<IPlayer_NFT_TI_EG_PK>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_NFT_TI_EG_PK>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_NFT_TI_EG_PK, PlayerProperties_NFT_TI_EG_PK>(location);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            Thread.Sleep(1000);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Deactivation does not affect Total indexes

            p2 = base.GetGrain<IPlayer_NFT_TI_EG_PK>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Lookup_3Grains_TXN_TI_EG_PK()
        {
            var p1 = base.GetGrain<IPlayer_TXN_TI_EG_PK>(1);
            await p1.SetLocation(ITC.Seattle);

            var p2 = base.GetGrain<IPlayer_TXN_TI_EG_PK>(2);
            var p3 = base.GetGrain<IPlayer_TXN_TI_EG_PK>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_TXN_TI_EG_PK>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCountTxn<IPlayer_TXN_TI_EG_PK, PlayerProperties_TXN_TI_EG_PK>(location);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            Thread.Sleep(1000);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Transactional indexes are always Total, so the count remains 2

            p2 = base.GetGrain<IPlayer_TXN_TI_EG_PK>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_3Grains_NFT_AI_EG_PS()
        {
            await update_NFT_AI_yy_PS<IPlayer_NFT_AI_EG_PS, PlayerProperties_NFT_AI_EG_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_3Grains_NFT_AI_LZ_PS()
        {
            await update_NFT_AI_yy_PS<IPlayer_NFT_AI_LZ_PS, PlayerProperties_NFT_AI_LZ_PS>();
        }

        private async Task update_NFT_AI_yy_PS<TIGrain, TProperties>() where TIGrain : IGrainWithIntegerKey, IPlayerGrain, IIndexableGrain
                                                                       where TProperties : IPlayerProperties
        {
            var p1 = base.GetGrain<TIGrain>(1);
            await p1.SetLocation(ITC.Seattle);

            var p2 = base.GetGrain<TIGrain>(2);
            await p2.SetLocation(ITC.Redmond);

            var p3 = base.GetGrain<TIGrain>(3);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, TIGrain>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<TIGrain, TProperties>(location);

            Assert.Equal(1, await getLocationCount(ITC.Seattle));
            Assert.Equal(1, await getLocationCount(ITC.Redmond));

            await p2.Deactivate();
            Thread.Sleep(1000);

            Assert.Equal(1, await getLocationCount(ITC.Seattle));
            Assert.Equal(0, await getLocationCount(ITC.Redmond));

            p2 = base.GetGrain<TIGrain>(2);
            Assert.Equal(ITC.Redmond, await p2.GetLocation());

            Assert.Equal(1, await getLocationCount(ITC.Seattle));
            Assert.Equal(1, await getLocationCount(ITC.Redmond));

            // Test updates
            await p1.SetLocation(ITC.NewYork);
            await p2.SetLocation(ITC.LosAngeles);

            Assert.Equal(0, await getLocationCount(ITC.Seattle));
            Assert.Equal(0, await getLocationCount(ITC.Redmond));

            Assert.Equal(1, await getLocationCount(ITC.NewYork));
            Assert.Equal(1, await getLocationCount(ITC.LosAngeles));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey with two indexes
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_3Grains_NFT_AI_EG_PSPS()
        {
            await update_NFT_xx_yy_PSPS<IPlayer_NFT_AI_EG_PSPS, PlayerProperties_NFT_AI_EG_PSPS>(isActive: true);
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey with two indexes
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_3Grains_NFT_AI_LZ_PSPS()
        {
            await update_NFT_xx_yy_PSPS<IPlayer_NFT_AI_LZ_PSPS, PlayerProperties_NFT_AI_LZ_PSPS>(isActive: true);
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey with two indexes
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_3Grains_NFT_TI_EG_PSPS()
        {
            await update_NFT_xx_yy_PSPS<IPlayer_NFT_TI_EG_PKSB, PlayerProperties_NFT_AI_EG_PSPS>(isActive: false);
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey with two indexes
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_3Grains_NFT_TI_LZ_PSPS()
        {
            await update_NFT_xx_yy_PSPS<IPlayer_NFT_TI_LZ_PKSB, PlayerProperties_NFT_AI_LZ_PSPS>(isActive: false);
        }

        private async Task update_NFT_xx_yy_PSPS<TIGrain, TProperties>(bool isActive) where TIGrain : IGrainWithIntegerKey, IPlayerGrain, IIndexableGrain
                                                                         where TProperties : IPlayerProperties
        { 
            var p1 = base.GetGrain<TIGrain>(1);
            await p1.SetLocation(ITC.Seattle);
            await p1.SetScore(42);

            var p2 = base.GetGrain<TIGrain>(2);
            var p3 = base.GetGrain<TIGrain>(3);

            await p2.SetLocation(ITC.Seattle);
            await p2.SetScore(34);
            await p3.SetLocation(ITC.SanFrancisco);
            await p3.SetScore(34);

            var locIdx = await base.GetAndWaitForIndex<string, TIGrain>(ITC.LocationProperty);
            var scoreIdx = await base.GetAndWaitForIndex<int, TIGrain>(ITC.ScoreProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<TIGrain, TProperties>(location);
            Task<int> getScoreCount(int score) => this.GetPlayerScoreCount<TIGrain, TProperties>(score);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));
            Assert.Equal(2, await getScoreCount(34));

            await p2.Deactivate();
            Thread.Sleep(1000);
            Assert.Equal(isActive ? 1 : 2, await getLocationCount(ITC.Seattle));
            Assert.Equal(isActive ? 1 : 2, await getScoreCount(34));

            p2 = base.GetGrain<TIGrain>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
            Assert.Equal(2, await getScoreCount(34));

            // Test updates
            await p2.SetLocation(ITC.SanFrancisco);
            await p2.SetScore(42);

            Assert.Equal(1, await getLocationCount(ITC.Seattle));
            Assert.Equal(1, await getScoreCount(34));

            Assert.Equal(2, await getLocationCount(ITC.SanFrancisco));
            Assert.Equal(2, await getScoreCount(42));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey with two indexes
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_3Grains_TXN_TI_EG_PKSB()
        {
            var p1 = base.GetGrain<IPlayer_TXN_TI_EG_PKSB>(1);
            await p1.SetLocation(ITC.Seattle);
            await p1.SetScore(42);

            var p2 = base.GetGrain<IPlayer_TXN_TI_EG_PKSB>(2);
            var p3 = base.GetGrain<IPlayer_TXN_TI_EG_PKSB>(3);

            await p2.SetLocation(ITC.Seattle);
            await p2.SetScore(34);
            await p3.SetLocation(ITC.SanFrancisco);
            await p3.SetScore(34);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_TXN_TI_EG_PKSB>(ITC.LocationProperty);
            var scoreIdx = await base.GetAndWaitForIndex<int, IPlayer_TXN_TI_EG_PKSB>(ITC.ScoreProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCountTxn<IPlayer_TXN_TI_EG_PKSB, PlayerProperties_TXN_TI_EG_PKSB>(location);
            Task<int> getScoreCount(int score) => this.GetPlayerScoreCountTxn<IPlayer_TXN_TI_EG_PKSB, PlayerProperties_TXN_TI_EG_PKSB>(score);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));
            Assert.Equal(2, await getScoreCount(34));

            await p2.Deactivate();
            Thread.Sleep(1000);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Transactional indexes are always Total, so the count remains 2
            Assert.Equal(2, await getScoreCount(34));               // Transactional indexes are always Total, so the count remains 2 

            p2 = base.GetGrain<IPlayer_TXN_TI_EG_PKSB>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
            Assert.Equal(2, await getScoreCount(34));

            // Test updates
            await p2.SetLocation(ITC.SanFrancisco);
            await p2.SetScore(42);

            Assert.Equal(1, await getLocationCount(ITC.Seattle));
            Assert.Equal(1, await getScoreCount(34));

            Assert.Equal(2, await getLocationCount(ITC.SanFrancisco));
            Assert.Equal(2, await getScoreCount(42));
        }
    }
}
