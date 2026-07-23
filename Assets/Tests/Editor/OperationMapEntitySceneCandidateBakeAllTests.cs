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
            suite.SceneSetup_AcceptsLoadedActiveScene,
            suite.CandidateBakeAll_ValidatesSourcePhysicsBeforePopulation,
            suite.CandidateBakeAll_PreservesFailureAndSceneRestorationOrdering,
            suite.SourceCandidateParity_AcceptsExactMatrixAndBounds,
            suite.SourceCandidateParity_RejectsMatrixDrift,
            suite.SourceCandidateParity_RejectsBoundsDrift
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

    [Test]
    public void CandidateBakeAll_ValidatesSourcePhysicsBeforePopulation()
    {
        const string path =
            "Assets/Game/Scripts/Editor/OperationMapEntitySceneCandidateBakeAll.cs";
        string source = File.ReadAllText(path);
        int preflight = source.IndexOf(
            "RunStage(report, \"preflight-isolation\"",
            StringComparison.Ordinal);
        int sourcePhysics = source.IndexOf(
            "RunStage(\n                    report,\n                    \"source-physics-readiness\"",
            StringComparison.Ordinal);
        int population = source.IndexOf(
            "RunStage(report, \"candidate-population\"",
            StringComparison.Ordinal);

        Assert.That(preflight, Is.GreaterThanOrEqualTo(0));
        Assert.That(sourcePhysics, Is.GreaterThan(preflight));
        Assert.That(population, Is.GreaterThan(sourcePhysics));
    }

    [Test]
    public void CandidateBakeAll_PreservesFailureAndSceneRestorationOrdering()
    {
        const string path =
            "Assets/Game/Scripts/Editor/OperationMapEntitySceneCandidateBakeAll.cs";
        string source = File.ReadAllText(path);
        int catchBlock = source.IndexOf("catch (Exception exception)", StringComparison.Ordinal);
        int invalidation = source.IndexOf(
            "DenseCityPresentationBudgetValidator.InvalidateEvidence",
            catchBlock,
            StringComparison.Ordinal);
        int rollback = source.IndexOf("transaction.Rollback()", catchBlock, StringComparison.Ordinal);
        int protectedCheck = source.IndexOf(
            "production.RequireUnchanged()",
            rollback,
            StringComparison.Ordinal);
        int failureReport = source.IndexOf(
            "WriteReport(projectRoot, report)",
            protectedCheck,
            StringComparison.Ordinal);
        int rethrow = source.IndexOf(
            "throw new InvalidOperationException",
            failureReport,
            StringComparison.Ordinal);

        Assert.That(catchBlock, Is.GreaterThanOrEqualTo(0));
        Assert.That(invalidation, Is.GreaterThan(catchBlock));
        Assert.That(rollback, Is.GreaterThan(invalidation));
        Assert.That(protectedCheck, Is.GreaterThan(rollback));
        Assert.That(failureReport, Is.GreaterThan(protectedCheck));
        Assert.That(rethrow, Is.GreaterThan(failureReport));
        Assert.That(
            Count(source, "RestoreSceneSetupOrCreateEmpty(previousSetup)"),
            Is.EqualTo(2));
    }

    [Test]
    public void SourceCandidateParity_AcceptsExactMatrixAndBounds()
    {
        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(4f, 2f, -3f), Quaternion.Euler(0f, 30f, 0f), Vector3.one);
        var bounds = new Bounds(new Vector3(4f, 3f, -3f), new Vector3(2f, 4f, 6f));
        Assert.That(
            OperationMapEntityPresentationTransformParityValidator.GetSourceCandidateRejectionReason(
                matrix, matrix, true, bounds, true, bounds),
            Is.Empty);
    }

    [Test]
    public void SourceCandidateParity_RejectsMatrixDrift()
    {
        Assert.That(
            OperationMapEntityPresentationTransformParityValidator.GetSourceCandidateRejectionReason(
                Matrix4x4.identity,
                Matrix4x4.Translate(Vector3.right),
                false,
                default,
                false,
                default),
            Is.EqualTo("owner-matrix-residual"));
    }

    [Test]
    public void SourceCandidateParity_RejectsBoundsDrift()
    {
        var source = new Bounds(Vector3.zero, Vector3.one);
        var candidate = new Bounds(Vector3.right, Vector3.one);
        Assert.That(
            OperationMapEntityPresentationTransformParityValidator.GetSourceCandidateRejectionReason(
                Matrix4x4.identity,
                Matrix4x4.identity,
                true,
                source,
                true,
                candidate),
            Is.EqualTo("renderer-bounds-residual"));
    }

    private static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
