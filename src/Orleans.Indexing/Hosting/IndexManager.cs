using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.ApplicationParts;
using Orleans.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class may be instantiated internally in the ClusterClient as well as in the Silo.
    /// </summary>
    internal class IndexManager : ILifecycleParticipant<IClusterClientLifecycle>
    {
        internal IApplicationPartManager ApplicationPartManager;

        internal ITypeResolver CachedTypeResolver { get; }

        internal IndexRegistry IndexRegistry { get; private set; }

        // Explicit dependency on ServiceProvider is needed so we can retrieve SiloIndexManager.__silo after ctor returns; see comments there.
        // Also, in some cases this is passed through non-injected interfaces such as Hash classes.
        internal IServiceProvider ServiceProvider { get; }

        internal IGrainFactory GrainFactory { get; }

        // Note: For similar reasons as SiloIndexManager.__silo, __indexFactory relies on 'this' to have returned from its ctor.
        internal IndexFactory IndexFactory => this.__indexFactory ?? (__indexFactory = this.ServiceProvider.GetRequiredService<IndexFactory>());
        private IndexFactory __indexFactory;

        internal ILoggerFactory LoggerFactory { get; }

        public IndexManager(IServiceProvider sp, IGrainFactory gf, IApplicationPartManager apm, ILoggerFactory lf, ITypeResolver typeResolver)
        {
            this.ServiceProvider = sp;
            this.GrainFactory = gf;
            this.ApplicationPartManager = apm;
            this.LoggerFactory = lf;
            this.CachedTypeResolver = typeResolver;

            this.IndexingOptions = this.ServiceProvider.GetOptionsByName<IndexingOptions>(IndexingConstants.INDEXING_OPTIONS_NAME);
        }

        public void Participate(IClusterClientLifecycle lifecycle)
        {
            if (!(this is SiloIndexManager))
            {
                lifecycle.Subscribe(this.GetType().FullName, ServiceLifecycleStage.ApplicationServices, ct => this.OnStartAsync(ct), ct => this.OnStopAsync(ct));
            }
        }

        /// <summary>
        /// This method must be called after all application parts have been loaded.
        /// </summary>
        public virtual Task OnStartAsync(CancellationToken ct)
        {
            if (this.IndexRegistry == null)
            {
                this.IndexRegistry = new ApplicationPartsIndexableGrainLoader(this).CreateIndexRegistry();
            }
            return Task.CompletedTask;
        }

        public IndexingOptions IndexingOptions { get; }

        internal int NumWorkflowQueuesPerInterface => this.IndexingOptions.NumWorkflowQueuesPerInterface;

        /// <summary>
        /// This method is called at the begining of the process of uninitializing runtime services.
        /// </summary>
        public virtual Task OnStopAsync(CancellationToken ct) => Task.CompletedTask;

        internal static IndexManager GetIndexManager(ref IndexManager indexManager, IServiceProvider serviceProvider)
            => indexManager ?? (indexManager = GetIndexManager(serviceProvider));

        internal static IndexManager GetIndexManager(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<IndexManager>();

        internal static SiloIndexManager GetSiloIndexManager(ref SiloIndexManager siloIndexManager, IServiceProvider serviceProvider)
            => siloIndexManager ?? (siloIndexManager = GetSiloIndexManager(serviceProvider));

        internal static SiloIndexManager GetSiloIndexManager(IServiceProvider serviceProvider)
            => (SiloIndexManager)serviceProvider.GetRequiredService<IndexManager>();    // Throws an invalid cast operation if we're not on a Silo
    }
}
