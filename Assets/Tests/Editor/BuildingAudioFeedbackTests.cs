using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingAudioFeedbackTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new BuildingAudioFeedbackTests();
            tests.PlacementConfirmResults_ResolveExpectedAudioEvents();
            passed++;
            tests.ProductionResults_ResolveExpectedAudioEvents();
            passed++;
            tests.TryEmitPlacementAudio_EnqueuesInvalidPlacementRequest();
            passed++;
            tests.TryEmitProductionAudio_EnqueuesQueuedAndSuppressesRejectedRequests();
            passed++;

            Debug.Log($"[BuildingAudioFeedbackValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[BuildingAudioFeedbackValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void PlacementConfirmResults_ResolveExpectedAudioEvents()
    {
        AssertPlacementAudio(
            BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
            accepted: true,
            BuildingUiPlacementCommandResultElement.Completed,
            AudioEventIds.GameplayBuildPlaceValid,
            AudioEventIds.GameplayBuildPlaceValidHash);

        AssertPlacementAudio(
            BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
            accepted: false,
            BuildingUiPlacementCommandResultElement.InvalidPlacement,
            AudioEventIds.GameplayBuildPlaceInvalid,
            AudioEventIds.GameplayBuildPlaceInvalidHash);

        Assert.IsFalse(BuildingPlacementCommandRequestCompositionSystemHelper.TryResolvePlacementAudioEvent(
            BuildingUiPlacementCommandRequestElement.KindRotatePlacement,
            accepted: true,
            BuildingUiPlacementCommandResultElement.Completed,
            out _,
            out _));
    }

    [Test]
    public void ProductionResults_ResolveExpectedAudioEvents()
    {
        AssertProductionAudio(
            BuildingUiProductionCommandRequestElement.KindBuildingUnit,
            accepted: true,
            BuildingUiProductionCommandResultElement.Queued,
            AudioEventIds.GameplayProductionQueued,
            AudioEventIds.GameplayProductionQueuedHash);

        Assert.IsFalse(BuildingProductionRequestSystemHelper.TryResolveProductionCommandAudioEvent(
            BuildingUiProductionCommandRequestElement.KindSelectedBuildingUnit,
            accepted: false,
            BuildingUiProductionCommandResultElement.QueueFull,
            out _,
            out _));

        AssertCampItemAudio(
            accepted: true,
            BuildingUiCampItemCommandResultElement.ProductionQueued,
            AudioEventIds.GameplayProductionQueued,
            AudioEventIds.GameplayProductionQueuedHash);

        Assert.IsFalse(BuildingProductionRequestSystemHelper.TryResolveCampItemAudioEvent(
            accepted: false,
            BuildingUiCampItemCommandResultElement.MissingProducerBuilding,
            out _,
            out _));

        Assert.IsFalse(BuildingProductionRequestSystemHelper.TryResolveProductionCommandAudioEvent(
            BuildingUiProductionCommandRequestElement.KindCancelProduction,
            accepted: true,
            BuildingUiProductionCommandResultElement.Cancelled,
            out _,
            out _));

        Assert.IsFalse(BuildingProductionRequestSystemHelper.TryResolveCampItemAudioEvent(
            accepted: true,
            BuildingUiCampItemCommandResultElement.PlacementStarted,
            out _,
            out _));
    }

    [Test]
    public void TryEmitPlacementAudio_EnqueuesInvalidPlacementRequest()
    {
        using World world = new("BuildingPlacementAudioFeedbackTests");

        Assert.IsTrue(BuildingPlacementCommandRequestCompositionSystemHelper.TryEmitPlacementAudio(
            world.EntityManager,
            BuildingUiPlacementCommandRequestElement.KindConfirmPlacement,
            accepted: false,
            BuildingUiPlacementCommandResultElement.BlockedPlacement));

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(world.EntityManager);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.GameplayBuildPlaceInvalid, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.GameplayBuildPlaceInvalidHash, requests[0].EventHash);
        Assert.AreEqual("Gameplay", requests[0].BusId.ToString());
        Assert.AreEqual(AudioPlaybackPriority.Medium, requests[0].Priority);
        Assert.AreEqual(AudioPlaybackRequestStatus.Pending, requests[0].Status);
    }

    [Test]
    public void TryEmitProductionAudio_EnqueuesQueuedAndSuppressesRejectedRequests()
    {
        using World world = new("BuildingProductionAudioFeedbackTests");

        Assert.IsTrue(BuildingProductionRequestSystemHelper.TryEmitProductionCommandAudio(
            world.EntityManager,
            BuildingUiProductionCommandRequestElement.KindBuildingUnit,
            accepted: true,
            BuildingUiProductionCommandResultElement.Queued));
        Assert.IsFalse(BuildingProductionRequestSystemHelper.TryEmitCampItemAudio(
            world.EntityManager,
            accepted: false,
            BuildingUiCampItemCommandResultElement.ProductionQueueFull));

        DynamicBuffer<AudioPlaybackRequestElement> requests = GetAudioRequests(world.EntityManager);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(AudioEventIds.GameplayProductionQueued, requests[0].EventId.ToString());
        Assert.AreEqual(AudioEventIds.GameplayProductionQueuedHash, requests[0].EventHash);
    }

    private static void AssertPlacementAudio(
        byte requestKind,
        bool accepted,
        byte resultCode,
        string expectedEventId,
        uint expectedEventHash)
    {
        Assert.IsTrue(BuildingPlacementCommandRequestCompositionSystemHelper.TryResolvePlacementAudioEvent(
            requestKind,
            accepted,
            resultCode,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }

    private static void AssertProductionAudio(
        byte requestKind,
        bool accepted,
        byte resultCode,
        string expectedEventId,
        uint expectedEventHash)
    {
        Assert.IsTrue(BuildingProductionRequestSystemHelper.TryResolveProductionCommandAudioEvent(
            requestKind,
            accepted,
            resultCode,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }

    private static void AssertCampItemAudio(
        bool accepted,
        byte resultCode,
        string expectedEventId,
        uint expectedEventHash)
    {
        Assert.IsTrue(BuildingProductionRequestSystemHelper.TryResolveCampItemAudioEvent(
            accepted,
            resultCode,
            out string eventId,
            out uint eventHash));
        Assert.AreEqual(expectedEventId, eventId);
        Assert.AreEqual(expectedEventHash, eventHash);
    }

    private static DynamicBuffer<AudioPlaybackRequestElement> GetAudioRequests(EntityManager em)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        return em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
    }
}
