using System;
using System.Runtime.Serialization;

namespace Orleans.Indexing
{
    /// <summary>
    /// This exception is thrown when a workflow indexing exception is encountered.
    /// </summary>
    [Serializable]
    public class WorkflowIndexException : IndexException
    {
        public WorkflowIndexException(string message) : base(message)
        {
        }

        protected WorkflowIndexException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
