using System.IO;
using UnityEditor;
using UnityEngine;

public static class VehicleVisualAdornmentsMigration
{
    private const string VehiclePrefabDirectory = "Assets/Game/Prefabs/Vehicles";
    private const string UnitVehiclePrefabPath = VehiclePrefabDirectory + "/Unit_Veh.prefab";
    private const string CharacterUnitPrefabPath = "Assets/Game/Prefabs/Characters/Unit.prefab";
    private const string VehicleSelectionMarkerPrefabPath = VehiclePrefabDirectory + "/VehicleSelectionMarker.prefab";
    private const string VehicleHealthBarPrefabPath = VehiclePrefabDirectory + "/VehicleHealthBar.prefab";
    private const string DestroyedVisualDirectory = VehiclePrefabDirectory + "/DestroyedVisuals";

    [MenuItem("Game/Migrations/Apply Vehicle Visual Adornments Refactor")]
    public static void Run()
    {
        Directory.CreateDirectory(DestroyedVisualDirectory);

        GameObject selectionMarkerPrefab = CreateSharedChildPrefab(
            "SelectionMarker",
            VehicleSelectionMarkerPrefabPath,
            removeSelectionMarkerAuthoring: true);
        GameObject healthBarPrefab = CreateSharedChildPrefab(
            "HealthBar",
            VehicleHealthBarPrefabPath,
            removeSelectionMarkerAuthoring: false);

        RemoveInheritedChildVisualsFromVehicleBase();
        MigrateVehicleVariants(selectionMarkerPrefab, healthBarPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VehicleVisualAdornmentsMigration] completed.");
    }

    private static GameObject CreateSharedChildPrefab(string childName, string outputPath, bool removeSelectionMarkerAuthoring)
    {
        GameObject unitRoot = PrefabUtility.LoadPrefabContents(CharacterUnitPrefabPath);
        try
        {
            Transform child = unitRoot.transform.Find(childName);
            if (child == null)
            {
                Debug.LogError($"[VehicleVisualAdornmentsMigration] missing {childName} in {CharacterUnitPrefabPath}");
                return null;
            }

            GameObject copy = Object.Instantiate(child.gameObject);
            copy.name = childName == "SelectionMarker" ? "VehicleSelectionMarker" : "VehicleHealthBar";
            if (removeSelectionMarkerAuthoring)
            {
                SelectionMarkerAuthoring authoring = copy.GetComponent<SelectionMarkerAuthoring>();
                if (authoring != null)
                    Object.DestroyImmediate(authoring, true);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, outputPath);
            Object.DestroyImmediate(copy);
            return prefab;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(unitRoot);
        }
    }

    private static void RemoveInheritedChildVisualsFromVehicleBase()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UnitVehiclePrefabPath);
        try
        {
            RemoveChildIfExists(root.transform, "SelectionMarker");
            RemoveChildIfExists(root.transform, "FactionMarker");
            RemoveChildIfExists(root.transform, "HealthBar");
            PrefabUtility.SaveAsPrefabAsset(root, UnitVehiclePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void MigrateVehicleVariants(GameObject selectionMarkerPrefab, GameObject healthBarPrefab)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { VehiclePrefabDirectory });
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (prefabPath == VehicleSelectionMarkerPrefabPath ||
                prefabPath == VehicleHealthBarPrefabPath ||
                prefabPath.StartsWith(DestroyedVisualDirectory + "/", System.StringComparison.Ordinal))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                UnitGridAuthoring unit = root.GetComponent<UnitGridAuthoring>();
                if (unit == null)
                    continue;

                GameObject destroyedVisualPrefab = ExtractDestroyedVisualPrefab(root, prefabPath);
                AssignVehicleVisualConfig(unit, destroyedVisualPrefab, selectionMarkerPrefab, healthBarPrefab);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static GameObject ExtractDestroyedVisualPrefab(GameObject root, string sourcePrefabPath)
    {
        Transform destroyed = root.transform.Find("Destroyed");
        if (destroyed == null)
            return null;

        string sourceName = Path.GetFileNameWithoutExtension(sourcePrefabPath);
        string outputPath = $"{DestroyedVisualDirectory}/{sourceName}_Destroyed.prefab";
        GameObject copy = Object.Instantiate(destroyed.gameObject);
        copy.name = $"{sourceName}_Destroyed";
        GameObject destroyedPrefab = PrefabUtility.SaveAsPrefabAsset(copy, outputPath);
        Object.DestroyImmediate(copy);
        Object.DestroyImmediate(destroyed.gameObject, true);
        return destroyedPrefab;
    }

    private static void AssignVehicleVisualConfig(
        UnitGridAuthoring unit,
        GameObject destroyedVisualPrefab,
        GameObject selectionMarkerPrefab,
        GameObject healthBarPrefab)
    {
        var unitObject = new SerializedObject(unit);
        SerializedProperty configProperty = unitObject.FindProperty("config");
        Object config = configProperty != null ? configProperty.objectReferenceValue : null;
        if (config == null)
            return;

        var configObject = new SerializedObject(config);
        SetObjectReference(configObject, "vehicleDestroyedVisualPrefab", destroyedVisualPrefab);
        SetObjectReference(configObject, "vehicleSelectionMarkerPrefab", selectionMarkerPrefab);
        SetObjectReference(configObject, "vehicleHealthBarPrefab", healthBarPrefab);
        SerializedProperty tintProperty = configObject.FindProperty("tintVehicleModelRenderers");
        if (tintProperty != null)
            tintProperty.boolValue = true;
        configObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void RemoveChildIfExists(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        if (child != null)
            Object.DestroyImmediate(child.gameObject, true);
    }
}
