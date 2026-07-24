using System.IO;
using System.Linq;
using Game.Authoring;
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
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

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
