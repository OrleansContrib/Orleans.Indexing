using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Orleans.Indexing
{
    public static class ClientBuilderExtensions
    {
        /// <summary>
        /// Configure cluster to use indexing using a configure action.
        /// </summary>
        public static IClientBuilder UseIndexing(this IClientBuilder builder, Action<IndexingOptions> configureOptions)
            => UseIndexing(builder, ob => ob.Configure(configureOptions));

        /// <summary>
        /// Configure cluster to use indexing using a configuration builder.
        /// </summary>
        public static IClientBuilder UseIndexing(this IClientBuilder builder, Action<OptionsBuilder<IndexingOptions>> configureAction = null)
        {
            return builder.AddSimpleMessageStreamProvider(IndexingConstants.INDEXING_STREAM_PROVIDER_NAME)
                .ConfigureServices(services => services.UseIndexing(configureAction))
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SiloBuilderExtensions).Assembly));
        }

        /// <summary>
        /// Configure cluster services to use indexing using a configuration builder.
        /// </summary>
        private static IServiceCollection UseIndexing(this IServiceCollection services, Action<OptionsBuilder<IndexingOptions>> configureAction = null)
        {
            configureAction?.Invoke(services.AddOptions<IndexingOptions>(IndexingConstants.INDEXING_OPTIONS_NAME));
            services.AddSingleton<IndexFactory>()
                    .AddFromExisting<IIndexFactory, IndexFactory>();
            services.AddSingleton<IndexManager>()
                    .AddFromExisting<ILifecycleParticipant<IClusterClientLifecycle>, IndexManager>();
            return services;
        }
    }
}
