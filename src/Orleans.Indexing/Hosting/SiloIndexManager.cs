using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.ApplicationParts;
using Orleans.Core;
using Orleans.Indexing.TestInjection;
using Orleans.Runtime;
using Orleans.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is instantiated internally only in the Silo.
    /// </summary>
    class SiloIndexManager : IndexManager, ILifecycleParticipant<ISiloLifecycle>
    {
        internal SiloAddress SiloAddress => this.Silo.SiloAddress;

        // Note: this.Silo must not be called until the Silo ctor has returned to the ServiceProvider which then
        // sets the Singleton; if called during the Silo ctor, the Singleton is not found so another Silo is
        // constructed. Thus we cannot have the Silo on the IndexManager ctor params or retrieve it during
        // IndexManager ctor, because ISiloLifecycle participants are constructed during the Silo ctor.
        internal Silo Silo => _silo ?? (_silo = this.ServiceProvider.GetRequiredService<Silo>());
        private Silo _silo;

        internal IInjectableCode InjectableCode { get; }

        internal IGrainReferenceRuntime GrainReferenceRuntime { get; }

        internal IGrainServiceFactory GrainServiceFactory { get; }
        

        public SiloIndexManager(IServiceProvider sp, IGrainFactory gf, IApplicationPartManager apm, ILoggerFactory lf, ITypeResolver tr)
            : base(sp, gf, apm, lf, tr)
        {
            this.InjectableCode = this.ServiceProvider.GetService<IInjectableCode>() ?? new ProductionInjectableCode();
            this.GrainReferenceRuntime = this.ServiceProvider.GetRequiredService<IGrainReferenceRuntime>();
            this.GrainServiceFactory = this.ServiceProvider.GetRequiredService<IGrainServiceFactory>();
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe(this.GetType().FullName, ServiceLifecycleStage.ApplicationServices, ct => base.OnStartAsync(ct), ct => base.OnStopAsync(ct));
        }

        internal Task<Dictionary<SiloAddress, SiloStatus>> GetSiloHosts(bool onlyActive = false)
            => this.GrainFactory.GetGrain<IManagementGrain>(0).GetHosts(onlyActive);

        public GrainReference MakeGrainServiceGrainReference(int typeData, string systemGrainId, SiloAddress siloAddress)
            => GrainServiceFactory.MakeGrainServiceReference(typeData, systemGrainId, siloAddress);

        internal T GetGrainService<T>(GrainReference grainReference) where T : IGrainService
            => GrainServiceFactory.CastToGrainServiceReference<T>(grainReference);

        internal IStorage<TGrainState> GetStorageBridge<TGrainState>(Grain grain, string storageName) where TGrainState : class, new()
            => new StateStorageBridge<TGrainState>(grain.GetType().FullName, grain.GrainReference, IndexUtils.GetGrainStorage(this.ServiceProvider, storageName), this.LoggerFactory);
    }
}
