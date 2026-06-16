using System;
using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingSelectionPortraitSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static Sprite Resolve(
        RuntimeBuildingEntity building,
        Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab)
    {
        if (building == null)
            return null;

        Sprite sprite = resolveSelectionPortraitSpriteFromPrefab?.Invoke(building.Definition?.Prefab);
        return sprite != null
            ? sprite
            : resolveSelectionPortraitSpriteFromPrefab?.Invoke(building.Instance);
    }
}
