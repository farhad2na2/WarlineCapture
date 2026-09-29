using System;
using System.Collections.Generic;
using Game.Components;
using Game.Tactical.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Game.Editor
{
    public static partial class CH04M03SplitFrontInputProbe
    {
        private static int cancelStage;
        private static int observedCancelStage=-1;
        private static double cancelStageStarted;
        private static bool TickPlayerLauncherCancellation(EntityManager em,Entity root)
        {
            if(cancelStage==13)return true;
            if(observedCancelStage!=cancelStage)
            {observedCancelStage=cancelStage;cancelStageStarted=EditorApplication.timeSinceStartup;Debug.Log("[SplitFrontCancelState] stage="+cancelStage);}
            if(EditorApplication.timeSinceStartup-cancelStageStarted>45)throw new TimeoutException("Native cancellation input stalled at stage "+cancelStage);
            if(UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions)&&restrictions.CinematicInteractionLocked)return false;
            if(smokeHand==null){smokeHand=new AriaTouchInputUiSystemHelper();if(!smokeHand.Start())throw new InvalidOperationException("Player launcher touch actuator unavailable");smokeHost=new GameObject("Split Front Player Launcher Input",typeof(SmokeInputHost)){hideFlags=HideFlags.DontSave};}
            if(smokeHand.IsBusy||EditorApplication.timeSinceStartup<nextSmokeAction)return false;
            var mission=em.GetComponentData<CampaignMissionSplitFrontState>(root);var launcher=mission.Launcher;
            var view=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            var launcherCommand=em.GetComponentData<SplitFrontLauncherCommandState>(launcher);
            if(launcherCommand.Launches!=0)throw new InvalidOperationException("Launcher fired before native Hold evidence");
            if(cancelStage==0)
            {
                if((em.GetComponentData<CampaignMissionDefenseStateComponent>(root).AcknowledgedGuidanceMask&1)!=0){cancelStage=1;return false;}
                SmokeTap(view?.ContinueButton);return false;
            }
            if(cancelStage==1){if(UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)&&command.ActiveCommandMode==TacticalCommandMode.Select)cancelStage=2;else SmokeTapNamed("SelectCommand");return false;}
            if(cancelStage==2){if(SmokeTap(view?.ShowMeButton)){cancelStage=3;nextSmokeAction=EditorApplication.timeSinceStartup+3;}return false;}
            if(cancelStage==3){if(em.HasComponent<SelectedUnitTag>(launcher)){cancelStage=4;return false;}TapLauncherWorld(em,launcher);return false;}
            if(cancelStage==4){if(UiShellRuntimeGateway.TryReadMatchHudCommandState(out var command)&&command.ActiveCommandMode==TacticalCommandMode.Attack)cancelStage=5;else SmokeTapNamed("AttackCommand");return false;}
            if(cancelStage==5){if(SmokeTap(view?.ShowMeButton)){cancelStage=6;nextSmokeAction=EditorApplication.timeSinceStartup+3;}return false;}
            if(cancelStage==6){if(TapLauncherWorld(em,mission.Battery))cancelStage=7;return false;}
            if(cancelStage==7)
            {
                var state=em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
                if(launcherCommand.CommandedTarget==Entity.Null||state.Phase!=(byte)GroundMissileLauncherPhase.Preparing)return false;
                Shot("launcher-attack-preparing");
                if(SmokeTapNamed("HoldCommand")){cancelStage=8;nextSmokeAction=EditorApplication.timeSinceStartup+.4;}
                return false;
            }
            if(cancelStage==8)
            {
                if(launcherCommand.CommandedTarget!=Entity.Null||launcherCommand.Stops!=1||launcherCommand.AttackOrders!=1||launcherCommand.Launches!=0||em.GetComponentData<GroundMissileLauncherStateComponent>(launcher).Phase!=(byte)GroundMissileLauncherPhase.Idle||!em.HasComponent<HoldPositionOrderTag>(launcher))
                    throw new InvalidOperationException("Native Hold failed to stop the normal Attack before launch");
                Shot("launcher-hold-stopped");
                Debug.Log("[SplitFrontPlayerHold] result=Passed input=touch attack=direct preparation=stopped-by-Hold target=cleared launches=0 centerPanel=absent");
                FinishOptionalSmokeProbe();cancelStage=13;return true;
            }
            return false;
        }
        private static bool TapLauncherWorld(EntityManager em,Entity entity)
        {
            if(!em.Exists(entity)||!em.HasComponent<LocalTransform>(entity))return false;
            var transform=em.GetComponentData<LocalTransform>(entity);var position=transform.Position;
            if(em.HasComponent<UnitSelectionHitbox>(entity))position=Unity.Mathematics.math.transform(transform.ToMatrix(),em.GetComponentData<UnitSelectionHitbox>(entity).Center);
            var point=Camera.main.WorldToScreenPoint(position);if(point.z<=0||!Screen.safeArea.Contains(point))return false;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            return hits.Count==0&&SmokeGesture(point);
        }
    }
}
