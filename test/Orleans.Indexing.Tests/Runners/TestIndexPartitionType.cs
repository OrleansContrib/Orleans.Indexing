using System;

namespace Orleans.Indexing.Tests
{
    [Flags]
    internal enum TestIndexPartitionType
    {
        SingleBucket = 0x1,
        PerKeyHash = 0x2,
        PerSilo = 0x4,
        All = SingleBucket | PerKeyHash | PerSilo
    }
}
