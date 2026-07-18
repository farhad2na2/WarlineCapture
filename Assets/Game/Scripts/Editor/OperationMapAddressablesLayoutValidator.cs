using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.Rendering;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Game.Editor
{
    public static class OperationMapAddressablesLayoutValidator
    {
        public const string MinimapRasterAddress =
            OperationMapAddressablesLayoutBuilder.AddressPrefix + "minimap-raster";

        public static bool TryValidateCurrentLayout(
            bool requireMinimapRaster,
            out string error)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return Fail("Addressables settings are missing.", out error);
            if (settings.BuildRemoteCatalog || settings.UniqueBundleIds)
                return Fail("The local milestone forbids a remote catalog and unique bundle ids.", out error);

            if (!TryRequireGroup(settings, OperationMapAddressablesLayoutBuilder.CatalogGroupName, 2, false, out AddressableAssetGroup catalog, out error) ||
                !TryRequireGroup(settings, OperationMapAddressablesLayoutBuilder.SharedGroupName, -1, true, out AddressableAssetGroup shared, out error) ||
                !TryRequireGroup(settings, OperationMapAddressablesLayoutBuilder.CoreGroupName, 6, false, out AddressableAssetGroup core, out error) ||
                !TryRequireGroup(settings, OperationMapAddressablesLayoutBuilder.PresentationGroupName, 514, true, out AddressableAssetGroup presentation, out error))
                return false;

            HashSet<AddressableAssetGroup> ownedGroups = new() { catalog, shared, core, presentation };
            HashSet<string> ownedAddresses = new(StringComparer.Ordinal);
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (!entry.address.StartsWith("operation-map/", StringComparison.Ordinal))
                        continue;
                    if (!ownedGroups.Contains(group))
                        return Fail($"Operation-map entry is outside an owned group: {entry.address}.", out error);
                    if (!ownedAddresses.Add(entry.address))
                        return Fail($"Duplicate operation-map address: {entry.address}.", out error);
                }
            }

            OperationMapCatalogConfig catalogConfig =
                AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(OperationMapAddressablesLayoutBuilder.CatalogPath);
            if (catalogConfig == null || !catalogConfig.TryValidate(out error) ||
                catalogConfig.Entries.Length != 1 ||
                catalogConfig.Entries[0].ContentPack.DeliveryKind != OperationMapDeliveryKind.BuiltInLocal)
                return Fail(error ?? "Exactly one valid BuiltInLocal catalog entry is required.", out error);

            if (!TryRequireEntry(settings, catalog, OperationMapAddressablesLayoutBuilder.CatalogPath, "operation-map/catalog", OperationMapAddressablesLayoutBuilder.MetadataRoleLabel, false, out error) ||
                !TryRequireEntry(settings, catalog, OperationMapAddressablesLayoutBuilder.DefinitionPath, OperationMapAddressablesLayoutBuilder.AddressPrefix + "definition", OperationMapAddressablesLayoutBuilder.DefinitionRoleLabel, false, out error) ||
                !TryRequireEntry(settings, core, OperationMapAddressablesLayoutBuilder.SourceScenePath, OperationMapAddressablesLayoutBuilder.AddressPrefix + "source-scene", OperationMapAddressablesLayoutBuilder.SourceSceneRoleLabel, false, out error) ||
                !TryRequireEntry(settings, core, OperationMapAddressablesLayoutBuilder.MapSurfacePath, OperationMapAddressablesLayoutBuilder.AddressPrefix + "map-surface", OperationMapAddressablesLayoutBuilder.MetadataRoleLabel, false, out error) ||
                !TryRequireEntry(settings, core, OperationMapAddressablesLayoutBuilder.ManifestPath, OperationMapAddressablesLayoutBuilder.AddressPrefix + "static-manifest", OperationMapAddressablesLayoutBuilder.MetadataRoleLabel, false, out error) ||
                !TryRequireEntry(settings, core, OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath, OperationMapAddressablesLayoutBuilder.AddressPrefix + "building-placements", OperationMapAddressablesLayoutBuilder.MetadataRoleLabel, false, out error) ||
                !TryRequireEntry(settings, core, OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath, OperationMapAddressablesLayoutBuilder.AddressPrefix + "vehicle-placements", OperationMapAddressablesLayoutBuilder.MetadataRoleLabel, false, out error) ||
                !TryRequireEntry(settings, core, OperationMapAddressablesLayoutBuilder.MinimapRasterPath, MinimapRasterAddress, OperationMapAddressablesLayoutBuilder.MinimapRasterRoleLabel, false, out error))
                return false;

            if (requireMinimapRaster && !ContainsAddress(settings, MinimapRasterAddress))
                return Fail($"Missing required address: {MinimapRasterAddress}.", out error);

            OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
                OperationMapAddressablesLayoutBuilder.MapSurfacePath);
            Texture2D minimapRaster = AssetDatabase.LoadAssetAtPath<Texture2D>(
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath);
            TextureImporter minimapImporter = AssetImporter.GetAtPath(
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath) as TextureImporter;
            TextureImporterPlatformSettings androidImporter =
                minimapImporter?.GetPlatformTextureSettings("Android");
            string minimapGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath);
            if (definition == null || surface == null || minimapRaster == null || minimapImporter == null ||
                minimapRaster.width != OperationMapMinimapRasterBaker.Resolution ||
                minimapRaster.height != OperationMapMinimapRasterBaker.Resolution ||
                minimapImporter.mipmapEnabled ||
                minimapImporter.isReadable ||
                androidImporter == null ||
                !androidImporter.overridden ||
                androidImporter.maxTextureSize != OperationMapMinimapRasterBaker.Resolution ||
                androidImporter.format != TextureImporterFormat.ASTC_6x6 ||
                definition.MinimapRasterReference == null ||
                !string.Equals(definition.MinimapRasterReference.AssetGUID, minimapGuid, StringComparison.Ordinal) ||
                !string.Equals(
                    minimapImporter.userData,
                    OperationMapMinimapRasterBaker.BuildImporterUserData(definition, surface),
                    StringComparison.Ordinal))
            {
                return Fail("Operation-map minimap raster asset, source identity, or definition reference is stale.", out error);
            }

            if (!definition.TryValidateLocalContentReferences(out error))
                return false;

            string subSceneGuid = AssetDatabase.AssetPathToGUID(
                "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity");
            if (settings.FindAssetEntry(subSceneGuid) != null)
                return Fail("The Entities subscene must remain a source-scene dependency, not a direct entry.", out error);

            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(OperationMapAddressablesLayoutBuilder.ManifestPath);
            if (manifest == null || manifest.Chunks.Count != presentation.entries.Count)
                return Fail("Presentation entries must exactly match the current manifest.", out error);

            string[] sharedDependencies =
                OperationMapAddressablesLayoutBuilder.CollectSharedDependencyPaths(settings, manifest);
            if (shared.entries.Count != sharedDependencies.Length)
                return Fail("Shared dependency membership drifted from measured partition reuse.", out error);
            for (int index = 0; index < sharedDependencies.Length; index++)
            {
                string path = sharedDependencies[index];
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!TryRequireEntry(
                        settings,
                        shared,
                        path,
                        "operation-map/shared/" + guid,
                        OperationMapAddressablesLayoutBuilder.SharedDependencyRoleLabel,
                        false,
                        out error))
                    return false;

                AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                string expectedShard = OperationMapAddressablesLayoutBuilder.BuildSharedShardLabel(path, guid);
                int shardLabelCount = entry.labels.Count(label =>
                    label.StartsWith(OperationMapAddressablesLayoutBuilder.SharedShardLabelPrefix, StringComparison.Ordinal));
                if (shardLabelCount != 1 || !entry.labels.Contains(expectedShard))
                    return Fail($"Shared dependency shard drifted: {path}.", out error);
            }

            Dictionary<string, int> partitions = new(StringComparer.Ordinal);
            for (int index = 0; index < manifest.Chunks.Count; index++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[index];
                if (!TryRequireEntry(
                        settings,
                        presentation,
                        chunk.ScenePath,
                        OperationMapAddressablesLayoutBuilder.AddressPrefix + "presentation/" + chunk.ChunkId,
                        OperationMapAddressablesLayoutBuilder.PresentationRoleLabel,
                        true,
                        out error))
                    return false;

                AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(chunk.ScenePath));
                foreach (string label in entry.labels)
                {
                    if (!label.StartsWith("operation-map-partition-", StringComparison.Ordinal))
                        continue;
                    partitions.TryGetValue(label, out int count);
                    partitions[label] = count + 1;
                }
            }

            foreach (KeyValuePair<string, int> partition in partitions)
            {
                if (partition.Value < 1 || partition.Value > 25)
                    return Fail($"Partition '{partition.Key}' has invalid chunk count {partition.Value}.", out error);
            }

            error = null;
            return true;
        }

        private static bool TryRequireGroup(
            AddressableAssetSettings settings,
            string groupName,
            int entryCount,
            bool packTogetherByLabel,
            out AddressableAssetGroup group,
            out string error)
        {
            group = settings.FindGroup(groupName);
            if (group == null || (entryCount >= 0 && group.entries.Count != entryCount))
                return Fail($"Group '{groupName}' must contain exactly {entryCount} entries.", out error);
            BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null ||
                schema.BuildPath.GetName(settings) != AddressableAssetSettings.kLocalBuildPath ||
                schema.LoadPath.GetName(settings) != AddressableAssetSettings.kLocalLoadPath ||
                schema.Compression != BundledAssetGroupSchema.BundleCompressionMode.LZ4 ||
                !schema.UseAssetBundleCrc ||
                !schema.UseAssetBundleCrcForCachedBundles ||
                schema.BundleNaming != BundledAssetGroupSchema.BundleNamingStyle.OnlyHash ||
                schema.BundleMode != (packTogetherByLabel
                    ? BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel
                    : BundledAssetGroupSchema.BundlePackingMode.PackTogether))
                return Fail($"Group '{groupName}' does not satisfy the local bundle schema.", out error);
            error = null;
            return true;
        }

        private static bool TryRequireEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address,
            string roleLabel,
            bool requirePartition,
            out string error)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            if (entry == null || entry.parentGroup != group ||
                !string.Equals(entry.address, address, StringComparison.Ordinal))
                return Fail($"Addressable entry drifted: {assetPath}.", out error);

            int roleCount = 0;
            int partitionCount = 0;
            foreach (string label in entry.labels)
            {
                if (label.StartsWith("operation-map-role-", StringComparison.Ordinal))
                    roleCount++;
                if (label.StartsWith("operation-map-partition-", StringComparison.Ordinal))
                    partitionCount++;
            }

            if (!entry.labels.Contains(OperationMapAddressablesLayoutBuilder.OperationMapLabel) ||
                !entry.labels.Contains(OperationMapAddressablesLayoutBuilder.LocalLabel) ||
                !entry.labels.Contains(OperationMapAddressablesLayoutBuilder.PackLabel) ||
                !entry.labels.Contains(roleLabel) || roleCount != 1 ||
                partitionCount != (requirePartition ? 1 : 0))
                return Fail($"Addressable labels drifted: {address}.", out error);

            error = null;
            return true;
        }

        private static bool ContainsAddress(AddressableAssetSettings settings, string address)
        {
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (string.Equals(entry.address, address, StringComparison.Ordinal))
                        return true;
                }
            }
            return false;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
