using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed partial class BuildingDestroyedVisualSystem : SystemBase
{
    public readonly struct Context
    {
        public readonly BuildingVisualSystem VisualSystem;
        public readonly BuildingCombatSystem.ObjectAction DestroyObject;

        public Context(
            BuildingVisualSystem visualSystem,
            BuildingCombatSystem.ObjectAction destroyObject)
        {
            VisualSystem = visualSystem;
            DestroyObject = destroyObject;
        }
    }

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    internal void BeginDestroyedVisual(Context context, RuntimeBuildingEntity building)
    {
        if (building == null)
            return;

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
        GameObject instance = Object.Instantiate(prefab, source.position, source.rotation, source.parent);
        instance.name = $"{source.name}_Destroyed";
        instance.transform.localScale = source.localScale;
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
