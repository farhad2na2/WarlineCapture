using Game.Editor;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

public sealed class OperationMapAddressablesLayoutBuilderTests
{
    [Test]
    public void CurrentLayout_UsesExactLocalOneMapGroupTopology()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        Assert.That(settings, Is.Not.Null);

        AddressableAssetGroup catalog = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.CatalogGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogether);
        AddressableAssetGroup shared = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.SharedGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogether);
        AddressableAssetGroup core = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.CoreGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogether);
        AddressableAssetGroup presentation = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.PresentationGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);

        Assert.That(catalog.entries.Count, Is.EqualTo(2));
        Assert.That(shared.entries, Is.Empty);
        Assert.That(core.entries.Count, Is.EqualTo(5));
        Assert.That(presentation.entries.Count, Is.EqualTo(514));

        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.CatalogPath, catalog, "operation-map/catalog");
        AssertEntry(
            settings,
            OperationMapAddressablesLayoutBuilder.DefinitionPath,
            catalog,
            "operation-map/opmap.skirmish.desert_base_01/definition");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.SourceScenePath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "source-scene");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.MapSurfacePath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "map-surface");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.ManifestPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "static-manifest");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "building-placements");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "vehicle-placements");

        Dictionary<string, int> partitionCounts = new();
        foreach (AddressableAssetEntry entry in presentation.entries)
        {
            Assert.That(entry.address, Does.StartWith(OperationMapAddressablesLayoutBuilder.AddressPrefix + "presentation/chunk_"));
            AssertOperationMapLabels(entry, OperationMapAddressablesLayoutBuilder.PresentationRoleLabel, true);
            foreach (string label in entry.labels)
            {
                if (!label.StartsWith("operation-map-partition-", System.StringComparison.Ordinal))
                    continue;
                partitionCounts.TryGetValue(label, out int count);
                partitionCounts[label] = count + 1;
            }
        }

        Assert.That(partitionCounts, Is.Not.Empty);
        foreach (KeyValuePair<string, int> partition in partitionCounts)
            Assert.That(partition.Value, Is.InRange(1, 25), partition.Key);

        AssertOperationMapLabels(settings.FindAssetEntry(UnityEditor.AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.CatalogPath)), OperationMapAddressablesLayoutBuilder.MetadataRoleLabel, false);
        AssertOperationMapLabels(settings.FindAssetEntry(UnityEditor.AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.DefinitionPath)), OperationMapAddressablesLayoutBuilder.DefinitionRoleLabel, false);
        foreach (AddressableAssetEntry entry in core.entries)
        {
            string role = entry.address.EndsWith("/source-scene", System.StringComparison.Ordinal)
                ? OperationMapAddressablesLayoutBuilder.SourceSceneRoleLabel
                : OperationMapAddressablesLayoutBuilder.MetadataRoleLabel;
            AssertOperationMapLabels(entry, role, false);
        }
    }

    private static void AssertOperationMapLabels(
        AddressableAssetEntry entry,
        string expectedRole,
        bool expectsPartition)
    {
        Assert.That(entry.labels, Does.Contain(OperationMapAddressablesLayoutBuilder.OperationMapLabel));
        Assert.That(entry.labels, Does.Contain(OperationMapAddressablesLayoutBuilder.LocalLabel));
        Assert.That(entry.labels, Does.Contain(OperationMapAddressablesLayoutBuilder.PackLabel));
        Assert.That(entry.labels, Does.Contain(expectedRole));

        int roleCount = 0;
        int partitionCount = 0;
        foreach (string label in entry.labels)
        {
            if (label.StartsWith("operation-map-role-", System.StringComparison.Ordinal))
                roleCount++;
            if (label.StartsWith("operation-map-partition-", System.StringComparison.Ordinal))
                partitionCount++;
        }

        Assert.That(roleCount, Is.EqualTo(1), entry.address);
        Assert.That(partitionCount, Is.EqualTo(expectsPartition ? 1 : 0), entry.address);
    }

    private static AddressableAssetGroup RequireGroup(
        AddressableAssetSettings settings,
        string groupName,
        BundledAssetGroupSchema.BundlePackingMode bundleMode)
    {
        AddressableAssetGroup group = settings.FindGroup(groupName);
        Assert.That(group, Is.Not.Null, groupName);
        BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
        Assert.That(schema, Is.Not.Null, groupName);
        Assert.That(schema.BuildPath.GetName(settings), Is.EqualTo(AddressableAssetSettings.kLocalBuildPath));
        Assert.That(schema.LoadPath.GetName(settings), Is.EqualTo(AddressableAssetSettings.kLocalLoadPath));
        Assert.That(schema.Compression, Is.EqualTo(BundledAssetGroupSchema.BundleCompressionMode.LZ4));
        Assert.That(schema.UseAssetBundleCrc, Is.True);
        Assert.That(schema.UseAssetBundleCrcForCachedBundles, Is.True);
        Assert.That(schema.BundleNaming, Is.EqualTo(BundledAssetGroupSchema.BundleNamingStyle.AppendHash));
        Assert.That(schema.BundleMode, Is.EqualTo(bundleMode));
        return group;
    }

    private static void AssertEntry(
        AddressableAssetSettings settings,
        string assetPath,
        AddressableAssetGroup expectedGroup,
        string expectedAddress)
    {
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
        AddressableAssetEntry entry = settings.FindAssetEntry(guid);
        Assert.That(entry, Is.Not.Null, assetPath);
        Assert.That(entry.parentGroup, Is.SameAs(expectedGroup));
        Assert.That(entry.address, Is.EqualTo(expectedAddress));
    }
}
