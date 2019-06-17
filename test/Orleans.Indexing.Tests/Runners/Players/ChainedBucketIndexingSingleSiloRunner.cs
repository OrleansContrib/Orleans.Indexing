using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public abstract class ChainedBucketIndexingSingleSiloRunner : IndexingTestRunnerBase
    {
        protected ChainedBucketIndexingSingleSiloRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucket with chained buckets
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_10Grains_FT_TI_EG_SB()
        {
            var p1 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(1);
            await p1.SetLocation(ITC.Seattle);

            // MaxEntriesPerBucket == 5
            var p2 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(2);
            var p3 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(3);
            var p4 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(4);
            var p5 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(5);
            var p6 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(6);
            var p7 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(7);
            var p8 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(8);
            var p9 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(9);
            var p10 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(10);

            await p2.SetLocation(ITC.SanJose);
            await p3.SetLocation(ITC.SanFrancisco);
            await p4.SetLocation(ITC.Bellevue);
            await p5.SetLocation(ITC.Redmond);
            await p6.SetLocation(ITC.Kirkland);
            await p7.SetLocation(ITC.Kirkland);
            await p8.SetLocation(ITC.Kirkland);
            await p9.SetLocation(ITC.Seattle);
            await p10.SetLocation(ITC.Kirkland);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayerChain_FT_TI_EG_SB>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayerChain_FT_TI_EG_SB, PlayerChainProperties_FT_TI_EG_SB>(location);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));
            Assert.Equal(4, await getLocationCount(ITC.Kirkland));

            await p8.Deactivate();
            await p9.Deactivate();
            Thread.Sleep(ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Deactivation does not affect Total indexes
            Assert.Equal(4, await getLocationCount(ITC.Kirkland));  // Deactivation does not affect Total indexes

            p10 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(10);
            Assert.Equal(ITC.Kirkland, await p10.GetLocation());

            p8 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(8);
            p9 = base.GetGrain<IPlayerChain_FT_TI_EG_SB>(9);
            Assert.Equal(ITC.Kirkland, await p8.GetLocation());     // Must call a method first before it is activated (and inserted into active indexes)
            Assert.Equal(ITC.Seattle, await p9.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
            Assert.Equal(4, await getLocationCount(ITC.Kirkland));

            // Test updates
            await p2.SetLocation(ITC.Yazd);

            Assert.Equal(0, await getLocationCount(ITC.SanJose));
            Assert.Equal(1, await getLocationCount(ITC.Yazd));
        }

        /// <summary>
        /// Tests basic functionality of ActiveHashIndexPartitionedPerSiloImpl with 1 Silo
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_NFT_AI_EG_PS()
        {
            var p1 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(1);
            await p1.SetLocation(ITC.Tehran);

            var p2 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(2);
            var p3 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(3);

            await p2.SetLocation(ITC.Tehran);
            await p3.SetLocation(ITC.Yazd);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_NFT_AI_EG_PS>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_NFT_AI_EG_PS, PlayerProperties_NFT_AI_EG_PS>(location);

            Assert.Equal(2, await getLocationCount(ITC.Tehran));

            await p2.Deactivate();
            Thread.Sleep(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(1, await getLocationCount(ITC.Tehran));

            p2 = base.GetGrain<IPlayer_NFT_AI_EG_PS>(2);
            Thread.Sleep(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(ITC.Tehran, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Tehran));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucket with chained buckets
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_20Grains_FT_TI_EG_PK()
        {
            await update_20Grains_FT_TI_EG_zz<IPlayerChain_FT_TI_EG_PK, PlayerChainProperties_FT_TI_EG_PK>();
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucket with chained buckets
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Update_20Grains_FT_TI_EG_SB()
        {
            await update_20Grains_FT_TI_EG_zz<IPlayerChain_FT_TI_EG_SB, PlayerChainProperties_FT_TI_EG_SB>();
        }

        private async Task update_20Grains_FT_TI_EG_zz<TIGrain, TProperties>() where TIGrain : IGrainWithIntegerKey, IPlayerGrain, IIndexableGrain
                                                                               where TProperties : IPlayerProperties
        {
            // Different cities and IDs to avoid conflict with other TIGrain usage
            // MaxEntriesPerBucket == 5
            var grains = (await Task.WhenAll(Enumerable.Range(0, 20).Select(async ii =>
            {
                var grain = base.GetGrain<TIGrain>(ii * 100);
                await grain.SetLocation(ITC.NewYork);
                return grain;
            }))).ToArray();

            var locIdx = await base.GetAndWaitForIndex<string, TIGrain>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<TIGrain, TProperties>(location);

            Assert.Equal(20, await getLocationCount(ITC.NewYork));

            for (var ii = 19; ii >= 9; ii -= 2)
            {
                await grains[ii].SetLocation(ITC.LosAngeles);
            }

            Assert.Equal(14, await getLocationCount(ITC.NewYork));
            Assert.Equal(6, await getLocationCount(ITC.LosAngeles));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucket with chained buckets
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Update_20Grains_TXN_TI_EG_PK()
        {
            await update_20Grains_TXN_TI_EG_zz<IPlayerChain_TXN_TI_EG_PK, PlayerChainProperties_TXN_TI_EG_SB>();
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucket with chained buckets
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Update_20Grains_TXN_TI_EG_SB()
        {
            await update_20Grains_TXN_TI_EG_zz<IPlayerChain_TXN_TI_EG_SB, PlayerChainProperties_TXN_TI_EG_SB>();
        }

        private async Task update_20Grains_TXN_TI_EG_zz<TIGrain, TProperties>() where TIGrain : IGrainWithIntegerKey, IPlayerGrainTransactional, IIndexableGrain
                                                                                where TProperties : IPlayerProperties
        {
            // MaxEntriesPerBucket == 5
            var grains = (await Task.WhenAll(Enumerable.Range(0, 20).Select(async ii =>
            {
                var grain = base.GetGrain<TIGrain>(ii);
                await grain.SetLocation(ITC.Seattle);
                return grain;
            }))).ToArray();

            var locIdx = await base.GetAndWaitForIndex<string, TIGrain>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCountTxn<TIGrain, TProperties>(location);

            Assert.Equal(20, await getLocationCount(ITC.Seattle));

            for (var ii = 19; ii >= 9; ii -= 2)
            {
                await grains[ii].SetLocation(ITC.Redmond);
            }

            Assert.Equal(14, await getLocationCount(ITC.Seattle));
            Assert.Equal(6, await getLocationCount(ITC.Redmond));
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
            Thread.Sleep(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Deactivate does not affect Total indexes

            p2 = base.GetGrain<IPlayer_NFT_TI_EG_PK>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            // Test updates
            await p2.SetLocation(ITC.SanFrancisco);
            Assert.Equal(1, await getLocationCount(ITC.Seattle));
            Assert.Equal(2, await getLocationCount(ITC.SanFrancisco));
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
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Transactional indexes are Total, so the count remains 2

            p2 = base.GetGrain<IPlayer_TXN_TI_EG_PK>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            // Test updates
            await p2.SetLocation(ITC.SanFrancisco);
            Assert.Equal(1, await getLocationCount(ITC.Seattle));
            Assert.Equal(2, await getLocationCount(ITC.SanFrancisco));
        }
    }
}
