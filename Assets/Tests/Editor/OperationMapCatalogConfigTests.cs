using System;
using System.Reflection;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapCatalogConfigTests
{
    private const string CatalogPath =
        "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset";
    private const string DefinitionPath =
        "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";

    [Test]
    public void CompatibilityCatalog_ValidatesAndResolvesCurrentDefinition()
    {
        OperationMapCatalogConfig catalog =
            AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(CatalogPath);
        OperationMapDefinition expected =
            AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);

        Assert.That(catalog, Is.Not.Null);
        Assert.That(expected, Is.Not.Null);
        Assert.That(catalog.TryValidate(out string error), Is.True, error);
        Assert.That(catalog.Definitions.Length, Is.EqualTo(1));
        Assert.That(catalog.Entries.Length, Is.EqualTo(1));
        Assert.That(catalog.TryResolve(expected.OperationMapId, out OperationMapDefinition resolved), Is.True);
        Assert.That(resolved, Is.SameAs(expected));
        Assert.That(catalog.TryResolveEntry(expected.OperationMapId, out OperationMapCatalogEntryConfig entry), Is.True);
        Assert.That(entry.Definition, Is.SameAs(expected));
        Assert.That(entry.ContentPack.ContentPackId, Is.EqualTo("opmap-pack.skirmish.desert_base_01"));
        Assert.That(entry.ContentPack.DeliveryKind, Is.EqualTo(OperationMapDeliveryKind.BuiltInLocal));
        Assert.That(entry.ContentPack.ContentVersion, Is.EqualTo(expected.ContentVersion));
        Assert.That(entry.ContentPack.ContentHash, Is.EqualTo(expected.ContentHash));
        Assert.That(catalog.TryResolve("opmap.skirmish.missing", out resolved), Is.False);
        Assert.That(resolved, Is.Null);
        Assert.That(catalog.TryResolveEntry("opmap.skirmish.missing", out _), Is.False);
    }

    [Test]
    public void Validation_RejectsMissingAndDuplicateDefinitions()
    {
        OperationMapDefinition definition =
            AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
        OperationMapCatalogConfig catalog = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
        try
        {
            Assert.That(catalog.TryValidate(out _), Is.False);

            SetDefinitions(catalog, new OperationMapDefinition[] { null });
            Assert.That(catalog.TryValidate(out string missingError), Is.False);
            StringAssert.Contains("missing", missingError);

            SetDefinitions(catalog, new[] { definition, definition });
            Assert.That(catalog.TryValidate(out string duplicateError), Is.False);
            StringAssert.Contains("Duplicate", duplicateError);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void ContentPackValidation_RejectsIdentityVersionHashAndDeliveryDrift()
    {
        OperationMapDefinition definition =
            AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);

        Assert.That(new OperationMapContentPackConfig(
            "opmap-pack.skirmish.desert_base_01",
            OperationMapDeliveryKind.BuiltInLocal,
            definition.ContentVersion,
            definition.ContentHash).TryValidate(definition, out string error), Is.True, error);

        Assert.That(new OperationMapContentPackConfig(
            "opmap-pack.skirmish.wrong",
            OperationMapDeliveryKind.BuiltInLocal,
            definition.ContentVersion,
            definition.ContentHash).TryValidate(definition, out error), Is.False);
        StringAssert.Contains("Content-pack id", error);

        Assert.That(new OperationMapContentPackConfig(
            "opmap-pack.skirmish.desert_base_01",
            OperationMapDeliveryKind.BuiltInLocal,
            definition.ContentVersion + 1,
            definition.ContentHash).TryValidate(definition, out error), Is.False);
        StringAssert.Contains("version and hash", error);

        Assert.That(new OperationMapContentPackConfig(
            "opmap-pack.skirmish.desert_base_01",
            (OperationMapDeliveryKind)byte.MaxValue,
            definition.ContentVersion,
            definition.ContentHash).TryValidate(definition, out error), Is.False);
        StringAssert.Contains("delivery kind", error);
    }

    [Test]
    public void Resolve_IsAllocationFreeAfterWarmup()
    {
        OperationMapCatalogConfig catalog =
            AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(CatalogPath);
        const string operationMapId = "opmap.skirmish.desert_base_01";
        Assert.That(catalog.TryResolve(operationMapId, out _), Is.True);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 256; index++)
            catalog.TryResolve(operationMapId, out _);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private static void SetDefinitions(
        OperationMapCatalogConfig catalog,
        OperationMapDefinition[] definitions)
    {
        FieldInfo field = typeof(OperationMapCatalogConfig).GetField(
            "definitions",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(catalog, definitions);
    }
}
