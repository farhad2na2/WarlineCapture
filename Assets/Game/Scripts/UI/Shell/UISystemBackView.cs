using System.Linq;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class UISystemBackView : MonoBehaviour
    {
        private UIShellView shell;
        private float nextBack;
        private void Awake()=>shell=GetComponent<UIShellView>();
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Inner UI consumes Back; only the unobstructed root delegates to Android.
            Input.backButtonLeavesApp=CanLeaveFromRoot();
#endif
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true&&Time.unscaledTime>=nextBack)
            {
                nextBack=Time.unscaledTime+.25f;
                HandleBack();
            }
        }
        public bool CanLeaveFromRoot()
        {
            if(!UiShellRuntimeGateway.TryReadShellState(out var state)||state.IsTransitionRunning||state.CurrentMode!=UiShellMode.MainMenu||state.ActiveRoute!=UIRoute.MainMenu)return false;
            return !(shell?.ContentSystem?.HasBackOverlay??false)
                && !FindObjectsByType<NarrativeSequenceView>(FindObjectsSortMode.None).Any(v=>v.IsVisible)
                && !UiBackOverlayRegistry.HasOpenOverlay;
        }
        public bool HandleBack()
        {
            var selected=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            var input=selected!=null?selected.GetComponent<TMP_InputField>():null;
            if(input!=null&&input.isFocused){input.DeactivateInputField();return true;}
            if(!UiShellRuntimeGateway.TryReadShellState(out var state)||state.IsTransitionRunning||state.CurrentMode==UiShellMode.Loading)return true;
            if(UiBackOverlayRegistry.HandleBack())return true;
            var narrative=FindObjectsByType<NarrativeSequenceView>(FindObjectsSortMode.None).FirstOrDefault(v=>v.IsVisible);
            if(narrative!=null){narrative.PlaybackControlsView?.HandleBack();return true;}
            if(shell?.ContentSystem?.HandleOverlayBack()==true)return true;
            if(state.CurrentMode==UiShellMode.MatchHud||state.ActiveRoute==UIRoute.Match)
                return UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.Pause);
            if(state.ActiveRoute==UIRoute.MainMenu)return false;
            // Reuse the native Back control's route and fallback, preserving mission selection/history.
            if(shell?.ContentSystem?.SubmitScreenBack()==true)return true;
            return UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.BackMenuRoute,UIRoute.MainMenu,false);
        }
    }
}
