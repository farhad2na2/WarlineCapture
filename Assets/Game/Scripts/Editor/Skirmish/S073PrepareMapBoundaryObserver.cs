#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Editor
{
    [InitializeOnLoad]
    internal static class S073PrepareMapBoundaryObserver
    {
        private const string EnvironmentKey = "WARLINE_S073_PREPARE_BOUNDARY_OUTPUT";
        private const string CoordinateRoadCaptureKey = "WARLINE_S073_COORDINATE_ROAD_CAPTURE";
        private const string SourceSceneGuid = "dad0bd13fb20943dfb2f881cbe225f05";
        private const string EcsSceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        private static JObject before;
        private static JObject boundaryAfter;
        private static string session;
        private static bool completed;
        private static bool coordinatedRoadCapture;
        private static bool postStreamWritten;
        private static string roadMaskPrefixPath;
        private static string placementAuditPath;
        private static double deadline;
        private static int stableFrames;
        private static JObject pinsAtBoundary;

        static S073PrepareMapBoundaryObserver()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentKey)))
            {
                coordinatedRoadCapture = Environment.GetEnvironmentVariable(CoordinateRoadCaptureKey) == "1";
                SkirmishLaunchProjection.PrepareMapObserver += Observe;
            }
        }

        private static void Observe(EntityManager em, bool after)
        {
            if (completed) return;
            try
            {
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1) return;
                var match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729) return;
                var snapshot = Snapshot(em, match);
                if (!after)
                {
                    if (before != null || match.Phase != SkirmishPhase.Queued)
                        throw new InvalidOperationException("Unique queued S073 before-state required");
                    if (coordinatedRoadCapture) ValidateFreshRoadCaptureOutputs();
                    session = match.SessionId.ToString();
                    before = snapshot;
                    return;
                }
                if (before == null || session != match.SessionId.ToString() || match.Phase != SkirmishPhase.Preparing)
                    throw new InvalidOperationException("Matching S073 before/after session required");
                boundaryAfter = snapshot;
                pinsAtBoundary = SourcePins();
                SkirmishLaunchProjection.PrepareMapObserver -= Observe;
                Write(OutputPath() + ".boundary.json", Result("BoundaryObserved", null));
                deadline = EditorApplication.timeSinceStartup + 180;
                EditorApplication.update += ObserveStream;
                Debug.Log("[S073PrepareMapBoundary] result=Observed postStreamPending=1");
            }
            catch (Exception error)
            {
                Fail(error.ToString());
            }
        }

        private static void ObserveStream()
        {
            if (completed) return;
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Play mode ended before the post-stream observation");
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new TimeoutException(postStreamWritten
                        ? "Road capture outputs or placement audit did not arrive before the coordinated deadline"
                        : "ECS source scene did not reach the required same-session streamed state");
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) throw new InvalidOperationException("Original world disappeared");
                var em = world.EntityManager;
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1) throw new InvalidOperationException("Original session disappeared");
                var match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.SessionId.ToString() != session ||
                    (match.Phase != SkirmishPhase.Preparing && match.Phase != SkirmishPhase.Playing))
                    throw new InvalidOperationException("Original S073 session or phase changed unexpectedly");
                var post = Snapshot(em, match);
                if (!JToken.DeepEquals(boundaryAfter["world_sequence"], post["world_sequence"]) ||
                    !JToken.DeepEquals(boundaryAfter["session_entity"], post["session_entity"]) ||
                    !JToken.DeepEquals(boundaryAfter["map_root_entity"], post["map_root_entity"]) ||
                    !JToken.DeepEquals(boundaryAfter["generation"], post["generation"]) ||
                    !JToken.DeepEquals(boundaryAfter["source_identity_hash"], post["source_identity_hash"]) ||
                    !JToken.DeepEquals(boundaryAfter["source_content_hash"], post["source_content_hash"]) ||
                    (boundaryAfter["ecs_scene_entity"].Type != JTokenType.Null &&
                     !JToken.DeepEquals(boundaryAfter["ecs_scene_entity"], post["ecs_scene_entity"])))
                    throw new InvalidOperationException("S073 world, map, or source identity changed during streaming");
                bool ready = (bool)post["source_scene_loaded"] && (int)post["physical_source_validated"] == 1 &&
                    (bool)post["ecs_scene_loaded"] && (bool)post["authored_grid_section_owned"] &&
                    (int)post["resolved_section_count"] > 0;
                if (Environment.GetEnvironmentVariable("WARLINE_S073_REQUIRE_STATIC_OWNER") == "1")
                    ready = ready && (int)post["entity_count"] > 0 &&
                        (int)post["static_history_bound_count"] == (int)post["entity_count"] &&
                        (bool)post["static_grid_committed"];
                if (Environment.GetEnvironmentVariable("WARLINE_S073_REQUIRE_INITIAL_SPAWN") == "1")
                {
                    using var configs = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
                    using var initialized = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig), typeof(InitialUnitsSpawnInitialized));
                    ready = ready && configs.CalculateEntityCount() == 1 && initialized.CalculateEntityCount() == 1;
                }
                if (postStreamWritten && coordinatedRoadCapture && !ready)
                    throw new InvalidOperationException("Original streamed S073 state stopped satisfying its capture gate");
                if (postStreamWritten && coordinatedRoadCapture &&
                    (pinsAtBoundary == null || !JToken.DeepEquals(pinsAtBoundary, SourcePins())))
                    throw new InvalidOperationException("Pinned native source changed while awaiting coordinated road capture");
                if (!postStreamWritten)
                {
                    // The coordinated road receipt joins this census to the original placement audit,
                    // which only runs once the same S073 session enters Playing. Keep observing the
                    // same owned world and source pins until that phase; do not qualify a Preparing
                    // snapshot and later pair it with Playing placement evidence.
                    if (coordinatedRoadCapture && match.Phase != SkirmishPhase.Playing)
                    {
                        stableFrames = 0;
                        return;
                    }
                    stableFrames = ready ? stableFrames + 1 : 0;
                    if (stableFrames < 2) return;
                    if (pinsAtBoundary == null || !JToken.DeepEquals(pinsAtBoundary, SourcePins()))
                        throw new InvalidOperationException("Pinned native source changed during S073 observation");
                    if (Environment.GetEnvironmentVariable("WARLINE_S073_CAPTURE_BLOCKER_DOMAIN") == "1")
                        post["complete_static_blocker_observation"] = ObserveBlockerDomain(em);
                    var result = Result("Passed", post);
                    result["stable_updates"] = stableFrames;
                    Write(OutputPath(), result);
                    postStreamWritten = true;
                    if (coordinatedRoadCapture) deadline = EditorApplication.timeSinceStartup + 180;
                    Debug.Log("[S073PostStreamCensus] result=Passed ecsLoaded=1 ownedGrid=1 entities=" + (int)post["entity_count"]);
                    if (!coordinatedRoadCapture)
                    {
                        completed = true;
                        EditorApplication.update -= ObserveStream;
                        EditorApplication.delayCall += () => EditorApplication.Exit(0);
                        return;
                    }
                }
                if (coordinatedRoadCapture && RoadCaptureOutputsReady())
                {
                    completed = true;
                    EditorApplication.update -= ObserveStream;
                    string placementAuditSha;
                    using (var sha = SHA256.Create())
                        placementAuditSha = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(placementAuditPath)))
                            .Replace("-", "").ToLowerInvariant();
                    Debug.Log("[S073RoadCaptureCoordination] result=Passed postStream=1 masks=2 placementAudit=1 " +
                        "placementAuditPath=" + placementAuditPath + " placementAuditSha256=" + placementAuditSha + " missionReady=0");
                    EditorApplication.delayCall += () => EditorApplication.Exit(0);
                }
            }
            catch (Exception error) { Fail(error.ToString()); }
        }

        private static bool RoadCaptureOutputsReady()
        {
            if (string.IsNullOrEmpty(roadMaskPrefixPath) || string.IsNullOrEmpty(placementAuditPath))
                throw new InvalidOperationException("Coordinated road output paths were not pinned before launch");
            string surfacePath = roadMaskPrefixPath + ".surface.bin", sidewalkPath = roadMaskPrefixPath + ".sidewalk.bin";
            if (!File.Exists(surfacePath) || !File.Exists(sidewalkPath) || !File.Exists(placementAuditPath)) return false;
            const int expectedBytes = 2048 * 1024;
            byte[] surface = File.ReadAllBytes(surfacePath), sidewalk = File.ReadAllBytes(sidewalkPath);
            if (surface.Length != expectedBytes || sidewalk.Length != expectedBytes ||
                surface.Any(value => value > 1) || sidewalk.Any(value => value > 1))
                throw new InvalidDataException("Coordinated S073 road masks must be complete row-major bool grids");
            string firstLine = File.ReadLines(placementAuditPath).FirstOrDefault();
            if (string.IsNullOrEmpty(firstLine) ||
                !firstLine.StartsWith("phase=Playing fail=None auditPassed=", StringComparison.Ordinal))
                throw new InvalidDataException("S073 placement audit did not record the same Playing session without startup failure");
            return true;
        }

        private static void ValidateFreshRoadCaptureOutputs()
        {
            if (!string.IsNullOrEmpty(roadMaskPrefixPath) || !string.IsNullOrEmpty(placementAuditPath))
                throw new InvalidOperationException("Coordinated road output paths were already initialized");
            roadMaskPrefixPath = RequireFreshOutputPath("WARLINE_S073_CURRENT_ROAD_MASK_PREFIX");
            placementAuditPath = RequireFreshOutputPath("WARLINE_S073_PLACEMENT_AUDIT_OUTPUT");
        }

        private static string RequireFreshOutputPath(string environmentKey)
        {
            string value = Environment.GetEnvironmentVariable(environmentKey);
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException("Coordinated road capture requires " + environmentKey);
            string path = Path.GetFullPath(value);
            string projectLogs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) &&
                !path.StartsWith(projectLogs, StringComparison.Ordinal))
                throw new InvalidOperationException("Coordinated capture output must be under /private/tmp or project SkirmishProduction");
            if (environmentKey == "WARLINE_S073_CURRENT_ROAD_MASK_PREFIX" &&
                (File.Exists(path + ".surface.bin") || File.Exists(path + ".sidewalk.bin")))
                throw new InvalidOperationException("Fresh current-road mask outputs required");
            if (environmentKey == "WARLINE_S073_PLACEMENT_AUDIT_OUTPUT" && File.Exists(path))
                throw new InvalidOperationException("Fresh placement-audit output required");
            return path;
        }

        private static JObject ObserveBlockerDomain(EntityManager em)
        {
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(DynamicBlockerComponent));
            if (grids.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique committed grid required");
            var grid = grids.GetSingletonEntity();
            var config = em.GetComponentData<GridConfig>(grid);
            var data = em.GetComponentData<DynamicBlockerComponent>(grid);
            if (data.StorageEpoch == 0 || data.WriteInProgress != 0 || data.AppliedStorageEpoch != data.StorageEpoch ||
                !data.Counts.IsCreated || !data.Blocked.IsCreated || data.Counts.Length != config.Width * config.Height)
                throw new InvalidOperationException("Committed static blocker storage required");
            var bytes = new byte[data.Counts.Length];
            int blocked = 0; long counts = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                int count = data.Counts[i];
                if (count < 0 || data.Blocked.IsSet(i) != (count > 0)) throw new InvalidOperationException("Inconsistent native blocker storage");
                if (count > 0) { bytes[i] = 1; blocked++; }
                counts += count;
            }
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            using var query = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
            using var entities = query.ToEntityArray(Allocator.Temp);
            var other = new JArray(); int source = 0; int bound = 0;
            foreach (var entity in entities)
            {
                bool isSource = em.HasComponent<OperationMapBuildingComponent>(entity);
                if (isSource) source++;
                bool hasHistory = em.HasComponent<StaticBlockerPrevBounds>(entity);
                var h = hasHistory ? em.GetComponentData<StaticBlockerPrevBounds>(entity) : default;
                bool owned = hasHistory && h.AppliedGrid == grid && h.StorageEpoch == data.StorageEpoch;
                if (owned) bound++;
                if (!isSource)
                    other.Add(new JObject { ["entity"] = new JArray(entity.Index, entity.Version),
                        ["faction"] = em.HasComponent<Faction>(entity) ? (int?)em.GetComponentData<Faction>(entity).Id : null,
                        ["source_key"] = em.HasComponent<UnitSourcePrefabKey>(entity) ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() : null,
                        ["history_bound"] = owned,
                        ["min"] = hasHistory ? new JArray(h.Min.x, h.Min.y) : null,
                        ["max"] = hasHistory ? new JArray(h.Max.x, h.Max.y) : null,
                        ["friendly_pass_faction"] = hasHistory ? (int?)h.FriendlyPassFactionId : null });
            }
            using var states = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (states.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique gameplay state required");
            var state = states.GetSingleton<RuntimeGameplayStateComponent>();
            return new JObject { ["grid_width"] = config.Width, ["grid_height"] = config.Height,
                ["blocked_cells"] = blocked, ["count_sum"] = counts, ["blocked_mask_sha256"] = hash,
                ["static_domain_count"] = entities.Length, ["source_building_count"] = source,
                ["histories_bound_to_grid"] = bound, ["other_blockers"] = other,
                ["play_requested"] = state.PlayRequested, ["simulation_active"] = state.SimulationActive,
                ["scope"] = "Committed native static domain at one post-stream initialized-spawn observation; not mission readiness" };
        }

        private static JObject Result(string status, JObject post) => new JObject
        {
            ["kind"] = "S073PrepareMapBoundaryAndStreamCensus", ["schema_version"] = 1, ["status"] = status,
            ["scope"] = "SameSessionBoundaryAndEcsStream_NotMissionOutcomeOrReadiness",
            ["unity_version"] = Application.unityVersion, ["session_id"] = session,
            ["source_pins"] = SourcePins(), ["before"] = before, ["after"] = boundaryAfter,
            ["post_stream"] = post, ["mission_ready"] = false, ["mission_outcome_observed"] = false
        };

        private static string OutputPath()
        {
            string path = Path.GetFullPath(Environment.GetEnvironmentVariable(EnvironmentKey));
            string projectLogs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) && !path.StartsWith(projectLogs, StringComparison.Ordinal))
                throw new InvalidOperationException("Private temporary or project SkirmishProduction output required");
            return path;
        }

        private static void Write(string path, JObject result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(file);
            writer.Write(result.ToString(Formatting.Indented) + "\n");
        }

        private static void Fail(string reason)
        {
            completed = true;
            SkirmishLaunchProjection.PrepareMapObserver -= Observe;
            EditorApplication.update -= ObserveStream;
            try { var result = Result("Failed", null); result["failure"] = reason; Write(OutputPath() + ".failure.json", result); }
            catch (Exception writeError) { Debug.LogException(writeError); }
            Debug.LogError("[S073PostStreamCensus] result=Failed " + reason);
            EditorApplication.delayCall += () => EditorApplication.Exit(1);
        }

        private static JObject Snapshot(EntityManager em, SkirmishMatchState match)
        {
            using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (maps.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique active operation-map root required");
            if (matches.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique S073 session required");
            var sessionEntity = matches.GetSingletonEntity();
            var mapRoot = maps.GetSingletonEntity();
            var active = maps.GetSingleton<ActiveOperationMapComponent>();
            var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
            if (!metadata.Blob.IsCreated || active.Generation != metadata.Generation ||
                active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                metadata.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01")
                throw new InvalidOperationException("Exact S073 logical/source map required");
            string scenePath = AssetDatabase.GUIDToAssetPath(SourceSceneGuid);
            var unityScene = SceneManager.GetSceneByPath(scenePath);
            var ecsGuid = new Unity.Entities.Hash128(EcsSceneGuid);
            var ecsScene = SceneSystem.GetSceneEntity(em.World.Unmanaged, ecsGuid);
            bool ecsLoaded = ecsScene != Entity.Null && SceneSystem.IsSceneLoaded(em.World.Unmanaged, ecsScene);
            int sections = ecsScene != Entity.Null && em.Exists(ecsScene) && em.HasBuffer<ResolvedSectionEntity>(ecsScene)
                ? em.GetBuffer<ResolvedSectionEntity>(ecsScene, true).Length : -1;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            bool gridOwned = false;
            Entity gridEntity = grids.CalculateEntityCount() == 1 ? grids.GetSingletonEntity() : Entity.Null;
            if (ecsLoaded && sections > 0 && grids.CalculateEntityCount() == 1)
            {
                var grid = grids.GetSingletonEntity();
                if (em.HasComponent<SceneTag>(grid))
                {
                    var section = em.GetSharedComponent<SceneTag>(grid).SceneEntity;
                    if (em.Exists(section) && em.HasComponent<SceneSectionData>(section) && em.HasComponent<SceneEntityReference>(section) &&
                        em.GetComponentData<SceneSectionData>(section).SceneGUID.Equals(ecsGuid) &&
                        em.GetComponentData<SceneEntityReference>(section).SceneEntity == ecsScene)
                        foreach (var resolved in em.GetBuffer<ResolvedSectionEntity>(ecsScene, true))
                            if (resolved.SectionEntity == section) gridOwned = true;
                }
            }
            using var query = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction));
            using var entities = query.ToEntityArray(Allocator.Temp);
            var rows = new JArray();
            bool gridCommitted = gridEntity != Entity.Null && em.HasComponent<DynamicBlockerComponent>(gridEntity) &&
                em.GetComponentData<DynamicBlockerComponent>(gridEntity).StorageEpoch != 0 &&
                em.GetComponentData<DynamicBlockerComponent>(gridEntity).WriteInProgress == 0 &&
                em.GetComponentData<DynamicBlockerComponent>(gridEntity).AppliedStorageEpoch ==
                    em.GetComponentData<DynamicBlockerComponent>(gridEntity).StorageEpoch;
            ulong gridEpoch = gridCommitted ? em.GetComponentData<DynamicBlockerComponent>(gridEntity).StorageEpoch : 0;
            int boundHistories = 0;
            foreach (var entity in entities)
            {
                var building = em.GetComponentData<OperationMapBuildingComponent>(entity);
                JObject staticHistory = null;
                if (em.HasComponent<StaticBlockerPrevBounds>(entity))
                {
                    var history = em.GetComponentData<StaticBlockerPrevBounds>(entity);
                    bool bound = gridCommitted && history.AppliedGrid == gridEntity && history.StorageEpoch == gridEpoch;
                    if (bound) boundHistories++;
                    staticHistory = new JObject { ["min"] = new JArray(history.Min.x, history.Min.y),
                        ["max"] = new JArray(history.Max.x, history.Max.y),
                        ["friendly_pass_faction"] = history.FriendlyPassFactionId, ["bound_to_grid_epoch"] = bound };
                }
                Entity section = em.HasComponent<SceneTag>(entity) ? em.GetSharedComponent<SceneTag>(entity).SceneEntity : Entity.Null;
                bool sectionOwned = false;
                if (section != Entity.Null && sections > 0 && em.Exists(section) &&
                    em.HasComponent<SceneSectionData>(section) && em.HasComponent<SceneEntityReference>(section) &&
                    em.GetComponentData<SceneSectionData>(section).SceneGUID.Equals(ecsGuid) &&
                    em.GetComponentData<SceneEntityReference>(section).SceneEntity == ecsScene)
                    foreach (var resolved in em.GetBuffer<ResolvedSectionEntity>(ecsScene, true))
                        if (resolved.SectionEntity == section) sectionOwned = true;
                rows.Add(new JObject
                {
                    ["entity"] = new JArray(entity.Index, entity.Version), ["faction"] = em.GetComponentData<Faction>(entity).Id,
                    ["stable_id"] = building.StableId.ToString(), ["source_global_object_id"] = building.SourceGlobalObjectId.ToString(),
                    ["placement_index"] = building.PlacementIndex,
                    ["static_history"] = staticHistory,
                    ["health_current"] = em.HasComponent<UnitHealth>(entity) ? (int?)em.GetComponentData<UnitHealth>(entity).Current : null,
                    ["scene_section"] = section == Entity.Null ? null : new JArray(section.Index, section.Version),
                    ["source_section_owned"] = sectionOwned,
                    ["ai"] = em.HasComponent<AIControlledTag>(entity),
                    ["combat_owner"] = em.HasComponent<RuntimeBuildingCombatInfo>(entity)
                        ? (int?)em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OwnerFactionId : null,
                    ["storage_owner"] = em.HasComponent<BuildingResourceStorageComponent>(entity)
                        ? (int?)em.GetComponentData<BuildingResourceStorageComponent>(entity).OwnerFactionId : null
                });
            }
            return new JObject
            {
                ["phase"] = match.Phase.ToString(), ["startup_failure"] = match.StartupFailure.ToString(), ["source_scene_guid"] = SourceSceneGuid,
                ["world_sequence"] = em.World.SequenceNumber.ToString(),
                ["session_entity"] = new JArray(sessionEntity.Index, sessionEntity.Version),
                ["map_root_entity"] = new JArray(mapRoot.Index, mapRoot.Version),
                ["source_scene_path"] = scenePath, ["source_scene_loaded"] = unityScene.IsValid() && unityScene.isLoaded,
                ["ecs_scene_guid"] = EcsSceneGuid, ["ecs_scene_loaded"] = ecsLoaded,
                ["ecs_scene_entity"] = ecsScene == Entity.Null ? null : new JArray(ecsScene.Index, ecsScene.Version),
                ["resolved_section_count"] = sections, ["authored_grid_section_owned"] = gridOwned,
                ["physical_source_validated"] = metadata.PhysicalSourceValidated,
                ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
                ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
                ["generation"] = metadata.Generation, ["entity_count"] = entities.Length, ["entities"] = rows,
                ["static_grid_committed"] = gridCommitted, ["static_history_bound_count"] = boundHistories
            };
        }

        private static JObject SourcePins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string runtimeScene = AssetDatabase.GUIDToAssetPath(SourceSceneGuid);
            string[] paths = { "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs",
                "Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs",
                "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
                "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset",
                "Assets/Game/Scripts/Editor/SkirmishIndustrialBasinAssetsBuilder.cs",
                "Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
                "Assets/Game/Scripts/Systems/BuildingPlacementRoadSurfaceMask.cs",
                "Assets/Game/Scripts/Composition/MapSurfaceSceneOverlayPresentation.cs",
                "Assets/Game/Scripts/Composition/MapSurfaceMeshGeometryPresentation.cs",
                "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset",
                runtimeScene,
                AssetDatabase.GUIDToAssetPath(EcsSceneGuid),
                "Assets/Game/Scripts/Editor/Skirmish/S073PrepareMapBoundaryObserver.cs" };
            var allPaths = new HashSet<string>(paths, StringComparer.Ordinal);
            foreach (string dependency in AssetDatabase.GetDependencies(runtimeScene, true))
                if (AssetDatabase.GetMainAssetTypeAtPath(dependency) == typeof(MapSurfaceMeshGeometryAsset)) allPaths.Add(dependency);
            var pins = new JObject();
            using var sha = SHA256.Create();
            foreach (string relative in allPaths.OrderBy(path => path, StringComparer.Ordinal))
                pins[relative] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, relative))))
                    .Replace("-", "").ToLowerInvariant();
            return pins;
        }
    }
}
#endif
