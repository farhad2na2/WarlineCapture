using System;
using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class OperationMapEntitySceneCandidateBakeAllTests
{
    private string projectRoot;
    private string tempDirectory;

    public static void RunFocusedValidation()
    {
        var suite = new OperationMapEntitySceneCandidateBakeAllTests();
        Action[] tests =
        {
            suite.CandidateTransaction_RestoresExistingAndDeletesNewOutputs,
            suite.ProtectedProductionSnapshot_RejectsFileDrift,
            suite.BakeBudget_AcceptsCandidateBaseline,
            suite.BakeBudget_RejectsManagedVisualCompanions,
            suite.LayoutBudget_RejectsLegacyPlacementOwnership,
            suite.LayoutBudget_AcceptsEntitySceneOnlyOwnership,
            suite.SceneSetup_RejectsEmptyBatchSetup,
            suite.SceneSetup_AcceptsLoadedActiveScene
        };

        for (int i = 0; i < tests.Length; i++)
        {
            suite.SetUp();
            try
            {
                tests[i]();
            }
            finally
            {
                suite.TearDown();
            }
        }

        Debug.Log($"[OperationMapEntitySceneCandidateBakeAllValidation] result=Passed tests={tests.Length}");
    }

    [SetUp]
    public void SetUp()
    {
        projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        tempDirectory = Path.Combine(projectRoot, "Temp", "OperationMapEntitySceneCandidateBakeAllTests");
        Directory.CreateDirectory(tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDirectory))
            Directory.Delete(tempDirectory, true);
    }

    [Test]
    public void CandidateTransaction_RestoresExistingAndDeletesNewOutputs()
    {
        string existingRelative = "Temp/OperationMapEntitySceneCandidateBakeAllTests/existing.bin";
        string createdRelative = "Temp/OperationMapEntitySceneCandidateBakeAllTests/created.bin";
        string existing = Path.Combine(projectRoot, existingRelative);
        string created = Path.Combine(projectRoot, createdRelative);
        File.WriteAllText(existing, "before");

        OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction transaction =
            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                projectRoot,
                new[] { existingRelative, createdRelative });

        File.WriteAllText(existing, "after");
        File.WriteAllText(created, "new");
        transaction.Rollback();

        Assert.That(File.ReadAllText(existing), Is.EqualTo("before"));
        Assert.That(File.Exists(created), Is.False);
    }

    [Test]
    public void ProtectedProductionSnapshot_RejectsFileDrift()
    {
        string relative = "Temp/OperationMapEntitySceneCandidateBakeAllTests/protected.bin";
        string physical = Path.Combine(projectRoot, relative);
        File.WriteAllText(physical, "accepted");
        OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot snapshot =
            OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot.Capture(
                projectRoot,
                new[] { relative },
                Array.Empty<string>());

        File.WriteAllText(physical, "changed");

        Assert.That(
            () => snapshot.RequireUnchanged(),
            Throws.InvalidOperationException.With.Message.Contains("Protected production file changed"));
    }

    [Test]
    public void BakeBudget_AcceptsCandidateBaseline()
    {
        Assert.That(
            () => OperationMapEntitySceneCandidateBakeAll.RequireBakeBudget(
                new OperationMapEntitySceneCandidateBakeAll.CandidateBakeBudget(
                    "CandidateBakeValidationPassed",
                    432,
                    3,
                    13909,
                    0,
                    0)),
            Throws.Nothing);
    }

    [Test]
    public void BakeBudget_RejectsManagedVisualCompanions()
    {
        Assert.That(
            () => OperationMapEntitySceneCandidateBakeAll.RequireBakeBudget(
                new OperationMapEntitySceneCandidateBakeAll.CandidateBakeBudget(
                    "CandidateBakeValidationPassed",
                    432,
                    3,
                    13909,
                    0,
                    1)),
            Throws.InvalidOperationException);
    }

    [Test]
    public void LayoutBudget_RejectsLegacyPlacementOwnership()
    {
        Assert.That(
            () => OperationMapEntitySceneCandidateBakeAll.RequireLayoutBudget(
                new OperationMapEntitySceneCandidateBakeAll.CandidateLayoutBudget(
                    "CandidateEntitySceneAddressablesLayoutReady",
                    1841,
                    0,
                    0,
                    1,
                    0)),
            Throws.InvalidOperationException);
    }

    [Test]
    public void LayoutBudget_AcceptsEntitySceneOnlyOwnership()
    {
        Assert.That(
            () => OperationMapEntitySceneCandidateBakeAll.RequireLayoutBudget(
                new OperationMapEntitySceneCandidateBakeAll.CandidateLayoutBudget(
                    "CandidateEntitySceneAddressablesLayoutReady",
                    1841,
                    0,
                    0,
                    0,
                    0)),
            Throws.Nothing);
    }

    [Test]
    public void SceneSetup_RejectsEmptyBatchSetup()
    {
        Assert.That(
            OperationMapEntitySceneCandidateBakeAll.HasRestorableSceneSetup(Array.Empty<SceneSetup>()),
            Is.False);
    }

    [Test]
    public void SceneSetup_AcceptsLoadedActiveScene()
    {
        var setup = new[]
        {
            new SceneSetup
            {
                path = "Assets/TestScene.unity",
                isLoaded = true,
                isActive = true
            }
        };

        Assert.That(OperationMapEntitySceneCandidateBakeAll.HasRestorableSceneSetup(setup), Is.True);
    }
}
