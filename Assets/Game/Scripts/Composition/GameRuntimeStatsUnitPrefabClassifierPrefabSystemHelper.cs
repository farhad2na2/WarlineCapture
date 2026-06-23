using UnityEngine;

internal static class GameRuntimeStatsUnitPrefabClassifierPrefabSystemHelper
{
    public static GameRuntimeStats.UnitOrderKind ClassifyUnitPrefab(GameObject prefab)
    {
        if (prefab == null)
            return GameRuntimeStats.UnitOrderKind.Soldier;

        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        string displayName = authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName)
            ? authoring.ConfiguredDisplayName
            : prefab.name;

        if (displayName.IndexOf("Ammo", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            prefab.name.IndexOf("Ammo", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return GameRuntimeStats.UnitOrderKind.Ammo;
        }

        if (authoring != null)
        {
            Vector2Int footprint = authoring.GetConfiguredFootprintCells();
            if (footprint.x > 1 || footprint.y > 1 || authoring.IsAirUnit)
                return GameRuntimeStats.UnitOrderKind.Vehicle;
        }

        if (prefab.name.IndexOf("Veh", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            prefab.name.IndexOf("Vehicle", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return GameRuntimeStats.UnitOrderKind.Vehicle;
        }

        return GameRuntimeStats.UnitOrderKind.Soldier;
    }
}
