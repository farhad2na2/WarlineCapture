using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishObjectiveFactProjectionSystem))]
    [UpdateAfter(typeof(UnitDeathSystem))]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    [UpdateBefore(typeof(SkirmishNativeObjectiveTickSystem))]
    public partial class VehicleFieldRepairSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // GetEntityQuery returns a SystemBase-owned cached query.
            var requests=GetEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<VehicleRepairRequestComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using(var actors=requests.ToEntityArray(Allocator.Temp))
                foreach(var actor in actors)VehicleFieldRepairService.ProcessRequest(EntityManager,actor,out _);
            var active=GetEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<VehicleRepairStateComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using(var actors=active.ToEntityArray(Allocator.Temp))
                foreach(var actor in actors)VehicleFieldRepairService.Tick(EntityManager,actor,out _);
        }
    }
}
