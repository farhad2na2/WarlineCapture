using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    public sealed class BuildingDestroyedVisualPresentationSystemHelper
    {
        public readonly struct Context
        {
            public readonly BuildingVisualSystem VisualSystem;
            public readonly BuildingCombatUtilitySystemHelper.ObjectAction DestroyObject;

            public Context(
                BuildingVisualSystem visualSystem,
                BuildingCombatUtilitySystemHelper.ObjectAction destroyObject)
            {
                VisualSystem = visualSystem;
                DestroyObject = destroyObject;
            }
        }

        internal void BeginDestroyedVisual(Context context, RuntimeBuildingEntity building)
        {
            if (building == null)
                return;

            // Copy a legacy destroyed child before hiding its live ancestors.
            if (building.DestroyedVisualInstance == null && building.Instance != null &&
                (building.Definition == null || building.Definition.DestroyedVisualPrefab == null))
            {
                foreach (Transform child in building.Instance.GetComponentsInChildren<Transform>(true))
                {
                    if (!string.Equals(child.name, "Destroyed", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    building.DestroyedVisualInstance = Object.Instantiate(child.gameObject, building.Instance.transform, true);
                    break;
                }
            }
            HideAliveVisuals(context, building);
            if (building.DestroyedVisualInstance != null)
            {
                building.DestroyedVisualInstance.SetActive(true);
                return;
            }

            GameObject prefab = building.Definition != null ? building.Definition.DestroyedVisualPrefab : null;
            if (prefab == null || building.Instance == null)
                return;

            Transform source = building.Instance.transform;
            Transform visualRoot = source.childCount > 0 ? source.GetChild(0) : source;
            bool hasWrapperVisualRoot = visualRoot != source;
            Transform parent = hasWrapperVisualRoot ? source : source.parent;
            GameObject instance = Object.Instantiate(prefab, parent, false);
            instance.name = $"{source.name}_Destroyed";
            if (hasWrapperVisualRoot)
            {
                instance.transform.localPosition = visualRoot.localPosition;
                instance.transform.localRotation = visualRoot.localRotation;
                instance.transform.localScale = visualRoot.localScale;
            }
            else
            {
                instance.transform.SetPositionAndRotation(source.position, source.rotation);
                instance.transform.localScale = source.localScale;
            }

            instance.SetActive(true);
            building.DestroyedVisualInstance = instance;
        }

        internal void CleanupDestroyedVisual(Context context, RuntimeBuildingEntity building)
        {
            if (building?.DestroyedVisualInstance == null)
                return;

            context.DestroyObject?.Invoke(building.DestroyedVisualInstance);
            building.DestroyedVisualInstance = null;
        }

        private static void HideAliveVisuals(Context context, RuntimeBuildingEntity building)
        {
            IReadOnlyList<Transform> aliveRoots = building.AliveVisualRootTransforms;
            if (aliveRoots == null)
                return;

            for (int i = 0; i < aliveRoots.Count; i++)
                context.VisualSystem?.SetTransformVisible(aliveRoots[i], false);
        }
    }
}
