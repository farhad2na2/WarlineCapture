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
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionCatalogComponent),
            typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent));
        em.SetComponentData(root, CreateCatalog());
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

        world.GetOrCreateSystem<CampaignMissionCatalogDisposalSystem>();
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

    private static CampaignMissionCatalogComponent CreateCatalog()
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
        catalog.SchemaVersion = 1;
        BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
        missions[0].MissionId = "saga.ch01.m01.first_contact";
        missions[0].ScenarioId = "scenario.ch01.m01.first_contact";
        missions[0].OperationMapId = "opmap.ch01.district_edge_01";
        BlobBuilderArray<CampaignMissionObjectiveBlob> objectives =
            builder.Allocate(ref missions[0].Objectives, 2);
        objectives[0] = new CampaignMissionObjectiveBlob
        {
            ObjectiveId = "obj.ch01.m01.destroy_patrol",
            DisplayTextKey = "mission.m01.objective.secure_corridor",
            MissionRoleId = "role.hostile.patrol",
            Rule = MissionObjectiveRuleKind.DestroyMissionRole,
            RequiredCount = 3
        };
        objectives[1] = new CampaignMissionObjectiveBlob
        {
            ObjectiveId = "obj.ch01.m01.keep_command_squad_alive",
            DisplayTextKey = "mission.m01.failure.command_squad_destroyed",
            MissionRoleId = "role.friendly.command_squad",
            Rule = MissionObjectiveRuleKind.ProtectMissionRole,
            RequiredCount = 1,
            FailureOnRuleBreak = 1
        };
        return new CampaignMissionCatalogComponent
        {
            Blob = builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent),
            SourceVersion = 8,
            OwnsBlob = 1
        };
    }
}
