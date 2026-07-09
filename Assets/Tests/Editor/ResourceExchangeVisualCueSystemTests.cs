using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class ResourceExchangeVisualCueSystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ExportQueue_EmitsStartLoadLandingAndDepartingOnce),
                test => test.ExportQueue_EmitsStartLoadLandingAndDepartingOnce(),
                ref passed);
            RunValidationStep(
                nameof(ImportQueue_EmitsLandingUnloadAndDepartingOnce),
                test => test.ImportQueue_EmitsLandingUnloadAndDepartingOnce(),
                ref passed);
            RunValidationStep(
                nameof(TerminalQueue_EmitsCompletionAndCancellationOnce),
                test => test.TerminalQueue_EmitsCompletionAndCancellationOnce(),
                ref passed);
            RunValidationStep(
                nameof(MissingAnchors_EmitsUnresolvedCuesWithoutStartingPresentation),
                test => test.MissingAnchors_EmitsUnresolvedCuesWithoutStartingPresentation(),
                ref passed);
            RunValidationStep(
                nameof(WorldPresentationDisabled_EmitsNoCues),
                test => test.WorldPresentationDisabled_EmitsNoCues(),
                ref passed);

            Debug.Log($"[ResourceExchangeVisualCueValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeVisualCueValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ExportQueue_EmitsStartLoadLandingAndDepartingOnce()
    {
        using World world = new(nameof(ExportQueue_EmitsStartLoadLandingAndDepartingOnce));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1);
        AddDefaultAnchors(em, exchange);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Export,
            remainingSeconds: 20f));

        Emit(em, exchange);

        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange);
        Assert.AreEqual(4, requests.Length);
        AssertCue(requests, 0, ResourceExchangeVisualCueKind.ExchangeStarted, ResourceExchangePresentationAnchorKind.BaseDepot, 1);
        AssertCue(requests, 1, ResourceExchangeVisualCueKind.TransportPlaneLanding, ResourceExchangePresentationAnchorKind.RunwayLandingZone, 1);
        AssertCue(requests, 2, ResourceExchangeVisualCueKind.ExportLoadStarted, ResourceExchangePresentationAnchorKind.Storage, 1);
        AssertCue(requests, 3, ResourceExchangeVisualCueKind.TransportPlaneDeparting, ResourceExchangePresentationAnchorKind.RunwayLandingZone, 1);

        DynamicBuffer<ResourceExchangeQueueComponent> queue =
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(1, queue[0].PresentationStarted);
        Assert.AreEqual(1, queue[0].VisualStartedEmitted);
        Assert.AreEqual(1, queue[0].VisualLoadEmitted);
        Assert.AreEqual(1, queue[0].VisualLandingEmitted);
        Assert.AreEqual(1, queue[0].VisualDepartingEmitted);

        Emit(em, exchange);

        Assert.AreEqual(4, em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange).Length);
    }

    [Test]
    public void ImportQueue_EmitsLandingUnloadAndDepartingOnce()
    {
        using World world = new(nameof(ImportQueue_EmitsLandingUnloadAndDepartingOnce));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1);
        AddDefaultAnchors(em, exchange);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Import,
            inputResource: ResourceExchangeResourceKind.Credits,
            outputResource: ResourceExchangeResourceKind.Fuel,
            remainingSeconds: 20f));

        Emit(em, exchange);

        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange);
        Assert.AreEqual(4, requests.Length);
        AssertCue(requests, 0, ResourceExchangeVisualCueKind.ExchangeStarted, ResourceExchangePresentationAnchorKind.BaseDepot, 1);
        AssertCue(requests, 1, ResourceExchangeVisualCueKind.TransportPlaneLanding, ResourceExchangePresentationAnchorKind.RunwayLandingZone, 1);
        AssertCue(requests, 2, ResourceExchangeVisualCueKind.ImportUnloadStarted, ResourceExchangePresentationAnchorKind.Storage, 1);
        AssertCue(requests, 3, ResourceExchangeVisualCueKind.TransportPlaneDeparting, ResourceExchangePresentationAnchorKind.RunwayLandingZone, 1);
        Assert.IsFalse(ContainsCue(requests, ResourceExchangeVisualCueKind.ExportLoadStarted));

        DynamicBuffer<ResourceExchangeQueueComponent> queue =
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(1, queue[0].PresentationStarted);
        Assert.AreEqual(1, queue[0].VisualUnloadEmitted);
        Assert.AreEqual(0, queue[0].VisualLoadEmitted);

        Emit(em, exchange);

        Assert.AreEqual(4, em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange).Length);
    }

    [Test]
    public void TerminalQueue_EmitsCompletionAndCancellationOnce()
    {
        using World world = new(nameof(TerminalQueue_EmitsCompletionAndCancellationOnce));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1);
        AddDefaultAnchors(em, exchange);
        DynamicBuffer<ResourceExchangeQueueComponent> queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        queue.Add(CreateQueueItem(queueItemId: 1, state: ResourceExchangeQueueState.Completed));
        queue.Add(CreateQueueItem(queueItemId: 2, state: ResourceExchangeQueueState.Cancelled));

        Emit(em, exchange);

        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange);
        Assert.AreEqual(2, requests.Length);
        AssertCue(requests, 0, ResourceExchangeVisualCueKind.ExchangeCompleted, ResourceExchangePresentationAnchorKind.BaseDepot, 1);
        AssertCue(requests, 1, ResourceExchangeVisualCueKind.ExchangeCancelled, ResourceExchangePresentationAnchorKind.FallbackSafe, 1);

        queue = em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(1, queue[0].VisualCompletionEmitted);
        Assert.AreEqual(1, queue[1].VisualCancellationEmitted);

        Emit(em, exchange);

        Assert.AreEqual(2, em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange).Length);
    }

    [Test]
    public void MissingAnchors_EmitsUnresolvedCuesWithoutStartingPresentation()
    {
        using World world = new(nameof(MissingAnchors_EmitsUnresolvedCuesWithoutStartingPresentation));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 1);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Export,
            remainingSeconds: 20f));

        Emit(em, exchange);

        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange);
        Assert.AreEqual(4, requests.Length);
        for (int i = 0; i < requests.Length; i++)
        {
            Assert.AreEqual(0, requests[i].AnchorResolved);
            Assert.AreEqual(ResourceExchangePresentationAnchorKind.None, requests[i].ResolvedAnchorKind);
        }

        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].PresentationStarted);
    }

    [Test]
    public void WorldPresentationDisabled_EmitsNoCues()
    {
        using World world = new(nameof(WorldPresentationDisabled_EmitsNoCues));
        EntityManager em = world.EntityManager;
        Entity exchange = CreateExchangeEntity(em, allowWorldPresentation: 0);
        AddDefaultAnchors(em, exchange);
        em.GetBuffer<ResourceExchangeQueueComponent>(exchange).Add(CreateQueueItem(
            routeType: ResourceExchangeRouteType.Export,
            remainingSeconds: 20f));

        Emit(em, exchange);

        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange).Length);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].VisualStartedEmitted);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeQueueComponent>(exchange)[0].PresentationStarted);
    }

    private static Entity CreateExchangeEntity(EntityManager em, byte allowWorldPresentation)
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
            RecipeId = new FixedString128Bytes("exchange.visual.test"),
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

    private static void Emit(EntityManager em, Entity exchange)
    {
        ResourceExchangeVisualCueSystem.EmitVisualCues(
            em.GetComponentData<ResourceExchangeEnabledComponent>(exchange),
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange),
            em.GetBuffer<ResourceExchangeVisualRequestComponent>(exchange),
            em.GetBuffer<ResourceExchangePresentationAnchorComponent>(exchange));
    }

    private static void AssertCue(
        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests,
        int index,
        ResourceExchangeVisualCueKind cueKind,
        ResourceExchangePresentationAnchorKind requestedAnchorKind,
        byte anchorResolved)
    {
        Assert.AreEqual(cueKind, requests[index].CueKind);
        Assert.AreEqual(requestedAnchorKind, requests[index].RequestedAnchorKind);
        Assert.AreEqual(anchorResolved, requests[index].AnchorResolved);
        Assert.AreEqual(1, requests[index].FactionId);
        Assert.AreEqual(100, requests[index].InputAmount);
        Assert.AreEqual(75, requests[index].OutputAmount);
    }

    private static bool ContainsCue(
        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests,
        ResourceExchangeVisualCueKind cueKind)
    {
        for (int i = 0; i < requests.Length; i++)
        {
            if (requests[i].CueKind == cueKind)
                return true;
        }

        return false;
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeVisualCueSystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeVisualCueSystemTests();
        action(test);
        passed++;
    }
}
#endif
