using System;
using UnityEngine;

internal static class BuildingSelectionPortraitUiSystemHelper
{
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
