using System;
using System.IO;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapSourceScenePhysicsValidatorTests
{
    private const string TempFolder =
        "Assets/Tests/Editor/OperationMapSourceScenePhysicsTemp";
    private const string AnchorScenePath = TempFolder + "/Anchor.unity";
    private const string TempScenePath = TempFolder + "/SourcePhysicsFixture.unity";
    private SceneSetup[] previousSetup;

    public static void RunFocusedValidation()
    {
        var tests = new OperationMapSourceScenePhysicsValidatorTests();
        Action[] cases =
        {
            tests.CleanLoadedScenePasses,
            tests.InactiveProhibitedPhysicsReportsSceneAndHierarchyPath,
            tests.RealAcceptedSourcesPassWithoutMutationAndRestoreSceneSetup
        };

        for (int index = 0; index < cases.Length; index++)
        {
            tests.SetUp();
            try
            {
                cases[index]();
            }
            finally
            {
                tests.TearDown();
            }
        }

        Debug.Log(
            $"[OperationMapSourceScenePhysicsValidation] result=Passed tests={cases.Length}");
        ValidationExit.Exit(0);
    }

    [SetUp]
    public void SetUp()
    {
        previousSetup = EditorSceneManager.GetSceneManagerSetup();
        EnsureFolder(TempFolder);
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || string.IsNullOrEmpty(active.path))
        {
            if (!active.IsValid())
                active = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(EditorSceneManager.SaveScene(active, AnchorScenePath), Is.True);
        }
    }

    [TearDown]
    public void TearDown()
    {
        Scene scene = SceneManager.GetSceneByPath(TempScenePath);
        if (scene.IsValid() && scene.isLoaded)
            EditorSceneManager.CloseScene(scene, true);

        if (previousSetup != null &&
            previousSetup.Length > 0 &&
            previousSetup.All(setup => !string.IsNullOrEmpty(setup.path)))
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
        else
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        AssetDatabase.DeleteAsset(TempFolder);
    }

    [Test]
    public void CleanLoadedScenePasses()
    {
        Scene scene = CreateFixtureScene();

        Assert.That(
            OperationMapSourceScenePhysicsValidator.TryValidateLoadedScene(
                scene,
                TempScenePath,
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void InactiveProhibitedPhysicsReportsSceneAndHierarchyPath()
    {
        Type[] prohibitedTypes =
        {
            typeof(BoxCollider),
            typeof(BoxCollider2D),
            typeof(Rigidbody),
            typeof(Rigidbody2D)
        };

        for (int index = 0; index < prohibitedTypes.Length; index++)
        {
            Scene scene = CreateFixtureScene();
            GameObject root = scene.GetRootGameObjects().Single();
            var child = new GameObject($"Inactive_{prohibitedTypes[index].Name}");
            child.transform.SetParent(root.transform, false);
            child.SetActive(false);
            child.AddComponent(prohibitedTypes[index]);

            Assert.That(
                OperationMapSourceScenePhysicsValidator.TryValidateLoadedScene(
                    scene,
                    TempScenePath,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain(TempScenePath));
            Assert.That(error, Does.Contain($"AcceptedRoot/{child.name}"));
            Assert.That(error, Does.Contain(prohibitedTypes[index].Name));

            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void RealAcceptedSourcesPassWithoutMutationAndRestoreSceneSetup()
    {
        SceneSetup[] beforeSetup = EditorSceneManager.GetSceneManagerSetup();
        byte[] operationMapBefore = File.ReadAllBytes(
            OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath);
        byte[] subSceneBefore = File.ReadAllBytes(
            OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);

        Assert.That(
            OperationMapSourceScenePhysicsValidator.TryValidateAcceptedSources(out string error),
            Is.True,
            error);

        Assert.That(
            EditorSceneManager.GetSceneManagerSetup().Select(ToSetupKey),
            Is.EqualTo(beforeSetup.Select(ToSetupKey)));
        Assert.That(
            File.ReadAllBytes(
                OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath),
            Is.EqualTo(operationMapBefore));
        Assert.That(
            File.ReadAllBytes(OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath),
            Is.EqualTo(subSceneBefore));
    }

    private static Scene CreateFixtureScene()
    {
        Scene loaded = SceneManager.GetSceneByPath(TempScenePath);
        if (loaded.IsValid() && loaded.isLoaded)
            EditorSceneManager.CloseScene(loaded, true);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var root = new GameObject("AcceptedRoot");
        SceneManager.MoveGameObjectToScene(root, scene);
        Assert.That(EditorSceneManager.SaveScene(scene, TempScenePath), Is.True);
        return scene;
    }

    private static string ToSetupKey(SceneSetup setup) =>
        $"{setup.path}|{setup.isLoaded}|{setup.isActive}|{setup.isSubScene}";

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }
}
