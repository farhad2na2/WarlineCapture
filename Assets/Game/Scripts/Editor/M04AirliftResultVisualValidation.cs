using System;
using System.IO;
using System.Collections.Generic;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Game.Editor
{
    public static class M04AirliftResultVisualValidation
    {
        public static void Run()
        {
            M04AirliftPresentationBuilder.Build();
            string priorLocale=GameLocalization.CurrentLocaleCode;
            var failures=new List<string>();int captures=0;
            string output="Design/AgentReports/M04Airlift/ResultQA";Directory.CreateDirectory(output);
            try
            {
                foreach(int width in new[]{1920,2400})foreach(bool fa in new[]{false,true})foreach(bool victory in new[]{true,false})
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);GameLocalization.SetLocale(fa?"fa-IR":"en",false);
                    var cameraObject=new GameObject("ResultReviewCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
                    camera.orthographic=true;camera.orthographicSize=1080;camera.aspect=width/1080f;camera.transform.position=new Vector3(0,0,-100);
                    var target=new RenderTexture(width,1080,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,1080,TextureFormat.RGBA32,false);var previous=RenderTexture.active;camera.targetTexture=target;
                    var canvasObject=new GameObject("ResultReviewCanvas",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                    var canvasRect=(RectTransform)canvas.transform;canvasRect.sizeDelta=new Vector2(width*2,2160);
                    var instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MissionResultV3PrefabBuilder.PrefabPath),canvasRect);
                    var rect=(RectTransform)instance.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                    string id=(victory?"victory":"defeat")+(fa?"-fa-":"-en-")+width;
                    try
                    {
                        string rewards="500 "+(fa?"تجربهٔ فرمانده":"Commander XP")+"  ·  2500 "+(fa?"اعتبار":"Credits")+"  ·  1 "+GameText.Get("mission.m04.reward.laila")+"  ·  1 "+GameText.Get("mission.m04.reward.transport");
                        var model=new UiMissionResultPopupModel(1,"saga.ch01.m04.airlift",victory?UiMissionResultOutcome.Victory:UiMissionResultOutcome.Loss,
                            GameText.Get("mission.m04.result."+(victory?"victory":"defeat")),GameText.Get("mission.m04.result.subtitle"),GameText.Get(victory?"mission.m04.result.success":"mission.m04.result.carrier_lost"),
                            victory?(byte)3:(byte)0,"01:15","0","0/4",victory?rewards:"—",GameText.Get(victory?"mission.m03.action.continue":"mission.m03.result.retry"),true,!victory,true,false,default,false,
                            new UiMissionExtractionResultDetails(victory?4:0,0,victory?4:0,!victory,false,false));
                        if(string.IsNullOrWhiteSpace(model.PrimaryActionLabel))throw new InvalidOperationException("Missing localized result action");
                        var view=instance.GetComponent<MissionResultPopupView>();view.Apply(in model);
                        for(int i=0;i<3;i++){foreach(var layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>())layout.RefreshLayout();Canvas.ForceUpdateCanvases();}
                        view.Apply(in model);foreach(var binding in instance.GetComponentsInChildren<V3LocalizedTextBindingView>())binding.ApplyLocalization();Canvas.ForceUpdateCanvases();
                        foreach(var text in instance.GetComponentsInChildren<TMP_Text>())
                        {text.ForceMeshUpdate();if(text.isTextOverflowing||text.isTextTruncated)failures.Add(id+" "+text.transform.parent.name+"/"+text.name+" source="+text.text+" rect="+text.rectTransform.rect+" preferred="+text.preferredWidth+","+text.preferredHeight);}
                        camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,1080),0,0);texture.Apply();File.WriteAllBytes(output+"/"+id+".png",texture.EncodeToPNG());captures++;
                    }
                    finally{RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(canvasObject);Object.DestroyImmediate(cameraObject);}
                }
                File.WriteAllLines(output+"/layout.txt",failures);
                if(failures.Count>0)throw new InvalidOperationException(string.Join("\n",failures));
                Debug.Log("[M04ResultVisualValidation] result=Passed captures="+captures+" locales=2 aspects=2 outcomes=2");
            }
            finally{GameLocalization.SetLocale(priorLocale,false);}
        }
    }
}
