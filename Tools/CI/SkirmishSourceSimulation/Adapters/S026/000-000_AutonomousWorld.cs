using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Collections;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class GeneratedInitializationBlocked : NotSupportedException
    {
        internal readonly string Owner;
        internal GeneratedInitializationBlocked(string owner, string details) : base(details) { Owner = owner; }
    }

    // One world/registry for source construction and all later original owners.
    // The initial unsupported owner is explicit; it is never represented as an empty world.
    internal sealed partial class AutonomousWorld : IDisposable
    {
        internal readonly EntityManager Entities = new EntityManager();
        internal readonly Dictionary<int, RuntimeBuildingEntity> Buildings = new Dictionary<int, RuntimeBuildingEntity>();
        internal Entity GridEntity { get; private set; }
        internal Entity SurfaceEntity { get; private set; }
        internal Entity Session { get; private set; }
        internal Entity Boundary { get; private set; }
        internal SkirmishScenarioDefinitionConfig ActualDefinition { get; private set; }
        internal SkirmishResolvedSetup Setup { get; private set; }
        internal SkirmishBattleCatalogConfig ActualCanonicalCatalog { get; private set; }
        internal System.Collections.Generic.IReadOnlyList<SkirmishSetupMatrixRow> ActualCompileMatrix { get; private set; }
        internal SkirmishContentManifest ActualCompileManifest { get; private set; }
        internal SkirmishArmyProfileConfig Army { get; private set; }
        internal int ActualDefinitionScenarioIndex { get; private set; }
        internal string WorldId { get; } = "generated-" + Guid.NewGuid().ToString("N");
        internal int Tick { get; private set; }
        internal double SimulationSeconds { get; private set; }
        internal bool Initialized { get; private set; }
        internal string InitializationStage { get; private set; } = "Uninitialized";
        internal float FixedDeltaTime { get; private set; }
        private int nextIdentity = 1;
        private bool initializationAttempted;
        private bool disposed;
        private string inputHash;

        internal static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        internal static byte[] PinnedFile(string path, string expected, long maximum)
        {
            if (!Path.IsPathRooted(path)) throw new ArgumentException("Absolute pinned source path required");
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 1 || info.Length > maximum || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new NotSupportedException("Bounded regular source input required");
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.LongLength != info.Length || Hash(bytes) != expected) throw new InvalidOperationException("Source input hash changed: " + path);
            return bytes;
        }
        private static JObject Parse(byte[] bytes)
        {
            return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }
        internal static byte[] Descriptor(JToken token, long maximum)
        {
            var value = token as JObject ?? throw new ArgumentException("Pinned source descriptor required");
            return PinnedFile((string)value["path"], (string)value["sha256"], maximum);
        }
        internal SourceMetadataCompatibility MetadataCompatibility { get; private set; }
        internal static (JObject graph,SourceConfigMaterializer assets,SkirmishBattleCatalogConfig canonicalCatalog,SkirmishScenarioDefinitionConfig definition,SkirmishExpansionCatalogConfig catalog,List<SkirmishSetupMatrixRow> matrix,SkirmishContentManifest manifest,SkirmishResolvedSetup setup,bool sourceOnlyS026,JObject selection) ReadCompiledInitialization(JObject request)
        {
            SourceAdmittedProfiles.RequireInitialization(request);
            var graph = Parse(Descriptor(request["source_config_graph"], 16 * 1024 * 1024));
            foreach (var pin in ((JObject)graph["source_pins"]).Properties()) PinnedFile(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
            var assets = new SourceConfigMaterializer(graph);
            var canonicalDescriptor = request["canonical_battle_catalog"] as JObject ?? throw new ArgumentException("Explicit pinned canonical battle catalog required");
            if (canonicalDescriptor.Properties().Count() != 2 || canonicalDescriptor["path"] == null || canonicalDescriptor["sha256"] == null)
                throw new ArgumentException("Exact canonical catalog path/sha descriptor required");
            Descriptor(canonicalDescriptor, 4 * 1024 * 1024);
            string canonicalPath = (string)canonicalDescriptor["path"], canonicalHash = (string)canonicalDescriptor["sha256"];
            if ((string)graph["source_pins"]?[canonicalPath] != canonicalHash)
                throw new InvalidOperationException("Canonical catalog descriptor is not in actual source graph pins");
            var canonicalNodes = ((JArray)graph["nodes"]).OfType<JObject>().Where(n =>
                (string)n["source_type"] == nameof(SkirmishBattleCatalogConfig) &&
                (string)n["source_sha256"] == canonicalHash &&
                canonicalPath.Replace('\\', '/').EndsWith("/" + (string)n["source_path"], StringComparison.Ordinal)).ToArray();
            if (canonicalNodes.Length != 1) throw new InvalidOperationException("Unique actual canonical catalog graph node required");
            var canonicalCatalog = assets.ByPath<SkirmishBattleCatalogConfig>((string)canonicalNodes[0]["source_path"]);
            SkirmishScenarioDefinitionConfig definition;JObject nativeLogicalBinding=null;
            bool declaredContext=request["metadata_compiler_context"]!=null;
            bool sourceOnlyS026 = (string)request["mission_id"] == "S026"||declaredContext;
            if(declaredContext){definition=SourceDeclaredMissionConfiguration.Configure(request,assets);SourceDeclaredMissionConfiguration.RequireHonestSourceOnlyCatalog(canonicalCatalog,definition.CatalogId);}
            else if (sourceOnlyS026) {
                var typed = request["typed_mission_configuration"] as JObject;
                string[] fields = { "profiles", "campaign_source_path", "layout" };
                if (typed == null || typed.Properties().Count() != fields.Length || typed.Properties().Any(p => !fields.Contains(p.Name)) ||
                    (string)typed["campaign_source_path"] != SourceS026TypedConfiguration.SourcePath)
                    throw new NotSupportedException("Exact S026 typed configuration descriptor required");
                var logical = assets.LogicalLayout(Parse(Descriptor(typed["layout"], 1024 * 1024)));
                SourceS026TypedConfiguration.RequireFrozenMeasuredLayout(logical);
                definition = SourceS026TypedConfiguration.Configure(assets, typed["profiles"] as JObject, logical,
                    assets.ByPath<OperationMapDefinition>(SourceS026TypedConfiguration.SourcePath));
                nativeLogicalBinding=SourceNativeMissionLogicalBindingReference.Bind(request["mission_logical_binding"] as JObject,definition);
                SourceS026TypedConfiguration.RequireHonestSourceOnlyCatalog(canonicalCatalog);
            } else {
                if (request["typed_mission_configuration"] != null || request["mission_logical_binding"] != null) throw new NotSupportedException("Typed S026 configuration cannot override another mission");
                definition = assets.ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
                if (definition.CatalogId != "S002" || definition.ObjectiveConfig.Kind != SkirmishObjectiveKind.BaseAssault || (string)request["mission_id"] != definition.CatalogId)
                    throw new NotSupportedException("Actual authored S002 BaseAssault required");
            }
            var catalog = assets.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");
            var matrixBytes = Descriptor(request["setup_matrix"], 4 * 1024 * 1024);
            UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>
            {
                [SkirmishBattleCatalogConfig.ResourceName] = canonicalCatalog,
                [SkirmishExpansionCatalogConfig.ResourceName] = catalog,
                [SkirmishSetupMatrixTable.PackagedResourceName] = new UnityEngine.TextAsset(System.Text.Encoding.UTF8.GetString(matrixBytes))
            });
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error)) throw new InvalidOperationException(error);
            var selection = (JObject)request["selection"];
            // Same source launch manifest as SkirmishLaunchProjection; actual owner admission is separate.
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(definition, (SkirmishDifficultyId)(int)selection["difficulty_id"],
                (SkirmishSizeId)(int)selection["size_id"], (int)selection["seed"], manifest, matrix, out var setup, out var reasons))
                throw new InvalidOperationException("Original setup compiler rejected authored input: " + string.Join(";", reasons));
            if(declaredContext)SourceDeclaredMissionLogicalBindingReference.RequireCompiled(request["mission_logical_binding"]as JObject,definition,setup,request);else if(nativeLogicalBinding!=null)SourceNativeMissionLogicalBindingReference.RequireCompiled(nativeLogicalBinding,definition,setup);
            return (graph,assets,canonicalCatalog,definition,catalog,matrix,manifest,setup,sourceOnlyS026,selection);
        }
        internal void Initialize(string path, string sha256, float fixedDeltaTime, string metadataSourceRepo=null, string metadataDataRepo=null)
        {
            if (disposed || initializationAttempted) throw new InvalidOperationException("Initialize a fresh generated world exactly once");
            initializationAttempted = true;
            if (!(fixedDeltaTime > 0) || fixedDeltaTime > 1 || float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime))
                throw new ArgumentException("Finite user schedule delta in (0,1] required");
            RequireInitialRoadsBeforeWrites();
            sourceInitializationBytes=PinnedFile(path, sha256, 4 * 1024 * 1024);
            var request = Parse(sourceInitializationBytes);
            if ((int)request["schema_version"] != 1 || (string)request["kind"] != "GeneratedMissionInitialization" ||
                (string)request["authority"] != "AuthoredConfigAndBakedCampaignMap")
                throw new NotSupportedException("Explicit authored generated initialization profile required");
            inputHash = sha256;
            FixedDeltaTime = fixedDeltaTime;
            InitializationStage = "ReadingAuthoredConfig";
            var compiled=ReadCompiledInitialization(request);
            var graph=compiled.graph;
            var assets=compiled.assets;
            var canonicalCatalog=compiled.canonicalCatalog;
            var definition=compiled.definition;
            var catalog=compiled.catalog;
            var matrix=compiled.matrix;
            var manifest=compiled.manifest;
            var setup=compiled.setup;
            var sourceOnlyS026=compiled.sourceOnlyS026;
            var selection=compiled.selection;
            Setup = setup;
            ActualDefinition = definition;
            ActualCanonicalCatalog = canonicalCatalog;
            ActualCompileMatrix = matrix;
            ActualCompileManifest = manifest;
            Army = definition.ArmyProfileConfig;
            int actualScenarioIndex;
            if (sourceOnlyS026) actualScenarioIndex = request["metadata_compiler_context"]!=null?SourceDeclaredMissionConfiguration.RequireHonestSourceOnlyCatalog(canonicalCatalog,Setup.CatalogId):SourceS026TypedConfiguration.RequireHonestSourceOnlyCatalog(canonicalCatalog);
            else if (!SkirmishMissionDispatch.TryGetLegacyIndex(definition.CatalogId, out actualScenarioIndex)) throw new InvalidOperationException("Source catalog dispatch index missing");
            ActualDefinitionScenarioIndex = actualScenarioIndex;
            MetadataCompatibility=SourceMetadataCompatibility.Load(request,Setup,metadataSourceRepo,metadataDataRepo);
            RetainSelectedOperationMapInputs(request, graph, assets);
            freshStorageAuthority=FreshSourceStorageLifetimeAuthority.FromValidatedSource(request,inputHash,sourceInitializationBytes,ActualDefinition,Setup,ActualCompileManifest,ActualCompileMatrix);
            if(request["placement_domain"]?["s026_inactive_diagnostic"]!=null||(Setup.CatalogId=="S049"&&(string)SourceConfigurationQualification.Context(request)["kind"]=="OriginalS049DirectNativeFactoryContext"))Game.Configs.SourceEnglishCommandText.BindS026Diagnostic(Descriptor(request["command_text_data"],4*1024*1024),metadataDataRepo);else Game.Configs.SourceEnglishCommandText.Bind(Descriptor(request["command_text_data"],4*1024*1024));
            var deathInputs=request["placement_domain"]?["s026_inactive_diagnostic"]==null?SourceNativeDeathReadModel.LoadAtBoundary(Descriptor(request["death_mechanical_inputs"],1024*1024)):SourceNativeDeathReadModel.LoadS026Diagnostic(request["death_mechanical_inputs"] as JObject);
            SourceNativeDeathBinding.BindAtBoundary(Entities,deathInputs);
            sourceQualifiedPrefabKeys=deathInputs.QualifiedPrefabKeys;
            InitializationStage = "ConstructingBakedCampaignMap";
            Entities.LogicalAllocatorAdmitted = true;
            Entities.AllocateLogicalIdentity = () => new Entity { Index = checked(nextIdentity++), Version = 1 };
            SourceDeclaredRuntimeInputs.ResolveMapAssets(request,graph,assets,ActualDefinition,out var surfaceData,out var gridData);
            if(!surfaceData.TryCreateRuntimeBlobAsset(Allocator.Persistent,out var surfaceBlob))throw new InvalidOperationException("Original baked surface decoder rejected actual source payload");
            ref MapSurfaceBlob surface=ref surfaceBlob.Value;
            if(surface.Dimensions.x!=gridData.Width||surface.Dimensions.y!=gridData.Height||surface.CellSize!=gridData.CellSize)throw new InvalidOperationException("Actual baked grid/surface source domains differ");
            GeneratedMapSurfaceFlags.GetSurfaceFeatureFlags(ref surface,out byte layered,out byte roads,out byte bridges);
            SurfaceEntity=Entities.CreateEntity();
            Entities.AddComponentData(SurfaceEntity,new MapSurfaceComponent{SurfaceBlob=surfaceBlob,GridOrigin=surface.GridOrigin,CellSize=surface.CellSize,Dimensions=surface.Dimensions,HasSurfaceData=1,HasLayeredCells=layered,HasRoadSurfaces=roads,HasBridgeSurfaces=bridges});
            Entities.AddComponentData(SurfaceEntity,new MapSurfacePathCostComponent{EnableSlopeCost=0,GentleSlopeTraversalCost=0,SteepSlopeTraversalCost=0});
            GridEntity=Entities.CreateEntity();GeneratedGridSourceBaking.Bake(Entities,GridEntity,gridData);
            InitializationStage="ConstructingAuthoredPlacementMasks";
            ConstructSourceStartingWorld(request);
        }
        private void RequireReady()
        {
            if (disposed || !Initialized) throw new InvalidOperationException("Generated source startup is incomplete at " + InitializationStage);
        }
        internal void AdvanceOneTick()
        {
            RequireReady();
            throw new GeneratedInitializationBlocked("GeneratedSourceScheduler", "Original retained owner phase composition has not yet completed startup admission");
        }
        internal JObject Dispatch(string requestId, string controlId, long expectedTick)
        {
            RequireReady();
            throw new GeneratedInitializationBlocked("SourceCommandComposition", "Source UI/production/command composition is not yet bound; no default acceptance or rejection");
        }
        internal JObject Observe()
        {
            return new JObject
            {
                ["world_id"] = WorldId, ["tick"] = Tick, ["simulation_seconds"] = SimulationSeconds,
                ["initialization_stage"] = InitializationStage,
                ["terminal"] = null,
                ["consumer_admission"] = new JObject { ["mode"] = "generated", ["status"] = Initialized ? "initialized" : "blocked", ["initialization_sha256"] = inputHash, ["future_native_inputs"] = false },
                ["constructed_map"] = GridEntity==Entity.Null?null:new JObject{["width"]=Entities.GetComponentData<GridConfig>(GridEntity).Width,["height"]=Entities.GetComponentData<GridConfig>(GridEntity).Height,["surface_samples"]=MapSurfaceBlobAccess.SurfaceCount(ref Entities.GetComponentData<MapSurfaceComponent>(SurfaceEntity).SurfaceBlob.Value)},
                ["computed_setup"] = Setup == null ? null : new JObject { ["catalog_id"] = Setup.CatalogId, ["setup_hash"] = Setup.SetupHash, ["force_actors"] = Setup.Forces.Sum(force => force.Quantity), ["structure_bindings"] = Setup.Structures.Length }
            };
        }
        public void Dispose() { if(disposed)return;Exception first=null;try{ground?.Dispose();}catch(Exception error){first=error;}finally{try{DisposeActualOperationMapPublication();}catch(Exception error){Unity.Jobs.OwnedJobBackend.PreserveCleanupFailure(ref first,error);}finally{try{initialRoads?.Dispose();}catch(Exception error){Unity.Jobs.OwnedJobBackend.PreserveCleanupFailure(ref first,error);}finally{disposed=true;}}}if(first!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw(); }
    }
}
