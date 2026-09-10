using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string SaveRecoveryKey="Warline.M03.Probe.SaveRecovery";
        private static string probeSavePath,saveRecoveryFaultPath,saveRecoveryOriginalProfile;
        private static int saveRecoveryStep,saveRecoveryCredits,saveRecoveryXp;
        private static bool saveRecoveryCaptured,saveRecoveryVerified;
        public static void RunSaveRecoveryValidation()=>RunChecked(()=>
        {
            saveRecoveryStep=0; saveRecoveryFaultPath=null; saveRecoveryCaptured=saveRecoveryVerified=false;
            SessionState.SetBool(SaveRecoveryKey,true); StartResultValidation(false);
        });
        private static bool AdvanceSaveRecoveryValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(SaveRecoveryKey,false)) return false;
            if(saveRecoveryFaultPath==null && runtime.Phase==MissionPhaseKind.Engage && facts.ElapsedMilliseconds>=5000)
            {
                if(string.IsNullOrEmpty(probeSavePath)) throw new InvalidOperationException("Isolated probe save path missing.");
                saveRecoveryOriginalProfile=File.ReadAllText(Path.Combine(probeSavePath,SaveService.ProfileFileName));
                var before=new SaveService(new JsonSaveRepository(probeSavePath)).LoadProfile();
                saveRecoveryCredits=before.credits; saveRecoveryXp=before.commanderXp;
                // Only this probe's new isolated temp directory is affected. A directory at
                // the atomic-write temp filename creates a real, reversible write failure.
                saveRecoveryFaultPath=Path.Combine(probeSavePath,SaveService.ProfileFileName+".tmp");
                Directory.CreateDirectory(saveRecoveryFaultPath);
                Debug.Log("[M03SaveRecoveryProbe] isolated atomic-write fault installed");
            }
            if(runtime.Phase==MissionPhaseKind.Result && runtime.Outcome==MissionOutcomeKind.Victory && saveRecoveryStep<4)
            {
                if(!UiShellRuntimeGateway.TryReadMissionResult(out var model)) return true;
                var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>(); if(view==null) return true;
                if(!model.SettlementFailed || !model.PrimaryActionEnabled || !model.DebriefRequired || model.RetryVisible)
                    throw new InvalidOperationException("Save failure must show an enabled save retry, with continuation withheld.");
                if(File.ReadAllText(Path.Combine(probeSavePath,SaveService.ProfileFileName))!=saveRecoveryOriginalProfile)
                    throw new InvalidOperationException("Failed atomic save changed persisted progress/rewards.");
                if(!saveRecoveryCaptured)
                {
                    GameLocalization.SetLocale(saveRecoveryStep%2==0 ? "en" : "fa-IR",false);
                    MainMenuV3PrefabBuilder.SetGameViewResolution(saveRecoveryStep<2 ? 1920 : 2400,1080);
                    saveRecoveryCaptured=true; return true;
                }
                if(!CaptureUiBeforeAction("save-failure-"+saveRecoveryStep)) return true;
                ValidateResultText();
                if(++saveRecoveryStep<4) {saveRecoveryCaptured=false; return true;}
                Directory.Delete(saveRecoveryFaultPath,false);
                var button=typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(view) as Button;
                ClickCommand(button);
                if(UiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.RetrySave))
                    throw new InvalidOperationException("A rapid duplicate save retry was accepted.");
                GameLocalization.SetLocale("en",false); MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
                Debug.Log("[M03SaveRecoveryProbe] real Retry Save button queued once; duplicate rejected");
                return true;
            }
            if(saveRecoveryStep==4 && !saveRecoveryVerified && runtime.Phase==MissionPhaseKind.ResultAfterDebrief)
            {
                var profile=new SaveService(new JsonSaveRepository(probeSavePath)).LoadProfile();
                if(profile.credits!=saveRecoveryCredits+2000 || profile.commanderXp!=saveRecoveryXp+400 ||
                    UiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.RetrySave))
                    throw new InvalidOperationException("Save recovery did not persist exactly one reward grant or accepted a settled retry.");
                saveRecoveryVerified=true;
                Debug.Log("[M03SaveRecoveryProbe] result=Passed genuine write failure -> visible bilingual/aspect error -> actual retry -> one durable grant -> debrief -> normal Victory");
            }
            return false;
        }
        private static void StopSaveRecoveryValidation()
        {
            if(!SessionState.GetBool(SaveRecoveryKey,false)) return;
            if(saveRecoveryFaultPath!=null && Directory.Exists(saveRecoveryFaultPath)) Directory.Delete(saveRecoveryFaultPath,false);
            SessionState.SetBool(SaveRecoveryKey,false);
        }
    }
}
