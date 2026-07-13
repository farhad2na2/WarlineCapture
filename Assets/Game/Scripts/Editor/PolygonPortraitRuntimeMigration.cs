using System;
using System.Collections.Generic;
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class PolygonPortraitRuntimeMigration
    {
        private const string ConfigRoot = "Assets/Game/Configs/Prefabs";
        private const string MatchHudPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";

        private readonly struct AssetSpec
        {
            public readonly string Path;
            public readonly bool HasAlpha;

            public AssetSpec(string path, bool hasAlpha = false)
            {
                Path = path;
                HasAlpha = hasAlpha;
            }
        }

        private readonly struct ConfigRoleSpec
        {
            public readonly string ConfigPath;
            public readonly string PropertyName;
            public readonly string SpritePath;

            public ConfigRoleSpec(string configPath, string propertyName, string spritePath)
            {
                ConfigPath = configPath;
                PropertyName = propertyName;
                SpritePath = spritePath;
            }
        }

        private readonly struct PrefabPortraitSpec
        {
            public readonly string PropertyName;
            public readonly string SpritePath;

            public PrefabPortraitSpec(string propertyName, string spritePath)
            {
                PropertyName = propertyName;
                SpritePath = spritePath;
            }
        }

        private static readonly AssetSpec[] NewAssets =
        {
            new("Assets/Game/Art/UI/Portraits/Generated/Portrait_Building_Hall_Polygon.png", true),
            new("Assets/Game/Art/UI/Portraits/Generated/Portrait_Building_Helipad_Polygon.png", true),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Hall_Card_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Hall_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Helipad_Card_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Helipad_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Shop_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Contractor_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Expert_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Refugee_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Regular_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Wall_Dirt_Straight_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Wall_Fence_Straight_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_WaterTank_Action_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Aircraft_Polygon_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Transports_Polygon_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Buildings_Polygon_512.png"),
            new("Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedForce_Polygon_512.png")
        };

        private static readonly ConfigRoleSpec[] MissingConfigRoles =
        {
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Hall_Config.asset", "portraitSprite", "Assets/Game/Art/UI/Portraits/Generated/Portrait_Building_Hall_Polygon.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Hall_Config.asset", "portraitCardSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Hall_Card_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Hall_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Hall_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Helipad_Config.asset", "portraitSprite", "Assets/Game/Art/UI/Portraits/Generated/Portrait_Building_Helipad_Polygon.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Helipad_Config.asset", "portraitCardSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Helipad_Card_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Helipad_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Helipad_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Shop_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Shop_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Tent_Contractor_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Contractor_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Tent_Expert_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Expert_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Tent_Refugee_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Refugee_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Tent_Regular_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Tent_Regular_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Wall_Dirt_Straight_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Wall_Dirt_Straight_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Wall_Fence_Straight_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_Wall_Fence_Straight_Action_512.png"),
            new("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_WaterTank_Config.asset", "portraitActionSprite", "Assets/Game/Art/UI/Portraits/Secondary/Portrait_Building_WaterTank_Action_512.png")
        };

        private static readonly PrefabPortraitSpec[] MatchHudFallbacks =
        {
            new("genericSquadPortraitSprite", "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_selected_squad_group_portrait.png"),
            new("soldierSquadPortraitSprite", "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_squad_rifle_portrait.png"),
            new("vehicleSquadPortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_VehicleSquad_512.png"),
            new("aircraftSquadPortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Aircraft_Polygon_512.png"),
            new("transportSquadPortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Transports_Polygon_512.png"),
            new("buildingSquadPortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_Buildings_Polygon_512.png"),
            new("mixedForcePortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedForce_Polygon_512.png"),
            new("mixedSoldierVehiclePortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedSoldierVehicle_512.png"),
            new("mixedSoldierAircraftPortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedSoldierAircraft_512.png"),
            new("mixedVehicleAircraftPortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedVehicleAircraft_512.png"),
            new("mixedSoldierVehicleAircraftPortraitSprite", "Assets/Game/Art/UI/Portraits/Secondary/SelectionSummary_MixedSoldierVehicleAircraft_512.png")
        };

        [MenuItem("Game/UI/Apply Polygon Portrait Runtime Migration")]
        public static void Apply()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureNewImporters();
            AssignMissingConfigRoles();
            AssignMatchHudFallbacks();
            PortraitSpriteAtlasBuilder.RebuildPortraitSpriteAtlases();
            ValidateRuntimeAssignments();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[PolygonPortraitRuntimeMigration] Applied and validated 238 Polygon portrait assignments.");
        }

        private static void ConfigureNewImporters()
        {
            foreach (AssetSpec spec in NewAssets)
            {
                if (!File.Exists(spec.Path))
                    throw new FileNotFoundException($"Missing Polygon portrait runtime asset: {spec.Path}");

                AssetDatabase.ImportAsset(spec.Path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                if (AssetImporter.GetAtPath(spec.Path) is not TextureImporter importer)
                    throw new InvalidOperationException($"Expected TextureImporter for {spec.Path}");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = spec.HasAlpha;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
            }
        }

        private static void AssignMissingConfigRoles()
        {
            foreach (ConfigRoleSpec spec in MissingConfigRoles)
            {
                UnityEngine.Object config = AssetDatabase.LoadMainAssetAtPath(spec.ConfigPath);
                if (config == null)
                    throw new InvalidOperationException($"Missing config: {spec.ConfigPath}");

                Sprite sprite = LoadSprite(spec.SpritePath);
                var serializedConfig = new SerializedObject(config);
                SerializedProperty property = serializedConfig.FindProperty(spec.PropertyName);
                if (property == null)
                    throw new InvalidOperationException($"Missing property {spec.PropertyName} on {spec.ConfigPath}");
                property.objectReferenceValue = sprite;
                serializedConfig.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }
        }

        private static void AssignMatchHudFallbacks()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MatchHudPrefabPath);
            try
            {
                MatchHudSelectionPanelView view = root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
                if (view == null)
                    throw new InvalidOperationException("SCN08 Match HUD prefab has no MatchHudSelectionPanelView.");

                var serializedView = new SerializedObject(view);
                foreach (PrefabPortraitSpec spec in MatchHudFallbacks)
                {
                    SerializedProperty property = serializedView.FindProperty(spec.PropertyName);
                    if (property == null)
                        throw new InvalidOperationException($"Missing Match HUD property {spec.PropertyName}");
                    property.objectReferenceValue = LoadSprite(spec.SpritePath);
                }

                serializedView.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, MatchHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateRuntimeAssignments()
        {
            string[] roleProperties = { "portraitSprite", "portraitCardSprite", "portraitActionSprite" };
            int configCount = 0;
            int roleCount = 0;
            foreach (string configPath in EnumeratePortraitConfigPaths())
            {
                UnityEngine.Object config = AssetDatabase.LoadMainAssetAtPath(configPath);
                if (config == null)
                    throw new InvalidOperationException($"Could not load portrait config {configPath}");

                configCount++;
                var serializedConfig = new SerializedObject(config);
                foreach (string propertyName in roleProperties)
                {
                    SerializedProperty property = serializedConfig.FindProperty(propertyName);
                    if (property?.objectReferenceValue is not Sprite sprite)
                        throw new InvalidOperationException($"{configPath} has no assigned {propertyName}");
                    ValidateSprite(sprite, $"{configPath}:{propertyName}");
                    roleCount++;
                }
            }

            if (configCount != 74 || roleCount != 222)
                throw new InvalidOperationException($"Expected 74 configs and 222 roles, validated {configCount} configs and {roleCount} roles.");

            GameObject root = PrefabUtility.LoadPrefabContents(MatchHudPrefabPath);
            try
            {
                MatchHudSelectionPanelView view = root.GetComponentInChildren<MatchHudSelectionPanelView>(true);
                var serializedView = new SerializedObject(view);
                foreach (PrefabPortraitSpec spec in MatchHudFallbacks)
                {
                    SerializedProperty property = serializedView.FindProperty(spec.PropertyName);
                    if (property?.objectReferenceValue is not Sprite sprite)
                        throw new InvalidOperationException($"Match HUD has no assigned {spec.PropertyName}");
                    if (!string.Equals(AssetDatabase.GetAssetPath(sprite), spec.SpritePath, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Match HUD {spec.PropertyName} resolves to the wrong sprite.");
                    ValidateSprite(sprite, $"Match HUD:{spec.PropertyName}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static IEnumerable<string> EnumeratePortraitConfigPaths()
        {
            foreach (string absolutePath in Directory.GetFiles(ConfigRoot, "*.asset", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(absolutePath);
                bool isCharacter = fileName.StartsWith("Prefab_UnitGrid_Chr_", StringComparison.Ordinal) && fileName.EndsWith("_Config.asset", StringComparison.Ordinal);
                bool isVehicle = fileName.StartsWith("Prefab_UnitGrid_Veh_", StringComparison.Ordinal);
                bool isBuilding = fileName.StartsWith("Prefab_BuildingDefinition_", StringComparison.Ordinal) && fileName.EndsWith("_Config.asset", StringComparison.Ordinal);
                if (isCharacter || isVehicle || isBuilding)
                    yield return absolutePath.Replace('\\', '/');
            }
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Could not load Sprite at {path}");
            ValidateSprite(sprite, path);
            return sprite;
        }

        private static void ValidateSprite(Sprite sprite, string context)
        {
            if (sprite.texture == null || sprite.texture.width != 512 || sprite.texture.height != 512)
                throw new InvalidOperationException($"{context} must resolve to a 512x512 texture.");
        }
    }
}
