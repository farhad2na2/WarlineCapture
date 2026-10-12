using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static int RestoreSharedCampaignAssets(string map,MeshRenderer[] rs,List<string> journal)
        {
            var affected=rs.Where(r=>{var mf=r.GetComponent<MeshFilter>();return (mf!=null&&new[]{"CurbsV2/","CanalEnds/"}.Any(d=>AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith(FixArt+"/"+d)))||r.sharedMaterials.Any(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(FixArt+"/RoadEnds/"));}).ToArray();
            if(affected.Length==0)return 0;
            var census=JsonUtility.FromJson<SceneInspection>(File.ReadAllText(FixReport+"/inspection/"+map+".json"));
            var rows=census.renderers.ToLookup(r=>r.path);int count=0;
            var curb=AssetDatabase.LoadAllAssetsAtPath("Assets/PolygonMilitary/Models/SM_Env_Sidewalk_Straight_02.fbx").OfType<Mesh>().Single(m=>m.name=="SM_Env_Sidewalk_Straight_02");
            foreach(var r in affected)
            {
                var mf=r.GetComponent<MeshFilter>();string meshPath=mf==null?"":AssetDatabase.GetAssetPath(mf.sharedMesh);
                bool canal=meshPath.StartsWith(FixArt+"/CanalEnds/");bool road=r.sharedMaterials.Any(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(FixArt+"/RoadEnds/"));
                RenderInspection row=null;
                if(canal||road)
                {
                    var matches=rows[Hierarchy(r.transform)].Where(v=>(v.position-r.transform.position).sqrMagnitude<.000001f&&(v.scale-r.transform.lossyScale).sqrMagnitude<.000001f).ToArray();
                    if(matches.Length!=1)throw new Exception("Original render row ambiguous "+map+" "+Hierarchy(r.transform)+" matches="+matches.Length);row=matches[0];
                }
                if(meshPath.StartsWith(FixArt+"/CurbsV2/"))mf.sharedMesh=curb;
                if(canal){var original=AssetDatabase.LoadAssetAtPath<Mesh>(RefinementArt+"/WaterMeshes/"+row.mesh+".asset");if(original==null)throw new Exception("Missing original canal mesh "+row.mesh);mf.sharedMesh=original;}
                if(road){var materials=row.materials.Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();if(materials.Any(m=>m==null)||materials.Length!=r.sharedMaterials.Length)throw new Exception("Original road material mismatch");r.sharedMaterials=materials;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                if(mf!=null){EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);}journal.Add("RESTORED INSTANCE BASELINE "+Hierarchy(r.transform)+" oldMesh="+meshPath);count++;
            }
            return count;
        }
        public static void FinalizeCampaignDestinationOwnership()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();int moved=0;var journal=new List<string>();
            try
            {
                var maps=VisualMaps.ToArray();Directory.CreateDirectory(FixReport+"/source-final");
                foreach(var map in maps)
                {
                    if(File.Exists(FixReport+"/source-final/"+map+".txt")){Debug.Log("[CampaignFinalSourceResume] retained="+map);continue;}
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var protection=new RefinementProtection(scene);var rs=SceneRenderers(scene);var def=AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapDefinition>(Definition(map));int movedBefore=moved;
                    var groups=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("CampaignDestinationV2_20261011_",StringComparison.Ordinal)).ToArray();
                    foreach(var group in groups)
                    {
                        if(!group.parent.GetComponentsInParent<Transform>(true).Any(t=>t.name.StartsWith("CampaignDestinationV2_20261011_",StringComparison.Ordinal)))continue;
                        var dense=group.GetComponentInParent<DenseCityPresentationIdentityAuthoring>();var accepted=group.GetComponentInParent<OperationMapEntityPresentationIdentityAuthoring>();
                        var owner=dense!=null?dense.transform:accepted!=null?accepted.transform:null;
                        if(owner==null||(dense!=null?dense.Role:accepted.Role)!=OperationMapEntityPresentationRole.RenderOnly)throw new Exception("Destination has no render-only owner "+Hierarchy(group));
                        var renderers=group.GetComponentsInChildren<MeshRenderer>(true);var bounds=renderers.Select(r=>r.bounds).ToArray();string before=Hierarchy(group.parent);
                        group.SetParent(owner,true);protection.Reparented.Add(group.GetEntityId().ToString());protection.Moved.Add(group.GetEntityId().ToString());
                        for(int i=0;i<renderers.Length;i++)if((renderers[i].bounds.center-bounds[i].center).sqrMagnitude>.000004f||(renderers[i].bounds.size-bounds[i].size).sqrMagnitude>.000004f)throw new Exception("Reparenting changed destination geometry "+group.name);
                        EditorUtility.SetDirty(group);PrefabUtility.RecordPrefabInstancePropertyModifications(group);journal.Add(map+" "+group.name+" from="+before+" canonicalOwner="+Hierarchy(owner));moved++;
                    }
                    var mapJournal=new List<string>();var views=new List<Tuple<string,Vector3,float>>();
                    int restored=RestoreSharedCampaignAssets(map,rs,mapJournal);
                    int industrial=map=="AshLinePort"||map=="RefineryDistrict"||map=="DenseCity"?GroupCampaignLooseScenery(map,rs,def,protection,mapJournal):0;
                    int curbs=FinishCampaignCurbs(map,rs,def,views,mapJournal);
                    int canals=FinishCampaignCanalEnds(map,rs,mapJournal,views);
                    int roadEnds=FinishCampaignRoadTerminals(map,rs,def,mapJournal,views);
                    string hash=protection.Validate(scene);
                    if(moved>movedBefore||industrial+roadEnds+restored+curbs+canals>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Destination ownership finalize failed "+map);}
                    if(industrial+roadEnds+restored+curbs+canals>0)Capture(map,scene,def,rs,"after",FixReport+"/source-final",views.ToArray());
                    File.WriteAllLines(FixReport+"/source-final/"+map+".txt",mapJournal);
                    Debug.Log("[CampaignFinalSource] map="+map+" restored="+restored+" curbs="+curbs+" canals="+canals+" industrialGroups="+industrial+" roadEnds="+roadEnds+" normalizedParents="+(moved-movedBefore));
                }
                File.WriteAllLines(FixReport+"/destinations/ownership-finalization.txt",journal);
                CaptureCampaignFixDetails();
                Game.Editor.CampaignRenderReconciliation20261011.Inspect();
                Debug.Log("[CampaignDestinationOwnership] result=Passed normalized="+moved+" worldGeometry=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
