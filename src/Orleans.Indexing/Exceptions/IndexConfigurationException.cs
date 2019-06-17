using System;
using System.Runtime.Serialization;

namespace Orleans.Indexing
{
    /// <summary>
    /// This exception is thrown when an indexing configuration exception is encountered.
    /// </summary>
    [Serializable]
    public class IndexConfigurationException : IndexException
    {
        public IndexConfigurationException(string message) : base(message)
        {
        }

        protected IndexConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
