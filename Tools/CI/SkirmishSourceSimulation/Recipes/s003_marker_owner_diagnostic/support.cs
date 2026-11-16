using System;
using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    // Diagnostic source-host orchestration of the original marker jobs. The
    // boundary is private to this observer and is never passed to the planner.
    internal sealed class SourceS003MarkerOwnerCensus
    {
        readonly EntityManager em;
        readonly Entity session;
        readonly DynamicBuffer<MatchHudMinimapMarkerElement> markers = new DynamicBuffer<MatchHudMinimapMarkerElement>(Array.Empty<MatchHudMinimapMarkerElement>());
        double nextRefresh;
        double previousTime = -1;
        int markerGeneration, comparedMarkerGeneration = -1;
        uint comparedContactGeneration;

        internal SourceS003MarkerOwnerCensus(EntityManager em, Entity session)
        { this.em = em ?? throw new ArgumentNullException(nameof(em)); this.session = session; }

        internal void Write(long tick, int frame, float time)
        {
            SourceGroundMissionFamily.RequireSession(em, session, true);
            using var campaign = em.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));
            if (!campaign.IsEmptyIgnoreFilter) throw new NotSupportedException("Campaign marker roster is outside this census.");
            double worldTime = em.WorldUnmanaged.Time.ElapsedTime;
            if (double.IsNaN(worldTime) || double.IsInfinity(worldTime) || worldTime < 0 || worldTime > float.MaxValue)
                throw new InvalidOperationException("Finite source-world marker clock required.");
            if (worldTime < previousTime) throw new InvalidOperationException("Marker clock moved backwards.");
            previousTime = worldTime;
            bool refreshed = worldTime >= nextRefresh;
            if (!refreshed) {
                var prune = new OriginalMarkerCollectorProbe.PruneDeadMarkersJob {
                    Health = new ComponentLookup<UnitHealth>(em),
                    FogOwned = new ComponentLookup<SkirmishAttemptOwnedComponent>(em),
                    FogSight = new ComponentLookup<SkirmishContactSightComponent>(em),
                    FogKnowledge = new BufferLookup<SkirmishContactKnowledgeElement>(em),
                    Now = (float)worldTime, Markers = markers
                };
                prune.Execute();
                markerGeneration = checked(markerGeneration + 1);
                Report(tick, frame, time, worldTime, false);
                return;
            }
            nextRefresh = worldTime + Game.Simulation.AriaPublicPlayerMarkerPolicy.RefreshIntervalSeconds;
            int max = Game.Simulation.AriaPublicPlayerMarkerPolicy.MaxMarkers;
            var job = new OriginalMarkerCollectorProbe.CollectMarkersJob {
                MaxMarkers = max, CollectMode = 1, Now = (float)worldTime,
                Represented = new NativeParallelHashSet<OriginalMarkerCollectorProbe.ContactKey>(max, Allocator.Temp),
                ProducedUnits = new NativeParallelHashSet<Entity>(max, Allocator.Temp),
                Markers = new NativeList<MatchHudMinimapMarkerElement>(max, Allocator.Temp),
                RuntimeBuildingLookup = new ComponentLookup<RuntimeBuildingCombatInfo>(em),
                MapBuildingLookup = new ComponentLookup<OperationMapBuildingComponent>(em),
                MissionRoleLookup = new ComponentLookup<CampaignMissionUnitRoleComponent>(em),
                LastSeenLookup = new ComponentLookup<ScanIntelLastSeen>(em),
                CombatSuppressionLookup = new ComponentLookup<CampaignMissionCombatSuppressedTag>(em),
                FogOwned = new ComponentLookup<SkirmishAttemptOwnedComponent>(em),
                FogDisabled = new ComponentLookup<Disabled>(em),
                FogPassenger = new ComponentLookup<UnitTransportPassenger>(em),
                FogPassengerOwner = new ComponentLookup<SupportPassengerOwnerComponent>(em),
                FogSight = new ComponentLookup<SkirmishContactSightComponent>(em),
                FogKnowledge = new BufferLookup<SkirmishContactKnowledgeElement>(em)
            };
            try {
                using var query = em.CreateEntityQuery(new EntityQueryDesc {
                    All = new[] { ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<Faction>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities
                });
                using var entities = query.ToEntityArray(Allocator.Temp);
                for (byte mode = 1; mode <= 2; mode++) {
                    job.CollectMode = mode;
                    foreach (var entity in entities) {
                        var health = em.GetComponentData<UnitHealth>(entity);
                        var transform = em.GetComponentData<LocalTransform>(entity);
                        var faction = em.GetComponentData<Faction>(entity);
                        job.Execute(entity, in health, in transform, in faction);
                    }
                }
                var scan = new OriginalMarkerCollectorProbe.CollectScanIntelMarkersJob {
                    MaxMarkers = max, Markers = job.Markers, FogOwned = job.FogOwned
                };
                using (var scanQuery = em.CreateEntityQuery(new EntityQueryDesc {
                    All = new[] { ComponentType.ReadOnly<ScanIntelRevealedTag>(), ComponentType.ReadOnly<ScanIntelLastSeen>() },
                    None = new[] { ComponentType.ReadOnly<UnitHealth>() }
                })) using (var scanEntities = scanQuery.ToEntityArray(Allocator.Temp)) {
                    foreach (var entity in scanEntities) {
                        var lastSeen = em.GetComponentData<ScanIntelLastSeen>(entity);
                        scan.Execute(entity, in lastSeen);
                    }
                }
                var frozen = new OriginalMarkerCollectorProbe.CollectFrozenLedgerMarkersJob {
                    MaxMarkers = max, Now = (float)worldTime, Represented = job.Represented, Markers = job.Markers
                };
                using (var ledgerQuery = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent),
                    typeof(SkirmishFogStateComponent), typeof(SkirmishContactLedgerElement), typeof(SkirmishContactLedgerStateComponent)))
                using (var ledgerEntities = ledgerQuery.ToEntityArray(Allocator.Temp)) {
                    foreach (var entity in ledgerEntities) {
                        var expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                        var fog = em.GetComponentData<SkirmishFogStateComponent>(entity);
                        var ledger = em.GetBuffer<SkirmishContactLedgerElement>(entity);
                        var ledgerState = em.GetComponentData<SkirmishContactLedgerStateComponent>(entity);
                        frozen.Execute(in expanded, in fog, in ledger, in ledgerState);
                    }
                }
                var publish = new OriginalMarkerCollectorProbe.PublishMarkersJob { MaxMarkers = max, Source = job.Markers, Destination = markers };
                publish.Execute();
                markerGeneration = checked(markerGeneration + 1);
                Report(tick, frame, time, worldTime, true);
            } finally { job.Markers.Dispose(); job.Represented.Dispose(); job.ProducedUnits.Dispose(); }
        }

        internal void ComparePlayers(SourcePublicPlayerContactOwner.Snapshot snapshot, long tick, int frame)
        {
            if (markerGeneration == comparedMarkerGeneration && snapshot.Generation == comparedContactGeneration) return;
            comparedMarkerGeneration = markerGeneration;
            comparedContactGeneration = snapshot.Generation;
            var counts = new Dictionary<(int, int, int), int>();
            int original = 0;
            for (int i = 0; i < markers.Length; i++) {
                var row = markers[i];
                if (row.FactionId != 1) continue;
                var key = (math.asint(row.Position.x), math.asint(row.Position.y), math.asint(row.Position.z));
                counts.TryGetValue(key, out int count); counts[key] = checked(count + 1); original++;
            }
            int matched = 0;
            foreach (var position in snapshot.Players) {
                var key = (math.asint(position.X), math.asint(position.Y), math.asint(position.Z));
                if (counts.TryGetValue(key, out int count) && count > 0) { counts[key] = count - 1; matched++; }
            }
            var fresh = snapshot.Frame == frame ? FreshPlayerComparison(snapshot) : null;
            Console.WriteLine("[S003OriginalMarkerComparison] " + new JObject {
                ["tick"] = tick, ["frame"] = frame, ["original_count"] = original,
                ["logical_count"] = snapshot.Players.Length, ["exact_multiset_matched"] = matched,
                ["original_only"] = original - matched, ["logical_only"] = snapshot.Players.Length - matched,
                ["marker_generation"] = markerGeneration, ["logical_generation"] = snapshot.Generation,
                ["logical_marker_frame"] = snapshot.Frame, ["logical_world_time"] = snapshot.WorldTime,
                ["same_frame_collector"] = fresh,
                ["feeds_planner"] = false, ["native_presentation"] = false, ["mission_ready"] = false
            }.ToString(Newtonsoft.Json.Formatting.None));
        }

        JObject FreshPlayerComparison(SourcePublicPlayerContactOwner.Snapshot snapshot)
        {
            int max = Game.Simulation.AriaPublicPlayerMarkerPolicy.MaxMarkers;
            var job = new OriginalMarkerCollectorProbe.CollectMarkersJob {
                MaxMarkers = max, CollectMode = 1, Now = (float)em.WorldUnmanaged.Time.ElapsedTime,
                Represented = new NativeParallelHashSet<OriginalMarkerCollectorProbe.ContactKey>(max, Allocator.Temp),
                ProducedUnits = new NativeParallelHashSet<Entity>(max, Allocator.Temp),
                Markers = new NativeList<MatchHudMinimapMarkerElement>(max, Allocator.Temp),
                RuntimeBuildingLookup = new ComponentLookup<RuntimeBuildingCombatInfo>(em),
                MapBuildingLookup = new ComponentLookup<OperationMapBuildingComponent>(em),
                MissionRoleLookup = new ComponentLookup<CampaignMissionUnitRoleComponent>(em),
                LastSeenLookup = new ComponentLookup<ScanIntelLastSeen>(em),
                CombatSuppressionLookup = new ComponentLookup<CampaignMissionCombatSuppressedTag>(em),
                FogOwned = new ComponentLookup<SkirmishAttemptOwnedComponent>(em),
                FogDisabled = new ComponentLookup<Disabled>(em),
                FogPassenger = new ComponentLookup<UnitTransportPassenger>(em),
                FogPassengerOwner = new ComponentLookup<SupportPassengerOwnerComponent>(em),
                FogSight = new ComponentLookup<SkirmishContactSightComponent>(em),
                FogKnowledge = new BufferLookup<SkirmishContactKnowledgeElement>(em)
            };
            try {
                using var query = em.CreateEntityQuery(new EntityQueryDesc {
                    All = new[] { ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<Faction>() },
                    Options = EntityQueryOptions.IncludeDisabledEntities
                });
                using var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities) {
                    var health = em.GetComponentData<UnitHealth>(entity);
                    var transform = em.GetComponentData<LocalTransform>(entity);
                    var faction = em.GetComponentData<Faction>(entity);
                    job.Execute(entity, in health, in transform, in faction);
                }
                var counts = new Dictionary<(int, int, int), int>();
                for (int i = 0; i < job.Markers.Length; i++) {
                    var p = job.Markers[i].Position; var key = (math.asint(p.x), math.asint(p.y), math.asint(p.z));
                    counts.TryGetValue(key, out int count); counts[key] = checked(count + 1);
                }
                int matched = 0;
                foreach (var p in snapshot.Players) {
                    var key = (math.asint(p.X), math.asint(p.Y), math.asint(p.Z));
                    if (counts.TryGetValue(key, out int count) && count > 0) { counts[key] = count - 1; matched++; }
                }
                return new JObject { ["original_count"] = job.Markers.Length, ["logical_count"] = snapshot.Players.Length,
                    ["matched"] = matched, ["original_only"] = job.Markers.Length - matched,
                    ["logical_only"] = snapshot.Players.Length - matched,
                    ["logical_snapshot_republished_this_frame"] = true,
                    ["native_query_order_verified"] = false };
            } finally { job.Markers.Dispose(); job.Represented.Dispose(); job.ProducedUnits.Dispose(); }
        }

        void Report(long tick, int frame, float time, double worldTime, bool refreshed)
        {
                int player = 0, hostile = 0, lastSeen = 0, playerOwned = 0, playerBuilding = 0;
                for (int i = 0; i < markers.Length; i++) {
                    var row = markers[i];
                    if (row.FactionId == 1) {
                        player++;
                        if (row.SourceEntity != Entity.Null && em.HasComponent<SkirmishAttemptOwnedComponent>(row.SourceEntity)) playerOwned++;
                        if (row.SourceEntity != Entity.Null && (em.HasComponent<RuntimeBuildingCombatInfo>(row.SourceEntity) || em.HasComponent<OperationMapBuildingComponent>(row.SourceEntity))) playerBuilding++;
                    } else hostile++;
                    if (row.IsLastSeen != 0) lastSeen++;
                }
                Console.WriteLine("[S003OriginalMarkerCensus] " + new JObject {
                    ["tick"] = tick, ["frame"] = frame, ["time"] = time,
                    ["source_world_time"] = worldTime,
                    ["player"] = player, ["hostile"] = hostile, ["last_seen"] = lastSeen,
                    ["player_attempt_owned"] = playerOwned, ["player_building"] = playerBuilding,
                    ["refreshed"] = refreshed, ["source_host_orchestration"] = true,
                    ["native_scheduler_executed"] = false, ["native_query_order_verified"] = false,
                    ["rendered_presentation"] = false, ["mission_ready"] = false
                }.ToString(Newtonsoft.Json.Formatting.None));
        }
    }
}
