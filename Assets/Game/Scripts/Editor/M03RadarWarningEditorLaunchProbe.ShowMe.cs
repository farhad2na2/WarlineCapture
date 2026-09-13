using System;
using Game.Composition;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class M03RadarWarningEditorLaunchProbe
    {
        private static int showMePhase;
        private static double showMeNext;
        public static void RunShowMeTutorial()
        {
            showMePhase=0; showMeNext=0;
            SessionState.SetBool("Warline.M03.Probe.ShowMe",true);
            RunFullGuidanceJourney();
        }

        private static bool AdvanceShowMeValidation()
        {
            if(EditorApplication.timeSinceStartup<showMeNext) return true;
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(view==null || !view.IsPresentationVisible || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep!=5) return true;
            var match=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target)) throw new InvalidOperationException("M3 movement lesson has no target.");
            switch(showMePhase)
            {
                case 0:
                    if(!target.NeedsSelection) throw new InvalidOperationException("Expected unselected squad after the optional defense lesson.");
                    ClickCommand(view.ShowMeButton); break;
                case 1:
                    AssertVisibleTutorialWorld(match.MatchBootstrap.WorldCamera,target.Selection);
                    ScreenCapture.CaptureScreenshot(Output+"/show-me-select-squad.png"); break;
                case 2:
                    if(!match.MatchBootstrap.SelectionUiCommand.RequestSelectAllSoldiers()) throw new InvalidOperationException("Squad selection rejected.");
                    break;
                case 3: ClickCommand(view.ShowMeButton); break;
                case 4:
                    var indicator=GameObject.Find("AriaAssistantTargetIndicatorRuntime");
                    if(indicator==null || !indicator.activeInHierarchy) throw new InvalidOperationException("Move button cue missing.");
                    ScreenCapture.CaptureScreenshot(Output+"/show-me-move-button.png"); break;
                case 5: ClickCommand(controls.MoveButton); break;
                case 6: ClickCommand(view.ShowMeButton); break;
                case 7:
                    AssertVisibleTutorialWorld(match.MatchBootstrap.WorldCamera,target.Destination);
                    ScreenCapture.CaptureScreenshot(Output+"/show-me-destination.png"); break;
                default:
                    SessionState.SetBool("Warline.M03.Probe.ShowMe",false);
                    SessionState.SetBool(TutorialBuildKey,false);
                    SessionState.SetBool("Warline.M03.Probe.RoadGateTutorial",false);
                    Complete(true,"SHOW ME: selection ring/camera -> Move button -> destination ring/camera, through the actual M3 tutorial flow."); return true;
            }
            showMePhase++; showMeNext=EditorApplication.timeSinceStartup+1.2; return true;
        }

        private static void AssertVisibleTutorialWorld(Camera camera,Vector3 expected)
        {
            var marker=GameObject.Find("AriaAssistantPreviewHighlightRuntime");
            if(marker==null || !marker.activeInHierarchy) throw new InvalidOperationException("SHOW ME has no visible world marker.");
            var ring=marker.GetComponentInChildren<LineRenderer>();
            if(ring==null || Vector2.Distance(new Vector2(ring.bounds.center.x,ring.bounds.center.z),new Vector2(expected.x,expected.z))>1f)
                throw new InvalidOperationException("World marker does not identify the requested tutorial target.");
            var viewport=camera.WorldToViewportPoint(expected);
            if(viewport.z<=0 || viewport.x<.12f || viewport.x>.72f || viewport.y<.28f || viewport.y>.86f)
                throw new InvalidOperationException("SHOW ME target remains outside playable viewport: "+viewport);
            Debug.Log("[MissionShowMeLive] target="+expected+" viewport="+viewport+" marker=visible");
        }
    }
}
