using System;
using System.Linq;
using Game.Components;
using Game.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class MissionSurfaceContactRepair
    {
        // Refresh only contact data in the two current dense Campaign bindings.
        // Keep existing roads, grid, physical scenery and all character/UI assets intact.
        public static void RebuildGeneratedPavingSupport()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Game.Authoring.MapSurfaceMeshOverlayBuilder.ClearCachedGeometry();
                var paving = OperationMapRuntimeBindingSceneBuilder.CaptureGeneratedPavingSupport(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
                if (paving.Length == 0 || paving.Any(o => (o.Flags & MapSurfaceFlags.ExactMesh) == 0 ||
                        o.Geometry.Triangles.Length == 0))
                    throw new InvalidOperationException("Generated paving requires actual rendered mesh triangles.");
                if (!paving.Any(o => OperationMapRuntimeBindingSceneBuilder.GroundGeometrySourcePath(o.Geometry).EndsWith("/DenseCity_PavingBridgeCutouts.asset", StringComparison.Ordinal)) ||
                    !paving.Any(o => OperationMapRuntimeBindingSceneBuilder.GroundGeometrySourcePath(o.Geometry).EndsWith("/DenseCity_KerbPaving.asset", StringComparison.Ordinal)))
                    throw new InvalidOperationException("Current Campaign paving or kerb support is missing from the authored presentation.");
                foreach (string path in new[] { OperationMapRuntimeBindingSceneBuilder.OutputPath,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath })
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OperationMapSceneView>(true)).Single();
                    var original = view.MapSurfaceAuthoring.SceneOverlays;
                    var retained = original.Where(o => o.Geometry == null ||
                        !OperationMapRuntimeBindingSceneBuilder.IsGeneratedPavingGeometry(o.Geometry)).ToArray();
                    var combined = retained.Concat(paving).ToArray();
                    OperationMapRuntimeBindingSceneBuilder.ApplySurfaceSceneOverlays(view.MapSurfaceAuthoring, combined);
                    if (view.MapSurfaceAuthoring.SceneOverlays.Length != retained.Length + paving.Length)
                        throw new InvalidOperationException("Paving contact publication lost existing overlays.");
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Failed to save paving contact binding: " + path);
                    Debug.Log($"[GeneratedPavingContactRepair] path={path} retained={retained.Length} paving={paving.Length}");
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[GeneratedPavingContactRepair] result=Passed physicalGeometryChanged=0 profilesChanged=0");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        public static void Build()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{OperationMapRuntimeBindingSceneBuilder.OutputPath,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath})
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    var view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapSceneView>(true)).Single();
                    string source=path==OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath
                        ? DenseCityCandidateAuthoringTransaction.CandidateMapScenePath : StaticMapPresentationBaker.CurrentStagedOperationMapScenePath;
                    var overlays=OperationMapRuntimeBindingSceneBuilder.CaptureSurfaceSceneOverlays(source);
                    if(overlays.Length==0||overlays.Any(o=>(o.Flags&MapSurfaceFlags.ExactMesh)==0))throw new InvalidOperationException("Road support must come from actual mesh faces: "+path);
                    scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    view=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapSceneView>(true)).Single();
                    OperationMapRuntimeBindingSceneBuilder.ApplySurfaceSceneOverlays(view.MapSurfaceAuthoring,overlays);
                    if(view.MapSurfaceAuthoring.SceneOverlays.Any(o=>o.Geometry==null || o.Geometry.Triangles.Length==0))throw new InvalidOperationException("Road mesh data missing after publication.");
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    Debug.Log("[MissionSurfaceContactRepair] path="+path+" meshPlacements="+overlays.Length);
                }
                UnitGroundContactProfileBuilder.Build();
                MissionSupportUiStyle.Build();
                AssetDatabase.SaveAssets();
                Debug.Log("[MissionSurfaceContactRepair] result=Passed");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
