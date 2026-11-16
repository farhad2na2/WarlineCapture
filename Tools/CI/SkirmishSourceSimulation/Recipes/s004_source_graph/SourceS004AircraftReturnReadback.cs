using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

// Read-only same-world evidence; does not issue a return or claim its acceptance.
internal static class SourceS004AircraftReturnReadback
{
    internal static JObject Capture(EntityManager em, Entity session, Entity boundary, int producerId)
    {
        var rows = new JArray();
        if (em.Exists(boundary) && em.HasBuffer<BuildingProducedUnitReadModel>(boundary))
            foreach (var produced in em.GetBuffer<BuildingProducedUnitReadModel>(boundary, true))
            {
                if (produced.BuildingRuntimeId != producerId || !em.Exists(produced.Unit)) continue;
                var actor = produced.Unit;
                var row = new JObject { ["entity_index"] = actor.Index, ["entity_version"] = actor.Version,
                    ["source_key"] = produced.UnitSourceKey.ToString(), ["producer_id"] = produced.BuildingRuntimeId,
                    ["slot_building_id"] = produced.ProductionSlotBuildingRuntimeId, ["slot_index"] = produced.ProductionSlotIndex,
                    ["produced_has_owner_faction"] = produced.HasOwnerFaction != 0, ["produced_owner_faction"] = (int)produced.OwnerFactionId,
                    ["active_owned_actor"] = SkirmishActorCommandAuthority.IsActiveOwnedActor(em, session, 1, actor),
                    ["has_air_state"] = em.HasComponent<UnitAirComponent>(actor) };
                if (em.HasComponent<UnitAirComponent>(actor))
                {
                    var air = em.GetComponentData<UnitAirComponent>(actor);
                    row["home_initialized"] = air.HomeInitialized != 0; row["home_cell"] = new JArray(air.HomeCell.x, air.HomeCell.y);
                    row["airborne"] = air.Airborne != 0; row["returning_home"] = air.ReturningHome != 0;
                    row["takeoff_rolling"] = air.TakeoffRolling != 0; row["landing_rolling"] = air.LandingRolling != 0;
                }
                rows.Add(row);
            }
        var points = new JArray();
        if (em.Exists(boundary) && em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary))
            foreach (var p in em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary, true))
                if (p.BuildingRuntimeId == producerId)
                    points.Add(new JObject { ["faction"] = (int)p.FactionId, ["building_id"] = p.BuildingRuntimeId,
                        ["slot"] = p.SlotIndex, ["cell"] = new JArray(p.Cell.x, p.Cell.y) });
        using var boundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
        using var queues = em.CreateEntityQuery(typeof(RespawnQueueTag), typeof(RespawnQueueComponent));
        return new JObject { ["scope"] = "ActualSameWorldReadOnly_NoReturnCommand", ["actors"] = rows,
            ["producer_spawn_points"] = points, ["building_boundary_count"] = boundaries.CalculateEntityCount(),
            ["respawn_queue_count"] = queues.CalculateEntityCount(), ["return_attempted"] = false,
            ["return_acceptance"] = JValue.CreateNull(), ["native_input"] = false, ["mission_ready"] = false };
    }
}
