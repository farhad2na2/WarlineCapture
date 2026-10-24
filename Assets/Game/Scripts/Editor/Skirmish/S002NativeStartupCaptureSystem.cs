#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Editor
{
    // Passive, opt-in observation of the actual mission World after roster initialization.
    // It never creates actors, changes stocks, supplies positions or orders, or ticks gameplay.
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SkirmishRosterProjectionSystem))]
    public partial class S002NativeStartupCaptureSystem : SystemBase
    {
        bool captured;
        protected override void OnUpdate()
        {
            string output = Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_STARTUP_CAPTURE");
            if (captured || string.IsNullOrEmpty(output)) return;
            using var sessions = EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
            if (sessions.CalculateEntityCount() != 1) return;
            var sessionEntity = sessions.GetSingletonEntity();
            var session = EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            if (session.CatalogId.ToString() != "S002" || session.SpawnComplete == 0 || session.IsLegacy != 0) return;
            if (!EntityManager.HasComponent<SkirmishSharedBuildingsReady>(sessionEntity)) return;
            var setup = EntityManager.GetComponentObject<SkirmishResolvedSetupRecord>(sessionEntity).Setup;
            if (setup == null || setup.CatalogId != "S002") throw new InvalidOperationException("Exact S002 setup missing");
            using var actors = EntityManager.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using var entities = actors.ToEntityArray(Allocator.Temp);
            int forceCount = 0; var rows = new JArray();
            foreach (var actor in entities)
            {
                var owner = EntityManager.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (!owner.SessionId.Equals(session.SessionId)) continue;
                if (EntityManager.HasComponent<SkirmishUnitRoleComponent>(actor))
                {
                    if (!EntityManager.HasComponent<SkirmishRoleOverlayComponent>(actor) ||
                        EntityManager.GetComponentData<SkirmishRoleOverlayComponent>(actor).Applied == 0) return;
                    forceCount++;
                }
                var row = SkirmishBakedActorSnapshotExporter.CaptureActor(EntityManager, actor);
                row["stable_id"] = owner.StableObjectId.ToString(); row["faction"] = owner.FactionId;
                row["is_structure"] = owner.IsStructure != 0; row["reservation_id"] = owner.ReservationId;
                row["source_key"] = EntityManager.HasComponent<UnitSourcePrefabKey>(actor)
                    ? EntityManager.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString() : null;
                row["role"] = Value<SkirmishUnitRoleComponent>(actor);
                row["overlay"] = Value<SkirmishRoleOverlayComponent>(actor);
                row["target_policy"] = Value<CombatTargetPolicy>(actor);
                row["building_storage"] = Value<BuildingResourceStorageComponent>(actor);
                row["building_weapon"] = Value<BuildingDefenseWeapon>(actor);
                rows.Add(row);
            }
            int expected = 0; foreach (var force in setup.Forces) expected += Math.Max(1, force.Quantity);
            if (forceCount != expected) return;
            using var grids = EntityManager.CreateEntityQuery(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            using var maps = EntityManager.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            if (grids.CalculateEntityCount() != 1 || maps.CalculateEntityCount() != 1) return;
            var gridEntity = grids.GetSingletonEntity(); var grid = grids.GetSingleton<GridConfig>();
            var blocker = EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
            var occupancy = EntityManager.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            if (!blocker.Blocked.IsCreated || !occupancy.Occupied.IsCreated || !RuntimeGridBlockerEpoch.IsCommitted(grid, blocker)) return;
            int cells = checked(grid.Width * grid.Height);
            if (blocker.Blocked.Length != cells || occupancy.Occupied.Length != cells || !blocker.FriendlyPassFactionIds.IsCreated ||
                blocker.FriendlyPassFactionIds.Length != cells || cells > SkirmishOfflineDataExporter.MaximumCells)
                throw new InvalidOperationException("Incomplete actual runtime masks");
            var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
            if (!metadata.Blob.IsCreated) return;
            ref var map = ref metadata.Blob.Value;
            S002StartupAllocatorCapture.RequireComplete(expected);
            string path = S002StartupAllocatorCapture.OutputPath(output);
            var artifacts = (JObject)S002StartupAllocatorCapture.Artifacts.DeepClone();
            WriteMask(path, "dynamic-blocked.u8", blocker.Blocked, artifacts);
            WriteMask(path, "dynamic-occupied.u8", occupancy.Occupied, artifacts);
            WriteBytes(path, "friendly-pass.u8", cells, i => blocker.FriendlyPassFactionIds[i], artifacts);
            var walk = EntityManager.GetBuffer<GridWalkable>(gridEntity, true);
            var road = EntityManager.GetBuffer<GridRoad>(gridEntity, true);
            var sidewalk = EntityManager.GetBuffer<GridRoadSidewalk>(gridEntity, true);
            var dirt = EntityManager.GetBuffer<GridRoadDirt>(gridEntity, true);
            if (walk.Length != cells || road.Length != cells || sidewalk.Length != cells || dirt.Length != cells)
                throw new InvalidOperationException("Incomplete real grid classification buffers");
            WriteBytes(path, "walkable.u8", cells, i => walk[i].Value, artifacts);
            S002StartupAllocatorCapture.RequireWalkable((string)artifacts["walkable.u8"]["sha256"]);
            WriteBytes(path, "road.u8", cells, i => road[i].Value, artifacts);
            WriteBytes(path, "sidewalk.u8", cells, i => sidewalk[i].Value, artifacts);
            WriteBytes(path, "dirt-road.u8", cells, i => dirt[i].Value, artifacts);
            using var surfaces = EntityManager.CreateEntityQuery(typeof(MapSurfaceComponent));
            if (surfaces.CalculateEntityCount() != 1) throw new InvalidOperationException("Exact real surface owner required");
            var surface = surfaces.GetSingleton<MapSurfaceComponent>();
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated || surface.Dimensions.x != grid.Width || surface.Dimensions.y != grid.Height ||
                surface.CellSize != grid.CellSize || !Unity.Mathematics.math.all(surface.GridOrigin == grid.Origin))
                throw new InvalidOperationException("Real surface frame unavailable or mismatched");
            var surfaceRuntime = S002RuntimeGroundingCapture.Write(EntityManager, surfaces.GetSingletonEntity(), surface, path, artifacts);
            var profiles = new JArray(); var profileKeys = new System.Collections.Generic.HashSet<string>();
            foreach (var actor in entities)
            {
                var owner = EntityManager.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (!owner.SessionId.Equals(session.SessionId) || !EntityManager.HasComponent<SkirmishUnitRoleComponent>(actor)) continue;
                var size = EntityManager.GetComponentData<UnitFootprint>(actor).Size;
                bool vehicle = EntityManager.GetComponentData<UnitMovementBehavior>(actor).UsesVehicleMotion != 0;
                string key = size.x + "x" + size.y + (vehicle ? "-vehicle" : "-infantry");
                if (!profileKeys.Add(key)) continue;
                string name = "admission-" + key + ".u8";
                WriteBytes(path, name, cells, i => new MapSurfaceTraversalValidation().CanTraverseFootprint(surface, 1, grid,
                    new Unity.Mathematics.int2(i % grid.Width, i / grid.Width), size, vehicle) ? (byte)1 : (byte)0, artifacts);
                profiles.Add(new JObject { ["key"] = key, ["width"] = size.x, ["height"] = size.y,
                    ["vehicle"] = vehicle, ["artifact"] = name, ["policy"] = "Native MapSurfaceTraversalValidation.CanTraverseFootprint" });
            }
            var stocks = new JObject { ["player"] = Value<SkirmishEconomyStockComponent>(sessionEntity),
                ["enemy"] = Value<SkirmishEnemyStockComponent>(sessionEntity),
                ["capacity"] = Value<SkirmishCapacityComponent>(sessionEntity),
                ["enemy_capacity"] = Value<SkirmishEnemyCapacityComponent>(sessionEntity),
                ["objective"] = Value<SkirmishObjectiveStateComponent>(sessionEntity),
                ["clock"] = Value<SkirmishObjectiveClockComponent>(sessionEntity) };
            var report = new JObject { ["schema_version"] = 1, ["scope"] = "ActualS002World_PostRoster_FrameBoundaryObservation",
                ["engine_frame"] = UnityEngine.Time.frameCount, ["world_elapsed_seconds"] = World.Time.ElapsedTime,
                ["session_id"] = session.SessionId.ToString(), ["setup_hash"] = session.SetupHash,
                ["session_phase"] = session.Phase.ToString(), ["tick_clock"] = session.TickClock,
                ["seed"] = session.Seed, ["size"] = session.SizeId.ToString(), ["difficulty"] = session.DifficultyId.ToString(),
                ["resolved_setup"] = JObject.Parse(JsonUtility.ToJson(setup)), ["session_facts"] = stocks,
                ["registry_templates"] = SkirmishBakedActorSnapshotExporter.Capture(EntityManager),
                ["allocator_transactions"] = S002StartupAllocatorCapture.Events.DeepClone(),
                ["footprint_surface_profiles"] = profiles,
                ["surface_runtime"] = surfaceRuntime,
                ["physical_startup_inputs"] = S002PhysicalStartupCapture.Finish(setup.Structures?.Length ?? 0),
                ["grid"] = JObject.Parse(JsonUtility.ToJson(grid)), ["actors"] = rows, ["expected_force_count"] = expected,
                ["operation_map_id"] = map.OperationMapId.ToString(), ["physical_source_id"] = map.SourceOperationMapId.ToString(),
                ["source_identity_hash"] = map.SourceIdentityHash.ToString(), ["content_hash"] = map.ContentHash.ToString(),
                ["grid_asset_guid"] = map.Grid.AssetGuid.ToString(), ["surface_asset_guid"] = map.Surface.AssetGuid.ToString(),
                ["authored_subscene_guid"] = map.Navigation.AuthoredSubSceneGuid.ToString(), ["artifacts"] = artifacts,
                ["blocker_storage_epoch"] = blocker.StorageEpoch, ["blocker_revision"] = blocker.AppliedRevision,
                ["full_mission_initialized"] = false, ["offline_parity"] = false,
                ["pending_capture"] = new JArray("complete building/economy graph", "source-pinned startup transaction", "offline-derived grounded transforms and complete startup") };
            File.WriteAllText(Path.Combine(path, "startup-observation.json"), report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            captured = true;
            S002StartupAllocatorCapture.Completed = true;
            Debug.Log("[S002PhysicalStartupInputs] result=Captured buildingTransactions=" + report["physical_startup_inputs"]["building_transactions"] +
                " supplyStages=" + report["physical_startup_inputs"]["supply_stages"] + " fullPhysicalStartupDerived=0");
            Debug.Log("[S002RuntimeGroundingInputs] result=Captured compactCells=" + surfaceRuntime["compact_count"] +
                " overlays=" + surfaceRuntime["overlay_count"] + " triangles=" + surfaceRuntime["triangle_count"] +
                " instances=" + surfaceRuntime["instance_count"] + " beforePlacementWalkableVerified=" + expected);
            Debug.Log("[S002NativeStartupObservation] result=Captured forceActors=" + forceCount + " expected=" + expected + " fullMissionInitialized=0 offlineParity=0");
        }
        JToken Value<T>(Entity entity) where T : unmanaged, IComponentData =>
            EntityManager.HasComponent<T>(entity) ? JObject.Parse(JsonUtility.ToJson(EntityManager.GetComponentData<T>(entity))) : JValue.CreateNull();
        static void WriteBytes(string directory, string name, int count, Func<int, byte> read, JObject artifacts)
        {
            var bytes = new byte[count]; for (int i = 0; i < count; i++) bytes[i] = read(i);
            string path = Path.Combine(directory, name); File.WriteAllBytes(path, bytes);
            artifacts[name] = new JObject { ["bytes"] = count, ["encoding"] = "u8", ["sha256"] = SkirmishOfflineDataExporter.HashFile(path) };
        }
        static void WriteMask(string directory, string name, NativeBitArray bits, JObject artifacts)
        {
            var bytes = new byte[bits.Length]; for (int i = 0; i < bytes.Length; i++) bytes[i] = bits.IsSet(i) ? (byte)1 : (byte)0;
            string path = Path.Combine(directory, name); File.WriteAllBytes(path, bytes);
            artifacts[name] = new JObject { ["bytes"] = bytes.Length, ["encoding"] = "u8", ["sha256"] = SkirmishOfflineDataExporter.HashFile(path) };
        }
    }
}
#endif
