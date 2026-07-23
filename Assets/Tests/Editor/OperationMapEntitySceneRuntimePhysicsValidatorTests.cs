using System;
using System.IO;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapEntitySceneRuntimePhysicsValidatorTests
{
    private const string TempFolder =
        "Assets/Tests/Editor/OperationMapEntitySceneRuntimePhysicsTemp";
    private const string AnchorScenePath = TempFolder + "/Anchor.unity";
    private const string FixtureScenePath = TempFolder + "/RuntimePhysicsFixture.unity";
    private SceneSetup[] previousSetup;

    public static void RunFocusedValidation()
    {
        var tests = new OperationMapEntitySceneRuntimePhysicsValidatorTests();
        Action[] cases =
        {
            tests.InactiveProhibitedPhysicsReportsAssetHierarchyAndType,
            tests.CurrentCandidatePassesWithoutMutationAndRestoresSceneSetup
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
            $"[OperationMapEntitySceneRuntimePhysicsValidation] result=Passed tests={cases.Length}");
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
        Scene fixture = SceneManager.GetSceneByPath(FixtureScenePath);
        if (fixture.IsValid() && fixture.isLoaded)
            EditorSceneManager.CloseScene(fixture, true);

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
    public void InactiveProhibitedPhysicsReportsAssetHierarchyAndType()
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
                OperationMapEntitySceneRuntimePhysicsValidator.TryValidateSceneHierarchy(
                    scene,
                    FixtureScenePath,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain(FixtureScenePath));
            Assert.That(error, Does.Contain($"RuntimeRoot/{child.name}"));
            Assert.That(error, Does.Contain(prohibitedTypes[index].Name));

            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void CurrentCandidatePassesWithoutMutationAndRestoresSceneSetup()
    {
        string bindingPath =
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath;
        string subScenePath =
            OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
        SceneSetup[] beforeSetup = EditorSceneManager.GetSceneManagerSetup();
        byte[] bindingBefore = File.ReadAllBytes(bindingPath);
        byte[] subSceneBefore = File.ReadAllBytes(subScenePath);

        Assert.That(
            OperationMapEntitySceneRuntimePhysicsValidator.TryValidateCurrentCandidate(
                out string error),
            Is.True,
            error);

        Assert.That(
            EditorSceneManager.GetSceneManagerSetup().Select(ToSetupKey),
            Is.EqualTo(beforeSetup.Select(ToSetupKey)));
        Assert.That(File.ReadAllBytes(bindingPath), Is.EqualTo(bindingBefore));
        Assert.That(File.ReadAllBytes(subScenePath), Is.EqualTo(subSceneBefore));
    }

    private static Scene CreateFixtureScene()
    {
        Scene loaded = SceneManager.GetSceneByPath(FixtureScenePath);
        if (loaded.IsValid() && loaded.isLoaded)
            EditorSceneManager.CloseScene(loaded, true);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var root = new GameObject("RuntimeRoot");
        SceneManager.MoveGameObjectToScene(root, scene);
        Assert.That(EditorSceneManager.SaveScene(scene, FixtureScenePath), Is.True);
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
