using System;

namespace Orleans.Indexing
{
    [Flags] enum ConsistencyScheme
    {
        Workflow = 1,
        FaultTolerantWorkflow = 3,
        NonFaultTolerantWorkflow = 5,

        Transactional = 1024
    }
}
