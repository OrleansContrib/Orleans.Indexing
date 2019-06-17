using System;
using System.Runtime.Serialization;

namespace Orleans.Indexing
{
    /// <summary>
    /// This exception is thrown when an indexing operation exception is encountered.
    /// </summary>
    [Serializable]
    public class IndexOperationException : IndexException
    {
        public IndexOperationException(string message) : base(message)
        {
        }

        protected IndexOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
