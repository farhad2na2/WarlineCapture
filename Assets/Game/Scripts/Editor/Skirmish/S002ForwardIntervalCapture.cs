#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // A single opt-in trace on the existing normally launched S002 World.
    // No creation of actors/orders, synthetic clock, world update, or gameplay writes.
    [InitializeOnLoad]
    internal static class S002ForwardIntervalCapture
    {
        const int UnitLimit = 512, StorageLimit = 4096, VisitLimit = 16384;
        static NativeArray<S002PhysicalStartupObservation.ForwardVehicleStep> steps;
        static NativeArray<S002PhysicalStartupObservation.ForwardCleanupStep> cleanupSteps;
        static readonly Dictionary<string, byte[]> pendingBuffers = new Dictionary<string, byte[]>();
        static int expectedCleanupCount;
        static readonly JArray unitOrder = new JArray(), storageVisits = new JArray();
        static readonly JArray commands = new JArray(), productionHistory = new JArray();
        static bool verifiedMove;
        static int idleIntervals;
        internal static bool Completed => completed;
        internal static string Failure => failure;
        static readonly List<Entity> fuelEntities = new List<Entity>();
        static readonly int[] visitOrdinals = new int[256];
        static JObject trace;
        static Entity sessionEntity;
        static World observedWorld;
        static int frame, expectedMoveCount;
        static bool armed, completed;
        static string output, failure;
        static S002ForwardIntervalCapture()
        {
            if (Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_FORWARD_TRACE") != "1" ||
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_STARTUP_CAPTURE"))) return;
            try { SerializerPreflight(); }
            catch (Exception ex) { Fail(ex); completed = true; Debug.LogError("[S002ForwardSerializer] result=Failed reason=" + failure); return; }
            S002PhysicalStartupObservation.BeginGridInterval = Begin;
            S002PhysicalStartupObservation.ForwardFuelBoundary = Boundary;
            S002PhysicalStartupObservation.ForwardFuelUnit = FuelUnit;
            S002PhysicalStartupObservation.ForwardFuelStorageVisit = StorageVisit;
            S002PhysicalStartupObservation.ForwardFuelRequested = Requested;
            S002PhysicalStartupObservation.ForwardProductionBoundary = Production;
            AriaCommandEvidence.Recorded += Command;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingPlayMode) Release(); };
        }
        static void SerializerPreflight()
        {
            using var world = new World("S002 passive serializer preflight");
            var em = world.EntityManager;
            var e = em.CreateEntity(typeof(ManualMoveOrderTag), typeof(ManualMoveGroupMemberTag), typeof(AutoWanderMoveTag), typeof(CampaignMissionGuidedMoveInProgressTag), typeof(UnitTarget));
            em.SetComponentData(e, new UnitTarget { Cell = new int2(3, 7) });
            Require(Value<ManualMoveOrderTag>(em, e) is JObject a && a.Count == 0, "Present manual tag must be empty object");
            Require(Value<ManualMoveGroupMemberTag>(em, e) is JObject b && b.Count == 0, "Present manual group tag must be empty object");
            Require(Value<AutoWanderMoveTag>(em, e) is JObject c && c.Count == 0, "Present wander tag must be empty object");
            Require(Value<CampaignMissionGuidedMoveInProgressTag>(em, e) is JObject d && d.Count == 0, "Present guided tag must be empty object");
            Require(Value<CampaignMissionStationaryUnitTag>(em, e).Type == Newtonsoft.Json.Linq.JTokenType.Null, "Absent tag must remain null");
            var target = Value<UnitTarget>(em, e);
            Require((int)target["Cell"]["x"] == 3 && (int)target["Cell"]["y"] == 7, "Nonzero component values must round trip");
            em.DestroyEntity(e);
            Debug.Log("[S002ForwardSerializer] result=Passed controls=6 scope=IsolatedSerializerOnly");
        }
        static void Command(string operation, uint receipt, bool verified)
        {
            if (completed) return;
            try
            {
                Require(commands.Count < 512, "Normal command history bound exceeded");
                commands.Add(new JObject { ["operation"] = operation, ["receipt"] = receipt,
                    ["verified_touch_release"] = verified, ["engine_frame"] = Time.frameCount });
                if ((operation == "Move" || operation == "AttackMove") && verified && receipt != 0) verifiedMove = true;
            }
            catch (Exception ex) { Fail(ex); completed = true; Release(); }
        }
        static bool FindSession(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupComponent), typeof(SkirmishEconomyStockComponent), typeof(SkirmishEnemyStockComponent));
            if (query.CalculateEntityCount() != 1) return false;
            sessionEntity = query.GetSingletonEntity();
            return em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity).CatalogId.ToString() == "S002";
        }
        static void Production(EntityManager em, EntityQuery query, string boundary, float secondsPerDay, float deltaTime, float oilRatio)
        {
            if (completed) return;
            try
            {
                if (!FindSession(em)) return;
                Require(productionHistory.Count < 512, "Production boundary history bound exceeded");
                Require(query.CalculateEntityCount() <= StorageLimit, "Production query bound exceeded");
                var rows = new JArray();
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities) rows.Add(new JObject { ["id"] = Id(entity),
                    ["storage"] = Value<BuildingResourceStorageComponent>(em, entity),
                    ["profile"] = Value<BuildingResourceProductionProfileComponent>(em, entity) });
                productionHistory.Add(new JObject { ["stamp"] = Stamp(em, boundary), ["seconds_per_day_bits"] = Bits(secondsPerDay),
                    ["delta_time_bits"] = Bits(deltaTime), ["oil_ratio_bits"] = Bits(oilRatio),
                    ["readonly_query_snapshot"] = rows, ["actual_loop_order_proven"] = false });
            }
            catch (Exception ex) { Fail(ex); completed = true; Release(); }
        }
        static string Id(Entity e) => e.Index + ":" + e.Version;
        static JObject EntityId(Entity e) => new JObject { ["index"] = e.Index, ["version"] = e.Version };
        static int Bits(float value) => BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        static JToken Value<T>(EntityManager em, Entity e) where T : unmanaged, IComponentData
        {
            if (!em.HasComponent<T>(e)) return JValue.CreateNull();
            if (TypeManager.GetTypeInfo<T>().IsZeroSized) return new JObject();
            string encoded = JsonUtility.ToJson(em.GetComponentData<T>(e));
            Require(JsonUtility.ToJson(JsonUtility.FromJson<T>(encoded)) == encoded, "Component serialization changed: " + typeof(T).Name);
            return JObject.Parse(encoded);
        }
        static JObject Actor(EntityManager em, Entity e)
        {
            var row = SkirmishBakedActorSnapshotExporter.CaptureActor(em, e); row["entity"] = EntityId(e); row["query_id"] = Id(e);
            var data = (JObject)row["components"];
            data["Faction"] = Value<Faction>(em, e); data["UnitPrevWorldPos"] = Value<UnitPrevWorldPos>(em, e);
            data["UnitFuelConsumptionState"] = Value<UnitFuelConsumptionState>(em, e);
            data["UnitTarget"] = Value<UnitTarget>(em, e); data["UnitPathRequest"] = Value<UnitPathRequest>(em, e);
            data["UnitPathFollow"] = Value<UnitPathFollow>(em, e); data["UnitPathRange"] = Value<UnitPathRange>(em, e);
            data["UnitPathRetryCooldown"] = Value<UnitPathRetryCooldown>(em, e); data["UnitLongDistanceMove"] = Value<UnitLongDistanceMove>(em, e);
            data["EngageTarget"] = Value<EngageTarget>(em, e); data["ManualMoveOrderTag"] = Value<ManualMoveOrderTag>(em, e);
            data["ManualMoveGroupMemberTag"] = Value<ManualMoveGroupMemberTag>(em, e); data["AutoWanderMoveTag"] = Value<AutoWanderMoveTag>(em, e);
            data["CampaignMissionGuidedMoveInProgressTag"] = Value<CampaignMissionGuidedMoveInProgressTag>(em, e);
            data["UnitTransportBoardingTarget"] = Value<UnitTransportBoardingTarget>(em, e);
            data["UnitResourceHaulOrder"] = Value<UnitResourceHaulOrder>(em, e); data["SkirmishAttemptOwnedComponent"] = Value<SkirmishAttemptOwnedComponent>(em, e);
            data["SkirmishUnitRoleComponent"] = Value<SkirmishUnitRoleComponent>(em, e);
            if (em.HasComponent<SkirmishAttemptOwnedComponent>(e)) row["stable_id"] = em.GetComponentData<SkirmishAttemptOwnedComponent>(e).StableObjectId.ToString();
            // The inherited list describes the base exporter only; keep its meaning explicit.
            row["base_exporter_unexported_component_values"] = row["unexported_component_values"];
            row.Remove("unexported_component_values"); row["replay_ready"] = false;
            return row;
        }
        static JArray Roster(EntityManager em)
        {
            var rows = new JArray(); var session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var e in entities) if (em.GetComponentData<SkirmishAttemptOwnedComponent>(e).SessionId.Equals(session.SessionId)) rows.Add(Actor(em, e));
            Require(rows.Count == 52, "Exact S002 roster of 52 required"); return rows;
        }
        static JObject Stamp(EntityManager em, string boundary)
        {
            var session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            var grids = new JArray();
            using (var gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>()))
            using (var gridEntities = gridQuery.ToEntityArray(Allocator.Temp))
                foreach (var e in gridEntities)
                {
                    var gridRow = new JObject { ["entity"] = EntityId(e), ["config"] = Value<GridConfig>(em, e), ["mask_payload_captured"] = false };
                    if (em.HasComponent<DynamicBlockerComponent>(e)) { var b = em.GetComponentData<DynamicBlockerComponent>(e); gridRow["storage_epoch"] = b.StorageEpoch; gridRow["applied_storage_epoch"] = b.AppliedStorageEpoch; gridRow["applied_revision"] = b.AppliedRevision; gridRow["write_in_progress"] = b.WriteInProgress; }
                    if (em.HasComponent<DynamicOccupancyComponent>(e)) gridRow["occupied_grid_size"] = em.GetComponentData<DynamicOccupancyComponent>(e).GridSize;
                    grids.Add(gridRow);
                }
            return new JObject { ["grids"] = grids, ["boundary"] = boundary, ["engine_frame"] = Time.frameCount,
                ["world_elapsed_seconds"] = em.World.Time.ElapsedTime, ["world_delta_time"] = em.World.Time.DeltaTime,
                ["session_id"] = session.SessionId.ToString(), ["session_phase"] = session.Phase.ToString(), ["tick_clock"] = session.TickClock,
                ["objective_clock"] = Value<SkirmishObjectiveClockComponent>(em, sessionEntity),
                ["logical_player_stock"] = Value<SkirmishEconomyStockComponent>(em, sessionEntity),
                ["logical_enemy_stock"] = Value<SkirmishEnemyStockComponent>(em, sessionEntity),
                ["resolved_setup"] = Value<SkirmishResolvedSetupComponent>(em, sessionEntity),
                ["shared_supply_initialized"] = em.HasComponent<SkirmishSharedSupplyInitialized>(sessionEntity) };
        }
        static JArray StorageCensus(EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
            Require(query.CalculateEntityCount() <= StorageLimit, "Global storage census bound exceeded");
            using var entities = query.ToEntityArray(Allocator.Temp); var rows = new JArray();
            foreach (var e in entities) rows.Add(new JObject { ["id"] = Id(e), ["entity"] = EntityId(e),
                ["entity_enabled"] = em.IsEnabled(e), ["value"] = Value<BuildingResourceStorageComponent>(em, e) });
            return rows;
        }
        static S002PhysicalStartupObservation.ForwardTraceBuffers Begin(EntityManager em, float actualDeltaTime, S002PhysicalStartupObservation.ForwardGridFacts facts)
        {
            if (completed || armed || !verifiedMove || !AriaCommandEvidence.Active) return default;
            try
            {
                if (!FindSession(em)) return default;
                var session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
                Require(AriaCommandEvidence.Violations == 0, "Unverified normal command receipt observed");
                if (session.CatalogId.ToString() != "S002" || session.SpawnComplete == 0 || !em.HasComponent<SkirmishSharedSupplyInitialized>(sessionEntity)) return default;
                output = S002StartupAllocatorCapture.OutputPath(Environment.GetEnvironmentVariable("WARLINE_S002_OFFLINE_STARTUP_CAPTURE"));
                observedWorld = em.World; frame = Time.frameCount;
                trace = new JObject { ["schema_version"] = 1, ["scope"] = "one-grid-dispatch-and-ground-fuel-boundary", ["offline_parity"] = false,
                    ["parallel_execution_order_known"] = false, ["full_frame_derived"] = false, ["consumer_storage_tail_order_proven"] = false };
                trace["before_grid_dispatch"] = Stamp(em, "before-grid-job-dispatch"); trace["actual_grid_delta_time_bits"] = Bits(actualDeltaTime);
                trace["before_grid_roster"] = Roster(em);
                // This readonly count is checked against the native job's observed dense query indices.
                using var moves = em.CreateEntityQuery(new EntityQueryDesc {
                    All = new[] { ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitPathFollow>(), ComponentType.ReadOnly<UnitVehicleKinematics>(),
                        ComponentType.ReadOnly<UnitMove>(), ComponentType.ReadOnly<UnitPathRange>(), ComponentType.ReadOnly<UnitFootprint>(), ComponentType.ReadOnly<UnitMovementBehavior>(), ComponentType.ReadOnly<UnitVehicleMovement>(), ComponentType.ReadOnly<Faction>() },
                    None = new[] { ComponentType.ReadOnly<EngageTarget>(), ComponentType.ReadOnly<UnitDeathAnimationComponent>(), ComponentType.ReadOnly<UnitAirMovement>(), ComponentType.ReadOnly<CampaignMissionStationaryUnitTag>() } });
                expectedMoveCount = moves.CalculateEntityCount();
                if (expectedMoveCount == 0) return default;
                unitOrder.Clear(); storageVisits.Clear(); fuelEntities.Clear(); Array.Clear(visitOrdinals, 0, visitOrdinals.Length);
                Require(expectedMoveCount <= UnitLimit, "Movement query exceeds bounded trace slots");
                trace["readonly_move_query_count_assertion"] = expectedMoveCount;
                using var cleanupQuery = em.CreateEntityQuery(new EntityQueryDesc {
                    All = new[] { ComponentType.ReadOnly<UnitVehicleKinematics>(), ComponentType.ReadOnly<UnitPathFollow>(), ComponentType.ReadOnly<UnitPathRange>() },
                    None = new[] { ComponentType.ReadOnly<EngageTarget>(), ComponentType.ReadOnly<UnitDeathAnimationComponent>(), ComponentType.ReadOnly<UnitAirMovement>() } });
                expectedCleanupCount = cleanupQuery.CalculateEntityCount(); Require(expectedCleanupCount <= UnitLimit, "Cleanup query exceeds bounded slots");
                CaptureGridFacts(em, facts);
                steps = new NativeArray<S002PhysicalStartupObservation.ForwardVehicleStep>(UnitLimit, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                cleanupSteps = new NativeArray<S002PhysicalStartupObservation.ForwardCleanupStep>(UnitLimit, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                armed = true; return new S002PhysicalStartupObservation.ForwardTraceBuffers { Movement = steps, Cleanup = cleanupSteps };
            }
            catch (Exception ex) { Fail(ex); Release(); completed = true; return default; }
        }
        static int[] PoseBits(LocalTransform pose, UnitVehicleKinematics k) => new[] {
            Bits(pose.Position.x), Bits(pose.Position.y), Bits(pose.Position.z), Bits(pose.Rotation.value.x),
            Bits(pose.Rotation.value.y), Bits(pose.Rotation.value.z), Bits(pose.Rotation.value.w), Bits(pose.Scale), Bits(k.CurrentSpeed), Bits(k.StallSeconds) };
        static JObject Buffer(string name, byte[] bytes)
        {
            Require(!pendingBuffers.ContainsKey(name), "Duplicate forward buffer"); pendingBuffers.Add(name, bytes);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return new JObject { ["path"] = name, ["bytes"] = bytes.Length,
                ["sha256"] = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() };
        }
        static void CaptureGridFacts(EntityManager em, S002PhysicalStartupObservation.ForwardGridFacts facts)
        {
            Require(facts.Grid.Width == 2048 && facts.Grid.Height == 1024, "Exact S002 full map dimensions required");
            int cells = checked(facts.Grid.Width * facts.Grid.Height);
            Require(facts.Walkable.Length == cells && facts.Roads.Length == cells && facts.Sidewalks.Length == cells && facts.DirtRoads.Length == cells &&
                facts.DynamicBlocked.Length == cells && facts.Occupied.Length == cells && facts.FriendlyPass.Length == cells, "Actual grid buffer dimensions differ");
            Require(facts.Pool.Length <= 1048576 && facts.LiveEntities.Length <= 8192 && facts.LiveEntities.Length == facts.LiveGrids.Length && facts.LiveEntities.Length == facts.LiveFootprints.Length, "Borrowed grid facts bounds differ");
            pendingBuffers.Clear();
            var walkable = new byte[cells]; var roads = new byte[cells]; var sidewalks = new byte[cells]; var dirt = new byte[cells];
            var blocked = new byte[cells]; var occupied = new byte[cells]; var friendly = new byte[cells];
            for (int i = 0; i < cells; i++) { walkable[i] = facts.Walkable[i].Value; roads[i] = facts.Roads[i].Value;
                sidewalks[i] = facts.Sidewalks[i].Value; dirt[i] = facts.DirtRoads[i].Value; blocked[i] = (byte)(facts.DynamicBlocked.IsSet(i) ? 1 : 0);
                occupied[i] = (byte)(facts.Occupied.IsSet(i) ? 1 : 0); friendly[i] = facts.FriendlyPass[i]; }
            Require(BitConverter.IsLittleEndian, "Explicit little-endian path export required");
            var pool = new byte[checked(facts.Pool.Length * 8)];
            for (int i = 0; i < facts.Pool.Length; i++) { Array.Copy(BitConverter.GetBytes(facts.Pool[i].x), 0, pool, i * 8, 4); Array.Copy(BitConverter.GetBytes(facts.Pool[i].y), 0, pool, i * 8 + 4, 4); }
            var buffers = new JObject { ["walkable"] = Buffer("forward-walkable.u8", walkable), ["road"] = Buffer("forward-road.u8", roads),
                ["sidewalk"] = Buffer("forward-sidewalk.u8", sidewalks), ["dirt"] = Buffer("forward-dirt.u8", dirt), ["dynamic_blocked"] = Buffer("forward-blocked.u8", blocked),
                ["occupied"] = Buffer("forward-occupied.u8", occupied), ["friendly_pass"] = Buffer("forward-friendly.u8", friendly), ["path_pool_i32le_xy"] = Buffer("forward-path-pool.bin", pool) };
            var live = new JArray();
            for (int i = 0; i < facts.LiveEntities.Length; i++)
            {
                var e = facts.LiveEntities[i]; var row = new JObject { ["entity"] = EntityId(e), ["query_id"] = Id(e), ["index"] = i,
                    ["grid"] = JObject.Parse(JsonUtility.ToJson(facts.LiveGrids[i])), ["footprint"] = JObject.Parse(JsonUtility.ToJson(facts.LiveFootprints[i])) };
                var lookups = new JObject { ["AutoWanderMoveTag"] = Value<AutoWanderMoveTag>(em, e), ["UnitTarget"] = Value<UnitTarget>(em, e),
                    ["UnitLongDistanceMove"] = Value<UnitLongDistanceMove>(em, e), ["ManualMoveGroupMemberTag"] = Value<ManualMoveGroupMemberTag>(em, e),
                    ["ManualMoveOrderTag"] = Value<ManualMoveOrderTag>(em, e), ["CampaignMissionGuidedMoveInProgressTag"] = Value<CampaignMissionGuidedMoveInProgressTag>(em, e),
                    ["UnitTransportBoardingTarget"] = Value<UnitTransportBoardingTarget>(em, e) };
                row["lookups"] = lookups; live.Add(row);
            }
            trace["before_grid_borrowed_facts"] = new JObject { ["grid"] = JObject.Parse(JsonUtility.ToJson(facts.Grid)), ["buffers"] = buffers,
                ["path_pool_count"] = facts.Pool.Length, ["full_live_units_snapshot"] = live, ["live_query_snapshot_complete"] = true,
                ["cleanup_readonly_count_assertion"] = expectedCleanupCount, ["cleanup_scheduled_after_move"] = true, ["global_ecb_playback_proven"] = false };
        }
        static void FuelUnit(EntityManager em, Entity e)
        {
            if (!armed || completed) return;
            try { Require(fuelEntities.Count < UnitLimit, "Global fuel query bound exceeded"); fuelEntities.Add(e); unitOrder.Add(Actor(em, e)); }
            catch (Exception ex) { Fail(ex); }
        }
        static void StorageVisit(EntityManager em, Entity e, int faction, float remaining)
        {
            if (!armed || completed) return;
            try { Require(storageVisits.Count < VisitLimit, "Actual drain visit bound exceeded"); storageVisits.Add(new JObject { ["faction"] = faction,
                ["ordinal"] = visitOrdinals[faction]++, ["id"] = Id(e), ["remaining_before_bits"] = Bits(remaining) }); }
            catch (Exception ex) { Fail(ex); }
        }
        static void Requested(NativeArray<float> requested)
        {
            if (!armed || completed) return;
            try { var bits = new JArray(); foreach (var value in requested) bits.Add(Bits(value)); trace["requested_bits_assertion"] = bits; }
            catch (Exception ex) { Fail(ex); }
        }
        static void Boundary(EntityManager em, string boundary, bool steelPush, int expandedCount, bool legacyExpandedRouting)
        {
            if (!armed || completed) return;
            try
            {
                Require(ReferenceEquals(observedWorld, em.World) && Time.frameCount == frame, "Forward trace escaped its World/frame");
                if (boundary == "before-ground-fuel-consumer")
                {
                    // Read dependencies already required by the synchronous fuel query.
                    // UnitGridMoveJob writes UnitGrid; completion also proves its trace writes finished.
                    em.CompleteDependencyBeforeRO<UnitGrid>();
                    em.CompleteDependencyBeforeRO<UnitVehicleKinematics>();
                    trace["actual_consumer_scope"] = new JObject { ["steel_push"] = steelPush, ["expanded_session_count"] = expandedCount, ["legacy_expanded_routing_active"] = legacyExpandedRouting };
                    trace["before_fuel"] = Stamp(em, boundary); trace["before_fuel_roster"] = Roster(em);
                    trace["global_storage_census_before"] = StorageCensus(em);
                    var calls = new JArray(); int seen = 0;
                    for (int i = 0; i < steps.Length; i++)
                    {
                        var step = steps[i]; if (step.Seen == 0) continue;
                        Require(step.QueryIndex == i && i == seen++, "Native query trace indices are not dense");
                        var row = new JObject { ["entity"] = EntityId(step.Entity), ["query_index"] = i, ["utility_called"] = step.Called != 0 };
                        if (step.Called != 0) { row["before_bits"] = new JArray(PoseBits(step.Before, step.KinematicsBefore)); row["after_bits_assertion"] = new JArray(PoseBits(step.After, step.KinematicsAfter));
                            row["direction_bits"] = new JArray(Bits(step.Direction.x), Bits(step.Direction.y), Bits(step.Direction.z)); row["before"] = JObject.Parse(JsonUtility.ToJson(step.Before)); row["after_assertion"] = JObject.Parse(JsonUtility.ToJson(step.After));
                            row["kinematics_before"] = JObject.Parse(JsonUtility.ToJson(step.KinematicsBefore)); row["kinematics_after_assertion"] = JObject.Parse(JsonUtility.ToJson(step.KinematicsAfter));
                            row["movement"] = JObject.Parse(JsonUtility.ToJson(step.Movement)); row["direction"] = JObject.Parse(JsonUtility.ToJson(step.Direction));
                            row["threshold_bits"] = Bits(step.Threshold); row["speed_bits"] = Bits(step.Speed); row["delta_time_bits"] = Bits(step.DeltaTime); row["max_distance_bits"] = Bits(step.MaxDistance); row["moved_assertion"] = step.Moved != 0; }
                        calls.Add(row);
                    }
                    Require(seen == expectedMoveCount, "Movement query census/indexed slots differ"); trace["grid_job_query_order_and_utility_calls"] = calls;
                    var cleanup = new JArray(); int cleanupSeen = 0;
                    for (int i = 0; i < cleanupSteps.Length; i++) { var step = cleanupSteps[i]; if (step.Seen == 0) continue;
                        Require(step.QueryIndex == i && i == cleanupSeen++, "Cleanup query trace indices are not dense");
                        cleanup.Add(new JObject { ["entity"] = EntityId(step.Entity), ["query_index"] = i }); }
                    Require(cleanupSeen == expectedCleanupCount, "Cleanup query census/indexed slots differ"); trace["cleanup_query_order"] = cleanup;
                }
                else if (boundary == "after-ground-fuel-consumer")
                {
                    bool nonIdle = false;
                    for (int i = 0; i < steps.Length; i++)
                    {
                        var step = steps[i];
                        if (step.Called != 0 && step.Moved != 0 && !step.Before.Position.Equals(step.After.Position)) nonIdle = true;
                    }
                    if (failure == null && !nonIdle) { idleIntervals++; Release(); trace = null; return; }
                    trace["non_idle_vehicle_utility_interval"] = nonIdle;
                    trace["idle_intervals_skipped"] = idleIntervals;
                    trace["accepted_normal_commands"] = commands.DeepClone();
                    trace["completed_touch_releases"] = AriaCommandEvidence.CompletedTouches;
                    trace["accepted_command_count"] = AriaCommandEvidence.AcceptedCommands;
                    trace["command_receipt_violations"] = AriaCommandEvidence.Violations;
                    trace["production_boundary_history"] = productionHistory.DeepClone();
                    trace["production_history_starts_at_first_observed_s002_session"] = true;
                    trace["earlier_production_derived"] = false;
                    trace["after_fuel_assertion"] = Stamp(em, boundary); trace["after_fuel_roster_assertion"] = Roster(em);
                    trace["global_storage_census_after_assertion"] = StorageCensus(em); trace["exact_fuel_query_rows_in_iteration_order"] = unitOrder;
                    trace["actual_storage_query_visits_in_iteration_order"] = storageVisits;
                    var states = new JArray(); foreach (var e in fuelEntities) states.Add(new JObject { ["id"] = Id(e), ["fuel_state_assertion"] = Value<UnitFuelConsumptionState>(em, e) });
                    trace["fuel_query_post_states_assertion"] = states; trace["capture_failure"] = failure == null ? JValue.CreateNull() : new JValue(failure);
                    trace["capture_complete"] = failure == null; Flush(); completed = true; Release();
                }
                else throw new InvalidOperationException("Unknown native trace boundary");
            }
            catch (Exception ex) { Fail(ex); if (boundary == "after-ground-fuel-consumer") { completed = true; Release(); } }
        }
        static void Fail(Exception ex) { if (failure == null) failure = ex.GetType().Name + ": " + ex.Message; if (trace != null) trace["capture_failure"] = failure; }
        static void Flush()
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(trace.ToString(Newtonsoft.Json.Formatting.None) + "\n");
            Require(bytes.Length <= 32 * 1024 * 1024, "Forward trace JSON byte bound exceeded");
            foreach (var buffer in pendingBuffers) { using var binary = new FileStream(Path.Combine(output, buffer.Key), FileMode.CreateNew, FileAccess.Write); binary.Write(buffer.Value, 0, buffer.Value.Length); }
            using var stream = new FileStream(Path.Combine(output, "forward-interval.json"), FileMode.CreateNew, FileAccess.Write); stream.Write(bytes, 0, bytes.Length);
        }
        static void Release()
        {
            try
            {
                if (!steps.IsCreated && !cleanupSteps.IsCreated) { armed = false; pendingBuffers.Clear(); return; }
                // Explicit owner disposal; never dispose while the grid job can write.
                if (observedWorld != null && observedWorld.IsCreated) { observedWorld.EntityManager.CompleteDependencyBeforeRO<UnitGrid>(); observedWorld.EntityManager.CompleteDependencyBeforeRO<UnitVehicleKinematics>(); }
                if (steps.IsCreated) steps.Dispose(); if (cleanupSteps.IsCreated) cleanupSteps.Dispose(); armed = false; pendingBuffers.Clear();
            }
            catch (Exception ex) { Fail(ex); } // Retain allocation if completion cannot be proven.
        }
    }
}
#endif
