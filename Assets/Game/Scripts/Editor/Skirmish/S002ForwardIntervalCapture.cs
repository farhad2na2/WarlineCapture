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
            S002PhysicalStartupObservation.StorageWriter = StorageWriter;
            S002PhysicalStartupObservation.InitialHaulerDispatch = CaptureInitialHaulerDispatch;
            AriaCommandEvidence.Recorded += Command;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            AssemblyReloadEvents.beforeAssemblyReload += DetachStorageWriter;
            EditorApplication.quitting += Release;
            EditorApplication.quitting += DetachStorageWriter;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingPlayMode) { Release(); DetachStorageWriter(); } };
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
                productionHistory.Add(new JObject { ["observer_ordinal"] = StorageOrdinal(), ["stamp"] = Stamp(em, boundary), ["seconds_per_day_bits"] = Bits(secondsPerDay),
                    ["delta_time_bits"] = Bits(deltaTime), ["oil_ratio_bits"] = Bits(oilRatio),
                    ["readonly_query_snapshot"] = rows, ["actual_loop_order_proven"] = false });
            }
            catch (Exception ex) { Fail(ex); completed = true; Release(); }
        }
        const int InitialCensusByteLimit = 400 * 1024, InitialCensusWorkLimit = 1280;
        static JObject initialHaulerDispatch;
        static bool initialHaulerDispatchTaken;
        static int initialHaulerBytes, initialHaulerWork, initialHaulerProviderLimit;
        static World initialHaulerWorld;
        static readonly JArray initialHaulerProviders = new JArray();
        static void CaptureInitialHaulerProvider(S002PhysicalStartupObservation.InitialHaulerProviderFacts facts)
        {
            if (completed || failure != null || initialHaulerDispatch == null) return;
            try
            {
                Require(ReferenceEquals(initialHaulerWorld, facts.Manager.World), "Initial provider escaped dispatch World");
                Require(initialHaulerProviders.Count < initialHaulerProviderLimit, "Initial provider planned event bound exceeded");
                Require(initialHaulerBytes + 512 <= InitialCensusByteLimit && storageWriterBytes + 512 <= StorageJournalBytes,
                    "Initial provider pre-copy byte budget exceeded");
                Require(initialHaulerWork + 1 <= InitialCensusWorkLimit && storageWriterWork + 1 <= StorageWorkLimit,
                    "Initial provider pre-copy work budget exceeded");
                long ordinal = StorageOrdinal(); initialHaulerWork++;
                var row = new JObject { ["observer_ordinal"] = ordinal, ["kind"] = facts.Kind, ["callsite"] = facts.Callsite,
                    ["unit"] = EntityId(facts.Unit), ["result"] = facts.Result.HasValue ? new JValue(facts.Result.Value) : JValue.CreateNull() };
                if (facts.Kind == "focus-return") { Require(facts.Building != null, "Actual focus return building missing");
                    row["runtime_building_id"] = facts.Building.Id; row["position_bits"] = new JArray(unchecked((uint)Bits(facts.Position.x)), unchecked((uint)Bits(facts.Position.y)), unchecked((uint)Bits(facts.Position.z))); }
                if (facts.Kind == "ai-resolver-return") { row["faction"] = facts.Faction; row["resolver_present"] = facts.ResolverPresent;
                    row["has_input"] = facts.HasInput; row["input"] = new JObject { ["PlannedMaterialsCost"] = facts.Input.PlannedMaterialsCost,
                        ["AvailableMaterials"] = facts.Input.AvailableMaterials, ["MaterialsCapacity"] = facts.Input.MaterialsCapacity,
                        ["StoredFuelBarrelsBits"] = unchecked((uint)Bits(facts.Input.StoredFuelBarrels)), ["FuelStorageCapacity"] = facts.Input.FuelStorageCapacity }; }
                int bytes = System.Text.Encoding.UTF8.GetByteCount(row.ToString(Newtonsoft.Json.Formatting.None)) + 1;
                Require(bytes <= 512, "Initial provider row allowance exceeded");
                storageWriterBytes += bytes; initialHaulerBytes += bytes; initialHaulerProviders.Add(row);
                if (facts.Kind == "selection-exit") { Require(facts.Result.HasValue, "First selection threw before actual return");
                    initialHaulerDispatch["provider_returns_complete"] = true; }
            }
            catch (Exception ex) { Fail(new InvalidOperationException(ex.Message + StorageFailureMetrics())); completed = true; S002PhysicalStartupObservation.InitialHaulerProvider = null; }
        }
        static JToken InitialOwned(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity) || !em.HasComponent<SkirmishAttemptOwnedComponent>(entity)) return JValue.CreateNull();
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
            string session = owned.SessionId.ToString(), stable = owned.StableObjectId.ToString();
            Require(new FixedString64Bytes(session).Equals(owned.SessionId) && new FixedString64Bytes(stable).Equals(owned.StableObjectId), "Owned fixedstrings must roundtrip losslessly");
            return new JObject { ["SessionId"] = session, ["StableObjectId"] = stable, ["FactionId"] = owned.FactionId,
                ["IsStructure"] = owned.IsStructure, ["CapacityReleased"] = owned.CapacityReleased, ["ReservationId"] = owned.ReservationId };
        }
        static JObject InitialHauler(EntityManager em, Entity entity, int ordinal)
        {
            Require(em.Exists(entity), "Initial hauler disappeared from actual query");
            string sourceKey = null;
            if (em.HasComponent<UnitSourcePrefabKey>(entity)) { var key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
                sourceKey = key.ToString(); Require(new FixedString64Bytes(sourceKey).Equals(key), "Source key must roundtrip losslessly"); }
            return new JObject { ["query_ordinal"] = ordinal, ["entity"] = EntityId(entity), ["source_prefab_key"] = sourceKey,
                ["UnitResourceHauler"] = StorageComponent<UnitResourceHauler>(em, entity), ["UnitGrid"] = StorageComponent<UnitGrid>(em, entity),
                ["UnitMove"] = StorageComponent<UnitMove>(em, entity), ["Faction"] = StorageComponent<Faction>(em, entity),
                ["UnitFootprint"] = StorageComponent<UnitFootprint>(em, entity), ["UnitHealth"] = StorageComponent<UnitHealth>(em, entity),
                ["UnitDeathAnimationComponent"] = StorageComponent<UnitDeathAnimationComponent>(em, entity),
                ["ManualMoveOrderTag"] = StorageComponent<ManualMoveOrderTag>(em, entity), ["UnitAirMovement"] = StorageComponent<UnitAirMovement>(em, entity),
                ["UnitResourceHaulOrder"] = StorageComponent<UnitResourceHaulOrder>(em, entity), ["UnitResourceHaulReservation"] = StorageComponent<UnitResourceHaulReservation>(em, entity),
                ["UnitResourceHaulStatus"] = StorageComponent<UnitResourceHaulStatus>(em, entity), ["UnitTarget"] = StorageComponent<UnitTarget>(em, entity),
                ["UnitPathRequest"] = StorageComponent<UnitPathRequest>(em, entity), ["UnitPathFollow"] = StorageComponent<UnitPathFollow>(em, entity),
                ["UnitPathRange"] = StorageComponent<UnitPathRange>(em, entity), ["UnitPathRetryCooldown"] = StorageComponent<UnitPathRetryCooldown>(em, entity),
                ["UnitLongDistanceMove"] = StorageComponent<UnitLongDistanceMove>(em, entity), ["EngageTarget"] = StorageComponent<EngageTarget>(em, entity),
                ["AutoWanderMoveTag"] = StorageComponent<AutoWanderMoveTag>(em, entity), ["LocalTransform"] = StorageComponent<LocalTransform>(em, entity),
                ["UnitVehicleKinematics"] = StorageComponent<UnitVehicleKinematics>(em, entity), ["UnitFuelConsumptionState"] = StorageComponent<UnitFuelConsumptionState>(em, entity),
                ["UnitPrevWorldPos"] = StorageComponent<UnitPrevWorldPos>(em, entity), ["attempt_owned"] = InitialOwned(em, entity),
                ["SkirmishUnitRoleComponent"] = StorageComponent<SkirmishUnitRoleComponent>(em, entity) };
        }
        static JObject InitialCandidate(EntityManager em, int key, RuntimeBuildingEntity building, int ordinal)
        {
            if (building == null) return new JObject { ["enumeration_ordinal"] = ordinal, ["dictionary_key"] = key, ["runtime"] = JValue.CreateNull() };
            var definition = building.Definition;
            Require(definition == null || definition.DisplayName == null || definition.DisplayName.Length <= 256, "Candidate definition label bound exceeded");
            JObject fields = definition == null ? null : new JObject { ["display_name"] = definition.DisplayName,
                ["footprint"] = new JArray(definition.FootprintCells.x, definition.FootprintCells.y), ["role"] = (int)definition.Role,
                ["has_runway"] = definition.HasRunway, ["runway_local_position"] = StorageBits(definition.RunwayLocalPosition),
                ["runway_local_rotation"] = StorageBits(definition.RunwayLocalRotation), ["runway_half_extents"] = StorageBits(definition.RunwayHalfExtents),
                ["oil_capacity"] = definition.OilStorageCapacity, ["fuel_capacity"] = definition.FuelStorageCapacity,
                ["oil_rate_bits"] = unchecked((uint)Bits(definition.OilBarrelsPerDay)), ["fuel_rate_bits"] = unchecked((uint)Bits(definition.FuelBarrelsPerDay)),
                ["material_fabrication_enabled"] = definition.MaterialFabricationEnabled };
            return new JObject { ["enumeration_ordinal"] = ordinal, ["dictionary_key"] = key, ["runtime_id"] = building.Id,
                ["combat_entity"] = EntityId(building.CombatEntity), ["is_destroyed"] = building.IsDestroyed,
                ["has_owner_faction"] = building.HasOwnerFaction, ["owner_faction"] = building.OwnerFactionId,
                ["origin_cell"] = new JArray(building.OriginCell.x, building.OriginCell.y), ["definition_fields"] = fields,
                ["instance_present"] = building.Instance != null,
                ["instance_position_fields"] = building.Instance == null ? JValue.CreateNull() : StorageBits(building.Instance.transform.position),
                ["storage"] = StorageComponent<BuildingResourceStorageComponent>(em, building.CombatEntity),
                ["production_profile"] = StorageComponent<BuildingResourceProductionProfileComponent>(em, building.CombatEntity),
                ["fabrication_input_tag"] = StorageComponent<MaterialFabricationInputTag>(em, building.CombatEntity),
                ["fabrication"] = StorageComponent<MaterialFabricationComponent>(em, building.CombatEntity), ["attempt_owned"] = InitialOwned(em, building.CombatEntity) };
        }
        static void CaptureInitialHaulerDispatch(S002PhysicalStartupObservation.InitialHaulerDispatchFacts facts)
        {
            if (completed || failure != null || initialHaulerDispatchTaken) return;
            try
            {
                if (!FindSession(facts.Manager)) return;
                initialHaulerDispatchTaken = true; S002PhysicalStartupObservation.InitialHaulerDispatch = null;
                // Only the concrete source Dictionary/List are observed; never invoke a provider/resolver.
                var buildings = facts.Context.RuntimeBuildings as Dictionary<int, RuntimeBuildingEntity>;
                Require(buildings != null && facts.Haulers != null, "Complete concrete initial collections unavailable");
                int haulers = facts.Haulers.Count, candidates = buildings.Count;
                Require(haulers == facts.ActualQueryCount && haulers > 0 && haulers <= 512 && candidates <= 4096, "Complete initial census count bound/membership mismatch");
                long work = 16L + 32L * haulers + 16L * candidates, allowance = 8192L + 8192L * haulers + 4096L * candidates;
                // One first selector: failure can visit source/destination loops twice; success oil-cycle visits at most three.
                // Thus <=4*C+2 actual focus returns +one resolver result +entry/exit. No callback is invoked to estimate this.
                long providerBound = 4L * candidates + 5, plannedWork = work + providerBound, plannedBytes = allowance + 512L * providerBound;
                string counts = " H=" + haulers + " C=" + candidates + " census_work=" + work + " provider_bound=" + providerBound + " planned_work=" + plannedWork + " planned_bytes=" + plannedBytes;
                Require(providerBound <= 256 && plannedWork <= InitialCensusWorkLimit && storageWriterWork + work <= StorageWorkLimit && 2700L + plannedWork <= StorageWorkLimit,
                    "Initial census conditional prior-interval admission/work exceeded" + counts);
                Require(plannedBytes <= InitialCensusByteLimit && storageWriterBytes + plannedBytes <= StorageJournalBytes,
                    "Initial census pre-copy combined byte budget exceeded" + counts);
                initialHaulerProviderLimit = checked((int)providerBound); initialHaulerWorld = facts.Manager.World;
                storageWriterWork += checked((int)work) - 1; long ordinal = StorageOrdinal();
                var units = new JArray(); var universe = new JArray();
                for (int i = 0; i < haulers; i++) { var row = InitialHauler(facts.Manager, facts.Haulers[i], i);
                    Require(System.Text.Encoding.UTF8.GetByteCount(row.ToString(Newtonsoft.Json.Formatting.None)) + 1 <= 8192, "Initial hauler row allowance exceeded"); units.Add(row); }
                int index = 0; foreach (var pair in buildings) { var row = InitialCandidate(facts.Manager, pair.Key, pair.Value, index++);
                    Require(System.Text.Encoding.UTF8.GetByteCount(row.ToString(Newtonsoft.Json.Formatting.None)) + 1 <= 4096, "Initial candidate row allowance exceeded"); universe.Add(row); }
                Require(index == candidates && facts.Haulers.Count == haulers && buildings.Count == candidates, "Initial census collection changed");
                var snapshot = new JObject { ["schema_version"] = 2, ["observer_ordinal"] = ordinal,
                    ["scope"] = "InitialActualDispatchCensusOnly_SelectorProvidersAndSchedulerPending", ["stamp"] = CompactStorageStamp(facts.Manager, "initial-hauler-dispatch"),
                    ["now_argument_bits"] = unchecked((uint)Bits(facts.Now)), ["unity_time_observation_bits"] = unchecked((uint)Bits(Time.time)),
                    ["unity_delta_observation_bits"] = unchecked((uint)Bits(Time.deltaTime)), ["has_pending_path_job"] = facts.HasPendingPathJob,
                    ["grid"] = StorageBits(facts.Grid), ["actual_query_count"] = haulers, ["actual_candidate_count"] = candidates,
                    ["private_state"] = new JObject { ["has_signature"] = facts.HasSignature, ["last_signature"] = facts.LastSignature,
                        ["next_refresh_at_bits"] = unchecked((uint)Bits(facts.NextRefreshAt)), ["next_reservation_id"] = facts.NextReservationId,
                        ["approach_query_created"] = facts.ApproachQueryCreated, ["ai_policy_present"] = facts.AIPolicyPresent },
                    ["callback_presence"] = new JObject { ["focus"] = facts.Context.ResolveBuildingFocusWorldPosition != null,
                        ["effective_rect"] = facts.Context.GetEffectivePlacementRect != null, ["ai_input"] = facts.Context.TryResolveFactionAIOilAllocationInput != null },
                    ["full_hauler_query_order_and_components"] = units, ["full_runtime_buildings_enumeration"] = universe,
                    ["census_complete"] = true, ["full_caller_inputs_complete"] = false,
                    ["provider_returns"] = initialHaulerProviders, ["provider_returns_complete"] = false,
                    ["conditional_interval_admission"] = new JObject { ["prior_other_work_estimate"] = 2700,
                        ["census_work"] = work, ["provider_event_bound"] = providerBound, ["planned_combined_work"] = plannedWork,
                        ["planned_combined_bytes"] = plannedBytes, ["scope"] = "prior observed interval estimate only; not future completion guarantee" },
                    ["unsupported_facts"] = new JArray("private AI input cache contents unobserved; require derived cache-clear before consuming cache",
                        "first-selection actual focus/AI resolver returns are scoped external-provider facts; producer not derived", "service-rect/buildPlaneY binding and later provider calls unresolved",
                        "caller-time approach masks/live-footprint query and queue/ECB state not captured", "owner scheduler realtime/due/private clocks not in this dispatch Context") };
                int bytes = System.Text.Encoding.UTF8.GetByteCount(snapshot.ToString(Newtonsoft.Json.Formatting.None));
                Require(bytes <= allowance && bytes <= InitialCensusByteLimit, "Initial census actual serialized budget exceeded");
                storageWriterBytes += bytes; initialHaulerBytes = bytes; initialHaulerWork = checked((int)work); initialHaulerDispatch = snapshot;
                S002PhysicalStartupObservation.InitialHaulerProvider = CaptureInitialHaulerProvider;
            }
            catch (Exception ex) { Fail(new InvalidOperationException(ex.Message + StorageFailureMetrics())); completed = true; S002PhysicalStartupObservation.InitialHaulerDispatch = null; S002PhysicalStartupObservation.InitialHaulerProvider = null; }
        }
        const int StorageCallLimit = 512, StorageJournalBytes = 1024 * 1024, StorageWorkLimit = 4096;
        static readonly JArray storageWriterJournal = new JArray();
        static int storageWriterCalls, storageWriterWork, storageWriterBytes;
        static string storageLastStage = "none";
        static readonly Dictionary<string, int> storageStageCounts = new Dictionary<string, int>();
        static string StorageFailureMetrics()
        {
            return " storage_metrics calls=" + storageWriterCalls + " events=" + storageWriterJournal.Count +
                " work=" + storageWriterWork + " bytes=" + storageWriterBytes +
                " fuel_boundaries=" + storageCausalBoundaries.Count +
                " facts=" + S002PhysicalStartupObservation.StorageFactAllocations +
                " readonly_sync_skipped=" + S002PhysicalStartupObservation.StorageReadonlySyncSkipped +
                " unparented_writes=" + S002PhysicalStartupObservation.StorageUnparentedWrites +
                " last_stage=" + storageLastStage + " stage_counts=" + string.Join(",", storageStageCounts);
        }
        static void StorageStage(string stage)
        {
            storageLastStage = stage;
            if (!storageStageCounts.ContainsKey(stage)) Require(storageStageCounts.Count < 32, "Storage stage metric bound exceeded");
            storageStageCounts.TryGetValue(stage, out int count); storageStageCounts[stage] = count + 1;
        }
        static long storageObserverOrdinal;
        static readonly JArray storageCausalBoundaries = new JArray();
        static JObject storageCausalHeader;
        static World storageCausalWorld;
        static Entity storageCausalSessionEntity;
        static FixedString64Bytes storageCausalSessionId;
        static JObject FuelCausalHeader(string sessionId, int entityIndex, int entityVersion)
        {
            return new JObject { ["schema_version"] = 2,
                ["columns"] = new JArray("observer_ordinal", "boundary_code", "engine_frame", "tick_clock",
                    "world_elapsed_bits", "world_delta_bits", "session_phase", "mode_flags", "expanded_session_count"),
                ["boundary_codes"] = new JArray("before-ground-fuel-consumer", "after-ground-fuel-consumer"),
                ["mode_flag_bits"] = new JObject { ["steel_push"] = 1, ["legacy_expanded_routing_active"] = 2 },
                ["context"] = new JObject { ["session_id"] = sessionId,
                    ["session_entity"] = new JObject { ["index"] = entityIndex, ["version"] = entityVersion } },
                ["rows"] = storageCausalBoundaries };
        }
        static JArray FuelCausalRow(long ordinal, int boundaryCode, int engineFrame, int tickClock,
            ulong elapsedBits, uint deltaBits, byte phase, bool steelPush, int expandedCount, bool legacy)
        {
            return new JArray(ordinal, boundaryCode, engineFrame, tickClock, elapsedBits, deltaBits,
                phase, (steelPush ? 1 : 0) | (legacy ? 2 : 0), expandedCount);
        }

        static JObject CompactStorageStamp(EntityManager em, string boundary)
        {
            // Read scalar fields directly; do not construct the expanded production Stamp.
            var session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            return new JObject { ["boundary"] = boundary, ["engine_frame"] = Time.frameCount,
                ["session_entity"] = EntityId(sessionEntity), ["session_id"] = session.SessionId.ToString(),
                ["session_phase"] = session.Phase.ToString(), ["tick_clock"] = StorageBits(session.TickClock),
                ["world_elapsed_bits"] = unchecked((ulong)BitConverter.DoubleToInt64Bits(em.World.Time.ElapsedTime)),
                ["world_delta_bits"] = unchecked((uint)Bits(em.World.Time.DeltaTime)) };
        }
        static long StorageOrdinal()
        {
            Require(storageWriterWork < StorageWorkLimit, "Causal observer work bound exceeded");
            storageWriterWork++; return storageObserverOrdinal++;
        }
        static void StorageBoundary(EntityManager em, string boundary, bool steelPush, int expandedCount, bool legacy)
        {
            if (completed || failure != null) return;
            try { if (!FindSession(em)) return;
                StorageStage(boundary);
                Require(S002PhysicalStartupObservation.StorageWriterFailure == null, "Storage source observer failed: " + S002PhysicalStartupObservation.StorageWriterFailure);
                int contextAllowance = storageCausalHeader == null ? 2048 : 0;
                Require(storageWriterBytes + contextAllowance + 95 <= StorageJournalBytes, "Causal event pre-copy byte budget exceeded");
                var session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
                if (storageCausalHeader == null)
                {
                    storageCausalWorld = em.World; storageCausalSessionEntity = sessionEntity;
                    storageCausalSessionId = session.SessionId;
                    var header = FuelCausalHeader(session.SessionId.ToString(), sessionEntity.Index, sessionEntity.Version);
                    int contextBytes = System.Text.Encoding.UTF8.GetByteCount(header.ToString(Newtonsoft.Json.Formatting.None));
                    Require(contextBytes <= 2048, "Causal session header exceeded pre-copy allowance");
                    storageWriterBytes += contextBytes; storageCausalHeader = header;
                }
                Require(ReferenceEquals(storageCausalWorld, em.World) && storageCausalSessionEntity == sessionEntity &&
                    storageCausalSessionId.Equals(session.SessionId), "Fuel causal session context changed");
                int boundaryCode = boundary == "before-ground-fuel-consumer" ? 0 : boundary == "after-ground-fuel-consumer" ? 1 : -1;
                Require(boundaryCode >= 0, "Unknown fuel causal boundary");
                long ordinal = StorageOrdinal();
                var row = FuelCausalRow(ordinal, boundaryCode, Time.frameCount, session.TickClock,
                    unchecked((ulong)BitConverter.DoubleToInt64Bits(em.World.Time.ElapsedTime)),
                    unchecked((uint)Bits(em.World.Time.DeltaTime)), (byte)session.Phase, steelPush, expandedCount, legacy);
                int bytes = System.Text.Encoding.UTF8.GetByteCount(row.ToString(Newtonsoft.Json.Formatting.None)) + 1;
                Require(bytes <= 95, "Causal event exceeded pre-copy allowance");
                storageWriterBytes += bytes; storageCausalBoundaries.Add(row);
            } catch (Exception ex) { Fail(new InvalidOperationException(ex.Message + StorageFailureMetrics())); completed = true; S002PhysicalStartupObservation.StorageWriter = null; }
        }
        static JToken StorageBits(object value)
        {
            if (value == null) return JValue.CreateNull();
            if (value is float scalar) return new JValue(unchecked((uint)Bits(scalar)));
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum) return JToken.FromObject(value);
            var row = new JObject();
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                row[field.Name] = StorageBits(field.GetValue(value));
            return row;
        }
        static JToken StorageComponent<T>(EntityManager em, Entity unit) where T : unmanaged, IComponentData
        {
            if (unit == Entity.Null || !em.Exists(unit) || !em.HasComponent<T>(unit)) return JValue.CreateNull();
            if (TypeManager.GetTypeInfo<T>().IsZeroSized) return new JObject();
            return StorageBits(em.GetComponentData<T>(unit));
        }
        static JObject StorageBinding(EntityManager em, RuntimeBuildingEntity building)
        {
            if (building == null) return null;
            var entity = building.CombatEntity;
            bool exists = entity != Entity.Null && em.Exists(entity);
            return new JObject { ["entity"] = EntityId(entity), ["runtime_building_id"] = building.Id,
                ["ecs_exists"] = exists, ["definition_present"] = building.Definition != null,
                ["runtime_metadata"] = new JObject { ["OwnerFactionId"] = building.OwnerFactionId,
                    ["OilStorageCapacity"] = building.OilStorageCapacity, ["FuelStorageCapacity"] = building.FuelStorageCapacity,
                    ["OilBarrelsPerDay"] = unchecked((uint)Bits(building.OilBarrelsPerDay)),
                    ["FuelBarrelsPerDay"] = unchecked((uint)Bits(building.FuelBarrelsPerDay)),
                    ["StoredOilBarrels"] = unchecked((uint)Bits(building.StoredOilBarrels)),
                    ["StoredFuelBarrels"] = unchecked((uint)Bits(building.StoredFuelBarrels)) },
                ["storage"] = StorageComponent<BuildingResourceStorageComponent>(em, entity),
                ["profile"] = StorageComponent<BuildingResourceProductionProfileComponent>(em, entity) };
        }
        static void StorageWriter(S002PhysicalStartupObservation.StorageWriterFacts facts)
        {
            if (completed || failure != null) return;
            try
            {
                if (!FindSession(facts.Manager)) return;
                StorageStage(facts.Stage);
                // Admission occurs BEFORE component/runtime metadata snapshots or JSON field copies.
                int stores = facts.Source == null ? 0 : 1;
                if (facts.Destination != null && !ReferenceEquals(facts.Source, facts.Destination)) stores++;
                if (facts.Building != null) stores = 1;
                Require(stores <= 2, "Storage transaction store bound exceeded");
                bool unit = facts.Unit != Entity.Null && facts.Manager.Exists(facts.Unit);
                int components = unit ? (facts.Manager.HasComponent<UnitResourceHauler>(facts.Unit) ? 1 : 0)
                    + (facts.Manager.HasComponent<UnitResourceHaulOrder>(facts.Unit) ? 1 : 0)
                    + (facts.Manager.HasComponent<UnitResourceHaulReservation>(facts.Unit) ? 1 : 0) : 0;
                int cost = 1 + stores + components + (facts.Operation == "ReservePair" ? 2 : 1);
                Require(storageWriterWork + cost <= StorageWorkLimit, "Storage journal aggregate work bound exceeded");
                Require(storageWriterBytes + 8192 <= StorageJournalBytes, "Storage journal pre-copy byte budget exceeded");
                if (facts.Stage == "entry") Require(storageWriterCalls < StorageCallLimit, "Storage invocation bound exceeded");
                storageWriterWork += cost; if (facts.Stage == "entry") storageWriterCalls++;
                var row = new JObject { ["ordinal"] = storageWriterJournal.Count, ["observer_ordinal"] = StorageOrdinal(),
                    ["invocation_id"] = facts.Invocation, ["parent_invocation_id"] = facts.Parent == 0 ? JValue.CreateNull() : new JValue(facts.Parent),
                    ["stage"] = facts.Stage, ["operation"] = facts.Operation, ["callsite"] = facts.Callsite,
                    ["stamp"] = CompactStorageStamp(facts.Manager, "storage-writer-" + facts.Stage),
                    ["resource_kind"] = facts.Kind, ["amount_bits"] = unchecked((uint)Bits(facts.Amount)),
                    ["allow_deferred_source_reservation"] = facts.Deferred, ["utility_present"] = facts.UtilityPresent,
                    ["next_reservation_id"] = facts.Counter,
                    ["source"] = StorageBinding(facts.Manager, facts.Source),
                    ["destination"] = StorageBinding(facts.Manager, facts.Destination),
                    ["access_binding"] = StorageBinding(facts.Manager, facts.Building),
                    ["unit"] = new JObject { ["entity"] = unit ? EntityId(facts.Unit) : null,
                        ["hauler"] = StorageComponent<UnitResourceHauler>(facts.Manager, facts.Unit),
                        ["order"] = StorageComponent<UnitResourceHaulOrder>(facts.Manager, facts.Unit),
                        ["reservation"] = StorageComponent<UnitResourceHaulReservation>(facts.Manager, facts.Unit) },
                    ["actual_result"] = facts.Result.HasValue ? new JValue(facts.Result.Value) : JValue.CreateNull(),
                    ["returned_reservation"] = facts.Stage == "exit" && facts.Operation == "ReservePair" ? StorageBits(facts.Reservation) : JValue.CreateNull(),
                    ["actual_storage_argument"] = facts.HasStorage ? StorageBits(facts.Storage) : JValue.CreateNull(),
                    ["actual_hauler_argument"] = facts.HasHauler ? StorageBits(facts.Hauler) : JValue.CreateNull(),
                    ["replay_frontier"] = facts.Operation == "ReservePair" && facts.Parent == 0,
                    ["child_evidence_only"] = facts.Parent != 0 || facts.Operation != "ReservePair",
                    ["uncommitted_pure_leaf_outputs_captured"] = false };
                int bytes = System.Text.Encoding.UTF8.GetByteCount(row.ToString(Newtonsoft.Json.Formatting.None)) + 1;
                Require(bytes <= 8192, "Storage journal event exceeded conservative pre-copy allowance");
                storageWriterBytes += bytes; storageWriterJournal.Add(row);
            }
            catch (Exception ex) { Fail(new InvalidOperationException(ex.Message + StorageFailureMetrics())); completed = true; S002PhysicalStartupObservation.StorageWriter = null; }
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
            StorageBoundary(em, boundary, steelPush, expandedCount, legacyExpandedRouting);
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
                    trace["storage_writer_journal"] = storageWriterJournal.DeepClone();
                    trace["initial_hauler_dispatch"] = initialHaulerDispatch == null ? JValue.CreateNull() : initialHaulerDispatch.DeepClone();
                    trace["storage_causal_boundaries"] = storageCausalHeader == null ? JValue.CreateNull() : storageCausalHeader.DeepClone();
                    trace["storage_writer_frontier"] = "pair-transactions-only; childcalls-evidence-only";
                    trace["storage_writer_failure"] = S002PhysicalStartupObservation.StorageWriterFailure;
                    trace["storage_writer_metrics"] = StorageFailureMetrics();
                    Require(S002PhysicalStartupObservation.StorageWriterFailure == null, "Storage observer failure latched");
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
        static void DetachStorageWriter() { S002PhysicalStartupObservation.StorageWriter = null; S002PhysicalStartupObservation.InitialHaulerDispatch = null; S002PhysicalStartupObservation.InitialHaulerProvider = null; }
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
            finally { if (completed || failure != null) DetachStorageWriter(); }
        }
    }
}
#endif
