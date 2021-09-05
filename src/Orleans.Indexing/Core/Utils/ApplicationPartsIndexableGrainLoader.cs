using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.ApplicationParts;
using Orleans.Hosting;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    internal class ApplicationPartsIndexableGrainLoader
    {
        private readonly IndexManager indexManager;
        private readonly ILogger logger;

        private static readonly Type indexAttrType = typeof(IndexAttribute);
        private static readonly PropertyInfo indexTypeProperty = typeof(IndexAttribute).GetProperty(nameof(IndexAttribute.IndexType));
        private static readonly PropertyInfo isEagerProperty = typeof(IndexAttribute).GetProperty(nameof(IndexAttribute.IsEager));
        private static readonly PropertyInfo isUniqueProperty = typeof(IndexAttribute).GetProperty(nameof(IndexAttribute.IsUnique));
        private static readonly PropertyInfo maxEntriesPerBucketProperty = typeof(IndexAttribute).GetProperty(nameof(IndexAttribute.MaxEntriesPerBucket));
        private static readonly PropertyInfo transactionalVariantProperty = typeof(TransactionalIndexVariantAttribute).GetProperty(nameof(TransactionalIndexVariantAttribute.TransactionalIndexType));

        internal ApplicationPartsIndexableGrainLoader(IndexManager indexManager)
        {
            this.indexManager = indexManager;
            this.logger = this.indexManager.LoggerFactory.CreateLoggerWithFullCategoryName<ApplicationPartsIndexableGrainLoader>();
        }

        private static Type[] GetIndexedConcreteGrainClasses(IApplicationPartManager applicationPartManager, ILogger logger = null)
            => applicationPartManager.ApplicationParts.OfType<AssemblyPart>()
                .SelectMany(part => part.Assembly.GetIndexedGrainClasses())
                .ToArray();

        /// <summary>
        /// This method crawls the assemblies and looks for the index definitions (determined by extending the <see cref="IIndexableGrain{TProperties}"/>
        /// interface and adding annotations to properties in TProperties), and creates the indexes.
        /// </summary>
        /// <returns>An index registry for the silo. </returns>
        internal IndexRegistry CreateIndexRegistry()
            => GetIndexRegistry(this, GetIndexedConcreteGrainClasses(this.indexManager.ApplicationPartManager, this.logger));

        /// <summary>
        /// This method crawls the assemblies and looks for the index definitions (determined by extending the <see cref="IIndexableGrain{TProperties}"/>
        /// interface and adding annotations to properties in TProperties), and registers the grain services needed for queues and PerSilo partitioning.
        /// </summary>
        /// <remarks>This two-step approach (RegisterGrainServices and then CreateIndexRegistry) is necessary because GrainServices should be
        /// registered to the IServiceCollection before Silo construction and the rest of Index creation requires the IServiceProvider which
        /// is created as part of Silo construction.</remarks>
        /// <returns>An index registry for the silo. </returns>
        internal static void RegisterGrainServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services, IndexingOptions indexingOptions)
        {
            var indexedClasses = new HashSet<Type>();
            var indexedInterfaces = new HashSet<Type>();

            foreach (var grainClassType in GetIndexedConcreteGrainClasses(context.GetApplicationPartManager()))
            {
                var consistencyScheme = grainClassType.GetConsistencyScheme();
                if (consistencyScheme == ConsistencyScheme.Transactional)
                {
                    continue;
                }

                var indexedInterfacesAndProperties = EnumerateIndexedInterfacesForAGrainClassType(grainClassType).ToList();
                foreach (var (grainInterfaceType, propertiesClassType) in indexedInterfacesAndProperties)
                {
                    if (indexedInterfaces.Contains(grainInterfaceType))
                    {
                        continue;
                    }
                    indexedInterfaces.Add(grainInterfaceType);

                    var createQueues = consistencyScheme != ConsistencyScheme.Transactional;
                    foreach (var propInfo in propertiesClassType.GetProperties())
                    {
                        var indexName = IndexUtils.PropertyNameToIndexName(propInfo.Name);
                        var indexAttrs = propInfo.GetCustomAttributes<IndexAttribute>(inherit: false);
                        foreach (var indexAttr in indexAttrs)
                        {
                            if (createQueues && !(bool)isEagerProperty.GetValue(indexAttr))
                            {
                                // Queues are per-interface, not per-index.
                                IndexFactory.RegisterIndexWorkflowQueueGrainServices(services, grainInterfaceType, indexingOptions,
                                                                                     consistencyScheme == ConsistencyScheme.FaultTolerantWorkflow);
                            }
                            createQueues = false;

                            var indexType = (Type)indexTypeProperty.GetValue(indexAttr);
                            var regMethod = indexType.GetCustomAttribute<PerSiloIndexGrainServiceClassAttribute>(inherit:false)
                                                    ?.GrainServiceClassType
                                                    ?.GetMethod("RegisterGrainService", BindingFlags.Static | BindingFlags.NonPublic);
                            if (regMethod != null)  // Static method so cannot use an interface
                            {
                                var regDelegate = (Action<IServiceCollection, Type, string>)Delegate.CreateDelegate(typeof(Action<IServiceCollection, Type, string>), regMethod);
                                regDelegate(services, grainInterfaceType, indexName);
                            }
                        }
                    }
                }
            }
        }

        internal static IndexRegistry GetIndexRegistry(ApplicationPartsIndexableGrainLoader loader, IEnumerable<Type> grainClassTypes)
        {
            var registry = new IndexRegistry();
            foreach (var grainClassType in grainClassTypes)
            {
                if (registry.ContainsGrainType(grainClassType))
                {
                    throw new InvalidOperationException($"Precondition violated: GetIndexRegistry should not encounter a duplicate grain class type ({IndexUtils.GetFullTypeName(grainClassType)})");
                }
                GetIndexesForASingleGrainType(loader, registry, grainClassType);
            }
            return registry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetIndexesForASingleGrainType(ApplicationPartsIndexableGrainLoader loader, IndexRegistry registry, Type grainClassType)
        {
            // First see if any indexed interfaces on this grain were already encountered on another grain (unless we're
            // in validation mode, which doesn't create the indexes).
            var indexedInterfacesAndProperties = EnumerateIndexedInterfacesForAGrainClassType(grainClassType).ToList();
            var indexedInterfaces = loader != null
                    ? new HashSet<Type>(indexedInterfacesAndProperties.Select(tup => tup.interfaceType).Where(itfType => registry.ContainsKey(itfType)))
                    : new HashSet<Type>();

            var grainIndexesAreEager = indexedInterfaces.Count > 0 ? registry[indexedInterfaces.First()].HasAnyEagerIndex : default(bool?);
            var consistencyScheme = grainClassType.GetConsistencyScheme();

            // Now find all indexed interfaces we're seeing for the first time (again, unless we're in validation mode).
            foreach (var (grainInterfaceType, propertiesClassType) in indexedInterfacesAndProperties)
            {
                if (!indexedInterfaces.Contains(grainInterfaceType))
                {
                    grainIndexesAreEager = CreateIndexesForASingleInterface(loader, registry, propertiesClassType, grainInterfaceType,
                                                                            grainClassType, consistencyScheme, grainIndexesAreEager);
                    indexedInterfaces.Add(grainInterfaceType);
                }
            }

            IReadOnlyDictionary<string, object> getNullValuesDictionary()
            {
                IEnumerable<(string propName, (string itfName, object nullValue))> getNullPropertyValuesForInterface(Type interfaceType)
                    => registry[interfaceType].PropertiesClassType.GetProperties()
                                              .Select(info => (name: info.Name, nullSpec: (itfname: interfaceType.Name, nullValue: IndexUtils.GetNullValue(info))))
                                              .Where(p => p.nullSpec.nullValue != null);

                Dictionary<string, (string, object)> addToDict(Dictionary<string, (string, object)> dict, (string propName, (string itfName, object nullValue) nullSpec) current)
                {
                    bool isInDict(string propName)
                    {
                        return dict.TryGetValue(propName, out (string itfName, object nullValue) prevNullSpec)
                            ? (prevNullSpec.nullValue.Equals(current.nullSpec.nullValue)
                                ? true
                                : throw new IndexConfigurationException($"Property {propName} has conflicting NullValues defined on interfaces {prevNullSpec.itfName} and {current.nullSpec.itfName}"))
                            : false;
                    }

                    if (!isInDict(current.propName))
                    {
                        dict[current.propName] = current.nullSpec;
                    }
                    return dict;
                }

                return indexedInterfaces.SelectMany(itf => getNullPropertyValuesForInterface(itf))
                                        .Aggregate(new Dictionary<string, (string itfName, object nullValue)>(), (dict, pair) => addToDict(dict, pair))
                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.nullValue);
            }

            if (indexedInterfaces.Count > 0)
            {
                registry.SetGrainIndexes(grainClassType, indexedInterfaces.ToArray(), getNullValuesDictionary());
            }
        }

        internal static IEnumerable<(Type interfaceType, Type propertiesType)> EnumerateIndexedInterfacesForAGrainClassType(Type grainClassType)
        {
            Type[] allInterfaces = grainClassType.GetInterfaces();

            // If there are any interface that directly extends IIndexableGrain<TProperties>...
            var indexableBaseInterfaces = allInterfaces.Where(itf => itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(IIndexableGrain<>)).ToArray();
            if (indexableBaseInterfaces.Length == 0)
            {
                yield break;
            }

            foreach (var indexableBaseInterface in indexableBaseInterfaces)
            {
                // ... and its generic argument is a class (TProperties)...
                var propertiesClassType = indexableBaseInterface.GetGenericArguments()[0];
                if (propertiesClassType.GetTypeInfo().IsClass)
                {
                    // ... then add the indexes for all the descendant interfaces of IIndexableGrain<TProperties>; these interfaces are defined by end-users.
                    foreach (var grainInterfaceType in allInterfaces.Where(itf => !indexableBaseInterfaces.Contains(itf)
                                                                                    && indexableBaseInterface.IsAssignableFrom(itf)))
                    {
                        yield return (grainInterfaceType, propertiesClassType);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool? CreateIndexesForASingleInterface(ApplicationPartsIndexableGrainLoader loader, IndexRegistry registry,
                                                              Type propertiesClassType, Type grainInterfaceType, Type grainClassType,
                                                              ConsistencyScheme consistencyScheme, bool? grainIndexesAreEager)
        {
            // All the properties in TProperties are scanned for Index annotation.
            // If found, the index is created using the information provided in the annotation.
            var indexesOnInterface = new NamedIndexMap(propertiesClassType);
            foreach (var propInfo in propertiesClassType.GetProperties())
            {
                var indexAttrs = propInfo.GetCustomAttributes<IndexAttribute>(inherit:false);
                foreach (var indexAttr in indexAttrs)
                {
                    var indexName = IndexUtils.PropertyNameToIndexName(propInfo.Name);
                    if (indexesOnInterface.ContainsKey(indexName))
                    {
                        throw new InvalidOperationException($"An index named {indexName} already exists for user-defined grain interface {grainInterfaceType.Name}");
                    }

                    var indexType = (Type)indexTypeProperty.GetValue(indexAttr);
                    if (consistencyScheme == ConsistencyScheme.Transactional)
                    {
                        var transactionalVariantAttr = indexType.GetCustomAttribute<TransactionalIndexVariantAttribute>();
                        if (transactionalVariantAttr != null)
                        {
                            indexType = transactionalVariantAttr.TransactionalIndexType;
                        }
                    }
                    if (indexType.IsGenericTypeDefinition)
                    {
                        // For the (Active|Total) constructors that take (Active|Total)IndexType parameters, leaving the indexType's key type and interface type
                        // generic arguments as "<,>", this fills them in.
                        indexType = indexType.MakeGenericType(propInfo.PropertyType, grainInterfaceType);
                    }

                    // If it's not eager, then it's configured to be lazily updated
                    var isEager = (bool)isEagerProperty.GetValue(indexAttr);
                    if (!grainIndexesAreEager.HasValue) grainIndexesAreEager = isEager;
                    var isUnique = (bool)isUniqueProperty.GetValue(indexAttr);

                    ValidateSingleIndex(indexAttr, grainInterfaceType, grainClassType, propertiesClassType, propInfo, grainIndexesAreEager,
                                        consistencyScheme, isEager:isEager, isUnique:isUnique);   // Multiple bools, so use param names for safety

                    var maxEntriesPerBucket = (int)maxEntriesPerBucketProperty.GetValue(indexAttr);
                    if (loader != null)
                    {
                        loader.CreateIndex(propertiesClassType, grainInterfaceType, indexesOnInterface, propInfo, indexName, indexType, isEager, isUnique, maxEntriesPerBucket);
                    }
                    else
                    {
                        IndexFactory.ValidateIndexType(indexType, propInfo, out _, out _);
                    }
                }
            }
            registry[grainInterfaceType] = indexesOnInterface;
            return grainIndexesAreEager;
        }

        private void CreateIndex(Type propertiesArg, Type grainInterfaceType, NamedIndexMap indexesOnGrain, PropertyInfo property,
                                 string indexName, Type indexType, bool isEager, bool isUnique, int maxEntriesPerBucket)
        {
            indexesOnGrain[indexName] = this.indexManager.IndexFactory.CreateIndex(indexType, indexName, isUnique, isEager, maxEntriesPerBucket, property);
            this.logger.Info($"Index created: Interface = {grainInterfaceType.Name}, property = {propertiesArg.Name}, index = {indexName}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateSingleIndex(IndexAttribute indexAttr, Type grainInterfaceType, Type grainClassType, Type propertiesArgType,
                                                PropertyInfo propInfo, bool? grainIndexesAreEager, ConsistencyScheme consistencyScheme, bool isEager, bool isUnique)
        {
            var indexType = (Type)indexTypeProperty.GetValue(indexAttr);
            var isTotalIndex = indexType.IsTotalIndex();
            var isPerSiloIndex = indexType.IsPartitionedPerSiloIndex();
            var isFaultTolerantWorkflow = consistencyScheme == ConsistencyScheme.FaultTolerantWorkflow;
            var isTransactional = consistencyScheme == ConsistencyScheme.Transactional;

            if (indexAttr is ActiveIndexAttribute && isUnique)
            {
                // See comments in ActiveIndexAttribute for details of why this is disallowed.
                throw new InvalidOperationException($"An active Index cannot be configured to be unique, because multiple activations, persisting, and deactivations can create duplicates." +
                                                    $" Active Index of type {IndexUtils.GetFullTypeName(indexType)} is defined to be unique on property {propInfo.Name}" +
                                                    $" of class {IndexUtils.GetFullTypeName(propertiesArgType)} on the {IndexUtils.GetFullTypeName(grainInterfaceType)} grain interface.");
            }
            if (isPerSiloIndex && isUnique)
            {
                throw new InvalidOperationException($"Unique indexes cannot be partitioned per silo because uniqueness across silos is currently not enforced." +
                                                    $" Partitioned Per Silo Index of type {IndexUtils.GetFullTypeName(indexType)} is defined to be unique on property {propInfo.Name}" +
                                                    $" of class {IndexUtils.GetFullTypeName(propertiesArgType)} on the {IndexUtils.GetFullTypeName(grainInterfaceType)} grain interface.");
            }
            if (isFaultTolerantWorkflow && isEager)
            {
                throw new InvalidOperationException($"A workflow-fault-tolerant consistency scheme cannot be configured to eagerly update its indexes." +
                                                    $" The only option for updating the indexes of a fault-tolerant indexable grain is lazy updating." +
                                                    $" The index of type {IndexUtils.GetFullTypeName(indexType)} is defined to be updated eagerly on property {propInfo.Name}" +
                                                    $" of class {IndexUtils.GetFullTypeName(propertiesArgType)} on the {IndexUtils.GetFullTypeName(grainClassType)} grain implementation class.");
            }
            if (isFaultTolerantWorkflow && indexType.IsActiveIndex())
            {
                throw new InvalidOperationException($"A workflow-fault-tolerant consistency scheme cannot be used with an Active index, because it will continually be reactivated as part of the fault-tolerant workflow." +
                                                    $" An Active index of type {IndexUtils.GetFullTypeName(indexType)} is defined on property {propInfo.Name}" +
                                                    $" of class {IndexUtils.GetFullTypeName(propertiesArgType)} on the {IndexUtils.GetFullTypeName(grainClassType)} grain implementation class" +
                                                    $" which uses workflow-fault-tolerant indexing.");
            }
            if (isTransactional && !isEager)
            {
                throw new InvalidOperationException($"A transactional consistency scheme must be configured to eagerly update its indexes." +
                                                    $" The index of type {IndexUtils.GetFullTypeName(indexType)} is defined to be updated eagerly on property {propInfo.Name}" +
                                                    $" of class {IndexUtils.GetFullTypeName(propertiesArgType)} on {IndexUtils.GetFullTypeName(grainClassType)} grain implementation class.");
            }
            if (isTransactional && indexType.IsActiveIndex())
            {
                throw new InvalidOperationException($"A transactional consistency scheme cannot be used with an Active index, because activation and deactivation do not always run in a transactional context." +
                                                    $" An Active index of type {IndexUtils.GetFullTypeName(indexType)} is defined on property {propInfo.Name}" +
                                                    $" of class {IndexUtils.GetFullTypeName(propertiesArgType)} on the {IndexUtils.GetFullTypeName(grainClassType)} grain implementation class" +
                                                    $" which uses transactional indexing.");
            }
            if (grainIndexesAreEager.HasValue && grainIndexesAreEager.Value != isEager)
            {
                throw new InvalidOperationException($"Some indexes on {IndexUtils.GetFullTypeName(grainClassType)} grain implementation class are defined as eager while others are lazy." +
                                                    $" The index of type {IndexUtils.GetFullTypeName(indexType)} is defined to be updated {(isEager ? "eagerly" : "lazily")} on property { propInfo.Name}" +
                                                    $" of property class {IndexUtils.GetFullTypeName(propertiesArgType)} on {IndexUtils.GetFullTypeName(grainInterfaceType)} grain interface," +
                                                    $" while previous indexes have been configured to be updated {(isEager ? "lazily" : "eagerly")}." +
                                                    $" You must fix this by configuring all indexes to be updated lazily or eagerly." +
                                                    $" Note: If you have at least one Total Index among your indexes, this must be lazy, and thus all other indexes must be lazy also.");
            }
            if (!VerifyNullValue(propInfo, isUnique, out string convertErrMsg))
            {
                throw new InvalidOperationException($"The index of type {IndexUtils.GetFullTypeName(indexType)} on {IndexUtils.GetFullTypeName(grainClassType)} grain implementation class" +
                                                    $" failed verification. " + convertErrMsg);
            }
        }

        internal static bool VerifyNullValue(PropertyInfo propInfo, bool isUnique, out string errorMessage)
        {
            errorMessage = string.Empty;
            var indexAttrs = propInfo.GetCustomAttributes<IndexAttribute>(inherit: false).Where(attr => !string.IsNullOrEmpty(attr.NullValue)).ToArray();

            if (propInfo.PropertyType.IsNullable())
            {
                if (indexAttrs.Length > 0)
                {
                    errorMessage = $"Cannot specify a NullValue attribute for nullable property {propInfo.Name}";
                    return false;
                }
                return true;
            }

            if (indexAttrs.Length == 0)
            {
                errorMessage = $"Must specify a NullValue attribute for non-nullable property {propInfo.Name}";
                return false;
            }

            string firstValue = null;
            foreach (var attr in indexAttrs)
            {
                if (firstValue == null)
                {
                    firstValue = attr.NullValue;
                }
                else if (firstValue != attr.NullValue)
                {
                    errorMessage = $"Inconsistent NullValues attribute for property {propInfo.Name}: {firstValue} and {attr.NullValue}";
                    return false;
                }

                try
                {
                    attr.NullValue.ConvertTo(propInfo.PropertyType);
                } catch (Exception ex)
                {
                    errorMessage = $"Error parsing NullValue attribute for property {propInfo.Name}: {attr.NullValue}; {ex.Message}";
                    return false;
                }
            }
            return true;
        }

        public static bool IsSubclassOfRawGenericType(Type genericType, Type typeToCheck)
        {
            // Used to check for IndexableGrain<,> inheritance; IndexableGrain is a fault-tolerant subclass of IndexableGrainNonFaultTolerant,
            // so we will only see it if the grain's index type is fault tolerant.
            for (; typeToCheck != null && typeToCheck != typeof(object); typeToCheck = typeToCheck.BaseType)
            {
                if (genericType == (typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
