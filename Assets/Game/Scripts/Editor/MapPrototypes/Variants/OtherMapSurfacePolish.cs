using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        private const string Report="Design/AgentReports/OtherMapSurfacePolish20261009";
        private static readonly string[] Maps={"CityEdgeAirfield","RefineryDistrict","AshLinePort","CH04M04GroundedSignal","CH04M05ArmorBreak","CH05M01CitywideAlert","CH05M02TrustUnderFire","CH05M03NetworkCollapse","CH05M04LastCorridor","CH05M05CommandNode"};
        private static bool Variant(string map)=>!map.StartsWith("CH");
        private static string Source(string map)=>"Assets/Game/Scenes/OperationMaps/"+(Variant(map)?"Variants/":"")+map+"/PreparedEntities.unity";
        private static string Binding(string map)=>Path.GetDirectoryName(Source(map))+"/RuntimeBinding.unity";
        private static string Definition(string map)=>"Assets/Game/GeneratedOperationMaps/"+(Variant(map)?"Variants/":"")+map+(Variant(map)?"/Candidate":"")+"/Definition.asset";
        [Serializable] private sealed class SurfaceRow{public string name,path,mesh,material,shader;public Vector3 center,size;}
        [Serializable] private sealed class MaterialRow{public string path,shader;public int renderers;}
        [Serializable] private sealed class Audit{public string map,source,sha256;public Vector3 min,max;public int rendererCount;public SurfaceRow[] surfaces;public MaterialRow[] materials;}
        private static string Hierarchy(Transform t)=>t.parent==null?t.name:Hierarchy(t.parent)+"/"+t.name;
        private static string Hash(string path){using var h=SHA256.Create();return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static void Check(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit mode required.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Existing scene has unsaved edits.");}
        public static void AuditAndCapture(){Run(false);}
        private static void Run(bool apply)
        {
            Check();Directory.CreateDirectory(Report);var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string map in Maps)
                {
                    var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));if(def==null)throw new InvalidOperationException("Missing map definition "+map);
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);
                    var rs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var audit=new Audit{map=map,source=Source(map),sha256=Hash(Source(map)),min=def.Bounds.PlayableMin,max=def.Bounds.PlayableMax,rendererCount=rs.Length,
                        surfaces=rs.Where(r=>IsSurface(r)).Select(r=>new SurfaceRow{name=r.name,path=Hierarchy(r.transform),mesh=r.GetComponent<MeshFilter>()?.sharedMesh?.name,material=AssetDatabase.GetAssetPath(r.sharedMaterial),shader=r.sharedMaterial?.shader?.name,center=r.bounds.center,size=r.bounds.size}).ToArray(),
                        materials=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).GroupBy(m=>m).Select(g=>new MaterialRow{path=AssetDatabase.GetAssetPath(g.Key),shader=g.Key.shader.name,renderers=g.Count()}).OrderByDescending(x=>x.renderers).ToArray()};
                    File.WriteAllText(Report+"/"+map+"-audit.json",JsonUtility.ToJson(audit,true));
                    Capture(map,scene,def,rs,"before");
                    if(Hash(Source(map))!=audit.sha256)throw new InvalidOperationException("Source scene changed during audit.");
                    Debug.Log($"[OtherMapSurfaceAudit] map={map} result=Passed renderers={rs.Length} surfaces={audit.surfaces.Length}");
                }
                Debug.Log("[OtherMapSurfaceAudit] result=Passed maps="+Maps.Length);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static bool IsSurface(MeshRenderer r)
        {
            string n=r.name.ToLowerInvariant();return n.Contains("ground")||n.Contains("paving")||n.Contains("asphalt")||n.Contains("concrete")||n.Contains("road")||n.Contains("sandwear")||n.Contains("sidewalk")||n.Contains("terrain")||n.Contains("surface");
        }
        private static void Capture(string map,Scene scene,OperationMapDefinition def,MeshRenderer[] rs,string label)
        {
            var binding=SceneManager.GetSceneByPath(Binding(map));if(!binding.IsValid()||!binding.isLoaded)binding=EditorSceneManager.OpenScene(Binding(map),OpenSceneMode.Additive);
            SceneManager.SetActiveScene(binding);
            // Standalone CH05 bindings deliberately contain no sun. MatchSceneView keeps
            // the main Match scene active at runtime; include that authored environment here.
            var lighting=default(Scene);
            bool hasSun=new[]{scene,binding}.SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Any(l=>l.type==LightType.Directional&&l.enabled&&l.gameObject.activeInHierarchy);
            if(!hasSun)
            {
                lighting=EditorSceneManager.OpenScene("Assets/Game/Scenes/Match.unity",OpenSceneMode.Additive);
                foreach(var root in lighting.GetRootGameObjects())
                {
                    foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                    foreach(var c in root.GetComponentsInChildren<Camera>(true))c.enabled=false;
                    foreach(var c in root.GetComponentsInChildren<Canvas>(true))c.enabled=false;
                }
                SceneManager.SetActiveScene(lighting);
                if(!lighting.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Any(l=>l.type==LightType.Directional&&l.enabled&&l.gameObject.activeInHierarchy))throw new InvalidOperationException("Capture requires the authored Match sun: "+map);
            }
            foreach(var owner in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)))if(owner.DestroyedVisualRoot!=null)owner.DestroyedVisualRoot.SetActive(false);
            bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var go=new GameObject("SurfaceReviewCamera");SceneManager.MoveGameObjectToScene(go,binding);var camera=go.AddComponent<Camera>();camera.nearClipPlane=.3f;camera.farClipPlane=4000;camera.fieldOfView=48;
            var type=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");if(type!=null){var data=go.AddComponent(type);type.GetProperty("renderPostProcessing")?.SetValue(data,true);}
            var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32){antiAliasing=4};var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;var min=def.Bounds.PlayableMin;var max=def.Bounds.PlayableMax;var center=(min+max)*.5f;center.y=0;
                var buildings=rs.Where(r=>r.bounds.size.y>3&&r.bounds.size.x>4&&r.bounds.size.z>4&&r.bounds.size.x<100&&r.bounds.size.z<100&&r.bounds.center.x>min.x&&r.bounds.center.x<max.x&&r.bounds.center.z>min.z&&r.bounds.center.z<max.z)
                    .Where(r=>{string n=(r.GetComponent<MeshFilter>()?.sharedMesh?.name??r.name).ToLowerInvariant();return n.Contains("house")||n.Contains("hangar")||n.Contains("factory")||n.Contains("warehouse")||n.Contains("building");}).ToArray();
                Vector3 close=buildings.OrderBy(r=>(r.bounds.center-center).sqrMagnitude).Select(r=>r.bounds.center).DefaultIfEmpty(center).First();close.y=0;
                var views=new[]{("overview",center,Mathf.Max(max.x-min.x,max.z-min.z)*.72f),("detail",close,85f)};
                foreach(var view in views)
                {
                    // Wide inspection cameras exceed gameplay camera altitude. Suppress only
                    // distance fog/post processing in the overview, retaining native detail lighting.
                    bool fog=RenderSettings.fog;var data=type==null?null:go.GetComponent(type);
                    try
                    {
                        if(view.Item1=="overview"){RenderSettings.fog=false;type?.GetProperty("renderPostProcessing")?.SetValue(data,false);}
                        go.transform.rotation=Quaternion.Euler(58,0,0);go.transform.position=view.Item2-go.transform.forward*view.Item3;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Report+"/"+map+"-"+label+"-"+view.Item1+".png",image.EncodeToPNG());
                    }
                    finally{RenderSettings.fog=fog;type?.GetProperty("renderPostProcessing")?.SetValue(data,true);}
                }
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);ShaderUtil.allowAsyncCompilation=async;if(lighting.IsValid()&&lighting.isLoaded){SceneManager.SetActiveScene(binding);EditorSceneManager.CloseScene(lighting,true);}}
        }
    }
}
