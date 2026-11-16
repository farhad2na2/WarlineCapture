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
        internal readonly EntityManager Entities;
        internal readonly Dictionary<int, RuntimeBuildingEntity> Buildings;
        internal Entity GridEntity { get; private set; }
        internal Entity SurfaceEntity { get; private set; }
        internal Entity Session { get; private set; }
        internal Entity Boundary { get; private set; }
        internal SkirmishScenarioDefinitionConfig ActualDefinition { get; private set; }
        internal SkirmishResolvedSetup Setup { get; private set; }
        internal SkirmishBattleCatalogConfig ActualCanonicalCatalog { get; private set; }
        internal SkirmishExpansionCatalogConfig ActualExpansionCatalog { get; private set; }
        internal System.Collections.Generic.IReadOnlyList<SkirmishSetupMatrixRow> ActualCompileMatrix { get; private set; }
        internal SkirmishContentManifest ActualCompileManifest { get; private set; }
        internal SkirmishArmyProfileConfig Army { get; private set; }
        internal int ActualDefinitionScenarioIndex { get; private set; }
        internal string WorldId { get; } = "generated-" + Guid.NewGuid().ToString("N");
        internal int Tick { get; private set; }
        internal double SimulationSeconds { get; private set; }
        internal bool Initialized { get; private set; }
        internal string InitializationStage { get; private set; } = "Uninitialized";
        internal JObject HistoricalPlacementObservation { get; private set; }
        internal JObject HistoricalOpeningObservation { get; private set; }
        internal string SourceRepositoryRoot { get; private set; }
        internal float FixedDeltaTime { get; private set; }
        private int nextIdentity = 1;
        private bool initializationAttempted;
        private bool disposed;
        private string inputHash;
        private Newtonsoft.Json.Linq.JObject s002HistoricalInputDiagnostic;
        internal bool ReusedS002PreflightSetup { get; private set; }
#if S002_CURRENT_PACKAGE_DIAGNOSTIC
        private sealed class S002PreflightSetupContext
        {
            internal string InitializationSha256, PackageSha256, PackagePath, SourceRoot, GraphSha256, MatrixSha256, CanonicalSha256;
            internal JObject Graph;
            internal SourceConfigMaterializer Assets;
            internal SkirmishScenarioDefinitionConfig Definition;
            internal SkirmishBattleCatalogConfig CanonicalCatalog;
            internal SkirmishExpansionCatalogConfig ExpansionCatalog;
            internal SkirmishResolvedSetup Setup;
            internal List<SkirmishSetupMatrixRow> Matrix;
            internal SkirmishContentManifest Manifest;
        }
        private static S002PreflightSetupContext pendingS002PreflightSetup;

        private static S002PreflightSetupContext ConsumeS002PreflightSetup(JObject request, string initializationSha256, string packageSha256)
        {
            var context = pendingS002PreflightSetup ?? throw new InvalidOperationException("Same-process S002 preflight setup required before Initialize");
            string graphSha = (string)request["source_config_graph"]?["sha256"];
            string matrixSha = (string)request["setup_matrix"]?["sha256"];
            string canonicalSha = (string)request["canonical_battle_catalog"]?["sha256"];
            if (context.InitializationSha256 != initializationSha256 || context.PackageSha256 != packageSha256 ||
                context.GraphSha256 != graphSha || context.MatrixSha256 != matrixSha || context.CanonicalSha256 != canonicalSha)
                throw new InvalidOperationException("Same exact initialization/package/graph/matrix/catalog preflight required");

            // Recheck every file after preflight, then consume the actual objects once.
            if (Path.GetFullPath(context.SourceRoot) != SourceSimulationProgram.CurrentS002TrustedRoot ||
                (string)request["s002_current_source_diagnostic"]?["package"]?["path"] != context.PackagePath ||
                (string)request["s002_current_source_diagnostic"]?["package"]?["sha256"] != context.PackageSha256)
                throw new InvalidOperationException("Exact diagnostic package binding changed after preflight");
            SourceSimulationProgram.RequireCurrentPackageFile(context.PackagePath, context.PackageSha256, context.SourceRoot);
            var graphBytes = Descriptor(request["source_config_graph"], 16 * 1024 * 1024);
            if (Hash(graphBytes) != context.GraphSha256) throw new InvalidOperationException("Preflight source graph changed before Initialize");
            foreach (var pin in ((JObject)context.Graph["source_pins"]).Properties()) PinnedFile(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
            Descriptor(request["setup_matrix"], 4 * 1024 * 1024);
            Descriptor(request["canonical_battle_catalog"], 4 * 1024 * 1024);
            if (pendingS002PreflightSetup != context) throw new InvalidOperationException("S002 preflight setup was already consumed");
            pendingS002PreflightSetup = null;
            return context;
        }

        internal static bool RejectsS002PreflightSetupConsumption(JObject request, string initializationSha256, string packageSha256)
        {
            try { ConsumeS002PreflightSetup(request, initializationSha256, packageSha256); return false; }
            catch (InvalidOperationException) { return true; }
        }

#endif
        internal AutonomousWorld() : this(new EntityManager(), new Dictionary<int, RuntimeBuildingEntity>()) { }

        // Diagnostic startup hosts may attach only to the already populated same
        // EntityManager and building registry. They never create a second world.
        internal AutonomousWorld(EntityManager entities, Dictionary<int, RuntimeBuildingEntity> buildings)
        {
            Entities = entities ?? throw new ArgumentNullException(nameof(entities));
            Buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
        }

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
        internal static SkirmishResolvedSetup CompileOriginalSetup(JObject request,
            SkirmishScenarioDefinitionConfig definition, SkirmishBattleCatalogConfig canonicalCatalog,
            SkirmishExpansionCatalogConfig expansionCatalog,
            out List<SkirmishSetupMatrixRow> matrix, out SkirmishContentManifest manifest,
            IReadOnlyDictionary<string, object> additionalSourceResources = null)
        {
            if (request == null || definition == null || canonicalCatalog == null || expansionCatalog == null)
                throw new ArgumentNullException("Actual authored setup inputs required");
            var matrixBytes = Descriptor(request["setup_matrix"], 4 * 1024 * 1024);
            var sourceResources = new Dictionary<string, object>
            {
                [SkirmishBattleCatalogConfig.ResourceName] = canonicalCatalog,
                [SkirmishExpansionCatalogConfig.ResourceName] = expansionCatalog,
                [SkirmishSetupMatrixTable.PackagedResourceName] = new UnityEngine.TextAsset(System.Text.Encoding.UTF8.GetString(matrixBytes))
            };
            if (additionalSourceResources != null)
                foreach (var pair in additionalSourceResources)
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null || !sourceResources.TryAdd(pair.Key, pair.Value))
                        throw new InvalidDataException("Unique non-null additional source resource required");
            UnityEngine.Resources.BindSourceResources(sourceResources);
            matrix = SourceExternalSetupMatrix.ReadVerified(matrixBytes, (string)request["setup_matrix"]["sha256"]);
            var selection = request["selection"] as JObject ?? throw new InvalidDataException("Actual compiled setup selection required");
            SourceExternalSetupMatrix.Select(matrix, definition.CatalogId, (SkirmishSizeId)(int)selection["size_id"]);
            manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
            using var layoutReplay = request.TryGetValue("layout_json_replay", out var replayDescriptor)
                ? SourceValidatedLayoutJson.OpenDescriptor(definition.MapLayout, replayDescriptor) : null;
            if (!SkirmishSetupCompiler.TryCompile(definition, (SkirmishDifficultyId)(int)selection["difficulty_id"],
                (SkirmishSizeId)(int)selection["size_id"], (int)selection["seed"], manifest, matrix,
                out var setup, out var reasons))
                throw new InvalidOperationException("Original setup compiler rejected authored input: " + string.Join(";", reasons));
            return setup;
        }

#if S002_CURRENT_PACKAGE_DIAGNOSTIC
        internal static void PreflightCurrentS002SelectedMapInputs(JObject request, string sourceRoot, string initializationSha256, string packageSha256)
        {
            if (pendingS002PreflightSetup != null) throw new InvalidOperationException("S002 preflight setup context is already pending");
            if (request == null || (string)request["mission_id"] != "S002")
                throw new InvalidDataException("Exact S002 package preflight input required");
            var graphDescriptor = request["source_config_graph"] as JObject ?? throw new InvalidDataException("Actual S002 source graph required");
            var graphBytes = Descriptor(graphDescriptor, 16 * 1024 * 1024);
            S002CurrentSelectedMapGraphBuildAttestation.Require((string)graphDescriptor["sha256"], () => graphBytes);
            var graph = Parse(graphBytes);
            foreach (var pin in ((JObject)graph["source_pins"]).Properties()) PinnedFile(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
            var assets = new SourceConfigMaterializer(graph);
            if ((bool?)graph["compiled_setup"] != false || (bool?)graph["world_initialized"] != false ||
                (bool?)graph["native_world_state_used"] != false ||
                (string)graph["definition_path"] != "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset")
                throw new InvalidDataException("Current authored S002 graph must remain uncompiled source data");
            var definition = assets.ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
            if (definition.CatalogId != "S002" || definition.ObjectiveConfig.Kind != SkirmishObjectiveKind.BaseAssault || definition.CampaignMap != null)
                throw new InvalidDataException("Actual authored S002 BaseAssault definition required");
            var canonicalDescriptor = request["canonical_battle_catalog"] as JObject ?? throw new InvalidDataException("Actual pinned canonical battle catalog required");
            if (canonicalDescriptor.Properties().Count() != 2 || canonicalDescriptor["path"] == null || canonicalDescriptor["sha256"] == null)
                throw new InvalidDataException("Exact canonical catalog path/sha descriptor required");
            Descriptor(canonicalDescriptor, 4 * 1024 * 1024);
            string canonicalPath = (string)canonicalDescriptor["path"], canonicalHash = (string)canonicalDescriptor["sha256"];
            if ((string)graph["source_pins"]?[canonicalPath] != canonicalHash)
                throw new InvalidDataException("Canonical catalog descriptor is not in actual graph source pins");
            var canonicalNodes = ((JArray)graph["nodes"]).OfType<JObject>().Where(n =>
                (string)n["source_type"] == nameof(SkirmishBattleCatalogConfig) &&
                (string)n["source_sha256"] == canonicalHash &&
                canonicalPath.Replace('\\', '/').EndsWith("/" + (string)n["source_path"], StringComparison.Ordinal)).ToArray();
            if (canonicalNodes.Length != 1) throw new InvalidDataException("Unique actual canonical catalog graph node required");
            var canonicalCatalog = assets.ByPath<SkirmishBattleCatalogConfig>((string)canonicalNodes[0]["source_path"]);
            var expansion = assets.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");
            var setup = CompileOriginalSetup(request, definition, canonicalCatalog, expansion, out var matrix, out var manifest);
            PreflightSelectedOperationMapInputs(request, graph, assets, definition, setup, sourceRoot);
            string packagePath = (string)request["s002_current_source_diagnostic"]?["package"]?["path"];
            string requestPackageSha = (string)request["s002_current_source_diagnostic"]?["package"]?["sha256"];
            if (string.IsNullOrEmpty(packagePath) || requestPackageSha != packageSha256 || !Path.IsPathRooted(packagePath) || Path.GetFullPath(packagePath) != packagePath)
                throw new InvalidDataException("Exact validated current diagnostic package binding required for preflight context");
            SourceSimulationProgram.RequireCurrentPackageFile(packagePath, packageSha256, sourceRoot);
            pendingS002PreflightSetup = new S002PreflightSetupContext
            {
                InitializationSha256 = initializationSha256,
                PackageSha256 = packageSha256,
                PackagePath = packagePath,
                SourceRoot = Path.GetFullPath(sourceRoot),
                GraphSha256 = (string)graphDescriptor["sha256"],
                MatrixSha256 = (string)request["setup_matrix"]["sha256"],
                CanonicalSha256 = canonicalHash,
                Graph = graph,
                Assets = assets,
                Definition = definition,
                CanonicalCatalog = canonicalCatalog,
                ExpansionCatalog = expansion,
                Setup = setup,
                Matrix = matrix,
                Manifest = manifest
            };
        }
#endif
        internal SourceMetadataCompatibility MetadataCompatibility { get; private set; }
        internal void Initialize(string path, string sha256, float fixedDeltaTime, string metadataSourceRepo=null, string metadataDataRepo=null, Newtonsoft.Json.Linq.JObject historicalInputDiagnostic=null, string diagnosticPackageSha256=null)
        {
            if (disposed || initializationAttempted) throw new InvalidOperationException("Initialize a fresh generated world exactly once");
            initializationAttempted = true;
            if (!(fixedDeltaTime > 0) || fixedDeltaTime > 1 || float.IsNaN(fixedDeltaTime) || float.IsInfinity(fixedDeltaTime))
                throw new ArgumentException("Finite user schedule delta in (0,1] required");
            RequireInitialRoadsBeforeWrites();
            sourceInitializationBytes=PinnedFile(path, sha256, 4 * 1024 * 1024);
            var request = Parse(sourceInitializationBytes);
            s002HistoricalInputDiagnostic=historicalInputDiagnostic==null?null:(Newtonsoft.Json.Linq.JObject)historicalInputDiagnostic.DeepClone();
            if ((int)request["schema_version"] != 1 || (string)request["kind"] != "GeneratedMissionInitialization" ||
                (string)request["authority"] != "AuthoredConfigAndBakedCampaignMap")
                throw new NotSupportedException("Explicit authored generated initialization profile required");
            inputHash = sha256;
            SourceRepositoryRoot = metadataSourceRepo;
            FixedDeltaTime = fixedDeltaTime;
            InitializationStage = "ReadingAuthoredConfig";
            bool sourceOnlyS026 = (string)request["mission_id"] == "S026";
            JObject graph;
            SourceConfigMaterializer assets;
            SkirmishBattleCatalogConfig canonicalCatalog;
            SkirmishScenarioDefinitionConfig definition;
            JObject nativeLogicalBinding = null;
            SkirmishExpansionCatalogConfig catalog;
            SkirmishResolvedSetup setup;
            List<SkirmishSetupMatrixRow> matrix;
            SkirmishContentManifest manifest;
#if S002_CURRENT_PACKAGE_DIAGNOSTIC
            if (!sourceOnlyS026 && request["s002_current_source_diagnostic"] != null)
            {
                var context = ConsumeS002PreflightSetup(request, sha256, diagnosticPackageSha256);
                graph = context.Graph;
                assets = context.Assets;
                canonicalCatalog = context.CanonicalCatalog;
                definition = context.Definition;
                catalog = context.ExpansionCatalog;
                setup = context.Setup;
                matrix = context.Matrix;
                manifest = context.Manifest;
                ReusedS002PreflightSetup = true;
            }
            else
#endif
            {
                graph = Parse(Descriptor(request["source_config_graph"], 16 * 1024 * 1024));
                foreach (var pin in ((JObject)graph["source_pins"]).Properties()) PinnedFile(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
                assets = new SourceConfigMaterializer(graph);
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
                canonicalCatalog = assets.ByPath<SkirmishBattleCatalogConfig>((string)canonicalNodes[0]["source_path"]);
                if (sourceOnlyS026) {
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
                    bool authoredS002 = definition.CatalogId == "S002" && definition.ObjectiveConfig.Kind == SkirmishObjectiveKind.BaseAssault && (string)request["mission_id"] == definition.CatalogId;
#if WARLINE_EXPANDED_SOURCE_DIAGNOSTIC
                    var diagnostic = request["source_expanded_diagnostic"] as JObject;
                    bool authoredDiagnostic = diagnostic != null && diagnostic.Count == 4 &&
                        (string)diagnostic["kind"] == "ExpandedAuthoredSourceDiagnostic" &&
                        diagnostic["source_only"]?.Type == JTokenType.Boolean && (bool)diagnostic["source_only"] &&
                        diagnostic["native_parity"]?.Type == JTokenType.Boolean && !(bool)diagnostic["native_parity"] &&
                        diagnostic["mission_ready"]?.Type == JTokenType.Boolean && !(bool)diagnostic["mission_ready"] &&
                        request["layout_json_replay"] is JObject && request["source_resource_bindings"] is JObject &&
                        SkirmishMissionDispatch.Canonical(definition.CatalogId) && (string)request["mission_id"] == definition.CatalogId &&
                        canonicalCatalog.TryGet(definition.CatalogId, out var diagnosticRow) &&
                        diagnosticRow.DefinitionId == definition.DefinitionId && diagnosticRow.ContentVersion == definition.ContentVersion;
                    if (!authoredS002 && !authoredDiagnostic)
#else
                    if (!authoredS002)
#endif
                        throw new NotSupportedException("Actual authored S002 BaseAssault required");
                }
                catalog = assets.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");
                IReadOnlyDictionary<string, object> additionalResources = null;
                if (request.TryGetValue("source_resource_bindings", out var resourceDescriptor))
                {
                    additionalResources = SourceExpandedAuthoredSetupInput.LoadOperationMapResources(assets, definition, resourceDescriptor);
                    InitializationStage = "ResolvedAuthoredSourceResources";
                }
                setup = CompileOriginalSetup(request, definition, canonicalCatalog, catalog,
                    out matrix, out manifest, additionalResources);
                if (additionalResources != null) InitializationStage = "CompiledAuthoredSourceWithResources";
            }
            if(nativeLogicalBinding!=null)SourceNativeMissionLogicalBindingReference.RequireCompiled(nativeLogicalBinding,definition,setup);
            Setup = setup;
            ActualDefinition = definition;
            ActualCanonicalCatalog = canonicalCatalog;
            ActualExpansionCatalog = catalog;
            ActualCompileMatrix = matrix;
            ActualCompileManifest = manifest;
            Army = definition.ArmyProfileConfig;
            int actualScenarioIndex;
            if (sourceOnlyS026) actualScenarioIndex = SourceS026TypedConfiguration.RequireHonestSourceOnlyCatalog(canonicalCatalog);
            else if (!SkirmishMissionDispatch.TryGetLegacyIndex(definition.CatalogId, out actualScenarioIndex)) throw new InvalidOperationException("Source catalog dispatch index missing");
            ActualDefinitionScenarioIndex = actualScenarioIndex;
#if WARLINE_EXPANDED_SOURCE_DIAGNOSTIC
            GridAuthoringSceneConfigAsset expandedGrid = null;
            MapSurfaceDataAsset expandedSurface = null;
            OperationMapDefinition expandedMap = null;
            if (Setup.CatalogId == "S004" && request["source_expanded_diagnostic"] is JObject)
            {
                if (Session != Entity.Null || Entities.BankBound)
                    throw new InvalidOperationException("Fresh source session required before S004 map selection");
                var selectedDescriptor = request["selected_operation_map"] as JObject;
                if (selectedDescriptor == null || selectedDescriptor.Count != 2 ||
                    selectedDescriptor["path"]?.Type != JTokenType.String || selectedDescriptor["sha256"]?.Type != JTokenType.String)
                    throw new InvalidDataException("Exact S004 selected map descriptor required");
                Descriptor(selectedDescriptor, 4 * 1024 * 1024);
                string selectedPath = (string)selectedDescriptor["path"], selectedHash = (string)selectedDescriptor["sha256"];
                var selectedNodes = ((JArray)graph["nodes"]).OfType<JObject>().Where(n =>
                    (string)n["source_type"] == nameof(OperationMapDefinition) &&
                    (string)n["source_sha256"] == selectedHash &&
                    selectedPath.Replace('\\', '/').EndsWith("/" + (string)n["source_path"], StringComparison.Ordinal)).ToArray();
                if ((string)graph["source_pins"]?[selectedPath] != selectedHash || selectedNodes.Length != 1 ||
                    !ReferenceEquals(assets.ByPath<OperationMapDefinition>((string)selectedNodes[0]["source_path"]),
                        UnityEngine.Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap")))
                    throw new InvalidDataException("S004 selected map descriptor differs from the actual bound source object");
                using var launchLayoutReplay = SourceValidatedLayoutJson.OpenDescriptor(ActualDefinition.MapLayout,
                    request["layout_json_replay"]);
                Entities.LogicalAllocatorAdmitted = true;
                Entities.AllocateLogicalIdentity = () => new Entity { Index = checked(nextIdentity++), Version = 1 };
                Session = SourceExpandedSessionConstruction.Create(Entities, Setup, ActualDefinition,
                    "generated-" + Setup.CatalogId + "-" + Setup.Seed);
                var selected = SkirmishS004Environment.LoadMapForSession(Entities);
                var bound = UnityEngine.Resources.Load<OperationMapDefinition>("SkirmishS004OperationMap");
                if (selected == null || !ReferenceEquals(selected, bound) || selected.OperationMapId != Setup.OperationMapId)
                    throw new InvalidOperationException("Original S004 map owner did not resolve the bound compiled map");
                expandedMap = selected;
                InitializationStage = "ExpandedSessionMapSelected";
                (expandedGrid, expandedSurface) = SourceExpandedAuthoredSetupInput.ResolvePhysicalInputs(graph, assets, selected,
                    selectedDescriptor, request["static_map"]);
                InitializationStage = "ExpandedPhysicalInputsSelected";
            }
#endif
            MetadataCompatibility=SourceMetadataCompatibility.Load(request,Setup,metadataSourceRepo,metadataDataRepo);
#if WARLINE_S004_SOURCE_MAP
            if (request.Property("source_metadata_diagnostic") != null)
            {
                var metadata = request["source_metadata_diagnostic"] as JObject;
                if (expandedMap == null || metadata == null || metadata.Count != 3 ||
                    (string)metadata["kind"] != "S004SourceMetadataDiagnostic" ||
                    metadata["source_only"]?.Type != JTokenType.Boolean || (bool)metadata["source_only"] != true ||
                    metadata["mission_ready"]?.Type != JTokenType.Boolean || (bool)metadata["mission_ready"] != false)
                    throw new InvalidDataException("Exact S004 source-only metadata diagnostic required");
                PublishS004SourceMetadata(expandedMap, request["operation_map_rotation_reference"] as JObject);
                InitializationStage = "ExpandedSourceMetadataPublished";
            }
#endif
#if WARLINE_EXPANDED_SOURCE_DIAGNOSTIC
            if (expandedGrid == null)
#endif
            {
            RetainSelectedOperationMapInputs(request, graph, assets, metadataSourceRepo);
            freshStorageAuthority=FreshSourceStorageLifetimeAuthority.FromValidatedSource(request,inputHash,sourceInitializationBytes,ActualDefinition,Setup,ActualCompileManifest,ActualCompileMatrix);
            }
            InitializationStage = "ConstructingBakedCampaignMap";
            Entities.LogicalAllocatorAdmitted = true;
            Entities.AllocateLogicalIdentity = () => new Entity { Index = checked(nextIdentity++), Version = 1 };
#if WARLINE_EXPANDED_SOURCE_DIAGNOSTIC
            var surfaceData=expandedGrid != null ? expandedSurface : assets.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
            var gridData=expandedGrid ?? assets.ByPath<GridAuthoringSceneConfigAsset>("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
#else
            var surfaceData=assets.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
            var gridData=assets.ByPath<GridAuthoringSceneConfigAsset>("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset");
#endif
            if(!surfaceData.TryCreateRuntimeBlobAsset(Allocator.Persistent,out var surfaceBlob))throw new InvalidOperationException("Original baked surface decoder rejected actual source payload");
            ref MapSurfaceBlob surface=ref surfaceBlob.Value;
            if(surface.Dimensions.x!=gridData.Width||surface.Dimensions.y!=gridData.Height||surface.CellSize!=gridData.CellSize)throw new InvalidOperationException("Actual baked grid/surface source domains differ");
            GeneratedMapSurfaceFlags.GetSurfaceFeatureFlags(ref surface,out byte layered,out byte roads,out byte bridges);
            SurfaceEntity=Entities.CreateEntity();
            Entities.AddComponentData(SurfaceEntity,new MapSurfaceComponent{SurfaceBlob=surfaceBlob,GridOrigin=surface.GridOrigin,CellSize=surface.CellSize,Dimensions=surface.Dimensions,HasSurfaceData=1,HasLayeredCells=layered,HasRoadSurfaces=roads,HasBridgeSurfaces=bridges});
            Entities.AddComponentData(SurfaceEntity,new MapSurfacePathCostComponent{EnableSlopeCost=0,GentleSlopeTraversalCost=0,SteepSlopeTraversalCost=0});
            GridEntity=Entities.CreateEntity();GeneratedGridSourceBaking.Bake(Entities,GridEntity,gridData);
#if WARLINE_EXPANDED_SOURCE_DIAGNOSTIC
            if (expandedGrid != null)
            {
                InitializationStage = "ExpandedSourceGridConstructed";
                SourceHistoricalStaticBlockers historical = null;
                if (request.Property("source_historical_navigation") != null)
                    historical = SourceHistoricalStaticBlockers.Load(request["source_historical_navigation"],
                        SourceRepositoryRoot, SkirmishS004Environment.LoadMapForSession(Entities));
                GeneratedFreshGridStorage.Initialize(Entities, GridEntity);
                if (historical != null) AuthoredNavigationCount = historical.Materialize(Entities);
                initialRoads.Sync(Entities, GridEntity);
                if (historical != null)
                {
                    var blockerOwner = new StaticGridBlockerUpdateSystem();
                    var blockerState = new SystemState { EntityManager = Entities, Enabled = true };
                    blockerState.Begin(); blockerOwner.OnCreate(ref blockerState); blockerState.End();
                    blockerState.Begin();
                    try { if (!blockerState.Enabled || !blockerState.RequiredQueriesPresent) throw new InvalidOperationException("Historical blocker owner prerequisites missing"); blockerOwner.OnUpdate(ref blockerState); }
                    finally { blockerState.End(); }
                    historical.VerifyInputs();
                    InitializationStage = "ExpandedHistoricalBlockersInitialized";
                    if (request.Property("source_historical_factory") != null)
                    {
                        using var grantLayoutReplay = SourceValidatedLayoutJson.OpenDescriptor(ActualDefinition.MapLayout,
                            request["layout_json_replay"]);
                        var grant = SourceS004HistoricalStartingGrant.Load(request["source_historical_factory"], SourceRepositoryRoot, Entities, Setup);
                        Boundary = Entities.CreateEntity();
                        Entities.AddComponentData(Boundary, new BuildingRuntimeStateTag());
                        Entities.AddBuffer<BuildingRuntimeSpawnRequest>(Boundary);
                        grant.PublishAndSubmit(Entities, Session, Boundary, Setup);
                        grant.VerifyInputs(); historical.VerifyInputs();
                        InitializationStage = "ExpandedHistoricalStartingGrantsSubmitted";
                        if (request.Property("source_placement_diagnostic") != null)
                        {
                            var placement = request["source_placement_diagnostic"] as JObject;
                            if (placement == null || placement.Count != 6 || (string)placement["kind"] != "S004HistoricalPlacementDiagnostic" ||
                                new[] { "source_only", "water_ignored", "sidewalk_ignored", "authored_building_mask_ignored", "mission_ready" }
                                    .Any(key => placement[key]?.Type != JTokenType.Boolean) ||
                                (bool?)placement["source_only"] != true || (bool?)placement["water_ignored"] != true ||
                                (bool?)placement["sidewalk_ignored"] != (request.Property("source_historical_placement_masks") == null) ||
                                (bool?)placement["authored_building_mask_ignored"] != (request.Property("source_historical_placement_masks") == null) ||
                                (bool?)placement["mission_ready"] != false)
                                throw new InvalidDataException("Exact source-only placement assumptions required");
                            var config = request["building_placement_config"] as JObject;
                            string expectedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(SourceRepositoryRoot,
                                "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset"));
                            if ((string)config?["path"] != expectedPath) throw new InvalidDataException("Actual Match placement configuration required");
                            string configText = System.Text.Encoding.UTF8.GetString(Descriptor(config, 1024 * 1024));
                            var scalar = System.Text.RegularExpressions.Regex.Match(configText, @"^  buildPlaneY: ([^\r\n]+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                            if (!scalar.Success || !float.TryParse(scalar.Groups[1].Value, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out float plane) || float.IsNaN(plane) || float.IsInfinity(plane))
                                throw new InvalidDataException("Finite authored placement plane required");
#if WARLINE_S004_HISTORICAL_MASKS
                            SourceS004HistoricalPlacementMasks historicalMasks = null;
                            if (request.Property("source_historical_placement_masks") != null)
                                historicalMasks = SourceS004HistoricalPlacementMasks.Load(request["source_historical_placement_masks"], SourceRepositoryRoot, expandedMap);
                            HistoricalPlacementObservation = grant.ProcessDiagnostic(Entities, Session, Boundary, GridEntity, SurfaceEntity,
                                Setup, Buildings, plane, FixedDeltaTime, historicalMasks?.Buildings, historicalMasks?.Roads, historicalMasks?.Sidewalks);
                            historicalMasks?.VerifyInputs();
#else
                            if (request.Property("source_historical_placement_masks") != null)
                                throw new NotSupportedException("Historical placement masks require the explicit diagnostic build");
                            HistoricalPlacementObservation = grant.ProcessDiagnostic(Entities, Session, Boundary, GridEntity, SurfaceEntity,
                                Setup, Buildings, plane, FixedDeltaTime);
#endif
                            blockerState.Begin();
                            try { blockerOwner.OnUpdate(ref blockerState); }
                            finally { blockerState.End(); }
                            HistoricalPlacementObservation["original_blocker_owner_after_placement"] = true;
                            if ((bool?)HistoricalPlacementObservation["original_bind_complete"] == true)
                            {
                                if (StartingPipeline != null || SourceDefinitions != null || grant.CompletedPipeline == null || grant.CompletedDefinitions == null)
                                    throw new InvalidOperationException("Completed same-world starting pipeline and definitions required");
                                StartingPipeline = grant.CompletedPipeline;
                                SourceDefinitions = grant.CompletedDefinitions;
                                generatedBuildPlane = plane;
                                HistoricalPlacementObservation["same_world_original_pipeline_retained"] = true;
                            }
                            grant.VerifyInputs(); historical.VerifyInputs();
                            InitializationStage = "ExpandedHistoricalPlacementProcessed";
#if WARLINE_S004_SOURCE_LEDGER
                            if (request.Property("source_opening_diagnostic") != null)
                            {
                                var opening = request["source_opening_diagnostic"] as JObject;
                                if (opening == null || opening.Count != 4 || (string)opening["kind"] != "S004HistoricalOpeningDiagnostic" ||
                                    opening["source_only"]?.Type != JTokenType.Boolean || (bool)opening["source_only"] != true ||
                                    opening["native_parity"]?.Type != JTokenType.Boolean || (bool)opening["native_parity"] != false ||
                                    opening["mission_ready"]?.Type != JTokenType.Boolean || (bool)opening["mission_ready"] != false ||
                                    (bool?)HistoricalPlacementObservation["original_bind_complete"] != true)
                                    throw new InvalidDataException("Exact source-only opening assumptions and completed original buildings required");
                                grant.InstallSourcePrefabRegistry(Entities, Setup);
                                var setupDescriptor = (JObject)request["source_historical_factory"]["setup"];
                                bool completed; SkirmishReasonCode reason; byte visualPending;
                                bool outerInvoked = false, nativePresentationDeferred = false;
                                using (SourceS004OpeningContext.Bind(Entities, Session, Setup,
                                    (string)setupDescriptor["path"], (string)setupDescriptor["sha256"], SourceRepositoryRoot))
                                {
#if WARLINE_S004_SOURCE_OUTER
                                    if (request.Property("source_outer_diagnostic") != null)
                                    {
                                        var outer = request["source_outer_diagnostic"] as JObject;
                                        if (outer == null || outer.Count != 5 || (string)outer["kind"] != "S004SourceOuterSpawnDiagnostic" ||
                                            outer["source_only"]?.Type != JTokenType.Boolean || (bool)outer["source_only"] != true ||
                                            (string)outer["source_view_profile"] != "DeferNativePresentation" ||
                                            outer["native_parity"]?.Type != JTokenType.Boolean || (bool)outer["native_parity"] != false ||
                                            outer["mission_ready"]?.Type != JTokenType.Boolean || (bool)outer["mission_ready"] != false ||
                                            actualOperationMapOwner == null)
                                            throw new InvalidDataException("Exact S004 source outer-spawn diagnostic and published metadata required");
                                        SkirmishScenarioSpawnSystem.TrySpawn(Entities, Session);
                                        outerInvoked = true;
                                        var afterOuter = Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
                                        completed = afterOuter.SpawnComplete == 1;
                                        reason = afterOuter.FailureCode; visualPending = afterOuter.SpawnVisualPending;
                                        if (afterOuter.Phase == SkirmishSessionPhase.Playing && afterOuter.SpawnComplete == 1)
                                        { nativePresentationDeferred = true; }
                                    }
                                    else
#endif
                                    completed = SkirmishScenarioSpawnSystem.TrySpawnLedgers(Entities, Session, Setup, out reason, out visualPending);
                                }
                                var session = Entities.GetComponentData<SkirmishExpandedSessionComponent>(Session);
                                int unitCount = 0, playerUnits = 0, enemyUnits = 0, otherUnits = 0;
                                var roles = new JObject();
                                using (var owned = Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent)))
                                using (var units = owned.ToEntityArray(Allocator.Temp))
                                    foreach (var unit in units)
                                    {
                                        var actor = Entities.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                                        if (actor.SessionId.Equals(session.SessionId) && actor.IsStructure == 0)
                                        {
                                            unitCount++;
                                            if (actor.FactionId == 1) playerUnits++; else if (actor.FactionId == 2) enemyUnits++; else otherUnits++;
                                            string role = Entities.GetComponentData<SkirmishUnitRoleComponent>(unit).Role.ToString();
                                            roles[role] = (int?)roles[role] + 1 ?? 1;
                                        }
                                    }
                                HistoricalOpeningObservation = new JObject { ["kind"] = "S004HistoricalSourceOpeningDiagnostic",
                                    ["original_ledger_completed"] = completed, ["reason"] = reason.ToString(), ["visual_pending"] = visualPending,
                                    ["outer_spawn_owner_invoked"] = outerInvoked,
                                    ["ledger_result_basis"] = outerInvoked ? "InferredFromOriginalSpawnComplete" : "DirectOriginalReturn",
                                    ["native_presentation_deferred"] = nativePresentationDeferred,
                                    ["observed_owned_units"] = unitCount, ["configured_units"] = Setup.Forces.Sum(force => force.Quantity),
                                    ["player_units"] = playerUnits, ["enemy_units"] = enemyUnits, ["other_units"] = otherUnits,
                                    ["roles"] = roles, ["original_supply_initialized"] = Entities.HasComponent<SkirmishSharedSupplyInitialized>(Session),
                                    ["session_phase"] = session.Phase.ToString(),
                                    ["match_phase"] = Entities.HasComponent<SkirmishMatchState>(Session) ? Entities.GetComponentData<SkirmishMatchState>(Session).Phase.ToString() : null,
                                    ["session_spawn_complete"] = session.SpawnComplete,
                                    ["placement_masks"] = new JObject { ["authored_buildings"] = HistoricalPlacementObservation["authored_building_mask"].DeepClone(),
                                        ["sidewalks"] = HistoricalPlacementObservation["sidewalk_mask"].DeepClone(),
                                        ["water"] = HistoricalPlacementObservation["water_mask"].DeepClone() },
                                    ["native_parity"] = false, ["source_fit_certified"] = false, ["mission_ready"] = false };
#if WARLINE_S004_SOURCE_MAP
                                if (actualOperationMapOwner != null)
                                {
                                    var root = ActualOperationMapRoot;
                                    var active = Entities.GetComponentData<ActiveOperationMapComponent>(root);
                                    var mapMetadata = Entities.GetComponentData<OperationMapMetadataComponent>(root);
                                    HistoricalOpeningObservation["map_publication"] = new JObject { ["operation_map_id"] = active.OperationMapId.ToString(),
                                        ["scenario_id"] = active.ScenarioId.ToString(), ["mission_id"] = active.MissionId.ToString(),
                                        ["physical_source_validated"] = mapMetadata.PhysicalSourceValidated,
                                        ["scope"] = "Source metadata only; no loaded physical scene" };
                                }
#endif
                                if (request.Property("source_roster_diagnostic") != null)
                                {
                                    var rosterRequest = request["source_roster_diagnostic"] as JObject;
                                    if (rosterRequest == null || rosterRequest.Count != 3 ||
                                        (string)rosterRequest["kind"] != "S004OriginalRosterOwnerDiagnostic" ||
                                        (bool?)rosterRequest["source_only"] != true || (bool?)rosterRequest["mission_ready"] != false ||
                                        !outerInvoked || session.Phase != SkirmishSessionPhase.Playing || session.SpawnComplete != 1)
                                        throw new InvalidDataException("S004 roster diagnostic requires actual completed outer startup");
                                    var roster = new SkirmishRosterProjectionSystem();
                                    var rosterState = FreshState();
                                    rosterState.Begin();
                                    try { roster.OnCreate(ref rosterState); }
                                    finally { rosterState.End(); }
                                    rosterState.Begin();
                                    bool invoked = false;
                                    try { if (rosterState.Enabled && rosterState.RequiredQueriesPresent) { roster.OnUpdate(ref rosterState); invoked = true; } }
                                    finally { rosterState.End(); }
                                    int overlays = 0, suppressed = 0, living = 0;
                                    using (var q = Entities.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent)))
                                    using (var actors = q.ToEntityArray(Allocator.Temp))
                                        foreach (var actor in actors)
                                        {
                                            var owner = Entities.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                                            if (!owner.SessionId.Equals(session.SessionId) || owner.IsStructure != 0) continue;
                                            if (Entities.HasComponent<SkirmishRoleOverlayComponent>(actor) && Entities.GetComponentData<SkirmishRoleOverlayComponent>(actor).Applied != 0) overlays++;
                                            if (Entities.HasComponent<CampaignMissionCombatSuppressedTag>(actor)) suppressed++;
                                            if (Entities.HasComponent<UnitHealth>(actor) && Entities.GetComponentData<UnitHealth>(actor).Current > 0) living++;
                                        }
                                    HistoricalOpeningObservation["roster_owner"] = new JObject { ["original_on_update_invoked"] = invoked,
                                        ["applied_unit_overlays"] = overlays, ["suppressed_units"] = suppressed, ["units_with_positive_health"] = living,
                                        ["full_scheduler_executed"] = false, ["native_parity"] = false, ["mission_ready"] = false };
                                }
#if WARLINE_S004_HISTORICAL_MASKS
                                historicalMasks?.VerifyInputs();
#endif
                                grant.VerifyInputs(); historical.VerifyInputs();
                                InitializationStage = "ExpandedHistoricalOpeningObserved";
                                throw new GeneratedInitializationBlocked("ExpandedSourceStartup", "S004 original source opening ledger observed; player input, native parity, phase transition and full gameplay are not admitted");
                            }
#endif
                            throw new GeneratedInitializationBlocked("ExpandedSourceStartup", "S004 original starting placement owner processed historical diagnostic inputs; source fit, current native parity and gameplay are not admitted");
                        }
                        throw new GeneratedInitializationBlocked("ExpandedSourceStartup", "S004 historical source definitions published and original starting grants submitted; placement, current native parity, map publication and scheduling are not yet admitted");
                    }
                    throw new GeneratedInitializationBlocked("ExpandedSourceStartup", "S004 historical source blockers and original blocker owner executed; current native parity, original startup, map publication and scheduling are not yet admitted");
                }
                if (request.Property("source_historical_factory") != null)
                    throw new InvalidDataException("Historical factory diagnostic requires historical same-world blockers");
                InitializationStage = "ExpandedSourceRoadsInitialized";
                throw new GeneratedInitializationBlocked("ExpandedSourceStartup", "S004 source grid, surface, fresh storage and initial road owner executed; authored navigation, original startup, map publication and scheduling are not yet admitted");
            }
#endif
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
        public void Dispose() { DisposeActualOperationMapPublication(); initialRoads?.Dispose(); disposed = true; }
    }
}
