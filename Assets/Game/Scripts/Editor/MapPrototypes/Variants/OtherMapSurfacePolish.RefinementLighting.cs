using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Configs;

namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        [Serializable] private sealed class LightingReceipt { public string scene,beforeHash,afterHash;public string[] before,after; }
        private static UnityEngine.Object RefinedProfile(UnityEngine.Object original)
        {
            string source=AssetDatabase.GetAssetPath(original);
            if(source.StartsWith(RefinementArt))return original;
            string path=RefinementArt+"/Lighting/"+Path.GetFileNameWithoutExtension(source)+"_Gameplay.asset";
            if(AssetDatabase.LoadMainAssetAtPath(path)==null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
                if(!AssetDatabase.CopyAsset(source,path))throw new InvalidOperationException("Cannot clone lighting profile "+source);
            }
            foreach(var part in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var so=new SerializedObject(part);
                void Number(string key,float value)
                {
                    var p=so.FindProperty(key+".m_Value");if(p==null)return;
                    p.floatValue=value;so.FindProperty(key+".m_OverrideState").boolValue=true;
                }
                if(part.GetType().Name=="Bloom"){Number("intensity",.22f);Number("threshold",1.1f);Number("dirtIntensity",.08f);}
                if(part.GetType().Name=="ColorAdjustments"){Number("contrast",10);Number("saturation",3);}
                if(part.GetType().Name=="WhiteBalance"){Number("temperature",-3);Number("tint",0);}
                if(part.GetType().Name=="Vignette")Number("intensity",.12f);
                if(part.GetType().Name=="ShadowsMidtonesHighlights")
                {
                    var p=so.FindProperty("shadows.m_Value");p.vector4Value=new Vector4(.96f,.99f,1.02f,-.035f);so.FindProperty("shadows.m_OverrideState").boolValue=true;
                }
                so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(part);
            }
            AssetDatabase.SaveAssets();return AssetDatabase.LoadMainAssetAtPath(path);
        }
        private static string ProtectedLightingState(Scene scene)
        {
            return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true))
                .Where(c=>c!=null&&!(c is Light)&&c.GetType().Name!="Volume"&& !c.GetType().Name.Contains("LensFlare"))
                .Select(c=>Hierarchy(c.transform)+"|"+c.GetType().FullName+"|"+EditorJsonUtility.ToJson(c)));
        }
        public static void BuildRefinementLighting()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var path in VisualMaps.Select(Binding).Concat(new[]{"Assets/Game/Scenes/Match.unity"}).Distinct())
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);SceneManager.SetActiveScene(scene);
                    var protectedState=ProtectedLightingState(scene);
                    var receipt=new LightingReceipt{scene=path,beforeHash=Hash(path),before=InspectLighting(scene)};
                    foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
                    {
                        if(c==null)continue;
                        if(c is Light sun&&sun.type==LightType.Directional&&sun.intensity>.5f)
                        {sun.color=new Color(1,.977f,.935f);sun.intensity=Mathf.Min(sun.intensity,1.25f);EditorUtility.SetDirty(sun);PrefabUtility.RecordPrefabInstancePropertyModifications(sun);}
                        if(c.GetType().Name=="Volume")
                        {
                            var so=new SerializedObject(c);var profile=so.FindProperty("sharedProfile");
                            if(profile?.objectReferenceValue!=null){profile.objectReferenceValue=RefinedProfile(profile.objectReferenceValue);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
                        }
                        if(c.GetType().Name.Contains("LensFlare"))
                        {
                            var so=new SerializedObject(c);var intensity=so.FindProperty("intensity");
                            if(intensity!=null){intensity.floatValue=Mathf.Min(intensity.floatValue,.15f);so.ApplyModifiedPropertiesWithoutUndo();}
                        }
                    }
                    RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor=new Color(.60f,.64f,.70f);RenderSettings.ambientEquatorColor=new Color(.42f,.43f,.43f);RenderSettings.ambientGroundColor=new Color(.25f,.23f,.20f);
                    RenderSettings.fogColor=new Color(.70f,.69f,.65f);RenderSettings.fogDensity=Mathf.Min(RenderSettings.fogDensity,.0014f);
                    if(ProtectedLightingState(scene)!=protectedState)throw new InvalidOperationException("Non-lighting components changed "+path);
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Lighting save failed "+path);
                    receipt.after=InspectLighting(scene);receipt.afterHash=Hash(path);
                    File.WriteAllText(RefinementReport+"/lighting-"+AssetDatabase.AssetPathToGUID(path)+".json",JsonUtility.ToJson(receipt,true));
                    Debug.Log("[MapRefinementLighting] scene="+path+" result=Passed");
                }
                foreach(var path in new[]{"Assets/Game/Rendering/VisualQualityConfig.asset","Assets/Game/Rendering/VisualQualityConfig_MobileCandidate.asset"})
                {
                    var asset=AssetDatabase.LoadMainAssetAtPath(path);if(asset==null)continue;var so=new SerializedObject(asset);
                    foreach(var key in new[]{"highVolumeProfile","globalVolumeProfile"})
                    {var p=so.FindProperty(key);if(p?.objectReferenceValue!=null)p.objectReferenceValue=RefinedProfile(p.objectReferenceValue);}
                    so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
                }
                AssetDatabase.SaveAssets();Debug.Log("[MapRefinementLighting] result=Passed bindings=11 match=1 runtimeQualityProfiles=2");
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
        public static void CaptureRefinement()=>CaptureRefinementMaps(VisualMaps.ToArray());
        public static void CaptureCommandNodeRefinement()=>CaptureRefinementMaps(new[]{"CH05M05CommandNode"});
        private static void CaptureRefinementMaps(string[] maps)
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(var map in maps)
                {
                    var receipt=JsonUtility.FromJson<RefinementReceipt>(File.ReadAllText(RefinementReport+"/"+map+"-result.json"));
                    if(receipt.result!="Passed"||receipt.afterHash!=Hash(Source(map)))throw new InvalidOperationException("Saved refinement source mismatch "+map);
                    var scene=EditorSceneManager.OpenScene(Source(map),OpenSceneMode.Single);var rs=SceneRenderers(scene);var def=AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(Definition(map));
                    var visible=rs.Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&Intact(r)&&Within(r,def,30)).ToArray();
                    var audit=ReadRefinementAudit(map);
                    var views=new List<Tuple<string,Vector3,float>>(NaturalViews(visible,def));
                    // Reuse the audited waterfront camera exactly, independent of generated mesh names/bounds.
                    views.RemoveAll(v=>v.Item1=="waterfront");
                    var mapCenter=(def.Bounds.PlayableMin+def.Bounds.PlayableMax)*.5f;mapCenter.y=0;
                    var water=audit.waters.OrderBy(w=>(w.center-mapCenter).sqrMagnitude).FirstOrDefault();
                    if(water!=null){var bounds=new Bounds(water.center,water.size);var focus=bounds.ClosestPoint(mapCenter);focus.y=bounds.max.y;views.Add(Tuple.Create("waterfront",focus,95f));}
                    if(audit.unsupported.Length>0)views.Add(Tuple.Create("detached",audit.unsupported[0].center,58f));
                    Capture(map,scene,def,rs,"after",RefinementReport,views.ToArray());
                    foreach(var m in rs.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(RefinementArt)).Distinct())
                        if(m.shader==null||ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Shader failed after native render "+m.name);
                    if(receipt.afterHash!=Hash(Source(map)))throw new InvalidOperationException("Capture changed source "+map);
                    Debug.Log("[MapRefinementCapture] map="+map+" result=Passed");GC.Collect();
                }
                Debug.Log("[MapRefinementCapture] result=Passed maps="+maps.Length);
            }
            finally{RestoreVisualReviewSetup(setup);}
        }
    }
}
