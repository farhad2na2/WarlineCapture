using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Game.Editor
{
    public static partial class CH02M02SupplyLineInputProbe
    {
        private static int lastManualGuidance,manualActions;
        private static bool manualFocused;
        private static Vector2 manualPoint;
        private static float manualPointStableAt;
        public static void RunManualPersian(){SessionState.SetString(Active+".Locale","fa-IR");RunManual();}
        private static void DriveManual(EntityManager em,Entity root)
        {
            if(UiShellRuntimeGateway.ReadAriaPlay().Active)throw new InvalidOperationException("ARIA is active during manual journey.");
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();if(view==null||!view.IsPresentationVisible)return;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))
            {Tap(view.ContinueButton);return;}
            if(target.Moving||target.BattleAction==UiTutorialBattleAction.Watch)return;
            int key=guidance.GuidanceId*2+(target.NeedsSelection?0:1);
            if(lastManualGuidance!=key){lastManualGuidance=key;manualFocused=false;ScreenCapture.CaptureScreenshot(Output+"/manual-"+key+".png");}
            if(!manualFocused){Tap(view.ShowMeButton);manualFocused=true;return;}
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();var camera=Camera.main;
            if(controls==null||camera==null||!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return;
            var mode=target.NeedsSelection?TacticalCommandMode.Select:target.BattleAction==UiTutorialBattleAction.Attack?TacticalCommandMode.Attack:TacticalCommandMode.Move;
            if(state.ActiveCommandMode!=mode){Tap(mode==TacticalCommandMode.Select?controls.SelectButton:mode==TacticalCommandMode.Attack?controls.AttackButton:controls.MoveButton);return;}
            if(target.NeedsSelection&&target.DragSelection)
            {
                var from=new Vector2(float.MaxValue,float.MaxValue);
                var to=new Vector2(float.MinValue,float.MinValue);
                for(int i=0;i<8;i++)
                {
                    var worldCorner=new Vector3((i&1)==0?target.SelectionMin.x:target.SelectionMax.x,
                        (i&2)==0?target.SelectionMin.y:target.SelectionMax.y,
                        (i&4)==0?target.SelectionMin.z:target.SelectionMax.z);
                    var screen=camera.WorldToScreenPoint(worldCorner);
                    if(screen.z<=0)return;
                    from=Vector2.Min(from,screen);to=Vector2.Max(to,screen);
                }
                from-=Vector2.one*8;to+=Vector2.one*8;
                var selectionHits=new List<RaycastResult>();
                foreach(Vector2 p in new[]{(Vector2)from,(Vector2)to,((Vector2)from+(Vector2)to)*.5f})
                {
                    selectionHits.Clear();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=p},selectionHits);
                    if(!camera.pixelRect.Contains(p)||selectionHits.Count>0){Tap(view.ShowMeButton);return;}
                }
                if(Vector2.Distance(manualPoint,from)>1f){manualPoint=from;manualPointStableAt=Time.unscaledTime;return;}
                if(Time.unscaledTime-manualPointStableAt<.5f)return;
                if(touch.TryGesture(from,to,.5f,.9f,Time.unscaledTime))
                {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SupplyLineManual] normalTouch=selection-drag rifles="+target.RequiredSelectionCount);}
                return;
            }
            var world=target.NeedsSelection?target.Selection+Vector3.up:target.Destination;
            var point=camera.WorldToScreenPoint(world);var hits=new List<RaycastResult>();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            // Editor update callbacks report the window size, while the camera
            // and input device use the selected Game View render resolution.
            if(point.z<=0||!camera.pixelRect.Contains(point)||hits.Count>0){Tap(view.ShowMeButton);return;}
            if(Vector2.Distance(manualPoint,point)>1f){manualPoint=point;manualPointStableAt=Time.unscaledTime;return;}
            if(Time.unscaledTime-manualPointStableAt<.5f)return;
            if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime))
            {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SupplyLineManual] normalTouch="+mode+" world="+world);}
        }
    }
}
