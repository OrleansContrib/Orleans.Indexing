using Orleans.Runtime;
using System;

namespace Orleans.Indexing
{
    /// <summary>
    /// The persistent unit for storing the information for an <see cref="IndexWorkflowQueueGrainService"/>
    /// </summary>
    /// <remarks>This requires GrainState instead of using StateStorageBridge, due to having to set the ETag for upsert.</remarks>
    [Serializable]
    internal class IndexWorkflowQueueState : GrainState<IndexWorkflowQueueEntry>
    {
        public IndexWorkflowQueueState() : base(new IndexWorkflowQueueEntry())
        {
        }
    }

    /// <summary>
    /// All the information stored for a single <see cref="IndexWorkflowQueueGrainService"/>
    /// </summary>
    [Serializable]
    internal class IndexWorkflowQueueEntry
    {
        // Updates that must be propagated to indexes.
        internal IndexWorkflowRecordNode WorkflowRecordsHead;

        public IndexWorkflowQueueEntry() => this.WorkflowRecordsHead = null;
    }
}
