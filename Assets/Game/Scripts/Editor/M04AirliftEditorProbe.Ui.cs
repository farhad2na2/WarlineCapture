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
        private static void TickLaunchUi(CampaignMissionAttemptFactsComponent facts)
        {
            if(step==0){guidePage=guideLanguage=0;captureQueued=false;GameLocalization.SetLocale("en",false);Next();return;}
            if(Time.frameCount-frame<8)return;
            if(step==1){ScreenCapture.CaptureScreenshot(Output+"/hud-en-16x9.png");Next();return;}
            if(step==2){GameLocalization.SetLocale("fa-IR",false);Next();return;}
            if(step==3){ScreenCapture.CaptureScreenshot(Output+"/hud-fa-16x9.png");Next();return;}
            if(step==4){MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);Next();return;}
            if(step==5){ScreenCapture.CaptureScreenshot(Output+"/hud-fa-20x9.png");step=50;frame=Time.frameCount;return;}
            if(step==50){GameLocalization.SetLocale("en",false);step=51;frame=Time.frameCount;return;}
            if(step==51){ScreenCapture.CaptureScreenshot(Output+"/hud-en-20x9.png");step=6;frame=Time.frameCount;return;}
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
            if(view.Guide==null||view.Guide.name!="M04_Airlift_FieldGuide"||view.Guide.Topics.Length!=12||view.Guide.Classes.Length!=57)
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
            foreach(var binding in view.GetComponentsInChildren<V3LocalizedTextBinding>())
            {
                string source=typeof(V3LocalizedTextBinding).GetField("runtimeSource",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(binding)as string??"";
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
