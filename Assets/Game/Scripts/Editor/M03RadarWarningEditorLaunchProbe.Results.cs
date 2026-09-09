using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string ResultKey="Warline.M03.Probe.Results";
        private const string DefeatResultKey="Warline.M03.Probe.DefeatResults";
        private static double finaleStarted,dialogueClick;
        private static int finaleClock,resultStep,resultGuideStep;
        private static readonly HashSet<string> resultPanels=new();
        private static bool resultEnglishRequested;
        private static bool defeatRetreatRequested,defeatRetreatObserved;
        private static CampaignMissionRuntimeComponent defeatedAttempt;
        public static void RunResultValidation()=>RunChecked(()=>StartResultValidation(false));
        public static void RunDefeatResultValidation()=>RunChecked(()=>StartResultValidation(true));
        private static void StartResultValidation(bool defeat)
        {
            M03RadarWarningNarrativeBuilder.BuildCaptionedArtAndInstall(); M03RadarWarningUiBuilder.Build();
            finaleStarted=dialogueClick=0; resultStep=resultGuideStep=0; resultPanels.Clear(); pendingCapture=null;
            resultEnglishRequested=false;
            defeatRetreatRequested=defeatRetreatObserved=false;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            SessionState.SetBool(ResultKey,true); SessionState.SetBool(DefeatResultKey,defeat);
            if(defeat) {M03RadarWarningConfigBuilder.Build(); RunConvoy();} else RunRifleDefense();
        }
        private static bool AdvanceResultValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(ResultKey,false)) return false;
            bool defeat=SessionState.GetBool(DefeatResultKey,false);
            if(defeat && resultStep==0 && runtime.Phase==MissionPhaseKind.Engage)
                PrepareDefeatRetreat(em,root,in facts);
            if(defeat && resultStep==6)
            {
                if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<1000) return true;
                var ping=em.GetComponentData<RadarPingState>(root);
                if(!runtime.SessionToken.Equals(defeatedAttempt.SessionToken) || runtime.AttemptOrdinal!=defeatedAttempt.AttemptOrdinal+1 ||
                    runtime.Outcome!=MissionOutcomeKind.None || facts.HostileDefeatedCount!=0 || facts.CoreBreached!=0 || facts.HostileRosterIntegrityFault!=0 ||
                    em.GetBuffer<CampaignMissionDefenseMember>(root).Length!=20 || ping.Charges!=2 || ping.PendingRequestId!=0)
                    throw new InvalidOperationException("Retry retained stale attempt state.");
                ValidateStartingBudget(em);
                Complete(true,"real rifle-retreat core-breach defeat -> localized failure result -> guide return -> actual Retry; same session, isolated next attempt, 20 members, fresh 2 Ping uses and full budget"); return true;
            }
            if(runtime.Phase<MissionPhaseKind.SecureCorridor) return false;
            if(defeat && (runtime.Outcome!=MissionOutcomeKind.Defeat || !defeatRetreatObserved))
                throw new InvalidOperationException($"Defeat journey did not establish a retreat loss: outcome={runtime.Outcome} retreated={defeatRetreatObserved} defeated={facts.HostileDefeatedCount}/7 core={facts.CoreBreached}.");
            if(!resultEnglishRequested) {GameLocalization.SetLocale("en",false); resultEnglishRequested=true;}
            if(!defeat && runtime.Outcome==MissionOutcomeKind.Defeat) throw new InvalidOperationException("Result journey strategy unexpectedly lost.");
            if(finaleStarted==0) {finaleStarted=EditorApplication.timeSinceStartup; finaleClock=facts.ElapsedMilliseconds; Time.timeScale=1;}
            if(facts.ElapsedMilliseconds!=finaleClock) throw new InvalidOperationException("Finale/result consumed mission time.");
            if(runtime.Phase==MissionPhaseKind.SecureCorridor)
            {
                if(EditorApplication.timeSinceStartup-finaleStarted>5) throw new InvalidOperationException("Finale failed to finish within its 3-second presentation budget.");
                if(EditorApplication.timeSinceStartup-finaleStarted>1) CaptureUiBeforeAction("finale-post");
                return true;
            }
            if(!defeat && EditorApplication.timeSinceStartup-finaleStarted<2.5) throw new InvalidOperationException("Finale ended before its visible hold.");
            var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
            if(narrative!=null && Visible(narrative,"rootGroup"))
            {
                if(defeat) throw new InvalidOperationException("Defeat incorrectly played a victory debrief.");
                if(UiShellRuntimeGateway.TryReadMissionResult(out _)) throw new InvalidOperationException("Victory became available before the debrief finished.");
                string panel=narrative.CurrentPanelSprite?.name;
                if(panel==null || !panel.StartsWith("M03-D",StringComparison.Ordinal)) throw new InvalidOperationException("Debrief used a foreign/provisional panel.");
                var button=typeof(NarrativeDialogueView).GetField("inputButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(narrative.DialogueView) as Button;
                if(button==null || !button.interactable) throw new InvalidOperationException("Debrief continue is unavailable.");
                if(narrative.DialogueView.Phase==NarrativeDialoguePhase.Revealing)
                {button.onClick.Invoke(); dialogueClick=EditorApplication.timeSinceStartup; return true;}
                if(!CaptureUiBeforeAction("debrief-"+panel)) return true;
                resultPanels.Add(panel);
                if(EditorApplication.timeSinceStartup-dialogueClick>.8)
                {
                    button.onClick.Invoke(); dialogueClick=EditorApplication.timeSinceStartup;
                }
                return true;
            }
            if(!UiShellRuntimeGateway.TryReadMissionResult(out var model)) return true;
            if(resultPanels.Count!=(defeat ? 0 : 3) || !model.Defense.Applicable || model.Outcome!=(defeat ? UiMissionResultOutcome.Loss : UiMissionResultOutcome.Victory) || model.DebriefRequired ||
                !model.Subtitle.Contains(GameText.Get("mission.m03.name")) || model.Defense.CivilianLosses!=facts.CivilianLossCount)
                throw new InvalidOperationException("Result identity, facts or comic-before-Victory ordering is incorrect.");
            var guide=UnityEngine.Object.FindAnyObjectByType<MissionFieldGuideView>();
            string capture=defeat ? "defeat-" : "result-";
            switch(resultStep)
            {
                case 0:
                    if(!CaptureUiBeforeAction(capture+"en-16x9")) return true;
                    ValidateResultText();
                    GameLocalization.SetLocale("fa-IR",false); MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080); resultStep++; break;
                case 1:
                    if(!CaptureUiBeforeAction(capture+"fa-20x9")) return true;
                    ValidateResultText(); MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080); resultStep++; break;
                case 2:
                    if(!CaptureUiBeforeAction(capture+"fa-16x9")) return true;
                    ValidateResultText(); GameLocalization.SetLocale("en",false); MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080); resultStep++; break;
                case 3:
                    if(!CaptureUiBeforeAction(capture+"en-20x9")) return true;
                    ValidateResultText(); ClickLive("M03ResultGuide"); resultStep++; break;
                case 4:
                    if(guide==null) return true;
                    if(resultGuideStep==0)
                    {
                        if(!UiShellRuntimeGateway.TryReadMissionRadioArchive()) throw new InvalidOperationException("Delivered radio clue missing from result archive.");
                        guide.ShowRadio(); resultGuideStep++; return true;
                    }
                    if(resultGuideStep==1)
                    {
                        if(!CaptureUiBeforeAction("radio-en-20x9")) return true;
                        if(UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>()!=null)
                            throw new InvalidOperationException("Result was recreated over the guide.");
                        ValidateGuideText(guide); GameLocalization.SetLocale("fa-IR",false);
                        guide.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0; resultGuideStep++; return true;
                    }
                    string radioCapture=resultGuideStep<3 ? "radio-fa-20x9" : resultGuideStep==3 ? "radio-fa-16x9" : "radio-en-16x9";
                    if(!CaptureUiBeforeAction(radioCapture)) return true;
                    ValidateGuideText(guide);
                    if(SessionState.GetBool(ComicKey,false) && resultGuideStep<4)
                    {
                        MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
                        if(resultGuideStep==3) GameLocalization.SetLocale("en",false);
                        resultGuideStep++; return true;
                    }
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide); resultStep++; break;
                case 5:
                    if(guide!=null || UiShellRuntimeGateway.IsMissionFieldGuidePresenting()) return true;
                    if(!CaptureUiBeforeAction(capture+"guide-return")) return true;
                    if(UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>()==null) throw new InvalidOperationException("Guide lost the result return context.");
                    GameLocalization.SetLocale("en",false);
                    if(defeat)
                    {
                        if(!model.Defense.CoreBreached || model.Stars!=0)
                            throw new InvalidOperationException("Defeat reason or zero-star truth incorrect.");
                        defeatedAttempt=runtime;
                        if(!UiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.Retry) || UiShellRuntimeGateway.TryEnqueueMissionResultAction(UiMissionResultActionKind.Retry))
                            throw new InvalidOperationException("Actual Retry or duplicate rejection failed.");
                        resultStep++; break;
                    }
                    if(SessionState.GetBool(ComicKey,false) && comicCaptures.Count!=24)
                        throw new InvalidOperationException("Comic QA did not capture all six opening/debrief panels in both locales and aspects.");
                    if(SessionState.GetBool(ComicKey,false) && ReadComicPresentation()?.ResidentPanelAssetCount!=0)
                        throw new InvalidOperationException("Narrative panel handles survived the result/guide round trip.");
                    Complete(true,$"real rifle victory -> 3-second finale (clock frozen) -> 3 final M3 debrief panels -> localized Victory; result guide returns; stars={model.Stars}; civiliansLost={model.Defense.CivilianLosses}"); break;
            }
            return true;
        }
        private static void PrepareDefeatRetreat(EntityManager em,Entity root,in CampaignMissionAttemptFactsComponent facts)
        {
            if(facts.ElapsedMilliseconds<1000 || defeatRetreatObserved) return;
            using var members=em.GetBuffer<CampaignMissionDefenseMember>(root,true).ToNativeArray(Allocator.Temp);
            int rifles=0,retreated=0;
            foreach(var member in members)
            {
                if(member.FactionId!=1 || member.IsSensor!=0 || !em.HasComponent<UnitAttack>(member.Entity)) continue;
                if(!defeatRetreatRequested)
                    UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(em,member.Entity,new int2(1088+(rifles%2)*3,446+(rifles/2)*3));
                if(Alive(em,member.Entity) && em.GetComponentData<LocalTransform>(member.Entity).Position.x>=1040) retreated++;
                rifles++;
            }
            if(rifles!=8) throw new InvalidOperationException("Defeat retreat requires all eight real rifles.");
            if(!defeatRetreatRequested)
                Debug.Log("[M03DefeatProbe] retreat requested through normal Move orders for eight rifles; no health, transform, path or outcome writes");
            defeatRetreatRequested=true;
            if(retreated==8)
            {
                defeatRetreatObserved=true;
                Debug.Log($"[M03DefeatProbe] retreat observed rifles=8 elapsed={facts.ElapsedMilliseconds}");
            }
            else if(facts.ElapsedMilliseconds>65000)
                throw new InvalidOperationException($"Defeat retreat failed to move all rifles out of the defense: {retreated}/8.");
        }
        private static void ValidateResultText()
        {
            var result=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
            if(result==null) throw new InvalidOperationException("Result view did not appear.");
            foreach(var text in result.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                if(string.IsNullOrWhiteSpace(text.text)) continue;
                text.ForceMeshUpdate();
                if(text.isTextOverflowing || text.isTextTruncated || text.textInfo.characterInfo.Take(text.textInfo.characterCount)
                    .Any(c=>c.isVisible && (c.character=='\u25a1' || c.character=='\ufffd' || c.textElement==null)))
                    throw new InvalidOperationException($"Result text clipped/missing glyph: {text.name} locale={GameLocalization.CurrentLocaleCode} height={text.rectTransform.rect.height} preferred={text.preferredHeight}");
            }
        }
    }
}
