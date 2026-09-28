#if UNITY_EDITOR
using System;
using System.IO;
using Game.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Game.Editor
{
    public static class SupportSourceAssetCapture
    {
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Source asset inspection requires stopped Play mode in the existing Editor.");
            string output="Design/AgentReports/SupportSystem/Evidence/SourceAssets/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(output);
            Capture("Assets/Game/Prefabs/Vehicles/Unit_Veh_Jet_01.prefab",output+"/strike-jet.png",false);
            Capture("Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab",output+"/transport.png",false);
            Capture("Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_Crate_01.prefab",output+"/crate.png",false);
            Capture("Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Parachute_01.prefab",output+"/parachute.png",false);
            Capture("Assets/PolygonMilitary/Prefabs/FX/FX_Smoke_Medium_01.prefab",output+"/smoke-established.png",true);
            Debug.Log("[SupportSourceAssetCapture] result=Rendered assets=5 visualReview=pending output="+output);
        }
        private static void Capture(string path,string output,bool smoke)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Missing source asset "+path);
            var scene=EditorSceneManager.NewPreviewScene();RenderTexture target=null;Texture2D image=null;
            try
            {
                var model=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(model,scene);
                model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                foreach(var transform in model.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=30;
                foreach(var lod in model.GetComponentsInChildren<LODGroup>(true))lod.ForceLOD(0);
                Bounds bounds=default;bool first=true;
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    if(!renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_SnivelerModelShown",1);renderer.SetPropertyBlock(block);
                    if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                }
                if(smoke)
                {
                    SupportSmokePresentationSystem.ConfigureSmoke(model,12);
                    foreach(var p in model.GetComponentsInChildren<ParticleSystem>())p.Simulate(5,true,true,false);
                    bounds=new Bounds(Vector3.up,new Vector3(35,10,35));
                }
                else if(first)throw new InvalidOperationException("Source has no active renderer "+path);
                var cameraObject=new GameObject("Support source inspection camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.18f,.2f,.23f);camera.orthographic=true;camera.aspect=1;camera.nearClipPlane=.01f;
                float span=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);camera.farClipPlane=span*8+100;camera.orthographicSize=span*.65f;
                camera.transform.position=bounds.center+new Vector3(1,.8f,-1).normalized*(span*3+10);camera.transform.LookAt(bounds.center);
                var lightObject=new GameObject("Support source inspection light");SceneManager.MoveGameObjectToScene(lightObject,scene);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.cullingMask=1<<30;
                light.transform.rotation=Quaternion.Euler(45,-35,0);light.shadows=LightShadows.None;
                target=new RenderTexture(1024,1024,24);target.Create();camera.targetTexture=target;camera.Render();
                var prior=RenderTexture.active;
                try {RenderTexture.active=target;image=new Texture2D(1024,1024,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1024,1024),0,0);image.Apply();File.WriteAllBytes(output,image.EncodeToPNG());}
                finally {RenderTexture.active=prior;}
                Debug.Log("[SupportSourceAsset] prefab="+path+" guid="+AssetDatabase.AssetPathToGUID(path)+" output="+output);
            }
            finally
            {
                if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
#endif
