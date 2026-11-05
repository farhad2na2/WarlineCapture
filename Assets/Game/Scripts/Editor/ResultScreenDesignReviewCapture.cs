#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Game.Editor
{
    // Design inspection only: current native views with representative read models.
    // No gameplay outcome, settlement or save state is changed by these fixtures.
    public static class ResultScreenDesignReviewCapture
    {
        const string Output = "Design/AgentReports/ResultScreenReview20261021/Implementation/NativeFirst";
        public static void Capture()
        {
            bool passed = false;
            string locale = GameLocalization.CurrentLocaleCode;
            var originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            try
            {
                Directory.CreateDirectory(Output);
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                GameLocalization.Initialize(catalog, "en", false);
                foreach (string mode in new[] { "campaign", "skirmish", "operations" })
                    foreach (bool victory in new[] { true, false }) Render(mode, victory);
                passed = true;
                Debug.Log("[ResultScreenDesignReview] result=Captured nativeViews=6 representativeModels=true missionReadiness=NotClaimed candidate=SharedPrefabImplementation");
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                GameLocalization.SetLocale(locale, false);
                if (originalScene.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(originalScene);
                EditorSceneManager.CloseScene(scene, true);
                MissionEditorValidationExit.Complete(passed);
            }
        }
        public static GameObject CreateCase(RectTransform rect,string mode,bool victory)
        {
                string W(string key,string en,string fa)=>SharedMissionResultView.Words("capture_"+key,en,fa);
                if (mode == "campaign")
                {
                    var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MissionResultV3PrefabBuilder.PrefabPath),rect);
                    var ir = (RectTransform)instance.transform; ir.anchorMin=Vector2.zero;ir.anchorMax=Vector2.one;ir.offsetMin=ir.offsetMax=Vector2.zero;
                    var model = new UiMissionResultPopupModel(1,"saga.ch01.m01.first_contact",victory ? UiMissionResultOutcome.Victory : UiMissionResultOutcome.Loss,
                        victory ? "VICTORY" : "DEFEAT", "FIRST CONTACT • OLD MARKET", victory ? "Hostile patrol neutralized. The Old Market corridor is secure." : "The command squad was lost. Regroup and redeploy.",
                        victory ? (byte)3 : (byte)0, victory ? "00:44" : "01:12",victory ? "0" : "1",victory ? "3/3" : "1/3",victory ? "260 Commander XP  ·  1200 Credits" : "NO REWARD",victory ? "CONTINUE" : "RETRY MISSION",true,!victory,firstContact:new UiMissionFirstContactResultDetails(true,victory));
                    var view=instance.GetComponent<MissionResultPopupView>();view.Apply(in model);
                    foreach(var layout in instance.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true))layout.RefreshLayout();
                    Canvas.ForceUpdateCanvases();view.Apply(in model);
                    return instance;
                }
                else
                {
                    var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset");
                    var card=CampaignStyleResultCard.Mount(rect,font);card.Bind(()=>{},()=>{},()=>{},()=>{});card.BindBattlefield(()=>{});
                    ModeResultContent content;
                    if(mode=="skirmish")
                    {
                        var model=new UiSkirmishModel{Finished=true,CatalogId="S002",MissionTitle=W("siege","COMMAND SIEGE","محاصرهٔ فرماندهی"),ResultTitle=victory?"VICTORY":"DEFEAT",ResultStars=victory?(byte)3:(byte)0,
                            ResultElapsed=victory?"06:42":"05:18",PlayerUnitsLost=victory?2:8,EnemyUnitsLost=victory?12:6,PlayerBuildingsLost=victory?0:1,EnemyBuildingsLost=victory?1:0,
                            PlayerBaseHeld=victory,EnemyBaseDestroyed=victory,MedalIsFirstAward=victory,MedalScenarioId=victory?"S002":"",MedalDifficulty=1,
                            Objective="Destroy the enemy HQ. Keep your own HQ standing.",ResultDetail=victory?W("hq_destroyed","Enemy HQ destroyed. Your main base held.","ستاد دشمن نابود شد. پایگاه اصلی شما پابرجا ماند."):W("base_lost","Your main base was lost.","پایگاه اصلی شما از دست رفت.")};
                        content=(ModeResultContent)typeof(SkirmishMatchView).GetMethod("BuildResult",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{model});
                    }
                    else
                    {
                        var model=new UiOperationsMissionModel{Finished=true,Saved=true,ResultStars=victory?(byte)3:(byte)0,ResultTitle=victory?"VICTORY":"OPERATION FAILED",ResultStatus=victory?W("evidence","EVIDENCE SECURED","شواهد ایمن شد"):W("timeout","TIME EXPIRED","زمان به پایان رسید"),
                            ResultElapsed=victory?"09:40":"12:00",CompletedScans=victory?3:2,EvidenceRecovered=victory,InfantryAtExit=victory?4:0,SurvivingInfantry=victory?4:2,
                            ResultSummary=victory?W("safe_return","Relay evidence recovered. The squad returned safely.","شواهد رله بازیابی شد. گروه سالم بازگشت."):W("deadline","The operation deadline passed before extraction.","مهلت عملیات پیش از خروج به پایان رسید.")};
                        var owner=new GameObject("OperationProjection",typeof(OperationsMissionScreenView));
                        try{content=(ModeResultContent)typeof(OperationsMissionScreenView).GetMethod("BuildResult",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner.GetComponent<OperationsMissionScreenView>(),new object[]{model});}
                        finally{Object.DestroyImmediate(owner);}
                    }
                    card.Present(in content);return card.gameObject;
                }
        }
        static void Render(string mode, bool victory)
        {
            var go = new GameObject("ResultDesignCamera", typeof(Camera));
            var camera = go.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 540;
            camera.aspect = 1920f / 1080; camera.transform.position = new Vector3(0, 0, -100);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(5, 16, 23, 255);
            var target = new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1920,1080,TextureFormat.RGBA32,false);
            var previous = RenderTexture.active; camera.targetTexture = target;
            var root = new GameObject("ResultDesignCanvas",typeof(RectTransform),typeof(Canvas));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(1920,1080);
            try
            {
                CreateCase(rect,mode,victory);
                Canvas.ForceUpdateCanvases();
                foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))t.ForceMeshUpdate();
                camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                string id=mode+"-"+(victory?"victory":"defeat");File.WriteAllBytes(Output+"/"+id+".png",texture.EncodeToPNG());
                File.WriteAllLines(Output+"/"+id+"-texts.txt",Array.ConvertAll(root.GetComponentsInChildren<TMP_Text>(),t=>t.transform.parent.name+"/"+t.name+" | "+t.text));
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(root);Object.DestroyImmediate(go);Object.DestroyImmediate(texture);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
#endif
