using System;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private static int retryAttempt;
        private static bool retryClicked;
        public static void RunTimeoutRetry()
        {
            SessionState.SetBool("Warline.M05.Guided",false);
            SessionState.SetBool("Warline.M05.RetryProbe",true);
            retryClicked=false;retryAttempt=0;Run();
        }
        private static void TickRetry(EntityManager em,Entity root,CampaignMissionRuntimeComponent runtime,
            CampaignMissionAttemptFactsComponent facts,CampaignMissionBreachState breach)
        {
            if(retryClicked)
            {
                Time.timeScale=1;
                if(runtime.Phase!=MissionPhaseKind.Engage || breach.Ready==0)return;
                if(runtime.AttemptOrdinal<=retryAttempt || runtime.RunKind!=MissionRunKind.Retry)
                    throw new InvalidOperationException("Retry did not create a new attempt");
                var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                if(guidance.GuidanceId!=65001)return;
                if(breach.GateDestroyed!=0 || breach.CoreDestroyed!=0 || breach.GuidanceCompletedMask!=0 || facts.ElapsedMilliseconds!=0)
                    throw new InvalidOperationException("Retry retained objective or tutorial state");
                ScreenCapture.CaptureScreenshot(Output+"/retry-fresh-fa.png");
                SessionState.SetBool("Warline.M05.RetryProbe",false);
                Complete(true,"Persian real deadline defeat and visible Retry button create fresh targets, clock and lesson 1; time accelerated, no health/outcome edits");return;
            }
            if(runtime.Outcome==MissionOutcomeKind.Defeat)
            {
                Time.timeScale=1;
                if(facts.BreachTimedOut==0)throw new InvalidOperationException("Unexpected failure before deadline");
                var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                if(view==null)return;
                var button=typeof(MissionResultPopupView).GetField("retryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(view)as Button;
                if(button==null || !button.isActiveAndEnabled || !button.interactable)return;
                if(victoryAt==0){victoryAt=EditorApplication.timeSinceStartup;ScreenCapture.CaptureScreenshot(Output+"/timeout-fa.png");return;}
                if(EditorApplication.timeSinceStartup-victoryAt<2)return;
                retryAttempt=runtime.AttemptOrdinal;button.onClick.Invoke();retryClicked=true;return;
            }
            if(runtime.Phase==MissionPhaseKind.Engage && breach.Ready!=0)
            {
                UiShellRuntimeGateway.TryContinueBreachPlan();Time.timeScale=12;
            }
        }
    }
}
