using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests.MultiInterface
{
    public abstract class MultiInterface_All_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_All_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_MultiInterface_All()
        {
            const int NumRepsPerTest = 3;
            IEnumerable<Task> getTasks(IEnumerable<Func<IndexingTestRunnerBase, int, Task>> getTasksFuncs)
                => Enumerable.Range(0, NumRepsPerTest).SelectMany(ii => getTasksFuncs.Select(lambda => lambda(this, ii)));

            // Flags for bug diagnosing
            var testIndexTypes = TestIndexPartitionType.All;
            //var testIndexTypes = TestIndexPartitionType.PerSilo;
            await Task.WhenAll(getTasks(MultiInterface_AI_EG_Runner.GetAllTestTasks(testIndexTypes))
                    .Concat(getTasks(MultiInterface_AI_LZ_Runner.GetAllTestTasks(testIndexTypes)))
                    .Concat(getTasks(MultiInterface_TI_EG_Runner.GetAllTestTasks(testIndexTypes)))
                    .Concat(getTasks(MultiInterface_TI_LZ_Runner.GetAllTestTasks(testIndexTypes)))
                    .Concat(getTasks(MultiInterface_XI_EG_Runner.GetAllTestTasks(testIndexTypes)))
                    .Concat(getTasks(MultiInterface_XI_LZ_Runner.GetAllTestTasks(testIndexTypes))));
        }
    }
}