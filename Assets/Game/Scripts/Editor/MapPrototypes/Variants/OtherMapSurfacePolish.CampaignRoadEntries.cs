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
        public static void FinishCampaignDirtRoadEntries()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int total=0;
            string report=FixReport+"/road-entry-grade-01";Directory.CreateDirectory(report);
            try
            {
                foreach(string map in VisualMaps.OrderBy(m=>m=="DenseCity"?0:1))
                {
                    if(File.Exists(report+"/"+map+".txt"))continue;
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var all=SceneRenderers(scene);var protection=new RefinementProtection(scene);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var playable=new Rect(def.Bounds.PlayableMin.x-30,def.Bounds.PlayableMin.z-30,def.Bounds.PlayableMax.x-def.Bounds.PlayableMin.x+60,def.Bounds.PlayableMax.z-def.Bounds.PlayableMin.z+60);
                    var visible=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&playable.Overlaps(XzRect(r.bounds))).ToArray();
                    var roads=visible.Where(r=>r.name=="Asphalt"||MeshName(r).Contains("Sidewalk")).ToArray();
                    var entries=visible.Where(r=>MeshName(r).Contains("DirtRoad")).ToArray();
                    int changed=GradeRunwayDunes(map,entries,roads,"RoadEntries",1,1,true);
                    string hash=protection.Validate(scene);
                    if(changed>0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Road entry protected data changed");
                        var views=entries.Where(r=>AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).StartsWith(FixArt+"/RoadEntries/")).Take(2).Select((r,i)=>Tuple.Create("graded-entry-"+i,r.bounds.center,45f)).ToList();
                        if(map=="DenseCity")views.AddRange(new[]{Tuple.Create("market-left",new Vector3(650,0,430),45f),Tuple.Create("market-right",new Vector3(782,0,430),45f)});
                        Capture(map,scene,def,all,"after",report,views.ToArray());
                    }
                    File.WriteAllText(report+"/"+map+".txt","gradedDirtRoadEntries="+changed+"\nprotectedHash="+hash+"\ncollisionRoadFootprintsAndGameplayOwners=unchanged\n");
                    Debug.Log("[CampaignRoadEntries] map="+map+" graded="+changed);total+=changed;
                }
                CampaignRenderReconciliation20261011.Inspect();
                Debug.Log("[CampaignRoadEntries] result=Passed graded="+total+" sources=11 packedRebuildRequired=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
