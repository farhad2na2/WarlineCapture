#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    // Native prefab rendering only. This is deliberately separate from mission acceptance.
    public static class SupportPopupNativeCapture
    {
        public static void Run()
        {
            SupportPopupPrefabBuilder.Build();
            var oldWorld=World.DefaultGameObjectInjectionWorld;string oldLocale=GameLocalization.CurrentLocaleCode;
            using var world=new World("Support native visual fixture");var em=world.EntityManager;var root=em.CreateEntity();
            var config=AssetDatabase.LoadAssetAtPath<SupportAbilityCatalogConfig>(SupportAbilityCatalogBuilder.CatalogPath);
            SupportCatalogProjection.Install(em,root,config,0,15);
            var session=em.GetComponentData<SupportSessionComponent>(root);session.SessionToken="support-visual-fixture";session.AttemptOrdinal=1;session.TestEncounter=1;session.Active=1;em.SetComponentData(root,session);
            var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);policy.TestGrantMask=1;em.SetComponentData(root,policy);
            var states=em.GetBuffer<SupportAbilityStateElement>(root);
            foreach(var a in config.Abilities)states.Add(new SupportAbilityStateElement {Kind=a.Kind,ChargesRemaining=a.Charges,StateVersion=1,Enabled=(byte)(a.Kind==SupportAbilityKind.Smoke?1:0)});
            var storage=em.CreateEntity(typeof(BuildingResourceStorageComponent));em.SetComponentData(storage,new BuildingResourceStorageComponent {OwnerFactionId=1,FuelStorageCapacity=200,StoredFuelBarrels=160,CivilianFuelReserveBarrels=40});
            em.SetComponentData(root,new SupportFuelAvailabilityComponent {Total=Game.Runtime.SupportFuelTransactionSystem.Available(em,1),Version=1});
            World.DefaultGameObjectInjectionWorld=world;UiShellEcsGateway.RegisterAsRuntimeGateway();
            try
            {
                Capture("en",1920,1080);Capture("fa-IR",2400,1080);
                for(byte kind=1;kind<=4;kind++){Capture("en",1920,1080,kind,true);Capture("fa-IR",2400,1080,kind,true);}
                Debug.Log("[SupportPopupNativeCapture] result=Passed captures=10 evidence=native-prefab-only normalInput=unverified");
            }
            finally
            {
                World.DefaultGameObjectInjectionWorld=oldWorld;UiShellEcsGateway.RegisterAsRuntimeGateway();GameLocalization.SetLocale(oldLocale,false);
                var catalog=em.GetComponentData<SupportCatalogComponent>(root);catalog.Blob.Dispose();
            }
        }
        private static void Capture(string locale,int width,int height,byte selected=1,bool lesson=false)
        {
            GameLocalization.SetLocale(locale,false);
            var cameraObject=new GameObject("SupportNativeCaptureCamera");var canvasObject=new GameObject("SupportNativeCaptureCanvas",typeof(RectTransform));
            RenderTexture render=null;Texture2D texture=null;GameObject instance=null;
            try
            {
                var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height*.5f;camera.aspect=width/(float)height;
                camera.transform.position=new Vector3(0,0,-10);camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.02f,.025f,1);
                var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                canvasObject.GetComponent<RectTransform>().sizeDelta=new Vector2(width,height);canvasObject.AddComponent<GraphicRaycaster>();
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SupportPopupPrefabBuilder.PopupPath);instance=UnityEngine.Object.Instantiate(prefab,canvas.transform,false);
                var rect=instance.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                UiShellRuntimeGateway.SelectSupport(selected);
                var em=World.DefaultGameObjectInjectionWorld.EntityManager;using var q=em.CreateEntityQuery(typeof(SupportMissionPolicyComponent));var root=q.GetSingletonEntity();var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);policy.LessonKind=lesson?(SupportAbilityKind)selected:SupportAbilityKind.None;em.SetComponentData(root,policy);
                if(!UiShellRuntimeGateway.TryReadSupport(out var model))throw new InvalidOperationException("Native fixture read model missing.");
                foreach(var binding in instance.GetComponentsInChildren<V3LocalizedTextBindingView>(true))binding.ApplyLocalization();
                instance.GetComponent<SupportPopupView>().Apply(model);Canvas.ForceUpdateCanvases();
                render=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);camera.targetTexture=render;
                foreach(var text in instance.GetComponentsInChildren<TMP_Text>(true))
                {text.ForceMeshUpdate();if(text.isTextTruncated)throw new InvalidOperationException(locale+" truncated text: "+text.name);}
                var panel=instance.transform.Find("SupportFrame/Outer") as RectTransform;var corners=new Vector3[4];panel.GetWorldCorners(corners);
                foreach(var corner in corners){var point=camera.WorldToScreenPoint(corner);if(point.x<0 || point.x>width || point.y<0 || point.y>height)throw new InvalidOperationException("Support panel exceeds "+locale+" capture bounds.");}
                var previous=RenderTexture.active;
                try
                {
                    RenderTexture.active=render;camera.Render();texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
                    texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                    const string folder="Design/AgentReports/SupportSystem/Evidence/NativePopup";Directory.CreateDirectory(folder);
                    File.WriteAllBytes(Path.Combine(folder,"support-"+locale+"-"+width+"x"+height+(lesson?"-lesson-"+selected:"-final")+".png"),texture.EncodeToPNG());
                }
                finally{RenderTexture.active=previous;camera.targetTexture=null;}
            }
            finally
            {
                if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);
                if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
                if(render!=null){render.Release();UnityEngine.Object.DestroyImmediate(render);}
                UnityEngine.Object.DestroyImmediate(canvasObject);UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
#endif
