using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
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
using Directory = System.IO.Directory;

namespace Orleans.Indexing
{

    public class GrainDocument
    {
        public static string GrainIdFieldName = "___grainId";
        public GrainDocument(string grainId)
        {
            this.LuceneDocument = new Document();
            this.LuceneDocument.Add(new StringField(GrainIdFieldName, grainId, Field.Store.NO));
        }
        public Document LuceneDocument { get; }
    }

    public interface IIndexService : IGrainService
    {

    }


    public interface IIndexGrain : IGrainWithStringKey
    {
        Task WriteIndex(GrainDocument document);
        Task<TopDocs> QueryByField(string field, string query, int take = 1000);
    }


    [Reentrant]
    public class IndexService : GrainService, IIndexService
    {

    }

    [PreferLocalPlacement]
    public class IndexGrain : Grain, IIndexGrain
    {
        // Ensures index backward compatibility
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        // private static string indexPath = "indexPath";
        private BaseDirectory indexDirectory;
        private DirectoryReader directoryReader;
        private Analyzer analyzer;
        private IndexWriter indexWriter;
        private IndexSearcher indexSearcher;

        public override Task OnActivateAsync()
        {
            this.indexDirectory = GetDirectory();
            this.analyzer = new StandardAnalyzer(AppLuceneVersion);
            var config = new IndexWriterConfig(AppLuceneVersion, this.analyzer);
            this.indexWriter = new IndexWriter(this.indexDirectory, config);
            this.indexWriter.Commit();

            this.directoryReader = DirectoryReader.Open(this.indexDirectory);
            this.indexSearcher = new IndexSearcher(this.directoryReader);

            return Task.CompletedTask;
        }

        public override Task OnDeactivateAsync()
        {
            this.indexWriter?.Dispose();
            this.analyzer?.Dispose();
            this.directoryReader?.Dispose();
            return Task.CompletedTask;
        }

        private BaseDirectory GetDirectory()
        {
            return new RAMDirectory();
            // return FSDirectory.Open(indexPath);
        }

        public Task WriteIndex(GrainDocument document) => Task.Run(() =>
        {
            var parser = new QueryParser(AppLuceneVersion, GrainDocument.GrainIdFieldName, this.analyzer);
            var query = parser.Parse(document.LuceneDocument.GetField(GrainDocument.GrainIdFieldName).GetStringValue());
            this.indexWriter.DeleteDocuments(query);
            this.indexWriter.AddDocument(document.LuceneDocument);
            this.indexWriter.Commit();

            this.directoryReader = DirectoryReader.OpenIfChanged(this.directoryReader) ?? this.directoryReader;
            this.indexSearcher = new IndexSearcher(this.directoryReader);

            return Task.CompletedTask;
        });

        public Task<TopDocs> QueryByField(string field, string query, int take = 1000) => Task.Run(() =>
        {
            var parser = new QueryParser(AppLuceneVersion, GrainDocument.GrainIdFieldName, this.analyzer);
            var result = this.indexSearcher.Search(parser.Parse(query), null, take);
            return result;
        });
    }
}