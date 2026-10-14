using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class VehicleSlopeAlignmentPauseTests
{
    // Rotation and position captured from the enemy logistics truck in native S005
    // lifecycle run 1460, paused baseline cycle 5/frame 4559.
    private static readonly LocalTransform CapturedTruck = new LocalTransform
    {
        Position = new float3(1413.6185302734375f, 0.00917959213256836f, 657.3849487304688f),
        Rotation = new quaternion(0f, -0.05294444411993027f, 0f, 0.9985975623130798f),
        Scale = 1f
    };

    [Test]
    public void FrozenVehicleAlignmentPreservesExactTruckStateAndResumes()
    {
        using var world = new World("S005 captured logistics truck slope pause");
        var em = world.EntityManager;
        var gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { SimulationActive = 1 });
        var truck = em.CreateEntity(typeof(LocalTransform), typeof(UnitSurfaceComponent),
            typeof(VehicleSurfaceAlignmentComponent), typeof(UnitFootprint), typeof(UnitMovementBehavior));
        em.SetComponentData(truck, CapturedTruck);
        em.SetComponentData(truck, new UnitSurfaceComponent
        {
            HasSurface = 1,
            LastSampledNormal = math.normalize(new float3(0.18f, 1f, 0.12f))
        });
        em.SetComponentData(truck, new UnitFootprint { Size = new int2(2, 3) });
        em.SetComponentData(truck, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        var beforeAlignment = em.GetComponentData<VehicleSurfaceAlignmentComponent>(truck);
        var system = world.CreateSystem<VehicleSlopeAlignmentSystem>();

        void Step(double elapsed, float delta)
        {
            world.SetTime(new TimeData(elapsed, delta));
            system.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
        }

        // Both pause signals matter: the Editor can continue rendering at zero
        // scaled delta, and the gameplay state can freeze before time stops.
        for (int frame = 0; frame < 13; frame++)
        {
            Step(100d, 0f);
            RequireFrozen(frame, "zero-delta", em.GetComponentData<LocalTransform>(truck),
                em.GetComponentData<VehicleSurfaceAlignmentComponent>(truck), beforeAlignment);
        }
        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { SimulationActive = 0 });
        for (int frame = 0; frame < 13; frame++)
        {
            Step(100d + (frame + 1) * 0.016d, 0.016f);
            RequireFrozen(frame, "inactive-gameplay", em.GetComponentData<LocalTransform>(truck),
                em.GetComponentData<VehicleSurfaceAlignmentComponent>(truck), beforeAlignment);
        }

        em.SetComponentData(gameplay, new RuntimeGameplayStateComponent { SimulationActive = 1 });
        Step(101d, 0.1f);
        var resumedTransform = em.GetComponentData<LocalTransform>(truck);
        var resumedAlignment = em.GetComponentData<VehicleSurfaceAlignmentComponent>(truck);
        Assert.That(math.lengthsq(resumedTransform.Rotation.value - CapturedTruck.Rotation.value),
            Is.GreaterThan(1e-8f), "Positive delta must resume slope rotation.");
        Assert.That(resumedAlignment.AlignmentWeight, Is.GreaterThan(0f),
            "Positive delta must resume slope alignment.");
        Assert.That(math.lengthsq(resumedAlignment.SurfaceNormal - math.up()), Is.GreaterThan(1e-5f),
            "The resumed job must consume the sampled slope.");
    }

    public static void RunFocusedValidation()
    {
        try
        {
            new VehicleSlopeAlignmentPauseTests().FrozenVehicleAlignmentPreservesExactTruckStateAndResumes();
            Debug.Log("[VehicleSlopeAlignmentPauseTests] result=Passed capturedTruck=1460 zeroDeltaFrames=13 inactiveFrames=13 exactFrozenTransformAndAlignment=1 positiveDeltaResumed=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[VehicleSlopeAlignmentPauseTests] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RequireFrozen(int frame, string phase, LocalTransform transform,
        VehicleSurfaceAlignmentComponent alignment, VehicleSurfaceAlignmentComponent originalAlignment)
    {
        Assert.That(SameBits(transform.Position, CapturedTruck.Position) &&
                    SameBits(transform.Rotation.value, CapturedTruck.Rotation.value) &&
                    SameBits(transform.Scale, CapturedTruck.Scale), Is.True,
            $"{phase} frame {frame} changed captured truck transform");
        Assert.That(SameBits(alignment.SurfaceNormal, originalAlignment.SurfaceNormal) &&
                    SameBits(alignment.PitchDegrees, originalAlignment.PitchDegrees) &&
                    SameBits(alignment.RollDegrees, originalAlignment.RollDegrees) &&
                    SameBits(alignment.AlignmentWeight, originalAlignment.AlignmentWeight), Is.True,
            $"{phase} frame {frame} changed truck alignment state");
    }

    private static bool SameBits(float a, float b) =>
        BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b);

    private static bool SameBits(float3 a, float3 b) =>
        SameBits(a.x, b.x) && SameBits(a.y, b.y) && SameBits(a.z, b.z);

    private static bool SameBits(float4 a, float4 b) =>
        SameBits(a.x, b.x) && SameBits(a.y, b.y) && SameBits(a.z, b.z) && SameBits(a.w, b.w);
}
