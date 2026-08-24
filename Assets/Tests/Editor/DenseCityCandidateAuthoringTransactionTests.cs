using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityCandidateAuthoringTransactionTests
{
    private const string TempRoot =
        "Assets/Tests/Editor/DenseCityCandidateAuthoringTransactionTemp";
    private const string SourceMapPath = TempRoot + "/source-map.unity";
    private const string SourceEntityPath = TempRoot + "/source-entity.unity";
    private const string CandidateMapPath = TempRoot + "/candidate-map.unity";
    private const string CandidateEntityPath = TempRoot + "/candidate-entity.unity";
    private const string PlacementConfigPath = TempRoot + "/building-placements.asset";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static void RunProxyFailureRollbackValidation()
    {
        var suite = new DenseCityCandidateAuthoringTransactionTests();
        suite.SetUp();
        try
        {
            suite.ProxyFailure_RestoresAcceptedOutputAndRemovesPartialReplacement();
        }
        finally
        {
            suite.TearDown();
        }

        Debug.Log(
            "[DenseCityCandidateAuthoringProxyFailureRollbackValidation] " +
            "result=Passed tests=1");
    }

    public static void RunRetainedRooftopSupportGeometryValidation()
    {
        var suite = new DenseCityCandidateAuthoringTransactionTests();
        suite.RetainedRooftopSupportAssembly_MustCoverPropAndConnectToBuilding();
        suite.RetainedRooftopSupportPlan_ProducesAttachedGroundConnectedAssembly();
        suite.RetainedRooftopSupportPlan_RotatedFacadeAndRooftopRemainAttachedAndDeterministic();
        suite.DetachedBuildingAttachments_RemoveOnlyElevatedDisconnectedProps();
        suite.CanonicalPresentationCleanup_RemovesDetachedPropAndKeepsGroundedProp();
        suite.CanonicalPresentationCleanup_ReplacesUnownedPrefabSignWithSemanticSign();
        Debug.Log(
            "[DenseCityRetainedRooftopSupportGeometryValidation] " +
            "result=Passed tests=6");
    }

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
        CreateScene(SourceMapPath, "AcceptedMapRoot");
        CreateScene(SourceEntityPath, "AcceptedEntityRoot");
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(TempRoot);
    }

    [Test]
    public void TryCreate_CopiesBothSourcesAndCreatesExactSemanticOwnership()
    {
        string sourceMapText = File.ReadAllText(ToPhysicalPath(SourceMapPath));
        string sourceEntityText = File.ReadAllText(ToPhysicalPath(SourceEntityPath));
        string sourceMapGuid = AssetDatabase.AssetPathToGUID(SourceMapPath);
        string sourceEntityGuid = AssetDatabase.AssetPathToGUID(SourceEntityPath);

        Assert.That(
            DenseCityCandidateAuthoringTransaction.TryCreate(
                SourceMapPath,
                SourceEntityPath,
                CandidateMapPath,
                CandidateEntityPath,
                "dense-city:test:24681357",
                "dense-city-v1",
                1,
                24681357,
                Hash,
                out string error),
            Is.True,
            error);

        Assert.That(
            File.ReadAllText(ToPhysicalPath(SourceMapPath)),
            Is.EqualTo(sourceMapText));
        Assert.That(
            File.ReadAllText(ToPhysicalPath(SourceEntityPath)),
            Is.EqualTo(sourceEntityText));
        Assert.That(
            AssetDatabase.AssetPathToGUID(CandidateMapPath),
            Is.Not.EqualTo(sourceMapGuid));
        Assert.That(
            AssetDatabase.AssetPathToGUID(CandidateEntityPath),
            Is.Not.EqualTo(sourceEntityGuid));

        Scene mapScene =
            EditorSceneManager.OpenScene(CandidateMapPath, OpenSceneMode.Additive);
        Scene entityScene =
            EditorSceneManager.OpenScene(CandidateEntityPath, OpenSceneMode.Additive);
        try
        {
            Assert.That(
                DenseCitySemanticHierarchyBuilder.TryValidate(
                    mapScene,
                    entityScene,
                    "dense-city:test:24681357",
                    out error),
                Is.True,
                error);
            Assert.That(
                mapScene.GetRootGameObjects().Any(root => root.name == "AcceptedMapRoot"),
                Is.True);
            Assert.That(
                entityScene.GetRootGameObjects().Any(root => root.name == "AcceptedEntityRoot"),
                Is.True);

            DenseCityGeneratedRootAuthoring mapRoot =
                mapScene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                    .Single();
            DenseCityGeneratedRootAuthoring entityRoot =
                entityScene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                    .Single();
            Assert.That(
                mapRoot.GetComponentsInChildren<MapBakeGroupAuthoring>(true),
                Has.Length.EqualTo(5));
            Assert.That(
                entityRoot.GetComponentsInChildren<MapBakeGroupAuthoring>(true),
                Is.Empty);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void TryCreate_RejectsExistingOutputWithoutCreatingSibling()
    {
        Assert.That(AssetDatabase.CopyAsset(SourceEntityPath, CandidateEntityPath), Is.True);
        string existingText = File.ReadAllText(ToPhysicalPath(CandidateEntityPath));

        Assert.That(
            DenseCityCandidateAuthoringTransaction.TryCreate(
                SourceMapPath,
                SourceEntityPath,
                CandidateMapPath,
                CandidateEntityPath,
                "dense-city:test:24681357",
                "dense-city-v1",
                1,
                24681357,
                Hash,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("already exists"));
        Assert.That(File.Exists(ToPhysicalPath(CandidateMapPath)), Is.False);
        Assert.That(
            File.ReadAllText(ToPhysicalPath(CandidateEntityPath)),
            Is.EqualTo(existingText));
    }

    [Test]
    public void TryCreate_RejectsInvalidIdentityWithoutCreatingOutputs()
    {
        Assert.That(
            DenseCityCandidateAuthoringTransaction.TryCreate(
                SourceMapPath,
                SourceEntityPath,
                CandidateMapPath,
                CandidateEntityPath,
                "dense-city:test:24681357",
                "dense-city-v1",
                1,
                24681357,
                "invalid",
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("hash is invalid"));
        Assert.That(File.Exists(ToPhysicalPath(CandidateMapPath)), Is.False);
        Assert.That(File.Exists(ToPhysicalPath(CandidateEntityPath)), Is.False);
    }

    [Test]
    public void RetainedRooftopSupportAssembly_MustCoverPropAndConnectToBuilding()
    {
        var propBounds = new Bounds(new Vector3(4f, 6f, -3f), new Vector3(2f, 4f, 2f));
        var attachedPlatform = new Bounds(
            new Vector3(4f, 3.83f, -3f),
            new Vector3(2.4f, 0.32f, 2.4f));
        var detachedPlatform = new Bounds(
            new Vector3(4f, 3.2f, -3f),
            new Vector3(2.4f, 0.32f, 2.4f));
        var undersizedPlatform = new Bounds(
            new Vector3(4f, 3.83f, -3f),
            new Vector3(1.4f, 0.32f, 1.4f));
        var attachment = new Bounds(
            new Vector3(4f, 1.5f, -3f),
            new Vector3(5f, 3f, 5f));
        var attachmentAnchor = new DenseMiddleEasternCityEditModeBuilder.RetainedRooftopPropAnchor(
            "SM_Bld_WaterTank_01[42]",
            Matrix4x4.identity,
            propBounds,
            attachment,
            attachment,
            Matrix4x4.identity);
        var connectedVerticalSupport = new Bounds(
            new Vector3(4f, 3.36f, -3f),
            new Vector3(0.5f, 0.72f, 0.5f));
        var floatingVerticalSupport = new Bounds(
            new Vector3(4f, 3.34f, -3f),
            new Vector3(0.3f, 0.2f, 0.3f));

        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.CoversRetainedRooftopProp(
                propBounds,
                attachedPlatform),
            Is.True);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.CoversRetainedRooftopProp(
                propBounds,
                detachedPlatform),
            Is.False);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.CoversRetainedRooftopProp(
                propBounds,
                undersizedPlatform),
            Is.False);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.ConnectsRetainedRooftopPlatformToAttachment(
                attachedPlatform,
                connectedVerticalSupport,
                attachmentAnchor),
            Is.True);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.ConnectsRetainedRooftopPlatformToAttachment(
                attachedPlatform,
                floatingVerticalSupport,
                attachmentAnchor),
            Is.False);
    }

    [Test]
    public void RetainedRooftopSupportPlan_ProducesAttachedGroundConnectedAssembly()
    {
        var propBounds = new Bounds(new Vector3(4f, 6f, -3f), new Vector3(2f, 4f, 2f));
        var anchor = new DenseMiddleEasternCityEditModeBuilder.RetainedRooftopPropAnchor(
            "SM_Bld_WaterTank_01[42]",
            Matrix4x4.identity,
            propBounds,
            new Bounds(new Vector3(1.5f, 3f, -3f), new Vector3(3f, 6f, 6f)),
            new Bounds(Vector3.zero, new Vector3(3f, 6f, 6f)),
            Matrix4x4.TRS(
                new Vector3(1.5f, 3f, -3f),
                Quaternion.identity,
                Vector3.one));
        var platformPrefabBounds = new Bounds(
            new Vector3(0f, 0.6015f, 0f),
            new Vector3(10.436f, 1.203f, 10.436f));
        var supportPrefabBounds = new Bounds(
            new Vector3(0f, -2.3745f, 0f),
            new Vector3(4.374f, 4.749f, 1.705f));

        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.TryPlanRetainedRooftopSupportAssembly(
                anchor,
                0f,
                platformPrefabBounds,
                supportPrefabBounds,
                out DenseMiddleEasternCityEditModeBuilder.RetainedRooftopSupportPlan plan,
                out string error),
            Is.True,
            error);

        Bounds platformBounds = TransformBounds(
            platformPrefabBounds,
            plan.PlatformWorldMatrix);
        Bounds supportBounds = TransformBounds(
            supportPrefabBounds,
            plan.VerticalSupportWorldMatrix);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.CoversRetainedRooftopProp(
                propBounds,
                platformBounds),
            Is.True);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.ConnectsRetainedRooftopPlatformToAttachment(
                platformBounds,
                supportBounds,
                anchor),
            Is.True);
    }

    [Test]
    public void RetainedRooftopSupportPlan_RotatedFacadeAndRooftopRemainAttachedAndDeterministic()
    {
        var platformPrefabBounds = new Bounds(
            new Vector3(0f, 0.6015f, 0f),
            new Vector3(10.436f, 1.203f, 10.436f));
        var supportPrefabBounds = new Bounds(
            new Vector3(0f, -2.3745f, 0f),
            new Vector3(2.193f, 7.679f, 0.56f));
        Matrix4x4 attachmentMatrix = Matrix4x4.TRS(
            new Vector3(4f, 3f, -3f),
            Quaternion.Euler(0f, 35f, 0f),
            Vector3.one);
        var attachmentLocalBounds = new Bounds(Vector3.zero, new Vector3(4f, 6f, 5f));
        Bounds attachmentWorldBounds = TransformBounds(
            attachmentLocalBounds,
            attachmentMatrix);
        var facadeAnchor = new DenseMiddleEasternCityEditModeBuilder.RetainedRooftopPropAnchor(
            "facade",
            Matrix4x4.identity,
            new Bounds(new Vector3(7.2f, 6f, -3f), new Vector3(2f, 4f, 2f)),
            attachmentWorldBounds,
            attachmentLocalBounds,
            attachmentMatrix);

        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.TryPlanRetainedRooftopSupportAssembly(
                facadeAnchor,
                0f,
                platformPrefabBounds,
                supportPrefabBounds,
                out DenseMiddleEasternCityEditModeBuilder.RetainedRooftopSupportPlan firstFacade,
                out string facadeError),
            Is.True,
            facadeError);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.TryPlanRetainedRooftopSupportAssembly(
                facadeAnchor,
                0f,
                platformPrefabBounds,
                supportPrefabBounds,
                out DenseMiddleEasternCityEditModeBuilder.RetainedRooftopSupportPlan secondFacade,
                out _),
            Is.True);
        AssertMatricesEqual(firstFacade.PlatformWorldMatrix, secondFacade.PlatformWorldMatrix);
        AssertMatricesEqual(
            firstFacade.VerticalSupportWorldMatrix,
            secondFacade.VerticalSupportWorldMatrix);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.ConnectsRetainedRooftopPlatformToAttachment(
                TransformBounds(platformPrefabBounds, firstFacade.PlatformWorldMatrix),
                TransformBounds(supportPrefabBounds, firstFacade.VerticalSupportWorldMatrix),
                facadeAnchor),
            Is.True);

        var rooftopAnchor = new DenseMiddleEasternCityEditModeBuilder.RetainedRooftopPropAnchor(
            "rooftop",
            Matrix4x4.identity,
            new Bounds(new Vector3(4f, 7f, -3f), new Vector3(2f, 2f, 2f)),
            attachmentWorldBounds,
            attachmentLocalBounds,
            attachmentMatrix);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.TryPlanRetainedRooftopSupportAssembly(
                rooftopAnchor,
                0f,
                platformPrefabBounds,
                supportPrefabBounds,
                out DenseMiddleEasternCityEditModeBuilder.RetainedRooftopSupportPlan rooftopPlan,
                out string rooftopError),
            Is.True,
            rooftopError);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.ConnectsRetainedRooftopPlatformToAttachment(
                TransformBounds(platformPrefabBounds, rooftopPlan.PlatformWorldMatrix),
                TransformBounds(supportPrefabBounds, rooftopPlan.VerticalSupportWorldMatrix),
                rooftopAnchor),
            Is.True);
    }

    [Test]
    public void DetachedBuildingAttachments_RemoveOnlyElevatedDisconnectedProps()
    {
        const float foundationHeight = 0.035f;
        var groundedProp = new Bounds(
            new Vector3(0f, 0.55f, 0f),
            new Vector3(1f, 1f, 1f));
        var elevatedProp = new Bounds(
            new Vector3(0f, 3f, 0f),
            new Vector3(1f, 1f, 1f));

        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.IsDetachedElevatedBuildingAttachment(
                groundedProp,
                foundationHeight,
                2f),
            Is.False);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.IsDetachedElevatedBuildingAttachment(
                elevatedProp,
                foundationHeight,
                0.2f),
            Is.False);
        Assert.That(
            DenseMiddleEasternCityEditModeBuilder.IsDetachedElevatedBuildingAttachment(
                elevatedProp,
                foundationHeight,
                1.35f),
            Is.True);
    }

    [Test]
    public void CanonicalPresentationCleanup_RemovesDetachedPropAndKeepsGroundedProp()
    {
        var root = new GameObject("GeneratedRoot");
        var building = new GameObject("DenseCityBuilding");
        building.transform.SetParent(root.transform, false);
        GameObject body = CreateBoundsRenderer(
            "Model",
            "SM_Bld_Test",
            building.transform,
            new Vector3(0f, 0.5f, 0f));
        GameObject grounded = CreateBoundsRenderer(
            "SM_Prop_Grounded",
            "SM_Prop_Grounded",
            building.transform,
            new Vector3(3f, 0.5f, 0f));
        GameObject detached = CreateBoundsRenderer(
            "SM_Prop_Detached",
            "SM_Prop_Detached",
            building.transform,
            new Vector3(3f, 3f, 0f));
        Mesh bodyMesh = body.GetComponent<MeshFilter>().sharedMesh;
        Mesh groundedMesh = grounded.GetComponent<MeshFilter>().sharedMesh;
        Mesh detachedMesh = detached.GetComponent<MeshFilter>().sharedMesh;
        try
        {
            Assert.That(
                DenseMiddleEasternCityEditModeBuilder
                    .RemoveDetachedElevatedBuildingAttachments(root.transform),
                Is.EqualTo(1));
            Assert.That(detached == null, Is.True);
            Assert.That(grounded == null, Is.False);
            Assert.That(
                DenseMiddleEasternCityEditModeBuilder
                    .CountDetachedElevatedBuildingAttachments(root.transform),
                Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(bodyMesh);
            UnityEngine.Object.DestroyImmediate(groundedMesh);
            UnityEngine.Object.DestroyImmediate(detachedMesh);
        }
    }

    [Test]
    public void CanonicalPresentationCleanup_ReplacesUnownedPrefabSignWithSemanticSign()
    {
        var root = new GameObject("GeneratedRoot");
        var building = new GameObject("DenseCityBuilding");
        building.transform.SetParent(root.transform, false);
        GameObject body = CreateBoundsRenderer(
            "Model",
            "SM_Bld_Test",
            building.transform,
            new Vector3(0f, 0.5f, 0f));
        GameObject unownedSign = CreateBoundsRenderer(
            "SM_Prop_Sign_Shop_04",
            "SM_Prop_Sign_Shop_04",
            building.transform,
            new Vector3(0f, 1f, 0f));
        GameObject semanticSign = CreateBoundsRenderer(
            "SM_Prop_Sign_Shop_04_ShopWall_0001",
            "SM_Prop_Sign_Shop_04",
            building.transform,
            new Vector3(0f, 1f, 0f));
        semanticSign.AddComponent<OperationMapBuildingAttachmentAuthoring>();
        Mesh bodyMesh = body.GetComponent<MeshFilter>().sharedMesh;
        Mesh unownedSignMesh = unownedSign.GetComponent<MeshFilter>().sharedMesh;
        Mesh semanticSignMesh = semanticSign.GetComponent<MeshFilter>().sharedMesh;
        try
        {
            Assert.That(
                DenseMiddleEasternCityEditModeBuilder
                    .RemoveDetachedElevatedBuildingAttachments(root.transform),
                Is.EqualTo(1));
            Assert.That(unownedSign == null, Is.True);
            Assert.That(semanticSign == null, Is.False);
            Assert.That(
                DenseMiddleEasternCityEditModeBuilder
                    .CountDetachedElevatedBuildingAttachments(root.transform),
                Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(bodyMesh);
            UnityEngine.Object.DestroyImmediate(unownedSignMesh);
            UnityEngine.Object.DestroyImmediate(semanticSignMesh);
        }
    }

    private static GameObject CreateBoundsRenderer(
        string objectName,
        string meshName,
        Transform parent,
        Vector3 localPosition)
    {
        var instance = new GameObject(objectName);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        var mesh = new Mesh { name = meshName };
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        instance.AddComponent<MeshFilter>().sharedMesh = mesh;
        instance.AddComponent<MeshRenderer>();
        return instance;
    }

    [Test]
    public void ProtectedPlacementConfig_RejectsAnyDenseGenerationMutation()
    {
        var config = ScriptableObject.CreateInstance<MapBuildingPlacementConfig>();
        config.EditorSetPlacements(new List<MapBuildingPlacementConfigEntry>());
        AssetDatabase.CreateAsset(config, PlacementConfigPath);
        AssetDatabase.SaveAssets();
        DenseCityCandidateAuthoringTransaction.ProtectedPlacementConfigSnapshot snapshot =
            DenseCityCandidateAuthoringTransaction.CaptureProtectedPlacementConfig(
                PlacementConfigPath);

        Assert.That(
            DenseCityCandidateAuthoringTransaction.TryValidateProtectedPlacementConfig(
                snapshot,
                out string error),
            Is.True,
            error);

        config.EditorSetPlacements(new List<MapBuildingPlacementConfigEntry>
        {
            new(
                "generated/densecity",
                "Generated",
                null,
                0,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                Vector3.one,
                0f,
                false)
        });
        AssetDatabase.SaveAssets();

        Assert.That(
            DenseCityCandidateAuthoringTransaction.TryValidateProtectedPlacementConfig(
                snapshot,
                out error),
            Is.False);
        Assert.That(error, Does.Contain("changed the protected building-placement config"));
        Assert.That(error, Does.Contain("count=1/0"));
    }

    [Test]
    public void ProxyFailure_RestoresAcceptedOutputAndRemovesPartialReplacement()
    {
        string proxyFolder = TempRoot + "/SurfaceProxies";
        string acceptedAsset = proxyFolder + "/accepted-proxy.txt";
        string partialAsset = proxyFolder + "/partial-proxy.txt";
        EnsureFolder(proxyFolder);
        File.WriteAllText(ToPhysicalPath(acceptedAsset), "accepted-proxy-bytes");
        AssetDatabase.ImportAsset(acceptedAsset, ImportAssetOptions.ForceSynchronousImport);
        string acceptedFolderGuid = AssetDatabase.AssetPathToGUID(proxyFolder);
        string acceptedAssetGuid = AssetDatabase.AssetPathToGUID(acceptedAsset);
        byte[] acceptedBytes = File.ReadAllBytes(ToPhysicalPath(acceptedAsset));

        string backupFolder =
            DenseCityCandidateAuthoringTransaction.MoveAssetFolderAside(proxyFolder);
        try
        {
            EnsureFolder(proxyFolder);
            File.WriteAllText(ToPhysicalPath(partialAsset), "partial-proxy-bytes");
            AssetDatabase.ImportAsset(
                partialAsset,
                ImportAssetOptions.ForceSynchronousImport);
            throw new InvalidOperationException("Injected proxy generation failure.");
        }
        catch (InvalidOperationException exception)
        {
            Assert.That(exception.Message, Does.Contain("Injected proxy generation failure"));
            DenseCityCandidateAuthoringTransaction.RestoreAssetFolder(
                backupFolder,
                proxyFolder);
            backupFolder = null;
        }

        Assert.That(AssetDatabase.IsValidFolder(proxyFolder), Is.True);
        Assert.That(
            AssetDatabase.AssetPathToGUID(proxyFolder),
            Is.EqualTo(acceptedFolderGuid));
        Assert.That(File.Exists(ToPhysicalPath(acceptedAsset)), Is.True);
        Assert.That(
            AssetDatabase.AssetPathToGUID(acceptedAsset),
            Is.EqualTo(acceptedAssetGuid));
        Assert.That(
            File.ReadAllBytes(ToPhysicalPath(acceptedAsset)),
            Is.EqualTo(acceptedBytes));
        Assert.That(File.Exists(ToPhysicalPath(partialAsset)), Is.False);
        Assert.That(
            AssetDatabase.IsValidFolder(proxyFolder + "__TransactionBackup"),
            Is.False);
    }

    [Test]
    public void ProtectedCandidateAssets_ReopenWithExactSemanticOwnership()
    {
        Assert.That(
            File.Exists(ToPhysicalPath(
                DenseCityCandidateAuthoringTransaction.CandidateMapScenePath)),
            Is.True);
        Assert.That(
            File.Exists(ToPhysicalPath(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath)),
            Is.True);

        Scene mapScene = EditorSceneManager.OpenScene(
            DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
            OpenSceneMode.Additive);
        Scene entityScene = EditorSceneManager.OpenScene(
            DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
            OpenSceneMode.Additive);
        try
        {
            DenseCityGeneratedRootAuthoring mapRoot =
                mapScene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                    .Single();
            Assert.That(
                DenseCitySemanticHierarchyBuilder.TryValidate(
                    mapScene,
                    entityScene,
                    mapRoot.GenerationId,
                    out string error),
                Is.True,
                error);
            Assert.That(
                DenseCityBakeReadinessValidator.TryResolveGenerationState(
                    mapScene,
                    entityScene,
                    out bool generated,
                    out string generationId,
                    out error),
                Is.True,
                error);
            Assert.That(generated, Is.True);
            Assert.That(generationId, Is.EqualTo(mapRoot.GenerationId));
            Assert.That(
                mapRoot.GetComponentsInChildren<MapBakeGroupAuthoring>(true),
                Has.Length.EqualTo(5));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static void CreateScene(string path, string rootName)
    {
        Scene scene =
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.MoveGameObjectToScene(new GameObject(rootName), scene);
        Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static string ToPhysicalPath(string assetPath) =>
        Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
            assetPath));

    private static Bounds TransformBounds(Bounds localBounds, Matrix4x4 matrix)
    {
        Vector3 center = matrix.MultiplyPoint3x4(localBounds.center);
        Vector3 extents = localBounds.extents;
        Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
        var worldExtents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
        return new Bounds(center, worldExtents * 2f);
    }

    private static void AssertMatricesEqual(Matrix4x4 expected, Matrix4x4 actual)
    {
        for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
        {
            Assert.That(
                actual[row, column],
                Is.EqualTo(expected[row, column]).Within(0.00001f),
                $"matrix[{row},{column}]");
        }
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string segment in path.Substring("Assets/".Length).Split('/'))
        {
            string next = current + "/" + segment;
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segment);
            current = next;
        }
    }
}
