using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Orleans.Indexing.Tests.MultiInterface;
using Orleans.Indexing.Facet;
using System.Reflection;

namespace Orleans.Indexing.Tests
{
    public static class IndexingTestUtils
    {
        private static TimeSpan waitTimeout = TimeSpan.FromSeconds(5);

        public static async Task<int> CountItemsStreamingIn<TIGrain, TProperties, TQueryProp>(this IndexingTestRunnerBase runner,
                                                                Func<IndexingTestRunnerBase, TQueryProp, Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<TQueryProp>>>> queryTupleFunc,
                                                                string propertyName, TQueryProp queryValue, int delayInMilliseconds = 0)
            where TIGrain : IIndexableGrain
        {
            if (delayInMilliseconds > 0)
            {
                await Task.Delay(delayInMilliseconds);
            }
            var taskCompletionSource = new TaskCompletionSource<int>();

            var queryTuple = queryTupleFunc(runner, queryValue);
            var queryItems = queryTuple.Item1;
            var queryPropAsync = queryTuple.Item2;

            int counter = 0;
            await queryItems.ObserveResults(new QueryResultStreamObserver<TIGrain>(/*async*/ entry =>
            {
                counter++;
 
                // For Total indexes, the grain may not be active; querying the property will activate it. If we have a mix of Active and Total
                // indexes on a grain, this will cause the Active counts to be incorrect during testing. TODO: specify per-test whether to retrieve this
                var isActiveIndex = runner.IndexFactory.GetIndex(typeof(TIGrain), IndexUtils.PropertyNameToIndexName(propertyName)).IsActiveIndex();
                var propertyValue = /* isActiveIndex ? (await queryPropAsync(entry)).ToString() : */ "[not retrieved]";
                runner.Output.WriteLine($"grain id = {entry}, {propertyName} = {propertyValue}, primary key = {entry.GetPrimaryKeyLong()}");
                return Task.CompletedTask;
            }, () =>
            {
                taskCompletionSource.SetResult(counter);
                return Task.CompletedTask;
            }));

            int observedCount = await taskCompletionSource.Task.WithTimeout(waitTimeout);
            Assert.Equal(observedCount, (await queryItems.GetResults()).Count());
            return observedCount;
        }

        internal static async Task SetPropertyAndWriteStateAsync<TGrainState>(Action<TGrainState> setterAction, IIndexedState<TGrainState> indexedState, bool retry)
            where TGrainState: class, new()
        {
            const int MaxRetries = 10;
            int retries = 0;
            while (true)
            {
                try
                {
                    await indexedState.PerformUpdate(setterAction);
                    return;
                }
                catch (Exception) when (retry && retries < MaxRetries)
                {
                    ++retries;
                    await indexedState.PerformRead();
                }
            }
        }

        private static IOrleansQueryable<TIGrain, TProperties> QueryActiveGrains<TIGrain, TProperties>(IndexingTestRunnerBase runner)
            where TIGrain : IIndexableGrain
            => runner.IndexFactory.GetActiveGrains<TIGrain, TProperties>();

        #region PlayerGrain

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByPlayerLocation<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : IPlayerGrain, IIndexableGrain where TProperties : IPlayerProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.Location == queryValue select item,
                            entry => entry.GetLocation());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByPlayerLocationTxn<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : IPlayerGrainTransactional, IIndexableGrain where TProperties : IPlayerProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.Location == queryValue select item,
                            entry => entry.GetLocation());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByPlayerScore<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : IPlayerGrain, IIndexableGrain where TProperties : IPlayerProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.Score == queryValue select item,
                            entry => entry.GetScore());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByPlayerScoreTxn<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : IPlayerGrainTransactional, IIndexableGrain where TProperties : IPlayerProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.Score == queryValue select item,
                            entry => entry.GetScore());

        internal static Task<int> GetPlayerLocationCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string location, int delayInMilliseconds = 0)
            where TIGrain : IPlayerGrain, IIndexableGrain where TProperties : IPlayerProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByPlayerLocation<TIGrain, TProperties>(v), nameof(IPlayerProperties.Location), location, delayInMilliseconds);

        internal static Task<int> GetPlayerLocationCountTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string location, int delayInMilliseconds = 0)
            where TIGrain : IPlayerGrainTransactional, IIndexableGrain where TProperties : IPlayerProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByPlayerLocationTxn<TIGrain, TProperties>(v), nameof(IPlayerProperties.Location), location, delayInMilliseconds);

        internal static Task<int> GetPlayerScoreCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int score, int delayInMilliseconds = 0)
            where TIGrain : IPlayerGrain, IIndexableGrain where TProperties : IPlayerProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByPlayerScore<TIGrain, TProperties>(v), nameof(IPlayerProperties.Score), score, delayInMilliseconds);

        internal static Task<int> GetPlayerScoreCountTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int score, int delayInMilliseconds = 0)
            where TIGrain : IPlayerGrainTransactional, IIndexableGrain where TProperties : IPlayerProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByPlayerScoreTxn<TIGrain, TProperties>(v), nameof(IPlayerProperties.Score), score, delayInMilliseconds);

        #endregion PlayerGrain

        #region MultiIndex

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByUniqueInt<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.UniqueInt == queryValue select item,
                            entry => entry.GetUniqueInt());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByUniqueIntTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.UniqueInt == queryValue select item,
                            entry => entry.GetUniqueInt());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByUniqueString<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.UniqueString == queryValue select item,
                            entry => entry.GetUniqueString());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByUniqueStringTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.UniqueString == queryValue select item,
                            entry => entry.GetUniqueString());

        // Note: QueryByNonUnique* reverses the order of the comparison so that both variations are tested.

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByNonUniqueInt<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where queryValue == item.NonUniqueInt select item,
                            entry => entry.GetNonUniqueInt());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByNonUniqueIntTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where queryValue == item.NonUniqueInt select item,
                            entry => entry.GetNonUniqueInt());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByNonUniqueString<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where queryValue == item.NonUniqueString select item,
                            entry => entry.GetNonUniqueString());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByNonUniqueStringTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where queryValue == item.NonUniqueString select item,
                            entry => entry.GetNonUniqueString());

        internal static Task<int> GetUniqueIntCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int uniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByUniqueInt<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.UniqueInt), uniqueValue, delayInMilliseconds);

        internal static Task<int> GetUniqueIntCountTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int uniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByUniqueIntTxn<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.UniqueInt), uniqueValue, delayInMilliseconds);

        internal static Task<int> GetUniqueStringCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string uniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByUniqueString<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.UniqueString), uniqueValue, delayInMilliseconds);

        internal static Task<int> GetUniqueStringCountTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string uniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByUniqueStringTxn<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.UniqueString), uniqueValue, delayInMilliseconds);

        internal static Task<int> GetNonUniqueIntCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int nonUniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByNonUniqueInt<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.NonUniqueInt), nonUniqueValue, delayInMilliseconds);

        internal static Task<int> GetNonUniqueIntCountTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int nonUniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByNonUniqueIntTxn<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.NonUniqueInt), nonUniqueValue, delayInMilliseconds);

        internal static Task<int> GetNonUniqueStringCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string nonUniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByNonUniqueString<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.NonUniqueString), nonUniqueValue, delayInMilliseconds);

        internal static Task<int> GetNonUniqueStringCountTxn<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string nonUniqueValue, int delayInMilliseconds = 0)
            where TIGrain : ITestMultiIndexGrainTransactional, IIndexableGrain where TProperties : ITestMultiIndexProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByNonUniqueStringTxn<TIGrain, TProperties>(v), nameof(ITestMultiIndexProperties.NonUniqueString), nonUniqueValue, delayInMilliseconds);

        #endregion MultiIndex

        #region MultiInterface

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByPersonName<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : IPersonGrain, IIndexableGrain where TProperties : IPersonProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.Name == queryValue select item,
                            entry => entry.GetName());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByPersonAge<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : IPersonGrain, IIndexableGrain where TProperties : IPersonProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.Age == queryValue select item,
                            entry => entry.GetAge());

        // Note: Queries for Job and Employee reverse the order of the comparison so that both variations are tested.
        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByJobTitle<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : IJobGrain, IIndexableGrain where TProperties : IJobProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where queryValue == item.Title select item,
                            entry => entry.GetTitle());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>> QueryByJobDepartment<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, string queryValue)
            where TIGrain : IJobGrain, IIndexableGrain where TProperties : IJobProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<string>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where queryValue == item.Department select item,
                            entry => entry.GetDepartment());

        internal static Tuple<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>> QueryByEmployeeId<TIGrain, TProperties>(
                        this IndexingTestRunnerBase runner, int queryValue)
            where TIGrain : IEmployeeGrain, IIndexableGrain where TProperties : IEmployeeProperties
            => Tuple.Create<IOrleansQueryable<TIGrain, TProperties>, Func<TIGrain, Task<int>>>(
                            from item in QueryActiveGrains<TIGrain, TProperties>(runner) where item.EmployeeId == queryValue select item,
                            entry => entry.GetEmployeeId());

        internal static Task<int> GetNameCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string name, int delayInMilliseconds = 0)
            where TIGrain : IPersonGrain, IIndexableGrain where TProperties : IPersonProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByPersonName<TIGrain, TProperties>(v), nameof(IPersonProperties.Name), name, delayInMilliseconds);

        internal static Task<int> GetPersonAgeCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int age, int delayInMilliseconds = 0)
            where TIGrain : IPersonGrain, IIndexableGrain where TProperties : IPersonProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByPersonAge<TIGrain, TProperties>(v), nameof(IPersonProperties.Age), age, delayInMilliseconds);

        internal static Task<int> GetJobTitleCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string title, int delayInMilliseconds = 0)
            where TIGrain : IJobGrain, IIndexableGrain where TProperties : IJobProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByJobTitle<TIGrain, TProperties>(v), nameof(IJobProperties.Title), title, delayInMilliseconds);

        internal static Task<int> GetJobDepartmentCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, string department, int delayInMilliseconds = 0)
            where TIGrain : IJobGrain, IIndexableGrain where TProperties : IJobProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByJobDepartment<TIGrain, TProperties>(v), nameof(IJobProperties.Department), department, delayInMilliseconds);

        internal static Task<int> GetEmployeeIdCount<TIGrain, TProperties>(this IndexingTestRunnerBase runner, int id, int delayInMilliseconds = 0)
            where TIGrain : IEmployeeGrain, IIndexableGrain where TProperties : IEmployeeProperties
            => runner.CountItemsStreamingIn((r, v) => r.QueryByEmployeeId<TIGrain, TProperties>(v), nameof(IEmployeeProperties.EmployeeId), id, delayInMilliseconds);

        #endregion MultiInterface
    }
}
