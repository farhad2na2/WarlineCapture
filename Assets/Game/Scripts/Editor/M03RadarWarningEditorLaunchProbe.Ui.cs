using System;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SettingsService=Game.UI.Runtime.SettingsService;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string UiKey="Warline.M03.Probe.Ui";
        private const string UiLargeKey="Warline.M03.Probe.UiLarge",UiOldLargeKey="Warline.M03.Probe.UiOldLarge";
        private static int uiStep,uiFrame,uiClass,uiTopic,uiElapsed,uiSelectionCount,uiWarningStep;
        private static double uiNext,uiDeadline;
        private static string pendingCapture;
        private static int captureFrame;
        private static double captureAt;
        private static string originalLocale;
        private static float3 focusSnapshot;
        private static float3 uiPreviousFocus;
        private static float4 uiPreviousPerspective;
        private static int uiCameraStep;
        private static (Entity Entity,float3 Position,int Health)[] frozenMembers;
        public static void RunUiValidation()=>RunChecked(()=>StartUiValidation(false));
        public static void RunUiLargeValidation()=>RunChecked(()=>StartUiValidation(true));
        public static void RunDeliveredUiValidation()=>RunChecked(()=>StartUiValidation(false,false));
        public static void RepairAndRunUiValidation()=>RunChecked(()=>
        {M03RadarWarningUiBuilder.RepairHud();StartUiValidation(false,false);});
        private static void StartUiValidation(bool large,bool rebuild=true)
        {
            if(rebuild) {M03RadarWarningConfigBuilder.Build(); M03RadarWarningUiBuilder.Build();}
            var settings=SettingsService.Load(); SessionState.SetBool(UiOldLargeKey,settings.Accessibility.LargeText);
            SessionState.SetBool(UiLargeKey,large); settings.Accessibility.LargeText=large; SettingsService.Save(settings);
            SessionState.SetBool(UiKey,true); uiStep=uiClass=uiTopic=uiFrame=uiWarningStep=0; uiNext=0; uiDeadline=0; pendingCapture=null;
            uiCameraStep=0;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            Run();
        }
        private static bool AdvanceUiValidation(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!SessionState.GetBool(UiKey,false)) return false;
            if(runtime.Phase!=MissionPhaseKind.Engage || facts.ElapsedMilliseconds<5000 || Time.frameCount-uiFrame<3 || EditorApplication.timeSinceStartup<uiNext) return true;
            if(uiDeadline>0 && EditorApplication.timeSinceStartup>uiDeadline) throw new InvalidOperationException("UI step timed out: "+uiStep);
            var guide=UnityEngine.Object.FindAnyObjectByType<MissionFieldGuideView>();
            switch(uiStep)
            {
                case 0:
                    ValidateStartingBudget(em); originalLocale=GameLocalization.CurrentLocaleCode; GameLocalization.SetLocale("en",false); NextUi(); break;
                case 1:
                    var map=UnityEngine.Object.FindAnyObjectByType<MatchHudMinimapView>();
                    if(map==null || map.MapImage==null || map.MapImage.sprite==null || !map.MapImage.sprite.name.StartsWith("Runtime_MatchHudMinimap",StringComparison.Ordinal))
                        throw new InvalidOperationException("The installed minimap must render its live projection, not template imagery.");
                    if(!CaptureUiBeforeAction("hud-en-16x9")) return true;
                    if(!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStepCount!=12)
                        throw new InvalidOperationException("M3 must project its own 12-step ARIA panel.");
                    using(var selection=em.CreateEntityQuery(typeof(SelectedUnitTag))) uiSelectionCount=selection.CalculateEntityCount();
                    ReadUiCameraPose(em,out uiPreviousFocus,out uiPreviousPerspective);
                    var records=em.GetBuffer<ThreatWarningRecord>(root,true); focusSnapshot=records[0].FocusPosition;
                    ClickLive("ReadWarning"); NextUi(); break;
                case 2:
                    if(uiWarningStep==1)
                    {
                        if(guide==null || Time.timeScale!=0) return true;
                        UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide); uiWarningStep++; return true;
                    }
                    var warning=UnityEngine.Object.FindAnyObjectByType<ThreatAlertV3PopupView>(); if(warning==null) return true;
                    if(!warning.JumpToThreatButton.interactable || !CaptureUiBeforeAction("warning-en-16x9")) return true;
                    if(uiWarningStep==0) {ClickLive("WarningFieldGuide"); uiWarningStep++; return true;}
                    warning.JumpToThreatButton.onClick.Invoke(); NextUi(); break;
                case 3:
                    if(uiCameraStep==0)
                    {
                        using(var camera=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent)))
                        {if(camera.CalculateEntityCount()!=1 || math.distance(camera.GetSingleton<RuntimeCameraFocusRequestComponent>().World,focusSnapshot)>.01f) throw new InvalidOperationException("Jump did not use the report position.");}
                        if(!UiShellRuntimeGateway.TryReadMissionDefense(out var focused) || !focused.CanReturnCamera)
                            throw new InvalidOperationException("Jump did not retain the preceding tactical camera context.");
                        if(!CaptureUiBeforeAction("warning-return-view")) return true;
                        ClickLive("ReturnWarningCamera"); uiCameraStep=1; uiNext=EditorApplication.timeSinceStartup+2; return true;
                    }
                    using(var camera=em.CreateEntityQuery(typeof(RtsCameraStateComponent)))
                        if(camera.GetSingleton<RtsCameraStateComponent>().HasSmoothFocusTarget!=0 || camera.GetSingleton<RtsCameraStateComponent>().HasSmoothPerspectiveTarget!=0) return true;
                    ReadUiCameraPose(em,out var restoredFocus,out var restoredPerspective);
                    if(math.distance(restoredFocus,uiPreviousFocus)>.15f || math.distance(restoredPerspective,uiPreviousPerspective)>.15f)
                        throw new InvalidOperationException("Return View did not restore the previous tactical camera pose.");
                    using(var selection=em.CreateEntityQuery(typeof(SelectedUnitTag))) if(selection.CalculateEntityCount()!=uiSelectionCount) throw new InvalidOperationException("Jump changed selection.");
                    Debug.Log("[M03UiProbe] warning focus -> actual Return View button -> preceding camera pose restored; selection preserved");
                    ClickLive("FieldGuide"); NextUi(); break;
                case 4:
                    if(guide==null || Time.timeScale!=0) return true;
                    uiElapsed=facts.ElapsedMilliseconds;
                    using(var members=em.GetBuffer<CampaignMissionDefenseMember>(root,true).ToNativeArray(Allocator.Temp))
                        frozenMembers=members.Select(m=>(m.Entity,em.GetComponentData<LocalTransform>(m.Entity).Position,em.GetComponentData<UnitHealth>(m.Entity).Current)).ToArray();
                    NextUi(2); break;
                case 5:
                    if(facts.ElapsedMilliseconds!=uiElapsed) throw new InvalidOperationException("Reading the guide consumed mission time.");
                    foreach(var member in frozenMembers) if(math.distance(member.Position,em.GetComponentData<LocalTransform>(member.Entity).Position)>.001f || member.Health!=em.GetComponentData<UnitHealth>(member.Entity).Current)
                        throw new InvalidOperationException("A unit moved or took damage during guide pause.");
                    if(!CaptureUiBeforeAction("guide-en-16x9")) return true;
                    ValidateGuideText(guide);
                    if(++uiTopic<12) {guide.Next(); uiFrame=Time.frameCount; return true;}
                    guide.ShowClasses(); NextUi(); break;
                case 6:
                    if(guide==null || guide.ClassLoadPending) return true;
                    ValidateGuideText(guide);
                    if(guide.VisibleClassCount!=57 || guide.ClassLoadFailed || guide.ResidentClassCount>1) throw new InvalidOperationException("Guide class coverage/loading/residency failure.");
                    if((uiClass==0 || uiClass==33 || uiClass==51) && !CaptureUiBeforeAction("class-en-"+uiClass)) return true;
                    if(++uiClass<57) {guide.Next(); uiFrame=Time.frameCount; uiDeadline=EditorApplication.timeSinceStartup+20; return true;}
                    ValidateGuideSearchAndFilters(guide);
                    guide.ShowTopics(); uiTopic=0; GameLocalization.SetLocale("fa-IR",false); NextUi(); break;
                case 7:
                    ValidateGuideText(guide); ValidateGuideNumbers(guide);
                    if(!CaptureUiBeforeAction("guide-fa-16x9")) return true;
                    if(++uiTopic<12) {guide.Next(); uiFrame=Time.frameCount; return true;}
                    guide.ShowTopics(); MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080); NextUi(); break;
                case 8: if(!CaptureUiBeforeAction("guide-fa-20x9")) return true; guide.ShowClasses(); uiClass=0; NextUi(); break;
                case 9:
                    if(guide.ClassLoadPending) return true;
                    ValidateGuideText(guide);
                    if(guide.ClassLoadFailed || guide.ResidentClassCount>1) throw new InvalidOperationException("Persian guide class loading failed.");
                    if((uiClass==0 || uiClass==33 || uiClass==51) && !CaptureUiBeforeAction("class-fa-"+uiClass)) return true;
                    if(++uiClass<57) {guide.Next(); uiFrame=Time.frameCount; uiDeadline=EditorApplication.timeSinceStartup+20; return true;}
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide); NextUi(); break;
                case 10:
                    if(guide!=null || Time.timeScale==0) return true;
                    if(!CaptureUiBeforeAction("hud-fa-20x9")) return true;
                    UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause); NextUi(); break;
                case 11:
                    var pause=UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>(); if(pause==null || Time.timeScale!=0) return true;
                    pause.HelpButton.onClick.Invoke(); NextUi(); break;
                case 12:
                    if(guide==null) return true;
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide); NextUi(); break;
                case 13:
                    if(UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>()==null || Time.timeScale!=0) return true;
                    GameLocalization.SetLocale("en",false);
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide); NextUi(); break;
                case 14:
                    if(guide==null || !CaptureUiBeforeAction("guide-en-20x9")) return true;
                    ValidateGuideText(guide);
                    guide.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0; NextUi(); break;
                case 15:
                    if(!CaptureUiBeforeAction("guide-en-20x9-bottom")) return true;
                    UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide); NextUi(); break;
                case 16:
                    if(guide!=null || UnityEngine.Object.FindAnyObjectByType<PauseOptionsV3PopupView>()==null || Time.timeScale!=0) return true;
                    if(originalLocale!=null) GameLocalization.SetLocale(originalLocale,false);
                    Complete(true,"Editor UI: real warning/guide/return/Jump; selection preserved; guide freezes clock, position and health; 12 topics + 57 classes x 2 locales; Persian digits ordered; search and filters; 1 resident class; return to Pause; both locales at 16:9 and 20:9; scroll bottom; largeText="+SessionState.GetBool(UiLargeKey,false)); break;
            }
            return true;
        }
        private static void ReadUiCameraPose(EntityManager em,out float3 focus,out float4 perspective)
        {
            using var snapshots=em.CreateEntityQuery(typeof(RuntimeCameraSnapshotComponent));
            if(snapshots.CalculateEntityCount()!=1 || !Game.Runtime.CampaignMissionPatrolOrderSystem.TryCaptureTourStart(
                snapshots.GetSingleton<RuntimeCameraSnapshotComponent>(),0,out focus,out perspective))
                throw new InvalidOperationException("A valid tactical camera snapshot is required.");
        }
        private static void NextUi(double delay=0) {uiStep++; uiFrame=Time.frameCount; uiNext=EditorApplication.timeSinceStartup+delay; uiDeadline=uiNext+25; Debug.Log("[M03UiProbe] step="+uiStep);}
        private static bool CaptureUiBeforeAction(string name)
        {
            if(SessionState.GetBool(UiKey,false) && SessionState.GetBool(UiLargeKey,false)) name+="-large";
            if(pendingCapture!=name) {pendingCapture=name; captureAt=EditorApplication.timeSinceStartup+.6; captureFrame=-1; return false;}
            if(EditorApplication.timeSinceStartup<captureAt) return false;
            if(captureFrame<0) {captureFrame=Time.frameCount; ScreenCapture.CaptureScreenshot(Output+"/"+name+".png"); return false;}
            return Time.frameCount-captureFrame>=2;
        }
        private static void RestoreUiSettings()
        {
            if(!SessionState.GetBool(UiKey,false)) return;
            var settings=SettingsService.Load(); settings.Accessibility.LargeText=SessionState.GetBool(UiOldLargeKey,false); SettingsService.Save(settings);
        }
        private static void ValidateGuideSearchAndFilters(MissionFieldGuideView guide)
        {
            var input=guide.GetComponentInChildren<TMPro.TMP_InputField>();
            input.text="no-such-class-m03-qa";
            if(guide.VisibleClassCount!=0 || guide.ResidentClassCount!=0) throw new InvalidOperationException("Empty class search retains a stale card/handle.");
            input.text="";
            var filter=guide.GetComponentsInChildren<Button>().Single(b=>b.name=="Filter");
            for(int category=0;category<5;category++)
            {
                filter.onClick.Invoke();
                int expected=0; foreach(var card in guide.Guide.Classes) if((int)card.Availability==category) expected++;
                if(guide.VisibleClassCount!=expected) throw new InvalidOperationException("Class availability filter disagrees with canonical guide.");
            }
            filter.onClick.Invoke();
            if(guide.VisibleClassCount!=57) throw new InvalidOperationException("All-class filter did not restore 57 identities.");
        }
        private static void ValidateGuideText(MissionFieldGuideView guide)
        {
            foreach(var text in guide.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                if(string.IsNullOrWhiteSpace(text.text) || text.name=="Input") continue;
                text.ForceMeshUpdate();
                if(text.isTextOverflowing || text.isTextTruncated || !text.textInfo.characterInfo.Take(text.textInfo.characterCount).Any(c=>c.isVisible))
                    throw new InvalidOperationException($"Guide text clipped: {text.transform.parent.name}/{text.name} locale={GameLocalization.CurrentLocaleCode} size={text.fontSize} height={text.rectTransform.rect.height} preferred={text.preferredHeight}");
            }
        }
        private static void ValidateGuideNumbers(MissionFieldGuideView guide)
        {
            foreach(var item in new[]{("Topics","۱۲"),("Classes","۵۷")})
            {
                var text=guide.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.transform.parent.name==item.Item1);
                text.ForceMeshUpdate();
                string digits=new(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>char.IsDigit(c.character)).OrderBy(c=>c.origin).Select(c=>c.character).ToArray());
                if(digits!=item.Item2) throw new InvalidOperationException("Persian guide count rendered in the wrong order: "+item.Item1+"="+digits);
            }
        }
        private static void ClickLive(string name)
        {
            var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).SingleOrDefault(b=>b.name==name);
            if(button==null || !button.interactable) throw new InvalidOperationException("Live UI control unavailable: "+name);
            button.onClick.Invoke();
        }
    }
}
