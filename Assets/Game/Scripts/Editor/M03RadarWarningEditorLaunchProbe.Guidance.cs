using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using SettingsService = Game.UI.Runtime.SettingsService;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string GuidanceJourneyKey="Warline.M03.Probe.GuidanceJourney";
        private static readonly HashSet<int> guidanceVisited=new();
        private static int guidancePrompt,guidanceSubstep;
        private static double guidanceNext;
        private static int guidanceDiagnosticAt;
        private static bool guidanceHeld,guidanceVerifyHold,guidanceRestoreHold,guidanceJourneyVerified;
        private static UIAssistanceLevel guidanceOriginalMode;
        private static string guidanceOriginalLocale;
        public static void RunCompletionGuidanceAndMotion()
        {
            SessionState.SetString("Warline.M03.ReadinessOutput", "/private/tmp/warline-m03-final-guidance");
            MissionMotionEditorAudit.Begin();
            SessionState.SetBool("Warline.M03.ReadinessReturn", true);
            RunFullGuidanceLargeComicValidation();
        }
        public static void RunFullGuidanceJourney()=>RunChecked(()=>
        {
            var settings=SettingsService.Load(); guidanceOriginalMode=settings.Assistant.AssistanceLevel;
            settings.Assistant.AssistanceLevel=UIAssistanceLevel.FullGuidance; SettingsService.Save(settings);
            guidanceOriginalLocale=GameLocalization.CurrentLocaleCode; GameLocalization.SetLocale("en",false);
            guidanceVisited.Clear(); guidancePrompt=guidanceSubstep=0; guidanceNext=0; guidanceDiagnosticAt=0;
            guidanceHeld=guidanceVerifyHold=guidanceRestoreHold=guidanceJourneyVerified=false;
            SessionState.SetBool(GuidanceJourneyKey,true); StartResultValidation(false);
            // This journey uses the real tutorial and defensive Hold. The separate rifle
            // driver is disabled so it cannot move or aim on the learner's behalf.
            SessionState.SetBool(RifleKey,false);
        });
        private static bool AdvanceGuidanceJourney(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(GuidanceJourneyKey,false)) return false;
            if(runtime.Phase==MissionPhaseKind.ResultAfterDebrief && !guidanceJourneyVerified)
            {
                if(!guidanceHeld || runtime.Outcome!=MissionOutcomeKind.Victory || em.GetComponentData<RadarPingState>(root).Charges!=2)
                    throw new InvalidOperationException("Full Guidance did not complete through real defensive orders without optional Ping.");
                AssertBudget(em,50000,100); guidanceJourneyVerified=true;
                Debug.Log("[M03GuidanceJourney] result=Passed real Full Guidance buttons, optional choices declined, defensive Hold victory, full budget and two Ping charges retained; visited="+string.Join(",",guidanceVisited.OrderBy(x=>x)));
                return false;
            }
            if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<1500) return false;
            if(runtime.Guidance!=NarrativeGuidanceMode.Full) throw new InvalidOperationException("Full Guidance preference was not used by the real campaign launch.");
            if(facts.ElapsedMilliseconds>=guidanceDiagnosticAt)
            {
                guidanceDiagnosticAt=facts.ElapsedMilliseconds+10000;
                var system=em.World.Unmanaged.GetExistingUnmanagedSystem<UnitEngagementSystem>();
                Debug.Log("[M03GuidanceCombat] acquisitionEnabled="+em.World.Unmanaged.ResolveSystemStateRef(system).Enabled);
                foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                {
                    var unit=member.Entity;
                    if(member.FactionId!=1 || member.IsSensor!=0 || !em.Exists(unit)) continue;
                    Debug.Log($"[M03GuidanceCombat] time={facts.ElapsedMilliseconds} unit={unit} hold={em.HasComponent<HoldPositionOrderTag>(unit)} auto={em.GetComponentData<UnitCombat>(unit).AutoEngage} target={em.HasComponent<EngageTarget>(unit)} path={em.HasComponent<UnitPathFollow>(unit)}/{em.HasComponent<UnitPathRequest>(unit)} blocker={em.HasComponent<StaticGridBlocker>(unit)} range={em.GetComponentData<UnitAttack>(unit).Range} suppressed={em.HasComponent<CampaignMissionCombatSuppressedTag>(unit)}");
                }
            }
            if(EditorApplication.timeSinceStartup<guidanceNext) return false;
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>(); if(match==null) return false;
            var commands=match.MatchBootstrap.SelectionUiCommand;
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if(guidanceVerifyHold)
            {
                VerifyGuidanceOrders(em, true); guidanceHeld=true; guidanceVerifyHold=false;
            }
            if(guidanceRestoreHold)
            {
                VerifyGuidanceOrders(em, false);
                ClickCommand(controls.HoldButton); guidanceVerifyHold=true; guidanceRestoreHold=false; guidanceNext=EditorApplication.timeSinceStartup+.3;
                return false;
            }
            // Opening a real drawer intentionally hides ARIA. Finish the owned
            // drawer interaction before waiting for the next visible tutorial card.
            if(guidancePrompt is 4 or 9 && guidanceSubstep==1)
            {
                var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                if(drawer==null || !drawer.IsOpen)
                    throw new InvalidOperationException("SHOW ME did not open the optional build/production drawer.");
                AssertBudget(em,50000,100);
                if(match.MatchBootstrap.BuildingUiCommandContract.HasPendingBuildingPlacement)
                    throw new InvalidOperationException("SHOW ME started a paid placement without player choice.");
                ClickCommand(drawer.CloseButton); guidanceSubstep=2;
                guidanceNext=EditorApplication.timeSinceStartup+.5; return false;
            }
            var warning=UnityEngine.Object.FindAnyObjectByType<ThreatAlertV3PopupView>();
            if(warning!=null && warning.JumpToThreatButton!=null && warning.JumpToThreatButton.isActiveAndEnabled)
            {ClickCommand(warning.JumpToThreatButton); guidanceVisited.Add(2); guidanceNext=EditorApplication.timeSinceStartup+.5; return false;}
            var projection=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(projection.Active==0) return false;
            int step=(int)projection.Prompt-12;
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(view==null || !view.IsPresentationVisible || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep!=step ||
                (byte)typeof(AriaTutorialBriefingView).GetField("_tutorialStep",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view)!=step) return false;
            if(!view.ShowMeButton.isActiveAndEnabled || !view.DoItButton.isActiveAndEnabled) return false;
            if(step!=guidancePrompt) {guidancePrompt=step; guidanceSubstep=0; guidanceVisited.Add(step); Debug.Log($"[M03GuidanceJourney] step={step} at={facts.ElapsedMilliseconds} canExecute={panel.CanExecute}");}
            guidanceNext=EditorApplication.timeSinceStartup+.35;
            switch(step)
            {
                case 1:
                    ClickCommand(view.ShowMeButton); break;
                case 2:
                    ClickCommand(view.DoItButton); break;
                case 3:
                    if(guidanceSubstep==0 && UiShellRuntimeGateway.TryReadMissionDefense(out var focus) && focus.CanReturnCamera)
                    {ClickLive("ReturnWarningCamera"); guidanceSubstep=1; guidanceNext=EditorApplication.timeSinceStartup+2; break;}
                    using(var camera=em.CreateEntityQuery(typeof(RtsCameraStateComponent)))
                        if(camera.GetSingleton<RtsCameraStateComponent>().HasSmoothFocusTarget!=0 || camera.GetSingleton<RtsCameraStateComponent>().HasSmoothPerspectiveTarget!=0) return false;
                    ClickCommand(view.DoItButton); break;
                case 10: case 11:
                    ClickCommand(view.DoItButton); break;
                case 4: case 9:
                    if(guidanceSubstep==0) {ClickCommand(view.ShowMeButton); guidanceSubstep=1; break;}
                    ClickLive("SkipLesson"); break;
                case 5: case 6: case 7:
                    if(guidanceSubstep==0)
                    {if(!commands.RequestSelectAllSoldiers()) throw new InvalidOperationException("Normal rifle selection failed."); guidanceSubstep=1; break;}
                    if(guidanceSubstep==1)
                    {
                        if(!panel.CanExecute)
                        {
                            using var selection=em.CreateEntityQuery(typeof(SelectedUnitTag));
                            throw new InvalidOperationException("ARIA command remained unavailable after visible-unit selection: selected="+selection.CalculateEntityCount());
                        }
                        ClickCommand(view.DoItButton); guidanceSubstep=2;
                        if(step==6) guidanceVerifyHold=true;
                        if(step==7) guidanceRestoreHold=true;
                        break;
                    }
                    if(step==5 && guidanceSubstep==2)
                    {
                        var input=(RtsSelectionInputCompositionSystemHelper)typeof(SelectionUiCommandUiSystemHelper).GetField("_inputSystem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(commands);
                        var point=(Vector3)projection.WorldPosition; var cell=new int2(876,426);
                        if(!input.TryGetActiveCommandMode(out var mode) || mode!=Game.Tactical.Contracts.TacticalCommandMode.Move ||
                            !input.QueueMoveCommandRequest(match.MatchBootstrap.WorldCamera.WorldToScreenPoint(point),cell,point,Time.frameCount))
                            throw new InvalidOperationException("ARIA Move did not retain normal player target input.");
                        guidanceSubstep=3;
                    }
                    break;
                case 8:
                    ClickLive("SkipLesson"); break;
            }
            return false;
        }
        private static void VerifyGuidanceOrders(EntityManager em, bool hold)
        {
            using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitHealth),typeof(Faction));
            using var units=selected.ToEntityArray(Allocator.Temp);
            if(units.Length!=8) throw new InvalidOperationException("ARIA command lost the eight-rifle selection.");
            foreach(var unit in units)
                if(em.HasComponent<HoldPositionOrderTag>(unit)!=hold || em.HasComponent<UnitPathRequest>(unit) ||
                    em.HasComponent<UnitPathFollow>(unit) || em.GetComponentData<UnitCombat>(unit).AutoEngage!=(hold ? 1 : 0))
                    throw new InvalidOperationException("ARIA "+(hold ? "Hold" : "Stop")+" failed on a real selected rifle.");
            Debug.Log("[M03GuidanceJourney] actual "+(hold ? "Hold" : "Stop")+" verified on all eight rifles");
        }
        private static void StopGuidanceJourney()
        {
            if(!SessionState.GetBool(GuidanceJourneyKey,false)) return;
            var settings=SettingsService.Load(); settings.Assistant.AssistanceLevel=guidanceOriginalMode; SettingsService.Save(settings);
            GameLocalization.SetLocale(guidanceOriginalLocale,false); SessionState.SetBool(GuidanceJourneyKey,false);
        }
    }
}
