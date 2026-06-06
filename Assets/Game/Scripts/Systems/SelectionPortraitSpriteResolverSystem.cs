using UnityEngine;

internal static class SelectionPortraitSpriteResolverSystem
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

    public static Sprite ResolveSelectionPortraitSprite(RuntimeBuildingData building)
    {
        if (building == null)
            return null;

        Sprite sprite = ResolveSelectionPortraitSprite(building.Definition?.Prefab);
        return sprite != null
            ? sprite
            : ResolveSelectionPortraitSprite(building.Instance);
    }
}
