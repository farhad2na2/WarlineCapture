using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishStartupPolicy
    {
        public const double TimeoutSeconds = 120;

        // Failed startup is not a match result. Keep the first useful reason and
        // let the player retry through normal teardown with a fresh session.
        public static bool Fail(EntityManager em, SkirmishStartupFailureCode reason)
        {
            using var sessions = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (reason == SkirmishStartupFailureCode.None || sessions.CalculateEntityCount() != 1) return false;
            var session = sessions.GetSingletonEntity();
            var match = em.GetComponentData<SkirmishMatchState>(session);
            if (match.Phase >= SkirmishPhase.Playing || match.StartupFailure != SkirmishStartupFailureCode.None) return false;
            match.StartupFailure = reason;
            em.SetComponentData(session, match);
            return true;
        }

        public static void StopFailedStartup(EntityManager em)
        {
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1) return;
            var state = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
            state.PlayRequested = 0;
            state.SimulationActive = 0;
            state.SelectionModeActive = 0;
            state.BuildModeActive = 0;
            em.SetComponentData(gameplay.GetSingletonEntity(), state);
        }
    }
}
