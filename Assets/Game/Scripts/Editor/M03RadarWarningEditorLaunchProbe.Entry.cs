using System;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using SettingsService = Game.UI.Runtime.SettingsService;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string EntryKey="Warline.M03.Probe.EveryEntry";
        private static int entryStage,entryCount,entryFrame,entryOpeningSeen;
        private static UIAssistanceLevel entryOldAssistance;
        private static string entryOldLocale;
        private static CampaignMissionRuntimeComponent entryPrevious;
        private static bool EveryEntryActive=>SessionState.GetBool(EntryKey,false);

        public static void RunEveryEntryValidation()=>RunChecked(()=>
        {
            M03RadarWarningUiBuilder.RepairHud();
            var settings=SettingsService.Load();entryOldAssistance=settings.Assistant.AssistanceLevel;
            settings.Assistant.AssistanceLevel=UIAssistanceLevel.Off;SettingsService.Save(settings);
            entryOldLocale=GameLocalization.CurrentLocaleCode;GameLocalization.SetLocale("fa-IR",false);
            entryStage=entryCount=entryFrame=0;entryOpeningSeen=-1;SessionState.SetBool(EntryKey,true);
            MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);Run();
        });
        private static bool PrepareEveryEntryCampaign()
        {
            if(!EveryEntryActive)return false;
            if(!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.IsTransitionRunning)return true;
            if(shell.ActiveRoute==UIRoute.Campaign && UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>()!=null)return false;
            UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.Campaign,true);return true;
        }
        private static void PrepareEveryEntryStore(CampaignMissionProgressStore store)
        {
            if(EveryEntryActive)store.Settle(M03RadarWarningConfigBuilder.MissionId,"previous-clear",1,true,3,90000,"saga.ch01.m04.airlift");
        }
        private static void RestoreEveryEntrySettings()
        {
            if(!EveryEntryActive)return;
            var settings=SettingsService.Load();settings.Assistant.AssistanceLevel=entryOldAssistance;SettingsService.Save(settings);
            GameLocalization.SetLocale(entryOldLocale,false);SessionState.SetBool(EntryKey,false);
        }
        private static bool AdvanceEveryEntryValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!EveryEntryActive)return false;
            if(runtime.Phase==MissionPhaseKind.FindSquad && em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage==2 && entryOpeningSeen!=entryCount)
            {
                var hint=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude).SingleOrDefault(t=>t.name=="OpeningHint");
                if(hint==null)throw new InvalidOperationException("The camera tour leaves ARIA blank.");
                hint.ForceMeshUpdate();if(hint.isTextOverflowing || hint.isTextTruncated)throw new InvalidOperationException("Opening instruction is clipped.");
                ScreenCapture.CaptureScreenshot(Output+"/every-entry-tour-"+entryCount+".png");entryOpeningSeen=entryCount;
            }
            switch(entryStage)
            {
                case 0:
                    if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<1000)return true;
                    if(runtime.Guidance!=NarrativeGuidanceMode.Full || runtime.ReplayTutorialEnabled==0)
                        throw new InvalidOperationException("M3 entry did not require the full tutorial.");
                    if(entryCount==0 && runtime.RunKind!=MissionRunKind.Replay || entryCount==1 && runtime.RunKind!=MissionRunKind.Retry || entryCount==2 && runtime.RunKind==MissionRunKind.FirstClear)
                        throw new InvalidOperationException("The QA fixture did not exercise replay/retry.");
                    if(entryCount==1 && runtime.AttemptOrdinal!=entryPrevious.AttemptOrdinal+1)
                        throw new InvalidOperationException("Retry did not create a fresh attempt.");
                    if(!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep!=1 || panel.TutorialStepCount!=12 ||
                        string.IsNullOrWhiteSpace(panel.RecommendationBody))throw new InvalidOperationException("Missing first M3 tutorial recommendation.");
                    var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                    if(aria==null || !aria.IsPresentationVisible)return true;
                    foreach(var text in new[]{aria.TitleText,aria.BodyText,aria.ProgressText})
                    {
                        text.ForceMeshUpdate();
                        if(!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || text.isTextOverflowing || text.isTextTruncated)
                            throw new InvalidOperationException("ARIA tutorial text is blank, hidden or clipped: "+text.name);
                    }
                    if(!aria.DoItButton.isActiveAndEnabled || !aria.DoItButton.interactable || !aria.ShowMeButton.interactable)
                        throw new InvalidOperationException("Tutorial action buttons are unavailable.");
                    if(!UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) || restrictions.ShowMissionCredits || restrictions.HideLogisticsResources)
                        throw new InvalidOperationException("M3 still substitutes credits for oil/fuel.");
                    var hud=UnityEngine.Object.FindAnyObjectByType<MatchHudResourceIconView>();
                    if(hud==null || !hud.gameObject.activeInHierarchy)throw new InvalidOperationException("Fuel slot is hidden.");
                    var oil=hud.transform.parent.Find("OilSlot");
                    if(oil==null || !oil.gameObject.activeInHierarchy)throw new InvalidOperationException("Oil slot is hidden.");
                    using(var cameras=em.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent)))
                    {
                        if(!CampaignMissionPatrolOrderSystem.TryCaptureTourStart(cameras.GetSingleton<RuntimeCameraSnapshotComponent>(),0,out var focus,out var pose) || pose.x>100)
                            throw new InvalidOperationException("M3 command camera is too high.");
                        var tour=em.GetComponentData<CampaignMissionCameraTourState>(root);
                        if(math.distance(focus,tour.StartFocus)>.2f)throw new InvalidOperationException("Tour did not return to squad command view.");
                    }
                    using(var squadQuery=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(Unity.Transforms.LocalTransform)))
                    using(var squad=squadQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
                    {
                        int visible=0;
                        foreach(var unit in squad)
                        {
                            var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(unit);
                            if(role.MissionRoleId.ToString()!="role.friendly.command_squad" || !role.SessionToken.Equals(runtime.SessionToken))continue;
                            var point=Camera.main.WorldToViewportPoint(em.GetComponentData<Unity.Transforms.LocalTransform>(unit).Position);
                            if(point.z>0 && point.x>.15f && point.x<.8f && point.y>.2f && point.y<.85f)visible++;
                        }
                        if(visible<8)throw new InvalidOperationException("The opening camera hides command soldiers behind the HUD: visible="+visible);
                    }
                    ScreenCapture.CaptureScreenshot(Output+"/every-entry-"+entryCount+".png");entryFrame=Time.frameCount;entryStage++;return true;
                case 1:
                    if(Time.frameCount-entryFrame<4)return true;
                    UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>().DoItButton.onClick.Invoke();entryStage++;return true;
                case 2:
                    var warning=UnityEngine.Object.FindAnyObjectByType<ThreatAlertV3PopupView>();
                    if(warning==null || !warning.JumpToThreatButton.interactable)return true;
                    warning.JumpToThreatButton.onClick.Invoke();entryStage++;return true;
                case 3:
                    if(!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var next) || next.TutorialStep<2)return true;
                    Debug.Log($"[M03EveryEntry] entry={entryCount} kind={runtime.RunKind} tutorial=1/12 visible=true action=advanced locale={GameLocalization.CurrentLocaleCode} assistance=Off");
                    entryPrevious=runtime;
                    if(entryCount==0)
                    {
                        entryCount++;entryStage=0;capturedBrief=false;
                        if(!UiShellRuntimeGateway.TryRestartCurrentMission())throw new InvalidOperationException("Retry rejected.");
                        entryStage=4;return true;
                    }
                    if(entryCount==1){UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause);entryStage=5;return true;}
                    Complete(true,"Every-entry M3: completed-save replay, retry, campaign exit and replay with Assistance Off; actual visible 1/12 tutorial and Do It/Jump advance in Farsi and English; command camera <=100; oil/fuel header");return true;
                case 4:
                    if(runtime.AttemptOrdinal==entryPrevious.AttemptOrdinal)return true;
                    entryStage=0;return true;
                case 5:
                    var pause=UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>();
                    if(pause==null)return true;
                    pause.ExitButton.onClick.Invoke();entryStage++;return true;
                case 6:
                    if(!UiShellRuntimeGateway.TryReadShellState(out var shell) || shell.ActiveRoute!=UIRoute.Campaign || shell.IsTransitionRunning)return true;
                    if(UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>()==null)return true;
                    GameLocalization.SetLocale("en",false);entryCount++;entryStage=0;
                    deployed=capturedBrief=capturedHud=false;return true;
            }
            return true;
        }
    }
}
