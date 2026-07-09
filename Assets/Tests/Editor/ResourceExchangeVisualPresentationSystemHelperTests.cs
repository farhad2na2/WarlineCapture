using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ResourceExchangeVisualPresentationSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ConsumeVisualRequests_PlacesActorsDisablesCollidersAndClearsBuffer),
                test => test.ConsumeVisualRequests_PlacesActorsDisablesCollidersAndClearsBuffer(),
                ref passed);
            RunValidationStep(
                nameof(TerminalCue_ReleasesActiveActorsAndReusesPooledPlane),
                test => test.TerminalCue_ReleasesActiveActorsAndReusesPooledPlane(),
                ref passed);
            RunValidationStep(
                nameof(RepeatedPlaneCuesForSameQueue_ReusesActivePlaneActor),
                test => test.RepeatedPlaneCuesForSameQueue_ReusesActivePlaneActor(),
                ref passed);
            RunValidationStep(
                nameof(MissingAnchorAndMissingPrefab_DoNotCreateActors),
                test => test.MissingAnchorAndMissingPrefab_DoNotCreateActors(),
                ref passed);
            RunValidationStep(
                nameof(ResolveActorKind_MapsCueKindsToPresentationActors),
                test => test.ResolveActorKind_MapsCueKindsToPresentationActors(),
                ref passed);

            Debug.Log($"[ResourceExchangeVisualPresentationValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeVisualPresentationValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ConsumeVisualRequests_PlacesActorsDisablesCollidersAndClearsBuffer()
    {
        using World world = new(nameof(ConsumeVisualRequests_PlacesActorsDisablesCollidersAndClearsBuffer));
        Entity entity = CreateVisualRequestEntity(world.EntityManager);
        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            world.EntityManager.GetBuffer<ResourceExchangeVisualRequestComponent>(entity);
        requests.Add(CreateRequest(ResourceExchangeVisualCueKind.TransportPlaneLanding, new float3(4f, 2f, 8f)));
        requests.Add(CreateRequest(ResourceExchangeVisualCueKind.ExportLoadStarted, new float3(12f, 0f, 6f)));

        GameObject root = new("ResourceExchangeVisualPresentationTestRoot");
        GameObject planePrefab = CreatePrefab("TransportPlanePrefab");
        GameObject truckPrefab = CreatePrefab("ResourceTruckPrefab");
        var helper = new ResourceExchangeVisualPresentationSystemHelper();
        try
        {
            ResourceExchangeVisualPresentationSystemHelper.Context context =
                CreateContext(root.transform, planePrefab, truckPrefab);

            ResourceExchangeVisualPresentationSystemHelper.Result result =
                helper.ConsumeVisualRequests(context, requests);

            Assert.AreEqual(2, result.ProcessedCount);
            Assert.AreEqual(2, result.PlayedCount);
            Assert.AreEqual(2, result.ClearedRequestCount);
            Assert.AreEqual(0, result.MissingAnchorCount);
            Assert.AreEqual(0, result.MissingPrefabCount);
            Assert.AreEqual(0, requests.Length);
            Assert.AreEqual(2, helper.ActiveActorCount);
            Assert.AreEqual(2, helper.CreatedActorCount);

            Transform plane = FindChild(root.transform, "TransportPlanePrefab_ResourceExchangeActor");
            Transform truck = FindChild(root.transform, "ResourceTruckPrefab_ResourceExchangeActor");
            Assert.IsNotNull(plane);
            Assert.IsNotNull(truck);
            Assert.IsTrue(Vector3.Distance(new Vector3(4f, 2f, 8f), plane.position) < 0.001f);
            Assert.IsTrue(Vector3.Distance(new Vector3(12f, 0f, 6f), truck.position) < 0.001f);
            AssertCollidersDisabled(plane.gameObject);
            AssertCollidersDisabled(truck.gameObject);
        }
        finally
        {
            helper.Dispose();
            DestroyImmediate(planePrefab);
            DestroyImmediate(truckPrefab);
            DestroyImmediate(root);
        }
    }

    [Test]
    public void TerminalCue_ReleasesActiveActorsAndReusesPooledPlane()
    {
        using World world = new(nameof(TerminalCue_ReleasesActiveActorsAndReusesPooledPlane));
        Entity entity = CreateVisualRequestEntity(world.EntityManager);
        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            world.EntityManager.GetBuffer<ResourceExchangeVisualRequestComponent>(entity);

        GameObject root = new("ResourceExchangeVisualPresentationPoolRoot");
        GameObject planePrefab = CreatePrefab("TransportPlanePrefab");
        var helper = new ResourceExchangeVisualPresentationSystemHelper();
        try
        {
            ResourceExchangeVisualPresentationSystemHelper.Context context =
                new(
                    root.transform,
                    (ResourceExchangeVisualActorKind actorKind, ResourceExchangeVisualRequestComponent request, out GameObject prefab) =>
                    {
                        prefab = actorKind == ResourceExchangeVisualActorKind.TransportPlane
                            ? planePrefab
                            : null;
                        return prefab != null;
                    });

            requests.Add(CreateRequest(ResourceExchangeVisualCueKind.TransportPlaneLanding, new float3(2f, 0f, 2f), queueItemId: 7));
            ResourceExchangeVisualPresentationSystemHelper.Result first = helper.ConsumeVisualRequests(context, requests);
            Assert.AreEqual(1, first.PlayedCount);
            Assert.AreEqual(1, helper.ActiveActorCount);
            Assert.AreEqual(1, helper.CreatedActorCount);

            requests.Add(CreateRequest(ResourceExchangeVisualCueKind.ExchangeCompleted, new float3(2f, 0f, 2f), queueItemId: 7));
            ResourceExchangeVisualPresentationSystemHelper.Result terminal = helper.ConsumeVisualRequests(context, requests);
            Assert.AreEqual(1, terminal.ReleasedActorCount);
            Assert.AreEqual(1, terminal.MissingPrefabCount);
            Assert.AreEqual(0, helper.ActiveActorCount);
            Assert.AreEqual(1, helper.GetPooledActorCount(planePrefab));

            requests.Add(CreateRequest(ResourceExchangeVisualCueKind.TransportPlaneLanding, new float3(4f, 0f, 4f), queueItemId: 8));
            ResourceExchangeVisualPresentationSystemHelper.Result second = helper.ConsumeVisualRequests(context, requests);
            Assert.AreEqual(1, second.PlayedCount);
            Assert.AreEqual(1, helper.ActiveActorCount);
            Assert.AreEqual(1, helper.CreatedActorCount);
            Assert.AreEqual(0, helper.GetPooledActorCount(planePrefab));
        }
        finally
        {
            helper.Dispose();
            DestroyImmediate(planePrefab);
            DestroyImmediate(root);
        }
    }

    [Test]
    public void RepeatedPlaneCuesForSameQueue_ReusesActivePlaneActor()
    {
        using World world = new(nameof(RepeatedPlaneCuesForSameQueue_ReusesActivePlaneActor));
        Entity entity = CreateVisualRequestEntity(world.EntityManager);
        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            world.EntityManager.GetBuffer<ResourceExchangeVisualRequestComponent>(entity);
        requests.Add(CreateRequest(ResourceExchangeVisualCueKind.TransportPlaneLanding, new float3(3f, 0f, 3f), queueItemId: 9));
        requests.Add(CreateRequest(ResourceExchangeVisualCueKind.TransportPlaneDeparting, new float3(9f, 0f, 9f), queueItemId: 9));

        GameObject root = new("ResourceExchangeVisualPresentationNoDuplicateRoot");
        GameObject planePrefab = CreatePrefab("TransportPlanePrefab");
        var helper = new ResourceExchangeVisualPresentationSystemHelper();
        try
        {
            var context = new ResourceExchangeVisualPresentationSystemHelper.Context(
                root.transform,
                (ResourceExchangeVisualActorKind actorKind, ResourceExchangeVisualRequestComponent request, out GameObject prefab) =>
                {
                    prefab = actorKind == ResourceExchangeVisualActorKind.TransportPlane
                        ? planePrefab
                        : null;
                    return prefab != null;
                });

            ResourceExchangeVisualPresentationSystemHelper.Result result =
                helper.ConsumeVisualRequests(context, requests);

            Assert.AreEqual(2, result.ProcessedCount);
            Assert.AreEqual(2, result.PlayedCount);
            Assert.AreEqual(1, helper.ActiveActorCount);
            Assert.AreEqual(1, helper.CreatedActorCount);
            Transform plane = FindChild(root.transform, "TransportPlanePrefab_ResourceExchangeActor");
            Assert.IsNotNull(plane);
            Assert.IsTrue(Vector3.Distance(new Vector3(9f, 0f, 9f), plane.position) < 0.001f);
        }
        finally
        {
            helper.Dispose();
            DestroyImmediate(planePrefab);
            DestroyImmediate(root);
        }
    }

    [Test]
    public void MissingAnchorAndMissingPrefab_DoNotCreateActors()
    {
        using World world = new(nameof(MissingAnchorAndMissingPrefab_DoNotCreateActors));
        Entity entity = CreateVisualRequestEntity(world.EntityManager);
        DynamicBuffer<ResourceExchangeVisualRequestComponent> requests =
            world.EntityManager.GetBuffer<ResourceExchangeVisualRequestComponent>(entity);
        requests.Add(CreateRequest(
            ResourceExchangeVisualCueKind.TransportPlaneLanding,
            new float3(1f, 0f, 1f),
            anchorResolved: 0));
        requests.Add(CreateRequest(
            ResourceExchangeVisualCueKind.ExportLoadStarted,
            new float3(2f, 0f, 2f)));

        GameObject root = new("ResourceExchangeVisualPresentationFallbackRoot");
        var helper = new ResourceExchangeVisualPresentationSystemHelper();
        try
        {
            var context = new ResourceExchangeVisualPresentationSystemHelper.Context(
                root.transform,
                (ResourceExchangeVisualActorKind actorKind, ResourceExchangeVisualRequestComponent request, out GameObject prefab) =>
                {
                    prefab = null;
                    return false;
                });

            ResourceExchangeVisualPresentationSystemHelper.Result result =
                helper.ConsumeVisualRequests(context, requests);

            Assert.AreEqual(2, result.ProcessedCount);
            Assert.AreEqual(0, result.PlayedCount);
            Assert.AreEqual(1, result.MissingAnchorCount);
            Assert.AreEqual(1, result.MissingPrefabCount);
            Assert.AreEqual(0, helper.ActiveActorCount);
            Assert.AreEqual(0, helper.CreatedActorCount);
            Assert.AreEqual(0, requests.Length);
        }
        finally
        {
            helper.Dispose();
            DestroyImmediate(root);
        }
    }

    [Test]
    public void ResolveActorKind_MapsCueKindsToPresentationActors()
    {
        Assert.AreEqual(
            ResourceExchangeVisualActorKind.ExchangeMarker,
            ResourceExchangeVisualPresentationSystemHelper.ResolveActorKind(ResourceExchangeVisualCueKind.ExchangeStarted));
        Assert.AreEqual(
            ResourceExchangeVisualActorKind.ResourceTruck,
            ResourceExchangeVisualPresentationSystemHelper.ResolveActorKind(ResourceExchangeVisualCueKind.ExportLoadStarted));
        Assert.AreEqual(
            ResourceExchangeVisualActorKind.ResourceTruck,
            ResourceExchangeVisualPresentationSystemHelper.ResolveActorKind(ResourceExchangeVisualCueKind.ImportUnloadStarted));
        Assert.AreEqual(
            ResourceExchangeVisualActorKind.TransportPlane,
            ResourceExchangeVisualPresentationSystemHelper.ResolveActorKind(ResourceExchangeVisualCueKind.TransportPlaneDeparting));
        Assert.AreEqual(
            ResourceExchangeVisualActorKind.CompletionMarker,
            ResourceExchangeVisualPresentationSystemHelper.ResolveActorKind(ResourceExchangeVisualCueKind.ExchangeCompleted));
        Assert.AreEqual(
            ResourceExchangeVisualActorKind.CancellationMarker,
            ResourceExchangeVisualPresentationSystemHelper.ResolveActorKind(ResourceExchangeVisualCueKind.ExchangeCancelled));
    }

    private static Entity CreateVisualRequestEntity(EntityManager em)
    {
        Entity entity = em.CreateEntity();
        em.AddBuffer<ResourceExchangeVisualRequestComponent>(entity);
        return entity;
    }

    private static ResourceExchangeVisualPresentationSystemHelper.Context CreateContext(
        Transform root,
        GameObject planePrefab,
        GameObject truckPrefab)
    {
        return new ResourceExchangeVisualPresentationSystemHelper.Context(
            root,
            (ResourceExchangeVisualActorKind actorKind, ResourceExchangeVisualRequestComponent request, out GameObject prefab) =>
            {
                switch (actorKind)
                {
                    case ResourceExchangeVisualActorKind.TransportPlane:
                        prefab = planePrefab;
                        return true;
                    case ResourceExchangeVisualActorKind.ResourceTruck:
                        prefab = truckPrefab;
                        return true;
                    default:
                        prefab = null;
                        return false;
                }
            });
    }

    private static ResourceExchangeVisualRequestComponent CreateRequest(
        ResourceExchangeVisualCueKind cueKind,
        float3 anchorPosition,
        int queueItemId = 1,
        byte anchorResolved = 1)
    {
        return new ResourceExchangeVisualRequestComponent
        {
            QueueItemId = queueItemId,
            FactionId = 1,
            CueKind = cueKind,
            RecipeId = new FixedString128Bytes("exchange.presentation.test"),
            RouteType = ResourceExchangeRouteType.Export,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Credits,
            InputAmount = 100,
            OutputAmount = 75,
            RequestedAnchorKind = ResourceExchangePresentationAnchorKind.RunwayLandingZone,
            ResolvedAnchorKind = anchorResolved != 0
                ? ResourceExchangePresentationAnchorKind.RunwayLandingZone
                : ResourceExchangePresentationAnchorKind.None,
            AnchorPosition = anchorPosition,
            AnchorRotation = quaternion.identity,
            AnchorRadius = anchorResolved != 0 ? 4f : 0f,
            AnchorResolved = anchorResolved
        };
    }

    private static GameObject CreatePrefab(string name)
    {
        GameObject prefab = new(name);
        prefab.AddComponent<BoxCollider>();
        prefab.SetActive(false);
        return prefab;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static void AssertCollidersDisabled(GameObject instance)
    {
        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        Assert.AreEqual(1, colliders.Length);
        Assert.IsFalse(colliders[0].enabled);
    }

    private static void DestroyImmediate(GameObject instance)
    {
        if (instance != null)
            Object.DestroyImmediate(instance);
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeVisualPresentationSystemHelperTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeVisualPresentationSystemHelperTests();
        action(test);
        passed++;
    }
}
#endif
