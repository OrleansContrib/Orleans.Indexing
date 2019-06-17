using Orleans.TestingHost;
using Orleans.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace Orleans.Indexing.Tests
{
    public class IndexingTestFixture : BaseIndexingFixture
    {
        internal virtual void AddSiloBuilderConfigurator(TestClusterBuilder builder) => builder.AddSiloBuilderConfigurator<SiloBuilderConfigurator>();

        protected override void ConfigureTestCluster(TestClusterBuilder builder)
        {
            base.ConfigureTestClusterForIndexing(builder);
            AddSiloBuilderConfigurator(builder);
            builder.AddClientBuilderConfigurator<ClientBuilderConfigurator>();
        }

        private class SiloBuilderConfigurator : ISiloBuilderConfigurator
        {
            public void Configure(ISiloHostBuilder hostBuilder) =>
                BaseIndexingFixture.Configure(hostBuilder)
                                   .UseIndexing(indexingOptions => ConfigureBasicOptions(indexingOptions));
        }

        private class ClientBuilderConfigurator : IClientBuilderConfigurator
        {
            public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) =>
                BaseIndexingFixture.Configure(clientBuilder)
                                   .UseIndexing(indexingOptions => ConfigureBasicOptions(indexingOptions));
        }

        protected static IndexingOptions ConfigureBasicOptions(IndexingOptions indexingOptions)
        {
            indexingOptions.MaxHashBuckets = 42;
            indexingOptions.NumWorkflowQueuesPerInterface = Math.Min(4, Environment.ProcessorCount); // Debugging startup is slow due to multiple GrainServices if this is high
            return indexingOptions; // allow chaining
        }
    }
}
