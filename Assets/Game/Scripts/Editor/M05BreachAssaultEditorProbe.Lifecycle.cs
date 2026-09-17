using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private static string recoverySample;
        public static void RunLifecycleAudit()
        {
            SessionState.SetBool("Warline.M05.LifecycleAudit",true);
            recoverySample=null;
            RunScreenTapAudit();
        }
        private static void SampleLifecycleUi(EntityManager em,Entity root)
        {
            foreach(var hud in UnityEngine.Object.FindObjectsByType<MissionDefenseHudView>(FindObjectsInactive.Exclude))
            {
                var button=(Button)typeof(MissionDefenseHudView).GetField("skipCameraTourButton",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
                if(button!=null && button.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Transition return button is visible");
            }
            if(!em.HasComponent<CampaignMissionBreachState>(root))return;
            var b=em.GetComponentData<CampaignMissionBreachState>(root);
            if(b.SecureHoldMilliseconds<2000 || b.SecureHoldMilliseconds>19000)return;
            int seconds=Math.Max(0,(b.SecureRequiredMilliseconds-b.SecureHoldMilliseconds+999)/1000);
            string sample=GameLocalization.CurrentLocaleCode+":"+seconds;
            if(sample==recoverySample)return;
            string expected=string.Format(GameText.Get("mission.m05.hud.recovery.progress"),seconds);
            if(!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || !panel.RecommendationBody.StartsWith(expected,StringComparison.Ordinal))
                throw new InvalidOperationException("ARIA recovery countdown is stale. Expected: "+expected+" Actual: "+panel.RecommendationBody);
            recoverySample=sample;
            if(seconds is 16 or 10)
                ScreenCapture.CaptureScreenshot(Output+"/archive-recovery-"+GameLocalization.CurrentLocaleCode+"-"+seconds+".png");
            Debug.Log("[M05RecoveryUi] result=Passed locale="+GameLocalization.CurrentLocaleCode+" seconds="+seconds+" body="+panel.RecommendationBody);
        }
    }
}
