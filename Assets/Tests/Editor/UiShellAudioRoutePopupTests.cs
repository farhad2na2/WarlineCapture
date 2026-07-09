using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
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
            RunCase(test => test.InitialMenuState_RequestsMenuMusicAndSkipsDuplicateWhenCurrent());
            passed++;
            RunCase(test => test.EnterMatchRoute_RequestsMatchMusicAndSkipsDuplicateWhenCurrent());
            passed++;
            RunCase(test => test.ReturnToMainMenuRoute_RequestsMenuMusicFromMatchHud());
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

    [Test]
    public void InitialMenuState_RequestsMenuMusicAndSkipsDuplicateWhenCurrent()
    {
        Entity boundary = CreateShellFlowBoundary(UiShellMode.None, UIRoute.Splash);
        SystemHandle system = _world.CreateSystem<UiShellFlowSystem>();

        system.Update(_world.Unmanaged);

        AssertMusicStateRequested(AudioEventIds.MusicMenuLoop, AudioEventIds.MusicMenuLoopHash, 1.5f);
        AssertMusicRequestCount(AudioEventIds.MusicMenuLoopHash, 1);

        ApplyRequestedMusicState();
        CompleteCurrentTransition(boundary, UiShellCommandKind.EnterMenu);
        system.Update(_world.Unmanaged);

        _world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
        {
            Route = UIRoute.MainMenu,
            Intent = UiShellRouteIntent.OpenMenuRoute
        });
        system.Update(_world.Unmanaged);

        AssertMusicStateCurrentOnly(AudioEventIds.MusicMenuLoop, AudioEventIds.MusicMenuLoopHash);
        AssertMusicRequestCount(AudioEventIds.MusicMenuLoopHash, 1);
    }

    [Test]
    public void EnterMatchRoute_RequestsMatchMusicAndSkipsDuplicateWhenCurrent()
    {
        Entity boundary = CreateShellFlowBoundary();
        SystemHandle system = _world.CreateSystem<UiShellFlowSystem>();
        _world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
        {
            Route = UIRoute.Match,
            Intent = UiShellRouteIntent.EnterMatch
        });

        system.Update(_world.Unmanaged);

        AssertMusicStateRequested(AudioEventIds.MusicMatchCalmLoop, AudioEventIds.MusicMatchCalmLoopHash, 2f);
        AssertMusicRequestCount(AudioEventIds.MusicMatchCalmLoopHash, 1);
        AssertLatestAudioRequest(AudioEventIds.UIScreenForward, AudioEventIds.UIScreenForwardHash);

        ApplyRequestedMusicState();
        CompleteCurrentTransition(boundary, UiShellCommandKind.ShowLoading);
        system.Update(_world.Unmanaged);

        _world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
        {
            Route = UIRoute.Match,
            Intent = UiShellRouteIntent.EnterMatch
        });
        system.Update(_world.Unmanaged);

        AssertMusicStateCurrentOnly(AudioEventIds.MusicMatchCalmLoop, AudioEventIds.MusicMatchCalmLoopHash);
        AssertMusicRequestCount(AudioEventIds.MusicMatchCalmLoopHash, 1);
    }

    [Test]
    public void ReturnToMainMenuRoute_RequestsMenuMusicFromMatchHud()
    {
        Entity boundary = CreateShellFlowBoundary(UiShellMode.MatchHud, UIRoute.Match);
        SetCurrentMusicState(AudioEventIds.MusicMatchCalmLoop, AudioEventIds.MusicMatchCalmLoopHash);
        _world.EntityManager.GetBuffer<UiShellRouteRequestComponent>(boundary).Add(new UiShellRouteRequestComponent
        {
            Route = UIRoute.MainMenu,
            Intent = UiShellRouteIntent.ReturnToMainMenu
        });

        _world.CreateSystem<UiShellFlowSystem>().Update(_world.Unmanaged);

        AssertMusicStateRequested(AudioEventIds.MusicMenuLoop, AudioEventIds.MusicMenuLoopHash, 1.5f);
        AssertMusicRequestCount(AudioEventIds.MusicMenuLoopHash, 1);
        AssertLatestAudioRequest(AudioEventIds.UIScreenForward, AudioEventIds.UIScreenForwardHash);
    }

    private Entity CreateShellFlowBoundary(
        UiShellMode currentMode = UiShellMode.MainMenu,
        UIRoute activeRoute = UIRoute.MainMenu)
    {
        Entity boundary = _world.EntityManager.CreateEntity(
            typeof(UiShellStateComponent),
            typeof(UiShellLoadingProgressComponent),
            typeof(MatchIntroTransitionComponent),
            typeof(UiShellActivePopupComponent));
        _world.EntityManager.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = currentMode,
            ActiveRoute = activeRoute,
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

    private void AssertMusicStateRequested(string eventId, uint eventHash, float transitionSeconds)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        AudioMusicStateComponent musicState = _world.EntityManager.GetComponentData<AudioMusicStateComponent>(audioEntity);
        Assert.AreEqual(eventId, musicState.RequestedEventId.ToString());
        Assert.AreEqual(eventHash, musicState.RequestedEventHash);
        Assert.AreEqual(1, musicState.IsTransitioning);
        Assert.That(musicState.TransitionSeconds, Is.EqualTo(transitionSeconds).Within(0.001f));

        DynamicBuffer<AudioPlaybackRequestElement> requests =
            _world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.Greater(requests.Length, 0, "Expected at least one audio request.");
        bool found = false;
        for (int i = 0; i < requests.Length; i++)
        {
            AudioPlaybackRequestElement request = requests[i];
            if (request.EventHash != eventHash)
                continue;

            found = true;
            Assert.AreEqual(eventId, request.EventId.ToString());
            Assert.AreEqual("Music", request.BusId.ToString());
            Assert.AreEqual(AudioPlaybackPriority.High, request.Priority);
            Assert.AreEqual(AudioPlaybackRequestStatus.Pending, request.Status);
        }

        Assert.IsTrue(found, $"Expected music audio request {eventId}.");
    }

    private void AssertMusicStateCurrentOnly(string eventId, uint eventHash)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        AudioMusicStateComponent musicState = _world.EntityManager.GetComponentData<AudioMusicStateComponent>(audioEntity);
        Assert.AreEqual(eventId, musicState.CurrentEventId.ToString());
        Assert.AreEqual(eventHash, musicState.CurrentEventHash);
        Assert.AreEqual(0u, musicState.RequestedEventHash);
    }

    private void AssertMusicRequestCount(uint eventHash, int expectedCount)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests =
            _world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        int count = 0;
        for (int i = 0; i < requests.Length; i++)
        {
            AudioPlaybackRequestElement request = requests[i];
            if (request.BusId.ToString() == "Music" && request.EventHash == eventHash)
                count++;
        }

        Assert.AreEqual(expectedCount, count);
    }

    private void ApplyRequestedMusicState()
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        AudioMusicStateComponent musicState = _world.EntityManager.GetComponentData<AudioMusicStateComponent>(audioEntity);
        Assert.IsTrue(AudioMusicStateSystem.ApplyRequestedMusicState(ref musicState));
        _world.EntityManager.SetComponentData(audioEntity, musicState);
    }

    private void SetCurrentMusicState(string eventId, uint eventHash)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(_world.EntityManager);
        AudioMusicStateComponent musicState = _world.EntityManager.GetComponentData<AudioMusicStateComponent>(audioEntity);
        musicState.CurrentEventId = eventId;
        musicState.CurrentEventHash = eventHash;
        musicState.RequestedEventId = default;
        musicState.RequestedEventHash = 0u;
        musicState.IsTransitioning = 0;
        _world.EntityManager.SetComponentData(audioEntity, musicState);
    }

    private void CompleteCurrentTransition(Entity boundary, UiShellCommandKind commandKind)
    {
        UiShellStateComponent shellState = _world.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
        DynamicBuffer<UiShellTransitionCompleteComponent> completions =
            _world.EntityManager.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        completions.Add(new UiShellTransitionCompleteComponent
        {
            Kind = commandKind,
            SequenceId = shellState.TransitionSequenceId
        });
    }
}
