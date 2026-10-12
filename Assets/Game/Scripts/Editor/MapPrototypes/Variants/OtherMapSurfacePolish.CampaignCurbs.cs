using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        sealed class CurbEnd { public MeshRenderer renderer; public bool left;public Vector3 end;public float roadHeight; }
        static bool FlatCurb(MeshRenderer r)=>MeshName(r)=="SM_Env_Sidewalk_Straight_02" && !AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh).StartsWith(FixArt+"/CurbsV2/") && !AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh).StartsWith(FixArt+"/CurbsV3/") && Mathf.Abs(Vector3.Dot(r.transform.up,Vector3.up))>.9999f;
        static bool PlanarContains(Bounds b,Vector3 p,float margin)=>p.x>=b.min.x-margin&&p.x<=b.max.x+margin&&p.z>=b.min.z-margin&&p.z<=b.max.z+margin;
        static IEnumerable<CurbEnd> FindCurbEnds(MeshRenderer[] rs,OperationMapDefinition def)
        {
            var curbs=rs.Where(r=>MeshName(r).Contains("Sidewalk")&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
            var roads=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(MeshName(r).Contains("DirtRoad")||r.name=="Asphalt")&&r.bounds.size.y<.8f).ToArray();
            var bins=new Dictionary<Vector2Int,List<MeshRenderer>>();
            foreach(var r in curbs.Concat(roads))
                for(int x=Mathf.FloorToInt(r.bounds.min.x/10)-1;x<=Mathf.FloorToInt(r.bounds.max.x/10)+1;x++)
                for(int z=Mathf.FloorToInt(r.bounds.min.z/10)-1;z<=Mathf.FloorToInt(r.bounds.max.z/10)+1;z++)
                {var key=new Vector2Int(x,z);if(!bins.TryGetValue(key,out var list))bins[key]=list=new List<MeshRenderer>();list.Add(r);}
            foreach(var r in curbs.Where(r=>FlatCurb(r)&&Within(r,def)&&r.GetComponentsInChildren<Collider>(true).Length==0))
            {
                var b=r.GetComponent<MeshFilter>().sharedMesh.bounds;
                foreach(bool left in new[]{true,false})
                {
                    var end=r.transform.TransformPoint(new Vector3(left?b.min.x:b.max.x,b.max.y,b.center.z));
                    var direction=r.transform.TransformVector(Vector3.right).normalized*(left?-1:1);var sample=end+direction*.4f;
                    if(!bins.TryGetValue(new Vector2Int(Mathf.FloorToInt(sample.x/10),Mathf.FloorToInt(sample.z/10)),out var near))continue;
                    if(near.Any(n=>n!=r&&MeshName(n).Contains("Sidewalk")&&Mathf.Abs(n.bounds.max.y-end.y)<.7f&&PlanarContains(n.bounds,sample,.12f)))continue;
                    // A legal lowered end is only created where the authored road continues.
                    // Keep isolated footpaths and terrain-following slopes untouched.
                    var support=near.Where(n=>!MeshName(n).Contains("Sidewalk")&&PlanarContains(n.bounds,end+direction*2,1.25f)&&n.bounds.max.y<end.y-.04f&&n.bounds.max.y>end.y-.55f).OrderByDescending(n=>n.bounds.max.y).FirstOrDefault();
                    if(support==null)continue;
                    yield return new CurbEnd{renderer=r,left=left,end=end,roadHeight=support.bounds.max.y+.015f};
                }
            }
        }
        static int FinishCampaignCurbs(string map,MeshRenderer[] rs,OperationMapDefinition def,List<Tuple<string,Vector3,float>> views,List<string> ledger)
        {
            int total=0;var unique=new HashSet<string>();
                    var surface=AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(AssetDatabase.GUIDToAssetPath(def.MapSurfaceDataReference.AssetGUID));
                    if(surface==null||!surface.TryCreateRuntimeBlobAsset(Allocator.Temp,out BlobAssetReference<MapSurfaceBlob> blob))throw new Exception("Missing curb surface: "+map);
                    var occluders=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(NaturalRock(r)||MeshName(r).Contains("SandDunes")||IsBuilding(r))).ToArray();
                    CurbEnd[] ends;
                    using(blob)ends=FindCurbEnds(rs,def).Where(e=>
                    {
                        string path=Hierarchy(e.renderer.transform);
                        if(path.Contains("_Corner/")||path.Contains("_Intersection/")||!CampaignCurbVisible(e.end,occluders))return false;
                        int2 cell=(int2)math.floor((new float2(e.end.x,e.end.z)-blob.Value.GridOrigin.xz)/blob.Value.CellSize);
                        return MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob.Value,cell,out var sample)&&sample.Height<e.end.y+.2f;
                    }).ToArray();
                    views.AddRange(ends.Take(3).Select((e,i)=>Tuple.Create("transition-"+i,e.end,40f)));
                    foreach(var group in ends.GroupBy(e=>e.renderer))
                    {
                        var r=group.Key;var mf=r.GetComponent<MeshFilter>();var source=mf.sharedMesh;var mesh=UnityEngine.Object.Instantiate(source);var vertices=mesh.vertices;var b=source.bounds;
                        foreach(var end in group)
                        {
                            float target=r.transform.InverseTransformPoint(new Vector3(end.end.x,end.roadHeight,end.end.z)).y;
                            float distance=Mathf.Min(2.5f/Mathf.Abs(r.transform.lossyScale.x),b.size.x*.8f);
                            for(int v=0;v<vertices.Length;v++)
                            {
                                float fromEnd=end.left?vertices[v].x-b.min.x:b.max.x-vertices[v].x;
                                float amount=1-Mathf.Clamp01(fromEnd/distance);
                                // Move each existing section coherently; preserve XZ and the closed end face.
                                vertices[v].y+=(target-b.max.y)*amount;
                            }
                            ledger.Add(Hierarchy(r.transform)+" end="+end.end+" roadHeight="+end.roadHeight);total++;
                        }
                        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
                        string id=TextHash(GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).Substring(0,20);if(!unique.Add(id))throw new Exception("Curb instance asset collision");
                        mf.sharedMesh=SaveVisualMesh(FixArt+"/CurbsV3/"+map+"_"+id+".asset",mesh);mf.sharedMesh.name=source.name;EditorUtility.SetDirty(mf.sharedMesh);AssetDatabase.SaveAssetIfDirty(mf.sharedMesh);EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
                    }
            return total;
        }
        public static void BuildCampaignCurbTransitions()
        {
            Check();Directory.CreateDirectory(FixReport+"/curbs-v3");var setup=EditorSceneManager.GetSceneManagerSetup();int total=0;
            try
            {
                foreach(var map in VisualMaps)
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var rs=SceneRenderers(scene);var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var protection=new RefinementProtection(scene);
                    var views=new List<Tuple<string,Vector3,float>>();var ledger=new List<string>();int count=FinishCampaignCurbs(map,rs,def,views,ledger);total+=count;
                    string hash=protection.Validate(scene);if(count>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)||hash!=protection.Validate(scene))throw new Exception("Curb protected data changed");Capture(map,scene,def,rs,"after",FixReport+"/curbs-v3",views.ToArray());}
                    File.WriteAllLines(FixReport+"/curbs-v3/"+map+".txt",ledger);Debug.Log("[CampaignCurbTransitionsV3] map="+map+" loweredEnds="+count);
                }
                Debug.Log("[CampaignCurbTransitionsV3] result=Passed ends="+total);
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
