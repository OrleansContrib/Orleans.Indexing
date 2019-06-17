using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System;
using Orleans.Transactions;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public abstract class TransactionalPlayerRunner : IndexingTestRunnerBase
    {
        protected TransactionalPlayerRunner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        const int operationCount = 100;
        const int abortAfterCount = 50;
        const int noAbortCount = -1;

        private Task<int> getScoreCount(int score) => this.GetPlayerScoreCountTxn<ITransactionalPlayerGrain, TransactionalPlayerProperties>(score);
        private Task<int> getLocationCount(string location) => this.GetPlayerLocationCountTxn<ITransactionalPlayerGrain, TransactionalPlayerProperties>(location);

        private Task GetIndexes()
            => Task.WhenAll(base.GetAndWaitForIndex<int, ITransactionalPlayerGrain>(ITC.ScoreProperty),
                            base.GetAndWaitForIndex<string, ITransactionalPlayerGrain>(ITC.LocationProperty));

        /// <summary>
        /// Tests basic functionality of Transactional insert aborts
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Commit_Insert()
        {
            const int firstScore = 100;
            const string firstLocation = ITC.Seattle;

            var rootGrain = base.GetGrain<ITransactionalPlayerGrainRoot>(0);

            await GetIndexes();

            await rootGrain.InsertAsync(firstScore, firstLocation, operationCount, noAbortCount);

            Assert.Equal(operationCount, await getScoreCount(firstScore));
            Assert.Equal(operationCount, await getLocationCount(firstLocation));
        }

        /// <summary>
        /// Tests basic functionality of Transactional insert aborts
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Abort_Insert()
        {
            const int firstScore = 1000;
            const string firstLocation = ITC.Redmond;

            var rootGrain = base.GetGrain<ITransactionalPlayerGrainRoot>(0);

            await GetIndexes();

            try
            {
                await rootGrain.InsertAsync(firstScore, firstLocation, operationCount, abortAfterCount);
            }
            catch (Exception ex)
            {
                Assert.IsType<OrleansTransactionAbortedException>(ex);
            }

            Assert.Equal(0, await getScoreCount(firstScore));
            Assert.Equal(0, await getLocationCount(firstLocation));
        }

        /// <summary>
        /// Tests basic functionality of Transactional update aborts
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Commit_Update()
        {
            const int firstScore = 10000;
            const int secondScore = 20000;
            const string firstLocation = ITC.Kirkland;
            const string secondLocation = ITC.Bellevue;

            var rootGrain = base.GetGrain<ITransactionalPlayerGrainRoot>(0);

            await GetIndexes();

            await rootGrain.InsertAsync(firstScore, firstLocation, operationCount, noAbortCount);
            Assert.Equal(operationCount, await getScoreCount(firstScore));
            Assert.Equal(operationCount, await getLocationCount(firstLocation));

            await rootGrain.UpdateAsync(firstScore, secondScore, firstLocation, secondLocation, operationCount, noAbortCount);

            Assert.Equal(0, await getScoreCount(firstScore));
            Assert.Equal(0, await getLocationCount(firstLocation));
            Assert.Equal(operationCount, await getScoreCount(secondScore));
            Assert.Equal(operationCount, await getLocationCount(secondLocation));
        }

        /// <summary>
        /// Tests basic functionality of Transactional update aborts
        /// </summary>
        [Fact, TestCategory("BVT"), TestCategory("Indexing"), TestCategory("TransactionalIndexing")]
        public async Task Test_Abort_Update()
        {
            const int firstScore = 100000;
            const int secondScore = 200000;
            const string firstLocation = ITC.NewYork;
            const string secondLocation = ITC.LosAngeles;

            var rootGrain = base.GetGrain<ITransactionalPlayerGrainRoot>(0);

            await GetIndexes();

            await rootGrain.InsertAsync(firstScore, firstLocation, operationCount, noAbortCount);
            Assert.Equal(operationCount, await getScoreCount(firstScore));
            Assert.Equal(operationCount, await getLocationCount(firstLocation));

            try
            {
                await rootGrain.UpdateAsync(firstScore, secondScore, firstLocation, secondLocation, operationCount, abortAfterCount);
            }
            catch (Exception ex)
            {
                Assert.IsType<OrleansTransactionAbortedException>(ex);
            }

            Assert.Equal(operationCount, await getScoreCount(firstScore));
            Assert.Equal(operationCount, await getLocationCount(firstLocation));
            Assert.Equal(0, await getScoreCount(secondScore));
            Assert.Equal(0, await getLocationCount(secondLocation));
        }
    }
}
