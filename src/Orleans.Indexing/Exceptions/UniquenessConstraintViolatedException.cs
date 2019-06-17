using System;
using System.Runtime.Serialization;

namespace Orleans.Indexing
{
    /// <summary>
    /// This exception is thrown when a uniqueness constraint defined on an index is violated.
    /// </summary>
    [Serializable]
    public class UniquenessConstraintViolatedException : IndexException
    {
        public UniquenessConstraintViolatedException(string message) : base(message)
        {
        }

        protected UniquenessConstraintViolatedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
