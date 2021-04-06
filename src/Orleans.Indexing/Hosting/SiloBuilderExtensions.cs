using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Indexing.Facet;
using Orleans.Services;
using System.Linq;

namespace Orleans.Indexing
{
    public static class SiloBuilderExtensions
    {
        /// <summary>
        /// Configure silo to use indexing using a configure action.
        /// </summary>
        public static ISiloBuilder UseIndexing(this ISiloBuilder builder, Action<IndexingOptions> configureOptions = null)
        {
            // This is necessary to get the configured NumWorkflowQueuesPerInterface for IndexFactory.RegisterIndexWorkflowQueueGrainServices.
            var indexingOptions = new IndexingOptions();
            configureOptions?.Invoke(indexingOptions);

            return builder
                .ConfigureDefaults()
                .AddSimpleMessageStreamProvider(IndexingConstants.INDEXING_STREAM_PROVIDER_NAME)
                .AddMemoryGrainStorage(IndexingConstants.INDEXING_WORKFLOWQUEUE_STORAGE_PROVIDER_NAME)
                .AddMemoryGrainStorage(IndexingConstants.INDEXING_STORAGE_PROVIDER_NAME)
                .AddMemoryGrainStorage(IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)
                .ConfigureApplicationParts(parts => parts.AddFrameworkPart(typeof(SiloBuilderExtensions).Assembly))
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

        /// <summary>
        /// Registers an existing registration of <typeparamref name="TImplementation"/> as a provider of service type <typeparamref name="TService"/>.
        /// Copied from https://github.com/dotnet/orleans/blob/master/src/Orleans.Core/Configuration/ServiceCollectionExtensions.cs
        /// </summary>
        /// <typeparam name="TService">The service type being provided.</typeparam>
        /// <typeparam name="TImplementation">The implementation of <typeparamref name="TService"/>.</typeparam>
        /// <param name="services">The service collection.</param>
        internal static void AddFromExisting<TService, TImplementation>(this IServiceCollection services) where TImplementation : TService
        {
            var registration = services.FirstOrDefault(service => service.ServiceType == typeof(TImplementation));
            if (registration != null)
            {
                var newRegistration = new ServiceDescriptor(
                    typeof(TService),
                    sp => sp.GetRequiredService<TImplementation>(),
                    registration.Lifetime);
                services.Add(newRegistration);
            }
        }
    }
}
