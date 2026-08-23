using System;
using System.Linq;
using Game.Authoring;
using Game.Composition;
using Game.Components;
using Game.Editor;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapRuntimeBindingSceneValidatorTests
{
    public static void RunFocusedValidation()
    {
        var tests = new OperationMapRuntimeBindingSceneValidatorTests();
        int passed = 0;
        try
        {
            tests.CombinedMeshBakerScriptResolves();
            passed++;
            tests.SourceScenePathTargetsOnlyThinRuntimeBindingScene();
            passed++;
            tests.GeneratedScenePassesStructuralValidation();
            passed++;
            tests.GeneratedSceneDoesNotDependOnAuthoringScene();
            passed++;
            tests.CandidateEntityScenePassesStructuralValidation();
            passed++;
            tests.CandidateEntitySceneDoesNotDependOnAuthoringOrStaticPresentationScenes();
            passed++;
            tests.CandidateEntitySceneRejectsAddedRenderer();
            passed++;
            tests.CandidateEntitySceneRejectsAddedCollider();
            passed++;
            tests.ProductionEntitySceneRejectsAddedRenderer();
            passed++;
            tests.ProductionEntitySceneRejectsMissingSurfaceOverlays();
            passed++;
            tests.GeneratedScenePreservesSourceSurfaceOverlays();
            passed++;
            tests.MissionOneDenseRuntimeBindingPreservesSourceSurfaceOverlays();
            passed++;
            tests.SerializedRoadOverlaysStayInsideActiveSurfaceBounds();
            passed++;
            tests.MissionOneDirtRoadOverlayMatchesVirtualizedRendererBounds();
            passed++;
            Debug.Log($"[OperationMapRuntimeBindingSceneValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[OperationMapRuntimeBindingSceneValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void CombinedMeshBakerScriptResolves()
    {
        const string path = "Assets/Game/Scripts/Tools/CombinedMeshBaker.cs";
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        Assert.That(script, Is.Not.Null, $"CombinedMeshBaker MonoScript is missing at {path}.");
        Assert.That(script.GetClass(), Is.EqualTo(typeof(CombinedMeshBaker)),
            $"CombinedMeshBaker MonoScript resolved '{script.GetClass()?.AssemblyQualifiedName ?? "<null>"}' " +
            $"instead of '{typeof(CombinedMeshBaker).AssemblyQualifiedName}'.");
    }

    [Test]
    public void SourceScenePathTargetsOnlyThinRuntimeBindingScene()
    {
        Assert.That(
            OperationMapAddressablesLayoutBuilder.SourceScenePath,
            Is.EqualTo(OperationMapRuntimeBindingSceneBuilder.OutputPath));
        Assert.That(
            OperationMapAddressablesLayoutBuilder.SourceScenePath,
            Is.Not.EqualTo(OperationMapAddressablesLayoutBuilder.AuthoringScenePath));
        Assert.That(
            OperationMapAddressablesLayoutBuilder.SourceScenePath,
            Is.Not.EqualTo(OperationMapAddressablesLayoutBuilder.SourceSubScenePath));
        Assert.That(
            AssetDatabase.LoadAssetAtPath<SceneAsset>(OperationMapAddressablesLayoutBuilder.SourceScenePath),
            Is.Not.Null);
    }

    [Test]
    public void GeneratedScenePassesStructuralValidation()
    {
        Scene scene = EditorSceneManager.OpenScene(
            OperationMapRuntimeBindingSceneBuilder.OutputPath,
            OpenSceneMode.Single);
        try
        {
            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    OperationMapAddressablesLayoutBuilder.DefinitionPath,
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                    out string error),
                Is.True,
                error);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void GeneratedSceneDoesNotDependOnAuthoringScene()
    {
        string[] dependencies = AssetDatabase.GetDependencies(
            OperationMapRuntimeBindingSceneBuilder.OutputPath,
            true);

        Assert.That(
            dependencies,
            Does.Not.Contain(StaticMapPresentationBaker.CurrentStagedOperationMapScenePath));
        Assert.That(
            dependencies.Any(path =>
                path.Contains("GeneratedStaticMapPresentation", StringComparison.Ordinal) &&
                path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)),
            Is.False,
            "Presentation chunk scenes must remain independently streamed dependencies.");
    }

    [Test]
    public void CandidateEntityScenePassesStructuralValidation()
    {
        Scene scene = OpenCandidateEntityScene();
        try
        {
            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                    out string error),
                Is.True,
                error);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void CandidateEntitySceneDoesNotDependOnAuthoringOrStaticPresentationScenes()
    {
        string[] dependencies = AssetDatabase.GetDependencies(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
            true);

        Assert.That(
            dependencies,
            Does.Not.Contain(OperationMapAddressablesLayoutBuilder.AuthoringScenePath));
        Assert.That(
            dependencies.Any(path =>
                path.Contains(
                    "GeneratedStaticMapPresentation",
                    StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)),
            Is.False);
    }

    [Test]
    public void CandidateEntitySceneRejectsAddedRenderer()
    {
        Scene scene = OpenCandidateEntityScene();
        try
        {
            OperationMapSceneView view = FindSingleView(scene);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(view.MapRoot, false);

            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("renderer"));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void CandidateEntitySceneRejectsAddedCollider()
    {
        Scene scene = OpenCandidateEntityScene();
        try
        {
            OperationMapSceneView view = FindSingleView(scene);
            var colliderRoot = new GameObject("UnexpectedCollider");
            colliderRoot.AddComponent<BoxCollider>();
            colliderRoot.transform.SetParent(view.MapRoot, false);

            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("collider"));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void ProductionEntitySceneRejectsAddedRenderer()
    {
        Scene scene = EditorSceneManager.OpenScene(
            OperationMapRuntimeBindingSceneBuilder.OutputPath,
            OpenSceneMode.Single);
        try
        {
            OperationMapSceneView view = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true))
                .Single();
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(view.MapRoot, false);

            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    OperationMapAddressablesLayoutBuilder.DefinitionPath,
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("renderer"));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void ProductionEntitySceneRejectsMissingSurfaceOverlays()
    {
        Scene scene = EditorSceneManager.OpenScene(
            OperationMapRuntimeBindingSceneBuilder.OutputPath,
            OpenSceneMode.Single);
        try
        {
            OperationMapSceneView view = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true))
                .Single();
            OperationMapRuntimeBindingSceneBuilder.ApplySurfaceSceneOverlays(
                view.MapSurfaceAuthoring,
                Array.Empty<MapSurfaceSceneOverlayAuthoringData>());

            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    OperationMapAddressablesLayoutBuilder.DefinitionPath,
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("road surface overlays"));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void GeneratedScenePreservesSourceSurfaceOverlays()
    {
        AssertSurfaceOverlayParity(
            DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
            OperationMapRuntimeBindingSceneBuilder.OutputPath);
    }

    [Test]
    public void MissionOneDenseRuntimeBindingPreservesSourceSurfaceOverlays()
    {
        AssertSurfaceOverlayParity(
            DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                .DenseCandidateRuntimeBindingPath);
    }

    private static void AssertSurfaceOverlayParity(string sourceScenePath, string runtimeScenePath)
    {
        MapSurfaceSceneOverlayAuthoringData[] expected =
            OperationMapRuntimeBindingSceneBuilder.CaptureSurfaceSceneOverlays(sourceScenePath);

        Scene runtimeScene = EditorSceneManager.OpenScene(
            runtimeScenePath,
            OpenSceneMode.Single);
        try
        {
            MapSurfaceSceneOverlayAuthoringData[] actual =
                FindSingleView(runtimeScene).MapSurfaceAuthoring.SceneOverlays;
            Assert.That(expected.Length, Is.GreaterThan(0));
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index].Center, Is.EqualTo(expected[index].Center));
                Assert.That(actual[index].Rotation, Is.EqualTo(expected[index].Rotation));
                Assert.That(actual[index].HalfExtents, Is.EqualTo(expected[index].HalfExtents));
                Assert.That(actual[index].Height, Is.EqualTo(expected[index].Height).Within(0.0001f));
                Assert.That(actual[index].SurfaceType, Is.EqualTo(expected[index].SurfaceType));
                Assert.That(actual[index].MovementMask, Is.EqualTo(expected[index].MovementMask));
                Assert.That(actual[index].Flags, Is.EqualTo(expected[index].Flags));
                Assert.That(actual[index].LayerId, Is.EqualTo(expected[index].LayerId));
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(runtimeScene, true);
        }
    }

    [Test]
    public void SerializedRoadOverlaysStayInsideActiveSurfaceBounds()
    {
        MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
            OperationMapAddressablesLayoutBuilder.MapSurfacePath);
        Assert.That(surface, Is.Not.Null);
        Vector3 min = surface.GridOrigin;
        Vector3 max = min + new Vector3(
            surface.Dimensions.x * surface.CellSize,
            0f,
            surface.Dimensions.y * surface.CellSize);
        MapSurfaceSceneOverlayAuthoringData[] overlays =
            OperationMapRuntimeBindingSceneBuilder.CaptureSurfaceSceneOverlays(
                DenseCityCandidateAuthoringTransaction.CandidateMapScenePath);

        Assert.That(overlays.Length, Is.GreaterThan(0));
        for (int index = 0; index < overlays.Length; index++)
        {
            MapSurfaceSceneOverlayAuthoringData overlay = overlays[index];
            Assert.That(overlay.Center.x + overlay.HalfExtents.x, Is.GreaterThan(min.x));
            Assert.That(overlay.Center.x - overlay.HalfExtents.x, Is.LessThan(max.x));
            Assert.That(overlay.Center.z + overlay.HalfExtents.y, Is.GreaterThan(min.z));
            Assert.That(overlay.Center.z - overlay.HalfExtents.y, Is.LessThan(max.z));
        }
    }

    [Test]
    public void MissionOneDirtRoadOverlayMatchesVirtualizedRendererBounds()
    {
        MapSurfaceSceneOverlayAuthoringData[] overlays =
            OperationMapRuntimeBindingSceneBuilder.CaptureVirtualizedRoadSurfaceOverlays();
        Assert.That(
            Array.Exists(
                overlays,
                overlay =>
                    overlay.SurfaceType == MapSurfaceType.DirtRoad &&
                    Mathf.Abs(overlay.Center.x - 1790.856f) <= 0.01f &&
                    Mathf.Abs(overlay.Center.z - 692.928f) <= 0.01f &&
                    Mathf.Abs(overlay.Height - 0.418999f) <= 0.01f),
            Is.True,
            "Mission 1 dirt road must use the exact virtualized renderer bounds.");
    }

    private static Scene OpenCandidateEntityScene()
    {
        Assert.That(
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath),
            Is.Not.Null,
            "Candidate EntityScene runtime binding must exist before validation.");
        return EditorSceneManager.OpenScene(
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
            OpenSceneMode.Single);
    }

    private static OperationMapSceneView FindSingleView(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true))
            .Single();
    }
}
