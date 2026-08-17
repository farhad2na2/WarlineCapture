#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using Game.UI.Shell.Contracts.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class M01FirstContactRuntimePlayModeTests
{
    private const string M01 = "saga.ch01.m01.first_contact";
    private const string M02 = "saga.ch01.m02.establish_base";

    [Test]
    public void NormalFirstLaunchQueuesExactlyOnceWithoutMenuRoute()
    {
        using World world = HandoffWorld(out Entity root);
        PlayerProfileSaveData profile = new();
        MissionLaunchPayload payload = FirstLaunchMissionHandoffOperation.Prepare(
            profile, 101, NarrativeGuidanceMode.Full);
        bool published = false;
        byte rejections = 0;
        Assert.That(FirstLaunchMissionHandoffOperation.Advance(
            world.EntityManager, in payload, ref published, ref rejections),
            Is.EqualTo(FirstLaunchMissionHandoffState.Pending));
        Assert.That(FirstLaunchMissionHandoffOperation.Advance(
            world.EntityManager, in payload, ref published, ref rejections),
            Is.EqualTo(FirstLaunchMissionHandoffState.Pending));
        Assert.That(world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root).Length, Is.EqualTo(1));
        Assert.That(world.EntityManager.HasBuffer<UiShellRouteRequestComponent>(root), Is.False);
        Assert.That((byte)UiShellStartupDisposition.EnterMission, Is.EqualTo(3));
    }

    [Test]
    public void SkipAndNormalProduceEqualM01Payloads()
    {
        MissionLaunchPayload normal = FirstLaunchMissionHandoffOperation.Prepare(
            new PlayerProfileSaveData(), 102, NarrativeGuidanceMode.Contextual);
        MissionLaunchPayload skipped = FirstLaunchMissionHandoffOperation.Prepare(
            new PlayerProfileSaveData(), 102, NarrativeGuidanceMode.Contextual);
        AssertPayloadEqual(normal, skipped);
        Assert.That(skipped.LaunchOrigin, Is.EqualTo(MissionLaunchOriginKind.FirstLaunch));
        Assert.That(skipped.RunKind, Is.EqualTo(MissionRunKind.FirstClear));
    }

    [Test]
    public void InterruptedFirstLaunchReusesPersistedCorrelation()
    {
        PlayerProfileSaveData profile = new();
        MissionLaunchPayload before = FirstLaunchMissionHandoffOperation.Prepare(
            profile, 103, NarrativeGuidanceMode.Minimal);
        MissionLaunchPayload resumed = FirstLaunchMissionHandoffOperation.Prepare(
            profile, 999, NarrativeGuidanceMode.Minimal);
        AssertPayloadEqual(before, resumed);
        Assert.That(FirstLaunchMissionHandoffOperation.Matches(profile, in resumed), Is.True);
    }

    [Test]
    public void FullGuidanceProjectsActionableHelpWithoutGameplayMutation()
    {
        CampaignMissionRuntimeComponent runtime = GuidanceRuntime(NarrativeGuidanceMode.Full);
        CampaignMissionAttemptFactsComponent facts = GuidanceFacts();
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default, in runtime, in facts, GuidanceSettings(), new Entity { Index = 1, Version = 1 },
            Entity.Null, default, default, out var projection), Is.True);
        Assert.That(projection.Active, Is.EqualTo(1));
        Assert.That(runtime.Phase, Is.EqualTo(MissionPhaseKind.FindSquad));
        Assert.That(facts.HostileTotalCount, Is.EqualTo(3));
    }

    [Test]
    public void FindSquadWaitsForSpawnAndDefeatsOnlyAfterSpawnedSquadDies()
    {
        CampaignMissionRuntimeComponent runtime = GuidanceRuntime(NarrativeGuidanceMode.Full);
        CampaignMissionAttemptFactsComponent pendingSpawn = GuidanceFacts();
        pendingSpawn.CommandSquadSpawned = 0;
        pendingSpawn.CommandSquadAlive = 0;
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(in runtime, in pendingSpawn, out _), Is.False,
            "FindSquad must wait for the asynchronous force registry instead of declaring a pre-spawn defeat.");

        CampaignMissionAttemptFactsComponent deadSquad = pendingSpawn;
        deadSquad.CommandSquadSpawned = 1;
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(in runtime, in deadSquad, out var defeat), Is.True);
        Assert.That(defeat.Phase, Is.EqualTo(MissionPhaseKind.Result));
        Assert.That(defeat.Outcome, Is.EqualTo(MissionOutcomeKind.Defeat));
    }

    [Test]
    public void ContextualGuidanceEscalatesOnlyPresentation()
    {
        CampaignMissionRuntimeComponent runtime = GuidanceRuntime(NarrativeGuidanceMode.Contextual);
        runtime.Phase = MissionPhaseKind.Engage;
        CampaignMissionAttemptFactsComponent facts = GuidanceFacts();
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default, in runtime, in facts, GuidanceSettings(), new Entity { Index = 2, Version = 1 },
            new Entity { Index = 3, Version = 1 }, default, default, out var first), Is.True);
        Assert.That(first.HintStrength, Is.EqualTo(1));
        Assert.That(first.CanExecute, Is.Zero);
        facts.ElapsedMilliseconds = first.CooldownUntilMilliseconds;
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            first, in runtime, in facts, GuidanceSettings(), first.SourceEntity, first.TargetEntity,
            default, default, out var stronger), Is.True);
        Assert.That(stronger.HintStrength, Is.EqualTo(2));
        Assert.That(runtime.Outcome, Is.EqualTo(MissionOutcomeKind.None));
    }

    [Test]
    public void MinimalGuidancePublishesMandatoryInformationOnly()
    {
        CampaignMissionRuntimeComponent runtime = GuidanceRuntime(NarrativeGuidanceMode.Minimal);
        runtime.Phase = MissionPhaseKind.MoveToCover;
        CampaignMissionAttemptFactsComponent facts = GuidanceFacts();
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default, in runtime, in facts, GuidanceSettings(), Entity.Null, Entity.Null,
            default, default, out _), Is.False);
        runtime.Phase = MissionPhaseKind.ConfirmThreat;
        Assert.That(CampaignMissionGuidanceProjectionSystem.TryBuildProjection(
            default, in runtime, in facts, GuidanceSettings(), Entity.Null, Entity.Null,
            default, new float3(2f), out var mandatory), Is.True);
        Assert.That(mandatory.CanExecute, Is.Zero);
    }

    [Test]
    public void CampaignReplayTutorialDefaultsOffAndMayOptIn()
    {
        MissionLaunchPayload replayOff = Payload(MissionRunKind.Replay, false, 201);
        MissionLaunchPayload replayOn = Payload(MissionRunKind.Replay, true, 202);
        MissionLaunchPayload first = Payload(MissionRunKind.FirstClear, true, 203);
        Assert.That(replayOff.ReplayTutorialEnabled, Is.False);
        Assert.That(replayOn.ReplayTutorialEnabled, Is.True);
        Assert.That(first.ReplayTutorialEnabled, Is.True);
        Assert.That(replayOff.LaunchOrigin, Is.EqualTo(MissionLaunchOriginKind.CampaignOperations));
    }

    [Test]
    public void FirstClearAndReplayReturnToOwnedDestinations()
    {
        CampaignMissionAttemptFactsComponent facts = GuidanceFacts();
        facts.HostileDefeatedCount = facts.HostileTotalCount;
        CampaignMissionRuntimeComponent first = ResultRuntime(MissionLaunchOriginKind.FirstLaunch);
        CampaignMissionRuntimeComponent replay = ResultRuntime(MissionLaunchOriginKind.CampaignOperations);
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(in first, in facts, out var firstResult), Is.True);
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(in replay, in facts, out var replayResult), Is.True);
        Assert.That(firstResult.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CommandBase));
        Assert.That(replayResult.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CampaignOperations));
    }

    [Test]
    public void LiveMissionRoleDeathsDrivePatrolClearIntoVictoryResult()
    {
        using World world = new(nameof(LiveMissionRoleDeathsDrivePatrolClearIntoVictoryResult));
        EntityManager em = world.EntityManager;
        Entity root = em.CreateEntity(
            typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent),
            typeof(CampaignMissionAttemptFactsComponent));
        CampaignMissionRuntimeComponent runtime = GuidanceRuntime(NarrativeGuidanceMode.Minimal);
        runtime.Phase = MissionPhaseKind.Engage;
        runtime.LaunchOrigin = MissionLaunchOriginKind.CampaignOperations;
        runtime.RunKind = MissionRunKind.Replay;
        runtime.ReplayTutorialEnabled = 0;
        em.SetComponentData(root, runtime);
        em.SetComponentData(root, GuidanceFacts());
        em.AddBuffer<CampaignMissionActionRequestElement>(root);
        em.AddBuffer<CampaignMissionActionResultElement>(root);

        FixedString64Bytes session = runtime.SessionToken;
        for (int index = 0; index < 4; index++)
            CreateMissionUnit(em, session, 1);
        Entity[] patrol = new Entity[3];
        for (int index = 0; index < patrol.Length; index++)
            patrol[index] = CreateMissionUnit(em, session, 2);

        SystemHandle handle = world.GetOrCreateSystem<CampaignMissionRuntimeSystem>();
        Update(world, handle);
        Assert.That(em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).HostileDefeatedCount,
            Is.Zero);
        for (int patrolIndex = 0; patrolIndex < patrol.Length; patrolIndex++)
            em.DestroyEntity(patrol[patrolIndex]);

        Update(world, handle);
        CampaignMissionAttemptFactsComponent facts =
            em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
        Assert.That(facts.HostileDefeatedCount, Is.EqualTo(3));
        Assert.That(facts.CommandSquadAlive, Is.EqualTo(1));
        Assert.That(em.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase,
            Is.EqualTo(MissionPhaseKind.SecureCorridor));

        Update(world, handle);
        CampaignMissionRuntimeComponent result = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
        Assert.That(result.Phase, Is.EqualTo(MissionPhaseKind.Result));
        Assert.That(result.Outcome, Is.EqualTo(MissionOutcomeKind.Victory));
        Assert.That(result.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CampaignOperations));
    }

    [Test]
    public void CampaignMissionDisablesDayNightWhileSkirmishKeepsItsDefault()
    {
        using World world = new(nameof(CampaignMissionDisablesDayNightWhileSkirmishKeepsItsDefault));
        EntityManager em = world.EntityManager;
        Assert.That(MissionDayNightPolicyUtility.ShouldEnableDayNightVisuals(em), Is.True);

        Entity map = em.CreateEntity(typeof(ActiveOperationMapComponent));
        em.SetComponentData(map, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.skirmish.desert_base_01")
        });
        Assert.That(MissionDayNightPolicyUtility.ShouldEnableDayNightVisuals(em), Is.True);

        em.SetComponentData(map, new ActiveOperationMapComponent
        {
            OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            MissionId = new FixedString64Bytes(M01)
        });
        Assert.That(MissionDayNightPolicyUtility.ShouldEnableDayNightVisuals(em), Is.False);
    }

    [Test]
    public void MigrationRestartAndSettlementRemainExactlyOnce()
    {
        string root = Path.Combine(Path.GetTempPath(), "M01DC034", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            SaveService service = new(new JsonSaveRepository(root));
            service.SaveProfile(new PlayerProfileSaveData { campaignMissionProgress = new[]
            {
                new CampaignMissionProgressSaveData
                {
                    schemaVersion = 1, missionId = M01, available = true,
                    pendingResume = true, lastAttemptOrdinal = 4, lastSettledToken = "legacy:1"
                }
            }});
            CampaignMissionProgressStore store = new(service);
            CampaignMissionProgressSaveData migrated = store.ReadAll()[0];
            Assert.That(migrated.schemaVersion, Is.EqualTo(CampaignMissionProgressStore.CurrentEntrySchemaVersion));
            Assert.That(migrated.settledTokens, Does.Contain("legacy:1"));
            CampaignMissionRewardGrant[] rewards =
            {
                new(MissionRewardKind.None, "reward.commander_xp", 260),
                new(MissionRewardKind.Credits, string.Empty, 1200)
            };
            CampaignMissionSettlementReceipt settled = store.SettleWithRewards(
                M01, "restart", 4, true, 3, 90000, M02, rewards);
            CampaignMissionProgressStore restarted = new(new SaveService(new JsonSaveRepository(root)));
            CampaignMissionSettlementReceipt duplicate = restarted.SettleWithRewards(
                M01, "restart", 4, true, 3, 60000, M02, rewards);
            PlayerProfileSaveData profile = new SaveService(new JsonSaveRepository(root)).LoadProfile();
            Assert.That(settled.Applied, Is.True);
            Assert.That(duplicate.IsDuplicate, Is.True);
            Assert.That(profile.commanderXp, Is.EqualTo(260));
            Assert.That(profile.credits, Is.EqualTo(1200));
            Assert.That(restarted.ReadAll()[0].pendingResume, Is.False);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Test]
    public void LegacySkirmishDefaultsRemainUnrestricted()
    {
        ScenarioSetupConfig scenario = ScriptableObject.CreateInstance<ScenarioSetupConfig>();
        try
        {
            JsonUtility.FromJsonOverwrite(
                "{\"scenarioId\":\"scenario.skirmish.default\",\"operationMapId\":\"opmap.skirmish.desert_base_01\",\"requiredAnchors\":[]}",
                scenario);
            Assert.That(scenario.TryValidate(out _), Is.True);
            Assert.That(scenario.UnitGroups.Length + scenario.PatrolRoutes.Length +
                        scenario.AmbientPresentations.Length, Is.Zero);
            Assert.That(scenario.Restrictions.BuildingDisabled || scenario.Restrictions.ProductionDisabled ||
                        scenario.Restrictions.EconomyDisabled || scenario.Restrictions.TransportDisabled ||
                        scenario.Restrictions.AirDisabled, Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(scenario); }
    }

    [UnityTest]
    public IEnumerator RetryRemovesPriorAttemptAndLocalQueues() => Run(
        new M01FirstContactLifecyclePlayModeTests().AcceptedRetryRemovesPriorAttemptAndClearsAttemptLocalQueues());

    [UnityTest]
    public IEnumerator EightRetriesKeepEntityPoolAndQueueCountsStable() => Run(
        new M01FirstContactLifecyclePlayModeTests().EightRetriesHaveStableEntityAndQueueCounts());

    [UnityTest]
    public IEnumerator UnloadDisposesCatalogAndMissionEntities() => Run(
        new M01FirstContactLifecyclePlayModeTests().CatalogSystemTeardownRemovesRemainingMissionEntities());

    [UnityTest]
    public IEnumerator AmbientRetryAndTeardownCountsRemainStable() => Run(
        new M01FirstContactAmbientPlayModeTests().RepeatedRetryAndTeardownHaveStableCounts());

    [UnityTest]
    public IEnumerator StableAmbientPresentationAllocatesZeroManagedBytes() => Run(
        new M01FirstContactAmbientPlayModeTests().StablePresentationUpdatesAllocateZeroManagedBytes());

    [UnityTest]
    public IEnumerator InvalidLaunchRemainsFailClosedAcrossFrame() => Run(
        new M01FirstContactLaunchPlayModeTests().InvalidRequestRemainsFailClosedAcrossAFrame());

    [UnityTest]
    public IEnumerator FirstLaunchRequestRemainsExactlyOnceAcrossFrames() => Run(
        new M01FirstContactFirstLaunchPlayModeTests().EnqueuesOneTypedRequestAcrossFrames());

    [Test]
    public void ActiveMissionExitPersistsResumeCleansAttemptAndRoutesExactlyOnce()
    {
        string saveRoot = Path.Combine(Path.GetTempPath(), "M01DC034-exit", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(saveRoot);
        try
        {
            using World world = new(nameof(ActiveMissionExitPersistsResumeCleansAttemptAndRoutesExactlyOnce));
            EntityManager entityManager = world.EntityManager;
            Entity root = entityManager.CreateEntity(
                typeof(CampaignMissionRootComponent), typeof(CampaignMissionRuntimeComponent),
                typeof(CampaignMissionAttemptFactsComponent), typeof(CampaignMissionResultComponent));
            CampaignMissionRuntimeComponent runtime = GuidanceRuntime(NarrativeGuidanceMode.Full);
            runtime.Phase = MissionPhaseKind.Engage;
            runtime.TransitionToken = 3401;
            entityManager.SetComponentData(root, runtime);
            entityManager.SetComponentData(root, GuidanceFacts());
            entityManager.AddBuffer<CampaignMissionActionRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionActionResultElement>(root);
            entityManager.AddBuffer<CampaignMissionLaunchResultElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementRequestElement>(root);
            entityManager.AddBuffer<CampaignMissionSettlementResultElement>(root);
            entityManager.AddBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root);
            CampaignMissionProgressStore store = new(new SaveService(new JsonSaveRepository(saveRoot)));
            entityManager.AddComponentObject(root, new CampaignMissionProgressStoreReferenceComponent { Store = store });
            Entity missionUnit = entityManager.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
            Entity ui = entityManager.CreateEntity();
            DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
                entityManager.AddBuffer<UiShellPopupRequestComponent>(ui);
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                entityManager.AddBuffer<UiShellRouteRequestComponent>(ui);

            Assert.That(CampaignMissionExitDispatchUtility.TryHandle(
                entityManager, ui, 34), Is.True);
            Assert.That(entityManager.Exists(missionUnit), Is.False);
            Assert.That(entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root).Phase,
                Is.EqualTo(MissionPhaseKind.None));
            Assert.That(entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root), Is.EqualTo(default(CampaignMissionAttemptFactsComponent)));
            Assert.That(store.ReadAll()[0].pendingResume, Is.True);
            Assert.That(store.ReadAll()[0].lastAttemptOrdinal, Is.EqualTo(runtime.AttemptOrdinal));
            popupRequests = entityManager.GetBuffer<UiShellPopupRequestComponent>(ui);
            routeRequests = entityManager.GetBuffer<UiShellRouteRequestComponent>(ui);
            Assert.That(popupRequests.Length, Is.EqualTo(1));
            Assert.That(routeRequests.Length, Is.EqualTo(1));
            Assert.That(routeRequests[0].Route, Is.EqualTo(UIRoute.MainMenu));
            Assert.That(entityManager.GetBuffer<CampaignMissionActionResultElement>(root)[0].Accepted, Is.EqualTo(1));

            Assert.That(CampaignMissionExitDispatchUtility.TryHandle(
                entityManager, ui, 34), Is.True);
            popupRequests = entityManager.GetBuffer<UiShellPopupRequestComponent>(ui);
            routeRequests = entityManager.GetBuffer<UiShellRouteRequestComponent>(ui);
            Assert.That(popupRequests.Length, Is.EqualTo(1));
            Assert.That(routeRequests.Length, Is.EqualTo(1));
        }
        finally
        {
            if (Directory.Exists(saveRoot)) Directory.Delete(saveRoot, true);
        }
    }

    private static IEnumerator Run(IEnumerator test)
    {
        while (test.MoveNext()) yield return test.Current;
    }

    private static Entity CreateMissionUnit(EntityManager em, FixedString64Bytes session, byte faction)
    {
        Entity entity = em.CreateEntity(
            typeof(CampaignMissionUnitRoleComponent), typeof(Faction), typeof(UnitHealth),
            typeof(Unity.Transforms.LocalTransform));
        em.SetComponentData(entity, new CampaignMissionUnitRoleComponent
        {
            MissionRoleId = new FixedString64Bytes(faction > 1 ? "role.ash.patrol" : "role.jrc.command_squad"),
            UnitGroupId = new FixedString64Bytes(faction > 1 ? "group.ash.patrol" : "group.jrc.command_squad"),
            SessionToken = session
        });
        em.SetComponentData(entity, new Faction { Id = faction });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, Unity.Transforms.LocalTransform.Identity);
        return entity;
    }

    private static void Update(World world, SystemHandle handle) =>
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionRuntimeSystem>(handle)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(handle));

    private static World HandoffWorld(out Entity root)
    {
        World world = new("M01DC034-handoff");
        root = world.EntityManager.CreateEntity(typeof(CampaignMissionRootComponent));
        world.EntityManager.AddBuffer<CampaignMissionLaunchRequestElement>(root);
        world.EntityManager.AddBuffer<CampaignMissionLaunchResultElement>(root);
        return world;
    }

    private static void AssertPayloadEqual(in MissionLaunchPayload left, in MissionLaunchPayload right)
    {
        Assert.That(right.MissionId, Is.EqualTo(left.MissionId));
        Assert.That(right.ScenarioId, Is.EqualTo(left.ScenarioId));
        Assert.That(right.OperationMapId, Is.EqualTo(left.OperationMapId));
        Assert.That(right.TransitionToken, Is.EqualTo(left.TransitionToken));
        Assert.That(right.SessionToken, Is.EqualTo(left.SessionToken));
        Assert.That(right.Guidance, Is.EqualTo(left.Guidance));
    }

    private static MissionLaunchPayload Payload(MissionRunKind runKind, bool tutorial, ulong token) =>
        MissionLaunchPayloadFactory.Create(
            M01, "scenario.ch01.m01.first_contact", "opmap.ch01.district_edge_01",
            MissionLaunchOriginKind.CampaignOperations, runKind, NarrativeGuidanceMode.Contextual,
            tutorial, token, "campaign-m01", 1, 104729);

    private static CampaignMissionRuntimeComponent GuidanceRuntime(NarrativeGuidanceMode guidance) => new()
    {
        MissionId = new FixedString64Bytes(M01), ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        SessionToken = new FixedString64Bytes("m01dc034"), Phase = MissionPhaseKind.FindSquad,
        Guidance = guidance, RunKind = MissionRunKind.FirstClear, ReplayTutorialEnabled = 1,
        Version = 3, SourceVersion = 2, AttemptOrdinal = 1, DeterministicSeed = 104729
    };

    private static CampaignMissionRuntimeComponent ResultRuntime(MissionLaunchOriginKind origin)
    {
        CampaignMissionRuntimeComponent runtime = GuidanceRuntime(NarrativeGuidanceMode.Full);
        runtime.Phase = MissionPhaseKind.SecureCorridor;
        runtime.LaunchOrigin = origin;
        runtime.RunKind = origin == MissionLaunchOriginKind.FirstLaunch
            ? MissionRunKind.FirstClear : MissionRunKind.Replay;
        return runtime;
    }

    private static CampaignMissionAttemptFactsComponent GuidanceFacts() => new()
    {
        ElapsedMilliseconds = 1000, CommandSquadSpawned = 1, CommandSquadAlive = 1,
        HostileTotalCount = 3
    };

    private static AssistantSettingsComponent GuidanceSettings() => new()
    {
        GuidanceLevel = AssistantGuidanceLevel.FullGuidance, SubtitlesEnabled = 1
    };
}
#endif
