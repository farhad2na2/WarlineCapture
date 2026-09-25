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
            // Replay keeps the loaded scene, whose one-time loading gate has
            // already completed. Arm only this pending fresh attempt after spawn;
            // an ordinary pause or terminal state must never be resumed here.
            if (EntityManager.HasComponent<SkirmishExpandedReplayStartPending>(session) &&
                match.Phase == SkirmishPhase.Playing && match.StartupFailure == SkirmishStartupFailureCode.None)
            {
                var replayScene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
                if (replayScene != null && replayScene.GameplayStartComplete &&
                    SkirmishLaunchProjection.TryArmExpandedSimulation(EntityManager))
                    EntityManager.RemoveComponent<SkirmishExpandedReplayStartPending>(session);
            }
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
            bool s002Session = EntityManager.HasComponent<SkirmishExpandedSessionComponent>(session) &&
                EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(session).CatalogId.ToString() ==
                SkirmishBattleCatalogConfig.DesertBaseEstablishedScenarioId;
            if(!startupFailed&&UiShellRuntimeGateway.TryReadShellState(out var shell)&&shell.CurrentMode==UiShellMode.MatchHud&&!shell.IsTransitionRunning&&focusedSession!=match.SessionId.ToString())
            {if(Focus(match.PlayerMainBase,true,match.ScenarioIndex,false,s002Session))focusedSession=match.SessionId.ToString();}
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
                if(action==SkirmishAction.FocusPlayer)Focus(match.PlayerMainBase,false,match.ScenarioIndex,false,s002Session);
                else if(action==SkirmishAction.FocusEnemy)Focus(match.EnemyMainBase,false,match.ScenarioIndex,true,s002Session);
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
        private bool Focus(Entity entity,bool opening,int scenarioIndex,bool enemy,bool s002Session)
        {
            if(!EntityManager.Exists(entity)||!EntityManager.HasComponent<LocalTransform>(entity))return false;
            if(opening && EntityManager.HasComponent<SkirmishAttemptOwnedComponent>(entity) &&
               (!EntityManager.HasComponent<SkirmishVisualSpawnedComponent>(entity) ||
                EntityManager.GetComponentData<SkirmishVisualSpawnedComponent>(entity).Spawned==0))return false;
            using var query=EntityManager.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if(query.CalculateEntityCount()!=1)return false;
            bool basinOpening = opening && scenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex;
            bool s002Focus = s002Session;
            float s002Offset = enemy ? -30f : 30f;
            var focusPosition = EntityManager.GetComponentData<LocalTransform>(entity).Position;
            float openingDistance = 40f;
            bool authoredOpening = opening && EntityManager.HasComponent<SkirmishAttemptOwnedComponent>(entity);
            if (authoredOpening)
            {
                // Frame the actual base and nearby deployment. Distant vehicles
                // remain selectable in the tray; they must not shrink the opening
                // army into unreadable dots. Include legal placement offsets.
                // This only computes a camera request and never changes troop orders.
                var owner = EntityManager.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                var minimum = focusPosition;
                var maximum = focusPosition;
                using var army = EntityManager.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),
                    typeof(SkirmishUnitRoleComponent), typeof(UnitHealth), typeof(LocalTransform));
                using var actors = army.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var actor in actors)
                {
                    var candidate = EntityManager.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    if (!candidate.SessionId.Equals(owner.SessionId) || candidate.FactionId != owner.FactionId ||
                        EntityManager.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                    var position = EntityManager.GetComponentData<LocalTransform>(actor).Position;
                    if (Unity.Mathematics.math.distancesq(position.xz, focusPosition.xz) > 80f * 80f) continue;
                    minimum = Unity.Mathematics.math.min(minimum, position);
                    maximum = Unity.Mathematics.math.max(maximum, position);
                }
                focusPosition = (minimum + maximum) * .5f;
                float extent = Unity.Mathematics.math.max(maximum.x - minimum.x, maximum.z - minimum.z);
                openingDistance = Unity.Mathematics.math.clamp(extent * 1.3f + 30f, 60f, 180f);
            }
            EntityManager.SetComponentData(query.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
            {
                Requested=1,UseExplicitPerspective=1,
                // S002 frames the Barracks and its deployment pad on every focus,
                // including ARIA's subsequent public base-focus action.
                Perspective=new Unity.Mathematics.float4(authoredOpening ? openingDistance : s002Focus ? 100 : basinOpening ? 60 : opening ? 40 : 55,58,0,60),
                World=focusPosition+
                    (authoredOpening ? Unity.Mathematics.float3.zero : s002Focus ? new Unity.Mathematics.float3(s002Offset,0,0) :
                        opening ? new Unity.Mathematics.float3(20,0,6) : new Unity.Mathematics.float3(0,0,12))
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
                if(SkirmishLaunchProjection.TryGet(EntityManager,out var session,out _) &&
                    EntityManager.HasComponent<SkirmishExpandedSessionComponent>(session) &&
                    !EntityManager.HasComponent<SkirmishExpandedCleanupRequest>(session))
                    EntityManager.AddComponent<SkirmishExpandedCleanupRequest>(session);
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
