using System;
using System.IO;
using System.Threading.Tasks;
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
            var RamDirectory = FSDirectory.Open(indexPath);

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

        [Fact]
        public async Task GrainTest()
        {
            var grain = new IndexGrain();

            await grain.OnActivateAsync();

            int count = 0;
            int foundCont = 0;

            await Task.WhenAll(Task.Run(async () =>
            {
                for (int i = 0; i < 150; i++)
                {
                    var doc = new GrainDocument(i.ToString());
                    doc.LuceneDocument.Add(new StringField("property",$"i={i}", Field.Store.YES));
                    await grain.WriteIndex(doc);
                    count++;
                }
            }), Task.Run( async () =>
            {
                await Task.Delay(1000);
                for (int i = 0; i < 300; i++)
                {
                    var doc = await grain.QueryByField("property",$"i={i}");
                    count++;

                    if (doc.TotalHits > 0)
                    {
                        foundCont += 1;
                    }

                }
            }));

            await grain.OnDeactivateAsync();

            count.Should().Be(450);
            foundCont.Should().Be(150);

        }
    }
}