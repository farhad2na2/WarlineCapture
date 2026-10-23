using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;
using Game.Runtime.Movement;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(UnitGridMovementSystem))]
    public partial struct UnitEngagedMovementSystem : ISystem
    {
        private EntityQuery _gridQuery;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<EngageTarget>();
            state.RequireForUpdate<GridRoad>();
            state.RequireForUpdate<GridRoadSidewalk>();
            state.RequireForUpdate<GridRoadDirt>();
            state.RequireForUpdate<DynamicBlockerComponent>();
            state.RequireForUpdate<UnitAttack>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Paused updates must not change facing, path state, or vehicle kinematics.
            if (SystemAPI.Time.DeltaTime <= 0f ||
                (SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) &&
                 gameplay.SimulationActive == 0))
                return;

            Entity gridEntity = _gridQuery.GetSingletonEntity();
            var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
            var walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            var blockerData = state.EntityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
            var roads = SystemAPI.GetBuffer<GridRoad>(gridEntity).AsNativeArray();
            var sidewalks = SystemAPI.GetBuffer<GridRoadSidewalk>(gridEntity).AsNativeArray();
            var dirtRoads = SystemAPI.GetBuffer<GridRoadDirt>(gridEntity).AsNativeArray();
            var footprintLookup = SystemAPI.GetComponentLookup<UnitFootprint>(true);
            var healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
            var staticBlockerLookup = SystemAPI.GetComponentLookup<StaticGridBlocker>(true);
            var holdPositionLookup = SystemAPI.GetComponentLookup<HoldPositionOrderTag>(true);
            var debugFireTargetLookup = SystemAPI.GetComponentLookup<DebugFireTargetTag>(true);
            var scanOrderLookup = SystemAPI.GetComponentLookup<UnitScanOrder>(true);

            var handle = new EngagedMoveJob
            {
                Grid = grid,
                Walkable = walkable,
                DynamicBlocked = blockerData.Blocked,
                FriendlyPassFactionIds = blockerData.FriendlyPassFactionIds,
                Roads = roads,
                Sidewalks = sidewalks,
                DirtRoads = dirtRoads,
                DeltaTime = SystemAPI.Time.DeltaTime,
                FootprintLookup = footprintLookup,
                HealthLookup = healthLookup,
                StaticBlockerLookup = staticBlockerLookup,
                HoldPositionLookup = holdPositionLookup,
                DebugFireTargetLookup = debugFireTargetLookup,
                ScanOrderLookup = scanOrderLookup
            }.ScheduleParallel(state.Dependency);

            state.Dependency = handle;
        }

        [BurstCompile]
        [WithNone(
            typeof(StaticGridBlocker),
            typeof(UnitDeathAnimationComponent),
            typeof(UnitAirMovement),
            typeof(CampaignMissionStationaryUnitTag))]
        internal partial struct EngagedMoveJob : IJobEntity
        {
            public GridConfig Grid;
            public float DeltaTime;

            [ReadOnly]
            public Unity.Collections.NativeArray<GridWalkable> Walkable;
            [ReadOnly] public NativeBitArray DynamicBlocked;
            [ReadOnly] public Unity.Collections.NativeArray<byte> FriendlyPassFactionIds;
            [ReadOnly]
            public Unity.Collections.NativeArray<GridRoad> Roads;
            [ReadOnly] public Unity.Collections.NativeArray<GridRoadSidewalk> Sidewalks;
            [ReadOnly] public Unity.Collections.NativeArray<GridRoadDirt> DirtRoads;
            [ReadOnly] public ComponentLookup<UnitFootprint> FootprintLookup;
            [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
            [ReadOnly] public ComponentLookup<StaticGridBlocker> StaticBlockerLookup;
            [ReadOnly] public ComponentLookup<HoldPositionOrderTag> HoldPositionLookup;
            [ReadOnly] public ComponentLookup<DebugFireTargetTag> DebugFireTargetLookup;
            [ReadOnly] public ComponentLookup<UnitScanOrder> ScanOrderLookup;

            public void Execute(
                Entity entity,
                ref LocalTransform transform,
                ref UnitGrid selfGrid,
                ref EngageTarget engage,
                ref UnitVehicleKinematics vehicleKinematics,
                in UnitCombat combat,
                in UnitAttack attack,
                in UnitMove move,
                in UnitFootprint footprint,
                in UnitMovementBehavior movementBehavior,
                in UnitVehicleMovement vehicleMovement,
                in Faction faction)
            {
                var context=new NativeContext{Job=this,Source=entity,Target=engage.Target};
                var input=new EngagedMovementInput{GridWidth=Grid.Width,GridHeight=Grid.Height,CellSize=Grid.CellSize,DeltaTime=DeltaTime,
                    AttackRange=attack.Range,AggroRangeCells=combat.AggroRangeCells,ChaseBreakDistance=combat.ChaseBreakDistance,
                    MoveSpeed=move.Speed,RoadSpeedMultiplier=move.RoadSpeedMultiplier,Footprint=Cell(footprint.Size),FactionId=faction.Id,
                    IsVehicle=UnitVehicleMovementUtility.IsVehicle(footprint,movementBehavior)};
                var movement=new EngagedMovementState{Position=Point(transform.Position),Cell=Cell(selfGrid.Cell),
                    TargetIndex=engage.Target.Index,TargetVersion=engage.Target.Version,TargetCell=Cell(engage.Cell),TargetPosition=Point(engage.Position),
                    IsCommanded=engage.IsCommanded,CurrentSpeed=vehicleKinematics.CurrentSpeed};
                var result=EngagedMovementKernel.Step(ref context,input,ref movement);
                transform.Position=Vector(movement.Position);selfGrid.Cell=IntCell(movement.Cell);
                engage.Target=new Entity{Index=movement.TargetIndex,Version=movement.TargetVersion};engage.Cell=IntCell(movement.TargetCell);
                engage.Position=Vector(movement.TargetPosition);engage.IsCommanded=movement.IsCommanded;vehicleKinematics.CurrentSpeed=movement.CurrentSpeed;
                if(result.WriteFacing)transform.Rotation=quaternion.LookRotationSafe(Vector(result.Direction),math.up());
                if(!result.DelegateVehicle)return;
                // Steering/rotation/kinematics remain the original native vehicle branch.
                // Offline contexts must reject vehicle motion until that utility is shared.
                float3 oldPosition=transform.Position;
                bool moved=UnitVehicleMovementUtility.MoveVehicle(ref transform,ref vehicleKinematics,vehicleMovement,
                    Vector(result.Direction),math.radians(8f),result.DirectMoveSpeed,DeltaTime,result.Remaining);
                if(!moved)return;
                if(!EngagedMovementKernel.CanMoveAlongSegment(ref context,input,Point(oldPosition),Point(transform.Position),Cell(footprint.Size),Cell(selfGrid.Cell),faction.Id))
                {transform.Position=oldPosition;vehicleKinematics.CurrentSpeed=0f;return;}
                int2 movedCell=GridUtils.WorldToCell(Grid,transform.Position);
                if(GridUtils.InBounds(movedCell,Grid.Width,Grid.Height))selfGrid.Cell=movedCell;
            }
            private static EngagedPosition Point(float3 p)=>new EngagedPosition(p.x,p.y,p.z);
            private static float3 Vector(EngagedPosition p)=>new float3(p.X,p.Y,p.Z);
            private static EngagedCell Cell(int2 p)=>new EngagedCell(p.x,p.y);
            private static int2 IntCell(EngagedCell p)=>new int2(p.X,p.Z);
            private struct NativeContext:IEngagedMovementContext
            {
                public EngagedMoveJob Job;public Entity Source,Target;
                public bool DebugTargetForSource()=>Job.DebugFireTargetLookup.HasComponent(Target)&&Job.DebugFireTargetLookup[Target].Source==Source;
                public bool TargetIsStatic()=>Job.StaticBlockerLookup.HasComponent(Target);
                public bool TargetHasHealth()=>Job.HealthLookup.HasComponent(Target);
                public int TargetHealth()=>Job.HealthLookup[Target].Current;
                public bool HoldingPosition()=>Job.HoldPositionLookup.HasComponent(Source);
                public bool TryReadScan(out EngagedScanOrder scan)
                {
                    scan=default;if(!Job.ScanOrderLookup.HasComponent(Source))return false;
                    var value=Job.ScanOrderLookup[Source];scan=new EngagedScanOrder{Center=Cell(value.CenterCell),RadiusCells=value.RadiusCells,HasStarted=value.HasStarted};return true;
                }
                public bool TryTargetFootprint(out EngagedCell footprint)
                {footprint=default;if(!Job.FootprintLookup.HasComponent(Target))return false;footprint=Cell(Job.FootprintLookup[Target].Size);return true;}
                public EngagedCell WorldToCell(EngagedPosition p)=>Cell(GridUtils.WorldToCell(Job.Grid,Vector(p)));
                public bool PreferredSurface(bool vehicle,int index)=>vehicle?Job.DirtRoads[index].Value!=0:Job.Sidewalks[index].Value!=0;
                public bool CanMoveIntoCell(EngagedCell target,EngagedCell footprint,EngagedCell current,byte faction)=>Job.CanMoveIntoCell(IntCell(target),IntCell(footprint),IntCell(current),faction);
            }

            private bool CanMoveIntoCell(int2 targetCell, int2 footprintSize, int2 currentCell, byte factionId)
            {
                if (targetCell.Equals(currentCell))
                    return true;

                return UnitGridMoveJob.CanOccupyMovementTarget(
                    Grid,
                    Walkable,
                    DynamicBlocked,
                    FriendlyPassFactionIds,
                    targetCell,
                    footprintSize,
                    currentCell,
                    factionId);
            }
        }
    }
}
