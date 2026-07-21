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

        AddressableAssetGroup catalog = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.CatalogGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogether);
        AddressableAssetGroup shared = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.SharedGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
        AddressableAssetGroup core = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.CoreGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogether);
        AddressableAssetGroup presentation = RequireGroup(
            settings,
            OperationMapAddressablesLayoutBuilder.PresentationGroupName,
            BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);

        Assert.That(catalog.entries.Count, Is.EqualTo(2));
        Game.Rendering.StaticMapPresentationManifest manifest = AssetDatabase.LoadAssetAtPath<Game.Rendering.StaticMapPresentationManifest>(
            OperationMapAddressablesLayoutBuilder.ManifestPath);
        string[] expectedShared = OperationMapAddressablesLayoutBuilder.CollectSharedDependencyPaths(
            settings,
            manifest);
        Assert.That(shared.entries.Count, Is.EqualTo(expectedShared.Length));
        Assert.That(shared.entries, Is.Not.Empty);
        Assert.That(core.entries.Count, Is.EqualTo(6));
        Assert.That(presentation.entries.Count, Is.EqualTo(manifest.Chunks.Count));

        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.CatalogPath, catalog, "operation-map/catalog");
        AssertEntry(
            settings,
            OperationMapAddressablesLayoutBuilder.DefinitionPath,
            catalog,
            "operation-map/opmap.skirmish.desert_base_01/definition");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.SourceScenePath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "source-scene");
        Assert.That(
            settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath)),
            Is.Null,
            "The hand-authored scene must remain outside Addressables after runtime-binding cutover.");
        Assert.That(
            AssetDatabase.GetDependencies(OperationMapAddressablesLayoutBuilder.SourceScenePath, true),
            Does.Not.Contain(OperationMapAddressablesLayoutBuilder.AuthoringScenePath));
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.MapSurfacePath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "map-surface");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.ManifestPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "static-manifest");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "building-placements");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "vehicle-placements");
        AssertEntry(settings, OperationMapAddressablesLayoutBuilder.MinimapRasterPath, core, OperationMapAddressablesLayoutBuilder.AddressPrefix + "minimap-raster");

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

        foreach (AddressableAssetEntry entry in shared.entries)
        {
            Assert.That(entry.address, Does.StartWith("operation-map/shared/"));
            AssertOperationMapLabels(entry, OperationMapAddressablesLayoutBuilder.SharedDependencyRoleLabel, false);
            string path = AssetDatabase.GUIDToAssetPath(entry.guid);
            string expectedShard = OperationMapAddressablesLayoutBuilder.BuildSharedShardLabel(path, entry.guid);
            Assert.That(entry.labels, Does.Contain(expectedShard));
            Assert.That(entry.labels.Count(label => label.StartsWith(
                OperationMapAddressablesLayoutBuilder.SharedShardLabelPrefix,
                System.StringComparison.Ordinal)), Is.EqualTo(1));
        }

        string[] activeSharedShardLabels = shared.entries
            .SelectMany(entry => entry.labels)
            .Where(label => label.StartsWith(
                OperationMapAddressablesLayoutBuilder.SharedShardLabelPrefix,
                System.StringComparison.Ordinal))
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();
        string[] configuredSharedShardLabels = settings.GetLabels()
            .Where(label => label.StartsWith(
                OperationMapAddressablesLayoutBuilder.SharedShardLabelPrefix,
                System.StringComparison.Ordinal))
            .ToArray();
        Assert.That(configuredSharedShardLabels, Is.EquivalentTo(activeSharedShardLabels));

        AssertOperationMapLabels(settings.FindAssetEntry(UnityEditor.AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.CatalogPath)), OperationMapAddressablesLayoutBuilder.MetadataRoleLabel, false);
        AssertOperationMapLabels(settings.FindAssetEntry(UnityEditor.AssetDatabase.AssetPathToGUID(OperationMapAddressablesLayoutBuilder.DefinitionPath)), OperationMapAddressablesLayoutBuilder.DefinitionRoleLabel, false);
        foreach (AddressableAssetEntry entry in core.entries)
        {
            string role = entry.address.EndsWith("/source-scene", System.StringComparison.Ordinal)
                ? OperationMapAddressablesLayoutBuilder.SourceSceneRoleLabel
                : entry.address.EndsWith("/minimap-raster", System.StringComparison.Ordinal)
                    ? OperationMapAddressablesLayoutBuilder.MinimapRasterRoleLabel
                    : OperationMapAddressablesLayoutBuilder.MetadataRoleLabel;
            AssertOperationMapLabels(entry, role, false);
        }
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
        AssertReference(
            definition.StaticPresentationManifestReference,
            OperationMapAddressablesLayoutBuilder.ManifestPath);
        AssertReference(
            definition.BuildingPlacementsReference,
            OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath);
        AssertReference(
            definition.VehiclePlacementsReference,
            OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath);
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
