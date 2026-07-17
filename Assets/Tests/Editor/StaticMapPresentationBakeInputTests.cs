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
            tests.EmptyOperationMapIdFails();
            tests.InvalidSourceSceneOrRootFails();
            tests.OutputFilesOutsideOwnedRootFail();
            tests.InvalidOutputExtensionsFail();
            tests.InvalidChunkSizesFail();
            Debug.Log("[StaticMapPresentationBakeInputValidation] result=Passed tests=7");
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
            Is.EqualTo("Assets/Game/GeneratedStaticMapPresentation/Scenes"));
    }

    [Test]
    public void EmptyOperationMapIdFails()
    {
        AssertInvalid(Create(operationMapId: string.Empty));
        AssertInvalid(Create(operationMapId: new string('x', 65)));
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
        AssertInvalid(Create(manifestPath: "Assets/Game/GeneratedStaticMapPresentation/Manifest.json"));
        AssertInvalid(Create(integrityPath: "Assets/Game/GeneratedStaticMapPresentation/Integrity.asset"));
    }

    [Test]
    public void InvalidChunkSizesFail()
    {
        AssertInvalid(Create(chunkSize: 0f));
        AssertInvalid(Create(chunkSize: float.NaN));
        AssertInvalid(Create(chunkSize: float.PositiveInfinity));
    }

    private static StaticMapPresentationBakeInput CurrentInput()
    {
        return Create();
    }

    private static StaticMapPresentationBakeInput Create(
        string operationMapId = "opmap.skirmish.desert_base_01",
        string sourceScenePath = "Assets/Game/Scenes/Match.unity",
        string sourceMapRootPath = "Map",
        string outputRoot = "Assets/Game/GeneratedStaticMapPresentation",
        string manifestPath = "Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset",
        string integrityPath = "Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationSceneIntegrity.json",
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
