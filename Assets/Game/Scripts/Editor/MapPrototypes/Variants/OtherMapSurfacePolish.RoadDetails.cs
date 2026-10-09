using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Configs;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class RoadDetailReceipt
        {
            public string map,result,beforeHash,afterHash,protectedSceneHash;
            public int roadPieces,runwayPieces,originalTriangles,newTriangles;
        }
        private static string NonVisualReferences(string text)
        {
            text=Regex.Replace(text,@"(?m)^  m_Materials:\n(?:  - .*\n)*", "  m_Materials: [visual references excluded]\n");
            text=Regex.Replace(text,@"(?m)^    - target: .*\n      propertyPath: '?m_Materials\.Array\.(?:data\[\d+\]|size)'?\n      value: .*\n      objectReference: .*\n", "");
            text=Regex.Replace(text,@"(?m)^    - target: .*\n      propertyPath: m_Mesh\n      value: .*\n      objectReference: .*\n", "");
            // Only MeshFilter references may change. Collider meshes are kept in the hash.
            return Regex.Replace(text,@"(?ms)(^--- !u!33 &.*?\n)(.*?)(?=^--- |\z)",m=>m.Groups[1].Value+Regex.Replace(m.Groups[2].Value,@"(?m)^  m_Mesh: .*\n", "  m_Mesh: [visual mesh excluded]\n"));
        }
        private static Mesh SaveVisualMesh(string path,Mesh mesh)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);return existing;
        }
        private static Mesh LowReliefTrack(Mesh source)
        {
            string path=VisualArt+"/Roads/"+source.name+"_LowRelief.asset";
            var result=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(result!=null)return result;
            result=UnityEngine.Object.Instantiate(source);result.name=source.name+"_LowRelief";
            var vertices=result.vertices;float bottom=source.bounds.min.y,range=source.bounds.size.y;
            if(range>.8f)throw new InvalidOperationException("Refusing to flatten an authored slope: "+source.name);
            for(int i=0;i<vertices.Length;i++)vertices[i].y=bottom+(vertices[i].y-bottom)*.20f;
            result.vertices=vertices;result.RecalculateNormals();result.RecalculateBounds();return SaveVisualMesh(path,result);
        }
        private static Mesh RunwaySurface(Mesh source)
        {
            string path=VisualArt+"/Roads/RunwaySurface.asset";var found=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(found!=null)return found;
            var vertices=new List<Vector3>();var asphalt=new List<int>();var paint=new List<int>();var b=source.bounds;
            float y=b.max.y;
            void Quad(float x0,float z0,float x1,float z1,float h,List<int> triangles)
            {
                int n=vertices.Count;vertices.AddRange(new[]{new Vector3(x0,h,z0),new Vector3(x0,h,z1),new Vector3(x1,h,z1),new Vector3(x1,h,z0)});
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            // Keep the exact existing XZ footprint, with a continuous straight surface.
            Quad(b.min.x,b.min.z,b.max.x,b.max.z,y,asphalt);
            float inset=2.1f;Quad(b.min.x,b.min.z+inset,b.max.x,b.min.z+inset+.18f,y+.008f,paint);
            Quad(b.min.x,b.max.z-inset-.18f,b.max.x,b.max.z-inset,y+.008f,paint);
            for(float x=b.min.x+8;x<b.max.x-8;x+=14)Quad(x,-.20f,Mathf.Min(x+6,b.max.x-8),.20f,y+.008f,paint);
            var result=new Mesh{name="Campaign_RunwaySurface",subMeshCount=2};result.SetVertices(vertices);result.SetTriangles(asphalt,0);result.SetTriangles(paint,1);result.RecalculateNormals();result.RecalculateBounds();
            return SaveVisualMesh(path,result);
        }
        public static void BuildRoadDetails()
        {
            Check();Directory.CreateDirectory(VisualArt+"/Roads");AssetDatabase.Refresh();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var paint=MaterialCopy(VisualArt+"/Roads/RunwayPaint.mat",Templates+"/RoadSand.mat");
                paint.SetColor("_BaseColor",new Color(.73f,.72f,.66f));paint.SetFloat("_SandPaletteStrength",0);paint.SetFloat("_DistrictSurfaceStrength",0);Save(paint);
                foreach(var map in VisualMaps)
                {
                    string source=Source(map),original=File.ReadAllText(source),baseline=VisualReport+"/local-baselines/"+map+"-road-scene.unity";
                    Directory.CreateDirectory(Path.GetDirectoryName(baseline));if(!File.Exists(baseline))File.WriteAllText(baseline,original);
                    else original=File.ReadAllText(baseline);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;
                    var receipt=new RoadDetailReceipt{map=map,beforeHash=Hash(baseline),protectedSceneHash=TextHash(NonVisualReferences(original))};
                    foreach(var r in rs)
                    {
                        if(!r.enabled||!r.gameObject.activeInHierarchy||!Intact(r))continue;
                        var p=r.bounds.center;if(p.x<min.x-30||p.x>max.x+30||p.z<min.z-30||p.z>max.z+30)continue;
                        var filter=r.GetComponent<MeshFilter>();var mesh=filter?.sharedMesh;if(mesh==null)continue;
                        bool runway=mesh.name=="SM_Env_Runway_01"||mesh.name=="Campaign_RunwaySurface",track=mesh.name.StartsWith("SM_Env_DirtRoad_Straight_01")||mesh.name.StartsWith("SM_Env_DirtRoad_Corner_01");
                        if(mesh.name.EndsWith("_LowRelief"))mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/PolygonMilitary/Models/"+mesh.name.Replace("_LowRelief","")+".fbx");
                        if(runway)mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/PolygonMilitary/Models/SM_Env_Runway_01.fbx");
                        if(!runway&&!track)continue;
                        if(r.GetComponentInParent<Game.Authoring.OperationMapBuildingAuthoring>()!=null||r.GetComponentsInChildren<Collider>(true).Length>0)continue;
                        // Genuine ramps and rotated terrain-following pieces retain their authored relief.
                        if(track&&(Mathf.Abs(Vector3.Dot(filter.transform.up,Vector3.up))<.995f||r.bounds.size.y>1))continue;
                        receipt.originalTriangles+=mesh.triangles.Length/3;filter.sharedMesh=runway?RunwaySurface(mesh):LowReliefTrack(mesh);
                        receipt.newTriangles+=filter.sharedMesh.triangles.Length/3;EditorUtility.SetDirty(filter);
                        if(runway)
                        {
                            string asphaltPath=map=="DenseCity"?Templates+"/CityStreetAsphalt.mat":Art+"/"+map+"/Asphalt.mat";
                            r.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(asphaltPath),paint};EditorUtility.SetDirty(r);receipt.runwayPieces++;
                        }
                        else receipt.roadPieces++;
                    }
                    if(receipt.roadPieces+receipt.runwayPieces>0){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed: "+map);}
                    if(NonVisualReferences(original)!=NonVisualReferences(File.ReadAllText(source)))throw new InvalidOperationException("Protected scene data changed: "+map);
                    receipt.afterHash=Hash(source);receipt.result="Passed";File.WriteAllText(VisualReport+"/"+map+"-road-details.json",JsonUtility.ToJson(receipt,true));
                    Capture(map,scene,def,rs,"road-details",VisualReport,DetailViews(rs,def));Debug.Log("[MapRoadDetails] "+JsonUtility.ToJson(receipt));
                }
                Debug.Log("[MapRoadDetails] result=Passed maps=11 transformsCollidersAndGameplay=unchanged");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
