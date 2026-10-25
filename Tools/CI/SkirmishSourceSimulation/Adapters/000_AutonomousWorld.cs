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
        private static byte[] Descriptor(JToken token, long maximum)
        {
            var value = token as JObject ?? throw new ArgumentException("Pinned source descriptor required");
            return PinnedFile((string)value["path"], (string)value["sha256"], maximum);
        }
        internal void Initialize(string path, string sha256, float fixedDeltaTime)
        {
            if (disposed || initializationAttempted) throw new InvalidOperationException("Initialize a fresh generated world exactly once");
            initializationAttempted = true;
            if (!(fixedDeltaTime > 0) || fixedDeltaTime > 1 || float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime))
                throw new ArgumentException("Finite user schedule delta in (0,1] required");
            RequireInitialRoadsBeforeWrites();
            var request = Parse(PinnedFile(path, sha256, 4 * 1024 * 1024));
            if ((int)request["schema_version"] != 1 || (string)request["kind"] != "GeneratedMissionInitialization" ||
                (string)request["authority"] != "AuthoredConfigAndBakedCampaignMap")
                throw new NotSupportedException("Explicit authored generated initialization profile required");
            inputHash = sha256;
            FixedDeltaTime = fixedDeltaTime;
            InitializationStage = "ReadingAuthoredConfig";
            var graph = Parse(Descriptor(request["source_config_graph"], 16 * 1024 * 1024));
            foreach (var pin in ((JObject)graph["source_pins"]).Properties()) PinnedFile(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
            var assets = new SourceConfigMaterializer(graph);
            var definition = assets.ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
            if (definition.CatalogId != "S002" || definition.ObjectiveConfig.Kind != SkirmishObjectiveKind.BaseAssault || (string)request["mission_id"] != definition.CatalogId)
                throw new NotSupportedException("First generated profile is the actual authored S002 BaseAssault");
            var catalog = assets.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");
            var matrixBytes = Descriptor(request["setup_matrix"], 4 * 1024 * 1024);
            UnityEngine.Resources.BindSourceResources(new Dictionary<string, object>
            {
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
            Setup = setup;
            ActualDefinition = definition;
            Army = definition.ArmyProfileConfig;
            if(!SkirmishMissionDispatch.TryGetLegacyIndex(definition.CatalogId,out int actualScenarioIndex))throw new InvalidOperationException("Source catalog dispatch index missing");
            ActualDefinitionScenarioIndex = actualScenarioIndex;
            InitializationStage = "ConstructingBakedCampaignMap";
            Entities.LogicalAllocatorAdmitted = true;
            Entities.AllocateLogicalIdentity = () => new Entity { Index = checked(nextIdentity++), Version = 1 };
            var surfaceData=assets.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
            var gridData=assets.ByPath<GridAuthoringSceneConfigAsset>("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
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
        public void Dispose() { initialRoads?.Dispose(); disposed = true; }
    }
}
