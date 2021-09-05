using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.TestingHost;
using Orleans.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using TestExtensions;

namespace Orleans.Indexing.Tests
{
    public abstract class BaseIndexingFixture : BaseTestClusterFixture
    {
        protected TestClusterBuilder ConfigureTestClusterForIndexing(TestClusterBuilder builder)
        {
            // Currently nothing
            //builder.Options.InitialSilosCount = 1;    // For debugging if needed
            return builder;
        }

        internal static ISiloBuilder Configure(ISiloBuilder hostBuilder, string databaseName = null)
        {
            hostBuilder.AddMemoryGrainStorage(IndexingTestConstants.GrainStore)
                       .AddMemoryGrainStorage("PubSubStore") // PubSubStore service is needed for the streams underlying OrleansQueryResults
                       .ConfigureLogging(loggingBuilder =>
                       {
                           loggingBuilder.SetMinimumLevel(LogLevel.Information);
                           loggingBuilder.AddDebug();
                       })
                       .ConfigureApplicationParts(parts =>
                       {
                           parts.AddApplicationPart(typeof(BaseIndexingFixture).Assembly).WithReferences();
                       });
            return hostBuilder;
        }

        internal static IClientBuilder Configure(IClientBuilder clientBuilder)
        {
            return clientBuilder.ConfigureLogging(loggingBuilder =>
                                {
                                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                                    loggingBuilder.AddDebug();
                                })
                                .ConfigureApplicationParts(parts =>
                                {
                                    parts.AddApplicationPart(typeof(BaseIndexingFixture).Assembly);
                                });
        }
    }
}
