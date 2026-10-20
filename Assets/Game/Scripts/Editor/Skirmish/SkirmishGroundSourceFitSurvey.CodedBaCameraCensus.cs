#if UNITY_EDITOR
using System;
using System.IO;
using System.Globalization;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Composition;
using Game.Skirmish.Contracts;
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
        // Diagnostic export only. Never queues a mission or authors a view.
        public static void ExportCodedBaCameraCensusInputs()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating, "Idle Editor required for native compiler census");
            string output = Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_INPUTS");
            Require(CodedCameraLogsPath(output) && !File.Exists(output), "Fresh exact Logs output required");
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Require(catalog != null, "Actual packaged catalog required");
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error), error);
            var rows = new JArray();
            foreach (string id in new[] { "S002", "S003", "S004" })
            {
                Require(catalog.TryGetDefinition(id, out var definition) && definition != null &&
                    definition.CatalogId == id && AssetDatabase.Contains(definition) && !EditorUtility.IsDirty(definition),
                    "Exact clean existing packaged coded definition required");
                int seed = id == "S002" ? 104731 : id == "S003" ? 104732 : 155925;
                foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
                {
                    Require(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, seed,
                        new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds },
                        matrix, out var setup, out var reasons), "Normal unchanged compiler must admit: " + string.Join(";", reasons));
                    rows.Add(new JObject {
                        ["catalogId"] = id, ["definitionId"] = setup.DefinitionId,
                        ["definitionVersion"] = setup.ContentVersion, ["size"] = setup.SizeId.ToString(),
                        ["difficulty"] = setup.DifficultyId.ToString(), ["seed"] = setup.Seed,
                        ["setupHash"] = setup.SetupHash, ["contentHash"] = setup.ContentHash,
                        ["nativeConfigSha256"] = SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                        ["definitionPath"] = AssetDatabase.GetAssetPath(definition),
                        ["definitionSha256"] = Hash(AssetDatabase.GetAssetPath(definition)),
                        ["operationMapIdFromSetup"] = setup.OperationMapId, ["layoutId"] = setup.LayoutId,
                        ["serializedCampaignMapPresent"] = definition.CampaignMap != null,
                        ["canonicalForces"] = JToken.FromObject(setup.Forces),
                        ["requiredAircraftRoles"] = new JArray(CodedCameraAirRoles(setup).Select(x => x.ToString())),
                        ["liveSourceIdentityObserved"] = false, ["liveOpeningPoseObserved"] = false,
                        ["actualAircraftVisibleGeometryObserved"] = false
                    });
                }
            }
            Require(rows.Count == 9, "Exactly three missions by three sizes required");
            CodedCameraWrite(output, new JObject { ["scope"] = "ActualNativeNormalCompiler9Inputs_NotLiveCensus",
                ["cases"] = rows, ["cameraEngineeringDerived"] = false, ["cameraAccepted"] = false, ["missionReady"] = false });
            Debug.Log("[CodedBaCameraCompilerCensus] result=Collected catalogs=3 sizes=3 cases=9 liveSourceCensus=0 cameraAccepted=0");
        }

        // For an already-running normal native session; never launches or queues.
        public static void CaptureCurrentCodedBaCameraCensus()
        {
            Require(EditorApplication.isPlaying, "A genuine normal Play session is required");
            var world = World.DefaultGameObjectInjectionWorld;
            Require(world != null && world.IsCreated, "Actual native World required");
            var em = world.EntityManager;
            Require(SkirmishLaunchProjection.TryGet(em, out var session, out var match), "Actual native session required");
            CaptureCodedBaCameraObservation(em, session, in match, SkirmishLaunchProjection.IsSimulationActive(em),
                SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match));
        }

        static SkirmishRoleKind[] CodedCameraAirRoles(SkirmishResolvedSetup setup) =>
            setup.Forces.Where(x => x.Quantity > 0).Select(x => x.RoleKind)
                .Concat(setup.RoleOverlays.Select(x => x.RoleKind)).Distinct()
                .Where(x => SkirmishRoleIds.Category(x) == SkirmishPopulationCategory.Air)
                .OrderBy(x => (byte)x).ToArray();

        static bool CodedCameraLogsPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) return false;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SkirmishProduction")) +
                Path.DirectorySeparatorChar;
            return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        static void CodedCameraWrite(string path, JObject row)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
            writer.Write(row.ToString(Newtonsoft.Json.Formatting.Indented)); writer.Flush(); stream.Flush(true);
        }

        // Passive normal-frame diagnostic. Does not select, move, spawn, pause,
        // focus, change a camera, or send ARIA any hidden state.

        static OperationMapDefinition ResolveCodedCameraObservationSource(EntityManager em,
            SkirmishResolvedSetup setup, SkirmishScenarioDefinitionConfig definition, int scenarioIndex)
        {
            Require(em.World != null && em.World.IsCreated && definition != null &&
                definition.CatalogId == setup.CatalogId && definition.DefinitionId == setup.DefinitionId &&
                definition.ContentVersion == setup.ContentVersion &&
                SkirmishMissionDispatch.TryGetLegacyIndex(setup.CatalogId, out int expectedIndex) &&
                expectedIndex == scenarioIndex, "Coded observation must retain actual packaged launch identity");
            var source = SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition);
            Require(MatchSceneView.OperationMapLaunchResolver.TryResolve(em.World,
                SkirmishLaunchProjection.MissionId, SkirmishLaunchProjection.ScenarioId,
                SkirmishLaunchProjection.OperationMapId, out var selected, out _, out _),
                "Actual normal operation-map resolver refused coded observation");
            var catalogs = UnityEngine.Object.FindObjectsByType<MatchSceneView>(FindObjectsInactive.Exclude)
                .Select(view => view.OperationMapCatalog).ToArray();
            ValidateCodedCameraObservationSource(setup.CatalogId, source, selected.Definition,
                selected.OperationMapId.ToString(), selected.MissionId.ToString(),
                selected.ScenarioId.ToString(), catalogs);
            return source;
        }

        static void ValidateCodedCameraObservationSource(string catalogId, OperationMapDefinition source,
            OperationMapDefinition selectedDefinition, string mapId, string missionId, string scenarioId,
            OperationMapCatalogConfig[] liveCatalogs)
        {
            Require(source != null && mapId == source.OperationMapId &&
                mapId == SkirmishLaunchProjection.OperationMapId &&
                missionId == SkirmishLaunchProjection.MissionId &&
                scenarioId == SkirmishLaunchProjection.ScenarioId,
                "Actual normal selected map/mission/scenario differs from coded Campaign source");
            if (selectedDefinition != null)
            {
                Require(selectedDefinition == source, "Explicit normal source differs from strict source");
                return;
            }
            Require((catalogId == "S002" || catalogId == "S003") &&
                liveCatalogs != null && liveCatalogs.Length > 0,
                "Compatibility source requires an actual live MatchScene catalog");
            foreach (var catalog in liveCatalogs)
                Require(catalog != null && catalog.TryResolve(source.OperationMapId, out var actual) &&
                    actual == source, "Live MatchScene catalog differs from serialized normal source");
        }

        public static void RunCodedCameraSourceResolutionControls()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Require(catalog != null, "Actual packaged catalog required for source controls");
            int controls = 0;
            foreach (string id in new[] { "S002", "S003", "S004" })
            {
                Require(catalog.TryGetDefinition(id, out var definition) && definition != null,
                    "Actual coded definition required");
                var source = SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(definition);
                Require(source != null && source.OperationMapId == definition.OperationMapId &&
                    source.TryValidateMetadata(out _), "Actual strict packaged source must resolve");
                Require(SkirmishMissionDispatch.TryGetLegacyIndex(id, out int index),
                    "Actual coded index required");
                using var world = new World("coded-camera-source-controls");
                var entity = world.EntityManager.CreateEntity();
                world.EntityManager.AddComponentData(entity, new SkirmishMatchState { ScenarioIndex = index });
                Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError), matrixError);
                Require(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard, 155925, new SkirmishContentManifest
                    { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix, out var setup, out _),
                    "Actual packaged coded setup must compile");
                world.EntityManager.AddComponentData(entity, new SkirmishExpandedSessionComponent
                {
                    IsLegacy = 0, CatalogId = setup.CatalogId, DefinitionId = setup.DefinitionId,
                    ContentVersion = setup.ContentVersion, SetupHash = setup.SetupHash,
                    SizeId = setup.SizeId, DifficultyId = setup.DifficultyId, Seed = setup.Seed
                });
                world.EntityManager.AddComponentObject(entity, new SkirmishResolvedSetupRecord { Setup = setup });
                Require(MatchSceneView.OperationMapLaunchResolver.TryResolve(world,
                    SkirmishLaunchProjection.MissionId, SkirmishLaunchProjection.ScenarioId,
                    SkirmishLaunchProjection.OperationMapId, out var selected, out _, out _),
                    "Actual normal coded resolver required");
                Require(selected.OperationMapId.ToString() == source.OperationMapId &&
                    (id == "S004" ? selected.Definition == source :
                        selected.Definition == null || selected.Definition == source),
                    "Normal resolver must retain strict packaged source");
                controls++;
                var transient = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
                try
                {
                    var serialized = new SerializedObject(transient);
                    var entries = serialized.FindProperty("definitions");
                    Require(entries != null && entries.isArray, "Actual catalog definitions schema required");
                    entries.arraySize = 1;
                    entries.GetArrayElementAtIndex(0).objectReferenceValue = source;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    ValidateCodedCameraObservationSource(id, source, source, source.OperationMapId,
                        SkirmishLaunchProjection.MissionId, SkirmishLaunchProjection.ScenarioId, null);
                    controls++;
                    if (id != "S004")
                    {
                        ValidateCodedCameraObservationSource(id, source, null, source.OperationMapId,
                            SkirmishLaunchProjection.MissionId, SkirmishLaunchProjection.ScenarioId,
                            new[] { transient });
                        controls++;
                    }
                    Action<Action> refuses = action =>
                    {
                        bool refused = false;
                        try { action(); } catch (Exception) { refused = true; }
                        Require(refused, "Changed or absent normal source must refuse");
                        controls++;
                    };
                    refuses(() => ValidateCodedCameraObservationSource(id, source, null,
                        source.OperationMapId, SkirmishLaunchProjection.MissionId,
                        SkirmishLaunchProjection.ScenarioId, System.Array.Empty<OperationMapCatalogConfig>()));
                    refuses(() => ValidateCodedCameraObservationSource(id, source, null,
                        source.OperationMapId, SkirmishLaunchProjection.MissionId,
                        SkirmishLaunchProjection.ScenarioId, new OperationMapCatalogConfig[] { null }));
                    Require(catalog.TryGetDefinition(id == "S004" ? "S002" : "S004", out var other),
                        "Actual different coded source definition required");
                    var foreign = SkirmishS002AriaRunHarness.ResolveProductionBatchWarmupSource(other);
                    Require(foreign != source && foreign.OperationMapId == source.OperationMapId,
                        "Control must use genuinely different physical sources sharing the coded map ID");
                    refuses(() => ValidateCodedCameraObservationSource(id, source, foreign,
                        source.OperationMapId, SkirmishLaunchProjection.MissionId,
                        SkirmishLaunchProjection.ScenarioId, null));
                    entries.GetArrayElementAtIndex(0).objectReferenceValue = foreign;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    refuses(() => ValidateCodedCameraObservationSource(id, source, null,
                        source.OperationMapId, SkirmishLaunchProjection.MissionId,
                        SkirmishLaunchProjection.ScenarioId, new[] { transient }));
                    entries.arraySize = 0;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    refuses(() => ValidateCodedCameraObservationSource(id, source, null,
                        source.OperationMapId, SkirmishLaunchProjection.MissionId,
                        SkirmishLaunchProjection.ScenarioId, new[] { transient }));
                    refuses(() => ValidateCodedCameraObservationSource(id, source, source, "foreign",
                        SkirmishLaunchProjection.MissionId, SkirmishLaunchProjection.ScenarioId, null));
                }
                finally { UnityEngine.Object.DestroyImmediate(transient); }
            }
            Debug.Log("[CodedCameraSourceResolutionControls] result=Passed controls=" + controls +
                " nativeVisualAccepted=0");
        }

        public static void CaptureCodedBaCameraObservation(EntityManager em, Entity session,
            in SkirmishMatchState match, bool simulationActive, float elapsed)
        {
            string directory = Environment.GetEnvironmentVariable("WARLINE_CODED_BA_CAMERA_NATIVE_DIRECTORY");
            if (string.IsNullOrEmpty(directory)) return;
            Require(CodedCameraLogsPath(directory), "Census directory must stay under Logs/SkirmishProduction");
            if (!em.Exists(session) || !em.HasComponent<SkirmishResolvedSetupRecord>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var native = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (setup == null || !new[] { "S002", "S003", "S004" }.Contains(setup.CatalogId)) return;
            Require(native.IsLegacy == 0 && native.CatalogId.ToString() == setup.CatalogId &&
                native.DefinitionId.ToString() == setup.DefinitionId && native.ContentVersion == setup.ContentVersion &&
                native.SetupHash == setup.SetupHash && native.SizeId == setup.SizeId && native.DifficultyId == setup.DifficultyId &&
                native.Seed == setup.Seed, "Exact actual native coded session/setup required");
            Require(setup.DifficultyId == SkirmishDifficultyId.Regular &&
                new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar }.Contains(setup.SizeId),
                "Only exact three supported Regular sizes belong to this census");
            string attempt = native.SessionId.ToString();
            Require(attempt.Length > 0 && attempt.All(c => char.IsLetterOrDigit(c) || c == '-'), "Safe actual attempt token required");
            string output = Path.Combine(directory, setup.CatalogId + "." + setup.SizeId + "." + attempt + ".json");
            if (File.Exists(output)) return; // Immutable first actual observation; never overwrite.
            using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable));
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if (maps.CalculateEntityCount() != 1 || grids.CalculateEntityCount() != 1 || surfaces.CalculateEntityCount() != 1) return;
            var root = maps.GetSingletonEntity(); var active = maps.GetSingleton<ActiveOperationMapComponent>();
            var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
            if (!metadata.Blob.IsCreated || metadata.PhysicalSourceValidated == 0 || metadata.Generation != active.Generation) return;
            ref var blob = ref metadata.Blob.Value;
            Require(active.OperationMapId.Equals(blob.OperationMapId), "Actual active map and metadata identity must agree");
            Require(SkirmishLaunchProjection.TryGet(em, out var selectedSession, out var selectedMatch) &&
                selectedSession == session, "Actual normal launch projection required");
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Require(catalog != null, "Actual packaged catalog required");
            Require(catalog.TryGetDefinition(setup.CatalogId, out var definition) &&
                definition != null && definition.DefinitionId == setup.DefinitionId &&
                definition.ContentVersion == setup.ContentVersion && !EditorUtility.IsDirty(definition),
                "Actual packaged coded definition required");
            var selectedSource = ResolveCodedCameraObservationSource(em, setup, definition, selectedMatch.ScenarioIndex);
            Require(selectedSource.TryValidateMetadata(out string sourceError), sourceError);
            Require(active.OperationMapId.ToString() == selectedSource.OperationMapId &&
                blob.ContentHash.ToString() == selectedSource.ContentHash &&
                blob.GeneratedMetadataHash.ToString() == selectedSource.GeneratedMetadataHash &&
                blob.SourceIdentityHash.ToString() == (selectedSource.SourceBinding.IsConfigured ?
                    selectedSource.SourceBinding.SourceIdentityHash : selectedSource.SourceIdentityHash) &&
                blob.SourceContentHash.ToString() == (selectedSource.SourceBinding.IsConfigured ?
                    selectedSource.SourceBinding.SourceContentHash : selectedSource.ContentHash) &&
                blob.Navigation.AuthoredSubSceneGuid.ToString() == selectedSource.NavigationMetadata.AuthoredSubSceneGuid,
                "Actual native metadata must join unchanged normal selected Campaign source");
            var grid = grids.GetSingleton<GridConfig>(); var surface = surfaces.GetSingleton<MapSurfaceComponent>();
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated) return;
            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError), matrixError);
            Require(SkirmishSetupCompiler.TryCompile(definition, setup.DifficultyId, setup.SizeId, setup.Seed,
                new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix,
                out var checkedSetup, out var reasons) && checkedSetup.SetupHash == setup.SetupHash &&
                checkedSetup.ContentHash == setup.ContentHash &&
                SkirmishAcceptanceCensusCapture.ComputeConfigHash(checkedSetup) == SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                "Observed identity must join unchanged normal native compiler");

            var actors = new JArray();
            using var actorQuery = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using var entities = actorQuery.ToEntityArray(Allocator.Temp);
            int expected = 0;
            for (int forceIndex = 0; forceIndex < setup.Forces.Length; forceIndex++)
            {
                var force = setup.Forces[forceIndex];
                for (int member = 0; member < force.Quantity; member++)
                {
                    expected++;
                    string stable = force.RoleId + "." + force.FactionId.ToString(CultureInfo.InvariantCulture) + "." +
                        forceIndex.ToString(CultureInfo.InvariantCulture) + "." + member.ToString(CultureInfo.InvariantCulture);
                    Entity found = Entity.Null;
                    foreach (var actor in entities)
                    {
                        var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                        if (owned.SessionId != native.SessionId || owned.StableObjectId.ToString() != stable) continue;
                        Require(found == Entity.Null && owned.FactionId == force.FactionId && owned.ReservationId == 0 &&
                            em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role == force.RoleKind,
                            "Unique exact original actor/faction/role lineage required");
                        found = actor;
                    }
                    if (found == Entity.Null || !em.HasComponent<UnitHealth>(found) ||
                        em.GetComponentData<UnitHealth>(found).Current <= 0) return; // Not complete opening roster.
                    var measurement = SkirmishCameraFramingMeasurement.Capture(em, found, Camera.main);
                    actors.Add(new JObject { ["forceIndex"] = forceIndex, ["member"] = member,
                        ["stableActorId"] = stable, ["role"] = force.RoleKind.ToString(), ["faction"] = force.FactionId,
                        ["nativeActorIndex"] = found.Index, ["nativeActorVersion"] = found.Version,
                        ["actualCameraMeasurement"] = JObject.Parse(JsonUtility.ToJson(measurement)) });
                }
            }
            var aircraft = new JArray();
            foreach (var role in CodedCameraAirRoles(setup))
            {
                var prefab = FindNativePrefab(em, SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                var row = new JObject { ["role"] = role.ToString(), ["runtimePrefabKey"] = SkirmishRoleCatalogConfig.RuntimePrefabKey(role),
                    ["nativePrefabMetadataPresent"] = false, ["actualVisibleGeometryObserved"] = false };
                if (prefab != Entity.Null && em.HasComponent<UnitSelectionHitbox>(prefab) && em.HasComponent<UnitAirMovement>(prefab))
                {
                    var hitbox = em.GetComponentData<UnitSelectionHitbox>(prefab);
                    row["nativePrefabMetadataPresent"] = true; row["nativePrefabIndex"] = prefab.Index; row["nativePrefabVersion"] = prefab.Version;
                    row["selectionBodyExtents"] = new JArray(hitbox.Extents.x, hitbox.Extents.y, hitbox.Extents.z);
                    row["cruiseHeight"] = em.GetComponentData<UnitAirMovement>(prefab).CruiseHeight;
                    row["bodyMetadataIsNotVisibleSweptGeometry"] = true;
                }
                row["actualVisibleGeometryObserved"] = actors.OfType<JObject>().Any(a => (string)a["role"] == role.ToString() &&
                    (bool?)a["actualCameraMeasurement"]?["Valid"] == true &&
                    (string)a["actualCameraMeasurement"]?["ProjectionGeometrySource"] == "ActualActiveDrawnPosedMeshVertices");
                aircraft.Add(row);
            }
            bool sourceGridJoin = grid.Width == blob.Grid.Dimensions.x && grid.Height == blob.Grid.Dimensions.y &&
                grid.CellSize == blob.Grid.CellSize && math.all(grid.Origin == blob.Grid.Origin);
            var record = new JObject { ["scope"] = "PassiveActualNormalNativeSourceOpeningRosterAndGeometryCensus_NotAcceptance",
                ["frame"] = Time.frameCount, ["world"] = em.WorldUnmanaged.SequenceNumber.ToString(),
                ["attempt"] = attempt, ["catalogId"] = setup.CatalogId, ["size"] = setup.SizeId.ToString(),
                ["difficulty"] = setup.DifficultyId.ToString(), ["seed"] = setup.Seed, ["setupHash"] = setup.SetupHash,
                ["nativeConfigSha256"] = SkirmishAcceptanceCensusCapture.ComputeConfigHash(setup),
                ["definitionPath"] = AssetDatabase.GetAssetPath(definition), ["definitionSha256"] = Hash(AssetDatabase.GetAssetPath(definition)),
                ["phase"] = match.Phase.ToString(), ["simulationActive"] = simulationActive, ["elapsedSeconds"] = elapsed,
                ["beforeSimulationAdvance"] = !simulationActive && elapsed == 0,
                ["activeOperationMapId"] = active.OperationMapId.ToString(), ["sourceOperationMapId"] = blob.SourceOperationMapId.ToString(),
                ["sourceIdentityHash"] = blob.SourceIdentityHash.ToString(), ["sourceContentHash"] = blob.SourceContentHash.ToString(),
                ["contentHash"] = blob.ContentHash.ToString(), ["generatedMetadataHash"] = blob.GeneratedMetadataHash.ToString(),
                ["selectedSourcePath"] = AssetDatabase.GetAssetPath(selectedSource),
                ["selectedSourceSha256"] = Hash(AssetDatabase.GetAssetPath(selectedSource)),
                ["selectedSourceSceneGuid"] = selectedSource.SourceSceneReference.AssetGUID,
                ["sourceGridAssetGuid"] = blob.Grid.AssetGuid.ToString(), ["sourceGridHash"] = blob.Grid.ContentHash.ToString(),
                ["sourceSurfaceAssetGuid"] = blob.Surface.AssetGuid.ToString(), ["sourceSurfaceHash"] = blob.Surface.ContentHash.ToString(),
                ["authoredSubSceneGuid"] = blob.Navigation.AuthoredSubSceneGuid.ToString(),
                ["sourceMinimumElevation"] = blob.Surface.MinimumHeight, ["sourceMaximumElevation"] = blob.Surface.MaximumHeight,
                ["grid"] = new JObject { ["width"] = grid.Width, ["height"] = grid.Height, ["cellSize"] = grid.CellSize,
                    ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z) },
                ["sourceGridMetadataMatchesActual"] = sourceGridJoin,
                ["expectedOriginalActors"] = expected, ["observedOriginalActors"] = actors,
                ["requiredAircraft"] = aircraft, ["cameraEngineeringDerived"] = false,
                ["actualAircraftVisibleGeometryComplete"] = aircraft.OfType<JObject>().All(a => (bool?)a["actualVisibleGeometryObserved"] == true),
                ["cameraAccepted"] = false, ["normalMissionReady"] = false };
            if (em.HasComponent<OperationMapBoundsComponent>(root))
            {
                var bounds = em.GetComponentData<OperationMapBoundsComponent>(root);
                record["actualPlayableBounds"] = new JArray(bounds.PlayableMin.x, bounds.PlayableMin.y, bounds.PlayableMin.z,
                    bounds.PlayableMax.x, bounds.PlayableMax.y, bounds.PlayableMax.z);
                record["actualCameraBounds"] = new JArray(bounds.CameraMin.x, bounds.CameraMin.y, bounds.CameraMin.z,
                    bounds.CameraMax.x, bounds.CameraMax.y, bounds.CameraMax.z);
            }
            CodedCameraWrite(output, record);
            Debug.Log("[CodedBaCameraNativeCensus] result=Collected catalog=" + setup.CatalogId + " size=" + setup.SizeId +
                " originalActors=" + expected + " cameraEngineeringDerived=0 cameraAccepted=0 normalMissionReady=0 output=" + output);
        }
    }
}
#endif
