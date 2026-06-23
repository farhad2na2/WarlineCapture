using UnityEngine;

internal static class BuildingSpawnPrefabLookupKeyPrefabSystemHelper
{
    public static string ResolveSpawnableLookupKey(GameObject prefab)
    {
        if (prefab == null)
            return string.Empty;

        BuildingDefinitionAuthoring authoring = prefab.GetComponent<BuildingDefinitionAuthoring>();
        if (authoring != null && !string.IsNullOrWhiteSpace(authoring.ConfiguredDisplayName))
            return authoring.ConfiguredDisplayName;

        return prefab.name;
    }
}
