using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class AssistantReadModelSystemTests
{
    private World _world;
    private EntityManager _entityManager;
    private SystemHandle _goalSystem;
    private SystemHandle _recommendationSystem;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantGoalReadModelSystem_PublishesObjectiveGoals());
            passed++;
            RunCase(test => test.AssistantRecommendationSystem_PublishesObjectiveRecommendation());
            passed++;
            RunCase(test => test.AssistantRecommendationSystem_PublishesNoSelectionRecommendation());
            passed++;
            RunCase(test => test.AssistantRecommendationSystem_PublishesFuelLogisticsWarningRecommendation());
            passed++;
            RunCase(test => test.AssistantRecommendationSystem_PublishesSelectedIdleCombatUnitRecommendation());
            passed++;
            RunCase(test => test.AssistantReadModels_DoNotRepublishWhenSourcesAreUnchanged());
            passed++;
            RunCase(test => test.AssistantGoalReadModelSystem_NoMissionPublishesNoGoals());
            passed++;
            RunCase(test => test.AssistantGoalReadModelSystem_PreStartClearsGoals());
            passed++;
            RunCase(test => test.AssistantBoundary_FitsFullUiShellArchetype());
            passed++;

            Debug.Log($"[AssistantReadModelValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantReadModelValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantReadModelSystemTests> testCase)
    {
        var tests = new AssistantReadModelSystemTests();
        tests.SetUp();
        try
        {
            testCase(tests);
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World("AssistantReadModelSystemTests");
        _entityManager = _world.EntityManager;
        _goalSystem = _world.CreateSystem<AssistantGoalReadModelSystem>();
        _recommendationSystem = _world.CreateSystem<AssistantRecommendationSystem>();
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void AssistantGoalReadModelSystem_PublishesObjectiveGoals()
    {
        Entity boundary = CreateBoundary(withMission: true);

        _goalSystem.Update(_world.Unmanaged);

        Assert.IsTrue(_entityManager.HasComponent<AssistantStateComponent>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantGoalReadModelElement>(boundary));

        DynamicBuffer<AssistantGoalReadModelElement> goals =
            _entityManager.GetBuffer<AssistantGoalReadModelElement>(boundary);
        Assert.AreEqual(3, goals.Length);
        Assert.AreEqual(1, goals[0].GoalId);
        Assert.AreEqual(AssistantGoalState.Active, goals[0].State);
        Assert.AreEqual(AssistantMessagePriority.High, goals[0].Priority);
        Assert.AreEqual("Neutralize hostile patrol", goals[0].Title.ToString());
        Assert.AreEqual(1, goals[0].IsPrimary);
        Assert.AreEqual(AssistantGoalState.Complete, goals[1].State);

        AssistantStateComponent assistant = _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        Assert.AreNotEqual(0u, assistant.SourceVersion);
        Assert.AreEqual(1, assistant.UiDirty);
    }

    [Test]
    public void AssistantRecommendationSystem_PublishesObjectiveRecommendation()
    {
        Entity focusedUnit = CreateSelectedUnit();
        CreateFocusedUnitReadModel(focusedUnit);
        Entity boundary = CreateBoundary(withMission: true);

        _goalSystem.Update(_world.Unmanaged);
        _recommendationSystem.Update(_world.Unmanaged);

        Assert.IsTrue(_entityManager.HasComponent<AssistantRecommendationReadModelComponent>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantRecommendationElement>(boundary));

        AssistantRecommendationReadModelComponent readModel =
            _entityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.GetBuffer<AssistantRecommendationElement>(boundary);

        Assert.AreEqual(1, recommendations.Length);
        Assert.AreEqual(1001, recommendations[0].RecommendationId);
        Assert.AreEqual(AssistantRecommendationKind.Move, recommendations[0].Kind);
        Assert.AreEqual(AssistantTargetKind.Cell, recommendations[0].TargetKind);
        Assert.AreEqual(new int2(12, 8), recommendations[0].TargetCell);
        Assert.AreEqual(1, recommendations[0].CanExecute);
        Assert.AreEqual(1, readModel.RecommendationCount);
        Assert.AreEqual(1001, readModel.TopRecommendationId);
        Assert.AreEqual(AssistantRecommendationKind.Move, readModel.TopKind);
    }

    [Test]
    public void AssistantRecommendationSystem_PublishesNoSelectionRecommendation()
    {
        CreateBoundary(withMission: true);

        _goalSystem.Update(_world.Unmanaged);
        _recommendationSystem.Update(_world.Unmanaged);

        using EntityQuery recommendationQuery =
            _entityManager.CreateEntityQuery(ComponentType.ReadOnly<AssistantRecommendationElement>());
        Entity boundary = recommendationQuery.GetSingletonEntity();
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.GetBuffer<AssistantRecommendationElement>(boundary);

        Assert.AreEqual(1, recommendations.Length);
        Assert.AreEqual(3001, recommendations[0].RecommendationId);
        Assert.AreEqual(AssistantRecommendationKind.Select, recommendations[0].Kind);
        Assert.AreEqual(AssistantTargetKind.UiSurface, recommendations[0].TargetKind);
        Assert.AreEqual(AssistantMessagePriority.High, recommendations[0].Priority);
        Assert.AreEqual("Select a unit", recommendations[0].Title.ToString());
    }

    [Test]
    public void AssistantRecommendationSystem_PublishesFuelLogisticsWarningRecommendation()
    {
        Entity boundary = CreateBoundary(withMission: true);
        AddUsableFuelSummary(boundary, storedOil: 25f, storedFuel: 0f, fuelVersion: 12u);

        _goalSystem.Update(_world.Unmanaged);
        _recommendationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.GetBuffer<AssistantRecommendationElement>(boundary);
        AssistantRecommendationReadModelComponent readModel =
            _entityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);

        Assert.AreEqual(1, recommendations.Length);
        Assert.AreEqual(4001, recommendations[0].RecommendationId);
        Assert.AreEqual(AssistantRecommendationKind.Logistics, recommendations[0].Kind);
        Assert.AreEqual(AssistantMessagePriority.High, recommendations[0].Priority);
        Assert.AreEqual(AssistantTargetKind.UiSurface, recommendations[0].TargetKind);
        Assert.AreEqual("Fuel reserves empty", recommendations[0].Title.ToString());
        Assert.AreEqual(1, recommendations[0].CanShow);
        Assert.AreEqual(1, readModel.RecommendationCount);
        Assert.AreEqual(4001, readModel.TopRecommendationId);
        Assert.AreEqual(AssistantRecommendationKind.Logistics, readModel.TopKind);
    }

    [Test]
    public void AssistantRecommendationSystem_PublishesSelectedIdleCombatUnitRecommendation()
    {
        Entity focusedUnit = CreateSelectedUnit();
        CreateFocusedUnitReadModel(focusedUnit);
        Entity hostile = _entityManager.CreateEntity(typeof(Faction), typeof(UnitHealth));
        _entityManager.SetComponentData(hostile, new Faction { Id = FactionIdentity.EnemyFactionId });
        _entityManager.SetComponentData(hostile, new UnitHealth { Current = 100, Max = 100 });
        Entity boundary = CreateBoundary(withMission: true);
        DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
            _entityManager.GetBuffer<MatchObjectiveRuntimeElement>(boundary);
        MatchObjectiveRuntimeElement objective = objectives[0];
        objective.TargetEntity = hostile;
        objective.HasTargetCell = 0;
        objectives[0] = objective;

        _goalSystem.Update(_world.Unmanaged);
        _recommendationSystem.Update(_world.Unmanaged);

        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.GetBuffer<AssistantRecommendationElement>(boundary);

        Assert.AreEqual(1, recommendations.Length);
        Assert.AreEqual(1001, recommendations[0].RecommendationId);
        Assert.AreEqual(AssistantRecommendationKind.Attack, recommendations[0].Kind);
        Assert.AreEqual(focusedUnit, recommendations[0].SourceEntity);
        Assert.AreEqual(AssistantTargetKind.Entity, recommendations[0].TargetKind);
        Assert.AreEqual(hostile, recommendations[0].TargetEntity);
        Assert.AreEqual("Attack objective target", recommendations[0].Title.ToString());
    }

    [Test]
    public void AssistantReadModels_DoNotRepublishWhenSourcesAreUnchanged()
    {
        Entity boundary = CreateBoundary(withMission: true);

        _goalSystem.Update(_world.Unmanaged);
        _recommendationSystem.Update(_world.Unmanaged);

        AssistantStateComponent assistantBefore =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        AssistantRecommendationReadModelComponent readModelBefore =
            _entityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);
        DynamicBuffer<AssistantRecommendationElement> recommendations =
            _entityManager.GetBuffer<AssistantRecommendationElement>(boundary);
        int recommendationSourceVersion = recommendations[0].SourceVersion;

        _goalSystem.Update(_world.Unmanaged);
        _recommendationSystem.Update(_world.Unmanaged);

        AssistantStateComponent assistantAfter =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        AssistantRecommendationReadModelComponent readModelAfter =
            _entityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);
        recommendations = _entityManager.GetBuffer<AssistantRecommendationElement>(boundary);

        Assert.AreEqual(assistantBefore.SourceVersion, assistantAfter.SourceVersion);
        Assert.AreEqual(assistantBefore.PublishedVersion, assistantAfter.PublishedVersion);
        Assert.AreEqual(readModelBefore.Version, readModelAfter.Version);
        Assert.AreEqual(recommendationSourceVersion, recommendations[0].SourceVersion);
        Assert.AreEqual(1, recommendations.Length);
    }

    [Test]
    public void AssistantGoalReadModelSystem_NoMissionPublishesNoGoals()
    {
        Entity boundary = CreateBoundary(withMission: false);

        _goalSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantGoalReadModelElement>(boundary).Length);
    }

    [Test]
    public void AssistantGoalReadModelSystem_PreStartClearsGoals()
    {
        Entity boundary = CreateBoundary(withMission: true, matchStarted: false);
        DynamicBuffer<AssistantGoalReadModelElement> goals =
            _entityManager.AddBuffer<AssistantGoalReadModelElement>(boundary);
        goals.Add(new AssistantGoalReadModelElement
        {
            GoalId = 99,
            Title = new FixedString64Bytes("STALE")
        });

        _goalSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, _entityManager.GetBuffer<AssistantGoalReadModelElement>(boundary).Length);
    }

    [Test]
    public void AssistantBoundary_FitsFullUiShellArchetype()
    {
        _world.CreateSystem<UiShellStateSystem>();
        using EntityQuery boundaryQuery =
            _entityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
        Entity boundary = boundaryQuery.GetSingletonEntity();

        Assert.DoesNotThrow(() => _goalSystem.Update(_world.Unmanaged));
        SystemHandle narrationSystem = _world.CreateSystem<AssistantNarrationRequestSystem>();
        Assert.DoesNotThrow(() => narrationSystem.Update(_world.Unmanaged));
        SystemHandle controlOwnerSystem = _world.CreateSystem<AssistantControlOwnerSystem>();
        Assert.DoesNotThrow(() => controlOwnerSystem.Update(_world.Unmanaged));

        Assert.IsTrue(_entityManager.HasBuffer<MatchObjectiveRuntimeElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantGoalReadModelElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantRecommendationElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantThreatReadModelElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantMessageElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantNarrationRequestElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantPreviewHighlightElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantCommandIntentRequestElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantCommandIntentResultElement>(boundary));
        Assert.IsTrue(_entityManager.HasBuffer<AssistantCommandDispatchElement>(boundary));
        Assert.IsTrue(_entityManager.HasComponent<AssistantNarrationStateComponent>(boundary));
        Assert.IsTrue(_entityManager.HasComponent<AssistantControlOwnerComponent>(boundary));
    }

    private Entity CreateBoundary(bool withMission, bool matchStarted = true)
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiMatchHudStatusSurfacesComponent),
            typeof(UiMatchHudHeaderComponent));

        _entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            ActiveRoute = UIRoute.Match,
            CurrentMode = UiShellMode.MatchHud,
            Phase = UiShellTransitionPhase.MatchHudReady
        });
        _entityManager.SetComponentData(boundary, new UiMatchHudHeaderComponent
        {
            OrderText = new FixedString32Bytes("MOVE ORDER"),
            SquadText = new FixedString32Bytes("RIFLE SQUAD"),
            CreditsText = new FixedString32Bytes("187,540"),
            FuelText = new FixedString32Bytes("9,750"),
            SupplyText = new FixedString32Bytes("92/120"),
            CivilianRiskText = new FixedString32Bytes("MED")
        });

        Entity matchStart = _entityManager.CreateEntity(typeof(MatchStartQueueComponent));
        _entityManager.SetComponentData(matchStart, new MatchStartQueueComponent
        {
            HasStarted = matchStarted ? (byte)1 : (byte)0,
            LastStatus = matchStarted ? MatchStartStatusKind.Started : MatchStartStatusKind.Starting
        });

        _entityManager.AddComponentData(boundary, new MatchObjectiveRuntimeStateComponent
        {
            Version = 7,
            MissionId = withMission ? new FixedString64Bytes("saga.ch01.m01.first_contact") : default,
            MatchActive = withMission ? (byte)1 : (byte)0
        });
        DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
            _entityManager.AddBuffer<MatchObjectiveRuntimeElement>(boundary);
        if (withMission)
            AddObjectiveFixtures(objectives);

        return boundary;
    }

    private static void AddObjectiveFixtures(DynamicBuffer<MatchObjectiveRuntimeElement> objectives)
    {
        objectives.Add(new MatchObjectiveRuntimeElement
        {
            GoalId = 1,
            ObjectiveId = new FixedString64Bytes("objective.destroy_patrol_group"),
            State = MatchObjectiveState.Active,
            Priority = (byte)AssistantMessagePriority.High,
            IsPrimary = 1,
            Title = new FixedString64Bytes("Neutralize hostile patrol"),
            Body = new FixedString128Bytes("Destroy the verified patrol target."),
            TargetCell = new int2(12, 8),
            HasTargetCell = 1
        });
        objectives.Add(new MatchObjectiveRuntimeElement
        {
            GoalId = 2,
            ObjectiveId = new FixedString64Bytes("objective.protect_civilians"),
            State = MatchObjectiveState.Complete,
            Priority = (byte)AssistantMessagePriority.Normal,
            Title = new FixedString64Bytes("Protect civilians"),
            Body = new FixedString128Bytes("Civilian corridor secured.")
        });
        objectives.Add(new MatchObjectiveRuntimeElement
        {
            GoalId = 3,
            ObjectiveId = new FixedString64Bytes("objective.keep_losses_low"),
            State = MatchObjectiveState.Active,
            Priority = (byte)AssistantMessagePriority.Low,
            Title = new FixedString64Bytes("Keep losses low"),
            Body = new FixedString128Bytes("Preserve the command squad.")
        });
    }

    private void AddUsableFuelSummary(Entity boundary, float storedOil, float storedFuel, uint fuelVersion)
    {
        DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
            _entityManager.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
        summaries.Add(new BuildingRuntimeFactionUsableFuelSummary
        {
            FactionId = FactionIdentity.PlayerFactionId,
            StoredOilBarrels = storedOil,
            StoredFuelBarrels = storedFuel,
            CurrentFuelBarrels = storedFuel,
            FuelProducedBarrels = storedFuel,
            FuelDeliveredBarrels = storedFuel,
            OilStorageCapacity = 1000,
            FuelStorageCapacity = 1000,
            Version = fuelVersion
        });
    }

    private Entity CreateSelectedUnit()
    {
        Entity entity = _entityManager.CreateEntity(typeof(SelectedUnitTag), typeof(Faction), typeof(UnitHealth));
        _entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        _entityManager.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        return entity;
    }

    private Entity CreateFocusedUnitReadModel(Entity focusedUnit)
    {
        Entity readModel = _entityManager.CreateEntity(typeof(FocusedUnitUiReadModelComponent));
        _entityManager.SetComponentData(readModel, new FocusedUnitUiReadModelComponent
        {
            FocusedUnit = focusedUnit,
            HasFocusedUnit = 1,
            OwnedByPlayer = 1,
            CanAttack = 1,
            CanHold = 1,
            CanStop = 1,
            CanScan = 1,
            CommandStateVersion = 7,
            Status = 0,
            Label = new FixedString64Bytes("Rifle Squad"),
            Description = new FixedString128Bytes("Infantry")
        });
        return readModel;
    }

}
