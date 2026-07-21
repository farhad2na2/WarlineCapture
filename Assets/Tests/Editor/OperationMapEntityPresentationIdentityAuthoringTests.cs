using System;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapEntityPresentationIdentityAuthoringTests
{
    private const string OperationMapId = "opmap.skirmish.desert_base_01";
    private const string SourceGlobalObjectId =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1990580264897520-8699699288898649154";

    private GameObject root;

    public static void RunFocusedValidation()
    {
        var suite = new OperationMapEntityPresentationIdentityAuthoringTests();
        Action[] tests =
        {
            suite.TryValidate_AcceptsEveryOwnedPresentationRole,
            suite.TryValidate_RejectsMalformedSourceIdentity,
            suite.TryValidate_RejectsRolePlacementMismatch,
            suite.TryValidate_RejectsUnknownRole,
            suite.Contract_IsPassiveAndBakesAnUnmanagedIdentity
        };

        for (int i = 0; i < tests.Length; i++)
        {
            try
            {
                tests[i]();
            }
            finally
            {
                suite.TearDown();
            }
        }

        Debug.Log($"[OperationMapEntityPresentationIdentityValidation] result=Passed tests={tests.Length}");
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            UnityEngine.Object.DestroyImmediate(root);
    }

    [Test]
    public void TryValidate_AcceptsEveryOwnedPresentationRole()
    {
        OperationMapEntityPresentationIdentityAuthoring marker = CreateMarker(
            OperationMapEntityPresentationRole.GameplayBuildings,
            placementIndex: 0);
        Assert.That(marker.TryValidate(out string error), Is.True, error);

        SetSerializedValues(marker, OperationMapEntityPresentationRole.GameplayVehicles, placementIndex: 21);
        Assert.That(marker.TryValidate(out error), Is.True, error);

        SetSerializedValues(
            marker,
            OperationMapEntityPresentationRole.RenderOnly,
            OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);
        Assert.That(marker.TryValidate(out error), Is.True, error);
    }

    [Test]
    public void TryValidate_RejectsMalformedSourceIdentity()
    {
        OperationMapEntityPresentationIdentityAuthoring marker = CreateMarker(
            OperationMapEntityPresentationRole.GameplayBuildings,
            placementIndex: 0,
            sourceGlobalObjectId: "GlobalObjectId_V1-not-deterministic");

        Assert.That(marker.TryValidate(out string error), Is.False);
        Assert.That(error, Does.Contain("Source GlobalObjectId"));
    }

    [Test]
    public void TryValidate_RejectsRolePlacementMismatch()
    {
        OperationMapEntityPresentationIdentityAuthoring marker = CreateMarker(
            OperationMapEntityPresentationRole.GameplayVehicles,
            OperationMapEntityPresentationIdentityAuthoring.NoPlacementIndex);
        Assert.That(marker.TryValidate(out string error), Is.False);
        Assert.That(error, Does.Contain("Placement index"));

        SetSerializedValues(marker, OperationMapEntityPresentationRole.RenderOnly, placementIndex: 0);
        Assert.That(marker.TryValidate(out error), Is.False);
        Assert.That(error, Does.Contain("Placement index"));
    }

    [Test]
    public void TryValidate_RejectsUnknownRole()
    {
        OperationMapEntityPresentationIdentityAuthoring marker = CreateMarker(
            (OperationMapEntityPresentationRole)byte.MaxValue,
            placementIndex: 0);

        Assert.That(marker.TryValidate(out string error), Is.False);
        Assert.That(error, Does.Contain("Unknown operation-map entity presentation role"));
    }

    [Test]
    public void Contract_IsPassiveAndBakesAnUnmanagedIdentity()
    {
        Type authoringType = typeof(OperationMapEntityPresentationIdentityAuthoring);
        Type componentType = typeof(OperationMapEntityPresentationIdentity);

        Assert.That(authoringType.IsSealed, Is.True);
        Assert.That(authoringType.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(authoringType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(authoringType.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(authoringType.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
        Assert.That(componentType.IsValueType, Is.True);
        Assert.That(componentType.GetFields(BindingFlags.Instance | BindingFlags.Public).Length, Is.EqualTo(4));
    }

    private OperationMapEntityPresentationIdentityAuthoring CreateMarker(
        OperationMapEntityPresentationRole role,
        int placementIndex,
        string sourceGlobalObjectId = SourceGlobalObjectId)
    {
        root = new GameObject(nameof(OperationMapEntityPresentationIdentityAuthoringTests));
        OperationMapEntityPresentationIdentityAuthoring marker =
            root.AddComponent<OperationMapEntityPresentationIdentityAuthoring>();
        SetSerializedValues(marker, role, placementIndex, sourceGlobalObjectId);
        return marker;
    }

    private static void SetSerializedValues(
        OperationMapEntityPresentationIdentityAuthoring marker,
        OperationMapEntityPresentationRole role,
        int placementIndex,
        string sourceGlobalObjectId = SourceGlobalObjectId)
    {
        var serialized = new SerializedObject(marker);
        serialized.FindProperty("operationMapId").stringValue = OperationMapId;
        serialized.FindProperty("sourceGlobalObjectId").stringValue = sourceGlobalObjectId;
        serialized.FindProperty("role").intValue = (byte)role;
        serialized.FindProperty("placementIndex").intValue = placementIndex;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
