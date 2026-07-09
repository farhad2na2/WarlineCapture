using System;
using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class ResourceExchangePresentationAnchorUtilityTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ResolveAnchor_UsesPreferredValidAnchorBeforeFallback),
                test => test.ResolveAnchor_UsesPreferredValidAnchorBeforeFallback(),
                ref passed);
            RunValidationStep(
                nameof(ResolveAnchor_FallsBackToSafeAnchorWhenPreferredMissing),
                test => test.ResolveAnchor_FallsBackToSafeAnchorWhenPreferredMissing(),
                ref passed);
            RunValidationStep(
                nameof(ResolveAnchor_IgnoresWrongFactionInvalidAndZeroRadiusAnchors),
                test => test.ResolveAnchor_IgnoresWrongFactionInvalidAndZeroRadiusAnchors(),
                ref passed);
            RunValidationStep(
                nameof(ResolveAnchor_UsesDeterministicFallbackOrderWhenSafeMissing),
                test => test.ResolveAnchor_UsesDeterministicFallbackOrderWhenSafeMissing(),
                ref passed);

            Debug.Log($"[ResourceExchangePresentationAnchorValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangePresentationAnchorValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ResolveAnchor_UsesPreferredValidAnchorBeforeFallback()
    {
        using World world = new(nameof(ResolveAnchor_UsesPreferredValidAnchorBeforeFallback));
        DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors = CreateAnchorBuffer(world.EntityManager);
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.FallbackSafe, new float3(1f, 0f, 1f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.RunwayLandingZone, new float3(12f, 0f, 4f)));

        bool resolved = ResourceExchangePresentationAnchorUtility.TryResolveAnchor(
            anchors,
            1,
            ResourceExchangePresentationAnchorKind.RunwayLandingZone,
            out ResourceExchangePresentationAnchorComponent anchor,
            out ResourceExchangePresentationAnchorKind resolvedKind,
            out byte usedFallback);

        Assert.IsTrue(resolved);
        Assert.AreEqual(ResourceExchangePresentationAnchorKind.RunwayLandingZone, resolvedKind);
        Assert.AreEqual(0, usedFallback);
        Assert.AreEqual(new float3(12f, 0f, 4f), anchor.Position);
    }

    [Test]
    public void ResolveAnchor_FallsBackToSafeAnchorWhenPreferredMissing()
    {
        using World world = new(nameof(ResolveAnchor_FallsBackToSafeAnchorWhenPreferredMissing));
        DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors = CreateAnchorBuffer(world.EntityManager);
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.FallbackSafe, new float3(3f, 0f, 8f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.Storage, new float3(20f, 0f, 20f)));

        bool resolved = ResourceExchangePresentationAnchorUtility.TryResolveAnchor(
            anchors,
            1,
            ResourceExchangePresentationAnchorKind.RunwayLandingZone,
            out ResourceExchangePresentationAnchorComponent anchor,
            out ResourceExchangePresentationAnchorKind resolvedKind,
            out byte usedFallback);

        Assert.IsTrue(resolved);
        Assert.AreEqual(ResourceExchangePresentationAnchorKind.FallbackSafe, resolvedKind);
        Assert.AreEqual(1, usedFallback);
        Assert.AreEqual(new float3(3f, 0f, 8f), anchor.Position);
    }

    [Test]
    public void ResolveAnchor_IgnoresWrongFactionInvalidAndZeroRadiusAnchors()
    {
        using World world = new(nameof(ResolveAnchor_IgnoresWrongFactionInvalidAndZeroRadiusAnchors));
        DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors = CreateAnchorBuffer(world.EntityManager);
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.RunwayLandingZone, new float3(50f, 0f, 50f), factionId: 2));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.RunwayLandingZone, new float3(60f, 0f, 60f), isValid: 0));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.FallbackSafe, new float3(70f, 0f, 70f), radius: 0f));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.BaseDepot, new float3(5f, 0f, 6f)));

        bool resolved = ResourceExchangePresentationAnchorUtility.TryResolveAnchor(
            anchors,
            1,
            ResourceExchangePresentationAnchorKind.RunwayLandingZone,
            out ResourceExchangePresentationAnchorComponent anchor,
            out ResourceExchangePresentationAnchorKind resolvedKind,
            out byte usedFallback);

        Assert.IsTrue(resolved);
        Assert.AreEqual(ResourceExchangePresentationAnchorKind.BaseDepot, resolvedKind);
        Assert.AreEqual(1, usedFallback);
        Assert.AreEqual(new float3(5f, 0f, 6f), anchor.Position);
    }

    [Test]
    public void ResolveAnchor_UsesDeterministicFallbackOrderWhenSafeMissing()
    {
        using World world = new(nameof(ResolveAnchor_UsesDeterministicFallbackOrderWhenSafeMissing));
        DynamicBuffer<ResourceExchangePresentationAnchorComponent> anchors = CreateAnchorBuffer(world.EntityManager);
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.RunwayLandingZone, new float3(30f, 0f, 1f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.Storage, new float3(20f, 0f, 1f)));
        anchors.Add(CreateAnchor(ResourceExchangePresentationAnchorKind.BaseDepot, new float3(10f, 0f, 1f)));

        bool resolved = ResourceExchangePresentationAnchorUtility.TryResolveAnchor(
            anchors,
            1,
            ResourceExchangePresentationAnchorKind.None,
            out ResourceExchangePresentationAnchorComponent anchor,
            out ResourceExchangePresentationAnchorKind resolvedKind,
            out byte usedFallback);

        Assert.IsTrue(resolved);
        Assert.AreEqual(ResourceExchangePresentationAnchorKind.BaseDepot, resolvedKind);
        Assert.AreEqual(1, usedFallback);
        Assert.AreEqual(new float3(10f, 0f, 1f), anchor.Position);
    }

    private static DynamicBuffer<ResourceExchangePresentationAnchorComponent> CreateAnchorBuffer(EntityManager em)
    {
        Entity entity = em.CreateEntity();
        return em.AddBuffer<ResourceExchangePresentationAnchorComponent>(entity);
    }

    private static ResourceExchangePresentationAnchorComponent CreateAnchor(
        ResourceExchangePresentationAnchorKind anchorKind,
        float3 position,
        byte factionId = 1,
        byte isValid = 1,
        float radius = 3f)
    {
        return new ResourceExchangePresentationAnchorComponent
        {
            FactionId = factionId,
            AnchorKind = anchorKind,
            AnchorId = new FixedString64Bytes(anchorKind.ToString()),
            Position = position,
            Rotation = quaternion.identity,
            Radius = radius,
            IsValid = isValid
        };
    }

    private static void RunValidationStep(string name, Action<ResourceExchangePresentationAnchorUtilityTests> action, ref int passed)
    {
        var test = new ResourceExchangePresentationAnchorUtilityTests();
        action(test);
        passed++;
    }
}
#endif
