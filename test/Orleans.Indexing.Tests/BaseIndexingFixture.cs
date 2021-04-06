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
            string cosmosDBEndpoint = string.Empty, cosmosDBKey = string.Empty;
            if (databaseName != null)
            {
                if (!TestDefaultConfiguration.GetValue("CosmosDBEndpoint", out cosmosDBEndpoint)
                    || !TestDefaultConfiguration.GetValue("CosmosDBKey", out cosmosDBKey))
                {
                    throw new IndexConfigurationException("CosmosDB connection values are not specified");
                }
            }

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
            return databaseName != null
                ? hostBuilder.AddCosmosDBGrainStorage(IndexingTestConstants.CosmosDBGrainStorage, opt =>
                    {
                        opt.AccountEndpoint = cosmosDBEndpoint;
                        opt.AccountKey = cosmosDBKey;
                        opt.DropDatabaseOnInit = true;
                        opt.CanCreateResources = true;
                        opt.DB = databaseName;
                        opt.InitStage = ServiceLifecycleStage.RuntimeStorageServices;
                        opt.StateFieldsToIndex.AddRange(GetDSMIStateFieldsToIndex());
                    })
                : hostBuilder;
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

        // Code below adapted from ApplicationPartsIndexableGrainLoader to identify the necessary fields for the DSMI storage
        // provider to index.

        private static IEnumerable<string> GetDSMIStateFieldsToIndex()
        {
            var grainClassTypes = typeof(BaseIndexingFixture).Assembly.GetConcreteGrainClasses().ToArray();

            // Orleans.CosmosDB appends the field names to "State."; thus we do not prepend the interface names.
            var interfacesToIndexedPropertyNames = new Dictionary<Type, string[]>();
            foreach (var grainClassType in grainClassTypes)
            {
                GetDSMIFieldsForASingleGrainType(grainClassType, interfacesToIndexedPropertyNames);
            }
            return new HashSet<string>(interfacesToIndexedPropertyNames.Where(kvp => kvp.Value.Length > 0).SelectMany(kvp => kvp.Value));
        }

        internal static void GetDSMIFieldsForASingleGrainType(Type grainClassType, Dictionary<Type, string[]> interfacesToIndexedPropertyNames)
        {
            foreach (var (grainInterfaceType, propertiesClassType) in ApplicationPartsIndexableGrainLoader.EnumerateIndexedInterfacesForAGrainClassType(grainClassType)
                                                                        .Where(tup => !interfacesToIndexedPropertyNames.ContainsKey(tup.interfaceType)))
            {
                // TODO: See comments in DSMIGrain.LookupGrainReferences; get the path with and without the transactional storage wrapper prefix.
                interfacesToIndexedPropertyNames[grainInterfaceType] = propertiesClassType.GetProperties()
                                                                        .Where(propInfo => propInfo.GetCustomAttributes<StorageManagedIndexAttribute>(inherit: false).Any())
                                                                        .Select(propInfo => IndexingConstants.UserStatePrefix + propInfo.Name)
                                                                        .SelectMany(path => new[] {path, $"{nameof(TransactionalStateRecord<object>.CommittedState)}.{path}"})
                                                                        .ToArray();
            }
        }
    }
}
