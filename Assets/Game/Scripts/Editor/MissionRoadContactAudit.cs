using System;
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Compares mission road contact with the actual rendered mesh, without physics colliders.</summary>
    public static class MissionRoadContactAudit
    {
        public static void RunPresentationReview() { Run(); M04AirliftEditorProbe.RunLaunch(); }
        public static void Run()
        {
            var config = AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(OperationMapRenderDatabaseBuilder.ConfigPath);
            var rows = new List<string>();
            rows.Add("placements="+config.Placements.Count+" parts="+config.Parts.Count);
            foreach (var point in new[] {new Vector3(979.5f,0,429.5f),new Vector3(812.5f,0,427.5f),new Vector3(1046.5f,0,428.5f)})
            foreach (var placement in config.Placements)
            {
                var prototype=config.Prototypes[placement.PrototypeIndex];
                for(int i=prototype.FirstPart;i<prototype.FirstPart+prototype.PartCount;i++)
                {
                    var part=config.Parts[i];
                    if((part.LodFlags & OperationMapRenderLodFlags.Lod0)==0)continue;
                    var mesh=config.Meshes[part.MeshIndex].Mesh;
                    if(mesh.name.IndexOf("Road",StringComparison.OrdinalIgnoreCase)<0)continue;
                    var matrix=placement.WorldMatrix*part.LocalToPlacement;
                    var vertices=mesh.vertices;var triangles=mesh.triangles;
                    float min=float.PositiveInfinity,max=float.NegativeInfinity,hit=float.NegativeInfinity;
                    for(int v=0;v<vertices.Length;v++){vertices[v]=matrix.MultiplyPoint3x4(vertices[v]);min=Mathf.Min(min,vertices[v].y);max=Mathf.Max(max,vertices[v].y);}
                    for(int t=0;t<triangles.Length;t+=3)
                        if(TryHeight(point,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]],out float y))hit=Mathf.Max(hit,y);
                    if(!float.IsNegativeInfinity(hit))rows.Add($"point={point} mesh={mesh.name} surface={hit:F6} bounds={min:F6}..{max:F6} oldLift={max-hit:F6} vertices={vertices.Length} triangles={triangles.Length/3}");
                }
            }
            var scene=EditorSceneManager.OpenScene(DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,OpenSceneMode.Single);
            foreach(var sceneRoot in scene.GetRootGameObjects())
            foreach(var filter in sceneRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;if(mesh==null||mesh.name.IndexOf("Road",StringComparison.OrdinalIgnoreCase)<0)continue;
                var renderer=filter.GetComponent<Renderer>();if(renderer==null)continue;
                var bounds=renderer.bounds;
                foreach(var point in new[]{new Vector3(979.5f,0,429.5f),new Vector3(812.5f,0,427.5f),new Vector3(1046.5f,0,428.5f)})
                {
                    if(point.x<bounds.min.x||point.x>bounds.max.x||point.z<bounds.min.z||point.z>bounds.max.z)continue;
                    var vertices=mesh.vertices;var triangles=mesh.triangles;float hit=float.NegativeInfinity;
                    for(int v=0;v<vertices.Length;v++)vertices[v]=filter.transform.TransformPoint(vertices[v]);
                    for(int t=0;t<triangles.Length;t+=3)if(TryHeight(point,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]],out float y))hit=Mathf.Max(hit,y);
                    rows.Add($"scene point={point} mesh={mesh.name} hit={hit:F6} boundsMax={bounds.max.y:F6} oldLift={bounds.max.y-hit:F6} triangles={triangles.Length/3}");
                }
            }
            MissionSupportUiStyle.Build();
            File.WriteAllLines("/private/tmp/warline-road-contact-baseline.txt",rows);
            foreach(var row in rows)Debug.Log("[MissionRoadContactAudit] "+row);
            Debug.Log("[MissionRoadContactAudit] result=Passed samples="+rows.Count);
        }

        public static bool TryHeight(Vector3 point,Vector3 a,Vector3 b,Vector3 c,out float height)
        {
            height=0;
            var normal=Vector3.Cross(b-a,c-a);
            if(Mathf.Abs(normal.y)<=.000001f)return false;
            float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.z-c.z))/normal.y * -1;
            float v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.z-c.z))/normal.y * -1;
            if(u<-.00001f||v<-.00001f||u+v>1.00001f)return false;
            height=u*a.y+v*b.y+(1-u-v)*c.y;return true;
        }
    }
}
