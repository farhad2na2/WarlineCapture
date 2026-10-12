using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        public static void RestoreRejectedCurbPass()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int total=0;
            var original=AssetDatabase.LoadAllAssetsAtPath("Assets/PolygonMilitary/Models/SM_Env_Sidewalk_Straight_02.fbx").OfType<Mesh>().Single(m=>m.name=="SM_Env_Sidewalk_Straight_02");
            try
            {
                foreach(var map in VisualMaps)
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var protection=new RefinementProtection(scene);int count=0;
                    foreach(var r in SceneRenderers(scene))
                    {
                        var mf=r.GetComponent<MeshFilter>();if(mf?.sharedMesh==null||!AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith(FixArt+"/Curbs/",StringComparison.Ordinal))continue;
                        var b=mf.sharedMesh.bounds;var o=original.bounds;
                        if(Mathf.Abs(b.min.x-o.min.x)>.001f||Mathf.Abs(b.max.x-o.max.x)>.001f||Mathf.Abs(b.min.z-o.min.z)>.001f||Mathf.Abs(b.max.z-o.max.z)>.001f)throw new Exception("Curb rollback source footprint mismatch "+Hierarchy(r.transform));
                        mf.sharedMesh=original;EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);count++;
                    }
                    string hash=protection.Validate(scene);if(count>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Curb rollback source protection failed");}
                    Debug.Log("[CampaignCurbRollback] map="+map+" restored="+count);total+=count;
                }
                File.WriteAllText(FixReport+"/curbs/REJECTED.txt","First curb pass rejected during visual review: false exposed-end classification at connected corners produced gaps. All owned modified meshes restored to their original model mesh via Unity. Evidence retained.\n");
                Debug.Log("[CampaignCurbRollback] result=Passed restored="+total);
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void BuildCampaignDestinationReview()
        {
            BuildCampaignRunwayEnds();BuildCampaignDestinationDressing();BuildCampaignCurbTransitions();CaptureCampaignFixDetails();
            Game.Editor.CampaignRenderReconciliation20261011.Inspect();
            Debug.Log("[CampaignDestinationReview] result=Passed");
        }
    }
}
