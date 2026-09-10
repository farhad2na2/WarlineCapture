using System;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M04AirliftEditorProbe
    {
        private static bool recoveryActive;
        private static CampaignMissionExtractionMember[] previousAttemptMembers;
        private static int replayAttempt,settledCredits,settledXp;
        private static string resultWaitReason;
        private static MissionResultPopupView observedResult;
        private static int resultFirstFrame;
        private static void BeginRecovery(EntityManager em,Entity root)
        {
            recoveryActive=true;deployed=false;step=100;stepAt=EditorApplication.timeSinceStartup;
            em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
            var profile=save.LoadProfile();settledCredits=profile.credits;settledXp=profile.commanderXp;
            Debug.Log("[M04EditorProbe] First-clear journey passed; validating replay defeat and retry through normal commands");
        }
        private static void TickRecovery(EntityManager em,Entity root,CampaignMissionRuntimeComponent runtime,CampaignMissionAttemptFactsComponent facts)
        {
            if(EditorApplication.timeSinceStartup-stepAt>(step==108?210:90))throw new TimeoutException("M04 recovery step timed out: "+step+" phase="+runtime.Phase);
            if(Time.frameCount-frame<10)return;
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            if(step==100)
            {
                if(runtime.Phase!=MissionPhaseKind.Engage||extraction.Ready==0)return;
                if(runtime.RunKind!=MissionRunKind.Replay)throw new InvalidOperationException("Completed M4 did not launch as Replay");
                if(em.GetBuffer<CampaignMissionExtractionMember>(root).Length!=18)throw new InvalidOperationException("Replay roster incorrect");
                foreach(var member in previousAttemptMembers)if(em.Exists(member.Entity))throw new InvalidOperationException("First-clear actor leaked into replay");
                replayAttempt=runtime.AttemptOrdinal;
                Transport(em,new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.FocusUnit,TargetEntity=extraction.Carrier,HasTargetEntity=1});Next();return;
            }
            if(step==101)
            {
                using var focus=em.CreateEntityQuery(typeof(FocusedUnitUiReadModelComponent));
                if(focus.CalculateEntityCount()!=1||focus.GetSingleton<FocusedUnitUiReadModelComponent>().FocusedUnit!=extraction.Carrier)return;
                Transport(em,new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.DestroyFocusedUnit});Next();return;
            }
            if(step==102)
            {
                if(runtime.Outcome!=MissionOutcomeKind.Defeat)return;
                if(facts.ExtractionCarrierLost==0)throw new InvalidOperationException("Destroying the APC did not produce the carrier-loss failure");
                if(!UiShellRuntimeGateway.TryReadMissionResult(out var result)||!result.Extraction.CarrierLost||!result.RetryVisible)return;
                if(!ResultVisible())return;
                GameLocalization.SetLocale("en",false);Next();return;
            }
            if(step==103){ScreenCapture.CaptureScreenshot(Output+"/defeat-en-16x9.png");Next();return;}
            if(step==104){GameLocalization.SetLocale("fa-IR",false);Next();return;}
            if(step==105){ScreenCapture.CaptureScreenshot(Output+"/defeat-fa-16x9.png");Next();return;}
            if(step==106)
            {
                if(!CaptureWideResult("defeat"))return;
                var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                var button=typeof(MissionResultPopupView).GetField("retryButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view)as Button;
                if(button==null||!button.isActiveAndEnabled||!button.interactable)return;
                using(var members=em.GetBuffer<CampaignMissionExtractionMember>(root).ToNativeArray(Allocator.Temp))previousAttemptMembers=members.ToArray();
                button.onClick.Invoke();Next();return;
            }
            if(step==107)
            {
                if(runtime.Phase!=MissionPhaseKind.Engage||runtime.AttemptOrdinal<=replayAttempt||extraction.Ready==0)return;
                if(runtime.RunKind!=MissionRunKind.Retry||em.GetBuffer<CampaignMissionExtractionMember>(root).Length!=18||facts.ExtractionPassengerTotal!=4||facts.ExtractionPassengersAboard!=0||facts.ExtractionSecureMilliseconds!=0)
                    throw new InvalidOperationException("Retry did not reset the rescue state");
                foreach(var member in previousAttemptMembers)if(em.Exists(member.Entity))throw new InvalidOperationException("Replay actor leaked into retry");
                var profile=save.LoadProfile();if(profile.credits!=settledCredits||profile.commanderXp!=settledXp)throw new InvalidOperationException("Defeat/retry changed settled rewards");
                // Accelerate normal simulation only; issue no tactical orders on this retry.
                Time.timeScale=4;Next();return;
            }
            if(step==108)
            {
                RecordIdlePursuit(em,facts.ElapsedMilliseconds);
                if(runtime.Outcome!=MissionOutcomeKind.Defeat)return;
                Time.timeScale=1;
                if(facts.ExtractionTimedOut != 0) throw new InvalidOperationException("Idle rescue failed only at the deadline; pursuers never made contact.");
                var profile=save.LoadProfile();if(profile.credits!=settledCredits||profile.commanderXp!=settledXp)throw new InvalidOperationException("Idle defeat changed settled rewards");
                Debug.Log("[M04EditorProbe] Idle retry defeated at ms="+facts.ElapsedMilliseconds+" passengerLosses="+facts.CivilianLossCount+" escorts="+facts.SquadLossCount+" timeout="+facts.ExtractionTimedOut);
                Complete(true,"HUD en/fa 16:9 and 20:9; 12 guide topics and 57 classes in both languages; real APC/helicopter rescue; debrief; saved unlocks; visible bilingual result; campaign return; real Replay carrier-loss defeat and clean Retry; idle failure under normal combat at 4x simulation; no duplicate rewards");
            }
        }
        private static bool ResultVisible()
        {
            var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
            if(view==null||!view.isActiveAndEnabled)return ResultWait("Result view inactive");
            if(observedResult!=view){observedResult=view;resultFirstFrame=Time.frameCount;}
            if(Time.frameCount-resultFirstFrame<10)return false;
            Canvas.ForceUpdateCanvases();
            foreach(var group in view.GetComponentsInParent<CanvasGroup>())
            {
                if(group.alpha<.99f||!group.interactable||!group.blocksRaycasts)
                    return ResultWait(group.name+" alpha="+group.alpha+" interactable="+group.interactable+" raycasts="+group.blocksRaycasts);
                if(group.ignoreParentGroups)break;
            }
            var region=view.GetComponentInParent<UIShellRegionView>();
            if(region!=null&&(region.RegionRoot.localScale.x<.99f||region.RegionRoot.localScale.y<.99f))return ResultWait("Region scale="+region.RegionRoot.localScale);
            if(view.transform.localScale.x<.99f||view.transform.localScale.y<.99f)return ResultWait("Result scale="+view.transform.localScale);
            ValidateResultViewport(view);
            foreach(var text in view.GetComponentsInChildren<TMPro.TMP_Text>())
            {text.ForceMeshUpdate();if(text.isTextOverflowing||text.isTextTruncated)throw new InvalidOperationException("M04 result text overflow: "+text.transform.parent.name+"/"+text.name+" "+text.text);}
            return true;
        }
        private static bool ResultWait(string reason)
        {
            if(resultWaitReason!=reason)
            {resultWaitReason=reason;Debug.Log("[M04EditorProbe] result waiting: "+reason);ScreenCapture.CaptureScreenshot(Output+"/result-await.png");}
            return false;
        }
    }
}
