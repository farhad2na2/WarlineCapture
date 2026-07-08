using System;
using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class UiShellAudioRoutePopupTests
{
    private World _world;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunCase(test => test.RouteRequest_EnqueuesScreenForwardAudio());
            passed++;
            RunCase(test => test.BackRouteRequest_EnqueuesScreenBackAudio());
            passed++;
            RunCase(test => test.SettingsPopupRequests_EnqueuePopupOpenAndCloseAudio());
            passed++;
            RunCase(test => test.BuildDrawerPopupRequest_EnqueuesDrawerOpenAudio());
            passed++;
            RunCase(test => test.PassengerDrawerToggle_EnqueuesDrawerOpenAudio());
            passed++;

            Debug.Log($"[UiShellAudioRoutePopupValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[UiShellAudioRoutePopupValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(Action<UiShellAudioRoutePopupTests> testCase)
    {
        var tests = new UiShellAudioRoutePopupTests();
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
        _world = new World("UiShellAudioRoutePopupTests");
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [Test]
    public void RouteRequest_EnqueuesScreenForwardAudio()
    {
        Entity boundary = CreateShellFlowBoundary();
        _world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
        {
            Route = UIRoute.Armory,
            Intent = UiShellRouteIntent.OpenMenuRoute,
            PushHistory = 1
        });

        _world.CreateSystem<UiShellFlowSystem>().Update(_world.Unmanaged);

        AssertLatestAudioRequest(AudioEventIds.UIScreenForward, AudioEventIds.UIScreenForwardHash);
    }

    [Test]
    public void BackRouteRequest_EnqueuesScreenBackAudio()
    {
        Entity boundary = CreateShellFlowBoundary();
        _world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
        {
            Route = UIRoute.MainMenu,
            Intent = UiShellRouteIntent.BackMenuRoute,
            PushHistory = 0
        });

        _world.CreateSystem<UiShellFlowSystem>().Update(_world.Unmanaged);

        AssertLatestAudioRequest(AudioEventIds.UIScreenBack, AudioEventIds.UIScreenBackHash);
    }

    [Test]
    public void SettingsPopupRequests_EnqueuePopupOpenAndCloseAudio()
    {
        Entity boundary = CreateShellFlowBoundary();
        DynamicBuffer<UiShellPopupRequestComponent> popupRequests =
            _world.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        popupRequests.Add(new UiShellPopupRequestComponent
        {
            PopupKind = UiShellPopupKind.Settings,
            Intent = UiShellPopupIntent.Show
        });

        SystemHandle system = _world.CreateSystem<UiShellFlowSystem>();
        system.Update(_world.Unmanaged);
        AssertLatestAudioRequest(AudioEventIds.UIPopupOpen, AudioEventIds.UIPopupOpenHash);

        UiShellStateComponent shellState = _world.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
        shellState.IsTransitionRunning = 0;
        _world.EntityManager.SetComponentData(boundary, shellState);
        popupRequests = _world.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        popupRequests.Add(new UiShellPopupRequestComponent
        {
            PopupKind = UiShellPopupKind.Settings,
            Intent = UiShellPopupIntent.Hide
        });

        system.Update(_world.Unmanaged);
        AssertLatestAudioRequest(AudioEventIds.UIPopupClose, AudioEventIds.UIPopupCloseHash);
    }

    [Test]
    public void BuildDrawerPopupRequest_EnqueuesDrawerOpenAudio()
    {
        Entity boundary = CreateShellFlowBoundary();
        _world.EntityManager.GetBuffer<UiShellPopupRequestComponent>(boundary).Add(new UiShellPopupRequestComponent
        {
            PopupKind = UiShellPopupKind.BuildDrawer,
            Intent = UiShellPopupIntent.Show
        });

        _world.CreateSystem<UiShellFlowSystem>().Update(_world.Unmanaged);

        AssertLatestAudioRequest(AudioEventIds.UIDrawerOpen, AudioEventIds.UIDrawerOpenHash);
    }

    [Test]
    public void PassengerDrawerToggle_EnqueuesDrawerOpenAudio()
    {
        Entity boundary = CreateActionBoundary();
        CreateSelectionInputEntity();
        _world.EntityManager.GetBuffer<UiActionRequestComponent>(boundary).Add(new UiActionRequestComponent
        {
            Kind = UiActionKind.TogglePassengerDrawer
        });

        _world.CreateSystem<UiActionRequestSystem>().Update(_world.Unmanaged);

        UiMatchHudPassengerDrawerStateComponent drawer =
            _world.EntityManager.GetComponentData<UiMatchHudPassengerDrawerStateComponent>(boundary);
        Assert.AreEqual(1, drawer.Visible);
        AssertLatestAudioRequest(AudioEventIds.UIDrawerOpen, AudioEventIds.UIDrawerOpenHash);
    }

    private Entity CreateShellFlowBoundary()
    {
        Entity boundary = _world.EntityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiShellLoadingProgressComponent),
            typeof(MatchIntroTransitionComponent),
            typeof(UiShellActivePopupComponent));
        _world.EntityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MainMenu,
            ActiveRoute = UIRoute.MainMenu,
            Phase = UiShellTransitionPhase.MenuReady
        });
        _world.EntityManager.SetComponentData(boundary, new UiShellActivePopupComponent
        {
            PopupKind = UiShellPopupKind.Settings
        });
        _world.EntityManager.AddBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
        _world.EntityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
        _world.EntityManager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);
        return boundary;
    }

    private Entity CreateActionBoundary()
    {
        Entity boundary = _world.EntityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiDiagnosticsOverlayComponent),
            typeof(UiMatchHudPassengerDrawerStateComponent),
            typeof(UiMatchHudSquadTrayStateComponent),
            typeof(UiBuildDrawerStateComponent));
        _world.EntityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MatchHud,
            ActiveRoute = UIRoute.Match
        });
        _world.EntityManager.AddBuffer<UiActionRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiBuildCatalogRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiBuildProductionRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiBuildPrimaryRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
        _world.EntityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        return boundary;
    }

    private void CreateSelectionInputEntity()
    {
        Entity selectionInput = _world.EntityManager.CreateEntity(
            typeof(RtsSelectionInputStateComponent),
            typeof(RtsSelectionInputRequestQueueComponent));
        _world.EntityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);
    }

    private void AssertLatestAudioRequest(string eventId, uint eventHash)
    {
        Entity audioEntity = Game.Runtime.AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests =
            _world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.Greater(requests.Length, 0, "Expected at least one audio request.");
        AudioPlaybackRequestElement request = requests[requests.Length - 1];
        Assert.AreEqual(eventId, request.EventId.ToString());
        Assert.AreEqual(eventHash, request.EventHash);
        Assert.AreEqual("UI", request.BusId.ToString());
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, request.Status);
    }
}
