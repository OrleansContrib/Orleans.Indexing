using System;
using System.IO;
using FluentAssertions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Xunit;
using Directory = Lucene.Net.Store.Directory;

namespace Orleans.Indexing.Tests.LuceneTests
{
    public class IndexTests
    {
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        // RAM
        private string indexPath = $"indexPath-{Guid.NewGuid().ToString("N")}.luc";
        string Text = "This is the text to be indexed.";
        string FieldName = "fieldname";
        Analyzer analyzer = new StandardAnalyzer(AppLuceneVersion);

        [Fact]
        public void Index_Write_Query()
        {
            Directory RamDirectory = FSDirectory.Open(indexPath);

            Analyzer analyzer = new StandardAnalyzer(AppLuceneVersion);

            // Parse a simple query that searches for "text":
            var parser = new QueryParser(AppLuceneVersion, FieldName, analyzer);
            var query = parser.Parse("text");

            var config = new IndexWriterConfig(AppLuceneVersion, analyzer);
            var indexWriter = new IndexWriter(RamDirectory, config);

            var doc1 = new Document();
            doc1.Add(new Field(FieldName, Text, TextField.TYPE_STORED));
            indexWriter.AddDocument(doc1);
            indexWriter.Commit();
            // indexWriter.Flush(true,true);


            var indexReader = DirectoryReader.Open(RamDirectory);
            var indexSearcher = new IndexSearcher(indexReader);

            var hits = indexSearcher.Search(query, null, 1000).ScoreDocs;
            hits.Length.Should().Be(1);

            // Iterate through the results:
            foreach (var t in hits)
            {
                var hitDoc = indexSearcher.Doc(t.Doc);
                hitDoc.Get(FieldName).Should().Be(Text);
            }

            indexWriter.DeleteDocuments(query);
            indexWriter.Commit();
            // indexWriter.Flush(true,true);

            var indexReader2 = DirectoryReader.Open(RamDirectory);
            var indexSearcher2 = new IndexSearcher(indexReader2);
            hits = indexSearcher2.Search(query, null, 1000).ScoreDocs;
            hits.Length.Should().Be(0);

            hits = indexSearcher.Search(query, null, 1000).ScoreDocs;
            hits.Length.Should().Be(1);


            indexReader2.Dispose();
            indexReader.Dispose();
        }
    }
}