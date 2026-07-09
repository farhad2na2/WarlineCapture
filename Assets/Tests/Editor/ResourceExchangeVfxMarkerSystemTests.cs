using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class ResourceExchangeVfxMarkerSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ExportQueue_EmitsPairedVfxMarkersForEveryCue),
                test => test.ExportQueue_EmitsPairedVfxMarkersForEveryCue(),
                ref passed);
            RunValidationStep(
                nameof(ImportQueue_UsesImportUnloadVfxMarker),
                test => test.ImportQueue_UsesImportUnloadVfxMarker(),
                ref passed);
            RunValidationStep(
                nameof(TerminalQueue_EmitsCompletionAndCancellationVfxMarkers),
                test => test.TerminalQueue_EmitsCompletionAndCancellationVfxMarkers(),
                ref passed);
            RunValidationStep(
                nameof(MissingAnchors_RecordsUnresolvedNonAuthoritativeMarkers),
                test => test.MissingAnchors_RecordsUnresolvedNonAuthoritativeMarkers(),
                ref passed);
            RunValidationStep(
                nameof(WorldPresentationDisabled_EmitsNoVfxMarkers),
                test => test.WorldPresentationDisabled_EmitsNoVfxMarkers(),
                ref passed);
            RunValidationStep(
                nameof(ResolveVfxMarkerKind_MapsCueKinds),
                test => test.ResolveVfxMarkerKind_MapsCueKinds(),
                ref passed);

            Debug.Log($"[ResourceExchangeVfxMarkerValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeVfxMarkerValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ExportQueue_EmitsPairedVfxMarkersForEveryCue()
    {
        using World world = new(nameof(ExportQueue_EmitsPairedVfxMarkersForEveryCue));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1, addVfxBuffer: true);
        AddDefaultAnchors(em, exchange);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Export,
            remainingSeconds: 20f));

        EmitWithMarkers(em, exchange);

        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange);
        DynamicBuffer<ResourceExchangeVfxMarkerComponent> markers =
            em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange);
        Assert.AreEqual(4, requests.Length);
        Assert.AreEqual(4, markers.Length);
        AssertMarker(markers[0], requests[0], 1, ResourceExchangeVfxMarkerKind.ExchangeStartedPulse);
        AssertMarker(markers[1], requests[1], 2, ResourceExchangeVfxMarkerKind.TransportLandingDust);
        AssertMarker(markers[2], requests[2], 3, ResourceExchangeVfxMarkerKind.ExportLoadPulse);
        AssertMarker(markers[3], requests[3], 4, ResourceExchangeVfxMarkerKind.TransportDepartingTrail);

        EmitWithMarkers(em, exchange);

        Assert.AreEqual(4, em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange).Length);
        Assert.AreEqual(4, em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange).Length);
    }

    [Test]
    public void ImportQueue_UsesImportUnloadVfxMarker()
    {
        using World world = new(nameof(ImportQueue_UsesImportUnloadVfxMarker));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1, addVfxBuffer: true);
        AddDefaultAnchors(em, exchange);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Import,
            inputResource: ResourceExchangeResourceKind.Credits,
            outputResource: ResourceExchangeResourceKind.Fuel,
            remainingSeconds: 20f));

        EmitWithMarkers(em, exchange);

        DynamicBuffer<ResourceExchangeVfxMarkerComponent> markers =
            em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange);
        Assert.AreEqual(4, markers.Length);
        Assert.AreEqual(ResourceExchangeVfxMarkerKind.ExchangeStartedPulse, markers[0].MarkerKind);
        Assert.AreEqual(ResourceExchangeVfxMarkerKind.TransportLandingDust, markers[1].MarkerKind);
        Assert.AreEqual(ResourceExchangeVfxMarkerKind.ImportUnloadPulse, markers[2].MarkerKind);
        Assert.AreEqual(ResourceExchangeVfxMarkerKind.TransportDepartingTrail, markers[3].MarkerKind);
        Assert.IsFalse(ContainsMarker(markers, ResourceExchangeVfxMarkerKind.ExportLoadPulse));
    }

    [Test]
    public void TerminalQueue_EmitsCompletionAndCancellationVfxMarkers()
    {
        using World world = new(nameof(TerminalQueue_EmitsCompletionAndCancellationVfxMarkers));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1, addVfxBuffer: true);
        AddDefaultAnchors(em, exchange);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(CreateQueueItem(queueItemId: 1, state: ResourceExchangeQueueState.Completed));
        queue.Add(CreateQueueItem(queueItemId: 2, state: ResourceExchangeQueueState.Cancelled));

        EmitWithMarkers(em, exchange);

        DynamicBuffer<ResourceExchangeVfxMarkerComponent> markers =
            em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange);
        Assert.AreEqual(2, markers.Length);
        Assert.AreEqual(1, markers[0].SequenceId);
        Assert.AreEqual(2, markers[1].SequenceId);
        Assert.AreEqual(ResourceExchangeVfxMarkerKind.ExchangeCompletedPulse, markers[0].MarkerKind);
        Assert.AreEqual(ResourceExchangeVfxMarkerKind.ExchangeCancelledPulse, markers[1].MarkerKind);
        Assert.AreEqual(ResourceExchangeVisualCueKind.ExchangeCompleted, markers[0].CueKind);
        Assert.AreEqual(ResourceExchangeVisualCueKind.ExchangeCancelled, markers[1].CueKind);

        EmitWithMarkers(em, exchange);

        Assert.AreEqual(2, em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange).Length);
    }

    [Test]
    public void MissingAnchors_RecordsUnresolvedNonAuthoritativeMarkers()
    {
        using World world = new(nameof(MissingAnchors_RecordsUnresolvedNonAuthoritativeMarkers));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1, addVfxBuffer: true);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Export,
            remainingSeconds: 20f));

        EmitWithMarkers(em, exchange);

        DynamicBuffer<ResourceExchangeVfxMarkerComponent> markers =
            em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange);
        Assert.AreEqual(4, markers.Length);
        for (int i = 0; i < markers.Length; i++)
        {
            Assert.AreEqual(i + 1, markers[i].SequenceId);
            Assert.AreEqual(0, markers[i].AnchorResolved);
            Assert.AreEqual(0, markers[i].UsedFallbackAnchor);
            Assert.AreEqual(1, markers[i].NonAuthoritative);
            Assert.AreEqual(ResourceExchangePresentationAnchorKind.None, markers[i].ResolvedAnchorKind);
            Assert.AreEqual(0f, markers[i].AnchorRadius);
            AssertVectorEquals(float3.zero, markers[i].AnchorPosition);
            Assert.IsTrue(markers[i].DurationSeconds > 0f);
        }

        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].PresentationStarted);
    }

    [Test]
    public void WorldPresentationDisabled_EmitsNoVfxMarkers()
    {
        using World world = new(nameof(WorldPresentationDisabled_EmitsNoVfxMarkers));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 0, addVfxBuffer: true);
        AddDefaultAnchors(em, exchange);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Export,
            remainingSeconds: 20f));

        EmitWithMarkers(em, exchange);

        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange).Length);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange).Length);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].VisualStartedEmitted);
    }

    [Test]
    public void ResolveVfxMarkerKind_MapsCueKinds()
    {
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.ExchangeStartedPulse,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.ExchangeStarted));
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.TransportLandingDust,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.TransportPlaneLanding));
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.ExportLoadPulse,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.ExportLoadStarted));
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.ImportUnloadPulse,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.ImportUnloadStarted));
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.TransportDepartingTrail,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.TransportPlaneDeparting));
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.ExchangeCompletedPulse,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.ExchangeCompleted));
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.ExchangeCancelledPulse,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.ExchangeCancelled));
        Assert.AreEqual(
            ResourceExchangeVfxMarkerKind.None,
            ResourceExchangeVisualCueSystem.ResolveVfxMarkerKind(ResourceExchangeVisualCueKind.None));
    }

    private static Entity CreateExchangeEntity(
        EntityManager em,
        byte allowWorldPresentation,
        bool addVfxBuffer)
    {
        Entity entity = em.CreateEntity(typeof(ResourceExchangeEnabledComponent));
        em.SetComponentData(entity, new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = 1,
            AllowWorldPresentation = allowWorldPresentation,
            MaxQueueItems = 4,
            ScenarioTag = new FixedString64Bytes("mission.active")
        });
        em.AddBuffer<ResourceExchangeQueueComponent>(entity);
        em.AddBuffer<ResourceExchangeVisualRequestComponent>(entity);
        em.AddBuffer<ResourceExchangePresentationAnchorComponent>(entity);
        if (addVfxBuffer)
            em.AddBuffer<ResourceExchangeVfxMarkerComponent>(entity);
        return entity;
    }

    private static ResourceExchangeQueueComponent CreateQueueItem(
        int queueItemId = 1,
        ResourceExchangeQueueState state = ResourceExchangeQueueState.InProgress,
        ResourceExchangeRouteType routeType = ResourceExchangeRouteType.Export,
        ResourceExchangeResourceKind inputResource = ResourceExchangeResourceKind.Oil,
        ResourceExchangeResourceKind outputResource = ResourceExchangeResourceKind.Credits,
        float durationSeconds = 100f,
        float remainingSeconds = 20f)
    {
        return new ResourceExchangeQueueComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            RecipeId = new FixedString128Bytes("exchange.vfx.test"),
            RouteType = routeType,
            InputResource = inputResource,
            OutputResource = outputResource,
            InputAmount = 100,
            ReservedInputAmount = 100,
            OutputAmount = 75,
            State = state,
            StateReason = ResourceExchangeReason.None,
            DurationSeconds = durationSeconds,
            RemainingSeconds = remainingSeconds,
            Version = 1
        };
    }

    private static void AddDefaultAnchors(EntityManager em, Entity exchange)
    {
        DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors =
            em.GetBuffer<ResourceExchangePresentationAnchorComponent>(exchange);
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.BaseDepot, new float3(1f, 0f, 1f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.Storage, new float3(8f, 0f, 2f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.RunwayLandingZone, new float3(15f, 0f, 4f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.FallbackSafe, new float3(3f, 0f, 5f)));
    }

    private static ResourceExchangePresentationAnchorComponent CreateAnchor(
        ResourceExchangePresentationAnchorKind anchorKind,
        float3 position)
    {
        return new ResourceExchangePresentationAnchorComponent
        {
            FactionId = 1,
            AnchorKind = anchorKind,
            AnchorId = new FixedString64Bytes(anchorKind.ToString()),
            Position = position,
            Rotation = quaternion.identity,
            Radius = 4f,
            IsValid = 1
        };
    }

    private static void EmitWithMarkers(EntityManager em, Entity exchange)
    {
        ResourceExchangeVisualCueSystem.EmitVisualCues(
            em.GetComponentData<ResourceExchangeEnabledComponent>(exchange),
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange),
            em.GetBuffer<ResourceExchangePresentationAnchorComponent>(exchange),
            em.GetBuffer<ResourceExchangeVfxMarkerComponent>(exchange),
            true);
    }

    private static void AssertMarker(
        in ResourceExchangeVfxMarkerComponent marker,
        in ResourceExchangeVisualRequestComponent request,
        int sequenceId,
        ResourceExchangeVfxMarkerKind markerKind)
    {
        Assert.AreEqual(sequenceId, marker.SequenceId);
        Assert.AreEqual(request.QueueItemId, marker.QueueItemId);
        Assert.AreEqual(request.FactionId, marker.FactionId);
        Assert.AreEqual(request.CueKind, marker.CueKind);
        Assert.AreEqual(markerKind, marker.MarkerKind);
        Assert.AreEqual(request.RouteType, marker.RouteType);
        Assert.AreEqual(request.InputResource, marker.InputResource);
        Assert.AreEqual(request.OutputResource, marker.OutputResource);
        Assert.AreEqual(request.InputAmount, marker.InputAmount);
        Assert.AreEqual(request.OutputAmount, marker.OutputAmount);
        Assert.AreEqual(request.RequestedAnchorKind, marker.RequestedAnchorKind);
        Assert.AreEqual(request.ResolvedAnchorKind, marker.ResolvedAnchorKind);
        AssertVectorEquals(request.AnchorPosition, marker.AnchorPosition);
        Assert.AreEqual(request.AnchorRadius, marker.AnchorRadius);
        Assert.AreEqual(request.AnchorResolved, marker.AnchorResolved);
        Assert.AreEqual(request.UsedFallbackAnchor, marker.UsedFallbackAnchor);
        Assert.AreEqual(1, marker.NonAuthoritative);
        Assert.IsTrue(marker.DurationSeconds > 0f);
    }

    private static void AssertVectorEquals(float3 expected, float3 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 0.0001f);
        Assert.AreEqual(expected.y, actual.y, 0.0001f);
        Assert.AreEqual(expected.z, actual.z, 0.0001f);
    }

    private static bool ContainsMarker(
        DynamicBuffer<ResourceExchangeVfxMarkerComponent> markers,
        ResourceExchangeVfxMarkerKind markerKind)
    {
        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i].MarkerKind == markerKind)
                return true;
        }

        return false;
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeVfxMarkerSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeVfxMarkerSystemTests();
        action(test);
        passed++;
    }
}
#endif
