using System;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    public static partial class CH02M05RouteReopenedInputProbe
    {
        private static string Negative=>SessionState.GetString(Active+".Negative","");
        private static bool negativeFocused;
        private static int negativeMapPhase;
        private static bool negativeRetry;
        private static int negativeAttempt;
        private static Entity[] retiredRouteMembers;
        private static bool negativeOrderIssued;
        private static Vector2 negativePoint;
        private static float negativePointStableAt;
        public static void RunRecordsLossEnglish()=>RunNegative("records");
        public static void RunLifelineLossEnglish()=>RunNegative("lifeline");
        private static void RunNegative(string kind)
        {
            ResetSpecialRuns();
            SessionState.SetString(Active+".Negative",kind);SessionState.SetBool(Manual,false);
            SessionState.SetBool(Active+".ManualInput",true);SessionState.SetString(Active+".Locale","en");
            negativeFocused=negativeOrderIssued=false;negativeMapPhase=0;negativeRetry=false;retiredRouteMembers=null;negativePoint=default;Run();
        }
        private static bool CompleteExpectedFailure(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,in CampaignMissionRouteReopenedState route)
        {
            if(Negative.Length==0)return false;
            var expected=Negative=="records"?RouteReopenedFailure.RecordsLost:RouteReopenedFailure.ReliefConvoyLost;
            if(route.Failure!=expected)throw new InvalidOperationException("Negative journey failed for an unexpected reason: "+route.Failure);
            if(runtime.Outcome!=MissionOutcomeKind.Defeat||!UiShellRuntimeGateway.TryReadMissionResult(out var result)||!result.RetryVisible)return true;
            if(store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.RouteReopened).firstClearRewardSettled)
                throw new InvalidOperationException("Negative journey settled completion rewards.");
            var profile=inputSave.LoadProfile();
            if(profile.credits!=startingCredits||profile.commanderXp!=startingXp)throw new InvalidOperationException("Defeat changed Campaign rewards.");
            if(Negative=="records"&&em.Exists(route.RecordsBuilding)&&em.HasComponent<UnitHealth>(route.RecordsBuilding)&&em.GetComponentData<UnitHealth>(route.RecordsBuilding).Current>0)
                throw new InvalidOperationException("Records defeat lacks a destroyed office.");
            ScreenCapture.CaptureScreenshot(Output+"/"+Negative+"-defeat.png");
            Debug.Log("[RouteReopenedNegative] result=Passed kind="+Negative+" cause="+expected+" input=UI-pointer-and-world-touch defeat=visible rewards=unsettled Retry=visible");
            if(!negativeRetry)
            {
                var members=em.GetBuffer<CampaignMissionRouteReopenedMember>(root,true);retiredRouteMembers=new Entity[members.Length];
                for(int i=0;i<members.Length;i++)retiredRouteMembers[i]=members[i].Entity;
                negativeAttempt=runtime.AttemptOrdinal;
                var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                var retry=view==null?null:typeof(MissionResultPopupView).GetField("retryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(view) as Button;
                if(Ready(retry)){Tap(retry);negativeRetry=true;}
            }
            return true;
        }
        private static bool DriveNegative(EntityManager em,Entity root,in CampaignMissionRouteReopenedState route)
        {
            if(Negative.Length==0)return false;
            if(negativeRetry)
            {
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if(runtime.AttemptOrdinal<=negativeAttempt)return true;
                if(runtime.RunKind!=MissionRunKind.Retry||route.ReliefDelivered!=0||route.FuelDelivered!=0||route.LinkRestored!=0||route.RecordsHoldMilliseconds!=0||retiredRouteMembers.Any(em.Exists))
                    throw new InvalidOperationException("Retry retained actors or progress from the failed attempt.");
                if(route.RecordsBuildingInitialized==0||em.GetComponentData<UnitHealth>(route.RecordsBuilding).Current<=0)return true;
                Complete(true,"negative="+Negative+" real-combat=Passed rewards=unchanged Retry=fresh-actors-and-objectives");return true;
            }
            if(Negative=="records"&&route.GarrisonCleared==0)return false;
            if(negativeOrderIssued)return true;
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();if(controls==null)return true;
            Entity actor=Entity.Null;
            foreach(var member in em.GetBuffer<CampaignMissionRouteReopenedMember>(root,true))
                if(member.Kind==(Negative=="records"?0:2)&&em.Exists(member.Entity)&&em.HasComponent<UnitHealth>(member.Entity)&&em.GetComponentData<UnitHealth>(member.Entity).Current>0){actor=member.Entity;break;}
            if(actor==Entity.Null)return true;
            if(!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return true;
            if(!em.HasComponent<SelectedUnitTag>(actor))
            {
                var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                if(!negativeFocused){Tap(view?.ShowMeButton);negativeFocused=true;return true;}
                if(state.ActiveCommandMode!=Game.Tactical.Contracts.TacticalCommandMode.Select){Tap(controls.SelectButton);return true;}
                TouchWorld(em.GetComponentData<LocalTransform>(actor).Position+new Unity.Mathematics.float3(0,1,0));return true;
            }
            var destination=Negative=="records"?(Vector3)em.GetComponentData<LocalTransform>(route.RecordsBuilding).Position+Vector3.up:new Vector3(610,0,620);
            var mode=Negative=="records"?Game.Tactical.Contracts.TacticalCommandMode.Attack:Game.Tactical.Contracts.TacticalCommandMode.Move;
            if(state.ActiveCommandMode!=mode){Tap(Negative=="records"?controls.AttackButton:controls.MoveButton);return true;}
            if(negativeMapPhase<3){FocusNegativeDestination(destination);return true;}
            if(TouchWorld(destination))negativeOrderIssued=true;return true;
        }
        private static bool TouchWorld(Vector3 position)
        {
            var camera=Camera.main;if(camera==null)return false;var point=camera.WorldToScreenPoint(position);
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(point.z<=0||!camera.pixelRect.Contains(point)||hits.Count>0)return false;
            if(Vector2.Distance(negativePoint,point)>1f){negativePoint=point;negativePointStableAt=Time.unscaledTime;return false;}
            if(Time.unscaledTime-negativePointStableAt<.5f)return false;
            if(!touch.TryGesture(point,point,.18f,0,Time.unscaledTime))return false;
            lastInput=EditorApplication.timeSinceStartup;manualActions++;Debug.Log("[RouteReopenedNegativeInput] normal-world-touch="+position);return true;
        }
        private static void FocusNegativeDestination(Vector3 destination)
        {
            var popup=UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            bool wasOpen=popup!=null&&popup.IsOpen;
            if(negativeMapPhase==2&&popup!=null){Tap(popup.CloseAction);negativeMapPhase=3;return;}
            var map=popup!=null&&popup.IsOpen?popup.Minimap:UnityEngine.Object.FindObjectsByType<MatchHudMinimapView>(FindObjectsInactive.Exclude).FirstOrDefault(x=>popup==null||x!=popup.Minimap);
            if(map==null)return;var canvas=map.GetComponentInParent<Canvas>();var eventCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            Vector2 point;
            if(popup==null||!popup.IsOpen){var rect=(RectTransform)map.transform;point=RectTransformUtility.WorldToScreenPoint(eventCamera,rect.TransformPoint(rect.rect.center));}
            else
            {
                object[] args={destination,eventCamera,default(Vector2)};
                var method=typeof(MatchHudMinimapView).GetMethod("TryGetPresentedMapPoint",BindingFlags.NonPublic|BindingFlags.Instance);
                if(method==null||!(bool)method.Invoke(map,args))throw new InvalidOperationException("Cannot locate objective on native full map.");
                point=(Vector2)args[2];
            }
            ExecuteEvents.Execute(map.gameObject,new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            negativeMapPhase=wasOpen?2:1;lastInput=EditorApplication.timeSinceStartup;
        }
    }
}
