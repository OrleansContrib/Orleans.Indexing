using System;
using System.Runtime.Serialization;

namespace Orleans.Indexing.Tests
{
    [Serializable]
    public class TestFailedException : Exception
    {
        public TestFailedException() : base("Indexing test failed.") { }

        public TestFailedException(string message) : base(message) { }

        public TestFailedException(string message, Exception innerException) : base(message, innerException) { }

        protected TestFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
