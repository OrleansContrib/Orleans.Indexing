using Orleans.TestingHost;

namespace Orleans.Indexing.Tests
{
    public class SingleSiloIndexingTestFixture : IndexingTestFixture
    {
        protected override void ConfigureTestCluster(TestClusterBuilder builder)
        {
            builder.Options.InitialSilosCount = 1;
            base.ConfigureTestCluster(builder);
        }
    }
}
