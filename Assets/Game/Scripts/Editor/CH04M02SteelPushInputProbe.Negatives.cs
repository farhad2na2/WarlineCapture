using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.Tactical.Contracts;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Game.Editor
{
    public static partial class CH04M02SteelPushInputProbe
    {
        private static bool Negative=>SessionState.GetBool(Active+".Negative",false);
        private static bool negativeFocused,negativeRetry,negativeCaptured,negativeSupportsPrepared,negativePreparingSupports,negativeFrontPrepared;
        private static int negativeStage,negativeMapPhase,negativeAttempt,negativeWave;
        private static float negativeShownAt,negativePointStableAt;
        private static Vector2 negativePoint;
        private static Entity[] retiredMembers;
        private static Entity retiredReserve,negativeActor;
        private static readonly HashSet<Entity> negativeWithdrawn=new();
        private static bool Ready(Button b)=>b!=null&&b.IsActive()&&b.IsInteractable();
        public static void RunRetreatLossEnglish()
        {
            ResetModes();SessionState.SetBool(Active+".Negative",true);SessionState.SetBool(Active+".ManualInput",true);
            negativeFocused=negativeRetry=negativeCaptured=negativeSupportsPrepared=negativePreparingSupports=negativeFrontPrepared=false;negativeStage=negativeMapPhase=negativeWave=0;negativeShownAt=-1;retiredMembers=null;negativeActor=Entity.Null;negativeWithdrawn.Clear();RunEnglish();
        }
        private static bool CompleteNegative(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!Negative)return false;
            if(negativeRetry)
            {
                if(runtime.AttemptOrdinal<=negativeAttempt)return true;
                if(!em.HasComponent<CampaignMissionSteelPushState>(root))return true;
                var reserve=em.GetComponentData<CampaignMissionSteelPushState>(root);
                if(reserve.Initialized==0)return true;
                if(runtime.RunKind!=MissionRunKind.Retry||facts.CoreBreached!=0||facts.HostileDefeatedCount!=0||retiredMembers.Any(em.Exists)||em.Exists(retiredReserve)||reserve.StartingUsableFuel!=120||reserve.LowestUsableFuel!=120)
                    throw new InvalidOperationException("Steel Push Retry retained old actors, reserve or progress.");
                var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve.FuelReserve);
                if(storage.StoredFuelBarrels!=160||storage.CivilianFuelReserveBarrels!=40)throw new InvalidOperationException("Retry did not restore the authored finite reserve.");
                Shot("retry-fresh");Complete(true,"negative=retreat real-native-failure=Passed rewards=unchanged Smoke=locked Retry=fresh-actors-and-reserve");return true;
            }
            if(runtime.Outcome==MissionOutcomeKind.Victory)throw new InvalidOperationException("Withdrawal negative ended in Victory; expected native defeat was not established.");
            if(runtime.Outcome!=MissionOutcomeKind.Defeat)return false;
            if(!UiShellRuntimeGateway.TryReadMissionResult(out var result)||!result.RetryVisible)return true;
            var profile=inputSave.LoadProfile();
            if(profile.credits!=startingCredits||profile.commanderXp!=startingXp||store.ReadSupportUnlocks().Contains("ability.smoke_screen")||store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.SteelPush).firstClearRewardSettled)
                throw new InvalidOperationException("Steel Push defeat granted first-clear rewards or Smoke.");
            var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();var retry=view==null?null:typeof(MissionResultPopupView).GetField("retryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(view)as Button;
            if(!Ready(retry)||view.GetComponentsInParent<CanvasGroup>().Any(x=>x.alpha<.9f))return true;
            if(!negativeCaptured)
            {
                if(negativeShownAt<0)negativeShownAt=Time.unscaledTime;if(Time.unscaledTime-negativeShownAt<1)return true;
                Shot("retreat-defeat");negativeCaptured=true;Debug.Log("[SteelPushNativeDefeat] result=Passed core="+facts.CoreBreached+" reserveLost="+facts.ForwardPostDestroyed+" normalInput=retreat rewards=unchanged");return true;
            }
            var retired=new List<Entity>();foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))retired.Add(member.Entity);retiredMembers=retired.ToArray();
            retiredReserve=em.GetComponentData<CampaignMissionSteelPushState>(root).FuelReserve;negativeAttempt=runtime.AttemptOrdinal;
            if(Click(retry))negativeRetry=true;return true;
        }
        private static bool DriveNegative(EntityManager em,Entity root,in CampaignMissionAttemptFactsComponent facts)
        {
            if(!Negative)return false;
            if(!negativeSupportsPrepared)
            {
                var guide=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                if(!negativePreparingSupports&&guide.GuidanceId!=65006)return false;
                negativePreparingSupports=true;negativeWave=1;
            }
            else if(facts.ElapsedMilliseconds<2000&&negativeFrontPrepared)return false;
            if(touch==null){touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Public touch unavailable.");}
            touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<1)return true;
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            if(controls==null||Camera.main==null||!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return true;
            var live=new List<Entity>();foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))if(member.FactionId==1&&em.Exists(member.Entity)&&em.HasComponent<UnitHealth>(member.Entity)&&em.GetComponentData<UnitHealth>(member.Entity).Current>0)live.Add(member.Entity);var actors=live.Where(e=>em.HasComponent<CampaignMissionUnitRoleComponent>(e)&&
                (em.GetComponentData<CampaignMissionUnitRoleComponent>(e).UnitGroupId.ToString()=="group.ch04.m02.tank_front")== (negativeWave==0)).ToArray();
            if(!negativeSupportsPrepared)
            {
                // Withdraw the armored counterforce. The four rifles remain
                // at their authored post and must face real armored combat.
                actors=actors.Where(e=>em.HasComponent<UnitSourcePrefabKey>(e)&&em.GetComponentData<UnitSourcePrefabKey>(e).Value.ToString().Contains("Veh_")&&!negativeWithdrawn.Contains(e)).ToArray();
                if(actors.Length==0)
                {
                    Debug.Log("[SteelPushNegativeReceipt] nativeMove=Passed group=support count="+negativeWithdrawn.Count+" preparationClock="+facts.ElapsedMilliseconds+" withdrawnBeyond=860");
                    negativeSupportsPrepared=true;negativePreparingSupports=false;negativeWave=negativeStage=negativeMapPhase=0;negativeFocused=false;negativeActor=Entity.Null;return false;
                }
                if(negativeActor==Entity.Null||!actors.Contains(negativeActor))negativeActor=actors[0];
                actors=new[]{negativeActor};
            }
            if(actors.Length==0){negativeStage=4;return true;}
            if(negativeStage>=3)
            {
                bool issued=actors.All(e=>em.HasComponent<ManualMoveOrderTag>(e)||em.GetComponentData<LocalTransform>(e).Position.x>860);
                if(!issued){negativeStage=2;return true;}
                if(negativeWave==0){if(negativeStage==3)Debug.Log("[SteelPushNegativeReceipt] nativeMove=Passed group=front");negativeStage=4;return true;}
                if(!negativeSupportsPrepared)
                {
                    if(actors.Any(e=>em.GetComponentData<LocalTransform>(e).Position.x<860))return true;
                    if(!em.HasComponent<HoldPositionOrderTag>(negativeActor))
                    {
                        if(Click(controls.HoldButton)){manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SteelPushNegativeInput] normalButton=Hold withdrawnSupport=True");}
                        return true;
                    }
                    negativeWithdrawn.Add(negativeActor);negativeActor=Entity.Null;negativeStage=negativeMapPhase=0;negativeFocused=false;return true;
                }
                if(negativeStage==3){Debug.Log("[SteelPushNegativeReceipt] nativeMove=Passed group=support count="+actors.Length);negativeStage=4;}
                return true;
            }
            if(negativeStage==0)
            {
                if(state.ActiveCommandMode!=TacticalCommandMode.Select){Click(controls.SelectButton);return true;}
                if(!negativeFocused)
                {
                    Vector3 center=Vector3.zero;foreach(var actor in actors)center+=(Vector3)em.GetComponentData<LocalTransform>(actor).Position;center/=actors.Length;
                    if(negativeMapPhase<3){FocusNegativeDestination(center);return true;}
                    negativeFocused=true;negativeMapPhase=0;lastInput=EditorApplication.timeSinceStartup;return true;
                }
                Vector2 from=new(float.MaxValue,float.MaxValue),to=new(float.MinValue,float.MinValue);
                foreach(var actor in actors)
                {
                    var p=em.GetComponentData<LocalTransform>(actor).Position;
                    foreach(var offset in new[]{new Vector3(-3,0,-3),new Vector3(3,3,3),new Vector3(-3,3,3),new Vector3(3,0,-3)})
                    {var screen=Camera.main.WorldToScreenPoint((Vector3)p+offset);if(screen.z<=0)return true;from=Vector2.Min(from,screen);to=Vector2.Max(to,screen);}
                }
                from-=Vector2.one*8;to+=Vector2.one*8;
                var hits=new List<RaycastResult>();foreach(var point in new[]{from,to,(from+to)*.5f}){hits.Clear();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);if(!Camera.main.pixelRect.Contains(point)||hits.Count>0){negativeFocused=false;negativeMapPhase=0;return true;}}
                if(touch.TryGesture(from,to,.5f,.9f,Time.unscaledTime)){negativeStage=1;manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SteelPushNegativeInput] normalTouch=select-counterforce group="+negativeWave+" count="+actors.Length);}return true;
            }
            if(actors.Any(x=>!em.HasComponent<SelectedUnitTag>(x))){negativeStage=0;return true;}
            if(negativeSupportsPrepared&&!negativeFrontPrepared&&facts.ElapsedMilliseconds<2000)
            {
                if(Click(controls.HoldButton))
                {negativeFrontPrepared=true;negativeStage=negativeMapPhase=0;negativeFocused=false;manualActions++;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SteelPushNegativeInput] normalButton=Hold frontPreparation=True");}
                return true;
            }
            if(state.ActiveCommandMode!=TacticalCommandMode.Move){Click(controls.MoveButton);return true;}
            var destination=new Vector3(865,0,665);if(negativeMapPhase<3){FocusNegativeDestination(destination);return true;}
            if(TouchWorld(destination)){negativeStage=3;Shot("retreat-order");}return true;
        }
        private static bool TouchWorld(Vector3 position)
        {
            var camera=Camera.main;if(camera==null)return false;var point=camera.WorldToScreenPoint(position);
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            if(point.z<=0||!camera.pixelRect.Contains(point))
            {
                // A guidance change can finish its smooth pan after the map
                // gesture. Retry native map navigation once the pan settles.
                if(EditorApplication.timeSinceStartup-lastInput>4)negativeMapPhase=0;
                return false;
            }
            if(hits.Count>0)return false;
            if(Vector2.Distance(negativePoint,point)>1f){negativePoint=point;negativePointStableAt=Time.unscaledTime;return false;}
            if(Time.unscaledTime-negativePointStableAt<.5f)return false;
            if(!touch.TryGesture(point,point,.18f,0,Time.unscaledTime))return false;
            lastInput=EditorApplication.timeSinceStartup;manualActions++;Debug.Log("[SteelPushNegativeInput] normal-world-touch="+position);return true;
        }
        private static void FocusNegativeDestination(Vector3 destination)
        {
            var popup=UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            bool wasOpen=popup!=null&&popup.IsOpen;
            if(negativeMapPhase==2&&popup!=null)
            {
                Click(popup.CloseAction);negativeMapPhase=3;return;
            }
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
            if(Negative&&wasOpen)
            {
                // A click inside the current camera viewport deliberately does
                // not recenter the shipping map. Drag its viewport normally.
                var viewport=typeof(MatchHudMinimapView).GetField("viewportRect",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(map)as RectTransform;
                if(viewport==null||!viewport.gameObject.activeInHierarchy)throw new InvalidOperationException("Native full-map viewport unavailable.");
                var from=RectTransformUtility.WorldToScreenPoint(eventCamera,viewport.TransformPoint(viewport.rect.center));
                if(!touch.TryGesture(from,point,.15f,.6f,Time.unscaledTime))return;
                Debug.Log("[SteelPushNegativeMap] normalTouch=viewport-drag target="+destination);
            }
            else ExecuteEvents.Execute(map.gameObject,new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
            negativeMapPhase=wasOpen?2:1;lastInput=EditorApplication.timeSinceStartup;
        }
    }
}
