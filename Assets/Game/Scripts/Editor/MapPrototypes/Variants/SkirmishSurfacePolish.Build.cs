using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.MapVariants
{
    public static partial class SkirmishSurfacePolish
    {
        private const string MaskPath=Art+"/DistrictSurfaceMask.asset";
        [Serializable] private sealed class BuildReport
        {
            public string result, sourceSceneSha256Before, sourceSceneSha256After;
            public int roadTiles, previousDirtPieces, concreteZones, limestoneZones, tileZones, groundMaterials;
            public string scope="Shared dense-city presentation assets; no scene, navigation, building, objective or budget changes.";
        }
        public static void BuildAndCapture()
        {
            Check(); Directory.CreateDirectory(Report);
            var setup=EditorSceneManager.GetSceneManagerSetup();var report=new BuildReport();
            report.sourceSceneSha256Before=Hash(Source);
            try
            {
                var scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
                if(!File.Exists(Report+"/baseline-town-junction.png")) Capture(scene,"baseline");
                var renderers=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                BuildDistrictMask(renderers,report);
                RebuildRoadSurfaces(renderers,report);
                // Legacy sculpted dirt tracks share a soil-only material. Match the desert palette,
                // but keep the map-specific district mask off this cross-map material.
                var legacySoil=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Rendering/Materials/PolygonMilitary_GroundVariation.mat");
                BackupAsset(legacySoil);
                legacySoil.SetFloat("_SandPaletteStrength",1);
                legacySoil.SetColor("_SandDryColor",new Color(.82f,.73f,.56f));
                legacySoil.SetColor("_SandWornColor",new Color(.62f,.56f,.45f));
                legacySoil.SetColor("_SandScrubColor",new Color(.62f,.63f,.48f));
                legacySoil.SetFloat("_SandScrubCoverage",.19f);
                var landscape=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Warm_PolygonMilitary_GroundVariation.mat");
                foreach(var property in new[]{"_MacroTintA","_MacroTintB"})legacySoil.SetColor(property,landscape.GetColor(property));
                foreach(var property in new[]{"_MacroScale","_MacroStrength","_MacroContrastLow","_MacroContrastHigh"})legacySoil.SetFloat(property,landscape.GetFloat(property));
                legacySoil.SetFloat("_GroundDetailTiling",.085f);legacySoil.SetFloat("_GroundDetailStrength",.3f);
                legacySoil.SetFloat("_DetailAntiRepeat",1);
                EditorUtility.SetDirty(legacySoil);AssetDatabase.SaveAssetIfDirty(legacySoil);
                foreach(var material in renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct())
                {
                    if(material.shader==null || material.shader.name!="Game/Environment/GroundMacroVariation")continue;
                    if(material.name!="OutskirtsSand" && !material.name.StartsWith("Warm_"))continue;
                    BackupAsset(material);
                    material.SetTexture("_DistrictSurfaceMap",AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath));
                    material.SetVector("_DistrictSurfaceRect",new Vector4(0,0,2048,1024));
                    material.SetFloat("_DistrictSurfaceStrength",1);
                    material.SetFloat("_DetailAntiRepeat",1);
                    material.SetFloat("_GroundDetailStrength",.3f);
                    material.SetFloat("_GroundDetailNormalStrength",.24f);
                    material.SetFloat("_GroundDetailTiling",.085f);
                    material.SetColor("_SandDryColor",new Color(.82f,.73f,.56f));
                    material.SetColor("_SandWornColor",new Color(.62f,.56f,.45f));
                    material.SetColor("_SandScrubColor",new Color(.62f,.63f,.48f));
                    material.SetFloat("_SandScrubCoverage",.19f);
                    EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);report.groundMaterials++;
                }
                if(report.groundMaterials<4 || report.roadTiles<100 || report.concreteZones<2 || report.limestoneZones<20 || report.tileZones<5)
                    throw new InvalidOperationException("Surface coverage incomplete: "+JsonUtility.ToJson(report));
                // The source scene and every gameplay object remain untouched on disk.
                report.sourceSceneSha256After=Hash(Source);
                if(report.sourceSceneSha256Before!=report.sourceSceneSha256After)throw new InvalidOperationException("Source scene unexpectedly changed.");
                report.result="Passed";File.WriteAllText(Report+"/surface-build.json",JsonUtility.ToJson(report,true));
                Capture(scene,"after");
                Debug.Log("[SkirmishSurfacePolish] result=Passed "+JsonUtility.ToJson(report));
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static string Hash(string path){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static void BackupAsset(UnityEngine.Object asset)
        {
            string path=AssetDatabase.GetAssetPath(asset);if(string.IsNullOrEmpty(path)||!File.Exists(path))return;
            string target=Report+"/baseline-assets/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));
            if(!File.Exists(target))File.Copy(path,target);
        }
        private static void BuildDistrictMask(MeshRenderer[] renderers,BuildReport report)
        {
            // One shared 2048 x 1024 world mask, one metre per texel; generated once, never per frame.
            const int width=2048,height=1024;var colors=new Color32[width*height];
            foreach(var r in renderers.OrderBy(r=>Hierarchy(r.transform)))
            {
                var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;
                string key=mesh.name.ToLowerInvariant(),path=Hierarchy(r.transform).ToLowerInvariant();
                if(path.Contains("destroyed") || key.Contains("destroyed") || key.Contains("door") || key.Contains("roof") || key.Contains("stairs") || key.Contains("cloth"))continue;
                bool military=key.Contains("hangar") || key.Contains("barracks") || key.Contains("warehouse") || key.Contains("factory");
                bool residence=key.Contains("house") || key.Contains("shop") || key.Contains("apartment") || key.Contains("office");
                var b=r.bounds;if((!military&&!residence)||b.size.x<4||b.size.z<4||b.size.y<3||b.size.x>100||b.size.z>100)continue;
                int kind=military?0:((Mathf.FloorToInt(b.center.x/25)+Mathf.FloorToInt(b.center.z/21))%4==0?2:1);
                float padding=military?9:4.5f;
                PaintZone(colors,width,height,b.center,new Vector2(b.extents.x+padding,b.extents.z+padding),kind,military?1.25f:1f);
                if(kind==0)report.concreteZones++;else if(kind==1)report.limestoneZones++;else report.tileZones++;
            }
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
            if(texture==null){texture=new Texture2D(width,height,TextureFormat.RGBA32,true,true){name="DistrictSurfaceMask"};AssetDatabase.CreateAsset(texture,MaskPath);}
            else BackupAsset(texture);
            texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;texture.anisoLevel=2;
            texture.SetPixels32(colors);texture.Apply(true,false);EditorUtility.SetDirty(texture);AssetDatabase.SaveAssetIfDirty(texture);
        }
        private static void PaintZone(Color32[] pixels,int width,int height,Vector3 center,Vector2 extents,int kind,float feather)
        {
            int x0=Mathf.Clamp(Mathf.FloorToInt(center.x-extents.x),0,width-1),x1=Mathf.Clamp(Mathf.CeilToInt(center.x+extents.x),0,width-1);
            int z0=Mathf.Clamp(Mathf.FloorToInt(center.z-extents.y),0,height-1),z1=Mathf.Clamp(Mathf.CeilToInt(center.z+extents.y),0,height-1);
            for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
            {
                float edge=Mathf.Min(extents.x-Mathf.Abs(x+.5f-center.x),extents.y-Mathf.Abs(z+.5f-center.z));
                float wear=Mathf.PerlinNoise(x*.19f+13,z*.19f+39)*.65f;
                byte value=(byte)Mathf.RoundToInt(Mathf.SmoothStep(0,1,Mathf.Clamp01((edge-wear)/feather))*255);
                int i=z*width+x;var c=pixels[i];byte old=Math.Max(c.r,Math.Max(c.g,c.b));
                if(value<=old)continue;
                pixels[i]=kind==0?new Color32(value,0,0,255):kind==1?new Color32(0,value,0,255):new Color32(0,0,value,255);
            }
        }
        private sealed class SurfaceMesh
        {
            private readonly List<Vector3> vertices=new();private readonly List<int> indices=new();
            private readonly Transform parent;
            public SurfaceMesh(Transform parent){this.parent=parent;}
            public void Quad(Transform tile,float x0,float z0,float x1,float z1,float y)
            {
                int index=vertices.Count;
                foreach(var p in new[]{new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z0)})
                    vertices.Add(parent.InverseTransformPoint(tile.TransformPoint(p)));
                indices.AddRange(new[]{index,index+1,index+2,index,index+2,index+3});
            }
            public void AddTopFaces(MeshFilter filter,float worldHeight)
            {
                var mesh=filter.sharedMesh;if(mesh==null)return;
                var source=mesh.vertices;var normals=mesh.normals;var tris=mesh.triangles;
                var points=new List<Vector3>();var remap=new int[source.Length];
                var welded=new Dictionary<(int,int),int>();
                for(int i=0;i<source.Length;i++)
                {
                    var p=filter.transform.TransformPoint(source[i]);p.y=worldHeight;
                    var key=(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.z*10000));
                    if(!welded.TryGetValue(key,out int index)){index=points.Count;points.Add(p);welded.Add(key,index);}
                    remap[i]=index;
                }
                var edges=new Dictionary<(int,int),(int from,int to,int count)>();
                void Edge(int from,int to)
                {
                    var key=from<to?(from,to):(to,from);
                    if(edges.TryGetValue(key,out var edge))edges[key]=(edge.from,edge.to,edge.count+1);
                    else edges[key]=(from,to,1);
                }
                for(int i=0;i<tris.Length;i+=3)
                {
                    if(normals.Length!=source.Length || filter.transform.TransformDirection(normals[tris[i]]).y<.45f)continue;
                    int a=remap[tris[i]],b=remap[tris[i+1]],c=remap[tris[i+2]];
                    float turn=Vector3.Cross(points[b]-points[a],points[c]-points[a]).y;
                    if(Mathf.Abs(turn)<.000001f)continue;
                    if(turn<0)(b,c)=(c,b);
                    Edge(a,b);Edge(b,c);Edge(c,a);
                }
                var boundary=edges.Values.Where(e=>e.count==1).ToList();
                while(boundary.Count>0)
                {
                    var first=boundary[0];boundary.RemoveAt(0);var contour=new List<Vector3>{points[first.from]};int end=first.to;
                    while(end!=first.from)
                    {
                        contour.Add(points[end]);int next=boundary.FindIndex(e=>e.from==end);
                        if(next<0)throw new InvalidOperationException("Open kerb outline: "+filter.name);
                        end=boundary[next].to;boundary.RemoveAt(next);
                    }
                    foreach(var polygon in SplitTouchingLoops(contour))
                    {
                    // Remove collinear tessellation points, retaining the authored rounded silhouette.
                    bool changed=true;
                    while(changed && polygon.Count>3)
                    {
                        changed=false;
                        for(int i=0;i<polygon.Count;i++)
                        {
                            var a=polygon[(i+polygon.Count-1)%polygon.Count];var b=polygon[i];var c=polygon[(i+1)%polygon.Count];
                            if(Vector3.Dot(b-a,c-a)>=0 && Vector3.Dot(b-c,a-c)>=0 && Mathf.Abs(Vector3.Cross(b-a,c-a).y)/Mathf.Max(.001f,(c-a).magnitude)<.08f)
                            {polygon.RemoveAt(i);changed=true;break;}
                        }
                    }
                    float area=0;var origin=polygon[0];for(int i=0;i<polygon.Count;i++){var a=polygon[i]-origin;var b=polygon[(i+1)%polygon.Count]-origin;area+=a.z*b.x-a.x*b.z;}
                    // The authored sidewalk pieces are open strips/corners, never closed rings.
                    // Interior groove loops are decorative tile detail, replaced by the surface shader.
                    if(area<=.0001f)continue;
                    TriangulateOutline(polygon);
                    }
                }
            }
            private static IEnumerable<List<Vector3>> SplitTouchingLoops(List<Vector3> contour)
            {
                // Decorative grooves can meet the outer rim at one vertex. Split those rings
                // before triangulation instead of interpreting a touching contour as one polygon.
                var pending=new Stack<List<Vector3>>();pending.Push(contour);
                while(pending.Count>0)
                {
                    var loop=pending.Pop();bool split=false;
                    for(int i=0;i<loop.Count&&!split;i++)for(int j=i+1;j<loop.Count;j++)
                    {
                        if((loop[i]-loop[j]).sqrMagnitude>1e-10f)continue;
                        var ring=loop.GetRange(i,j-i);var rest=loop.Take(i).Concat(loop.Skip(j)).ToList();
                        if(ring.Count>=3)pending.Push(ring);if(rest.Count>=3)pending.Push(rest);
                        split=true;break;
                    }
                    if(!split && loop.Count>=3)yield return loop;
                }
            }
            private void TriangulateOutline(List<Vector3> polygon)
            {
                int start=vertices.Count;vertices.AddRange(polygon.Select(parent.InverseTransformPoint));
                var remaining=Enumerable.Range(0,polygon.Count).ToList();
                while(remaining.Count>3)
                {
                    bool clipped=false;
                    for(int i=0;i<remaining.Count;i++)
                    {
                        int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                        var pa=polygon[a];var pb=polygon[b];var pc=polygon[c];
                        if(Vector3.Cross(pb-pa,pc-pb).y<=.000001f)continue;
                        bool contains=false;
                        foreach(int other in remaining)
                        {
                            if(other==a||other==b||other==c)continue;var p=polygon[other];
                            if(Vector3.Cross(pb-pa,p-pa).y>=-.000001f && Vector3.Cross(pc-pb,p-pb).y>=-.000001f && Vector3.Cross(pa-pc,p-pc).y>=-.000001f){contains=true;break;}
                        }
                        if(contains)continue;
                        indices.AddRange(new[]{start+a,start+b,start+c});remaining.RemoveAt(i);clipped=true;break;
                    }
                    if(!clipped)
                    {
                        File.WriteAllLines(Report+"/failed-kerb-outline.txt",polygon.Select(p=>$"{p.x:R},{p.z:R}"));
                        throw new InvalidOperationException("Kerb outline could not be triangulated; exported contour for diagnosis.");
                    }
                }
                indices.AddRange(remaining.Select(i=>start+i));
            }
            public void Save(string name)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"/DenseCity_"+name+".asset");
                if(mesh==null)throw new InvalidOperationException("Missing shared presentation mesh "+name);
                if(vertices.Count==0)throw new InvalidOperationException("Refusing empty "+name+" mesh");
                BackupAsset(mesh);mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
            }
        }
        private static void RebuildRoadSurfaces(MeshRenderer[] renderers,BuildReport report)
        {
            var existing=renderers.Single(r=>r.name=="DenseCity_FlatPaving");
            var dirt=renderers.Where(r=>r.name=="Dirt" && Hierarchy(r.transform).Contains("/Infrastructure/")).ToArray();
            var all=renderers.Where(r=>(r.name=="Dirt"||r.name=="Asphalt")&&Hierarchy(r.transform).Contains("/Infrastructure/"))
                .Select(r=>r.transform.parent).Distinct().OrderBy(t=>t.position.x).ThenBy(t=>t.position.z).ToArray();
            var paving=new SurfaceMesh(existing.transform);var kerbs=new SurfaceMesh(existing.transform);
            var lanes=new SurfaceMesh(existing.transform);var cracks=new SurfaceMesh(existing.transform);var sand=new SurfaceMesh(existing.transform);
            foreach(var tile in all)
            {
                // Tile-local height clears the authored sand mesh and matches the existing visual layer.
                paving.Quad(tile,0,0,10,10,.44f);
                foreach(var sidewalk in tile.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.name=="Sidewalk"))
                    kerbs.AddTopFaces(sidewalk,tile.TransformPoint(new Vector3(5,.46f,5)).y);
                if(tile.name.Contains("End"))sand.Quad(tile,0,0,10,.7f,.449f);
            }
            int n=0;
            foreach(var tile in all)
            {
                if(!tile.name.Contains("Straight"))continue;
                lanes.Quad(tile,1.5f,4.94f,3.35f,5.06f,.452f);lanes.Quad(tile,6.65f,4.94f,8.5f,5.06f,.452f);
                if(n++%7==0)
                {
                    cracks.Quad(tile,6.7f,2.2f,6.735f,3.3f ,.454f);
                    cracks.Quad(tile,6.3f,3.3f,6.735f,3.335f ,.454f);
                }
            }
            paving.Save("FlatPaving");kerbs.Save("KerbPaving");lanes.Save("Lanes");cracks.Save("Cracks");sand.Save("SandSpill");
            foreach(string name in new[]{"CityStreetAsphalt","WornAsphalt"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat");BackupAsset(material);
                // Asphalt uses the same world-projected wear across neighbouring tiles.
                material.SetFloat("_BaseColorInfluence",1);material.SetTexture("_BaseMap",null);
                material.SetColor("_BaseColor",new Color(.29f,.315f,.34f));
                var wear=AssetDatabase.LoadAssetAtPath<Material>(Art+"/CityStreetAsphalt.mat").GetTexture("_DesertDetailMap");
                material.SetTexture("_DesertDetailMap",wear);material.SetTexture("_GreenDetailMap",wear);
                material.SetFloat("_GroundDetailStrength",.38f);material.SetFloat("_GroundDetailTiling",.085f);
                material.SetFloat("_GroundDetailNormalStrength",.1f);material.SetFloat("_DetailAntiRepeat",.75f);
                material.SetFloat("_MacroStrength",.15f);material.SetFloat("_SandPaletteStrength",0);
                EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
            }
            var sandMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/RoadSand.mat");BackupAsset(sandMaterial);
            sandMaterial.SetColor("_BaseColor",new Color(.70f,.62f,.46f));EditorUtility.SetDirty(sandMaterial);AssetDatabase.SaveAssetIfDirty(sandMaterial);
            var kerbMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/KerbPaving.mat");BackupAsset(kerbMaterial);
            kerbMaterial.SetVector("_DistrictSurfaceConstant",new Vector4(0,1,0,0));kerbMaterial.SetFloat("_DistrictSurfaceStrength",1);
            EditorUtility.SetDirty(kerbMaterial);AssetDatabase.SaveAssetIfDirty(kerbMaterial);
            report.roadTiles=all.Length;report.previousDirtPieces=dirt.Length;
        }
    }
}
