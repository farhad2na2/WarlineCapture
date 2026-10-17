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
        const string WaterRoadArt="Assets/Game/Art/MapBeautify/CampaignVisualFixes20261016";
        const string WaterRoadReport="Design/AgentReports/WaterBridgesRoads20261016";
        const string GroundShaderName="Game/Environment/GroundMacroVariation";
        const string WaterShaderName="Game/Environment/CampaignWater";

        struct RoadJoin{public Vector2 seam,dir,lateral;public float halfWidth;public string dirt,paved;public MeshRenderer dirtRenderer,pavedRenderer;}
        [Serializable] sealed class WaterRoadMapReport
        {
            public string map,sourceHash,result;
            public int waterRenderers,bankMeshes,channelMeshes,waterMaterials,joins,pavedMaterials,earthMaterials,shoreSegments;
            public string[] joinRows,skipped;
        }
        sealed class WaterRoadMap
        {
            public string map;public OperationMapDefinition def;public WaterRoadMapReport report;
            public readonly List<RoadJoin> joins=new List<RoadJoin>();public readonly List<Rect> water=new List<Rect>();
            public readonly HashSet<Mesh> bankMeshes=new HashSet<Mesh>();public readonly HashSet<Material> waterMaterials=new HashSet<Material>();
            public readonly Dictionary<Material,HashSet<string>> roles=new Dictionary<Material,HashSet<string>>();
            public readonly List<Tuple<string,Vector3,float>> views=new List<Tuple<string,Vector3,float>>();
        }

        static bool PavedSurface(MeshRenderer r)=>r.name=="Asphalt"||r.name=="Pad_M01_Asphalt"||MeshName(r).Contains("Sidewalk");
        static bool DirtSurface(MeshRenderer r)=>MeshName(r).Contains("DirtRoad");
        static float Smooth(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);}

        // Only task-keyed copies may be edited in place; shared templates stay untouched.
        static bool MapOwnedMaterial(string path,string map)
        {
            bool keyed=Path.GetFileName(path).StartsWith(map+"_",StringComparison.Ordinal)||path.Contains("/"+map+"/");
            return keyed&&(path.StartsWith(WaterRoadArt+"/")||path.StartsWith(VisualArt+"/")||path.StartsWith(FixArt+"/")||path.StartsWith(Art+"/"));
        }

        public static void CensusWaterAndRoadJoins()=>RunWaterRoadBlend(false);
        public static void ApplyWaterAndRoadBlend()=>RunWaterRoadBlend(true);

        [Serializable] sealed class WaterRoadParityStamp { public string contentHash; }
        public static void ValidateWaterRoadIntegration()
        {
            string path=Game.Editor.OperationMapRenderDatabaseBuilder.ReportPath;
            string before=JsonUtility.FromJson<WaterRoadParityStamp>(File.ReadAllText(path)).contentHash;
            ApplyWaterAndRoadBlend();
            Game.Editor.CampaignRenderReconciliation20261011.Inspect();
            Game.Editor.OperationMapRenderDatabaseBuilder.RunSourceParityValidation();
            string after=JsonUtility.FromJson<WaterRoadParityStamp>(File.ReadAllText(path)).contentHash;
            if(string.IsNullOrEmpty(after))throw new InvalidOperationException("Missing packed content hash");
            Debug.Log("[WaterRoadIntegration] result=Passed packedContentHash="+after+" previousPackedContentHash="+before+" sourceParity=Passed");
        }

        static void RunWaterRoadBlend(bool apply)
        {
            Check();
            foreach(var shaderName in new[]{GroundShaderName,WaterShaderName})
            {
                var shader=Shader.Find(shaderName);
                if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Invalid shader "+shaderName);
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();string label=apply?"after":"before";
            string captures=WaterRoadReport+"/captures";Directory.CreateDirectory(captures);
            var maps=new List<WaterRoadMap>();
            try
            {
                foreach(string map in VisualMaps)
                {
                    string source=Source(map),hash=Hash(source);
                    var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Single);var all=SceneRenderers(scene);
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));if(def==null)throw new InvalidOperationException("Missing map definition "+map);
                    var data=new WaterRoadMap{map=map,def=def,report=new WaterRoadMapReport{map=map,sourceHash=hash}};maps.Add(data);
                    var visible=all.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,80)).ToArray();
                    CollectRoadJoins(data,visible);CollectWater(data,visible);CollectBlendRoles(data,visible);
                    data.report.joins=data.joins.Count;
                    data.report.joinRows=data.joins.Select(j=>j.dirt+" -> "+j.paved+" seam="+j.seam+" dir="+j.dir+" halfWidth="+j.halfWidth.ToString("F2")).ToArray();
                    // Census and apply derive identical views from unchanged geometry, so captures pair exactly.
                    foreach(var j in data.joins.Take(4).Select((j,i)=>(j,i)))data.views.Add(Tuple.Create("road-join-"+j.i,new Vector3(j.j.seam.x,0,j.j.seam.y),38f));
                    foreach(var w in data.water.OrderByDescending(w=>w.width*w.height).Take(2).Select((w,i)=>(w,i)))data.views.Add(Tuple.Create("water-"+w.i,new Vector3(w.w.center.x,0,w.w.center.y),45f));
                    foreach(var site in WaterBridgeSites(visible).Take(1))
                    {
                        var center=site.bounds.center;center.y=site.deckY;
                        data.views.Add(Tuple.Create("bridge-oblique",center,Mathf.Max(38,site.bounds.size.magnitude*1.2f)));
                    }
                    if(apply)
                    {
                        ApplyWaterRoadMap(data);
                        if(map=="AshLinePort"||map=="CH05M02TrustUnderFire")ApplyTerrainShore(data,visible);
                    }
                    foreach(var view in data.views.Where(v=>v.Item1.StartsWith("water-")))
                        foreach(var w in visible.Where(NaturalWater).Where(w=>(w.bounds.center-view.Item2).sqrMagnitude<60*60))
                        {
                            var mesh=w.GetComponent<MeshFilter>()?.sharedMesh;var uv0=new List<Vector4>();var uv1=new List<Vector4>();mesh?.GetUVs(0,uv0);mesh?.GetUVs(1,uv1);
                            var m=w.sharedMaterials.FirstOrDefault(x=>x!=null&&x.shader!=null&&x.shader.name==WaterShaderName);
                            Debug.Log($"[WaterRoadBlendView] map={map} view={view.Item1} renderer={Hierarchy(w.transform)} bounds={w.bounds.center}/{w.bounds.size} mesh={AssetDatabase.GetAssetPath(mesh)} uv0={(uv0.Count>0?uv0[0].ToString():"none")} uv1={(uv1.Count>0?uv1[0].ToString():"none")} rotation={w.transform.rotation.eulerAngles} material={AssetDatabase.GetAssetPath(m)} foam={(m!=null&&m.HasProperty("_FoamStrength")?m.GetFloat("_FoamStrength"):-1)}");
                        }
                    if(data.views.Count>0)Capture(map,scene,def,all,label,captures,data.views.ToArray());
                    foreach(var name in new[]{GroundShaderName,WaterShaderName})
                        if(ShaderUtil.ShaderHasError(Shader.Find(name)))throw new InvalidOperationException("Shader error after native capture: "+name);
                    if(Hash(source)!=hash)throw new InvalidOperationException("Source scene changed during water/road blend "+map);
                    data.report.result=apply?"Applied":"Census";
                    File.WriteAllText(WaterRoadReport+"/"+map+"-"+label+".json",JsonUtility.ToJson(data.report,true));
                    Debug.Log($"[WaterRoadBlend] map={map} mode={label} joins={data.joins.Count} water={data.report.waterRenderers} bankMeshes={data.report.bankMeshes} paved={data.report.pavedMaterials} earth={data.report.earthMaterials} skipped={data.report.skipped?.Length??0}");
                }
                var crossMap=maps.SelectMany(m=>m.roles.Keys.Concat(m.waterMaterials).Distinct().Select(x=>(x,m.map))).GroupBy(p=>p.x).Where(g=>g.Count()>1).Select(g=>AssetDatabase.GetAssetPath(g.Key)+" maps="+string.Join(",",g.Select(p=>p.map))).ToArray();
                if(crossMap.Length>0)
                {
                    File.WriteAllLines(WaterRoadReport+"/cross-map-materials-"+label+".txt",crossMap);
                    if(apply)throw new InvalidOperationException("Materials shared across maps were edited: "+crossMap.Length);
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"[WaterRoadBlend] result=Passed mode={label} maps={maps.Count} joins={maps.Sum(m=>m.joins.Count)} crossMapMaterials={crossMap.Length} sourceScenes=unchanged");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }

        static void CollectRoadJoins(WaterRoadMap data,MeshRenderer[] visible)
        {
            var paved=visible.Where(r=>PavedSurface(r)&&r.bounds.size.y<.8f).ToArray();
            var seen=new HashSet<string>();
            foreach(var d in visible.Where(r=>DirtSurface(r)&&r.bounds.size.y<.8f))
            {
                var db=d.bounds;
                foreach(var axis in new[]{new Vector2(1,0),new Vector2(-1,0),new Vector2(0,1),new Vector2(0,-1)})
                {
                    bool x=axis.x!=0;float sign=x?axis.x:axis.y;
                    float seam=x?(sign>0?db.max.x:db.min.x):(sign>0?db.max.z:db.min.z);
                    float lo=x?db.min.z:db.min.x,hi=x?db.max.z:db.max.x,depth=x?db.size.x:db.size.z;
                    // A long dirt edge running beside the asphalt is a shoulder, not an entry.
                    if(hi-lo>1.3f*depth)continue;
                    foreach(var p in paved)
                    {
                        var pb=p.bounds;
                        float near=x?(sign>0?pb.min.x:-pb.max.x):(sign>0?pb.min.z:-pb.max.z),far=x?(sign>0?pb.max.x:-pb.min.x):(sign>0?pb.max.z:-pb.min.z);
                        float s=seam*sign;
                        if(near>s+.6f||far<s+.5f||near<s-2f)continue;
                        float plo=x?pb.min.z:pb.min.x,phi=x?pb.max.z:pb.max.x;
                        if(Mathf.Min(hi,phi)-Mathf.Max(lo,plo)<1f)continue;
                        string key=d.GetEntityId()+"/"+axis;if(!seen.Add(key))break;
                        float center=(lo+hi)*.5f;
                        data.joins.Add(new RoadJoin{seam=x?new Vector2(seam,center):new Vector2(center,seam),dir=axis,lateral=new Vector2(-axis.y,axis.x),halfWidth=(hi-lo)*.5f,dirt=Hierarchy(d.transform),paved=Hierarchy(p.transform),dirtRenderer=d,pavedRenderer=p});
                        break;
                    }
                }
            }
        }

        static void CollectWater(WaterRoadMap data,MeshRenderer[] visible)
        {
            var waters=visible.Where(NaturalWater).ToArray();data.report.waterRenderers=waters.Length;
            foreach(var w in waters)
            {
                data.water.Add(XzRect(w.bounds));
                var mesh=w.GetComponent<MeshFilter>()?.sharedMesh;
                string meshPath=AssetDatabase.GetAssetPath(mesh);
                if(meshPath.StartsWith(RefinementArt+"/WaterMeshes/Bank_",StringComparison.Ordinal)||meshPath.StartsWith(FixArt+"/CanalEndsV2/"+data.map+"_",StringComparison.Ordinal))data.bankMeshes.Add(mesh);
                foreach(var m in w.sharedMaterials.Where(m=>m!=null&&m.shader!=null&&m.shader.name==WaterShaderName))data.waterMaterials.Add(m);
            }
            data.report.bankMeshes=data.bankMeshes.Count;data.report.waterMaterials=data.waterMaterials.Count;
        }

        static void CollectBlendRoles(WaterRoadMap data,MeshRenderer[] visible)
        {
            foreach(var r in visible)
            {
                string role=MeshName(r).Contains("Sidewalk")?"kerb":PavedSurface(r)?"paved":RunwayRenderer(r)||r.name=="Pad_M01_Concrete"?"excluded":"earth";
                foreach(var m in r.sharedMaterials.Where(m=>m!=null&&m.shader!=null&&m.shader.name==GroundShaderName))
                {
                    if(!data.roles.TryGetValue(m,out var set))data.roles[m]=set=new HashSet<string>();
                    set.Add(role);
                }
            }
            var skipped=new List<string>();
            foreach(var pair in data.roles)
            {
                string path=AssetDatabase.GetAssetPath(pair.Key);
                if(pair.Value.Count!=1)skipped.Add("role-conflict "+path+" roles="+string.Join(",",pair.Value));
                else if(!pair.Value.Contains("excluded")&&!MapOwnedMaterial(path,data.map))skipped.Add("not-map-owned "+path);
            }
            foreach(var m in data.waterMaterials)if(!MapOwnedMaterial(AssetDatabase.GetAssetPath(m),data.map))skipped.Add("water-not-map-owned "+AssetDatabase.GetAssetPath(m));
            data.report.pavedMaterials=data.roles.Count(p=>p.Value.Count==1&&p.Value.Contains("paved"));
            data.report.earthMaterials=data.roles.Count(p=>p.Value.Count==1&&p.Value.Contains("earth"));
            data.report.skipped=skipped.ToArray();
        }

        static void ApplyWaterRoadMap(WaterRoadMap data)
        {
            foreach(var mesh in data.bankMeshes)
            {
                if(!ChannelUvs(mesh,out var uv0,out var uv1,out var uv2)){data.report.skipped=data.report.skipped.Append("bank-layout "+AssetDatabase.GetAssetPath(mesh)).ToArray();continue;}
                var vertices=mesh.vertices;var triangles=mesh.triangles;var normals=mesh.normals;var colors=mesh.colors;var bounds=mesh.bounds;
                mesh.SetUVs(0,uv0);mesh.SetUVs(1,uv1);mesh.SetUVs(2,uv2);
                if(!vertices.SequenceEqual(mesh.vertices)||!triangles.SequenceEqual(mesh.triangles)||!normals.SequenceEqual(mesh.normals)||!colors.SequenceEqual(mesh.colors)||bounds!=mesh.bounds)
                    throw new InvalidOperationException("Channel UV update changed physical mesh data "+AssetDatabase.GetAssetPath(mesh));
                EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);data.report.channelMeshes++;
            }
            bool harbour=data.map=="AshLinePort";
            foreach(var m in data.waterMaterials.Where(m=>MapOwnedMaterial(AssetDatabase.GetAssetPath(m),data.map)))
            {
                var deep=m.GetColor("_BaseColor");
                // Canals are only 5-10 m wide, so the shallow band must reach deep water within ~1.6 m.
                m.SetColor("_ShallowColor",Color.Lerp(deep,new Color(.46f,.53f,.43f,1),.4f));
                m.SetFloat("_DepthFalloff",harbour?7f:1.6f);m.SetFloat("_FlowSpeed",harbour?.08f:.3f);
                m.SetFloat("_FoamStrength",.5f);m.SetFloat("_FoamWidth",.35f);m.SetFloat("_SkyReflection",.12f);
                Save(m);
            }
            var editable=data.roles.Where(p=>p.Value.Count==1&&!p.Value.Contains("excluded")&&MapOwnedMaterial(AssetDatabase.GetAssetPath(p.Key),data.map)).ToArray();
            if(data.joins.Count==0&&data.water.Count==0)return;
            var field=new RoadBlendField(data.def);
            var editableSet=new HashSet<Material>(editable.Select(p=>p.Key));
            bool Blendable(MeshRenderer r)=>r.sharedMaterials.Where(m=>m!=null&&m.shader!=null&&m.shader.name==GroundShaderName).All(editableSet.Contains);
            // A half-edited join would draw a broken asphalt edge on the dirt with no matching asphalt beside it.
            foreach(var j in data.joins)
                if(Blendable(j.dirtRenderer)&&Blendable(j.pavedRenderer))field.Join(j);
                else data.report.skipped=data.report.skipped.Append("join-not-editable "+j.dirt+" -> "+j.paved).ToArray();
            foreach(var w in data.water)field.Damp(w);
            var mask=field.Save(data.map);
            foreach(var pair in editable)
            {
                var m=pair.Key;
                bool kerb=pair.Value.Contains("kerb");
                m.SetTexture("_RoadBlendMap",mask);m.SetVector("_RoadBlendRect",field.Rect);m.SetFloat("_RoadBlendStrength",kerb?.4f:1);
                m.SetFloat("_RoadBlendRole",kerb||pair.Value.Contains("paved")?1:0);
                m.SetColor("_RoadBlendSand",new Color(.72f,.665f,.56f,1));m.SetColor("_RoadBlendAsphalt",new Color(.23f,.25f,.28f,1));
                Save(m);
                if(m.GetTexture("_RoadBlendMap")!=mask)throw new InvalidOperationException("Road blend mask was not retained by "+AssetDatabase.GetAssetPath(m));
            }
        }

        static bool LipTop(Color c)=>c.a>.5f&&Mathf.Abs(c.r-.41f)<.02f&&Mathf.Abs(c.b-.37f)<.02f;
        // Derives channel coordinates from the bank mesh itself: the first quad is the water
        // surface, each following pair of quads is one stone lip (top, then inner face), and
        // canal-end copies append headwall quads that close one or both ends.
        static bool ChannelUvs(Mesh mesh,out List<Vector4> uv0,out List<Vector4> uv1,out List<Vector4> uv2)
        {
            uv0=null;uv1=null;uv2=null;var v=mesh.vertices;var c=mesh.colors;
            if(v.Length<4||c.Length!=v.Length||c.Take(4).Any(x=>x.a>.01f))return false;
            Vector3 o=v[0],e1=v[1]-v[0],e2=v[3]-v[0];float l1=e1.magnitude,l2=e2.magnitude;if(l1<.01f||l2<.01f)return false;
            e1/=l1;e2/=l2;if(Mathf.Abs(Vector3.Dot(e1,e2))>.01f)return false;
            bool first=l1>=l2;
            Vector3 flow=first?e1:e2,side=first?e2:e1;float length=first?l1:l2,width=first?l2:l1;
            int flags=0,n=4;
            for(;n+8<=v.Length&&LipTop(c[n]);n+=8)
            {
                float a0=Vector3.Dot(v[n]-o,side),a1=Vector3.Dot(v[n+1]-o,side),b0=Vector3.Dot(v[n]-o,flow),b1=Vector3.Dot(v[n+1]-o,flow);
                if(Mathf.Abs(a0-a1)<.01f)flags|=a0<width*.5f?1:2;
                else if(Mathf.Abs(b0-b1)<.01f)flags|=b0<length*.5f?4:8;
                else return false;
            }
            if(n<v.Length)
            {
                if((v.Length-n)%4!=0||c.Skip(n).Any(x=>x.a<.5f))return false;
                foreach(var p in v.Skip(n))flags|=Vector3.Dot(p-o,flow)<length*.5f?4:8;
                flags|=16;
            }
            uv0=new List<Vector4>(v.Length);uv1=new List<Vector4>(v.Length);uv2=new List<Vector4>(v.Length);
            foreach(var p in v)
            {
                uv0.Add(new Vector4(Vector3.Dot(p-o,side)/width,Vector3.Dot(p-o,flow)/length,width,length));
                uv1.Add(new Vector4(flow.x,flow.y,flow.z,flags));
                uv2.Add(new Vector4(side.x,side.y,side.z,0));
            }
            return true;
        }

        sealed class RoadBlendField
        {
            public readonly int Width,Height;public readonly Color[] Pixels;public readonly Vector4 Rect;
            public RoadBlendField(OperationMapDefinition def)
            {
                Rect=new Vector4(def.Bounds.PlayableMin.x-80,def.Bounds.PlayableMin.z-80,def.Bounds.PlayableMax.x-def.Bounds.PlayableMin.x+160,def.Bounds.PlayableMax.z-def.Bounds.PlayableMin.z+160);
                Width=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Rect.z*2)),512,2048);Height=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Rect.w*2)),512,2048);
                Pixels=new Color[Width*Height];
            }
            Vector2 World(int i,int j)=>new Vector2(Rect.x+(i+.5f)/Width*Rect.z,Rect.y+(j+.5f)/Height*Rect.w);
            void Range(Rect area,out int i0,out int i1,out int j0,out int j1)
            {
                i0=Mathf.Clamp(Mathf.FloorToInt((area.xMin-Rect.x)/Rect.z*Width),0,Width-1);i1=Mathf.Clamp(Mathf.CeilToInt((area.xMax-Rect.x)/Rect.z*Width),0,Width-1);
                j0=Mathf.Clamp(Mathf.FloorToInt((area.yMin-Rect.y)/Rect.w*Height),0,Height-1);j1=Mathf.Clamp(Mathf.CeilToInt((area.yMax-Rect.y)/Rect.w*Height),0,Height-1);
            }
            public void Join(RoadJoin join)
            {
                float reach=join.halfWidth+3;
                var corners=new[]{-10f,7f}.SelectMany(u=>new[]{-reach,reach}.Select(l=>join.seam+join.dir*u+join.lateral*l)).ToArray();
                var area=UnityEngine.Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y));
                Range(area,out int i0,out int i1,out int j0,out int j1);
                for(int j=j0;j<=j1;j++)for(int i=i0;i<=i1;i++)
                {
                    var d=World(i,j)-join.seam;float u=Vector2.Dot(d,join.dir),v=Mathf.Abs(Vector2.Dot(d,join.lateral));
                    // R: signed seam distance (0.5 + metres/16). G: join zone. B: wheel paths.
                    // Kept within ~4 m of the seam so opposite joins across a narrow road never overlap.
                    float zone=(1-Smooth(join.halfWidth-.5f,join.halfWidth+1.2f,v))*Smooth(-6,-3.5f,u)*(1-Smooth(2.5f,4.2f,u));
                    float wheel=(1-Smooth(.18f,.45f,Mathf.Abs(v-1.05f)))*Smooth(-8,-4,u)*(1-Smooth(3,4.8f,u));
                    int id=j*Width+i;var c=Pixels[id];
                    // Distance is written across the whole stamp so filtering never blends towards an empty texel.
                    if(zone>c.g||c.g==0f){c.g=zone;c.r=Mathf.Clamp01(.5f+u/16);}
                    c.b=Mathf.Max(c.b,wheel);Pixels[id]=c;
                }
            }
            public void Damp(Rect water)
            {
                Range(new Rect(water.xMin-4,water.yMin-4,water.width+8,water.height+8),out int i0,out int i1,out int j0,out int j1);
                for(int j=j0;j<=j1;j++)for(int i=i0;i<=i1;i++)
                {
                    var p=World(i,j);float dx=Mathf.Max(water.xMin-p.x,0,p.x-water.xMax),dz=Mathf.Max(water.yMin-p.y,0,p.y-water.yMax);
                    float damp=1-Smooth(.3f,3.5f,Mathf.Sqrt(dx*dx+dz*dz));
                    int id=j*Width+i;var c=Pixels[id];c.a=Mathf.Max(c.a,damp);Pixels[id]=c;
                }
            }
            public Texture2D Save(string map)
            {
                var texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false,true);texture.SetPixels(Pixels);texture.Apply();
                string path=WaterRoadArt+"/RoadBlend/"+map+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=false;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=2048;
                foreach(var platform in new[]{"Android","iPhone"}){var settings=importer.GetPlatformTextureSettings(platform);settings.name=platform;settings.overridden=true;settings.maxTextureSize=2048;settings.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(settings);}
                importer.SaveAndReimport();
                var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(path)??AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture2D>().FirstOrDefault();
                if(mask==null)throw new InvalidOperationException("Road blend mask did not import "+path+" type="+AssetDatabase.GetMainAssetTypeAtPath(path));
                return mask;
            }
        }
    }
}
