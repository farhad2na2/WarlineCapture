using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingRuntimeVisualPresentationSystemHelper
{
    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Dictionary<int, RuntimeBuildingEntity> RuntimeBuildingMap;
        public readonly BuildingVisualSystem VisualSystem;
        public readonly BuildingFactionVisualSystem FactionVisualSystem;
        public readonly BuildingBarrierSystem BarrierSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;
        public readonly float FactionTintStrength;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            BuildingVisualSystem visualSystem,
            BuildingFactionVisualSystem factionVisualSystem,
            BuildingBarrierSystem barrierSystem,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock,
            float factionTintStrength)
        {
            RuntimeBuildings = runtimeBuildings;
            RuntimeBuildingMap = runtimeBuildings as Dictionary<int, RuntimeBuildingEntity>;
            VisualSystem = visualSystem;
            FactionVisualSystem = factionVisualSystem;
            BarrierSystem = barrierSystem;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
            FactionTintStrength = Mathf.Clamp01(factionTintStrength);
        }
    }

    public void InitializeBuildingVisuals(Context context, RuntimeBuildingEntity building)
    {
        if (building?.Instance == null || context.VisualSystem == null)
            return;

        Transform visualRoot = building.Instance.transform.childCount > 0
            ? building.Instance.transform.GetChild(0)
            : building.Instance.transform;

        building.DoorZ = context.VisualSystem.FindDescendantByName(visualRoot, "Door_Z");

        if (building.DoorZ != null)
        {
            building.DoorClosedLocalEulerZ = 0f;
            building.DoorOpenLocalEulerZ = NormalizeSignedAngle(building.DoorZ.localEulerAngles.z);
            building.DoorOpen01 = 0f;
            context.BarrierSystem?.SetBarrierDoorOpen01(building, 0f);
        }

        var aliveRoots = new List<Transform>();
        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);
            aliveRoots.Add(child);
        }

        building.AliveVisualRoots = aliveRoots.ToArray();
        building.AnimatedParts = context.VisualSystem.FindAnimatedBuildingParts(visualRoot);
        context.FactionVisualSystem?.CacheBuildingRenderers(building, visualRoot, null);
        context.FactionVisualSystem?.ApplyOwnerFaction(new BuildingFactionVisualSystem.Context(
            context.FactionVisualSettings,
            context.MarkerPropertyBlock,
            context.FactionTintStrength),
            building);
    }

    public void UpdateBuildingResourceVisuals(Context context, float time)
    {
        if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0 || context.VisualSystem == null)
            return;

        if (context.RuntimeBuildingMap != null)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildingMap)
                UpdateBuildingResourceVisual(context, entry.Value, time);
            return;
        }

        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
            UpdateBuildingResourceVisual(context, entry.Value, time);
    }

    private static void UpdateBuildingResourceVisual(Context context, RuntimeBuildingEntity building, float time)
    {
        if (building == null || building.IsDestroyed || building.AnimatedParts == null || building.AnimatedParts.Length == 0 || building.Definition == null)
            return;

        bool isProducingOil = building.Definition.OilStorageCapacity > 0 &&
                              building.Definition.OilBarrelsPerDay > 0f &&
                              building.StoredOilBarrels < building.Definition.OilStorageCapacity;
        bool isProducingFuel = building.Definition.FuelStorageCapacity > 0 &&
                               building.Definition.FuelBarrelsPerDay > 0f &&
                               building.StoredOilBarrels > 0f &&
                               building.StoredFuelBarrels < building.Definition.FuelStorageCapacity;
        context.VisualSystem.UpdateAnimatedBuildingParts(building.AnimatedParts, isProducingOil || isProducingFuel, time);
    }

    public void RefreshBuildingMarkerVisibility(Context context)
    {
        if (context.RuntimeBuildings == null || context.VisualSystem == null)
            return;

        if (context.RuntimeBuildingMap != null)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildingMap)
                RefreshBuildingMarkerVisibility(context, entry.Value);
            return;
        }

        foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
            RefreshBuildingMarkerVisibility(context, entry.Value);
    }

    private static void RefreshBuildingMarkerVisibility(Context context, RuntimeBuildingEntity building)
    {
        if (building == null)
            return;

        if (building.IsDestroyed)
            context.FactionVisualSystem?.Clear(new BuildingFactionVisualSystem.Context(
                context.FactionVisualSettings,
                context.MarkerPropertyBlock,
                context.FactionTintStrength),
                building);
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
