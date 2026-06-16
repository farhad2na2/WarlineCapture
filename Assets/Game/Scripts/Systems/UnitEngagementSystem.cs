using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(DynamicOccupancyRebuildSystem))]
[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct UnitEngagementSystem : ISystem
{
    private const double TargetAcquisitionIntervalSeconds = 0.12d;
    private EntityQuery _unitsQuery;
    private EntityQuery _acquisitionQuery;
    private double _nextTargetAcquisitionTime;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
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
                ComponentType.ReadOnly<UnitCombat>(),
                ComponentType.ReadOnly<UnitAttack>(),
                ComponentType.ReadOnly<LocalTransform>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>()
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
                ComponentType.ReadOnly<GroundMissileLauncherComponent>()
            }
        });

        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        double now = SystemAPI.Time.ElapsedTime;
        if (now < _nextTargetAcquisitionTime)
            return;

        _nextTargetAcquisitionTime = now + TargetAcquisitionIntervalSeconds;

        var grid = SystemAPI.GetSingleton<GridConfig>();
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
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var acquireJob = new AcquireTargetsJob
        {
            Grid = grid,
            SpatialMap = map,
            AttackerCounts = attackerCounts,
            FactionLookup = factionLookup,
            TransformLookup = transformLookup,
            EngageLookup = engageLookup,
            HealthLookup = healthLookup,
            RecentAttackerLookup = recentAttackerLookup,
            ManualMoveLookup = manualMoveLookup,
            PathFollowLookup = pathFollowLookup,
            PathRequestLookup = pathRequestLookup,
            HoldPositionLookup = holdPositionLookup,
            ScanOrderLookup = scanOrderLookup,
            Ecb = ecb
        }.ScheduleParallel(buildHandle);

        var mapDisposeHandle = map.Dispose(acquireJob);
        state.Dependency = attackerCounts.Dispose(mapDisposeHandle);
    }

    [BurstCompile]
    [WithNone(typeof(StaticGridBlocker))]
    private partial struct BuildSpatialMapJob : IJobEntity
    {
        public GridConfig Grid;
        public NativeParallelMultiHashMap<int, Entity>.ParallelWriter Writer;

        public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in LocalTransform transform, in UnitGrid unitGrid, in Faction faction, in UnitCombat combat, in UnitAttack attack)
        {
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
    [WithNone(typeof(StaticGridBlocker), typeof(EngageTarget), typeof(GroundMissileInFlightComponent), typeof(GroundMissileLauncherComponent))]
    private partial struct AcquireTargetsJob : IJobEntity
    {
        [ReadOnly] public GridConfig Grid;
        [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;
        [ReadOnly] public NativeParallelHashMap<Entity, int> AttackerCounts;

        [ReadOnly] public ComponentLookup<Faction> FactionLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<EngageTarget> EngageLookup;
        [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
        [ReadOnly] public ComponentLookup<RecentAttacker> RecentAttackerLookup;
        [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;
        [ReadOnly] public ComponentLookup<UnitPathFollow> PathFollowLookup;
        [ReadOnly] public ComponentLookup<UnitPathRequest> PathRequestLookup;
        [ReadOnly] public ComponentLookup<HoldPositionOrderTag> HoldPositionLookup;
        [ReadOnly] public ComponentLookup<UnitScanOrder> ScanOrderLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute([EntityIndexInQuery] int sortKey, Entity entity, in UnitGrid selfGrid, in Faction selfFaction, in UnitCombat combat, in UnitAttack attack, in LocalTransform selfTransform)
        {
            if (combat.CanAttack == 0)
                return;
            if (combat.AutoEngage == 0)
                return;

            bool hasActiveManualMove =
                ManualMoveLookup.HasComponent(entity) &&
                (PathFollowLookup.HasComponent(entity) || PathRequestLookup.HasComponent(entity));

            bool holdingPosition = HoldPositionLookup.HasComponent(entity);
            bool scanning = TryGetActiveScanOrder(entity, out UnitScanOrder scanOrder);
            int attackRangeCells = Grid.CellSize > 1e-6f && attack.Range > 0f
                ? (int)math.ceil(attack.Range / Grid.CellSize)
                : 0;
            int scanRangeCells = holdingPosition
                ? attackRangeCells
                : math.max(math.max(0, combat.AggroRangeCells), attackRangeCells);
            if (scanning)
                scanRangeCells = math.max(scanRangeCells, math.max(1, scanOrder.RadiusCells));

            if (scanRangeCells <= 0)
                return;

            float maxDist = holdingPosition && attack.Range > 0f
                ? attack.Range
                : scanRangeCells * Grid.CellSize;
            if (!holdingPosition && attack.Range > 0f)
                maxDist = math.max(maxDist, attack.Range);
            if (scanning)
                maxDist = math.max(maxDist, math.max(1, scanOrder.RadiusCells) * Grid.CellSize * 2f);
            float maxDistSq = maxDist * maxDist;

            float bestScore = float.MaxValue;
            Entity best = Entity.Null;

            if (RecentAttackerLookup.HasComponent(entity))
            {
                RecentAttacker recent = RecentAttackerLookup[entity];
                bool movementBlocksCombat = hasActiveManualMove && !scanning;
                if (!movementBlocksCombat &&
                    IsValidRetaliationTarget(recent.Attacker, selfFaction.Id) &&
                    (!scanning || IsTargetInsideScanArea(recent.Attacker, scanOrder)))
                {
                    float3 recentPos = TransformLookup[recent.Attacker].Position;
                    float3 recentDelta = recentPos - selfTransform.Position;
                    recentDelta.y = 0f;
                    if (!holdingPosition || math.lengthsq(recentDelta) <= maxDistSq)
                    {
                        int2 recentCell = GridUtils.WorldToCell(Grid, recentPos);
                        Ecb.AddComponent(sortKey, entity, new EngageTarget
                        {
                            Target = recent.Attacker,
                            Cell = recentCell,
                            Position = recentPos,
                            IsCommanded = 0
                        });
                        Ecb.RemoveComponent<RecentAttacker>(sortKey, entity);
                        Ecb.RemoveComponent<UnitPathFollow>(sortKey, entity);
                        Ecb.RemoveComponent<UnitPathRange>(sortKey, entity);
                        Ecb.RemoveComponent<UnitPathRequest>(sortKey, entity);
                        Ecb.RemoveComponent<AutoWanderMoveTag>(sortKey, entity);
                        return;
                    }
                }

                Ecb.RemoveComponent<RecentAttacker>(sortKey, entity);
            }

            if (hasActiveManualMove && !scanning)
                return;

            int2 c0 = GridUtils.WorldToCell(Grid, selfTransform.Position);
            if (!GridUtils.InBounds(c0, Grid.Width, Grid.Height))
                c0 = selfGrid.Cell;
            int2 searchCenter = scanning ? scanOrder.CenterCell : c0;
            int searchRangeCells = scanning ? math.max(1, scanOrder.RadiusCells) : scanRangeCells;
            for (int dy = -searchRangeCells; dy <= searchRangeCells; dy++)
            {
                int y = searchCenter.y + dy;
                if ((uint)y >= (uint)Grid.Height)
                    continue;

                for (int dx = -searchRangeCells; dx <= searchRangeCells; dx++)
                {
                    int x = searchCenter.x + dx;
                    if ((uint)x >= (uint)Grid.Width)
                        continue;

                    int key = x + y * Grid.Width;
                    if (!SpatialMap.TryGetFirstValue(key, out var candidate, out var it))
                        continue;

                    do
                    {
                        if (candidate == entity)
                            continue;

                        if (!FactionLookup.HasComponent(candidate) || !TransformLookup.HasComponent(candidate))
                            continue;

                        var otherFaction = FactionLookup[candidate];
                        if (otherFaction.Id == selfFaction.Id)
                        {
                            // Ally assist: if a nearby ally is engaged, consider its target too.
                            if (EngageLookup.HasComponent(candidate))
                            {
                                var allyEngage = EngageLookup[candidate];
                                if (allyEngage.Target != Entity.Null)
                                {
                                    EvaluateEnemyCandidate(allyEngage.Target, selfFaction.Id, selfTransform.Position, maxDistSq, scanning, scanOrder, ref bestScore, ref best);
                                }
                            }
                            continue;
                        }

                        EvaluateEnemyCandidate(candidate, selfFaction.Id, selfTransform.Position, maxDistSq, scanning, scanOrder, ref bestScore, ref best);
                    } while (SpatialMap.TryGetNextValue(out candidate, ref it));
                }
            }

            if (best == Entity.Null)
                return;

            float3 bestPos = TransformLookup.HasComponent(best) ? TransformLookup[best].Position : selfTransform.Position;
            int2 bestCell = GridUtils.WorldToCell(Grid, bestPos);
            if (!GridUtils.InBounds(bestCell, Grid.Width, Grid.Height))
                bestCell = selfGrid.Cell;
            Ecb.AddComponent(sortKey, entity, new EngageTarget { Target = best, Cell = bestCell, Position = bestPos, IsCommanded = 0 });
            Ecb.RemoveComponent<UnitPathFollow>(sortKey, entity);
            Ecb.RemoveComponent<UnitPathRange>(sortKey, entity);
            Ecb.RemoveComponent<UnitPathRequest>(sortKey, entity);
            Ecb.RemoveComponent<AutoWanderMoveTag>(sortKey, entity);
        }

        private void EvaluateEnemyCandidate(
            Entity candidate,
            byte selfFactionId,
            float3 selfPos,
            float maxDistSq,
            bool scanning,
            in UnitScanOrder scanOrder,
            ref float bestScore,
            ref Entity best)
        {
            if (!FactionLookup.HasComponent(candidate) || !TransformLookup.HasComponent(candidate))
                return;

            var otherFaction = FactionLookup[candidate];
            if (otherFaction.Id == selfFactionId)
                return;

            if (HealthLookup.HasComponent(candidate) && HealthLookup[candidate].Current <= 0)
                return;

            float3 otherPos = TransformLookup[candidate].Position;
            if (scanning && !IsPositionInsideScanArea(otherPos, scanOrder))
                return;

            float3 delta = otherPos - selfPos;
            delta.y = 0f;
            float distSq = math.lengthsq(delta);
            if (distSq > maxDistSq)
                return;

            int attackers = 0;
            AttackerCounts.TryGetValue(candidate, out attackers);
            float score = distSq + attackers * (Grid.CellSize * Grid.CellSize * 9f);

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        private bool TryGetActiveScanOrder(Entity entity, out UnitScanOrder scanOrder)
        {
            scanOrder = default;
            if (!ScanOrderLookup.HasComponent(entity))
                return false;

            scanOrder = ScanOrderLookup[entity];
            return scanOrder.HasStarted != 0 &&
                   scanOrder.EngageDetectedTargets != 0 &&
                   scanOrder.RadiusCells > 0;
        }

        private bool IsTargetInsideScanArea(Entity target, in UnitScanOrder scanOrder)
        {
            return TransformLookup.HasComponent(target) &&
                   IsPositionInsideScanArea(TransformLookup[target].Position, scanOrder);
        }

        private bool IsPositionInsideScanArea(float3 position, in UnitScanOrder scanOrder)
        {
            int2 cell = GridUtils.WorldToCell(Grid, position);
            if (!GridUtils.InBounds(cell, Grid.Width, Grid.Height))
                return false;

            return ChebyshevDistance(cell, scanOrder.CenterCell) <= math.max(1, scanOrder.RadiusCells);
        }

        private static int ChebyshevDistance(int2 a, int2 b)
        {
            int2 delta = math.abs(a - b);
            return math.max(delta.x, delta.y);
        }

        private bool IsValidRetaliationTarget(Entity candidate, byte selfFactionId)
        {
            if (candidate == Entity.Null ||
                !FactionLookup.HasComponent(candidate) ||
                !TransformLookup.HasComponent(candidate))
            {
                return false;
            }

            if (FactionLookup[candidate].Id == selfFactionId)
                return false;

            return !HealthLookup.HasComponent(candidate) || HealthLookup[candidate].Current > 0;
        }
    }
}
