using System;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudMinimapView
    {
        Action normalCameraZoomIn,normalCameraZoomOut;
        Func<MatchHudZoomControlState> normalCameraZoomState;
        public bool HasNormalCameraZoomControls=>!UseFullMapProjection&&!_allowZoom&&normalCameraZoomState!=null&&zoomInButton!=null&&zoomOutButton!=null;

        // Existing compact Campaign controls, separate from full-map projection
        // zoom. The owner supplies the ordinary camera request and endstop state.
        public void BindNormalCameraZoom(Action zoomIn,Action zoomOut,Func<MatchHudZoomControlState> state)
        {
            UnbindNormalCameraZoom();
            if(UseFullMapProjection||_allowZoom||zoomInButton==null||zoomOutButton==null||zoomIn==null||zoomOut==null||state==null)return;
            normalCameraZoomIn=zoomIn;normalCameraZoomOut=zoomOut;normalCameraZoomState=state;
            zoomInButton.onClick.AddListener(HandleNormalCameraZoomIn);
            zoomOutButton.onClick.AddListener(HandleNormalCameraZoomOut);
            RestoreCompactCameraButton(zoomInButton,10);
            RestoreCompactCameraButton(zoomOutButton,84);
            RefreshNormalCameraZoomControls();
        }
        public void UnbindNormalCameraZoom()
        {
            if(zoomInButton!=null)zoomInButton.onClick.RemoveListener(HandleNormalCameraZoomIn);
            if(zoomOutButton!=null)zoomOutButton.onClick.RemoveListener(HandleNormalCameraZoomOut);
            bool wasBound=normalCameraZoomState!=null;
            normalCameraZoomIn=null;normalCameraZoomOut=null;normalCameraZoomState=null;
            if(wasBound&&!_allowZoom){SetZoomVisible(zoomInButton,false);SetZoomVisible(zoomOutButton,false);}
        }
        static void RestoreCompactCameraButton(UnityEngine.UI.Button button,float right)
        {
            var rect=button.transform as RectTransform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            rect.sizeDelta=new Vector2(64,64);rect.anchoredPosition=new Vector2(-right,10);rect.localScale=Vector3.one;
            rect.SetAsLastSibling();button.gameObject.SetActive(true);
        }
        public void RefreshNormalCameraZoomControls()
        {
            if(!HasNormalCameraZoomControls)return;
            var state=normalCameraZoomState();
            zoomInButton.interactable=state.ZoomInEnabled;zoomOutButton.interactable=state.ZoomOutEnabled;
            HudActionVisuals.Refresh(zoomInButton);HudActionVisuals.Refresh(zoomOutButton);
        }
        void HandleNormalCameraZoomIn(){if(HasNormalCameraZoomControls&&normalCameraZoomState().ZoomInEnabled){UIAudioEventGateway.Raise(UIAudioEventKind.ButtonPrimaryClick);normalCameraZoomIn();RefreshNormalCameraZoomControls();}}
        void HandleNormalCameraZoomOut(){if(HasNormalCameraZoomControls&&normalCameraZoomState().ZoomOutEnabled){UIAudioEventGateway.Raise(UIAudioEventKind.ButtonPrimaryClick);normalCameraZoomOut();RefreshNormalCameraZoomControls();}}
    }
}
