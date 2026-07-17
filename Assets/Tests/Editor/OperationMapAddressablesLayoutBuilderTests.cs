using Game.Editor;
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
        Assert.That(core.entries, Is.Empty);
        Assert.That(presentation.entries, Is.Empty);

        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.CatalogPath, catalog, "operation-map/catalog");
        AssertEntry(
            settings,
            OperationMapAddressablesLayoutBuilder.DefinitionPath,
            catalog,
            "operation-map/opmap.skirmish.desert_base_01/definition");
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
