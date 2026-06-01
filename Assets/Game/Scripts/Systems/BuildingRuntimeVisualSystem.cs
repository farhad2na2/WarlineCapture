using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingRuntimeVisualSystem
{
    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly BuildingVisualSystem VisualSystem;
        public readonly BuildingBarrierSystem BarrierSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            BuildingVisualSystem visualSystem,
            BuildingBarrierSystem barrierSystem,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock)
        {
            RuntimeBuildings = runtimeBuildings;
            VisualSystem = visualSystem;
            BarrierSystem = barrierSystem;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
        }
    }

    public void InitializeBuildingVisuals(Context context, RuntimeBuildingData building)
    {
        if (building?.Instance == null || context.VisualSystem == null)
            return;

        Transform visualRoot = building.Instance.transform.childCount > 0
            ? building.Instance.transform.GetChild(0)
            : building.Instance.transform;

        building.FactionMarker = context.VisualSystem.FindDescendantByName(visualRoot, "FactionMarker");
        building.DoorZ = context.VisualSystem.FindDescendantByName(visualRoot, "Door_Z");
        building.DestroyedVisual = context.VisualSystem.FindDescendantByName(visualRoot, "Destroyed");

        if (building.DoorZ != null)
        {
            building.DoorClosedLocalEulerZ = 0f;
            building.DoorOpenLocalEulerZ = NormalizeSignedAngle(building.DoorZ.localEulerAngles.z);
            building.DoorOpen01 = 0f;
            context.BarrierSystem?.SetBarrierDoorOpen01(building, 0f);
        }

        if (building.FactionMarker != null)
            building.FactionMarkerRenderers = building.FactionMarker.GetComponentsInChildren<Renderer>(true);

        var aliveRoots = new List<Transform>();
        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);
            if (child == building.DestroyedVisual ||
                child == building.FactionMarker)
            {
                continue;
            }

            aliveRoots.Add(child);
        }

        building.AliveVisualRoots = aliveRoots.ToArray();
        building.AnimatedParts = context.VisualSystem.FindAnimatedBuildingParts(visualRoot);

        Color factionColor = context.FactionVisualSettings != null
            ? context.FactionVisualSettings.GetColor(0)
            : new Color(0.12f, 0.72f, 1f, 1f);

        context.VisualSystem.ApplyMarkerColor(building.FactionMarkerRenderers, factionColor, context.MarkerPropertyBlock);
        context.VisualSystem.SetTransformVisible(building.DestroyedVisual, false);
    }

    public void UpdateBuildingResourceVisuals(Context context, float time)
    {
        if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0 || context.VisualSystem == null)
            return;

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null || building.IsDestroyed || building.AnimatedParts == null || building.AnimatedParts.Length == 0 || building.Definition == null)
                continue;

            bool isProducingOil = building.Definition.OilStorageCapacity > 0 &&
                                  building.Definition.OilBarrelsPerDay > 0f &&
                                  building.StoredOilBarrels < building.Definition.OilStorageCapacity;
            bool isProducingFuel = building.Definition.FuelStorageCapacity > 0 &&
                                   building.Definition.FuelBarrelsPerDay > 0f &&
                                   building.StoredOilBarrels > 0f &&
                                   building.StoredFuelBarrels < building.Definition.FuelStorageCapacity;
            context.VisualSystem.UpdateAnimatedBuildingParts(building.AnimatedParts, isProducingOil || isProducingFuel, time);
        }
    }

    public void RefreshBuildingMarkerVisibility(Context context)
    {
        if (context.RuntimeBuildings == null || context.VisualSystem == null)
            return;

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null)
                continue;

            if (building.IsDestroyed)
                context.VisualSystem.SetTransformVisible(building.FactionMarker, false);
        }
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
