using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        private const string Art="Assets/Game/Art/MapBeautify/CampaignSurfaces";
        private const string Templates="Assets/Game/Art/MapBeautify/DenseCity";
        [Serializable] private sealed class SurfaceBuild
        {
            public string map,result,beforeSha256,afterSha256,nonMaterialSha256;
            public int groundRenderers,padRenderers,roadRenderers,stoneZones,clayZones,concreteZones;
            public int maskWidth,maskHeight,padZones;
        }
        // Compare every saved scene property except renderer material references. This also
        // protects transforms, mesh/collider references, ownership and authoring data.
        private static string NonMaterials(string text)
        {
            text=Regex.Replace(text,@"(?m)^  m_Materials:\n(?:  - .*\n)*", "  m_Materials: [surface references excluded]\n");
            // Prefab instances persist a slot assignment as an override instead of a renderer block.
            return Regex.Replace(text,@"(?m)^    - target: .*\n      propertyPath: 'm_Materials.Array.data\[0\]'\n      value: .*\n      objectReference: .*\n", "");
        }
        public static void ValidateSavedSurfaces()
        {
            Check();int materialCount=0;
            foreach(string map in Maps)
            {
                var receipt=JsonUtility.FromJson<SurfaceBuild>(File.ReadAllText(Report+"/"+map+"-build.json"));
                if(TextHash(NonMaterials(File.ReadAllText(Source(map))))!=receipt.nonMaterialSha256)throw new InvalidOperationException("Non-material scene mismatch: "+map);
                if(receipt.result!="Passed"||receipt.afterSha256!=Hash(Source(map)))throw new InvalidOperationException("Stale surface build receipt: "+map);
                foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Art+"/"+map}))
                {
                    var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if(m.shader==null||m.shader.name!="Game/Environment/GroundMacroVariation"||ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Invalid surface shader: "+m.name);
                    materialCount++;
                }
                var ground=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+map+"/Ground.mat");var mask=ground.GetTexture("_DistrictSurfaceMap") as Texture2D;
                if(mask==null||mask.width!=receipt.maskWidth||mask.height!=receipt.maskHeight||ground.GetFloat("_DistrictSurfaceStrength")!=1)throw new InvalidOperationException("Missing district mask: "+map);
                var pixels=mask.GetPixels32();for(int x=0;x<mask.width;x++)if(pixels[x].r+pixels[x].g+pixels[x].b!=0||pixels[(mask.height-1)*mask.width+x].r+pixels[(mask.height-1)*mask.width+x].g+pixels[(mask.height-1)*mask.width+x].b!=0)throw new InvalidOperationException("Mask edge bleed: "+map);
                Debug.Log("[OtherMapSurfaceValidation] map="+map+" result=Passed geometryAndGameplay=unchanged");
            }
            if(materialCount!=Maps.Length*5)throw new InvalidOperationException("Missing surface materials.");
            Debug.Log("[OtherMapSurfaceValidation] result=Passed maps="+Maps.Length+" materials="+materialCount);
        }
        private static string TextHash(string value){using var h=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();}
        public static void BuildAndCapture()
        {
            Check();Directory.CreateDirectory(Report);Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in Maps)
                {
                    string baseline=Report+"/baseline-scenes/"+map+".unity";Directory.CreateDirectory(Path.GetDirectoryName(baseline));
                    if(!File.Exists(baseline))File.Copy(Source(map),baseline);
                    string original=File.ReadAllText(baseline);
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var report=new SurfaceBuild{map=map,beforeSha256=Hash(baseline),nonMaterialSha256=TextHash(NonMaterials(original))};
                    string folder=Art+"/"+map;Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                    var mask=CreateMask(folder,def,rs,report,out Vector4 rect);
                    var ground=MaterialCopy(folder+"/Ground.mat",Templates+"/Warm_PolygonMilitary_GroundVariation.mat");
                    ground.SetTexture("_DistrictSurfaceMap",mask);ground.SetVector("_DistrictSurfaceRect",rect);ground.SetVector("_DistrictSurfaceConstant",Vector4.zero);
                    ground.SetFloat("_DistrictSurfaceStrength",1);ground.SetFloat("_SandPaletteStrength",1);ground.SetFloat("_DetailAntiRepeat",1);
                    ground.SetFloat("_GroundDetailStrength",.30f);ground.SetFloat("_GroundDetailNormalStrength",.20f);ground.SetFloat("_GroundDetailTiling",.085f);
                    ground.SetFloat("_SandScrubCoverage",.19f);ground.SetColor("_SandDryColor",new Color(.82f,.73f,.56f));ground.SetColor("_SandWornColor",new Color(.62f,.56f,.45f));ground.SetColor("_SandScrubColor",new Color(.62f,.63f,.48f));
                    Save(ground);
                    var soil=MaterialCopy(folder+"/TrackSoil.mat",folder+"/Ground.mat");soil.SetFloat("_DistrictSurfaceStrength",0);Save(soil);
                    var concrete=MaterialCopy(folder+"/Concrete.mat",folder+"/Ground.mat");concrete.SetTexture("_DistrictSurfaceMap",null);concrete.SetVector("_DistrictSurfaceConstant",new Vector4(1,0,0,0));concrete.SetFloat("_GroundDetailNormalStrength",0);Save(concrete);
                    var asphalt=MaterialCopy(folder+"/Asphalt.mat",Templates+"/CityStreetAsphalt.mat");asphalt.SetFloat("_DistrictSurfaceStrength",0);Save(asphalt);
                    var stone=MaterialCopy(folder+"/Limestone.mat",Templates+"/KerbPaving.mat");Save(stone);
                    foreach(var r in rs)
                    {
                        if(r.sharedMaterials.Length!=1)continue;
                        string n=r.name,mesh=r.GetComponent<MeshFilter>()?.sharedMesh?.name??"";Material replacement=null;
                        if(n.StartsWith("Ground_")||mesh.StartsWith("SM_Env_Ground_")||n.Contains("_SandWear_")){replacement=ground;report.groundRenderers++;}
                        else if(mesh.StartsWith("SM_Env_DirtRoad_")){replacement=soil;report.roadRenderers++;}
                        else if(n=="Pad_M01_Concrete"||n.Contains("_Concrete_")){replacement=concrete;report.padRenderers++;}
                        else if(n=="Pad_M01_DistrictGround"){replacement=ground;report.padRenderers++;}
                        else if(n=="Pad_M01_Asphalt"||n=="Asphalt"){replacement=asphalt;report.roadRenderers++;}
                        else if(n=="Sidewalk"){replacement=stone;report.roadRenderers++;}
                        if(replacement==null)continue;r.sharedMaterial=replacement;EditorUtility.SetDirty(r);
                    }
                    if(report.groundRenderers==0||report.concreteZones+report.stoneZones+report.clayZones==0)throw new InvalidOperationException("No surface coverage: "+map);
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Scene save failed "+map);
                    string saved=File.ReadAllText(Source(map));
                    if(NonMaterials(original)!=NonMaterials(saved))throw new InvalidOperationException("Non-material scene property changed: "+map);
                    report.afterSha256=Hash(Source(map));report.result="Passed";
                    File.WriteAllText(Report+"/"+map+"-build.json",JsonUtility.ToJson(report,true));
                    Capture(map,scene,def,rs,"after");Debug.Log("[OtherMapSurfaceBuild] "+JsonUtility.ToJson(report));
                }
                Debug.Log("[OtherMapSurfaceBuild] result=Passed maps="+Maps.Length);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void RefineIndustrialEdgesAndCapture()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string map in Maps.Where(Variant))
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var report=JsonUtility.FromJson<SurfaceBuild>(File.ReadAllText(Report+"/"+map+"-build.json"));
                    report.stoneZones=report.clayZones=report.concreteZones=report.padZones=0;
                    CreateMask(Art+"/"+map,def,rs,report,out _);
                    if(report.padZones==0||Hash(Source(map))!=report.afterSha256)throw new InvalidOperationException("Industrial edge refinement changed scene or found no pads: "+map);
                    File.WriteAllText(Report+"/"+map+"-build.json",JsonUtility.ToJson(report,true));Capture(map,scene,def,rs,"after");
                    Debug.Log("[OtherMapSurfaceEdges] map="+map+" result=Passed pads="+report.padZones);
                }
                Debug.Log("[OtherMapSurfaceEdges] result=Passed maps=3");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void FinalizeAndValidate()
        {
            RefineIndustrialEdgesAndCapture();Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in Maps.Where(m=>!Variant(m)))
                {
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    if(map.StartsWith("CH05")&&map!="CH05M02TrustUnderFire")
                    {
                        // Re-capture the baseline with the same authored Match light as the result.
                        // Material references are restored in memory; the saved scene is never reverted.
                        var audit=JsonUtility.FromJson<Audit>(File.ReadAllText(Report+"/"+map+"-audit.json"));
                        var originals=audit.surfaces.GroupBy(x=>x.path).ToDictionary(g=>g.Key,g=>g.ToArray());
                        var prefabBlocks=Regex.Matches(File.ReadAllText(Report+"/baseline-scenes/"+map+".unity"),@"(?ms)^--- !u!1001 &(\d+)\n(.*?)(?=^--- |\z)").Cast<Match>().ToDictionary(m=>ulong.Parse(m.Groups[1].Value),m=>m.Groups[2].Value);
                        var changed=rs.Where(r=>AssetDatabase.GetAssetPath(r.sharedMaterial).StartsWith(Art+"/")).ToArray();
                        var current=changed.Select(r=>r.sharedMaterial).ToArray();
                        try
                        {
                            for(int i=0;i<changed.Length;i++)
                            {
                                var r=changed[i];
                                if(originals.TryGetValue(Hierarchy(r.transform),out var rows))r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(rows.Where(x=>(x.center-r.bounds.center).sqrMagnitude<.0001f).Select(x=>x.material).Distinct().Single());
                                else r.sharedMaterial=OriginalPrefabMaterial(r,prefabBlocks);
                            }
                            Capture(map,scene,def,rs,"before");
                        }
                        finally{for(int i=0;i<changed.Length;i++)changed[i].sharedMaterial=current[i];}
                    }
                    Capture(map,scene,def,rs,"after");
                }
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
            ValidateSavedSurfaces();
        }
        private static Material OriginalPrefabMaterial(MeshRenderer renderer,System.Collections.Generic.Dictionary<ulong,string> prefabBlocks)
        {
            var source=PrefabUtility.GetCorrespondingObjectFromSource(renderer);
            var handle=PrefabUtility.GetPrefabInstanceHandle(renderer);
            if(source==null||handle==null)throw new InvalidOperationException("No baseline material or prefab source: "+Hierarchy(renderer.transform));
            ulong instanceId=GlobalObjectId.GetGlobalObjectIdSlow(handle).targetObjectId;
            if(!prefabBlocks.TryGetValue(instanceId,out string block))throw new InvalidOperationException("No baseline prefab instance: "+instanceId);
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id))throw new InvalidOperationException("No prefab renderer identity.");
            string target="    - target: {fileID: "+id+", guid: "+guid+", type: 3}";
            var match=Regex.Match(block,Regex.Escape(target)+@"\n      propertyPath: '?m_Materials.Array.data\[0\]'?\n      value: [^\n]*\n      objectReference: \{fileID: [^,]+, guid: ([a-f0-9]+), type: 2\}");
            return match.Success?AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value)):source.sharedMaterial;
        }
        private static Material MaterialCopy(string path,string template)
        {
            var source=AssetDatabase.LoadAssetAtPath<Material>(template);if(source==null)throw new InvalidOperationException("Missing template "+template);
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(source);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(source);
            m.name=Path.GetFileNameWithoutExtension(path);return m;
        }
        private static void Save(Material m){EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}
        private static Texture2D CreateMask(string folder,OperationMapDefinition def,MeshRenderer[] rs,SurfaceBuild report,out Vector4 rect)
        {
            var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;
            float x0=Mathf.Floor(min.x-128),z0=Mathf.Floor(min.z-128),sx=Mathf.Ceil(max.x-min.x+256),sz=Mathf.Ceil(max.z-min.z+256);
            int width=Mathf.Min(2048,Mathf.NextPowerOfTwo(Mathf.CeilToInt(sx))),height=Mathf.Min(2048,Mathf.NextPowerOfTwo(Mathf.CeilToInt(sz)));
            rect=new Vector4(x0,z0,sx,sz);report.maskWidth=width;report.maskHeight=height;var pixels=new Color32[width*height];
            foreach(var r in rs.OrderBy(r=>Hierarchy(r.transform),StringComparer.Ordinal))
            {
                string key=(r.GetComponent<MeshFilter>()?.sharedMesh?.name??"").ToLowerInvariant(),path=Hierarchy(r.transform).ToLowerInvariant();var b=r.bounds;
                if(path.Contains("destroyed")||key.Contains("destroyed")||key.Contains("door")||key.Contains("roof")||key.Contains("stairs")||key.Contains("cloth"))continue;
                bool military=key.Contains("hangar")||key.Contains("barracks")||key.Contains("warehouse")||key.Contains("factory");
                bool residence=key.Contains("house")||key.Contains("shop")||key.Contains("apartment")||key.Contains("office");
                if((!military&&!residence)||b.size.x<4||b.size.z<4||b.size.y<3||b.size.x>100||b.size.z>100||b.max.x<x0||b.min.x>x0+sx||b.max.z<z0||b.min.z>z0+sz)continue;
                int kind=military?0:(Math.Abs(Mathf.FloorToInt(b.center.x/25)+Mathf.FloorToInt(b.center.z/21))%4==0?2:1);float padding=military?7:4.5f;
                float ex=b.extents.x+padding,ez=b.extents.z+padding;
                int xa=Mathf.Clamp(Mathf.FloorToInt((b.center.x-ex-x0)/sx*width),1,width-2),xb=Mathf.Clamp(Mathf.CeilToInt((b.center.x+ex-x0)/sx*width),1,width-2);
                int za=Mathf.Clamp(Mathf.FloorToInt((b.center.z-ez-z0)/sz*height),1,height-2),zb=Mathf.Clamp(Mathf.CeilToInt((b.center.z+ez-z0)/sz*height),1,height-2);
                for(int z=za;z<=zb;z++)for(int x=xa;x<=xb;x++)
                {
                    float wx=x0+(x+.5f)/width*sx,wz=z0+(z+.5f)/height*sz;
                    float edge=Mathf.Min(ex-Mathf.Abs(wx-b.center.x),ez-Mathf.Abs(wz-b.center.z));
                    float wear=Mathf.PerlinNoise(wx*.19f+13,wz*.19f+39)*.65f;
                    byte value=(byte)Mathf.RoundToInt(Mathf.SmoothStep(0,1,Mathf.Clamp01((edge-wear)/1.25f))*255);int i=z*width+x;var c=pixels[i];
                    if(value<=Math.Max(c.r,Math.Max(c.g,c.b)))continue;
                    pixels[i]=kind==0?new Color32(value,0,0,255):kind==1?new Color32(0,value,0,255):new Color32(0,0,value,255);
                }
                if(kind==0)report.concreteZones++;else if(kind==1)report.stoneZones++;else report.clayZones++;
            }
            // Existing sand-wear meshes repeat a scalloped silhouette over the paved pads.
            // Continue the slab material across those overlaps using the exact authored pad bounds.
            // Outside the pads they retain the same soil material as the surrounding ground.
            if(rs.Any(r=>r.name.Contains("_SandWear_")))
            foreach(var pad in rs.Where(r=>r.name=="Pad_M01_Concrete"||r.name=="Pad_M01_Asphalt"))
            {
                var b=pad.bounds;int xa=Mathf.Clamp(Mathf.FloorToInt((b.min.x-x0)/sx*width),1,width-2),xb=Mathf.Clamp(Mathf.CeilToInt((b.max.x-x0)/sx*width),1,width-2);
                int za=Mathf.Clamp(Mathf.FloorToInt((b.min.z-z0)/sz*height),1,height-2),zb=Mathf.Clamp(Mathf.CeilToInt((b.max.z-z0)/sz*height),1,height-2);
                for(int z=za;z<=zb;z++)for(int x=xa;x<=xb;x++)
                {
                    float wx=x0+(x+.5f)/width*sx,wz=z0+(z+.5f)/height*sz;
                    float edge=Mathf.Min(Mathf.Min(wx-b.min.x,b.max.x-wx),Mathf.Min(wz-b.min.z,b.max.z-wz));
                    byte value=(byte)Mathf.RoundToInt(Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.7f))*255);
                    if(value==0)continue;pixels[z*width+x]=new Color32(value,0,0,255);
                }
                report.padZones++;
            }
            string asset=folder+"/DistrictMask.asset";var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            if(mask==null){mask=new Texture2D(width,height,TextureFormat.RGBA32,true,true){name="DistrictMask"};AssetDatabase.CreateAsset(mask,asset);}
            mask.wrapMode=TextureWrapMode.Clamp;mask.filterMode=FilterMode.Bilinear;mask.anisoLevel=2;mask.SetPixels32(pixels);mask.Apply(true,false);EditorUtility.SetDirty(mask);AssetDatabase.SaveAssetIfDirty(mask);return mask;
        }
    }
}
