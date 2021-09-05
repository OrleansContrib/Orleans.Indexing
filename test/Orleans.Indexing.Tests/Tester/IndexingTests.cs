using System;
using System.Threading.Tasks;
using Orleans.Indexing.Tests.Grains;
using Xunit.Abstractions;
using Xunit;

namespace Orleans.Indexing.Tests
{
    [TestCategory("BVT"), TestCategory("Indexing1")]
    public class SomeTests : SimpleIndexingSingleSiloRunner, IClassFixture<IndexingTestFixture>
    {
        public SomeTests(IndexingTestFixture fixture) : base(fixture, null) { }

        [Fact]
        public async Task DoIt()
        {
            await base.StartAndWaitForSecondSilo();

        }
    }
}
