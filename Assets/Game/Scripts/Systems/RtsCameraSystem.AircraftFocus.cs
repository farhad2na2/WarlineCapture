using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    [Serializable] public sealed class AircraftProductionFocusFact
    {
        public string stage,reason,attempt,stable,focusOrigin;
        public int frame,requestId,actorIndex,actorVersion,zoomLevel;
        public uint reservation;
        public Vector3 capturedCenter,cameraPosition,cameraEuler;
        public Quaternion cameraRotation;
        public float distance,fieldOfView,desiredFieldOfView,aspect;
        public bool orthographic;
    }
    public static class AircraftProductionFocusEvidence
    {
        public static event Action<AircraftProductionFocusFact> Recorded;
        internal static void Observe(AircraftProductionFocusFact row)
        {
            var listeners=Recorded;if(listeners==null)return;
            foreach(Action<AircraftProductionFocusFact> listener in listeners.GetInvocationList())
                try{listener(row);}catch(Exception error){Debug.LogError("[AircraftProductionFocusEvidence] observerFailed="+error.Message);}
        }
    }
    public sealed partial class RtsCameraSystem
    {
        sealed class AircraftLens
        {
            public Entity Actor;public ulong World;public FixedString64Bytes Attempt;public string Stable;
            public uint Reservation;public int Request,Level;public int RequestedFrame;
            public double Deadline;public bool Applied,WaitingForGeometry;public bool RequiresPaid;
            public ulong Selection;
            public Vector3 Center,Forward;public Quaternion Rotation;
            public float FitDistance,Distance,Fov,MaxDistance,GroundHeight,GroundPitch,GroundYaw,GroundFov;
            // Selected-aircraft follow (owner 2026-11-18): the view keeps the moving airframe,
            // rotor, heading and intended landing patch together until the player leaves it.
            public double RequestedAt,LastFollowAt;public bool HasMeasured,HasPatch,PatchFramed;
            public Bounds LastMeasured;public Vector3 LastMeasuredActor,Patch;public string BoundsSource="None";
        }
        /// <summary>Read-only follow facts for evidence capture (no camera or gameplay effect).</summary>
        public struct AircraftFollowState
        {
            public Entity Actor;public bool Applied,Following,HasLandingPatch,LandingPatchFramed;public string Origin,BoundsSource;
            public Vector3 LookAt,LandingPatch;public float Distance,FitDistance;public int Level;
        }
        // A selected aircraft whose measured geometry is not readable yet (for example a distant
        // Far impostor) is reached with its role-sized box after this grace, instead of never.
        public const double AircraftNominalGraceSeconds=.75d;
        const float AircraftFollowWidenSeconds=.3f,AircraftFollowNarrowSeconds=.8f;
        AircraftLens aircraftLens;
        public bool HasAircraftFocusContext=>aircraftLens!=null;
        public int AircraftFocusZoomLevel=>aircraftLens?.Level??0;
        public string LastAircraftFocusClearReason{get;private set;}
        public int LastGroundReacquireFrame{get;private set;}=-1;
        public Vector3 LastGroundReacquireCenter{get;private set;}
        public bool TryReadAircraftFollowState(out AircraftFollowState state)
        {
            var lens=aircraftLens;state=default;if(lens==null)return false;
            state=new AircraftFollowState{Actor=lens.Actor,Applied=lens.Applied,Following=lens.Applied&&!lens.RequiresPaid,Origin=lens.RequiresPaid?"PaidProduction":"NormalSelectedOwnActor",
                BoundsSource=lens.BoundsSource,LookAt=lens.Center,LandingPatch=lens.Patch,HasLandingPatch=lens.HasPatch,LandingPatchFramed=lens.PatchFramed,
                Distance=lens.Distance,FitDistance=lens.FitDistance,Level=lens.Level};
            return true;
        }
        public bool TryRequestAircraftFocus(EntityManager em,Entity actor,Camera camera,int request,bool requirePaid=true)
        {
            if(camera==null||!OwnAircraft(em,actor,requirePaid,out var owner)||!SkirmishMissionViewProfileApplication.TryResolve(em,out var profile))return false;
            // Leaving the lens restores the Skirmish tactical ground view, not the closer authored floor.
            var framing=SkirmishMissionViewProfileApplication.ResolveFraming(em,profile);
            aircraftLens=new AircraftLens{Actor=actor,World=em.World.SequenceNumber,Attempt=owner.SessionId,Stable=owner.StableObjectId.ToString(),Reservation=owner.ReservationId,
                RequiresPaid=requirePaid,Request=request,RequestedFrame=UnityEngine.Time.frameCount,Deadline=UnityEngine.Time.realtimeSinceStartupAsDouble+8,
                RequestedAt=UnityEngine.Time.realtimeSinceStartupAsDouble,
                GroundHeight=framing.IsValid?SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(framing.TacticalHeight):profile.NormalHeight,GroundPitch=framing.IsValid?framing.Pitch:profile.Pitch,GroundYaw=framing.IsValid?framing.Yaw:profile.Yaw,GroundFov=framing.IsValid?framing.FieldOfView:profile.NormalFieldOfView,MaxDistance=profile.AircraftMaximumDistance};
            aircraftLens.Selection=SelectionFingerprint(em,out _);EmitAircraftFocus("Requested",null,camera);return true;
        }
        public bool TickAircraftFocus(EntityManager em,Camera camera,bool tacticalFollow)
        {
            if(aircraftLens==null)return false;
            var lens=aircraftLens;
            if(camera==null||em.World.SequenceNumber!=lens.World||!OwnAircraft(em,lens.Actor,lens.RequiresPaid,out var owner)||owner.SessionId!=lens.Attempt||
                owner.StableObjectId.ToString()!=lens.Stable||owner.ReservationId!=lens.Reservation)
            {ClearAircraftFocusToGroundSelection(em,camera,"ActorSessionOrWorldChanged",!tacticalFollow);return false;}
            using(var states=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
            {
                if(states.CalculateEntityCount()!=1||states.GetSingleton<RuntimeGameplayStateComponent>().BuildModeActive!=0)
                {ClearAircraftFocus(camera,"GroundBuildContext",!tacticalFollow);return false;}
            }
            ulong selection=SelectionFingerprint(em,out bool ground);
            if(selection!=lens.Selection&&ground){ClearAircraftFocusToGroundSelection(em,camera,"GroundUnitSelection",!tacticalFollow);return false;}
            lens.Selection=selection;
            bool transport=em.GetComponentData<SkirmishUnitRoleComponent>(lens.Actor).Role==Game.Skirmish.Contracts.SkirmishRoleKind.TransportPlane;
            var metadata=transport?SkirmishRigidAircraftGeometryConfig.Load():null;
            if(transport?(metadata==null||!metadata.TryPrepareIndex()):SkirmishCanonicalAircraftGeometryConfig.Load()==null)
            {ClearAircraftFocus(camera,"FramingMetadataUnavailable",!tacticalFollow);return false;}
            if(tacticalFollow)return false; // Existing public Follow owns its pose/restore lifecycle.
            if(!lens.RequiresPaid)return TickSelectedAircraftFollow(em,camera,lens);
            var pose=default(TacticalFollowCameraPoseComponent);
            if(em.HasComponent<UnitRenderVisualComponent>(lens.Actor)&&em.GetComponentData<UnitRenderVisualComponent>(lens.Actor).Desired==(byte)UnitRenderVisualKind.Far)
            {ClearAircraftFocus(camera,"AircraftUnexpectedDesiredFarVisual",lens.Applied);return false;}
            if(!SkirmishMissionViewProfileApplication.TryApplyAircraftFollowPose(em,lens.Actor,camera,ref pose))
            {
                if(!lens.Applied&&!lens.WaitingForGeometry)
                {
                    lens.WaitingForGeometry=true;
                    EmitAircraftFocus("WaitingForVisibleGeometry","NormalForcedDetailInitializationPending_NoCameraMutation",camera);
                }
                if(lens.Applied||UnityEngine.Time.realtimeSinceStartupAsDouble>lens.Deadline)ClearAircraftFocus(camera,"VisibleGeometryNotReadyBeforeDeadline",lens.Applied);
                return false;
            }
            // Paid production focus stays a one-shot acquisition view of the delivered aircraft.
            if(lens.Applied)return true; // Validate current geometry, but never follow its changing center.
            lens.Center=pose.LookAt;lens.Rotation=pose.DesiredRotation;lens.Forward=lens.Rotation*Vector3.forward;lens.Fov=pose.FieldOfView;
            lens.FitDistance=Vector3.Distance(pose.DesiredPosition,lens.Center);lens.Applied=true;lens.Level=0;lens.BoundsSource="Measured";
            ClearSmoothFocusTarget();ClearSmoothPerspectiveTarget();IsZoomTransitionActive=false;MatchIntroZoomSettlePending=false;
            ApplyAircraftLens(camera);EmitAircraftFocus("Applied",null,camera);return true;
        }

        // Selected own aircraft: follow it every tick. Earlier this lens was one-shot ("never follow
        // its changing center"), so a flying helicopter left a fixed camera behind; and its strict
        // visible-geometry gate kept a distant aircraft (Far impostor / LOD transition) from ever
        // applying, so the lens timed out without moving the camera at all.
        bool TickSelectedAircraftFollow(EntityManager em,Camera camera,AircraftLens lens)
        {
            double now=UnityEngine.Time.realtimeSinceStartupAsDouble;
            Vector3 actorPosition=em.GetComponentData<Unity.Transforms.LocalTransform>(lens.Actor).Position;
            Game.Configs.SkirmishMissionViewProfile profile;Bounds bounds;string source;
            if(SkirmishMissionViewProfileApplication.TryReadAircraftFollowBounds(em,lens.Actor,out profile,out bounds))
            {source="Measured";lens.HasMeasured=true;lens.LastMeasured=bounds;lens.LastMeasuredActor=actorPosition;}
            else if(lens.HasMeasured&&SkirmishMissionViewProfileApplication.TryResolve(em,out profile))
            {source="CarriedMeasured";bounds=lens.LastMeasured;bounds.center+=actorPosition-lens.LastMeasuredActor;}
            else if(now-lens.RequestedAt>=AircraftNominalGraceSeconds&&SkirmishMissionViewProfileApplication.TryNominalAircraftBounds(em,lens.Actor,out profile,out bounds))
                source="Nominal";
            else{profile=null;bounds=default;source=null;}
            SkirmishMissionViewProfileApplication.AircraftFollowFrame frame=default;
            if(source==null||!SkirmishMissionViewProfileApplication.TryComposeAircraftFollow(em,lens.Actor,camera,profile,bounds,out frame))
            {
                if(!lens.Applied&&!lens.WaitingForGeometry)
                {
                    lens.WaitingForGeometry=true;
                    EmitAircraftFocus("WaitingForVisibleGeometry","NormalForcedDetailInitializationPending_NoCameraMutation",camera);
                }
                if(lens.Applied)return true; // keep the last followed view for this tick
                if(now>lens.Deadline)ClearAircraftFocus(camera,"VisibleGeometryNotReadyBeforeDeadline",false);
                return false;
            }
            float fit=Vector3.Distance(frame.Position,frame.LookAt);
            lens.Center=frame.LookAt;lens.Rotation=frame.Rotation;lens.Forward=lens.Rotation*Vector3.forward;lens.Fov=frame.FieldOfView;
            lens.HasPatch=frame.HasLandingPatch;lens.Patch=frame.LandingPatch;lens.PatchFramed=frame.LandingPatchFramed;lens.BoundsSource=source;
            if(!lens.Applied)
            {
                lens.FitDistance=fit;lens.Applied=true;lens.Level=0;lens.LastFollowAt=now;
                ClearSmoothFocusTarget();ClearSmoothPerspectiveTarget();IsZoomTransitionActive=false;MatchIntroZoomSettlePending=false;
                ApplyAircraftLens(camera);EmitAircraftFocus("Applied",source,camera);return true;
            }
            // The airframe is tracked exactly; only the framing distance eases (widening faster
            // than narrowing), so a new landing patch or touchdown never snaps the zoom.
            lens.FitDistance=FollowDistance(lens.FitDistance,fit,(float)(now-lens.LastFollowAt));lens.LastFollowAt=now;
            ApplyAircraftLens(camera);return true;
        }

        /// <summary>Eased follow distance; dt 0 (several ticks in one frame) keeps the current value.</summary>
        public static float FollowDistance(float current,float target,float dt)
        {
            if(!float.IsFinite(current)||current<=0f)return target;
            if(!(dt>0f))return current;
            float seconds=target>current?AircraftFollowWidenSeconds:AircraftFollowNarrowSeconds;
            return Mathf.Lerp(current,target,1f-Mathf.Exp(-Mathf.Min(dt,.25f)/seconds));
        }

        /// <summary>
        /// Leaves the aircraft view. When the player has selected own ground units the restored
        /// ground view is centred on them, so switching back from a flight reacquires the squad.
        /// </summary>
        public void ClearAircraftFocusToGroundSelection(EntityManager em,Camera camera,string reason,bool restoreGround)
        {
            var prior=aircraftLens;if(prior==null)return;
            ClearAircraftFocus(camera,reason,restoreGround);
            if(!restoreGround||camera==null||!prior.Applied||!TrySelectedGroundCenter(em,out var center))return;
            camera.transform.position=Game.Configs.SkirmishCameraFraming.CameraForGroundCenter(center,prior.GroundHeight,prior.GroundPitch,prior.GroundYaw);
            ApplyPerspectiveCameraModeInstant(camera,prior.GroundHeight,prior.GroundPitch,prior.GroundYaw,prior.GroundFov);
            LastGroundReacquireFrame=UnityEngine.Time.frameCount;LastGroundReacquireCenter=center;
        }
        static bool TrySelectedGroundCenter(EntityManager em,out Vector3 center)
        {
            center=default;int count=0;var sum=Vector3.zero;
            using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitHealth),typeof(Faction),typeof(Unity.Transforms.LocalTransform));
            using var actors=selected.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)
            {
                if(em.GetComponentData<UnitHealth>(actor).Current<=0||em.GetComponentData<Faction>(actor).Id!=1||
                    em.HasComponent<UnitAirMovement>(actor)||em.HasComponent<Unity.Transforms.Parent>(actor))continue;
                Vector3 position=em.GetComponentData<Unity.Transforms.LocalTransform>(actor).Position;
                if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z))continue;
                sum+=position;count++;
            }
            if(count==0)return false;
            center=sum/count;return true;
        }
        public bool ZoomAircraftFocus(EntityManager em,Camera camera,int level)
        {
            if(!TickAircraftFocus(em,camera,false)||aircraftLens==null||!aircraftLens.Applied)return false;
            aircraftLens.Level=Mathf.Clamp(level,-1,1);ApplyAircraftLens(camera);EmitAircraftFocus("Zoomed",null,camera);return true;
        }
        public void ClearAircraftFocus(Camera camera,string reason,bool restoreGround)
        {
            if(aircraftLens==null)return;var prior=aircraftLens;EmitAircraftFocus("Cleared",reason,camera);aircraftLens=null;LastAircraftFocusClearReason=reason;
            if(restoreGround&&camera!=null&&prior.Applied)ApplyPerspectiveCameraModeInstant(camera,prior.GroundHeight,prior.GroundPitch,prior.GroundYaw,prior.GroundFov);
        }
        public static bool ObserveNormalGroundSelection(EntityManager em,Entity actor)
        {
            if(!em.Exists(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1||
                OwnAircraft(em,actor,false,out _))return false;
            var cameraOwner=em.World.GetExistingSystemManaged<RtsCameraSystem>();
            if(cameraOwner==null||!cameraOwner.HasAircraftFocusContext)return false;
            return em.World.GetOrCreateSystemManaged<RtsCameraRequestSystem>().QueueClearAircraftFocus(em);
        }
        void ApplyAircraftLens(Camera camera)
        {
            var lens=aircraftLens;
            float farthest=Mathf.Min(lens.MaxDistance,lens.FitDistance*1.35f);
            lens.Distance=lens.Level<0?lens.FitDistance:lens.Level>0?farthest:(lens.FitDistance+farthest)*.5f;
            UpdateTacticalFollowPose(camera,lens.Center-lens.Forward*lens.Distance,lens.Center,lens.Fov,0,false,0,true,lens.Rotation);
        }
        void EmitAircraftFocus(string stage,string reason,Camera camera)
        {
            var lens=aircraftLens;if(lens==null)return;
            AircraftProductionFocusEvidence.Observe(new AircraftProductionFocusFact{focusOrigin=lens.RequiresPaid?"PaidProduction":"NormalSelectedOwnActor",stage=stage,reason=reason,attempt=lens.Attempt.ToString(),stable=lens.Stable,reservation=lens.Reservation,
                frame=UnityEngine.Time.frameCount,requestId=lens.Request,actorIndex=lens.Actor.Index,actorVersion=lens.Actor.Version,zoomLevel=lens.Level,capturedCenter=lens.Center,
                distance=lens.Distance,desiredFieldOfView=lens.Fov,fieldOfView=camera!=null?camera.fieldOfView:0,
                cameraPosition=camera!=null?camera.transform.position:default,cameraRotation=camera!=null?camera.transform.rotation:default,cameraEuler=camera!=null?camera.transform.eulerAngles:default,
                aspect=camera!=null?camera.aspect:0,orthographic=camera!=null&&camera.orthographic});
        }
        static bool OwnAircraft(EntityManager em,Entity actor,bool requirePaid,out SkirmishAttemptOwnedComponent owner)
        {
            owner=default;if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||
                !SupportedOwnAircraftRole(em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role)||!em.HasComponent<Faction>(actor)||
                em.GetComponentData<Faction>(actor).Id!=1||!em.HasComponent<UnitHealth>(actor)||em.GetComponentData<UnitHealth>(actor).Current<=0||
                !em.HasComponent<Unity.Transforms.LocalTransform>(actor)||!em.HasComponent<UnitAirMovement>(actor)||!em.HasComponent<UnitGrid>(actor)||!em.HasComponent<UnitMovementBehavior>(actor)||
                em.HasComponent<Unity.Transforms.Parent>(actor)||em.HasComponent<Disabled>(actor)||em.HasComponent<Prefab>(actor))return false;
            var position=em.GetComponentData<Unity.Transforms.LocalTransform>(actor).Position;
            if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z))return false;
            owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            if(owner.FactionId!=1||owner.IsStructure!=0||owner.CapacityReleased!=0||requirePaid&&owner.ReservationId==0||owner.StableObjectId.IsEmpty)return false;
            var nativeRole=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            if((nativeRole.Role!=Game.Skirmish.Contracts.SkirmishRoleKind.TransportPlane||!requirePaid)&&nativeRole.Category!=Game.Skirmish.Contracts.SkirmishPopulationCategory.Air)return false;
            string sourceKey=nativeRole.Role==Game.Skirmish.Contracts.SkirmishRoleKind.TransportPlane?"Unit_Veh_Plane_Transport":SkirmishCanonicalAircraftGeometryConfig.ExpectedKey(nativeRole.Role);
            if((nativeRole.Role!=Game.Skirmish.Contracts.SkirmishRoleKind.TransportPlane||!requirePaid)&&(!em.HasComponent<UnitSourcePrefabKey>(actor)||em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString()!=sourceKey))return false;
            if(!requirePaid&&!em.HasComponent<SelectedUnitTag>(actor))return false;
            using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            return sessions.CalculateEntityCount()==1&&sessions.GetSingleton<SkirmishExpandedSessionComponent>().SessionId==owner.SessionId&&
                sessions.GetSingleton<SkirmishExpandedSessionComponent>().Phase==Game.Skirmish.Contracts.SkirmishSessionPhase.Playing;
        }
        static bool SupportedOwnAircraftRole(Game.Skirmish.Contracts.SkirmishRoleKind role)=>role==Game.Skirmish.Contracts.SkirmishRoleKind.TransportPlane||SkirmishCanonicalAircraftGeometryConfig.Supported(role);
        public static bool ObserveNormalOwnAircraftSelection(EntityManager em,Entity actor)
        {
            if(!em.Exists(actor)||!em.HasComponent<SelectedUnitTag>(actor)||!OwnAircraft(em,actor,false,out _)||!SkirmishMissionViewProfileApplication.TryResolve(em,out _))return false;
            return em.World.GetOrCreateSystemManaged<RtsCameraRequestSystem>().QueueFocusOwnAircraft(em,actor);
        }
        static ulong SelectionFingerprint(EntityManager em,out bool ground)
        {
            ground=false;ulong hash=0;
            using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag),typeof(UnitHealth),typeof(Faction));
            using var actors=selected.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)
            {
                if(em.GetComponentData<UnitHealth>(actor).Current<=0||em.GetComponentData<Faction>(actor).Id!=1)continue;
                hash^=((ulong)(uint)actor.Index*1099511628211UL)^((ulong)(uint)actor.Version*1469598103934665603UL);
                if(!OwnAircraft(em,actor,false,out _))ground=true;
            }
            return hash;
        }
    }
}
