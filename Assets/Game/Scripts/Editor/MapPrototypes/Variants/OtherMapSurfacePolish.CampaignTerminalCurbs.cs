using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        static int LowerCampaignTerminalCurbs(string map,MeshRenderer road,MeshRenderer[] ground,List<Vector4> ends,List<string> journal,List<Tuple<string,Vector3,float>> views)
        {
            if(ends.Count==0)return 0;int count=0;
            foreach(var r in road.transform.parent.GetComponentsInChildren<MeshRenderer>(true).Where(r=>(r==road||(MeshName(r)=="SM_Env_Sidewalk_Straight_02"&&Mathf.Abs(Vector3.Dot(r.transform.up,Vector3.up))>.9999f))&&r.GetComponentsInChildren<Collider>(true).Length==0))
            {
                var mf=r.GetComponent<MeshFilter>();if(AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith(FixArt+"/TerminalCurbs/"))continue;
                var original=mf.sharedMesh;var mesh=UnityEngine.Object.Instantiate(original);var vertices=mesh.vertices;bool changed=false;
                foreach(var e in ends)
                {
                    var direction=new Vector3(e.z,0,e.w);var point=new Vector3(e.x,r.bounds.max.y,e.y);
                    if(Mathf.Abs(direction.x)>.9f)point.z=r.bounds.center.z;else point.x=r.bounds.center.x;
                    if(!RunwayGroundSupport(ground,point+direction*.2f,point.y,out float height))throw new Exception("Terminal surface has no verified ground support: "+Hierarchy(r.transform));
                    if(height>point.y+.05f||point.y-height>1)continue;
                    float delta=height+.015f-point.y;
                    for(int i=0;i<vertices.Length;i++)
                    {var world=r.transform.TransformPoint(vertices[i]);float distance=Vector3.Dot(point-world,direction);float amount=1-Mathf.Clamp01(distance/2.5f);world.y+=delta*amount;vertices[i]=r.transform.InverseTransformPoint(world);}
                    changed=true;journal.Add("FINISHED TERMINAL SURFACE "+Hierarchy(r.transform)+" end="+point+" ground="+height);
                    if(views.Count(v=>v.Item1.StartsWith("terminal-curb-"))<3)views.Add(Tuple.Create("terminal-curb-"+views.Count,point,45f));
                }
                if(!changed){UnityEngine.Object.DestroyImmediate(mesh);continue;}
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();string id=TextHash(GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).Substring(0,20);
                mf.sharedMesh=SaveCampaignGradeMesh(FixArt+"/TerminalCurbs/"+map+"_"+id+".asset",mesh);mf.sharedMesh.name=original.name;EditorUtility.SetDirty(mf.sharedMesh);AssetDatabase.SaveAssetIfDirty(mf.sharedMesh);EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);count++;
            }
            return count;
        }
    }
}
