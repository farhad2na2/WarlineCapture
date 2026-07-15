#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Game.Configs;
    using Newtonsoft.Json.Linq;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class OperationMapPhase0NavigationMetadataOwnershipProbe
    {
        internal const string ReportSchema = "warline.operation-map.phase0-navigation-metadata-ownership";
        internal const int ReportSchemaVersion = 1;
        internal const string BaselineCommit = "2873e4dd7b2f1f5a5727c1de81bd9c86f97dc60d";
        internal const string ReportPathEnvironmentVariable =
            "WARLINE_OPERATION_MAP_PHASE0_NAVIGATION_METADATA_OWNERSHIP_REPORT_PATH";
        internal const string DefaultReportPath =
            "/private/tmp/warline-operation-map-phase0-navigation-metadata-ownership.json";

        private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
        private const string MatchSceneGuid = "cc4f48a57793d4597b4ffac2906c515e";
        private const string MatchSubScenePath = "Assets/Game/Scenes/Match/MatchSubScene.unity";
        private const string MatchSubSceneGuid = "8d5e3c3f2ef84b61a4d61472c40c9a11";
        private const string SurfaceAssetPath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
        private const string SurfaceAssetGuid = "12f517deb32ab49698acbfdaf7c3eac7";
        private const string GridAssetPath = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset";
        private const string GridAssetGuid = "b201000000000000000000000000000b";
        private const string AirportPrefabPath = "Assets/Game/Prefabs/Buildings/Building_Airport.prefab";
        private const string AirportPrefabGuid = "37ac92ce4e0e7473891bdb9b135724f2";
        private const string HelipadPrefabPath = "Assets/Game/Prefabs/Buildings/Building_Helipad.prefab";
        private const string HelipadPrefabGuid = "077763b7910da4a67be0c118c091c302";
        private const string AirportDefinitionPath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Airport_Config.asset";
        private const string AirportDefinitionGuid = "1b2efc28619c34a21acc851aa669589f";
        private const string HelipadDefinitionPath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Helipad_Config.asset";
        private const string HelipadDefinitionGuid = "b3010000000000000000000000000004";
        private const long SurfaceAuthoringLocalId = 8387065878709727638;
        private const long GridAuthoringLocalId = 146043441;
        private const long RunwayLocalId = 4896067565179094789;
        private const long RunwayStartLocalId = 1583421028472175978;
        private const long RunwayEndLocalId = 2285726051564322001;
        private const long HelipadSpawnLocalId = 8234254015469105350;
        private const long MainAssetLocalId = 11400000;
        private const string DecisionState = "NeedsDecision";
        private const string CurrentOwner = "Match map compatibility content and runtime systems";
        private const string TargetOwner = "Operation map definition navigation metadata";
        private const string MigrationDisposition =
            "Move the exact GUID-backed metadata binding with the operation map definition, preserve asset and local-file identities, and prove runtime parity before removing Match compatibility ownership.";
        private const string MigrationOwner = "Operation map architecture owner and navigation gameplay owner";
        private const string DecisionOwner =
            "Operation map architecture owner, navigation gameplay owner, and air operations owner";

        private const string ExpectedPayloadSha256 =
            "f330f6914b7f48563625a083b3925bf7cee2d3490e7589a56372058338105de3";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public static void Run()
        {
            string projectRoot = RequireProjectRoot();
            string outputPath = ResolveReportOutputPath(
                projectRoot,
                Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable));
            InvalidateOutput(outputPath);
            List<InputHashReport> before = HashDirectInputs(projectRoot);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            NavigationMetadataOwnershipReport report;

            try
            {
                RequireCleanLoadedScenes();
                List<AuthorityReport> authorities = InspectAuthorities();
                List<RuntimeConsumerReport> consumers = BuildRuntimeConsumers(projectRoot);
                List<CrossReferenceReport> references = BuildCrossReferences(projectRoot);
                List<InputHashReport> after = HashDirectInputs(projectRoot);
                RequireInputHashesEqual(before, after);
                report = BuildReport(authorities, consumers, references, before);
            }
            finally
            {
                RestoreSceneSetup(previousSetup);
            }

            string json = UnityEngine.JsonUtility.ToJson(report, true) + "\n";
            PublishReportAtomically(outputPath, json);
            Debug.Log(
                $"[OperationMapPhase0NavigationMetadataOwnershipProbe] result={report.result} " +
                $"authorities={report.counts.authorities} consumers={report.counts.runtimeConsumers} " +
                $"needsDecision={report.counts.needsDecision} payload={report.identityPayloadSha256} " +
                $"report={outputPath}");
        }

        internal static string ResolveReportOutputPath(string projectRoot, string configuredPath)
        {
            string resolved = OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                string.IsNullOrWhiteSpace(configuredPath) ? DefaultReportPath : configuredPath);
            RequireSymlinkSafePath(resolved);
            return resolved;
        }

        internal static NavigationMetadataOwnershipReport BuildReport(
            List<AuthorityReport> authorities,
            List<RuntimeConsumerReport> consumers,
            List<CrossReferenceReport> references,
            List<InputHashReport> directInputHashes)
        {
            var report = new NavigationMetadataOwnershipReport
            {
                reportSchema = ReportSchema,
                reportSchemaVersion = ReportSchemaVersion,
                baselineCommit = BaselineCommit,
                result = DecisionState,
                counts = new ReportCounts
                {
                    authorities = authorities?.Count ?? 0,
                    runtimeConsumers = consumers?.Count ?? 0,
                    acceptedCrossReferences = references?.Count ?? 0,
                    directInputs = directInputHashes?.Count ?? 0,
                    needsDecision = authorities?.Count ?? 0
                },
                acceptedEvidence = references?.OrderBy(entry => entry.task, StringComparer.Ordinal).ToList(),
                directInputHashes = directInputHashes?.OrderBy(entry => entry.path, StringComparer.Ordinal).ToList(),
                authorities = authorities?.OrderBy(entry => entry.stableIdentity, StringComparer.Ordinal).ToList(),
                runtimeConsumers = consumers?.OrderBy(entry => entry.stableIdentity, StringComparer.Ordinal).ToList()
            };
            report.identityPayloadSha256 = ComputeIdentityPayloadSha256(report);

            if (!string.Equals(ExpectedPayloadSha256, "__PIN_AFTER_FIRST_RUN__", StringComparison.Ordinal) &&
                !string.Equals(report.identityPayloadSha256, ExpectedPayloadSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Navigation metadata identity payload drifted: {report.identityPayloadSha256}");
            }

            if (!HasRequiredReportShape(UnityEngine.JsonUtility.ToJson(report)))
                throw new InvalidOperationException("Navigation metadata ownership report validation failed.");
            return report;
        }

        internal static bool HasRequiredReportShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json) ||
                json.Contains("projectRoot", StringComparison.Ordinal) ||
                json.Contains("unityVersion", StringComparison.Ordinal) ||
                json.Contains("outputPath", StringComparison.Ordinal) ||
                !HasExactJsonSchema(json))
            {
                return false;
            }

            try
            {
                NavigationMetadataOwnershipReport report =
                    UnityEngine.JsonUtility.FromJson<NavigationMetadataOwnershipReport>(json);
                if (report == null ||
                    !string.Equals(report.reportSchema, ReportSchema, StringComparison.Ordinal) ||
                    report.reportSchemaVersion != ReportSchemaVersion ||
                    !string.Equals(report.baselineCommit, BaselineCommit, StringComparison.Ordinal) ||
                    !string.Equals(report.result, DecisionState, StringComparison.Ordinal) ||
                    report.counts == null ||
                    report.counts.authorities != 15 ||
                    report.counts.runtimeConsumers != 13 ||
                    report.counts.acceptedCrossReferences != 3 ||
                    report.counts.directInputs != ExpectedDirectInputHashes().Count ||
                    report.counts.needsDecision != report.counts.authorities)
                {
                    return false;
                }

                if (!HasExpectedCrossReferences(report.acceptedEvidence) ||
                    !HasExpectedInputHashes(report.directInputHashes) ||
                    !HasExpectedAuthorities(report.authorities) ||
                    !HasExpectedRuntimeConsumers(report.runtimeConsumers))
                {
                    return false;
                }

                string computed = ComputeIdentityPayloadSha256(report);
                if (!string.Equals(report.identityPayloadSha256, computed, StringComparison.Ordinal))
                    return false;
                return string.Equals(ExpectedPayloadSha256, "__PIN_AFTER_FIRST_RUN__", StringComparison.Ordinal) ||
                       string.Equals(computed, ExpectedPayloadSha256, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        internal static void PublishReportAtomically(
            string outputPath,
            string json,
            Action beforeCommit = null)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is required.", nameof(outputPath));

            RequireSymlinkSafePath(outputPath);
            InvalidateOutput(outputPath);
            string temporaryPath = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                if (!HasRequiredReportShape(json))
                    throw new InvalidOperationException("Refusing to publish invalid navigation metadata evidence.");

                using (FileStream stream = new(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           4096,
                           FileOptions.WriteThrough))
                using (StreamWriter writer = new(stream, Utf8WithoutBom))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                RequireRegularFile(temporaryPath);
                string persisted = File.ReadAllText(temporaryPath, Utf8WithoutBom);
                if (!string.Equals(persisted, json, StringComparison.Ordinal) ||
                    !HasRequiredReportShape(persisted))
                {
                    throw new InvalidOperationException("Persisted navigation metadata evidence is invalid.");
                }

                beforeCommit?.Invoke();
                if (File.Exists(outputPath))
                    throw new IOException("Output appeared during atomic publication.");
                File.Move(temporaryPath, outputPath);
                RequireRegularFile(outputPath);
            }
            catch
            {
                DeleteIfPresent(outputPath);
                throw;
            }
            finally
            {
                DeleteIfPresent(temporaryPath);
            }
        }

        internal static void InvalidateOutput(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                return;
            if (File.Exists(outputPath))
            {
                RequireRegularFile(outputPath);
                File.Delete(outputPath);
            }
        }

        internal static List<InputHashReport> HashFiles(string projectRoot, IEnumerable<string> paths)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            var reports = new List<InputHashReport>();
            foreach (string path in paths.OrderBy(value => value, StringComparer.Ordinal))
            {
                string fullPath = Path.Combine(projectRoot, path);
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Required navigation metadata source is absent: {path}", fullPath);
                reports.Add(new InputHashReport
                {
                    path = path,
                    sha256 = OperationMapPhase0BaselineProbe.ComputeSha256(File.ReadAllBytes(fullPath))
                });
            }
            return reports;
        }

        internal static string ComputeIdentityPayloadSha256(NavigationMetadataOwnershipReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            var builder = new StringBuilder(32768);
            Append(builder, report.reportSchema, report.reportSchemaVersion.ToString(CultureInfo.InvariantCulture),
                report.baselineCommit, report.result);
            foreach (CrossReferenceReport entry in report.acceptedEvidence ?? new List<CrossReferenceReport>())
                Append(builder, entry.task, entry.path, entry.reportSchema, entry.sourceRevision, entry.evidenceSha256);
            foreach (InputHashReport entry in report.directInputHashes ?? new List<InputHashReport>())
                Append(builder, entry.path, entry.sha256);
            foreach (AuthorityReport entry in report.authorities ?? new List<AuthorityReport>())
            {
                Append(builder, entry.kind, entry.stableIdentity, entry.sourceRevision, entry.assetPath,
                    entry.assetGuid, entry.localId.ToString(CultureInfo.InvariantCulture), entry.exactType,
                    entry.scenePath, entry.hierarchyPath, entry.metadata, entry.currentOwner, entry.targetOwner,
                    entry.migrationDisposition, entry.migrationOwner, entry.state, entry.decisionOwner);
            }
            foreach (RuntimeConsumerReport entry in report.runtimeConsumers ?? new List<RuntimeConsumerReport>())
                Append(builder, entry.stableIdentity, entry.sourceRevision, entry.sourcePath, entry.exactType,
                    entry.consumes, entry.responsibility);
            return OperationMapPhase0BaselineProbe.ComputeSha256(Utf8WithoutBom.GetBytes(builder.ToString()));
        }

        private static List<AuthorityReport> InspectAuthorities()
        {
            MapSurfaceDataAsset surface = RequireAsset<MapSurfaceDataAsset>(SurfaceAssetPath, SurfaceAssetGuid);
            GridAuthoringSceneConfigAsset grid = RequireAsset<GridAuthoringSceneConfigAsset>(GridAssetPath, GridAssetGuid);
            BuildingDefinitionAuthoringConfig airportDefinition =
                RequireAsset<BuildingDefinitionAuthoringConfig>(AirportDefinitionPath, AirportDefinitionGuid);
            BuildingDefinitionAuthoringConfig helipadDefinition =
                RequireAsset<BuildingDefinitionAuthoringConfig>(HelipadDefinitionPath, HelipadDefinitionGuid);
            GameObject airport = RequireAsset<GameObject>(AirportPrefabPath, AirportPrefabGuid);
            GameObject helipad = RequireAsset<GameObject>(HelipadPrefabPath, HelipadPrefabGuid);

            RequireGridParity(surface, grid);
            Scene matchScene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            MapSurfaceAuthoring surfaceAuthoring = RequireSingleComponent<MapSurfaceAuthoring>(matchScene);
            RequireObjectIdentity(surfaceAuthoring, MatchSceneGuid, SurfaceAuthoringLocalId);
            if (surfaceAuthoring.BakedSurfaceData != surface || surfaceAuthoring.GridConfig != grid)
                throw new InvalidOperationException("MapSurfaceAuthoring asset binding drifted.");
            string surfaceAuthoringHierarchy = HierarchyPath(surfaceAuthoring.transform);
            string surfaceAuthoringMetadata =
                $"surfaceGuid={SurfaceAssetGuid}|gridGuid={GridAssetGuid}|samplesPerCellAxis={surfaceAuthoring.SamplesPerCellAxis}|maxSampleHeightDelta={F(surfaceAuthoring.MaxSampleHeightDelta)}|maxBuildingSlope={F(surfaceAuthoring.MaxBuildingSlopeDegrees)}|maxInfantrySlope={F(surfaceAuthoring.MaxInfantrySlopeDegrees)}|maxVehicleSlope={F(surfaceAuthoring.MaxVehicleSlopeDegrees)}";

            Scene subScene = EditorSceneManager.OpenScene(MatchSubScenePath, OpenSceneMode.Single);
            GridAuthoring gridAuthoring = RequireSingleComponent<GridAuthoring>(subScene);
            RequireObjectIdentity(gridAuthoring, MatchSubSceneGuid, GridAuthoringLocalId);
            SerializedProperty gridConfigProperty = new SerializedObject(gridAuthoring).FindProperty("config");
            if (gridConfigProperty == null || gridConfigProperty.objectReferenceValue != grid)
                throw new InvalidOperationException("GridAuthoring config binding drifted.");

            SurfaceCounts surfaceCounts = InspectSurfaceCounts(surface);
            Transform runway = RequireTransformIdentity(airport, RunwayLocalId, "Runway");
            Transform runwayStart = RequireTransformIdentity(airport, RunwayStartLocalId, "Runway_Start");
            Transform runwayEnd = RequireTransformIdentity(airport, RunwayEndLocalId, "Runway_End");
            Transform helipadSpawn = RequireTransformIdentity(helipad, HelipadSpawnLocalId, "Spawn_01");
            if (runwayStart.parent != runway || runwayEnd.parent != runway)
                throw new InvalidOperationException("Runway endpoint parent identity drifted.");

            string gridMetadata =
                $"width={grid.Width}|height={grid.Height}|cellSize={F(grid.CellSize)}|origin={V(grid.Origin)}|blockedCells={grid.BlockedCells.Length}";
            string surfaceMetadata =
                $"width={surface.Dimensions.x}|height={surface.Dimensions.y}|cellSize={F(surface.CellSize)}|origin={V(surface.GridOrigin)}|surfaces={surface.SurfaceCount}|connections={surface.ConnectionCount}|payloadVersion={surface.PayloadVersion}|payloadEncoding={surface.PayloadEncoding}|runtimeBlobHash={surface.ComputeRuntimeBlobHash()}";
            string helipadProductionMetadata = string.Join(",", helipadDefinition.Productions.Select((entry, index) =>
                $"{index}:{RequireAssetIdentity(entry.SpawnUnitPrefab).guid}:{RequireAssetIdentity(entry.SpawnUnitPrefab).localId}"));

            var authorities = new List<AuthorityReport>
            {
                Authority("bridge-surfaces", $"{SurfaceAssetPath}#surface-type:BridgeDeck", SurfaceAssetPath,
                    SurfaceAssetGuid, MainAssetLocalId, typeof(MapSurfaceDataAsset).FullName, string.Empty, string.Empty,
                    $"surfaceType={MapSurfaceType.BridgeDeck}|count={surfaceCounts.bridge}|flag={MapSurfaceFlags.Bridge}"),
                Authority("dynamic-blockers", "Game.Components.DynamicBlockerComponent", string.Empty, string.Empty, 0,
                    typeof(DynamicBlockerComponent).FullName, string.Empty, string.Empty,
                    "storage=counts+blocked+friendlyPassFactionIds|authority=Game.Runtime.DynamicBlockerInitSystem"),
                Authority("dynamic-occupancy", "Game.Components.DynamicOccupancyComponent", string.Empty, string.Empty, 0,
                    typeof(DynamicOccupancyComponent).FullName, string.Empty, string.Empty,
                    "storage=occupied|authority=Game.Runtime.DynamicOccupancyRebuildSystem|tracked=UnitGrid+UnitFootprint"),
                Authority("grid-authoring", $"{MatchSubScenePath}#localId:{GridAuthoringLocalId}", MatchSubScenePath,
                    MatchSubSceneGuid, GridAuthoringLocalId, typeof(GridAuthoring).FullName, MatchSubScenePath,
                    HierarchyPath(gridAuthoring.transform), gridMetadata),
                Authority("grid-config", GridAssetPath, GridAssetPath, GridAssetGuid, MainAssetLocalId,
                    typeof(GridAuthoringSceneConfigAsset).FullName, string.Empty, string.Empty, gridMetadata),
                Authority("helipad-definition", HelipadDefinitionPath, HelipadDefinitionPath, HelipadDefinitionGuid,
                    MainAssetLocalId, helipadDefinition.GetType().FullName, string.Empty, string.Empty,
                    $"displayName={helipadDefinition.DisplayName}|productions={helipadDefinition.Productions.Count}|productionAssets={helipadProductionMetadata}"),
                Authority("helipad-spawn", $"{HelipadPrefabPath}#localId:{HelipadSpawnLocalId}", HelipadPrefabPath,
                    HelipadPrefabGuid, HelipadSpawnLocalId, typeof(Transform).FullName, string.Empty,
                    HierarchyPath(helipadSpawn), $"localPosition={V(helipadSpawn.localPosition)}|localRotation={Q(helipadSpawn.localRotation)}"),
                Authority("map-surface-authoring", $"{MatchScenePath}#localId:{SurfaceAuthoringLocalId}", MatchScenePath,
                    MatchSceneGuid, SurfaceAuthoringLocalId, typeof(MapSurfaceAuthoring).FullName, MatchScenePath,
                    surfaceAuthoringHierarchy, surfaceAuthoringMetadata),
                Authority("map-surface-data", SurfaceAssetPath, SurfaceAssetPath, SurfaceAssetGuid, MainAssetLocalId,
                    typeof(MapSurfaceDataAsset).FullName, string.Empty, string.Empty, surfaceMetadata),
                Authority("road-surfaces", $"{SurfaceAssetPath}#flag:Road", SurfaceAssetPath, SurfaceAssetGuid,
                    MainAssetLocalId, typeof(MapSurfaceDataAsset).FullName, string.Empty, string.Empty,
                    $"flag={MapSurfaceFlags.Road}|count={surfaceCounts.road}"),
                Authority("runway-end", $"{AirportPrefabPath}#localId:{RunwayEndLocalId}", AirportPrefabPath,
                    AirportPrefabGuid, RunwayEndLocalId, typeof(Transform).FullName, string.Empty,
                    HierarchyPath(runwayEnd), $"localPosition={V(runwayEnd.localPosition)}|localRotation={Q(runwayEnd.localRotation)}"),
                Authority("runway-metadata", "Game.Runtime.BuildingRunwaySystem", AirportDefinitionPath,
                    AirportDefinitionGuid, MainAssetLocalId, "Game.Runtime.BuildingRunwaySystem", string.Empty,
                    HierarchyPath(runway), $"airportProductions={airportDefinition.Productions.Count}|runwayLocalId={RunwayLocalId}|startLocalId={RunwayStartLocalId}|endLocalId={RunwayEndLocalId}"),
                Authority("runway-start", $"{AirportPrefabPath}#localId:{RunwayStartLocalId}", AirportPrefabPath,
                    AirportPrefabGuid, RunwayStartLocalId, typeof(Transform).FullName, string.Empty,
                    HierarchyPath(runwayStart), $"localPosition={V(runwayStart.localPosition)}|localRotation={Q(runwayStart.localRotation)}"),
                Authority("static-blockers", "Game.Components.StaticGridBlocker", string.Empty, string.Empty, 0,
                    typeof(StaticGridBlocker).FullName, string.Empty, string.Empty,
                    "marker=StaticGridBlocker|bounds=GridBlockerSize+StaticBlockerPrevBounds|authority=Game.Runtime.StaticGridBlockerUpdateSystem"),
                Authority("terrain-surfaces", $"{SurfaceAssetPath}#surface-type:Terrain", SurfaceAssetPath,
                    SurfaceAssetGuid, MainAssetLocalId, typeof(MapSurfaceDataAsset).FullName, string.Empty, string.Empty,
                    $"surfaceType={MapSurfaceType.Terrain}|count={surfaceCounts.terrain}|blocked={surfaceCounts.blocked}")
            };
            authorities.Sort((left, right) => string.CompareOrdinal(left.stableIdentity, right.stableIdentity));
            return authorities;
        }

        private static AuthorityReport Authority(
            string kind,
            string stableIdentity,
            string assetPath,
            string assetGuid,
            long localId,
            string exactType,
            string scenePath,
            string hierarchyPath,
            string metadata)
        {
            return new AuthorityReport
            {
                kind = kind,
                stableIdentity = stableIdentity,
                sourceRevision = BaselineCommit,
                assetPath = assetPath,
                assetGuid = assetGuid,
                localId = localId,
                exactType = exactType,
                scenePath = scenePath,
                hierarchyPath = hierarchyPath,
                metadata = metadata,
                currentOwner = CurrentOwner,
                targetOwner = TargetOwner,
                migrationDisposition = MigrationDisposition,
                migrationOwner = MigrationOwner,
                state = DecisionState,
                decisionOwner = DecisionOwner
            };
        }

        private static List<RuntimeConsumerReport> BuildRuntimeConsumers(string projectRoot)
        {
            ConsumerSpec[] specs =
            {
                new("Game.Composition.MapSurfaceRuntimeBootstrapSceneSystemHelper", "Assets/Game/Scripts/Composition/MapSurfaceRuntimeBootstrapSceneSystemHelper.cs", "MapSurfaceDataAsset|MapSurfaceAuthoring|MapSurfaceComponent", "Publishes the selected baked map surface and scene overlays into the runtime world."),
                new("Game.Runtime.BuildingRuntimeProcessingCompositionSystemHelper", "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs", "BuildingFactionProductionSpawnPointReadModel", "Publishes exact building production spawn slots for downstream consumers."),
                new("Game.Runtime.BuildingRunwaySystem", "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs", "definition.HasRunway|Runway|Runway_Start|Runway_End", "Resolves runway geometry and effective placement occupancy."),
                new("Game.Runtime.BuildingSpawnCompositionSystemHelper", "Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs", "Building_Helipad|BuildingFactionProductionSpawnPointReadModel", "Resolves available faction helipad spawn slots from the runtime read model."),
                new("Game.Runtime.BuildingRuntimeSurfaceOverlaySystem", "Assets/Game/Scripts/Systems/BuildingRuntimeSurfaceOverlaySystem.cs", "BuildingRunwaySystem|BuildingRuntimeSurfaceOverlay", "Publishes runtime runway road-surface overlays."),
                new("Game.Runtime.DynamicBlockerInitSystem", "Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs", "GridConfig|DynamicBlockerComponent|DynamicOccupancyComponent", "Allocates singleton dynamic blocker and occupancy storage from grid dimensions."),
                new("Game.Runtime.DynamicOccupancyRebuildSystem", "Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs", "GridConfig|DynamicOccupancyComponent|UnitGrid|UnitFootprint", "Maintains moving-unit occupancy authority."),
                new("Game.Runtime.InitialUnitsSpawnSystem", "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs", "Building_Helipad|BuildingFactionProductionSpawnPointReadModel", "Selects exact initial air-platform spawn slots."),
                new("Game.Runtime.MapSurfaceDiagnosticsSystem", "Assets/Game/Scripts/Systems/MapSurfaceDiagnosticsSystem.cs", "MapSurfaceComponent|MapSurfaceDiagnosticsComponent", "Counts terrain, road, bridge, ramp, blocked, and layered runtime surface metadata."),
                new("Game.Runtime.MapSurfacePathfindingSnapshot", "Assets/Game/Scripts/Systems/MapSurfacePathfindingSnapshot.cs", "MapSurfaceComponent|MapSurfacePathCostComponent", "Snapshots authored surface and path-cost metadata for pathfinding."),
                new("Game.Runtime.StaticGridBlockerUpdateSystem", "Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs", "StaticGridBlocker|DynamicBlockerComponent|StaticBlockerPrevBounds", "Projects static blocker bounds into the dynamic blocker authority."),
                new("Game.Runtime.UnitAirMovementSystem", "Assets/Game/Scripts/Systems/UnitAirMovementSystem.cs", "UnitAirComponent|RunwayTakeoffPosition|RunwayLandingPosition|MapSurfaceComponent", "Consumes typed runway endpoints and map-surface dimensions for aircraft movement."),
                new("Game.Runtime.UnitPathGridSnapshot", "Assets/Game/Scripts/Systems/UnitPathGridSnapshot.cs", "GridWalkable|GridRoad|DynamicBlocked|Occupied", "Copies walkability, road, blocker, and occupancy buffers for pathfinding jobs.")
            };

            var reports = new List<RuntimeConsumerReport>(specs.Length);
            foreach (ConsumerSpec spec in specs.OrderBy(entry => entry.type, StringComparer.Ordinal))
            {
                string source = File.ReadAllText(Path.Combine(projectRoot, spec.path));
                foreach (string token in spec.consumes.Split('|'))
                {
                    if (!source.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Runtime consumer contract token is absent: {spec.path}::{token}");
                }
                reports.Add(new RuntimeConsumerReport
                {
                    stableIdentity = spec.type + "@" + spec.path,
                    sourceRevision = BaselineCommit,
                    sourcePath = spec.path,
                    exactType = spec.type,
                    consumes = spec.consumes,
                    responsibility = spec.responsibility
                });
            }
            return reports;
        }

        private static List<CrossReferenceReport> BuildCrossReferences(string projectRoot)
        {
            return CrossReferenceSpecs().Select(spec => new CrossReferenceReport
            {
                task = spec.task,
                path = spec.path,
                reportSchema = spec.schema,
                sourceRevision = spec.revision,
                evidenceSha256 = OperationMapPhase0BaselineProbe.ComputeSha256(
                    File.ReadAllBytes(Path.Combine(projectRoot, spec.path)))
            }).OrderBy(entry => entry.task, StringComparer.Ordinal).ToList();
        }

        private static CrossReferenceSpec[] CrossReferenceSpecs()
        {
            return new[]
            {
                new CrossReferenceSpec("opmap-002", "Design/AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md", "warline.operation-map.phase0-baseline", "996e460029730a69832bc8df81255a1892f1bca9", "d4d4674850766c5cd95e1bb5fbb6f26893e0bb019dbaf266a0c9897a3befc807"),
                new CrossReferenceSpec("opmap-004", "Design/AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.json", "warline.operation-map.phase0-ownership", "2069aa01f66040f34fa0fb48ea1d8fec41691bab", "e1080bd90e88140d8151755b7ef6086c02d8683b7d277708004797893fc3c49b"),
                new CrossReferenceSpec("opmap-006", "Design/AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.json", "warline.operation-map.phase0-placement-ownership", "47c84afc5f873dbf2ea665ab4875d0825b51efd8", "115270bdb5844b5df504f33b5796caa4c85c49e82f02d23ea05e5ce732d0f759")
            };
        }

        private static List<InputHashReport> HashDirectInputs(string projectRoot)
        {
            List<InputHashReport> reports = HashFiles(projectRoot, ExpectedDirectInputHashes().Keys);
            if (!HasExpectedInputHashes(reports))
                throw new InvalidOperationException("Navigation metadata direct input drifted from the pinned baseline.");
            return reports;
        }

        private static Dictionary<string, string> ExpectedDirectInputHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Airport_Config.asset"] = "5d300773279e675a8a01fef21854ecc1b2f8708756ba2577395afb4f6941e333",
                ["Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Helipad_Config.asset"] = "3689f7caf4b8c7bdaa18ed44b272d6663d3056a31ca205aedc124c4f085eea52",
                [GridAssetPath] = "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb",
                [SurfaceAssetPath] = "aa08cb9115e8727bfdbc671a4a2cfd9334ef48134c00d58d7d29e350c45b752c",
                [AirportPrefabPath] = "1e2456eafce50f155b020b996c7a4be28646284344a3159dccecfe54bb4751cf",
                [HelipadPrefabPath] = "0a182d98bc33d6a4ce4b55c83252f4b974dadfb3286eccdf21fdd8d7d107da43",
                [MatchScenePath] = "dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46",
                [MatchSubScenePath] = "bcc255f3fb140a0d91687b45b679b47fb60f01f5cfa8690bac3032ec642dadd8",
                ["Assets/Game/Scripts/Authorings/GridAuthoring.cs"] = "5ac5169f0351d57ed44c89716614f177ac829d597f34780225da06eb0f4da348",
                ["Assets/Game/Scripts/Authorings/MapSurfaceAuthoring.cs"] = "0b2f878ee56e78702d9c1f7e648a5f568caccc6c425795c6f05b665297a08789",
                ["Assets/Game/Scripts/Components/GridComponents.cs"] = "632d66e1479fa0b0773ea1635c29a26c5efadfcc998caf5151980d4d5e20cd39",
                ["Assets/Game/Scripts/Components/MapSurfaceComponents.cs"] = "f11cb3fa488153f20de6bf7ea4b5f1399266d38fa34dc737dc60ec6173d90036",
                ["Assets/Game/Scripts/Composition/MapSurfaceRuntimeBootstrapSceneSystemHelper.cs"] = "3b070dee815412915960d9b4ef84bd51cb5d89603850f09401fdd31f958b84db",
                ["Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs"] = "e0af3018b60ce198ad51f8a497123bcdb92c530cef04a1bb14f813da5a49e5dd",
                ["Assets/Game/Scripts/Systems/BuildingRuntimeSurfaceOverlaySystem.cs"] = "35ecb50d19a3291336e4c10032a37ffd1fc31957e87d6beaaf1e54bf2f3ad9d0",
                ["Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs"] = "8f81e5a883e478d8ace1aa96301340526289fd6b193f0db250663e00c2f31896",
                ["Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs"] = "f4b59b87e76270f9c0f750dcb3a976b9270c5d3fe6013bd13eebf05cd6d798d7",
                ["Assets/Game/Scripts/Systems/DynamicBlockerInitSystem.cs"] = "5113911427664a72a5c0c7736aa633a6e8c4439e92fda9d14e7798230974fb7d",
                ["Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs"] = "4ab5a347080e10c9168d21cd7be512ccb995ca056d93c4a91ffe0b751d3a0ab5",
                ["Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs"] = "76b91da24026174d7ee13fd5691dfd6f75192b779e2805e212d3949748657aca",
                ["Assets/Game/Scripts/Systems/MapSurfaceDiagnosticsSystem.cs"] = "e7c7ddfb49f6ed1c5a8599d0fbb96a631082addf981c6f72fb6812749f0135af",
                ["Assets/Game/Scripts/Systems/MapSurfacePathfindingSnapshot.cs"] = "8779412fa70d6f4551a291f8086352f6c171c65f49eabdacdac1f014a4b125d6",
                ["Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs"] = "002fe9d4f81ea7d765f750c9927264cc3745a83a72385b57ab100208b862c0ed",
                ["Assets/Game/Scripts/Systems/UnitAirMovementSystem.cs"] = "d051537735be1409b25b000ec15298179aaa01798453ad3ff326fa23204ffbb8",
                ["Assets/Game/Scripts/Systems/UnitPathGridSnapshot.cs"] = "53d06bc98e48817079dd31ba1b341209e9509ce31d23b72dea972519b56ac9cb",
                ["Design/AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md"] = "d4d4674850766c5cd95e1bb5fbb6f26893e0bb019dbaf266a0c9897a3befc807",
                ["Design/AgentReports/2026-07-15_opmap-004_phase0_ownership_baseline.json"] = "e1080bd90e88140d8151755b7ef6086c02d8683b7d277708004797893fc3c49b",
                ["Design/AgentReports/2026-07-15_opmap-006_phase0_placement_ownership.json"] = "115270bdb5844b5df504f33b5796caa4c85c49e82f02d23ea05e5ce732d0f759"
            };
        }

        private static bool HasExpectedInputHashes(IReadOnlyList<InputHashReport> hashes)
        {
            Dictionary<string, string> expected = ExpectedDirectInputHashes();
            return hashes != null && hashes.Count == expected.Count &&
                   IsStrictlyOrdered(hashes.Select(entry => entry.path)) &&
                   hashes.All(entry => entry != null && expected.TryGetValue(entry.path, out string hash) &&
                                       string.Equals(entry.sha256, hash, StringComparison.Ordinal));
        }

        private static bool HasExpectedCrossReferences(IReadOnlyList<CrossReferenceReport> references)
        {
            CrossReferenceSpec[] expected = CrossReferenceSpecs();
            return references != null && references.Count == expected.Length &&
                   IsStrictlyOrdered(references.Select(entry => entry.task)) &&
                   references.Zip(expected, (actual, spec) => actual != null &&
                       actual.task == spec.task && actual.path == spec.path && actual.reportSchema == spec.schema &&
                       actual.sourceRevision == spec.revision && actual.evidenceSha256 == spec.sha256).All(value => value);
        }

        private static bool HasExpectedAuthorities(IReadOnlyList<AuthorityReport> authorities)
        {
            if (authorities == null || authorities.Count != 15 ||
                !IsStrictlyOrdered(authorities.Select(entry => entry.stableIdentity)))
                return false;
            return authorities.All(entry => entry != null && !string.IsNullOrWhiteSpace(entry.kind) &&
                !string.IsNullOrWhiteSpace(entry.stableIdentity) && entry.sourceRevision == BaselineCommit &&
                !string.IsNullOrWhiteSpace(entry.exactType) && !string.IsNullOrWhiteSpace(entry.metadata) &&
                entry.currentOwner == CurrentOwner && entry.targetOwner == TargetOwner &&
                entry.migrationDisposition == MigrationDisposition && entry.migrationOwner == MigrationOwner &&
                entry.state == DecisionState && entry.decisionOwner == DecisionOwner &&
                (string.IsNullOrEmpty(entry.assetGuid) || entry.assetGuid.Length == 32));
        }

        private static bool HasExpectedRuntimeConsumers(IReadOnlyList<RuntimeConsumerReport> consumers)
        {
            return consumers != null && consumers.Count == 13 &&
                   IsStrictlyOrdered(consumers.Select(entry => entry.stableIdentity)) &&
                   consumers.All(entry => entry != null && entry.sourceRevision == BaselineCommit &&
                       entry.stableIdentity == entry.exactType + "@" + entry.sourcePath &&
                       !string.IsNullOrWhiteSpace(entry.consumes) && !string.IsNullOrWhiteSpace(entry.responsibility));
        }

        private static bool HasExactJsonSchema(string json)
        {
            try
            {
                JObject root = JObject.Parse(json);
                if (!Exact(root, "reportSchema", "reportSchemaVersion", "baselineCommit", "result", "counts",
                        "identityPayloadSha256", "acceptedEvidence", "directInputHashes", "authorities", "runtimeConsumers") ||
                    !Exact((JObject)root["counts"], "authorities", "runtimeConsumers", "acceptedCrossReferences", "directInputs", "needsDecision"))
                    return false;
                if (!ExactArray(root["acceptedEvidence"], "task", "path", "reportSchema", "sourceRevision", "evidenceSha256") ||
                    !ExactArray(root["directInputHashes"], "path", "sha256") ||
                    !ExactArray(root["authorities"], "kind", "stableIdentity", "sourceRevision", "assetPath", "assetGuid", "localId", "exactType", "scenePath", "hierarchyPath", "metadata", "currentOwner", "targetOwner", "migrationDisposition", "migrationOwner", "state", "decisionOwner") ||
                    !ExactArray(root["runtimeConsumers"], "stableIdentity", "sourceRevision", "sourcePath", "exactType", "consumes", "responsibility"))
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ExactArray(JToken token, params string[] properties)
        {
            return token is JArray array && array.All(entry => entry is JObject item && Exact(item, properties));
        }

        private static bool Exact(JObject value, params string[] properties)
        {
            if (value == null)
                return false;
            string[] actual = value.Properties().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] expected = properties.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            return actual.SequenceEqual(expected, StringComparer.Ordinal);
        }

        private static SurfaceCounts InspectSurfaceCounts(MapSurfaceDataAsset surface)
        {
            if (!surface.TryCreateRuntimeBlobAsset(Allocator.Persistent, out BlobAssetReference<MapSurfaceBlob> blob))
                throw new InvalidOperationException("Baked map surface payload could not be decoded.");
            try
            {
                SurfaceCounts counts = default;
                ref MapSurfaceBlob value = ref blob.Value;
                int count = MapSurfaceBlobAccess.SurfaceCount(ref value);
                for (int i = 0; i < count; i++)
                {
                    if (!MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref value, i, out MapSurfaceSample sample))
                        throw new InvalidOperationException($"Map surface sample {i} could not be read.");
                    if (sample.SurfaceType == MapSurfaceType.Terrain) counts.terrain++;
                    if ((sample.Flags & MapSurfaceFlags.Road) != 0) counts.road++;
                    if ((sample.Flags & MapSurfaceFlags.Bridge) != 0) counts.bridge++;
                    if (sample.MovementMask == MapSurfaceMovementMask.None) counts.blocked++;
                }
                if (counts.terrain + (count - counts.terrain) != surface.SurfaceCount)
                    throw new InvalidOperationException("Map surface count drifted during inspection.");
                return counts;
            }
            finally
            {
                blob.Dispose();
            }
        }

        private static void RequireGridParity(MapSurfaceDataAsset surface, GridAuthoringConfig grid)
        {
            if (surface.Dimensions.x != grid.Width || surface.Dimensions.y != grid.Height ||
                !Mathf.Approximately(surface.CellSize, grid.CellSize) ||
                (surface.GridOrigin - grid.Origin).sqrMagnitude > 0.00000001f)
                throw new InvalidOperationException("Map surface and grid authority metadata diverged.");
        }

        private static T RequireAsset<T>(string path, string guid) where T : UnityEngine.Object
        {
            if (!string.Equals(AssetDatabase.AssetPathToGUID(path), guid, StringComparison.Ordinal))
                throw new InvalidOperationException($"Asset GUID drifted: {path}");
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required asset is absent or has the wrong type: {path}");
            AssetIdentity identity = RequireAssetIdentity(asset);
            if (identity.guid != guid || (asset is not GameObject && identity.localId != MainAssetLocalId))
                throw new InvalidOperationException($"Asset identity drifted: {path}");
            return asset;
        }

        private static AssetIdentity RequireAssetIdentity(UnityEngine.Object value)
        {
            if (value == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId))
                throw new InvalidOperationException("Unity object lacks a stable asset identity.");
            return new AssetIdentity(guid, localId);
        }

        private static T RequireSingleComponent<T>(Scene scene) where T : Component
        {
            T[] components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            if (components.Length != 1)
                throw new InvalidOperationException($"Expected exactly one {typeof(T).FullName} in {scene.path}, found {components.Length}.");
            return components[0];
        }

        private static void RequireObjectIdentity(UnityEngine.Object value, string expectedGuid, long expectedLocalId)
        {
            GlobalObjectId identity = GlobalObjectId.GetGlobalObjectIdSlow(value);
            if (!string.Equals(identity.assetGUID.ToString(), expectedGuid, StringComparison.Ordinal) ||
                identity.targetObjectId != checked((ulong)expectedLocalId))
                throw new InvalidOperationException($"Stable object identity drifted for {value.GetType().FullName}.");
        }

        private static Transform RequireTransformIdentity(GameObject prefab, long localId, string expectedName)
        {
            Transform[] matches = prefab.GetComponentsInChildren<Transform>(true).Where(candidate =>
            {
                AssetIdentity identity = RequireAssetIdentity(candidate);
                return identity.localId == localId;
            }).ToArray();
            if (matches.Length != 1 || !string.Equals(matches[0].name, expectedName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Prefab transform identity drifted: {localId}/{expectedName}");
            return matches[0];
        }

        private static string HierarchyPath(Transform transform)
        {
            var parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                parts.Push(current.name);
            return string.Join("/", parts);
        }

        private static void RequireCleanLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isDirty)
                    throw new InvalidOperationException($"Refusing to inspect with a dirty loaded scene: {scene.path}");
            }
        }

        private static void RestoreSceneSetup(SceneSetup[] setup)
        {
            if (setup != null && setup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void RequireInputHashesEqual(IReadOnlyList<InputHashReport> before, IReadOnlyList<InputHashReport> after)
        {
            if (before == null || after == null || before.Count != after.Count)
                throw new InvalidOperationException("Navigation metadata direct input set changed during inspection.");
            for (int i = 0; i < before.Count; i++)
            {
                if (before[i].path != after[i].path || before[i].sha256 != after[i].sha256)
                    throw new InvalidOperationException($"Navigation metadata direct input changed during inspection: {before[i].path}");
            }
        }

        private static string RequireProjectRoot()
        {
            DirectoryInfo parent = Directory.GetParent(Application.dataPath);
            return parent?.FullName ?? throw new InvalidOperationException("Unity project root is unavailable.");
        }

        private static void RequireSymlinkSafePath(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string current = Path.GetPathRoot(fullPath);
            foreach (string segment in fullPath.Substring(current.Length).Split(Path.DirectorySeparatorChar))
            {
                if (string.IsNullOrEmpty(segment)) continue;
                current = Path.Combine(current, segment);
                if ((Directory.Exists(current) || File.Exists(current)) && IsSymlink(current))
                    throw new InvalidOperationException($"Refusing symlinked evidence path: {current}");
            }
        }

        private static bool IsSymlink(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

        private static void RequireRegularFile(string path)
        {
            if (!File.Exists(path) || Directory.Exists(path) || IsSymlink(path))
                throw new InvalidOperationException($"Evidence path is not a regular file: {path}");
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static bool IsStrictlyOrdered(IEnumerable<string> values)
        {
            string previous = null;
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    (previous != null && string.CompareOrdinal(previous, value) >= 0))
                    return false;
                previous = value;
            }
            return true;
        }

        private static void Append(StringBuilder builder, params string[] values)
        {
            foreach (string value in values)
                builder.Append(value ?? string.Empty).Append('\0');
            builder.Append('\n');
        }

        private static string F(float value) => value == 0f ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
        private static string V(Vector3 value) => $"({F(value.x)},{F(value.y)},{F(value.z)})";
        private static string Q(Quaternion value) => $"({F(value.x)},{F(value.y)},{F(value.z)},{F(value.w)})";

        [Serializable]
        internal sealed class NavigationMetadataOwnershipReport
        {
            public string reportSchema;
            public int reportSchemaVersion;
            public string baselineCommit;
            public string result;
            public ReportCounts counts;
            public string identityPayloadSha256;
            public List<CrossReferenceReport> acceptedEvidence;
            public List<InputHashReport> directInputHashes;
            public List<AuthorityReport> authorities;
            public List<RuntimeConsumerReport> runtimeConsumers;
        }

        [Serializable]
        internal sealed class ReportCounts
        {
            public int authorities;
            public int runtimeConsumers;
            public int acceptedCrossReferences;
            public int directInputs;
            public int needsDecision;
        }

        [Serializable]
        internal sealed class CrossReferenceReport
        {
            public string task;
            public string path;
            public string reportSchema;
            public string sourceRevision;
            public string evidenceSha256;
        }

        [Serializable]
        internal sealed class InputHashReport
        {
            public string path;
            public string sha256;
        }

        [Serializable]
        internal sealed class AuthorityReport
        {
            public string kind;
            public string stableIdentity;
            public string sourceRevision;
            public string assetPath;
            public string assetGuid;
            public long localId;
            public string exactType;
            public string scenePath;
            public string hierarchyPath;
            public string metadata;
            public string currentOwner;
            public string targetOwner;
            public string migrationDisposition;
            public string migrationOwner;
            public string state;
            public string decisionOwner;
        }

        [Serializable]
        internal sealed class RuntimeConsumerReport
        {
            public string stableIdentity;
            public string sourceRevision;
            public string sourcePath;
            public string exactType;
            public string consumes;
            public string responsibility;
        }

        private readonly struct ConsumerSpec
        {
            public readonly string type;
            public readonly string path;
            public readonly string consumes;
            public readonly string responsibility;
            public ConsumerSpec(string type, string path, string consumes, string responsibility)
            {
                this.type = type; this.path = path; this.consumes = consumes; this.responsibility = responsibility;
            }
        }

        private readonly struct CrossReferenceSpec
        {
            public readonly string task;
            public readonly string path;
            public readonly string schema;
            public readonly string revision;
            public readonly string sha256;
            public CrossReferenceSpec(string task, string path, string schema, string revision, string sha256)
            {
                this.task = task; this.path = path; this.schema = schema; this.revision = revision; this.sha256 = sha256;
            }
        }

        private readonly struct AssetIdentity
        {
            public readonly string guid;
            public readonly long localId;
            public AssetIdentity(string guid, long localId) { this.guid = guid; this.localId = localId; }
        }

        private struct SurfaceCounts
        {
            public int terrain;
            public int road;
            public int bridge;
            public int blocked;
        }
    }
}
#endif
