#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Narrow causal inputs for the existing eighteen S002 starting buildings.
    // No placements, resources, component values or orders are changed here.
    [InitializeOnLoad]
    internal static class S002PhysicalStartupCapture
    {
        static readonly JArray Transactions = new JArray(), Supply = new JArray();
        static readonly Dictionary<int, JObject> Pending = new Dictionary<int, JObject>();
        static string sessionId;
        static MapSurfaceComponent? firstSurface;
        static bool granted;
        static int currentRequest, currentFrame;
        static S002PhysicalStartupObservation.OriginalPlacementFacts originalFacts;
        static S002PhysicalStartupCapture()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_STARTUP_CAPTURE"))) return;
            S002PhysicalStartupObservation.BeforePlacement = BeforePlacement;
            S002PhysicalStartupObservation.AfterPlacement = AfterPlacement;
            S002PhysicalStartupObservation.BeforeFactory = BeforeFactory;
            S002PhysicalStartupObservation.AfterFactory = AfterFactory;
            S002PhysicalStartupObservation.SupplyStage = SupplyStage;
            S002PhysicalStartupObservation.OriginalPlacement = OriginalPlacement;
            S002PhysicalStartupObservation.FootprintCenter = FootprintCenter;
        }
        static bool Qualify(EntityManager em, Entity session)
        {
            if (S002StartupAllocatorCapture.Completed || session == Entity.Null || !em.Exists(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)) return false;
            var value = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (value.CatalogId.ToString() != "S002") return false;
            if (sessionId != null && sessionId != value.SessionId.ToString()) throw new InvalidOperationException("Mixed physical startup sessions");
            sessionId = value.SessionId.ToString();
            return true;
        }
        static string Output => S002StartupAllocatorCapture.OutputPath(Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_STARTUP_CAPTURE"));
        static JObject EntityId(Entity value) => new JObject { ["index"] = value.Index, ["version"] = value.Version };
        static JArray Pair(Vector2Int value) => new JArray(value.x, value.y);
        static JObject Stamp(EntityManager em) => new JObject { ["frame"] = Time.frameCount, ["world_elapsed_seconds"] = em.World.Time.ElapsedTime };
        static JToken Value<T>(EntityManager em, Entity actor) where T : unmanaged, IComponentData =>
            em.HasComponent<T>(actor) ? JObject.Parse(JsonUtility.ToJson(em.GetComponentData<T>(actor))) : JValue.CreateNull();
        static JObject Building(EntityManager em, Entity actor)
        {
            var row = SkirmishBakedActorSnapshotExporter.CaptureActor(em, actor);
            row["storage"] = Value<BuildingResourceStorageComponent>(em, actor);
            row["production_profile"] = Value<BuildingResourceProductionProfileComponent>(em, actor);
            row["fabrication"] = Value<MaterialFabricationComponent>(em, actor);
            row["fabrication_queue"] = Value<MaterialFabricationCommandQueueComponent>(em, actor);
            row["weapon"] = Value<BuildingDefenseWeapon>(em, actor);
            row["combat_info"] = Value<RuntimeBuildingCombatInfo>(em, actor);
            row["structure_identity"] = Value<SkirmishStructureIdentityComponent>(em, actor);
            row["objective_role"] = Value<SkirmishObjectiveRoleComponent>(em, actor);
            row["target_policy"] = Value<CombatTargetPolicy>(em, actor);
            row["ground_staging"] = Value<SkirmishGroundStagingStateComponent>(em, actor);
            return row;
        }
        static JObject Asset(UnityEngine.Object value)
        {
            if (value == null) return null;
            string path = AssetDatabase.GetAssetPath(value);
            var row = new JObject { ["name"] = value.name, ["path"] = path };
            if (!string.IsNullOrEmpty(path))
            {
                if (!File.Exists(path) || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long local))
                    throw new InvalidOperationException("Selected building source asset unavailable");
                row["guid"] = guid; row["local_file_id"] = local;
                row["sha256"] = SkirmishOfflineDataExporter.HashFile(path);
                row["meta_sha256"] = SkirmishOfflineDataExporter.HashFile(path + ".meta");
            }
            return row;
        }
        static JObject Definition(BuildingDefinition value)
        {
            var result = new JObject();
            // This fixed selected-definition type is the factory input, not the
            // post-spawn building. Unknown gameplay field types fail closed.
            foreach (var field in typeof(BuildingDefinition).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object v = field.GetValue(value); Type t = field.FieldType;
                if (v is float scalar && (float.IsNaN(scalar) || float.IsInfinity(scalar))) throw new InvalidOperationException("Nonfinite selected building field " + field.Name);
                if (t.IsPrimitive || t.IsEnum || t == typeof(string))
                    result[field.Name] = v == null ? JValue.CreateNull() : t.IsEnum ? new JValue(Convert.ToInt32(v)) : JToken.FromObject(v);
                else if (typeof(UnityEngine.Object).IsAssignableFrom(t)) result[field.Name] = Asset((UnityEngine.Object)v);
                else if (t == typeof(Vector2Int)) result[field.Name] = Pair((Vector2Int)v);
                else if (t == typeof(Vector3)) { var p = (Vector3)v; result[field.Name] = new JArray(p.x, p.y, p.z); }
                else if (t == typeof(Quaternion)) { var p = (Quaternion)v; result[field.Name] = new JArray(p.x, p.y, p.z, p.w); }
                else if (t == typeof(Color)) { var p = (Color)v; result[field.Name] = new JArray(p.r, p.g, p.b, p.a); }
                else if (t == typeof(Bounds)) { var p = (Bounds)v; result[field.Name] = new JObject { ["center"] = new JArray(p.center.x, p.center.y, p.center.z), ["extents"] = new JArray(p.extents.x, p.extents.y, p.extents.z) }; }
                else if (t == typeof(Vector3[]))
                { var items = new JArray(); if (v is Vector3[] array) foreach (var p in array) items.Add(new JArray(p.x, p.y, p.z)); result[field.Name] = items; }
                else if (t == typeof(List<BuildingDefinition.ProductionSlotDefinition>))
                {
                    var items = new JArray();
                    if (v is List<BuildingDefinition.ProductionSlotDefinition> slots) foreach (var slot in slots)
                        items.Add(new JObject { ["prefab"] = Asset(slot.SpawnUnitPrefab), ["source_key"] = slot.SpawnUnitSourceKey.ToString(), ["quantity"] = slot.Quantity });
                    result[field.Name] = items;
                }
                else if (t == typeof(List<Mesh>)) result[field.Name] = new JObject { ["visual_only_omitted"] = true, ["count"] = (v as List<Mesh>)?.Count ?? 0 };
                else throw new InvalidOperationException("Unmodeled selected building definition field " + field.Name);
            }
            return result;
        }
        static JObject JsonArtifact(string file, JObject value)
        {
            string path = Path.Combine(Output, file);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToString(Newtonsoft.Json.Formatting.None) + "\n");
            if (bytes.Length > 128 * 1024 * 1024) throw new InvalidOperationException("Bounded physical input JSON required");
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) stream.Write(bytes, 0, bytes.Length);
            return new JObject { ["artifact"] = file, ["bytes"] = bytes.Length, ["encoding"] = "utf8-json", ["sha256"] = SkirmishOfflineDataExporter.HashFile(path) };
        }
        static JObject Mask(string name, int cells, Func<int, bool> value)
        {
            if (cells <= 0 || cells > SkirmishOfflineDataExporter.MaximumCells) throw new InvalidOperationException("Bounded complete building grid required");
            byte[] bytes = new byte[cells]; for (int i = 0; i < cells; i++) bytes[i] = (byte)(value(i) ? 1 : 0);
            string hash;
            using (var algorithm = System.Security.Cryptography.SHA256.Create()) hash = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            string file = "building-mask-" + hash + ".u8", path = Path.Combine(Output, file);
            if (File.Exists(path)) { if (SkirmishOfflineDataExporter.HashFile(path) != hash) throw new InvalidOperationException("Existing physical mask differs"); }
            else File.WriteAllBytes(path, bytes);
            return new JObject { ["role"] = name, ["artifact"] = file, ["bytes"] = cells, ["encoding"] = "u8", ["sha256"] = hash };
        }
        static void BeforePlacement(EntityManager em, BuildingRuntimeSpawnRequest request, BuildingDefinition definition,
            BuildingRuntimeSpawnCompositionSystemHelper.Context context, BuildingPlacementAuthoredBuildingCache buildings, BuildingPlacementAuthoredRoadCache roads)
        {
            if (!Qualify(em, request.PlanEntity)) return;
            if (Transactions.Count >= 18 || Pending.ContainsKey(request.RequestId) || request.AuthoredStartingGrant != 1 ||
                request.RequestKind != BuildingRuntimeSpawnRequest.KindBuilding || request.RequirePreferredOrigin != 0 || request.RotateVertical != 0)
                throw new InvalidOperationException("Unexpected S002 physical building transaction");
            if (!context.TryGetGridData(out var gridEntity, out var grid, out var gridRoads, out var blockers) ||
                !blockers.Blocked.IsCreated || blockers.Blocked.Length != checked(grid.Width * grid.Height) || gridRoads.Length != grid.Width * grid.Height || !buildings.IsReady)
                throw new InvalidOperationException("Complete pre-building placement facts unavailable");
            using (var surfaceQuery = em.CreateEntityQuery(typeof(MapSurfaceComponent)))
            {
                if (surfaceQuery.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique pre-building surface required");
                var surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
                if (!firstSurface.HasValue) firstSurface = surface;
                RequireSameSurface(surface);
                if (surface.Dimensions.x != grid.Width || surface.Dimensions.y != grid.Height || surface.CellSize != grid.CellSize || !surface.GridOrigin.Equals(grid.Origin))
                    throw new InvalidOperationException("Pre-building surface/grid differs");
            }
            int cells = checked(grid.Width * grid.Height); var sourceBuildings = new bool[cells]; var sourceRoads = new bool[cells];
            buildings.AppendTo(sourceBuildings); roads.AppendTo(sourceRoads); var water = roads.GetWater(); var sidewalks = roads.GetSidewalks();
            if (water == null || sidewalks == null || water.Length != cells || sidewalks.Length != cells) throw new InvalidOperationException("Complete authored building placement masks required");
            var masks = new JArray(Mask("blocked_before", cells, i => blockers.Blocked.IsSet(i)), Mask("grid_roads_before", cells, i => gridRoads[i].Value != 0),
                Mask("authored_buildings", cells, i => sourceBuildings[i]), Mask("authored_roads_and_sidewalks", cells, i => sourceRoads[i]),
                Mask("water", cells, i => water[i]), Mask("sidewalks", cells, i => sidewalks[i]));
            var runtime = new JArray();
            if (context.WallValidationContext.RuntimeBuildings != null)
                foreach (var pair in context.WallValidationContext.RuntimeBuildings)
                    if (pair.Value != null && pair.Value.Definition != null)
                        runtime.Add(new JObject { ["runtime_id"] = pair.Key, ["is_destroyed"] = pair.Value.IsDestroyed, ["origin"] = Pair(pair.Value.OriginCell), ["definition"] = Definition(pair.Value.Definition) });
            var row = Stamp(em); row["ordinal"] = Transactions.Count; row["request"] = JObject.Parse(JsonUtility.ToJson(request));
            row["grid_entity"] = EntityId(gridEntity); row["grid"] = JObject.Parse(JsonUtility.ToJson(grid)); row["selected_request_definition"] = Definition(definition);
            row["placement_footprint_assertion"] = Pair(context.GetPlacementFootprint(definition, false)); row["masks_before"] = masks;
            row["existing_runtime_buildings_before"] = runtime;
            var occupancy = S002BuildingOccupancyCapture.Read(em, grid);
            row["pre_placement_live_occupancy"] = JsonArtifact("building-occupancy-" + Transactions.Count.ToString("D2") + ".json", occupancy);
            row["original_placement_cache_inputs_complete"] = false;
            // The original placement owner's cache/prefix/runtime-blocker exception
            // still needs its own input observation. Never substitute this cache.
            if (currentRequest != 0) throw new InvalidOperationException("Nested physical placement observation");
            currentRequest = request.RequestId; currentFrame = Time.frameCount; originalFacts = null;
            Pending.Add(request.RequestId, row); Transactions.Add(row); Flush();
        }
        static void OriginalPlacement(S002PhysicalStartupObservation.OriginalPlacementFacts facts)
        {
            if (currentRequest == 0) return;
            if (!Pending.TryGetValue(currentRequest, out var row) || currentFrame != Time.frameCount || facts.Rotated)
                throw new InvalidOperationException("Original placement observation escaped its request/frame");
            if (originalFacts != null)
            {
                if (!ReferenceEquals(originalFacts.InvalidPrefix, facts.InvalidPrefix) || originalFacts.HasInvalidPrefix != facts.HasInvalidPrefix ||
                    originalFacts.PrefixWidth != facts.PrefixWidth || originalFacts.PrefixHeight != facts.PrefixHeight ||
                    !ReferenceEquals(originalFacts.Buildings, facts.Buildings) || !ReferenceEquals(originalFacts.Roads, facts.Roads) ||
                    !ReferenceEquals(originalFacts.Startup, facts.Startup) || !ReferenceEquals(originalFacts.Dependencies, facts.Dependencies) ||
                    originalFacts.Grid.Width != facts.Grid.Width || originalFacts.Grid.Height != facts.Grid.Height ||
                    originalFacts.Grid.CellSize != facts.Grid.CellSize || !originalFacts.Grid.Origin.Equals(facts.Grid.Origin))
                    throw new InvalidOperationException("Original placement inputs changed within the search");
                return;
            }
            int cells = checked(facts.Grid.Width * facts.Grid.Height);
            if (cells <= 0 || cells > SkirmishOfflineDataExporter.MaximumCells || !facts.Buildings.IsReady)
                throw new InvalidOperationException("Complete original building placement grid required");
            var buildings = new bool[cells]; var roads = new bool[cells]; var startupRoads = new bool[cells];
            facts.Buildings.AppendTo(buildings); facts.Roads.AppendTo(roads); facts.Startup.FillRoadFootprintMask(facts.Grid, startupRoads);
            var water = facts.Roads.GetWater();
            if (water == null || water.Length != cells) throw new InvalidOperationException("Original water placement mask unavailable");
            var data = new JObject
            {
                ["has_invalid_prefix"] = facts.HasInvalidPrefix, ["prefix_width"] = facts.PrefixWidth, ["prefix_height"] = facts.PrefixHeight,
                ["masks"] = new JArray(Mask("original_authored_buildings", cells, i => buildings[i]), Mask("original_authored_roads_and_sidewalks", cells, i => roads[i]),
                    Mask("original_water", cells, i => water[i]), Mask("original_startup_roads", cells, i => startupRoads[i]),
                    Mask("runtime_blocker_exceptions", cells, i => facts.Dependencies.IsRuntimeBlockerCell(i % facts.Grid.Width, i / facts.Grid.Width, facts.Grid.Width, facts.Grid.Height))),
                ["first_validated_candidate_assertion"] = Pair(facts.Origin), ["footprint_assertion"] = Pair(facts.Footprint),
                ["effective_rect_assertion"] = new JArray(facts.EffectiveRect.x, facts.EffectiveRect.y, facts.EffectiveRect.width, facts.EffectiveRect.height),
                ["mask_scope"] = "Captured static cache/source facts remain inputs; later placement results are assertions only"
            };
            if (facts.HasInvalidPrefix)
            {
                if (facts.InvalidPrefix == null || facts.PrefixWidth <= 0 || facts.PrefixHeight <= 0 ||
                    (long)facts.PrefixWidth * facts.PrefixHeight != facts.InvalidPrefix.Length || facts.InvalidPrefix.Length > 17000000)
                    throw new InvalidOperationException("Complete original invalid prefix required");
                byte[] bytes;
                using (var stream = new MemoryStream())
                { using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) foreach (int value in facts.InvalidPrefix) writer.Write(value); bytes = stream.ToArray(); }
                string hash; using (var algorithm = System.Security.Cryptography.SHA256.Create()) hash = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                string file = "building-prefix-" + hash + ".i32", path = Path.Combine(Output, file);
                if (File.Exists(path)) { if (SkirmishOfflineDataExporter.HashFile(path) != hash) throw new InvalidOperationException("Original prefix artifact differs"); }
                else File.WriteAllBytes(path, bytes);
                data["invalid_prefix"] = new JObject { ["artifact"] = file, ["bytes"] = bytes.Length, ["encoding"] = "i32-le", ["sha256"] = hash };
            }
            row["original_placement_inputs"] = data; row["original_placement_cache_inputs_complete"] = true;
            originalFacts = facts; Flush();
        }
        static void AfterPlacement(EntityManager em, BuildingRuntimeSpawnRequest request)
        {
            if (!Qualify(em, request.PlanEntity)) return;
            if (!Pending.TryGetValue(request.RequestId, out var row)) throw new InvalidOperationException("Physical placement result without input transaction");
            if (currentRequest != request.RequestId) throw new InvalidOperationException("Physical placement observation scope changed");
            currentRequest = 0; originalFacts = null;
            row["placement_result_assertion"] = JObject.Parse(JsonUtility.ToJson(request)); row["placement_result_stamp"] = Stamp(em);
            Flush();
        }
        static void FootprintCenter(Vector2Int origin, Vector2Int footprint, GridConfig grid, float buildPlaneY)
        {
            if (currentRequest == 0 || !Pending.TryGetValue(currentRequest, out var row) || row["factory_inputs"] == null || row["factory_result_assertion"] != null) return;
            if (originalFacts == null || row["factory_center_inputs"] != null || float.IsNaN(buildPlaneY) || float.IsInfinity(buildPlaneY) ||
                buildPlaneY != originalFacts.Startup.BuildPlaneY)
                throw new InvalidOperationException("Unqualified physical factory center inputs");
            row["factory_center_inputs"] = new JObject { ["origin_assertion"] = Pair(origin), ["footprint_assertion"] = Pair(footprint),
                ["grid"] = JObject.Parse(JsonUtility.ToJson(grid)), ["build_plane_y"] = buildPlaneY,
                ["active_startup_config"] = Asset(originalFacts.Startup.ObservedSourceConfig),
                ["policy"] = "BuildingPlacementGridCameraSystemHelper.GetFootprintCenter; center XZ from footprint and Y from actual startup BuildPlaneY" };
            Flush();
        }
        static void BeforeFactory(EntityManager em, Entity grid, BuildingDefinition definition, int runtimeId, Vector2Int origin, byte faction, Quaternion rotation, RuntimeActorBirthContext birth)
        {
            if (!Qualify(em, birth.Session)) return;
            if (!Pending.TryGetValue(birth.RequestId, out var row) || row["factory_inputs"] != null) throw new InvalidOperationException("Unpaired physical factory input");
            row["factory_inputs"] = new JObject { ["stamp"] = Stamp(em), ["grid_entity"] = EntityId(grid), ["runtime_id_assertion"] = runtimeId,
                ["origin_assertion"] = Pair(origin), ["owner_faction"] = faction, ["rotation"] = new JArray(rotation.x, rotation.y, rotation.z, rotation.w),
                ["selected_definition"] = Definition(definition), ["birth_entry_index"] = birth.EntryIndex, ["birth_request_id"] = birth.RequestId };
            Flush();
        }
        static void AfterFactory(EntityManager em, Entity actor, RuntimeActorBirthContext birth)
        {
            if (!Qualify(em, birth.Session)) return;
            if (!Pending.TryGetValue(birth.RequestId, out var row) || row["factory_inputs"] == null || row["factory_center_inputs"] == null || row["factory_result_assertion"] != null)
                throw new InvalidOperationException("Unpaired physical factory result");
            row["factory_result_assertion"] = Building(em, actor); row["factory_result_stamp"] = Stamp(em); Flush();
        }
        static void SupplyStage(EntityManager em, Entity session, string stage, Entity[] grantOrder)
        {
            if (!Qualify(em, session)) return;
            if (Supply.Count >= 256 || granted) throw new InvalidOperationException("Unexpected physical supply stage sequence");
            using var all = em.CreateEntityQuery(typeof(BuildingResourceStorageComponent)); using var entities = all.ToEntityArray(Allocator.Temp);
            if (entities.Length > 65536 || grantOrder.Length > 65536) throw new InvalidOperationException("Bounded complete physical store census required");
            var rows = new JArray();
            foreach (var entity in entities)
            {
                var row = new JObject { ["entity"] = EntityId(entity), ["storage"] = Value<BuildingResourceStorageComponent>(em, entity),
                    ["fabrication"] = Value<MaterialFabricationComponent>(em, entity), ["production_profile"] = Value<BuildingResourceProductionProfileComponent>(em, entity) };
                if (em.HasComponent<SkirmishAttemptOwnedComponent>(entity))
                { var own = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity); row["attempt"] = own.SessionId.ToString(); row["stable_id"] = own.StableObjectId.ToString(); }
                rows.Add(row);
            }
            var order = new JArray(); foreach (var entity in grantOrder) order.Add(EntityId(entity));
            var observed = Stamp(em); observed["stage"] = stage; observed["all_enabled_stores_native_query_order"] = rows;
            observed["native_grant_order_assertion"] = order; Supply.Add(observed); granted = stage == "after-grant"; Flush();
        }
        static void Flush()
        {
            bool cacheComplete = Transactions.Count == 18;
            foreach (JObject transaction in Transactions) cacheComplete &= transaction["original_placement_inputs"] != null;
            var value = new JObject { ["schema_version"] = 1, ["scope"] = "ActualS002PhysicalStartupInputsAndAssertions", ["session_id"] = sessionId,
                ["transactions"] = Transactions, ["supply_stages"] = Supply, ["original_placement_cache_inputs_complete"] = cacheComplete,
                ["full_physical_startup_derived"] = false, ["full_mission_initialized"] = false };
            File.WriteAllText(Path.Combine(Output, "building-startup.json"), value.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        }
        internal static void RequireSameSurface(MapSurfaceComponent surface)
        {
            if (!firstSurface.HasValue || !surface.SurfaceBlob.IsCreated || !surface.SurfaceBlob.Equals(firstSurface.Value.SurfaceBlob) ||
                !surface.Dimensions.Equals(firstSurface.Value.Dimensions) || surface.CellSize != firstSurface.Value.CellSize ||
                !surface.GridOrigin.Equals(firstSurface.Value.GridOrigin))
                throw new InvalidOperationException("Physical building surface owner/blob/frame changed");
        }
        internal static JObject Finish(int expected)
        {
            if (expected != 18 || Transactions.Count != expected || !granted || currentRequest != 0) throw new InvalidOperationException("Incomplete physical building startup observation");
            foreach (JObject row in Transactions)
                if (row["factory_inputs"] == null || row["factory_center_inputs"] == null || row["factory_result_assertion"] == null || row["placement_result_assertion"] == null || row["original_placement_inputs"] == null ||
                    (byte)row["placement_result_assertion"]["Status"] != BuildingRuntimeSpawnRequest.Succeeded)
                    throw new InvalidOperationException("Incomplete physical building transaction");
            Flush(); string path = Path.Combine(Output, "building-startup.json");
            return new JObject { ["artifact"] = "building-startup.json", ["bytes"] = new FileInfo(path).Length,
                ["sha256"] = SkirmishOfflineDataExporter.HashFile(path), ["building_transactions"] = Transactions.Count, ["supply_stages"] = Supply.Count,
                ["original_placement_cache_inputs_complete"] = true, ["same_surface_blob_as_allocator_and_post_roster"] = true, ["full_physical_startup_derived"] = false };
        }
    }
}
#endif
