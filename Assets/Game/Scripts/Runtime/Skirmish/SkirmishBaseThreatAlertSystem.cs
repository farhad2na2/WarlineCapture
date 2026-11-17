using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public enum SkirmishBaseThreatReason : byte { None = 0, Perimeter = 1, FirstDamage = 2 }

    /// <summary>
    /// Player-facing "base under attack" facts for the HUD (faction 1 only). Raised when a
    /// hostile the player can see enters the base perimeter, and again on the first damage of
    /// an attack. The perimeter equals the base reveal radius, so anything that raises it is
    /// already visible: the alert never reports hidden state.
    /// </summary>
    public struct SkirmishBaseThreatAlertState : IComponentData
    {
        public int Revision;
        public SkirmishBaseThreatReason Reason;
        public float RaisedAt;
        public float PerimeterRadius;
        public byte IntruderInside;
        public float3 IntruderPosition;
        public int LastHealth;
        public byte HealthKnown;
        public float LastDamageAt;
        public float LastInsideAt;
    }

    /// <summary>Pure edge rules (EditMode tested through <see cref="SkirmishBaseThreatAlertRules"/>).</summary>
    public static class SkirmishBaseThreatAlertRules
    {
        /// <summary>A new perimeter alert needs this long with no intruder inside.</summary>
        public const float PerimeterRearmSeconds = 10f;
        /// <summary>An attack ends after this long without damage; the next hit is a "first" hit again.</summary>
        public const float DamageQuietSeconds = 8f;
        /// <summary>Hysteresis: an intruder counts as gone only beyond radius + this margin.</summary>
        public const float ExitMarginMetres = 15f;

        public static SkirmishBaseThreatReason Step(ref SkirmishBaseThreatAlertState state, float now,
            int baseHealth, bool intruderWithinRadius, bool intruderWithinExitMargin)
        {
            var raised = SkirmishBaseThreatReason.None;
            bool wasInside = state.IntruderInside != 0;
            bool inside = intruderWithinRadius || wasInside && intruderWithinExitMargin;
            if (inside && !wasInside && (state.LastInsideAt <= 0f || now - state.LastInsideAt > PerimeterRearmSeconds))
                raised = SkirmishBaseThreatReason.Perimeter;
            if (inside) state.LastInsideAt = now;
            state.IntruderInside = inside ? (byte)1 : (byte)0;

            if (state.HealthKnown != 0 && baseHealth < state.LastHealth)
            {
                if (state.LastDamageAt <= 0f || now - state.LastDamageAt > DamageQuietSeconds)
                    raised = SkirmishBaseThreatReason.FirstDamage;
                state.LastDamageAt = now;
            }
            state.LastHealth = baseHealth;
            state.HealthKnown = 1;

            if (raised != SkirmishBaseThreatReason.None)
            {
                state.Revision++;
                state.Reason = raised;
                state.RaisedAt = now;
            }
            return raised;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishSharedFogUpdateSystem))]
    public partial class SkirmishBaseThreatAlertSystem : SystemBase
    {
        protected override void OnCreate() => RequireForUpdate<SkirmishExpandedSessionComponent>();

        protected override void OnUpdate()
        {
            if (SystemAPI.Time.DeltaTime <= 0) return;
            var em = EntityManager;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishMatchState));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            float cellSize = 1f;
            using (var grids = em.CreateEntityQuery(typeof(GridConfig)))
                if (grids.CalculateEntityCount() == 1) cellSize = math.max(0, grids.GetSingleton<GridConfig>().CellSize);
            float now = (float)SystemAPI.Time.ElapsedTime;
            foreach (var session in entities)
            {
                var native = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                var match = em.GetComponentData<SkirmishMatchState>(session);
                if (native.IsLegacy != 0 || match.Phase != SkirmishPhase.Playing) continue;
                Entity home = match.PlayerMainBase;
                if (home == Entity.Null || !em.Exists(home) || !em.HasComponent<LocalTransform>(home) || !em.HasComponent<UnitHealth>(home)) continue;
                int health = em.GetComponentData<UnitHealth>(home).Current;
                if (health <= 0) continue;
                float3 homePosition = em.GetComponentData<LocalTransform>(home).Position;
                int2 footprint = em.HasComponent<UnitFootprint>(home) ? em.GetComponentData<UnitFootprint>(home).Size : new int2(1, 1);
                float radius = SkirmishBaseVisionPolicy.RevealRadius(footprint, cellSize);

                var state = em.HasComponent<SkirmishBaseThreatAlertState>(session)
                    ? em.GetComponentData<SkirmishBaseThreatAlertState>(session) : default;
                state.PerimeterRadius = radius;
                FindVisibleIntruder(em, native.SessionId, homePosition, radius, out float nearest, out float3 intruder);
                bool within = nearest <= radius;
                bool withinExit = nearest <= radius + SkirmishBaseThreatAlertRules.ExitMarginMetres;
                if (withinExit) state.IntruderPosition = intruder;
                var raised = SkirmishBaseThreatAlertRules.Step(ref state, now, health, within, withinExit);
                if (raised != SkirmishBaseThreatReason.None)
                    AudioEventRequestSystem.EnqueueOneShot(em, new FixedString64Bytes(AudioEventIds.AlertBaseBreached),
                        AudioEventIds.AlertBaseBreachedHash, new FixedString32Bytes("Alerts"), AudioPlaybackPriority.Critical,
                        now, 5f, home);
                if (em.HasComponent<SkirmishBaseThreatAlertState>(session)) em.SetComponentData(session, state);
                else em.AddComponentData(session, state);
            }
        }

        // Only hostiles the player can currently see (faction-1 fog), never hidden contacts.
        private static void FindVisibleIntruder(EntityManager em, FixedString64Bytes sessionId, float3 home, float radius,
            out float nearest, out float3 position)
        {
            nearest = float.MaxValue;
            position = default;
            float limit = radius + SkirmishBaseThreatAlertRules.ExitMarginMetres;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<UnitHealth>());
            using var actors = query.ToEntityArray(Allocator.Temp);
            foreach (var actor in actors)
            {
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if (!owned.SessionId.Equals(sessionId) || owned.FactionId != 2 || owned.IsStructure != 0 || owned.CapacityReleased != 0) continue;
                if (em.GetComponentData<UnitHealth>(actor).Current <= 0 || em.HasComponent<Parent>(actor)) continue;
                float3 p = em.GetComponentData<LocalTransform>(actor).Position;
                float distance = math.distance(p.xz, home.xz);
                if (distance > limit || distance >= nearest) continue;
                if (!SkirmishFogService.IsVisibleToFaction(em, actor, 1)) continue;
                nearest = distance;
                position = p;
            }
        }
    }
}
