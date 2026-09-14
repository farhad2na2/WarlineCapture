using System;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private static int recoveryStage,completedAttempt;
        private static bool replay;
        private static double returnAt,returnLogAt;
        private static Entity[] oldActors;
        private static void BeginReturn(EntityManager em,Entity root,UiMissionResultPopupModel result)
        {
            var entry=store.ReadAll().Single(x=>x.missionId==M05BreachAssaultConfigBuilder.MissionId);
            if(!entry.firstClearRewardSettled || result.SettlementFailed)throw new InvalidOperationException("First-clear reward settlement missing");
            if(replay && result.FirstClear)throw new InvalidOperationException("Replay offered first-clear grants again");
            var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
            var button=typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view)as Button;
            if(button==null || !button.isActiveAndEnabled || !button.interactable)return;
            var roster=em.GetBuffer<CampaignMissionBreachMember>(root,true);oldActors=new Entity[roster.Length];
            for(int i=0;i<roster.Length;i++)oldActors[i]=roster[i].Entity;
            completedAttempt=em.GetComponentData<CampaignMissionRuntimeComponent>(root).AttemptOrdinal;
            button.onClick.Invoke();recoveryStage=1;returnAt=EditorApplication.timeSinceStartup;
        }
        private static void TickReturn(EntityManager em,Entity root,CampaignMissionRuntimeComponent runtime,CampaignMissionBreachState breach)
        {
            if(EditorApplication.timeSinceStartup-returnAt>180)throw new TimeoutException("Campaign return/replay did not complete");
            if(recoveryStage==1)
            {
                using var query=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));
                if(query.CalculateEntityCount()!=1)return;
                var shell=query.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>();
                if(EditorApplication.timeSinceStartup-returnLogAt>5){returnLogAt=EditorApplication.timeSinceStartup;Debug.Log($"[M05Return] stage={recoveryStage} route={shell.ActiveRoute} mode={shell.CurrentMode} transition={shell.IsTransitionRunning} phase={shell.Phase}");}
                if(shell.ActiveRoute==UIRoute.Match || shell.IsTransitionRunning!=0)return;
                if(shell.ActiveRoute!=UIRoute.Campaign)throw new InvalidOperationException("M5 returned to "+shell.ActiveRoute);
                var view=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(view==null || !view.isActiveAndEnabled)return;
                ScreenCapture.CaptureScreenshot(Output+"/campaign-return-"+GameLocalization.CurrentLocaleCode+".png");
                if(replay){Complete(true,"guided English and real Persian replay victory; 3-objective combat/hold; visible rewards; campaign return; fresh replay actors/tutorial; no objective or health edits");return;}
                MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);GameLocalization.SetLocale("fa-IR",false);SessionState.SetBool("Warline.M05.Guided",false);
                if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign))return;
                if(campaign.SelectedMission.MissionId!=M05BreachAssaultConfigBuilder.MissionId)
                {UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,M05BreachAssaultConfigBuilder.MissionId);return;}
                if(!UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,M05BreachAssaultConfigBuilder.MissionId))return;
                recoveryStage=2;returnAt=EditorApplication.timeSinceStartup;return;
            }
            if(runtime.Phase!=MissionPhaseKind.Engage || breach.Ready==0)return;
            if(runtime.RunKind!=MissionRunKind.Replay || runtime.AttemptOrdinal<=completedAttempt)throw new InvalidOperationException("M5 did not start a new replay attempt");
            foreach(var entity in oldActors)if(em.Exists(entity))throw new InvalidOperationException("M5 retained an actor from the previous attempt");
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);if(guidance.GuidanceId!=65001)return;
            if(breach.GateDestroyed!=0 || breach.CoreDestroyed!=0 || breach.SecureHoldMilliseconds!=0 || breach.GuidanceCompletedMask!=0)throw new InvalidOperationException("M5 replay retained old objectives");
            Debug.Log("[M05EditorProbe] replay starts with fresh roster, targets and lesson 1 in Persian");
            replay=true;recoveryStage=0;step=0;guideAuditStage=0;victoryAt=0;
        }
    }
}
