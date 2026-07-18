using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Editor;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class OperationMapPhase0BaselineProbeTests
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string MatchSubScenePath = "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity";
    private const string CanonicalMapScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
    private const string ManifestPath =
        "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";
    private const string IntegrityPath =
        "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationSceneIntegrity.json";

    [Test]
    public void ComputeSha256_IsStableAndLowercase()
    {
        string hash = OperationMapPhase0BaselineProbe.ComputeSha256(Encoding.UTF8.GetBytes("abc"));

        Assert.That(
            hash,
            Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
    }

    [Test]
    public void ComputeAggregateHash_IsOrderIndependentAndRejectsDuplicatePaths()
    {
        var first = new[]
        {
            new OperationMapPhase0BaselineProbe.HashInput("b", "02"),
            new OperationMapPhase0BaselineProbe.HashInput("a", "01")
        };
        var second = first.Reverse().ToArray();

        Assert.That(
            OperationMapPhase0BaselineProbe.ComputeAggregateHash(first),
            Is.EqualTo(OperationMapPhase0BaselineProbe.ComputeAggregateHash(second)));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ComputeAggregateHash(new[]
            {
                new OperationMapPhase0BaselineProbe.HashInput("a", "01"),
                new OperationMapPhase0BaselineProbe.HashInput("a", "02")
            }));
    }

    [Test]
    public void ResolveReportOutputPath_UsesDefaultAndRejectsUnsafeLocations()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

        Assert.That(
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, null),
            Is.EqualTo(OperationMapPhase0BaselineProbe.DefaultReportPath));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, projectRoot));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "Design", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "Library", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "Assets", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "Packages", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                projectRoot,
                Path.Combine(projectRoot, "ProjectSettings", "probe.json")));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, "relative.json"));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapPhase0BaselineProbe.ResolveReportOutputPath(projectRoot, "/private/tmp/probe.txt"));

        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.OSXEditor)
        {
            string caseAlias = CreateCaseAlias(projectRoot);
            Assert.That(caseAlias, Is.Not.EqualTo(projectRoot));
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0BaselineProbe.ResolveReportOutputPath(
                    projectRoot,
                    Path.Combine(caseAlias, "probe.json")));
        }
    }

    [Test]
    public void HasRequiredReportShape_RequiresIdentityAndAllMajorSections()
    {
        OperationMapPhase0BaselineProbe.BaselineReport report = CreateValidMinimalReport();

        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.True);

        report.result = "Failed";
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.reportSchemaVersion++;
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.project.productGuid = string.Empty;
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.subSceneReference = null;
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.scenes.RemoveAt(1);
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.matchSceneViewReferences.Clear();
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.generatedOutputs.files.Clear();
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.generatedOutputs.ledgerSceneCount++;
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.generatedOutputs.combinedAggregateSha256 = new string('0', 64);
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.buildSettingsScenes.Clear();
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.manifest.sourceCount = 0;
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.buildingPlacements.count = 0;
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.vehiclePlacements.entries.Clear();
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        report = CreateValidMinimalReport();
        report.mapData.gridCellCount++;
        Assert.That(
            OperationMapPhase0BaselineProbe.HasRequiredReportShape(JsonUtility.ToJson(report)),
            Is.False);

        Assert.That(OperationMapPhase0BaselineProbe.HasRequiredReportShape("{}"), Is.False);
    }

    [Test]
    public void PublishReportAtomically_FailuresInvalidatePriorSuccessAndCleanTemporaryFiles()
    {
        string outputPath = Path.Combine(
            "/private/tmp",
            $"warline-operation-map-phase0-publish-{Guid.NewGuid():N}.json");
        string pattern = Path.GetFileName(outputPath) + ".tmp-*";
        string directory = Path.GetDirectoryName(outputPath);

        try
        {
            File.WriteAllText(outputPath, "Passed sentinel");
            Assert.Throws<InvalidOperationException>(() =>
                OperationMapPhase0BaselineProbe.PublishReportAtomically(outputPath, () => "{}"));
            Assert.That(File.Exists(outputPath), Is.False);
            Assert.That(Directory.GetFiles(directory, pattern), Is.Empty);

            File.WriteAllText(outputPath, "Passed sentinel");
            string validJson = JsonUtility.ToJson(CreateValidMinimalReport(), true) + "\n";
            Assert.Throws<IOException>(() =>
                OperationMapPhase0BaselineProbe.PublishReportAtomically(
                    outputPath,
                    () => validJson,
                    (temporaryPath, content, encoding) =>
                    {
                        File.WriteAllText(temporaryPath, content, encoding);
                        throw new IOException("Forced write failure.");
                    }));
            Assert.That(File.Exists(outputPath), Is.False);
            Assert.That(Directory.GetFiles(directory, pattern), Is.Empty);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            foreach (string temporaryPath in Directory.GetFiles(directory, pattern))
                File.Delete(temporaryPath);
        }
    }

    [Test]
    public void IntegrityDocumentShape_RejectsUnsupportedSchema()
    {
        const string valid =
            "{\"schemaVersion\":1,\"contentHash\":\"0123456789abcdef0123456789abcdef\"," +
            "\"scenes\":[{\"scenePath\":\"Assets/Chunk.unity\"," +
            "\"fileHash\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"," +
            "\"metaHash\":\"abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789\"}]}";

        Assert.That(OperationMapPhase0BaselineProbe.HasSupportedIntegrityDocumentShape(valid), Is.True);
        Assert.That(
            OperationMapPhase0BaselineProbe.HasSupportedIntegrityDocumentShape(
                valid.Replace("\"schemaVersion\":1", "\"schemaVersion\":2")),
            Is.False);
    }

    [Test]
    public void PlacementOrdering_UsesFullStableIdentityForOldKeyCollisions()
    {
        var entries = new List<OperationMapPhase0BaselineProbe.PlacementEntryReport>
        {
            CreatePlacementEntry(factionId: 2, localId: 8, worldX: 4f, yaw: 90f),
            CreatePlacementEntry(factionId: 1, localId: 9, worldX: 4f, yaw: 90f),
            CreatePlacementEntry(
                factionId: 1,
                localId: 8,
                worldX: 4f,
                yaw: 90f,
                prefabGuid: "99999999999999999999999999999999"),
            CreatePlacementEntry(factionId: 1, localId: 8, worldX: 5f, yaw: 90f),
            CreatePlacementEntry(factionId: 1, localId: 8, worldX: 4f, yaw: 180f)
        };
        var reversed = entries.AsEnumerable().Reverse().ToList();

        entries.Sort(OperationMapPhase0BaselineProbe.ComparePlacementEntries);
        reversed.Sort(OperationMapPhase0BaselineProbe.ComparePlacementEntries);

        string[] ordered = entries
            .Select(OperationMapPhase0BaselineProbe.BuildPlacementStableIdentity)
            .ToArray();
        string[] reverseOrdered = reversed
            .Select(OperationMapPhase0BaselineProbe.BuildPlacementStableIdentity)
            .ToArray();
        Assert.That(reverseOrdered, Is.EqualTo(ordered));
        for (int i = 1; i < entries.Count; i++)
        {
            Assert.That(
                OperationMapPhase0BaselineProbe.ComparePlacementEntries(entries[i - 1], entries[i]),
                Is.LessThan(0));
        }
    }

    [Test]
    public void Run_WritesOnlyExternalReportAndLeavesOwnedProjectFilesUnchanged()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(
            "/private/tmp",
            $"warline-operation-map-phase0-baseline-test-{Guid.NewGuid():N}.json");
        string previousOverride = Environment.GetEnvironmentVariable(
            OperationMapPhase0BaselineProbe.ReportPathEnvironmentVariable);
        string[] trackedProbeInputs =
        {
            MatchScenePath,
            MatchSubScenePath,
            CanonicalMapScenePath,
            ManifestPath,
            IntegrityPath
        };
        string[] beforeHashes = trackedProbeInputs
            .Select(path => OperationMapPhase0BaselineProbe.ComputeSha256(
                File.ReadAllBytes(Path.Combine(projectRoot, path))))
            .ToArray();
        var setupBefore = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            Environment.SetEnvironmentVariable(
                OperationMapPhase0BaselineProbe.ReportPathEnvironmentVariable,
                outputPath);
            OperationMapPhase0BaselineProbe.Run();

            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(
                OperationMapPhase0BaselineProbe.HasRequiredReportShape(File.ReadAllText(outputPath)),
                Is.True);
            Assert.That(
                trackedProbeInputs.Select(path => OperationMapPhase0BaselineProbe.ComputeSha256(
                    File.ReadAllBytes(Path.Combine(projectRoot, path)))),
                Is.EqualTo(beforeHashes));
            AssertSceneSetupEquivalent(setupBefore, EditorSceneManager.GetSceneManagerSetup());
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                OperationMapPhase0BaselineProbe.ReportPathEnvironmentVariable,
                previousOverride);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    private static void AssertSceneSetupEquivalent(SceneSetup[] expected, SceneSetup[] actual)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(actual[i].path, Is.EqualTo(expected[i].path), $"Scene path drifted at index {i}.");
            Assert.That(actual[i].isLoaded, Is.EqualTo(expected[i].isLoaded), $"Loaded state drifted at index {i}.");
            Assert.That(actual[i].isActive, Is.EqualTo(expected[i].isActive), $"Active state drifted at index {i}.");
            Assert.That(actual[i].isSubScene, Is.EqualTo(expected[i].isSubScene), $"SubScene state drifted at index {i}.");
        }
    }

    private static OperationMapPhase0BaselineProbe.BaselineReport CreateValidMinimalReport()
    {
        const string hash128 = "0123456789abcdef0123456789abcdef";
        const string hash256 =
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        const string matchGuid = "11111111111111111111111111111111";
        const string subSceneGuid = "22222222222222222222222222222222";
        const string chunkPath = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/Chunk.unity";

        OperationMapPhase0BaselineProbe.ObjectIdentityReport asset =
            CreateAssetIdentity("Asset", "33333333333333333333333333333333", 11400000);
        OperationMapPhase0BaselineProbe.PlacementReport building = CreatePlacementReport("Building");
        OperationMapPhase0BaselineProbe.PlacementReport vehicle = CreatePlacementReport("Vehicle");
        var report = new OperationMapPhase0BaselineProbe.BaselineReport
        {
            reportSchema = OperationMapPhase0BaselineProbe.ReportSchema,
            reportSchemaVersion = OperationMapPhase0BaselineProbe.ReportSchemaVersion,
            result = "Passed",
            reportPath = "/private/tmp/report.json",
            project = new OperationMapPhase0BaselineProbe.ProjectReport
            {
                unityVersion = "6000.5.2f1",
                projectName = "Project",
                projectRoot = "/project",
                productName = "Product",
                productGuid = "product-guid",
                companyName = "Company",
                applicationIdentifier = "com.company.product"
            },
            sceneSetupBeforeProbe = new List<OperationMapPhase0BaselineProbe.SceneSetupReport>(),
            scenes = new List<OperationMapPhase0BaselineProbe.SceneReport>
            {
                CreateSceneReport(MatchScenePath, matchGuid, autoLoad: false),
                CreateSceneReport(MatchSubScenePath, subSceneGuid, autoLoad: true)
            },
            subSceneReference = new OperationMapPhase0BaselineProbe.SubSceneReferenceReport
            {
                componentHierarchyPath = "Match/SubScene[0]",
                componentGlobalObjectId = "GlobalObjectId_SubScene",
                sceneAssetPath = MatchSubScenePath,
                sceneAssetGuid = subSceneGuid,
                autoLoad = true
            },
            matchSceneViewReferences = new List<OperationMapPhase0BaselineProbe.SerializedObjectReferenceFieldReport>
            {
                new()
                {
                    propertyName = "asset",
                    declaredType = "UnityEngine.Object",
                    isCollection = false,
                    elementCount = 1,
                    targets = new List<OperationMapPhase0BaselineProbe.ObjectIdentityReport> { asset }
                }
            },
            manifest = new OperationMapPhase0BaselineProbe.ManifestReport
            {
                asset = asset,
                schemaVersion = StaticMapPresentationManifest.CurrentSchemaVersion,
                operationMapId = StaticMapPresentationBaker.CurrentOperationMapId,
                canonicalScenePath = CanonicalMapScenePath,
                canonicalSceneGuid = matchGuid,
                canonicalSceneDependencyHash = hash128,
                computedCanonicalSceneDependencyHash = hash128,
                chunkSize = 32f,
                contentHash = hash128,
                computedContentHash = hash128,
                chunkCount = 1,
                sourceCount = 1,
                fileSha256 = hash256,
                metaSha256 = hash256
            },
            generatedOutputs = new OperationMapPhase0BaselineProbe.GeneratedOutputsReport
            {
                integrityPath =
                    "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationSceneIntegrity.json",
                integritySchemaVersion = 1,
                integrityContentHash = hash128,
                integrityFileSha256 = hash256,
                integrityMetaSha256 = hash256,
                manifestSceneCount = 1,
                ledgerSceneCount = 1,
                diskSceneCount = 1,
                diskMetaCount = 1,
                exactFileSetParity = true,
                aggregateAlgorithm =
                    "sha256(utf8(path\\0sha256\\n), entries sorted by ordinal path)",
                sceneFilesAggregateSha256 = hash256,
                sceneMetasAggregateSha256 = hash256,
                combinedAggregateSha256 = hash256,
                files = new List<OperationMapPhase0BaselineProbe.GeneratedSceneFileReport>
                {
                    new()
                    {
                        scenePath = chunkPath,
                        sceneGuid = "44444444444444444444444444444444",
                        sceneSha256 = hash256,
                        metaPath = chunkPath + ".meta",
                        metaSha256 = hash256
                    }
                }
            },
            buildSettingsScenes = new List<OperationMapPhase0BaselineProbe.BuildSettingsSceneReport>
            {
                new()
                {
                    buildSettingsIndex = 0,
                    path = MatchScenePath,
                    guid = matchGuid,
                    enabled = true
                }
            },
            buildingPlacements = building,
            vehiclePlacements = vehicle,
            mapData = new OperationMapPhase0BaselineProbe.MapDataReport
            {
                mapSurfaceAuthoring = CreateSceneIdentity("Map", MatchScenePath, matchGuid, "Map[0]"),
                gridAsset = CreateAssetIdentity("Grid", "55555555555555555555555555555555", 11400000),
                gridWidth = 1,
                gridHeight = 1,
                gridCellSize = 1f,
                gridOrigin = Vector3.zero,
                gridCellCount = 1,
                blockedCellCount = 0,
                mapSurfaceAsset =
                    CreateAssetIdentity("Surface", "66666666666666666666666666666666", 11400000),
                mapSurfaceDimensions = Vector2Int.one,
                mapSurfaceCellSize = 1f,
                mapSurfaceOrigin = Vector3.zero,
                surfaceCount = 1,
                connectionCount = 0,
                payloadVersion = 1,
                payloadEncoding = 1,
                compressedPayloadBytes = 1,
                uncompressedPayloadBytes = 1,
                runtimeBlobHash = hash128,
                dimensionsOriginCellSizeConsistent = true
            }
        };

        OperationMapPhase0BaselineProbe.GeneratedSceneFileReport generatedFile =
            report.generatedOutputs.files.Single();
        report.generatedOutputs.sceneFilesAggregateSha256 =
            OperationMapPhase0BaselineProbe.ComputeAggregateHash(new[]
            {
                new OperationMapPhase0BaselineProbe.HashInput(
                    generatedFile.scenePath,
                    generatedFile.sceneSha256)
            });
        report.generatedOutputs.sceneMetasAggregateSha256 =
            OperationMapPhase0BaselineProbe.ComputeAggregateHash(new[]
            {
                new OperationMapPhase0BaselineProbe.HashInput(
                    generatedFile.metaPath,
                    generatedFile.metaSha256)
            });
        report.generatedOutputs.combinedAggregateSha256 =
            OperationMapPhase0BaselineProbe.ComputeAggregateHash(new[]
            {
                new OperationMapPhase0BaselineProbe.HashInput(
                    generatedFile.scenePath,
                    generatedFile.sceneSha256),
                new OperationMapPhase0BaselineProbe.HashInput(
                    generatedFile.metaPath,
                    generatedFile.metaSha256)
            });
        return report;
    }

    private static OperationMapPhase0BaselineProbe.SceneReport CreateSceneReport(
        string path,
        string guid,
        bool autoLoad)
    {
        const string hash256 =
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        return new OperationMapPhase0BaselineProbe.SceneReport
        {
            path = path,
            guid = guid,
            loadMode = "ExplicitInspection",
            loadedDuringInspection = true,
            autoLoadKnown = true,
            autoLoad = autoLoad,
            rootObjectCount = 1,
            hierarchyObjectCount = 1,
            hierarchySha256 = hash256,
            rootObjects = new List<OperationMapPhase0BaselineProbe.RootObjectReport>
            {
                new()
                {
                    name = "Root",
                    siblingIndex = 0,
                    hierarchyPath = "Root[0]",
                    activeSelf = true,
                    directChildCount = 0,
                    hierarchyObjectCount = 1,
                    hierarchySha256 = hash256,
                    rootComponentTypes = new List<string> { "UnityEngine.Transform" }
                }
            }
        };
    }

    private static OperationMapPhase0BaselineProbe.PlacementReport CreatePlacementReport(string kind)
    {
        OperationMapPhase0BaselineProbe.PlacementEntryReport entry =
            CreatePlacementEntry(factionId: 1, localId: 11400000, worldX: 0f, yaw: 0f);
        string identity = OperationMapPhase0BaselineProbe.BuildPlacementStableIdentity(entry);
        return new OperationMapPhase0BaselineProbe.PlacementReport
        {
            kind = kind,
            config = CreateAssetIdentity(kind + "Config", "77777777777777777777777777777777", 11400000),
            spawnOnMatchStart = true,
            hideAuthoringVisualsAfterSpawn = true,
            authoringRoot = CreateSceneIdentity(
                kind + "Root",
                MatchScenePath,
                "11111111111111111111111111111111",
                kind + "Root[0]"),
            count = 1,
            identityPathAggregateSha256 =
                OperationMapPhase0BaselineProbe.ComputeAggregateHash(new[]
                {
                    new OperationMapPhase0BaselineProbe.HashInput(
                        identity,
                        OperationMapPhase0BaselineProbe.ComputeSha256(Encoding.UTF8.GetBytes(identity)))
                }),
            entries = new List<OperationMapPhase0BaselineProbe.PlacementEntryReport> { entry }
        };
    }

    private static OperationMapPhase0BaselineProbe.PlacementEntryReport CreatePlacementEntry(
        byte factionId,
        long localId,
        float worldX,
        float yaw,
        string prefabGuid = "88888888888888888888888888888888")
    {
        return new OperationMapPhase0BaselineProbe.PlacementEntryReport
        {
            sourcePath = "Map/Source",
            category = "Category",
            sourceKey = "source",
            factionId = factionId,
            configSourcePathOccurrenceCount = 1,
            sceneMatchCount = 1,
            worldCenter = new Vector3(worldX, 1f, 2f),
            worldPosition = new Vector3(worldX, 3f, 4f),
            worldEulerAngles = new Vector3(0f, yaw, 0f),
            worldScale = Vector3.one,
            yawDegrees = yaw,
            rotateVertical = false,
            prefab = CreateAssetIdentity("Prefab", prefabGuid, localId)
        };
    }

    private static string CreateCaseAlias(string path)
    {
        char[] characters = path.ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            if (!char.IsLetter(characters[i]))
                continue;

            characters[i] = char.IsUpper(characters[i])
                ? char.ToLowerInvariant(characters[i])
                : char.ToUpperInvariant(characters[i]);
            return new string(characters);
        }

        throw new InvalidOperationException($"Path contains no letter for case-alias coverage: {path}");
    }

    private static OperationMapPhase0BaselineProbe.ObjectIdentityReport CreateAssetIdentity(
        string name,
        string guid,
        long localId)
    {
        return new OperationMapPhase0BaselineProbe.ObjectIdentityReport
        {
            name = name,
            type = "UnityEngine.Object",
            assetPath = $"Assets/{name}.asset",
            assetGuid = guid,
            localId = localId,
            scenePath = string.Empty,
            sceneGuid = string.Empty,
            hierarchyPath = string.Empty,
            globalObjectId = $"GlobalObjectId_{guid}_{localId}"
        };
    }

    private static OperationMapPhase0BaselineProbe.ObjectIdentityReport CreateSceneIdentity(
        string name,
        string scenePath,
        string sceneGuid,
        string hierarchyPath)
    {
        return new OperationMapPhase0BaselineProbe.ObjectIdentityReport
        {
            name = name,
            type = "UnityEngine.Transform",
            assetPath = string.Empty,
            assetGuid = string.Empty,
            localId = 0,
            scenePath = scenePath,
            sceneGuid = sceneGuid,
            hierarchyPath = hierarchyPath,
            globalObjectId = $"GlobalObjectId_{sceneGuid}_{hierarchyPath}"
        };
    }
}
