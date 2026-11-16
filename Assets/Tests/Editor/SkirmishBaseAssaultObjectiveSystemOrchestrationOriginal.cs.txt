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
        private EntityQuery sessions;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            state.RequireForUpdate<SkirmishObjectiveStateComponent>();
            designatedBases = state.GetEntityQuery(
                ComponentType.ReadOnly<SkirmishObjectiveRoleComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
            sessions = state.GetEntityQuery(
                ComponentType.ReadWrite<SkirmishExpandedSessionComponent>(),
                ComponentType.ReadWrite<SkirmishObjectiveStateComponent>(),
                ComponentType.ReadOnly<SkirmishResolvedSetupComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            float matchElapsed = 0f;
            bool surrender = false;
            if (SystemAPI.TryGetSingleton(out SkirmishMatchState match))
            {
                matchElapsed = match.ElapsedSeconds;
                surrender = match.SurrenderRequested != 0;
            }

            // No SystemAPI.Query foreach in this update. ReadFacts copies other
            // queries, and that is illegal while an idiomatic foreach is open.
            using NativeArray<Entity> entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (session.IsLegacy != 0 || session.Phase != SkirmishSessionPhase.Playing)
                    continue;
                SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(entity);
                if (objective.Kind != SkirmishObjectiveKind.BaseAssault || objective.Terminal != 0)
                    continue;

                SkirmishResolvedSetupComponent setup = em.GetComponentData<SkirmishResolvedSetupComponent>(entity);
                SkirmishBaseAssaultFacts facts = ReadFacts(
                    em,
                    entity,
                    designatedBases,
                    setup.DeadlineSeconds,
                    matchElapsed,
                    surrender);
                if (!TryEvaluate(in facts, out SkirmishOutcomeKind outcome, out SkirmishEndReasonKind reason))
                    continue;

                objective.Outcome = outcome;
                objective.Reason = reason;
                objective.Terminal = 1;
                objective.State = outcome == SkirmishOutcomeKind.Victory
                    ? SkirmishObjectiveStateKind.TerminalVictory
                    : outcome == SkirmishOutcomeKind.Defeat
                        ? SkirmishObjectiveStateKind.TerminalDefeat
                        : SkirmishObjectiveStateKind.TerminalDraw;
                em.SetComponentData(entity, objective);
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

        /// <summary>
        /// Writes the Base Assault outcome from the clock and designated-base facts.
        /// Does not award a result that the facts do not already show.
        /// </summary>
        public static bool TryPublishTerminal(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishObjectiveStateComponent>(session) ||
                !em.HasComponent<SkirmishObjectiveClockComponent>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))
                return false;

            SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            if (objective.Terminal != 0 || objective.Kind != SkirmishObjectiveKind.BaseAssault)
                return false;
            if (em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy != 0 ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase != SkirmishSessionPhase.Playing)
                return false;

            SkirmishObjectiveClockComponent clock = em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            int deadline = clock.DeadlineSeconds;
            if (deadline <= 0 && em.HasComponent<SkirmishResolvedSetupComponent>(session))
                deadline = em.GetComponentData<SkirmishResolvedSetupComponent>(session).DeadlineSeconds;

            var facts = new SkirmishBaseAssaultFacts
            {
                Playing = clock.Playing != 0,
                Paused = clock.Paused != 0,
                ElapsedSeconds = clock.ElapsedSeconds,
                DeadlineSeconds = deadline,
                PlayerDesignatedAlive = true,
                EnemyDesignatedAlive = true
            };
            if (em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                SkirmishBaseAssaultFactComponent stored = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                facts.PlayerDesignatedAlive = stored.PlayerDesignatedAlive != 0;
                facts.EnemyDesignatedAlive = stored.EnemyDesignatedAlive != 0;
                facts.ReplacementBarracksPresent = stored.ReplacementBarracksPresent != 0;
                facts.FieldArmyWiped = stored.FieldArmyWiped != 0;
            }

            if (!TryEvaluate(in facts, out SkirmishOutcomeKind outcome, out SkirmishEndReasonKind reason))
                return false;

            objective.Outcome = outcome;
            objective.Reason = reason;
            objective.Terminal = 1;
            objective.State = outcome == SkirmishOutcomeKind.Victory
                ? SkirmishObjectiveStateKind.TerminalVictory
                : outcome == SkirmishOutcomeKind.Defeat
                    ? SkirmishObjectiveStateKind.TerminalDefeat
                    : SkirmishObjectiveStateKind.TerminalDraw;
            em.SetComponentData(session, objective);
            return true;
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
