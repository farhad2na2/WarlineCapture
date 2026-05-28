using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public readonly struct UnitRenderBudgetDistanceSystem
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
        EntityManager em,
        Camera camera,
        NativeArray<Entity> units,
        NativeArray<LocalTransform> transforms,
        NativeList<UnitDistance> distances,
        float alwaysDetailedDistanceSq,
        float viewportPadding,
        float edgeSafetyMargin)
    {
        float3 cameraPosition = camera.transform.position;
        for (int i = 0; i < units.Length; i++)
        {
            Entity unit = units[i];
            if (!em.Exists(unit) || em.HasComponent<UnitTransportPassenger>(unit))
                continue;

            float3 unitPosition = transforms[i].Position;
            float distanceSq = math.distancesq(unitPosition, cameraPosition);
            Vector3 worldPosition = new(unitPosition.x, unitPosition.y, unitPosition.z);
            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPosition);
            bool visible =
                viewportPosition.z > 0f &&
                viewportPosition.x >= -viewportPadding && viewportPosition.x <= 1f + viewportPadding &&
                viewportPosition.y >= -viewportPadding && viewportPosition.y <= 1f + viewportPadding;
            bool screenEdge =
                visible &&
                (viewportPosition.x <= edgeSafetyMargin ||
                 viewportPosition.x >= 1f - edgeSafetyMargin ||
                 viewportPosition.y <= edgeSafetyMargin ||
                 viewportPosition.y >= 1f - edgeSafetyMargin ||
                 viewportPosition.x < 0f ||
                 viewportPosition.x > 1f ||
                 viewportPosition.y < 0f ||
                 viewportPosition.y > 1f);
            bool near = distanceSq <= alwaysDetailedDistanceSq;
            byte priority = near
                ? (byte)(visible ? 0 : 1)
                : (byte)(visible ? 2 : 3);
            distances.Add(new UnitDistance
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
