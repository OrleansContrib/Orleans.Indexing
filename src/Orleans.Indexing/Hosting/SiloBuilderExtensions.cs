using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Indexing.Facet;
using Orleans.Services;

namespace Orleans.Indexing
{
    public static class SiloBuilderExtensions
    {
        /// <summary>
        /// Configure silo to use indexing using a configure action.
        /// </summary>
        public static ISiloHostBuilder UseIndexing(this ISiloHostBuilder builder, Action<IndexingOptions> configureOptions = null)
        {
            // This is necessary to get the configured NumWorkflowQueuesPerInterface for IndexFactory.RegisterIndexWorkflowQueueGrainServices.
            var indexingOptions = new IndexingOptions();
            configureOptions?.Invoke(indexingOptions);

            return builder.AddSimpleMessageStreamProvider(IndexingConstants.INDEXING_STREAM_PROVIDER_NAME)
                .AddMemoryGrainStorage(IndexingConstants.INDEXING_WORKFLOWQUEUE_STORAGE_PROVIDER_NAME)
                .AddMemoryGrainStorage(IndexingConstants.INDEXING_STORAGE_PROVIDER_NAME)
                .AddMemoryGrainStorage(IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SiloBuilderExtensions).Assembly))
                .ConfigureServices(services => services.UseIndexing(indexingOptions))
                .ConfigureServices((context, services) => ApplicationPartsIndexableGrainLoader.RegisterGrainServices(context, services, indexingOptions))
                .UseTransactions();
        }

        /// <summary>
        /// Configure silo services to use indexing using a configuration builder.
        /// </summary>
        private static IServiceCollection UseIndexing(this IServiceCollection services, IndexingOptions indexingOptions)
        {
            services.AddOptions<IndexingOptions>(IndexingConstants.INDEXING_OPTIONS_NAME).Configure(options => options.ShallowCopyFrom(indexingOptions));

            services.AddSingleton<IndexFactory>()
                    .AddFromExisting<IIndexFactory, IndexFactory>();
            services.AddSingleton<SiloIndexManager>()
                    .AddFromExisting<ILifecycleParticipant<ISiloLifecycle>, SiloIndexManager>();
            services.AddFromExisting<IndexManager, SiloIndexManager>();

            // Facet Factory and Mappers
            services.AddTransient<IIndexedStateFactory, IndexedStateFactory>()
                    .AddSingleton(typeof(IAttributeToFactoryMapper<NonFaultTolerantWorkflowIndexedStateAttribute>),
                                  typeof(NonFaultTolerantWorkflowIndexedStateAttributeMapper))
                    .AddSingleton(typeof(IAttributeToFactoryMapper<FaultTolerantWorkflowIndexedStateAttribute>),
                                  typeof(FaultTolerantWorkflowIndexedStateAttributeMapper))
                    .AddSingleton(typeof(IAttributeToFactoryMapper<TransactionalIndexedStateAttribute>),
                                  typeof(TransactionalIndexedStateAttributeMapper));
            return services;
        }

        internal static void AddGrainService(this IServiceCollection services, Func<IServiceProvider, IGrainService> creationFunc)
            => services.AddSingleton(sp => creationFunc(sp));
    }
}
