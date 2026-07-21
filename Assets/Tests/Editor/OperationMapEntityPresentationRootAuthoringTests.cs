using System.Reflection;
using Game.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapEntityPresentationRootAuthoringTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [Test]
    public void TryValidate_AcceptsCompleteDeterministicIdentity()
    {
        OperationMapEntityPresentationRootAuthoring marker = CreateMarker(
            "opmap.skirmish.desert_base_01",
            OperationMapEntityPresentationRole.RenderOnly,
            OperationMapEntityPresentationRootAuthoring.CurrentSchemaVersion,
            new string('a', 64));

        Assert.That(marker.TryValidate(out string error), Is.True, error);
        Assert.That(marker.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(marker.Role, Is.EqualTo(OperationMapEntityPresentationRole.RenderOnly));
        Assert.That(
            marker.SchemaVersion,
            Is.EqualTo(OperationMapEntityPresentationRootAuthoring.CurrentSchemaVersion));
        Assert.That(marker.MigrationRecordSetHash, Is.EqualTo(new string('a', 64)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("opmap.skirmish")]
    [TestCase("OPMAP.skirmish.desert_base_01")]
    public void TryValidate_RejectsEmptyOrMalformedOperationMapIdentity(string operationMapId)
    {
        OperationMapEntityPresentationRootAuthoring marker = CreateMarker(
            operationMapId,
            OperationMapEntityPresentationRole.GameplayBuildings,
            OperationMapEntityPresentationRootAuthoring.CurrentSchemaVersion,
            new string('b', 64));

        Assert.That(marker.TryValidate(out string error), Is.False);
        Assert.That(error, Does.Contain("Invalid operation-map id"));
    }

    [Test]
    public void TryValidate_RejectsUnknownRoleAndSchema()
    {
        OperationMapEntityPresentationRootAuthoring marker = CreateMarker(
            "opmap.skirmish.desert_base_01",
            (OperationMapEntityPresentationRole)byte.MaxValue,
            OperationMapEntityPresentationRootAuthoring.CurrentSchemaVersion,
            new string('c', 64));

        Assert.That(marker.TryValidate(out string error), Is.False);
        Assert.That(error, Does.Contain("Unknown operation-map entity presentation role"));

        SetSerializedValues(
            marker,
            "opmap.skirmish.desert_base_01",
            OperationMapEntityPresentationRole.GameplayVehicles,
            schemaVersion: 0,
            migrationRecordSetHash: new string('c', 64));

        Assert.That(marker.TryValidate(out error), Is.False);
        Assert.That(error, Does.Contain("schema version"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("ABCDEF")]
    [TestCase("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void TryValidate_RejectsEmptyOrMalformedMigrationIdentity(string migrationRecordSetHash)
    {
        OperationMapEntityPresentationRootAuthoring marker = CreateMarker(
            "opmap.skirmish.desert_base_01",
            OperationMapEntityPresentationRole.RenderOnly,
            OperationMapEntityPresentationRootAuthoring.CurrentSchemaVersion,
            migrationRecordSetHash);

        Assert.That(marker.TryValidate(out string error), Is.False);
        Assert.That(error, Does.Contain("Migration record-set hash"));
    }

    [Test]
    public void Contract_IsSealedDataOnlyMarkerWithoutUpdateMethods()
    {
        System.Type type = typeof(OperationMapEntityPresentationRootAuthoring);

        Assert.That(type.IsSealed, Is.True);
        Assert.That(type.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(type.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(type.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(type.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
    }

    private OperationMapEntityPresentationRootAuthoring CreateMarker(
        string operationMapId,
        OperationMapEntityPresentationRole role,
        int schemaVersion,
        string migrationRecordSetHash)
    {
        root = new GameObject("OperationMapEntityPresentationRootAuthoringTests");
        OperationMapEntityPresentationRootAuthoring marker =
            root.AddComponent<OperationMapEntityPresentationRootAuthoring>();
        SetSerializedValues(marker, operationMapId, role, schemaVersion, migrationRecordSetHash);
        return marker;
    }

    private static void SetSerializedValues(
        OperationMapEntityPresentationRootAuthoring marker,
        string operationMapId,
        OperationMapEntityPresentationRole role,
        int schemaVersion,
        string migrationRecordSetHash)
    {
        var serialized = new SerializedObject(marker);
        serialized.FindProperty("operationMapId").stringValue = operationMapId;
        serialized.FindProperty("role").intValue = (byte)role;
        serialized.FindProperty("schemaVersion").intValue = schemaVersion;
        serialized.FindProperty("migrationRecordSetHash").stringValue = migrationRecordSetHash;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
