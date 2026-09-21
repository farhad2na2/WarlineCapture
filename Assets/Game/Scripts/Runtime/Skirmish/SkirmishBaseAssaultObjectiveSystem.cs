using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SkirmishOutcomeSystem))]
    public partial struct SkirmishBaseAssaultObjectiveSystem : ISystem
    {
        private EntityQuery designatedBases;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            state.RequireForUpdate<SkirmishObjectiveStateComponent>();
            designatedBases = state.GetEntityQuery(
                ComponentType.ReadOnly<SkirmishObjectiveRoleComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session,
                      RefRW<SkirmishObjectiveStateComponent> objective,
                      RefRO<SkirmishResolvedSetupComponent> setup,
                      Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>,
                         RefRW<SkirmishObjectiveStateComponent>,
                         RefRO<SkirmishResolvedSetupComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.Phase != SkirmishSessionPhase.Playing)
                    continue;
                if (objective.ValueRO.Kind != SkirmishObjectiveKind.BaseAssault || objective.ValueRO.Terminal != 0)
                    continue;

                float matchElapsed = 0f;
                bool surrender = false;
                if (SystemAPI.TryGetSingleton(out SkirmishMatchState match))
                {
                    matchElapsed = match.ElapsedSeconds;
                    surrender = match.SurrenderRequested != 0;
                }

                SkirmishBaseAssaultFacts facts = ReadFacts(
                    em,
                    entity,
                    designatedBases,
                    setup.ValueRO.DeadlineSeconds,
                    matchElapsed,
                    surrender);
                if (!TryEvaluate(in facts, out SkirmishOutcomeKind outcome, out SkirmishEndReasonKind reason))
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
            var facts = new SkirmishBaseAssaultFacts
            {
                PlayerDesignatedAlive = playerBaseAlive,
                EnemyDesignatedAlive = enemyBaseAlive,
                ElapsedSeconds = elapsedSeconds,
                DeadlineSeconds = deadlineSeconds,
                Surrender = surrender,
                Playing = true,
                Paused = false
            };
            return TryEvaluate(in facts, out outcome, out reason);
        }

        public static bool TryEvaluate(
            in SkirmishBaseAssaultFacts facts,
            out SkirmishOutcomeKind outcome,
            out SkirmishEndReasonKind reason)
        {
            outcome = SkirmishOutcomeKind.None;
            reason = SkirmishEndReasonKind.None;
            if (!facts.Playing)
                return false;

            if (facts.Surrender)
            {
                outcome = SkirmishOutcomeKind.Defeat;
                reason = SkirmishEndReasonKind.Surrender;
                return true;
            }

            if (!facts.PlayerDesignatedAlive && !facts.EnemyDesignatedAlive)
            {
                outcome = SkirmishOutcomeKind.Draw;
                reason = SkirmishEndReasonKind.BothBasesDestroyed;
                return true;
            }

            if (!facts.EnemyDesignatedAlive)
            {
                outcome = SkirmishOutcomeKind.Victory;
                reason = SkirmishEndReasonKind.MainBaseDestroyed;
                return true;
            }

            if (!facts.PlayerDesignatedAlive)
            {
                outcome = SkirmishOutcomeKind.Defeat;
                reason = SkirmishEndReasonKind.MainBaseDestroyed;
                return true;
            }

            if (facts.Paused)
                return false;

            if (facts.DeadlineSeconds > 0 && facts.ElapsedSeconds >= facts.DeadlineSeconds)
            {
                outcome = SkirmishOutcomeKind.Draw;
                reason = SkirmishEndReasonKind.TimeLimit;
                return true;
            }

            _ = facts.ReplacementBarracksPresent;
            _ = facts.FieldArmyWiped;
            return false;
        }

        private static SkirmishBaseAssaultFacts ReadFacts(
            EntityManager em,
            Entity session,
            EntityQuery designated,
            int deadlineSeconds,
            float matchElapsedSeconds,
            bool surrender)
        {
            bool playerAlive = IsDesignatedBaseAlive(em, designated, SkirmishObjectiveRoleKind.PlayerBase);
            bool enemyAlive = IsDesignatedBaseAlive(em, designated, SkirmishObjectiveRoleKind.EnemyBase);
            var facts = new SkirmishBaseAssaultFacts
            {
                PlayerDesignatedAlive = playerAlive,
                EnemyDesignatedAlive = enemyAlive,
                Playing = true,
                DeadlineSeconds = deadlineSeconds,
                ElapsedSeconds = matchElapsedSeconds,
                Surrender = surrender
            };
            if (em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                SkirmishBaseAssaultFactComponent stored = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                facts.PlayerDesignatedAlive = stored.PlayerDesignatedAlive != 0;
                facts.EnemyDesignatedAlive = stored.EnemyDesignatedAlive != 0;
                facts.ReplacementBarracksPresent = stored.ReplacementBarracksPresent != 0;
                facts.FieldArmyWiped = stored.FieldArmyWiped != 0;
            }

            if (em.HasComponent<SkirmishObjectiveClockComponent>(session))
            {
                SkirmishObjectiveClockComponent clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
                facts.ElapsedSeconds = clock.ElapsedSeconds;
                facts.DeadlineSeconds = clock.DeadlineSeconds > 0 ? clock.DeadlineSeconds : deadlineSeconds;
                facts.Paused = clock.Paused != 0;
                facts.Playing = clock.Playing != 0;
            }

            return facts;
        }

        private static bool IsDesignatedBaseAlive(
            EntityManager em,
            EntityQuery query,
            SkirmishObjectiveRoleKind role)
        {
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            bool found = false;
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]).Role != role)
                    continue;
                found = true;
                if (em.GetComponentData<UnitHealth>(entities[i]).Current > 0)
                    return true;
            }

            return !found;
        }
    }
}
