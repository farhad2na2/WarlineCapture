using System;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Editor;
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
            suite.VehicleOwnershipAuthoring_PreservesVehicleFaction,
            suite.Contract_IsPassiveAndBakesAnUnmanagedIdentity,
            suite.BackfillContract_RequiresEveryAcceptedOwner,
            suite.MatrixComparison_UsesExplicitTolerance,
            suite.ExistingCopyMatrix_ExposesScaledAncestryLoss,
            suite.MirroredParentChain_PreservesNestedWorldMatrix,
            suite.VisualParity_RejectsFlattenedScaledAncestry
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
    public void VehicleOwnershipAuthoring_PreservesVehicleFaction()
    {
        root = new GameObject(nameof(OperationMapEntityPresentationIdentityAuthoringTests));
        OperationMapAuthoredVehicleOwnershipAuthoring ownership =
            root.AddComponent<OperationMapAuthoredVehicleOwnershipAuthoring>();
        ownership.ConfigureForEditor(FactionIdentity.PlayerFactionId);

        Assert.That(ownership.FactionId, Is.EqualTo(FactionIdentity.PlayerFactionId));
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

    [Test]
    public void BackfillContract_RequiresEveryAcceptedOwner()
    {
        Assert.That(OperationMapEntityPresentationIdentityBackfillEditor.ExpectedBuildingCount, Is.EqualTo(432));
        Assert.That(OperationMapEntityPresentationIdentityBackfillEditor.ExpectedVehicleCount, Is.EqualTo(22));
        Assert.That(OperationMapEntityPresentationIdentityBackfillEditor.ExpectedRenderOnlyCount, Is.EqualTo(9090));
        Assert.That(OperationMapEntityPresentationIdentityBackfillEditor.ExpectedIdentityCount, Is.EqualTo(9544));
    }

    [Test]
    public void MatrixComparison_UsesExplicitTolerance()
    {
        Matrix4x4 baseline = Matrix4x4.TRS(
            new Vector3(100f, 2f, -30f),
            Quaternion.Euler(15f, 45f, 5f),
            new Vector3(-2f, 1.5f, 0.75f));
        Matrix4x4 withinTolerance = baseline;
        withinTolerance.m03 += 0.00005f;
        Matrix4x4 outsideTolerance = baseline;
        outsideTolerance.m03 += 0.001f;

        Assert.That(
            OperationMapEntityPresentationIdentityBackfillEditor.MatricesApproximatelyEqual(
                baseline,
                withinTolerance,
                0.0001f),
            Is.True);
        Assert.That(
            OperationMapEntityPresentationIdentityBackfillEditor.MatricesApproximatelyEqual(
                baseline,
                outsideTolerance,
                0.0001f),
            Is.False);
    }

    [Test]
    public void ExistingCopyMatrix_ExposesScaledAncestryLoss()
    {
        root = new GameObject("ScaledParent");
        root.transform.localScale = new Vector3(2f, 3f, 4f);
        var child = new GameObject("SourceOwner");
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = new Vector3(3f, 1f, -2f);
        child.transform.localRotation = Quaternion.Euler(10f, 30f, 5f);
        child.transform.localScale = new Vector3(0.5f, 1.25f, 2f);

        Matrix4x4 fullSourceMatrix = child.transform.localToWorldMatrix;
        Matrix4x4 existingCopyMatrix =
            OperationMapEntityPresentationIdentityBackfillEditor.CreateExistingCopyWorldMatrix(child.transform);

        Assert.That(
            OperationMapEntityPresentationIdentityBackfillEditor.MatricesApproximatelyEqual(
                fullSourceMatrix,
                existingCopyMatrix,
                0.0001f),
            Is.False);
    }

    [Test]
    public void MirroredParentChain_PreservesNestedWorldMatrix()
    {
        root = new GameObject("Root");
        Transform sourceParent = CreateChild(root.transform, "SourceParent");
        sourceParent.localPosition = new Vector3(7f, 2f, -3f);
        sourceParent.localRotation = Quaternion.Euler(0f, 37f, 11f);
        sourceParent.localScale = new Vector3(-2f, 3f, 0.5f);
        Transform sourceOwner = CreateChild(sourceParent, "SourceOwner");
        sourceOwner.localPosition = new Vector3(4f, -1f, 8f);
        sourceOwner.localRotation = Quaternion.Euler(23f, 9f, 41f);
        sourceOwner.localScale = new Vector3(0.75f, 1.25f, 2f);

        Transform mirroredParent = CreateChild(root.transform, "MirroredParent");
        OperationMapRenderOnlyCandidateMigrationEditor.CopyLocalTransform(sourceParent, mirroredParent);
        Transform candidateOwner = CreateChild(mirroredParent, "CandidateOwner");
        OperationMapRenderOnlyCandidateMigrationEditor.CopyLocalTransform(sourceOwner, candidateOwner);

        Assert.That(
            OperationMapEntityPresentationIdentityBackfillEditor.MatricesApproximatelyEqual(
                sourceOwner.localToWorldMatrix,
                candidateOwner.localToWorldMatrix,
                0.0001f),
            Is.True);
    }

    [Test]
    public void VisualParity_RejectsFlattenedScaledAncestry()
    {
        root = new GameObject("Root");
        Transform sourceParent = CreateChild(root.transform, "SourceParent");
        sourceParent.localRotation = Quaternion.Euler(0f, 45f, 0f);
        sourceParent.localScale = new Vector3(2f, 1f, 0.5f);
        Transform sourceOwner = CreateChild(sourceParent, "SourceOwner");
        sourceOwner.localRotation = Quaternion.Euler(20f, 0f, 35f);

        Transform flattened = CreateChild(root.transform, "Flattened");
        OperationMapRenderOnlyCandidateMigrationEditor.CopyLocalTransform(sourceOwner, flattened);

        Assert.Throws<InvalidOperationException>(() =>
            OperationMapRenderOnlyCandidateMigrationEditor.RequireExactVisualParity(
                sourceOwner.gameObject,
                flattened.gameObject,
                SourceGlobalObjectId));
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
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
