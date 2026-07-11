using System;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Editor;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class StaticMapPresentationSceneWiringTests
{
    [Test]
    public void MatchScene_SerializesTheGeneratedPresentationManifest()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene(
                StaticMapPresentationBaker.CanonicalMatchScenePath,
                OpenSceneMode.Single);
            MatchSceneView view = FindSingleView(scene);
            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(
                    StaticMapPresentationBaker.ManifestPath);

            Assert.NotNull(manifest, "Generated static-map presentation manifest is missing.");
            Assert.AreSame(manifest, view.StaticMapPresentationManifest);
            Assert.AreEqual(
                StaticMapPresentationBaker.ManifestPath,
                AssetDatabase.GetAssetPath(view.StaticMapPresentationManifest));
            Assert.AreEqual(StaticMapPresentationManifest.CurrentSchemaVersion, manifest.SchemaVersion);
            Assert.AreEqual(StaticMapPresentationBaker.CanonicalMatchScenePath, manifest.CanonicalScenePath);
            Assert.That(manifest.Chunks.Count, Is.GreaterThan(0));
            Assert.NotNull(view.WorldCamera, "MatchSceneView must serialize the camera used for chunk residency.");
        }
        finally
        {
            if (previousSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        }
    }

    [Test]
    public void CompositionAndWiringSources_DoNotUseGameObjectFindFallback()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string[] sourcePaths =
        {
            "Assets/Game/Scripts/Composition/MatchSceneView.cs",
            "Assets/Game/Scripts/Editor/StaticMapPresentationSceneWiring.cs"
        };

        for (int index = 0; index < sourcePaths.Length; index++)
        {
            string source = File.ReadAllText(Path.Combine(projectRoot, sourcePaths[index]));
            StringAssert.DoesNotContain(
                "GameObject.Find(",
                source,
                $"{sourcePaths[index]} must use explicit serialized/composition ownership.");
        }
    }

    [Test]
    public void MenuLifecycle_GatesMatchStartAndUnloadOnPresentationStreaming()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string source = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs"));

        const string startGate = "if (CanAdvanceMatchStart(shellState))";
        const string startUpdate = "matchStartSystem.Update(entityManager);";
        int startGateIndex = source.IndexOf(startGate, StringComparison.Ordinal);
        int startUpdateIndex = source.IndexOf(startUpdate, StringComparison.Ordinal);
        Assert.That(startGateIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(startUpdateIndex, Is.GreaterThan(startGateIndex));

        const string drainGate = "if (!staticMapPresentationStreamer.DrainComplete)";
        const string unloadCall = "sceneLifecycleSceneSystemHelper.QueueUnloadMatch(entityManager);";
        int drainGateIndex = source.IndexOf(drainGate, StringComparison.Ordinal);
        int unloadCallIndex = source.IndexOf(unloadCall, StringComparison.Ordinal);
        Assert.That(drainGateIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(unloadCallIndex, Is.GreaterThan(drainGateIndex));
        Assert.AreEqual(1, CountOccurrences(source, unloadCall));
        StringAssert.Contains("else if (staticMapPresentationStreamer.IsDraining)", source);
        StringAssert.Contains("!staticMapPresentationStreamer.IsDraining", source);
    }

    private static MatchSceneView FindSingleView(Scene scene)
    {
        List<MatchSceneView> views = new();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            views.AddRange(roots[rootIndex].GetComponentsInChildren<MatchSceneView>(true));

        Assert.AreEqual(1, views.Count, $"Expected one MatchSceneView in {scene.path}.");
        return views[0];
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }
}
