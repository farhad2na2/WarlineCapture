using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SkirmishOutcomeSystem))]
    public partial struct SkirmishBaseAssaultObjectiveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            state.RequireForUpdate<SkirmishObjectiveStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRO<SkirmishExpandedSessionComponent> session,
                      RefRW<SkirmishObjectiveStateComponent> objective,
                      RefRO<SkirmishResolvedSetupComponent> setup) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>,
                         RefRW<SkirmishObjectiveStateComponent>,
                         RefRO<SkirmishResolvedSetupComponent>>())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.Phase != SkirmishSessionPhase.Playing)
                    continue;
                if (objective.ValueRO.Kind != SkirmishObjectiveKind.BaseAssault || objective.ValueRO.Terminal != 0)
                    continue;

                bool playerAlive = IsDesignatedBaseAlive(ref state, SkirmishObjectiveRoleKind.PlayerBase);
                bool enemyAlive = IsDesignatedBaseAlive(ref state, SkirmishObjectiveRoleKind.EnemyBase);
                float elapsed = 0f;
                if (SystemAPI.TryGetSingleton(out SkirmishMatchState match))
                    elapsed = match.ElapsedSeconds;
                bool surrender = SystemAPI.TryGetSingleton(out SkirmishMatchState surrendered) &&
                                 surrendered.SurrenderRequested != 0;
                if (!TryEvaluate(
                        playerAlive,
                        enemyAlive,
                        elapsed,
                        setup.ValueRO.DeadlineSeconds,
                        surrender,
                        out SkirmishOutcomeKind outcome,
                        out SkirmishEndReasonKind reason))
                    continue;

                objective.ValueRW.Outcome = outcome;
                objective.ValueRW.Reason = reason;
                objective.ValueRW.Terminal = 1;
                objective.ValueRW.State = outcome == SkirmishOutcomeKind.Victory
                    ? SkirmishObjectiveStateKind.TerminalVictory
                    : outcome == SkirmishOutcomeKind.Defeat
                        ? SkirmishObjectiveStateKind.TerminalDefeat
                        : SkirmishObjectiveStateKind.TerminalDraw;
            }
        }

        public static bool TryEvaluate(
            bool playerBaseAlive,
            bool enemyBaseAlive,
            float elapsedSeconds,
            int deadlineSeconds,
            bool surrender,
            out SkirmishOutcomeKind outcome,
            out SkirmishEndReasonKind reason)
        {
            outcome = SkirmishOutcomeKind.None;
            reason = SkirmishEndReasonKind.None;
            if (!playerBaseAlive && !enemyBaseAlive)
            {
                outcome = SkirmishOutcomeKind.Draw;
                reason = SkirmishEndReasonKind.BothBasesDestroyed;
                return true;
            }

            if (!enemyBaseAlive)
            {
                outcome = SkirmishOutcomeKind.Victory;
                reason = SkirmishEndReasonKind.MainBaseDestroyed;
                return true;
            }

            if (!playerBaseAlive)
            {
                outcome = SkirmishOutcomeKind.Defeat;
                reason = SkirmishEndReasonKind.MainBaseDestroyed;
                return true;
            }

            if (surrender)
            {
                outcome = SkirmishOutcomeKind.Defeat;
                reason = SkirmishEndReasonKind.Surrender;
                return true;
            }

            if (deadlineSeconds > 0 && elapsedSeconds >= deadlineSeconds)
            {
                outcome = SkirmishOutcomeKind.Draw;
                reason = SkirmishEndReasonKind.TimeLimit;
                return true;
            }

            return false;
        }

        private static bool IsDesignatedBaseAlive(ref SystemState state, SkirmishObjectiveRoleKind role)
        {
            bool found = false;
            foreach ((RefRO<SkirmishObjectiveRoleComponent> roleRef, RefRO<UnitHealth> health) in
                     SystemAPI.Query<RefRO<SkirmishObjectiveRoleComponent>, RefRO<UnitHealth>>())
            {
                if (roleRef.ValueRO.Role != role)
                    continue;
                found = true;
                if (health.ValueRO.Current > 0)
                    return true;
            }

            return !found;
        }
    }
}
