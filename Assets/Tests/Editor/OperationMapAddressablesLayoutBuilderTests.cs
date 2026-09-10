using Game.Configs;
using Game.Editor;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.AddressableAssets;

public sealed class OperationMapAddressablesLayoutBuilderTests
{
    [Test]
    public void SharedDependencyThresholdCoversEveryCrossBundleDependency()
    {
        Assert.That(OperationMapAddressablesLayoutBuilder.SharedDependencyPartitionThreshold, Is.EqualTo(2));
    }

    [Test]
    public void CurrentLayout_UsesExactLocalOneMapGroupTopology()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.BuildRemoteCatalog, Is.False);
        Assert.That(settings.DisableCatalogUpdateOnStartup, Is.True);
        Assert.That(settings.UniqueBundleIds, Is.False);
        var catalog = RequireGroup(settings, OperationMapAddressablesLayoutBuilder.CatalogGroupName, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
        var shared = RequireGroup(settings, OperationMapAddressablesLayoutBuilder.SharedGroupName, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
        var core = RequireGroup(settings, OperationMapAddressablesLayoutBuilder.CoreGroupName, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
        var presentation = RequireGroup(settings, OperationMapAddressablesLayoutBuilder.PresentationGroupName, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
        Assert.That(catalog.entries, Has.Count.EqualTo(2));
        Assert.That(core.entries, Has.Count.EqualTo(4));
        Assert.That(shared.entries, Is.Empty);
        Assert.That(presentation.entries, Is.Empty);
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.CatalogPath, catalog, "operation-map/catalog");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.DefinitionPath, catalog, "operation-map/opmap.skirmish.desert_base_01/definition");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.SourceScenePath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "source-scene");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.MapSurfacePath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "map-surface");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.MinimapRasterPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "minimap-raster");
        AssertEntry(settings, "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity", core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "entity-scene");
        foreach (string retiredPath in new[] { OperationMapAddressablesLayoutBuilder.AuthoringScenePath, OperationMapAddressablesLayoutBuilder.ManifestPath,
                     OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath, OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath })
            Assert.That(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(retiredPath)), Is.Null, retiredPath);
    }

    [Test]
    public void SharedShardLabel_IsDeterministicAndBounded()
    {
        string first = OperationMapAddressablesLayoutBuilder.BuildSharedShardLabel(
            "Assets/Textures/Map.png",
            "7f000000000000000000000000000000");
        string second = OperationMapAddressablesLayoutBuilder.BuildSharedShardLabel(
            "Assets/Textures/Map.png",
            "7f000000000000000000000000000000");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.EqualTo("operation-map-shared-shard-texture-07"));
    }

    [Test]
    public void SharedShardLabel_UsesDedicatedShaderKind()
    {
        string label = OperationMapAddressablesLayoutBuilder.BuildSharedShardLabel(
            "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader",
            "933532a4fcc9baf4fa0491de14d08ed7");
        string projectShaderLabel = OperationMapAddressablesLayoutBuilder.BuildSharedShardLabel(
            "Assets/Game/Rendering/Shaders/GroundMacroVariation.shader",
            "ccc0634edfe14e0c95ffa7446dd9ec82");

        Assert.That(label, Is.EqualTo("operation-map-shared-shard-shader-00"));
        Assert.That(projectShaderLabel, Is.EqualTo(label));
    }

    [TestCase("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader", true)]
    [TestCase("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Lit.mat", true)]
    [TestCase("Packages/com.unity.shadergraph/Editor/Resources/Shaders/FallbackError.shader", false)]
    [TestCase("Packages/com.example.rendering/eDiToR/Shaders/FallbackError.shader", false)]
    [TestCase("Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl", false)]
    [TestCase("Assets/Game/Materials/Map.mat", true)]
    public void ShareableDependencyPath_RestrictsPackageAssetsToRuntimeShaderOwnership(
        string path,
        bool expected)
    {
        Assert.That(OperationMapAddressablesLayoutBuilder.IsShareableDependencyPath(path), Is.EqualTo(expected));
    }

    [Test]
    public void CurrentDefinition_ReferencesConfiguredHeavyAssetsByGuid()
    {
        OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapAddressablesLayoutBuilder.DefinitionPath);
        Assert.That(definition, Is.Not.Null);

        AssertReference(definition.SourceSceneReference, OperationMapAddressablesLayoutBuilder.SourceScenePath);
        AssertReference(definition.MapSurfaceDataReference, OperationMapAddressablesLayoutBuilder.MapSurfacePath);
        Assert.That(definition.StaticPresentationManifestReference.RuntimeKeyIsValid(), Is.False);
        Assert.That(definition.BuildingPlacementsReference.RuntimeKeyIsValid(), Is.False);
        Assert.That(definition.VehiclePlacementsReference.RuntimeKeyIsValid(), Is.False);
        AssertReference(
            definition.MinimapRasterReference,
            OperationMapAddressablesLayoutBuilder.MinimapRasterPath);
        Assert.That(definition.OptionalHeavyMetadataReference.RuntimeKeyIsValid(), Is.False);
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
        Assert.That(schema.BundleNaming, Is.EqualTo(BundledAssetGroupSchema.BundleNamingStyle.FileNameHash));
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

    private static void AssertReference(AssetReference reference, string expectedPath)
    {
        Assert.That(reference, Is.Not.Null, expectedPath);
        Assert.That(reference.AssetGUID, Is.EqualTo(AssetDatabase.AssetPathToGUID(expectedPath)));
    }
}
