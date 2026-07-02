using UnityEngine;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal static class SelectionPortraitSpriteResolverUiSystemHelper
    {
        public static Sprite ResolveSelectionPortraitSprite(GameObject prefab)
        {
            if (prefab == null)
                return null;

            if (prefab.TryGetComponent(out UnitGridAuthoring unitAuthoring))
                return unitAuthoring.PortraitActionSprite != null
                    ? unitAuthoring.PortraitActionSprite
                    : unitAuthoring.PortraitCardSprite;

            if (prefab.TryGetComponent(out BuildingDefinitionAuthoring buildingAuthoring))
                return buildingAuthoring.ConfiguredPortraitActionSprite != null
                    ? buildingAuthoring.ConfiguredPortraitActionSprite
                    : buildingAuthoring.ConfiguredPortraitCardSprite;

            return null;
        }

        public static Sprite ResolveSelectionCardPortraitSprite(GameObject prefab)
        {
            if (prefab == null)
                return null;

            if (prefab.TryGetComponent(out UnitGridAuthoring unitAuthoring))
                return unitAuthoring.PortraitCardSprite != null
                    ? unitAuthoring.PortraitCardSprite
                    : unitAuthoring.PortraitSprite;

            if (prefab.TryGetComponent(out BuildingDefinitionAuthoring buildingAuthoring))
                return buildingAuthoring.ConfiguredPortraitCardSprite != null
                    ? buildingAuthoring.ConfiguredPortraitCardSprite
                    : buildingAuthoring.ConfiguredPortraitSprite;

            return null;
        }
    }
}
