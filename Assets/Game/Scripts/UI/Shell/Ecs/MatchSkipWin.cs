using Game.Components;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    /// <summary>
    /// Ends the live campaign, operations, or skirmish match as a saved three-star win.
    /// </summary>
    internal static class MatchSkipWin
    {
        public static bool TryApply()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;
            EntityManager em = world.EntityManager;
            if (TryCampaign(em) || TryOperations(em) || TrySkirmish(em))
            {
                FreezeSimulation(em);
                return true;
            }
            return false;
        }

        private static bool TryCampaign(EntityManager em)
        {
            using EntityQuery roots = em.CreateEntityQuery(typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent));
            if (roots.CalculateEntityCount() != 1)
                return false;
            Entity root = roots.GetSingletonEntity();
            CampaignMissionRuntimeComponent runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (runtime.SessionToken.IsEmpty || runtime.MissionId.IsEmpty ||
                runtime.Phase >= MissionPhaseKind.Result || runtime.Outcome != MissionOutcomeKind.None)
                return false;

            if (runtime.Version == 0)
                runtime.Version = 1;
            runtime.Phase = MissionPhaseKind.Result;
            runtime.Outcome = MissionOutcomeKind.Victory;
            runtime.ReturnDestination = runtime.LaunchOrigin == MissionLaunchOriginKind.FirstLaunch
                ? MissionReturnDestinationKind.CommandBase
                : MissionReturnDestinationKind.CampaignOperations;
            em.SetComponentData(root, runtime);

            int elapsed = 0;
            if (em.HasComponent<CampaignMissionAttemptFactsComponent>(root))
                elapsed = math.max(0, em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds);

            var result = new CampaignMissionResultComponent
            {
                MissionId = runtime.MissionId,
                SessionToken = runtime.SessionToken,
                AttemptOrdinal = runtime.AttemptOrdinal,
                SourceVersion = runtime.Version,
                Outcome = MissionOutcomeKind.Victory,
                ReturnDestination = runtime.ReturnDestination,
                Stars = 3,
                ElapsedMilliseconds = elapsed,
                SquadLossCount = 0,
                CivilianLossCount = 0
            };
            if (em.HasComponent<CampaignMissionResultComponent>(root))
                em.SetComponentData(root, result);
            else
                em.AddComponentData(root, result);

            DynamicBuffer<CampaignMissionSettlementRequestElement> requests =
                em.HasBuffer<CampaignMissionSettlementRequestElement>(root)
                    ? em.GetBuffer<CampaignMissionSettlementRequestElement>(root)
                    : em.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            bool queued = false;
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].SourceVersion == runtime.Version &&
                    requests[i].Outcome == MissionOutcomeKind.Victory)
                    queued = true;
            }
            if (!queued)
            {
                requests.Add(new CampaignMissionSettlementRequestElement
                {
                    SourceVersion = runtime.Version,
                    MissionId = runtime.MissionId,
                    SessionToken = runtime.SessionToken,
                    AttemptOrdinal = runtime.AttemptOrdinal,
                    Outcome = MissionOutcomeKind.Victory
                });
            }

            if (em.HasComponent<CampaignMissionFinalKillCinematicComponent>(root))
            {
                CampaignMissionFinalKillCinematicComponent cinematic =
                    em.GetComponentData<CampaignMissionFinalKillCinematicComponent>(root);
                cinematic.Active = 0;
                em.SetComponentData(root, cinematic);
            }
            return true;
        }

        private static bool TryOperations(EntityManager em)
        {
            using EntityQuery missions = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
            if (missions.CalculateEntityCount() != 1)
                return false;
            Entity root = missions.GetSingletonEntity();
            OperationsReconMissionComponent mission = em.GetComponentData<OperationsReconMissionComponent>(root);
            if (mission.Phase != OperationsReconPhase.Playing)
                return false;

            mission.Phase = OperationsReconPhase.Terminal;
            mission.Outcome = OperationsReconOutcome.Victory;
            mission.CompletedScans = 3;
            mission.InfantryAtExit = math.max(mission.InfantryAtExit, 2);
            mission.SurvivingInfantry = math.max(mission.SurvivingInfantry, mission.InfantryAtExit);
            mission.MasteryCompleted = 1;
            mission.ReconLostBeforeScansComplete = 0;
            em.SetComponentData(root, mission);

            if (em.HasBuffer<OperationsReconSiteElement>(root))
            {
                DynamicBuffer<OperationsReconSiteElement> sites = em.GetBuffer<OperationsReconSiteElement>(root);
                for (int i = 0; i < sites.Length; i++)
                {
                    OperationsReconSiteElement site = sites[i];
                    site.Completed = 1;
                    site.Actor = Entity.Null;
                    sites[i] = site;
                }
            }
            if (em.HasComponent<OperationsReconEvidenceComponent>(root))
            {
                OperationsReconEvidenceComponent evidence = em.GetComponentData<OperationsReconEvidenceComponent>(root);
                evidence.Recovered = 1;
                evidence.Actor = Entity.Null;
                em.SetComponentData(root, evidence);
            }
            return true;
        }

        private static bool TrySkirmish(EntityManager em)
        {
            using EntityQuery matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.CalculateEntityCount() != 1)
                return false;
            Entity session = matches.GetSingletonEntity();
            SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
            if (match.Phase is not (SkirmishPhase.Preparing or SkirmishPhase.Playing))
                return false;

            match.Phase = SkirmishPhase.Finished;
            match.Outcome = SkirmishOutcome.Victory;
            match.Reason = SkirmishEndReason.MainBaseDestroyed;
            match.PlayerUnitsLost = 0;
            match.PlayerBuildingsLost = 0;
            em.SetComponentData(session, match);

            if (em.HasComponent<SkirmishExpandedSessionComponent>(session))
            {
                SkirmishExpandedSessionComponent expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                expanded.Phase = SkirmishSessionPhase.Finished;
                em.SetComponentData(session, expanded);
                var result = new SkirmishResultComponent
                {
                    Outcome = SkirmishOutcomeKind.Victory,
                    Reason = SkirmishEndReasonKind.MainBaseDestroyed,
                    SetupHash = expanded.SetupHash,
                    SessionId = expanded.SessionId,
                    Frozen = 1,
                    SaveAcknowledged = 0
                };
                if (em.HasComponent<SkirmishResultComponent>(session))
                    em.SetComponentData(session, result);
                else
                    em.AddComponentData(session, result);
            }
            if (em.HasComponent<SkirmishObjectiveStateComponent>(session))
            {
                SkirmishObjectiveStateComponent objective = em.GetComponentData<SkirmishObjectiveStateComponent>(session);
                objective.State = SkirmishObjectiveStateKind.TerminalVictory;
                objective.Outcome = SkirmishOutcomeKind.Victory;
                objective.Reason = SkirmishEndReasonKind.MainBaseDestroyed;
                objective.Terminal = 1;
                em.SetComponentData(session, objective);
            }
            if (em.HasComponent<SkirmishBaseAssaultFactComponent>(session))
            {
                SkirmishBaseAssaultFactComponent facts = em.GetComponentData<SkirmishBaseAssaultFactComponent>(session);
                facts.PlayerDesignatedAlive = 1;
                facts.EnemyDesignatedAlive = 0;
                em.SetComponentData(session, facts);
            }
            return true;
        }

        private static void FreezeSimulation(EntityManager em)
        {
            using EntityQuery gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1)
                return;
            Entity entity = gameplay.GetSingletonEntity();
            RuntimeGameplayStateComponent state = em.GetComponentData<RuntimeGameplayStateComponent>(entity);
            state.SimulationActive = 0;
            state.PlayRequested = 0;
            state.SelectionModeActive = 0;
            state.BuildModeActive = 0;
            em.SetComponentData(entity, state);
        }
    }
}
