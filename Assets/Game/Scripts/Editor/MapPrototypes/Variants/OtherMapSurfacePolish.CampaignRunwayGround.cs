using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        // Local render-only grading removes sand protruding through an existing runway.
        // No collider, transform, runway footprint or mission owner is changed.
        static int GradeRunwayDunes(string map,MeshRenderer[] rs,MeshRenderer[] strips,string assetFolder="RunwayGround",float shoulder=5,float maxEdge=4,bool includeDirtRoads=false)
        {
            int changed=0;
            foreach(var r in rs.Where(r=>(MeshName(r).Contains("SandDunes")||(assetFolder=="RoadGround"&&MeshName(r).Contains("SandEdge"))||(includeDirtRoads&&MeshName(r).Contains("DirtRoad")))&&r.enabled&&r.gameObject.activeInHierarchy))
            {
                var nearby=strips.Where(s=>XzRect(s.bounds,shoulder).Overlaps(XzRect(r.bounds,0))&&r.bounds.max.y>s.bounds.min.y-.08f).ToArray();
                if(nearby.Length==0)continue;
                if(r.GetComponentsInChildren<Collider>(true).Length!=0||r.GetComponentsInParent<Collider>(true).Length!=0)throw new InvalidOperationException("Runway dune has collision; requires a coordinated surface bake: "+Hierarchy(r.transform));
                var mf=r.GetComponent<MeshFilter>();var source=mf.sharedMesh;
                if(AssetDatabase.GetAssetPath(source).StartsWith(FixArt+"/"+assetFolder+"/"))continue;
                var positions=source.vertices;var uv=source.uv;var uv2=source.uv2;
                var output=new List<Vector3>();var outUv=new List<Vector2>();var outUv2=new List<Vector2>();var indices=new List<int>[source.subMeshCount];
                int adjusted=0;
                void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 la,Vector2 lb,Vector2 lc,int sub,int depth)
                {
                    var wa=r.transform.TransformPoint(a);var wb=r.transform.TransformPoint(b);var wc=r.transform.TransformPoint(c);
                    var triangle=new Bounds(wa,Vector3.zero);triangle.Encapsulate(wb);triangle.Encapsulate(wc);
                    bool overlap=nearby.Any(s=>XzRect(s.bounds,shoulder).Overlaps(XzRect(triangle,0)));
                    float ab=(wa-wb).sqrMagnitude,bc=(wb-wc).sqrMagnitude,ca=(wc-wa).sqrMagnitude;
                    if(overlap&&Mathf.Max(ab,bc,ca)>maxEdge*maxEdge&&depth<12)
                    {
                        if(ab>=bc&&ab>=ca){var m=(a+b)*.5f;var u=(ua+ub)*.5f;var l=(la+lb)*.5f;Triangle(a,m,c,ua,u,uc,la,l,lc,sub,depth+1);Triangle(m,b,c,u,ub,uc,l,lb,lc,sub,depth+1);}
                        else if(bc>=ca){var m=(b+c)*.5f;var u=(ub+uc)*.5f;var l=(lb+lc)*.5f;Triangle(a,b,m,ua,ub,u,la,lb,l,sub,depth+1);Triangle(a,m,c,ua,u,uc,la,l,lc,sub,depth+1);}
                        else{var m=(c+a)*.5f;var u=(uc+ua)*.5f;var l=(lc+la)*.5f;Triangle(a,b,m,ua,ub,u,la,lb,l,sub,depth+1);Triangle(m,b,c,u,ub,uc,l,lb,lc,sub,depth+1);}return;
                    }
                    Vector3 Grade(Vector3 local)
                    {
                        var w=r.transform.TransformPoint(local);float before=w.y;
                        foreach(var strip in nearby)
                        {
                            var b=strip.bounds;float dx=Mathf.Max(b.min.x-w.x,0,w.x-b.max.x),dz=Mathf.Max(b.min.z-w.z,0,w.z-b.max.z);
                            float distance=Mathf.Sqrt(dx*dx+dz*dz),weight=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/shoulder));
                            w.y=Mathf.Lerp(w.y,Mathf.Min(w.y,b.min.y-.08f),weight);
                        }
                        if(w.y<before-.001f)adjusted++;return r.transform.InverseTransformPoint(w);
                    }
                    int n=output.Count;output.Add(Grade(a));output.Add(Grade(b));output.Add(Grade(c));outUv.AddRange(new[]{ua,ub,uc});outUv2.AddRange(new[]{la,lb,lc});indices[sub].AddRange(new[]{n,n+1,n+2});
                }
                for(int sub=0;sub<source.subMeshCount;sub++)
                {
                    indices[sub]=new List<int>();var triangles=source.GetTriangles(sub);
                    for(int i=0;i<triangles.Length;i+=3)
                    {int a=triangles[i],b=triangles[i+1],c=triangles[i+2];Triangle(positions[a],positions[b],positions[c],uv.Length==positions.Length?uv[a]:Vector2.zero,uv.Length==positions.Length?uv[b]:Vector2.zero,uv.Length==positions.Length?uv[c]:Vector2.zero,uv2.Length==positions.Length?uv2[a]:Vector2.zero,uv2.Length==positions.Length?uv2[b]:Vector2.zero,uv2.Length==positions.Length?uv2[c]:Vector2.zero,sub,0);}
                }
                if(adjusted==0)continue;
                var mesh=new Mesh{name=source.name,indexFormat=IndexFormat.UInt32,subMeshCount=source.subMeshCount};mesh.SetVertices(output);mesh.SetUVs(0,outUv);mesh.SetUVs(1,outUv2);
                for(int sub=0;sub<indices.Length;sub++)mesh.SetTriangles(indices[sub],sub);mesh.RecalculateNormals();mesh.RecalculateBounds();
                string id=TextHash(GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).Substring(0,20);mf.sharedMesh=SaveVisualMesh(FixArt+"/"+assetFolder+"/"+map+"_"+id+".asset",mesh);mf.sharedMesh.name=source.name;EditorUtility.SetDirty(mf.sharedMesh);AssetDatabase.SaveAssetIfDirty(mf.sharedMesh);EditorUtility.SetDirty(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(mf);changed++;
                Debug.Log("[CampaignRunwayGround] map="+map+" dune="+Hierarchy(r.transform)+" loweredVertices="+adjusted+" vertices="+output.Count);
            }
            return changed;
        }
    }
}
