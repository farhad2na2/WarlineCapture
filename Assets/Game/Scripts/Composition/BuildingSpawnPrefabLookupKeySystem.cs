using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingSpawnPrefabLookupKeySystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

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
