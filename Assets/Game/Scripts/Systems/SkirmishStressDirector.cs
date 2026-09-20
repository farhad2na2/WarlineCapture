using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(InitialUnitsSpawnSystem))]
    public partial struct SkirmishStressDirector : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishMatchState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (sessions.CalculateEntityCount() != 1) return;
            var entity = sessions.GetSingletonEntity();
            var match = em.GetComponentData<SkirmishMatchState>(entity);
            if (match.ScenarioIndex != SkirmishStressRecipe.ScenarioIndex) return;

            EnsureSession(em, entity, match);
            var session = em.GetComponentData<SkirmishStressSession>(entity);
            if (session.ForceProjected == 0)
                session.ForceProjected = (byte)(TryProjectForces(em, session) ? 1 : 0);

            if (match.Phase == SkirmishPhase.Playing)
            {
                if (session.SpawnComplete == 0)
                    TryCompleteSpawn(em, ref session, SystemAPI.Time.DeltaTime);
                if (session.SpawnComplete != 0)
                    Advance(em, entity, ref session, SystemAPI.Time.DeltaTime);
            }

            em.SetComponentData(entity, session);
        }

        internal static void EnsureSession(EntityManager em, Entity entity, in SkirmishMatchState match)
        {
            if (em.HasComponent<SkirmishStressSession>(entity))
            {
                if (!em.HasBuffer<SkirmishStressCensusSample>(entity))
                    em.AddBuffer<SkirmishStressCensusSample>(entity);
                return;
            }

            var request = em.HasComponent<SkirmishStressLaunchRequest>(entity)
                ? em.GetComponentData<SkirmishStressLaunchRequest>(entity)
                : DefaultRequest(match.Seed);
            var scale = SkirmishStressRecipe.NormalizeScale(request.Scale);
            bool sequence = request.Sequence != 0;
            var phase = sequence ? SkirmishStressPhase.Warmup : (SkirmishStressPhase)request.Phase;
            var requested = SkirmishStressRecipe.Requested(scale, phase, sequence);
            em.AddComponentData(entity, new SkirmishStressSession
            {
                Seed = request.Seed > 0 ? request.Seed : SkirmishStressRecipe.FixedSeed,
                Scale = (int)scale,
                ActivePhase = (SkirmishStressPhaseCode)phase,
                Layout = request.Layout,
                Sequence = sequence ? (byte)1 : (byte)0,
                RequestedCombat = requested.Combat,
                RequestedAir = requested.Air,
                RequestedSupport = requested.Support,
                RequestedBuildings = requested.Buildings
            });
            em.AddBuffer<SkirmishStressCensusSample>(entity);
        }

        internal static SkirmishStressLaunchRequest DefaultRequest(int seed) => new()
        {
            Seed = seed > 0 ? seed : SkirmishStressRecipe.FixedSeed,
            Scale = (int)SkirmishStressRecipe.DefaultScale,
            Phase = SkirmishStressPhaseCode.Warmup,
            Layout = SkirmishStressLayoutCode.Spread,
            Sequence = 1
        };

        internal static bool TryProjectForces(EntityManager em, in SkirmishStressSession session)
        {
            using var query = em.CreateEntityQuery(
                ComponentType.ReadWrite<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<CustomGameStartupStateComponent>());
            if (query.CalculateEntityCount() != 1) return false;
            var startup = query.GetSingletonEntity();
            if (!em.HasBuffer<InitialUnitsFactionUnitSpawnEntry>(startup) ||
                !em.HasBuffer<CustomGameFactionUnitSourceSpawnEntry>(startup))
                return false;
            if (em.HasComponent<InitialUnitsSpawnInitialized>(startup)) return false;
            if (em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(startup))
            {
                var progress = em.GetBuffer<InitialUnitsFactionUnitSpawnProgress>(startup);
                for (int i = 0; i < progress.Length; i++)
                    if (progress[i].Spawned > 0) return false;
            }

            var scale = SkirmishStressRecipe.NormalizeScale(session.Scale);
            var phase = (SkirmishStressPhase)session.ActivePhase;
            var layout = (SkirmishStressLayout)session.Layout;
            var lines = SkirmishStressRecipe.UnitsFor(scale, phase, session.Sequence != 0);
            var units = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(startup);
            var sources = em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(startup);
            units.Clear();
            sources.Clear();
            for (byte faction = 1; faction <= 2; faction++)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].CountPerSide <= 0) continue;
                    var offset = SkirmishStressRecipe.UnitOffset(lines[i], faction, layout);
                    var prefab = ResolvePrefab(em, startup, lines[i].SourceKey);
                    units.Add(new InitialUnitsFactionUnitSpawnEntry
                    {
                        FactionId = faction,
                        Prefab = prefab,
                        Count = lines[i].CountPerSide,
                        SpawnOffset = new int2(offset.x, offset.y)
                    });
                    sources.Add(new CustomGameFactionUnitSourceSpawnEntry
                    {
                        FactionId = faction,
                        SourceKey = new FixedString64Bytes(lines[i].SourceKey),
                        Count = lines[i].CountPerSide,
                        SpawnOffset = new int2(offset.x, offset.y)
                    });
                }
            }

            var config = em.GetComponentData<InitialUnitsSpawnConfig>(startup);
            config.SpawnRadiusCells = SkirmishStressRecipe.SpawnRadiusCells(scale, layout);
            config.RandomSeed = (uint)math.max(1, session.Seed);
            em.SetComponentData(startup, config);
            if (em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(startup))
                em.RemoveComponent<InitialUnitsFactionUnitSpawnProgress>(startup);
            Debug.Log(SkirmishStressRecipe.ReportMarker + " projected scale=" + (int)scale
                + " phase=" + phase + " layout=" + layout + " sequence=" + session.Sequence
                + " requestedCombat=" + session.RequestedCombat
                + " requestedAir=" + session.RequestedAir
                + " requestedSupport=" + session.RequestedSupport
                + " unitEntries=" + units.Length);
            return true;
        }

        internal static Entity ResolvePrefab(EntityManager em, Entity startup, string sourceKey)
        {
            if (em.HasBuffer<CustomGameUnitSourceRegistryEntry>(startup))
            {
                var registry = em.GetBuffer<CustomGameUnitSourceRegistryEntry>(startup);
                var key = new FixedString64Bytes(sourceKey);
                for (int i = 0; i < registry.Length; i++)
                    if (registry[i].SourceKey.Equals(key) && registry[i].LegacyUnitPrefab != Entity.Null)
                        return registry[i].LegacyUnitPrefab;
            }
            if (!em.HasBuffer<UnitPrefabRegistryEntry>(startup)) return Entity.Null;
            var entries = em.GetBuffer<UnitPrefabRegistryEntry>(startup);
            for (int i = 0; i < entries.Length; i++)
            {
                var prefab = entries[i].Prefab;
                if (prefab == Entity.Null || !em.HasComponent<UnitSourcePrefabKey>(prefab)) continue;
                if (em.GetComponentData<UnitSourcePrefabKey>(prefab).Value.ToString() == sourceKey)
                    return prefab;
            }
            return Entity.Null;
        }

        internal readonly struct SpawnStatus
        {
            public bool HasProgress;
            public bool Finished;
            public int Spawned;
            public int Requested;
        }

        internal static SpawnStatus EvaluateSpawn(EntityManager em)
        {
            using var query = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
            if (query.CalculateEntityCount() != 1)
                return default;
            var startup = query.GetSingletonEntity();
            bool initialized = em.HasComponent<InitialUnitsSpawnInitialized>(startup);
            if (!em.HasBuffer<InitialUnitsFactionUnitSpawnEntry>(startup) ||
                !em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(startup))
            {
                return new SpawnStatus { Finished = initialized };
            }

            var units = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(startup);
            var progress = em.GetBuffer<InitialUnitsFactionUnitSpawnProgress>(startup);
            int spawned = 0;
            int requested = 0;
            bool finished = initialized || progress.Length >= units.Length;
            int limit = math.min(units.Length, progress.Length);
            for (int i = 0; i < units.Length; i++)
                requested += units[i].Count;
            for (int i = 0; i < limit; i++)
            {
                spawned += progress[i].Spawned;
                if (progress[i].Spawned < units[i].Count)
                    finished = initialized;
            }
            if (progress.Length < units.Length && !initialized)
                finished = false;
            return new SpawnStatus
            {
                HasProgress = true,
                Finished = finished,
                Spawned = spawned,
                Requested = requested
            };
        }

        internal static bool TryCompleteSpawn(EntityManager em, ref SkirmishStressSession session, float dt)
        {
            var status = EvaluateSpawn(em);
            if (status.Finished)
            {
                session.SpawnComplete = 1;
                session.LastObservedSpawned = status.Spawned;
                return true;
            }

            if (!status.HasProgress)
                return false;

            if (status.Spawned != session.LastObservedSpawned)
            {
                session.LastObservedSpawned = status.Spawned;
                session.SpawnStallSeconds = 0f;
                return false;
            }

            session.SpawnStallSeconds += math.max(0f, dt);
            if (session.SpawnStallSeconds < SkirmishStressRecipe.SpawnStallCompleteSeconds)
                return false;

            session.SpawnComplete = 1;
            session.SpawnStalled = 1;
            Debug.Log(SkirmishStressRecipe.ReportMarker + " spawn-stalled spawnedProgress="
                + status.Spawned + " requestedProgress=" + status.Requested
                + " stallSeconds=" + session.SpawnStallSeconds.ToString("0.00")
                + " missingProgress=" + math.max(0, status.Requested - status.Spawned));
            return true;
        }

        private static void Advance(EntityManager em, Entity entity, ref SkirmishStressSession session, float dt)
        {
            if (session.OrdersIssued == 0)
            {
                IssuePhaseOrders(em, session);
                session.OrdersIssued = 1;
            }

            session.PhaseElapsed += math.max(0f, dt);
            float hold = SkirmishStressRecipe.PhaseHoldSeconds((SkirmishStressPhase)session.ActivePhase);
            if (session.ActivePhase == SkirmishStressPhaseCode.Warmup && session.SpawnComplete == 0)
                return;
            if (session.PhaseElapsed < hold) return;

            RecordSample(em, entity, session);
            if (session.Sequence == 0 || session.ActivePhase == SkirmishStressPhaseCode.Destruction)
                return;
            session.ActivePhase = NextPhase(session.ActivePhase);
            session.PhaseElapsed = 0;
            session.OrdersIssued = 0;
            var requested = SkirmishStressRecipe.Requested(
                SkirmishStressRecipe.NormalizeScale(session.Scale),
                (SkirmishStressPhase)session.ActivePhase,
                true);
            session.RequestedCombat = requested.Combat;
            session.RequestedAir = requested.Air;
            session.RequestedSupport = requested.Support;
            session.RequestedBuildings = requested.Buildings;
        }

        internal static void IssuePhaseOrders(EntityManager em, in SkirmishStressSession session)
        {
            var phase = (SkirmishStressPhase)session.ActivePhase;
            var layout = (SkirmishStressLayout)session.Layout;
            if (phase == SkirmishStressPhase.Warmup) return;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1) return;
            var grid = grids.GetSingleton<GridConfig>();
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitHealth>(), ComponentType.ReadOnly<UnitMove>() },
                None = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<UnitResourceHauler>(), ComponentType.ReadOnly<OperationMapBuildingComponent>() }
            });
            using var units = query.ToEntityArray(Allocator.Temp);
            int issued = 0;
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                byte faction = em.GetComponentData<Faction>(unit).Id;
                if (faction is not (1 or 2) || em.GetComponentData<UnitHealth>(unit).Current <= 0) continue;
                if (phase == SkirmishStressPhase.AirTransport && !SkirmishStressCensus.IsAir(em, unit) && !IsTransportEscort(em, unit))
                    continue;
                if (phase == SkirmishStressPhase.Idle)
                {
                    if (!em.HasComponent<HoldPositionOrderTag>(unit))
                        em.AddComponent<HoldPositionOrderTag>(unit);
                    issued++;
                    continue;
                }

                var goal = SkirmishStressRecipe.MoveGoal(faction, layout);
                if (phase == SkirmishStressPhase.AirTransport && SkirmishStressCensus.IsAir(em, unit))
                    goal = layout == SkirmishStressLayout.Concentrated ? new Vector2Int(1022, 620) : goal;
                if (!UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, unit, new int2(goal.x, goal.y)))
                    continue;
                if (phase is SkirmishStressPhase.DenseCombat or SkirmishStressPhase.Destruction or SkirmishStressPhase.AirTransport)
                {
                    float3 position = em.HasComponent<LocalTransform>(unit)
                        ? GridUtils.CellToWorldCenter(grid, new int2(goal.x, goal.y))
                        : default;
                    var order = new AttackMoveOrder { Destination = new int2(goal.x, goal.y), Position = position, RetryAt = 0 };
                    if (em.HasComponent<AttackMoveOrder>(unit)) em.SetComponentData(unit, order);
                    else em.AddComponentData(unit, order);
                }
                issued++;
            }
            Debug.Log(SkirmishStressRecipe.ReportMarker + " orders phase=" + phase + " issued=" + issued + " candidates=" + units.Length);
        }

        internal static void RecordSample(EntityManager em, Entity entity, in SkirmishStressSession session)
        {
            if (!em.HasBuffer<SkirmishStressCensusSample>(entity))
                em.AddBuffer<SkirmishStressCensusSample>(entity);
            var sample = SkirmishStressCensus.Measure(em, session, session.PhaseElapsed);
            em.GetBuffer<SkirmishStressCensusSample>(entity).Add(sample);
            Debug.Log(SkirmishStressCensus.FormatLine(sample));
        }

        private static SkirmishStressPhaseCode NextPhase(SkirmishStressPhaseCode phase) => phase switch
        {
            SkirmishStressPhaseCode.Warmup => SkirmishStressPhaseCode.Idle,
            SkirmishStressPhaseCode.Idle => SkirmishStressPhaseCode.MassMove,
            SkirmishStressPhaseCode.MassMove => SkirmishStressPhaseCode.DenseCombat,
            SkirmishStressPhaseCode.DenseCombat => SkirmishStressPhaseCode.AirTransport,
            _ => SkirmishStressPhaseCode.Destruction
        };

        private static bool IsTransportEscort(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<UnitSourcePrefabKey>(entity)) return false;
            string key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString().ToLowerInvariant();
            return key.Contains("soldier") || key.Contains("apc");
        }
    }
}
