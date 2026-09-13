using System;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private static int guideAuditStage;
        public static void RunGuideReadability()
        {
            SessionState.SetBool("Warline.M05.GuideOnly",true);SessionState.SetBool("Warline.M05.RetryProbe",false);SessionState.SetBool("Warline.M05.Guided",true);Run();
        }
        private static double guideAuditAt;
        private static bool TickGuideAudit()
        {
            if(guideAuditStage==5)
            {
                if(!SessionState.GetBool("Warline.M05.GuideOnly",false))return false;
                if(UiShellRuntimeGateway.IsMissionFieldGuidePresenting())return true;
                if(GameLocalization.CurrentLocaleCode=="en") {MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);GameLocalization.SetLocale("fa-IR",false);guideAuditStage=0;return true;}
                SessionState.SetBool("Warline.M05.GuideOnly",false);Complete(true,"English and Persian eight-lesson / 57-class guide open, render and close at 16:9 and 20:9");return true;
            }
            if(guideAuditStage==0)
            {
                if(!UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide))return true;
                guideAuditStage=1;guideAuditAt=EditorApplication.timeSinceStartup;return true;
            }
            if(EditorApplication.timeSinceStartup-guideAuditAt<2)return true;
            var view=UnityEngine.Object.FindAnyObjectByType<MissionFieldGuideView>();
            if(guideAuditStage<3 && view==null)throw new InvalidOperationException("M5 field guide failed to open");
            if(guideAuditStage==1)
            {
                if(view.VisibleClassCount!=57)throw new InvalidOperationException("M5 class guide inventory incomplete");
                ScreenCapture.CaptureScreenshot(Output+"/guide-lessons-"+GameLocalization.CurrentLocaleCode+".png");
                guideAuditStage=2;
            }
            else if(guideAuditStage==2)
            {
                view.ShowClasses();guideAuditStage=3;
            }
            else if(guideAuditStage==3)
            {
                ScreenCapture.CaptureScreenshot(Output+"/guide-classes-"+GameLocalization.CurrentLocaleCode+".png");guideAuditStage=4;
            }
            else {UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide);guideAuditStage=5;}
            guideAuditAt=EditorApplication.timeSinceStartup;return true;
        }
    }
}
