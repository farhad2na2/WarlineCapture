using System;
using Game.Components;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

public sealed class OperationMapEcsContractTests
{
    [Test]
    public void ComponentsAndBuffers_UseExpectedEcsKinds()
    {
        AssertComponent<OperationMapRootComponent>();
        AssertComponent<OperationMapQueueComponent>();
        AssertComponent<OperationMapLoadStateComponent>();
        AssertComponent<ActiveOperationMapComponent>();
        AssertComponent<OperationMapBoundsComponent>();
        AssertComponent<OperationMapMetadataComponent>();
        AssertComponent<OperationMapReadinessComponent>();
        AssertBuffer<OperationMapLoadRequestElement>();
        AssertBuffer<OperationMapLoadResultElement>();
    }

    [Test]
    public void ContractTypes_AreUnmanaged()
    {
        AssertUnmanaged<OperationMapRootComponent>();
        AssertUnmanaged<OperationMapQueueComponent>();
        AssertUnmanaged<OperationMapLoadStateComponent>();
        AssertUnmanaged<ActiveOperationMapComponent>();
        AssertUnmanaged<OperationMapBoundsComponent>();
        AssertUnmanaged<OperationMapMetadataComponent>();
        AssertUnmanaged<OperationMapReadinessComponent>();
        AssertUnmanaged<OperationMapLoadRequestElement>();
        AssertUnmanaged<OperationMapLoadResultElement>();
        AssertUnmanaged<OperationMapBlob>();
        AssertUnmanaged<OperationMapAnchorBlob>();
        AssertUnmanaged<OperationMapCameraBlob>();
        AssertUnmanaged<OperationMapMinimapBlob>();
    }

    [Test]
    public void Readiness_UsesExplicitGenerationScopedFlags()
    {
        OperationMapReadinessFlags required = OperationMapReadinessFlags.SourceContent |
                                              OperationMapReadinessFlags.Metadata |
                                              OperationMapReadinessFlags.MapSurface |
                                              OperationMapReadinessFlags.AuthoredConversion |
                                              OperationMapReadinessFlags.PresentationManifest |
                                              OperationMapReadinessFlags.RequiredPresentationPreload;

        Assert.That(required.HasFlag(OperationMapReadinessFlags.SourceContent), Is.True);
        Assert.That(required.HasFlag(OperationMapReadinessFlags.Metadata), Is.True);
        Assert.That(required.HasFlag(OperationMapReadinessFlags.MapSurface), Is.True);
        Assert.That(required.HasFlag(OperationMapReadinessFlags.AuthoredConversion), Is.True);
        Assert.That(required.HasFlag(OperationMapReadinessFlags.PresentationManifest), Is.True);
        Assert.That(required.HasFlag(OperationMapReadinessFlags.RequiredPresentationPreload), Is.True);
        Assert.That(required.HasFlag(OperationMapReadinessFlags.SubScene), Is.False,
            "Subscene readiness is optional and must be added to RequiredFlags only when selected content needs it.");

        OperationMapReadinessComponent readiness = new()
        {
            Generation = 4,
            ReadyFlags = required,
            RequiredFlags = required
        };
        Assert.That((readiness.ReadyFlags & readiness.RequiredFlags) == readiness.RequiredFlags, Is.True);
        Assert.That(readiness.FailedFlags, Is.EqualTo(OperationMapReadinessFlags.None));
    }

    [Test]
    public void ActiveAndRequestData_CarryBoundedMapScenarioAndMissionIds()
    {
        ActiveOperationMapComponent active = new()
        {
            OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
            ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
            MissionId = new FixedString64Bytes("m01"),
            SchemaVersion = 1,
            ContentVersion = 1,
            Generation = 2
        };
        OperationMapLoadRequestElement request = new()
        {
            Kind = OperationMapLoadRequestKind.Load,
            RequestId = 7,
            OperationMapId = active.OperationMapId,
            ScenarioId = active.ScenarioId,
            MissionId = active.MissionId,
            ActivateOnLoad = 1
        };

        Assert.That(request.OperationMapId, Is.EqualTo(active.OperationMapId));
        Assert.That(request.ScenarioId, Is.EqualTo(active.ScenarioId));
        Assert.That(request.MissionId, Is.EqualTo(active.MissionId));
        Assert.That(request.ActivateOnLoad, Is.EqualTo(1));
    }

    [Test]
    public void BoundsAndBlobRecords_UseBurstReadableMathAndFixedStrings()
    {
        OperationMapBoundsComponent bounds = new()
        {
            WorldMin = new float3(-100f, -10f, -100f),
            WorldMax = new float3(100f, 50f, 100f),
            CameraMin = new float3(-80f, 10f, -80f),
            CameraMax = new float3(80f, 40f, 80f)
        };
        OperationMapAnchorBlob anchor = new()
        {
            Id = new FixedString64Bytes("anchor.ch01.m01.objective.patrol"),
            Kind = OperationMapAnchorKind.Objective,
            Position = float3.zero,
            Rotation = quaternion.identity,
            Radius = 4f,
            FactionId = -1,
            LaneIndex = -1
        };

        Assert.That(bounds.WorldMin.x, Is.LessThan(bounds.WorldMax.x));
        Assert.That(anchor.Id.ToString(), Is.EqualTo("anchor.ch01.m01.objective.patrol"));
        Assert.That(anchor.Kind, Is.EqualTo(OperationMapAnchorKind.Objective));
    }

    [Test]
    public void BufferAndStateSizes_RemainBounded()
    {
        Assert.That(UnsafeUtility.SizeOf<OperationMapLoadStateComponent>(), Is.LessThanOrEqualTo(32));
        Assert.That(UnsafeUtility.SizeOf<ActiveOperationMapComponent>(), Is.LessThanOrEqualTo(224));
        Assert.That(UnsafeUtility.SizeOf<OperationMapLoadRequestElement>(), Is.LessThanOrEqualTo(224));
        Assert.That(UnsafeUtility.SizeOf<OperationMapLoadResultElement>(), Is.LessThanOrEqualTo(384));
        Assert.That(UnsafeUtility.SizeOf<OperationMapBoundsComponent>(), Is.EqualTo(72));
    }

    [Test]
    public void ResultCodes_CoverFailureUnwindBoundaries()
    {
        Assert.That(OperationMapLoadResultCode.InvalidOperationMapId, Is.Not.EqualTo(OperationMapLoadResultCode.None));
        Assert.That(OperationMapLoadResultCode.MissingSourceContent, Is.Not.EqualTo(OperationMapLoadResultCode.None));
        Assert.That(OperationMapLoadResultCode.StaleContent, Is.Not.EqualTo(OperationMapLoadResultCode.None));
        Assert.That(OperationMapLoadResultCode.MetadataBindFailed, Is.Not.EqualTo(OperationMapLoadResultCode.None));
        Assert.That(OperationMapLoadResultCode.PresentationPreloadFailed, Is.Not.EqualTo(OperationMapLoadResultCode.None));
        Assert.That(OperationMapLoadResultCode.Interrupted, Is.Not.EqualTo(OperationMapLoadResultCode.None));
        Assert.That(OperationMapLoadResultCode.SourceUnloadFailed, Is.Not.EqualTo(OperationMapLoadResultCode.None));
        Assert.That(OperationMapLoadResultCode.TeardownFailed, Is.Not.EqualTo(OperationMapLoadResultCode.None));
    }

    private static void AssertComponent<T>() where T : unmanaged, IComponentData =>
        Assert.That(typeof(IComponentData).IsAssignableFrom(typeof(T)), Is.True, typeof(T).Name);

    private static void AssertBuffer<T>() where T : unmanaged, IBufferElementData =>
        Assert.That(typeof(IBufferElementData).IsAssignableFrom(typeof(T)), Is.True, typeof(T).Name);

    private static void AssertUnmanaged<T>() where T : unmanaged =>
        Assert.That(UnsafeUtility.IsUnmanaged<T>(), Is.True, typeof(T).Name);
}
