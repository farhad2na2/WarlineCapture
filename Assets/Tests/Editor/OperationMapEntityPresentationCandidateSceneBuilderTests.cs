using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapEntityPresentationCandidateSceneBuilderTests
{
    private const string TempRoot = "Assets/Tests/Temp/OperationMapEntityPresentationCandidate";
    private const string SourcePath = TempRoot + "/accepted.unity";
    private const string CandidatePath = TempRoot + "/candidate.unity";
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(TempRoot);
        EnsureFolder(TempRoot);
        // Replace any untitled dirty editor scene so additive/temp fixtures can be created headlessly.
        Scene source = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.MoveGameObjectToScene(new GameObject("Grid"), source);
        SceneManager.MoveGameObjectToScene(new GameObject("InitialUnitsSpawnerAuthoring"), source);
        Assert.That(EditorSceneManager.SaveScene(source, SourcePath), Is.True);
        EditorSceneManager.CloseScene(source, true);
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(TempRoot);
    }

    [Test]
    public void Transaction_CopiesSourceAndCreatesExactProtectedHierarchy()
    {
        string sourceGuid = AssetDatabase.AssetPathToGUID(SourcePath);
        string sourceText = File.ReadAllText(ToPhysicalPath(SourcePath));

        Assert.That(
            OperationMapEntityPresentationCandidateSceneBuilder.TryCreateProtectedCandidateHierarchy(
                SourcePath,
                CandidatePath,
                "opmap.skirmish.candidate_test_01",
                Hash,
                new[] { SourcePath },
                out OperationMapEntityPresentationCandidateBuildResult result,
                out string rejectionReason),
            Is.True,
            rejectionReason);

        Assert.That(result.SourceSceneGuid, Is.EqualTo(sourceGuid));
        Assert.That(result.CandidateSceneGuid, Is.Not.EqualTo(sourceGuid));
        Assert.That(File.ReadAllText(ToPhysicalPath(SourcePath)), Is.EqualTo(sourceText));

        Scene candidate = EditorSceneManager.OpenScene(CandidatePath, OpenSceneMode.Additive);
        try
        {
            GameObject root = candidate.GetRootGameObjects().Single(owner =>
                owner.name == "AuthoredOperationMapEntityPresentation");
            Assert.That(root.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(root.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));

            AssertRole(root.transform.Find("GameplayBuildings"),
                OperationMapEntityPresentationRole.GameplayBuildings);
            AssertRole(root.transform.Find("GameplayVehicles"),
                OperationMapEntityPresentationRole.GameplayVehicles);
            AssertRole(root.transform.Find("RenderOnly"),
                OperationMapEntityPresentationRole.RenderOnly);

            Assert.That(root.transform.Find("GameplayBuildings/MilitaryBase"), Is.Not.Null);
            Assert.That(root.transform.Find("GameplayBuildings/HandmadeCity"), Is.Not.Null);
            Assert.That(root.transform.Find("GameplayBuildings/Infrastructure"), Is.Not.Null);
            Assert.That(root.transform.Find("RenderOnly/Terrain"), Is.Not.Null);
            Assert.That(root.transform.Find("RenderOnly/RoadsAndBridges"), Is.Not.Null);
            Assert.That(root.transform.Find("RenderOnly/Mountains"), Is.Not.Null);
            Assert.That(root.transform.Find("RenderOnly/Vegetation"), Is.Not.Null);
            Assert.That(root.transform.Find("RenderOnly/Props"), Is.Not.Null);
            Assert.That(root.transform.Find("RenderOnly/Infrastructure"), Is.Not.Null);
            Assert.That(root.transform.Find("RenderOnly/Horizon"), Is.Not.Null);
        }
        finally
        {
            EditorSceneManager.CloseScene(candidate, true);
        }
    }

    [Test]
    public void Transaction_RejectsExistingCandidateWithoutOverwritingIt()
    {
        Assert.That(AssetDatabase.CopyAsset(SourcePath, CandidatePath), Is.True);
        string before = File.ReadAllText(ToPhysicalPath(CandidatePath));

        Assert.That(
            OperationMapEntityPresentationCandidateSceneBuilder.TryCreateProtectedCandidateHierarchy(
                SourcePath,
                CandidatePath,
                "opmap.skirmish.candidate_test_01",
                Hash,
                new[] { SourcePath },
                out _,
                out string rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Is.EqualTo("candidate-already-exists"));
        Assert.That(File.ReadAllText(ToPhysicalPath(CandidatePath)), Is.EqualTo(before));
    }

    private static void AssertRole(Transform owner, OperationMapEntityPresentationRole expected)
    {
        Assert.That(owner, Is.Not.Null);
        Assert.That(owner.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(owner.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(owner.localScale, Is.EqualTo(Vector3.one));
        var marker = owner.GetComponent<OperationMapEntityPresentationRootAuthoring>();
        Assert.That(marker, Is.Not.Null);
        Assert.That(marker.Role, Is.EqualTo(expected));
        Assert.That(marker.MigrationRecordSetHash, Is.EqualTo(Hash));
        Assert.That(marker.TryValidate(out string error), Is.True, error);
    }

    private static string ToPhysicalPath(string assetPath) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));

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
