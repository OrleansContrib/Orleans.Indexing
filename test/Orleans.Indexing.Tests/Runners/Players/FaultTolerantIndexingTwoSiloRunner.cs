using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public abstract class FaultTolerantIndexingTwoSiloRunner : IndexingTestRunnerBase
    {
        protected FaultTolerantIndexingTwoSiloRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        /// <summary>
        /// Tests basic functionality of HashIndexSingleBucket
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_FT_TI_LZ_SB()
        {
            IPlayer_FT_TI_LZ_SB p1 = base.GetGrain<IPlayer_FT_TI_LZ_SB>(1);
            await p1.SetLocation(ITC.Seattle);

            IPlayer_FT_TI_LZ_SB p2 = base.GetGrain<IPlayer_FT_TI_LZ_SB>(2);
            IPlayer_FT_TI_LZ_SB p3 = base.GetGrain<IPlayer_FT_TI_LZ_SB>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_FT_TI_LZ_SB>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_FT_TI_LZ_SB, PlayerProperties_FT_TI_LZ_SB>(location, ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            p2 = base.GetGrain<IPlayer_FT_TI_LZ_SB>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_3Grains_FT_TI_LZ_PK()
        {
            IPlayer_FT_TI_LZ_PK p1 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(1);
            await p1.SetLocation(ITC.Seattle);

            IPlayer_FT_TI_LZ_PK p2 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(2);
            IPlayer_FT_TI_LZ_PK p3 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(3);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);

            var locIdx = await base.GetAndWaitForIndex<string, IPlayer_FT_TI_LZ_PK>(ITC.LocationProperty);

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCount<IPlayer_FT_TI_LZ_PK, PlayerProperties_FT_TI_LZ_PK>(location, ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            p2 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());

            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        /// <summary>
        /// Tests basic transactional functionality of HashIndexPartitionedPerKey
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

            Task<int> getLocationCount(string location) => this.GetPlayerLocationCountTxn<IPlayer_TXN_TI_EG_PK, PlayerProperties_TXN_TI_EG_PK>(location, ITC.DelayUntilIndexesAreUpdatedLazily);

            Assert.Equal(2, await getLocationCount(ITC.Seattle));

            await p2.Deactivate();
            await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);
            Assert.Equal(2, await getLocationCount(ITC.Seattle));   // Transactional indexes are always Total, so the count remains 2

            p2 = base.GetGrain<IPlayer_TXN_TI_EG_PK>(2);
            Assert.Equal(ITC.Seattle, await p2.GetLocation());
            Assert.Equal(2, await getLocationCount(ITC.Seattle));
        }

        /// <summary>
        /// Tests basic functionality of HashIndexPartitionedPerKey
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_Lookup_5Grains_FT_TI_LZ_PK()
        {
            //await base.StartAndWaitForSecondSilo();

            IPlayer_FT_TI_LZ_PK p1 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(1);
            await p1.SetLocation(ITC.Seattle);

            IPlayer_FT_TI_LZ_PK p2 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(2);
            IPlayer_FT_TI_LZ_PK p3 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(3);
            IPlayer_FT_TI_LZ_PK p4 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(4);
            IPlayer_FT_TI_LZ_PK p5 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(5);

            await p2.SetLocation(ITC.Seattle);
            await p3.SetLocation(ITC.SanFrancisco);
            await p4.SetLocation(ITC.Tehran);
            await p5.SetLocation(ITC.Yazd);

            for (int i = 0; i < 100; ++i)
            {
                var tasks = new List<Task>();
                const int offset = 10000;   // Make unique to avoid conflict with other tests using this grain interface
                for (int j = offset; j < offset + 10; ++j)
                {
                    p1 = base.GetGrain<IPlayer_FT_TI_LZ_PK>(j);
                    tasks.Add(p1.SetLocation(ITC.Yazd + i + "-" + j));
                }
                await Task.WhenAll(tasks);
            }
        }
    }
}
