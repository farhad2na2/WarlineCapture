using System;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

internal static class SourceS004AirSelfTransactionReadback
{
    internal static JObject Capture(EntityManager em, Entity session, Entity boundary,
        SourceProductionDeliveryOwner owner, int producerId, uint receiptId)
    {
        if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
            !em.HasComponent<SkirmishCapacityComponent>(session) || !em.HasBuffer<SkirmishProductionReservation>(session) ||
            !owner.World.Buildings.TryGetValue(producerId, out var producer) || producer == null)
            throw new InvalidOperationException("Actual same-world session, ledger and producer required");
        var capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
        var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
        var rows = new JArray();
        foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session, true))
            if (receiptId != 0 && receipt.ReservationId == receiptId)
                rows.Add(new JObject { ["id"] = receipt.ReservationId, ["role"] = receipt.Role.ToString(),
                    ["category"] = receipt.Category.ToString(), ["member_count"] = receipt.MemberCount,
                    ["remaining_members"] = receipt.RemainingMembers, ["delivered_members"] = receipt.DeliveredMembers,
                    ["group_id"] = receipt.ProductionGroupId, ["producer_id"] = receipt.ProducerRuntimeId,
                    ["materials_paid"] = receipt.MaterialsPaid, ["supply_cost"] = receipt.SupplyCost,
                    ["phase"] = receipt.Phase.ToString(), ["faction"] = (int)receipt.FactionId });
        var queue = new JArray();
        if (producer.PendingProductions != null) foreach (var p in producer.PendingProductions)
            if (p != null && (receiptId == 0 || p.ReceiptId == receiptId))
                queue.Add(new JObject { ["receipt_id"] = p.ReceiptId, ["production_index"] = p.ProductionIndex,
                    ["prefab"] = p.Prefab?.name, ["remaining_quantity"] = p.RemainingQuantity,
                    ["reserved_slot"] = p.ReservedProductionSlotIndex, ["ready_at"] = p.ReadyAt });
        var members = new JArray();
        using (var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent)))
        using (var entities = query.ToEntityArray(Allocator.Temp))
            foreach (var entity in entities)
            {
                var identity = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (receiptId == 0 || identity.ReservationId != receiptId || !identity.SessionId.Equals(sessionId)) continue;
                var item = new JObject { ["entity_index"] = entity.Index, ["entity_version"] = entity.Version,
                    ["faction"] = (int)identity.FactionId, ["capacity_released"] = (int)identity.CapacityReleased,
                    ["stable_id"] = identity.StableObjectId.ToString(),
                    ["source_key"] = em.HasComponent<UnitSourcePrefabKey>(entity) ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() : null,
                    ["role"] = em.HasComponent<SkirmishUnitRoleComponent>(entity) ? em.GetComponentData<SkirmishUnitRoleComponent>(entity).Role.ToString() : null };
                if (em.HasComponent<UnitGrid>(entity)) { var cell=em.GetComponentData<UnitGrid>(entity).Cell; item["cell"]=new JArray(cell.x,cell.y); }
                if (em.HasComponent<LocalTransform>(entity)) { var p=em.GetComponentData<LocalTransform>(entity).Position; item["position"]=new JArray(p.x,p.y,p.z); }
                members.Add(item);
            }
        var produced = new JArray();
        if (em.Exists(boundary) && em.HasBuffer<BuildingProducedUnitReadModel>(boundary))
            foreach (var row in em.GetBuffer<BuildingProducedUnitReadModel>(boundary, true))
                if (row.BuildingRuntimeId == producerId)
                    produced.Add(new JObject { ["producer_id"] = row.BuildingRuntimeId, ["slot_building_id"] = row.ProductionSlotBuildingRuntimeId,
                        ["production_index"] = row.ProductionIndex, ["slot_index"] = row.ProductionSlotIndex,
                        ["unit_index"] = row.Unit.Index, ["unit_version"] = row.Unit.Version, ["source_key"] = row.UnitSourceKey.ToString() });
        return new JObject { ["scope"] = "SameWorldReadOnly_ReceiptCapacityBankAndProducer_NotFullConservation",
            ["session_id"] = sessionId.ToString(), ["receipt_id"] = receiptId,
            ["materials"] = SkirmishMaterialsService.Read(em, session, 1), ["fuel"] = SkirmishStartingSupplyService.ReadFuel(em, session, 1),
            ["air_live"] = capacity.AirLive, ["air_reserved"] = capacity.AirReserved,
            ["supply_live"] = capacity.SupplyLive, ["supply_reserved"] = capacity.SupplyReserved,
            ["receipts"] = rows, ["producer_queue"] = queue, ["owned_members"] = members,
            ["producer_readmodel"] = produced, ["airself_active_count"] = owner.AirSelf?.ActiveCount ?? 0,
            ["native_parity"] = false, ["mission_ready"] = false };
    }
}
