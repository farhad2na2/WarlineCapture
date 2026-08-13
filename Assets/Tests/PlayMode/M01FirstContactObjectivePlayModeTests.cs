using System.Collections;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TestTools;

public sealed class M01FirstContactObjectivePlayModeTests
{
    [UnityTest]
    public IEnumerator ObjectiveProjectionFeedsHudAndAssistantWithoutCompetingTruth()
    {
        using World world = new(nameof(M01FirstContactObjectivePlayModeTests));
        EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity(
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent));
        em.SetComponentData(root, new CampaignMissionRuntimeComponent
        {
            MissionId = new FixedString64Bytes("saga.ch01.m01.first_contact"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
            SessionToken = new FixedString64Bytes("playmode-objective"),
            Phase = MissionPhaseKind.Engage,
            LaunchOrigin = MissionLaunchOriginKind.FirstLaunch,
            RunKind = MissionRunKind.FirstClear,
            Version = 3,
            SourceVersion = 8,
            AttemptOrdinal = 1,
            DeterministicSeed = 8001
        });
        em.SetComponentData(root, new CampaignMissionAttemptFactsComponent
        {
            ElapsedMilliseconds = 125000,
            HostileTotalCount = 3,
            HostileDefeatedCount = 2,
            CommandSquadSpawned = 1,
            CommandSquadAlive = 1
        });
        Entity boundary = em.CreateEntity(
            typeof(UiShellRootComponent), typeof(MatchObjectiveProjectionBoundaryComponent),
            typeof(UiShellStateComponent),
            typeof(UiMatchHudHeaderComponent), typeof(UiMatchHudStatusSurfacesComponent));
        em.SetComponentData(boundary, new UiShellStateComponent
        {
            ActiveRoute = UIRoute.Match,
            CurrentMode = UiShellMode.MatchHud,
            Phase = UiShellTransitionPhase.MatchHudReady
        });
        Entity matchStart = em.CreateEntity(typeof(MatchStartQueueComponent));
        em.SetComponentData(matchStart, new MatchStartQueueComponent { HasStarted = 1 });

        SystemHandle writer = world.GetOrCreateSystem<CampaignMissionObjectiveProjectionSystem>();
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionObjectiveProjectionSystem>(writer)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(writer));
        SystemHandle reader = world.GetOrCreateSystem<AssistantGoalReadModelSystem>();
        world.Unmanaged.GetUnsafeSystemRef<AssistantGoalReadModelSystem>(reader)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(reader));

        MatchObjectiveRuntimeStateComponent beforeReader =
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        DynamicBuffer<AssistantGoalReadModelElement> goals =
            em.GetBuffer<AssistantGoalReadModelElement>(boundary);
        UiMatchHudStatusSurfacesComponent hud = em.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
        Assert.AreEqual(2, goals.Length);
        Assert.AreEqual("Destroy the hostile patrol", hud.Objective0Text.ToString());
        Assert.AreEqual("TIME 2:05", hud.ElapsedText.ToString());
        Assert.AreEqual(beforeReader.Version,
            em.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary).Version);
        yield break;
    }
}
