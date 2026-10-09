using System;
using System.IO;
using System.Linq;
using Game.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    public static partial class SkirmishSurfacePolish
    {
        private const string Source = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        private const string Binding = "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";
        private const string Report = "Design/AgentReports/SkirmishSurfacePolish20261009";
        private const string Art = "Assets/Game/Art/MapBeautify/DenseCity";
        [Serializable] private sealed class Row { public string name, path, mesh, material; public Vector3 position, center, size, euler, scale; public bool enabled; }
        [Serializable] private sealed class Inventory { public Row[] roads, buildings, ground; }
        private static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent)+"/"+t.name;
        private static Row Describe(Renderer r) => new Row { name=r.name, path=Hierarchy(r.transform), mesh=r.GetComponent<MeshFilter>()?.sharedMesh?.name,
            material=r.sharedMaterial?.name, position=r.transform.position, center=r.bounds.center, size=r.bounds.size,
            euler=r.transform.eulerAngles, scale=r.transform.lossyScale, enabled=r.enabled };
        public static void AuditAndCaptureBefore()
        {
            Check(); Directory.CreateDirectory(Report);
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
                var rs=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                var inventory=new Inventory {
                    roads=rs.Where(r=>r.name=="Dirt" || r.name=="Asphalt" || r.name.Contains("Paving")).Select(Describe).ToArray(),
                    buildings=rs.Where(r=>r.name.StartsWith("SM_Bld_")).Select(Describe).ToArray(),
                    ground=rs.Where(r=>r.name.StartsWith("SM_Env_Ground_")).Select(Describe).ToArray() };
                File.WriteAllText(Report+"/source-inventory.json",JsonUtility.ToJson(inventory,true));
                Capture(scene,"before");
                Debug.Log($"[SkirmishSurfacePolish] audit=Passed roads={inventory.roads.Length} buildings={inventory.buildings.Length} ground={inventory.ground.Length}");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        public static void AuditSandOverlays()
        {
            Check();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
                var rows=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
                    .Where(r=>r.bounds.center.x>910 && r.bounds.center.x<1020 && r.bounds.center.z>300 && r.bounds.center.z<390 && r.bounds.size.y<4).Select(Describe).ToArray();
                File.WriteAllText(Report+"/sand-overlay-audit.json",JsonUtility.ToJson(new Inventory{ground=rows},true));
                Debug.Log("[SkirmishSandAudit] result=Passed count="+rows.Length);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static void Check()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required.");
            for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save existing scene edits before surface authoring.");
        }
        private static void Capture(Scene scene,string label)
        {
            bool asyncCompilation=ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation=false;
            var binding=SceneManager.GetSceneByPath(Binding);
            if(!binding.IsValid() || !binding.isLoaded) binding=EditorSceneManager.OpenScene(Binding,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(binding);
            foreach(var owner in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OperationMapBuildingAuthoring>(true)))
                if(owner.DestroyedVisualRoot!=null) owner.DestroyedVisualRoot.SetActive(false);
            var go=new GameObject("SurfaceReviewCamera"); SceneManager.MoveGameObjectToScene(go,binding);
            var camera=go.AddComponent<Camera>();camera.nearClipPlane=.3f;camera.farClipPlane=3000;camera.fieldOfView=48;
            var type=Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if(type!=null){var data=go.AddComponent(type);type.GetProperty("renderPostProcessing")?.SetValue(data,true);}
            var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32){antiAliasing=4};
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=rt;
                var views=new[]{("old-quarter",new Vector3(1040,0,530),85f),("military-yard",new Vector3(970,0,365),85f),("player-base",new Vector3(949,0,650),80f),("town-junction",new Vector3(1110,0,770),90f),("enemy-base",new Vector3(1405,0,650),85f),("district-overview",new Vector3(1180,0,700),280f)};
                foreach(var view in views)
                {
                    go.transform.rotation=Quaternion.Euler(58,0,0);go.transform.position=view.Item2-go.transform.forward*view.Item3;
                    camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
                    File.WriteAllBytes(Report+"/"+label+"-"+view.Item1+".png",image.EncodeToPNG());
                }
            }
            finally {camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);ShaderUtil.allowAsyncCompilation=asyncCompilation;}
        }
    }
}
