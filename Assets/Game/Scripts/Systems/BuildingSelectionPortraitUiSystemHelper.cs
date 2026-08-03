using System;
using System.Collections.Generic;
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

        public static Sprite ResolveSelected(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            int? selectedBuildingId,
            Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab)
        {
            if (selectedBuildingId.HasValue &&
                runtimeBuildings != null &&
                runtimeBuildings.TryGetValue(selectedBuildingId.Value, out RuntimeBuildingEntity building))
            {
                return Resolve(building, resolveSelectionPortraitSpriteFromPrefab);
            }

            return null;
        }
    }
}
