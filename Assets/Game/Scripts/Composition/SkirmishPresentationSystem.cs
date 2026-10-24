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
            if (TryConsumePlayerResume()) return;
            using var returns=EntityManager.CreateEntityQuery(typeof(SkirmishReturnRequest));
            if(returns.CalculateEntityCount()==1)
            {
                HandleReturn(returns.GetSingletonEntity());
                return;
            }
            if(!SkirmishLaunchProjection.TryGet(EntityManager,out var session,out var match))
            {if(view!=null)UnityEngine.Object.Destroy(view.gameObject);view=null;focusedSession=null;return;}
            // An explicit fresh launch or Continue waits for native loading.
            // Clear this one-use marker after startup so later pauses stay paused.
            if (EntityManager.HasComponent<SkirmishExpandedReplayStartPending>(session) &&
                match.Phase == SkirmishPhase.Playing && match.StartupFailure == SkirmishStartupFailureCode.None)
            {
                SkirmishLaunchProjection.TryRequestPlay(EntityManager);
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
            {
                if(SkirmishMissionLogicalBindingProvider.OwnsOpeningIntroduction(EntityManager,session)||SkirmishS005OpeningViewPolicy.OwnsIntroduction(EntityManager,session))focusedSession=match.SessionId.ToString();
                else if(Focus(match.PlayerMainBase,true,match.ScenarioIndex,false,s002Session))focusedSession=match.SessionId.ToString();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var worldCamera = Camera.main;
            var publicObjectiveFacts = SkirmishPublicObjectiveProjection.Read(EntityManager, match);
            var knownEnemyPosition = new Vector3(publicObjectiveFacts.EnemyPosition.X, publicObjectiveFacts.EnemyPosition.Y, publicObjectiveFacts.EnemyPosition.Z);
            var knownPlayerPosition = new Vector3(publicObjectiveFacts.PlayerPosition.X, publicObjectiveFacts.PlayerPosition.Y, publicObjectiveFacts.PlayerPosition.Z);
            view.PresentEnemyBaseMarker(publicObjectiveFacts.EnemyObjectiveAlive && worldCamera != null
                ? worldCamera.WorldToScreenPoint(knownEnemyPosition) : Vector3.back, publicObjectiveFacts.EnemyObjectiveAlive,
                knownEnemyPosition, knownPlayerPosition, publicObjectiveFacts.EnemyTargetVisible);
#endif
            var requests=EntityManager.GetBuffer<SkirmishActionRequest>(session);
            if(requests.Length>0)
            {
                var action=requests[0].Action;requests.Clear();
                if(action==SkirmishAction.FocusPlayer)Focus(match.PlayerMainBase,false,match.ScenarioIndex,false,s002Session);
                else if(action==SkirmishAction.FocusEnemy)Focus(match.EnemyMainBase,false,match.ScenarioIndex,true,s002Session);
                else if(action==SkirmishAction.SaveExit)
                    SkirmishNativePlayerSaveService.TrySaveAndExit(EntityManager,session);
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
                        SkirmishNativePlayerSaveService.CompleteAttempt(match.SessionId.ToString(), EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(session).CatalogId.ToString());
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
            var campaignMap=Game.Runtime.SkirmishS004Environment.LoadMapForSession(EntityManager);
            bool s004Focus = SkirmishS004CampaignLighting.Supports(campaignMap);
            float campaignYaw=-35f;
            float campaignHeight=60f;
            if(s004Focus && campaignMap != null)
                foreach(var camera in campaignMap.Cameras)
                    if(camera.CameraId == campaignMap.BattleCameraId && camera.EulerAngles.y != 0)
                    {
                        campaignYaw=camera.EulerAngles.y;
                        campaignHeight=Unity.Mathematics.math.clamp(camera.Position.y,55f,100f);
                    }
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
                    if (EntityManager.GetComponentData<SkirmishUnitRoleComponent>(actor).Category ==
                        Game.Skirmish.Contracts.SkirmishPopulationCategory.LogisticsSupport) continue;
                    var position = EntityManager.GetComponentData<LocalTransform>(actor).Position;
                    if (Unity.Mathematics.math.distancesq(position.xz, focusPosition.xz) > 80f * 80f) continue;
                    minimum = Unity.Mathematics.math.min(minimum, position);
                    maximum = Unity.Mathematics.math.max(maximum, position);
                }
                focusPosition = (minimum + maximum) * .5f;
                float extent = Unity.Mathematics.math.max(maximum.x - minimum.x, maximum.z - minimum.z);
                openingDistance = Unity.Mathematics.math.clamp(extent * 1.3f + 30f, 60f, 180f);
                if(s004Focus) openingDistance=campaignHeight;
            }
            float focusHeight = authoredOpening ? openingDistance : s004Focus ? campaignHeight : s002Focus ? 100 : basinOpening ? 60 : opening ? 40 : 55;
            bool missionViewBound = SkirmishMissionViewProfileApplication.TryResolve(EntityManager, out var missionView);
            if (missionViewBound) focusHeight = missionView.NormalHeight;
            focusHeight = SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(focusHeight);
            EntityManager.SetComponentData(query.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent
            {
                Requested=1,UseExplicitPerspective=1,
                // S002 frames the Barracks and its deployment pad on every focus,
                // including ARIA's subsequent public base-focus action.
                Perspective=missionViewBound ? new Unity.Mathematics.float4(focusHeight,missionView.Pitch,missionView.Yaw,missionView.NormalFieldOfView) :
                    new Unity.Mathematics.float4(focusHeight,s004Focus ? 50 : 58,s004Focus ? campaignYaw : 0,s004Focus ? 50 : 60),
                World=focusPosition+
                    (s004Focus ? (enemy ? Unity.Mathematics.float3.zero : new Unity.Mathematics.float3(-12,0,-8)) :
                        authoredOpening ? Unity.Mathematics.float3.zero : s002Focus ? new Unity.Mathematics.float3(s002Offset,0,0) :
                        opening ? new Unity.Mathematics.float3(20,0,6) : new Unity.Mathematics.float3(0,0,12))
            });
            return true;
        }
        private void HandleReturn(Entity entity)
        {
            if(view!=null){UnityEngine.Object.Destroy(view.gameObject);view=null;}
            if(!UiShellRuntimeGateway.TryReadShellState(out var shell)||shell.IsTransitionRunning)return;
            var request=EntityManager.GetComponentData<SkirmishReturnRequest>(entity);
            if(returnStage==3)
            {
                if(shell.ActiveRoute!=UIRoute.QuickCustomSetup)return;
                bool restored=false;
                foreach(var setupView in UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(FindObjectsInactive.Exclude))
                {
                    if(!setupView.IsBaseAssaultSetup)continue;
                    setupView.PrepareScenarioAdjustment(request.ScenarioIndex,request.Seed);
                    restored=true;
                }
                if(!restored)return;
                EntityManager.DestroyEntity(entity);returnStage=0;focusedSession=null;
                return;
            }
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
                if (EntityManager.HasComponent<SkirmishReplayLaunchRecord>(entity))
                {
                    var launch = EntityManager.GetComponentObject<SkirmishReplayLaunchRecord>(entity);
                    if (!SkirmishExpandedLaunchProjection.TryQueue(EntityManager, launch.Payload, launch.Setup)) return;
                }
                else
                {
                    var config=QuickGameConfig.Defaults;config.MapSeed=request.Seed;config.ScenarioIndex=request.ScenarioIndex;
                    if(!SkirmishLaunchProjection.TryQueue(EntityManager,config))return;
                }
                UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.EnterMatch,UIRoute.Match,false);
            }
            else
            {
                if(!UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,
                    request.Action==SkirmishAction.MainMenu?UIRoute.MainMenu:UIRoute.QuickCustomSetup,false))return;
                if(request.Action==SkirmishAction.AdjustSetup){returnStage=3;return;}
            }
            EntityManager.DestroyEntity(entity);returnStage=0;focusedSession=null;
        }
        private bool TryConsumePlayerResume()
        {
            using var requests = EntityManager.CreateEntityQuery(typeof(SkirmishNativeResumeRequest));
            if (requests.CalculateEntityCount() != 1) return false;
            Entity entity = requests.GetSingletonEntity();
            var request = EntityManager.GetComponentObject<SkirmishNativeResumeRequest>(entity);
            bool failed = UnityEngine.Time.realtimeSinceStartupAsDouble > request.Deadline;
            if (!failed && request.Session == Entity.Null)
            {
                var setup = request.Setup;
                var payload = new Game.Skirmish.Contracts.SkirmishLaunchPayload {
                    SessionId = request.Snapshot.SessionId, CatalogId = setup.CatalogId, DefinitionId = setup.DefinitionId,
                    ContentVersion = setup.ContentVersion, AllConfigHashes = setup.SetupHash.ToString("X8"), MapId = setup.OperationMapId,
                    LayoutId = setup.LayoutId, Seed = setup.Seed, SizeId = setup.SizeId, DifficultyId = setup.DifficultyId };
                var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
                if (!SkirmishExpandedLaunchProjection.TryQueue(EntityManager, payload, setup, preset?.buildingPlacement?.UnitPrefabRegistryConfig)) failed = true;
                else
                {
                    using var sessions = EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
                    request.Session = sessions.GetSingletonEntity();
                    if (!SkirmishNativeCheckpointRecoveryRunner.TryQueue(EntityManager, request.Session, request.Snapshot, setup)) failed = true;
                    else EntityManager.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(request.Session).ResumeAfterCommit = true;
                }
            }
            if (failed)
            {
                if (EntityManager.Exists(request.Session))
                {
                    if (EntityManager.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(request.Session))
                        SkirmishNativeCheckpointTransientState.DestroyCreated(EntityManager,
                            EntityManager.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(request.Session).Transients);
                    SkirmishNativeCheckpointStructureReconstruction.Cancel(EntityManager, request.Session);
                    SkirmishScenarioSpawnSystem.DestroyAttemptOwned(EntityManager, EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(request.Session).SessionId);
                    EntityManager.DestroyEntity(request.Session);
                }
                SkirmishNativePlayerSaveService.ReportResumeFailure(EntityManager);
                Debug.LogWarning("[SkirmishPlayerResume] result=Failed reason=LaunchPreflightOrRouteFailed");
                EntityManager.DestroyEntity(entity); return true;
            }
            if (!UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.EnterMatch, UIRoute.Match, false)) return true;
            Debug.Log("[SkirmishPlayerResume] phase=Queued session=" + request.Snapshot.SessionId);
            EntityManager.DestroyEntity(entity); return true;
        }

        protected override void OnDestroy(){if(view!=null)UnityEngine.Object.Destroy(view.gameObject);}
    }
}
