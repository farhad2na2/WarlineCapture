using System;
using System.Reflection;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class OperationMapRenderVirtualizationValidation
{
    public static void RunFocusedValidation()
    {
        try
        {
            OperationMapRenderVirtualizationValidation tests = new();
            tests.RenderResidencyMode_IsClosedAndStable();
            tests.OperationMapDefinition_DefaultsToResidentEntities();
            tests.StaticSceneChunks_RejectsVirtualizedProxyPool();
            tests.EntityScene_ResidentEntitiesRetainsCurrentBehavior();
            tests.EntityScene_VirtualizedProxyPoolFailsClosedUntilDatabaseContractExists();
            tests.UnknownRenderResidencyMode_IsRejected();
            Debug.Log("[OperationMapRenderVirtualizationValidation] result=Passed tests=6");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[OperationMapRenderVirtualizationValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RenderResidencyMode_IsClosedAndStable()
    {
        Assert.That((byte)OperationMapRenderResidencyMode.ResidentEntities, Is.EqualTo(0));
        Assert.That((byte)OperationMapRenderResidencyMode.VirtualizedProxyPool, Is.EqualTo(1));
        Assert.That(
            Enum.GetValues(typeof(OperationMapRenderResidencyMode)).Length,
            Is.EqualTo(2));
    }

    [Test]
    public void OperationMapDefinition_DefaultsToResidentEntities()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        try
        {
            Assert.That(
                definition.RenderResidencyMode,
                Is.EqualTo(OperationMapRenderResidencyMode.ResidentEntities));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void StaticSceneChunks_RejectsVirtualizedProxyPool()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(
                definition,
                "renderResidencyMode",
                OperationMapRenderResidencyMode.VirtualizedProxyPool);

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("require ResidentEntities"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void EntityScene_ResidentEntitiesRetainsCurrentBehavior()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Set(definition, "staticPresentationManifestReference", new AssetReference());

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.True, error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void EntityScene_VirtualizedProxyPoolFailsClosedUntilDatabaseContractExists()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Set(
                definition,
                "renderResidencyMode",
                OperationMapRenderResidencyMode.VirtualizedProxyPool);
            Set(definition, "staticPresentationManifestReference", new AssetReference());

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("validated render-virtualization database"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void UnknownRenderResidencyMode_IsRejected()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(definition, "renderResidencyMode", (OperationMapRenderResidencyMode)byte.MaxValue);

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("Unknown operation-map render-residency mode: 255"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    private static OperationMapDefinition CreateDefinitionWithRequiredLocalReferences()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(definition, "sourceSceneReference", CreateReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
        Set(definition, "mapSurfaceDataReference", CreateReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
        Set(definition, "minimapRasterReference", CreateReference("cccccccccccccccccccccccccccccccc"));
        Set(
            definition,
            "staticPresentationManifestReference",
            CreateReference("dddddddddddddddddddddddddddddddd"));
        Set(definition, "buildingPlacementsReference", CreateReference("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"));
        Set(definition, "vehiclePlacementsReference", CreateReference("ffffffffffffffffffffffffffffffff"));
        return definition;
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
