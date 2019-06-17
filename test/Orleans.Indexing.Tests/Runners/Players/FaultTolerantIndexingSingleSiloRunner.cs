using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public abstract class FaultTolerantIndexingSingleSiloRunner : IndexingTestRunnerBase
    {
        protected FaultTolerantIndexingSingleSiloRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests basic functionality of ActiveHashIndexPartitionedPerSiloImpl with 2 Silos
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_FT_TI_LZ_PK()
        {
            await base.StartAndWaitForSecondSilo();

            IPlayer_FT_TI_LZ_PK p1 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(1);
            await p1.SetLocation(ITC.Seattle);

            IPlayer_FT_TI_LZ_PK p2 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(2);
            IPlayer_FT_TI_LZ_PK p3 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_FT_TI_LZ_PK>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_FT_TI_LZ_PK, PlayerProperties_FT_TI_LZ_PK>(location, ITC.DelayUntilIndexesAreUpdatedLazily);

            base.Output.WriteLine("Before check 1");
            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

            base.Output.WriteLine("Before check 2");
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Deactivate does not affect Total indexes

            p2 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(2);
            base.Output.WriteLine("Before check 3");
            Assert.Equal(ITC.Seattle, await p2.GetLocation());

            base.Output.WriteLine("Before check 4");
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
            base.Output.WriteLine("Done.");
        }
    }
}
