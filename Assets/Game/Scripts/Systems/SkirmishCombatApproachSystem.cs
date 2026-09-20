using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    // The shared close-range combat motor steers directly toward its target.
    // Across the skirmish map, first use the existing interruptible path/breach
    // order to reach a firing position, then hand back to that combat motor.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(UnitEngagedMovementSystem))]
    public partial struct SkirmishCombatApproachSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishMatchState>();
            state.RequireForUpdate<GridConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if(SystemAPI.GetSingleton<SkirmishMatchState>().Phase!=SkirmishPhase.Playing)return;
            var em=state.EntityManager;
            using var grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent));
            if(grids.CalculateEntityCount()!=1)return;
            var gridEntity=grids.GetSingletonEntity();
            var grid=em.GetComponentData<GridConfig>(gridEntity);
            ResumeReadyApproaches(em, grid);
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            var surface = surfaces.CalculateEntityCount() == 1 ? surfaces.GetSingleton<MapSurfaceComponent>() : default;
            using var query=em.CreateEntityQuery(new EntityQueryDesc{
                All=new[]{ComponentType.ReadOnly<EngageTarget>(),ComponentType.ReadOnly<UnitAttack>(),ComponentType.ReadOnly<UnitFootprint>(),ComponentType.ReadOnly<LocalTransform>(),ComponentType.ReadOnly<UnitMove>()},
                None=new[]{ComponentType.ReadOnly<UnitAirMovement>(),ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),ComponentType.ReadOnly<UnitDeathAnimationComponent>(),ComponentType.ReadOnly<HoldPositionOrderTag>(),ComponentType.ReadOnly<BaseBreachOrder>()}});
            using var units=query.ToEntityArray(Allocator.Temp);
            foreach(var unit in units)
            {
                var engage=em.GetComponentData<EngageTarget>(unit);
                if(!em.Exists(engage.Target)||!em.HasComponent<LocalTransform>(engage.Target))continue;
                var target=em.GetComponentData<LocalTransform>(engage.Target).Position;
                var origin=em.GetComponentData<LocalTransform>(unit).Position;
                var footprint = em.GetComponentData<UnitFootprint>(unit);
                var size = footprint.Size;
                var behavior = em.HasComponent<UnitMovementBehavior>(unit) ? em.GetComponentData<UnitMovementBehavior>(unit) : default;
                bool isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, behavior);
                var targetSize=em.HasComponent<UnitFootprint>(engage.Target)?em.GetComponentData<UnitFootprint>(engage.Target).Size:new int2(1);
                float range=em.GetComponentData<UnitAttack>(unit).Range+
                    (math.cmax(size)-1+math.cmax(targetSize)-1)*grid.CellSize*.5f;
                if(range<=0||math.distance(origin.xz,target.xz)<=range)continue;
                if(!TryFindApproach(grid,em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray(),
                    em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked,origin,target,size,range,out var goal,surface,isVehicle))continue;
                // This existing order is cleared by Move/Hold/new Attack, so the
                // approach cannot resume after the player gives a different order.
                bool advancing = em.HasComponent<AttackMoveOrder>(unit);
                var advance = advancing ? em.GetComponentData<AttackMoveOrder>(unit) : default;
                if(!UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em,unit,goal))continue;
                if (advancing) em.AddComponentData(unit, advance);
                em.RemoveComponent<EngageTarget>(unit);
                em.AddComponentData(unit,new BaseBreachOrder{
                    FinalTarget=engage.Target,FinalCell=goal,FinalPosition=target,
                    Stage=BaseBreachOrder.StageMovingToFinalTarget,IsCommanded=engage.IsCommanded});
            }
        }

        internal static void ResumeReadyApproaches(EntityManager em, GridConfig grid)
        {
            using var query = em.CreateEntityQuery(typeof(BaseBreachOrder), typeof(UnitAttack), typeof(LocalTransform));
            using var units = query.ToEntityArray(Allocator.Temp);
            foreach (var unit in units)
            {
                var order = em.GetComponentData<BaseBreachOrder>(unit);
                // Only the simple Skirmish firing approach; real gate-breach orders
                // keep their existing staged behavior and player overrides cancel this component.
                if (order.BreachTarget != Entity.Null || order.Stage != BaseBreachOrder.StageMovingToFinalTarget) continue;
                bool alive = em.Exists(order.FinalTarget) && em.HasComponent<LocalTransform>(order.FinalTarget) &&
                    (!em.HasComponent<UnitHealth>(order.FinalTarget) || em.GetComponentData<UnitHealth>(order.FinalTarget).Current > 0);
                float3 target = alive ? em.GetComponentData<LocalTransform>(order.FinalTarget).Position : default;
                if (alive)
                {
                    var size = em.HasComponent<UnitFootprint>(unit) ? em.GetComponentData<UnitFootprint>(unit).Size : new int2(1);
                    var targetSize = em.HasComponent<UnitFootprint>(order.FinalTarget) ? em.GetComponentData<UnitFootprint>(order.FinalTarget).Size : new int2(1);
                    float range = em.GetComponentData<UnitAttack>(unit).Range +
                        (math.cmax(size) - 1 + math.cmax(targetSize) - 1) * grid.CellSize * .5f;
                    if (range <= 0 || math.distance(em.GetComponentData<LocalTransform>(unit).Position.xz, target.xz) > range) continue;
                }
                // A moving target can enter range before the old destination is
                // reached. Stop walking now; a dead target must not leave a stale Move.
                em.RemoveComponent<UnitPathRequest>(unit);
                em.RemoveComponent<UnitPathFollow>(unit);
                em.RemoveComponent<UnitPathRange>(unit);
                em.RemoveComponent<UnitPathRetryCooldown>(unit);
                em.RemoveComponent<UnitTarget>(unit);
                em.RemoveComponent<ManualMoveOrderTag>(unit);
                em.RemoveComponent<ManualMoveGroupMemberTag>(unit);
                em.RemoveComponent<BaseBreachOrder>(unit);
                if (alive)
                {
                    var engage = new EngageTarget { Target = order.FinalTarget, Cell = GridUtils.WorldToCell(grid, target),
                        Position = target, IsCommanded = order.IsCommanded };
                    if (em.HasComponent<EngageTarget>(unit)) em.SetComponentData(unit, engage);
                    else em.AddComponentData(unit, engage);
                }
            }
        }

        internal static bool TryFindApproach(GridConfig grid,NativeArray<GridWalkable> walkable,
            NativeBitArray blocked,float3 origin,float3 target,int2 size,float range,out int2 goal, MapSurfaceComponent surface = default, bool isVehicle = false)
        {
            goal=default;float best=float.MaxValue;
            var validation = new Pathfinding.MapSurfaceTraversalValidation();
            float angle=math.atan2(origin.z-target.z,origin.x-target.x);
            for(int i=0;i<32;i++)
            {
                float a=angle+i*math.PI/16;
                var point=target+new float3(math.cos(a),0,math.sin(a))*math.max(grid.CellSize,range-2*grid.CellSize);
                var cell=GridUtils.WorldToCell(grid,point);
                var min=UnitFootprintUtility.GetMinCell(cell,size);
                bool free=true;
                for(int y=0;y<size.y&&free;y++)for(int x=0;x<size.x;x++)
                {
                    var c=min+new int2(x,y);
                    if(!GridUtils.InBounds(c,grid.Width,grid.Height)){free=false;break;}
                    int index=GridUtils.CellToIndex(c,grid.Width);
                    if(walkable[index].Value==0||blocked.IsSet(index)){free=false;break;}
                }
                free &= validation.CanTraverseFootprint(surface, surface.HasSurfaceData, grid, cell, size, isVehicle);
                float distance=math.distancesq(origin.xz,point.xz);
                if(!free||distance>=best)continue;
                goal=cell;best=distance;
            }
            return best<float.MaxValue;
        }
    }
}
