using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
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
            RunCase(test => test.AssistantReadModels_DoNotRepublishWhenSourcesAreUnchanged());
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
        Entity boundary = CreateBoundary(DefaultStatus());

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
        Entity boundary = CreateBoundary(DefaultStatus());

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
        Assert.AreEqual(AssistantRecommendationKind.CameraFocus, recommendations[0].Kind);
        Assert.AreEqual(AssistantTargetKind.Objective, recommendations[0].TargetKind);
        Assert.AreEqual(1, recommendations[0].CanShow);
        Assert.AreEqual(1, readModel.RecommendationCount);
        Assert.AreEqual(1001, readModel.TopRecommendationId);
        Assert.AreEqual(AssistantRecommendationKind.CameraFocus, readModel.TopKind);
    }

    [Test]
    public void AssistantReadModels_DoNotRepublishWhenSourcesAreUnchanged()
    {
        Entity boundary = CreateBoundary(DefaultStatus());

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

    private Entity CreateBoundary(UiMatchHudStatusSurfacesComponent status)
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiMatchHudStatusSurfacesComponent),
            typeof(UiMatchHudHeaderComponent));

        _entityManager.SetComponentData(boundary, status);
        _entityManager.SetComponentData(boundary, new UiMatchHudHeaderComponent
        {
            OrderText = new FixedString32Bytes("MOVE ORDER"),
            SquadText = new FixedString32Bytes("RIFLE SQUAD"),
            CreditsText = new FixedString32Bytes("187,540"),
            FuelText = new FixedString32Bytes("9,750"),
            SupplyText = new FixedString32Bytes("92/120"),
            CivilianRiskText = new FixedString32Bytes("MED")
        });

        return boundary;
    }

    private static UiMatchHudStatusSurfacesComponent DefaultStatus()
    {
        return new UiMatchHudStatusSurfacesComponent
        {
            ObjectivesTitle = new FixedString32Bytes("OBJECTIVES"),
            Objective0Text = new FixedString64Bytes("Neutralize hostile patrol"),
            Objective1Text = new FixedString64Bytes("Protect civilians"),
            Objective2Text = new FixedString64Bytes("Keep losses low"),
            Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked,
            Objective1IconKind = UiMatchHudObjectiveIconKind.Checked,
            Objective2IconKind = UiMatchHudObjectiveIconKind.Star,
            ElapsedText = new FixedString32Bytes("00:30")
        };
    }
}
