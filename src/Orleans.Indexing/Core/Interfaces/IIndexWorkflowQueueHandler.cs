using Orleans.Concurrency;
using Orleans.Services;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// The interface for the <see cref="IndexWorkflowQueueGrainService"/>.
    /// </summary>
    [Unordered]
    internal interface IIndexWorkflowQueueHandler : IGrainService, IGrainWithStringKey
    {
        /// <summary>
        /// Accepts a linked list of workflow records to handle until reaching a punctuation
        /// </summary>
        /// <param name="workflowRecordsHead">the head of workflow record linked-list</param>
        Task HandleWorkflowsUntilPunctuation(Immutable<IndexWorkflowRecordNode> workflowRecordsHead);

        /// <summary>
        /// This method is called for initializing the ReincarnatedIndexWorkflowQueueHandler
        /// </summary>
        /// <param name="oldParentGrainService"></param>
        Task Initialize(IIndexWorkflowQueue oldParentGrainService);
    }
}
