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
        private string startupSession;
        private double startupBeganAt;
        protected override void OnUpdate()
        {
            using var returns=EntityManager.CreateEntityQuery(typeof(SkirmishReturnRequest));
            if(returns.CalculateEntityCount()==1)
            {
                if(SkirmishLaunchProjection.TryGet(EntityManager,out var liveSession,out _) &&
                   SkirmishCheckpointCompositionSystemHelper.TryConsumeExpandedReplayRequest(EntityManager,liveSession))
                    EntityManager.DestroyEntity(returns.GetSingletonEntity());
                else HandleReturn(returns.GetSingletonEntity());
                return;
            }
            if(!SkirmishLaunchProjection.TryGet(EntityManager,out var session,out var match))
            {if(view!=null)UnityEngine.Object.Destroy(view.gameObject);view=null;focusedSession=null;return;}
            if(match.ScenarioIndex==SkirmishPresetConfig.StressScaleProbeScenarioIndex &&
               match.Phase<SkirmishPhase.Playing && match.StartupFailure!=SkirmishStartupFailureCode.Content)
                SkirmishLaunchProjection.DriveStressLaunch(EntityManager);
            ObserveStartup(session, ref match);
            bool startupFailed=match.StartupFailure!=SkirmishStartupFailureCode.None &&
                !(match.ScenarioIndex==SkirmishPresetConfig.StressScaleProbeScenarioIndex &&
                  match.StartupFailure==SkirmishStartupFailureCode.Timeout);
            if(startupFailed)SkirmishStartupPolicy.StopFailedStartup(EntityManager);
            if(match.Phase<SkirmishPhase.Playing&&!startupFailed)return;
            if(view==null)view=SkirmishMatchView.Create();
            if(!startupFailed&&UiShellRuntimeGateway.TryReadShellState(out var shell)&&shell.CurrentMode==UiShellMode.MatchHud&&!shell.IsTransitionRunning&&focusedSession!=match.SessionId.ToString())
            {
                // Latch only after the focus request is actually written. The base
                // transform or the camera boundary can lag the shell entry by a frame;
                // marking focused on an early-out dropped the opening frame forever.
                if(Focus(match.PlayerMainBase,true))focusedSession=match.SessionId.ToString();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var worldCamera = Camera.main;
            bool baseAlive = EntityManager.Exists(match.EnemyMainBase) && EntityManager.HasComponent<UnitHealth>(match.EnemyMainBase) &&
                EntityManager.GetComponentData<UnitHealth>(match.EnemyMainBase).Current > 0 && EntityManager.HasComponent<LocalTransform>(match.EnemyMainBase);
            view.PresentEnemyBaseMarker(baseAlive && worldCamera != null
                ? worldCamera.WorldToScreenPoint(EntityManager.GetComponentData<LocalTransform>(match.EnemyMainBase).Position) : Vector3.back, baseAlive, baseAlive ? (Vector3)EntityManager.GetComponentData<LocalTransform>(match.EnemyMainBase).Position : default, EntityManager.Exists(match.PlayerMainBase) && EntityManager.HasComponent<LocalTransform>(match.PlayerMainBase) ? (Vector3)EntityManager.GetComponentData<LocalTransform>(match.PlayerMainBase).Position : default);
#endif
            var requests=EntityManager.GetBuffer<SkirmishActionRequest>(session);
            if(requests.Length>0)
            {
                var action=requests[0].Action;requests.Clear();
                if(action==SkirmishAction.FocusPlayer)Focus(match.PlayerMainBase,false);
                else if(action==SkirmishAction.FocusEnemy)Focus(match.EnemyMainBase,false);
                else if(action==SkirmishAction.Surrender&&match.Phase==SkirmishPhase.Playing)
                {match.SurrenderRequested=1;EntityManager.SetComponentData(session,match);UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);}
                else if(action>=SkirmishAction.Replay && (match.Phase==SkirmishPhase.Finished||startupFailed||action==SkirmishAction.Restart))
                {
                    if(SkirmishCheckpointCompositionSystemHelper.TryConsumeExpandedReplay(EntityManager,session,action))
                    {
                        UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);
                        return;
                    }
                    var entity=EntityManager.CreateEntity(typeof(SkirmishReturnRequest));
                    EntityManager.SetComponentData(entity,new SkirmishReturnRequest{Action=action,Seed=match.Seed,ScenarioIndex=match.ScenarioIndex});
                    returnStage=0;
                }
            }
            if(SkirmishCheckpointCompositionSystemHelper.TryConsumeExpandedReplayRequest(EntityManager,session))
                UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);
            if(match.Phase==SkirmishPhase.Finished&&match.ResultSaved==0&&UnityEngine.Time.unscaledTime>=retrySaveAt)
            {
                if(SkirmishCheckpointCompositionSystemHelper.TrySettleExpandedPresentation(EntityManager,session))
                {
                    if(EntityManager.HasComponent<SkirmishResultComponent>(session) &&
                       EntityManager.GetComponentData<SkirmishResultComponent>(session).SaveAcknowledged!=0)
                    {
                        match.ResultSaved=1;EntityManager.SetComponentData(session,match);
                    }
                }
                else
                try
                {
                    var save=SaveService.CreateDefault();var data=save.LoadQuickGame();
                    data.lastResult=new SkirmishResultSaveData
                    {
                        sessionId=match.SessionId.ToString(),outcome=match.Outcome.ToString(),reason=match.Reason.ToString(),
                        elapsedSeconds=match.ElapsedSeconds,seed=match.Seed,scenarioIndex=match.ScenarioIndex,
                        playerUnitsLost=match.PlayerUnitsLost,enemyUnitsLost=match.EnemyUnitsLost,
                        playerBuildingsLost=match.PlayerBuildingsLost,enemyBuildingsLost=match.EnemyBuildingsLost
                    };
                    save.SaveQuickGame(data);match.ResultSaved=1;EntityManager.SetComponentData(session,match);
                }
                catch(Exception e){retrySaveAt=UnityEngine.Time.unscaledTime+5;Debug.LogWarning("Skirmish result save failed: "+e.Message);}
            }
        }
        private void ObserveStartup(Entity session, ref SkirmishMatchState match)
        {
            if(match.Phase>=SkirmishPhase.Playing||match.StartupFailure!=SkirmishStartupFailureCode.None)return;
            if(!UiShellRuntimeGateway.TryReadShellState(out var shell)||shell.CurrentMode!=UiShellMode.Loading||shell.ActiveRoute!=UIRoute.Match)return;
            string id=match.SessionId.ToString();
            var scene=UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if(scene!=null&&scene.GameplayStartFailed)
            {
                SkirmishStartupPolicy.Fail(EntityManager,SkirmishStartupFailureCode.Content);
                match=EntityManager.GetComponentData<SkirmishMatchState>(session);
                return;
            }
            // The operation-map import can exceed the startup budget before the
            // prefab registry exists. Spawn cannot mark Playing until that buffer
            // is present, so count the budget only after both are ready.
            double now=UnityEngine.Time.realtimeSinceStartupAsDouble;
            bool mapReady=scene!=null&&scene.OperationMapSourceSceneLoadComplete&&scene.OperationMapContentReady;
            bool registryReady=mapReady&&PrefabRegistryReady();
            string playId=id+"#play";
            if(!registryReady)
            {
                if(!mapReady||startupSession!=id)
                {
                    startupSession=id;
                    startupBeganAt=now;
                    return;
                }
                if(now-startupBeganAt>=300d)
                    SkirmishStartupPolicy.Fail(EntityManager,SkirmishStartupFailureCode.Timeout);
                match=EntityManager.GetComponentData<SkirmishMatchState>(session);
                return;
            }
            if(startupSession!=playId){startupSession=playId;startupBeganAt=now;}
            using var starts=EntityManager.CreateEntityQuery(typeof(MatchStartQueueComponent));
            bool contentFailed=starts.CalculateEntityCount()==1&&starts.GetSingleton<MatchStartQueueComponent>().LastStatus==MatchStartStatusKind.Failed;
            contentFailed|=scene!=null&&(!string.IsNullOrEmpty(scene.OperationMapContentFailure)||scene.GameplayStartFailed);
            if(contentFailed)SkirmishStartupPolicy.Fail(EntityManager,SkirmishStartupFailureCode.Content);
            else if(UnityEngine.Time.realtimeSinceStartupAsDouble-startupBeganAt>=SkirmishStartupPolicy.TimeoutSeconds &&
                    match.ScenarioIndex!=SkirmishPresetConfig.StressScaleProbeScenarioIndex)
                SkirmishStartupPolicy.Fail(EntityManager,SkirmishStartupFailureCode.Timeout);
            match=EntityManager.GetComponentData<SkirmishMatchState>(session);
        }
        private bool PrefabRegistryReady()
        {
            using var prefabs=EntityManager.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            return !prefabs.IsEmptyIgnoreFilter;
        }
        private bool Focus(Entity entity,bool opening)
        {
            if(!EntityManager.Exists(entity)||!EntityManager.HasComponent<LocalTransform>(entity))return false;
            using var query=EntityManager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if(query.CalculateEntityCount()!=1)return false;
            using var matches = EntityManager.CreateEntityQuery(typeof(SkirmishMatchState));
            bool basinOpening = opening && matches.CalculateEntityCount() == 1 &&
                matches.GetSingleton<SkirmishMatchState>().ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex;
            EntityManager.SetComponentData(query.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
            {
                Requested=1,UseExplicitPerspective=1,
                // Start close to the troops. An explicit base lookup frames the
                // whole Barracks and its defenses below the objective strip.
                Perspective=new Unity.Mathematics.float4(basinOpening ? 60 : opening ? 40 : 55,58,0,60),
                World=EntityManager.GetComponentData<LocalTransform>(entity).Position+
                    (opening?new Unity.Mathematics.float3(20,0,6):new Unity.Mathematics.float3(0,0,12))
            });
            return true;
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
                var config=QuickGameConfig.Defaults;config.MapSeed=request.Seed;config.ScenarioIndex=request.ScenarioIndex;
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
