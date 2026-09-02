#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class V3LegacyUiSpriteMigrationBuilder
    {
        private const string UiPrefabRoot = "Assets/Game/Prefabs/UI";
        private const string AtlasRoot = "Assets/Game/Art/UI/V3Shared/Atlases";
        private const string SyntySpriteRoot = "Assets/Synty/InterfaceMilitaryCombatHUD/Sprites/";

        private static readonly Dictionary<string, string> Replacements = new(StringComparer.Ordinal)
        {
            [SyntySpriteRoot + "HUD/SPR_HUD_MilitaryCombat_Map_Ring_Small_01_Clean.png"] = V3UiFoundationBuilder.FirstLaunchGlobeRingPath,
            [SyntySpriteRoot + "Icons_Inventory/ICON_MilitaryCombat_Inventory_Ammo_Bullets_01_Clean.png"] = V3UiFoundationBuilder.OperationsArmoryIconPath,
            [SyntySpriteRoot + "Icons_Inventory/ICON_MilitaryCombat_Inventory_Notes_01_Clean.png"] = V3UiFoundationBuilder.OperationsIntelIconPath,
            [SyntySpriteRoot + "Icons_Inventory/ICON_MilitaryCombat_Inventory_Repair_01_Clean.png"] = V3UiFoundationBuilder.OperationsRepairIconPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Danger_01_Clean.png"] = V3UiFoundationBuilder.OperationsWarningIconPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Flag_01_Clean.png"] = V3UiFoundationBuilder.OperationsMapPinIconPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Lock_01_Clean.png"] = V3UiFoundationBuilder.EquipmentLockIconPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Objective_01_Clean.png"] = V3UiFoundationBuilder.FirstLaunchMapIconPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Pin_01_Clean.png"] = V3UiFoundationBuilder.OperationsMapPinIconPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Pin_01_Underlay.png"] = V3UiFoundationBuilder.OperationsMapPinUnderlayPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Skull_01_Clean.png"] = V3UiFoundationBuilder.MissionEnemyIconPath,
            [SyntySpriteRoot + "Icons_Map/ICON_MilitaryCombat_Map_Tank_01_Clean.png"] = V3UiFoundationBuilder.OperationsTankIconPath,
            [SyntySpriteRoot + "Icons_Resources/ICON_SM_Chr_Attach_Radio_01_Military.png"] = V3UiFoundationBuilder.MissionRadioIconPath,
            [SyntySpriteRoot + "Icons_Resources/ICON_SM_Item_Binoculars_01_Military.png"] = V3UiFoundationBuilder.OperationsPatrolIconPath,
            [SyntySpriteRoot + "Icons_Resources/ICON_SM_Prop_MedicalBox_02_BattleRoyale.png"] = V3UiFoundationBuilder.OperationsAidIconPath,
            [SyntySpriteRoot + "Icons_Special/ICON_MilitaryCombat_Special_Drone_01_Clean.png"] = V3UiFoundationBuilder.OperationsDroneIconPath,
            [SyntySpriteRoot + "Icons_Status/ICON_MilitaryCombat_Status_Burning_01_Clean.png"] = V3UiFoundationBuilder.OperationsHeatIconPath
        };

        private static readonly HashSet<string> RemovedDecorativeSprites = new(StringComparer.Ordinal)
        {
            SyntySpriteRoot + "HUD/SPR_HUD_MilitaryCombat_Gradient_Horizontal_01.png",
            SyntySpriteRoot + "HUD/SPR_HUD_MilitaryCombat_Shadow_Octagon_01.png",
            SyntySpriteRoot + "HUD/SPR_HUD_MilitaryCombat_Vignette_Box_Small01.png"
        };

        [MenuItem("Game/UI/V3/Migrate Legacy UI Sprites")]
        public static void Build()
        {
            V3UiFoundationBuilder.Build();
            int prefabs = 0;
            int replacements = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                try
                {
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null)
                            continue;
                        string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                        if (Replacements.TryGetValue(spritePath, out string replacementPath))
                        {
                            image.sprite = RequireSprite(replacementPath);
                            changed = true;
                            replacements++;
                        }
                        else if (RemovedDecorativeSprites.Contains(spritePath))
                        {
                            image.sprite = null;
                            image.color = Color.clear;
                            image.raycastTarget = false;
                            image.enabled = false;
                            changed = true;
                            replacements++;
                        }
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabs++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log($"[V3LegacyUiSpriteMigrationBuilder] result=Passed prefabs={prefabs} replacements={replacements} legacySprites=0 placeholderSprites=0 atlasPackableDuplicates=0");
        }

        [MenuItem("Game/UI/V3/Validate No Legacy Or Placeholder UI Sprites")]
        public static void Validate()
        {
            V3UiFoundationBuilder.Validate();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UiPrefabRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null)
                            continue;
                        string path = AssetDatabase.GetAssetPath(image.sprite);
                        string lower = path.ToLowerInvariant();
                        if (path.StartsWith(SyntySpriteRoot, StringComparison.Ordinal) ||
                            lower.Contains("placeholder") || lower.Contains("/legacy/"))
                        {
                            throw new InvalidOperationException(
                                $"{prefabPath} still references legacy or placeholder UI sprite {path} at {GetPath(image.transform, root.transform)}.");
                        }
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            ValidateAtlasPackablesAreUnique();
        }

        private static void ValidateAtlasPackablesAreUnique()
        {
            var ownerByPackable = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { AtlasRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string atlasPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                foreach (UnityEngine.Object packable in SpriteAtlasExtensions.GetPackables(atlas))
                {
                    string packablePath = AssetDatabase.GetAssetPath(packable);
                    if (ownerByPackable.TryGetValue(packablePath, out string owner))
                        throw new InvalidOperationException($"V3 atlas packable is duplicated: {packablePath} appears in {owner} and {atlasPath}.");
                    ownerByPackable.Add(packablePath, atlasPath);
                }
            }
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new FileNotFoundException($"Missing V3 replacement sprite: {path}");
            return sprite;
        }

        private static string GetPath(Transform current, Transform root)
        {
            string path = current.name;
            while (current.parent != null && current != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            return path;
        }
    }
}
#endif
