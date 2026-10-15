#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Editor
{
    // Source-fit measurement only. No authored assets, gameplay systems, orders, stocks,
    // readiness, publication, physical source geometry or default World are modified.
    public static partial class SkirmishGroundSourceFitSurvey
    {
        const string Inventory = "Design/AgentReports/SkirmishExpansion/GroundPilots/six-ba-ground-pilot-inputs-20261014.json";
        const string Support = "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity";
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            string output = Environment.GetEnvironmentVariable("WARLINE_GROUND_SOURCE_SURVEY_OUTPUT");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string allowed = Path.Combine(root, "Logs", "SkirmishProduction") + Path.DirectorySeparatorChar;
            if (string.IsNullOrEmpty(output) || !Path.IsPathRooted(output) || !Path.GetFullPath(output).StartsWith(allowed, StringComparison.OrdinalIgnoreCase) || File.Exists(output))
                throw new InvalidOperationException("Fresh absolute Logs/SkirmishProduction output required");
            RunPilotLogicalAuthoringControls();
            RunPilotCompoundReservationControls();
            var input = JObject.Parse(File.ReadAllText(Inventory));
            Require((int?)input["schema"] == 1 && Hash((string)input["budgetAuthority"]) == (string)input["matrixAssetSha256"], "Pinned matrix changed");
            var report = new JObject { ["schema"] = 1, ["scope"] = "ReadOnlyPinnedSourceFitSurvey", ["inventorySha256"] = Hash(Inventory),
                ["runtimeFitCertified"] = false, ["paidAircraftLifecycleCertified"] = false, ["playerReady"] = false };
            var sources = new JArray(); report["sources"] = sources;
            report["actualBakedPrefabHierarchies"] = RegistryHierarchies();
            int errors = 0;
            foreach (var group in new[] { "CC", "AP", "MP", "IB" })
            {
                var row = new JObject { ["group"] = group }; sources.Add(row);
                try { SurveyGroup(input, group, row); }
                catch (Exception error) { row["error"] = error.ToString(); row["collectionCompleted"] = false; errors++; }
            }
            report["collectionErrors"] = errors;
            report["result"] = errors == 0 ? "Collected" : "Failed";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false))) writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented));
            if (errors != 0) throw new InvalidOperationException("Source survey preserved errors: " + output);
            Debug.Log("[SkirmishGroundSourceFitSurvey] result=Collected sources=4 runtimeFitCertified=0 paidAircraftLifecycleCertified=0 output=" + output);
        }

        public static void RunRegistryHierarchyAudit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            string output = Environment.GetEnvironmentVariable("WARLINE_GROUND_SOURCE_SURVEY_OUTPUT");
            string allowed = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
            Require(!string.IsNullOrEmpty(output) && Path.IsPathRooted(output) && Path.GetFullPath(output).StartsWith(allowed, StringComparison.OrdinalIgnoreCase) && !File.Exists(output), "Fresh absolute persistent hierarchy output required");
            var rows = RegistryHierarchies();
            var report = new JObject { ["schema"] = 1, ["scope"] = "ActualBakedPrefabHierarchyOnly", ["prefabs"] = rows,
                ["prefabSourceScene"] = Support, ["prefabSourceSceneSha256"] = Hash(Support), ["departureCertified"] = false,
                ["actorsInstantiated"] = false, ["actorsDestroyed"] = false, ["runtimeOwnershipAdded"] = false,
                ["note"] = "Source Prefab tags are expected; they are not interpreted as runtime departure failures." };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(file, new System.Text.UTF8Encoding(false))) writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented));
            Require(rows.Count == 2 && rows.All(r => (bool?)r["rootFound"] == true), "Actual required Rifle/TruckTray source prefab missing; report preserved");
            Debug.Log("[SkirmishNativePrefabHierarchyAudit] result=Collected prefabs=2 departureCertified=0 output=" + output);
        }

        static JArray RegistryHierarchies()
        {
            using var world = StreamingWorld(); Load(world, AssetDatabase.AssetPathToGUID(Support));
            var em = world.EntityManager; var output = new JArray();
            foreach (var role in new[] { SkirmishRoleKind.Rifle, SkirmishRoleKind.LogisticsTruck })
            {
                string key = SkirmishRoleCatalogConfig.RuntimePrefabKey(role);
                var root = FindNativePrefab(em, key); var nodes = new JArray(); var edges = new JArray();
                var record = new JObject { ["role"] = role.ToString(), ["expectedKey"] = key, ["rootFound"] = root != Entity.Null,
                    ["scope"] = "SourceTemplateHierarchy_NotRuntimeDeparture", ["nodes"] = nodes, ["edges"] = edges };
                output.Add(record); if (root == Entity.Null) continue;
                record["rootIndex"] = root.Index; record["rootVersion"] = root.Version;
                var visited = new HashSet<Entity>(); var queue = new Queue<Entity>(); queue.Enqueue(root);
                int childHealth = 0, childOwnership = 0, missingNodes = 0;
                while (queue.Count > 0)
                {
                    var entity = queue.Dequeue(); if (!visited.Add(entity)) continue;
                    Require(visited.Count <= 4096, "Bounded native prefab hierarchy exceeded 4096 entities");
                    var node = new JObject { ["index"] = entity.Index, ["version"] = entity.Version, ["exists"] = em.Exists(entity), ["isRoot"] = entity == root }; nodes.Add(node);
                    if (!em.Exists(entity)) { missingNodes++; continue; }
                    using var types = em.GetComponentTypes(entity);
                    node["components"] = new JArray(types.Select(t => t.GetManagedType().FullName).OrderBy(n => n, StringComparer.Ordinal));
                    node["name"] = em.GetName(entity); node["prefab"] = em.HasComponent<Prefab>(entity); node["disabled"] = em.HasComponent<Disabled>(entity);
                    node["hasHealth"] = em.HasComponent<UnitHealth>(entity); node["hasAttemptOwner"] = em.HasComponent<SkirmishAttemptOwnedComponent>(entity);
                    node["sharedActor"] = em.HasComponent<SkirmishSharedActorTag>(entity);
                    node["passenger"] = em.HasComponent<UnitTransportPassenger>(entity); node["cargoPassenger"] = em.HasComponent<UnitTransportCargoPassenger>(entity);
                    if (em.HasComponent<UnitSourcePrefabKey>(entity)) node["sourceKey"] = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                    if (em.HasComponent<UnitHealth>(entity))
                    { var health = em.GetComponentData<UnitHealth>(entity); node["healthCurrent"] = health.Current; node["healthMax"] = health.Max; if (entity != root) childHealth++; }
                    if (em.HasComponent<SkirmishAttemptOwnedComponent>(entity))
                    {
                        var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                        node["attempt"] = owner.SessionId.ToString(); node["stableId"] = owner.StableObjectId.ToString(); node["faction"] = owner.FactionId;
                        node["reservation"] = owner.ReservationId; node["capacityReleased"] = owner.CapacityReleased; node["isStructure"] = owner.IsStructure;
                        if (entity != root) childOwnership++;
                    }
                    if (em.HasComponent<SkirmishObjectiveRoleComponent>(entity))
                    { var identity = em.GetComponentData<SkirmishObjectiveRoleComponent>(entity); node["objectiveStableId"] = identity.StableObjectId.ToString(); node["objectiveFaction"] = identity.FactionId; node["objectiveRole"] = identity.Role.ToString(); }
                    if (em.HasComponent<Parent>(entity))
                    { var parent = em.GetComponentData<Parent>(entity).Value; node["parentIndex"] = parent.Index; node["parentVersion"] = parent.Version; }
                    if (em.HasBuffer<Child>(entity)) foreach (var child in em.GetBuffer<Child>(entity, true))
                    {
                        bool reciprocal = em.Exists(child.Value) && em.HasComponent<Parent>(child.Value) && em.GetComponentData<Parent>(child.Value).Value == entity;
                        edges.Add(new JObject { ["kind"] = "Child", ["fromIndex"] = entity.Index, ["fromVersion"] = entity.Version,
                            ["toIndex"] = child.Value.Index, ["toVersion"] = child.Value.Version, ["reciprocalParent"] = reciprocal }); queue.Enqueue(child.Value);
                    }
                    if (em.HasBuffer<LinkedEntityGroup>(entity)) foreach (var linked in em.GetBuffer<LinkedEntityGroup>(entity, true))
                    {
                        edges.Add(new JObject { ["kind"] = "LinkedEntityGroup", ["fromIndex"] = entity.Index, ["fromVersion"] = entity.Version,
                            ["toIndex"] = linked.Value.Index, ["toVersion"] = linked.Value.Version }); queue.Enqueue(linked.Value);
                    }
                }
                record["nodeCount"] = visited.Count; record["nonRootHealthCount"] = childHealth;
                record["nonRootAttemptOwnerCount"] = childOwnership; record["missingNodeCount"] = missingNodes;
                record["blanketNonRootHealthGuardPotentialConflict"] = childHealth > 0;
                record["runtimeDepartureCompatibility"] = "NotEstablishedBySourceTemplateAudit";
            }
            return output;
        }

        // The CC source publishes its serialized surface through this exact
        // production owner at runtime rather than baking a MapSurfaceComponent.
        // Only this disposable survey world is changed. No flat/clear substitute.
        static IDisposable PublishMissingPinnedSurface(World world, OperationMapDefinition source, string assetPath, JObject row)
        {
            using var surfaces=world.EntityManager.CreateEntityQuery(typeof(MapSurfaceComponent));
            row["surfacePublicationScope"]="ExistingBakedSurface";
            if(surfaces.CalculateEntityCount()!=0)return null;
            var data=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(assetPath);
            Require(data!=null&&!EditorUtility.IsDirty(data),"Clean exact source surface asset required");
            var expected=source.SurfaceMetadata;var grid=source.GridMetadata;
            Require(string.Equals(data.ComputeRuntimeBlobHash().ToString(),expected.RuntimeBlobHash,StringComparison.OrdinalIgnoreCase)&&
                data.SurfaceCount==expected.SurfaceCount&&data.PayloadVersion==expected.PayloadVersion&&data.PayloadEncoding==expected.PayloadEncoding&&
                data.Dimensions.x==grid.Dimensions.x&&data.Dimensions.y==grid.Dimensions.y&&
                data.CellSize==grid.CellSize&&math.all((float3)data.GridOrigin==(float3)grid.Origin),"Serialized source surface metadata mismatch");
            var type=typeof(Game.Composition.SkirmishLaunchProjection).Assembly.GetType("Game.Composition.MapSurfaceRuntimeBootstrapSceneSystemHelper",true);
            var owner=(IDisposable)Activator.CreateInstance(type,new object[]{world});
            try
            {
                var method=type.GetMethod("Ensure",BindingFlags.Instance|BindingFlags.Public,null,
                    new[]{typeof(MapSurfaceDataAsset),typeof(string).MakeByRefType()},null);
                Require(method!=null,"Actual runtime surface publication seam missing");
                object[] arguments={data,null};
                Require((bool)method.Invoke(owner,arguments),"Actual runtime source surface publication failed: "+arguments[1]);
                Require(surfaces.CalculateEntityCount()==1,"Exact one actual decoded surface required");
                row["surfacePublicationScope"]="ExactRuntimeOwnerDecodedPinnedAsset_IsolatedWorldOnly";
                row["runtimeDecodedSurfaceAssetSha256"]=Hash(assetPath);
                row["runtimeDecodedSurfaceBlobHash"]=data.ComputeRuntimeBlobHash().ToString();
                row["runtimeDecodedSurfaceOwner"]=type.FullName;
                return owner;
            }
            catch { owner.Dispose();throw; }
        }

        static void SurveyGroup(JObject input, string group, JObject row)
        {
            var pin = (JObject)input["sources"][group];
            string path = (string)pin["path"];
            Require(Hash(path) == (string)pin["assetSha256"], "Current source bytes differ: " + group);
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
            Require(source != null && !EditorUtility.IsDirty(source), "Clean persistent Campaign source required");
            Require(source.ContentHash == (string)pin["contentHash"] && source.SourceIdentityHash == (string)pin["sourceIdentityHash"], "Source identity differs");
            row["sourcePath"] = path; row["sourceAssetSha256"] = Hash(path);
            row["operationMapId"] = source.OperationMapId; row["sourceIdentityHash"] = source.SourceIdentityHash;
            row["contentHash"] = source.ContentHash; row["serializedMetadata"] = EditorJsonUtility.ToJson(source);
            string surfacePath = AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID);
            string gridPath = AssetDatabase.GUIDToAssetPath(source.GridMetadata.AssetGuid);
            row["surfacePath"] = surfacePath; row["surfaceAssetSha256"] = Hash(surfacePath);
            row["gridPath"] = gridPath; row["gridAssetSha256"] = Hash(gridPath);
            row["sourceSceneGuid"] = source.SourceSceneReference.AssetGUID;
            row["authoredSubSceneGuid"] = source.NavigationMetadata.AuthoredSubSceneGuid;
            row["surfaceHeightMin"] = source.SurfaceMetadata.MinimumHeight;
            row["surfaceHeightMax"] = source.SurfaceMetadata.MaximumHeight;
            var requests = ((JArray)input["requests"]).OfType<JObject>().Where(r => (string)r["mapGroup"] == group).ToArray();
            foreach (var request in requests) Require(Hash((string)request["packetPath"]) == (string)request["packetSha256"], "Packet changed");
            row["canonicalRequests"] = new JArray(requests.Select(r => new JObject { ["catalogId"] = r["catalogId"].DeepClone(), ["rows"] = r["canonicalSetupRows"].DeepClone() }));
            using var candidateOwner = new PilotCandidateOwner();
            SkirmishMapLayoutConfig layout;
            if (!(input["layoutSnapshots"]?[group] is JObject snapshot))
                layout = CreatePilotLogicalCandidate(source, group, row, candidateOwner);
            else
            {
                string layoutPath = (string)snapshot["templatePath"];
                Require(Hash(layoutPath) == (string)snapshot["templateAssetSha256"], "Existing projected template changed");
                Require(Hash((string)snapshot["candidateDraftPath"]) == (string)snapshot["candidateDraftSha256"], "Logical candidate bytes changed");
                layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(layoutPath);
                Require(layout != null && !EditorUtility.IsDirty(layout), "Existing clean projected template required");
                row["layoutTemplatePath"] = layoutPath; row["layoutTemplateSha256"] = Hash(layoutPath);
                row["candidateDraftSha256"] = snapshot["candidateDraftSha256"].DeepClone();
                row["layoutStatus"] = "UnmeasuredReuseCandidate";
                row["projectedAnchors"] = snapshot["anchors"].DeepClone(); row["projectedPads"] = snapshot["pads"].DeepClone();
            }
            using var world = StreamingWorld();
            Load(world, source.NavigationMetadata.AuthoredSubSceneGuid);
            Load(world, AssetDatabase.AssetPathToGUID(Support));
            using var runtimeSurface = PublishMissingPinnedSurface(world, source, surfacePath, row);
            var em = world.EntityManager;
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(GridRoad));
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            row["bakedGridCount"] = grids.CalculateEntityCount(); row["bakedSurfaceCount"] = surfaces.CalculateEntityCount();
            row["liveOccupancyStatus"] = "NotMeasured_NoGameplaySystemsRun";
            row["paidHeliReconStatus"] = "PostMaterializationNormalPaidProductionFlightReturnPending";
            if (grids.CalculateEntityCount() != 1 || surfaces.CalculateEntityCount() != 1)
            {
                row["nativeQueryStatus"] = "UnavailableBakedGridOrSurface_NotSubstitutedWithClearRuntimeState";
                MeasureDecodedSurface(source, layout, row);
                row["collectionCompleted"] = true;
                return;
            }
            var gridEntity = grids.GetSingletonEntity(); var grid = grids.GetSingleton<GridConfig>();
            var surface = surfaces.GetSingleton<MapSurfaceComponent>();
            var expectedGrid=source.GridMetadata;
            Require(grid.Width==expectedGrid.Dimensions.x&&grid.Height==expectedGrid.Dimensions.y&&
                grid.CellSize==expectedGrid.CellSize&&math.all(grid.Origin==(float3)expectedGrid.Origin),
                "Actual baked native grid differs from pinned source metadata");
            Require(surface.Dimensions.x==grid.Width&&surface.Dimensions.y==grid.Height&&
                surface.CellSize==grid.CellSize&&math.all(surface.GridOrigin==grid.Origin),
                "Actual native grid and surface dimensions/origin differ");
            Require(surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated, "Native baked surface unavailable");
            row["nativeQueryStatus"] = "BakedGridSurfaceQueries_NoRuntimeOccupancyCertificate";
            var roadCache = new BuildingPlacementAuthoredRoadCache(); roadCache.Ensure(em, surfaces, grid);
            var roadMask = new bool[grid.Width * grid.Height]; roadCache.AppendTo(roadMask);
            var sidewalkMask = roadCache.GetSidewalks(); var waterMask = roadCache.GetWater();
            var facilities = new JArray(); row["allSizeFacilities"] = facilities;
            var catalog = SkirmishExpansionCatalogConfig.Load(); Require(catalog != null, "Actual profile catalog missing");
            var profiles = catalog.ToAuthoredSet();
            Require(SkirmishSetupMatrixTable.TryLoad(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), out var matrix, out string error), error);
            var buildStructures = typeof(SkirmishSetupCompiler).GetMethod("BuildStructures", BindingFlags.Static | BindingFlags.NonPublic);
            Require(buildStructures != null, "Exact pure structure enumeration seam missing");
            foreach (var request in requests)
            {
                var definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
                try
                {
                    definition.Configure((string)request["catalogId"], profiles.ObjectiveBa, profiles.ArmyGround,
                        (string)request["start"] == "F" ? profiles.StartField : profiles.StartEstablished,
                        profiles.Economy, profiles.Upgrades, profiles.Intel, profiles.Roles, layout);
                    foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
                    {
                        Require(SkirmishSetupMatrixTable.TryFind(matrix, definition.CatalogId, size, out var vector), "Canonical size row missing");
                        // Pure planning enumeration only; not TryCompile and no measured/ready flags changed.
                        var structures = (SkirmishResolvedStructureEntry[])buildStructures.Invoke(null, new object[] { definition, vector, false });
                        foreach (var structure in structures)
                        {
                            var result = new JObject { ["catalogId"] = definition.CatalogId, ["size"] = size.ToString(), ["faction"] = structure.FactionId, ["structureId"] = structure.StructureId };
                            facilities.Add(result);
                            if (!Game.Configs.SkirmishMapLayoutBuilder.TryPadForStructure(layout, structure, out var pad))
                            { result["status"] = "MissingTypedPad"; continue; }
                            if (!layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out float x, out float z))
                            { result["status"] = "MissingPadProjection"; continue; }
                            Game.Configs.SkirmishMapLayoutBuilder.ApplyFrameOffset(layout, structure.AcrossOffsetMetres, structure.ForwardOffsetMetres, ref x, ref z);
                            result["x"] = x; result["z"] = z; result["padId"] = pad.PadId;
                            string key = SkirmishStructureIds.VisualKey(structure.StructureId);
                            var prefabs = AssetDatabase.FindAssets(key + " t:Prefab", new[] { "Assets/Game/Prefabs/Buildings", "Assets/Game/Prefabs/Skirmish" })
                                .Select(AssetDatabase.GUIDToAssetPath).Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(p => p != null && p.name == key).ToArray();
                            if (prefabs.Length != 1 || prefabs[0].GetComponent<BuildingDefinitionAuthoring>() == null)
                            { result["status"] = "ExactBuildingPrefabMissingOrAmbiguous"; continue; }
                            var prefab = prefabs[0]; var authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
                            var footprint = authoring.ConfiguredFootprintCells;
                            result["prefabPath"] = AssetDatabase.GetAssetPath(prefab); result["prefabSha256"] = Hash(AssetDatabase.GetAssetPath(prefab));
                            result["footprintSource"] = "SerializedConfiguredPrefab_NoApplyConfigCall";
                            var center = GridUtils.WorldToCell(grid, new float3(x, 0, z)); var origin = center - new int2(footprint.x, footprint.y) / 2;
                            result["originX"] = origin.x; result["originZ"] = origin.y; result["widthCells"] = footprint.x; result["depthCells"] = footprint.y;
                            result["insideGrid"] = BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(new Vector2Int(origin.x, origin.y), footprint, grid);
                            result["foundationLevel"] = BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, new RectInt(origin.x, origin.y, footprint.x, footprint.y));
                            int roadCells = 0, sidewalkCells = 0, waterCells = 0, blockedGridCells = 0;
                            var walkable = em.GetBuffer<GridWalkable>(gridEntity, true);
                            for (int zz = origin.y; zz < origin.y + footprint.y; zz++) for (int xx = origin.x; xx < origin.x + footprint.x; xx++)
                            {
                                if (!GridUtils.InBounds(new int2(xx, zz), grid.Width, grid.Height)) continue;
                                int index = zz * grid.Width + xx;
                                if (roadMask[index]) roadCells++; if (sidewalkMask != null && sidewalkMask[index]) sidewalkCells++;
                                if (waterMask != null && waterMask[index]) waterCells++; if (walkable[index].Value == 0) blockedGridCells++;
                            }
                            result["roadCells"] = roadCells; result["sidewalkCells"] = sidewalkCells; result["waterCells"] = waterCells; result["blockedGridCells"] = blockedGridCells;
                            result["packedSceneryOverlap"] = "PendingExactActualModelBoundsAndPackedGeometryJoin";
                            result["status"] = "PreferredFootprintMeasured_ActualPlacementMayRelocate";
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(definition); }
            }
            var routes = new JArray(); row["heavyRouteQueries"] = routes;
            foreach (var route in layout.Routes.Where(r => r.AllowsVehicles))
                foreach (var role in new[] { SkirmishRoleKind.Tank, SkirmishRoleKind.Siege, SkirmishRoleKind.ApcHeavy })
                {
                    var prefab = FindNativePrefab(em, SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                    for (int leg = 1; leg < route.WaypointAnchorIds.Length; leg++)
                    {
                        var result = new JObject { ["routeId"] = route.RouteId, ["role"] = role.ToString(), ["leg"] = leg,
                            ["scope"] = "ActualPathfindBatchJob_BakedGridSurfaceOnly", ["liveOccupancyCertified"] = false };
                        routes.Add(result);
                        if (prefab == Entity.Null || !em.HasComponent<UnitFootprint>(prefab) || !em.HasComponent<UnitMovementBehavior>(prefab))
                        { result["status"] = "ExactNativePrefabUnavailable"; continue; }
                        if (!layout.TryGetAnchorById(route.WaypointAnchorIds[leg - 1], out var a) || !layout.TryGetAnchorById(route.WaypointAnchorIds[leg], out var b)) throw new InvalidOperationException("Typed route anchor missing");
                        if (!layout.TryProjectLocalToMap(a.WorldX, a.WorldZ, out float ax, out float az) || !layout.TryProjectLocalToMap(b.WorldX, b.WorldZ, out float bx, out float bz)) throw new InvalidOperationException("Typed source route projection missing");
                        QueryPath(em, gridEntity, grid, surface, prefab, GridUtils.WorldToCell(grid, new float3(ax, 0, az)), GridUtils.WorldToCell(grid, new float3(bx, 0, bz)), result);
                    }
                }
            SurveyLogicalEndpointCandidates(em,gridEntity,grid,surface,layout,row);
            ProbePilotFailedRouteDetours(em, gridEntity, grid, surface, source, layout, row);
            row["allSizeForcePacking"] = SurveyForcePacking(input, requests, layout, profiles.Roles, em, gridEntity, grid, surface, facilities);
            ProbePilotServiceAccess(em, gridEntity, grid, surface, layout, row);
            RecordPilotPlacementGates(row);
            ProbePilotNormalFacilityRelocation(input,requests,layout,profiles.Roles,em,gridEntity,grid,surface,row);
            ProbePilotNormalTerrainFallback(input,requests,layout,profiles.Roles,em,gridEntity,grid,surface,facilities,row);
            CollectMaterializedPilotCandidates(input,requests,source,layout,profiles.Roles,em,gridEntity,grid,surface,row);
            row["collectionCompleted"] = true;
        }

        static JArray SurveyForcePacking(JObject input, JObject[] requests, SkirmishMapLayoutConfig layout, SkirmishRoleCatalogConfig roles,
            EntityManager em, Entity gridEntity, GridConfig grid, MapSurfaceComponent surface, JArray facilities, bool applyNormalTerrainFallback = false)
        {
            var output = new JArray(); var walkable = em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray();
            var roster = new[] { ("rifle", SkirmishRoleIds.Rifle), ("gunner", SkirmishRoleIds.Gunner), ("rocketeer", SkirmishRoleIds.Rocketeer),
                ("car", SkirmishRoleIds.Car), ("apc", SkirmishRoleIds.ApcArmored), ("tank", SkirmishRoleIds.Tank),
                ("aa", SkirmishRoleIds.AntiAir), ("transport_heli", SkirmishRoleIds.TransportHeli), ("tanker", SkirmishRoleIds.Tanker), ("oil_truck", SkirmishRoleIds.LogisticsTruck) };
            foreach (var request in requests) foreach (JObject canonical in (JArray)request["canonicalSetupRows"])
            {
                using var blocked = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                using var occupied = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                try
                {
                    for (int i = 0; i < walkable.Length; i++) if (walkable[i].Value == 0) blocked.Set(i, true);
                    foreach (JObject facility in facilities)
                    {
                        if ((string)facility["catalogId"] != (string)request["catalogId"] || (string)facility["size"] != (string)canonical["size_id"] || (bool?)facility["insideGrid"] != true) continue;
                        int ox = (int)facility["originX"], oz = (int)facility["originZ"], width = (int)facility["widthCells"], depth = (int)facility["depthCells"];
                        for (int z = oz; z < oz + depth; z++) for (int x = ox; x < ox + width; x++) occupied.Set(z * grid.Width + x, true);
                    }
                    var random = new Unity.Mathematics.Random(1);
                    foreach (byte faction in new byte[] { 1, 2 }) foreach (var entry in roster)
                    {
                        string side = faction == 1 ? "player_" : "enemy_";
                        int support = int.Parse((string)canonical[side + "logistics_support"], System.Globalization.CultureInfo.InvariantCulture);
                        int quantity = entry.Item1 == "tanker" ? Math.Min(1, support) : entry.Item1 == "oil_truck" ? Math.Max(0, support - 1) : int.Parse((string)canonical[side + entry.Item1], System.Globalization.CultureInfo.InvariantCulture);
                        if (quantity == 0) continue;
                        var result = new JObject { ["catalogId"] = request["catalogId"], ["size"] = canonical["size_id"], ["faction"] = faction,
                            ["roleId"] = entry.Item2, ["requestedCount"] = quantity, ["scope"] = "NativeSpawnQueryOnCopiedBakedGridWithProposedFacilities", ["surveySeed"] = 1,
                            ["normalMissionSpawnCertified"] = false, ["liveOccupancyIncluded"] = false };
                        output.Add(result);
                        if (!roles.TryGet(entry.Item2, out var binding)) { result["status"] = "RoleBindingMissing"; continue; }
                        var prefab = FindNativePrefab(em, binding.RuntimePrefabKey);
                        if (prefab == Entity.Null || !em.HasComponent<UnitFootprint>(prefab)) { result["status"] = "ExactNativePrefabMissing"; continue; }
                        var force = new SkirmishResolvedForceEntry { FactionId = faction, RoleKind = binding.Kind };
                        if (!Game.Configs.SkirmishMapLayoutBuilder.TryPadForForce(layout, force, out var pad) || !layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out float px, out float pz))
                        { result["status"] = "TypedForcePadMissing"; continue; }
                        var footprint = em.GetComponentData<UnitFootprint>(prefab).Size;
                        var center = GridUtils.WorldToCell(grid, new float3(px, 0, pz)); var positions = new JArray(); result["candidateCells"] = positions;
                        int found = 0, traversable = 0, insidePad = 0; bool air = em.HasComponent<UnitAirMovement>(prefab);
                        bool vehicle = em.HasComponent<UnitMovementBehavior>(prefab) && em.GetComponentData<UnitMovementBehavior>(prefab).UsesVehicleMotion != 0;
                        var validation = new MapSurfaceTraversalValidation();
                        for (int actor = 0; actor < quantity; actor++)
                        {
                            if (!InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell(ref random, grid, walkable, blocked, occupied, ref reserved, center, 12, footprint, air, out var cell)) break;
                            var initialCell=cell;
                            bool fallbackAccepted=!applyNormalTerrainFallback || air || SkirmishSpawnPlacement.EnsureTraversable(em,grid,walkable,blocked,occupied,ref reserved,prefab,footprint,ref cell);
                            if(!fallbackAccepted){positions.Add(new JObject {["initialX"]=initialCell.x,["initialZ"]=initialCell.y,["normalFallbackAccepted"]=false});break;}
                            found++;
                            bool contained=!applyNormalTerrainFallback || PilotFootprintInsideProjectedPad(layout,pad,grid,cell,footprint);
                            if(contained)insidePad++;
                            bool terrain = air || validation.CanTraverseFootprint(surface, 1, grid, cell, footprint, vehicle);
                            if (terrain) traversable++;
                            var position=new JObject { ["x"] = cell.x, ["z"] = cell.y, ["surfaceTraversable"] = terrain };
                            if(applyNormalTerrainFallback){position["initialX"]=initialCell.x;position["initialZ"]=initialCell.y;position["normalFallbackAccepted"]=true;position["insideActualProjectedTypedPad"]=contained;}
                            positions.Add(position);
                        }
                        result["nativeFootprintX"] = footprint.x; result["nativeFootprintZ"] = footprint.y;
                        result["foundCount"] = found; result["surfaceTraversableCount"] = traversable;
                        result["status"] = found == quantity && traversable == quantity ? "ProposedPackingFound_RuntimeFitPending" : "ProposedPackingRejected";
                        if(applyNormalTerrainFallback)
                        {
                            result["scope"]="ActualNormalTerrainFallbackOnCopiedBakedGrid_ValidationOnlyMatchContext";
                            result["insideActualProjectedTypedPadCount"]=insidePad;
                            result["status"]=found==quantity&&traversable==quantity&&insidePad==quantity?"NormalStaticFallbackInsideTypedPad_LiveFitPending":"NormalStaticFallbackRejectedOrOutsideTypedPad";
                        }
                    }
                }
                finally { reserved.Dispose(); }
            }
            return output;
        }

        static void MeasureDecodedSurface(OperationMapDefinition source, SkirmishMapLayoutConfig layout, JObject row)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(source.MapSurfaceDataReference.AssetGUID));
            if (asset == null || !asset.TryCreateRuntimeBlobAsset(Allocator.Persistent, out var blob)) throw new InvalidOperationException("Actual source payload cannot be decoded");
            try
            {
                var surface = new MapSurfaceComponent { SurfaceBlob = blob, GridOrigin = asset.GridOrigin, CellSize = asset.CellSize,
                    Dimensions = new int2(asset.Dimensions.x, asset.Dimensions.y), HasSurfaceData = 1 };
                var pads = new JArray(); row["decodedSurfacePads"] = pads;
                foreach (var pad in layout.Pads)
                {
                    Require(layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out float x, out float z), "Pad projection unavailable");
                    var cell = (int2)math.floor((new float2(x, z) - surface.GridOrigin.xz) / surface.CellSize);
                    bool sampled = MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value, cell, out var sample);
                    pads.Add(new JObject { ["padId"] = pad.PadId, ["faction"] = pad.FactionId, ["x"] = x, ["z"] = z,
                        ["sampled"] = sampled, ["height"] = sampled ? (JToken)sample.Height : JValue.CreateNull(),
                        ["slopeDegrees"] = sampled ? (JToken)sample.SlopeDegrees : JValue.CreateNull(), ["scope"] = "DecodedCurrentNativeSurfacePayloadOnly" });
                }
                row["allSizeFacilities"] = "PendingBakedGridUnavailable";
                row["allSizeForcePacking"] = "PendingBakedGridUnavailable";
                row["heavyRouteQueries"] = "PendingBakedGridUnavailable";
            }
            finally { blob.Dispose(); }
        }

        static World StreamingWorld()
        {
            var systems = ground30MemoryWriter == null ? DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true) : Ground30DiagnosticSystems();
            foreach (var system in systems) Require(!TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal), "Gameplay system in streaming filter");
            var world = new World("ReadOnlyGroundSourceFitStreaming", WorldFlags.Game);
            try
            {
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                return world;
            }
            catch
            {
                if (world.IsCreated) world.Dispose();
                throw;
            }
        }
        static void Load(World world, string guid)
        {
            Require(!string.IsNullOrWhiteSpace(guid), "Pinned baked SubScene GUID missing");
            var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(guid), new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
            world.Update();
            Require(SceneSystem.IsSceneLoaded(world.Unmanaged, scene), "Native baked source streaming incomplete");
        }
        static Entity FindNativePrefab(EntityManager em, string key)
        {
            using var query = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            using var owners = query.ToEntityArray(Allocator.Temp);
            var found = Entity.Null;
            foreach (var owner in owners) foreach (var entry in em.GetBuffer<UnitPrefabRegistryEntry>(owner, true))
                if (em.Exists(entry.Prefab) && em.HasComponent<UnitSourcePrefabKey>(entry.Prefab) &&
                    BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(em.GetComponentData<UnitSourcePrefabKey>(entry.Prefab).Value.ToString()) == BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(key))
                { Require(found == Entity.Null || found == entry.Prefab, "Ambiguous exact native prefab"); found = entry.Prefab; }
            return found;
        }
        static NativeArray<T> Array<T>(int count, List<IDisposable> allocations) where T : unmanaged
        { var value = new NativeArray<T>(count, Allocator.Persistent); allocations.Add(value); return value; }
        static void QueryPath(EntityManager em, Entity gridEntity, GridConfig grid, MapSurfaceComponent surface, Entity prefab, int2 from, int2 to, JObject result, byte queryFaction = 1, NativeBitArray plannedFacilityBlockers = default)
        {
            var allocations = new List<IDisposable>(); var scratch = new UnitPathScratchWorkspace();
            using var stream = new NativeStream(1, Allocator.Persistent);
            try
            {
                scratch.Initialize(); scratch.Ensure(grid.Width * grid.Height, out _, out _);
                var entities = Array<Entity>(1, allocations); var units = Array<UnitGrid>(1, allocations); units[0] = new UnitGrid { Cell = from };
                var sizes = Array<UnitFootprint>(1, allocations); sizes[0] = em.GetComponentData<UnitFootprint>(prefab);
                var behaviors = Array<UnitMovementBehavior>(1, allocations); behaviors[0] = em.GetComponentData<UnitMovementBehavior>(prefab);
                var factions = Array<byte>(1, allocations); factions[0] = queryFaction;
                var manual = Array<byte>(1, allocations); manual[0] = 1;
                var skipped = Array<byte>(1, allocations); skipped[0] = 1;
                var requests = Array<UnitPathRequest>(1, allocations); requests[0] = new UnitPathRequest { Goal = to };
                var goals = Array<int2>(1, allocations); goals[0] = to;
                var status = Array<byte>(1, allocations); var failures = Array<int>(1, allocations); var expansions = Array<int>(1, allocations);
                var job = new PathfindBatchJob { Grid = grid, DynamicBlocked=plannedFacilityBlockers, Walkable = em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(),
                    Roads = em.GetBuffer<GridRoad>(gridEntity, true).AsNativeArray(), Sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity, true).AsNativeArray(), DirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity, true).AsNativeArray(),
                    MapSurface = surface, HasMapSurface = 1, SurfaceValidation = new MapSurfaceTraversalValidation(),
                    SurfacePathCost = new MapSurfacePathCost(), SurfaceRoadPriority = new MapSurfaceRoadPriorityPolicy(),
                    Entities = entities, UnitGrids = units, Footprints = sizes, MovementBehaviors = behaviors, Factions = factions, ManualMoves = manual,
                    IgnoredOccupancyEntities = Array<Entity>(1, allocations), IgnoredOccupancyCells = Array<int2>(1, allocations), IgnoredOccupancySizes = Array<int2>(1, allocations),
                    RequestedGoals = requests, Goals = goals, Segmented = Array<byte>(1, allocations), CheapSegmentModes = Array<byte>(1, allocations),
                    AlternateSearchSkipped = skipped, AlternateAttempts = Array<int>(1, allocations), Output = stream.AsWriter(), Status = status, FailureCodes = failures, ExpansionCounts = expansions,
                    GridSize = grid.Width * grid.Height, ScratchCameFrom = scratch.CameFrom, ScratchGScore = scratch.GScore, ScratchClosed = scratch.Closed,
                    ScratchInOpen = scratch.InOpen, ScratchEpoch = scratch.Epoch, ScratchOpen = scratch.Open, ScratchPath = scratch.Path, SearchEpochBase = scratch.ReserveEpochs(1) };
                job.Execute(0);
                var reader = stream.AsReader(); int count = reader.BeginForEachIndex(0); var points = new JArray();
                for (int i = 0; i < count; i++) { var point = reader.Read<int2>(); points.Add(new JArray(point.x, point.y)); }
                reader.EndForEachIndex();
                result["status"] = status[0] == 1 && goals[0].Equals(to) ? "NativeQueryFoundExactGoal" : "NativeQueryFailed";
                result["failureCode"] = failures[0]; result["expansions"] = expansions[0]; result["pathCells"] = points;
                result["footprintX"] = sizes[0].Size.x; result["footprintZ"] = sizes[0].Size.y;
                result["nativeQueryFaction"] = queryFaction;
                result["dynamicBlockersIncluded"] = false; result["liveOccupancyIncluded"] = false;
                if(plannedFacilityBlockers.IsCreated)result["plannedFacilityBlockerScope"]="ExactAcceptedStaticCandidateRectangles_NotLiveDynamicOccupancy";
            }
            finally { foreach (var value in allocations) value.Dispose(); scratch.Dispose(); }
        }
        static string Hash(string path)
        { using var stream = File.OpenRead(path); using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
