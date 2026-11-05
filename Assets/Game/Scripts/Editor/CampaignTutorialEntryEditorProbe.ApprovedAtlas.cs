using System;
using System.Collections.Generic;
using System.Linq;
using Game.Runtime;
using Game.Composition;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class CampaignTutorialEntryEditorProbe
    {
        private const string ApprovedAtlasJourney=Active+".ApprovedAtlas";
        private static AriaTouchInputUiSystemHelper approvedMenuTouch;
        private static InputSettings approvedPreviousInput,approvedInput;
        private static double approvedTouchDue;
        public static void RunApprovedAtlasM01Journey()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Requires idle Editor");
            string root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"warline-approved-campaign-"+Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",root);
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");
            var saves=SaveService.CreateDefault();var profile=saves.LoadProfile();profile.firstLaunchStatus=FirstLaunchProfileState.Completed;profile.firstLaunchWatched=true;
            profile.firstLaunchLanguage="English";profile.firstLaunchCommanderDisplayName="Commander";profile.firstLaunchCommanderPortraitIndex=0;saves.SaveProfile(profile);
            RunM01Journey();SessionState.SetBool(ApprovedAtlasJourney,true);
            Debug.Log("[ApprovedCampaignJourney] started menuInput=InputSystemTouch gameplayInput=InputSystemTouch profileFixture=FirstLaunchOnly outcomesInjected=0");
        }
        private static SharedMissionResultView resultViewInstance;
        private static double resultVisibleAt;
        private static bool WaitApprovedResult(MissionResultPopupView owner, byte stars, double now)
        {
            if(!SessionState.GetBool(ApprovedAtlasJourney,false)||owner.SharedResult==null)return false;
            var view=owner.SharedResult;
            if(resultViewInstance!=view){resultViewInstance=view;resultVisibleAt=now;}
            if(now-resultVisibleAt<2.6)return true;
            var c=view.Controls;
            for(int i=0;i<3;i++)
                if(c.FilledStars[i].gameObject.activeInHierarchy!=(i<stars)||i<stars&&c.StarRects[i].localScale.x<.99f||c.OutlineStars[i].gameObject.activeInHierarchy==(i<stars))
                    throw new InvalidOperationException("Shared result earned-star animation did not settle.");
            return false;
        }
        private static bool EnterApprovedCampaignThroughControls()
        {
            if(approvedMenuTouch==null)
            {
                approvedPreviousInput=InputSystem.settings;approvedInput=UnityEngine.Object.Instantiate(approvedPreviousInput);
                approvedInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                approvedInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=approvedInput;
                approvedMenuTouch=new AriaTouchInputUiSystemHelper();if(!approvedMenuTouch.Start())throw new InvalidOperationException("Native Campaign touch unavailable");
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();Application.runInBackground=true;
            }
            approvedMenuTouch.Tick(Time.unscaledTime);
            if(approvedMenuTouch.PlayerInterrupted||approvedMenuTouch.UnexpectedSamples!=0)throw new InvalidOperationException("Native Campaign input interrupted");
            if(approvedMenuTouch.IsBusy||EditorApplication.timeSinceStartup<approvedTouchDue)return false;
            if(!UiShellRuntimeGateway.TryReadShellState(out var shell)||shell.IsTransitionRunning)return false;
            if(shell.ActiveRoute==UIRoute.Match)return true;
            if(shell.ActiveRoute==UIRoute.MainMenu)
            {
                var button=UnityEngine.Object.FindObjectsByType<Button>().FirstOrDefault(b=>b.name=="ContinueButton"&&ApprovedPresented(b));
                if(button!=null)ApprovedTap(button);return false;
            }
            if(shell.ActiveRoute==UIRoute.Campaign)
            {
                var view=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();var atlas=view?.ApprovedAtlas;if(atlas==null)return false;
                if(!atlas.ShowingMissions||view.IsChapterTwo||view.IsChapterThree||view.IsChapterFour||view.IsChapterFive)ApprovedTap(atlas.NavigationCards[0].Button);
                else if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var model)||model.SelectedMission.MissionId!=Missions[0])ApprovedTap(atlas.Regions[0].Cards[0].Button);
                else ApprovedTap(atlas.PrimaryButton);
                return false;
            }
            if(shell.ActiveRoute==UIRoute.MissionBriefing)
            {
                var view=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();if(view!=null)ApprovedTap(view.DeployOperationButton);
            }
            return false;
        }
        private static bool ApprovedPresented(Button button)
        {
            if(button==null||!button.isActiveAndEnabled||!button.IsInteractable())return false;
            var r=(RectTransform)button.transform;var canvas=r.GetComponentInParent<Canvas>().rootCanvas;
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,r.TransformPoint(r.rect.center));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            return hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button;
        }
        private static void ApprovedTap(Button button)
        {
            if(!ApprovedPresented(button))return;var r=(RectTransform)button.transform;var canvas=r.GetComponentInParent<Canvas>().rootCanvas;
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,r.TransformPoint(r.rect.center));
            if(!approvedMenuTouch.TryGesture(point,point,.18f,0,Time.unscaledTime))throw new InvalidOperationException("Campaign menu touch rejected");
            approvedTouchDue=EditorApplication.timeSinceStartup+.8;Debug.Log("[ApprovedCampaignJourney] touch="+button.name);
        }
        private static bool TickApprovedSelection(MatchSceneView match,double now)
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target)||!target.NeedsSelection)return false;
            if(approvedMenuTouch==null||approvedMenuTouch.IsBusy)return true;
            string action=journeyStep+":NormalSelectionGesture";
            if(lastJourneyAction==action&&now-journeyActionAt<4)return true;
            Vector2 from,to;
            if(target.DragSelection)
            {
                var guide=GameObject.Find("NormalSelectionDragGuide")?.GetComponent<RectTransform>();
                if(guide==null)return true;
                var canvas=guide.GetComponentInParent<Canvas>().rootCanvas;var corners=new Vector3[4];guide.GetWorldCorners(corners);
                var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                from=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);to=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            }
            else from=to=match.MatchBootstrap.WorldCamera.WorldToScreenPoint(target.Selection);
            foreach(var point in new[]{from,to,(from+to)*.5f})
            {
                if(!Screen.safeArea.Contains(point))return true;
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                if(hits.Count>0)return true;
            }
            if(!approvedMenuTouch.TryGesture(from,to,.18f,target.DragSelection?.8f:0,Time.unscaledTime))throw new InvalidOperationException("Normal Campaign selection gesture rejected");
            RecordAction(action,now);return true;
        }
        private static void FinishApprovedCampaign(bool pass)
        {
            if(!SessionState.GetBool(ApprovedAtlasJourney,false))return;
            Debug.Log("[ApprovedCampaignJourney] result="+(pass?"Passed":"Failed")+" menuGestures="+(approvedMenuTouch?.CompletedGestures??0)+" outcomesInjected=0 deviceAcceptance=Pending");
            approvedMenuTouch?.Dispose();approvedMenuTouch=null;if(approvedPreviousInput!=null)InputSystem.settings=approvedPreviousInput;
            if(approvedInput!=null)UnityEngine.Object.DestroyImmediate(approvedInput);SessionState.EraseBool(ApprovedAtlasJourney);
        }
    }
}
