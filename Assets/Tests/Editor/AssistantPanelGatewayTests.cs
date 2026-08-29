using System;
using System.Text;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class AssistantPanelGatewayTests
{
    private World _previousWorld;
    private World _world;
    private EntityManager _entityManager;

    public static void RunFocusedValidation()
    {
        var tests = new AssistantPanelGatewayTests();
        tests.SetUp();
        try
        {
            tests.UnchangedSourcesReturnCachedVersionAndStrings();
            tests.TearDown();
            tests.SetUp();
            tests.UnchangedGatewayPollsAllocateZeroManagedBytes();
            tests.TearDown();
            tests.SetUp();
            tests.TutorialNarrationGatewayPreservesLongPersianUtf8Text();
            Debug.Log("[AssistantPanelGatewayValidation] result=Passed tests=3");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            ValidationExit.Exit(1);
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
        _world = new World(nameof(AssistantPanelGatewayTests));
        World.DefaultGameObjectInjectionWorld = _world;
        _entityManager = _world.EntityManager;
        UiShellEcsGateway.RegisterAsRuntimeGateway();
    }

    [TearDown]
    public void TearDown()
    {
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        UiShellEcsGateway.RegisterAsRuntimeGateway();
        _world?.Dispose();
    }

    [Test]
    public void UnchangedSourcesReturnCachedVersionAndStrings()
    {
        Entity boundary = CreateBoundary();

        Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel first));
        Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel second));

        Assert.Greater(first.Version, 0u);
        Assert.AreEqual(first.Version, second.Version);
        Assert.IsTrue(first.Goal0.Visible);
        Assert.AreEqual("Secure corridor", first.Goal0.Title);
        Assert.IsTrue(first.LargeTextEnabled);
        Assert.IsTrue(first.HighContrastEnabled);
        Assert.IsTrue(ReferenceEquals(first.Goal0.Title, second.Goal0.Title));

        MatchObjectiveRuntimeStateComponent objective =
            _entityManager.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
        objective.Version++;
        objective.ElapsedWholeSeconds = 61;
        _entityManager.SetComponentData(boundary, objective);

        Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel changed));
        Assert.Greater(changed.Version, second.Version);
        Assert.AreEqual(61, changed.ElapsedWholeSeconds);
    }

    [Test]
    public void UnchangedGatewayPollsAllocateZeroManagedBytes()
    {
        CreateBoundary();
        Assert.IsTrue(UiShellEcsGateway.TryReadMatchHudAssistantPanel(out UiAssistantPanelModel model));
        for (int i = 0; i < 16; i++)
            UiShellEcsGateway.TryReadMatchHudAssistantPanel(out model);

        bool allReadsSucceeded = true;
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            allReadsSucceeded &= UiShellEcsGateway.TryReadMatchHudAssistantPanel(out model);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Assert.IsTrue(allReadsSucceeded);
        Assert.AreEqual(0, allocatedBytes, "Unchanged assistant gateway polling must allocate zero managed bytes.");
        GC.KeepAlive(model.Version);
    }

    [Test]
    public void TutorialNarrationGatewayPreservesLongPersianUtf8Text()
    {
        const string text =
            "نوار منابع را بررسی کنید. سربازخانه ۴۰ هزار اعتبار و ۹۰ واحد مصالح هزینه دارد.";
        Entity boundary = CreateBoundary();

        Assert.Greater(Encoding.UTF8.GetByteCount(text), 127,
            "The regression text must exceed FixedString128Bytes UTF-8 capacity.");
        Assert.IsTrue(UiShellRuntimeGateway.TryEnqueueTutorialNarration(
            5,
            9,
            UiTutorialNarrationPhase.PrimaryAction,
            text));

        DynamicBuffer<AssistantMessageElement> messages =
            _entityManager.GetBuffer<AssistantMessageElement>(boundary);
        Assert.AreEqual(1, messages.Length);
        Assert.AreEqual(text, messages[0].Text.ToString());
    }

    private Entity CreateBoundary()
    {
        Entity boundary = _entityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent),
            typeof(UiMatchHudHeaderComponent),
            typeof(AssistantStateComponent),
            typeof(AssistantRecommendationReadModelComponent),
            typeof(AssistantMessageReadModelComponent),
            typeof(AssistantThreatReadModelStateComponent),
            typeof(AssistantTargetLockReadModelComponent),
            typeof(AssistantSettingsComponent),
            typeof(MatchObjectiveRuntimeStateComponent));
        _entityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            ActiveRoute = UIRoute.Match,
            CurrentMode = UiShellMode.MatchHud,
            Phase = UiShellTransitionPhase.MatchHudReady
        });
        _entityManager.SetComponentData(boundary, new AssistantStateComponent
        {
            SourceVersion = 3,
            ControlState = AssistantControlState.Player
        });
        _entityManager.SetComponentData(boundary, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
            NarrationMode = AssistantNarrationMode.Important,
            SubtitlesEnabled = 1,
            LargeTextEnabled = 1,
            HighContrastEnabled = 1
        });
        _entityManager.SetComponentData(boundary, new MatchObjectiveRuntimeStateComponent
        {
            Version = 4,
            MissionId = new FixedString64Bytes("test.mission"),
            MatchActive = 1,
            ElapsedWholeSeconds = 60
        });
        _entityManager.AddBuffer<AssistantGoalReadModelElement>(boundary).Add(new AssistantGoalReadModelElement
        {
            GoalId = 1,
            SourceVersion = 3,
            State = AssistantGoalState.Active,
            Priority = AssistantMessagePriority.High,
            Title = new FixedString64Bytes("Secure corridor"),
            Body = new FixedString128Bytes("Hold the verified objective area."),
            IsPrimary = 1
        });
        _entityManager.AddBuffer<AssistantRecommendationElement>(boundary);
        _entityManager.AddBuffer<AssistantMessageElement>(boundary);
        Entity matchStart = _entityManager.CreateEntity(typeof(MatchStartQueueComponent));
        _entityManager.SetComponentData(matchStart, new MatchStartQueueComponent
        {
            HasStarted = 1,
            LastStatus = MatchStartStatusKind.Started
        });
        return boundary;
    }
}
