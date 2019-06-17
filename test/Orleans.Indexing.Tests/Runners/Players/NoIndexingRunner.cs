using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public abstract class NoIndexingRunner : IndexingTestRunnerBase
    {
        protected NoIndexingRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Indexing_NoIndex()
        {
            IPlayer_NFT_TI_EG_SB p100 = base.GetGrain<IPlayer_NFT_TI_EG_SB>(100);
            IPlayer_NFT_TI_EG_SB p200 = base.GetGrain<IPlayer_NFT_TI_EG_SB>(200);
            IPlayer_NFT_TI_EG_SB p300 = base.GetGrain<IPlayer_NFT_TI_EG_SB>(300);

            await p100.SetLocation(ITC.Tehran);
            await p200.SetLocation(ITC.Tehran);
            await p300.SetLocation(ITC.Yazd);

            Assert.Equal(ITC.Tehran, await p100.GetLocation());
            Assert.Equal(ITC.Tehran, await p200.GetLocation());
            Assert.Equal(ITC.Yazd, await p300.GetLocation());
        }
    }
}
