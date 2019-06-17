using System;
using System.Threading.Tasks;

namespace Orleans.Indexing.TestInjection
{
    internal class ProductionInjectableCode : IInjectableCode
    {
        /// <summary>
        /// Do not run the queue thread; this lets the test deactivate the grain to test reactivation and rerunning the queue.
        /// </summary>
        public bool SkipQueueThread
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public bool ShouldRunQueueThread(Func<bool> pred) => pred();


        public bool ForceReincarnatedQueue
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public bool AreQueuesEqual(Func<bool> pred) => pred();

        public Task<T> GetRemainingWorkflowsIn<T>(Func<Task<T>> func) => func();
    }
}
