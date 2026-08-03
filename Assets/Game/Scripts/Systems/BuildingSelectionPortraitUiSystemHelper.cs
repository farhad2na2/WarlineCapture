using System;
using UnityEngine;

namespace Game.Runtime
{
    internal static class BuildingSelectionPortraitUiSystemHelper
    {
        public static Sprite Resolve(
            RuntimeBuildingEntity building,
            Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab)
        {
            if (building == null)
                return null;

            if (building.Definition?.SelectionPortraitSprite != null)
                return building.Definition.SelectionPortraitSprite;

            Sprite sprite = resolveSelectionPortraitSpriteFromPrefab?.Invoke(building.Definition?.Prefab);
            return sprite != null
                ? sprite
                : resolveSelectionPortraitSpriteFromPrefab?.Invoke(building.Instance);
        }
    }
}
