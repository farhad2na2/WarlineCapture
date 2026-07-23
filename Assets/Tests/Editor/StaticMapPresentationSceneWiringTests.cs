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
    public void MatchScene_ResolvesTheGeneratedPresentationManifestFromTheSelectedMap()
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
            Assert.That(
                view.OperationMapCatalog.TryResolve(
                    view.OperationMapId,
                    out Game.Configs.OperationMapDefinition definition),
                Is.True);
            string presentationSourceScenePath =
                StaticMapPresentationBaker.CurrentStagedOperationMapScenePath;

            Assert.NotNull(manifest, "Generated static-map presentation manifest is missing.");
            Assert.IsNull(
                view.StaticMapPresentationManifest,
                "Thin Match shell must not serialize a map-owned manifest directly.");
            Assert.AreSame(
                manifest,
                StaticMapPresentationSceneWiring.LoadValidatedManifest(
                    view.OperationMapCatalog,
                    view.OperationMapId,
                    presentationSourceScenePath));
            Assert.Throws<InvalidOperationException>(() =>
                StaticMapPresentationSceneWiring.LoadValidatedManifest(
                    view.OperationMapCatalog,
                    "opmap.skirmish.missing",
                    presentationSourceScenePath));
            Assert.AreEqual(
                StaticMapPresentationBaker.ManifestPath,
                AssetDatabase.GetAssetPath(manifest));
            Assert.AreEqual(StaticMapPresentationManifest.CurrentSchemaVersion, manifest.SchemaVersion);
            Assert.AreEqual(presentationSourceScenePath, manifest.CanonicalScenePath);
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
            if (sourcePaths[index].EndsWith("StaticMapPresentationSceneWiring.cs", StringComparison.Ordinal))
            {
                StringAssert.DoesNotContain(
                    "StaticMapPresentationBaker.ManifestPath",
                    source,
                    "Scene wiring must resolve the selected map manifest instead of the compatibility path.");
            }
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

        const string drainPolicy = "RequiresStaticPresentationDrain(streamedMatchView)";
        const string drainGate = "!staticMapPresentationStreamer.DrainComplete";
        const string contentUnloadGate = "!streamedMatchView.OperationMapContentUnloadComplete";
        const string contentUnloadCall = "streamedMatchView.TryBeginOperationMapContentUnload";
        const string unloadCall = "sceneLifecycleSceneSystemHelper.QueueUnloadMatch(entityManager);";
        int drainPolicyIndex = source.IndexOf(drainPolicy, StringComparison.Ordinal);
        int drainGateIndex = source.IndexOf(drainGate, drainPolicyIndex, StringComparison.Ordinal);
        int contentUnloadGateIndex = source.IndexOf(contentUnloadGate, StringComparison.Ordinal);
        int contentUnloadCallIndex = source.IndexOf(contentUnloadCall, StringComparison.Ordinal);
        int unloadCallIndex = source.IndexOf(unloadCall, StringComparison.Ordinal);
        Assert.That(drainPolicyIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(drainGateIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(contentUnloadGateIndex, Is.GreaterThan(drainGateIndex));
        Assert.That(contentUnloadCallIndex, Is.GreaterThan(contentUnloadGateIndex));
        Assert.That(unloadCallIndex, Is.GreaterThan(contentUnloadCallIndex));
        Assert.AreEqual(1, CountOccurrences(source, unloadCall));
        StringAssert.Contains("else if (staticMapPresentationStreamer.IsDraining)", source);
        StringAssert.Contains("!staticMapPresentationStreamer.IsDraining", source);
        StringAssert.Contains(
            "streamedMatchView = matchScene;",
            source,
            "EntityScene route transitions must retain the MatchSceneView lifecycle owner.");
    }

    [Test]
    public void MatchTeardown_RestoresCanonicalRenderersBeforeSourceSceneUnload()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string matchSceneViewSource = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Composition/MatchSceneView.cs"));
        string bootstrapSource = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs"));

        int shutdownIndex = matchSceneViewSource.IndexOf(
            "ShutdownMatchRuntimeBound(disposeSourceSceneLoad: false)",
            StringComparison.Ordinal);
        int sourceUnloadIndex = matchSceneViewSource.IndexOf(
            "operationMapSceneLoadingSystem.TryBeginUnload",
            StringComparison.Ordinal);
        int bootstrapDestroyIndex = bootstrapSource.IndexOf(
            "public void OnDestroy()",
            StringComparison.Ordinal);
        int restoreIndex = bootstrapSource.IndexOf(
            "mapVisuals.Dispose()",
            bootstrapDestroyIndex,
            StringComparison.Ordinal);

        Assert.That(shutdownIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(sourceUnloadIndex, Is.GreaterThan(shutdownIndex));
        Assert.That(bootstrapDestroyIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(restoreIndex, Is.GreaterThan(bootstrapDestroyIndex));
    }

    [Test]
    public void EntitySceneMenuTeardown_ReleasesMetadataBeforePackedContentAndMatchShell()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string matchSceneViewSource = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Composition/MatchSceneView.cs"));
        string loaderSource = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Composition/OperationMapSceneLoadingSceneSystemHelper.cs"));
        string menuSource = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs"));

        int contentUnloadIndex = menuSource.IndexOf(
            "streamedMatchView.TryBeginOperationMapContentUnload",
            StringComparison.Ordinal);
        int matchUnloadIndex = menuSource.IndexOf(
            "sceneLifecycleSceneSystemHelper.QueueUnloadMatch(entityManager);",
            StringComparison.Ordinal);
        int beginContentUnloadIndex = matchSceneViewSource.IndexOf(
            "internal bool TryBeginOperationMapContentUnload",
            StringComparison.Ordinal);
        int shutdownIndex = matchSceneViewSource.IndexOf(
            "ShutdownMatchRuntimeBound(disposeSourceSceneLoad: false);",
            beginContentUnloadIndex,
            StringComparison.Ordinal);
        int metadataDisposeIndex = matchSceneViewSource.IndexOf(
            "DisposeOperationMapMetadataBootstrap();",
            matchSceneViewSource.IndexOf(
                "private void ShutdownMatchRuntimeBound",
                StringComparison.Ordinal),
            StringComparison.Ordinal);
        int packedReleaseIndex = loaderSource.IndexOf(
            "SceneSystem.UnloadParameters.DestroyMetaEntities",
            StringComparison.Ordinal);

        Assert.That(contentUnloadIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(matchUnloadIndex, Is.GreaterThan(contentUnloadIndex));
        Assert.That(beginContentUnloadIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(shutdownIndex, Is.GreaterThan(beginContentUnloadIndex));
        Assert.That(metadataDisposeIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(packedReleaseIndex, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void EntitySceneSteadyState_DoesNotSearchHierarchyAccessSourceOrQueryPhysics()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string menuSource = ReadProjectSource(
            projectRoot,
            "Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs");
        string matchSource = ReadProjectSource(
            projectRoot,
            "Assets/Game/Scripts/Composition/MatchSceneView.cs");
        string bootstrapSource = ReadProjectSource(
            projectRoot,
            "Assets/Game/Scripts/Composition/OperationMapRuntimeBootstrapSceneSystemHelper.cs");
        string streamerSource = ReadProjectSource(
            projectRoot,
            "Assets/Game/Scripts/Composition/StaticMapPresentationStreamer.cs");
        string loaderSource = ReadProjectSource(
            projectRoot,
            "Assets/Game/Scripts/Composition/OperationMapSceneLoadingSceneSystemHelper.cs");
        string featureStartupSource = ReadProjectSource(
            projectRoot,
            "Assets/Game/Scripts/Composition/GameplayFeatureStartupCompositionSystemHelper.cs");

        string[] forbiddenSteadyStateTokens =
        {
            "GetRootGameObjects(",
            "GetComponentsInChildren",
            "GetComponentInChildren",
            "Transform.Find(",
            "GameObject.Find(",
            "Object.Find",
            "FindObjectOfType",
            "FindObjectsByType",
            "Resources.FindObjectsOfTypeAll",
            "SceneManager.",
            "EditorSceneManager.",
            "AssetDatabase.",
            "Resources.Load",
            "Addressables.Load",
            "DenseMiddleEasternCity",
            "DenseCity",
            "CityEditModeBuilder",
            ".Generate(",
            "Physics.",
            "Physics2D.",
            "Overlap",
            "Raycast",
            "SphereCast",
            "BoxCast",
            "Collider"
        };

        AssertMethodExcludes(
            menuSource,
            "internal void UpdateStaticMapPresentationForLoadedMatch(",
            forbiddenSteadyStateTokens);
        AssertMethodExcludes(
            matchSource,
            "internal bool TryPublishOperationMapReadiness(",
            forbiddenSteadyStateTokens);
        AssertMethodExcludes(
            matchSource,
            "private bool IsOperationMapSubSceneReady()",
            forbiddenSteadyStateTokens);
        AssertMethodExcludes(
            bootstrapSource,
            "public bool TryUpdateReadiness(",
            forbiddenSteadyStateTokens);
        AssertMethodExcludes(
            streamerSource,
            "public void Update()",
            forbiddenSteadyStateTokens);

        string matchUpdate = ExtractMethodBody(matchSource, "private void Update()");
        int boundGateIndex = matchUpdate.IndexOf(
            "if (!matchRuntimeBound)",
            StringComparison.Ordinal);
        int sourceLoadIndex = matchUpdate.IndexOf(
            "UpdateOperationMapSourceSceneLoad();",
            StringComparison.Ordinal);
        int gameplayUpdateIndex = matchUpdate.IndexOf(
            "matchBootstrapSystem.Update();",
            StringComparison.Ordinal);
        Assert.That(boundGateIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(sourceLoadIndex, Is.GreaterThan(boundGateIndex));
        Assert.That(gameplayUpdateIndex, Is.GreaterThan(sourceLoadIndex));

        string loaderUpdate = ExtractMethodBody(loaderSource, "public void Update()");
        int readyExitIndex = loaderUpdate.IndexOf("if (IsReady)", StringComparison.Ordinal);
        int sceneReferenceIndex = loaderUpdate.IndexOf(
            "sceneReference.TryGetLoadedSceneView(",
            StringComparison.Ordinal);
        Assert.That(readyExitIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(sceneReferenceIndex, Is.GreaterThan(readyExitIndex));

        string featureStartup = ExtractMethodBody(
            featureStartupSource,
            "public Result Initialize(");
        StringAssert.Contains(
            "RuntimeCityCompositionSystemHelper runtimeCity = enableLegacyRuntimeMapPresentation",
            featureStartup);
        StringAssert.Contains(
            "? ResolveRuntimeCityCompositionSystemHelper()",
            featureStartup);
        StringAssert.Contains(
            "? ResolveRuntimeGridBlockerPresentationHelper()",
            featureStartup);
        StringAssert.Contains(
            "? ResolveRuntimeDecorationSpawnerPresentationHelper()",
            featureStartup);
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

    private static string ReadProjectSource(string projectRoot, string relativePath)
    {
        return File.ReadAllText(Path.Combine(projectRoot, relativePath));
    }

    private static void AssertMethodExcludes(
        string source,
        string methodSignature,
        IReadOnlyList<string> forbiddenTokens)
    {
        string methodBody = ExtractMethodBody(source, methodSignature);
        for (int tokenIndex = 0; tokenIndex < forbiddenTokens.Count; tokenIndex++)
        {
            StringAssert.DoesNotContain(
                forbiddenTokens[tokenIndex],
                methodBody,
                $"{methodSignature} must remain free of runtime hierarchy, source-scene, " +
                "generator, and collider/physics access.");
        }
    }

    private static string ExtractMethodBody(string source, string methodSignature)
    {
        int signatureIndex = source.IndexOf(methodSignature, StringComparison.Ordinal);
        Assert.That(signatureIndex, Is.GreaterThanOrEqualTo(0), methodSignature);
        int bodyStart = source.IndexOf('{', signatureIndex);
        Assert.That(bodyStart, Is.GreaterThan(signatureIndex), methodSignature);

        int depth = 0;
        for (int index = bodyStart; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(bodyStart, index - bodyStart + 1);
        }

        Assert.Fail($"Method body is not balanced: {methodSignature}");
        return null;
    }
}
