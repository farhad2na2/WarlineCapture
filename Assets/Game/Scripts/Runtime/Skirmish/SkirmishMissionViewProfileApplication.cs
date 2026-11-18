using System;
using System.Collections.Generic;
using Game.Components;
using Game.Rendering.Contracts;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    public static class SkirmishMissionViewProfileApplication
    {
        public static bool TryResolve(EntityManager em, out Game.Configs.SkirmishMissionViewProfile profile)
        {
            profile = null;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
            if (sessions.CalculateEntityCount() != 1) return false;
            var session = sessions.GetSingletonEntity();
            if (em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy != 0) return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null) return false;
            if (Game.Configs.SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup))
            {
                if (!SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var logical)) return false;
                profile = logical.View; return true;
            }

            var catalog = Game.Configs.SkirmishExpansionCatalogConfig.Load();
            var candidate = Game.Configs.SkirmishMissionViewProfile.Load(setup.CatalogId);
            if (catalog == null || !catalog.TryGetDefinition(setup.CatalogId, setup.ContentVersion, out var definition) ||
                candidate == null || !MatchesSource(em,setup,definition,candidate)) return false;
            if(candidate.UsesActiveMapIdentity)
            {
                if(!em.HasComponent<SkirmishLaunchDefinitionRecord>(session))return false;
                var selected=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
                if(selected==null||selected.Definition!=definition||!selected.Matches(setup,em.GetComponentData<SkirmishExpandedSessionComponent>(session)))return false;
            }
            profile = candidate; return true;
        }
        internal static bool MatchesSource(EntityManager em,Game.Configs.SkirmishResolvedSetup setup,
            Game.Configs.SkirmishScenarioDefinitionConfig definition,Game.Configs.SkirmishMissionViewProfile profile)
        {
            if(profile==null)return false;
            if(!profile.UsesActiveMapIdentity)return profile.Matches(setup,definition);
            using var activeQuery=em.CreateEntityQuery(typeof(ActiveOperationMapComponent));
            using var metadataQuery=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            if(activeQuery.CalculateEntityCount()!=1||metadataQuery.CalculateEntityCount()!=1)return false;
            var active=activeQuery.GetSingleton<ActiveOperationMapComponent>();
            var metadata=metadataQuery.GetSingleton<OperationMapMetadataComponent>();
            if(active.Generation<=0||metadata.Generation!=active.Generation||metadata.PhysicalSourceValidated!=1||!metadata.Blob.IsCreated)return false;
            ref var map=ref metadata.Blob.Value;
            return active.OperationMapId.Equals(map.OperationMapId)&&
                profile.MatchesActiveMap(setup,definition,map.OperationMapId.ToString(),map.ContentHash.ToString(),map.SourceIdentityHash.ToString());
        }
        public static void Apply(Game.Configs.SkirmishMissionViewProfile profile, ref SelectionRuntimeConfigStartupSystemHelper.State state)
        {
            if (profile == null || !profile.IsValid) throw new ArgumentException("Invalid logical view profile.");
            // Retain the established platform cap without touching shared assets.
            state.MinZoomHeight = Mathf.Min(profile.MinHeight, SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(profile.MaxHeight));
            state.MaxZoomHeight = SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(profile.MaxHeight);
            state.NormalModeZoomHeight = Mathf.Clamp(profile.NormalHeight, state.MinZoomHeight, state.MaxZoomHeight);
            state.BuildModeZoomHeight = Mathf.Clamp(profile.BuildHeight, state.NormalModeZoomHeight, state.MaxZoomHeight);
            state.NormalModePitch = state.BuildModePitch = profile.Pitch; state.NormalModeYaw = state.BuildModeYaw = profile.Yaw;
            state.NormalModeFieldOfView = profile.NormalFieldOfView; state.BuildModeFieldOfView = profile.BuildFieldOfView;
        }

        // ---- Skirmish-wide presentation framing (camera worker 2026-11-17) ----
        // Owner request: wider default ground view, helicopter follow that shows the
        // ground, and a composed mission opening. The authored profile above stays the
        // reviewed engineering floor; these helpers never touch orders, selection,
        // gameplay data or any fingerprinted asset.
        static readonly Dictionary<string, Game.Configs.SkirmishOpeningCameraConfig> openingConfigs = new(StringComparer.Ordinal);
        const float FallbackLandscapeAspect = 2400f / 1080f;

        static bool TryReadSessionLayout(EntityManager em, out string catalogId, out Game.Configs.SkirmishMapLayoutConfig layout, out Entity session)
        {
            catalogId = null; layout = null; session = Entity.Null;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
            if (sessions.CalculateEntityCount() != 1) return false;
            session = sessions.GetSingletonEntity();
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup == null) return false;
            catalogId = setup.CatalogId;
            if (em.HasComponent<SkirmishLaunchDefinitionRecord>(session))
            {
                var record = em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
                if (record?.Definition?.MapLayout != null && record.Definition.MapLayout.LayoutId == setup.LayoutId) layout = record.Definition.MapLayout;
            }
            return true;
        }

        public static Game.Configs.SkirmishOpeningCameraConfig LoadOpeningCamera(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return null;
            if (!openingConfigs.TryGetValue(catalogId, out var config))
                openingConfigs[catalogId] = config = Game.Configs.SkirmishOpeningCameraConfig.Load(catalogId);
            return config;
        }

        /// <summary>Opening config matching the live session's exact layout, or null.</summary>
        public static Game.Configs.SkirmishOpeningCameraConfig ResolveOpeningCamera(EntityManager em, out Game.Configs.SkirmishMapLayoutConfig layout)
        {
            layout = null;
            if (!TryReadSessionLayout(em, out var catalogId, out layout, out _)) return null;
            var config = LoadOpeningCamera(catalogId);
            return config != null && config.Matches(catalogId, layout) ? config : null;
        }

        /// <summary>Framing for an already resolved/validated profile in the live session.</summary>
        public static Game.Configs.SkirmishGroundFraming ResolveFraming(EntityManager em, Game.Configs.SkirmishMissionViewProfile profile)
        {
            var opening = ResolveOpeningCamera(em, out var layout);
            float? yaw = null;
            if (opening != null && opening.TryResolveYaw(layout, out float missionYaw)) yaw = missionYaw;
            return Game.Configs.SkirmishCameraFraming.Resolve(profile, yaw);
        }

        public static bool TryResolveFraming(EntityManager em, out Game.Configs.SkirmishGroundFraming framing)
        {
            framing = default;
            if (!TryResolve(em, out var profile) || profile == null || !profile.IsValid) return false;
            framing = ResolveFraming(em, profile);
            return framing.IsValid;
        }

        public static void ApplyFraming(in Game.Configs.SkirmishGroundFraming framing, ref SelectionRuntimeConfigStartupSystemHelper.State state)
        {
            if (!framing.IsValid) return;
            state.MaxZoomHeight = SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(framing.WideHeight);
            state.MinZoomHeight = Mathf.Min(framing.CloseHeight, state.MaxZoomHeight);
            state.NormalModeZoomHeight = Mathf.Clamp(framing.TacticalHeight, state.MinZoomHeight, state.MaxZoomHeight);
            state.BuildModeZoomHeight = Mathf.Clamp(framing.BuildHeight, state.NormalModeZoomHeight, state.MaxZoomHeight);
            state.NormalModePitch = state.BuildModePitch = framing.Pitch; state.NormalModeYaw = state.BuildModeYaw = framing.Yaw;
            state.NormalModeFieldOfView = framing.FieldOfView; state.BuildModeFieldOfView = framing.BuildFieldOfView;
        }

        /// <summary>
        /// One-shot opening camera request values for the actual own opening group. A
        /// matching opening config composes group + landmarks; otherwise the group is
        /// centred at the tactical height. Camera presentation only.
        /// </summary>
        public static bool TryResolveOpeningRequest(EntityManager em, Game.Configs.SkirmishMissionViewProfile profile, Vector3 group,
            out Vector3 groundRequest, out Unity.Mathematics.float4 perspective, out bool composed)
        {
            groundRequest = default; perspective = default; composed = false;
            if (profile == null) return false;
            var opening = ResolveOpeningCamera(em, out var layout);
            float? yaw = null;
            if (opening != null && opening.TryResolveYaw(layout, out float missionYaw)) yaw = missionYaw;
            var framing = Game.Configs.SkirmishCameraFraming.Resolve(profile, yaw);
            if (!framing.IsValid) return false;
            var camera = Camera.main;
            float aspect = camera != null && camera.aspect > 0f && float.IsFinite(camera.aspect) ? camera.aspect : FallbackLandscapeAspect;
            float height = SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(framing.TacticalHeight);
            bool hasBase = false; Vector2 baseXZ = default;
            if (opening != null && opening.FrameActualPlayerBase && TryReadActualPlayerBase(em, out var basePosition))
            { hasBase = true; baseXZ = new Vector2(basePosition.x, basePosition.z); }
            if (opening != null && opening.TryResolveFramePoints(layout, hasBase, baseXZ, out var landmarks) &&
                Game.Configs.SkirmishCameraFraming.TryComposeOpening(framing, group, landmarks, aspect, opening.OpeningViewport, out var eye, out bool allFramed))
            {
                float capped = SelectionRuntimeConfigStartupSystemHelper.ClampAuthoredFocusHeight(eye.y);
                if (capped < eye.y)
                {
                    // Keep the composed ground centre when a platform height cap applies.
                    var centre = Game.Configs.SkirmishCameraFraming.ZeroPlaneGroundCenter(eye, framing.Pitch, framing.Yaw);
                    eye = Game.Configs.SkirmishCameraFraming.CameraForGroundCenter(centre, capped, framing.Pitch, framing.Yaw);
                }
                groundRequest = Game.Configs.SkirmishCameraFraming.ZeroPlaneGroundCenter(eye, framing.Pitch, framing.Yaw);
                perspective = new Unity.Mathematics.float4(eye.y, framing.Pitch, framing.Yaw, framing.FieldOfView);
                composed = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[SkirmishCameraFraming] opening=Composed catalog=" + opening.CatalogId + " allFramed=" + (allFramed ? 1 : 0) +
                    " group=" + group + " landmarks=" + landmarks.Length + (hasBase ? " playerBase=" + baseXZ : "") + " height=" + eye.y.ToString("F2") +
                    " aboveGround=" + (eye.y - framing.GroundY).ToString("F2") + " pitch=" + framing.Pitch + " yaw=" + framing.Yaw.ToString("F1") +
                    " fov=" + framing.FieldOfView + " aspect=" + aspect.ToString("F3") + " groundRequest=" + groundRequest +
                    " viewport=" + (opening.FrameInsideHudPlayArea ? "HudPlayArea" : "Legacy") + " orders=0");
#endif
                return true;
            }
            if (!SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(group, height, framing.Pitch, framing.Yaw, out groundRequest)) return false;
            perspective = new Unity.Mathematics.float4(height, framing.Pitch, framing.Yaw, framing.FieldOfView);
            return true;
        }

        /// <summary>
        /// The live player base of this attempt (its PlayerBase objective-role structure): the
        /// player's own structure, public by definition. Read only; no selection, no orders.
        /// </summary>
        public static bool TryReadActualPlayerBase(EntityManager em, out Vector3 position)
        {
            position = default;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (sessions.CalculateEntityCount() != 1) return false;
            var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(sessions.GetSingletonEntity()).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishObjectiveRoleComponent), typeof(SkirmishAttemptOwnedComponent), typeof(LocalTransform));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            int found = 0;
            foreach (var entity in entities)
            {
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(attempt) ||
                    em.GetComponentData<SkirmishObjectiveRoleComponent>(entity).Role != SkirmishObjectiveRoleKind.PlayerBase) continue;
                Vector3 candidate = em.GetComponentData<LocalTransform>(entity).Position;
                if (!Finite(candidate)) return false;
                position = candidate; found++;
            }
            return found == 1;
        }

        // Called only by the existing public follow-mode pose owner. Uses the
        // existing Rendering.Contracts gateway; no Runtime->Rendering dependency.
        private static bool TryAircraftBounds(EntityManager em, Entity actor, out Game.Configs.SkirmishMissionViewProfile profile, out Bounds combined)
        {
            profile = null; combined = default;
            if (!TryResolve(em, out profile) || !em.Exists(actor) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(actor) || !em.HasComponent<SkirmishUnitRoleComponent>(actor) ||
                !SupportedAircraft(profile,em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role) ||
                !em.HasComponent<UnitHealth>(actor) || em.GetComponentData<UnitHealth>(actor).Current <= 0 ||
                RenderBoundsGateway.Provider == null) return false;
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            if (sessions.CalculateEntityCount() != 1) return false;
            var sessionEntity = sessions.GetSingletonEntity();
            var session = em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            if ((profile.AuthoredNonBaseAssault || profile.AuthoredGenericBaseAssault) &&
                (!SkirmishMissionLogicalBindingProvider.TryRead(em,sessionEntity,out var logical) ||
                 !logical.TryReadFraming(em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role,out _))) return false;
            if (owner.FactionId != 1 || !owner.SessionId.Equals(session.SessionId) || !em.HasComponent<UnitRenderVisualComponent>(actor)) return false;
            var visual = em.GetComponentData<UnitRenderVisualComponent>(actor);
            if (visual.Current != visual.Desired) return false;
            Entity root = Entity.Null;
            if (visual.Current == (byte)UnitRenderVisualKind.Detail)
            {
                if (em.HasComponent<UnitDetailedVisualReference>(actor)) root = em.GetComponentData<UnitDetailedVisualReference>(actor).Root;
                else if (em.HasComponent<UnitModelInstanceReference>(actor)) root = em.GetComponentData<UnitModelInstanceReference>(actor).Instance;
            }
            else if (visual.Current == (byte)UnitRenderVisualKind.Mid && em.HasComponent<UnitMidLodInstanceReference>(actor)) root = em.GetComponentData<UnitMidLodInstanceReference>(actor).Instance;
            else if (visual.Current == (byte)UnitRenderVisualKind.Low && em.HasComponent<UnitLowLodInstanceReference>(actor)) root = em.GetComponentData<UnitLowLodInstanceReference>(actor).Instance;
            if (root == Entity.Null || !em.Exists(root)) return false;
            if(em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role==SkirmishRoleKind.TransportPlane)
                return TryReadRigidAircraftVisibleHierarchy(em,root,RenderBoundsGateway.Provider as IRenderVisibleGeometryPresentation,out combined);
            return TryReadCanonicalAircraftVisibleHierarchy(em,actor,root,RenderBoundsGateway.Provider as IRenderAircraftVisibleGeometryPresentation,out combined);
        }

        public static bool RequiresRigidAircraftFraming(EntityManager em,Entity actor)=>em.Exists(actor)&&
            em.HasComponent<SkirmishUnitRoleComponent>(actor)&&SkirmishRoleIds.Category(em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role)==SkirmishPopulationCategory.Air&&TryResolve(em,out _);

        public static bool TryReadRigidAircraftVisibleHierarchy(EntityManager em,Entity root,IRenderVisibleGeometryPresentation provider,out Bounds combined)
        {
            combined=default;if(provider==null||root==Entity.Null||!em.Exists(root))return false;
            var visited=new HashSet<Entity>();var pending=new Queue<Entity>();pending.Enqueue(root);bool found=false;
            while(pending.Count>0)
            {
                var entity=pending.Dequeue();if(!visited.Add(entity))continue;
                if(visited.Count>4096||!em.Exists(entity))return false;
                if(em.HasComponent<Disabled>(entity))continue;
                if(em.HasBuffer<LinkedEntityGroup>(entity))foreach(var child in em.GetBuffer<LinkedEntityGroup>(entity,true))pending.Enqueue(child.Value);
                if(em.HasBuffer<Unity.Transforms.Child>(entity))foreach(var child in em.GetBuffer<Unity.Transforms.Child>(entity,true))pending.Enqueue(child.Value);
                if(!provider.TryReadVisibleGeometryBounds(em,entity,out var bounds,out bool hasDraw,out _))return false;
                if(!hasDraw)continue;
                if(!Finite(bounds.center)||!Finite(bounds.extents)||bounds.extents.x<0||bounds.extents.y<0||bounds.extents.z<0)return false;
                if(!found)combined=bounds;else combined.Encapsulate(bounds);found=true;
            }
            return found;
        }

        public static bool TryApplyAircraftFollowPose(EntityManager em, Entity actor, Camera camera, ref TacticalFollowCameraPoseComponent pose)
        {
            if (camera == null || !TryAircraftBounds(em,actor,out var profile,out var combined)) return false;
            bool transport = em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role==SkirmishRoleKind.TransportPlane;
            float minimum = transport ? profile.AircraftMinimumDistance : camera.nearClipPlane*2+combined.extents.magnitude;
            // A mission yaw keeps the aircraft view on the same screen-up as the ground
            // view, so switching back to infantry never swings the camera around.
            var framing = ResolveFraming(em, profile);
            float yaw = framing.IsValid && framing.HasMissionYaw ? framing.Yaw : profile.EffectiveAircraftYaw;
            float fraction = transport ? profile.PlaneMaximumViewportDimension :
                Mathf.Min(profile.PlaneMaximumViewportDimension, Game.Configs.SkirmishCameraFraming.AircraftMaximumViewportFraction);
            Vector2 heading = default; bool airborne = combined.min.y - profile.GroundReferenceY > 3f;
            if (em.HasComponent<LocalTransform>(actor))
            {
                var forward = math.mul(em.GetComponentData<LocalTransform>(actor).Rotation, new float3(0, 0, 1));
                heading = new Vector2(forward.x, forward.z);
            }
            if (em.HasComponent<UnitAirComponent>(actor)) airborne = em.GetComponentData<UnitAirComponent>(actor).Airborne != 0 || airborne;
            Vector3 position, lookAt; Quaternion rotation;
            if (!Game.Configs.SkirmishCameraFraming.TryAircraftPose(combined, profile.GroundReferenceY, heading, airborne, profile.AircraftPitch, yaw,
                    profile.AircraftFieldOfView, fraction, camera.aspect, camera.nearClipPlane, minimum, profile.AircraftMaximumDistance,
                    out position, out rotation, out lookAt))
            {
                // Established bounds-only fit and its hard maximum-distance refusal.
                if (!TryAircraftPose(combined,profile,camera.aspect,camera.nearClipPlane,minimum,yaw,out position,out rotation)) return false;
                lookAt = combined.center;
            }
            pose.DesiredPosition = position; pose.DesiredRotation = new Unity.Mathematics.quaternion(rotation.x,rotation.y,rotation.z,rotation.w);
            pose.LookAt = lookAt; pose.FieldOfView = profile.AircraftFieldOfView; pose.Orthographic = 0; pose.OrthographicSize = 0;
            return true;
        }

        // Exact perspective fit for measured world AABB, centered on that actual
        // aircraft at its actual altitude. No orders, camera mutation or hidden data.
        public static bool TryAircraftPose(Bounds bounds, Game.Configs.SkirmishMissionViewProfile profile,
            float aspect, float nearClip, out Vector3 position, out Quaternion rotation)
        {
            return TryAircraftPose(bounds,profile,aspect,nearClip,profile!=null?profile.AircraftMinimumDistance:0,profile!=null?profile.EffectiveAircraftYaw:0,out position,out rotation);
        }
        static bool TryAircraftPose(Bounds bounds,Game.Configs.SkirmishMissionViewProfile profile,float aspect,float nearClip,float minimumDistance,float yaw,out Vector3 position,out Quaternion rotation)
        {
            position = default; rotation = default;
            if (!float.IsFinite(minimumDistance)||minimumDistance<=0||profile == null || !profile.IsValid || !float.IsFinite(aspect) || aspect <= 0 || !float.IsFinite(nearClip) || nearClip <= 0 ||
                !Finite(bounds.center) || !Finite(bounds.extents) || bounds.extents.x <= 0 || bounds.extents.y <= 0 || bounds.extents.z <= 0) return false;
            if (!float.IsFinite(yaw)) return false;
            rotation = Quaternion.Euler(profile.AircraftPitch, yaw, 0);
            Vector3 right = rotation * Vector3.right, up = rotation * Vector3.up, forward = rotation * Vector3.forward;
            float tangent = Mathf.Tan(profile.AircraftFieldOfView * Mathf.Deg2Rad * .5f);
            float fraction = profile.PlaneMaximumViewportDimension;
            float distance = minimumDistance;
            for (int i = 0; i < 8; i++)
            {
                var point = Vector3.Scale(bounds.extents, new Vector3((i&1)==0?-1:1, (i&2)==0?-1:1, (i&4)==0?-1:1));
                float depth = Vector3.Dot(point, forward);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(point,right))/(tangent*aspect*fraction)-depth);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(point,up))/(tangent*fraction)-depth);
                distance = Mathf.Max(distance, nearClip * 2 - depth);
            }
            if (!float.IsFinite(distance) || distance > profile.AircraftMaximumDistance) return false;
            position = bounds.center - forward * distance;
            return Finite(position);
        }
        private static bool SupportedAircraft(Game.Configs.SkirmishMissionViewProfile profile,SkirmishRoleKind role)=>SkirmishRoleIds.Category(role)==SkirmishPopulationCategory.Air;
        static bool TryReadCanonicalAircraftVisibleHierarchy(EntityManager em,Entity actor,Entity root,IRenderAircraftVisibleGeometryPresentation provider,out Bounds combined)
        {
            combined=default;if(provider==null||root==Entity.Null||!em.Exists(root))return false;
            var seen=new HashSet<Entity>();var queue=new Queue<Entity>();queue.Enqueue(root);bool found=false;
            while(queue.Count>0)
            {
                var node=queue.Dequeue();if(!seen.Add(node))continue;if(seen.Count>4096||!em.Exists(node))return false;
                if(em.HasComponent<Disabled>(node))continue;
                if(em.HasBuffer<LinkedEntityGroup>(node))foreach(var child in em.GetBuffer<LinkedEntityGroup>(node,true))queue.Enqueue(child.Value);
                if(em.HasBuffer<Unity.Transforms.Child>(node))foreach(var child in em.GetBuffer<Unity.Transforms.Child>(node,true))queue.Enqueue(child.Value);
                if(!provider.TryReadAircraftVisibleGeometryBounds(em,actor,node,out var bounds,out bool drawn,out _))return false;
                if(!drawn)continue;if(!Finite(bounds.center)||!Finite(bounds.extents)||bounds.extents.x<0||bounds.extents.y<0||bounds.extents.z<0)return false;
                if(!found)combined=bounds;else combined.Encapsulate(bounds);found=true;
            }
            return found;
        }
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
