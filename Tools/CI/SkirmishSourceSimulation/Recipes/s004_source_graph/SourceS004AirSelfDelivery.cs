using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime.TransportPose;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Explicit single-AirSelf source diagnostic. Mixed carrier/AirSelf activity
    // is not admitted; this does not establish native scene or renderer parity.
    internal sealed class SourceS004AirSelfDelivery
    {
        readonly SourceProductionDeliveryOwner owner;
        readonly SourceUnitPrefab prefab;
        readonly string root, report, reportSha;
        readonly Dictionary<int, SourceAirSelfFlightProjection> active = new Dictionary<int, SourceAirSelfFlightProjection>();
        internal int ActiveCount => active.Count;
        internal bool AnyActive => active.Count != 0;
        internal bool HasActive(int id) => active.ContainsKey(id);
        internal bool Admits(SourceUnitPrefab candidate) => ReferenceEquals(prefab, candidate);

        internal SourceS004AirSelfDelivery(SourceProductionDeliveryOwner owner, SourceUnitPrefab prefab,
            string root, string report, string reportSha)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.report = report ?? throw new ArgumentNullException(nameof(report));
            this.reportSha = reportSha ?? throw new ArgumentNullException(nameof(reportSha));
            // Validate the exact source token and report before paid admission.
            _ = new SourceS004AirSelfAuthoredPose(root, report, reportSha, prefab);
        }

        internal bool TryEnsure(RuntimeBuildingEntity building, SourcePendingProduction pending, float now, ref uint random)
        {
            if (building == null || pending == null || !Admits(pending.TransportPrefab) ||
                pending.TransportMode != BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode.AirSelf)
                throw new InvalidOperationException("Exact source AirSelf pending required");
            if (active.ContainsKey(building.Id)) return true;
            // Original lane/presentation conflicts for mixed transports remain unqualified.
            if (active.Count != 0 || owner.Helicopters == null || owner.Helicopters.AnyActive) return false;
            var world = owner.World; var em = world.Em;
            if (!world.Buildings.TryGetValue(building.Id, out var registered) || !ReferenceEquals(building, registered) ||
                !em.Exists(prefab.Template) || !em.HasComponent<UnitFootprint>(prefab.Template))
                throw new InvalidOperationException("Actual registered producer and source unit footprint required");
            byte faction = !building.HasOwnerFaction || building.OwnerFactionId == FactionIdentity.NeutralFactionId
                ? FactionIdentity.PlayerFactionId : building.OwnerFactionId;
            int2 footprint = em.GetComponentData<UnitFootprint>(prefab.Template).Size;
            JArray slotReservations = ReadHelipadSlotReservations(faction);
            bool found = world.TryResolveAvailableFactionHelipadSpawn(world.SpawnContext, faction, building, em, world.GridEntity,
                world.Grid, em.GetComponentData<DynamicBlockerComponent>(world.GridEntity), footprint, ref random, out var launchCell, out var position);
            if (!found) throw new NotSupportedException("Original no-helipad producer presentation fallback is not source-qualified");
            var touchdown = new float3(position.x, math.max(.5f, position.y), position.z);
            var geometry = SourceAirSelfLaunchGeometry.Resolve(touchdown, false, float3.zero, float3.zero, 0, pending.TransportMaxConcurrent);
            var pose = new SourceS004AirSelfAuthoredPose(root, report, reportSha, prefab);
            pose.SetRoot(geometry.Entry, geometry.Rotation);
            float hold = math.max(.5f, pending.TransportHoldForNextReadySeconds);
            float departure = math.max(.5f, pending.TransportArrivalSeconds);
            SourceAirSelfFlightProjection flight = null;
            var completion = new SourceAirSelfCompletionOwnerBoundary(owner, building, b => Remove(b));
            flight = new SourceAirSelfFlightProjection(building, prefab, pose, completion,
                geometry.Entry, geometry.Hover, geometry.Touchdown, geometry.Rotation, geometry.Rotation,
                pending.TransportArrivalSeconds, now,
                (float time, ref uint state) => ContinueWithoutReady(flight, hold, time),
                time => Depart(flight, departure, time), Publish);
            active.Add(building.Id, flight); Publish();
            owner.RecordDeliveryEvent("airself_launch", now, building.Id, pending.ProductionIndex, pending.Prefab?.name,
                launchCell, geometry.Touchdown, true, Entity.Null, true, pending.ReceiptId);
            owner.DeliveryEvents[owner.DeliveryEvents.Count - 1]["source_helipad_slot_reservations_before_resolver"] = slotReservations;
            return true;
        }

        JArray ReadHelipadSlotReservations(byte faction)
        {
            var rows = new JArray(); var world = owner.World; var em = world.Em;
            if (!world.BoundaryRead(out Entity boundary) || !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary) ||
                world.SpawnContext.ProductionSlotSystem == null)
                throw new NotSupportedException("Actual source helipad reservation readmodel required");
            foreach (var point in em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary, true))
            {
                if (point.FactionId != faction || point.BuildingId.ToString() != SpawnableKeyText.Normalize("Building_Helipad") ||
                    !world.Buildings.TryGetValue(point.BuildingRuntimeId, out var building) || building == null) continue;
                var ids = new JArray();
                if (building.PendingProductions != null) foreach (var queued in building.PendingProductions)
                    if (queued != null && queued.ReservedProductionSlotIndex == point.SlotIndex) ids.Add(queued.ReceiptId);
                rows.Add(new JObject { ["building_id"] = point.BuildingRuntimeId, ["slot_index"] = point.SlotIndex,
                    ["cell_x"] = point.Cell.x, ["cell_y"] = point.Cell.y,
                    ["original_reserved_by_pending"] = world.SpawnContext.ProductionSlotSystem.IsProductionSlotReservedByPending(building, point.SlotIndex),
                    ["pending_receipt_ids"] = ids });
            }
            return rows;
        }

        void ContinueWithoutReady(SourceAirSelfFlightProjection flight, float hold, float now)
        {
            var soon = owner.Production.FindNextSoonTransportPending(flight.Building.PendingProductions, flight.Prefab, now, hold);
            if (soon == null && now >= flight.HoverEnteredAt + hold) { flight.Phase = 2; flight.PhaseStartedAt = now; }
        }
        void Depart(SourceAirSelfFlightProjection flight, float departure, float now)
        {
            float t = math.clamp((now - flight.PhaseStartedAt) / departure, 0f, 1f);
            // Original AirSelf exit position and rotation equal hover position and rotation.
            flight.Pose.SetRoot(PoseMath.Lerp(flight.Hover, flight.Hover, t), PoseMath.Slerp(flight.HoverRotation, flight.HoverRotation, t));
            if (t >= 1f) { Remove(flight.Building); flight.Present = false; }
        }
        void Remove(RuntimeBuildingEntity building)
        {
            if (building == null || !active.Remove(building.Id)) throw new InvalidOperationException("Actual AirSelf transport missing");
        }
        internal void Advance(float now, float delta)
        {
            var ids = new List<int>(active.Keys);
            for (int i = ids.Count - 1; i >= 0; i--)
                if (active.TryGetValue(ids[i], out var flight)) flight.Tick(now, delta, ref owner.RandomState);
        }
        void Publish()
        {
            var em = owner.World.Em;
            if (!owner.World.BoundaryRead(out Entity boundary) || !em.HasComponent<BuildingProductionDeliveryReadModel>(boundary)) return;
            var current = em.GetComponentData<BuildingProductionDeliveryReadModel>(boundary);
            int count = active.Count + (owner.Helicopters?.ActiveCount ?? 0);
            if (current.ActiveManagedDeliveryCount == count) return;
            current.ActiveManagedDeliveryCount = count; current.Version++;
            em.SetComponentData(boundary, current);
        }
    }
}
