using Orleans.Indexing.TestInjection;
using System;
using System.Threading.Tasks;

namespace Orleans.Indexing.Tests
{
    internal class TestInjectableCode : IInjectableCode
    {
        /// <summary>
        /// Do not run the queue thread; this lets the test deactivate the grain to test reactivation and rerunning the queue.
        /// </summary>
        public bool SkipQueueThread { private get; set; }

        public bool ShouldRunQueueThread(Func<bool> pred)
        {
            if (this.SkipQueueThread)
            {
                this.SkipQueueThread = false;       // Only do this once.
                return false;
            }
            return pred();
        }

        /// <summary>
        /// Force fault-tolerant grain reactivation to use the reincarnated workflow queue
        /// </summary>
        public bool ForceReincarnatedQueue
        {
            private get => throw new NotImplementedException();
            set
            {
                forceQueuesUnequal = true;          // Only do this once.
                throwOnRemainingWorkflows = true;
            }
        }
        private bool forceQueuesUnequal;
        private bool throwOnRemainingWorkflows;

        public bool AreQueuesEqual(Func<bool> pred)
        {
            if (this.forceQueuesUnequal)
            {
                this.forceQueuesUnequal = false;    // Only do this once.
                return false;
            }
            return pred();
        }

        public Task<T> GetRemainingWorkflowsIn<T>(Func<Task<T>> func)
        {
            if (this.throwOnRemainingWorkflows)
            {
                this.throwOnRemainingWorkflows = false; // Only do this once.
                throw new IndexException("Test Queue");
            }
            return func();
        }
    }
}
