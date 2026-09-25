#if UNITY_EDITOR
using System.IO;
using Game.Configs;
using Game.Authoring;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishGroundStagingPrefabBuilder
    {
        public const string PrefabPath = SkirmishGroundStagingPrefabAccess.PrefabPath;

        [MenuItem("Tools/Warline/Skirmish/Rebuild Ground Staging")]
        public static void RebuildMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            Directory.CreateDirectory("Assets/Game/Prefabs/Skirmish");
            PersistAuthoredConfig();
            GameObject hierarchy = SkirmishGroundStagingBuilder.BuildHierarchy(
                SkirmishGroundStagingPrefabAccess.LoadAuthoredConfig());
            hierarchy.hideFlags = HideFlags.None;
            hierarchy.SetActive(true);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(hierarchy, PrefabPath);
            Object.DestroyImmediate(hierarchy);
            if (prefab == null || !SkirmishGroundStagingBuilder.HasRequiredSurfaces(prefab))
                return "[SkirmishGroundStagingPrefabBuilder] result=Failed";
            AssetDatabase.ImportAsset(PrefabPath);
            BindSharedRuntimeDefinition();
            return "[SkirmishGroundStagingPrefabBuilder] result=Passed path=" + PrefabPath;
        }

        public static void BindSharedRuntimeDefinition()
        {
            const string configPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_GroundStaging_Config.asset";
            var config = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringPrefabConfigAsset>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BuildingDefinitionAuthoringPrefabConfigAsset>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            var serialized = new SerializedObject(config);
            serialized.FindProperty("displayName").stringValue = "Ground Staging";
            serialized.FindProperty("description").stringValue = "Vehicle and logistics staging area.";
            serialized.FindProperty("maxHealth").intValue = SkirmishRoleOverlayCatalog.GroundStagingStructure().MaxHealth;
            serialized.FindProperty("canRequest").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var authoring = root.GetComponent<BuildingDefinitionAuthoring>() ?? root.AddComponent<BuildingDefinitionAuthoring>();
                authoring.ConfigureForEditor(config);
                var authoringSerialized = new SerializedObject(authoring);
                authoringSerialized.FindProperty("footprintCells").vector2IntValue = new Vector2Int(20, 14);
                authoringSerialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            if (preset == null || preset.buildingPlacement == null)
                throw new System.InvalidOperationException("Desert Base placement configuration is missing.");
            if (!preset.buildingPlacement.Spawnables.Contains(prefab))
                preset.buildingPlacement.Spawnables.Add(prefab);
            EditorUtility.SetDirty(preset.buildingPlacement);
            AssetDatabase.SaveAssets();
            Debug.Log("[SkirmishGroundStagingSharedDefinition] result=Passed");
        }

        private static void PersistAuthoredConfig()
        {
            SkirmishGroundStagingConfig config =
                AssetDatabase.LoadAssetAtPath<SkirmishGroundStagingConfig>(
                    SkirmishGroundStagingPrefabAccess.AuthoredConfigPath);
            if (config != null)
                return;
            config = ScriptableObject.CreateInstance<SkirmishGroundStagingConfig>();
            config.ConfigureEstablished();
            AssetDatabase.CreateAsset(config, SkirmishGroundStagingPrefabAccess.AuthoredConfigPath);
        }
    }
}
#endif
