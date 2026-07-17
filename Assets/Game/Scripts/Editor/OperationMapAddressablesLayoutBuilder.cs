using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    public static class OperationMapAddressablesLayoutBuilder
    {
        public const string CatalogGroupName = "Operation Maps - Catalog";
        public const string SharedGroupName = "Operation Maps - Shared";
        public const string CoreGroupName =
            "Operation Map - Local - skirmish-desert-base-01 - Core";
        public const string PresentationGroupName =
            "Operation Map - Local - skirmish-desert-base-01 - Presentation";
        public const string CatalogPath =
            "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset";
        public const string DefinitionPath =
            "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";

        [MenuItem("Game/Operation Maps/Configure Local Addressables Groups")]
        public static void Run()
        {
            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
                throw new InvalidOperationException("Addressables settings are required.");

            AddressableAssetGroup catalog = EnsureGroup(settings, CatalogGroupName, false);
            EnsureGroup(settings, SharedGroupName, false);
            EnsureGroup(settings, CoreGroupName, false);
            EnsureGroup(settings, PresentationGroupName, true);

            MoveEntry(settings, catalog, CatalogPath, "operation-map/catalog");
            MoveEntry(
                settings,
                catalog,
                DefinitionPath,
                "operation-map/opmap.skirmish.desert_base_01/definition");

            AssetDatabase.SaveAssets();
            Debug.Log("[OperationMapAddressablesLayoutBuilder] Configured one-map local group topology.");
        }

        private static AddressableAssetGroup EnsureGroup(
            AddressableAssetSettings settings,
            string groupName,
            bool packTogetherByLabel)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName) ?? settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
            BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
                throw new InvalidOperationException($"Addressables group '{groupName}' requires a bundled schema.");

            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.UseDefaultSchemaSettings = false;
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            schema.UseAssetBundleCrc = true;
            schema.UseAssetBundleCrcForCachedBundles = true;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            schema.BundleMode = packTogetherByLabel
                ? BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel
                : BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            return group;
        }

        private static void MoveEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Addressable asset is missing: {assetPath}");

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null || entry.parentGroup != group)
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (!string.Equals(entry.address, address, StringComparison.Ordinal))
                entry.SetAddress(address, false);
        }
    }
}
