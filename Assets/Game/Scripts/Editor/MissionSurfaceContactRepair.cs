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
