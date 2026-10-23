using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;
using Game.Runtime.Targeting;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(DynamicOccupancyRebuildSystem))]
    [UpdateBefore(typeof(UnitPathfindingSystem))]
    public partial struct UnitEngagementSystem : ISystem
    {
        private const double TargetAcquisitionIntervalSeconds = WorldSchedulingKernel.UnitTargetAcquisitionSeconds;
        private EntityQuery _gridQuery;
        private EntityQuery _ecbSingletonQuery;
        private EntityQuery _unitsQuery;
        private EntityQuery _acquisitionQuery;
        private EntityQuery _buildingTargetsQuery;
        private double _nextTargetAcquisitionTime;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            // ECB singletons live on system entities in the installed Entities package.
            // Excluding those entities prevents acquisition from updating at all.
            _ecbSingletonQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EndSimulationEntityCommandBufferSystem.Singleton>()
                .WithOptions(EntityQueryOptions.IncludeSystems).Build(ref state);
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<UnitGrid>();
            state.RequireForUpdate<Faction>();
            state.RequireForUpdate<UnitCombat>();
            state.RequireForUpdate<UnitAttack>();

            _unitsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<UnitHealth>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>()
                }
            });

            _acquisitionQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<UnitCombat>(),
                    ComponentType.ReadOnly<UnitAttack>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<EngageTarget>(),
                    ComponentType.ReadOnly<GroundMissileInFlightComponent>(),
                    ComponentType.ReadOnly<GroundMissileLauncherComponent>(),
                    ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>()
                }
            });

            _buildingTargetsQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<LocalTransform>() },
                Any = new[] { ComponentType.ReadOnly<StaticGridBlocker>(), ComponentType.ReadOnly<RuntimeBuildingCombatTag>() },
                None = new[] { ComponentType.ReadOnly<CampaignMissionCombatSuppressedTag>() }
            });
            state.RequireForUpdate(_ecbSingletonQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double now = SystemAPI.Time.ElapsedTime;
            if (!WorldSchedulingKernel.TryAdvanceUnitAcquisition(now, ref _nextTargetAcquisitionTime))
                return;

            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            int unitCount = _unitsQuery.CalculateEntityCount();
            if (unitCount == 0)
                return;
            if (_acquisitionQuery.IsEmptyIgnoreFilter)
                return;

            // This system already completes to dispose temp containers; complete up front so we can build
            // an "attacker count" map to spread units across multiple targets.
            state.Dependency.Complete();

            int engagedCount = SystemAPI.QueryBuilder().WithAll<EngageTarget>().Build().CalculateEntityCount();
            var attackerCounts = new NativeParallelHashMap<Entity, int>(math.max(16, engagedCount), Allocator.TempJob);
            foreach (var engage in SystemAPI.Query<RefRO<EngageTarget>>())
            {
                var t = engage.ValueRO.Target;
                if (t == Entity.Null)
                    continue;
                attackerCounts.TryGetValue(t, out int c);
                attackerCounts[t] = c + 1;
            }

            var map = new NativeParallelMultiHashMap<int, Entity>(math.max(16, unitCount * 2), Allocator.TempJob);

            var buildHandle = new BuildSpatialMapJob
            {
                Grid = grid,
                Writer = map.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            var factionLookup = SystemAPI.GetComponentLookup<Faction>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var engageLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);
            var healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
            var recentAttackerLookup = SystemAPI.GetComponentLookup<RecentAttacker>(true);
            var manualMoveLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true);
            var pathFollowLookup = SystemAPI.GetComponentLookup<UnitPathFollow>(true);
            var pathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true);
            var holdPositionLookup = SystemAPI.GetComponentLookup<HoldPositionOrderTag>(true);
            var scanOrderLookup = SystemAPI.GetComponentLookup<UnitScanOrder>(true);
            Entity ecbEntity = _ecbSingletonQuery.GetSingletonEntity();
            var ecbSystem = state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            var buildingTargets = _buildingTargetsQuery.ToEntityArray(Allocator.TempJob);
            var acquireJob = new AcquireTargetsJob
            {
                Grid = grid,
                SpatialMap = map,
                BuildingTargets = buildingTargets,
                AttackerCounts = attackerCounts,
                FactionLookup = factionLookup,
                TransformLookup = transformLookup,
                EngageLookup = engageLookup,
                HealthLookup = healthLookup,
                RecentAttackerLookup = recentAttackerLookup,
                ManualMoveLookup = manualMoveLookup,
                AttackMoveLookup = SystemAPI.GetComponentLookup<AttackMoveOrder>(true),
                PathFollowLookup = pathFollowLookup,
                PathRequestLookup = pathRequestLookup,
                HoldPositionLookup = holdPositionLookup,
                ScanOrderLookup = scanOrderLookup,
                TargetPolicyLookup = SystemAPI.GetComponentLookup<CombatTargetPolicy>(true),
                FogOwnedLookup = SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true),
                FogSightLookup = SystemAPI.GetComponentLookup<SkirmishContactSightComponent>(true),
                FogKnowledgeLookup = SystemAPI.GetBufferLookup<SkirmishContactKnowledgeElement>(true),
                TargetBuildingLookup = SystemAPI.GetComponentLookup<RuntimeBuildingCombatInfo>(true),
                ProtectedRecordsLookup = SystemAPI.GetComponentLookup<CampaignMissionProtectedRecordsTag>(true),
                TargetBlockerLookup = SystemAPI.GetComponentLookup<StaticGridBlocker>(true),
                TargetAirLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true),
                TargetMovementLookup = SystemAPI.GetComponentLookup<UnitMovementBehavior>(true),
                Ecb = ecb
            }.ScheduleParallel(buildHandle);

            var mapDisposeHandle = map.Dispose(buildingTargets.Dispose(acquireJob));
            state.Dependency = attackerCounts.Dispose(mapDisposeHandle);
        }

        [BurstCompile]
        [WithNone(typeof(StaticGridBlocker), typeof(CampaignMissionCombatSuppressedTag))]
        private partial struct BuildSpatialMapJob : IJobEntity
        {
            public GridConfig Grid;
            public NativeParallelMultiHashMap<int, Entity>.ParallelWriter Writer;

            public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in LocalTransform transform, in UnitGrid unitGrid, in Faction faction, in UnitHealth health)
            {
                if (health.Current <= 0) return;
                int2 cell = GridUtils.WorldToCell(Grid, transform.Position);
                if (!GridUtils.InBounds(cell, Grid.Width, Grid.Height))
                    cell = unitGrid.Cell;
                if (!GridUtils.InBounds(cell, Grid.Width, Grid.Height))
                    return;
                int idx = GridUtils.CellToIndex(cell, Grid.Width);
                Writer.Add(idx, entity);
            }
        }

        [BurstCompile]
        [WithNone(typeof(StaticGridBlocker), typeof(EngageTarget), typeof(BaseBreachOrder), typeof(GroundMissileInFlightComponent), typeof(GroundMissileLauncherComponent), typeof(CampaignMissionCombatSuppressedTag))]
        internal partial struct AcquireTargetsJob : IJobEntity
        {
            [ReadOnly] public GridConfig Grid;
            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;
            [ReadOnly] public NativeParallelHashMap<Entity, int> AttackerCounts;
            [ReadOnly] public NativeArray<Entity> BuildingTargets;

            [ReadOnly] public ComponentLookup<Faction> FactionLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<EngageTarget> EngageLookup;
            [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
            [ReadOnly] public ComponentLookup<RecentAttacker> RecentAttackerLookup;
            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;
            [ReadOnly] public ComponentLookup<AttackMoveOrder> AttackMoveLookup;
            [ReadOnly] public ComponentLookup<UnitPathFollow> PathFollowLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
            [ReadOnly] public ComponentLookup<HoldPositionOrderTag> HoldPositionLookup;
            [ReadOnly] public ComponentLookup<UnitScanOrder> ScanOrderLookup;
            [ReadOnly] public ComponentLookup<CombatTargetPolicy> TargetPolicyLookup;
            [ReadOnly] public ComponentLookup<SkirmishAttemptOwnedComponent> FogOwnedLookup;
            [ReadOnly] public ComponentLookup<SkirmishContactSightComponent> FogSightLookup;
            [ReadOnly] public BufferLookup<SkirmishContactKnowledgeElement> FogKnowledgeLookup;
            [ReadOnly] public ComponentLookup<RuntimeBuildingCombatInfo> TargetBuildingLookup;
            [ReadOnly] public ComponentLookup<CampaignMissionProtectedRecordsTag> ProtectedRecordsLookup;
            [ReadOnly] public ComponentLookup<StaticGridBlocker> TargetBlockerLookup;
            [ReadOnly] public ComponentLookup<UnitAirMovement> TargetAirLookup;
            [ReadOnly] public ComponentLookup<UnitMovementBehavior> TargetMovementLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in UnitGrid selfGrid, in Faction selfFaction, in UnitCombat combat, in UnitAttack attack, in LocalTransform selfTransform)
            {
                var context=new NativeContext{Job=this,Source=entity};
                var input=new TargetAcquisitionInput
                {
                    Source=Identity(entity),Position=Point(selfTransform.Position),GridCell=new TargetCell(selfGrid.Cell.x,selfGrid.Cell.y),
                    GridWidth=Grid.Width,GridHeight=Grid.Height,CellSize=Grid.CellSize,AggroRangeCells=combat.AggroRangeCells,
                    AttackRange=attack.Range,FactionId=selfFaction.Id,CanAttack=combat.CanAttack,AutoEngage=combat.AutoEngage
                };
                var result=TargetAcquisitionKernel.Acquire(ref context,input);
                // Preserve the original deferred command sequence: invalid recent removal
                // precedes normal acquisition; retaliation adds Engage before recent removal.
                if(result.RemoveRecentAttacker&&!result.Retaliation)Ecb.RemoveComponent<RecentAttacker>(sortKey,entity);
                if(!result.Acquired)return;
                Ecb.AddComponent(sortKey,entity,new EngageTarget{Target=EntityOf(result.Target),Cell=new int2(result.Cell.X,result.Cell.Z),
                    Position=new float3(result.Position.X,result.Position.Y,result.Position.Z),IsCommanded=0});
                if(result.Retaliation)Ecb.RemoveComponent<RecentAttacker>(sortKey,entity);
                Ecb.RemoveComponent<UnitPathFollow>(sortKey,entity);Ecb.RemoveComponent<UnitPathRange>(sortKey,entity);
                Ecb.RemoveComponent<UnitPathRequest>(sortKey,entity);Ecb.RemoveComponent<AutoWanderMoveTag>(sortKey,entity);
            }
            private static TargetIdentity Identity(Entity entity)=>new TargetIdentity(entity.Index,entity.Version);
            private static Entity EntityOf(TargetIdentity identity)=>new Entity{Index=identity.Index,Version=identity.Version};
            private static TargetPosition Point(float3 position)=>new TargetPosition(position.x,position.y,position.z);
            private struct NativeContext:ITargetAcquisitionContext
            {
                public AcquireTargetsJob Job;public Entity Source;
                private NativeParallelMultiHashMapIterator<int> iterator;
                public bool SourceHasAttackMove()=>Job.AttackMoveLookup.HasComponent(Source);
                public bool SourceHasManualMove()=>Job.ManualMoveLookup.HasComponent(Source);
                public bool SourceHasPathFollow()=>Job.PathFollowLookup.HasComponent(Source);
                public bool SourceHasPathRequest()=>Job.PathRequestLookup.HasComponent(Source);
                public bool SourceHoldsPosition()=>Job.HoldPositionLookup.HasComponent(Source);
                public bool TryReadScan(out TargetScanOrder order)
                {
                    order=default;if(!Job.ScanOrderLookup.HasComponent(Source))return false;
                    var value=Job.ScanOrderLookup[Source];order=new TargetScanOrder{CenterX=value.CenterCell.x,CenterZ=value.CenterCell.y,
                        RadiusCells=value.RadiusCells,HasStarted=value.HasStarted,EngageDetectedTargets=value.EngageDetectedTargets};return true;
                }
                public bool TryReadRecent(out TargetIdentity target)
                {target=default;if(!Job.RecentAttackerLookup.HasComponent(Source))return false;target=Identity(Job.RecentAttackerLookup[Source].Attacker);return true;}
                public bool BeginCell(int key,out TargetIdentity candidate)
                {bool found=Job.SpatialMap.TryGetFirstValue(key,out Entity value,out iterator);candidate=Identity(value);return found;}
                public bool NextCell(out TargetIdentity candidate)
                {bool found=Job.SpatialMap.TryGetNextValue(out Entity value,ref iterator);candidate=Identity(value);return found;}
                public int BuildingCount()=>Job.BuildingTargets.Length;
                public TargetIdentity BuildingAt(int index)=>Identity(Job.BuildingTargets[index]);
                public bool HasFaction(TargetIdentity target)=>Job.FactionLookup.HasComponent(EntityOf(target));
                public byte Faction(TargetIdentity target)=>Job.FactionLookup[EntityOf(target)].Id;
                public bool HasTransform(TargetIdentity target)=>Job.TransformLookup.HasComponent(EntityOf(target));
                public TargetPosition Position(TargetIdentity target)=>Point(Job.TransformLookup[EntityOf(target)].Position);
                public bool HasHealth(TargetIdentity target)=>Job.HealthLookup.HasComponent(EntityOf(target));
                public int Health(TargetIdentity target)=>Job.HealthLookup[EntityOf(target)].Current;
                public bool HasEngagement(TargetIdentity target)=>Job.EngageLookup.HasComponent(EntityOf(target));
                public TargetIdentity EngagementTarget(TargetIdentity target)=>Identity(Job.EngageLookup[EntityOf(target)].Target);
                public bool ProtectedRecords(TargetIdentity target)=>Job.ProtectedRecordsLookup.HasComponent(EntityOf(target));
                public int AttackerCount(TargetIdentity target){Job.AttackerCounts.TryGetValue(EntityOf(target),out int count);return count;}
                public TargetCell WorldToCell(TargetPosition position)
                {var cell=GridUtils.WorldToCell(Job.Grid,new float3(position.X,position.Y,position.Z));return new TargetCell(cell.x,cell.y);}
                public TargetPolicyFacts PolicyFacts(TargetIdentity source,TargetIdentity target)
                {
                    Entity s=EntityOf(source),t=EntityOf(target);
                    bool hasSource=Job.TargetPolicyLookup.HasComponent(s),hasTarget=Job.TargetPolicyLookup.HasComponent(t);
                    var sourcePolicy=hasSource?Job.TargetPolicyLookup[s]:default;
                    var facts=new TargetPolicyFacts{HasSourcePolicy=hasSource,HasTargetPolicy=hasTarget,SourceAllowedTargets=(byte)sourcePolicy.AllowedTargets};
                    if(hasTarget){var policy=Job.TargetPolicyLookup[t];facts.TargetDomain=(byte)policy.Domain;facts.TargetVisible=policy.Visible;}
                    else
                    {
                        facts.TargetIsStructure=Job.TargetBuildingLookup.HasComponent(t)||Job.TargetBlockerLookup.HasComponent(t);
                        facts.TargetIsAir=Job.TargetAirLookup.HasComponent(t);
                        facts.TargetUsesVehicleMotion=Job.TargetMovementLookup.HasComponent(t)&&Job.TargetMovementLookup[t].UsesVehicleMotion!=0;
                    }
                    if(Job.FogOwnedLookup.HasComponent(s)&&Job.FogOwnedLookup.HasComponent(t))
                    {
                        var observer=Job.FogOwnedLookup[s];facts.PairedFog=true;
                        facts.SameFogSession=observer.SessionId.Equals(Job.FogOwnedLookup[t].SessionId);
                        if(facts.SameFogSession)facts.FogVisibleToObserver=SkirmishFogConsumerUtility.IsVisible(Job.FogOwnedLookup,Job.FogSightLookup,Job.FogKnowledgeLookup,t,observer.FactionId);
                    }
                    return facts;
                }
            }
        }
    }
}
