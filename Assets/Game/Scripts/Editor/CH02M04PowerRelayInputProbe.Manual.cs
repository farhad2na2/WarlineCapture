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
    public static partial class CH02M04PowerRelayInputProbe
    {
        private static int lastManualGuidance,manualActions;
        private static bool manualFocused,schoolCaptured,sourceChecked;
        private static Vector2 manualPoint;
        private static float manualPointStableAt;
        private static bool ManualInput=>SessionState.GetBool(Active+".ManualInput",false);
        public static void RunManualEnglish(){SessionState.SetBool(Active+".ManualInput",true);SessionState.SetString(Active+".Locale","en");Run();}
        public static void RunWatchPersian(){SessionState.SetBool(Active+".ManualInput",false);SessionState.SetString(Active+".Locale","fa-IR");Run();}
        private static void ValidateRefinerySchool(EntityManager em,in CampaignMissionPowerRelayState state)
        {
            if(!sourceChecked && state.Ready!=0)
            {
                using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                if(maps.CalculateEntityCount()!=1)throw new InvalidOperationException("Power Relay map owner missing.");
                var metadata=maps.GetSingleton<OperationMapMetadataComponent>();
                if(!metadata.Blob.IsCreated||metadata.PhysicalSourceValidated==0||!metadata.Blob.Value.OperationMapId.Equals(CH02M04PowerRelayConfigBuilder.MapId)||
                    !metadata.Blob.Value.SourceContentHash.Equals("2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778"))throw new InvalidOperationException("Unexpected Power Relay refinery source.");
                using var owners=em.CreateEntityQuery(typeof(DenseCityPresentationIdentity));
                using var entities=owners.ToEntityArray(Unity.Collections.Allocator.Temp);int schoolOwners=0;
                foreach(var e in entities)if(em.GetComponentData<DenseCityPresentationIdentity>(e).StableId.Equals("densecity.3c718931b282002708f27070c2c0f14619d641ffc3d200a807b9b566be9faa55"))schoolOwners++;
                if(schoolOwners!=1)throw new InvalidOperationException("Shelter frontage must retain exactly one source building owner: "+schoolOwners);
                sourceChecked=true;Debug.Log("[PowerRelaySchool] result=Passed sourceOwner=one school=existing-frontage marker=localized commands=unchanged");
            }
            if(!schoolCaptured&&state.FamiliesSheltered!=0)
            {schoolCaptured=true;ScreenCapture.CaptureScreenshot(Output+"/shelter-frontage.png");}
        }
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
                {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[PowerRelayManual] normalTouch=selection-drag rifles="+target.RequiredSelectionCount);}
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
            {manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[PowerRelayManual] normalTouch="+mode+" world="+world);}
        }
    }
}
