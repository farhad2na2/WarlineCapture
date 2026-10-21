#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Game.Editor
{
    public static partial class SharedMissionResultValidation
    {
        static int checks,captures;
        static string Output;
        public static void BuildAndRun()
        {
            try{SharedMissionResultBuilder.Build();Run();}
            catch(Exception e){Debug.LogException(e);MissionEditorValidationExit.Complete(false);}
        }
        public static void Run()=>MissionEditorValidationExit.Complete(Validate());
        public static void RunThen(Action continuation)
        {if(Validate())continuation();else MissionEditorValidationExit.Complete(false);}
        static bool Validate()
        {
            bool passed=false;string locale=GameLocalization.CurrentLocaleCode;
            var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            try
            {
                Output="Design/AgentReports/ResultScreenReview20261021/Implementation/NativeMatrix-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(Output);checks=captures=0;GameLocalization.SetLocale("en",false);
                CheckSharedAsset();CheckClassifier();CheckActions();CheckCampaignReuseAndSettlement();CheckLongTitle();CheckReplayRewardIcons();
                foreach(var sample in new[]{("en",1920,1080),("en",1280,720),("fa-IR",1920,1080),("fa-IR",2400,1080),("fa-IR",1440,1080)})
                {
                    GameLocalization.SetLocale(sample.Item1,false);
                    foreach(string mode in new[]{"campaign","skirmish","operations"})foreach(bool victory in new[]{true,false})
                        Render(mode,victory,sample.Item1,sample.Item2,sample.Item3);
                }
                CheckCampaignCatalog();
                passed=true;
                Debug.Log("[SharedMissionResultValidation] result=Passed checks="+checks+" captures="+captures+" locales=en,fa-IR modes=3 outcomes=2 sharedPrefab=1 fixtures=true missionReadiness=NotClaimed output="+Output);
            }
            catch(Exception e){Debug.LogException(e);Debug.LogError("[SharedMissionResultValidation] result=Failed checks="+checks+" captures="+captures);}
            finally
            {
                GameLocalization.SetLocale(locale,false);if(original.IsValid())UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);
            }
            return passed;
        }
        static void CheckSharedAsset()
        {
            var shared=Resources.Load<GameObject>(SharedMissionResultView.ResourcePath);
            Check(AssetDatabase.GetAssetPath(shared)==SharedMissionResultBuilder.Prefab,"One native resource prefab");
            foreach(string path in new[]{MissionResultV3PrefabBuilder.PrefabPath,"Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab"})
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var owner=root.GetComponent<MissionResultPopupView>();if(owner==null)continue;
                    Check(owner.SharedResult!=null,"Campaign uses shared renderer");
                    var source=PrefabUtility.GetCorrespondingObjectFromSource(owner.SharedResult.gameObject);
                    Check(AssetDatabase.GetAssetPath(source)==SharedMissionResultBuilder.Prefab,"Campaign nested renderer preserves prefab identity");
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        static void CheckClassifier()
        {
            Check(SharedResultStateClassifier.Classify("INCOMPLETE")==ResultObjectiveState.Incomplete,"INCOMPLETE cannot be treated as COMPLETE");
            Check(SharedResultStateClassifier.Classify("NOT EARNED")==ResultObjectiveState.Incomplete,"Unearned goal not a completed goal");
            Check(SharedResultStateClassifier.Classify("UNDAMAGED")==ResultObjectiveState.Complete,"UNDAMAGED cannot be treated as DAMAGED");
            Check(SharedResultStateClassifier.Classify("2 / 3")==ResultObjectiveState.Incomplete,"Partial progress remains incomplete");
            Check(SharedResultStateClassifier.Classify("3 / 3")==ResultObjectiveState.Complete,"Complete numeric goal");
        }
        static void CheckActions()
        {
            var host=new GameObject("ActionFixture",typeof(RectTransform));((RectTransform)host.transform).sizeDelta=new Vector2(1920,1080);
            var card=CampaignStyleResultCard.Mount(host.transform,null);int retry=0,replay=0,leave=0,adjust=0,battlefield=0;
            card.Bind(()=>retry++,()=>replay++,()=>leave++,()=>adjust++);card.BindBattlefield(()=>battlefield++);
            var model=new ModeResultContent{Mode=ResultScreenMode.Skirmish,Victory=true,Stars=3,ShowBattlefieldBack=true,ShowAdjust=true,ActionsEnabled=true,Identity="SKIRMISH\nMISSION",LeaveLabel="MAIN MENU",HasObjectiveFacts=true,CompletedObjectives=3,Objective1="One",Objective2="Two",Objective3="Three",Objective1State="ناموفق",Objective2State="ناموفق",Objective3State="تکمیل شد",Signature="win"};
            try
            {
                card.Present(in model);var c=card.SharedView.Controls;
                Check(c.Primary.gameObject.activeSelf&&!c.Retry.gameObject.activeSelf,"Victory has one primary action");
                c.Primary.onClick.Invoke();c.Secondary.onClick.Invoke();c.Tertiary.onClick.Invoke();
                Check(battlefield==1&&replay==1&&leave==1&&retry==0,"Victory actions route distinctly");
                Check(c.ObjectiveValues[0].color==c.ObjectiveValues[1].color&&c.ObjectiveValues[0].color!=c.ObjectiveValues[2].color,"Authoritative goal facts override translated words");
                model.Victory=false;model.Signature="loss";card.Present(in model);
                Check(!c.Primary.gameObject.activeSelf&&c.Retry.gameObject.activeSelf,"Defeat has one retry and no replay");
                c.Retry.onClick.Invoke();c.Secondary.onClick.Invoke();c.Tertiary.onClick.Invoke();
                Check(retry==1&&battlefield==2&&adjust==1&&replay==1,"Defeat navigation and adjust do not replay");
                model.ActionsEnabled=false;card.Present(in model);
                Check(!c.Retry.IsInteractable()&&!c.Secondary.IsInteractable()&&!c.Tertiary.IsInteractable(),"No navigation before settlement");
                model.ActionsEnabled=true;card.Present(in model);
                Check(c.Retry.IsInteractable()&&c.Secondary.IsInteractable(),"Settled actions restore without stale disabled style");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void CheckReplayRewardIcons()
        {
            var host=new GameObject("ReplayRewardFixture",typeof(RectTransform));((RectTransform)host.transform).sizeDelta=new Vector2(1920,1080);
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MissionResultV3PrefabBuilder.PrefabPath),host.transform,false);
            try
            {
                var view=root.GetComponent<MissionResultPopupView>();
                foreach(string locale in new[]{"en","fa-IR"})
                {
                    GameLocalization.SetLocale(locale,false);
                    var replay=new UiMissionResultPopupModel(1,"saga.ch01.m01.first_contact",UiMissionResultOutcome.Victory,"VICTORY","FIRST CONTACT • OLD MARKET","Hostile patrol neutralized. The Old Market corridor is secure.",3,"00:44","0","3/3","250 Credits","CONTINUE",true,false,firstContact:new UiMissionFirstContactResultDetails(true,true));
                    view.Apply(in replay);var c=view.SharedResult.Controls;
                    if(locale=="fa-IR")Check(!System.Text.RegularExpressions.Regex.IsMatch(SharedMissionResultView.Source(c.Reason),"[A-Za-z]"),"Actual Campaign outcome reason is localized before sentence extraction");
                    Check(!c.RewardIcons[0].gameObject.activeSelf&&c.RewardIcons[1].gameObject.activeSelf,"Credits-only replay cannot display an XP award icon "+locale);
                    var row=c.InfoScroll.content.Find("RewardRow0");
                    Check(row!=null&&SharedMissionResultView.Source(row.Find("Amount").GetComponent<TMP_Text>())=="+250","Credits-only replay keeps its actual amount in a separate aligned column "+locale);
                }
            }
            finally{GameLocalization.SetLocale("en",false);Object.DestroyImmediate(host);}
        }
        static void CheckLongTitle()
        {
            var host=new GameObject("LongTitleFixture",typeof(RectTransform));((RectTransform)host.transform).sizeDelta=new Vector2(2400,1080);
            try
            {
                var card=CampaignStyleResultCard.Mount(host.transform,null);
                var model=new ModeResultContent{Mode=ResultScreenMode.Skirmish,MissionId="S002",Victory=true,Stars=2,Identity="SKIRMISH\nDesert Base · Base Assault · Established",Summary="Enemy main base destroyed.",ActionsEnabled=true,LeaveLabel="MAIN MENU",Signature="actual-catalog-title"};
                card.Present(in model);var ui=card.SharedView.Controls;ui.SummaryTitle.ForceMeshUpdate();ui.Mission.ForceMeshUpdate();
                Check(!ui.Mission.isTextOverflowing,"Actual long catalog identity fits the stacked header");
                Check(!ui.SummaryTitle.isTextOverflowing,"Actual long catalog title fits the story heading");
                float titleBottom=-ui.SummaryTitle.rectTransform.anchoredPosition.y+ui.SummaryTitle.rectTransform.rect.height;
                Check(titleBottom+8<=-ui.SummaryViewport.anchoredPosition.y,"Long title and aftermath retain a separate gutter");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void CheckCampaignReuseAndSettlement()
        {
            var host=new GameObject("CampaignFixture",typeof(RectTransform));((RectTransform)host.transform).sizeDelta=new Vector2(1920,1080);
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MissionResultV3PrefabBuilder.PrefabPath),host.transform,false);var view=root.GetComponent<MissionResultPopupView>();
            try
            {
                var construction=new UiMissionResultPopupModel(1,"saga.ch01.m02.establish_base",UiMissionResultOutcome.Loss,"FAILED","ESTABLISH BASE • FORWARD POST","Build objective incomplete.",0,"03:00","0","1","NO REWARD","RETRY MISSION",true,true,construction:new UiMissionConstructionResultDetails(1,0,0));
                view.Apply(in construction);
                var model=new UiMissionResultPopupModel(2,"saga.ch01.m01.first_contact",UiMissionResultOutcome.Loss,"DEFEAT","FIRST CONTACT • OLD MARKET","Command squad lost.",0,"01:12","1","1/3","NO REWARD","RETRY SAVE",true,false,settlementFailed:true);
                view.Apply(in model);var c=view.SharedResult.Controls;
                Check(SharedMissionResultView.Source(c.ObjectiveLabels[0]).Contains("patrol"),"A previous mission cannot leak its objective labels");
                Check(c.ObjectiveValues[2].color==SharedMissionResultView.Green,"Civilian goal stays complete after defeat with zero losses");
                Check(SharedMissionResultView.Source(c.SaveStatus).Contains("not saved"),"Settlement failure is explicit");
                Check(c.Primary.gameObject.activeSelf&&!c.Retry.gameObject.activeSelf,"Retry-save keeps its existing primary callback");
                Check(!c.Secondary.gameObject.activeSelf&&!c.Tertiary.gameObject.activeSelf,"Unbound Campaign exits cannot skip settlement or debrief");
                var independent=new UiMissionResultPopupModel(3,"saga.ch01.m01.first_contact",UiMissionResultOutcome.Loss,"DEFEAT","FIRST CONTACT • OLD MARKET","Patrol incomplete.",0,"01:12","2","1/3","NO REWARD","RETRY",true,true,firstContact:new UiMissionFirstContactResultDetails(true,true));
                view.Apply(in independent);
                Check(c.ObjectiveValues[1].color==SharedMissionResultView.Green,"Other squad losses cannot mark a surviving command squad failed");
            }
            finally{Object.DestroyImmediate(host);}
        }
        static void Render(string mode,bool victory,string language,int width,int height)
        {
            var cameraObject=new GameObject("ResultCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=height*.5f;camera.aspect=width/(float)height;camera.transform.position=new Vector3(0,0,-100);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(3,15,24,255);
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);var previous=RenderTexture.active;camera.targetTexture=target;
            var canvasObject=new GameObject("ResultCanvas",typeof(RectTransform),typeof(Canvas));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;var canvasRect=(RectTransform)canvasObject.transform;canvasRect.sizeDelta=new Vector2(width,height);
            string name=mode+"-"+(victory?"victory":"defeat")+"-"+language+"-"+width;
            try
            {
                var root=ResultScreenDesignReviewCapture.CreateCase(canvasRect,mode,victory);var view=root.GetComponentInChildren<SharedMissionResultView>(true);view.RefreshLayout();Canvas.ForceUpdateCanvases();
                foreach(var t in view.GetComponentsInChildren<TMP_Text>(true))t.ForceMeshUpdate();
                view.RefreshLayout();Canvas.ForceUpdateCanvases();
                if(language=="fa-IR")
                {
                    Check(!System.Text.RegularExpressions.Regex.IsMatch(view.Controls.Reason.text,"[A-Za-z]"),name+" localized sample outcome reason");
                    Check(!System.Text.RegularExpressions.Regex.IsMatch(view.Controls.Summary.text,"[A-Za-z]"),name+" localized sample summary");
                }
                Check(view.Controls.OutcomeIcon.GetComponent<CanvasRenderer>()!=null,name+" native symbol renderer");
                Check(view.Controls.Artwork.texture!=null,name+" own mode artwork");
                Check(view.Controls.Backdrop.rectTransform.rect.width*view.transform.localScale.x>=width-.1f&&view.Controls.Backdrop.rectTransform.rect.height*view.transform.localScale.y>=height-.1f,name+" full parent backdrop hides underlying HUD");
                Check(view.Controls.Primary.gameObject.activeSelf!=view.Controls.Retry.gameObject.activeSelf,name+" mutually exclusive primary controls");
                var bounds=new List<string>();
                foreach(var text in view.GetComponentsInChildren<TMP_Text>())
                {
                    text.ForceMeshUpdate();if(!text.gameObject.activeInHierarchy)continue;
                    bool scroll=text==view.Controls.Summary||text==view.Controls.InfoBody;
                    if(!scroll&&(text.isTextOverflowing||text.isTextTruncated))bounds.Add(text.name+" "+text.text+" size="+text.rectTransform.rect+" preferred="+text.preferredWidth+","+text.preferredHeight);
                    Check(text.margin.z>=8,name+" right text inset "+text.name);
                }
                var banner=(RectTransform)view.transform.Find("Outcome");
                float Center(TMP_Text text,RectTransform panel)=>panel.InverseTransformPoint(text.transform.TransformPoint(text.textBounds.center)).y;
                float reasonDelta=Mathf.Abs(Center(view.Controls.Reason,banner)-banner.rect.center.y);
                float saveDelta=Mathf.Abs(Center(view.Controls.SaveStatus,view.Controls.SaveStrip)-view.Controls.SaveStrip.rect.center.y);
                if(reasonDelta>2)bounds.Add("Outcome reason vertical center delta="+reasonDelta);
                if(view.Controls.SaveIcon.gameObject.activeSelf&&saveDelta>2)bounds.Add("Saved-state vertical center delta="+saveDelta);
                Check(view.Controls.OutcomeDivider.gameObject.activeSelf&&view.Controls.OutcomeDivider.rectTransform.rect.width>=2,name+" outcome divider present");
                foreach(Transform child in view.Controls.InfoScroll.content)
                {
                    if(!child.name.StartsWith("RewardRow")||!child.gameObject.activeSelf)continue;
                    var label=child.Find("Label").GetComponent<TMP_Text>();var amount=child.Find("Amount").GetComponent<TMP_Text>();
                    float delta=Mathf.Abs(Center(label,(RectTransform)child)-Center(amount,(RectTransform)child));
                    if(delta>2)bounds.Add("Reward row label/amount center delta="+delta);
                    Check(label.color==SharedMissionResultView.White&&amount.color==SharedMissionResultView.Amber,name+" reward label/value hierarchy");
                }
                File.WriteAllLines(Output+"/"+name+"-layout.txt",bounds);
                File.WriteAllText(Output+"/"+name+"-alignment.txt","reasonCenterDelta="+reasonDelta+" savedCenterDelta="+saveDelta+" summaryViewport="+view.Controls.SummaryScroll.viewport.rect.height+" summaryContent="+view.Controls.SummaryScroll.content.rect.height);
                // Always preserve the render before failing the layout gate.
                camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());captures++;
                Check(bounds.Count==0,name+" text bounds: "+string.Join("; ",bounds));
                if(mode=="campaign"&&victory)Check(view.Controls.SummaryScroll.content.rect.height<=view.Controls.SummaryScroll.viewport.rect.height+.1f,name+" short aftermath is fully visible");
                var panelNames=new[]{"Identity","Duration","Outcome","Story","Objectives","Performance","Information"};
                var panels=panelNames.Select(n=>(RectTransform)view.transform.Find(n)).ToArray();
                for(int i=0;i<panels.Length;i++)for(int j=i+1;j<panels.Length;j++)
                {
                    Rect Bounds(RectTransform r)=>new Rect(r.anchoredPosition.x,r.anchoredPosition.y-r.sizeDelta.y,r.sizeDelta.x,r.sizeDelta.y);
                    Check(!Bounds(panels[i]).Overlaps(Bounds(panels[j])),name+" distinct panel borders "+panels[i].name+"/"+panels[j].name);
                }
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(canvasObject);Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(texture);target.Release();Object.DestroyImmediate(target);}
        }
        static void Check(bool pass,string message){checks++;if(!pass)throw new InvalidOperationException(message);}
    }
}
#endif
