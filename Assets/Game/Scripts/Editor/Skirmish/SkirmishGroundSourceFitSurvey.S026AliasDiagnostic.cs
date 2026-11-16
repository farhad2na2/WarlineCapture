#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        const string S026AliasPath = "Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset";
        const string S026CampaignSourcePath = "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
        const string S026AliasSha = "a190c9fa0f40c77ebb0c828a48d0eb0266124ac739f7e6e661ada4cb2d4c3c6f";
        const string S026CampaignSourceSha = "68a631ca108e01c9a17839d2359e409226cdb009193f68ba910fa6f0f0a082b8";
        const string S026GridSha = "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb";
        const string S026SurfaceSha = "5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d";
        const string CompanionSceneUtilityPath = "Library/PackageCache/com.unity.entities@ef040875dea9/Unity.Scenes/Hybrid/CompanionSceneUtility.cs";
        const string CompanionSceneUtilitySha = "a775ad88d466afbebac429d19d40b2c3bac9311e936ab9b3f4d87b6b00dac489";

        sealed class S026AliasRoleRequest
        {
            public string RoleId;
            public string Role;
            public int PlayerCount;
            public int EnemyCount;
        }

        struct S026StaticPlacementContext : IPathPlacementContext
        {
            public int WidthValue, HeightValue;
            public NativeArray<GridWalkable> WalkableCells;
            public int Width => WidthValue;
            public int Height => HeightValue;
            public bool HasDynamicBlocked => false;
            public bool HasOccupied => false;
            public bool HasFriendlyPass => false;
            public int FriendlyPassLength => 0;
            public int UnitCount => 0;
            public int ManualGroupCount => 0;
            public byte Walkable(int index) => WalkableCells[index].Value;
            public bool DynamicBlocked(int index) => false;
            public bool Occupied(int index) => false;
            public byte FriendlyPass(int index) => 0;
            public PlacementIdentity UnitEntity(int index) => default;
            public PlacementCell UnitCell(int index) => default;
            public PlacementCell UnitSize(int index) => default;
            public byte ManualGroup(int index) => 0;
        }

        // Bounded source-owner probe for the existing CityCrossroads alias. It queries
        // the exact Standard S026 roster at the alias's authored deployment anchors,
        // and the original static path owner between cells that owner actually found.
        // It writes no assets and cannot establish full-size placement or readiness.
        public static void RunS026CityCrossroadsAliasSourceDiagnostic()
        {
            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=Entry");
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating && ground30MemoryWriter == null,
                "Idle exclusive Editor source query required");
            string output = Environment.GetEnvironmentVariable("WARLINE_S026_ALIAS_SOURCE_DIAGNOSTIC_OUTPUT");
            string allowed = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output) && Path.IsPathRooted(output) &&
                Path.GetFullPath(output).StartsWith(allowed, StringComparison.OrdinalIgnoreCase) && !File.Exists(output),
                "Fresh absolute Logs/SkirmishProduction output required");
            var worldBaseline = CaptureMaterializationWorldCensus();
            var defaultWorldBaseline = World.DefaultGameObjectInjectionWorld;
            int sceneCountBaseline = UnityEngine.SceneManagement.SceneManager.sceneCount;
            int previewCountBaseline = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;

            var report = new JObject
            {
                ["schema"] = 1,
                ["kind"] = "S026CityCrossroadsAliasSourceOwnerDiagnostic",
                ["scope"] = "ExistingCampaignSource_AliasFrame_StandardOnly",
                ["result"] = "Incomplete",
                ["sourceFitCertified"] = false,
                ["allSizeCapacityProven"] = false,
                ["nativeMissionInitialized"] = false,
                ["liveOccupancyIncluded"] = false,
                ["normalMissionReady"] = false,
                ["outputPath"] = output
            };
            reportCleanupBaseline();
            var previewPhases = new JArray();
            report["previewOwnershipPhases"] = previewPhases;
            try
            {
                report["controlsPassed"] = RunS026AliasSourceDiagnosticControls();
                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=ControlsPassed controls=" + report["controlsPassed"]);
                report["companionCleanupControlsPassed"] = RunS026CompanionCleanupControls();
                report["sourceCheckpointControlsPassed"] = RunS026SourceCheckpointControls();
                var alias = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(S026AliasPath);
                var campaign = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(S026CampaignSourcePath);
                Require(alias != null && campaign != null && !EditorUtility.IsDirty(alias) && !EditorUtility.IsDirty(campaign),
                    "Clean existing alias and Campaign physical source required");
                Require(Hash(S026AliasPath) == S026AliasSha && Hash(S026CampaignSourcePath) == S026CampaignSourceSha &&
                    Hash("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset") == S026GridSha &&
                    Hash("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset") == S026SurfaceSha,
                    "Exact alias, physical source, grid, or surface drift");
                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=InputPinsPassed");
                Require(alias.OperationMapId == "opmap.skirmish.city_crossroads" &&
                    alias.SourceBinding.SourceOperationMapId == campaign.OperationMapId &&
                    alias.SourceBinding.SourceIdentityHash == campaign.SourceIdentityHash &&
                    alias.SourceBinding.SourceContentHash == campaign.ContentHash &&
                    alias.SourceIdentityHash == campaign.SourceIdentityHash &&
                    alias.GridMetadata.AssetGuid == campaign.GridMetadata.AssetGuid &&
                    alias.GridMetadata.ContentHash == campaign.GridMetadata.ContentHash &&
                    alias.SurfaceMetadata.AssetGuid == campaign.SurfaceMetadata.AssetGuid &&
                    alias.SurfaceMetadata.ContentHash == campaign.SurfaceMetadata.ContentHash &&
                    alias.SourceSceneReference.AssetGUID == campaign.SourceSceneReference.AssetGUID &&
                    alias.NavigationMetadata.AuthoredSubSceneGuid == campaign.NavigationMetadata.AuthoredSubSceneGuid,
                    "CityCrossroads alias no longer resolves to exact pinned Campaign source components");
                Require(alias.Bounds.PlayableMin.x == 900f && alias.Bounds.PlayableMax.x == 1300f &&
                    alias.Bounds.PlayableMin.z == 260f && alias.Bounds.PlayableMax.z == 875f,
                    "Exact existing CityCrossroads alias frame required");
                int aliasLaneAnchorCount = alias.Anchors.ToArray().Count(a => a.Kind == OperationMapAnchorKind.Lane);
                int campaignLaneAnchorCount = campaign.Anchors.ToArray().Count(a => a.Kind == OperationMapAnchorKind.Lane);
                report["routeWaypointAnchorEvidence"] = new JObject
                {
                    ["aliasLaneAnchorCount"] = aliasLaneAnchorCount,
                    ["campaignLaneAnchorCount"] = campaignLaneAnchorCount,
                    ["authoredLaneWaypointsAvailable"] = aliasLaneAnchorCount + campaignLaneAnchorCount > 0,
                    ["source"] = "ExactPinnedAliasAndExistingCampaignDefinitionAnchorArrays"
                };

                var anchors = alias.Anchors.ToArray().Where(a => a.Kind == OperationMapAnchorKind.Deployment)
                    .OrderBy(a => a.FactionId).ToArray();
                Require(anchors.Length == 2 && anchors[0].FactionId == 1 && anchors[1].FactionId == 2 &&
                    anchors[0].AnchorId == "anchor.skirmish.city_crossroads.deployment.faction_1" &&
                    anchors[1].AnchorId == "anchor.skirmish.city_crossroads.deployment.faction_2" &&
                    anchors[0].Position.x == 1020f && anchors[0].Position.z == 750f &&
                    anchors[1].Position.x == 1100f && anchors[1].Position.z == 400f &&
                    IsInsideAlias(alias, anchors[0].Position.x, anchors[0].Position.z) &&
                    IsInsideAlias(alias, anchors[1].Position.x, anchors[1].Position.z),
                    "Exact source-authored alias deployment anchors required");
                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=AliasIdentityPassed");

                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                Require(SkirmishSetupMatrixTable.TryLoad(projectRoot, out var matrix, out string matrixError), matrixError);
                var all120 = ReadAll120RefinementIndex();
                Require(Hash(SkirmishSetupMatrixTable.RelativeCsvPath) == (string)all120["matrix_sha256"],
                    "Canonical setup matrix no longer matches reviewed All120 authority");
                var mission = ((JArray)all120["missions"]).OfType<JObject>().Single(r => (string)r["catalog_id"] == "S026");
                Require(SkirmishSetupMatrixTable.TryFind(matrix, "S026", SkirmishSizeId.Standard, out var standard),
                    "Exact S026 Standard setup-matrix row required");
                Require((string)mission["definition_id"] == "skirmish.s026" &&
                    (string)mission["scenario_setup_id"] == "scenario.skirmish.s026" &&
                    (string)mission["tuple"]["map_id"] == "CC" &&
                    (string)mission["tuple"]["objective_id"] == "BA" &&
                    (string)mission["tuple"]["army_profile"] == "G" &&
                    (string)mission["tuple"]["start_profile"] == "E" &&
                    mission["protected_binding"].Type == JTokenType.Null,
                    "Exact unprotected S026/CC/BA/G/E Standard authority required");
                var standardAuthority = ((JArray)mission["canonical_size_rows"]).OfType<JObject>()
                    .Single(r => (string)r["size_id"] == "Standard");
                Require(MatrixMatchesS026Authority(standard, standardAuthority),
                    "S026 Standard canonical matrix row differs from exact Full120 authority row");
                report["identity"] = new JObject { ["catalogId"] = "S026", ["definitionId"] = mission["definition_id"],
                    ["scenarioSetupId"] = mission["scenario_setup_id"], ["size"] = "Standard", ["difficulty"] = "Regular",
                    ["tuple"] = mission["tuple"].DeepClone(), ["canonicalBudgetMatrixSha256"] = Hash(SkirmishSetupMatrixTable.RelativeCsvPath),
                    ["canonicalBudgetAuthoritySha256"] = (string)all120["matrix_sha256"] };
                report["sourcePins"] = new JObject
                {
                    ["diagnosticImplementationPath"] = "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.S026AliasDiagnostic.cs",
                    ["diagnosticImplementationSha256"] = Hash("Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.S026AliasDiagnostic.cs"),
                    ["aliasPath"] = S026AliasPath, ["aliasSha256"] = Hash(S026AliasPath),
                    ["aliasOperationMapId"] = alias.OperationMapId, ["aliasContentHash"] = alias.ContentHash,
                    ["aliasSourceBinding"] = new JObject { ["operationMapId"] = alias.SourceBinding.SourceOperationMapId,
                        ["sourceIdentityHash"] = alias.SourceBinding.SourceIdentityHash, ["sourceContentHash"] = alias.SourceBinding.SourceContentHash },
                    ["campaignPath"] = S026CampaignSourcePath, ["campaignSha256"] = Hash(S026CampaignSourcePath),
                    ["campaignOperationMapId"] = campaign.OperationMapId, ["campaignSourceIdentityHash"] = campaign.SourceIdentityHash,
                    ["campaignContentHash"] = campaign.ContentHash,
                    ["sourceSceneGuid"] = alias.SourceSceneReference.AssetGUID,
                    ["navigationSubSceneGuid"] = alias.NavigationMetadata.AuthoredSubSceneGuid,
                    ["gridPath"] = AssetDatabase.GUIDToAssetPath(alias.GridMetadata.AssetGuid), ["gridSha256"] = Hash(AssetDatabase.GUIDToAssetPath(alias.GridMetadata.AssetGuid)),
                    ["surfacePath"] = AssetDatabase.GUIDToAssetPath(alias.SurfaceMetadata.AssetGuid), ["surfaceSha256"] = Hash(AssetDatabase.GUIDToAssetPath(alias.SurfaceMetadata.AssetGuid))
                };
                report["aliasPlayableBounds"] = new JObject { ["minX"] = alias.Bounds.PlayableMin.x, ["maxX"] = alias.Bounds.PlayableMax.x,
                    ["minZ"] = alias.Bounds.PlayableMin.z, ["maxZ"] = alias.Bounds.PlayableMax.z };
                report["deploymentAnchors"] = new JArray(anchors.Select(a => new JObject { ["anchorId"] = a.AnchorId,
                    ["faction"] = a.FactionId, ["worldX"] = a.Position.x, ["worldZ"] = a.Position.z, ["sourceAuthoredRadius"] = a.Radius }));

                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=BeforeStreamingWorld");
                using (var world = StreamingWorld())
                {
                    Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=StreamingWorldCreated");
                    Load(world, alias.NavigationMetadata.AuthoredSubSceneGuid);
                    RecordPreviewOwnership("AfterCampaignNavigationLoad");
                    Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=CampaignNavigationLoaded");
                    Load(world, AssetDatabase.AssetPathToGUID(Support));
                    RecordPreviewOwnership("AfterRuntimeSupportLoad");
                    Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=RuntimeSupportLoaded");
                    using (var runtimeSurface = PublishMissingPinnedSurface(world, campaign,
                        AssetDatabase.GUIDToAssetPath(campaign.MapSurfaceDataReference.AssetGUID), report))
                    {
                        var em = world.EntityManager;
                        using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(GridRoad));
                        using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
                        Require(grids.CalculateEntityCount() == 1 && surfaces.CalculateEntityCount() == 1,
                            "Exactly one source baked grid and surface required");
                        Entity gridEntity = grids.GetSingletonEntity(); GridConfig grid = grids.GetSingleton<GridConfig>();
                        MapSurfaceComponent surface = surfaces.GetSingleton<MapSurfaceComponent>();
                        Require(grid.Width == campaign.GridMetadata.Dimensions.x && grid.Height == campaign.GridMetadata.Dimensions.y &&
                            grid.CellSize == campaign.GridMetadata.CellSize && math.all(grid.Origin == (float3)campaign.GridMetadata.Origin) &&
                            surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated &&
                            surface.Dimensions.Equals(new int2(grid.Width, grid.Height)) && surface.CellSize == grid.CellSize &&
                            math.all(surface.GridOrigin == grid.Origin), "Exact current Campaign baked grid/surface required");
                        report["nativeSourceGrid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height,
                            ["cellSize"] = grid.CellSize, ["origin"] = new JArray(grid.Origin.x, grid.Origin.z),
                            ["scope"] = "ActualExistingCampaignBakedGridAndSurface_NoGameplaySystems" };
                        Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=NativeGridReady width=" + grid.Width + " height=" + grid.Height);

                        var forces = S026AliasStandardForceRequests(standard);
                        var roleCatalog = LoadS026AliasRoleCatalog();
                        report["standardBudget"] = new JObject { ["size"] = standard.SizeId.ToString(),
                            ["playerInfantry"] = standard.PlayerInfantry, ["playerGround"] = standard.PlayerGround,
                            ["playerLogisticsSupport"] = standard.PlayerLogisticsSupport,
                            ["enemyInfantry"] = standard.EnemyInfantry, ["enemyGround"] = standard.EnemyGround,
                            ["enemyLogisticsSupport"] = standard.EnemyLogisticsSupport,
                            ["source"] = "ExactCanonicalS026MatrixRow", ["budgetChanged"] = false };
                        report["forceRequests"] = new JArray(forces.Select(f => new JObject { ["roleId"] = f.RoleId,
                            ["role"] = f.Role, ["playerRequested"] = f.PlayerCount, ["enemyRequested"] = f.EnemyCount }));
                        var placements = new JArray(); report["candidatePlacements"] = placements;
                        using var blocked = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                        using var occupied = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                        try
                        {
                            var walkable = em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray();
                        var roads = em.GetBuffer<GridRoad>(gridEntity, true).AsNativeArray();
                        var sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity, true).AsNativeArray();
                        var dirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity, true).AsNativeArray();
                        int frameBlocked = 0, aliasGridRoadCells = 0, aliasSidewalkCells = 0, aliasDirtRoadCells = 0;
                            for (int i = 0; i < walkable.Length; i++)
                            {
                                int x = i % grid.Width, z = i / grid.Width;
                                float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                                float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                            bool insideAlias = IsInsideAlias(alias, wx, wz);
                            if (walkable[i].Value == 0 || !insideAlias) blocked.Set(i, true);
                            if (!insideAlias) frameBlocked++;
                            else
                            {
                                if (roads[i].Value != 0) aliasGridRoadCells++;
                                if (sidewalks[i].Value != 0) aliasSidewalkCells++;
                                if (dirtRoads[i].Value != 0) aliasDirtRoadCells++;
                            }
                            }
                            InitialUnitsSpawnSystem.ReserveStaticBlockerFootprints(em, ref reserved, grid);
                            report["aliasFrameSearchMask"] = new JObject { ["maskedCellsOutsideAliasFrame"] = frameBlocked,
                                ["physicalSourceWalkabilityPreserved"] = true, ["scope"] = "DiagnosticSearchConstraintOnly_NoCampaignMutation" };
                        report["sourceRoadRasterCellsInsideAlias"] = new JObject
                        {
                            ["gridRoad"] = aliasGridRoadCells, ["sidewalk"] = aliasSidewalkCells,
                            ["dirtRoad"] = aliasDirtRoadCells,
                            ["source"] = "ActualLoadedCampaignBakedGridBuffers",
                            ["usableAsAuthoredMissionWaypoint"] = false,
                            ["runtimeRoadProjectionOwnerLoaded"] = false,
                            ["interpretation"] = "Static baked buffers only; read-only StreamingWorld excludes Game systems, including gameplay road projection; zero masks do not imply no visual Campaign roads or no roads in an initialized gameplay world"
                        };
                        report["gridRoadOwnerEvidence"] = new JObject
                        {
                            ["gridBakerPath"] = "Assets/Game/Scripts/Authorings/GridAuthoring.Baker.cs",
                            ["gridBakerSha256"] = Hash("Assets/Game/Scripts/Authorings/GridAuthoring.Baker.cs"),
                            ["gridBakerStaticRoadInitialization"] = "GridRoad/GridRoadSidewalk/GridRoadDirt initialized to zero; only configured blocked cells are authored",
                            ["runtimeProjectionPath"] = "Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs",
                            ["runtimeProjectionSha256"] = Hash("Assets/Game/Scripts/Systems/RoadGridProjectionSystem.cs"),
                            ["runtimeProjectionOwner"] = "Game.Runtime.RoadGridProjectionSystem",
                            ["runtimeProjectionWorld"] = "SimulationSystemGroup; reads runtime RoadTiles and GameObjects",
                            ["projectionOwnerInThisStreamingWorld"] = false,
                            ["streamingWorldBuilderPath"] = "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.cs",
                            ["streamingWorldBuilderSha256"] = Hash("Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.cs"),
                            ["streamingWorldRule"] = "Rejects every system name beginning Game.; no gameplay road projection system executes"
                        };
                            var firstCellByFactionRole = new Dictionary<string, int2>(StringComparer.Ordinal);
                            var random = new Unity.Mathematics.Random(1);
                            int totalRequested = 0, totalFound = 0, outsideOrUntraversable = 0;
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=BeforeStandardSpawnQueries");
                            foreach (byte faction in new byte[] { 1, 2 })
                            foreach (var force in forces)
                            {
                                int quantity = faction == 1 ? force.PlayerCount : force.EnemyCount;
                                if (quantity <= 0) continue;
                                string side = faction == 1 ? "player" : "enemy";
                                var roleRow = new JObject { ["faction"] = faction, ["side"] = side,
                                    ["roleId"] = force.RoleId, ["role"] = force.Role, ["requestedCount"] = quantity,
                                    ["scope"] = "InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell_StaticSourceBlockersAndAliasFrameMask",
                                    ["candidateOnly"] = true, ["normalSpawnAcceptance"] = false, ["liveOccupancyIncluded"] = false };
                                placements.Add(roleRow); totalRequested += quantity;
                                if (!roleCatalog.TryGet(force.RoleId, out var binding))
                                { roleRow["status"] = "CanonicalRoleBindingMissing"; continue; }
                                Entity prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                                if (prefab == Entity.Null || !em.HasComponent<UnitFootprint>(prefab))
                                { roleRow["status"] = "ExactNativePrefabMissing"; continue; }
                                var footprint = em.GetComponentData<UnitFootprint>(prefab).Size;
                                bool air = em.HasComponent<UnitAirMovement>(prefab);
                                bool vehicle = em.HasComponent<UnitMovementBehavior>(prefab) &&
                                    em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion != 0;
                                var anchor = anchors.Single(a => a.FactionId == faction);
                                int2 requestedCenter = GridUtils.WorldToCell(grid, new float3(anchor.Position.x, 0, anchor.Position.z));
                                var cells = new JArray(); roleRow["candidateCells"] = cells;
                                roleRow["requestedCenterCell"] = new JArray(requestedCenter.x, requestedCenter.y);
                                roleRow["nativePrefabKey"] = binding.RuntimePrefabKey;
                                roleRow["nativeFootprint"] = new JArray(footprint.x, footprint.y);
                                int found = 0, traversable = 0;
                                var validation = new MapSurfaceTraversalValidation();
                                for (int member = 0; member < quantity; member++)
                                {
                                    if (!InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref random, grid, walkable, blocked,
                                        occupied, ref reserved, requestedCenter, 12, footprint, air, out var cell)) break;
                                    var min = UnitFootprintUtility.GetMinCell(cell, footprint);
                                    bool inside = IsFootprintInsideAlias(alias, grid, min, footprint);
                                    bool passable = inside && (air || validation.CanTraverseFootprint(surface, surface.HasSurfaceData,
                                        grid, cell, footprint, vehicle));
                                    var item = new JObject { ["member"] = member, ["cellX"] = cell.x, ["cellZ"] = cell.y,
                                        ["footprintMinX"] = min.x, ["footprintMinZ"] = min.y,
                                        ["insideAliasFrame"] = inside, ["sourceSurfaceTraversable"] = passable };
                                    cells.Add(item);
                                    if (!inside || !passable) { outsideOrUntraversable++; break; }
                                    found++; totalFound++; traversable++;
                                    if (!firstCellByFactionRole.ContainsKey(faction + "/" + force.RoleId))
                                        firstCellByFactionRole.Add(faction + "/" + force.RoleId, cell);
                                }
                                roleRow["foundCount"] = found; roleRow["sourceSurfaceTraversableCount"] = traversable;
                                roleRow["status"] = found == quantity ? "AllRequestedCellsFound_Unqualified" : "RequestedCellsNotAllFound";
                            }
                            report["spawnQueryTotals"] = new JObject { ["requestedActors"] = totalRequested,
                                ["foundActors"] = totalFound, ["outsideOrSurfaceRejected"] = outsideOrUntraversable,
                                ["completeStandardRosterFound"] = totalRequested == totalFound,
                                ["completeRosterCapacityProven"] = false };
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=StandardSpawnQueriesComplete requested=" + totalRequested + " found=" + totalFound);

                            // This is an independent, bounded static connectivity model. It uses the
                            // shared placement kernel and the same surface traversal owner, but it is
                            // not GridPathSearchKernel/PathfindBatchJob evidence and excludes dynamic
                            // blockers and live occupancy. Any candidate segment is checked separately
                            // by QueryPath below at the unchanged production expansion limits.
                            var connectivityRows = new JArray(); report["sourceConnectivityComponents"] = connectivityRows;
                            report["sourceConnectivityScope"] = "IndependentBoundedComponentModel_SharedPathPlacementKernel_GridWalkableAndMapSurface_NoDynamicBlockersOrLiveOccupancy_NoRouteOrFitClaim";
                            foreach (string roleId in new[] { SkirmishRoleIds.Rifle, SkirmishRoleIds.Tank, SkirmishRoleIds.ApcArmored })
                            {
                                if (!roleCatalog.TryGet(roleId, out var binding))
                                    throw new InvalidOperationException("Exact native role binding required for source connectivity model");
                                var prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                                Require(prefab != Entity.Null && em.HasComponent<UnitFootprint>(prefab) &&
                                    em.HasComponent<UnitMovementBehavior>(prefab) && !em.HasComponent<UnitAirMovement>(prefab),
                                    "Exact native ground prefab required for source connectivity model");
                                var nativeFootprint = em.GetComponentData<UnitFootprint>(prefab);
                                var footprint = nativeFootprint.Size;
                                bool vehicle = UnitVehicleMovementUtility.IsVehicle(nativeFootprint,
                                    em.GetComponentData<UnitMovementBehavior>(prefab));
                                var row = new JObject { ["roleId"] = roleId, ["modelKind"] = "IndependentSharedPlacementKernelComponentCensus",
                                    ["sourceFitCertified"] = false, ["normalMissionAcceptance"] = false,
                                    ["routeProof"] = false, ["dynamicBlockersIncluded"] = false, ["liveOccupancyIncluded"] = false,
                                    ["productionPathLimitsChanged"] = false, ["componentTileCapPerSearch"] = 2500000,
                                    ["componentSearchCountCap"] = 2,
                                    ["componentCountCap"] = 10000, ["candidatePairQueryCap"] = 8,
                                    ["primaryCheckpointCountCap"] = 5,
                                    ["alternateCheckpointCountCap"] = 6,
                                    ["primaryOwnerSegmentCountCap"] = 6,
                                    ["alternateOwnerSegmentCountCap"] = 7,
                                    ["componentOwnerSegmentCountCapBothCandidates"] = 13 };
                                connectivityRows.Add(row);
                                bool hasPlayer = firstCellByFactionRole.TryGetValue("1/" + roleId, out var playerCell);
                                bool hasEnemy = firstCellByFactionRole.TryGetValue("2/" + roleId, out var enemyCell);
                                int preferredSeed = hasPlayer ? playerCell.y * grid.Width + playerCell.x : 0;
                                row["componentTreeSeedPolicy"] = "SelectedPlayerSpawnCellIfAvailable_ThenWrappedGridOrder";
                                if (!TryBuildS026SourceComponents(alias, grid, walkable, surface, footprint, vehicle,
                                    out var componentIds, out var componentParents, out var componentSizes, out int validCellCount, false, preferredSeed))
                                { row["status"] = "BoundedModelCapExceededOrInputInvalid"; continue; }
                                row["validAliasCenters"] = validCellCount;
                                row["componentCount"] = componentSizes.Count;
                                row["componentCellCounts"] = new JArray(componentSizes.OrderByDescending(size => size));
                                int playerComponent = hasPlayer ? componentIds[playerCell.y * grid.Width + playerCell.x] : 0;
                                int enemyComponent = hasEnemy ? componentIds[enemyCell.y * grid.Width + enemyCell.x] : 0;
                                row["playerSelectedCell"] = hasPlayer ? new JArray(playerCell.x, playerCell.y) : JValue.CreateNull();
                                row["enemySelectedCell"] = hasEnemy ? new JArray(enemyCell.x, enemyCell.y) : JValue.CreateNull();
                                row["playerComponentId"] = playerComponent;
                                row["enemyComponentId"] = enemyComponent;
                                row["selectedCellsShareModeledComponent"] = playerComponent > 0 && playerComponent == enemyComponent;
                                row["componentFindingMeaning"] = "Same component means connected under this bounded static model only; different components are not native route or gameplay disconnection proof";
                                if (playerComponent > 0 && playerComponent == enemyComponent)
                                {
                                    var modelPath = ReconstructS026ComponentPath(componentParents, grid.Width,
                                        playerCell, enemyCell, tileCount: componentIds.Length);
                                    row["independentModelPathCellCount"] = modelPath.Count;
                                    row["independentModelPathClaim"] = "Candidate checkpoints only; path is reconstructed from independent component BFS tree, not native owner output";
                                    if (modelPath.Count > 1)
                                    {
                                        var candidateCheckpoints = new List<int2>();
                                        for (int part = 1; part < 6; part++)
                                        {
                                            int pathIndex = (int)Math.Round((modelPath.Count - 1) * (part / 6.0));
                                            var point = modelPath[pathIndex];
                                            if (point.Equals(playerCell) || point.Equals(enemyCell)) continue;
                                            if (candidateCheckpoints.Count == 0 || !candidateCheckpoints[candidateCheckpoints.Count - 1].Equals(point))
                                                candidateCheckpoints.Add(point);
                                        }
                                        var checkpointCells = new JArray();
                                        foreach (var point in candidateCheckpoints) checkpointCells.Add(new JArray(point.x, point.y));
                                        row["independentModelCheckpointCells"] = checkpointCells;
                                        var ownerSegments = new JArray(); row["componentRouteOwnerSegmentChecks"] = ownerSegments;
                                        var ownerPoints = new List<int2> { playerCell };
                                        ownerPoints.AddRange(candidateCheckpoints);
                                        ownerPoints.Add(enemyCell);
                                        bool allOwnerSegments = true;
                                        for (int segmentIndex = 0; segmentIndex + 1 < ownerPoints.Count; segmentIndex++)
                                        {
                                            var segment = new JObject { ["segmentIndex"] = segmentIndex,
                                                ["startCell"] = new JArray(ownerPoints[segmentIndex].x, ownerPoints[segmentIndex].y),
                                                ["goalCell"] = new JArray(ownerPoints[segmentIndex + 1].x, ownerPoints[segmentIndex + 1].y),
                                                ["checkpointSource"] = "IndependentComponentBfsTree_UnauthoredCandidate" };
                                            ownerSegments.Add(segment);
                                            QueryPath(em, gridEntity, grid, surface, prefab, ownerPoints[segmentIndex],
                                                ownerPoints[segmentIndex + 1], segment, 1);
                                            bool inside = PathInsideAlias(alias, grid, (JArray)segment["pathCells"]);
                                            segment["entirePathInsideAliasFrame"] = inside;
                                            segment["exactOwnerSegmentPassed"] = (string)segment["status"] == "NativeQueryFoundExactGoal" &&
                                                (int?)segment["failureCode"] == 0 && inside;
                                            allOwnerSegments &= (bool)segment["exactOwnerSegmentPassed"];
                                        }
                                        row["allComponentCheckpointOwnerSegmentsPassed"] = allOwnerSegments;
                                        row["componentCheckpointOwnerSegmentMeaning"] = "Per-segment original owner result at unchanged caps; success is not route binding, full mission fit, occupancy, or readiness";

                                        // A second deterministic static tree is only a candidate generator.
                                        // One extra checkpoint subdivides the failed vehicle segment in r28;
                                        // production pathfinder expansion and search limits stay unchanged.
                                        var alternate = new JObject { ["candidateOnly"] = true,
                                            ["routeProof"] = false, ["routeBindingExists"] = false,
                                            ["roadSemanticsUsed"] = false, ["liveOccupancyIncluded"] = false,
                                            ["dynamicBlockersIncluded"] = false,
                                            ["checkpointCountCap"] = 6, ["ownerSegmentCountCapPerCandidate"] = 7,
                                            ["ownerSegmentCountCapBothCandidates"] = 13,
                                            ["provenance"] = "IndependentReverseNeighborComponentBfsTree_UnauthoredCandidate" };
                                        row["alternateStaticTreeCandidate"] = alternate;
                                        if (TryBuildS026SourceComponents(alias, grid, walkable, surface, footprint, vehicle,
                                            out var altIds, out var altParents, out var altSizes, out int altValid, true, preferredSeed) &&
                                            altIds[playerCell.y * grid.Width + playerCell.x] > 0 &&
                                            altIds[playerCell.y * grid.Width + playerCell.x] == altIds[enemyCell.y * grid.Width + enemyCell.x])
                                        {
                                            var altPath = ReconstructS026ComponentPath(altParents, grid.Width, playerCell, enemyCell, altIds.Length);
                                            var altPoints = new List<int2> { playerCell };
                                            var altBaseCheckpoints = new List<int2>();
                                            if (altPath.Count > 1)
                                            {
                                                for (int part = 1; part < 6; part++)
                                                {
                                                    var point = altPath[(int)Math.Round((altPath.Count - 1) * (part / 6.0))];
                                                    if (!point.Equals(playerCell) && !point.Equals(enemyCell) &&
                                                        (altBaseCheckpoints.Count == 0 || !altBaseCheckpoints[altBaseCheckpoints.Count - 1].Equals(point)))
                                                        altBaseCheckpoints.Add(point);
                                                }
                                                foreach (double fraction in new[] { 1.0 / 6, 2.0 / 6, 3.0 / 6, 7.0 / 12, 4.0 / 6, 5.0 / 6 })
                                                {
                                                    var point = altPath[(int)Math.Round((altPath.Count - 1) * fraction)];
                                                    if (!point.Equals(playerCell) && !point.Equals(enemyCell) && !altPoints[altPoints.Count - 1].Equals(point))
                                                        altPoints.Add(point);
                                                }
                                            }
                                            altPoints.Add(enemyCell);
                                            bool different = altPath.Count > 1 && !altBaseCheckpoints.SequenceEqual(candidateCheckpoints);
                                            alternate["validAliasCenters"] = altValid;
                                            alternate["componentCount"] = altSizes.Count;
                                            alternate["modelPathCellCount"] = altPath.Count;
                                            alternate["checkpointSequenceDiffers"] = different;
                                            alternate["checkpointSequenceComparison"] = "Five original fractions before 7/12 subdivision; not proof of distinct approaches";
                                            alternate["extraCheckpointInserted"] = altPoints.Count - 2 > altBaseCheckpoints.Count;
                                            alternate["distinctApproachValidated"] = false;
                                            var altChecks = new JArray(); alternate["ownerSegmentChecks"] = altChecks;
                                            bool allAlt = different;
                                            if (different)
                                                for (int index = 0; index + 1 < altPoints.Count; index++)
                                                {
                                                    var segment = new JObject { ["segmentIndex"] = index,
                                                        ["startCell"] = new JArray(altPoints[index].x, altPoints[index].y),
                                                        ["goalCell"] = new JArray(altPoints[index + 1].x, altPoints[index + 1].y) };
                                                    altChecks.Add(segment);
                                                    QueryPath(em, gridEntity, grid, surface, prefab, altPoints[index], altPoints[index + 1], segment, 1);
                                                    bool inside = PathInsideAlias(alias, grid, (JArray)segment["pathCells"]);
                                                    segment["entirePathInsideAliasFrame"] = inside;
                                                    segment["exactOwnerSegmentPassed"] = (string)segment["status"] == "NativeQueryFoundExactGoal" &&
                                                        (int?)segment["failureCode"] == 0 && inside;
                                                    allAlt &= (bool)segment["exactOwnerSegmentPassed"];
                                                }
                                            alternate["allCandidateSegmentsOwnerPassed"] = allAlt;
                                            alternate["status"] = different ? "OwnerSegmentsCollected_Unqualified" : "SameCheckpointSequence_Unqualified";
                                        }
                                        else alternate["status"] = "AlternateStaticModelUnavailable_Unqualified";
                                    }
                                }

                                var candidates = new JArray(); row["existingSpawnCandidateComponentIds"] = candidates;
                                foreach (JObject placement in placements)
                                {
                                    if ((string)placement["roleId"] != roleId) continue;
                                    int faction = (int)placement["faction"];
                                    var cells = new JArray(); candidates.Add(new JObject { ["faction"] = faction,
                                        ["side"] = (string)placement["side"], ["candidateCells"] = cells });
                                    foreach (JObject candidateCell in (JArray)placement["candidateCells"])
                                    {
                                        int x = (int)candidateCell["cellX"], z = (int)candidateCell["cellZ"];
                                        int component = GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)
                                            ? componentIds[z * grid.Width + x] : 0;
                                        cells.Add(new JObject { ["cell"] = new JArray(x, z), ["componentId"] = component,
                                            ["candidateOnly"] = true, ["normalSpawnAcceptance"] = false });
                                    }
                                }
                                var alternativeRows = new JArray(); row["boundedAlternativeSpawnPairQueries"] = alternativeRows;
                                var playerPlacements = placements.OfType<JObject>().Where(p => (string)p["roleId"] == roleId &&
                                    (int)p["faction"] == 1).ToArray();
                                var enemyPlacements = placements.OfType<JObject>().Where(p => (string)p["roleId"] == roleId &&
                                    (int)p["faction"] == 2).ToArray();
                                int attemptedPairs = 0;
                                foreach (JObject playerPlacement in playerPlacements)
                                foreach (JObject playerCandidate in (JArray)playerPlacement["candidateCells"])
                                foreach (JObject enemyPlacement in enemyPlacements)
                                foreach (JObject enemyCandidate in (JArray)enemyPlacement["candidateCells"])
                                {
                                    if (attemptedPairs >= 8) break;
                                    var start = new int2((int)playerCandidate["cellX"], (int)playerCandidate["cellZ"]);
                                    var goal = new int2((int)enemyCandidate["cellX"], (int)enemyCandidate["cellZ"]);
                                    int startComponent = componentIds[start.y * grid.Width + start.x];
                                    int goalComponent = componentIds[goal.y * grid.Width + goal.x];
                                    if (startComponent <= 0 || startComponent != goalComponent) continue;
                                    var attempt = new JObject { ["candidateOnly"] = true,
                                        ["normalSpawnAcceptance"] = false, ["sourceFitCertified"] = false,
                                        ["startCell"] = new JArray(start.x, start.y), ["goalCell"] = new JArray(goal.x, goal.y),
                                        ["independentModelComponentId"] = startComponent,
                                        ["sameModeledComponent"] = true,
                                        ["scope"] = "OriginalPathfindBatchJob_QueryPath_UnchangedExpansionLimit_NoDynamicBlockersOrLiveOccupancy" };
                                    alternativeRows.Add(attempt);
                                    QueryPath(em, gridEntity, grid, surface, prefab, start, goal, attempt, 1);
                                    bool inside = PathInsideAlias(alias, grid, (JArray)attempt["pathCells"]);
                                    attempt["entirePathInsideAliasFrame"] = inside;
                                    attempt["exactOwnerPathFoundInsideAlias"] = (string)attempt["status"] == "NativeQueryFoundExactGoal" &&
                                        (int?)attempt["failureCode"] == 0 && inside;
                                    attemptedPairs++;
                                }
                                row["boundedAlternativePairQueryCount"] = attemptedPairs;
                                row["alternativePairQueryCap"] = 8;
                                row["alternativePairQueryMeaning"] = "Owner success proves only this exact static candidate path; failure does not disprove modeled/global connectivity";
                                row["status"] = hasPlayer && hasEnemy
                                    ? (playerComponent > 0 && playerComponent == enemyComponent
                                        ? "SelectedEndpointsShareIndependentStaticComponent_Unqualified"
                                        : "SelectedEndpointsInDifferentOrUnknownIndependentStaticComponents_Unqualified")
                                    : "SelectedEndpointMissing_Unqualified";
                            }
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=SourceConnectivityComponentsComplete count=" + connectivityRows.Count);

                            // Recheck the exact same tank-derived interior waypoints for each role.
                            // Per-role successes above used different points and cannot be combined
                            // into a shared mission route without this cross-role evidence.
                            var crossRole = new JObject { ["candidateOnly"] = true,
                                ["routeBindingExists"] = false, ["routeProof"] = false,
                                ["distinctApproachValidated"] = false, ["roadSemanticsUsed"] = false,
                                ["dynamicBlockersIncluded"] = false, ["liveOccupancyIncluded"] = false,
                                ["productionPathLimitsChanged"] = false, ["ownerQueryCountCap"] = 39,
                                ["source"] = "TankDeploymentRootedStaticTreeCheckpoints_UnAuthoredCandidates" };
                            report["sharedTankWaypointCrossRoleCandidates"] = crossRole;
                            var crossRows = new JArray(); crossRole["candidateRoles"] = crossRows;
                            var tankConnectivity = connectivityRows.OfType<JObject>().FirstOrDefault(r => (string)r["roleId"] == SkirmishRoleIds.Tank);
                            var tankPrimary = tankConnectivity?["independentModelCheckpointCells"] as JArray;
                            var tankAltSegments = tankConnectivity?["alternateStaticTreeCandidate"]?["ownerSegmentChecks"] as JArray;
                            if (tankPrimary != null && tankPrimary.Count == 5 && tankAltSegments != null && tankAltSegments.Count == 7 &&
                                (bool?)tankConnectivity["alternateStaticTreeCandidate"]?["allCandidateSegmentsOwnerPassed"] == true)
                            {
                                var tankAlternate = new JArray(tankAltSegments.OfType<JObject>().Take(tankAltSegments.Count - 1)
                                    .Select(s => s["goalCell"].DeepClone()));
                                int crossQueryCount = 0;
                                foreach (string crossRoleId in new[] { SkirmishRoleIds.Rifle, SkirmishRoleIds.Tank, SkirmishRoleIds.ApcArmored })
                                {
                                    if (!roleCatalog.TryGet(crossRoleId, out var crossBinding) ||
                                        !firstCellByFactionRole.TryGetValue("1/" + crossRoleId, out var crossStart) ||
                                        !firstCellByFactionRole.TryGetValue("2/" + crossRoleId, out var crossGoal))
                                        throw new InvalidOperationException("Exact role and spawn cells required for cross-role candidate");
                                    var crossPrefab = FindNativePrefab(em, crossBinding.RuntimePrefabKey);
                                    Require(crossPrefab != Entity.Null, "Exact native cross-role prefab required");
                                    foreach (var candidate in new[] { (Name: "TankPrimary", Waypoints: tankPrimary),
                                        (Name: "TankAlternate", Waypoints: tankAlternate) })
                                    {
                                        var points = new List<int2> { crossStart };
                                        foreach (JArray waypoint in candidate.Waypoints)
                                            points.Add(new int2((int)waypoint[0], (int)waypoint[1]));
                                        points.Add(crossGoal);
                                        var checks = new JArray();
                                        var candidateRow = new JObject { ["roleId"] = crossRoleId, ["candidate"] = candidate.Name,
                                            ["sharedInteriorWaypoints"] = candidate.Waypoints.DeepClone(), ["ownerSegmentChecks"] = checks,
                                            ["candidateOnly"] = true, ["routeProof"] = false };
                                        crossRows.Add(candidateRow);
                                        bool allPassed = true;
                                        for (int index = 0; index + 1 < points.Count; index++)
                                        {
                                            var segment = new JObject { ["segmentIndex"] = index,
                                                ["startCell"] = new JArray(points[index].x, points[index].y),
                                                ["goalCell"] = new JArray(points[index + 1].x, points[index + 1].y) };
                                            checks.Add(segment);
                                            QueryPath(em, gridEntity, grid, surface, crossPrefab, points[index], points[index + 1], segment, 1);
                                            crossQueryCount++;
                                            bool inside = PathInsideAlias(alias, grid, (JArray)segment["pathCells"]);
                                            segment["entirePathInsideAliasFrame"] = inside;
                                            segment["exactOwnerSegmentPassed"] = (string)segment["status"] == "NativeQueryFoundExactGoal" &&
                                                (int?)segment["failureCode"] == 0 && inside;
                                            allPassed &= (bool)segment["exactOwnerSegmentPassed"];
                                        }
                                        candidateRow["allSegmentsOwnerPassed"] = allPassed;
                                    }
                                }
                                crossRole["ownerQueryCount"] = crossQueryCount;
                                crossRole["status"] = "CrossRoleSegmentsCollected_Unqualified";
                            }
                            else crossRole["status"] = "TankCandidateUnavailable_Unqualified";

                            var routeRows = new JArray(); report["rolePathCandidates"] = routeRows;
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=BeforeRolePathQueries");
                            foreach (string roleId in new[] { SkirmishRoleIds.Rifle, SkirmishRoleIds.Tank, SkirmishRoleIds.ApcArmored })
                            {
                                var route = new JObject { ["roleId"] = roleId,
                                    ["scope"] = "ExistingPathfindBatchJob_FromActualSpawnOwnerCells_WholePathCheckedAgainstAliasBounds",
                                    ["independentRouteProof"] = false, ["missionRouteBinding"] = false,
                                    ["sourceFitCertified"] = false };
                                routeRows.Add(route);
                                if (!firstCellByFactionRole.TryGetValue("1/" + roleId, out var from) ||
                                    !firstCellByFactionRole.TryGetValue("2/" + roleId, out var to))
                                { route["status"] = "NoOwnerFoundEndpointCell"; continue; }
                                Require(roleCatalog.TryGet(roleId, out var binding), "Exact native role binding required for route query");
                                var prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                                Require(prefab != Entity.Null && em.HasComponent<UnitFootprint>(prefab) &&
                                    em.HasComponent<UnitMovementBehavior>(prefab) && !em.HasComponent<UnitAirMovement>(prefab),
                                    "Exact native ground route prefab required");
                                QueryPath(em, gridEntity, grid, surface, prefab, from, to, route, 1);
                                route["startCell"] = new JArray(from.x, from.y); route["goalCell"] = new JArray(to.x, to.y);
                                route["entirePathInsideAliasFrame"] = PathInsideAlias(alias, grid, (JArray)route["pathCells"]);
                                if ((string)route["status"] == "NativeQueryFoundExactGoal" && (bool)route["entirePathInsideAliasFrame"])
                                    route["status"] = "NativePathFoundInsideAlias_UnqualifiedCandidate";
                                else if ((string)route["status"] == "NativeQueryFoundExactGoal") route["status"] = "NativePathEscapedAliasFrame";
                            }
                            var roadCheckpointRows = new JArray(); report["sourceRoadCheckpointDiagnostics"] = roadCheckpointRows;
                            report["routeWaypointsAuthoredByAliasOrCampaignDefinition"] = aliasLaneAnchorCount + campaignLaneAnchorCount > 0;
                            report["sourceRoadCheckpointScope"] = "EphemeralNearestWalkableSourceGridRoadSidewalkOrDirtCellsAtChordFractions_OneThirdAndTwoThirds_NoMissionBinding";
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=BeforeSourceRoadCheckpointQueries");
                            foreach (string roleId in new[] { SkirmishRoleIds.Rifle, SkirmishRoleIds.Tank, SkirmishRoleIds.ApcArmored })
                            {
                                var candidate = new JObject
                                {
                                    ["roleId"] = roleId,
                                    ["scope"] = "OriginalPathfindBatchJobSegmentQueries_ExistingCampaignRasterCellsOnly",
                                    ["checkpointsAuthored"] = false,
                                    ["routeBindingExists"] = false,
                                    ["routeProof"] = false,
                                    ["sourceFitCertified"] = false,
                                    ["normalMissionAcceptance"] = false,
                                    ["limitsChanged"] = false
                                };
                                roadCheckpointRows.Add(candidate);
                                if (!firstCellByFactionRole.TryGetValue("1/" + roleId, out var from) ||
                                    !firstCellByFactionRole.TryGetValue("2/" + roleId, out var to))
                                { candidate["status"] = "NoOwnerFoundEndpointCell"; continue; }
                                Require(roleCatalog.TryGet(roleId, out var binding), "Exact native role binding required for road checkpoint query");
                                var prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                                Require(prefab != Entity.Null && em.HasComponent<UnitFootprint>(prefab) &&
                                    em.HasComponent<UnitMovementBehavior>(prefab) && !em.HasComponent<UnitAirMovement>(prefab),
                                    "Exact native ground prefab required for road checkpoint query");
                                var footprint = em.GetComponentData<UnitFootprint>(prefab).Size;
                                var nativeFootprint = em.GetComponentData<UnitFootprint>(prefab);
                                bool vehicle = UnitVehicleMovementUtility.IsVehicle(nativeFootprint,
                                    em.GetComponentData<UnitMovementBehavior>(prefab));
                                candidate["nativeExpansionLimitPerSegment"] = GridPathSearchKernel.ResolveExpansionLimit(vehicle,
                                    true, false, true, 0);
                                candidate["nativeSearchBoundsPaddingCells"] = GridPathSearchKernel.ResolveSearchPadding(vehicle,
                                    false, true, 0);
                                candidate["ownerRequestMode"] = "ManualMove=true_CheapSegmentMode=0_SameOriginalPathfindBatchJobAdapter";
                                var excluded = new HashSet<int> { from.y * grid.Width + from.x, to.y * grid.Width + to.x };
                                if (!TryFindNearestSourceRoadCheckpoint(alias, grid, walkable, roads, sidewalks, dirtRoads,
                                    surface, footprint, vehicle, from, to, 1f / 3f, excluded,
                                    out var checkpointOne, out string roadKindOne, out float distanceOne))
                                {
                                    candidate["status"] = "NoFirstStaticGridRoadMaskCheckpoint_GameplayProjectionExcluded";
                                    continue;
                                }
                                excluded.Add(checkpointOne.y * grid.Width + checkpointOne.x);
                                if (!TryFindNearestSourceRoadCheckpoint(alias, grid, walkable, roads, sidewalks, dirtRoads,
                                    surface, footprint, vehicle, from, to, 2f / 3f, excluded,
                                    out var checkpointTwo, out string roadKindTwo, out float distanceTwo))
                                {
                                    candidate["status"] = "NoDistinctTraversableSourceRoadCheckpoints";
                                    candidate["routeProof"] = false;
                                    continue;
                                }
                                candidate["waypoints"] = new JArray
                                {
                                    new JObject { ["cell"] = new JArray(checkpointOne.x, checkpointOne.y), ["sourceLayer"] = roadKindOne,
                                        ["targetChordFraction"] = 1.0 / 3.0, ["distanceFromChordTargetCells"] = distanceOne },
                                    new JObject { ["cell"] = new JArray(checkpointTwo.x, checkpointTwo.y), ["sourceLayer"] = roadKindTwo,
                                        ["targetChordFraction"] = 2.0 / 3.0, ["distanceFromChordTargetCells"] = distanceTwo }
                                };
                                var points = new[] { from, checkpointOne, checkpointTwo, to };
                                var segments = new JArray(); candidate["segments"] = segments;
                                bool allSegmentsFound = true;
                                for (int segmentIndex = 0; segmentIndex < points.Length - 1; segmentIndex++)
                                {
                                    var segment = new JObject { ["segmentIndex"] = segmentIndex,
                                        ["startCell"] = new JArray(points[segmentIndex].x, points[segmentIndex].y),
                                        ["goalCell"] = new JArray(points[segmentIndex + 1].x, points[segmentIndex + 1].y),
                                        ["sourceRoadCheckpointOwnerQuery"] = true };
                                    segments.Add(segment);
                                    QueryPath(em, gridEntity, grid, surface, prefab, points[segmentIndex], points[segmentIndex + 1], segment, 1);
                                    bool inside = PathInsideAlias(alias, grid, (JArray)segment["pathCells"]);
                                    segment["entirePathInsideAliasFrame"] = inside;
                                    bool segmentFound = (string)segment["status"] == "NativeQueryFoundExactGoal" &&
                                        (int?)segment["failureCode"] == 0 && inside;
                                    segment["exactOwnerSegmentPassed"] = segmentFound;
                                    allSegmentsFound &= segmentFound;
                                }
                                candidate["status"] = allSegmentsFound
                                    ? "AllRasterCheckpointSegmentsOwnerFoundExactGoalInsideAlias_UnqualifiedCandidate"
                                    : "AtLeastOneRasterCheckpointSegmentFailed_Unqualified";
                                candidate["allSegmentOwnerQueriesPassed"] = allSegmentsFound;
                                candidate["routeProof"] = false;
                            }
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=SourceRoadCheckpointQueriesComplete count=" + roadCheckpointRows.Count);
                            var walkabilityCheckpointRows = new JArray(); report["sourceWalkabilityCheckpointDiagnostics"] = walkabilityCheckpointRows;
                            report["sourceWalkabilityCheckpointScope"] = "EphemeralNearestWalkableAndSurfaceTraversableSourceGridCellsAtChordFractions_OneThirdAndTwoThirds_NoRoadSemantics_NoMissionBinding";
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=BeforeSourceWalkabilityCheckpointQueries");
                            foreach (string roleId in new[] { SkirmishRoleIds.Rifle, SkirmishRoleIds.Tank, SkirmishRoleIds.ApcArmored })
                            {
                                var candidate = new JObject
                                {
                                    ["roleId"] = roleId,
                                    ["scope"] = "OriginalPathfindBatchJobSegments_ActualCampaignGridWalkableAndMapSurfaceOnly",
                                    ["checkpointAuthored"] = false,
                                    ["roadSemanticsUsed"] = false,
                                    ["routeBindingExists"] = false,
                                    ["routeProof"] = false,
                                    ["sourceFitCertified"] = false,
                                    ["normalMissionAcceptance"] = false,
                                    ["limitsChanged"] = false
                                };
                                walkabilityCheckpointRows.Add(candidate);
                                if (!firstCellByFactionRole.TryGetValue("1/" + roleId, out var from) ||
                                    !firstCellByFactionRole.TryGetValue("2/" + roleId, out var to))
                                { candidate["status"] = "NoOwnerFoundEndpointCell"; continue; }
                                Require(roleCatalog.TryGet(roleId, out var binding), "Exact native role binding required for walkability checkpoint query");
                                var prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                                Require(prefab != Entity.Null && em.HasComponent<UnitFootprint>(prefab) &&
                                    em.HasComponent<UnitMovementBehavior>(prefab) && !em.HasComponent<UnitAirMovement>(prefab),
                                    "Exact native ground prefab required for walkability checkpoint query");
                                var nativeFootprint = em.GetComponentData<UnitFootprint>(prefab);
                                int2 footprint = nativeFootprint.Size;
                                bool vehicle = UnitVehicleMovementUtility.IsVehicle(nativeFootprint,
                                    em.GetComponentData<UnitMovementBehavior>(prefab));
                                candidate["nativeExpansionLimitPerSegment"] = GridPathSearchKernel.ResolveExpansionLimit(vehicle,
                                    true, false, true, 0);
                                candidate["nativeSearchBoundsPaddingCells"] = GridPathSearchKernel.ResolveSearchPadding(vehicle,
                                    false, true, 0);
                                candidate["ownerRequestMode"] = "ManualMove=true_CheapSegmentMode=0_SameOriginalPathfindBatchJobAdapter";
                                var excluded = new HashSet<int> { from.y * grid.Width + from.x, to.y * grid.Width + to.x };
                                if (!TryFindNearestSourceWalkabilityCheckpoint(alias, grid, walkable, surface, footprint, vehicle,
                                    from, to, 1f / 3f, excluded, out var checkpointOne, out int shellOne, out float distanceOne))
                                { candidate["status"] = "NoFirstWalkableSurfaceTraversableCheckpoint"; continue; }
                                excluded.Add(checkpointOne.y * grid.Width + checkpointOne.x);
                                if (!TryFindNearestSourceWalkabilityCheckpoint(alias, grid, walkable, surface, footprint, vehicle,
                                    from, to, 2f / 3f, excluded, out var checkpointTwo, out int shellTwo, out float distanceTwo))
                                { candidate["status"] = "NoDistinctSecondWalkableSurfaceTraversableCheckpoint"; continue; }
                                candidate["waypoints"] = new JArray
                                {
                                    new JObject { ["cell"] = new JArray(checkpointOne.x, checkpointOne.y),
                                        ["targetChordFraction"] = 1.0 / 3.0, ["searchShellCells"] = shellOne,
                                        ["distanceFromChordTargetCells"] = distanceOne },
                                    new JObject { ["cell"] = new JArray(checkpointTwo.x, checkpointTwo.y),
                                        ["targetChordFraction"] = 2.0 / 3.0, ["searchShellCells"] = shellTwo,
                                        ["distanceFromChordTargetCells"] = distanceTwo }
                                };
                                var points = new[] { from, checkpointOne, checkpointTwo, to };
                                var segments = new JArray(); candidate["segments"] = segments;
                                bool allSegmentsFound = true;
                                for (int segmentIndex = 0; segmentIndex < points.Length - 1; segmentIndex++)
                                {
                                    var segment = new JObject { ["segmentIndex"] = segmentIndex,
                                        ["startCell"] = new JArray(points[segmentIndex].x, points[segmentIndex].y),
                                        ["goalCell"] = new JArray(points[segmentIndex + 1].x, points[segmentIndex + 1].y),
                                        ["sourceWalkabilityCheckpointOwnerQuery"] = true };
                                    segments.Add(segment);
                                    QueryPath(em, gridEntity, grid, surface, prefab, points[segmentIndex], points[segmentIndex + 1], segment, 1);
                                    bool inside = PathInsideAlias(alias, grid, (JArray)segment["pathCells"]);
                                    segment["entirePathInsideAliasFrame"] = inside;
                                    bool segmentFound = (string)segment["status"] == "NativeQueryFoundExactGoal" &&
                                        (int?)segment["failureCode"] == 0 && inside;
                                    segment["exactOwnerSegmentPassed"] = segmentFound;
                                    allSegmentsFound &= segmentFound;
                                }
                                candidate["status"] = allSegmentsFound
                                    ? "AllWalkabilityCheckpointSegmentsOwnerFoundExactGoalInsideAlias_UnqualifiedCandidate"
                                    : "AtLeastOneWalkabilityCheckpointSegmentFailed_Unqualified";
                                candidate["allSegmentOwnerQueriesPassed"] = allSegmentsFound;
                                candidate["routeProof"] = false;
                            }
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=SourceWalkabilityCheckpointQueriesComplete count=" + walkabilityCheckpointRows.Count);
                            report["sourceAxisAndFlankLaneCandidates"] = RunS026AxisAndFlankLaneCandidates(
                                alias, em, gridEntity, grid, surface, walkable, roleCatalog, firstCellByFactionRole);
                            report["candidateStatus"] = "SourceOwnerQueriesCompleted_NoLayoutOrAcceptanceCreated";
                            report["sourceStructurePlacementCandidates"] = RunS026SourceStructurePlacementCandidates(alias,
                                anchors, em, gridEntity, grid, surface, em.GetBuffer<GridRoad>(gridEntity, true));
                            var blockedCrossRole = new JObject { ["candidateOnly"] = true, ["size"] = "Standard",
                                ["routeProof"] = false, ["routeBindingExists"] = false, ["sourceFitCertified"] = false,
                                ["liveOccupancyIncluded"] = false, ["dynamicBlockersIncluded"] = false,
                                ["roadSemanticsUsed"] = false, ["productionPathLimitsChanged"] = false,
                                ["scope"] = "OriginalPathOwner_WithExactCandidateFacilityRectanglesAsStaticBlockers",
                                ["crossRoleOwnerQueryCountCap"] = 39, ["totalOwnerQueryCountCap"] = 73 };
                            report["sharedTankWaypointsWithCandidateFacilityBlockers"] = blockedCrossRole;
                            var standardRows = (JArray)report["sourceStructurePlacementCandidates"]?["constrainedFirstMiddleBandProbe"]?["sizes"];
                            var standardSize = standardRows?.OfType<JObject>().FirstOrDefault(s => (string)s["size"] == "Standard");
                            var selectedAttempt = ((JArray)standardSize?["pairAttempts"])?.OfType<JObject>().FirstOrDefault(a =>
                                (bool?)a["allPlannedStructuresRevalidatedInCanonicalOrder"] == true &&
                                (bool?)a["completeLogicalInstanceBindingProjection"]?["tryApplyPassed"] == true &&
                                (bool?)a["completeLogicalInstanceBindingProjection"]?["exactCoordinatesPreserved"] == true &&
                                (bool?)a["completeLogicalInstanceBindingProjection"]?["allOffsetsZero"] == true);
                            var plans = selectedAttempt?["planByCompilerIndex"] as JObject;
                            if (plans != null && plans.Count == 20 && crossRows.Count == 6)
                            {
                                using var facilityBlockers = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                                int blockedCells = 0;
                                foreach (var plan in plans.Properties())
                                {
                                    var rect = (JObject)plan.Value["resolvedEffectiveRect"];
                                    Require((bool?)plan.Value["foundByOriginalOwner"] == true && rect != null,
                                        "Complete original-owner candidate facility rectangles required");
                                    int x0 = (int)rect["x"], z0 = (int)rect["z"], width = (int)rect["width"], depth = (int)rect["depth"];
                                    Require(x0 >= 0 && z0 >= 0 && width > 0 && depth > 0 && x0 + width <= grid.Width && z0 + depth <= grid.Height,
                                        "Candidate facility rectangle outside native grid");
                                    for (int z = z0; z < z0 + depth; z++)
                                        for (int x = x0; x < x0 + width; x++)
                                        { int index = z * grid.Width + x; if (!facilityBlockers.IsSet(index)) { facilityBlockers.Set(index, true); blockedCells++; } }
                                }
                                blockedCrossRole["candidateFacilityCount"] = plans.Count;
                                blockedCrossRole["blockedCellCount"] = blockedCells;
                                blockedCrossRole["selectedPlayerRoot"] = selectedAttempt["playerRoot"].DeepClone();
                                blockedCrossRole["selectedEnemyRoot"] = selectedAttempt["enemyRoot"].DeepClone();
                                blockedCrossRole["typedPadHypotheses"] = RunS026TypedPadHypotheses(alias, anchors, em,
                                    gridEntity, grid, surface, plans);
                                // The typed probe creates and destroys a temporary match entity.
                                // Reacquire the buffer view after that structural change.
                                walkable = em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray();
                                // Re-run the exact Standard roster allocator with fresh reservations
                                // containing both source-static and candidate-facility footprints.
                                // This is a bounded diagnostic, not a normal mission spawn claim.
                                var spawnWithFacilities = new JObject { ["candidateOnly"] = true,
                                    ["normalSpawnAcceptance"] = false, ["sourceFitCertified"] = false,
                                    ["liveOccupancyIncluded"] = false, ["dynamicBlockersIncluded"] = false,
                                    ["scope"] = "OriginalInitialUnitSpawnOwner_StandardRoster_SourceStaticPlusCandidateFacilityReservations",
                                    ["randomSeed"] = 1, ["searchRadiusCells"] = 12 };
                                blockedCrossRole["standardRosterSpawnWithCandidateFacilities"] = spawnWithFacilities;
                                var spawnRows = new JArray(); spawnWithFacilities["roles"] = spawnRows;
                                var spawnReservations = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                                try
                                {
                                    InitialUnitsSpawnSystem.ReserveStaticBlockerFootprints(em, ref spawnReservations, grid);
                                    for (int i = 0; i < grid.Width * grid.Height; i++)
                                        if (facilityBlockers.IsSet(i)) spawnReservations.Set(i, true);
                                    var spawnRandom = new Unity.Mathematics.Random(1);
                                    int requestedTotal = 0, foundTotal = 0, surfaceRejected = 0;
                                    foreach (byte faction in new byte[] { 1, 2 })
                                    foreach (var force in forces)
                                    {
                                        int quantity = faction == 1 ? force.PlayerCount : force.EnemyCount;
                                        if (quantity <= 0) continue;
                                        requestedTotal += quantity;
                                        var spawnRow = new JObject { ["faction"] = faction, ["roleId"] = force.RoleId,
                                            ["requestedCount"] = quantity, ["candidateOnly"] = true };
                                        spawnRows.Add(spawnRow);
                                        Require(roleCatalog.TryGet(force.RoleId, out var binding), "Exact candidate spawn role required");
                                        var prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                                        Require(prefab != Entity.Null && em.HasComponent<UnitFootprint>(prefab), "Exact candidate spawn prefab required");
                                        var footprint = em.GetComponentData<UnitFootprint>(prefab).Size;
                                        bool air = em.HasComponent<UnitAirMovement>(prefab);
                                        bool vehicle = em.HasComponent<UnitMovementBehavior>(prefab) &&
                                            em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion != 0;
                                        var anchor = anchors.Single(a => a.FactionId == faction);
                                        int2 center = GridUtils.WorldToCell(grid, new float3(anchor.Position.x, 0, anchor.Position.z));
                                        var cells = new JArray(); spawnRow["candidateCells"] = cells;
                                        int found = 0;
                                        var validation = new MapSurfaceTraversalValidation();
                                        for (int member = 0; member < quantity; member++)
                                        {
                                            if (!InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref spawnRandom, grid, walkable, blocked,
                                                occupied, ref spawnReservations, center, 12, footprint, air, out var cell)) break;
                                            var min = UnitFootprintUtility.GetMinCell(cell, footprint);
                                            bool inside = IsFootprintInsideAlias(alias, grid, min, footprint);
                                            bool passable = inside && (air || validation.CanTraverseFootprint(surface, surface.HasSurfaceData,
                                                grid, cell, footprint, vehicle));
                                            cells.Add(new JObject { ["member"] = member, ["cellX"] = cell.x, ["cellZ"] = cell.y,
                                                ["footprintMinX"] = min.x, ["footprintMinZ"] = min.y,
                                                ["insideAliasFrame"] = inside, ["sourceSurfaceTraversable"] = passable });
                                            if (!inside || !passable) { surfaceRejected++; break; }
                                            found++; foundTotal++;
                                        }
                                        spawnRow["foundCount"] = found;
                                        spawnRow["nativeFootprint"] = new JArray(footprint.x, footprint.y);
                                    }
                                    spawnWithFacilities["requestedActors"] = requestedTotal;
                                    spawnWithFacilities["foundActors"] = foundTotal;
                                    spawnWithFacilities["outsideOrSurfaceRejected"] = surfaceRejected;
                                    spawnWithFacilities["allRequestedCandidateCellsFound"] = requestedTotal == foundTotal && surfaceRejected == 0;
                                    spawnWithFacilities["status"] = "CandidateFacilityReservedSpawnQueriesCollected_Unqualified";
                                }
                                finally { spawnReservations.Dispose(); }
                                var blockedRows = new JArray(); blockedCrossRole["candidateRoles"] = blockedRows;
                                int queryCount = 0;
                                foreach (JObject sourceRow in crossRows)
                                {
                                    string roleId = (string)sourceRow["roleId"];
                                    Require(roleCatalog.TryGet(roleId, out var binding), "Exact blocker-query role required");
                                    var prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                                    Require(prefab != Entity.Null, "Exact blocker-query prefab required");
                                    var sourceChecks = (JArray)sourceRow["ownerSegmentChecks"];
                                    var checks = new JArray();
                                    var target = new JObject { ["roleId"] = roleId, ["candidate"] = sourceRow["candidate"].DeepClone(),
                                        ["candidateOnly"] = true, ["ownerSegmentChecks"] = checks };
                                    blockedRows.Add(target);
                                    bool allPassed = true;
                                    foreach (JObject sourceCheck in sourceChecks)
                                    {
                                        var from = (JArray)sourceCheck["startCell"]; var to = (JArray)sourceCheck["goalCell"];
                                        var row = new JObject { ["segmentIndex"] = sourceCheck["segmentIndex"].DeepClone(),
                                            ["startCell"] = from.DeepClone(), ["goalCell"] = to.DeepClone() };
                                        checks.Add(row);
                                        QueryPath(em, gridEntity, grid, surface, prefab, new int2((int)from[0], (int)from[1]),
                                            new int2((int)to[0], (int)to[1]), row, 1, facilityBlockers);
                                        queryCount++;
                                        bool inside = PathInsideAlias(alias, grid, (JArray)row["pathCells"]);
                                        row["entirePathInsideAliasFrame"] = inside;
                                        row["exactOwnerSegmentPassed"] = (string)row["status"] == "NativeQueryFoundExactGoal" &&
                                            (int?)row["failureCode"] == 0 && inside;
                                        allPassed &= (bool)row["exactOwnerSegmentPassed"];
                                    }
                                    target["allSegmentsOwnerPassed"] = allPassed;
                                }
                                blockedCrossRole["crossRoleOwnerQueryCount"] = queryCount;
                                Require(queryCount <= 39, "Cross-role query cap exceeded");
                                // Bounded retry only for the observed rifle/alternate first-leg
                                // expansion-limit failure: use its original owner's partial end
                                // as one ephemeral subdivision, without changing production limits.
                                var retry = new JObject { ["candidateOnly"] = true, ["routeProof"] = false,
                                    ["liveOccupancyIncluded"] = false, ["productionPathLimitsChanged"] = false,
                                    ["ownerQueryCountCap"] = 2,
                                    ["scope"] = "RifleAlternateFirstLeg_OriginalOwnerPartialEndpointSubdivision_WithSameCandidateFacilityBlockers" };
                                blockedCrossRole["rifleAlternateFirstLegSubdivision"] = retry;
                                var rifleAlternate = blockedRows.OfType<JObject>().FirstOrDefault(r =>
                                    (string)r["roleId"] == SkirmishRoleIds.Rifle && (string)r["candidate"] == "TankAlternate");
                                var firstLeg = ((JArray)rifleAlternate?["ownerSegmentChecks"])?.OfType<JObject>().FirstOrDefault();
                                var partial = firstLeg?["pathCells"] as JArray;
                                if ((bool?)firstLeg?["exactOwnerSegmentPassed"] == false && (int?)firstLeg?["failureCode"] == 9 && partial != null && partial.Count > 1 &&
                                    roleCatalog.TryGet(SkirmishRoleIds.Rifle, out var rifleBinding))
                                {
                                    var a = (JArray)firstLeg["startCell"]; var b = (JArray)partial[partial.Count - 1]; var c = (JArray)firstLeg["goalCell"];
                                    var points = new[] { new int2((int)a[0], (int)a[1]), new int2((int)b[0], (int)b[1]), new int2((int)c[0], (int)c[1]) };
                                    var partialStart = (JArray)partial[0];
                                    Require((int)partialStart[0] == points[0].x && (int)partialStart[1] == points[0].y,
                                        "Owner partial path must start at requested cell");
                                    retry["originalFailureCode"] = firstLeg["failureCode"].DeepClone();
                                    retry["originalExpansions"] = firstLeg["expansions"].DeepClone();
                                    retry["partialEndpoint"] = b.DeepClone();
                                    var checks = new JArray(); retry["ownerSegmentChecks"] = checks;
                                    bool allPassed = !points[0].Equals(points[1]) && !points[1].Equals(points[2]);
                                    var riflePrefab = FindNativePrefab(em, rifleBinding.RuntimePrefabKey);
                                    Require(riflePrefab != Entity.Null, "Exact native rifle prefab required for subdivision");
                                    if (allPassed)
                                        for (int i = 0; i < 2; i++)
                                        {
                                            var row = new JObject { ["segmentIndex"] = i,
                                                ["startCell"] = new JArray(points[i].x, points[i].y), ["goalCell"] = new JArray(points[i + 1].x, points[i + 1].y) };
                                            checks.Add(row);
                                            QueryPath(em, gridEntity, grid, surface, riflePrefab, points[i], points[i + 1], row, 1, facilityBlockers);
                                            bool inside = PathInsideAlias(alias, grid, (JArray)row["pathCells"]);
                                            row["entirePathInsideAliasFrame"] = inside;
                                            row["exactOwnerSegmentPassed"] = (string)row["status"] == "NativeQueryFoundExactGoal" &&
                                                (int?)row["failureCode"] == 0 && inside;
                                            allPassed &= (bool)row["exactOwnerSegmentPassed"];
                                        }
                                    retry["ownerQueryCount"] = checks.Count;
                                    retry["allSegmentsOwnerPassed"] = allPassed;
                                    retry["status"] = checks.Count == 0 ? "DegeneratePartialEndpoint_NoQueries" : "OwnerPartialEndpointSubdivisionCollected_Unqualified";
                                    var second = checks.Count == 2 ? (JObject)checks[1] : null;
                                    var secondPartial = second?["pathCells"] as JArray;
                                    var secondRetry = new JObject { ["candidateOnly"] = true, ["routeProof"] = false,
                                        ["ownerQueryCountCap"] = 2, ["productionPathLimitsChanged"] = false,
                                        ["scope"] = "SecondFailedLegOriginalOwnerPartialEndpointSubdivision_SameCandidateFacilityBlockers" };
                                    retry["secondSubdivision"] = secondRetry;
                                    if ((bool?)second?["exactOwnerSegmentPassed"] == false && (int?)second?["failureCode"] == 9 &&
                                        secondPartial != null && secondPartial.Count > 1)
                                    {
                                        var secondStart = (JArray)secondPartial[0]; var midpoint = (JArray)secondPartial[secondPartial.Count - 1];
                                        Require((int)secondStart[0] == points[1].x && (int)secondStart[1] == points[1].y,
                                            "Second owner partial path must start at requested cell");
                                        var morePoints = new[] { points[1], new int2((int)midpoint[0], (int)midpoint[1]), points[2] };
                                        secondRetry["partialEndpoint"] = midpoint.DeepClone();
                                        secondRetry["originalFailureCode"] = second["failureCode"].DeepClone();
                                        secondRetry["originalExpansions"] = second["expansions"].DeepClone();
                                        var moreChecks = new JArray(); secondRetry["ownerSegmentChecks"] = moreChecks;
                                        bool morePassed = !morePoints[0].Equals(morePoints[1]) && !morePoints[1].Equals(morePoints[2]);
                                        if (morePassed)
                                            for (int i = 0; i < 2; i++)
                                            {
                                                var row = new JObject { ["segmentIndex"] = i,
                                                    ["startCell"] = new JArray(morePoints[i].x, morePoints[i].y),
                                                    ["goalCell"] = new JArray(morePoints[i + 1].x, morePoints[i + 1].y) };
                                                moreChecks.Add(row);
                                                QueryPath(em, gridEntity, grid, surface, riflePrefab, morePoints[i], morePoints[i + 1], row, 1, facilityBlockers);
                                                bool inside = PathInsideAlias(alias, grid, (JArray)row["pathCells"]);
                                                row["entirePathInsideAliasFrame"] = inside;
                                                row["exactOwnerSegmentPassed"] = (string)row["status"] == "NativeQueryFoundExactGoal" &&
                                                    (int?)row["failureCode"] == 0 && inside;
                                                morePassed &= (bool)row["exactOwnerSegmentPassed"];
                                            }
                                        secondRetry["ownerQueryCount"] = moreChecks.Count;
                                        secondRetry["allSegmentsOwnerPassed"] = morePassed;
                                        secondRetry["status"] = moreChecks.Count == 0 ? "DegeneratePartialEndpoint_NoQueries" : "SecondOwnerPartialEndpointSubdivisionCollected_Unqualified";
                                        var vehicleChecks = new JArray(); secondRetry["vehicleSharedPointChecks"] = vehicleChecks;
                                        if (checks.Count == 2 && (bool?)checks[0]?["exactOwnerSegmentPassed"] == true && morePassed)
                                            foreach (string vehicleRole in new[] { SkirmishRoleIds.Tank, SkirmishRoleIds.ApcArmored })
                                            {
                                                bool hasVehicleBinding = roleCatalog.TryGet(vehicleRole, out var vehicleBinding);
                                                bool hasVehicleStart = firstCellByFactionRole.TryGetValue("1/" + vehicleRole, out var vehicleStart);
                                                Require(hasVehicleBinding && hasVehicleStart, "Vehicle prefab and source spawn required for shared refinement");
                                                var vehiclePrefab = FindNativePrefab(em, vehicleBinding.RuntimePrefabKey);
                                                Require(vehiclePrefab != Entity.Null, "Exact vehicle prefab required for shared refinement");
                                                var vehiclePoints = new[] { vehicleStart, points[1], morePoints[1], points[2] };
                                                var segments = new JArray();
                                                var vehicleRow = new JObject { ["roleId"] = vehicleRole, ["candidateOnly"] = true,
                                                    ["routeProof"] = false, ["ownerSegmentChecks"] = segments };
                                                vehicleChecks.Add(vehicleRow);
                                                bool vehiclePassed = true;
                                                for (int i = 0; i + 1 < vehiclePoints.Length; i++)
                                                {
                                                    var row = new JObject { ["segmentIndex"] = i,
                                                        ["startCell"] = new JArray(vehiclePoints[i].x, vehiclePoints[i].y),
                                                        ["goalCell"] = new JArray(vehiclePoints[i + 1].x, vehiclePoints[i + 1].y) };
                                                    segments.Add(row);
                                                    QueryPath(em, gridEntity, grid, surface, vehiclePrefab, vehiclePoints[i], vehiclePoints[i + 1], row, 1, facilityBlockers);
                                                    bool inside = PathInsideAlias(alias, grid, (JArray)row["pathCells"]);
                                                    row["entirePathInsideAliasFrame"] = inside;
                                                    row["exactOwnerSegmentPassed"] = (string)row["status"] == "NativeQueryFoundExactGoal" &&
                                                        (int?)row["failureCode"] == 0 && inside;
                                                    vehiclePassed &= (bool)row["exactOwnerSegmentPassed"];
                                                }
                                                vehicleRow["allSegmentsOwnerPassed"] = vehiclePassed;
                                            }
                                        secondRetry["vehicleOwnerQueryCount"] = vehicleChecks.OfType<JObject>().Sum(r => ((JArray)r["ownerSegmentChecks"]).Count);
                                        secondRetry["vehicleOwnerQueryCountCap"] = 6;
                                        // Probe nearby shared points only after the rifle-derived point
                                        // fails a vehicle footprint. These are transient candidates, not
                                        // route anchors or claims of road/corridor width.
                                        var nearby = new JObject { ["candidateOnly"] = true, ["routeProof"] = false,
                                            ["sourceFitCertified"] = false, ["liveOccupancyIncluded"] = false,
                                            ["roadSemanticsUsed"] = false, ["productionPathLimitsChanged"] = false,
                                            ["candidateCountCap"] = 4, ["ownerQueryCountCap"] = 24,
                                            ["scope"] = "OffsetsAroundRiflePartialEndpoint_SameCandidateFacilityBlockers_AllThreeNativeRoles" };
                                        secondRetry["nearbySharedPointProbe"] = nearby;
                                        var nearbyRows = new JArray(); nearby["candidates"] = nearbyRows;
                                        int nearbyQueryCount = 0;
                                        if (vehicleChecks.Count == 2 && vehicleChecks.OfType<JObject>().Any(v => (bool?)v["allSegmentsOwnerPassed"] == false))
                                            foreach (var offset in new[] { new int2(0, 3), new int2(3, 3), new int2(-3, 3), new int2(0, 5) })
                                            {
                                                var candidatePoint = morePoints[1] + offset;
                                                var candidateRow = new JObject { ["cell"] = new JArray(candidatePoint.x, candidatePoint.y),
                                                    ["offsetFromPartialCell"] = new JArray(offset.x, offset.y), ["candidateOnly"] = true };
                                                nearbyRows.Add(candidateRow);
                                                var roleRows = new JArray(); candidateRow["roles"] = roleRows;
                                                bool allRolesPassed = true;
                                                foreach (string candidateRole in new[] { SkirmishRoleIds.Rifle, SkirmishRoleIds.Tank, SkirmishRoleIds.ApcArmored })
                                                {
                                                    Require(roleCatalog.TryGet(candidateRole, out var candidateBinding), "Candidate role required");
                                                    var candidatePrefab = FindNativePrefab(em, candidateBinding.RuntimePrefabKey);
                                                    Require(candidatePrefab != Entity.Null, "Exact candidate prefab required");
                                                    var candidatePoints = new[] { points[1], candidatePoint, points[2] };
                                                    var candidateSegments = new JArray();
                                                    var roleRow = new JObject { ["roleId"] = candidateRole, ["ownerSegmentChecks"] = candidateSegments };
                                                    roleRows.Add(roleRow);
                                                    bool rolePassed = true;
                                                    for (int i = 0; i < 2; i++)
                                                    {
                                                        var row = new JObject { ["segmentIndex"] = i,
                                                            ["startCell"] = new JArray(candidatePoints[i].x, candidatePoints[i].y),
                                                            ["goalCell"] = new JArray(candidatePoints[i + 1].x, candidatePoints[i + 1].y) };
                                                        candidateSegments.Add(row);
                                                        QueryPath(em, gridEntity, grid, surface, candidatePrefab, candidatePoints[i], candidatePoints[i + 1], row, 1, facilityBlockers);
                                                        nearbyQueryCount++;
                                                        bool inside = PathInsideAlias(alias, grid, (JArray)row["pathCells"]);
                                                        row["entirePathInsideAliasFrame"] = inside;
                                                        row["exactOwnerSegmentPassed"] = (string)row["status"] == "NativeQueryFoundExactGoal" &&
                                                            (int?)row["failureCode"] == 0 && inside;
                                                        rolePassed &= (bool)row["exactOwnerSegmentPassed"];
                                                    }
                                                    roleRow["allSegmentsOwnerPassed"] = rolePassed;
                                                    allRolesPassed &= rolePassed;
                                                }
                                                candidateRow["allRolesOwnerPassed"] = allRolesPassed;
                                            }
                                        nearby["ownerQueryCount"] = nearbyQueryCount;
                                        nearby["status"] = nearbyRows.Count > 0 ? "NearbySharedCandidatesCollected_Unqualified" : "NoVehicleFailureToRefine_Unqualified";
                                    }
                                    else { secondRetry["ownerQueryCount"] = 0; secondRetry["status"] = "NoSecondExpansionLimitPartialPath_Unqualified"; }
                                }
                                else retry["status"] = "ExpectedExpansionLimitPartialPathUnavailable_Unqualified";
                                int retryCount = (int?)retry["ownerQueryCount"] ?? 0;
                                int secondRetryCount = (int?)retry["secondSubdivision"]?["ownerQueryCount"] ?? 0;
                                int vehicleRetryCount = (int?)retry["secondSubdivision"]?["vehicleOwnerQueryCount"] ?? 0;
                                int nearbyCount = (int?)retry["secondSubdivision"]?["nearbySharedPointProbe"]?["ownerQueryCount"] ?? 0;
                                blockedCrossRole["totalOwnerQueryCount"] = queryCount + retryCount + secondRetryCount + vehicleRetryCount + nearbyCount;
                                Require(vehicleRetryCount <= 6 && nearbyCount <= 24 && queryCount + retryCount + secondRetryCount + vehicleRetryCount + nearbyCount <= 73,
                                    "Total candidate blocker query cap exceeded");
                                blockedCrossRole["status"] = "CandidateFacilityBlockedSegmentsCollected_Unqualified";
                            }
                            else blockedCrossRole["status"] = "CompleteStandardFacilityOrSharedWaypointCandidateUnavailable_Unqualified";
                            report["spawnCandidatesUseExistingAliasAnchorsOnly"] = true;
                            report["routeCandidateCount"] = routeRows.Count;
                            report["sourceRoadCheckpointCandidateCount"] = roadCheckpointRows.Count;
                            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=RolePathQueriesComplete count=" + routeRows.Count);
                            report["newCampaignGeometryCreated"] = false;
                            report["budgetChanged"] = false;
                            report["fullMapOrAllSizeFitMeasured"] = false;
                            report["result"] = "CollectedUnqualified";
                        }
                        finally { reserved.Dispose(); }
                    }
                }
                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=StreamingWorldDisposed");
                RecordPreviewOwnership("AfterStreamingWorldDisposed");
                var companionCleanupBefore = PreviewOwnershipSnapshot();
                bool companionCleanupAttempted = false;
                string companionCleanupOutcome = "NotApplicable";
                try
                {
                    var baselinePreview = (JObject)report["cleanupBaseline"]["previewOwnership"];
                    if (TryValidateS026CompanionCleanupPlan(baselinePreview, companionCleanupBefore,
                        Environment.GetCommandLineArgs().Contains("-quit"), out string planError))
                    {
                        Require(Hash(CompanionSceneUtilityPath) == CompanionSceneUtilitySha,
                            "Pinned Unity Entities companion-scene owner implementation drifted");
                        var ownerType = AppDomain.CurrentDomain.GetAssemblies()
                            .Select(a => a.GetType("Unity.Entities.CompanionGameObjectUtility", false))
                            .FirstOrDefault(t => t != null);
                        Require(ownerType != null, "Exact Unity.Entities companion-scene owner type required");
                        var ownerCallback = ownerType.GetMethod("AssemblyReloadEventsOnbeforeAssemblyReload",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                            null, Type.EmptyTypes, null);
                        Require(ownerCallback != null && ownerCallback.ReturnType == typeof(void),
                            "Pinned companion-scene owner lifecycle callback unavailable");
                        companionCleanupAttempted = true;
                        ownerCallback.Invoke(null, null);
                        var companionCleanupAfter = PreviewOwnershipSnapshot();
                        Require(TryValidateS026CompanionCleanupPostcondition(baselinePreview, companionCleanupAfter,
                            out string postconditionError), postconditionError);
                        companionCleanupOutcome = "OwnerCallbackClosedOnlyTwoNewCompanionScenes_PostconditionPassed";
                        report["companionSceneOwnerCleanup"] = new JObject
                        {
                            ["ownerType"] = "Unity.Entities.CompanionGameObjectUtility",
                            ["ownerCallback"] = "AssemblyReloadEventsOnbeforeAssemblyReload",
                            ["ownerSourcePath"] = CompanionSceneUtilityPath,
                            ["ownerSourceSha256"] = CompanionSceneUtilitySha,
                            ["packageVersion"] = "6.5.0",
                            ["callbackSourceBehavior"] = "Closes only _companionScene and _companionSceneLiveConversion via EditorSceneManager.ClosePreviewScene",
                            ["wrapperQuitRequired"] = true,
                            ["wrapperQuitPresent"] = true,
                            ["precondition"] = planError,
                            ["postcondition"] = postconditionError,
                            ["after"] = companionCleanupAfter
                        };
                        RecordPreviewOwnership("AfterOwnerSpecificCompanionCleanup");
                    }
                    else companionCleanupOutcome = "NotAttempted_" + planError;
                }
                catch (Exception cleanupFailure)
                {
                    companionCleanupOutcome = "Failed_" + cleanupFailure.GetType().Name + ":" + cleanupFailure.Message;
                    report["companionSceneOwnerCleanupError"] = cleanupFailure.ToString();
                }
                report["companionSceneOwnerCleanupAttempted"] = companionCleanupAttempted;
                report["companionSceneOwnerCleanupOutcome"] = companionCleanupOutcome;
                Require(!EditorUtility.IsDirty(alias) && !EditorUtility.IsDirty(campaign) &&
                    Hash(S026AliasPath) == S026AliasSha && Hash(S026CampaignSourcePath) == S026CampaignSourceSha,
                    "Alias or Campaign source bytes changed during source-only query");
            }
            catch (Exception failure)
            {
                report["result"] = "Failed";
                report["error"] = failure.ToString();
                throw;
            }
            finally
            {
                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] stage=FinalizingReceipt");
                TryFinalizeS026CompanionCleanup(report);
                var worldAfter = CaptureMaterializationWorldCensus();
                bool worldsSame = worldBaseline.SequenceEqual(worldAfter);
                bool defaultWorldSame = ReferenceEquals(defaultWorldBaseline, World.DefaultGameObjectInjectionWorld);
                int sceneCountAfter = UnityEngine.SceneManagement.SceneManager.sceneCount;
                int previewCountAfter = UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
                bool sceneCountSame = sceneCountBaseline == sceneCountAfter;
                bool previewCountSame = previewCountBaseline == previewCountAfter;
                bool cleanup = worldsSame && defaultWorldSame && sceneCountSame && previewCountSame;
                report["cleanupDetails"] = new JObject
                {
                    ["worldSequenceNumbersBefore"] = new JArray(worldBaseline),
                    ["worldSequenceNumbersAfter"] = new JArray(worldAfter),
                    ["worldCensusSame"] = worldsSame,
                    ["defaultWorldSame"] = defaultWorldSame,
                    ["sceneCountBefore"] = sceneCountBaseline,
                    ["sceneCountAfter"] = sceneCountAfter,
                    ["sceneCountSame"] = sceneCountSame,
                    ["previewCountBefore"] = previewCountBaseline,
                    ["previewCountAfter"] = previewCountAfter,
                    ["previewCountSame"] = previewCountSame
                };
                report["cleanupDetails"]["previewOwnershipAfter"] = PreviewOwnershipSnapshot();
                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] cleanup worldCensusSame=" + worldsSame +
                    " defaultWorldSame=" + defaultWorldSame + " sceneCount=" + sceneCountBaseline + "->" + sceneCountAfter +
                    " previewCount=" + previewCountBaseline + "->" + previewCountAfter);
                bool sourceUnchanged = Hash(S026AliasPath) == S026AliasSha && Hash(S026CampaignSourcePath) == S026CampaignSourceSha;
                report["cleanupVerified"] = cleanup;
                report["sourceAssetsUnmodified"] = sourceUnchanged;
                if (!cleanup || !sourceUnchanged)
                {
                    report["result"] = "FailedCleanupOrSourceMutation";
                    report["cleanupError"] = !cleanup ? "World/default/scene/preview census changed" : "Alias or Campaign source bytes drifted";
                }
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(file, new System.Text.UTF8Encoding(false)))
                { writer.Write(report.ToString(Formatting.Indented)); writer.Flush(); file.Flush(true); }
            }
            Require((bool?)report["cleanupVerified"] == true && (bool?)report["sourceAssetsUnmodified"] == true,
                "Source diagnostic cleanup or immutable input check failed");
            Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] result=CollectedUnqualified sourceFitCertified=0 allSizeCapacityProven=0 output=" + output);

            void reportCleanupBaseline()
            {
                report["cleanupBaseline"] = new JObject
                {
                    ["worldSequenceNumbers"] = new JArray(worldBaseline),
                    ["defaultWorldPresent"] = defaultWorldBaseline != null,
                    ["sceneCount"] = sceneCountBaseline,
                    ["previewCount"] = previewCountBaseline,
                    ["previewOwnership"] = PreviewOwnershipSnapshot()
                };
            }

            void RecordPreviewOwnership(string phase)
            {
                var snapshot = PreviewOwnershipSnapshot();
                previewPhases.Add(new JObject { ["phase"] = phase, ["snapshot"] = snapshot });
                Debug.Log("[S026CityCrossroadsAliasSourceDiagnostic] previewOwnership phase=" + phase +
                    " actual=" + snapshot["actualPreviewCount"] + " measured=" + snapshot["measuredPreviewCount"] +
                    " complete=" + snapshot["previewCensusComplete"]);
            }

            JObject PreviewOwnershipSnapshot()
                => S026PreviewOwnershipSnapshot();
        }

        static int RunS026CompanionCleanupControls()
        {
            var baseline = S026CompanionCleanupSnapshot(false, false, "0", "0", false);
            var current = S026CompanionCleanupSnapshot(true, true, "scene-companion", "scene-live", true);
            Require(TryValidateS026CompanionCleanupPlan(baseline, current, true, out _),
                "Exact newly-owned companion cleanup plan fixture rejected");
            Require(!TryValidateS026CompanionCleanupPlan(baseline, current, false, out _),
                "Companion cleanup must reject a non-quitting Editor");
            var preowned = S026CompanionCleanupSnapshot(true, false, "preexisting", "0", true);
            Require(!TryValidateS026CompanionCleanupPlan(preowned, current, true, out _),
                "Companion cleanup must reject a preexisting owner scene");
            var wrongOwner = current.DeepClone() as JObject;
            ((JObject)((JArray)wrongOwner["companionOwners"])[0])["handle"] = "unrelated";
            Require(!TryValidateS026CompanionCleanupPlan(baseline, wrongOwner, true, out _),
                "Companion cleanup must reject an owner handle mismatch");
            var extraPreview = current.DeepClone() as JObject;
            ((JArray)extraPreview["scenes"]).Add(new JObject { ["handle"] = "unrelated", ["name"] = "UnownedPreview",
                ["preview"] = true, ["root_count"] = 0, ["loaded"] = true });
            extraPreview["actualPreviewCount"] = 4; extraPreview["measuredPreviewCount"] = 4;
            Require(!TryValidateS026CompanionCleanupPlan(baseline, extraPreview, true, out _),
                "Companion cleanup must reject any unrelated added preview");
            var missingOwner = current.DeepClone() as JObject;
            ((JObject)((JArray)missingOwner["companionOwners"])[1])["valid"] = false;
            Require(!TryValidateS026CompanionCleanupPlan(baseline, missingOwner, true, out _),
                "Companion cleanup must reject an absent owner scene");
            return 6;
        }

        static JObject S026CompanionCleanupSnapshot(bool companionValid, bool liveValid,
            string companionHandle, string liveHandle, bool includeCompanions)
        {
            var scenes = new JArray
            {
                new JObject { ["handle"] = "custom-lights", ["name"] = "CustomLightsScene-SceneView-Test",
                    ["preview"] = true, ["root_count"] = 3, ["loaded"] = true },
                new JObject { ["handle"] = "menu", ["name"] = "Menu", ["preview"] = false,
                    ["root_count"] = 2, ["loaded"] = true }
            };
            if (includeCompanions)
            {
                scenes.Add(new JObject { ["handle"] = companionHandle, ["name"] = "CompanionScene",
                    ["preview"] = true, ["root_count"] = 0, ["loaded"] = true });
                scenes.Add(new JObject { ["handle"] = liveHandle, ["name"] = "CompanionSceneLiveConversion - (Game View Only)",
                    ["preview"] = true, ["root_count"] = 0, ["loaded"] = true });
            }
            return new JObject
            {
                ["actualPreviewCount"] = includeCompanions ? 3 : 1,
                ["measuredPreviewCount"] = includeCompanions ? 3 : 1,
                ["previewCensusComplete"] = true,
                ["scenes"] = scenes,
                ["companionOwners"] = new JArray
                {
                    new JObject { ["owner_type"] = "Unity.Entities.CompanionGameObjectUtility",
                        ["owner_field"] = "_companionScene", ["valid"] = companionValid, ["handle"] = companionHandle },
                    new JObject { ["owner_type"] = "Unity.Entities.CompanionGameObjectUtility",
                        ["owner_field"] = "_companionSceneLiveConversion", ["valid"] = liveValid, ["handle"] = liveHandle }
                }
            };
        }

        static bool TryValidateS026CompanionCleanupPlan(JObject baseline, JObject current, bool wrapperQuit, out string reason)
        {
            reason = "";
            if (!wrapperQuit) { reason = "Requires this wrapper-owned -quit Editor process"; return false; }
            if (baseline == null || current == null || (bool?)baseline["previewCensusComplete"] != true ||
                (bool?)current["previewCensusComplete"] != true) { reason = "Complete before/after preview census required"; return false; }
            if ((int?)current["actualPreviewCount"] != (int?)baseline["actualPreviewCount"] + 2 ||
                (int?)current["measuredPreviewCount"] != (int?)baseline["measuredPreviewCount"] + 2)
            { reason = "Exactly two owner-created preview scenes required"; return false; }
            var beforeOwners = baseline["companionOwners"] as JArray;
            var afterOwners = current["companionOwners"] as JArray;
            var beforeCompanion = beforeOwners?.OfType<JObject>().SingleOrDefault(o => (string)o["owner_field"] == "_companionScene");
            var beforeLive = beforeOwners?.OfType<JObject>().SingleOrDefault(o => (string)o["owner_field"] == "_companionSceneLiveConversion");
            var afterCompanion = afterOwners?.OfType<JObject>().SingleOrDefault(o => (string)o["owner_field"] == "_companionScene");
            var afterLive = afterOwners?.OfType<JObject>().SingleOrDefault(o => (string)o["owner_field"] == "_companionSceneLiveConversion");
            if (beforeCompanion == null || beforeLive == null || (bool?)beforeCompanion["valid"] != false || (bool?)beforeLive["valid"] != false)
            { reason = "Both companion owner fields must be invalid at baseline"; return false; }
            if (afterCompanion == null || afterLive == null || (string)afterCompanion["owner_type"] != "Unity.Entities.CompanionGameObjectUtility" ||
                (string)afterLive["owner_type"] != "Unity.Entities.CompanionGameObjectUtility" ||
                (bool?)afterCompanion["valid"] != true || (bool?)afterLive["valid"] != true)
            { reason = "Exact Unity.Entities companion owner handles required"; return false; }
            string companionHandle = (string)afterCompanion["handle"], liveHandle = (string)afterLive["handle"];
            var beforeScenes = baseline["scenes"] as JArray; var afterScenes = current["scenes"] as JArray;
            if (beforeScenes == null || afterScenes == null) { reason = "Scene identity rows missing"; return false; }
            var beforeHandles = new HashSet<string>(beforeScenes.OfType<JObject>().Select(s => (string)s["handle"]), StringComparer.Ordinal);
            foreach (var prior in beforeScenes.OfType<JObject>())
            {
                var matching = afterScenes.OfType<JObject>().SingleOrDefault(s => (string)s["handle"] == (string)prior["handle"]);
                if (matching == null || !JToken.DeepEquals(prior, matching))
                { reason = "Preexisting Editor scene changed or disappeared"; return false; }
            }
            var added = afterScenes.OfType<JObject>().Where(s => !beforeHandles.Contains((string)s["handle"])).ToArray();
            if (added.Length != 2) { reason = "Unexpected number of new scene identities"; return false; }
            var companionScene = added.SingleOrDefault(s => (string)s["handle"] == companionHandle);
            var liveScene = added.SingleOrDefault(s => (string)s["handle"] == liveHandle);
            if (companionScene == null || liveScene == null || (string)companionScene["name"] != "CompanionScene" ||
                !((string)liveScene["name"]).StartsWith("CompanionSceneLiveConversion", StringComparison.Ordinal) ||
                (bool?)companionScene["preview"] != true || (bool?)liveScene["preview"] != true ||
                (bool?)companionScene["loaded"] != true || (bool?)liveScene["loaded"] != true ||
                (int?)companionScene["root_count"] != 0 || (int?)liveScene["root_count"] != 0)
            { reason = "New scenes are not exactly the two empty owner-managed companion previews"; return false; }
            reason = "Baseline-invalid owner fields created exactly two empty matching preview scenes; wrapper will quit after this call";
            return true;
        }

        static bool TryValidateS026CompanionCleanupPostcondition(JObject baseline, JObject after, out string reason)
        {
            reason = "";
            if (baseline == null || after == null || (bool?)after["previewCensusComplete"] != true ||
                (int?)after["actualPreviewCount"] != (int?)baseline["actualPreviewCount"] ||
                (int?)after["measuredPreviewCount"] != (int?)baseline["measuredPreviewCount"])
            { reason = "Owner cleanup did not restore exact preview counts"; return false; }
            var beforeScenes = baseline["scenes"] as JArray; var afterScenes = after["scenes"] as JArray;
            if (beforeScenes == null || afterScenes == null || beforeScenes.Count != afterScenes.Count)
            { reason = "Owner cleanup did not restore the exact scene identity count"; return false; }
            foreach (var prior in beforeScenes.OfType<JObject>())
            {
                var matching = afterScenes.OfType<JObject>().SingleOrDefault(s => (string)s["handle"] == (string)prior["handle"]);
                if (matching == null || !JToken.DeepEquals(prior, matching))
                { reason = "Owner cleanup changed a preexisting Editor scene"; return false; }
            }
            var owners = after["companionOwners"] as JArray;
            if (owners == null || owners.OfType<JObject>().Any(o =>
                ((string)o["owner_field"] == "_companionScene" || (string)o["owner_field"] == "_companionSceneLiveConversion") &&
                (bool?)o["valid"] == true))
            { reason = "Closed companion scenes remain valid in package owner fields"; return false; }
            reason = "Exact baseline scene set and invalid companion owner fields restored";
            return true;
        }

        static SkirmishRoleCatalogConfig LoadS026AliasRoleCatalog()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Require(catalog != null && !EditorUtility.IsDirty(catalog), "Clean canonical expansion catalog required");
            var roles = catalog.ToAuthoredSet().Roles;
            Require(roles != null && !EditorUtility.IsDirty(roles), "Clean actual canonical role catalog required");
            return roles;
        }

        static List<S026AliasRoleRequest> S026AliasStandardForceRequests(SkirmishSetupMatrixRow r)
        {
            Require(r.CatalogId == "S026" && r.SizeId == SkirmishSizeId.Standard && r.ArmyProfile == SkirmishArmyProfileId.GroundManeuver &&
                r.StartProfile == SkirmishStartPackageId.EstablishedBase && r.ObjectiveKind == SkirmishObjectiveKind.BaseAssault,
                "Exact canonical S026 Standard matrix row required");
            int playerSupport = r.PlayerLogisticsSupport, enemySupport = r.EnemyLogisticsSupport;
            return new List<S026AliasRoleRequest>
            {
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.Rifle,Role="rifle",PlayerCount=r.PlayerRifle,EnemyCount=r.EnemyRifle},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.Gunner,Role="gunner",PlayerCount=r.PlayerGunner,EnemyCount=r.EnemyGunner},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.Rocketeer,Role="rocketeer",PlayerCount=r.PlayerRocketeer,EnemyCount=r.EnemyRocketeer},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.Car,Role="car",PlayerCount=r.PlayerCar,EnemyCount=r.EnemyCar},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.ApcArmored,Role="apc",PlayerCount=r.PlayerApc,EnemyCount=r.EnemyApc},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.Tank,Role="tank",PlayerCount=r.PlayerTank,EnemyCount=r.EnemyTank},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.AntiAir,Role="aa",PlayerCount=r.PlayerAa,EnemyCount=r.EnemyAa},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.TransportHeli,Role="transport_heli",PlayerCount=r.PlayerTransportHeli,EnemyCount=r.EnemyTransportHeli},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.Tanker,Role="tanker",PlayerCount=Math.Min(1,playerSupport),EnemyCount=Math.Min(1,enemySupport)},
                new S026AliasRoleRequest{RoleId=SkirmishRoleIds.LogisticsTruck,Role="logistics_truck",PlayerCount=Math.Max(0,playerSupport-1),EnemyCount=Math.Max(0,enemySupport-1)}
            };
        }

        static bool MatrixMatchesS026Authority(SkirmishSetupMatrixRow r, JObject row)
        {
            bool Match(string key, int actual) => int.TryParse((string)row[key], out int expected) && actual == expected;
            return Match("player_rifle", r.PlayerRifle) && Match("player_gunner", r.PlayerGunner) &&
                Match("player_rocketeer", r.PlayerRocketeer) && Match("player_car", r.PlayerCar) &&
                Match("player_apc", r.PlayerApc) && Match("player_tank", r.PlayerTank) && Match("player_aa", r.PlayerAa) &&
                Match("player_transport_heli", r.PlayerTransportHeli) && Match("player_logistics_support", r.PlayerLogisticsSupport) &&
                Match("player_infantry", r.PlayerInfantry) && Match("player_ground", r.PlayerGround) &&
                Match("enemy_rifle", r.EnemyRifle) && Match("enemy_gunner", r.EnemyGunner) &&
                Match("enemy_rocketeer", r.EnemyRocketeer) && Match("enemy_car", r.EnemyCar) &&
                Match("enemy_apc", r.EnemyApc) && Match("enemy_tank", r.EnemyTank) && Match("enemy_aa", r.EnemyAa) &&
                Match("enemy_transport_heli", r.EnemyTransportHeli) && Match("enemy_logistics_support", r.EnemyLogisticsSupport) &&
                Match("enemy_infantry", r.EnemyInfantry) && Match("enemy_ground", r.EnemyGround);
        }

        static bool IsInsideAlias(OperationMapDefinition alias, float x, float z) =>
            x >= alias.Bounds.PlayableMin.x && x <= alias.Bounds.PlayableMax.x &&
            z >= alias.Bounds.PlayableMin.z && z <= alias.Bounds.PlayableMax.z;

        static bool IsFootprintInsideAlias(OperationMapDefinition alias, GridConfig grid, int2 min, int2 size)
        {
            for (int z = min.y; z < min.y + size.y; z++)
            for (int x = min.x; x < min.x + size.x; x++)
            {
                if (!GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)) return false;
                float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                if (!IsInsideAlias(alias, wx, wz)) return false;
            }
            return true;
        }

        static bool PathInsideAlias(OperationMapDefinition alias, GridConfig grid, JArray points)
        {
            if (points == null || points.Count == 0) return false;
            foreach (JArray point in points)
            {
                Require(point.Count == 2, "Native path point must remain an exact two-cell tuple");
                float wx = grid.Origin.x + ((int)point[0] + .5f) * grid.CellSize;
                float wz = grid.Origin.z + ((int)point[1] + .5f) * grid.CellSize;
                if (!IsInsideAlias(alias, wx, wz)) return false;
            }
            return true;
        }

        static bool TryFindNearestSourceRoadCheckpoint(OperationMapDefinition alias, GridConfig grid,
            NativeArray<GridWalkable> walkable, NativeArray<GridRoad> roads, NativeArray<GridRoadSidewalk> sidewalks,
            NativeArray<GridRoadDirt> dirtRoads, MapSurfaceComponent surface, int2 footprint, bool vehicle,
            int2 start, int2 end, float fraction, HashSet<int> excluded, out int2 checkpoint,
            out string sourceLayer, out float distanceFromTarget)
        {
            checkpoint = default; sourceLayer = ""; distanceFromTarget = 0;
            var validation = new MapSurfaceTraversalValidation();
            var validRoadCells = new List<int2>();
            for (int index = 0; index < walkable.Length; index++)
            {
                if (walkable[index].Value == 0 ||
                    roads[index].Value == 0 && sidewalks[index].Value == 0 && dirtRoads[index].Value == 0) continue;
                int x = index % grid.Width, z = index / grid.Width;
                if (excluded.Contains(index)) continue;
                var cell = new int2(x, z);
                var min = UnitFootprintUtility.GetMinCell(cell, footprint);
                if (!IsFootprintInsideAlias(alias, grid, min, footprint) ||
                    !validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, footprint, vehicle)) continue;
                validRoadCells.Add(cell);
            }
            if (!TrySelectNearestSourceCheckpoint(validRoadCells, grid.Width, new float2(start.x, start.y),
                new float2(end.x, end.y), fraction, excluded, out checkpoint, out float distanceSq)) return false;
            int selectedIndex = checkpoint.y * grid.Width + checkpoint.x;
            sourceLayer = roads[selectedIndex].Value != 0 ? "GridRoad" : sidewalks[selectedIndex].Value != 0 ? "GridRoadSidewalk" : "GridRoadDirt";
            distanceFromTarget = math.sqrt(distanceSq);
            return true;
        }

        static bool TryFindNearestSourceWalkabilityCheckpoint(OperationMapDefinition alias, GridConfig grid,
            NativeArray<GridWalkable> walkable, MapSurfaceComponent surface, int2 footprint, bool vehicle,
            int2 start, int2 end, float fraction, HashSet<int> excluded, out int2 checkpoint,
            out int searchShell, out float distanceFromTarget)
        {
            checkpoint = default; searchShell = -1; distanceFromTarget = 0;
            float2 target = math.lerp(new float2(start.x, start.y), new float2(end.x, end.y), fraction);
            int centerX = Mathf.RoundToInt(target.x), centerZ = Mathf.RoundToInt(target.y);
            int maxShell = Math.Max(grid.Width, grid.Height);
            var validation = new MapSurfaceTraversalValidation();
            for (int shell = 0; shell <= maxShell; shell++)
            {
                var candidates = new List<int2>();
                for (int z = centerZ - shell; z <= centerZ + shell; z++)
                for (int x = centerX - shell; x <= centerX + shell; x++)
                {
                    if (shell != 0 && Math.Abs(x - centerX) != shell && Math.Abs(z - centerZ) != shell) continue;
                    if (!GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)) continue;
                    int index = z * grid.Width + x;
                    if (walkable[index].Value == 0 || excluded.Contains(index)) continue;
                    var cell = new int2(x, z);
                    if (!IsFootprintInsideAlias(alias, grid, UnitFootprintUtility.GetMinCell(cell, footprint), footprint) ||
                        !validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, footprint, vehicle)) continue;
                    candidates.Add(cell);
                }
                if (candidates.Count == 0) continue;
                if (!TrySelectNearestSourceCheckpoint(candidates, grid.Width, new float2(start.x, start.y),
                    new float2(end.x, end.y), fraction, excluded, out checkpoint, out float distanceSq)) continue;
                searchShell = shell; distanceFromTarget = math.sqrt(distanceSq); return true;
            }
            return false;
        }

        static JObject RunS026AxisAndFlankLaneCandidates(OperationMapDefinition alias, EntityManager em,
            Entity gridEntity,
            GridConfig grid, MapSurfaceComponent surface, NativeArray<GridWalkable> walkable,
            SkirmishRoleCatalogConfig roleCatalog, IReadOnlyDictionary<string, int2> firstCellByFactionRole)
        {
            const int waypointShellCap = 8;
            const float flankOffsetMetres = 24f;
            string[] roles = { SkirmishRoleIds.Rifle, SkirmishRoleIds.Tank };
            string[] lanes = { "AxisCandidate", "FlankCandidate" };
            var result = new JObject
            {
                ["scope"] = "TemporaryAliasBoundAxisAndLateralFlankWaypoints_OriginalPathfindBatchJobSegments",
                ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                ["routeBindingExists"] = false, ["routeProof"] = false,
                ["normalMissionAcceptance"] = false, ["runtimeOccupancyIncluded"] = false,
                ["dynamicBlockersIncluded"] = false, ["roadSemanticsUsed"] = false,
                ["sourceWorldMinXZ"] = new JArray(alias.Bounds.WorldMin.x, alias.Bounds.WorldMin.z),
                ["sourceWorldMaxXZ"] = new JArray(alias.Bounds.WorldMax.x, alias.Bounds.WorldMax.z),
                ["aliasPlayableBounds"] = new JObject { ["minX"] = alias.Bounds.PlayableMin.x,
                    ["maxX"] = alias.Bounds.PlayableMax.x, ["minZ"] = alias.Bounds.PlayableMin.z,
                    ["maxZ"] = alias.Bounds.PlayableMax.z },
                ["waypointSearchShellCapCells"] = waypointShellCap,
                ["flankOffsetMetresInSourceDeploymentFrame"] = flankOffsetMetres,
                ["roles"] = new JArray(), ["ownerQueryCountCap"] = 24,
                ["sampling"] = "Actual source-owner spawn cells as endpoints; 1/3 and 2/3 chord targets with zero lateral offset or fixed 24m lateral offset; nearest walkable/surface-traversable cell within Chebyshev shell 8; original owner then checks all three segments in both directions",
                ["limitations"] = "Source-grid path candidates only; no gameplay roads, dynamic blockers, live occupancy, route binding, distinct-route validation, complete fit or readiness claim"
            };
            var traversal = new MapSurfaceTraversalValidation();
            int ownerQueryCount = 0;
            float2 lateral = default;
            // Use the exact source deployment axis when deriving the flank side.
            // Only orientation is used; the selected unit cells remain owner-found.
            var deployment = alias.Anchors.ToArray().Where(anchor => anchor.Kind == OperationMapAnchorKind.Deployment)
                .OrderBy(anchor => anchor.FactionId).ToArray();
            Require(deployment.Length == 2 && deployment[0].FactionId == 1 && deployment[1].FactionId == 2,
                "Exact two-faction source deployments required for axis/flank candidate frame");
            float2 forwardWorld = math.normalize(new float2(deployment[1].Position.x - deployment[0].Position.x,
                deployment[1].Position.z - deployment[0].Position.z));
            float2 acrossWorld = new float2(-forwardWorld.y, forwardWorld.x);
            lateral = acrossWorld;

            bool TryWaypoint(float2 target, int2 footprint, bool vehicle, HashSet<int> excluded,
                out int2 selected, out int selectedShell, out float distance)
            {
                selected = default; selectedShell = -1; distance = 0f;
                var bestDistance = float.PositiveInfinity;
                for (int shell = 0; shell <= waypointShellCap; shell++)
                {
                    bool foundInShell = false;
                    for (int z = Mathf.RoundToInt(target.y) - shell; z <= Mathf.RoundToInt(target.y) + shell; z++)
                    for (int x = Mathf.RoundToInt(target.x) - shell; x <= Mathf.RoundToInt(target.x) + shell; x++)
                    {
                        if (shell != 0 && Mathf.Abs(x - Mathf.RoundToInt(target.x)) != shell &&
                            Mathf.Abs(z - Mathf.RoundToInt(target.y)) != shell) continue;
                        var cell = new int2(x, z);
                        if (!GridUtils.InBounds(cell, grid.Width, grid.Height)) continue;
                        int index = z * grid.Width + x;
                        if (walkable[index].Value == 0 || excluded.Contains(index)) continue;
                        int2 min = UnitFootprintUtility.GetMinCell(cell, footprint);
                        if (!IsFootprintInsideAlias(alias, grid, min, footprint) ||
                            !traversal.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, footprint, vehicle)) continue;
                        float distanceSq = math.distancesq(new float2(x, z), target);
                        if (!foundInShell || distanceSq < bestDistance ||
                            (Mathf.Abs(distanceSq - bestDistance) < .0001f &&
                                (z < selected.y || z == selected.y && x < selected.x)))
                        {
                            foundInShell = true; bestDistance = distanceSq; selected = cell;
                        }
                    }
                    if (!foundInShell) continue;
                    selectedShell = shell; distance = math.sqrt(bestDistance); return true;
                }
                return false;
            }

            foreach (string roleId in roles)
            {
                Require(roleCatalog.TryGet(roleId, out var role), "Exact source role binding required for lane candidate " + roleId);
                Entity prefab = FindNativePrefab(em, role.RuntimePrefabKey);
                if (prefab == Entity.Null) { result["status"] = "ExactRolePrefabUnavailable"; return result; }
                if (!firstCellByFactionRole.TryGetValue("1/" + roleId, out var playerStart) ||
                    !firstCellByFactionRole.TryGetValue("2/" + roleId, out var enemyStart))
                {
                    ((JArray)result["roles"]).Add(new JObject { ["roleId"] = roleId,
                        ["status"] = "OwnerSpawnEndpointMissing", ["candidateOnly"] = true });
                    continue;
                }
                var nativeFootprint = em.GetComponentData<UnitFootprint>(prefab);
                var behavior = em.GetComponentData<UnitMovementBehavior>(prefab);
                int2 footprint = nativeFootprint.Size;
                bool vehicle = UnitVehicleMovementUtility.IsVehicle(nativeFootprint, behavior);
                int expansionLimit = GridPathSearchKernel.ResolveExpansionLimit(vehicle, true, false, true, 0);
                int padding = GridPathSearchKernel.ResolveSearchPadding(vehicle, false, true, 0);
                var roleRow = new JObject { ["roleId"] = roleId,
                    ["nativePrefabKey"] = role.RuntimePrefabKey, ["footprint"] = new JArray(footprint.x, footprint.y),
                    ["nativeExpansionLimitPerSegment"] = expansionLimit,
                    ["nativeSearchBoundsPaddingCells"] = padding, ["directions"] = new JArray() };
                ((JArray)result["roles"]).Add(roleRow);
                foreach (bool reverse in new[] { false, true })
                {
                    int2 from = reverse ? enemyStart : playerStart;
                    int2 to = reverse ? playerStart : enemyStart;
                    float2 chord = new float2(to.x - from.x, to.y - from.y);
                    float2 side = lateral / grid.CellSize;
                    var directionRow = new JObject { ["direction"] = reverse ? "Faction2ToFaction1" : "Faction1ToFaction2",
                        ["startCell"] = new JArray(from.x, from.y), ["goalCell"] = new JArray(to.x, to.y),
                        ["lanes"] = new JArray() };
                    ((JArray)roleRow["directions"]).Add(directionRow);
                    foreach (string lane in lanes)
                    {
                        float offset = lane == "FlankCandidate" ? flankOffsetMetres / grid.CellSize : 0f;
                        var excluded = new HashSet<int> { from.y * grid.Width + from.x, to.y * grid.Width + to.x };
                        var waypoints = new JArray(); var points = new List<int2> { from };
                        bool haveWaypoints = true;
                        for (int waypoint = 0; waypoint < 2; waypoint++)
                        {
                            float fraction = (waypoint + 1) / 3f;
                            float2 target = new float2(from.x, from.y) + chord * fraction + side * offset;
                            if (!TryWaypoint(target, footprint, vehicle, excluded, out var selected, out int shell,
                                out float distanceFromTarget))
                            {
                                haveWaypoints = false;
                                waypoints.Add(new JObject { ["fraction"] = fraction, ["searchStatus"] = "NoCandidateWithinShellCap",
                                    ["targetCell"] = new JArray(target.x, target.y), ["candidateOnly"] = true });
                                break;
                            }
                            excluded.Add(selected.y * grid.Width + selected.x); points.Add(selected);
                            waypoints.Add(new JObject { ["fraction"] = fraction, ["targetCell"] = new JArray(target.x, target.y),
                                ["selectedCell"] = new JArray(selected.x, selected.y), ["searchShellCells"] = shell,
                                ["distanceFromTargetCells"] = distanceFromTarget, ["sourceWalkableAndSurfaceTraversable"] = true,
                                ["candidateOnly"] = true });
                        }
                        points.Add(to);
                        var laneRow = new JObject { ["laneCandidateKind"] = lane,
                            ["candidateOnly"] = true, ["routeBindingExists"] = false, ["routeProof"] = false,
                            ["roadSemanticsUsed"] = false, ["waypoints"] = waypoints,
                            ["segments"] = new JArray(), ["allSegmentsOwnerFoundExactGoalInsideAlias"] = false };
                        ((JArray)directionRow["lanes"]).Add(laneRow);
                        if (!haveWaypoints) { laneRow["status"] = "WaypointSelectionFailed_Unqualified"; continue; }
                        bool allSegmentsPassed = true;
                        var segments = (JArray)laneRow["segments"];
                        for (int i = 0; i < points.Count - 1; i++)
                        {
                            ownerQueryCount++;
                            Require(ownerQueryCount <= 24, "Axis/flank owner path query cap exceeded");
                            var segment = new JObject { ["segmentIndex"] = i,
                                ["startCell"] = new JArray(points[i].x, points[i].y),
                                ["goalCell"] = new JArray(points[i + 1].x, points[i + 1].y),
                                ["originalPathOwnerQuery"] = true };
                            segments.Add(segment);
                            QueryPath(em, gridEntity, grid, surface, prefab, points[i], points[i + 1], segment,
                                reverse ? (byte)2 : (byte)1);
                            bool pathInside = PathInsideAlias(alias, grid, segment["pathCells"] as JArray);
                            bool passed = (string)segment["status"] == "NativeQueryFoundExactGoal" &&
                                (int?)segment["failureCode"] == 0 && pathInside;
                            segment["entirePathInsideAliasFrame"] = pathInside;
                            segment["exactOriginalOwnerSegmentPassed"] = passed;
                            allSegmentsPassed &= passed;
                        }
                        laneRow["allSegmentsOwnerFoundExactGoalInsideAlias"] = allSegmentsPassed;
                        laneRow["status"] = allSegmentsPassed
                            ? "AllSampledSegmentsOwnerFoundExactGoalInsideAlias_UnqualifiedCandidate"
                            : "AtLeastOneSampledSegmentFailed_Unqualified";
                        laneRow["polylineCellSignature"] = string.Join(">", points.Select(point => point.x + "," + point.y));
                    }
                    var directionLanes = (JArray)directionRow["lanes"];
                    if (directionLanes.Count == 2)
                    {
                        var center = directionLanes.OfType<JObject>().Single(row => (string)row["laneCandidateKind"] == lanes[0]);
                        var flank = directionLanes.OfType<JObject>().Single(row => (string)row["laneCandidateKind"] == lanes[1]);
                        var c = (JArray)center["waypoints"]; var f = (JArray)flank["waypoints"];
                        var deltas = new JArray();
                        for (int i = 0; i < Math.Min(c.Count, f.Count); i++)
                        {
                            if (c[i]["selectedCell"] is not JArray cc || f[i]["selectedCell"] is not JArray ff) continue;
                            float dx = (int)cc[0] - (int)ff[0], dz = (int)cc[1] - (int)ff[1];
                            deltas.Add(new JObject { ["waypointIndex"] = i,
                                ["centerToFlankSeparationCells"] = math.sqrt(dx * dx + dz * dz),
                                ["centerCell"] = cc.DeepClone(), ["flankCell"] = ff.DeepClone() });
                        }
                        HashSet<string> ReturnedCells(JObject laneRow) => ((JArray)laneRow["segments"])
                            .OfType<JObject>().SelectMany(segment => segment["pathCells"] is JArray pathCells
                                ? pathCells.OfType<JArray>().Select(point => (string)point[0] + "," + (string)point[1])
                                : Enumerable.Empty<string>()).ToHashSet(StringComparer.Ordinal);
                        var centerPathCells = ReturnedCells(center); var flankPathCells = ReturnedCells(flank);
                        int pathIntersection = centerPathCells.Intersect(flankPathCells, StringComparer.Ordinal).Count();
                        int pathUnion = centerPathCells.Union(flankPathCells, StringComparer.Ordinal).Count();
                        directionRow["laneDistinctnessMetrics"] = new JObject
                        {
                            ["correspondingWaypointSeparations"] = deltas,
                            ["candidateWaypointSequencesDiffer"] = !string.Equals((string)center["polylineCellSignature"],
                                (string)flank["polylineCellSignature"], StringComparison.Ordinal),
                            ["ownerReturnedPathCellSetsDiffer"] = !centerPathCells.SetEquals(flankPathCells),
                            ["ownerReturnedPathCellSetJaccard"] = pathUnion == 0 ? 1.0 : (double)pathIntersection / pathUnion,
                            ["ownerReturnedPathCellIntersectionCount"] = pathIntersection,
                            ["ownerReturnedPathCellUnionCount"] = pathUnion,
                            ["ownerReturnedPathsArePartialIfAnySegmentFailed"] =
                                !(bool)center["allSegmentsOwnerFoundExactGoalInsideAlias"] ||
                                !(bool)flank["allSegmentsOwnerFoundExactGoalInsideAlias"],
                            ["routeDistinctnessValidated"] = false,
                            ["reason"] = "Metrics compare temporary candidate waypoints and whatever original-owner path cells were returned; failed segments and excluded gameplay roads/blockers prevent independent route validation"
                        };
                    }
                }
            }
            result["originalOwnerQueryCount"] = ownerQueryCount;
            result["status"] = "CollectedTemporaryUnqualifiedLaneCandidates";
            return result;
        }

        static JObject RunS026TypedPadHypotheses(OperationMapDefinition alias, OperationMapAnchorConfig[] anchors,
            EntityManager em, Entity gridEntity, GridConfig grid, MapSurfaceComponent surface, JObject plans)
        {
            var report = new JObject { ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                ["normalMissionInitialized"] = false, ["normalSpawnAcceptance"] = false,
                ["liveOccupancyIncluded"] = false, ["fullLayoutValidated"] = false,
                ["scope"] = "TransientFourPadHypotheses_OriginalTypedMaskAndStaticSpawnOwner_ExactStandardBudget",
                ["seed"] = 104755, ["variantCountCap"] = 2,
                ["dimensionProvenance"] = "ExistingDesertBaseLayoutBuilderPrecedent_HypothesisOnly_NotS026Fit" };
            var variants = new JArray(); report["variants"] = variants;
            var ownerPins = new JArray(); report["ownerSourcePins"] = ownerPins;
            foreach (string path in new[] {
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.GroundObjective30.cs",
                "Assets/Game/Scripts/Runtime/Skirmish/SkirmishTypedSpawnConstraint.cs",
                "Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs",
                "Assets/Game/Scripts/Runtime/Skirmish/SkirmishVisualSpawnService.cs",
                "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs",
                "Assets/Game/Scripts/Systems/SkirmishSpawnPlacement.cs",
                "Assets/Game/Scripts/Utilities/SpawnCellUtility.cs",
                "Assets/Game/Scripts/Utilities/SpawnCellSearchKernel.cs",
                "Assets/Game/Scripts/Simulation/SkirmishMissionBootstrapKernel.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishMapLayoutConfig.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishMapLayoutBuilder.cs",
                "Assets/Game/Scripts/Systems/Pathfinding/Surface/MapSurfaceTraversalValidation.cs" })
                ownerPins.Add(new JObject { ["path"] = path, ["sha256"] = Hash(path) });
            const string shared = "Assets/Game/Configs/SkirmishExpansion/Shared/";
            var objective = AssetDatabase.LoadAssetAtPath<SkirmishObjectiveConfig>(shared + "SkirmishObjective_BA.asset");
            var army = AssetDatabase.LoadAssetAtPath<SkirmishArmyProfileConfig>(shared + "SkirmishArmy_GroundManeuver.asset");
            var start = AssetDatabase.LoadAssetAtPath<SkirmishStartPackageConfig>(shared + "SkirmishStart_Established.asset");
            var economy = AssetDatabase.LoadAssetAtPath<SkirmishEconomyConfig>(shared + "SkirmishEconomy_V1.asset");
            var upgrades = AssetDatabase.LoadAssetAtPath<SkirmishUpgradeConfig>(shared + "SkirmishUpgrade_V1.asset");
            var intel = AssetDatabase.LoadAssetAtPath<SkirmishIntelConfig>(shared + "SkirmishIntel_SharedFog.asset");
            var roles = AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(shared + "SkirmishRoleCatalog_V1.asset");
            Require(objective != null && army != null && start != null && economy != null && upgrades != null && intel != null && roles != null,
                "Exact S026 shared configuration required for typed pad hypotheses");
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError), matrixError);
            var facilities = new JArray();
            foreach (var property in plans.Properties())
                facilities.Add(new JObject { ["entireConfigurationFound"] = true,
                    ["resolvedEffectiveRect"] = property.Value["resolvedEffectiveRect"].DeepClone() });
            Require(facilities.Count == 20, "Exact 20 candidate facilities required for typed pad hypotheses");
            float width = alias.Bounds.PlayableMax.x - alias.Bounds.PlayableMin.x;
            float depth = alias.Bounds.PlayableMax.z - alias.Bounds.PlayableMin.z;
            var deployments = anchors.OrderBy(a => a.FactionId).ToArray();
            Require(deployments.Length == 2 && deployments[0].FactionId == 1 && deployments[1].FactionId == 2,
                "Exact faction deployment anchors required");
            for (int variant = 0; variant < 2; variant++)
            {
                var layout = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
                var definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
                try
                {
                    layout.ConfigureDesertBaseAssault();
                    var json = JObject.Parse(JsonUtility.ToJson(layout));
                    json["layoutId"] = "layout.skirmish.s026.typed-pad-hypothesis." + variant;
                    json["operationMapId"] = alias.OperationMapId;
                    json["contentHash"] = "unqualified-typed-pad-hypothesis";
                    json["worldWidthMetres"] = width; json["worldDepthMetres"] = depth;
                    json["originX"] = alias.Bounds.PlayableMin.x; json["originZ"] = alias.Bounds.PlayableMin.z;
                    json["cellSize"] = grid.CellSize;
                    var binding = (JObject)json["worldBinding"];
                    binding["OriginX"] = alias.Bounds.PlayableMin.x; binding["OriginZ"] = alias.Bounds.PlayableMin.z + depth * .5f;
                    binding["ForwardX"] = 1f; binding["ForwardZ"] = 0f; binding["AcrossX"] = 0f; binding["AcrossZ"] = 1f;
                    binding["ForwardMetres"] = width; binding["AcrossMetres"] = depth;
                    binding["SourceHash"] = alias.SourceBinding.SourceContentHash;
                    binding["PlayerAnchorId"] = deployments[0].AnchorId; binding["EnemyAnchorId"] = deployments[1].AnchorId;
                    binding["HasMeasuredDeployments"] = false; binding["BindAllConfigurations"] = false;
                    var pads = new JArray(); json["pads"] = pads;
                    foreach (var deployment in deployments)
                    foreach (var category in new[] { SkirmishLegalPadKind.InfantrySpawn, SkirmishLegalPadKind.VehicleSpawn })
                    {
                        bool infantry = category == SkirmishLegalPadKind.InfantrySpawn;
                        float direction = (infantry == (variant == 0)) ? -1f : 1f;
                        pads.Add(new JObject { ["PadId"] = "s026.hypothesis." + variant + "." + deployment.FactionId + "." + category,
                            ["AnchorId"] = deployment.AnchorId, ["RoleId"] = "hypothesis." + deployment.FactionId + "." + category,
                            ["Kind"] = (int)category, ["FactionId"] = deployment.FactionId,
                            ["CenterX"] = deployment.Position.x + direction * 8f, ["CenterZ"] = deployment.Position.z,
                            ["WidthMetres"] = infantry ? 12f : 14f, ["DepthMetres"] = infantry ? 10f : 12f });
                    }
                    JsonUtility.FromJsonOverwrite(json.ToString(Formatting.None), layout);
                    definition.Configure("S026", objective, army, start, economy, upgrades, intel, roles, layout,
                        SkirmishSizeId.Standard, new[] { "ground", "intel", "transport", "advanced_ground", "objective_ba" });
                    definition.BindCampaignMap(alias);
                    Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition, SkirmishDifficultyId.Regular,
                        SkirmishSizeId.Standard, 104755, new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                        out var budget, out var reasons) && budget != null && budget.ExactCanonicalQuantities,
                        "Exact Standard budget required for typed pad hypotheses: " + string.Join(";", (reasons ?? new List<SkirmishCompileReason>()).Select(r => r.Field + ":" + r.Detail)));
                    using var constraint = new SkirmishTypedSpawnConstraint(em, gridEntity, grid, layout);
                    var packed = Ground30PackBudget(budget, layout, em, gridEntity, grid, surface, facilities, constraint, 104755);
                    int requested = packed.OfType<JObject>().Sum(r => (int)r["requestedCount"]);
                    int found = packed.OfType<JObject>().Sum(r => (int?)r["foundCount"] ?? 0);
                    int contained = packed.OfType<JObject>().Sum(r => (int?)r["insideTypedPadCount"] ?? 0);
                    int traversable = packed.OfType<JObject>().Sum(r => (int?)r["surfaceTraversableCount"] ?? 0);
                    variants.Add(new JObject { ["variant"] = variant, ["candidatePads"] = pads.DeepClone(),
                        ["requestedActors"] = requested, ["foundActors"] = found, ["insideTypedPadActors"] = contained,
                        ["surfaceTraversableActors"] = traversable, ["packingRows"] = packed,
                        ["allCandidateActorsPacked"] = requested == 52 && found == requested && contained == requested && traversable == requested,
                        ["facilityPadRectangleOverlapChecked"] = false, ["candidateOnly"] = true,
                        ["sourceFitCertified"] = false, ["status"] = "TypedCandidatePackingCollected_Unqualified" });
                }
                finally { UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(layout); }
            }
            report["status"] = "TransientTypedPadHypothesesCollected_Unqualified";
            return report;
        }

        static bool TryBuildS026SourceComponents(OperationMapDefinition alias, GridConfig grid,
            NativeArray<GridWalkable> walkable, MapSurfaceComponent surface, int2 footprint, bool vehicle,
            out int[] componentIds, out int[] componentParents, out List<int> componentSizes, out int validAliasCenters,
            bool reverseNeighbors, int preferredSeed)
        {
            componentIds = null; componentParents = null; componentSizes = new List<int>(); validAliasCenters = 0;
            long tileCountLong = (long)grid.Width * grid.Height;
            // Bound memory and total work independently of the source grid. The current Campaign
            // grid fits; a larger or malformed source fails closed without partial component claims.
            if (grid.Width <= 0 || grid.Height <= 0 || tileCountLong > 2500000 ||
                tileCountLong != walkable.Length || footprint.x <= 0 || footprint.y <= 0) return false;
            int tileCount = (int)tileCountLong;
            var valid = new bool[tileCount];
            var labels = new int[tileCount];
            var parents = new int[tileCount];
            var queue = new int[tileCount];
            var placement = new S026StaticPlacementContext
            { WidthValue = grid.Width, HeightValue = grid.Height, WalkableCells = walkable };
            var validation = new MapSurfaceTraversalValidation();
            MapSurfaceMovementMask movement = validation.ResolveMovementMask(vehicle);
            for (int z = 0; z < grid.Height; z++)
            for (int x = 0; x < grid.Width; x++)
            {
                int index = z * grid.Width + x;
                if (walkable[index].Value == 0) continue;
                var cell = new int2(x, z);
                float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                if (!IsInsideAlias(alias, wx, wz) ||
                    !IsFootprintInsideAlias(alias, grid, UnitFootprintUtility.GetMinCell(cell, footprint), footprint) ||
                    !validation.CanTraverse(surface, surface.HasSurfaceData, cell, movement) ||
                    !validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, footprint, vehicle)) continue;
                if (!PathPlacementKernel.CanPlaceForPathing(ref placement, PlacementIdentity.Null,
                    new PlacementCell(x, z), new PlacementCell(footprint.x, footprint.y), new PlacementCell(x, z),
                    vehicle, true, 1)) continue;
                valid[index] = true;
                validAliasCenters++;
            }

            int componentId = 0;
            if (preferredSeed < 0 || preferredSeed >= tileCount) preferredSeed = 0;
            for (int seedOrdinal = 0; seedOrdinal < tileCount; seedOrdinal++)
            {
                int seed = (preferredSeed + seedOrdinal) % tileCount;
                if (!valid[seed] || labels[seed] != 0) continue;
                componentId++;
                if (componentId > 10000) return false;
                int head = 0, tail = 0;
                queue[tail++] = seed; labels[seed] = componentId; parents[seed] = -1;
                while (head < tail)
                {
                    int currentIndex = queue[head++];
                    int cx = currentIndex % grid.Width, cz = currentIndex / grid.Width;
                    for (int directionIndex = 0; directionIndex < 8; directionIndex++)
                    {
                        var direction = GridPathSearchKernel.Neighbor(reverseNeighbors ? 7 - directionIndex : directionIndex);
                        int nx = cx + direction.X, nz = cz + direction.Z;
                        if (!GridUtils.InBounds(new int2(nx, nz), grid.Width, grid.Height)) continue;
                        int nextIndex = nz * grid.Width + nx;
                        if (!valid[nextIndex] || labels[nextIndex] != 0) continue;
                        if (direction.X != 0 && direction.Z != 0)
                        {
                            var horizontal = new int2(nx, cz);
                            var vertical = new int2(cx, nz);
                            if (!IsFootprintInsideAlias(alias, grid, UnitFootprintUtility.GetMinCell(horizontal, footprint), footprint) ||
                                !IsFootprintInsideAlias(alias, grid, UnitFootprintUtility.GetMinCell(vertical, footprint), footprint) ||
                                !PathPlacementKernel.CanPlaceForPathing(ref placement, PlacementIdentity.Null,
                                    new PlacementCell(horizontal.x, horizontal.y), new PlacementCell(footprint.x, footprint.y),
                                    new PlacementCell(cx, cz), vehicle, true, 1) ||
                                !validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, horizontal, footprint, vehicle) ||
                                !PathPlacementKernel.CanPlaceForPathing(ref placement, PlacementIdentity.Null,
                                    new PlacementCell(vertical.x, vertical.y), new PlacementCell(footprint.x, footprint.y),
                                    new PlacementCell(cx, cz), vehicle, true, 1) ||
                                !validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, vertical, footprint, vehicle)) continue;
                        }
                        if (!PathPlacementKernel.CanPlaceForPathing(ref placement, PlacementIdentity.Null,
                            new PlacementCell(nx, nz), new PlacementCell(footprint.x, footprint.y),
                            new PlacementCell(cx, cz), vehicle, true, 1)) continue;
                        labels[nextIndex] = componentId;
                        parents[nextIndex] = currentIndex;
                        queue[tail++] = nextIndex;
                    }
                }
                componentSizes.Add(tail);
            }
            componentIds = labels;
            componentParents = parents;
            return true;
        }

        // Transient authoring diagnostic: place the actual compiler's canonical
        // structure roster relative to the existing alias deployment anchors, then
        // let the original building owner search and reserve effective rectangles.
        // No layout asset, Campaign source, budget, route or runtime state is changed.
        static JObject RunS026SourceStructurePlacementCandidates(OperationMapDefinition alias,
            OperationMapAnchorConfig[] deploymentAnchors, EntityManager em, Entity gridEntity, GridConfig grid,
            MapSurfaceComponent surface, DynamicBuffer<GridRoad> roads)
        {
            const int searchRadiusCap = 24;
            var report = new JObject
            {
                ["scope"] = "CompilerRosterOffsets_DeploymentAnchorFrame_OriginalBuildingOwner_StaticSourceCachesAndReservations",
                ["candidateOnly"] = true, ["normalPlacementAcceptance"] = false,
                ["sourceFitCertified"] = false, ["routeEvidence"] = false,
                ["fullMapOrAllSizeFitMeasured"] = false, ["budgetChanged"] = false,
                ["placementSearchRadiusLimitCells"] = searchRadiusCap,
                ["acceptedReservationsAreTransient"] = true,
                ["ownerImplementationPins"] = new JArray(
                    new JObject { ["path"] = "Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs", ["sha256"] = Hash("Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs") },
                    new JObject { ["path"] = "Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.RoadValidation.cs", ["sha256"] = Hash("Assets/Game/Scripts/Systems/BuildingPlacementInvalidCellCacheCompositionSystemHelper.RoadValidation.cs") },
                    new JObject { ["path"] = "Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs", ["sha256"] = Hash("Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs") },
                    new JObject { ["path"] = "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs", ["sha256"] = Hash("Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs") },
                    new JObject { ["path"] = "Assets/Game/Scripts/Systems/BuildingPlacementGridCameraSystemHelper.cs", ["sha256"] = Hash("Assets/Game/Scripts/Systems/BuildingPlacementGridCameraSystemHelper.cs") },
                    new JObject { ["path"] = "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs", ["sha256"] = Hash("Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs") }
                )
            };
            var results = new JArray(); report["sizes"] = results;
            Require(deploymentAnchors != null && deploymentAnchors.Length == 2 &&
                deploymentAnchors[0].FactionId == 1 && deploymentAnchors[1].FactionId == 2,
                "Exact existing player/enemy deployment anchors required for structure frame");
            float2 delta = new float2(deploymentAnchors[1].Position.x - deploymentAnchors[0].Position.x,
                deploymentAnchors[1].Position.z - deploymentAnchors[0].Position.z);
            float length = math.length(delta);
            Require(length > 0.01f && math.isfinite(length), "Distinct finite deployment anchors required");
            float2 forward = delta / length;
            float2 across = new float2(-forward.y, forward.x);
            report["derivedFrame"] = new JObject
            {
                ["source"] = "ExactPinnedAliasDeploymentAnchorVector",
                ["playerAnchorId"] = deploymentAnchors[0].AnchorId,
                ["enemyAnchorId"] = deploymentAnchors[1].AnchorId,
                ["playerWorld"] = new JArray(deploymentAnchors[0].Position.x, deploymentAnchors[0].Position.z),
                ["enemyWorld"] = new JArray(deploymentAnchors[1].Position.x, deploymentAnchors[1].Position.z),
                ["forwardUnitXZ"] = new JArray(forward.x, forward.y),
                ["acrossUnitXZ"] = new JArray(across.x, across.y),
                ["frameConvention"] = "forward points player-to-enemy; across=(-forwardZ,forwardX), matching desert-layout right-handed offset convention"
            };

            const string shared = "Assets/Game/Configs/SkirmishExpansion/Shared/";
            var objective = AssetDatabase.LoadAssetAtPath<SkirmishObjectiveConfig>(shared + "SkirmishObjective_BA.asset");
            var army = AssetDatabase.LoadAssetAtPath<SkirmishArmyProfileConfig>(shared + "SkirmishArmy_GroundManeuver.asset");
            var start = AssetDatabase.LoadAssetAtPath<SkirmishStartPackageConfig>(shared + "SkirmishStart_Established.asset");
            var economy = AssetDatabase.LoadAssetAtPath<SkirmishEconomyConfig>(shared + "SkirmishEconomy_V1.asset");
            var upgrades = AssetDatabase.LoadAssetAtPath<SkirmishUpgradeConfig>(shared + "SkirmishUpgrade_V1.asset");
            var intel = AssetDatabase.LoadAssetAtPath<SkirmishIntelConfig>(shared + "SkirmishIntel_SharedFog.asset");
            var roles = AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(shared + "SkirmishRoleCatalog_V1.asset");
            Require(objective != null && army != null && start != null && economy != null && upgrades != null && intel != null && roles != null,
                "Exact clean shared S026 BA/G/E config assets required");
            var dependencies = new JArray();
            foreach (var asset in new UnityEngine.Object[] { objective, army, start, economy, upgrades, intel, roles })
            {
                string path = AssetDatabase.GetAssetPath(asset);
                Require(!string.IsNullOrEmpty(path) && !EditorUtility.IsDirty(asset), "Clean exact shared config required: " + path);
                dependencies.Add(new JObject { ["path"] = path, ["sha256"] = Hash(path) });
            }
            report["sharedConfigPins"] = dependencies;

            using var layoutOwner = new S026TransientObjectOwner();
            using var definitionOwner = new S026TransientObjectOwner();
            var layout = layoutOwner.Own(ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>());
            layout.ConfigureDesertBaseAssault();
            // Force only the ID/frame metadata onto the existing S026 alias; all
            // accepted coordinates are separately constrained to the pinned bounds.
            var layoutJson = JObject.Parse(JsonUtility.ToJson(layout));
            layoutJson["layoutId"] = "layout.skirmish.s026.cc.ba.g.candidate.v1";
            layoutJson["operationMapId"] = alias.OperationMapId;
            layoutJson["contentHash"] = "unqualified.s026.alias.structure-candidate.v1";
            JsonUtility.FromJsonOverwrite(layoutJson.ToString(Formatting.None), layout);
            var definition = definitionOwner.Own(ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>());
            definition.Configure("S026", objective, army, start, economy, upgrades, intel, roles, layout,
                SkirmishSizeId.Standard, new[] { "ground", "intel", "transport", "advanced_ground", "objective_ba" });
            definition.BindCampaignMap(alias);
            Require(definition.CatalogId == "S026" && definition.DefinitionId == "skirmish.s026" &&
                definition.OperationMapId == alias.OperationMapId && definition.CampaignMap == alias && definition.MapLayout == layout,
                "Transient S026 tuple wrapper must preserve coded identity and existing Campaign alias");

            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError), matrixError);
            foreach (SkirmishSizeId size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition, SkirmishDifficultyId.Regular, size, 1,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                    out var budget, out var reasons) && budget != null && budget.ExactCanonicalQuantities,
                    "Exact canonical S026 structure roster unavailable for " + size + ": " +
                    string.Join(";", (reasons ?? new List<SkirmishCompileReason>()).Select(r => r.Field + ":" + r.Detail)));
                var sizeRow = new JObject
                {
                    ["size"] = size.ToString(), ["normalCompilerPassed"] = budget.NormalCompilerPassed,
                    ["exactCanonicalQuantities"] = budget.ExactCanonicalQuantities,
                    ["canonicalBudgetErrors"] = JArray.FromObject(budget.CanonicalBudgetErrors),
                    ["structureCount"] = budget.Structures.Length,
                    ["candidateOnly"] = true, ["sourceFitCertified"] = false
                };
                results.Add(sizeRow);
                var placementRows = new JArray(); sizeRow["structures"] = placementRows;
                using var sourceCacheOwner = new S026SourcePlacementOwner();
                var cache = new BuildingPlacementInvalidCellCacheCompositionSystemHelper();
                using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
                cache.EnsureAuthoredGeometry(em, surfaces, grid);
                Require(cache.AuthoredBuildings.IsReady, "Original authored-building source cache unavailable");
                var blockerMask = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                try
                {
                    var walkable = em.GetBuffer<GridWalkable>(gridEntity, true);
                    Require(walkable.Length == grid.Width * grid.Height, "Exact full source walkability buffer required");
                    for (int i = 0; i < walkable.Length; i++) if (walkable[i].Value == 0) blockerMask.Set(i, true);
                    var blocker = new DynamicBlockerComponent { Blocked = blockerMask };
                    var startup = new BuildingPlacementStartupSystemHelper();
                    var dependency = new BuildingGameplayDependencyCompositionSystemHelper();
                    var definitions = new BuildingDefinitionPrefabSystemHelper();
                    definitions.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                        BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
                    var runway = new BuildingRunwaySystem(); var geometry = new BuildingPlacementGridCameraSystemHelper();
                    var search = new BuildingRuntimeSpawnCompositionSystemHelper();
                    var reservations = new List<RectInt>();
                    var buildingPrefabPaths = S026StructurePrefabPaths();
                    foreach (var structure in budget.Structures)
                    {
                        string visualKey = SkirmishStructureIds.VisualKey(structure.StructureId);
                        string prefabPath = null;
                        bool hasPrefabPath = !string.IsNullOrEmpty(visualKey) && buildingPrefabPaths.TryGetValue(visualKey, out prefabPath);
                        Require(hasPrefabPath, "Canonical structure has no exact mapped prefab: " + structure.StructureId);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        Require(prefab != null && !EditorUtility.IsDirty(prefab) && prefab.name == visualKey,
                            "Exact clean canonical structure prefab unavailable: " + prefabPath);
                        var definitionForPlacement = definitions.CreateRuntimeBuildingDefinition(prefab, prefab.name, string.Empty,
                            Vector2Int.one, 1, runway);
                        Require(definitionForPlacement != null, "Original building definition factory rejected " + visualKey);
                        Vector2 root = structure.FactionId == 1
                            ? new Vector2(deploymentAnchors[0].Position.x, deploymentAnchors[0].Position.z)
                            : new Vector2(deploymentAnchors[1].Position.x, deploymentAnchors[1].Position.z);
                        float preferredX = root.x + structure.AcrossOffsetMetres * across.x + structure.ForwardOffsetMetres * forward.x;
                        float preferredZ = root.y + structure.AcrossOffsetMetres * across.y + structure.ForwardOffsetMetres * forward.y;
                        int2 center = GridUtils.WorldToCell(grid, new float3(preferredX, grid.Origin.y, preferredZ));
                        Vector2Int footprint = geometry.GetPlacementFootprint(definitionForPlacement, false);
                        var preferredOrigin = new Vector2Int(center.x - footprint.x / 2, center.y - footprint.y / 2);
                        RectInt Effective(BuildingDefinition d, Vector2Int origin, GridConfig g, bool rotated) =>
                            runway.GetEffectivePlacementRect(d, origin, g, rotated, grid.Origin.y, geometry.GetPlacementFootprint);
                        bool Inside(RectInt rect)
                        {
                            if (rect.width <= 0 || rect.height <= 0) return false;
                            for (int z = rect.yMin; z < rect.yMax; z++)
                            for (int x = rect.xMin; x < rect.xMax; x++)
                            {
                                if (!GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)) return false;
                                float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                                float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                                if (!IsInsideAlias(alias, wx, wz)) return false;
                            }
                            return true;
                        }
                        bool Valid(BuildingDefinition d, Vector2Int origin, Vector2Int modelFootprint, bool rotated,
                            GridConfig g, DynamicBuffer<GridRoad> nativeRoads, DynamicBlockerComponent nativeBlocker)
                        {
                            var rect = Effective(d, origin, g, rotated);
                            return Inside(rect) && !reservations.Any(rect.Overlaps) &&
                                BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect) &&
                                cache.IsPlacementValid(d, origin, modelFootprint, rotated, g, nativeRoads, nativeBlocker,
                                    dependency, startup, Effective, candidate => reservations.Any(candidate.Overlaps));
                        }
                        bool CachedInvalid(Vector2Int origin, Vector2Int modelFootprint) =>
                            BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                                sourceCacheOwner.Prefix, sourceCacheOwner.PrefixWidth, sourceCacheOwner.PrefixHeight, origin, modelFootprint);
                        // Build the same finite source prefix used by the normal owner
                        // helpers; this is only a cache short-circuit, not a new fit rule.
                        if (sourceCacheOwner.Prefix == null)
                        {
                            bool[] invalid = new bool[grid.Width * grid.Height];
                            startup.FillRoadFootprintMask(grid, invalid);
                            cache.AuthoredBuildings.AppendTo(invalid); cache.RoadSurfaces.AppendTo(invalid); cache.RoadSurfaces.AppendWaterTo(invalid);
                            int[] prefix = null;
                            BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid, roads, blocker, invalid,
                                dependency.IsRuntimeBlockerCell, ref prefix, out int pw, out int ph, out bool ready);
                            Require(ready, "Original static source-invalid prefix unavailable");
                            sourceCacheOwner.SetPrefix(prefix, pw, ph);
                        }
                        var context = new BuildingRuntimeSpawnCompositionSystemHelper.Context(null, null, null, null, default,
                            null, geometry.GetPlacementFootprint, Effective, Valid, CachedInvalid,
                            null, null, null, null);
                        bool found = search.TryFindValidInitialBuildingOrigin(context, definitionForPlacement, preferredOrigin,
                            false, grid, roads, blocker, out var resolvedOrigin, false, false, false, searchRadiusCap);
                        var resolvedRect = found ? Effective(definitionForPlacement, resolvedOrigin, grid, false) : new RectInt();
                        var row = new JObject
                        {
                            ["faction"] = structure.FactionId, ["structureId"] = structure.StructureId,
                            ["visualKey"] = visualKey, ["prefabPath"] = prefabPath, ["prefabSha256"] = Hash(prefabPath),
                            ["spawnAnchorIdFromCompiler"] = structure.SpawnAnchorId,
                            ["objectiveRoleIdFromCompiler"] = structure.ObjectiveRoleId,
                            ["designatedBaseFromCompiler"] = structure.DesignatedBase,
                            ["compilerAcrossOffsetMetres"] = structure.AcrossOffsetMetres,
                            ["compilerForwardOffsetMetres"] = structure.ForwardOffsetMetres,
                            ["deploymentRootWorldXZ"] = new JArray(root.x, root.y),
                            ["preferredWorldXZ"] = new JArray(preferredX, preferredZ),
                            ["preferredCellCenter"] = new JArray(center.x, center.y),
                            ["preferredOrigin"] = new JArray(preferredOrigin.x, preferredOrigin.y),
                            ["nativePlacementFootprint"] = new JArray(footprint.x, footprint.y),
                            ["searchRadiusLimitCells"] = searchRadiusCap,
                            ["foundByOriginalPlacementOwner"] = found,
                            ["candidateOnly"] = true, ["normalPlacementAcceptance"] = false,
                            ["sourceFitCertified"] = false
                        };
                        if (found)
                        {
                            Require(Inside(resolvedRect) && !reservations.Any(resolvedRect.Overlaps),
                                "Original owner result escaped alias or prior-reservation constraints");
                            reservations.Add(resolvedRect);
                            row["resolvedOrigin"] = new JArray(resolvedOrigin.x, resolvedOrigin.y);
                            row["resolvedEffectiveRect"] = new JObject { ["x"] = resolvedRect.x, ["z"] = resolvedRect.y,
                                ["width"] = resolvedRect.width, ["depth"] = resolvedRect.height };
                            row["reservationAcceptedForSubsequentCandidates"] = true;
                        }
                        else row["reservationAcceptedForSubsequentCandidates"] = false;
                        placementRows.Add(row);
                    }
                    sizeRow["foundCount"] = placementRows.OfType<JObject>().Count(r => (bool)r["foundByOriginalPlacementOwner"]);
                    sizeRow["allCompilerStructuresOwnerFound"] = (int)sizeRow["foundCount"] == budget.Structures.Length;
                    sizeRow["priorStructureReservationsIncluded"] = true;
                    sizeRow["sourceCacheScope"] = "Original authored geometry, road/water, walkability, dependency and reservation predicates; no live gameplay occupancy";
                    sizeRow["normalCompilerPassRemainsRequired"] = !budget.NormalCompilerPassed;
                }
                finally { blockerMask.Dispose(); }
            }
            report["alternativeRootSearch"] = RunS026AlternativeRootPlacements(alias, deploymentAnchors,
                em, gridEntity, grid, surface, em.GetBuffer<GridRoad>(gridEntity, true),
                em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(), definition, matrix);
            report["constrainedFirstMiddleBandProbe"] = RunS026ConstrainedFirstMiddleBandProbe(alias, deploymentAnchors,
                em, gridEntity, grid, surface, em.GetBuffer<GridRoad>(gridEntity, true),
                em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(), definition, matrix);
            report["moduleFirstDerivedEnemyRootSearch"] = RunS026ModuleFirstDerivedEnemyRootSearch(alias,
                deploymentAnchors, em, gridEntity, grid, surface, em.GetBuffer<GridRoad>(gridEntity, true),
                em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(), definition, matrix);
            report["perFactionFacilityRootSearch"] = RunS026PerFactionFacilityRootSearch(alias,
                deploymentAnchors, em, gridEntity, grid, surface, em.GetBuffer<GridRoad>(gridEntity, true),
                em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(), definition, matrix);
            report["interpretation"] = "Owner-found placements are ephemeral candidates generated from the pinned alias deployments and canonical compiler offsets. They are not saved logical anchors/pads, three route bindings, whole-mission fit, runtime occupancy or readiness.";
            return report;
        }

        readonly struct S026AlternativeRoot
        {
            public readonly byte Faction;
            public readonly int2 Cell;
            public readonly float2 World;
            public readonly float DistanceSquared;
            public readonly int BandIndex;
            public readonly int QuadrantIndex;
            public readonly int SpatialTileIndex;
            public S026AlternativeRoot(byte faction, int2 cell, float2 world, float distanceSquared, int bandIndex,
                int quadrantIndex = -1, int spatialTileIndex = -1)
            { Faction = faction; Cell = cell; World = world; DistanceSquared = distanceSquared; BandIndex = bandIndex; QuadrantIndex = quadrantIndex; SpatialTileIndex = spatialTileIndex; }
        }

        sealed class S026AlternativeRootPair
        {
            public S026AlternativeRoot Player;
            public S026AlternativeRoot Enemy;
            public float TotalDistanceSquared => Player.DistanceSquared + Enemy.DistanceSquared;
        }

        static JObject RunS026AlternativeRootPlacements(OperationMapDefinition alias,
            OperationMapAnchorConfig[] deploymentAnchors, EntityManager em, Entity gridEntity, GridConfig grid,
            MapSurfaceComponent surface, DynamicBuffer<GridRoad> roads, NativeArray<GridWalkable> walkable,
            SkirmishScenarioDefinitionConfig definition, IReadOnlyList<SkirmishSetupMatrixRow> matrix)
        {
            const int sourceRootRadiusMetres = 160;
            const int excludedExistingSearchRadiusMetres = 24;
            const int rootsPerFactionCap = 3;
            const int rootPairAttemptCap = 3;
            const int structureSearchRadiusCap = 24;
            int controls = RunS026AlternativeRootControls();
            var report = new JObject
            {
                ["scope"] = "BoundedSourceTraversableAlternativeRootCandidates_OriginalBuildingOwnerAndReservations",
                ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                ["routeEvidence"] = false, ["normalPlacementAcceptance"] = false,
                ["nativeMissionInitialized"] = false, ["liveOccupancyIncluded"] = false,
                ["controlsPassed"] = controls,
                ["sourceScanRadiusMetres"] = sourceRootRadiusMetres,
                ["excludedDistanceAtOrBelowMetres"] = excludedExistingSearchRadiusMetres,
                ["rootsPerFactionCap"] = rootsPerFactionCap,
                ["pairAttemptCapPerSize"] = rootPairAttemptCap,
                ["distanceAnnuliMetres"] = new JArray(new JArray(24, 48), new JArray(48, 96), new JArray(96, 160)),
                ["structureSearchRadiusCells"] = structureSearchRadiusCap,
                ["candidateRootSourceRule"] = "GridWalkable plus original MapSurfaceTraversalValidation.CanTraverseFootprint for a 1x1 ground cell; exact alias-frame cell-center constraint",
                ["factionPartitionRule"] = "player roots on negative and enemy roots on positive side of the perpendicular bisector of the pinned deployment-anchor vector; exact bisector cells excluded",
                ["rootRanking"] = "one nearest root in each (24,48], (48,96], and (96,160] m annulus; ties by grid z then x",
                ["candidatePairRanking"] = "pair roots from the same displacement annulus, then sum squared displacement, then player z/x, then enemy z/x",
                ["reservations"] = "All previously owner-found effective building rectangles in the attempted two-faction roster are reserved before the next structure"
            };
            var anchors = deploymentAnchors.OrderBy(a => a.FactionId).ToArray();
            Require(anchors.Length == 2 && anchors[0].FactionId == 1 && anchors[1].FactionId == 2,
                "Exact source deployment anchor pair required for alternative-root scan");
            float2 playerOriginal = new float2(anchors[0].Position.x, anchors[0].Position.z);
            float2 enemyOriginal = new float2(anchors[1].Position.x, anchors[1].Position.z);
            float2 forward = math.normalize(enemyOriginal - playerOriginal);
            float2 midpoint = (playerOriginal + enemyOriginal) * .5f;
            var scanCounts = new JObject(); report["sourceScanCounts"] = scanCounts;
            var playerRoots = S026FindAlternativeRoots(alias, grid, walkable, surface, 1, playerOriginal,
                midpoint, forward, sourceRootRadiusMetres, excludedExistingSearchRadiusMetres,
                rootsPerFactionCap, scanCounts);
            var enemyRoots = S026FindAlternativeRoots(alias, grid, walkable, surface, 2, enemyOriginal,
                midpoint, forward, sourceRootRadiusMetres, excludedExistingSearchRadiusMetres,
                rootsPerFactionCap, scanCounts);
            report["candidateRoots"] = new JArray(playerRoots.Concat(enemyRoots).Select(S026RootJson));
            var pairRows = S026BuildAlternativeRootPairs(playerRoots, enemyRoots, rootPairAttemptCap);
            report["selectedRootPairs"] = new JArray(pairRows.Select((p, i) => new JObject
            {
                ["pairIndex"] = i,
                ["playerRoot"] = S026RootJson(p.Player), ["enemyRoot"] = S026RootJson(p.Enemy),
                ["totalSquaredDisplacementMetres"] = p.TotalDistanceSquared,
                ["candidateOnly"] = true
            }));
            var sizeRows = new JArray(); report["sizes"] = sizeRows;
            var prefabPaths = S026StructurePrefabPaths();
            var definitions = new BuildingDefinitionPrefabSystemHelper();
            definitions.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var runway = new BuildingRunwaySystem(); var geometry = new BuildingPlacementGridCameraSystemHelper();
            var search = new BuildingRuntimeSpawnCompositionSystemHelper();
            float2 delta = enemyOriginal - playerOriginal;
            float2 across = new float2(-forward.y, forward.x);
            foreach (SkirmishSizeId size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition, SkirmishDifficultyId.Regular, size, 1,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                    out var budget, out var reasons) && budget != null && budget.ExactCanonicalQuantities,
                    "Exact canonical S026 roster unavailable for alternative-root size " + size + ": " +
                    string.Join(";", (reasons ?? new List<SkirmishCompileReason>()).Select(r => r.Field + ":" + r.Detail)));
                var sizeRow = new JObject
                {
                    ["size"] = size.ToString(), ["normalCompilerPassed"] = budget.NormalCompilerPassed,
                    ["exactCanonicalQuantities"] = budget.ExactCanonicalQuantities,
                    ["canonicalStructureCount"] = budget.Structures.Length,
                    ["sourceFitCertified"] = false, ["candidateOnly"] = true
                };
                sizeRows.Add(sizeRow);
                var pairAttempts = new JArray(); sizeRow["pairAttempts"] = pairAttempts;
                foreach (var pair in pairRows)
                {
                    var attempt = new JObject
                    {
                        ["playerRoot"] = S026RootJson(pair.Player), ["enemyRoot"] = S026RootJson(pair.Enemy),
                        ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                        ["normalPlacementAcceptance"] = false
                    };
                    pairAttempts.Add(attempt);
                    var placedRows = new JArray(); attempt["structures"] = placedRows;
                    var reservations = new List<RectInt>();
                    using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
                    var cache = new BuildingPlacementInvalidCellCacheCompositionSystemHelper();
                    cache.EnsureAuthoredGeometry(em, surfaces, grid);
                    Require(cache.AuthoredBuildings.IsReady, "Actual authored geometry cache unavailable for alternative roots");
                    var blockers = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    try
                    {
                        for (int i = 0; i < walkable.Length; i++) if (walkable[i].Value == 0) blockers.Set(i, true);
                        var blocker = new DynamicBlockerComponent { Blocked = blockers };
                        var startup = new BuildingPlacementStartupSystemHelper();
                        var dependency = new BuildingGameplayDependencyCompositionSystemHelper();
                        var invalidPrefixMask = new bool[grid.Width * grid.Height];
                        startup.FillRoadFootprintMask(grid, invalidPrefixMask);
                        cache.AuthoredBuildings.AppendTo(invalidPrefixMask); cache.RoadSurfaces.AppendTo(invalidPrefixMask);
                        cache.RoadSurfaces.AppendWaterTo(invalidPrefixMask);
                        int[] invalidPrefix = null;
                        BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid, roads, blocker,
                            invalidPrefixMask, dependency.IsRuntimeBlockerCell, ref invalidPrefix,
                            out int prefixWidth, out int prefixHeight, out bool prefixReady);
                        Require(prefixReady, "Original static placement prefix unavailable for alternative roots");
                        foreach (var structure in budget.Structures)
                        {
                            string visualKey = SkirmishStructureIds.VisualKey(structure.StructureId);
                            string prefabPath = null;
                            bool hasPath = !string.IsNullOrEmpty(visualKey) && prefabPaths.TryGetValue(visualKey, out prefabPath);
                            Require(hasPath, "Exact S026 structure visual mapping unavailable: " + structure.StructureId);
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            Require(prefab != null && !EditorUtility.IsDirty(prefab) && prefab.name == visualKey,
                                "Exact clean S026 structure prefab unavailable: " + prefabPath);
                            var building = definitions.CreateRuntimeBuildingDefinition(prefab, prefab.name,
                                string.Empty, Vector2Int.one, 1, runway);
                            Require(building != null, "Original building definition factory rejected " + visualKey);
                            var root = structure.FactionId == 1 ? pair.Player.World : pair.Enemy.World;
                            float wx = root.x + structure.AcrossOffsetMetres * across.x + structure.ForwardOffsetMetres * forward.x;
                            float wz = root.y + structure.AcrossOffsetMetres * across.y + structure.ForwardOffsetMetres * forward.y;
                            int2 center = GridUtils.WorldToCell(grid, new float3(wx, grid.Origin.y, wz));
                            var footprint = geometry.GetPlacementFootprint(building, false);
                            var preferredOrigin = new Vector2Int(center.x - footprint.x / 2, center.y - footprint.y / 2);
                            RectInt Effective(BuildingDefinition d, Vector2Int origin, GridConfig g, bool rotated) =>
                                runway.GetEffectivePlacementRect(d, origin, g, rotated, grid.Origin.y, geometry.GetPlacementFootprint);
                            bool Inside(RectInt rect)
                            {
                                if (rect.width <= 0 || rect.height <= 0) return false;
                                for (int z = rect.yMin; z < rect.yMax; z++)
                                for (int x = rect.xMin; x < rect.xMax; x++)
                                {
                                    if (!GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)) return false;
                                    float cellWorldX = grid.Origin.x + (x + .5f) * grid.CellSize;
                                    float cellWorldZ = grid.Origin.z + (z + .5f) * grid.CellSize;
                                    if (!IsInsideAlias(alias, cellWorldX, cellWorldZ)) return false;
                                }
                                return true;
                            }
                            bool Valid(BuildingDefinition d, Vector2Int origin, Vector2Int modelFootprint, bool rotated,
                                GridConfig g, DynamicBuffer<GridRoad> nativeRoads, DynamicBlockerComponent nativeBlocker)
                            {
                                var rect = Effective(d, origin, g, rotated);
                                return Inside(rect) && S026CanReservePlacement(rect, reservations) &&
                                    BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect) &&
                                    cache.IsPlacementValid(d, origin, modelFootprint, rotated, g, nativeRoads, nativeBlocker,
                                        dependency, startup, Effective, candidate => !S026CanReservePlacement(candidate, reservations));
                            }
                            bool CachedInvalid(Vector2Int origin, Vector2Int modelFootprint) =>
                                BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                                    invalidPrefix, prefixWidth, prefixHeight, origin, modelFootprint);
                            var context = new BuildingRuntimeSpawnCompositionSystemHelper.Context(null, null, null, null, default,
                                null, geometry.GetPlacementFootprint, Effective, Valid, CachedInvalid,
                                null, null, null, null);
                            bool found = search.TryFindValidInitialBuildingOrigin(context, building, preferredOrigin,
                                false, grid, roads, blocker, out var resolvedOrigin, false, false, false, structureSearchRadiusCap);
                            var resolvedRect = found ? Effective(building, resolvedOrigin, grid, false) : new RectInt();
                            var row = new JObject
                            {
                                ["faction"] = structure.FactionId, ["structureId"] = structure.StructureId,
                                ["visualKey"] = visualKey, ["prefabPath"] = prefabPath, ["prefabSha256"] = Hash(prefabPath),
                                ["compilerSpawnAnchorId"] = structure.SpawnAnchorId,
                                ["compilerAcrossOffsetMetres"] = structure.AcrossOffsetMetres,
                                ["compilerForwardOffsetMetres"] = structure.ForwardOffsetMetres,
                                ["candidateRootWorldXZ"] = new JArray(root.x, root.y),
                                ["preferredWorldXZ"] = new JArray(wx, wz),
                                ["preferredOrigin"] = new JArray(preferredOrigin.x, preferredOrigin.y),
                                ["nativePlacementFootprint"] = new JArray(footprint.x, footprint.y),
                                ["searchRadiusLimitCells"] = structureSearchRadiusCap,
                                ["foundByOriginalPlacementOwner"] = found,
                                ["reservationAcceptedForLaterStructures"] = found,
                                ["candidateOnly"] = true, ["normalPlacementAcceptance"] = false
                            };
                            if (found)
                            {
                                Require(Inside(resolvedRect) && S026CanReservePlacement(resolvedRect, reservations),
                                    "Alternative-root owner result escaped alias or reservation rule");
                                reservations.Add(resolvedRect);
                                row["resolvedOrigin"] = new JArray(resolvedOrigin.x, resolvedOrigin.y);
                                row["resolvedEffectiveRect"] = new JObject { ["x"] = resolvedRect.x, ["z"] = resolvedRect.y,
                                    ["width"] = resolvedRect.width, ["depth"] = resolvedRect.height };
                            }
                            placedRows.Add(row);
                        }
                        attempt["foundCount"] = placedRows.OfType<JObject>().Count(r => (bool)r["foundByOriginalPlacementOwner"]);
                        attempt["canonicalStructureCount"] = budget.Structures.Length;
                        attempt["allStructuresOwnerFound"] = (int)attempt["foundCount"] == budget.Structures.Length;
                    }
                    finally { blockers.Dispose(); }
                }
                sizeRow["pairAttemptCount"] = pairAttempts.Count;
                sizeRow["pairAttemptCap"] = rootPairAttemptCap;
            }
            report["interpretation"] = "Source-derived alternative roots and owner placements are unqualified candidates. Source-cell scan only selects roots; original building owner tests each structure and reserves successful effective rectangles. Search caps can censor valid alternatives; no route, global fit, occupancy or readiness claim.";
            return report;
        }

        static List<S026AlternativeRoot> S026FindAlternativeRoots(OperationMapDefinition alias, GridConfig grid,
            NativeArray<GridWalkable> walkable, MapSurfaceComponent surface, byte faction, float2 original,
            float2 midpoint, float2 forward, int radius, int excludedRadius, int cap, JObject counts)
        {
            var eligible = new List<S026AlternativeRoot>();
            long scanned = 0, frameRejected = 0, radiusRejected = 0, halfPlaneRejected = 0;
            int walkabilityRejected = 0, surfaceRejected = 0;
            var eligibleByBand = new int[3];
            var traversal = new MapSurfaceTraversalValidation();
            int minX = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.x - grid.Origin.x) / grid.CellSize));
            int maxX = Mathf.Min(grid.Width - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.x - grid.Origin.x) / grid.CellSize));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.z - grid.Origin.z) / grid.CellSize));
            int maxZ = Mathf.Min(grid.Height - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.z - grid.Origin.z) / grid.CellSize));
            float radiusSq = radius * radius, excludedSq = excludedRadius * excludedRadius;
            for (int z = minZ; z <= maxZ; z++)
            for (int x = minX; x <= maxX; x++)
            {
                scanned++;
                float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                if (!IsInsideAlias(alias, wx, wz)) { frameRejected++; continue; }
                float2 world = new float2(wx, wz);
                float2 fromAnchor = world - original;
                float distanceSq = math.lengthsq(fromAnchor);
                if (distanceSq <= excludedSq || distanceSq > radiusSq) { radiusRejected++; continue; }
                float side = math.dot(world - midpoint, forward);
                if (Mathf.Abs(side) < .0001f || faction == 1 && side >= 0f || faction == 2 && side <= 0f)
                { halfPlaneRejected++; continue; }
                int index = z * grid.Width + x;
                if (walkable[index].Value == 0) { walkabilityRejected++; continue; }
                var cell = new int2(x, z);
                if (!traversal.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, new int2(1, 1), false))
                { surfaceRejected++; continue; }
                int band = distanceSq <= 48f * 48f ? 0 : distanceSq <= 96f * 96f ? 1 : 2;
                eligibleByBand[band]++;
                eligible.Add(new S026AlternativeRoot(faction, cell, world, distanceSq, band));
            }
            var candidates = S026SelectAnnulusRepresentatives(eligible);
            Require(candidates.Count <= cap, "Annulus root selection exceeded explicit faction cap");
            int eligibleBeforeCap = eligible.Count;
            counts[faction.ToString()] = new JObject
            {
                ["scannedGridCells"] = scanned, ["outsideAliasCellCenters"] = frameRejected,
                ["outsideDistanceWindowOrAlreadyCoveredBy24mSearch"] = radiusRejected,
                ["wrongDeploymentHalfPlaneOrBisector"] = halfPlaneRejected,
                ["sourceUnwalkable"] = walkabilityRejected, ["surfaceNotTraversable"] = surfaceRejected,
                ["eligibleByAnnulus"] = new JArray(
                    new JObject { ["annulusIndex"] = 0, ["innerExclusiveMetres"] = 24, ["outerInclusiveMetres"] = 48, ["eligible"] = eligibleByBand[0] },
                    new JObject { ["annulusIndex"] = 1, ["innerExclusiveMetres"] = 48, ["outerInclusiveMetres"] = 96, ["eligible"] = eligibleByBand[1] },
                    new JObject { ["annulusIndex"] = 2, ["innerExclusiveMetres"] = 96, ["outerInclusiveMetres"] = 160, ["eligible"] = eligibleByBand[2] }),
                ["eligibleBeforeCap"] = eligibleBeforeCap,
                ["retainedAfterAnnulusSampling"] = candidates.Count, ["cap"] = cap,
                ["discardedEligibleAfterOnePerAnnulus"] = Math.Max(0, eligibleBeforeCap - candidates.Count),
                ["scanWindow"] = "Alias grid cells within Euclidean 160m of exact source deployment; no new geometry"
            };
            return candidates;
        }

        static List<S026AlternativeRoot> S026SelectAnnulusRepresentatives(IEnumerable<S026AlternativeRoot> eligible)
        {
            var best = new Dictionary<int, S026AlternativeRoot>();
            foreach (var root in eligible)
                if (!best.TryGetValue(root.BandIndex, out var prior) || S026CompareAlternativeRoots(root, prior) < 0)
                    best[root.BandIndex] = root;
            return best.Values.OrderBy(root => root.BandIndex).ToList();
        }

        static int S026CompareAlternativeRoots(S026AlternativeRoot a, S026AlternativeRoot b)
        {
            int distance = a.DistanceSquared.CompareTo(b.DistanceSquared);
            if (distance != 0) return distance;
            int z = a.Cell.y.CompareTo(b.Cell.y);
            return z != 0 ? z : a.Cell.x.CompareTo(b.Cell.x);
        }

        static JObject S026RootJson(S026AlternativeRoot root) => new JObject
        {
            ["faction"] = root.Faction, ["cell"] = new JArray(root.Cell.x, root.Cell.y),
            ["worldXZ"] = new JArray(root.World.x, root.World.y),
            ["distanceSquaredFromCurrentDeployment"] = root.DistanceSquared,
            ["annulusIndex"] = root.BandIndex < 0 ? JValue.CreateNull() : new JValue(root.BandIndex),
            ["quadrantIndex"] = root.QuadrantIndex < 0 ? JValue.CreateNull() : new JValue(root.QuadrantIndex),
            ["spatialTileIndex"] = root.SpatialTileIndex < 0 ? JValue.CreateNull() : new JValue(root.SpatialTileIndex),
            ["candidateOnly"] = true
        };

        static List<S026AlternativeRootPair> S026BuildAlternativeRootPairs(
            List<S026AlternativeRoot> player, List<S026AlternativeRoot> enemy, int cap)
        {
            var pairs = new List<S026AlternativeRootPair>();
            foreach (var p in player)
            foreach (var e in enemy)
                if (p.BandIndex == e.BandIndex)
                    pairs.Add(new S026AlternativeRootPair { Player = p, Enemy = e });
            pairs.Sort((a, b) =>
            {
                int score = a.TotalDistanceSquared.CompareTo(b.TotalDistanceSquared);
                if (score != 0) return score;
                int band = a.Player.BandIndex.CompareTo(b.Player.BandIndex);
                if (band != 0) return band;
                int pz = a.Player.Cell.y.CompareTo(b.Player.Cell.y);
                if (pz != 0) return pz;
                int px = a.Player.Cell.x.CompareTo(b.Player.Cell.x);
                if (px != 0) return px;
                int ez = a.Enemy.Cell.y.CompareTo(b.Enemy.Cell.y);
                return ez != 0 ? ez : a.Enemy.Cell.x.CompareTo(b.Enemy.Cell.x);
            });
            if (pairs.Count > cap) pairs.RemoveRange(cap, pairs.Count - cap);
            return pairs;
        }

        static List<S026AlternativeRoot> S026FindMiddleBandQuadrantRoots(OperationMapDefinition alias,
            GridConfig grid, NativeArray<GridWalkable> walkable, MapSurfaceComponent surface, byte faction,
            float2 original, float2 midpoint, float2 forward, JObject counts)
        {
            const float innerRadius = 48f, outerRadius = 96f;
            var best = new Dictionary<int, S026AlternativeRoot>();
            long scanned = 0, outsideAlias = 0, outsideBand = 0, wrongHalfPlane = 0;
            int unwalkable = 0, surfaceRejected = 0;
            var traversal = new MapSurfaceTraversalValidation();
            float2 across = new float2(-forward.y, forward.x);
            int minX = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.x - grid.Origin.x) / grid.CellSize));
            int maxX = Mathf.Min(grid.Width - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.x - grid.Origin.x) / grid.CellSize));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.z - grid.Origin.z) / grid.CellSize));
            int maxZ = Mathf.Min(grid.Height - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.z - grid.Origin.z) / grid.CellSize));
            float innerSq = innerRadius * innerRadius, outerSq = outerRadius * outerRadius;
            for (int z = minZ; z <= maxZ; z++)
            for (int x = minX; x <= maxX; x++)
            {
                scanned++;
                float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                if (!IsInsideAlias(alias, wx, wz)) { outsideAlias++; continue; }
                float2 world = new float2(wx, wz);
                float2 displacement = world - original;
                float distanceSq = math.lengthsq(displacement);
                if (distanceSq <= innerSq || distanceSq > outerSq) { outsideBand++; continue; }
                float side = math.dot(world - midpoint, forward);
                if (math.abs(side) < .0001f || faction == 1 && side >= 0f || faction == 2 && side <= 0f)
                { wrongHalfPlane++; continue; }
                int index = z * grid.Width + x;
                if (walkable[index].Value == 0) { unwalkable++; continue; }
                var cell = new int2(x, z);
                if (!traversal.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, new int2(1, 1), false))
                { surfaceRejected++; continue; }
                float forwardDelta = math.dot(displacement, forward), acrossDelta = math.dot(displacement, across);
                int quadrant = (forwardDelta >= 0f ? 2 : 0) + (acrossDelta >= 0f ? 1 : 0);
                var candidate = new S026AlternativeRoot(faction, cell, world, distanceSq, 1, quadrant);
                if (!best.TryGetValue(quadrant, out var prior) || S026CompareAlternativeRoots(candidate, prior) < 0)
                    best[quadrant] = candidate;
            }
            var roots = best.Values.OrderBy(r => r.QuadrantIndex).ToList();
            counts[faction.ToString()] = new JObject
            {
                ["scannedGridCells"] = scanned, ["outsideAliasCellCenters"] = outsideAlias,
                ["outsideMiddleAnnulus48To96Metres"] = outsideBand, ["wrongDeploymentHalfPlaneOrBisector"] = wrongHalfPlane,
                ["sourceUnwalkable"] = unwalkable, ["surfaceNotTraversable"] = surfaceRejected,
                ["eligibleQuadrantCount"] = best.Count, ["retainedNearestRootPerQuadrant"] = roots.Count,
                ["rootCapPerFaction"] = 4,
                ["quadrantFrame"] = "signs of candidate displacement projected onto existing across/forward deployment frame"
            };
            return roots;
        }

        static List<S026AlternativeRootPair> S026BuildCrossQuadrantRootPairs(
            List<S026AlternativeRoot> player, List<S026AlternativeRoot> enemy, int cap)
        {
            var pairs = new List<S026AlternativeRootPair>();
            foreach (var p in player)
            foreach (var e in enemy)
                pairs.Add(new S026AlternativeRootPair { Player = p, Enemy = e });
            pairs.Sort((a, b) =>
            {
                int score = a.TotalDistanceSquared.CompareTo(b.TotalDistanceSquared);
                if (score != 0) return score;
                int playerQuadrant = a.Player.QuadrantIndex.CompareTo(b.Player.QuadrantIndex);
                if (playerQuadrant != 0) return playerQuadrant;
                int enemyQuadrant = a.Enemy.QuadrantIndex.CompareTo(b.Enemy.QuadrantIndex);
                if (enemyQuadrant != 0) return enemyQuadrant;
                int pz = a.Player.Cell.y.CompareTo(b.Player.Cell.y);
                if (pz != 0) return pz;
                int px = a.Player.Cell.x.CompareTo(b.Player.Cell.x);
                if (px != 0) return px;
                int ez = a.Enemy.Cell.y.CompareTo(b.Enemy.Cell.y);
                return ez != 0 ? ez : a.Enemy.Cell.x.CompareTo(b.Enemy.Cell.x);
            });
            if (pairs.Count > cap) pairs.RemoveRange(cap, pairs.Count - cap);
            return pairs;
        }

        static int S026StructurePlanningPriority(string structureId)
        {
            if (structureId == SkirmishStructureIds.Refinery) return 0;
            if (structureId == SkirmishStructureIds.RefineryModule) return 1;
            if (structureId == SkirmishStructureIds.FabricationDepot) return 2;
            return 3;
        }

        static int S026CandidateStructurePlanningPriority(string structureId, bool helipadFirst)
        {
            if (!helipadFirst) return S026StructurePlanningPriority(structureId);
            if (structureId == SkirmishStructureIds.Helipad) return 0;
            if (structureId == SkirmishStructureIds.Refinery) return 1;
            if (structureId == SkirmishStructureIds.Barracks) return 2;
            if (structureId == SkirmishStructureIds.RefineryModule) return 3;
            if (structureId == SkirmishStructureIds.FabricationDepot) return 4;
            return 5;
        }

        static int[] S026ConstrainedFirstStructureOrder(SkirmishResolvedStructureEntry[] structures,
            bool helipadFirst = false) =>
            Enumerable.Range(0, structures?.Length ?? 0)
                .OrderBy(index => S026CandidateStructurePlanningPriority(structures[index].StructureId, helipadFirst))
                .ThenBy(index => index).ToArray();

        static int RunS026ConstrainedFirstOrderControls()
        {
            var rows = new[]
            {
                new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.Barracks },
                new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.FabricationDepot },
                new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.Refinery },
                new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.GroundStaging },
                new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.RefineryModule },
                new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.OilPump }
            };
            int[] expected = { 2, 4, 1, 0, 3, 5 };
            int[] first = S026ConstrainedFirstStructureOrder(rows), second = S026ConstrainedFirstStructureOrder(rows);
            Require(first.SequenceEqual(expected) && second.SequenceEqual(expected),
                "Constrained-first structure order must prioritize the named facility types and preserve compiler-row order within ties");
            var helipadRows = rows.Concat(new[]
                { new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.Helipad } }).ToArray();
            int[] helipadFirst = S026ConstrainedFirstStructureOrder(helipadRows, true);
            Require(helipadRows[helipadFirst[0]].StructureId == SkirmishStructureIds.Helipad &&
                helipadFirst.Skip(1).Select(i => helipadRows[i].StructureId).SequenceEqual(
                    new[] { SkirmishStructureIds.Refinery, SkirmishStructureIds.Barracks,
                        SkirmishStructureIds.RefineryModule, SkirmishStructureIds.FabricationDepot,
                        SkirmishStructureIds.GroundStaging, SkirmishStructureIds.OilPump }),
                "Helipad/refinery/barracks candidate ordering must preserve remaining priorities after the three facilities");
            return 1;
        }

        static int RunS026CrossQuadrantPairControls()
        {
            var player = new List<S026AlternativeRoot>
            {
                new S026AlternativeRoot(1, new int2(10, 10), new float2(10.5f, 10.5f), 1, 1, 0),
                new S026AlternativeRoot(1, new int2(20, 20), new float2(20.5f, 20.5f), 4, 1, 1)
            };
            var enemy = new List<S026AlternativeRoot>
            {
                new S026AlternativeRoot(2, new int2(30, 30), new float2(30.5f, 30.5f), 1, 1, 0),
                new S026AlternativeRoot(2, new int2(40, 40), new float2(40.5f, 40.5f), 4, 1, 1)
            };
            var pairs = S026BuildCrossQuadrantRootPairs(player, enemy, 16);
            Require(pairs.Count == 4 && pairs.Any(p => p.Player.QuadrantIndex != p.Enemy.QuadrantIndex) &&
                pairs.Select(p => (p.Player.QuadrantIndex, p.Enemy.QuadrantIndex)).SequenceEqual(
                    S026BuildCrossQuadrantRootPairs(player, enemy, 16)
                        .Select(p => (p.Player.QuadrantIndex, p.Enemy.QuadrantIndex))),
                "Cross-quadrant root pairing must include all combinations and remain deterministic");
            return 1;
        }

        sealed class S026ModuleRootTileCandidate
        {
            public int TileIndex;
            public S026AlternativeRoot Root;
            public Vector2Int ModuleOrigin;
        }

        sealed class S026PlacementFallbackCandidate
        {
            public int TileIndex;
            public int DistanceBand;
            public Vector2Int SourceCell;
            public Vector2Int Origin;
            public float DistanceSquared;
            public float DistanceBandTargetDelta;
        }

        static List<S026ModuleRootTileCandidate> S026SelectModuleTileCandidates(
            IEnumerable<S026ModuleRootTileCandidate> candidates, int cap) => candidates
                .OrderBy(candidate => candidate.Root.DistanceSquared)
                .ThenBy(candidate => candidate.Root.Cell.y)
                .ThenBy(candidate => candidate.Root.Cell.x)
                .Take(cap).ToList();

        static int S026CompareModuleTileCandidates(S026ModuleRootTileCandidate a, S026ModuleRootTileCandidate b)
        {
            int distance = a.Root.DistanceSquared.CompareTo(b.Root.DistanceSquared);
            if (distance != 0) return distance;
            int z = a.Root.Cell.y.CompareTo(b.Root.Cell.y);
            return z != 0 ? z : a.Root.Cell.x.CompareTo(b.Root.Cell.x);
        }

        static List<S026AlternativeRoot> S026FindPerFactionTileRoots(OperationMapDefinition alias,
            GridConfig grid, NativeArray<GridWalkable> walkable, MapSurfaceComponent surface, byte faction,
            float2 original, float2 midpoint, float2 forward, JObject counts)
        {
            const int tileColumns = 4, tileRows = 4, rootsPerTileCap = 4;
            var retainedByTile = Enumerable.Range(0, tileColumns * tileRows)
                .ToDictionary(i => i, _ => new List<S026ModuleRootTileCandidate>());
            var traversal = new MapSurfaceTraversalValidation();
            float2 across = new float2(-forward.y, forward.x);
            int minX = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.x - grid.Origin.x) / grid.CellSize));
            int maxX = Mathf.Min(grid.Width - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.x - grid.Origin.x) / grid.CellSize));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.z - grid.Origin.z) / grid.CellSize));
            int maxZ = Mathf.Min(grid.Height - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.z - grid.Origin.z) / grid.CellSize));
            float tileWidth = (alias.Bounds.PlayableMax.x - alias.Bounds.PlayableMin.x) / tileColumns;
            float tileDepth = (alias.Bounds.PlayableMax.z - alias.Bounds.PlayableMin.z) / tileRows;
            long scanned = 0, outsideAlias = 0, wrongHalfPlane = 0;
            int unwalkable = 0, surfaceRejected = 0;
            for (int z = minZ; z <= maxZ; z++)
            for (int x = minX; x <= maxX; x++)
            {
                scanned++;
                float2 world = new float2(grid.Origin.x + (x + .5f) * grid.CellSize,
                    grid.Origin.z + (z + .5f) * grid.CellSize);
                if (!IsInsideAlias(alias, world.x, world.y)) { outsideAlias++; continue; }
                float side = math.dot(world - midpoint, forward);
                if (math.abs(side) < .0001f || faction == 1 && side >= 0f || faction == 2 && side <= 0f)
                { wrongHalfPlane++; continue; }
                int index = z * grid.Width + x;
                if (walkable[index].Value == 0) { unwalkable++; continue; }
                if (!traversal.CanTraverseFootprint(surface, surface.HasSurfaceData, grid,
                    new int2(x, z), new int2(1, 1), false))
                { surfaceRejected++; continue; }
                int tileX = Mathf.Clamp(Mathf.FloorToInt((world.x - alias.Bounds.PlayableMin.x) / tileWidth), 0, tileColumns - 1);
                int tileZ = Mathf.Clamp(Mathf.FloorToInt((world.y - alias.Bounds.PlayableMin.z) / tileDepth), 0, tileRows - 1);
                int tileIndex = tileZ * tileColumns + tileX;
                float2 displacement = world - original;
                float forwardDelta = math.dot(displacement, forward), acrossDelta = math.dot(displacement, across);
                int quadrant = (forwardDelta >= 0f ? 2 : 0) + (acrossDelta >= 0f ? 1 : 0);
                S026KeepBestModuleTileCandidate(retainedByTile[tileIndex], new S026ModuleRootTileCandidate
                {
                    Root = new S026AlternativeRoot(faction, new int2(x, z), world,
                        math.lengthsq(displacement), 0, quadrant, tileIndex)
                }, rootsPerTileCap);
            }
            var roots = retainedByTile.OrderBy(pair => pair.Key)
                .SelectMany(pair => pair.Value.OrderBy(candidate => candidate.Root.DistanceSquared)
                    .ThenBy(candidate => candidate.Root.Cell.y).ThenBy(candidate => candidate.Root.Cell.x))
                .Select(candidate => candidate.Root).ToList();
            counts[faction.ToString()] = new JObject
            {
                ["gridCellsScanned"] = scanned, ["outsideAliasCellCenters"] = outsideAlias,
                ["wrongFactionHalfPlaneOrBisector"] = wrongHalfPlane, ["sourceUnwalkable"] = unwalkable,
                ["sourceSurfaceNotTraversable1x1"] = surfaceRejected,
                ["tileGrid"] = new JArray(tileColumns, tileRows), ["rootsPerTileCap"] = rootsPerTileCap,
                ["candidateRootCount"] = roots.Count,
                ["sampling"] = "Up to four nearest source-traversable root cell centers per alias tile; ties by displacement then grid z/x"
            };
            return roots;
        }

        static JObject RunS026HelipadRefineryFactionRootScreen(OperationMapDefinition alias, GridConfig grid,
            MapSurfaceComponent surface, DynamicBuffer<GridRoad> roads, NativeArray<GridWalkable> walkable,
            SkirmishResolvedStructureEntry[] canonicalStructures, byte faction,
            List<S026AlternativeRoot> sourceRoots, EntityManager em, BuildingDefinitionPrefabSystemHelper definitionFactory,
            IReadOnlyDictionary<string, string> prefabPaths, BuildingRunwaySystem runway,
            BuildingPlacementGridCameraSystemHelper geometry, BuildingRuntimeSpawnCompositionSystemHelper search,
            float2 forward, float2 across, List<S026AlternativeRoot> ownerPassedRoots)
        {
            const int rootOwnerSearchCallCap = 192;
            var report = new JObject
            {
                ["faction"] = faction, ["inputRootCandidateCount"] = sourceRoots.Count,
                ["perRootStructureOrder"] = new JArray("building.helipad", "building.refinery", "building.barracks"),
                ["perFactionOwnerSearchCallCap"] = rootOwnerSearchCallCap,
                ["sourceFitCertified"] = false, ["candidateOnly"] = true,
                ["ownerScreenRows"] = new JArray()
            };
            var helipadRows = canonicalStructures.Where(row => row.FactionId == faction &&
                row.StructureId == SkirmishStructureIds.Helipad).ToArray();
            var refineryRows = canonicalStructures.Where(row => row.FactionId == faction &&
                row.StructureId == SkirmishStructureIds.Refinery).ToArray();
            var barracksRows = canonicalStructures.Where(row => row.FactionId == faction &&
                row.StructureId == SkirmishStructureIds.Barracks).ToArray();
            Require(helipadRows.Length == 1 && refineryRows.Length == 1 && barracksRows.Length == 1,
                "Exactly one faction Helipad, Refinery, and Barracks are required for the per-faction root screen");
            BuildingDefinition Load(SkirmishResolvedStructureEntry structure)
            {
                string key = SkirmishStructureIds.VisualKey(structure.StructureId);
                string path = null;
                Require(!string.IsNullOrEmpty(key) && prefabPaths.TryGetValue(key, out path),
                    "Exact candidate building visual map missing: " + structure.StructureId);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(prefab != null && !EditorUtility.IsDirty(prefab) && prefab.name == key,
                    "Exact clean candidate structure prefab unavailable: " + path);
                var building = definitionFactory.CreateRuntimeBuildingDefinition(prefab, prefab.name,
                    string.Empty, Vector2Int.one, 1, runway);
                Require(building != null, "Original definition factory rejected " + key);
                return building;
            }
            var helipad = Load(helipadRows[0]);
            var refinery = Load(refineryRows[0]);
            var barracks = Load(barracksRows[0]);
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            var cache = new BuildingPlacementInvalidCellCacheCompositionSystemHelper();
            cache.EnsureAuthoredGeometry(em, surfaces, grid);
            Require(cache.AuthoredBuildings.IsReady, "Original authored-building cache unavailable for faction root screen");
            var blockers = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            try
            {
                for (int i = 0; i < walkable.Length; i++) if (walkable[i].Value == 0) blockers.Set(i, true);
                var blocker = new DynamicBlockerComponent { Blocked = blockers };
                var startup = new BuildingPlacementStartupSystemHelper();
                var dependency = new BuildingGameplayDependencyCompositionSystemHelper();
                var invalidMask = new bool[grid.Width * grid.Height];
                startup.FillRoadFootprintMask(grid, invalidMask);
                cache.AuthoredBuildings.AppendTo(invalidMask);
                cache.RoadSurfaces.AppendTo(invalidMask);
                cache.RoadSurfaces.AppendWaterTo(invalidMask);
                int[] invalidPrefix = null;
                BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid, roads, blocker,
                    invalidMask, dependency.IsRuntimeBlockerCell, ref invalidPrefix,
                    out int prefixWidth, out int prefixHeight, out bool prefixReady);
                Require(prefixReady, "Current exact owner invalid-cell prefix unavailable for faction root screen");
                RectInt Effective(BuildingDefinition building, Vector2Int origin, bool rotated) =>
                    runway.GetEffectivePlacementRect(building, origin, grid, rotated, grid.Origin.y,
                        geometry.GetPlacementFootprint);
                bool InsideAlias(RectInt rect)
                {
                    if (rect.width <= 0 || rect.height <= 0) return false;
                    for (int z = rect.yMin; z < rect.yMax; z++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        if (!GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)) return false;
                        float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                        float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                        if (!IsInsideAlias(alias, wx, wz)) return false;
                    }
                    return true;
                }
                JObject RejectionJson(IDictionary<string, int> counts)
                {
                    var result = new JObject();
                    foreach (var item in counts.OrderBy(pair => pair.Key, StringComparer.Ordinal)) result[item.Key] = item.Value;
                    return result;
                }
                void CountRejection(IDictionary<string, int> counts, string reason)
                {
                    counts.TryGetValue(reason, out int count);
                    counts[reason] = count + 1;
                }
                long ownerSearchCalls = 0;
                var traversal = new MapSurfaceTraversalValidation();
                foreach (var root in sourceRoots.OrderBy(candidate => candidate.SpatialTileIndex)
                    .ThenBy(candidate => candidate.DistanceSquared).ThenBy(candidate => candidate.Cell.y)
                    .ThenBy(candidate => candidate.Cell.x))
                {
                    var row = new JObject { ["root"] = S026RootJson(root), ["candidateOnly"] = true,
                        ["structures"] = new JArray(), ["allThreeFacilitiesPassed"] = false };
                    ((JArray)report["ownerScreenRows"]).Add(row);
                    var reservations = new List<RectInt>();
                    var structureRows = (JArray)row["structures"];
                    bool bothPassed = true;
                    foreach (var structure in new[] { helipadRows[0], refineryRows[0], barracksRows[0] })
                    {
                        var building = structure.StructureId == SkirmishStructureIds.Helipad ? helipad :
                            structure.StructureId == SkirmishStructureIds.Refinery ? refinery : barracks;
                        var footprint = geometry.GetPlacementFootprint(building, false);
                        float wx = root.World.x + structure.AcrossOffsetMetres * across.x + structure.ForwardOffsetMetres * forward.x;
                        float wz = root.World.y + structure.AcrossOffsetMetres * across.y + structure.ForwardOffsetMetres * forward.y;
                        int2 center = GridUtils.WorldToCell(grid, new float3(wx, grid.Origin.y, wz));
                        var origin = new Vector2Int(center.x - footprint.x / 2, center.y - footprint.y / 2);
                        var rect = Effective(building, origin, false);
                        bool inside = InsideAlias(rect);
                        bool surfaceTraversable = inside && traversal.CanTraverseFootprint(surface, surface.HasSurfaceData,
                            grid, new int2(rect.xMin, rect.yMin), new int2(rect.width, rect.height), false);
                        var rejectionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                        bool Valid(BuildingDefinition candidate, Vector2Int candidateOrigin, Vector2Int modelFootprint,
                            bool rotated, GridConfig candidateGrid, DynamicBuffer<GridRoad> nativeRoads,
                            DynamicBlockerComponent nativeBlocker)
                        {
                            var candidateRect = Effective(candidate, candidateOrigin, rotated);
                            if (!InsideAlias(candidateRect)) { CountRejection(rejectionCounts, "outsideAlias"); return false; }
                            if (!S026CanReservePlacement(candidateRect, reservations))
                            { CountRejection(rejectionCounts, "priorReservation"); return false; }
                            if (!BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, candidateRect))
                            { CountRejection(rejectionCounts, "surfaceLevel"); return false; }
                            bool ownerValid = cache.IsPlacementValid(candidate, candidateOrigin, modelFootprint, rotated,
                                candidateGrid, nativeRoads, nativeBlocker, dependency, startup,
                                (d, o, g, r) => runway.GetEffectivePlacementRect(d, o, g, r, grid.Origin.y,
                                    geometry.GetPlacementFootprint),
                                candidateRectValue => !S026CanReservePlacement(candidateRectValue, reservations));
                            if (!ownerValid)
                            {
                                if (cache.AuthoredBuildings.Overlaps(candidateGrid, candidateRect.position, candidateRect.size))
                                    CountRejection(rejectionCounts, "authoredBuildingOverlap");
                                else if (cache.RoadSurfaces.OverlapsWater(candidateGrid, candidateRect.position, candidateRect.size))
                                    CountRejection(rejectionCounts, "waterOverlap");
                                else if (cache.RoadSurfaces.Overlaps(candidateGrid, candidateRect.position, candidateRect.size))
                                    CountRejection(rejectionCounts, "roadOverlap");
                                else CountRejection(rejectionCounts, "ownerRectOrRuntimeReservation");
                            }
                            return ownerValid;
                        }
                        bool CachedInvalid(Vector2Int candidateOrigin, Vector2Int modelFootprint) =>
                            BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                                invalidPrefix, prefixWidth, prefixHeight, candidateOrigin, modelFootprint);
                        var context = new BuildingRuntimeSpawnCompositionSystemHelper.Context(null, null, null, null,
                            default, null, geometry.GetPlacementFootprint,
                            (d, o, g, r) => runway.GetEffectivePlacementRect(d, o, g, r, grid.Origin.y,
                                geometry.GetPlacementFootprint), Valid, CachedInvalid, null, null, null, null);
                        bool found = false;
                        bool ownerAttempted = false;
                        int cachedInvalidChecks = 0, originalPredicateChecks = 0;
                        Vector2Int resolved = default;
                        if (ownerSearchCalls < rootOwnerSearchCallCap)
                        {
                            ownerSearchCalls++;
                            ownerAttempted = true;
                            found = search.TryFindValidInitialBuildingOrigin(context, building, origin, false, grid,
                                roads, blocker, out resolved, false, false, false, 24,
                                (candidateOrigin, candidateRect, cachedInvalid) =>
                                {
                                    if (cachedInvalid) cachedInvalidChecks++;
                                    else originalPredicateChecks++;
                                });
                        }
                        var resolvedRect = found ? Effective(building, resolved, false) : new RectInt();
                        bool ownerPassed = found && InsideAlias(resolvedRect) &&
                            S026CanReservePlacement(resolvedRect, reservations);
                        if (ownerPassed) reservations.Add(resolvedRect);
                        else bothPassed = false;
                        structureRows.Add(new JObject
                        {
                            ["structureId"] = structure.StructureId, ["compilerOffsets"] = new JArray(
                                structure.AcrossOffsetMetres, structure.ForwardOffsetMetres),
                            ["preferredOrigin"] = new JArray(origin.x, origin.y),
                            ["effectiveRect"] = new JObject { ["x"] = rect.x, ["z"] = rect.y,
                                ["width"] = rect.width, ["depth"] = rect.height },
                            ["insideAlias"] = inside, ["sourceFootprintTraversable"] = surfaceTraversable,
                            ["searchRadiusLimitCells"] = 24,
                            ["ownerSearchPassed"] = ownerPassed,
                            ["ownerResolvedOrigin"] = found ? new JArray(resolved.x, resolved.y) : null,
                            ["ownerResolvedEffectiveRect"] = found ? new JObject { ["x"] = resolvedRect.x,
                                ["z"] = resolvedRect.y, ["width"] = resolvedRect.width, ["depth"] = resolvedRect.height } : null,
                            ["cachedInvalidPrefixChecks"] = cachedInvalidChecks,
                            ["originalPlacementPredicateChecks"] = originalPredicateChecks,
                            ["ownerFirstRejectionCategories"] = RejectionJson(rejectionCounts),
                            ["checkedByOriginalOwner"] = ownerAttempted,
                            ["transientReservationAdded"] = ownerPassed
                        });
                        if (!ownerPassed) break;
                    }
                    row["allThreeFacilitiesPassed"] = bothPassed;
                    if (bothPassed) ownerPassedRoots.Add(root);
                    if (ownerSearchCalls >= rootOwnerSearchCallCap) break;
                }
                report["ownerSearchCalls"] = ownerSearchCalls;
                report["ownerPassedRootCount"] = ownerPassedRoots.Count;
                report["ownerPassedRoots"] = new JArray(ownerPassedRoots.Select(S026RootJson));
            }
            finally { blockers.Dispose(); }
            return report;
        }

        static JObject RunS026PerFactionFacilityRootSearch(OperationMapDefinition alias,
            OperationMapAnchorConfig[] deploymentAnchors, EntityManager em, Entity gridEntity, GridConfig grid,
            MapSurfaceComponent surface, DynamicBuffer<GridRoad> roads, NativeArray<GridWalkable> walkable,
            SkirmishScenarioDefinitionConfig definition, IReadOnlyList<SkirmishSetupMatrixRow> matrix)
        {
            const int rootsPerTileCap = 4, tileColumns = 4, tileRows = 4, pairAttemptCap = 8;
            int controls = RunS026ConstrainedFirstOrderControls() + RunS026CrossQuadrantPairControls() +
                RunS026PlacementFallbackSelectionControls();
            var report = new JObject
            {
            ["scope"] = "IndependentFactionRoots_HelipadThenRefineryThenBarracksOriginalOwnerReservations_FullRosterCanonicalRecheck",
                ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                ["normalPlacementAcceptance"] = false, ["nativeMissionInitialized"] = false,
                ["liveOccupancyIncluded"] = false, ["controlsPassed"] = controls,
                ["aliasTileGrid"] = new JArray(tileColumns, tileRows), ["sourceRootsPerTileCap"] = rootsPerTileCap,
                ["candidateRootCapPerFaction"] = tileColumns * tileRows * rootsPerTileCap,
            ["ownerSearchCallsPerFactionSizeCap"] = tileColumns * tileRows * rootsPerTileCap * 3,
                ["aggregateOwnerSearchCallsAcrossTwoFactionsAndTwoSizesCap"] =
                    tileColumns * tileRows * rootsPerTileCap * 3 * 2 * 2,
                ["ownerSearchRadiusLimitCells"] = 24,
                ["fullRosterPairAttemptCapPerSize"] = pairAttemptCap,
                ["sizes"] = new JArray(),
                ["sourceRule"] = "GridWalkable plus original surface traversal for source root cell centers; per-faction source half-plane; candidates are existing Campaign alias coordinates only",
            ["screenRule"] = "For each faction root candidate, derive actual compiler-offset Helipad, Refinery, and Barracks preferred origins and record preferred-footprint alias/surface diagnostics, then call the original radius-24 owner search in that order with transient local reservations; the preferred footprint diagnostic does not gate the owner search",
                ["recheckRule"] = "Selected independent faction roots are paired and each full roster is planned through the original owner with reservations, then every found coordinate receives an exact preferred-origin recheck in untouched canonical compiler order",
                ["limitations"] = "Tile candidate sampling is nonexhaustive; per-faction screens do not include the opposing faction; pair cap and per-structure search caps can censor alternatives; root scan is bounded by the 4x4 tile sample (64/faction/size), radius 24 owner searches (192/faction/size; 768 total), and 8 full-roster pairs/size; no route, full fit, runtime occupancy, compiler admission or readiness claim"
            };
            var anchors = deploymentAnchors.OrderBy(anchor => anchor.FactionId).ToArray();
            Require(anchors.Length == 2 && anchors[0].FactionId == 1 && anchors[1].FactionId == 2,
                "Exact deployment anchors required for independent faction search");
            float2 playerOriginal = new float2(anchors[0].Position.x, anchors[0].Position.z);
            float2 enemyOriginal = new float2(anchors[1].Position.x, anchors[1].Position.z);
            float2 forward = math.normalize(enemyOriginal - playerOriginal);
            float2 across = new float2(-forward.y, forward.x);
            float2 midpoint = (playerOriginal + enemyOriginal) * .5f;
            var prefabPaths = S026StructurePrefabPaths();
            var definitionFactory = new BuildingDefinitionPrefabSystemHelper();
            definitionFactory.ConfigureAuthoringMetadataResolvers(
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var runway = new BuildingRunwaySystem();
            var geometry = new BuildingPlacementGridCameraSystemHelper();
            var search = new BuildingRuntimeSpawnCompositionSystemHelper();
            foreach (SkirmishSizeId size in new[] { SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,
                    SkirmishDifficultyId.Regular, size, 1,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                    out var budget, out var reasons) && budget != null && budget.ExactCanonicalQuantities,
                    "Exact canonical structure budget unavailable for independent faction root search " + size + ": " +
                    string.Join(";", (reasons ?? new List<SkirmishCompileReason>()).Select(reason => reason.Field + ":" + reason.Detail)));
                var sizeRow = new JObject { ["size"] = size.ToString(),
                    ["canonicalStructureCount"] = budget.Structures.Length,
                    ["normalCompilerPassed"] = budget.NormalCompilerPassed,
                    ["exactCanonicalQuantities"] = budget.ExactCanonicalQuantities,
                    ["candidateOnly"] = true, ["sourceFitCertified"] = false };
                ((JArray)report["sizes"]).Add(sizeRow);
                var sourceCounts = new JObject(); sizeRow["sourceScanCounts"] = sourceCounts;
                var playerCandidates = S026FindPerFactionTileRoots(alias, grid, walkable, surface, 1,
                    playerOriginal, midpoint, forward, sourceCounts);
                var enemyCandidates = S026FindPerFactionTileRoots(alias, grid, walkable, surface, 2,
                    enemyOriginal, midpoint, forward, sourceCounts);
                sizeRow["sourceCandidateRoots"] = new JArray(playerCandidates.Concat(enemyCandidates).Select(S026RootJson));
                var playerRoots = new List<S026AlternativeRoot>();
                var enemyRoots = new List<S026AlternativeRoot>();
                sizeRow["playerFactionOwnerScreen"] = RunS026HelipadRefineryFactionRootScreen(alias, grid,
                    surface, roads, walkable, budget.Structures, 1, playerCandidates, em, definitionFactory,
                    prefabPaths, runway, geometry, search, forward, across, playerRoots);
                sizeRow["enemyFactionOwnerScreen"] = RunS026HelipadRefineryFactionRootScreen(alias, grid,
                    surface, roads, walkable, budget.Structures, 2, enemyCandidates, em, definitionFactory,
                    prefabPaths, runway, geometry, search, forward, across, enemyRoots);
                sizeRow["ownerPassedRootCounts"] = new JArray(playerRoots.Count, enemyRoots.Count);
                var pairs = S026BuildCrossQuadrantRootPairs(playerRoots, enemyRoots, pairAttemptCap);
                sizeRow["selectedRootPairs"] = new JArray(pairs.Select((pair, index) => new JObject
                {
                    ["pairIndex"] = index, ["playerRoot"] = S026RootJson(pair.Player),
                    ["enemyRoot"] = S026RootJson(pair.Enemy),
                    ["totalSquaredDisplacementMetres"] = pair.TotalDistanceSquared,
                    ["candidateOnly"] = true
                }));
                sizeRow["fullRosterOriginalOwnerRecheck"] = RunS026ConstrainedFirstMiddleBandProbe(alias,
                    deploymentAnchors, em, gridEntity, grid, surface, roads, walkable, definition, matrix,
                    playerRoots, enemyRoots, false,
                    "IndependentFactionHelipadRefineryScreen_HelipadFirst_ExactOwnerThenCanonicalOrder",
                    true, size, pairAttemptCap, 2);
            }
            return report;
        }

        static void S026KeepBestModuleTileCandidate(List<S026ModuleRootTileCandidate> retained,
            S026ModuleRootTileCandidate candidate, int cap)
        {
            int insertion = retained.FindIndex(existing => S026CompareModuleTileCandidates(candidate, existing) < 0);
            if (insertion < 0)
            {
                if (retained.Count < cap) retained.Add(candidate);
                return;
            }
            retained.Insert(insertion, candidate);
            if (retained.Count > cap) retained.RemoveAt(retained.Count - 1);
        }

        static int S026ComparePlacementFallbackCandidates(S026PlacementFallbackCandidate a, S026PlacementFallbackCandidate b)
        {
            int distance = a.DistanceSquared.CompareTo(b.DistanceSquared);
            if (distance != 0) return distance;
            int tile = a.TileIndex.CompareTo(b.TileIndex);
            if (tile != 0) return tile;
            int z = a.SourceCell.y.CompareTo(b.SourceCell.y);
            return z != 0 ? z : a.SourceCell.x.CompareTo(b.SourceCell.x);
        }

        static int S026CompareDistanceBandPlacementCandidates(S026PlacementFallbackCandidate a,
            S026PlacementFallbackCandidate b)
        {
            int target = a.DistanceBandTargetDelta.CompareTo(b.DistanceBandTargetDelta);
            if (target != 0) return target;
            int distance = a.DistanceSquared.CompareTo(b.DistanceSquared);
            if (distance != 0) return distance;
            int z = a.SourceCell.y.CompareTo(b.SourceCell.y);
            return z != 0 ? z : a.SourceCell.x.CompareTo(b.SourceCell.x);
        }

        static void S026KeepOnePlacementFallbackCandidatePerDistanceBand(
            List<S026PlacementFallbackCandidate> retained, S026PlacementFallbackCandidate candidate, int cap)
        {
            int existingIndex = retained.FindIndex(existing => existing.DistanceBand == candidate.DistanceBand);
            if (existingIndex >= 0)
            {
                if (S026CompareDistanceBandPlacementCandidates(candidate, retained[existingIndex]) < 0)
                    retained[existingIndex] = candidate;
                return;
            }
            Require(retained.Count < cap, "Distance-band retention exceeded the per-tile sample cap");
            retained.Add(candidate);
        }

        static List<S026PlacementFallbackCandidate> S026FilterCachedInvalidFallbackCandidates(
            IEnumerable<S026PlacementFallbackCandidate> candidates, Vector2Int modelFootprint,
            Func<Vector2Int, Vector2Int, bool> cachedInvalid, out int skipped)
        {
            Require(candidates != null && cachedInvalid != null,
                "Fallback prefilter requires candidates and the original cached-invalid predicate");
            var retained = new List<S026PlacementFallbackCandidate>();
            skipped = 0;
            foreach (var candidate in candidates)
            {
                if (cachedInvalid(candidate.Origin, modelFootprint)) { skipped++; continue; }
                retained.Add(candidate);
            }
            return retained;
        }

        static int RunS026PlacementFallbackSelectionControls()
        {
            var candidates = new[]
            {
                new S026PlacementFallbackCandidate { TileIndex = 0, DistanceBand = 0, SourceCell = new Vector2Int(9, 1), DistanceSquared = 1, DistanceBandTargetDelta = .2f },
                new S026PlacementFallbackCandidate { TileIndex = 0, DistanceBand = 0, SourceCell = new Vector2Int(2, 2), DistanceSquared = 4, DistanceBandTargetDelta = .1f },
                new S026PlacementFallbackCandidate { TileIndex = 0, DistanceBand = 1, SourceCell = new Vector2Int(3, 3), DistanceSquared = 9, DistanceBandTargetDelta = .3f },
                new S026PlacementFallbackCandidate { TileIndex = 0, DistanceBand = 2, SourceCell = new Vector2Int(4, 4), DistanceSquared = 16, DistanceBandTargetDelta = .2f },
                new S026PlacementFallbackCandidate { TileIndex = 0, DistanceBand = 3, SourceCell = new Vector2Int(5, 5), DistanceSquared = 25, DistanceBandTargetDelta = .1f }
            };
            List<S026PlacementFallbackCandidate> Select(IEnumerable<S026PlacementFallbackCandidate> input)
            {
                var retained = new List<S026PlacementFallbackCandidate>();
                foreach (var candidate in input) S026KeepOnePlacementFallbackCandidatePerDistanceBand(retained, candidate, 4);
                return retained.OrderBy(candidate => candidate.DistanceBand).ToList();
            }
            var first = Select(candidates);
            var reversed = Select(candidates.Reverse());
            bool Expected(IReadOnlyList<S026PlacementFallbackCandidate> selected) => selected.Count == 4 &&
                selected.Select(candidate => candidate.DistanceBand).SequenceEqual(new[] { 0, 1, 2, 3 }) &&
                selected[0].SourceCell == new Vector2Int(2, 2) && selected[1].SourceCell == new Vector2Int(3, 3) &&
                selected[2].SourceCell == new Vector2Int(4, 4) && selected[3].SourceCell == new Vector2Int(5, 5);
            Require(Expected(first) && Expected(reversed),
                "Fallback per-tile cap must keep one closest-to-target radial quartile candidate per band deterministically");
            var withBlocked = first.Concat(new[] { new S026PlacementFallbackCandidate
            { TileIndex = 1, DistanceBand = 0, SourceCell = new Vector2Int(8, 8), Origin = new Vector2Int(7, 7) } }).ToArray();
            var filtered = S026FilterCachedInvalidFallbackCandidates(withBlocked, new Vector2Int(2, 2),
                (origin, footprint) => origin == new Vector2Int(7, 7) && footprint == new Vector2Int(2, 2),
                out int skipped);
            Require(skipped == 1 && filtered.Count == first.Count &&
                filtered.All(candidate => candidate.Origin != new Vector2Int(7, 7)),
                "Fallback prefilter must prune only candidates rejected by the exact cached-invalid owner predicate");
            return 1;
        }

        static JObject RunS026CandidateInstanceBindingProjection(OperationMapDefinition alias, GridConfig grid,
            SkirmishSizeId size, SkirmishResolvedStructureEntry[] canonicalStructures, JObject planByIndex)
        {
            var report = new JObject
            {
                ["method"] = "Existing SkirmishStructureInstanceBinding.TryApply",
                ["scope"] = "EphemeralCompleteInstancePadMap_IdentityProjectionForCandidateSourceCoordinates",
                ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                ["compilerAdmission"] = false, ["nativeMissionInitialized"] = false,
                ["layoutAssetCreatedOrSaved"] = false, ["budgetChanged"] = false,
                ["sourceMap"] = alias.OperationMapId,
                ["sourceBindingContentHash"] = alias.SourceBinding.SourceContentHash
            };
            SkirmishMapLayoutConfig layout = null;
            try
            {
                float width = alias.Bounds.PlayableMax.x - alias.Bounds.PlayableMin.x;
                float depth = alias.Bounds.PlayableMax.z - alias.Bounds.PlayableMin.z;
                Require(width > 0f && depth > 0f && grid.CellSize > 0f,
                    "Finite existing alias envelope and source grid required for candidate instance mapping");
                var anchors = new SkirmishLayoutAnchorConfig[canonicalStructures.Length];
                var pads = new SkirmishLegalPadConfig[canonicalStructures.Length];
                var bindings = new SkirmishStructureInstancePadBinding[canonicalStructures.Length];
                var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
                var candidateRows = new JArray(); report["candidateRows"] = candidateRows;
                for (int i = 0; i < canonicalStructures.Length; i++)
                {
                    var structure = canonicalStructures[i];
                    JObject plan = planByIndex[i.ToString()] as JObject;
                    Require(plan != null && (bool)plan["foundByOriginalOwner"],
                        "Full owner-validated placement row required before candidate instance binding");
                    var rect = plan["resolvedEffectiveRect"] as JObject;
                    Require(rect != null, "Owner-validated effective rectangle required before candidate instance binding");
                    int rectX = (int)rect["x"], rectZ = (int)rect["z"], rectWidth = (int)rect["width"], rectDepth = (int)rect["depth"];
                    Require(rectWidth > 0 && rectDepth > 0, "Positive owner effective footprint required for exact logical pad");
                    float centerX = grid.Origin.x + (rectX + rectWidth * .5f) * grid.CellSize;
                    float centerZ = grid.Origin.z + (rectZ + rectDepth * .5f) * grid.CellSize;
                    string stem = structure.FactionId + "/" + structure.StructureId;
                    int ordinal = ordinals.TryGetValue(stem, out var prior) ? prior : 0;
                    ordinals[stem] = ordinal + 1;
                    string anchorId = "anchor.s026.candidate." + size.ToString().ToLowerInvariant() + "." + i;
                    string padId = "pad.s026.candidate." + size.ToString().ToLowerInvariant() + "." + i;
                    string roleId = "structure.instance." + i;
                    anchors[i] = new SkirmishLayoutAnchorConfig
                    {
                        AnchorId = anchorId, RoleId = roleId,
                        NormalizedU = (centerX - alias.Bounds.PlayableMin.x) / width,
                        NormalizedV = (centerZ - alias.Bounds.PlayableMin.z) / depth,
                        WorldX = centerX, WorldZ = centerZ
                    };
                    pads[i] = new SkirmishLegalPadConfig
                    {
                        PadId = padId, AnchorId = anchorId, RoleId = roleId,
                        Kind = Game.Configs.SkirmishMapLayoutBuilder.PadKindForStructure(structure.StructureId),
                        CenterX = centerX, CenterZ = centerZ,
                        WidthMetres = rectWidth * grid.CellSize, DepthMetres = rectDepth * grid.CellSize,
                        FactionId = structure.FactionId
                    };
                    bindings[i] = new SkirmishStructureInstancePadBinding
                    {
                        SizeId = size, DifficultyId = SkirmishDifficultyId.Regular,
                        FactionId = structure.FactionId, StructureId = structure.StructureId,
                        InstanceOrdinal = ordinal, AnchorId = anchorId, PadId = padId
                    };
                    candidateRows.Add(new JObject
                    {
                        ["compilerIndex"] = i, ["faction"] = structure.FactionId,
                        ["structureId"] = structure.StructureId, ["instanceOrdinal"] = ordinal,
                        ["anchorId"] = anchorId, ["padId"] = padId,
                        ["centerWorldXZ"] = new JArray(centerX, centerZ),
                        ["effectiveFootprintCells"] = new JArray(rectWidth, rectDepth),
                        ["offsetsZeroedByExistingBinding"] = true
                    });
                }
                layout = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
                string layoutId = "layout.skirmish.s026.source_candidate." + size.ToString().ToLowerInvariant();
                // The Configs assembly intentionally keeps authoring mutators internal.
                // Populate only this newly-created, unsaved diagnostic object through
                // Unity's serialized-object API; no project asset is loaded or changed.
                var serializedLayout = new SerializedObject(layout);
                SerializedProperty Field(string name)
                {
                    var property = serializedLayout.FindProperty(name);
                    Require(property != null, "Expected serialized candidate layout field missing: " + name);
                    return property;
                }
                SerializedProperty ElementField(SerializedProperty element, string name)
                {
                    var property = element.FindPropertyRelative(name);
                    Require(property != null, "Expected serialized candidate row field missing: " + name);
                    return property;
                }
                Field("layoutId").stringValue = layoutId;
                Field("operationMapId").stringValue = alias.OperationMapId;
                Field("contentVersion").intValue = 1;
                Field("contentHash").stringValue = "source-owner-candidate-not-published";
                Field("worldWidthMetres").floatValue = width;
                Field("worldDepthMetres").floatValue = depth;
                Field("originX").floatValue = alias.Bounds.PlayableMin.x;
                Field("originZ").floatValue = alias.Bounds.PlayableMin.z;
                Field("cellSize").floatValue = grid.CellSize;
                var anchorArray = Field("anchors");
                anchorArray.arraySize = anchors.Length;
                for (int i = 0; i < anchors.Length; i++)
                {
                    var element = anchorArray.GetArrayElementAtIndex(i);
                    ElementField(element, "AnchorId").stringValue = anchors[i].AnchorId;
                    ElementField(element, "RoleId").stringValue = anchors[i].RoleId;
                    ElementField(element, "NormalizedU").floatValue = anchors[i].NormalizedU;
                    ElementField(element, "NormalizedV").floatValue = anchors[i].NormalizedV;
                    ElementField(element, "WorldX").floatValue = anchors[i].WorldX;
                    ElementField(element, "WorldZ").floatValue = anchors[i].WorldZ;
                }
                var routeArray = Field("routes");
                routeArray.arraySize = 0;
                var padArray = Field("pads");
                padArray.arraySize = pads.Length;
                for (int i = 0; i < pads.Length; i++)
                {
                    var element = padArray.GetArrayElementAtIndex(i);
                    ElementField(element, "PadId").stringValue = pads[i].PadId;
                    ElementField(element, "AnchorId").stringValue = pads[i].AnchorId;
                    ElementField(element, "RoleId").stringValue = pads[i].RoleId;
                    ElementField(element, "Kind").intValue = (int)pads[i].Kind;
                    ElementField(element, "CenterX").floatValue = pads[i].CenterX;
                    ElementField(element, "CenterZ").floatValue = pads[i].CenterZ;
                    ElementField(element, "WidthMetres").floatValue = pads[i].WidthMetres;
                    ElementField(element, "DepthMetres").floatValue = pads[i].DepthMetres;
                    ElementField(element, "FactionId").intValue = pads[i].FactionId;
                }
                var binding = Field("worldBinding");
                ElementField(binding, "OriginX").floatValue = alias.Bounds.PlayableMin.x;
                ElementField(binding, "OriginZ").floatValue = alias.Bounds.PlayableMin.z + depth * .5f;
                ElementField(binding, "ForwardX").floatValue = 1f;
                ElementField(binding, "ForwardZ").floatValue = 0f;
                ElementField(binding, "AcrossX").floatValue = 0f;
                ElementField(binding, "AcrossZ").floatValue = 1f;
                ElementField(binding, "ForwardMetres").floatValue = width;
                ElementField(binding, "AcrossMetres").floatValue = depth;
                var sourceDeployment = alias.Anchors.ToArray().Where(a => a.Kind == OperationMapAnchorKind.Deployment)
                    .OrderBy(a => a.FactionId).ToArray();
                Require(sourceDeployment.Length == 2, "Exact source deployment anchors required for candidate logical frame");
                ElementField(binding, "PlayerAnchorId").stringValue = sourceDeployment[0].AnchorId;
                ElementField(binding, "EnemyAnchorId").stringValue = sourceDeployment[1].AnchorId;
                ElementField(binding, "SourceHash").stringValue = alias.SourceBinding.SourceContentHash;
                ElementField(binding, "HasMeasuredDeployments").boolValue = false;
                ElementField(binding, "PlayerDeployment").vector2Value = new Vector2(
                    sourceDeployment[0].Position.x, sourceDeployment[0].Position.z);
                ElementField(binding, "EnemyDeployment").vector2Value = new Vector2(
                    sourceDeployment[1].Position.x, sourceDeployment[1].Position.z);
                ElementField(binding, "BindAllConfigurations").boolValue = false;
                serializedLayout.ApplyModifiedPropertiesWithoutUndo();
                layout.BindStructureInstancePads(bindings);
                var setup = new SkirmishResolvedSetup
                {
                    CatalogId = "S026", OperationMapId = alias.OperationMapId, LayoutId = layoutId,
                    SizeId = size, DifficultyId = SkirmishDifficultyId.Regular,
                    Structures = (SkirmishResolvedStructureEntry[])canonicalStructures.Clone()
                };
                var reasons = new List<SkirmishCompileReason>();
                bool applied = SkirmishStructureInstanceBinding.TryApply(layout, setup, reasons);
                bool exactPositions = applied && setup.Structures.Length == canonicalStructures.Length &&
                    setup.Structures.Select((row, i) => row.StructureId == canonicalStructures[i].StructureId &&
                        row.FactionId == canonicalStructures[i].FactionId && row.AcrossOffsetMetres == 0f &&
                        row.ForwardOffsetMetres == 0f && row.SpawnWorldX == (float)candidateRows[i]["centerWorldXZ"][0] &&
                        row.SpawnWorldZ == (float)candidateRows[i]["centerWorldXZ"][1]).All(value => value);
                report["tryApplyPassed"] = applied;
                report["completeCanonicalStructureDomain"] = setup.Structures.Length == canonicalStructures.Length;
                report["exactCoordinatesPreserved"] = exactPositions;
                report["allOffsetsZero"] = applied && setup.Structures.All(row => row.AcrossOffsetMetres == 0f && row.ForwardOffsetMetres == 0f);
                report["reasons"] = new JArray(reasons.Select(reason => reason.Field + ":" + reason.Detail));
                report["result"] = exactPositions ? "CompleteCandidateInstanceBindingMapped_Unqualified" : "CandidateInstanceBindingRejected";
            }
            catch (Exception exception)
            {
                report["result"] = "CandidateInstanceBindingDiagnosticFailed";
                report["error"] = exception.GetType().Name + ": " + exception.Message;
            }
            finally { if (layout != null) UnityEngine.Object.DestroyImmediate(layout); }
            return report;
        }

        static int RunS026ModuleTileSelectionControls()
        {
            var rows = new[]
            {
                new S026ModuleRootTileCandidate { Root = new S026AlternativeRoot(2, new int2(4, 8), new float2(4.5f, 8.5f), 4, -1, -1, 0) },
                new S026ModuleRootTileCandidate { Root = new S026AlternativeRoot(2, new int2(2, 9), new float2(2.5f, 9.5f), 1, -1, -1, 0) },
                new S026ModuleRootTileCandidate { Root = new S026AlternativeRoot(2, new int2(3, 1), new float2(3.5f, 1.5f), 1, -1, -1, 0) },
                new S026ModuleRootTileCandidate { Root = new S026AlternativeRoot(2, new int2(8, 5), new float2(8.5f, 5.5f), 9, -1, -1, 0) }
            };
            var first = S026SelectModuleTileCandidates(rows, 3);
            var second = S026SelectModuleTileCandidates(rows.Reverse(), 3);
            int[] expectedZx = { 1 * 100 + 3, 9 * 100 + 2, 8 * 100 + 4 };
            Require(first.Count == 3 && first.Select(candidate => candidate.Root.Cell.y * 100 + candidate.Root.Cell.x)
                    .SequenceEqual(expectedZx) &&
                second.Select(candidate => candidate.Root.Cell.y * 100 + candidate.Root.Cell.x).SequenceEqual(expectedZx),
                "Module tile candidate cap/order must be deterministic by distance then source cell z/x");
            return 1;
        }

        static JObject RunS026ModuleFirstDerivedEnemyRootSearch(OperationMapDefinition alias,
            OperationMapAnchorConfig[] deploymentAnchors, EntityManager em, Entity gridEntity, GridConfig grid,
            MapSurfaceComponent surface, DynamicBuffer<GridRoad> roads, NativeArray<GridWalkable> walkable,
            SkirmishScenarioDefinitionConfig definition, IReadOnlyList<SkirmishSetupMatrixRow> matrix)
        {
            const int tileColumns = 4, tileRows = 4, candidatesPerTileCap = 4, selectedRootCap = 16;
            int selectionControls = RunS026ModuleTileSelectionControls();
            var report = new JObject
            {
                ["scope"] = "SourceGridEnemyHalf_RefineryModuleExactOwnerScreen_ThenConstrainedFirstFullRosterAndCanonicalRecheck",
                ["candidateOnly"] = true, ["sourceFitCertified"] = false, ["normalPlacementAcceptance"] = false,
                ["routeEvidence"] = false, ["nativeMissionInitialized"] = false, ["liveOccupancyIncluded"] = false,
                ["spatialTileGrid"] = new JArray(tileColumns, tileRows),
                ["candidateRootsPerTileCap"] = candidatesPerTileCap, ["selectedOwnerValidRootsCap"] = selectedRootCap,
                ["exactModuleOwnerChecksCap"] = tileColumns * tileRows * candidatesPerTileCap,
                ["selectionControlsPassed"] = selectionControls,
                ["candidateOrder"] = "Within each alias world-space tile, nearest root candidate to existing enemy deployment, then source cell z/x; stop after first exact-origin module owner success",
                ["moduleScreen"] = "Exact preferred-origin original placement owner call, radius 0, source geometry only with no earlier structure reservations; every retained root is subsequently subjected to full constrained-first reservations and all compiler-order exact-origin rechecks",
                ["budgetOrGeometryChanged"] = false
            };
            var anchors = deploymentAnchors.OrderBy(a => a.FactionId).ToArray();
            Require(anchors.Length == 2 && anchors[0].FactionId == 1 && anchors[1].FactionId == 2,
                "Exact player/enemy source deployment anchors required for module-first root derivation");
            float2 playerOriginal = new float2(anchors[0].Position.x, anchors[0].Position.z);
            float2 enemyOriginal = new float2(anchors[1].Position.x, anchors[1].Position.z);
            float2 forward = math.normalize(enemyOriginal - playerOriginal);
            float2 midpoint = (playerOriginal + enemyOriginal) * .5f;
            float2 across = new float2(-forward.y, forward.x);

            Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition, SkirmishDifficultyId.Regular,
                SkirmishSizeId.War, 1, new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds },
                matrix, out var budget, out var reasons) && budget != null && budget.ExactCanonicalQuantities,
                "Exact War module definition unavailable for source candidate derivation: " +
                string.Join(";", (reasons ?? new List<SkirmishCompileReason>()).Select(r => r.Field + ":" + r.Detail)));
            var enemyModule = budget.Structures.Single(s => s.FactionId == 2 && s.StructureId == SkirmishStructureIds.RefineryModule);
            Require(enemyModule.AcrossOffsetMetres == 0f && enemyModule.ForwardOffsetMetres == 24f,
                "Expected current canonical enemy RefineryModule offset must be pinned before candidate-root inversion");

            var prefabPaths = S026StructurePrefabPaths();
            string visualKey = SkirmishStructureIds.VisualKey(enemyModule.StructureId);
            Require(prefabPaths.TryGetValue(visualKey, out var prefabPath), "Exact RefineryModule visual prefab path missing");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Require(prefab != null && !EditorUtility.IsDirty(prefab) && prefab.name == visualKey,
                "Exact clean RefineryModule visual prefab unavailable: " + prefabPath);
            var definitionFactory = new BuildingDefinitionPrefabSystemHelper();
            definitionFactory.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var runway = new BuildingRunwaySystem();
            var geometry = new BuildingPlacementGridCameraSystemHelper();
            var building = definitionFactory.CreateRuntimeBuildingDefinition(prefab, prefab.name,
                string.Empty, Vector2Int.one, 1, runway);
            Require(building != null, "Original definition factory rejected RefineryModule candidate prefab");
            Vector2Int footprint = geometry.GetPlacementFootprint(building, false);
            int minX = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.x - grid.Origin.x) / grid.CellSize));
            int maxX = Mathf.Min(grid.Width - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.x - grid.Origin.x) / grid.CellSize));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.z - grid.Origin.z) / grid.CellSize));
            int maxZ = Mathf.Min(grid.Height - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.z - grid.Origin.z) / grid.CellSize));
            float tileWidth = (alias.Bounds.PlayableMax.x - alias.Bounds.PlayableMin.x) / tileColumns;
            float tileDepth = (alias.Bounds.PlayableMax.z - alias.Bounds.PlayableMin.z) / tileRows;
            var candidates = Enumerable.Range(0, tileColumns * tileRows).ToDictionary(i => i, _ => new List<S026ModuleRootTileCandidate>());
            long scanned = 0, outsideAlias = 0, playerHalf = 0, unwalkable = 0, surfaceRejected = 0;
            int derivedOriginOutsideGrid = 0;
            var traversal = new MapSurfaceTraversalValidation();
            for (int z = minZ; z <= maxZ; z++)
            for (int x = minX; x <= maxX; x++)
            {
                scanned++;
                float wx = grid.Origin.x + (x + .5f) * grid.CellSize;
                float wz = grid.Origin.z + (z + .5f) * grid.CellSize;
                if (!IsInsideAlias(alias, wx, wz)) { outsideAlias++; continue; }
                float2 rootWorld = new float2(wx, wz);
                if (math.dot(rootWorld - midpoint, forward) <= 0f) { playerHalf++; continue; }
                int index = z * grid.Width + x;
                if (walkable[index].Value == 0) { unwalkable++; continue; }
                var rootCell = new int2(x, z);
                if (!traversal.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, rootCell, new int2(1, 1), false))
                { surfaceRejected++; continue; }

                float moduleCenterX = rootWorld.x + enemyModule.AcrossOffsetMetres * across.x + enemyModule.ForwardOffsetMetres * forward.x;
                float moduleCenterZ = rootWorld.y + enemyModule.AcrossOffsetMetres * across.y + enemyModule.ForwardOffsetMetres * forward.y;
                int2 moduleCenterCell = GridUtils.WorldToCell(grid, new float3(moduleCenterX, grid.Origin.y, moduleCenterZ));
                Vector2Int moduleOrigin = new Vector2Int(moduleCenterCell.x - footprint.x / 2, moduleCenterCell.y - footprint.y / 2);
                if (moduleOrigin.x < 0 || moduleOrigin.y < 0 || moduleOrigin.x + footprint.x > grid.Width || moduleOrigin.y + footprint.y > grid.Height)
                { derivedOriginOutsideGrid++; continue; }
                float tileXValue = (rootWorld.x - alias.Bounds.PlayableMin.x) / tileWidth;
                float tileZValue = (rootWorld.y - alias.Bounds.PlayableMin.z) / tileDepth;
                int tileX = Mathf.Clamp(Mathf.FloorToInt(tileXValue), 0, tileColumns - 1);
                int tileZ = Mathf.Clamp(Mathf.FloorToInt(tileZValue), 0, tileRows - 1);
                int tileIndex = tileZ * tileColumns + tileX;
                float distanceSquared = math.lengthsq(rootWorld - enemyOriginal);
                S026KeepBestModuleTileCandidate(candidates[tileIndex], new S026ModuleRootTileCandidate
                {
                    TileIndex = tileIndex,
                    Root = new S026AlternativeRoot(2, rootCell, rootWorld, distanceSquared, -1, -1, tileIndex),
                    ModuleOrigin = moduleOrigin
                }, candidatesPerTileCap);
            }

            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            var cache = new BuildingPlacementInvalidCellCacheCompositionSystemHelper();
            cache.EnsureAuthoredGeometry(em, surfaces, grid);
            Require(cache.AuthoredBuildings.IsReady, "Actual source authored-building cache unavailable for module-first screen");
            var blockers = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var ownerScreenRows = new JArray(); report["ownerScreenRows"] = ownerScreenRows;
            var selectedEnemyRoots = new List<S026AlternativeRoot>();
            var selectedModuleOrigins = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
            try
            {
                for (int i = 0; i < walkable.Length; i++) if (walkable[i].Value == 0) blockers.Set(i, true);
                var blocker = new DynamicBlockerComponent { Blocked = blockers };
                var startup = new BuildingPlacementStartupSystemHelper();
                var dependency = new BuildingGameplayDependencyCompositionSystemHelper();
                var invalidPrefixMask = new bool[grid.Width * grid.Height];
                startup.FillRoadFootprintMask(grid, invalidPrefixMask);
                cache.AuthoredBuildings.AppendTo(invalidPrefixMask); cache.RoadSurfaces.AppendTo(invalidPrefixMask);
                cache.RoadSurfaces.AppendWaterTo(invalidPrefixMask);
                int[] invalidPrefix = null;
                BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid, roads, blocker,
                    invalidPrefixMask, dependency.IsRuntimeBlockerCell, ref invalidPrefix,
                    out int prefixWidth, out int prefixHeight, out bool prefixReady);
                Require(prefixReady, "Original invalid-prefix owner unavailable for module-first screen");
                RectInt Effective(BuildingDefinition candidateBuilding, Vector2Int origin, GridConfig g, bool rotated) =>
                    runway.GetEffectivePlacementRect(candidateBuilding, origin, g, rotated, grid.Origin.y, geometry.GetPlacementFootprint);
                bool InsideAliasRect(RectInt rect)
                {
                    if (rect.width <= 0 || rect.height <= 0) return false;
                    for (int z = rect.yMin; z < rect.yMax; z++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        if (!GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)) return false;
                        float cx = grid.Origin.x + (x + .5f) * grid.CellSize;
                        float cz = grid.Origin.z + (z + .5f) * grid.CellSize;
                        if (!IsInsideAlias(alias, cx, cz)) return false;
                    }
                    return true;
                }
                var context = new BuildingRuntimeSpawnCompositionSystemHelper.Context(null, null, null, null, default,
                    null, geometry.GetPlacementFootprint,
                    (candidateBuilding, origin, g, rotated) => runway.GetEffectivePlacementRect(candidateBuilding,
                        origin, g, rotated, grid.Origin.y, geometry.GetPlacementFootprint),
                    (candidateBuilding, origin, modelFootprint, rotated, g, nativeRoads, nativeBlocker) =>
                    {
                        RectInt rect = Effective(candidateBuilding, origin, g, rotated);
                        return InsideAliasRect(rect) && BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect) &&
                            cache.IsPlacementValid(candidateBuilding, origin, modelFootprint, rotated, g,
                                nativeRoads, nativeBlocker, dependency, startup, Effective, _ => false);
                    },
                    (origin, modelFootprint) => BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                        invalidPrefix, prefixWidth, prefixHeight, origin, modelFootprint),
                    null, null, null, null);

                foreach (var tile in candidates.OrderBy(kv => kv.Key))
                {
                    var tileRow = new JObject
                    {
                        ["tileIndex"] = tile.Key, ["candidateRootsRetainedForTile"] = tile.Value.Count,
                        ["candidateOwnerChecks"] = new JArray(), ["selectedOwnerValidRoot"] = false
                    };
                    ownerScreenRows.Add(tileRow);
                    var checks = (JArray)tileRow["candidateOwnerChecks"];
                    foreach (var candidate in tile.Value)
                    {
                        int cached = 0, reached = 0;
                        bool found = new BuildingRuntimeSpawnCompositionSystemHelper().TryFindValidInitialBuildingOrigin(
                            context, building, candidate.ModuleOrigin, false, grid, roads, blocker,
                            out var resolved, true, false, false, 0,
                            (origin, rect, cachedInvalid) => { if (cachedInvalid) cached++; else reached++; });
                        var check = new JObject
                        {
                            ["tileIndex"] = candidate.TileIndex, ["rootCell"] = new JArray(candidate.Root.Cell.x, candidate.Root.Cell.y),
                            ["rootWorldXZ"] = new JArray(candidate.Root.World.x, candidate.Root.World.y),
                            ["modulePreferredOrigin"] = new JArray(candidate.ModuleOrigin.x, candidate.ModuleOrigin.y),
                            ["moduleFootprint"] = new JArray(footprint.x, footprint.y),
                            ["ownerExactOriginFound"] = found, ["ownerResolvedOrigin"] = found ? new JArray(resolved.x, resolved.y) : null,
                            ["cachedInvalidCandidates"] = cached, ["originalPlacementPredicateCandidates"] = reached,
                            ["candidateOnly"] = true
                        };
                        checks.Add(check);
                        if (!found) continue;
                        Require(resolved == candidate.ModuleOrigin, "Exact module owner screening moved the preferred origin");
                        selectedEnemyRoots.Add(candidate.Root);
                        selectedModuleOrigins[candidate.Root.Cell.x + "," + candidate.Root.Cell.y] = candidate.ModuleOrigin;
                        tileRow["selectedOwnerValidRoot"] = true;
                        tileRow["selectedRootCell"] = new JArray(candidate.Root.Cell.x, candidate.Root.Cell.y);
                        break;
                    }
                }
            }
            finally { blockers.Dispose(); }

            var rootCounts = new JObject();
            var playerRoots = S026FindMiddleBandQuadrantRoots(alias, grid, walkable, surface, 1,
                playerOriginal, midpoint, forward, rootCounts).Where(r => r.QuadrantIndex == 2).ToList();
            Require(playerRoots.Count == 1, "Existing source-derived q2 player root required for full compiler-order checks");
            report["playerRootSourceScanCounts"] = rootCounts;
            Require(selectedEnemyRoots.Count <= selectedRootCap, "Module-first root selection exceeded its cap");
            report["sourceScan"] = new JObject
            {
                ["scannedGridCells"] = scanned, ["outsideAlias"] = outsideAlias, ["playerHalfOrBisector"] = playerHalf,
                ["sourceUnwalkable"] = unwalkable, ["surfaceNotTraversable"] = surfaceRejected,
                ["derivedModuleOriginOutsideGrid"] = derivedOriginOutsideGrid,
                ["nonemptyCandidateTileCount"] = candidates.Count(kv => kv.Value.Count > 0),
                ["candidateRootsRetainedBeforeOwnerScreen"] = candidates.Sum(kv => kv.Value.Count),
                ["candidateOriginsRetainedPerTileCap"] = candidatesPerTileCap,
                ["rootFrame"] = "Existing deployment-to-deployment forward/across; inverse of the canonical enemy module offset at the source cell center"
            };
            report["selectedEnemyRootCount"] = selectedEnemyRoots.Count;
            report["selectedEnemyRoots"] = new JArray(selectedEnemyRoots.Select(S026RootJson));
            var placementRecheck = RunS026ConstrainedFirstMiddleBandProbe(alias, deploymentAnchors,
                em, gridEntity, grid, surface, roads, walkable, definition, matrix, playerRoots,
                selectedEnemyRoots, true,
                "ModuleOwnerScreenedEnemyHalfTileRoots_ConstrainedFirstReservationsThenCanonicalExactRecheck_WarAndLargeWar",
                true);
            report["placementRecheck"] = placementRecheck;
            var recheck = placementRecheck["sizes"] as JArray;
            if (recheck != null)
            foreach (JObject sizeRow in recheck.OfType<JObject>())
            foreach (JObject attempt in ((JArray)sizeRow["pairAttempts"]).OfType<JObject>())
            {
                int2 enemyCell = new int2((int)attempt["enemyRoot"]["cell"][0], (int)attempt["enemyRoot"]["cell"][1]);
                string key = enemyCell.x + "," + enemyCell.y;
                bool hasScreen = selectedModuleOrigins.TryGetValue(key, out var screenedModuleOrigin);
                JObject modulePlan = ((JArray)attempt["constrainedFirstPlanningRows"]).OfType<JObject>()
                    .FirstOrDefault(row => (byte)row["faction"] == 2 && (string)row["structureId"] == SkirmishStructureIds.RefineryModule);
                bool exactRootBinding = hasScreen && modulePlan != null && modulePlan["preferredOrigin"] is JArray preferred &&
                    preferred.Count == 2 && (int)preferred[0] == screenedModuleOrigin.x && (int)preferred[1] == screenedModuleOrigin.y;
                attempt["moduleFirstScreenOrigin"] = hasScreen ? new JArray(screenedModuleOrigin.x, screenedModuleOrigin.y) : null;
                attempt["fullRosterPreferredModuleOriginMatchesScreenedRoot"] = exactRootBinding;
            }
            report["interpretation"] = "The module owner screen has no previous structure reservations and only establishes a candidate root source. Full constrained-first reservations and compiler-order exact-origin rechecks are reported separately; sampled source roots do not establish overall source fit.";
            return report;
        }

        static JObject RunS026ConstrainedFirstMiddleBandProbe(OperationMapDefinition alias,
            OperationMapAnchorConfig[] deploymentAnchors, EntityManager em, Entity gridEntity, GridConfig grid,
            MapSurfaceComponent surface, DynamicBuffer<GridRoad> roads, NativeArray<GridWalkable> walkable,
            SkirmishScenarioDefinitionConfig definition, IReadOnlyList<SkirmishSetupMatrixRow> matrix,
            List<S026AlternativeRoot> playerRootsOverride = null,
            List<S026AlternativeRoot> enemyRootsOverride = null,
            bool warSizesOnly = false, string scopeOverride = null,
            bool fillUnplacedBySourceSearch = false, SkirmishSizeId? sizeFilter = null,
            int pairAttemptLimit = 16, int fallbackStructureScanCap = 22)
        {
            const int quadrantCapPerFaction = 4;
            const int defaultRootPairCap = 16;
            int rootPairCap = Mathf.Clamp(pairAttemptLimit, 1, defaultRootPairCap);
            const int structureSearchRadiusCap = 24;
            int controls = RunS026ConstrainedFirstOrderControls() + RunS026CrossQuadrantPairControls() +
                (fillUnplacedBySourceSearch ? RunS026PlacementFallbackSelectionControls() : 0);
            var report = new JObject
            {
                ["scope"] = scopeOverride ?? "MiddleBandQuadrantRoots_ConstrainedFirstOriginalOwnerReservationThenCanonicalExactRecheck",
                ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                ["routeEvidence"] = false, ["normalPlacementAcceptance"] = false,
                ["nativeMissionInitialized"] = false, ["liveOccupancyIncluded"] = false,
                ["controlsPassed"] = controls,
                ["sourceAnnulusMetres"] = playerRootsOverride == null && enemyRootsOverride == null ? new JArray(48, 96) : null,
                ["rootsPerFactionCap"] = playerRootsOverride == null && enemyRootsOverride == null
                    ? quadrantCapPerFaction : Math.Max(playerRootsOverride?.Count ?? 0, enemyRootsOverride?.Count ?? 0),
                ["pairAttemptCapPerSize"] = rootPairCap,
                ["placementSearchRadiusCells"] = structureSearchRadiusCap,
                ["planningOrder"] = fillUnplacedBySourceSearch
                    ? "Helipad, Refinery, Barracks, RefineryModule, FabricationDepot, then stable compiler order; bounded candidate heuristic only"
                    : "Refinery, RefineryModule, FabricationDepot first; stable compiler row index within priority; remaining rows then compiler index",
                ["verificationOrder"] = "Exact preferred-origin original-owner recheck in untouched compiler row order with fresh cumulative reservations",
                ["spatialSampling"] = playerRootsOverride == null && enemyRootsOverride == null
                    ? "Nearest eligible source-traversable cell in each across/forward sign quadrant of the 48–96m annulus; every retained player root paired with every retained enemy root, including cross-quadrant pairs"
                    : "Uses explicitly supplied source-derived root candidates; all candidate roots are still checked by the original placement owner and rechecked in canonical order"
            };
            report["fallbackSourceSearchEnabled"] = fillUnplacedBySourceSearch;
            if (fillUnplacedBySourceSearch)
            {
                report["fallbackSpatialTileGrid"] = new JArray(4, 4);
                report["fallbackCandidateOriginsPerTileCap"] = 4;
                report["fallbackOwnerChecksPerMissingStructureCap"] = 64;
                report["fallbackStructuresScannedPerPairCap"] = Mathf.Clamp(fallbackStructureScanCap, 0, 22);
                report["fallbackCandidateSampling"] = "One retained source-traversable exact origin closest to each radial distance-quartile target within each alias 4x4 tile; no more than four candidates per tile";
                report["fallbackDistanceBandTargetFractionsOfTileCornerRadius"] = new JArray(.125f, .375f, .625f, .875f);
                report["fallbackOwnerSearch"] = "Helipad, Refinery, and Barracks are planned first for this bounded candidate heuristic. Each missing structure gets at most four distance-band representatives per alias tile in its faction half-plane; cached-invalid origins are pruned with the exact owner prefix predicate before original owner exact-origin validation, which includes current cumulative reservations; all rows receive canonical compiler-order recheck";
            }
            var anchors = deploymentAnchors.OrderBy(a => a.FactionId).ToArray();
            Require(anchors.Length == 2 && anchors[0].FactionId == 1 && anchors[1].FactionId == 2,
                "Exact player/enemy source deployment anchors required");
            float2 playerOriginal = new float2(anchors[0].Position.x, anchors[0].Position.z);
            float2 enemyOriginal = new float2(anchors[1].Position.x, anchors[1].Position.z);
            float2 forward = math.normalize(enemyOriginal - playerOriginal);
            float2 midpoint = (playerOriginal + enemyOriginal) * .5f;
            var sourceScanCounts = new JObject(); report["sourceScanCounts"] = sourceScanCounts;
            var playerRoots = playerRootsOverride ?? S026FindMiddleBandQuadrantRoots(alias, grid, walkable, surface, 1,
                playerOriginal, midpoint, forward, sourceScanCounts);
            var enemyRoots = enemyRootsOverride ?? S026FindMiddleBandQuadrantRoots(alias, grid, walkable, surface, 2,
                enemyOriginal, midpoint, forward, sourceScanCounts);
            var pairs = S026BuildCrossQuadrantRootPairs(playerRoots, enemyRoots, rootPairCap);
            report["candidateRoots"] = new JArray(playerRoots.Concat(enemyRoots).Select(S026RootJson));
            report["selectedRootPairs"] = new JArray(pairs.Select((pair, index) => new JObject
            {
                ["pairIndex"] = index,
                ["playerQuadrantIndex"] = pair.Player.QuadrantIndex < 0 ? JValue.CreateNull() : new JValue(pair.Player.QuadrantIndex),
                ["enemyQuadrantIndex"] = pair.Enemy.QuadrantIndex < 0 ? JValue.CreateNull() : new JValue(pair.Enemy.QuadrantIndex),
                ["crossQuadrantPair"] = pair.Player.QuadrantIndex < 0 || pair.Enemy.QuadrantIndex < 0
                    ? JValue.CreateNull() : new JValue(pair.Player.QuadrantIndex != pair.Enemy.QuadrantIndex),
                ["playerRoot"] = S026RootJson(pair.Player), ["enemyRoot"] = S026RootJson(pair.Enemy),
                ["totalSquaredDisplacementMetres"] = pair.TotalDistanceSquared, ["candidateOnly"] = true
            }));

            Require(matrix != null && matrix.Count > 0, "Exact canonical setup matrix required for constrained-first probe");
            var prefabPaths = S026StructurePrefabPaths();
            var definitionFactory = new BuildingDefinitionPrefabSystemHelper();
            definitionFactory.ConfigureAuthoringMetadataResolvers(BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata,
                BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetUnitDefinitionMetadata);
            var runway = new BuildingRunwaySystem(); var geometry = new BuildingPlacementGridCameraSystemHelper();
            var search = new BuildingRuntimeSpawnCompositionSystemHelper();
            float2 across = new float2(-forward.y, forward.x);
            var sizeRows = new JArray(); report["sizes"] = sizeRows;
            IEnumerable<SkirmishSizeId> sizesToInspect = sizeFilter.HasValue
                ? new[] { sizeFilter.Value }
                : warSizesOnly
                    ? new[] { SkirmishSizeId.War, SkirmishSizeId.LargeWar }
                    : new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar };
            foreach (SkirmishSizeId size in sizesToInspect)
            {
                Require(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition, SkirmishDifficultyId.Regular, size, 1,
                    new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                    out var budget, out var reasons) && budget != null && budget.ExactCanonicalQuantities,
                    "Exact canonical S026 roster unavailable in constrained-first probe for " + size + ": " +
                    string.Join(";", (reasons ?? new List<SkirmishCompileReason>()).Select(r => r.Field + ":" + r.Detail)));
                var sizeRow = new JObject
                {
                    ["size"] = size.ToString(), ["normalCompilerPassed"] = budget.NormalCompilerPassed,
                    ["exactCanonicalQuantities"] = budget.ExactCanonicalQuantities,
                    ["canonicalStructureCount"] = budget.Structures.Length,
                    ["canonicalCompilerOrder"] = new JArray(budget.Structures.Select((s, i) => new JObject
                    { ["compilerIndex"] = i, ["faction"] = s.FactionId, ["structureId"] = s.StructureId,
                        ["spawnAnchorId"] = s.SpawnAnchorId, ["acrossOffsetMetres"] = s.AcrossOffsetMetres,
                        ["forwardOffsetMetres"] = s.ForwardOffsetMetres })),
                    ["constrainedFirstOrderCompilerIndices"] = new JArray(
                        S026ConstrainedFirstStructureOrder(budget.Structures, fillUnplacedBySourceSearch)),
                    ["candidateOnly"] = true, ["sourceFitCertified"] = false
                };
                sizeRows.Add(sizeRow);
                var pairRows = new JArray(); sizeRow["pairAttempts"] = pairRows;
                foreach (var pair in pairs)
                {
                    var attempt = new JObject
                    {
                        ["playerQuadrantIndex"] = pair.Player.QuadrantIndex < 0 ? JValue.CreateNull() : new JValue(pair.Player.QuadrantIndex),
                        ["enemyQuadrantIndex"] = pair.Enemy.QuadrantIndex < 0 ? JValue.CreateNull() : new JValue(pair.Enemy.QuadrantIndex),
                        ["crossQuadrantPair"] = pair.Player.QuadrantIndex < 0 || pair.Enemy.QuadrantIndex < 0
                            ? JValue.CreateNull() : new JValue(pair.Player.QuadrantIndex != pair.Enemy.QuadrantIndex),
                        ["playerRoot"] = S026RootJson(pair.Player), ["enemyRoot"] = S026RootJson(pair.Enemy),
                        ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                        ["normalPlacementAcceptance"] = false
                    };
                    pairRows.Add(attempt);
                    var planningRows = new JArray(); attempt["constrainedFirstPlanningRows"] = planningRows;
                    var canonicalRows = new JArray(); attempt["canonicalOrderExactRecheckRows"] = canonicalRows;
                    var planByIndex = new JObject(); attempt["planByCompilerIndex"] = planByIndex;
                    var sourceReservations = new List<RectInt>();
                    var runtimeReservations = new List<RectInt>();
                    using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
                    var cache = new BuildingPlacementInvalidCellCacheCompositionSystemHelper();
                    cache.EnsureAuthoredGeometry(em, surfaces, grid);
                    Require(cache.AuthoredBuildings.IsReady, "Actual source authored-building cache unavailable");
                    var blockers = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    try
                    {
                        for (int i = 0; i < walkable.Length; i++) if (walkable[i].Value == 0) blockers.Set(i, true);
                        var blocker = new DynamicBlockerComponent { Blocked = blockers };
                        var startup = new BuildingPlacementStartupSystemHelper();
                        var dependency = new BuildingGameplayDependencyCompositionSystemHelper();
                        var invalidPrefixMask = new bool[grid.Width * grid.Height];
                        startup.FillRoadFootprintMask(grid, invalidPrefixMask);
                        cache.AuthoredBuildings.AppendTo(invalidPrefixMask); cache.RoadSurfaces.AppendTo(invalidPrefixMask);
                        cache.RoadSurfaces.AppendWaterTo(invalidPrefixMask);
                        int[] invalidPrefix = null;
                        BuildingPlacementValidationUtilitySystemHelper.RebuildInvalidPrefix(grid, roads, blocker,
                            invalidPrefixMask, dependency.IsRuntimeBlockerCell, ref invalidPrefix,
                            out int prefixWidth, out int prefixHeight, out bool prefixReady);
                        Require(prefixReady, "Original invalid-prefix owner unavailable for candidate-order passes");
                        var buildingDefinitions = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);

                        RectInt Effective(BuildingDefinition building, Vector2Int origin, GridConfig g, bool rotated) =>
                            runway.GetEffectivePlacementRect(building, origin, g, rotated, grid.Origin.y, geometry.GetPlacementFootprint);
                        bool Inside(RectInt rect)
                        {
                            if (rect.width <= 0 || rect.height <= 0) return false;
                            for (int z = rect.yMin; z < rect.yMax; z++)
                            for (int x = rect.xMin; x < rect.xMax; x++)
                            {
                                if (!GridUtils.InBounds(new int2(x, z), grid.Width, grid.Height)) return false;
                                float cellX = grid.Origin.x + (x + .5f) * grid.CellSize;
                                float cellZ = grid.Origin.z + (z + .5f) * grid.CellSize;
                                if (!IsInsideAlias(alias, cellX, cellZ)) return false;
                            }
                            return true;
                        }
                        void CountRejection(IDictionary<string, int> counters, string reason)
                        {
                            if (counters == null) return;
                            counters.TryGetValue(reason, out int count);
                            counters[reason] = count + 1;
                        }
                        JObject RejectionCountsJson(IDictionary<string, int> counters)
                        {
                            var json = new JObject();
                            if (counters != null)
                                foreach (var pair in counters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                                    json[pair.Key] = pair.Value;
                            return json;
                        }
                        BuildingRuntimeSpawnCompositionSystemHelper.Context PlacementContext(List<RectInt> reservations,
                            IDictionary<string, int> rejectionCounters = null)
                        {
                            bool Valid(BuildingDefinition building, Vector2Int origin, Vector2Int modelFootprint, bool rotated,
                                GridConfig g, DynamicBuffer<GridRoad> nativeRoads, DynamicBlockerComponent nativeBlocker)
                            {
                                var rect = Effective(building, origin, g, rotated);
                                if (!Inside(rect)) { CountRejection(rejectionCounters, "outsideAlias"); return false; }
                                if (!S026CanReservePlacement(rect, reservations))
                                { CountRejection(rejectionCounters, "priorReservation"); return false; }
                                if (!BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect))
                                { CountRejection(rejectionCounters, "surfaceLevel"); return false; }
                                bool ownerValid = cache.IsPlacementValid(building, origin, modelFootprint, rotated, g,
                                    nativeRoads, nativeBlocker, dependency, startup, Effective,
                                    candidate => !S026CanReservePlacement(candidate, reservations));
                                if (!ownerValid)
                                {
                                    // Diagnostic attribution runs only after the authoritative combined predicate
                                    // rejected the candidate; it follows the original ordinary-placement order.
                                    if (cache.AuthoredBuildings.Overlaps(g, rect.position, rect.size))
                                        CountRejection(rejectionCounters, "authoredBuildingOverlap");
                                    else if (cache.RoadSurfaces.OverlapsWater(g, rect.position, rect.size))
                                        CountRejection(rejectionCounters, "waterOverlap");
                                    else if (cache.RoadSurfaces.Overlaps(g, rect.position, rect.size))
                                        CountRejection(rejectionCounters, "roadOverlap");
                                    else CountRejection(rejectionCounters, "ownerRectOrRuntimeReservation");
                                }
                                return ownerValid;
                            }
                            bool CachedInvalid(Vector2Int origin, Vector2Int modelFootprint) =>
                                BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                                    invalidPrefix, prefixWidth, prefixHeight, origin, modelFootprint);
                            return new BuildingRuntimeSpawnCompositionSystemHelper.Context(null, null, null, null, default,
                                null, geometry.GetPlacementFootprint, Effective, Valid, CachedInvalid,
                                null, null, null, null);
                        }
                        BuildingDefinition GetBuilding(SkirmishResolvedStructureEntry structure, out string visualKey,
                            out string prefabPath)
                        {
                            visualKey = SkirmishStructureIds.VisualKey(structure.StructureId);
                            prefabPath = null;
                            Require(!string.IsNullOrEmpty(visualKey) && prefabPaths.TryGetValue(visualKey, out prefabPath),
                                "Exact S026 structure visual mapping unavailable: " + structure.StructureId);
                            if (buildingDefinitions.TryGetValue(visualKey, out var existing)) return existing;
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            Require(prefab != null && !EditorUtility.IsDirty(prefab) && prefab.name == visualKey,
                                "Exact clean S026 structure prefab unavailable: " + prefabPath);
                            var built = definitionFactory.CreateRuntimeBuildingDefinition(prefab, prefab.name,
                                string.Empty, Vector2Int.one, 1, runway);
                            Require(built != null, "Original building definition factory rejected " + visualKey);
                            buildingDefinitions.Add(visualKey, built);
                            return built;
                        }
                        Vector2 RootFor(byte faction) => faction == 1
                            ? new Vector2(pair.Player.World.x, pair.Player.World.y)
                            : new Vector2(pair.Enemy.World.x, pair.Enemy.World.y);
                        Vector2Int PreferredOrigin(SkirmishResolvedStructureEntry structure, BuildingDefinition building)
                        {
                            var root = RootFor(structure.FactionId);
                            float x = root.x + structure.AcrossOffsetMetres * across.x + structure.ForwardOffsetMetres * forward.x;
                            float z = root.y + structure.AcrossOffsetMetres * across.y + structure.ForwardOffsetMetres * forward.y;
                            int2 center = GridUtils.WorldToCell(grid, new float3(x, grid.Origin.y, z));
                            var footprint = geometry.GetPlacementFootprint(building, false);
                            return new Vector2Int(center.x - footprint.x / 2, center.y - footprint.y / 2);
                        }

                        int[] order = S026ConstrainedFirstStructureOrder(budget.Structures, fillUnplacedBySourceSearch);
                        for (int planningOrdinal = 0; planningOrdinal < order.Length; planningOrdinal++)
                        {
                            int compilerIndex = order[planningOrdinal];
                            var structure = budget.Structures[compilerIndex];
                            var building = GetBuilding(structure, out string visualKey, out string prefabPath);
                            var preferred = PreferredOrigin(structure, building);
                            var footprint = geometry.GetPlacementFootprint(building, false);
                            int cachedInvalidCandidateCount = 0;
                            int originalPlacementPredicateCandidateCount = 0;
                            var placementRejections = new Dictionary<string, int>(StringComparer.Ordinal);
                            var context = PlacementContext(sourceReservations, placementRejections);
                            bool found = search.TryFindValidInitialBuildingOrigin(context, building, preferred,
                                false, grid, roads, blocker, out var origin, false, false, false, structureSearchRadiusCap,
                                (candidate, candidateRect, cachedInvalid) =>
                                {
                                    if (cachedInvalid) cachedInvalidCandidateCount++;
                                    else originalPlacementPredicateCandidateCount++;
                                });
                            var rect = found ? Effective(building, origin, grid, false) : new RectInt();
                            var row = new JObject
                            {
                                ["planningOrdinal"] = planningOrdinal, ["compilerIndex"] = compilerIndex,
                                ["priorityClass"] = S026CandidateStructurePlanningPriority(
                                    structure.StructureId, fillUnplacedBySourceSearch),
                                ["faction"] = structure.FactionId, ["structureId"] = structure.StructureId,
                                ["visualKey"] = visualKey, ["prefabPath"] = prefabPath, ["prefabSha256"] = Hash(prefabPath),
                                ["preferredOrigin"] = new JArray(preferred.x, preferred.y),
                                ["nativePlacementFootprint"] = new JArray(footprint.x, footprint.y),
                                ["foundByOriginalOwner"] = found, ["candidateOnly"] = true,
                                ["normalPlacementAcceptance"] = false,
                                ["cachedInvalidCandidates"] = cachedInvalidCandidateCount,
                                ["originalPlacementPredicateCandidates"] = originalPlacementPredicateCandidateCount,
                                ["originalOwnerFirstRejectionCategories"] = RejectionCountsJson(placementRejections),
                                ["rejectionCategoryEvidence"] = "Counts are incremented inside the unchanged placement delegate only after the original combined owner predicate has returned false; category checks follow ordinary owner first-failure order. ownerRectOrRuntimeReservation remains an aggregate.",
                                ["rejectionInterpretation"] = "The owner observer separates cached-invalid-prefix skips from predicate candidates. The first-rejection categories are read after the original combined predicate fails, preserving its result; ownerRectOrRuntimeReservation remains aggregated."
                            };
                            if (found)
                            {
                                Require(Inside(rect) && S026CanReservePlacement(rect, sourceReservations),
                                    "Constrained-first owner result escaped alias or prior plan reservations");
                                sourceReservations.Add(rect);
                                row["resolvedOrigin"] = new JArray(origin.x, origin.y);
                                row["resolvedEffectiveRect"] = new JObject { ["x"] = rect.x, ["z"] = rect.y,
                                    ["width"] = rect.width, ["depth"] = rect.height };
                            }
                            planningRows.Add(row);
                            planByIndex[compilerIndex.ToString()] = row.DeepClone();
                        }
                        if (fillUnplacedBySourceSearch)
                        {
                            const int fallbackTileColumns = 4, fallbackTileRows = 4, fallbackCandidatesPerTile = 4;
                            var fallbackRows = new JArray(); attempt["fallbackSourceSearchRows"] = fallbackRows;
                            int minX = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.x - grid.Origin.x) / grid.CellSize));
                            int maxX = Mathf.Min(grid.Width - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.x - grid.Origin.x) / grid.CellSize));
                            int minZ = Mathf.Max(0, Mathf.FloorToInt((alias.Bounds.PlayableMin.z - grid.Origin.z) / grid.CellSize));
                            int maxZ = Mathf.Min(grid.Height - 1, Mathf.CeilToInt((alias.Bounds.PlayableMax.z - grid.Origin.z) / grid.CellSize));
                            float tileWidth = (alias.Bounds.PlayableMax.x - alias.Bounds.PlayableMin.x) / fallbackTileColumns;
                            float tileDepth = (alias.Bounds.PlayableMax.z - alias.Bounds.PlayableMin.z) / fallbackTileRows;
                            var fallbackTraversal = new MapSurfaceTraversalValidation();
                            int fallbackStructuresScanned = 0;
                            var fallbackSearchSkipped = new JArray();
                            foreach (int compilerIndex in order)
                            {
                                JObject planned = planByIndex[compilerIndex.ToString()] as JObject;
                                if (planned != null && (bool)planned["foundByOriginalOwner"]) continue;
                                var structure = budget.Structures[compilerIndex];
                                if (fallbackStructuresScanned >= Mathf.Clamp(fallbackStructureScanCap, 0, 22))
                                {
                                    fallbackSearchSkipped.Add(new JObject { ["compilerIndex"] = compilerIndex,
                                        ["faction"] = structure.FactionId, ["structureId"] = structure.StructureId,
                                        ["reason"] = "PerPairSourceGridScanCapReached", ["candidateOnly"] = true });
                                    continue;
                                }
                                fallbackStructuresScanned++;
                                var building = GetBuilding(structure, out _, out _);
                                var footprint = geometry.GetPlacementFootprint(building, false);
                                Vector2Int preferred = planned != null && planned["preferredOrigin"] is JArray preferredJson
                                    ? new Vector2Int((int)preferredJson[0], (int)preferredJson[1])
                                    : PreferredOrigin(structure, building);
                                var preferredWorld = new float2(grid.Origin.x + (preferred.x + footprint.x * .5f) * grid.CellSize,
                                    grid.Origin.z + (preferred.y + footprint.y * .5f) * grid.CellSize);
                                var byTile = Enumerable.Range(0, fallbackTileColumns * fallbackTileRows)
                                    .ToDictionary(i => i, _ => new List<S026PlacementFallbackCandidate>());
                                var tileMaxDistance = new float[fallbackTileColumns * fallbackTileRows];
                                for (int tileZ = 0; tileZ < fallbackTileRows; tileZ++)
                                for (int tileX = 0; tileX < fallbackTileColumns; tileX++)
                                {
                                    float tileMinX = alias.Bounds.PlayableMin.x + tileX * tileWidth;
                                    float tileMaxX = tileX == fallbackTileColumns - 1 ? alias.Bounds.PlayableMax.x : tileMinX + tileWidth;
                                    float tileMinZ = alias.Bounds.PlayableMin.z + tileZ * tileDepth;
                                    float tileMaxZ = tileZ == fallbackTileRows - 1 ? alias.Bounds.PlayableMax.z : tileMinZ + tileDepth;
                                    float maximumDistanceSquared = 0f;
                                    maximumDistanceSquared = math.max(maximumDistanceSquared,
                                        math.distancesq(new float2(tileMinX, tileMinZ), preferredWorld));
                                    maximumDistanceSquared = math.max(maximumDistanceSquared,
                                        math.distancesq(new float2(tileMinX, tileMaxZ), preferredWorld));
                                    maximumDistanceSquared = math.max(maximumDistanceSquared,
                                        math.distancesq(new float2(tileMaxX, tileMinZ), preferredWorld));
                                    maximumDistanceSquared = math.max(maximumDistanceSquared,
                                        math.distancesq(new float2(tileMaxX, tileMaxZ), preferredWorld));
                                    tileMaxDistance[tileZ * fallbackTileColumns + tileX] = math.sqrt(maximumDistanceSquared);
                                }
                                long scanned = 0; int outsideAlias = 0, wrongHalfPlane = 0, sourceUnwalkable = 0;
                                int surfaceRejected = 0, effectiveFootprintOutsideAlias = 0;
                                for (int z = minZ; z <= maxZ; z++)
                                for (int x = minX; x <= maxX; x++)
                                {
                                    scanned++;
                                    float worldX = grid.Origin.x + (x + .5f) * grid.CellSize;
                                    float worldZ = grid.Origin.z + (z + .5f) * grid.CellSize;
                                    if (!IsInsideAlias(alias, worldX, worldZ)) { outsideAlias++; continue; }
                                    float2 sourceCenter = new float2(worldX, worldZ);
                                    float side = math.dot(sourceCenter - midpoint, forward);
                                    if (structure.FactionId == 1 && side >= 0f || structure.FactionId == 2 && side <= 0f)
                                    { wrongHalfPlane++; continue; }
                                    int cellIndex = z * grid.Width + x;
                                    if (walkable[cellIndex].Value == 0) { sourceUnwalkable++; continue; }
                                    if (!fallbackTraversal.CanTraverseFootprint(surface, surface.HasSurfaceData, grid,
                                        new int2(x, z), new int2(1, 1), false))
                                    { surfaceRejected++; continue; }
                                    var origin = new Vector2Int(x - footprint.x / 2, z - footprint.y / 2);
                                    var rect = Effective(building, origin, grid, false);
                                    if (!Inside(rect)) { effectiveFootprintOutsideAlias++; continue; }
                                    int tileX = Mathf.Clamp(Mathf.FloorToInt((worldX - alias.Bounds.PlayableMin.x) / tileWidth), 0, fallbackTileColumns - 1);
                                    int tileZ = Mathf.Clamp(Mathf.FloorToInt((worldZ - alias.Bounds.PlayableMin.z) / tileDepth), 0, fallbackTileRows - 1);
                                    int tileIndex = tileZ * fallbackTileColumns + tileX;
                                    float distance = math.sqrt(math.lengthsq(sourceCenter - preferredWorld));
                                    float maxDistance = tileMaxDistance[tileIndex];
                                    int distanceBand = maxDistance <= 0f ? 0 :
                                        Mathf.Clamp(Mathf.FloorToInt(distance / maxDistance * fallbackCandidatesPerTile),
                                            0, fallbackCandidatesPerTile - 1);
                                    float targetDistance = maxDistance * ((distanceBand + .5f) / fallbackCandidatesPerTile);
                                    S026KeepOnePlacementFallbackCandidatePerDistanceBand(byTile[tileIndex], new S026PlacementFallbackCandidate
                                    {
                                        TileIndex = tileIndex, SourceCell = new Vector2Int(x, z), Origin = origin,
                                        DistanceBand = distanceBand, DistanceSquared = math.lengthsq(sourceCenter - preferredWorld),
                                        DistanceBandTargetDelta = math.abs(distance - targetDistance)
                                    }, fallbackCandidatesPerTile);
                                }
                                var sampledCandidates = byTile.OrderBy(kv => kv.Key).SelectMany(kv => kv.Value)
                                    .OrderBy(candidate => candidate.DistanceSquared).ThenBy(candidate => candidate.TileIndex)
                                    .ThenBy(candidate => candidate.DistanceBand)
                                    .ThenBy(candidate => candidate.SourceCell.y).ThenBy(candidate => candidate.SourceCell.x).ToList();
                                int cachedInvalidPrefilterSkips;
                                var candidatesForOwner = S026FilterCachedInvalidFallbackCandidates(sampledCandidates,
                                    footprint, (candidateOrigin, modelFootprint) =>
                                        BuildingPlacementValidationUtilitySystemHelper.HasCachedInvalidCellInFootprint(
                                            invalidPrefix, prefixWidth, prefixHeight, candidateOrigin, modelFootprint),
                                    out cachedInvalidPrefilterSkips);
                                var retainedByTile = new JObject();
                                foreach (var tile in byTile.OrderBy(kv => kv.Key))
                                    retainedByTile[tile.Key.ToString()] = new JArray(tile.Value.OrderBy(c => c.DistanceBand)
                                        .Select(c => new JObject { ["distanceBand"] = c.DistanceBand,
                                            ["sourceCell"] = new JArray(c.SourceCell.x, c.SourceCell.y),
                                            ["distanceSquaredFromPreferred"] = c.DistanceSquared,
                                            ["targetDistanceDeltaMetres"] = c.DistanceBandTargetDelta }));
                                var fallback = new JObject
                                {
                                    ["compilerIndex"] = compilerIndex, ["faction"] = structure.FactionId,
                                    ["structureId"] = structure.StructureId, ["canonicalPreferredOrigin"] = new JArray(preferred.x, preferred.y),
                                    ["candidateOnly"] = true, ["sourceFitCertified"] = false,
                                    ["tileGrid"] = new JArray(fallbackTileColumns, fallbackTileRows),
                                    ["candidateOriginsPerTileCap"] = fallbackCandidatesPerTile,
                                    ["ownerExactOriginChecksCap"] = fallbackTileColumns * fallbackTileRows * fallbackCandidatesPerTile,
                                    ["sourceScan"] = new JObject { ["gridCellsScanned"] = scanned, ["outsideAlias"] = outsideAlias,
                                        ["wrongFactionHalfPlane"] = wrongHalfPlane, ["sourceUnwalkable"] = sourceUnwalkable,
                                        ["surfaceNotTraversable"] = surfaceRejected,
                                        ["effectiveFootprintOutsideAlias"] = effectiveFootprintOutsideAlias,
                                        ["radialDistanceBandsRetained"] = byTile.Sum(kv => kv.Value.Select(c => c.DistanceBand).Distinct().Count()) },
                                    ["retainedCandidatesByTile"] = retainedByTile,
                                    ["sampledCandidateCountBeforeCachedInvalidPrefilter"] = sampledCandidates.Count,
                                    ["cachedInvalidPrefixPrefilterSkips"] = cachedInvalidPrefilterSkips,
                                    ["retainedCandidateCountAfterCachedInvalidPrefilter"] = candidatesForOwner.Count,
                                    ["cachedInvalidPrefilter"] = "Uses the exact current owner HasCachedInvalidCellInFootprint predicate and model footprint; known-invalid sampled origins are pruned before exact-origin owner calls because the original search skips them before its placement predicate",
                                    ["ownerChecks"] = new JArray(), ["foundByOriginalOwner"] = false,
                                    ["originalOwnerFirstRejectionCategories"] = new JObject(),
                                    ["rejectionCategoryEvidence"] = "Post-rejection only; exact owner decision is unchanged. Owner rect/runtime reservation failures are not separated further."
                                };
                                fallbackRows.Add(fallback);
                                var ownerChecks = (JArray)fallback["ownerChecks"];
                                var fallbackRejections = new Dictionary<string, int>(StringComparer.Ordinal);
                                foreach (var candidate in candidatesForOwner)
                                {
                                    int cached = 0, reachedPredicate = 0;
                                    var context = PlacementContext(sourceReservations, fallbackRejections);
                                    bool found = search.TryFindValidInitialBuildingOrigin(context, building, candidate.Origin,
                                        false, grid, roads, blocker, out var resolved, true, false, false, 0,
                                        (candidateOrigin, candidateRect, cachedInvalid) =>
                                        {
                                            if (cachedInvalid) cached++;
                                            else reachedPredicate++;
                                        });
                                    ownerChecks.Add(new JObject
                                    {
                                        ["tileIndex"] = candidate.TileIndex,
                                        ["distanceBand"] = candidate.DistanceBand,
                                        ["distanceSquaredFromCanonicalPreferred"] = candidate.DistanceSquared,
                                        ["distanceBandTargetDeltaMetres"] = candidate.DistanceBandTargetDelta,
                                        ["sourceCell"] = new JArray(candidate.SourceCell.x, candidate.SourceCell.y),
                                        ["candidateOrigin"] = new JArray(candidate.Origin.x, candidate.Origin.y),
                                        ["ownerFoundExactOrigin"] = found,
                                        ["ownerResolvedOrigin"] = found ? new JArray(resolved.x, resolved.y) : null,
                                        ["cachedInvalidCandidates"] = cached,
                                        ["originalPlacementPredicateCandidates"] = reachedPredicate
                                    });
                                    if (!found) continue;
                                    Require(resolved == candidate.Origin && Inside(Effective(building, resolved, grid, false)) &&
                                        S026CanReservePlacement(Effective(building, resolved, grid, false), sourceReservations),
                                        "Fallback owner result escaped exact-origin alias or reservation constraints");
                                    var rect = Effective(building, resolved, grid, false);
                                    sourceReservations.Add(rect);
                                    if (planned == null) { planned = new JObject(); planningRows.Add(planned); }
                                    planned["foundByOriginalOwner"] = true;
                                    planned["resolvedOrigin"] = new JArray(resolved.x, resolved.y);
                                    planned["resolvedEffectiveRect"] = new JObject { ["x"] = rect.x, ["z"] = rect.y,
                                        ["width"] = rect.width, ["depth"] = rect.height };
                                    planned["fallbackSourceDerivedOrigin"] = true;
                                    planned["fallbackSpatialTileIndex"] = candidate.TileIndex;
                                    planned["fallbackOwnerExactOriginPassed"] = true;
                                    JObject visiblePlanningRow = planningRows.OfType<JObject>()
                                        .FirstOrDefault(row => row["compilerIndex"] != null && (int)row["compilerIndex"] == compilerIndex);
                                    if (visiblePlanningRow != null)
                                    {
                                        visiblePlanningRow["foundByOriginalOwner"] = true;
                                        visiblePlanningRow["resolvedOrigin"] = new JArray(resolved.x, resolved.y);
                                        visiblePlanningRow["resolvedEffectiveRect"] = new JObject { ["x"] = rect.x, ["z"] = rect.y,
                                            ["width"] = rect.width, ["depth"] = rect.height };
                                        visiblePlanningRow["fallbackSourceDerivedOrigin"] = true;
                                        visiblePlanningRow["fallbackSpatialTileIndex"] = candidate.TileIndex;
                                        visiblePlanningRow["fallbackOwnerExactOriginPassed"] = true;
                                    }
                                    fallback["foundByOriginalOwner"] = true;
                                    fallback["resolvedOrigin"] = new JArray(resolved.x, resolved.y);
                                    fallback["resolvedEffectiveRect"] = new JObject { ["x"] = rect.x, ["z"] = rect.y,
                                        ["width"] = rect.width, ["depth"] = rect.height };
                                    planByIndex[compilerIndex.ToString()] = planned.DeepClone();
                                    break;
                                }
                                fallback["originalOwnerFirstRejectionCategories"] = RejectionCountsJson(fallbackRejections);
                            }
                            attempt["fallbackStructuresScanned"] = fallbackStructuresScanned;
                            attempt["fallbackSearchSkippedByCap"] = fallbackSearchSkipped;
                            attempt["fallbackExactOriginChecks"] = fallbackRows.OfType<JObject>()
                                .Sum(row => ((JArray)row["ownerChecks"]).Count);
                        }
                        for (int compilerIndex = 0; compilerIndex < budget.Structures.Length; compilerIndex++)
                        {
                            var structure = budget.Structures[compilerIndex];
                            JObject planned = planByIndex[compilerIndex.ToString()] as JObject;
                            bool plannedFound = planned != null && (bool)planned["foundByOriginalOwner"];
                            var row = new JObject
                            {
                                ["compilerIndex"] = compilerIndex, ["faction"] = structure.FactionId,
                                ["structureId"] = structure.StructureId, ["plannedOwnerFound"] = plannedFound,
                                ["validationOrder"] = "ExactPreferredOrigin_OriginalCompilerRuntimeOrder",
                                ["candidateOnly"] = true, ["normalPlacementAcceptance"] = false
                            };
                            canonicalRows.Add(row);
                            if (!plannedFound)
                            { row["status"] = "NoPlannedOriginToRecheck"; row["canonicalOwnerExactOriginPassed"] = false; continue; }
                            Vector2Int plannedOrigin = new Vector2Int((int)planned["resolvedOrigin"][0], (int)planned["resolvedOrigin"][1]);
                            var building = GetBuilding(structure, out _, out _);
                            var footprint = geometry.GetPlacementFootprint(building, false);
                            var plannedRect = Effective(building, plannedOrigin, grid, false);
                            bool notReservedYet = S026CanReservePlacement(plannedRect, runtimeReservations);
                            bool exactFound = false;
                            Vector2Int exactOrigin = default;
                            if (notReservedYet && Inside(plannedRect))
                            {
                                var context = PlacementContext(runtimeReservations);
                                exactFound = search.TryFindValidInitialBuildingOrigin(context, building, plannedOrigin,
                                    false, grid, roads, blocker, out exactOrigin, true, false, false, 0);
                            }
                            bool exactSame = exactFound && exactOrigin == plannedOrigin;
                            row["plannedOrigin"] = new JArray(plannedOrigin.x, plannedOrigin.y);
                            row["plannedEffectiveRect"] = new JObject { ["x"] = plannedRect.x, ["z"] = plannedRect.y,
                                ["width"] = plannedRect.width, ["depth"] = plannedRect.height };
                            row["unreservedInCanonicalOrder"] = notReservedYet;
                            row["canonicalOwnerExactOriginPassed"] = exactSame;
                            row["status"] = exactSame ? "ExactOriginRevalidatedInCanonicalOrder_Unqualified" : "CanonicalOrderExactOriginRejected";
                            if (exactSame) runtimeReservations.Add(plannedRect);
                        }
                        attempt["planningOwnerFoundCount"] = planningRows.OfType<JObject>().Count(r => (bool)r["foundByOriginalOwner"]);
                        attempt["canonicalOrderExactRecheckPassedCount"] = canonicalRows.OfType<JObject>().Count(r => (bool)r["canonicalOwnerExactOriginPassed"]);
                        attempt["allPlannedStructuresRevalidatedInCanonicalOrder"] =
                            (int)attempt["planningOwnerFoundCount"] == budget.Structures.Length &&
                            (int)attempt["canonicalOrderExactRecheckPassedCount"] == budget.Structures.Length;
                        if ((bool)attempt["allPlannedStructuresRevalidatedInCanonicalOrder"])
                            attempt["completeLogicalInstanceBindingProjection"] = RunS026CandidateInstanceBindingProjection(
                                alias, grid, size, budget.Structures, planByIndex);
                        attempt["planningReservationCount"] = sourceReservations.Count;
                        attempt["canonicalReservationCount"] = runtimeReservations.Count;
                    }
                    finally { blockers.Dispose(); }
                }
                sizeRow["pairAttemptCount"] = pairRows.Count;
                sizeRow["pairAttemptCap"] = rootPairCap;
            }
            report["interpretation"] = "Constrained-first order is only a bounded authoring heuristic. Each planned coordinate is separately exact-rechecked by the original owner in canonical compiler/runtime order with fresh reservations. Candidate sampling is not exhaustive; no source fit, route, occupancy, or readiness claim.";
            return report;
        }

        static bool S026CanReservePlacement(RectInt candidate, IReadOnlyList<RectInt> reservations) =>
            candidate.width > 0 && candidate.height > 0 && (reservations == null || !reservations.Any(candidate.Overlaps));

        static int RunS026AlternativeRootControls()
        {
            int controls = 0;
            var p = new List<S026AlternativeRoot>
            {
                new S026AlternativeRoot(1, new int2(10, 10), new float2(10.5f, 10.5f), 25, 0),
                new S026AlternativeRoot(1, new int2(9, 9), new float2(9.5f, 9.5f), 25, 0),
                new S026AlternativeRoot(1, new int2(9, 10), new float2(9.5f, 10.5f), 36, 0),
                new S026AlternativeRoot(1, new int2(8, 10), new float2(8.5f, 10.5f), 2500, 1),
                new S026AlternativeRoot(1, new int2(7, 10), new float2(7.5f, 10.5f), 10000, 2)
            };
            var e = new List<S026AlternativeRoot>
            {
                new S026AlternativeRoot(2, new int2(20, 10), new float2(20.5f, 10.5f), 25, 0),
                new S026AlternativeRoot(2, new int2(21, 10), new float2(21.5f, 10.5f), 49, 1),
                new S026AlternativeRoot(2, new int2(22, 10), new float2(22.5f, 10.5f), 12100, 2)
            };
            var sampled = S026SelectAnnulusRepresentatives(p);
            var sampledAgain = S026SelectAnnulusRepresentatives(p);
            Require(sampled.Count == 3 && sampled[0].Cell.Equals(new int2(9, 9)) &&
                sampled[1].Cell.Equals(new int2(8, 10)) && sampled[2].Cell.Equals(new int2(7, 10)) &&
                sampled.Select(r => r.Cell).SequenceEqual(sampledAgain.Select(r => r.Cell)),
                "One nearest deterministic root per annulus required");
            var first = S026BuildAlternativeRootPairs(sampled, e, 3);
            var second = S026BuildAlternativeRootPairs(sampled, e, 3);
            Require(first.Count == 3 && second.Count == 3 && first[0].Player.BandIndex == 0 &&
                first[1].Player.BandIndex == 1 && first[2].Player.BandIndex == 2 &&
                first[0].Player.Cell.Equals(second[0].Player.Cell) && first[0].Enemy.Cell.Equals(second[0].Enemy.Cell) &&
                first[1].Player.Cell.Equals(second[1].Player.Cell) && first[1].Enemy.Cell.Equals(second[1].Enemy.Cell) &&
                first[2].Player.Cell.Equals(second[2].Player.Cell) && first[2].Enemy.Cell.Equals(second[2].Enemy.Cell),
                "Same-annulus root pair selection must be deterministic, diverse and capped");
            controls++;
            var reservations = new List<RectInt> { new RectInt(5, 5, 3, 3) };
            Require(!S026CanReservePlacement(new RectInt(7, 7, 2, 2), reservations) &&
                S026CanReservePlacement(new RectInt(8, 5, 2, 3), reservations) &&
                !S026CanReservePlacement(new RectInt(0, 0, 0, 2), reservations),
                "Effective-rectangle reservations must reject overlap/empty and permit edge adjacency");
            controls++;
            return controls;
        }

        static Dictionary<string, string> S026StructurePrefabPaths() => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Building_Barrack"] = "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab",
            ["Building_GroundStaging"] = "Assets/Game/Prefabs/Skirmish/Building_GroundStaging.prefab",
            ["Building_OilPump"] = "Assets/Game/Prefabs/Buildings/Building_OilPump.prefab",
            ["Building_Refinery"] = "Assets/Game/Prefabs/Buildings/Building_Refinery.prefab",
            ["Building_Fuel_Bladder"] = "Assets/Game/Prefabs/Buildings/Building_Fuel_Bladder.prefab",
            ["Building_Ammunition_Depot"] = "Assets/Game/Prefabs/Buildings/Building_Ammunition_Depot.prefab",
            ["Building_GuardTower"] = "Assets/Game/Prefabs/Buildings/Building_GuardTower.prefab",
            ["Building_Satelite_Dish"] = "Assets/Game/Prefabs/Buildings/Building_Satelite_Dish.prefab",
            ["Building_Helipad"] = "Assets/Game/Prefabs/Buildings/Building_Helipad.prefab"
        };

        static void TryFinalizeS026CompanionCleanup(JObject report)
        {
            try
            {
                if (!(report["cleanupBaseline"]?["previewOwnership"] is JObject baselinePreview)) return;
                var current = S026PreviewOwnershipSnapshot();
                bool quit = Environment.GetCommandLineArgs().Contains("-quit");
                if (!TryValidateS026CompanionCleanupPlan(baselinePreview, current, quit, out string planError))
                {
                    report["companionCleanupFinalizer"] = new JObject
                    { ["attempted"] = false, ["reason"] = planError, ["wrapperQuitPresent"] = quit };
                    return;
                }
                Require(Hash(CompanionSceneUtilityPath) == CompanionSceneUtilitySha,
                    "Pinned companion-scene owner implementation drifted in failure finalizer");
                var ownerType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("Unity.Entities.CompanionGameObjectUtility", false))
                    .FirstOrDefault(t => t != null);
                Require(ownerType != null, "Exact companion-scene owner unavailable in failure finalizer");
                var callback = ownerType.GetMethod("AssemblyReloadEventsOnbeforeAssemblyReload",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                    null, Type.EmptyTypes, null);
                Require(callback != null && callback.ReturnType == typeof(void),
                    "Pinned companion-scene cleanup callback unavailable in failure finalizer");
                callback.Invoke(null, null);
                var after = S026PreviewOwnershipSnapshot();
                Require(TryValidateS026CompanionCleanupPostcondition(baselinePreview, after, out string postError), postError);
                report["companionCleanupFinalizer"] = new JObject
                {
                    ["attempted"] = true,
                    ["ownerType"] = "Unity.Entities.CompanionGameObjectUtility",
                    ["ownerCallback"] = "AssemblyReloadEventsOnbeforeAssemblyReload",
                    ["ownerSourcePath"] = CompanionSceneUtilityPath,
                    ["ownerSourceSha256"] = CompanionSceneUtilitySha,
                    ["precondition"] = planError,
                    ["postcondition"] = postError,
                    ["after"] = after
                };
            }
            catch (Exception failure)
            {
                report["companionCleanupFinalizer"] = new JObject
                { ["attempted"] = true, ["error"] = failure.ToString() };
            }
        }

        static JObject S026PreviewOwnershipSnapshot()
        {
            var census = SkirmishProductionBatchOwnershipCensus.Capture();
            return new JObject
            {
                ["actualPreviewCount"] = census["actual_preview_count"]?.DeepClone(),
                ["measuredPreviewCount"] = census["measured_preview_count"]?.DeepClone(),
                ["previewCensusComplete"] = census["preview_census_complete"]?.DeepClone(),
                ["scenes"] = census["scenes"]?.DeepClone(),
                ["companionOwners"] = census["companion_package_owner_fields"]?.DeepClone()
            };
        }

        sealed class S026TransientObjectOwner : IDisposable
        {
            readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
            public T Own<T>(T value) where T : UnityEngine.Object { owned.Add(value); return value; }
            public void Dispose() { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) UnityEngine.Object.DestroyImmediate(owned[i]); }
        }

        sealed class S026SourcePlacementOwner : IDisposable
        {
            public int[] Prefix; public int PrefixWidth; public int PrefixHeight;
            public void SetPrefix(int[] prefix, int width, int height) { Prefix = prefix; PrefixWidth = width; PrefixHeight = height; }
            public void Dispose() { }
        }

        static List<int2> ReconstructS026ComponentPath(int[] parents, int width, int2 start, int2 goal, int tileCount)
        {
            var startChain = new List<int>();
            var goalChain = new List<int>();
            int startIndex = start.y * width + start.x, goalIndex = goal.y * width + goal.x;
            for (int index = startIndex, steps = 0; index >= 0 && index < tileCount && steps <= tileCount; index = parents[index], steps++)
                startChain.Add(index);
            for (int index = goalIndex, steps = 0; index >= 0 && index < tileCount && steps <= tileCount; index = parents[index], steps++)
                goalChain.Add(index);
            if (startChain.Count == 0 || goalChain.Count == 0) return new List<int2>();
            var startPositions = new Dictionary<int, int>();
            for (int i = 0; i < startChain.Count; i++) startPositions[startChain[i]] = i;
            int commonGoalPosition = -1, commonStartPosition = -1;
            for (int i = 0; i < goalChain.Count; i++)
                if (startPositions.TryGetValue(goalChain[i], out commonStartPosition)) { commonGoalPosition = i; break; }
            if (commonGoalPosition < 0) return new List<int2>();
            var path = new List<int2>(commonStartPosition + commonGoalPosition + 1);
            for (int i = 0; i <= commonStartPosition; i++)
            { int index = startChain[i]; path.Add(new int2(index % width, index / width)); }
            for (int i = commonGoalPosition - 1; i >= 0; i--)
            { int index = goalChain[i]; path.Add(new int2(index % width, index / width)); }
            return path;
        }

        static int RunS026SourceCheckpointControls()
        {
            var cells = new[] { new int2(3, 0), new int2(2, 0) };
            var excluded = new HashSet<int> { 0, 4 };
            Require(TrySelectNearestSourceCheckpoint(cells, 10, new float2(0, 0), new float2(6, 0), 0.5f,
                excluded, out var first, out float firstDistance) && first.Equals(new int2(3, 0)) && firstDistance == 0,
                "Nearest source-raster checkpoint selection failed");
            excluded.Add(first.y * 10 + first.x);
            Require(TrySelectNearestSourceCheckpoint(cells, 10, new float2(0, 0), new float2(6, 0), 0.5f,
                excluded, out var second, out float secondDistance) && second.Equals(new int2(2, 0)) && secondDistance == 1,
                "Distinct second source-raster checkpoint selection failed");
            excluded.Add(second.y * 10 + second.x);
            Require(!TrySelectNearestSourceCheckpoint(cells, 10, new float2(0, 0), new float2(6, 0), 0.5f,
                excluded, out _, out _), "Checkpoint selector must reject exhausted distinct source cells");
            var noRoad = System.Array.Empty<int2>();
            Require(!TrySelectNearestSourceCheckpoint(noRoad, 10, new float2(0, 0), new float2(6, 0), 0.5f,
                new HashSet<int>(), out _, out _), "Checkpoint selector must reject absent source road cells");
            return 4;
        }

        static bool TrySelectNearestSourceCheckpoint(IEnumerable<int2> cells, int gridWidth, float2 start, float2 end,
            float fraction, HashSet<int> excluded, out int2 selected, out float distanceSq)
        {
            selected = default; distanceSq = float.PositiveInfinity;
            float2 target = math.lerp(start, end, fraction);
            foreach (var cell in cells)
            {
                int key = cell.y * gridWidth + cell.x;
                if (excluded.Contains(key)) continue;
                float2 delta = new float2(cell.x, cell.y) - target; float distance = math.lengthsq(delta);
                if (distance > distanceSq || math.abs(distance - distanceSq) < 0.0001f &&
                    (cell.y > selected.y || cell.y == selected.y && cell.x >= selected.x)) continue;
                selected = cell; distanceSq = distance;
            }
            return !float.IsPositiveInfinity(distanceSq);
        }

        static int RunS026AliasSourceDiagnosticControls()
        {
            int controls = 0;
            void Check(bool condition, string message) { Require(condition, message); controls++; }
            var alias = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(S026AliasPath);
            Require(alias != null, "Pinned alias control fixture required");
            Check(IsInsideAlias(alias, 900, 260), "Alias minimum boundary accepted");
            Check(IsInsideAlias(alias, 1300, 875), "Alias maximum boundary accepted");
            Check(!IsInsideAlias(alias, 899.99f, 567.5f), "West outside frame rejected");
            Check(!IsInsideAlias(alias, 1300.01f, 567.5f), "East outside frame rejected");
            Check(!IsInsideAlias(alias, 1100, 259.99f), "South outside frame rejected");
            Check(!IsInsideAlias(alias, 1100, 875.01f), "North outside frame rejected");
            Require(controls == 6, "Exact alias frame boundary control count required");
            return controls;
        }
    }
}
#endif
