using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// Passive source-world snapshot after the existing owner/controller tick.
internal static class SourceS004FuelDriftReadback
{
    internal static JObject Capture(EntityManager em, Entity session, long tick)
    {
        var stores=new JArray();
        using(var q=em.CreateEntityQuery(typeof(BuildingResourceStorageComponent)))
        using(var entities=q.ToEntityArray(Allocator.Temp)) foreach(var e in entities)
        {
            var s=em.GetComponentData<BuildingResourceStorageComponent>(e);
            if(s.OwnerFactionId!=1)continue;
            stores.Add(new JObject { ["entity_index"]=e.Index,["entity_version"]=e.Version,["building_id"]=s.RuntimeBuildingId,
                ["fuel"]=s.StoredFuelBarrels,["reserved_outbound"]=s.ReservedFuelOutboundBarrels,
                ["civilian_reserve"]=s.CivilianFuelReserveBarrels,["capacity"]=s.FuelStorageCapacity,
                ["production_per_day"]=s.FuelBarrelsPerDay,["oil_per_day"]=s.OilBarrelsPerDay });
        }
        var consumers=new JArray();
        using(var q=em.CreateEntityQuery(typeof(Faction),typeof(UnitGrid),typeof(UnitMovementBehavior),typeof(UnitFuelConsumption),typeof(UnitFuelConsumptionState)))
        using(var entities=q.ToEntityArray(Allocator.Temp)) foreach(var e in entities)
        {
            if(em.GetComponentData<Faction>(e).Id!=1)continue;
            var rate=em.GetComponentData<UnitFuelConsumption>(e);var state=em.GetComponentData<UnitFuelConsumptionState>(e);
            var cell=em.GetComponentData<UnitGrid>(e).Cell;var movement=em.GetComponentData<UnitMovementBehavior>(e);
            var row=new JObject { ["entity_index"]=e.Index,["entity_version"]=e.Version,["enabled"]=(int)rate.Enabled,
                ["air"]=em.HasComponent<UnitAirMovement>(e),["vehicle"]=(int)movement.UsesVehicleMotion,
                ["ground_rate"]=rate.GroundFuelPerCell,["air_rate"]=rate.AirFuelPerCell,
                ["cell"]=new JArray(cell.x,cell.y),["last_cell"]=new JArray(state.LastCell.x,state.LastCell.y),["initialized"]=(int)state.Initialized };
            if(em.HasComponent<LocalTransform>(e)) { var p=em.GetComponentData<LocalTransform>(e).Position; row["position"]=new JArray(p.x,p.y,p.z); }
            if(em.HasComponent<SkirmishAttemptOwnedComponent>(e))row["stable_id"]=em.GetComponentData<SkirmishAttemptOwnedComponent>(e).StableObjectId.ToString();
            consumers.Add(row);
        }
        return new JObject { ["scope"]="PassiveSourceWorldAfterOwnerAndControllerTick",["tick"]=tick,
            ["reported_fuel"]=SkirmishStartingSupplyService.ReadFuel(em,session,1),["stores"]=stores,["consumers"]=consumers,
            ["native_parity"]=false,["mission_ready"]=false };
    }
}
