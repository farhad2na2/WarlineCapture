using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;

public readonly struct UnitRenderBudgetDistance
{
    public struct UnitDistance
    {
        public Entity Unit;
        public float DistanceSq;
        public byte Priority;
        public byte Visible;
        public byte ScreenEdge;
    }

    public void Collect(
        RuntimeCameraSnapshotComponent camera,
        NativeArray<Entity> units,
        NativeArray<LocalTransform> transforms,
        NativeList<UnitDistance> distances,
        ComponentLookup<UnitTransportPassenger> passengerLookup,
        EntityStorageInfoLookup entityStorageInfoLookup,
        float alwaysDetailedDistanceSq,
        float viewportPadding,
        float edgeSafetyMargin)
    {
        if (camera.IsValid == 0 || !units.IsCreated || !transforms.IsCreated || !distances.IsCreated)
        {
            if (distances.IsCreated)
                distances.Clear();
            return;
        }

        int count = math.min(units.Length, transforms.Length);
        if (distances.Capacity < count)
            distances.Capacity = count;
        distances.Clear();
        if (count == 0)
            return;

        new CollectDistanceJob
        {
            Units = units,
            Transforms = transforms,
            PassengerLookup = passengerLookup,
            EntityStorageInfoLookup = entityStorageInfoLookup,
            Distances = distances.AsParallelWriter(),
            CameraPosition = camera.Position,
            WorldToCamera = camera.WorldToCamera,
            ViewProjection = camera.ViewProjection,
            AlwaysDetailedDistanceSq = alwaysDetailedDistanceSq,
            ViewportPadding = viewportPadding,
            EdgeSafetyMargin = edgeSafetyMargin
        }.ScheduleParallel(count, 64, default).Complete();
    }

    [BurstCompile]
    private struct CollectDistanceJob : IJobFor
    {
        [ReadOnly] public NativeArray<Entity> Units;
        [ReadOnly] public NativeArray<LocalTransform> Transforms;
        [ReadOnly] public ComponentLookup<UnitTransportPassenger> PassengerLookup;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        public NativeList<UnitDistance>.ParallelWriter Distances;
        public float3 CameraPosition;
        public float4x4 WorldToCamera;
        public float4x4 ViewProjection;
        public float AlwaysDetailedDistanceSq;
        public float ViewportPadding;
        public float EdgeSafetyMargin;

        public void Execute(int i)
        {
            Entity unit = Units[i];
            if (!EntityStorageInfoLookup.Exists(unit) || PassengerLookup.HasComponent(unit))
                return;

            float3 unitPosition = Transforms[i].Position;
            float distanceSq = math.distancesq(unitPosition, CameraPosition);
            float4 worldPosition = new(unitPosition, 1f);
            float4 cameraPosition = math.mul(WorldToCamera, worldPosition);
            float4 clipPosition = math.mul(ViewProjection, worldPosition);
            float invW = math.abs(clipPosition.w) > 0.000001f ? 1f / clipPosition.w : 0f;
            float viewportX = clipPosition.x * invW * 0.5f + 0.5f;
            float viewportY = clipPosition.y * invW * 0.5f + 0.5f;
            float viewportZ = -cameraPosition.z;
            bool visible =
                viewportZ > 0f &&
                viewportX >= -ViewportPadding && viewportX <= 1f + ViewportPadding &&
                viewportY >= -ViewportPadding && viewportY <= 1f + ViewportPadding;
            bool screenEdge =
                visible &&
                (viewportX <= EdgeSafetyMargin ||
                 viewportX >= 1f - EdgeSafetyMargin ||
                 viewportY <= EdgeSafetyMargin ||
                 viewportY >= 1f - EdgeSafetyMargin ||
                 viewportX < 0f ||
                 viewportX > 1f ||
                 viewportY < 0f ||
                 viewportY > 1f);
            bool near = distanceSq <= AlwaysDetailedDistanceSq;
            byte priority = near
                ? (byte)(visible ? 0 : 1)
                : (byte)(visible ? 2 : 3);
            Distances.AddNoResize(new UnitDistance
            {
                Unit = unit,
                DistanceSq = distanceSq,
                Priority = priority,
                Visible = visible ? (byte)1 : (byte)0,
                ScreenEdge = screenEdge ? (byte)1 : (byte)0
            });
        }
    }
}
