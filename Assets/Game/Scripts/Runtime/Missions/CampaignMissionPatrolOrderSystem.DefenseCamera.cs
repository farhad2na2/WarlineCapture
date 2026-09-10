using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionPatrolOrderSystem
    {
        private EntityQuery defenseTourQuery,cameraSnapshotQuery,rtsCameraStateQuery;
        private void CreateDefenseCameraQueries(ref SystemState state)
        {
            defenseTourQuery=state.GetEntityQuery(typeof(CampaignMissionCameraTourState));
            cameraSnapshotQuery=state.GetEntityQuery(ComponentType.ReadOnly<RuntimeCameraSnapshotComponent>());
            rtsCameraStateQuery=state.GetEntityQuery(ComponentType.ReadOnly<RtsCameraStateComponent>());
        }
        private void AdvanceDefenseCamera(ref SystemState state,Entity focusEntity,in RuntimeCameraFocusRequestComponent focus,
            ref CampaignMissionOpeningPresentationComponent opening,ref MissionCameraTourBlob tour,OperationMapMetadataComponent map)
        {
            if(defenseTourQuery.CalculateEntityCount()!=1 || rtsCameraStateQuery.CalculateEntityCount()!=1 ||
                cameraSnapshotQuery.CalculateEntityCount()!=1) return;
            var owner=defenseTourQuery.GetSingletonEntity();
            var camera=defenseTourQuery.GetSingleton<CampaignMissionCameraTourState>();
            if(!camera.SessionToken.Equals(opening.SessionToken)) return;
            var rts=rtsCameraStateQuery.GetSingleton<RtsCameraStateComponent>();
            bool settled=focus.Requested==0 && rts.HasSmoothFocusTarget==0 && rts.HasSmoothPerspectiveTarget==0 &&
                rts.MatchIntroZoomSettlePending==0 && rts.IsZoomTransitionActive==0;
            if(camera.Captured==0)
            {
                var snapshot=cameraSnapshotQuery.GetSingleton<RuntimeCameraSnapshotComponent>();
                if(!settled || !TryCaptureTourStart(snapshot,map.Blob.Value.Grid.Origin.y,out camera.StartFocus,out camera.StartPerspective))
                {opening.ElapsedMilliseconds=0; return;}
                camera.Captured=1; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds;
            }
            int stageTime=opening.ElapsedMilliseconds-camera.StageStartedAtMilliseconds;
            if((camera.SkipRequested!=0 || camera.ReducedMotion!=0) && opening.Stage<5)
            {
                QueueTourCamera(state.EntityManager,focusEntity,camera.StartFocus,camera.StartPerspective,camera.ReducedMotion!=0 ? 0 : .25f);
                opening.Stage=5; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds;
            }
            else switch(opening.Stage)
            {
                case 0 when stageTime>=tour.StartHoldMilliseconds && settled:
                    QueueTourCamera(state.EntityManager,focusEntity,opening.EstablishingFocus,tour.PostPerspective,tour.SmoothTimeSeconds);
                    opening.Stage=1; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds; break;
                case 1 when stageTime>=100 && settled:
                    opening.Stage=2; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds; break;
                case 2 when stageTime>=tour.PostHoldMilliseconds:
                    QueueTourCamera(state.EntityManager,focusEntity,opening.HostileFocus,tour.ApproachPerspective,tour.SmoothTimeSeconds);
                    opening.Stage=3; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds; break;
                case 3 when stageTime>=100 && settled:
                    opening.Stage=4; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds; break;
                case 4 when stageTime>=tour.ApproachHoldMilliseconds:
                    QueueTourCamera(state.EntityManager,focusEntity,camera.StartFocus,camera.StartPerspective,tour.SmoothTimeSeconds);
                    opening.Stage=5; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds; break;
                case 5 when stageTime>=100 && settled:
                    if(camera.ReturnSettled==0) {camera.ReturnSettled=1; camera.StageStartedAtMilliseconds=opening.ElapsedMilliseconds;}
                    else if(stageTime>=tour.ReturnHoldMilliseconds) opening.Stage=6;
                    break;
            }
            state.EntityManager.SetComponentData(owner,camera);
        }
        internal static bool TryCaptureTourStart(in RuntimeCameraSnapshotComponent snapshot,float groundHeight,out float3 focus,out float4 perspective)
        {
            focus=default; perspective=default;
            float3 forward=math.mul(snapshot.Rotation,new float3(0,0,1));
            if(snapshot.IsValid==0 || !math.all(math.isfinite(snapshot.Position)) || forward.y>=-.01f ||
                !math.isfinite(snapshot.Projection.c1.y) || math.abs(snapshot.Projection.c1.y)<.01f) return false;
            focus=snapshot.Position+forward*((groundHeight-snapshot.Position.y)/forward.y);
            // RTS/tour poses are roll-free; derive the same pitch/yaw without a managed Unity API.
            float pitch = math.degrees(math.asin(math.clamp(-forward.y, -1f, 1f)));
            float yaw = math.degrees(math.atan2(forward.x, forward.z));
            if (yaw < 0) yaw += 360f;
            perspective = new float4(snapshot.Position.y, pitch, yaw,
                math.degrees(2 * math.atan(1 / math.abs(snapshot.Projection.c1.y))));
            return math.all(math.isfinite(focus)) && math.all(math.isfinite(perspective));
        }
        private void AdvanceDefenseFinale(ref SystemState state,Entity focusEntity,in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionFinalePresentationComponent finale,ref MissionCameraTourBlob config)
        {
            if(runtime.Phase!=MissionPhaseKind.SecureCorridor || runtime.Outcome!=MissionOutcomeKind.None) return;
            var tour=defenseTourQuery.CalculateEntityCount()==1 ? defenseTourQuery.GetSingleton<CampaignMissionCameraTourState>() : default;
            if(finale.Stage==0)
            {
                // Reuse the opening's protected-post anchor, never move or repair an actor for the shot.
                var opening=SystemAPI.GetSingleton<CampaignMissionOpeningPresentationComponent>();
                if(focusEntity!=Entity.Null && tour.ReducedMotion==0 && tour.FinaleSkipRequested==0)
                    {
                        var target=opening.EstablishingFocus;
                        if(SystemAPI.TryGetSingleton(out CampaignMissionExtractionState extraction) && extraction.SessionToken.Equals(runtime.SessionToken)) target=extraction.DepartureCenter;
                        QueueTourCamera(state.EntityManager,focusEntity,target,config.PostPerspective,.4f);
                    }
                finale.Stage=1; finale.ElapsedMilliseconds=0;
            }
            finale.ElapsedMilliseconds=SaturatingAddMilliseconds(finale.ElapsedMilliseconds,SystemAPI.Time.DeltaTime);
            if(finale.ElapsedMilliseconds<3000 && tour.FinaleSkipRequested==0) return;
            finale.Stage=4;
            var facts=SystemAPI.GetSingletonRW<CampaignMissionAttemptFactsComponent>();
            facts.ValueRW.FinalePresentationComplete=1;
        }
        private void AdvanceDefenseCameraFallbacks(ref SystemState state,in CampaignMissionRuntimeComponent runtime,ref MissionCameraTourBlob config)
        {
            if(runtime.Phase==MissionPhaseKind.FindSquad)
                foreach(var opening in SystemAPI.Query<RefRW<CampaignMissionOpeningPresentationComponent>>())
                {
                    var current=opening.ValueRO;
                    if(current.Stage>=6 || !current.SessionToken.Equals(runtime.SessionToken)) continue;
                    if(defenseTourQuery.CalculateEntityCount()!=1) {current.Stage=6; opening.ValueRW=current; continue;}
                    var camera=defenseTourQuery.GetSingleton<CampaignMissionCameraTourState>();
                    if(!camera.SessionToken.Equals(runtime.SessionToken)) continue;
                    camera.OpeningWatchdogMilliseconds=SaturatingAddMilliseconds(camera.OpeningWatchdogMilliseconds,SystemAPI.Time.DeltaTime);
                    // Readiness still belongs to the mission/map owners. Only the optional presentation times out.
                    if(camera.OpeningWatchdogMilliseconds>=30000)
                    {
                        if(camera.Captured!=0 && _cameraFocusQuery.CalculateEntityCount()==1)
                            QueueTourCamera(state.EntityManager,_cameraFocusQuery.GetSingletonEntity(),camera.StartFocus,camera.StartPerspective,0);
                        current.Stage=6; opening.ValueRW=current;
                    }
                    state.EntityManager.SetComponentData(defenseTourQuery.GetSingletonEntity(),camera);
                }
            if(_cameraFocusQuery.CalculateEntityCount()==1 || runtime.Phase!=MissionPhaseKind.SecureCorridor) return;
            foreach(var finale in SystemAPI.Query<RefRW<CampaignMissionFinalePresentationComponent>>())
            {
                var current=finale.ValueRO;
                if(current.Required==0 || current.Stage>=4 || !current.SessionToken.Equals(runtime.SessionToken)) continue;
                AdvanceDefenseFinale(ref state,Entity.Null,in runtime,ref current,ref config); finale.ValueRW=current;
            }
        }
        private static void QueueTourCamera(EntityManager em,Entity target,float3 world,float4 perspective,float smoothTime)
        {
            em.SetComponentData(target,new RuntimeCameraFocusRequestComponent{Requested=1,Smooth=smoothTime>0 ? (byte)1 : (byte)0,
                UseExplicitPerspective=1,Perspective=perspective,SmoothTimeSeconds=smoothTime,World=world});
        }
        private bool IsOpeningVisible(EntityManager entityManager)
        {
            int renderStateCount = _renderVirtualizationStateQuery.CalculateEntityCount();
            if (renderStateCount > 1)
                return false;
            if (renderStateCount == 1)
            {
                OperationMapRenderVirtualizationStateComponent renderState =
                    entityManager.GetComponentData<OperationMapRenderVirtualizationStateComponent>(
                        _renderVirtualizationStateQuery.GetSingletonEntity());
                if (renderState.Initialized == 0 || renderState.InitialViewApplied == 0)
                    return false;
            }

            // SimulationActive and the applied render state are the neutral runtime readiness
            // boundaries. UI-shell transition state remains owned by composition and UI assemblies.
            return true;
        }

    }
}
