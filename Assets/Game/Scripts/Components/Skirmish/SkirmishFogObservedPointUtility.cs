using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Components
{
    // Derived presentation check uses unchanged actual native observer radii.
    // It reads the request/projectile point, never an unseen target's live pose.
    public static class SkirmishFogObservedPointUtility
    {
        public static bool IsObserved(EntityManager em, Entity source, float3 position, byte detectorDomain)
        {
            Unity.Collections.FixedString64Bytes attempt;
            if (em.Exists(source) && em.HasComponent<SkirmishAttemptOwnedComponent>(source))
                attempt = em.GetComponentData<SkirmishAttemptOwnedComponent>(source).SessionId;
            else if (!TryActiveSharedAttempt(em, out attempt)) return false;
            return IsObserved(em, attempt, position, detectorDomain);
        }
        public static bool HasActiveSharedAttempt(EntityManager em)
        {
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var state = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (state.IsLegacy == 0 && state.Phase == Game.Skirmish.Contracts.SkirmishSessionPhase.Playing &&
                    em.GetComponentData<SkirmishFogStateComponent>(entity).SharedFog != 0) return true;
            }
            return false;
        }
        public static bool TryActiveSharedAttempt(EntityManager em, out Unity.Collections.FixedString64Bytes attempt)
        {
            attempt = default;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            bool found = false;
            foreach (var entity in entities)
            {
                var state = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                var fog = em.GetComponentData<SkirmishFogStateComponent>(entity);
                if (state.IsLegacy != 0 || state.Phase != Game.Skirmish.Contracts.SkirmishSessionPhase.Playing || fog.SharedFog == 0) continue;
                if (found) { attempt = default; return false; } // No ambiguous cross-attempt policy.
                attempt = state.SessionId; found = true;
            }
            return found;
        }
        public static bool IsObserved(EntityManager em, Unity.Collections.FixedString64Bytes attempt,
            float3 position, byte detectorDomain)
        {
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (grids.CalculateEntityCount() != 1) return false;
            var grid = grids.GetSingleton<GridConfig>();
            if (grid.CellSize <= 0) return false;
            int2 cell = (int2)math.floor((position.xz - grid.Origin.xz) / grid.CellSize);
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(Faction),
                typeof(UnitHealth), typeof(LocalTransform));
            using var observers = query.ToEntityArray(Allocator.Temp);
            foreach (var observer in observers)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(observer);
                if (owner.FactionId != 1 || owner.CapacityReleased != 0 || !owner.SessionId.Equals(attempt) ||
                    em.GetComponentData<Faction>(observer).Id != 1 ||
                    em.GetComponentData<UnitHealth>(observer).Current <= 0 ||
                    em.HasComponent<UnitDeathAnimationComponent>(observer) ||
                    em.HasComponent<UnitTransportPassenger>(observer)) continue;
                float3 origin = em.GetComponentData<LocalTransform>(observer).Position;
                float radius = 0;
                if (em.HasComponent<UnitCombat>(observer))
                {
                    radius = math.max(0, em.GetComponentData<UnitCombat>(observer).AggroRangeCells) * grid.CellSize;
                    if (em.HasComponent<UnitAttack>(observer))
                        radius = math.max(radius, math.ceil(math.max(0,
                            em.GetComponentData<UnitAttack>(observer).Range) / grid.CellSize) * grid.CellSize);
                }
                if (radius > 0 && math.distancesq(origin.xz, position.xz) <= radius * radius) return true;
                if (em.HasComponent<ThreatDetector>(observer) && em.HasComponent<UnitGrid>(observer))
                {
                    var detector = em.GetComponentData<ThreatDetector>(observer);
                    int2 distance = math.abs(em.GetComponentData<UnitGrid>(observer).Cell - cell);
                    if (detector.Kind == detectorDomain && detector.RadiusCells > 0 &&
                        math.max(distance.x, distance.y) <= detector.RadiusCells) return true;
                }
            }
            return false;
        }
    }
}
