using Game.Composition;
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
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private const string TutorialBuildKey="Warline.M03.Probe.TutorialBuild";
        private static int tutorialBuildClick;
        private static double tutorialBuildNext;
        private static bool tutorialBuildCaptured, tutorialPlotStaged;
        private static bool completeBuildingJourney;
        private static string tutorialBuildLocale="fa-IR";
        private static int tutorialMaterialsSpent, tutorialCreditsSpent;
        public static void RebuildAndRunBuildingJourneyEnglish() {M03RadarWarningConfigBuilder.Build(); RunBuildingJourneyEnglish();}
        public static void RunBuildingJourneyEnglish() { RunTutorialBuildClicks(); completeBuildingJourney=true; SessionState.SetBool("Warline.M03.Probe.RoadGateTutorial",true); tutorialBuildLocale="en"; Game.Configs.GameLocalization.SetLocale("en",false); }
        public static void RunBuildingJourneyPersian() { RunTutorialBuildClicks(); completeBuildingJourney=true; SessionState.SetBool("Warline.M03.Probe.RoadGateTutorial",true); }
        public static void RunTutorialBuildClicks()
        {
            completeBuildingJourney=false; tutorialBuildLocale="fa-IR"; tutorialMaterialsSpent=tutorialCreditsSpent=0;
            RunFullGuidanceJourney();
            Game.Configs.GameLocalization.SetLocale("fa-IR",false);
            placementDragPhase=0;
            SessionState.SetBool(TutorialBuildKey,true); tutorialBuildClick=0; tutorialBuildNext=0; tutorialBuildCaptured=tutorialPlotStaged=false;
        }
        private static bool AdvanceTutorialBuild(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime)
        {
            if(runtime.Phase!=MissionPhaseKind.Engage) return false;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(guidance.GuidanceId==45005 && tutorialBuildClick>=4)
            {
                if (EditorApplication.timeSinceStartup < tutorialBuildNext) return true;
                AssertCommittedPreviewPose();
                ScreenCapture.CaptureScreenshot(Output+"/placement-after-confirm.png");
                SessionState.SetBool(TutorialBuildKey,false); SessionState.SetBool("Warline.M03.Probe.RoadGateTutorial",false);
                if (placementIndicatorAudit && placementIndicatorVerified && !optionalJourney) { Complete(true,"Placement indicator aligned and animated; real confirm advanced to squad movement."); return true; }
                if (completeBuildingJourney) { Debug.Log("[M03BuildingJourney] one real defense placed; continuing through combat and results."); return false; }
                Complete(true,"M3 tutorial: Build -> defense card -> Place -> Confirm; pointer drag stayed fixed, valid placement advanced to squad movement."); return true;
            }
            if(guidance.GuidanceId!=45004) return false;
            if(EditorApplication.timeSinceStartup<tutorialBuildNext) return true;
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();
            if(tutorialBuildClick<3 && (drawer==null || !drawer.IsOpen) && (aria==null || !aria.IsPresentationVisible ||
                !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep!=4 ||
                (byte)typeof(AriaTutorialBriefingView).GetField("_tutorialStep",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(aria)!=4))
                return true;
            if(tutorialBuildClick==0)
            {
                if (drawer == null || !drawer.IsOpen)
                    ClickCommand(UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>().BuildButton);
                tutorialBuildClick++; tutorialBuildNext=EditorApplication.timeSinceStartup+2; return true;
            }
            Button button;
            if(tutorialBuildClick is 1 or 2)
            {
                if(drawer==null || !drawer.IsOpen) throw new InvalidOperationException("Tutorial Build drawer missing.");
                var catalog=drawer.GetComponent<BuildDrawerCatalogRuntimeView>();
                var method=typeof(BuildDrawerCatalogRuntimeView).GetMethod("ResolveBuildingTutorialTarget",BindingFlags.Instance|BindingFlags.NonPublic);
                object[] args={true,true,null}; button=method.Invoke(catalog,args) as Button;
                if(button==null) throw new InvalidOperationException("No next Build tutorial control.");
                string expected=tutorialBuildClick==1 ? "tutorial.next.defense" : "tutorial.next.place";
                if((string)args[2]!=expected) throw new InvalidOperationException("Wrong Build tutorial target: "+args[2]);
                var cue=GameObject.Find("AriaAssistantTargetIndicatorRuntime");
                if(cue==null || !cue.activeInHierarchy) throw new InvalidOperationException("Next click cue is invisible above Build.");
                if(!tutorialBuildCaptured)
                {
                    ScreenCapture.CaptureScreenshot(Output+"/tutorial-build-"+tutorialBuildClick+".png");
                    tutorialBuildCaptured=true; tutorialBuildNext=EditorApplication.timeSinceStartup+.5; return true;
                }
            }
            else if(tutorialBuildClick==3)
            {
                if(!tutorialPlotStaged)
                {
                    var bootstrap=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>().MatchBootstrap;
                    AssertInitialPlacementHandoff(bootstrap);
                    if (placementDragPhase == 0) ScreenCapture.CaptureScreenshot(Output+"/placement-initial-centered.png");
                    if (placementIndicatorAudit) StageLegalPlacementPreview(bootstrap);
                    else if (!SessionState.GetBool("Warline.M03.Probe.ConfirmDefaultPreview", false) && !AdvancePlacementPointerDrag(bootstrap)) return true;
                    tutorialPlotStaged=true; tutorialBuildNext=EditorApplication.timeSinceStartup+1; return true;
                }
                var bar=UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>();
                button=bar?.ConfirmButton; if(button==null || !button.interactable) return true;
                if(ObservePlacementIndicator(button)) return true;
                if(!tutorialBuildCaptured)
                {
                    ScreenCapture.CaptureScreenshot(Output+"/tutorial-build-confirm.png");
                    tutorialBuildCaptured=true; tutorialBuildNext=EditorApplication.timeSinceStartup+.5; return true;
                }
            }
            else return true;
            if (tutorialBuildClick==3)
            {
                var command=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>().MatchBootstrap.BuildingUiCommandContract;
                CapturePreviewPoseForCommit();
                tutorialMaterialsSpent=command.ActivePlacementCost; tutorialCreditsSpent=command.ActivePlacementCreditsCost;
            }
            if (tutorialBuildClick == 2) StageDistantPlacementCamera();
            ClickTutorialButton(button);
            if (tutorialBuildClick == 3) AssertCommittedPreviewPose();
            if (tutorialBuildClick == 2) AssertInitialPlacementHandoff(UnityEngine.Object.FindAnyObjectByType<MatchSceneView>().MatchBootstrap);
            tutorialBuildCaptured=false; tutorialBuildClick++; tutorialBuildNext=EditorApplication.timeSinceStartup+(placementIndicatorAudit && tutorialBuildClick==3 ? 3 : 1); return true;
        }
    }
}
