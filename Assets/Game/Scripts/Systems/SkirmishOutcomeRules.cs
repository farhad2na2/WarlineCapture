using Game.Components;
using Game.Configs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class SkirmishOutcomeRules
    {
        public static bool Evaluate(ref SkirmishMatchState match, bool playerBaseAlive, bool enemyBaseAlive,
            float deltaSeconds, bool simulationActive, bool surrender = false)
        {
            if (match.Phase != SkirmishPhase.Playing) return false;
            if (simulationActive && math.isfinite(deltaSeconds)) match.ElapsedSeconds += math.max(0, deltaSeconds);
            if (!playerBaseAlive || !enemyBaseAlive)
            {
                match.Outcome = playerBaseAlive ? SkirmishOutcome.Victory : enemyBaseAlive ? SkirmishOutcome.Defeat : SkirmishOutcome.Draw;
                match.Reason = !playerBaseAlive && !enemyBaseAlive ? SkirmishEndReason.BothBasesDestroyed : SkirmishEndReason.MainBaseDestroyed;
            }
            else if (surrender) { match.Outcome=SkirmishOutcome.Defeat; match.Reason=SkirmishEndReason.Surrender; }
            else if (match.ElapsedSeconds >= SkirmishLegacyMatchPolicy.MatchDurationSeconds)
            { match.Outcome=SkirmishOutcome.Draw; match.Reason=SkirmishEndReason.TimeLimit; }
            else return false;
            match.ElapsedSeconds=math.min(match.ElapsedSeconds,SkirmishLegacyMatchPolicy.MatchDurationSeconds);
            match.Phase=SkirmishPhase.Finished;
            return true;
        }

        // Shared original ECS outcome/deactivation boundary. Native scheduling
        // and the bounded source host both evaluate the same live match state.
        public static bool Evaluate(EntityManager em, ref SkirmishMatchState match,
            float deltaSeconds, ref RuntimeGameplayStateComponent gameplay)
        {
            bool ended = Evaluate(ref match,
                IsAlive(em, match.PlayerMainBase),
                IsAlive(em, match.EnemyMainBase),
                deltaSeconds,
                gameplay.SimulationActive != 0,
                match.SurrenderRequested != 0);
            if (!ended) return false;

            gameplay.SimulationActive = 0;
            gameplay.PlayRequested = 0;
            gameplay.SelectionModeActive = 0;
            gameplay.BuildModeActive = 0;
            return true;
        }

        private static bool IsAlive(EntityManager em, Entity entity) =>
            entity != Entity.Null && em.Exists(entity) && em.HasComponent<UnitHealth>(entity) &&
            em.GetComponentData<UnitHealth>(entity).Current > 0;
    }
}
