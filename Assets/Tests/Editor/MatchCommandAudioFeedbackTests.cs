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
            tests.RejectedCommandKinds_ResolveSharedRejectedAudioEvent();
            passed++;
            tests.TryEmitCommandAudio_EnqueuesGameplayAudioRequest();
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
    public void RejectedCommandKinds_ResolveSharedRejectedAudioEvent()
    {
        AssertResolved(
            RtsSelectionCommandIntentKind.Move,
            accepted: false,
            AudioEventIds.GameplayCommandRejected,
            AudioEventIds.GameplayCommandRejectedHash);
        AssertResolved(
            RtsSelectionCommandIntentKind.HoldPosition,
            accepted: false,
            AudioEventIds.GameplayCommandRejected,
            AudioEventIds.GameplayCommandRejectedHash);
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
}
