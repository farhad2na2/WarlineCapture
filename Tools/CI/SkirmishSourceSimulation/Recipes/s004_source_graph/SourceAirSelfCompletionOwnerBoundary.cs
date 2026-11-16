using System;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    // Diagnostic composition seam. The caller must supply the actual source
    // transport teardown owner; this type does not create or admit transports.
    internal sealed class SourceAirSelfCompletionOwnerBoundary : ISourceAirSelfCompletionView
    {
        readonly SourceProductionDeliveryOwner owner;
        readonly RuntimeBuildingEntity building;
        readonly Action<RuntimeBuildingEntity> destroyActualTransport;

        internal SourceAirSelfCompletionOwnerBoundary(SourceProductionDeliveryOwner owner,
            RuntimeBuildingEntity building, Action<RuntimeBuildingEntity> destroyActualTransport)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.building = building ?? throw new ArgumentNullException(nameof(building));
            this.destroyActualTransport = destroyActualTransport ?? throw new ArgumentNullException(nameof(destroyActualTransport));
            if (!owner.World.Buildings.TryGetValue(building.Id, out var registered) || !ReferenceEquals(building, registered))
                throw new InvalidOperationException("Same-world registered producer required");
        }

        void Require(RuntimeBuildingEntity value)
        {
            if (!ReferenceEquals(value, building) || !owner.World.Buildings.TryGetValue(building.Id, out var registered) || !ReferenceEquals(value, registered))
                throw new InvalidOperationException("Producer identity changed during AirSelf completion");
        }

        public SourcePendingProduction FindReady(RuntimeBuildingEntity b, SourceUnitPrefab prefab, float now)
        { Require(b); return owner.Production.FindNextReadyTransportPending(b.PendingProductions, prefab, now); }
        public int2 ResolveCell(float3 touchdown) => GridUtils.WorldToCell(owner.World.Grid, touchdown);
        public bool TrySpawn(RuntimeBuildingEntity b, int index, int slot, float3 position, int2 cell, ref uint random, SourcePendingProduction pending)
        {
            Require(b);
            if (pending == null || pending.ProductionIndex != index || pending.ReservedProductionSlotIndex != slot)
                throw new InvalidOperationException("Pending production identity changed");
            bool spawned = owner.World.TrySpawn(b, pending, position, cell, ref random, out Entity member);
            if (spawned)
            {
                owner.DeliveredMembers++;
                bool observed = SourceProductionDeliveryOwner.TryReadSpawnPlacement(owner.World.Em, member, out var actualCell, out var actualPosition);
                owner.RecordDeliveryEvent(observed ? "unit_spawned_and_placed" : "unit_spawned_without_placement_readback", owner.World.Now,
                    b.Id, pending.ProductionIndex, pending.Prefab?.name, actualCell, actualPosition, true, member, observed, pending.ReceiptId);
            }
            else owner.RecordDeliveryEvent("unit_spawn_failed", owner.World.Now, b.Id, pending.ProductionIndex,
                pending.Prefab?.name, new int2(-1, -1), default, false, Entity.Null, false, pending.ReceiptId);
            return spawned;
        }
        public bool Remove(RuntimeBuildingEntity b, SourcePendingProduction pending)
        { Require(b); return owner.Production.RemovePendingProduction(b.PendingProductions, pending); }
        public void Rebuild(RuntimeBuildingEntity b, float now, bool preserve)
        { Require(b); owner.Production.RebuildPendingProductionTimeline(b.PendingProductions, now, preserve); }
        public void Align(RuntimeBuildingEntity b, float3 forward)
        { Require(b); owner.World.SourceAlignNewestProducedUnitRotation(owner.QueueContext, b, new Vector3(forward.x, forward.y, forward.z)); }
        public void Release(SourcePendingProduction pending) => owner.SourceReleasePending(pending);
        public void Destroy(RuntimeBuildingEntity b) { Require(b); destroyActualTransport(b); }
    }
}
