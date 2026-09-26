using Game.UI.Runtime;
using Game.UI.Contracts;
using Game.Runtime;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Game.Editor
{
    public static partial class M05BreachAssaultEditorProbe
    {
        private const string AriaWatch = "Warline.M05.AriaWatch";
        private static double nextGuidedClick;
        private static bool ariaWatchStarted;
        private static int ariaWatchActions;
        private static double ariaWatchProgressAt;
        public static void RunGuidedEnglish()
        {
            SessionState.SetBool("Warline.M05.GuideOnly",false);SessionState.SetBool("Warline.M05.RetryProbe",false);SessionState.SetBool("Warline.M05.Guided",true);
            Run();
        }
        [MenuItem("Tools/Warline/Validation/Run M05 ARIA Watch")]
        public static void RunAriaWatchEnglish()
        {
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION", "1");
            SelectionRuntimeDiagnosticsSystemHelper.EditorClickDiagnosticsEnabled = true;
            SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled = true;
            SessionState.SetBool("Warline.M05.GuideOnly",false);SessionState.SetBool("Warline.M05.RetryProbe",false);
            SessionState.SetBool("Warline.M05.Guided",false);SessionState.SetBool("Warline.M05.SkipComics",true);
            SessionState.SetBool(AriaWatch,true);ariaWatchStarted=false;ariaWatchActions=0;ariaWatchProgressAt=0;
            Run();
        }
        private static void TickAriaWatch()
        {
            var state=UiShellRuntimeGateway.ReadAriaPlay();
            if(state.Phase==AriaPlayPhase.Blocked)throw new InvalidOperationException("Watch ARIA Play became blocked after "+state.Actions+" actions.");
            if(state.Active)
            {
                ariaWatchStarted=true;
                if(state.Actions!=ariaWatchActions)
                {
                    ariaWatchActions=state.Actions;ariaWatchProgressAt=EditorApplication.timeSinceStartup;
                    var feedback=UnityEngine.Object.FindAnyObjectByType<BattleHudRuntimeFeedbackView>();
                    Debug.Log("[M05AriaWatch] actions="+ariaWatchActions+" phase="+state.Phase+
                        " feedback="+(feedback!=null&&feedback.HasLastCommandResult
                            ? feedback.LastCommandResult.Accepted+":"+feedback.LastCommandResult.ReasonCode+":"+feedback.LastCommandResult.Message
                            : "none"));
                }
                else if(ariaWatchProgressAt>0 && EditorApplication.timeSinceStartup-ariaWatchProgressAt>190)
                    throw new TimeoutException("Watch ARIA Play made no progress for 190 seconds after "+ariaWatchActions+" actions.");
                return;
            }
            if(ariaWatchStarted)throw new InvalidOperationException("Watch ARIA Play stopped before mission victory after "+ariaWatchActions+" actions.");
            if(EditorApplication.timeSinceStartup<nextGuidedClick)return;
            var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
            var button=buttons.FirstOrDefault(x=>x.name=="ConfirmWatchAria"&&x.IsActive()&&x.IsInteractable())??
                buttons.FirstOrDefault(x=>x.name=="WatchAriaPlay"&&x.IsActive()&&x.IsInteractable());
            if(button==null)return;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            nextGuidedClick=EditorApplication.timeSinceStartup+1;
        }
        private static void TickGuided()
        {
            if(EditorApplication.timeSinceStartup<nextGuidedClick)return;
            var views=UnityEngine.Object.FindObjectsByType<AriaTutorialBriefingView>(FindObjectsInactive.Exclude);
            foreach(var view in views)
            {
                if(!view.IsPresentationVisible)continue;
                if(view.ShowMeButton!=null && view.ShowMeButton.isActiveAndEnabled && view.ShowMeButton.interactable)
                    view.ShowMeButton.onClick.Invoke();
                if(view.DoItButton!=null && view.DoItButton.isActiveAndEnabled && view.DoItButton.interactable)
                    view.DoItButton.onClick.Invoke();
                ScreenCapture.CaptureScreenshot(Output+"/guided-current-en.png");
                nextGuidedClick=EditorApplication.timeSinceStartup+3;
                return;
            }
        }
    }
}
