using System;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class SelectionGameplayStartupSystemHelper
    {
        private static string ResolveUnitSourceName(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity))
                return string.Empty;

            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (!string.IsNullOrWhiteSpace(sourceName))
                    return sourceName;
            }

            return em.GetName(entity);
        }

        private static string DescribeTransportBoardingEntity(EntityManager em, Entity entity)
        {
            if (entity == Entity.Null)
                return "null";
            if (!em.Exists(entity))
                return $"{entity}:missing";

            string sourceName = ResolveUnitSourceName(em, entity);
            if (string.IsNullOrWhiteSpace(sourceName))
                sourceName = "<unnamed>";

            string cell = em.HasComponent<UnitGrid>(entity)
                ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
                : "no-cell";
            string faction = em.HasComponent<Faction>(entity)
                ? em.GetComponentData<Faction>(entity).Id.ToString()
                : "no-faction";
            string health = em.HasComponent<UnitHealth>(entity)
                ? $"{em.GetComponentData<UnitHealth>(entity).Current}/{em.GetComponentData<UnitHealth>(entity).Max}"
                : "no-health";
            string capacity = em.HasComponent<UnitTransportCapacity>(entity)
                ? em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity.ToString()
                : "no-capacity";
            string passengers = em.HasBuffer<UnitTransportPassengerElement>(entity)
                ? em.GetBuffer<UnitTransportPassengerElement>(entity).Length.ToString()
                : "no-passengers";

            return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health} seats={passengers}/{capacity}";
        }

        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private static Quaternion ToQuaternion(quaternion value)
        {
            return new Quaternion(value.value.x, value.value.y, value.value.z, value.value.w);
        }

        private static Action CreateDisposeAction(
            RtsSelectionRuntimeCameraSystemHelper runtimeCamera,
            SelectionUiCameraSystemHelper selectionCamera,
            TacticalFollowCameraModeSystemHelper tacticalFollowCamera,
            SelectionOrderMarkerPresentationSystemHelper orderMarkers)
        {
            return () =>
            {
                runtimeCamera.Dispose();
                selectionCamera.Dispose();
                tacticalFollowCamera.Dispose();
                orderMarkers.Dispose();
            };
        }

        private static RtsSelectionRuntimeCameraSystemHelper ResolveRtsSelectionRuntimeCameraSystemHelper()
        {
            return new RtsSelectionRuntimeCameraSystemHelper();
        }
    }
}
