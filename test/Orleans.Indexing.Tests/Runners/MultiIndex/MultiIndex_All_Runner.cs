using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    public abstract class MultiIndex_All_Runner : IndexingTestRunnerBase
    {
        protected MultiIndex_All_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_MultiIndex_All()
        {
            const int NumRepsPerTest = 3;
            IEnumerable<Task> getTasks(IEnumerable<Func<IndexingTestRunnerBase, int, Task>> getTasksFuncs)
                => Enumerable.Range(0, NumRepsPerTest).SelectMany(ii => getTasksFuncs.Select(lambda => lambda(this, ii)));

            await Task.WhenAll(getTasks(MultiIndex_AI_EG_Runner.GetAllTestTasks())
                    .Concat(getTasks(MultiIndex_AI_LZ_Runner.GetAllTestTasks()))
                    .Concat(getTasks(MultiIndex_TI_EG_Runner.GetAllTestTasks()))
                    .Concat(getTasks(MultiIndex_TI_LZ_Runner.GetAllTestTasks()))
                    .Concat(getTasks(MultiIndex_XI_EG_Runner.GetAllTestTasks()))
                    .Concat(getTasks(MultiIndex_XI_LZ_Runner.GetAllTestTasks())));
        }
    }
}
