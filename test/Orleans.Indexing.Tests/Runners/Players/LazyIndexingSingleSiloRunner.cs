using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public abstract class LazyIndexingSingleSiloRunner : IndexingTestRunnerBase
    {
        protected LazyIndexingSingleSiloRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucker
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_NFT_TI_LZ_SB()
        {
            IPlayer_NFT_TI_LZ_SB p1 = base.GetGrain<IPlayer_NFT_TI_LZ_SB>(1);
            await p1.SetLocation(ITC.Seattle);

            IPlayer_NFT_TI_LZ_SB p2 = base.GetGrain<IPlayer_NFT_TI_LZ_SB>(2);
            IPlayer_NFT_TI_LZ_SB p3 = base.GetGrain<IPlayer_NFT_TI_LZ_SB>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_NFT_TI_LZ_SB>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_NFT_TI_LZ_SB, PlayerProperties_NFT_TI_LZ_SB>(location, ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            p2 = base.GetGrain<IPlayer_NFT_TI_LZ_SB>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        /// <summary>
        /// Tests basic functionality of ActiveHashIndexPartitionedPerSiloImpl with 1 Silo
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_NFT_AI_LZ_PS()
        {
            IPlayer_NFT_AI_LZ_PS p1 = base.GetGrain<IPlayer_NFT_AI_LZ_PS>(1);
            await p1.SetLocation(ITC.Tehran);

            IPlayer_NFT_AI_LZ_PS p2 = base.GetGrain<IPlayer_NFT_AI_LZ_PS>(2);
            IPlayer_NFT_AI_LZ_PS p3 = base.GetGrain<IPlayer_NFT_AI_LZ_PS>(3);

            await p2.SetLocation(ITC.Tehran);
            await p3.SetLocation(ITC.Yazd);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_NFT_AI_LZ_PS>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_NFT_AI_LZ_PS, PlayerProperties_NFT_AI_LZ_PS>(location, ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Tehran));

            await p2.Deactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(1, await getLocationCount(ITC.Tehran));

            p2 = base.GetGrain<IPlayer_NFT_AI_LZ_PS>(2);
            Assert.Equal(ITC.Tehran, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Tehran));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_NFT_TI_LZ_PK()
        {
            IPlayer_NFT_TI_LZ_PK p1 = base.GetGrain<IPlayer_NFT_TI_LZ_PK>(1);
            await p1.SetLocation(ITC.Seattle);

            IPlayer_NFT_TI_LZ_PK p2 = base.GetGrain<IPlayer_NFT_TI_LZ_PK>(2);
            IPlayer_NFT_TI_LZ_PK p3 = base.GetGrain<IPlayer_NFT_TI_LZ_PK>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_NFT_TI_LZ_PK>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_NFT_TI_LZ_PK, PlayerProperties_NFT_TI_LZ_PK>(location, ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Deactivation does not affect Total indexes

            p2 = base.GetGrain<IPlayer_NFT_TI_LZ_PK>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());

            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }
    }
}
