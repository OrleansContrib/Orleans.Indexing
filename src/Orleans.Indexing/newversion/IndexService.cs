using System;
using System.IO;
using System.Threading.Tasks;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Services;

namespace Orleans.Indexing
{

    public class GrainDocument
    {
        public GrainDocument(string grainId)
        {
            LuceneDocument = new Document();
            LuceneDocument.Add(new Field("ID", grainId, TextField.TYPE_NOT_STORED));
        }
        public Document LuceneDocument { get; }
    }

    public interface IIndexService : IGrainService
    {

    }


    public interface IIndexGrain : IGrainWithStringKey
    {

    }


    [Reentrant]
    public class IndexService : GrainService, IIndexService
    {

    }

    [PreferLocalPlacement]
    public class IndexGrain : Grain, IIndexGrain
    {
        // Ensures index backward compatibility
        const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public  Task DoIt()
        {


            return Task.CompletedTask;
        }
    }
}