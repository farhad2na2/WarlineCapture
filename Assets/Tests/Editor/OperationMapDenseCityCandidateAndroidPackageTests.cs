using System;
using System.IO;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
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
            suite.DeploymentScope_ActivatesAndClearsAdditionalFiles
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
            "assets/ContentArchives/b8ebb31853db87420b18072fd34a9579",
            "assets/aa/DenseCityCandidate/catalog.bin",
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
                    archiveCatalog),
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
