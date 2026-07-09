using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class MatchCommandAudioFeedbackTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new MatchCommandAudioFeedbackTests();
            tests.AcceptedCommandKinds_ResolveExpectedGameplayAudioEvents();
            passed++;
            tests.RejectedCommandKinds_DoNotResolveSharedRejectedAudioEvent();
            passed++;
            tests.TryEmitCommandAudio_EnqueuesGameplayAudioRequest();
            passed++;
            tests.TryEmitCommandAudio_SuppressesRejectedGameplayAudioRequest();
            passed++;
            tests.AcceptedCommandVoices_ResolveExpectedAriaEvents();
            passed++;
            tests.TryEmitCommandConfirmationVoice_EnqueuesVoiceRequest();
            passed++;
            tests.SelectionPanelCommandVoices_ResolveExpectedAriaEvents();
            passed++;
            tests.TransportCommandVoices_ResolveExpectedAriaEvents();
            passed++;
            tests.TryEmitAriaVoice_EnqueuesVoiceRequest();
            passed++;

            Debug.Log($"[MatchCommandAudioFeedbackValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[MatchCommandAudioFeedbackValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void AcceptedCommandKinds_ResolveExpectedGameplayAudioEvents()
    {
        AssertResolved(
            RtsSelectionCommandIntentKind.Move,
            accepted: true,
            AudioEventIds.GameplayCommandMoveAccepted,
            AudioEventIds.GameplayCommandMoveAcceptedHash);
        AssertResolved(
            RtsSelectionCommandIntentKind.Attack,
            accepted: true,
            AudioEventIds.GameplayCommandAttackAccepted,
            AudioEventIds.GameplayCommandAttackAcceptedHash);
        AssertResolved(
            RtsSelectionCommandIntentKind.HoldPosition,
            accepted: true,
            AudioEventIds.GameplayCommandHoldAccepted,
            AudioEventIds.GameplayCommandHoldAcceptedHash);
        AssertResolved(
            RtsSelectionCommandIntentKind.Stop,
            accepted: true,
            AudioEventIds.GameplayCommandStopReturning,
            AudioEventIds.GameplayCommandStopReturningHash);
        AssertResolved(
            RtsSelectionCommandIntentKind.EnterScanTargetMode,
            accepted: true,
            AudioEventIds.GameplayCommandScanTargeting,
            AudioEventIds.GameplayCommandScanTargetingHash);
        AssertResolved(
            RtsSelectionCommandIntentKind.Scan,
            accepted: true,
            AudioEventIds.GameplayCommandScanAccepted,
            AudioEventIds.GameplayCommandScanAcceptedHash);
    }

    [Test]
    public void RejectedCommandKinds_DoNotResolveSharedRejectedAudioEvent()
    {
        Assert.IsFalse(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveCommandAudioEvent(
            RtsSelectionCommandIntentKind.Move,
            accepted: false,
            out _,
            out _));
        Assert.IsFalse(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveCommandAudioEvent(
            RtsSelectionCommandIntentKind.HoldPosition,
            accepted: false,
            out _,
            out _));
    }

    [Test]
    public void TryEmitCommandAudio_EnqueuesGameplayAudioRequest()
    {
        using World world = new("MatchCommandAudioFeedbackTests");

        Assert.IsTrue(RtsSelectionCommandResultFlushCompositionSystemHelper.TryEmitCommandAudio(
            world.EntityManager,
            RtsSelectionCommandIntentKind.Attack,
            accepted: true));

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(world.EntityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests =
            world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.GameplayCommandAttackAccepted, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.GameplayCommandAttackAcceptedHash, requests[0].EventHash);
        Assert.AreEqual("Gameplay", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.Medium, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
    }

    [Test]
    public void TryEmitCommandAudio_SuppressesRejectedGameplayAudioRequest()
    {
        using World world = new("MatchCommandRejectedAudioFeedbackTests");

        Assert.IsFalse(RtsSelectionCommandResultFlushCompositionSystemHelper.TryEmitCommandAudio(
            world.EntityManager,
            RtsSelectionCommandIntentKind.Attack,
            accepted: false));

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(world.EntityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests =
            world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(0, requests.Length);
    }

    [Test]
    public void AcceptedCommandVoices_ResolveExpectedAriaEvents()
    {
        AssertCommandConfirmationVoiceResolved(
            RtsSelectionCommandIntentKind.Move,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedMoveTitle,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedMoveTitleHash);
        AssertCommandConfirmationVoiceResolved(
            RtsSelectionCommandIntentKind.Attack,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedAttackTitle,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedAttackTitleHash);
        AssertCommandConfirmationVoiceResolved(
            RtsSelectionCommandIntentKind.HoldPosition,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedHoldTitle,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedHoldTitleHash);
        AssertCommandConfirmationVoiceResolved(
            RtsSelectionCommandIntentKind.Stop,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedStopTitle,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedStopTitleHash);
        AssertCommandConfirmationVoiceResolved(
            RtsSelectionCommandIntentKind.Scan,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedScanTitle,
            AudioEventIds.VOARIAMessageTacticalBannerAcceptedScanTitleHash);

        Assert.IsFalse(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveCommandConfirmationVoiceEvent(
            RtsSelectionCommandIntentKind.Move,
            accepted: false,
            out _,
            out _));
    }

    [Test]
    public void TryEmitCommandConfirmationVoice_EnqueuesVoiceRequest()
    {
        using World world = new("MatchCommandConfirmationVoiceFeedbackTests");

        Assert.IsTrue(RtsSelectionCommandResultFlushCompositionSystemHelper.TryEmitCommandConfirmationVoice(
            world.EntityManager,
            RtsSelectionCommandIntentKind.Move,
            accepted: true));

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(world.EntityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests =
            world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.VOARIAMessageTacticalBannerAcceptedMoveTitle, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageTacticalBannerAcceptedMoveTitleHash, requests[0].EventHash);
        Assert.AreEqual("Voice", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.High, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
    }

    [Test]
    public void SelectionPanelCommandVoices_ResolveExpectedAriaEvents()
    {
        AssertImmediateVoiceResolved(
            RtsSelectionCommandIntentKind.ReturnToBase,
            accepted: true,
            issuedCount: 1,
            AudioEventIds.VOARIAMessageTacticalFeedbackUnitReturningToBase,
            AudioEventIds.VOARIAMessageTacticalFeedbackUnitReturningToBaseHash);
        AssertImmediateVoiceResolved(
            RtsSelectionCommandIntentKind.ReturnToBase,
            accepted: true,
            issuedCount: 3,
            AudioEventIds.VOARIAMessageTacticalFeedbackUnitsReturningToBase,
            AudioEventIds.VOARIAMessageTacticalFeedbackUnitsReturningToBaseHash);
        AssertImmediateVoiceResolved(
            RtsSelectionCommandIntentKind.DestroyFocusedUnit,
            accepted: true,
            issuedCount: 1,
            AudioEventIds.VOARIAMessageTacticalFeedbackDestroyedSelectedUnit,
            AudioEventIds.VOARIAMessageTacticalFeedbackDestroyedSelectedUnitHash);
        AssertImmediateVoiceResolved(
            RtsSelectionCommandIntentKind.DestroyFocusedUnit,
            accepted: true,
            issuedCount: 2,
            AudioEventIds.VOARIAMessageTacticalFeedbackDestroyedSelectedUnits,
            AudioEventIds.VOARIAMessageTacticalFeedbackDestroyedSelectedUnitsHash);

        Assert.IsFalse(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveImmediateCommandVoiceEvent(
            RtsSelectionCommandIntentKind.ReturnToBase,
            accepted: false,
            issuedCount: 1,
            out _,
            out _));
    }

    [Test]
    public void TransportCommandVoices_ResolveExpectedAriaEvents()
    {
        AssertTransportVoiceResolved(
            RtsSelectionCommandIntentKind.BoardTransport,
            AudioEventIds.VOARIAMessageTacticalFeedbackBoardingTransport,
            AudioEventIds.VOARIAMessageTacticalFeedbackBoardingTransportHash);
        AssertTransportVoiceResolved(
            RtsSelectionCommandIntentKind.BoardSelectedTransport,
            AudioEventIds.VOARIAMessageTacticalFeedbackLoadingTransport,
            AudioEventIds.VOARIAMessageTacticalFeedbackLoadingTransportHash);
        AssertTransportVoiceResolved(
            RtsSelectionCommandIntentKind.DisembarkTransportPassenger,
            AudioEventIds.VOARIAMessageTacticalFeedbackExitingUnit,
            AudioEventIds.VOARIAMessageTacticalFeedbackExitingUnitHash);
        AssertTransportVoiceResolved(
            RtsSelectionCommandIntentKind.DisembarkTransport,
            AudioEventIds.VOARIAMessageTacticalFeedbackExitingPassengers,
            AudioEventIds.VOARIAMessageTacticalFeedbackExitingPassengersHash);
    }

    [Test]
    public void TryEmitAriaVoice_EnqueuesVoiceRequest()
    {
        using World world = new("MatchCommandAriaVoiceFeedbackTests");

        Assert.IsTrue(RtsSelectionCommandResultFlushCompositionSystemHelper.TryEmitAriaVoice(
            world.EntityManager,
            AudioEventIds.VOARIAMessageTacticalFeedbackCameraFollowActive,
            AudioEventIds.VOARIAMessageTacticalFeedbackCameraFollowActiveHash));

        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(world.EntityManager);
        DynamicBuffer<AudioPlaybackRequestElement> requests =
            world.EntityManager.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.VOARIAMessageTacticalFeedbackCameraFollowActive, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.VOARIAMessageTacticalFeedbackCameraFollowActiveHash, requests[0].EventHash);
        Assert.AreEqual("Voice", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.High, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
    }

    private static void AssertResolved(
        RtsSelectionCommandIntentKind kind,
        bool accepted,
        string expectedEventId,
        uint expectedEventHash)
    {
        Assert.IsTrue(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveCommandAudioEvent(
            kind,
            accepted,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }

    private static void AssertCommandConfirmationVoiceResolved(
        RtsSelectionCommandIntentKind kind,
        string expectedEventId,
        uint expectedEventHash)
    {
        Assert.IsTrue(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveCommandConfirmationVoiceEvent(
            kind,
            accepted: true,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }

    private static void AssertImmediateVoiceResolved(
        RtsSelectionCommandIntentKind kind,
        bool accepted,
        int issuedCount,
        string expectedEventId,
        uint expectedEventHash)
    {
        Assert.IsTrue(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveImmediateCommandVoiceEvent(
            kind,
            accepted,
            issuedCount,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }

    private static void AssertTransportVoiceResolved(
        RtsSelectionCommandIntentKind kind,
        string expectedEventId,
        uint expectedEventHash)
    {
        RtsSelectionCommandResultElement result = new()
        {
            Kind = kind,
            Accepted = 1
        };

        Assert.IsTrue(RtsSelectionCommandResultFlushCompositionSystemHelper.TryResolveTransportCommandVoiceEvent(
            result,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }
}
