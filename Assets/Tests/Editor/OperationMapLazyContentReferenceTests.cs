using System.Reflection;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class OperationMapLazyContentReferenceTests
{
    private const string ValidGuid = "0123456789abcdef0123456789abcdef";

    [Test]
    public void LocalContentReferences_ValidRequiredReferences_PassWithoutOptionalMetadata()
    {
        OperationMapDefinition definition = CreateDefinitionWithValidReferences();

        try
        {
            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.True, error);
            Assert.That(definition.OptionalHeavyMetadataReference, Is.Null);
            Assert.That(definition.SourceSceneReference.AssetGUID, Is.EqualTo(ValidGuid));
            Assert.That(definition.StaticPresentationManifestReference.AssetGUID, Is.EqualTo(ValidGuid));
            Assert.That(definition.MapSurfaceDataReference.AssetGUID, Is.EqualTo(ValidGuid));
            Assert.That(definition.MinimapRasterReference.AssetGUID, Is.EqualTo(ValidGuid));
            Assert.That(definition.BuildingPlacementsReference.AssetGUID, Is.EqualTo(ValidGuid));
            Assert.That(definition.VehiclePlacementsReference.AssetGUID, Is.EqualTo(ValidGuid));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [TestCase("sourceSceneReference", "source scene")]
    [TestCase("staticPresentationManifestReference", "static presentation manifest")]
    [TestCase("mapSurfaceDataReference", "map surface data")]
    [TestCase("minimapRasterReference", "minimap raster")]
    [TestCase("buildingPlacementsReference", "building placements")]
    [TestCase("vehiclePlacementsReference", "vehicle placements")]
    public void LocalContentReferences_MissingRequiredReference_FailsWithRole(
        string fieldName,
        string expectedRole)
    {
        OperationMapDefinition definition = CreateDefinitionWithValidReferences();
        Set<AssetReference>(definition, fieldName, null);

        try
        {
            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            StringAssert.Contains(expectedRole, error);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void LocalContentReferences_InvalidOptionalMetadata_FailsClosed()
    {
        OperationMapDefinition definition = CreateDefinitionWithValidReferences();
        Set(definition, "optionalHeavyMetadataReference", new AssetReference(string.Empty));

        try
        {
            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            StringAssert.Contains("Optional heavy metadata", error);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    private static OperationMapDefinition CreateDefinitionWithValidReferences()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(definition, "sourceSceneReference", new AssetReference(ValidGuid));
        Set(definition, "staticPresentationManifestReference", new AssetReference(ValidGuid));
        Set(definition, "mapSurfaceDataReference", new AssetReference(ValidGuid));
        Set(definition, "minimapRasterReference", new AssetReference(ValidGuid));
        Set(definition, "buildingPlacementsReference", new AssetReference(ValidGuid));
        Set(definition, "vehiclePlacementsReference", new AssetReference(ValidGuid));
        return definition;
    }

    private static void Set<T>(OperationMapDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(OperationMapDefinition).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(definition, value);
    }
}
