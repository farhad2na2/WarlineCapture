using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class OperationMapEntitySceneCandidateBakeAllTests
{
    private const string DenseCandidateGeneratedRoot =
        "Assets/Game/GeneratedOperationMaps/DenseCity/" +
        "opmap.skirmish.desert_base_01/Candidate";
    private const string LegacyDenseRoadOutputRoot =
        "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/" +
        "opmap/skirmish/dense_city_roads";
    private const string IdentityDeltaReportPath =
        "Design/AgentReports/2026-07-25_dense_city_fresh_identity_delta.json";

    private static readonly string[] TwoRunCandidateFiles =
    {
        DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
        DenseCityCandidateAuthoringTransaction.CandidateMapScenePath + ".meta",
        DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
        DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath + ".meta",
        OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
        OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath + ".meta",
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath + ".meta",
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath + ".meta",
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateDefinitionPath,
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateDefinitionPath + ".meta",
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath,
        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath + ".meta",
        DenseCandidateGeneratedRoot + ".meta",
        LegacyDenseRoadOutputRoot + ".meta"
    };

    private static readonly string[] TwoRunCandidateDirectories =
    {
        DenseCandidateGeneratedRoot,
        LegacyDenseRoadOutputRoot
    };

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
            suite.LayoutBudget_RejectsExplicitSharedDependencyOwnership,
            suite.LayoutBudget_AcceptsEntitySceneOnlyOwnership,
            suite.SceneSetup_RejectsEmptyBatchSetup,
            suite.SceneSetup_AcceptsLoadedActiveScene,
            suite.CandidateBakeAll_ValidatesSourcePhysicsBeforePopulation,
            suite.CandidateBakeAll_ValidatesRuntimePhysicsAfterBindingBeforeBudget,
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

    public static void RunTwoRunNoOpValidation()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        using (CandidateOutputCheckpoint checkpoint = CandidateOutputCheckpoint.Capture(
                   root,
                   TwoRunCandidateFiles,
                   TwoRunCandidateDirectories))
        {
            RunCompleteDenseCandidateBake("first");
            string firstFingerprint = ComputeCandidateOutputFingerprint(root);

            RunCompleteDenseCandidateBake("second");
            string secondFingerprint = ComputeCandidateOutputFingerprint(root);

            if (!string.Equals(firstFingerprint, secondFingerprint, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Dense-city generation plus complete Candidate Bake All changed stable " +
                    $"candidate outputs on its second run. first={firstFingerprint} " +
                    $"second={secondFingerprint}");
            }

            checkpoint.Commit();
            Debug.Log(
                "[DenseCityCandidateTwoRunNoOpValidation] result=Passed " +
                $"fingerprint={secondFingerprint}");
        }
    }

    public static void RunFreshIdentityDeltaValidation()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        Dictionary<string, IdentitySnapshotEntry> accepted =
            CaptureDenseIdentitySnapshot(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
        CandidateIdentityDeltaReport report;

        using (CandidateOutputCheckpoint checkpoint = CandidateOutputCheckpoint.Capture(
                   root,
                   TwoRunCandidateFiles,
                   TwoRunCandidateDirectories))
        {
            if (!DenseCityCandidateAuthoringTransaction.TryRealizeCandidate(
                    out string summary,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Fresh dense-city identity diagnostic could not realize the candidate: {error}");
            }

            Dictionary<string, IdentitySnapshotEntry> fresh =
                CaptureDenseIdentitySnapshot(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            report = CreateIdentityDeltaReport(accepted, fresh, summary);
        }

        WriteIdentityDeltaReport(root, report);
        Debug.Log(
            "[DenseCityFreshIdentityDeltaValidation] result=Passed " +
            $"accepted={report.acceptedCount} fresh={report.freshCount} " +
            $"acceptedOnly={report.acceptedOnlyCount} freshOnly={report.freshOnlyCount} " +
            $"report={IdentityDeltaReportPath}");
    }

    public static void RunFailureRollbackValidation()
    {
        DenseCityCandidateAuthoringTransactionTests.RunProxyFailureRollbackValidation();
        DenseCityInfrastructurePlacementTransactionTests.RunSurfaceFailureRollbackValidation();

        var presentation = new DenseCityPresentationReplayTransactionTests();
        presentation.SetUp();
        try
        {
            presentation.Realize_LateFailurePreservesAcceptedAndRollsBackNewPresentationSet();
        }
        finally
        {
            presentation.TearDown();
        }

        var readiness = new OperationMapEntityPresentationReadinessValidatorTests();
        readiness.SetUp();
        readiness.ReadinessFailure_DoesNotMutateAcceptedHierarchy();

        var budget = new DenseCityPresentationBudgetValidatorTests();
        budget.BudgetFailure_RestoresAcceptedCandidateOutput();

        Debug.Log(
            "[DenseCityBakeAllFailureRollbackValidation] result=Passed categories=5");
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
                    0,
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
                    0,
                    0,
                    0,
                    0,
                    0)),
            Throws.Nothing);
    }

    [Test]
    public void LayoutBudget_RejectsExplicitSharedDependencyOwnership()
    {
        Assert.That(
            () => OperationMapEntitySceneCandidateBakeAll.RequireLayoutBudget(
                new OperationMapEntitySceneCandidateBakeAll.CandidateLayoutBudget(
                    "CandidateEntitySceneAddressablesLayoutReady",
                    1,
                    0,
                    0,
                    0,
                    0)),
            Throws.InvalidOperationException.With.Message.Contains("explicit shared-dependency"));
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
    public void CandidateBakeAll_ValidatesRuntimePhysicsAfterBindingBeforeBudget()
    {
        const string path =
            "Assets/Game/Scripts/Editor/OperationMapEntitySceneCandidateBakeAll.cs";
        string source = File.ReadAllText(path);
        int binding = source.IndexOf(
            "RunStage(report, \"candidate-binding-layout\"",
            StringComparison.Ordinal);
        int runtimePhysics = source.IndexOf(
            "\"runtime-physics-readiness\"",
            StringComparison.Ordinal);
        int bakeBudget = source.IndexOf(
            "RunStage(report, \"candidate-bake-budget\"",
            StringComparison.Ordinal);

        Assert.That(binding, Is.GreaterThanOrEqualTo(0));
        Assert.That(runtimePhysics, Is.GreaterThan(binding));
        Assert.That(bakeBudget, Is.GreaterThan(runtimePhysics));
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

    private static void RunCompleteDenseCandidateBake(string pass)
    {
        if (!DenseCityCandidateAuthoringTransaction.TryRealizeCandidate(
                out string summary,
                out string error))
        {
            throw new InvalidOperationException(
                $"Dense-city candidate realization failed during {pass} pass: {error}");
        }

        Debug.Log(
            $"[DenseCityCandidateTwoRunNoOpValidation] pass={pass} stage=realize {summary}");
        OperationMapEntityPresentationCandidateBakeValidator.BakeAndValidateDenseCityCandidate();
        OperationMapEntitySceneCandidateAddressablesLayoutBuilder
            .BuildDenseCityCandidateEntitySceneAddressablesLayout();
        OperationMapEntitySceneCandidateBakeAll.BakeAllCandidateEntityScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static Dictionary<string, IdentitySnapshotEntry> CaptureDenseIdentitySnapshot(
        string scenePath)
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        UnityEngine.SceneManagement.Scene scene = default;
        try
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            var result = new Dictionary<string, IdentitySnapshotEntry>(
                StringComparer.Ordinal);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                DenseCityPresentationIdentityAuthoring[] identities =
                    roots[rootIndex]
                        .GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true);
                for (int identityIndex = 0; identityIndex < identities.Length; identityIndex++)
                {
                    DenseCityPresentationIdentityAuthoring identity =
                        identities[identityIndex];
                    if (!identity.TryValidate(out string error))
                    {
                        throw new InvalidOperationException(
                            $"Dense-city identity snapshot rejected '{identity.name}': {error}");
                    }

                    string stableId = identity.StableId;
                    if (!result.TryAdd(
                            stableId,
                            new IdentitySnapshotEntry(
                                stableId,
                                identity.Role.ToString(),
                                identity.Category.ToString(),
                                identity.name,
                                GetHierarchyPath(identity.transform),
                                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                                    identity.gameObject))))
                    {
                        throw new InvalidOperationException(
                            $"Dense-city identity snapshot contains duplicate stable id '{stableId}'.");
                    }
                }
            }

            return result;
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
            if (OperationMapEntitySceneCandidateBakeAll.HasRestorableSceneSetup(previousSetup))
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static CandidateIdentityDeltaReport CreateIdentityDeltaReport(
        IReadOnlyDictionary<string, IdentitySnapshotEntry> accepted,
        IReadOnlyDictionary<string, IdentitySnapshotEntry> fresh,
        string realizationSummary)
    {
        List<IdentitySnapshotEntry> acceptedOnly = accepted
            .Where(pair => !fresh.ContainsKey(pair.Key))
            .Select(pair => pair.Value)
            .OrderBy(entry => entry.stableId, StringComparer.Ordinal)
            .ToList();
        List<IdentitySnapshotEntry> freshOnly = fresh
            .Where(pair => !accepted.ContainsKey(pair.Key))
            .Select(pair => pair.Value)
            .OrderBy(entry => entry.stableId, StringComparer.Ordinal)
            .ToList();
        return new CandidateIdentityDeltaReport
        {
            schema = "warline.dense-city.fresh-identity-delta",
            schemaVersion = 1,
            result = "FreshIdentityDeltaCaptured",
            acceptedCount = accepted.Count,
            freshCount = fresh.Count,
            sharedCount = accepted.Keys.Count(fresh.ContainsKey),
            acceptedOnlyCount = acceptedOnly.Count,
            freshOnlyCount = freshOnly.Count,
            realizationSummary = realizationSummary,
            acceptedOnlyGroups = CreateIdentityGroups(acceptedOnly),
            freshOnlyGroups = CreateIdentityGroups(freshOnly),
            acceptedOnly = acceptedOnly,
            freshOnly = freshOnly
        };
    }

    private static List<IdentityDeltaGroup> CreateIdentityGroups(
        IEnumerable<IdentitySnapshotEntry> entries) =>
        entries
            .GroupBy(
                entry => entry.category + "|" +
                         (string.IsNullOrEmpty(entry.prefabPath)
                             ? "<no-prefab>"
                             : entry.prefabPath),
                StringComparer.Ordinal)
            .Select(group => new IdentityDeltaGroup
            {
                category = group.First().category,
                prefabPath = group.First().prefabPath,
                count = group.Count()
            })
            .OrderByDescending(group => group.count)
            .ThenBy(group => group.category, StringComparer.Ordinal)
            .ThenBy(group => group.prefabPath, StringComparer.Ordinal)
            .ToList();

    private static void WriteIdentityDeltaReport(
        string root,
        CandidateIdentityDeltaReport report)
    {
        string physicalPath = ResolveProjectPath(root, IdentityDeltaReportPath);
        string parent = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        File.WriteAllText(
            physicalPath,
            JsonUtility.ToJson(report, true) + Environment.NewLine,
            new UTF8Encoding(false));
        AssetDatabase.ImportAsset(
            IdentityDeltaReportPath,
            ImportAssetOptions.ForceSynchronousImport);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var segments = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", segments);
    }

    private static string ComputeCandidateOutputFingerprint(string root)
    {
        var manifest = new StringBuilder();
        foreach (string path in TwoRunCandidateFiles.OrderBy(
                     value => value,
                     StringComparer.Ordinal))
        {
            AppendFileFingerprint(root, path, manifest);
        }

        foreach (string directory in TwoRunCandidateDirectories.OrderBy(
                     value => value,
                     StringComparer.Ordinal))
        {
            string physicalDirectory = ResolveProjectPath(root, directory);
            if (!Directory.Exists(physicalDirectory))
            {
                manifest.Append("directory-missing|").Append(directory).Append('\n');
                continue;
            }

            manifest.Append("directory|").Append(directory).Append('\n');
            string[] files = Directory.GetFiles(
                physicalDirectory,
                "*",
                SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
            {
                string relative = directory + "/" +
                                  files[i].Substring(physicalDirectory.Length)
                                      .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                      .Replace('\\', '/');
                AppendFileFingerprint(root, relative, manifest);
            }
        }

        return ComputeSha256(Encoding.UTF8.GetBytes(manifest.ToString()));
    }

    private static void AppendFileFingerprint(
        string root,
        string relativePath,
        StringBuilder manifest)
    {
        string physicalPath = ResolveProjectPath(root, relativePath);
        if (!File.Exists(physicalPath))
        {
            manifest.Append("file-missing|").Append(relativePath).Append('\n');
            return;
        }

        manifest.Append("file|")
            .Append(relativePath)
            .Append('|')
            .Append(ComputeFileSha256(physicalPath))
            .Append('\n');
    }

    private static string ComputeFileSha256(string path)
    {
        using (SHA256 sha256 = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            return FormatSha256(sha256.ComputeHash(stream));
        }
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return FormatSha256(sha256.ComputeHash(bytes));
        }
    }

    private static string FormatSha256(byte[] hash) =>
        BitConverter.ToString(hash)
            .Replace("-", string.Empty)
            .ToLowerInvariant();

    private static string ResolveProjectPath(string root, string relativePath) =>
        Path.GetFullPath(Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private sealed class CandidateOutputCheckpoint : IDisposable
    {
        private readonly string root;
        private readonly string backupRoot;
        private readonly List<FileCheckpoint> files;
        private readonly List<DirectoryCheckpoint> directories;
        private bool committed;

        private CandidateOutputCheckpoint(
            string root,
            string backupRoot,
            List<FileCheckpoint> files,
            List<DirectoryCheckpoint> directories)
        {
            this.root = root;
            this.backupRoot = backupRoot;
            this.files = files;
            this.directories = directories;
        }

        internal static CandidateOutputCheckpoint Capture(
            string root,
            IEnumerable<string> filePaths,
            IEnumerable<string> directoryPaths)
        {
            string backupRoot = Path.Combine(
                root,
                "Temp",
                "DenseCityCandidateTwoRunNoOp",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backupRoot);
            var files = new List<FileCheckpoint>();
            var directories = new List<DirectoryCheckpoint>();

            int index = 0;
            foreach (string relativePath in filePaths.Distinct(StringComparer.Ordinal))
            {
                string physicalPath = ResolveProjectPath(root, relativePath);
                bool existed = File.Exists(physicalPath);
                string backupPath = Path.Combine(backupRoot, $"file-{index++}.bin");
                if (existed)
                    File.Copy(physicalPath, backupPath, true);
                files.Add(new FileCheckpoint(relativePath, existed, backupPath));
            }

            index = 0;
            foreach (string relativePath in directoryPaths.Distinct(StringComparer.Ordinal))
            {
                string physicalPath = ResolveProjectPath(root, relativePath);
                bool existed = Directory.Exists(physicalPath);
                string backupPath = Path.Combine(backupRoot, $"directory-{index++}");
                if (existed)
                    CopyDirectory(physicalPath, backupPath);
                directories.Add(new DirectoryCheckpoint(relativePath, existed, backupPath));
            }

            return new CandidateOutputCheckpoint(root, backupRoot, files, directories);
        }

        internal void Commit()
        {
            committed = true;
        }

        public void Dispose()
        {
            try
            {
                if (!committed)
                    Restore();
            }
            finally
            {
                if (Directory.Exists(backupRoot))
                    Directory.Delete(backupRoot, true);
            }
        }

        private void Restore()
        {
            for (int i = 0; i < directories.Count; i++)
            {
                DirectoryCheckpoint checkpoint = directories[i];
                string target = ResolveProjectPath(root, checkpoint.RelativePath);
                if (Directory.Exists(target))
                    Directory.Delete(target, true);
                if (checkpoint.Existed)
                    CopyDirectory(checkpoint.BackupPath, target);
            }

            for (int i = 0; i < files.Count; i++)
            {
                FileCheckpoint checkpoint = files[i];
                string target = ResolveProjectPath(root, checkpoint.RelativePath);
                if (checkpoint.Existed)
                {
                    string parent = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(parent))
                        Directory.CreateDirectory(parent);
                    File.Copy(checkpoint.BackupPath, target, true);
                }
                else if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            string[] directories = Directory.GetDirectories(
                source,
                "*",
                SearchOption.AllDirectories);
            Array.Sort(directories, StringComparer.Ordinal);
            for (int i = 0; i < directories.Length; i++)
            {
                string relative = directories[i].Substring(source.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }

            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
            {
                string relative = files[i].Substring(source.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string target = Path.Combine(destination, relative);
                string parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                    Directory.CreateDirectory(parent);
                File.Copy(files[i], target, true);
            }
        }

        private readonly struct FileCheckpoint
        {
            internal FileCheckpoint(string relativePath, bool existed, string backupPath)
            {
                RelativePath = relativePath;
                Existed = existed;
                BackupPath = backupPath;
            }

            internal string RelativePath { get; }
            internal bool Existed { get; }
            internal string BackupPath { get; }
        }

        private readonly struct DirectoryCheckpoint
        {
            internal DirectoryCheckpoint(string relativePath, bool existed, string backupPath)
            {
                RelativePath = relativePath;
                Existed = existed;
                BackupPath = backupPath;
            }

            internal string RelativePath { get; }
            internal bool Existed { get; }
            internal string BackupPath { get; }
        }
    }

    [Serializable]
    private sealed class CandidateIdentityDeltaReport
    {
        public string schema;
        public int schemaVersion;
        public string result;
        public int acceptedCount;
        public int freshCount;
        public int sharedCount;
        public int acceptedOnlyCount;
        public int freshOnlyCount;
        public string realizationSummary;
        public List<IdentityDeltaGroup> acceptedOnlyGroups;
        public List<IdentityDeltaGroup> freshOnlyGroups;
        public List<IdentitySnapshotEntry> acceptedOnly;
        public List<IdentitySnapshotEntry> freshOnly;
    }

    [Serializable]
    private sealed class IdentityDeltaGroup
    {
        public string category;
        public string prefabPath;
        public int count;
    }

    [Serializable]
    private sealed class IdentitySnapshotEntry
    {
        internal IdentitySnapshotEntry(
            string stableId,
            string role,
            string category,
            string objectName,
            string hierarchyPath,
            string prefabPath)
        {
            this.stableId = stableId;
            this.role = role;
            this.category = category;
            this.objectName = objectName;
            this.hierarchyPath = hierarchyPath;
            this.prefabPath = prefabPath;
        }

        public string stableId;
        public string role;
        public string category;
        public string objectName;
        public string hierarchyPath;
        public string prefabPath;
    }
}
