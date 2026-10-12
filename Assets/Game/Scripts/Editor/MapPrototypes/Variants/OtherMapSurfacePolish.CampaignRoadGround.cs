using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        public static void ClearCampaignRoadSandIntrusions()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int total=0;
            string report=FixReport+"/road-ground-v2";Directory.CreateDirectory(report);
            try
            {
                foreach(string map in VisualMaps)
                {
                    if(File.Exists(report+"/"+map+".txt"))continue;
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var all=SceneRenderers(scene);var protection=new RefinementProtection(scene);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var playable=new Rect(def.Bounds.PlayableMin.x-30,def.Bounds.PlayableMin.z-30,def.Bounds.PlayableMax.x-def.Bounds.PlayableMin.x+60,def.Bounds.PlayableMax.z-def.Bounds.PlayableMin.z+60);
                    var roads=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(r.name=="Asphalt"||MeshName(r).Contains("Sidewalk"))&&playable.Overlaps(XzRect(r.bounds,0))).ToArray();
                    var dunes=all.Where(r=>(MeshName(r).Contains("SandDunes")||MeshName(r).Contains("SandEdge"))&&r.enabled&&r.gameObject.activeInHierarchy&&playable.Overlaps(XzRect(r.bounds,0))).ToArray();
                    int changed=GradeRunwayDunes(map,dunes,roads,"RoadGround",1,1);
                    string hash=protection.Validate(scene);
                    if(changed>0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Road ground protected data changed");
                        var views=dunes.Where(r=>AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).StartsWith(FixArt+"/RoadGround/")).Take(6).Select((r,i)=>Tuple.Create("graded-"+i,r.bounds.center,50f)).ToList();
                        if(map=="DenseCity")views.Add(Tuple.Create("market-frontage",new Vector3(715,0,440),45f));
                        Capture(map,scene,def,all,"after",report,views.ToArray());
                    }
                    File.WriteAllText(report+"/"+map+".txt","gradedDunes="+changed+"\nprotectedHash="+hash+"\ncollisionAndRoadFootprints=unchanged\n");
                    Debug.Log("[CampaignRoadGround] map="+map+" gradedDunes="+changed);total+=changed;
                }
                CampaignRenderReconciliation20261011.Inspect();
                Debug.Log("[CampaignRoadGround] result=Passed gradedDunes="+total);
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
