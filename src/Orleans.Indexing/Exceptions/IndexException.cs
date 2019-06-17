using System;
using System.Runtime.Serialization;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// This exception is thrown when a general indexing exception is encountered, or as a base for more specific subclasses.
    /// </summary>
    [Serializable]
    public class IndexException : OrleansException
    {
        public IndexException(string message) : base(message)
        {
        }

        protected IndexException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
