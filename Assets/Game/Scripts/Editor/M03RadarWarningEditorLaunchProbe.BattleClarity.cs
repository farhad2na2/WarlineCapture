using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Composition;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static bool battleClarityAudit;
        private static string battleClarityLocale;
        private static readonly HashSet<string> battleClarityStates=new();
        private static double battleClarityCaptureAt;
        private static int battleClarityStep;
        private static bool battleClarityCooldownSeen, battleClarityScanRequested, battleClarityScanVerified;
        private static double battleClarityScanAt;
        private static string battleClarityBody;
        private static double battleClarityBodyAt;
        public static void RunBattleClarityEnglish()=>BeginBattleClarity("en");
        public static void RunBattleClarityPersian()=>BeginBattleClarity("fa-IR");
        public static void RunBattleClarityBuildingPersian()=>BeginBattleClarity("fa-IR",true);
        private static void BeginBattleClarity(string locale,bool build=false)
        {
            // Run the focused state transitions on the same assembly as the real journey.
            var tests=System.AppDomain.CurrentDomain.GetAssemblies();
            bool tested=false;
            foreach(var assembly in tests)
            {
                var type=assembly.GetType("M03BattleClarityTests"); if(type==null) continue;
                var instance=System.Activator.CreateInstance(type);
                foreach(var name in new[]{"BattleChangesFromSelectionThroughMoveAndHoldToExplicitWaiting","OnlyLiveConfirmedContactCanBecomeBattleTarget","RadarSelectionCannotReplaceRifleGuidanceAndAllNewCopyIsBilingual"})
                    type.GetMethod(name).Invoke(instance,null);
                tested=true; Debug.Log("[M03BattleClarity] focused state regressions passed on journey assembly");break;
            }
            if(!tested) throw new InvalidOperationException("Focused battle clarity tests are unavailable in this Editor.");
            if(build) RunBuildingJourneyPersian(); else RunFullGuidanceJourney(); battleClarityAudit=true; battleClarityLocale=locale;
            battleClarityStates.Clear(); battleClarityStep=0; battleClarityCaptureAt=0;battleClarityCooldownSeen=battleClarityScanRequested=battleClarityScanVerified=false;
            GameLocalization.SetLocale(locale,false);
        }
        private static void ObserveBattleClarity(EntityManager em,Entity root)
        {
            if(!battleClarityAudit) return;
            if(GameLocalization.CurrentLocaleCode!=battleClarityLocale) GameLocalization.SetLocale(battleClarityLocale,false);
            var p=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(p.GuidanceId==45004 && !completeBuildingJourney)
            {
                var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
                if(drawer!=null && drawer.IsOpen) ClickCommand(drawer.CloseButton);
            }
            if(p.GuidanceId<45010 || p.Active==0) return;
            if(!battleClarityScanRequested)
            {
                var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                if(controls!=null && controls.ScanButton.IsInteractable())
                {ClickTutorialButton(controls.ScanButton);battleClarityScanRequested=true;battleClarityScanAt=EditorApplication.timeSinceStartup;return;}
            }
            if(battleClarityScanRequested && !battleClarityScanVerified && EditorApplication.timeSinceStartup-battleClarityScanAt>1)
            {
                var scan=em.GetComponentData<RadarPingState>(root);
                if(scan.Result!=RadarPingResultKind.Accepted || scan.Charges!=1 || scan.PendingRequestId!=0)
                    throw new InvalidOperationException("Optional Scan did not produce an accepted result.");
                VerifyGuidanceOrders(em,true);
                if(!UiShellRuntimeGateway.TryReadMissionDefense(out var scanModel) || string.IsNullOrEmpty(scanModel.ScanFeedback))
                    throw new InvalidOperationException("Optional Scan has no readable result.");
                var feedback=UnityEngine.Object.FindAnyObjectByType<BattleHudRuntimeFeedbackView>();
                var label=(TMPro.TMP_Text)typeof(BattleHudRuntimeFeedbackView).GetField("feedbackText",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(feedback);
                if(!label.gameObject.activeInHierarchy || label.text!=V3LocalizedTextBindingView.ShapeForRendering(scanModel.ScanFeedback))
                    throw new InvalidOperationException("Scan result is not visibly explained: "+label.text);
                ScreenCapture.CaptureScreenshot(Output+"/optional-scan-"+battleClarityLocale+".png");
                Debug.Log("[M03Clarity] optional Scan feedback visible, Hold preserved, battle continues: "+scanModel.ScanFeedback);
                battleClarityScanVerified=true;
            }
            if(!p.Body.ToString().StartsWith("mission.m03.clarity.",StringComparison.Ordinal))
                throw new InvalidOperationException("Battle lost its state-specific next instruction.");
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target) || target.BattleAction==UiTutorialBattleAction.None)
                throw new InvalidOperationException("Battle lesson has no actionable/defended world target.");
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(view==null || !view.IsPresentationVisible) return;
            if(view.ContinueButton.IsActive()) throw new InvalidOperationException("Battle requires a dead Continue click.");
            var resolved=GameLocalization.Get(p.Body.ToString());
            if(string.IsNullOrWhiteSpace(resolved) || resolved==p.Body.ToString()) throw new InvalidOperationException("Battle copy not localized.");
            if(battleClarityStep!=p.GuidanceId)
            {battleClarityStep=p.GuidanceId;battleClarityCaptureAt=EditorApplication.timeSinceStartup+2;}
            if(battleClarityBody!=resolved) {battleClarityBody=resolved;battleClarityBodyAt=EditorApplication.timeSinceStartup;}
            if(EditorApplication.timeSinceStartup<battleClarityCaptureAt || EditorApplication.timeSinceStartup-battleClarityBodyAt<1) return;
            if(view.CurrentInstructionBody!=resolved) throw new InvalidOperationException("Visible ARIA instruction is stale: "+view.CurrentInstructionBody);
            var body=(TMPro.TMP_Text)typeof(AriaTutorialBriefingView).GetField("bodyText",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(view);
            if(body.preferredHeight>body.rectTransform.rect.height+3) throw new InvalidOperationException("ARIA battle instruction is clipped.");
            if(view.ShowMeButton.IsActive())
            {ClickCommand(view.ShowMeButton);battleClarityCaptureAt=EditorApplication.timeSinceStartup+1;return;}
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var point=target.NeedsSelection ? target.Selection : target.Destination;
            AssertVisibleTutorialWorld(match.MatchBootstrap.WorldCamera,point);
            if(UiShellRuntimeGateway.TryReadMissionDefense(out var defense) && defense.CooldownSeconds>0)
            {
                var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                if(defense.CanPing) throw new InvalidOperationException("Cooling radar accepts another sweep.");
                battleClarityCooldownSeen=true;
            }
            if(battleClarityStates.Add(p.Body.ToString()))
            {
                ScreenCapture.CaptureScreenshot(Output+"/battle-"+battleClarityLocale+"-"+p.Body.ToString().Replace("mission.m03.clarity.","")+".png");
                Debug.Log("[M03BattleClarity] locale="+battleClarityLocale+" step="+p.GuidanceId+" state="+p.Body+" visibleTarget="+point+" instruction="+resolved);
            }
        }
        private static void VerifyBattleClarity()
        {
            if(!battleClarityAudit) return;
            if(!battleClarityScanVerified || !battleClarityCooldownSeen || !battleClarityStates.Contains("mission.m03.clarity.wait") || !battleClarityStates.Contains("mission.m03.clarity.engage"))
                throw new InvalidOperationException("Battle QA did not observe both readable waiting and enemy-contact states.");
            Debug.Log("[M03BattleClarity] result=Passed locale="+battleClarityLocale+" waiting,contact,visible targets,localized instructions,real defensive victory");
            battleClarityAudit=false;
        }
    }
}
