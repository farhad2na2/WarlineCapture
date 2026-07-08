using System;
using Game.Components;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class AssistantSettingsPersistenceSystemTests
{
    private World _previousWorld;
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.AssistantSettingsPersistenceSystemHelper_MapsSettingsModelToEcsComponent());
            passed++;
            RunCase(test => test.AssistantSettingsPersistenceSystemHelper_AppliesSettingsToShellBoundary());
            passed++;
            RunCase(test => test.SettingsServiceApplyRuntime_UpdatesDefaultWorldAssistantSettings());
            passed++;

            Debug.Log($"[AssistantSettingsPersistenceValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[AssistantSettingsPersistenceValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<AssistantSettingsPersistenceSystemTests> testCase)
    {
        var tests = new AssistantSettingsPersistenceSystemTests();
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
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World(nameof(AssistantSettingsPersistenceSystemTests));
        World.DefaultGameObjectInjectionWorld = _world;
        _entityManager = _world.EntityManager;
    }

    [TearDown]
    public void TearDown()
    {
        if (World.DefaultGameObjectInjectionWorld == _world)
            World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world?.Dispose();
    }

    [Test]
    public void AssistantSettingsPersistenceSystemHelper_MapsSettingsModelToEcsComponent()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Assistant.AssistanceLevel = UIAssistanceLevel.Minimal;
        model.Assistant.NarrationMode = UIAssistantNarrationMode.CriticalOnly;
        model.Assistant.AllowTakeover = false;
        model.Accessibility.LargeText = true;
        model.Accessibility.HighContrastUi = true;

        AssistantSettingsComponent projected =
            AssistantSettingsPersistenceSystemHelper.ToAssistantSettingsComponent(model);

        Assert.AreEqual(AssistantGuidanceLevel.Minimal, projected.GuidanceLevel);
        Assert.AreEqual(AssistantNarrationMode.CriticalOnly, projected.NarrationMode);
        Assert.AreEqual(0, projected.AllowTakeover);
        Assert.AreEqual(1, projected.SubtitlesEnabled);
        Assert.AreEqual(1, projected.LargeTextEnabled);
        Assert.AreEqual(1, projected.HighContrastEnabled);
    }

    [Test]
    public void AssistantSettingsPersistenceSystemHelper_AppliesSettingsToShellBoundary()
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(AssistantStateComponent),
            typeof(AssistantNarrationStateComponent));
        _entityManager.SetComponentData(boundary, new AssistantStateComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance
        });
        _entityManager.SetComponentData(boundary, new AssistantNarrationStateComponent
        {
            Mode = AssistantNarrationMode.Important
        });

        UISettingsModel model = SettingsService.Defaults;
        model.Assistant.AssistanceLevel = UIAssistanceLevel.HintsOnly;
        model.Assistant.NarrationMode = UIAssistantNarrationMode.All;
        model.Assistant.AllowTakeover = false;

        AssistantSettingsPersistenceSystemHelper.ApplyToWorld(_world, model);

        AssistantSettingsComponent settings =
            _entityManager.GetComponentData<AssistantSettingsComponent>(boundary);
        AssistantStateComponent assistantState =
            _entityManager.GetComponentData<AssistantStateComponent>(boundary);
        AssistantNarrationStateComponent narrationState =
            _entityManager.GetComponentData<AssistantNarrationStateComponent>(boundary);

        Assert.AreEqual(AssistantGuidanceLevel.HintsOnly, settings.GuidanceLevel);
        Assert.AreEqual(AssistantNarrationMode.All, settings.NarrationMode);
        Assert.AreEqual(0, settings.AllowTakeover);
        Assert.AreEqual(AssistantGuidanceLevel.HintsOnly, assistantState.GuidanceLevel);
        Assert.AreEqual(1, assistantState.UiDirty);
        Assert.AreEqual(AssistantNarrationMode.All, narrationState.Mode);
        Assert.AreEqual(1, narrationState.UiDirty);
    }

    [Test]
    public void SettingsServiceApplyRuntime_UpdatesDefaultWorldAssistantSettings()
    {
        _world.CreateSystem<AssistantSettingsPersistenceSystem>();
        Entity boundary = _entityManager.CreateEntity(typeof(UiShellStateComponent));

        UISettingsModel model = SettingsService.Defaults;
        model.Assistant.AssistanceLevel = UIAssistanceLevel.Off;
        model.Assistant.NarrationMode = UIAssistantNarrationMode.Off;
        model.Assistant.AllowTakeover = false;

        SettingsService.ApplyRuntime(model);

        AssistantSettingsComponent settings =
            _entityManager.GetComponentData<AssistantSettingsComponent>(boundary);

        Assert.AreEqual(AssistantGuidanceLevel.Off, settings.GuidanceLevel);
        Assert.AreEqual(AssistantNarrationMode.Off, settings.NarrationMode);
        Assert.AreEqual(0, settings.AllowTakeover);
    }
}
