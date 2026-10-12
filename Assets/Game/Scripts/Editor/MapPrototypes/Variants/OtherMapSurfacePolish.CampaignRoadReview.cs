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
        public static void ResumeCampaignSourceAndReview()
        {
            FinalizeCampaignDestinationOwnership();
            ReviewCampaignRoadTerminals();
        }
        public static void ReviewCampaignRoadTerminals()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int total=0;
            try
            {
                Directory.CreateDirectory(FixReport+"/road-terminal-review");
                foreach(var map in VisualMaps)
                {
                    if(File.Exists(FixReport+"/road-terminal-review/"+map+".txt")){Debug.Log("[CampaignRoadTerminalReview] retained="+map);continue;}
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var rs=SceneRenderers(scene);var protection=new RefinementProtection(scene);var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var roads=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(r.name=="Asphalt"||MeshName(r).Contains("DirtRoad"))).ToArray();
                    var journal=new List<string>();var views=new List<Tuple<string,Vector3,float>>();int changed=0,terminalCurbs=0;
                    var ground=rs.Where(r=>MeshName(r).StartsWith("Ground_")||MeshName(r).StartsWith("SM_Env_Ground_")||MeshName(r).StartsWith("SM_Env_SandDunes_")).ToArray();
                    int relocated=RepairCampaignDestinationRoadClearance(map,rs,def,protection,journal,views);
                    int graded=GradeCampaignRoadJoins(map,rs,def,journal,views);
                    foreach(var r in roads)
                    foreach(var m in r.sharedMaterials.Where(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(FixArt+"/RoadEndsV2/")))
                    {
                        var kept=new List<Vector4>();
                        foreach(string field in new[]{"_RoadTerminalA","_RoadTerminalB"})
                        {
                            var e=m.GetVector(field);if(e.z*e.z+e.w*e.w<.5f)continue;
                            var point=new Vector3(e.x,r.bounds.center.y,e.y);var ahead=point+new Vector3(e.z,0,e.w)*.8f;
                            if(!roads.Any(o=>o!=r&&PlanarContains(o.bounds,ahead,.15f))){kept.Add(e);continue;}
                            journal.Add("REMOVED FALSE TERMINAL "+Hierarchy(r.transform)+" at="+point+" reason=connected authored road at different height");views.Add(Tuple.Create("corrected-join-"+changed,point,50f));changed++;
                        }
                        if(kept.Count==0){m.SetFloat("_RoadTerminalStrength",0);m.SetVector("_RoadTerminalA",Vector4.zero);m.SetVector("_RoadTerminalB",Vector4.zero);}
                        else{m.SetVector("_RoadTerminalA",kept[0]);m.SetVector("_RoadTerminalB",kept.Count>1?kept[1]:Vector4.zero);}
                        Save(m);terminalCurbs+=LowerCampaignTerminalCurbs(map,r,ground,kept,journal,views);
                    }
                    string hash=protection.Validate(scene);
                    if(graded+terminalCurbs+relocated>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Road review protected data changed");}
                    if(changed+graded+terminalCurbs+relocated>0)Capture(map,scene,def,rs,"after",FixReport+"/road-terminal-review",views.ToArray());
                    File.WriteAllLines(FixReport+"/road-terminal-review/"+map+".txt",journal.Concat(new[]{"gradedJoins="+graded+" terminalSurfaces="+terminalCurbs+" removedFalseEnds="+changed+" relocatedDestinations="+relocated+" protectedHash="+hash}));total+=changed;
                    Debug.Log("[CampaignRoadTerminalReview] map="+map+" gradedJoins="+graded+" terminalSurfaces="+terminalCurbs+" removedFalseEnds="+changed+" relocatedDestinations="+relocated);
                }
                var dependencies=VisualMaps.SelectMany(map=>AssetDatabase.GetDependencies(Source(map),true)).ToHashSet();
                foreach(string name in new[]{"Curbs","CurbsV2","CanalEnds","RoadEnds"})
                {
                    string folder=FixArt+"/"+name;if(!AssetDatabase.IsValidFolder(folder))continue;
                    var paths=AssetDatabase.FindAssets("",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!AssetDatabase.IsValidFolder(p)).ToArray();
                    if(paths.Any(dependencies.Contains))throw new Exception("Rejected visual asset still referenced in source: "+folder);
                    if(!AssetDatabase.DeleteAsset(folder))throw new Exception("Could not remove rejected owned assets "+folder);
                }
                CampaignRenderReconciliation20261011.Inspect();
                Debug.Log("[CampaignRoadTerminalReview] result=Passed removedFalseEnds="+total+" rejectedAssetsUnreferenced=true");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
