using System;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M02EstablishBaseConfigBuilder
    {
        public const string BarracksConfigPath =
            "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset";
        public const string RequiredRiflePrefabPath =
            "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab";

        [MenuItem("Game/Campaign/M02/Configure Barracks Production")]
        public static void ConfigureBarracksProductionMenu() => ConfigureBarracksProduction();

        public static void ConfigureBarracksProduction()
        {
            BuildingDefinitionAuthoringConfig barracks =
                AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(BarracksConfigPath);
            GameObject riflePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RequiredRiflePrefabPath);
            if (barracks == null || riflePrefab == null)
            {
                throw new InvalidOperationException(
                    $"M02 requires the canonical Barracks config and rifle prefab: " +
                    $"'{BarracksConfigPath}', '{RequiredRiflePrefabPath}'.");
            }

            SerializedObject serialized = new(barracks);
            SerializedProperty productions = serialized.FindProperty("productions");
            productions.arraySize = 1;
            productions.GetArrayElementAtIndex(0)
                .FindPropertyRelative("spawnUnitPrefab")
                .objectReferenceValue = riflePrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(barracks);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[M02EstablishBaseConfigBuilder] result=Passed " +
                "scope=BarracksProduction entries=1 unit=Unit_Chr_Soldier_Male_02_Alt_04");
        }
    }
}
