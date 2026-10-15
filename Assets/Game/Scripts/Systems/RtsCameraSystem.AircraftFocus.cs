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
        }
        AircraftLens aircraftLens;
        public bool HasAircraftFocusContext=>aircraftLens!=null;
        public int AircraftFocusZoomLevel=>aircraftLens?.Level??0;
        public bool TryRequestAircraftFocus(EntityManager em,Entity actor,Camera camera,int request,bool requirePaid=true)
        {
            if(camera==null||!OwnAircraft(em,actor,requirePaid,out var owner)||!SkirmishMissionViewProfileApplication.TryResolve(em,out var profile))return false;
            aircraftLens=new AircraftLens{Actor=actor,World=em.World.SequenceNumber,Attempt=owner.SessionId,Stable=owner.StableObjectId.ToString(),Reservation=owner.ReservationId,
                RequiresPaid=requirePaid,Request=request,RequestedFrame=UnityEngine.Time.frameCount,Deadline=UnityEngine.Time.realtimeSinceStartupAsDouble+8,
                GroundHeight=profile.NormalHeight,GroundPitch=profile.Pitch,GroundYaw=profile.Yaw,GroundFov=profile.NormalFieldOfView,MaxDistance=profile.AircraftMaximumDistance};
            aircraftLens.Selection=SelectionFingerprint(em,out _);EmitAircraftFocus("Requested",null,camera);return true;
        }
        public bool TickAircraftFocus(EntityManager em,Camera camera,bool tacticalFollow)
        {
            if(aircraftLens==null)return false;
            var lens=aircraftLens;
            if(camera==null||em.World.SequenceNumber!=lens.World||!OwnAircraft(em,lens.Actor,lens.RequiresPaid,out var owner)||owner.SessionId!=lens.Attempt||
                owner.StableObjectId.ToString()!=lens.Stable||owner.ReservationId!=lens.Reservation)
            {ClearAircraftFocus(camera,"ActorSessionOrWorldChanged",!tacticalFollow);return false;}
            using(var states=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
            {
                if(states.CalculateEntityCount()!=1||states.GetSingleton<RuntimeGameplayStateComponent>().BuildModeActive!=0)
                {ClearAircraftFocus(camera,"GroundBuildContext",!tacticalFollow);return false;}
            }
            ulong selection=SelectionFingerprint(em,out bool ground);
            if(selection!=lens.Selection&&ground){ClearAircraftFocus(camera,"GroundUnitSelection",!tacticalFollow);return false;}
            lens.Selection=selection;
            bool transport=em.GetComponentData<SkirmishUnitRoleComponent>(lens.Actor).Role==Game.Skirmish.Contracts.SkirmishRoleKind.TransportPlane;
            var metadata=transport?SkirmishRigidAircraftGeometryConfig.Load():null;
            if(transport?(metadata==null||!metadata.TryPrepareIndex()):SkirmishCanonicalAircraftGeometryConfig.Load()==null)
            {ClearAircraftFocus(camera,"FramingMetadataUnavailable",!tacticalFollow);return false;}
            if(tacticalFollow)return false; // Existing public Follow owns its pose/restore lifecycle.
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
            if(lens.Applied)return true; // Validate current geometry, but never follow its changing center.
            lens.Center=pose.LookAt;lens.Rotation=pose.DesiredRotation;lens.Forward=lens.Rotation*Vector3.forward;lens.Fov=pose.FieldOfView;
            lens.FitDistance=Vector3.Distance(pose.DesiredPosition,lens.Center);lens.Applied=true;lens.Level=0;
            ClearSmoothFocusTarget();ClearSmoothPerspectiveTarget();IsZoomTransitionActive=false;MatchIntroZoomSettlePending=false;
            ApplyAircraftLens(camera);EmitAircraftFocus("Applied",null,camera);return true;
        }
        public bool ZoomAircraftFocus(EntityManager em,Camera camera,int level)
        {
            if(!TickAircraftFocus(em,camera,false)||aircraftLens==null||!aircraftLens.Applied)return false;
            aircraftLens.Level=Mathf.Clamp(level,-1,1);ApplyAircraftLens(camera);EmitAircraftFocus("Zoomed",null,camera);return true;
        }
        public void ClearAircraftFocus(Camera camera,string reason,bool restoreGround)
        {
            if(aircraftLens==null)return;var prior=aircraftLens;EmitAircraftFocus("Cleared",reason,camera);aircraftLens=null;
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
