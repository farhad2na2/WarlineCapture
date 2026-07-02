using Game.Components;
using Game.Rendering;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed partial class UnitRenderBudgetPerformanceValidation
{
    private const string ReportPath = "/private/tmp/warlinecapture-unit-render-budget-performance.json";
    private const int UnitCount = 512;
    private const int WarmupFrames = 32;
    private const int MeasuredFrames = 180;
    private const double MaxP95Ms = 8.0;
    private const double MaxP99Ms = 16.0;
    private const long MaxAllocatedBytes = 4096;

    public static void RunBatchValidation()
    {
        try
        {
            var tests = new UnitRenderBudgetPerformanceValidation();
            tests.RenderBudgetDistanceAndSortReportTiming();
            Debug.Log("[UnitRenderBudgetPerformanceValidation] result=Passed");
            ValidationExit.Passed();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[UnitRenderBudgetPerformanceValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RenderBudgetDistanceAndSortReportTiming()
    {
        using World world = new("UnitRenderBudgetPerformanceValidation");
        EntityManager em = world.EntityManager;
        GameObject cameraObject = new("UnitRenderBudgetPerformanceCamera");
        Camera camera = cameraObject.AddComponent<Camera>();

        try
        {
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2500f;
            camera.fieldOfView = 45f;
            CreateUnits(em);

            EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> units = query.ToEntityArray(Allocator.TempJob);
            using NativeArray<LocalTransform> transforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            using NativeList<UnitRenderBudgetDistance.UnitDistance> distances = new(UnitCount, Allocator.TempJob);
            UnitRenderBudgetDistance distanceSystem = new();
            UnitRenderBudgetSort sortSystem = new();
            UnitRenderBudgetLookupSystem lookupSystem = world.GetOrCreateSystemManaged<UnitRenderBudgetLookupSystem>();

            Assert.AreEqual(UnitCount, units.Length);
            for (int i = 0; i < WarmupFrames; i++)
                RunFrame(camera, lookupSystem, distanceSystem, sortSystem, units, transforms, distances, i);

            var samples = new double[MeasuredFrames];
            long allocationStart = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < MeasuredFrames; i++)
            {
                long start = Stopwatch.GetTimestamp();
                RunFrame(camera, lookupSystem, distanceSystem, sortSystem, units, transforms, distances, WarmupFrames + i);
                long stop = Stopwatch.GetTimestamp();
                samples[i] = TicksToMilliseconds(stop - start);
            }

            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
            Array.Sort(samples);
            double averageMs = Average(samples);
            double p95Ms = PercentileSorted(samples, 0.95);
            double p99Ms = PercentileSorted(samples, 0.99);
            double maxMs = samples[^1];

            WriteReport(averageMs, p95Ms, p99Ms, maxMs, allocatedBytes);

            Assert.Greater(distances.Length, 0);
            Assert.LessOrEqual(p95Ms, MaxP95Ms);
            Assert.LessOrEqual(p99Ms, MaxP99Ms);
            Assert.LessOrEqual(allocatedBytes, MaxAllocatedBytes);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static void RunFrame(
        Camera camera,
        UnitRenderBudgetLookupSystem lookupSystem,
        UnitRenderBudgetDistance distanceSystem,
        UnitRenderBudgetSort sortSystem,
        NativeArray<Entity> units,
        NativeArray<LocalTransform> transforms,
        NativeList<UnitRenderBudgetDistance.UnitDistance> distances,
        int frame)
    {
        float angle = frame * 0.035f;
        float height = 55f + math.sin(frame * 0.05f) * 12f;
        camera.transform.position = new Vector3(math.cos(angle) * 85f, height, math.sin(angle) * 85f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 0f) - camera.transform.position, Vector3.up);

        lookupSystem.Update();
        RuntimeCameraSnapshotComponent cameraSnapshot = CreateCameraSnapshot(camera);
        distances.Clear();
        distanceSystem.Collect(
            cameraSnapshot,
            units,
            transforms,
            distances,
            lookupSystem.GetPassengerLookup(),
            lookupSystem.GetStorageInfoLookup(),
            alwaysDetailedDistanceSq: 18f * 18f,
            viewportPadding: 0.35f,
            edgeSafetyMargin: 0.18f);
        sortSystem.Sort(distances);
    }

    private static RuntimeCameraSnapshotComponent CreateCameraSnapshot(Camera camera)
    {
        float4x4 worldToCamera = ToFloat4x4(camera.worldToCameraMatrix);
        float4x4 projection = ToFloat4x4(camera.projectionMatrix);
        return new RuntimeCameraSnapshotComponent
        {
            IsValid = 1,
            Position = camera.transform.position,
            Rotation = camera.transform.rotation,
            WorldToCamera = worldToCamera,
            Projection = projection,
            ViewProjection = math.mul(projection, worldToCamera)
        };
    }

    private static float4x4 ToFloat4x4(Matrix4x4 value)
    {
        return new float4x4(
            new float4(value.m00, value.m10, value.m20, value.m30),
            new float4(value.m01, value.m11, value.m21, value.m31),
            new float4(value.m02, value.m12, value.m22, value.m32),
            new float4(value.m03, value.m13, value.m23, value.m33));
    }

    private static void CreateUnits(EntityManager em)
    {
        for (int i = 0; i < UnitCount; i++)
        {
            int x = i % 32;
            int z = i / 32;
            Entity unit = em.CreateEntity(typeof(UnitHealth), typeof(LocalTransform));
            em.SetComponentData(unit, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(unit, LocalTransform.FromPosition(new float3(x * 4f - 64f, 0f, z * 4f - 32f)));
            if ((i % 31) == 0)
                em.AddComponentData(unit, new UnitTransportPassenger { Transport = Entity.Null });
        }
    }

    private static double Average(double[] samples)
    {
        double total = 0d;
        for (int i = 0; i < samples.Length; i++)
            total += samples[i];
        return total / samples.Length;
    }

    private static double PercentileSorted(double[] samples, double percentile)
    {
        int index = (int)math.clamp(
            math.ceil((float)(samples.Length * percentile)) - 1,
            0,
            samples.Length - 1);
        return samples[index];
    }

    private static double TicksToMilliseconds(long ticks)
    {
        return ticks * 1000d / Stopwatch.Frequency;
    }

    private static void WriteReport(double averageMs, double p95Ms, double p99Ms, double maxMs, long allocatedBytes)
    {
        StringBuilder builder = new();
        builder.AppendLine("{");
        AppendJson(builder, "unitCount", UnitCount, trailingComma: true);
        AppendJson(builder, "warmupFrames", WarmupFrames, trailingComma: true);
        AppendJson(builder, "measuredFrames", MeasuredFrames, trailingComma: true);
        AppendJson(builder, "averageMs", averageMs, trailingComma: true);
        AppendJson(builder, "p95Ms", p95Ms, trailingComma: true);
        AppendJson(builder, "p99Ms", p99Ms, trailingComma: true);
        AppendJson(builder, "maxMs", maxMs, trailingComma: true);
        AppendJson(builder, "allocatedBytesCurrentThread", allocatedBytes, trailingComma: false);
        builder.AppendLine("}");
        File.WriteAllText(ReportPath, builder.ToString());
    }

    private static void AppendJson(StringBuilder builder, string name, int value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value);
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, long value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value);
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    private static void AppendJson(StringBuilder builder, string name, double value, bool trailingComma)
    {
        builder.Append("  \"").Append(name).Append("\": ")
            .Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        builder.AppendLine(trailingComma ? "," : string.Empty);
    }

    [DisableAutoCreation]
    private sealed partial class UnitRenderBudgetLookupSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }

        public ComponentLookup<UnitTransportPassenger> GetPassengerLookup()
        {
            return GetComponentLookup<UnitTransportPassenger>(true);
        }

        public EntityStorageInfoLookup GetStorageInfoLookup()
        {
            return GetEntityStorageInfoLookup();
        }
    }
}
#endif
