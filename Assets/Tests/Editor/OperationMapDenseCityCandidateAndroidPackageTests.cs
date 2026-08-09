using System;
using System.IO;
using System.Linq;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapDenseCityCandidateAndroidPackageTests
{
    private const string DenseGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
    private const string ProductionGuid = "d50925a18e9164ce782536576cb833d8";

    public static void RunFocusedValidation()
    {
        var suite = new OperationMapDenseCityCandidateAndroidPackageTests();
        Action[] tests =
        {
            suite.EmbeddedLoadPath_UsesLocalAddressablesRuntimePath,
            suite.CreateFilePlan_MapsOnlyCandidateRuntimeContent,
            suite.ResolvePlayerScenes_AcceptsBaseScenes,
            suite.ResolvePlayerScenes_RejectsSourceAndStaticScenes,
            suite.PackageGate_AcceptsIsolatedDenseCandidate,
            suite.PackageGate_RejectsProductionEntityScene,
            suite.PackageGate_RejectsMissingCatalogOrBundles,
            suite.PackageGate_RejectsUnexpectedStaleCandidateBundle,
            suite.RuntimeSelectionContract_AcceptsExactCandidate,
            suite.RuntimeSelectionContract_RejectsWrongEntityScene,
            suite.RuntimeOverride_ProductionBuildResolvesValidatedCatalog,
            suite.RuntimeOverride_ProductionBuildRejectsUnknownMap,
            suite.DeploymentScope_ActivatesAndClearsAdditionalFiles,
            suite.DenseCandidateReleaseBuild_AlwaysCleansBuildCache,
            suite.DenseCandidateProfilerBuild_AlwaysCleansBuildCache
        };
        foreach (Action test in tests)
            test();

        Debug.Log(
            $"[DenseCityCandidateAndroidPackageValidation] result=Passed tests={tests.Length}");
    }

    [Test]
    public void EmbeddedLoadPath_UsesLocalAddressablesRuntimePath()
    {
        Assert.That(
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .EmbeddedAndroidAddressablesLoadPath,
            Is.EqualTo(
                "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" +
                "DenseCityCandidate/Android"));
        Assert.That(
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .EmbeddedAndroidAddressablesLoadPath,
            Does.Not.StartWith("http"));
        Assert.That(
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .EmbeddedAndroidAddressablesBuildPath,
            Is.EqualTo(
                "[UnityEngine.AddressableAssets.Addressables.BuildPath]/Android"));
    }

    [Test]
    public void CreateFilePlan_MapsOnlyCandidateRuntimeContent()
    {
        WithFixture(projectRoot =>
        {
            DenseCityCandidatePackageFile[] plan =
                OperationMapDenseCityCandidateAndroidPackageDeployment
                    .CreateFilePlan(projectRoot);

            Assert.That(
                plan.Select(file => file.DestinationPath),
                Is.EquivalentTo(new[]
                {
                    "aa/DenseCityCandidate/catalog.bin",
                    "aa/DenseCityCandidate/catalog.hash",
                    "aa/DenseCityCandidate/Android/candidate.bundle"
                }));
            Assert.That(
                plan.All(file => Path.IsPathRooted(file.SourcePath)),
                Is.True);
        });
    }

    [Test]
    public void ResolvePlayerScenes_AcceptsBaseScenes()
    {
        string[] scenes =
            OperationMapDenseCityCandidateAndroidPackageDeployment.ResolvePlayerScenes(
                new[] { "Assets/Game/Scenes/Menu.unity", "Assets/Game/Scenes/Match.unity" },
                _ => true);

        Assert.That(
            scenes,
            Is.EqualTo(new[]
            {
                "Assets/Game/Scenes/Menu.unity",
                "Assets/Game/Scenes/Match.unity"
            }));
    }

    [Test]
    public void ResolvePlayerScenes_RejectsSourceAndStaticScenes()
    {
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapDenseCityCandidateAndroidPackageDeployment.ResolvePlayerScenes(
                new[] { DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath },
                _ => true));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapDenseCityCandidateAndroidPackageDeployment.ResolvePlayerScenes(
                new[]
                {
                    StaticMapPresentationOutputPathContract.OperationMapsRoot +
                    "/opmap/skirmish/desert_base_01/Scenes/chunk.unity"
                },
                _ => true));
    }

    [Test]
    public void PackageGate_AcceptsIsolatedDenseCandidate()
    {
        string[] entries =
        {
            $"assets/EntityScenes/{DenseGuid}.entityheader",
            "assets/ContentArchives/archive_dependencies.bin",
            "assets/ContentArchives/archive_dependencies.txt",
            "assets/ContentArchives/b8ebb31853db87420b18072fd34a9579.archive",
            "assets/aa/DenseCityCandidate/catalog.bin",
            "assets/aa/DenseCityCandidate/catalog.hash",
            "assets/aa/DenseCityCandidate/Android/candidate.bundle"
        };
        string archiveCatalog =
            $"Archive: b8ebb31853db87420b18072fd34a9579\n" +
            $"\tObject: {DenseGuid}:0\n";

        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetPackageValidationError(
                    entries,
                    DenseGuid,
                    ProductionGuid,
                    archiveCatalog,
                    new[]
                    {
                        "aa/DenseCityCandidate/catalog.bin",
                        "aa/DenseCityCandidate/catalog.hash",
                        "aa/DenseCityCandidate/Android/candidate.bundle"
                    }),
            Is.Null);
    }

    [Test]
    public void PackageGate_RejectsProductionEntityScene()
    {
        string[] entries =
        {
            $"assets/EntityScenes/{DenseGuid}.entityheader",
            $"assets/EntityScenes/{ProductionGuid}.entityheader",
            "assets/ContentArchives/archive_dependencies.bin",
            "assets/ContentArchives/archive_dependencies.txt",
            "assets/ContentArchives/b8ebb31853db87420b18072fd34a9579",
            "assets/aa/DenseCityCandidate/catalog.bin",
            "assets/aa/DenseCityCandidate/Android/candidate.bundle"
        };
        string archiveCatalog =
            $"\tObject: {DenseGuid}:0\n\tObject: {ProductionGuid}:0\n";

        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetPackageValidationError(
                    entries,
                    DenseGuid,
                    ProductionGuid,
                    archiveCatalog),
            Does.Contain("production EntityScene"));
    }

    [Test]
    public void PackageGate_RejectsMissingCatalogOrBundles()
    {
        string[] entityEntries =
        {
            $"assets/EntityScenes/{DenseGuid}.entityheader",
            "assets/ContentArchives/archive_dependencies.bin",
            "assets/ContentArchives/archive_dependencies.txt",
            "assets/ContentArchives/b8ebb31853db87420b18072fd34a9579"
        };
        string archiveCatalog = $"\tObject: {DenseGuid}:0\n";
        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetPackageValidationError(
                    entityEntries,
                    DenseGuid,
                    ProductionGuid,
                    archiveCatalog),
            Does.Contain("catalog.bin"));

        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetPackageValidationError(
                    entityEntries.Append(
                            "assets/aa/DenseCityCandidate/catalog.bin")
                        .ToArray(),
                    DenseGuid,
                    ProductionGuid,
                    archiveCatalog),
            Does.Contain("no dense candidate Android bundles"));
    }

    [Test]
    public void PackageGate_RejectsUnexpectedStaleCandidateBundle()
    {
        string[] entries =
        {
            $"assets/EntityScenes/{DenseGuid}.entityheader",
            "assets/ContentArchives/archive_dependencies.bin",
            "assets/ContentArchives/archive_dependencies.txt",
            "assets/ContentArchives/b8ebb31853db87420b18072fd34a9579.archive",
            "assets/aa/DenseCityCandidate/catalog.bin",
            "assets/aa/DenseCityCandidate/catalog.hash",
            "assets/aa/DenseCityCandidate/Android/candidate.bundle",
            "assets/aa/DenseCityCandidate/Android/stale.bundle"
        };
        string archiveCatalog = $"\tObject: {DenseGuid}:0\n";

        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetPackageValidationError(
                    entries,
                    DenseGuid,
                    ProductionGuid,
                    archiveCatalog,
                    new[]
                    {
                        "aa/DenseCityCandidate/catalog.bin",
                        "aa/DenseCityCandidate/catalog.hash",
                        "aa/DenseCityCandidate/Android/candidate.bundle"
                    }),
            Does.Contain("unexpected stale dense candidate file"));
    }

    [Test]
    public void RuntimeSelectionContract_AcceptsExactCandidate()
    {
        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetRuntimeSelectionContractError(DenseGuid),
            Is.Null);
        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetRuntimeScriptingDefines(DenseGuid),
            Is.EqualTo(new[] { "WARLINE_DENSE_CITY_CANDIDATE" }));
    }

    [Test]
    public void RuntimeSelectionContract_RejectsWrongEntityScene()
    {
        Assert.That(
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetRuntimeSelectionContractError(ProductionGuid),
            Does.Contain("runtime EntityScene GUID"));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapDenseCityCandidateAndroidPackageDeployment
                .GetRuntimeScriptingDefines(ProductionGuid));
    }

    [Test]
    public void RuntimeOverride_ProductionBuildResolvesValidatedCatalog()
    {
        OperationMapCatalogConfig catalog =
            AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
                OperationMapAddressablesLayoutBuilder.CatalogPath);
        Assert.That(catalog, Is.Not.Null);
        Assert.That(
            OperationMapDenseCityCandidateRuntimeOverride.CandidateBuildEnabled,
            Is.False);
        Assert.That(
            catalog.TryResolve(
                OperationMapDenseCityCandidateRuntimeOverride.OperationMapId,
                out OperationMapDefinition expected),
            Is.True);

        using var runtimeOverride =
            new OperationMapDenseCityCandidateRuntimeOverride();
        Assert.That(
            runtimeOverride.TryResolve(
                catalog,
                OperationMapDenseCityCandidateRuntimeOverride.OperationMapId,
                out OperationMapDefinition actual,
                out bool waiting,
                out string error),
            Is.True,
            error);
        Assert.That(waiting, Is.False);
        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public void RuntimeOverride_ProductionBuildRejectsUnknownMap()
    {
        OperationMapCatalogConfig catalog =
            AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
                OperationMapAddressablesLayoutBuilder.CatalogPath);
        Assert.That(catalog, Is.Not.Null);

        using var runtimeOverride =
            new OperationMapDenseCityCandidateRuntimeOverride();
        Assert.That(
            runtimeOverride.TryResolve(
                catalog,
                "opmap.skirmish.unknown",
                out _,
                out bool waiting,
                out string error),
            Is.False);
        Assert.That(waiting, Is.False);
        Assert.That(error, Does.Contain("not present in the catalog"));
    }

    [Test]
    public void DeploymentScope_ActivatesAndClearsAdditionalFiles()
    {
        WithFixture(projectRoot =>
        {
            Assert.That(
                OperationMapDenseCityCandidateAndroidPackageDeployment
                    .TryGetActiveFiles(out _),
                Is.False);
            using (OperationMapDenseCityCandidateAndroidPackageDeployment.Begin(projectRoot))
            {
                Assert.That(
                    OperationMapDenseCityCandidateAndroidPackageDeployment
                        .TryGetActiveFiles(out var files),
                    Is.True);
                Assert.That(files, Is.Not.Empty);
            }
            Assert.That(
                OperationMapDenseCityCandidateAndroidPackageDeployment
                    .TryGetActiveFiles(out _),
                Is.False);
        });
    }

    [Test]
    public void DenseCandidateReleaseBuild_AlwaysCleansBuildCache()
    {
        BuildOptions options =
            BuildScript.ResolveDenseCityCandidateAndroidBuildOptions(profilerBuild: false);

        Assert.That(options & BuildOptions.CleanBuildCache, Is.Not.EqualTo(BuildOptions.None));
        Assert.That(options & BuildOptions.DetailedBuildReport, Is.Not.EqualTo(BuildOptions.None));
        Assert.That(options & BuildOptions.Development, Is.EqualTo(BuildOptions.None));
    }

    [Test]
    public void DenseCandidateProfilerBuild_AlwaysCleansBuildCache()
    {
        BuildOptions options =
            BuildScript.ResolveDenseCityCandidateAndroidBuildOptions(profilerBuild: true);

        Assert.That(options & BuildOptions.CleanBuildCache, Is.Not.EqualTo(BuildOptions.None));
        Assert.That(options & BuildOptions.Development, Is.Not.EqualTo(BuildOptions.None));
    }

    private static void WithFixture(Action<string> action)
    {
        string projectRoot = Path.Combine(
            Path.GetTempPath(),
            "warline-dense-candidate-package-" + Guid.NewGuid().ToString("N"));
        try
        {
            string addressablesRoot = Path.Combine(
                projectRoot,
                OperationMapDenseCityCandidateRuntimeContentBuilder.AddressablesOutputPath);
            string bundleRoot = Path.Combine(addressablesRoot, "Android");
            Directory.CreateDirectory(bundleRoot);
            File.WriteAllText(Path.Combine(addressablesRoot, "catalog.bin"), "catalog");
            File.WriteAllText(Path.Combine(addressablesRoot, "catalog.hash"), "hash");
            File.WriteAllText(Path.Combine(bundleRoot, "candidate.bundle"), "bundle");

            action(projectRoot);
        }
        finally
        {
            if (Directory.Exists(projectRoot))
                Directory.Delete(projectRoot, recursive: true);
        }
    }
}
