using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Composition
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SkirmishPresentationSystem : SystemBase
    {
        private SkirmishMatchView view;
        private string focusedSession;
        private float retrySaveAt;
        private int returnStage;
        protected override void OnUpdate()
        {
            using var returns=EntityManager.CreateEntityQuery(typeof(SkirmishReturnRequest));
            if(returns.CalculateEntityCount()==1){HandleReturn(returns.GetSingletonEntity());return;}
            if(!SkirmishLaunchProjection.TryGet(EntityManager,out var session,out var match))
            {if(view!=null)UnityEngine.Object.Destroy(view.gameObject);view=null;focusedSession=null;return;}
            if(match.Phase<SkirmishPhase.Playing)return;
            if(view==null)view=SkirmishMatchView.Create();
            if(UiShellRuntimeGateway.TryReadShellState(out var shell)&&shell.CurrentMode==UiShellMode.MatchHud&&!shell.IsTransitionRunning&&focusedSession!=match.SessionId.ToString())
            {Focus(match.PlayerMainBase,true);focusedSession=match.SessionId.ToString();}
            var requests=EntityManager.GetBuffer<SkirmishActionRequest>(session);
            if(requests.Length>0)
            {
                var action=requests[0].Action;requests.Clear();
                if(action==SkirmishAction.FocusPlayer)Focus(match.PlayerMainBase,false);
                else if(action==SkirmishAction.FocusEnemy)Focus(match.EnemyMainBase,false);
                else if(action==SkirmishAction.Surrender&&match.Phase==SkirmishPhase.Playing)
                {match.SurrenderRequested=1;EntityManager.SetComponentData(session,match);UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);}
                else if(action>=SkirmishAction.Replay && (match.Phase==SkirmishPhase.Finished||action==SkirmishAction.Restart))
                {
                    var entity=EntityManager.CreateEntity(typeof(SkirmishReturnRequest));
                    EntityManager.SetComponentData(entity,new SkirmishReturnRequest{Action=action,Seed=match.Seed});
                    returnStage=0;
                }
            }
            if(match.Phase==SkirmishPhase.Finished&&match.ResultSaved==0&&UnityEngine.Time.unscaledTime>=retrySaveAt)
            {
                try
                {
                    var save=SaveService.CreateDefault();var data=save.LoadQuickGame();
                    data.lastResult=new SkirmishResultSaveData
                    {
                        sessionId=match.SessionId.ToString(),outcome=match.Outcome.ToString(),reason=match.Reason.ToString(),
                        elapsedSeconds=match.ElapsedSeconds,seed=match.Seed,
                        playerUnitsLost=match.PlayerUnitsLost,enemyUnitsLost=match.EnemyUnitsLost,
                        playerBuildingsLost=match.PlayerBuildingsLost,enemyBuildingsLost=match.EnemyBuildingsLost
                    };
                    save.SaveQuickGame(data);match.ResultSaved=1;EntityManager.SetComponentData(session,match);
                }
                catch(Exception e){retrySaveAt=UnityEngine.Time.unscaledTime+5;Debug.LogWarning("Skirmish result save failed: "+e.Message);}
            }
        }
        private void Focus(Entity entity,bool opening)
        {
            if(!EntityManager.Exists(entity)||!EntityManager.HasComponent<LocalTransform>(entity))return;
            using var query=EntityManager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if(query.CalculateEntityCount()!=1)return;
            EntityManager.SetComponentData(query.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
            {
                Requested=1,UseExplicitPerspective=1,
                Perspective=new Unity.Mathematics.float4(40,58,0,60),
                World=EntityManager.GetComponentData<LocalTransform>(entity).Position+(opening?new Unity.Mathematics.float3(0,0,6):Unity.Mathematics.float3.zero)
            });
        }
        private void HandleReturn(Entity entity)
        {
            if(view!=null){UnityEngine.Object.Destroy(view.gameObject);view=null;}
            if(!UiShellRuntimeGateway.TryReadShellState(out var shell)||shell.IsTransitionRunning)return;
            var request=EntityManager.GetComponentData<SkirmishReturnRequest>(entity);
            if(returnStage==0)
            {
                UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);returnStage=1;return;
            }
            if(returnStage==1)
            {
                if(shell.Phase==UiShellTransitionPhase.PopupVisible||shell.Phase==UiShellTransitionPhase.ShowingPopup)return;
                if(UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.ReturnToMainMenu,UIRoute.QuickCustomSetup,false))returnStage=2;
                return;
            }
            if(shell.CurrentMode!=UiShellMode.MainMenu||SkirmishLaunchProjection.TryGet(EntityManager,out _,out _))return;
            if(request.Action==SkirmishAction.Replay||request.Action==SkirmishAction.Restart)
            {
                var config=QuickGameConfig.Defaults;config.MapSeed=request.Seed;
                if(!SkirmishLaunchProjection.TryQueue(EntityManager,config))return;
                UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.EnterMatch,UIRoute.Match,false);
            }
            else UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,
                request.Action==SkirmishAction.MainMenu?UIRoute.MainMenu:UIRoute.QuickCustomSetup,false);
            EntityManager.DestroyEntity(entity);returnStage=0;focusedSession=null;
        }
        protected override void OnDestroy(){if(view!=null)UnityEngine.Object.Destroy(view.gameObject);}
    }
}
