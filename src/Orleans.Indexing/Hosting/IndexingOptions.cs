using System;

namespace Orleans.Indexing
{
    public interface IIndexingOptions
    {
        int MaxHashBuckets { get; set; }

        int NumWorkflowQueuesPerInterface { get; set; }
    }

    public class IndexingOptions : IIndexingOptions
    {
        public IndexingOptions()
        {
            this.MaxHashBuckets = -1;
            this.NumWorkflowQueuesPerInterface = Environment.ProcessorCount;
        }

        public int MaxHashBuckets { get; set; }
        public int NumWorkflowQueuesPerInterface { get; set; }
    }
}
