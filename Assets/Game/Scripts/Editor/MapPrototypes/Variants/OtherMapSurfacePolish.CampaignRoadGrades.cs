using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static Mesh SaveCampaignGradeMesh(string path,Mesh mesh)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }
        sealed class RoadGradeEnd { public MeshRenderer renderer;public Vector3 point,direction;public float width;public int side;public float delta; }
        static int GradeCampaignRoadJoins(string map,MeshRenderer[] all,OperationMapDefinition def,List<string> journal,List<Tuple<string,Vector3,float>> views)
        {
            var roads=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.name=="Asphalt"&&r.transform.parent.name=="Road_Asphalt_With_Sidewalk_Straight"&&Within(r,def,80)&&r.bounds.size.y<.1f&&!AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh).StartsWith(FixArt+"/RoadGrades/")&&r.transform.parent.GetComponentsInChildren<Collider>(true).Length==0).ToArray();
            var ends=new List<RoadGradeEnd>();
            foreach(var r in roads)
            {
                var axis=r.transform.TransformVector(Vector3.up);axis.y=0;axis.Normalize();if(Mathf.Max(Mathf.Abs(axis.x),Mathf.Abs(axis.z))<.999f)continue;
                var b=r.bounds;float half=Mathf.Abs(axis.x)*b.extents.x+Mathf.Abs(axis.z)*b.extents.z,width=Mathf.Abs(axis.x)*b.size.z+Mathf.Abs(axis.z)*b.size.x;
                foreach(int side in new[]{-1,1})ends.Add(new RoadGradeEnd{renderer=r,point=b.center+axis*side*half,direction=axis*side,width=width,side=side});
            }
            Vector2Int Key(Vector3 p)=>new Vector2Int(Mathf.RoundToInt(p.x*10),Mathf.RoundToInt(p.z*10));
            var bins=ends.ToLookup(e=>Key(e.point));var done=new HashSet<RoadGradeEnd>();var occluders=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&(NaturalRock(r)||MeshName(r).Contains("SandDunes")||IsBuilding(r))).ToArray();int pairs=0;
            foreach(var e in ends)
            {
                if(done.Contains(e))continue;var key=Key(e.point);var neighbors=new List<RoadGradeEnd>();
                for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)neighbors.AddRange(bins[new Vector2Int(key.x+x,key.y+z)]);
                var candidates=neighbors.Where(n=>n.renderer!=e.renderer&&Vector3.Dot(n.direction,e.direction)<-.999f&&Mathf.Abs(n.width-e.width)<.05f&&new Vector2(n.point.x-e.point.x,n.point.z-e.point.z).sqrMagnitude<.0064f).ToArray();
                if(candidates.Length!=1)continue;var n=candidates[0];float difference=n.point.y-e.point.y;
                if(Mathf.Abs(difference)<.12f||Mathf.Abs(difference)>4||!CampaignCurbVisible(new Vector3(e.point.x,Mathf.Max(e.point.y,n.point.y),e.point.z),occluders))continue;
                e.delta=difference*.5f;n.delta=-difference*.5f;done.Add(e);done.Add(n);pairs++;
                journal.Add("GRADED ROAD JOIN "+e.point+" to="+n.point+" commonHeight="+(e.point.y+e.delta)+" road="+Hierarchy(e.renderer.transform));
                if(views.Count(v=>v.Item1.StartsWith("graded-join-"))<4)views.Add(Tuple.Create("graded-join-"+pairs,e.point,55f));
            }
            Directory.CreateDirectory(FixArt+"/RoadGrades");Directory.CreateDirectory(FixArt+"/TerminalCurbs");AssetDatabase.Refresh();
            var unique=new HashSet<string>();AssetDatabase.StartAssetEditing();
            try
            {
                foreach(var group in ends.GroupBy(e=>e.renderer).Where(g=>g.Any(e=>Mathf.Abs(e.delta)>.001f)))
                {
                    var road=group.Key;var minus=group.Single(e=>e.side==-1);var plus=group.Single(e=>e.side==1);var axis=plus.direction;float length=Vector3.Dot(plus.point-minus.point,axis);
                    foreach(var r in road.transform.parent.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var mf=r.GetComponent<MeshFilter>();if(mf?.sharedMesh==null)continue;
                        if(r!=road&&!MeshName(r).Contains("Sidewalk"))throw new Exception("Unexpected road grade child "+Hierarchy(r.transform));
                        var original=mf.sharedMesh;var mesh=UnityEngine.Object.Instantiate(original);var vertices=mesh.vertices;var before=r.bounds;
                        for(int i=0;i<vertices.Length;i++)
                        {var world=r.transform.TransformPoint(vertices[i]);float t=Mathf.Clamp01(Vector3.Dot(world-minus.point,axis)/length);world.y+=Mathf.Lerp(minus.delta,plus.delta,t);vertices[i]=r.transform.InverseTransformPoint(world);}
                        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();string id=TextHash(GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).Substring(0,20);
                        if(!unique.Add(id))throw new Exception("Road grade instance collision");mf.sharedMesh=SaveCampaignGradeMesh(FixArt+"/RoadGrades/"+map+"_"+id+".asset",mesh);mf.sharedMesh.name=original.name;EditorUtility.SetDirty(mf.sharedMesh);AssetDatabase.SaveAssetIfDirty(mf.sharedMesh);EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
                        var after=r.bounds;if(Mathf.Abs(after.min.x-before.min.x)>.002f||Mathf.Abs(after.max.x-before.max.x)>.002f||Mathf.Abs(after.min.z-before.min.z)>.002f||Mathf.Abs(after.max.z-before.max.z)>.002f)throw new Exception("Road grade changed XZ footprint");
                    }
                }
            }
            finally{AssetDatabase.StopAssetEditing();}
            foreach(var e in done)
            {
                var edge=e.renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(e.renderer.transform.TransformPoint).Where(p=>Mathf.Abs(Vector3.Dot(p-e.point,e.direction))<.01f).ToArray();
                if(edge.Length<2||edge.Any(p=>Mathf.Abs(p.y-e.point.y-e.delta)>.005f))throw new Exception("Graded road seam did not meet paired edge: "+Hierarchy(e.renderer.transform));
            }
            journal.Add("roadGradeSeamValidation=Passed pairedEnds="+done.Count+" maxAllowedHeightError=0.005m");return pairs;
        }
    }
}
