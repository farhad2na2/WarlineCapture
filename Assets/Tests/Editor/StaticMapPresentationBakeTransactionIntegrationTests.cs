using System;
using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class StaticMapPresentationBakeTransactionIntegrationTests
{
    private const string IntegrationScenePath =
        "Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_p999_p999.unity";

    [Test]
    public void Rollback_RestoresDeletedSceneBytesAndGuidAfterAssetDatabaseRefresh()
    {
        Assert.That(
            AssetDatabase.LoadMainAssetAtPath(IntegrationScenePath),
            Is.Null,
            $"Reserved integration-test path is already occupied: {IntegrationScenePath}");

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string sceneFilePath = Path.Combine(projectRoot, IntegrationScenePath);
        string sceneMetaPath = sceneFilePath + ".meta";
        Scene scene = default;
        try
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("StaticMapPresentationTransactionIntegrationTest");
            SceneManager.MoveGameObjectToScene(root, scene);
            Assert.That(EditorSceneManager.SaveScene(scene, IntegrationScenePath, true), Is.True);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene = default;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            byte[] expectedSceneBytes = File.ReadAllBytes(sceneFilePath);
            byte[] expectedMetaBytes = File.ReadAllBytes(sceneMetaPath);
            string expectedGuid = AssetDatabase.AssetPathToGUID(IntegrationScenePath);
            Assert.That(expectedGuid, Is.Not.Empty);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath), Is.Not.Null);

            Assert.Throws<InvalidOperationException>(() =>
            {
                using StaticMapPresentationBakeTransaction transaction =
                    StaticMapPresentationBakeTransaction.Begin(projectRoot, new[] { IntegrationScenePath });
                Assert.That(AssetDatabase.DeleteAsset(IntegrationScenePath), Is.True);
                throw new InvalidOperationException("Simulated bake failure after deleting an owned scene.");
            });

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Assert.That(File.ReadAllBytes(sceneFilePath), Is.EqualTo(expectedSceneBytes));
            Assert.That(File.ReadAllBytes(sceneMetaPath), Is.EqualTo(expectedMetaBytes));
            Assert.That(AssetDatabase.AssetPathToGUID(IntegrationScenePath), Is.EqualTo(expectedGuid));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationScenePath), Is.Not.Null);
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.DeleteAsset(IntegrationScenePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }
}
