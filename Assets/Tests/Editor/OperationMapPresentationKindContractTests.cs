using System.Reflection;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class OperationMapPresentationKindContractTests
{
    [Test]
    public void PresentationKind_DefaultsToStaticSceneChunksAndKeepsCurrentReferenceRules()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        try
        {
            Assert.That(
                definition.PresentationKind,
                Is.EqualTo(OperationMapPresentationKind.StaticSceneChunks));

            Set(definition, "sourceSceneReference", CreateReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            Set(definition, "mapSurfaceDataReference", CreateReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
            Set(definition, "minimapRasterReference", CreateReference("cccccccccccccccccccccccccccccccc"));

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("static presentation manifest"));

            Set(
                definition,
                "staticPresentationManifestReference",
                CreateReference("dddddddddddddddddddddddddddddddd"));
            Set(definition, "buildingPlacementsReference", CreateReference("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"));
            Set(definition, "vehiclePlacementsReference", CreateReference("ffffffffffffffffffffffffffffffff"));

            Assert.That(definition.TryValidateLocalContentReferences(out error), Is.True, error);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void EntityScene_RejectsStaticManifestAndAllowsLegacyPlacementEvidence()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        try
        {
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Set(definition, "sourceSceneReference", CreateReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
            Set(definition, "mapSurfaceDataReference", CreateReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
            Set(definition, "minimapRasterReference", CreateReference("cccccccccccccccccccccccccccccccc"));
            Set(
                definition,
                "staticPresentationManifestReference",
                CreateReference("dddddddddddddddddddddddddddddddd"));

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("must not require a production static presentation manifest"));

            Set(definition, "staticPresentationManifestReference", new AssetReference());
            Set(definition, "buildingPlacementsReference", CreateReference("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"));
            Set(definition, "vehiclePlacementsReference", CreateReference("ffffffffffffffffffffffffffffffff"));

            Assert.That(definition.TryValidateLocalContentReferences(out error), Is.True, error);
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void CanonicalPresentationMode_EntitySceneIsClosedAndDistinct()
    {
        Assert.That(
            (byte)Game.Rendering.OperationMapCanonicalPresentationMode.EntityScene,
            Is.EqualTo(2));
        Assert.That(
            System.Enum.IsDefined(
                typeof(Game.Rendering.OperationMapCanonicalPresentationMode),
                Game.Rendering.OperationMapCanonicalPresentationMode.EntityScene),
            Is.True);
    }

    private static AssetReference CreateReference(string guid)
    {
        return new AssetReference(guid);
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
