using System;
using System.Threading.Tasks;

namespace Orleans.Indexing.TestInjection
{
    public interface IInjectableCode
    {
        bool SkipQueueThread { set; }
        bool ShouldRunQueueThread(Func<bool> pred);
        bool ForceReincarnatedQueue { set; }
        bool AreQueuesEqual(Func<bool> pred);
        Task<T> GetRemainingWorkflowsIn<T>(Func<Task<T>> func);    // Must be T because IndexWorkflowQueueRecord is internal
    }
}
