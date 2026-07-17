using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class StaticMapPresentationBakeInputTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new StaticMapPresentationBakeInputTests();
            tests.CurrentCompatibilityInputValidates();
            tests.SceneOutputFolderIsDerivedFromOwnedRoot();
            tests.SceneFilePrefixIsDerivedFromOperationMapId();
            tests.EmptyOperationMapIdFails();
            tests.OperationMapIdDerivesUniqueOutputRoot();
            tests.OutputRootMustMatchOperationMapId();
            tests.InvalidSourceSceneOrRootFails();
            tests.OutputFilesOutsideOwnedRootFail();
            tests.InvalidOutputExtensionsFail();
            tests.InvalidChunkSizesFail();
            tests.CurrentCompatibilityFactoryMatchesLegacyConstants();
            tests.CompatibilityValidationRejectsAlternateOwnership();
            Debug.Log("[StaticMapPresentationBakeInputValidation] result=Passed tests=12");
        }
        catch (Exception exception)
        {
            Debug.LogError("[StaticMapPresentationBakeInputValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void CurrentCompatibilityInputValidates()
    {
        StaticMapPresentationBakeInput input = CurrentInput();

        Assert.That(input.TryValidate(out string error), Is.True, error);
        Assert.That(input.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(input.SourceMapRootPath, Is.EqualTo("Map"));
    }

    [Test]
    public void SceneOutputFolderIsDerivedFromOwnedRoot()
    {
        Assert.That(
            CurrentInput().SceneOutputFolder,
            Is.EqualTo(
                "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes"));
    }

    [Test]
    public void SceneFilePrefixIsDerivedFromOperationMapId()
    {
        Assert.That(
            CurrentInput().SceneFilePrefix,
            Is.EqualTo("StaticMapPresentation_opmap_skirmish_desert_base_01_"));
        Assert.That(
            StaticMapPresentationOutputPathContract.TryResolveSceneFilePrefix(
                "opmap.ch01.district-edge_01",
                out string alternatePrefix,
                out string error),
            Is.True,
            error);
        Assert.That(
            alternatePrefix,
            Is.EqualTo("StaticMapPresentation_opmap_ch01_district-edge_01_"));
        Assert.That(alternatePrefix, Is.Not.EqualTo(CurrentInput().SceneFilePrefix));
    }

    [Test]
    public void EmptyOperationMapIdFails()
    {
        AssertInvalid(Create(operationMapId: string.Empty));
        AssertInvalid(Create(operationMapId: new string('x', 65)));
    }

    [Test]
    public void OperationMapIdDerivesUniqueOutputRoot()
    {
        Assert.That(StaticMapPresentationOutputPathContract.TryResolveOutputRoot(
            "opmap.skirmish.desert_base_01",
            out string firstRoot,
            out string firstError), Is.True, firstError);
        Assert.That(StaticMapPresentationOutputPathContract.TryResolveOutputRoot(
            "opmap.ch01.district-edge_01",
            out string secondRoot,
            out string secondError), Is.True, secondError);
        Assert.That(firstRoot, Is.EqualTo(StaticMapPresentationBaker.OutputRoot));
        Assert.That(secondRoot, Is.EqualTo(
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/ch01/district-edge_01"));
        Assert.That(secondRoot, Is.Not.EqualTo(firstRoot));
    }

    [Test]
    public void OutputRootMustMatchOperationMapId()
    {
        AssertInvalid(Create(outputRoot:
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/wrong"));
        AssertInvalid(Create(operationMapId: "opmap.skirmish/escape.invalid"));
        AssertInvalid(Create(operationMapId: "opmap.Skirmish.invalid"));
    }

    [Test]
    public void InvalidSourceSceneOrRootFails()
    {
        AssertInvalid(Create(sourceScenePath: "/tmp/Match.unity"));
        AssertInvalid(Create(sourceScenePath: "Assets/Game/Scenes/Match.prefab"));
        AssertInvalid(Create(sourceMapRootPath: string.Empty));
        AssertInvalid(Create(sourceMapRootPath: "Map\\Vehicles"));
    }

    [Test]
    public void OutputFilesOutsideOwnedRootFail()
    {
        AssertInvalid(Create(manifestPath: "Assets/Game/Other/Manifest.asset"));
        AssertInvalid(Create(integrityPath: "Assets/Game/Other/Integrity.json"));
    }

    [Test]
    public void InvalidOutputExtensionsFail()
    {
        AssertInvalid(Create(manifestPath: StaticMapPresentationBaker.OutputRoot + "/Manifest.json"));
        AssertInvalid(Create(integrityPath: StaticMapPresentationBaker.OutputRoot + "/Integrity.asset"));
    }

    [Test]
    public void InvalidChunkSizesFail()
    {
        AssertInvalid(Create(chunkSize: 0f));
        AssertInvalid(Create(chunkSize: float.NaN));
        AssertInvalid(Create(chunkSize: float.PositiveInfinity));
    }

    [Test]
    public void CurrentCompatibilityFactoryMatchesLegacyConstants()
    {
        StaticMapPresentationBakeInput input =
            StaticMapPresentationBaker.CreateCurrentCompatibilityInput();

        Assert.That(input.TryValidate(out string error), Is.True, error);
        Assert.That(input.SourceScenePath, Is.EqualTo(StaticMapPresentationBaker.CanonicalMatchScenePath));
        Assert.That(input.OutputRoot, Is.EqualTo(StaticMapPresentationBaker.OutputRoot));
        Assert.That(input.SceneOutputFolder, Is.EqualTo(StaticMapPresentationBaker.SceneOutputFolder));
        Assert.That(input.ManifestPath, Is.EqualTo(StaticMapPresentationBaker.ManifestPath));
        Assert.That(input.ChunkSize, Is.EqualTo(StaticMapPresentationBaker.ChunkSize));
        Assert.DoesNotThrow(() => StaticMapPresentationBaker.ValidateCompatibilityInput(input));
    }

    [Test]
    public void CompatibilityValidationRejectsAlternateOwnership()
    {
        StaticMapPresentationBakeInput alternate = Create(
            operationMapId: "opmap.test.alternate",
            sourceScenePath: "Assets/Game/Scenes/OperationMaps/Test.unity",
            outputRoot: "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate",
            manifestPath:
                "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate/Manifest.asset",
            integrityPath:
                "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate/Integrity.json");

        Assert.That(alternate.TryValidate(out string error), Is.True, error);
        Assert.Throws<InvalidOperationException>(() =>
            StaticMapPresentationBaker.ValidateCompatibilityInput(alternate));
    }

    private static StaticMapPresentationBakeInput CurrentInput()
    {
        return Create();
    }

    private static StaticMapPresentationBakeInput Create(
        string operationMapId = "opmap.skirmish.desert_base_01",
        string sourceScenePath = "Assets/Game/Scenes/Match.unity",
        string sourceMapRootPath = "Map",
        string outputRoot = StaticMapPresentationBaker.OutputRoot,
        string manifestPath = StaticMapPresentationBaker.ManifestPath,
        string integrityPath = StaticMapPresentationBaker.OutputRoot +
            "/StaticMapPresentationSceneIntegrity.json",
        float chunkSize = 32f)
    {
        return new StaticMapPresentationBakeInput(
            operationMapId,
            sourceScenePath,
            sourceMapRootPath,
            outputRoot,
            manifestPath,
            integrityPath,
            chunkSize);
    }

    private static void AssertInvalid(StaticMapPresentationBakeInput input)
    {
        Assert.That(input.TryValidate(out string error), Is.False);
        Assert.That(error, Is.Not.Empty);
    }
}
