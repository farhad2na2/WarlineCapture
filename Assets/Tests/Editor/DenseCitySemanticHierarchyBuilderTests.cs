using System;
using UnityEditor;
using Game.Authoring;
using Game.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DenseCitySemanticHierarchyBuilderTests
{
    private const string TempRoot = "Assets/Tests/Editor/DenseCitySemanticHierarchyBuilderTemp";
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
    public void Create_BuildsAndValidatesExplicitTwoSceneOwnership()
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

            Assert.That(roots.MapBakeSource.gameObject.scene, Is.EqualTo(mapScene));
            Assert.That(roots.EntityPresentationSource.gameObject.scene, Is.EqualTo(entityScene));
            Assert.That(
                roots.MapBakeSource.transform.Find("BakeSources/Roads")
                    .GetComponent<MapBakeGroupAuthoring>().Role,
                Is.EqualTo(MapBakeGroupRole.Road));
            Assert.That(
                roots.EntityPresentationSource.transform.Find("GameplayBuildings/CivicAndMarket"),
                Is.Not.Null);
            Assert.That(
                DenseCitySemanticHierarchyBuilder.TryValidate(
                    mapScene,
                    entityScene,
                    "dense-city:test:42",
                    out string error),
                Is.True,
                error);
        }
        finally
        {
            EditorSceneManager.CloseScene(entityScene, true);
            EditorSceneManager.CloseScene(mapScene, true);
        }
    }

    [Test]
    public void Create_RejectsExistingMarkedGenerationRoot()
    {
        (Scene mapScene, Scene entityScene) = CreateScenePair();
        try
        {
            DenseCitySemanticHierarchyBuilder.Create(
                mapScene,
                entityScene,
                "dense-city:test:42",
                "dense-city-v1",
                1,
                42,
                Hash);

            Assert.That(
                () => DenseCitySemanticHierarchyBuilder.Create(
                    mapScene,
                    entityScene,
                    "dense-city:test:42",
                    "dense-city-v1",
                    1,
                    42,
                    Hash),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("transactional"));
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
        if (AssetDatabase.IsValidFolder(path))
            return;

        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}
