using System;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCityPresentationHierarchyContextTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCityPresentationHierarchyContextTemp";
    private const string MapScenePath = TempRoot + "/map.unity";
    private const string EntityScenePath = TempRoot + "/entity.unity";
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(TempRoot);
    }

    [Test]
    public void ResolveIndependentParent_RoutesEveryClosedCategoryToItsExplicitBucket()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            DenseCityPresentationHierarchyContext context =
                DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);

            Assert.That(
                context.ResolveIndependentParent(
                    DenseCityPresentationCategory.GameplayBuildingIntact,
                    GeneratedCityBuildingRole.House).name,
                Is.EqualTo("Buildings"));
            Assert.That(
                context.ResolveIndependentParent(
                    DenseCityPresentationCategory.GameplayBuildingDestroyed,
                    GeneratedCityBuildingRole.Civic).name,
                Is.EqualTo("CivicAndMarket"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Infrastructure).name,
                Is.EqualTo("Infrastructure"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Vegetation).name,
                Is.EqualTo("Vegetation"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Prop).name,
                Is.EqualTo("Props"));
            Assert.That(
                context.ResolveIndependentParent(DenseCityPresentationCategory.Horizon).name,
                Is.EqualTo("Horizon"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void ResolveIndependentParent_RejectsBuildingAttachments()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            DenseCityPresentationHierarchyContext context =
                DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource);

            Assert.That(
                () => context.ResolveIndependentParent(
                    DenseCityPresentationCategory.BuildingAttachmentIntact),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("declared building"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Create_RejectsNonIdentitySemanticBucket()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            var roots = DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);
            roots.EntityPresentationSource.transform.Find("RenderOnly/Props").localPosition = Vector3.right;

            Assert.That(
                () => DenseCityPresentationHierarchyContext.Create(roots.EntityPresentationSource),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("identity transforms"));
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    private static (Scene MapScene, Scene EntityScene) CreateScenePair()
    {
        Scene mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.That(EditorSceneManager.SaveScene(mapScene, MapScenePath), Is.True);
        Scene entityScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Assert.That(EditorSceneManager.SaveScene(entityScene, EntityScenePath), Is.True);
        return (mapScene, entityScene);
    }

    private static void EnsureFolder(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[index]);
            current = next;
        }
    }
}
