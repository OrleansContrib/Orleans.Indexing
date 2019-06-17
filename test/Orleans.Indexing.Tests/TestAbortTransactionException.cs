using System;
using System.Runtime.Serialization;

namespace Orleans.Indexing.Tests
{
    [Serializable]
    public class TestAbortTransactionException : Exception
    {
        public TestAbortTransactionException() : base("Aborting indexing test transaction.") { }

        public TestAbortTransactionException(string message) : base(message) { }

        public TestAbortTransactionException(string message, Exception innerException) : base(message, innerException) { }

        protected TestAbortTransactionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
