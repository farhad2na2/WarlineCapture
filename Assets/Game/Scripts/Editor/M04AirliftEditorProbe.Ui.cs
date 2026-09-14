using System;
using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private static int guidePage,guideLanguage,pausedClock;
        private static bool captureQueued;
        private static void ValidateAriaRows()
        {
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(aria==null || !aria.BriefingLayout.gameObject.activeInHierarchy)
                throw new InvalidOperationException("M4 must launch with visible ARIA instructions.");
            aria.RefreshContentLayout();Canvas.ForceUpdateCanvases();
            var utility=(RectTransform)aria.transform.Find("M04Navigation");
            if(utility==null || !utility.gameObject.activeInHierarchy)
                throw new InvalidOperationException("M4 navigation must be present in the live layout check.");
            var actions=(RectTransform)aria.DoItButton.transform.parent;
            var viewport=(RectTransform)aria.BodyText.transform.parent;
            var bodyCorners=new Vector3[4];var actionCorners=new Vector3[4];var utilityCorners=new Vector3[4];var railCorners=new Vector3[4];
            viewport.GetWorldCorners(bodyCorners);actions.GetWorldCorners(actionCorners);
            utility.GetWorldCorners(utilityCorners);((RectTransform)aria.transform).GetWorldCorners(railCorners);
            if(actionCorners[1].y>=bodyCorners[0].y || utilityCorners[1].y>=actionCorners[0].y || utilityCorners[0].y<=railCorners[0].y)
                throw new InvalidOperationException("M4 ARIA instructions, actions and utilities overlap or overflow the rail.");
            foreach(var text in aria.GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<TMP_Text>())
                if(text.transform.parent.name=="TopBorderCaption")
                {
                    text.ForceMeshUpdate();
                    if(text.isTextOverflowing || text.isTextTruncated)
                        throw new InvalidOperationException("M4 next-click caption does not fit: "+text.text+" rect="+text.rectTransform.rect+" preferred="+text.preferredWidth+","+text.preferredHeight+" font="+text.fontSize);
                }
            Debug.Log("[M04AriaRows] result=Passed locale="+GameLocalization.CurrentLocaleCode+" screen="+Screen.width+"x"+Screen.height);
        }
        private static void TickLaunchUi(CampaignMissionAttemptFactsComponent facts)
        {
            if(step==0){guidePage=guideLanguage=0;captureQueued=false;GameLocalization.SetLocale("en",false);Next();return;}
            if(Time.frameCount-frame<8)return;
            if(step==1){ValidateAriaRows();ScreenCapture.CaptureScreenshot(Output+"/hud-en-16x9.png");Next();return;}
            if(step==2){GameLocalization.SetLocale("fa-IR",false);Next();return;}
            if(step==3){ValidateAriaRows();ScreenCapture.CaptureScreenshot(Output+"/hud-fa-16x9.png");Next();return;}
            if(step==4){MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);Next();return;}
            if(step==5){ValidateAriaRows();ScreenCapture.CaptureScreenshot(Output+"/hud-fa-20x9.png");step=50;frame=Time.frameCount;return;}
            if(step==50){GameLocalization.SetLocale("en",false);step=51;frame=Time.frameCount;return;}
            if(step==51){ValidateAriaRows();ScreenCapture.CaptureScreenshot(Output+"/hud-en-20x9.png");step=6;frame=Time.frameCount;return;}
            if(step==6)
            {
                if(!UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide))throw new InvalidOperationException("M04 guide did not accept Open");
                Next();return;
            }
            if(step==11)
            {
                if(UiShellRuntimeGateway.IsMissionFieldGuidePresenting()||facts.ElapsedMilliseconds<pausedClock+100)return;
                if(UnityEditor.SessionState.GetBool(Both,false))
                {uiPassed=true;step=0;frame=Time.frameCount;stepAt=UnityEditor.EditorApplication.timeSinceStartup;MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);Debug.Log("[M04EditorProbe] UI phase passed; beginning real rescue");return;}
                Complete(true,"normal camera tour; HUD en/fa 16:9 and 20:9; guide 12 topics and all 57 classes in both languages; at most one resident class; guide pauses simulation and closing resumes it");return;
            }
            var view=UnityEngine.Object.FindAnyObjectByType<MissionFieldGuideView>();if(view==null||!view.isActiveAndEnabled)return;
            if(view.Guide==null||view.Guide.Name!="M04_Airlift_FieldGuide"||view.Guide.Topics.Length!=12||view.Guide.Classes.Length!=57)
                throw new InvalidOperationException("M04 guide loaded the wrong mission or inventory");
            if(step==7)
            {
                pausedClock=facts.ElapsedMilliseconds;GameLocalization.SetLocale("en",false);view.ShowTopics();Next();return;
            }
            if(facts.ElapsedMilliseconds!=pausedClock)throw new InvalidOperationException("Mission clock continued behind the guide");
            if(UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusTeam))throw new InvalidOperationException("Paused mission accepted an extraction camera action");
            if(view.ResidentClassCount>1||view.ClassLoadFailed)throw new InvalidOperationException("Field guide class loading failed or exceeded one resident asset");
            if(view.ClassLoadPending)return;
            foreach(var text in view.GetComponentsInChildren<TMP_Text>())
            {
                if(!text.isActiveAndEnabled)continue;text.ForceMeshUpdate();
                if(text.isTextOverflowing||text.isTextTruncated)throw new InvalidOperationException("M04 guide text overflow: "+text.name+" page="+guidePage);
            }
            foreach(var binding in view.GetComponentsInChildren<V3LocalizedTextBindingView>())
            {
                string source=typeof(V3LocalizedTextBindingView).GetField("runtimeSource",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(binding)as string??"";
                if(source.Contains("in M3")||source.Contains("مأموریت ۳"))
                    throw new InvalidOperationException("M04 guide retained M3-specific class copy");
            }
            string locale=guideLanguage==0?"en":"fa";
            if(step==8)
            {
                if((guidePage==0||guidePage==11)&&!captureQueued){ScreenCapture.CaptureScreenshot(Output+"/guide-topic-"+guidePage+"-"+locale+".png");captureQueued=true;frame=Time.frameCount;return;}
                captureQueued=false;
                if(++guidePage<12){view.Next();frame=Time.frameCount;return;}
                guidePage=0;view.ShowClasses();Next();return;
            }
            if(step==9)
            {
                if(view.VisibleClassCount!=57)throw new InvalidOperationException("The all-classes filter omitted entries");
                string id=view.Guide.Classes[guidePage].Id;
                if((id is "Unit_Veh_APC_Fast" or "Unit_Veh_Helicopter_Transport")&&!captureQueued)
                {ScreenCapture.CaptureScreenshot(Output+"/guide-"+id+"-"+locale+".png");captureQueued=true;frame=Time.frameCount;return;}
                captureQueued=false;
                if(++guidePage<57){view.Next();frame=Time.frameCount;return;}
                if(guideLanguage==0)
                {guideLanguage=1;guidePage=0;GameLocalization.SetLocale("fa-IR",false);view.ShowTopics();step=8;frame=Time.frameCount;return;}
                Next();return;
            }
            if(step==10)
            {
                if(!UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide))throw new InvalidOperationException("M04 guide did not accept Close");
                Next();
            }
        }
    }
}
