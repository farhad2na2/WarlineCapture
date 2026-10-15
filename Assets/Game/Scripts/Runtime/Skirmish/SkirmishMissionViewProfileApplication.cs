using System;
using System.Collections.Generic;
using Game.Components;
using Game.Rendering.Contracts;
using Game.Skirmish.Contracts;
using Unity.Entities;
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
            if (setup.CatalogId != "S005") return false;
            var catalog = Game.Configs.SkirmishExpansionCatalogConfig.Load();
            var candidate = Game.Configs.SkirmishMissionViewProfile.Load(setup.CatalogId);
            if (catalog == null || !catalog.TryGetDefinition(setup.CatalogId, out var definition) ||
                candidate == null || !candidate.Matches(setup, definition)) return false;
            profile = candidate; return true;
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
            var visited = new HashSet<Entity>(); var pending = new Queue<Entity>(); pending.Enqueue(root);
            bool found = false;
            while (pending.Count > 0)
            {
                var entity = pending.Dequeue();
                if (!visited.Add(entity) || !em.Exists(entity) || em.HasComponent<Disabled>(entity)) continue;
                if (em.HasBuffer<LinkedEntityGroup>(entity)) foreach (var child in em.GetBuffer<LinkedEntityGroup>(entity)) pending.Enqueue(child.Value);
                if (!RenderBoundsGateway.Provider.TryReadWorldBounds(em, entity, out var bounds)) continue;
                if (!Finite(bounds.center) || !Finite(bounds.extents) || bounds.extents.x <= 0 || bounds.extents.y <= 0 || bounds.extents.z <= 0) return false;
                if (!found) combined = bounds; else combined.Encapsulate(bounds);
                found = true;
            }
            return found;
        }

        public static bool TryApplyAircraftFollowPose(EntityManager em, Entity actor, Camera camera, ref TacticalFollowCameraPoseComponent pose)
        {
            if (camera == null || !TryAircraftBounds(em,actor,out var profile,out var combined) ||
                !TryAircraftPose(combined,profile,camera.aspect,camera.nearClipPlane,out var position,out var rotation)) return false;
            pose.DesiredPosition = position; pose.DesiredRotation = new Unity.Mathematics.quaternion(rotation.x,rotation.y,rotation.z,rotation.w);
            pose.LookAt = combined.center; pose.FieldOfView = profile.AircraftFieldOfView; pose.Orthographic = 0; pose.OrthographicSize = 0;
            return true;
        }

        // Exact perspective fit for measured world AABB, centered on that actual
        // aircraft at its actual altitude. No orders, camera mutation or hidden data.
        public static bool TryAircraftPose(Bounds bounds, Game.Configs.SkirmishMissionViewProfile profile,
            float aspect, float nearClip, out Vector3 position, out Quaternion rotation)
        {
            position = default; rotation = default;
            if (profile == null || !profile.IsValid || !float.IsFinite(aspect) || aspect <= 0 || !float.IsFinite(nearClip) || nearClip <= 0 ||
                !Finite(bounds.center) || !Finite(bounds.extents) || bounds.extents.x <= 0 || bounds.extents.y <= 0 || bounds.extents.z <= 0) return false;
            rotation = Quaternion.Euler(profile.AircraftPitch, profile.Yaw, 0);
            Vector3 right = rotation * Vector3.right, up = rotation * Vector3.up, forward = rotation * Vector3.forward;
            float tangent = Mathf.Tan(profile.AircraftFieldOfView * Mathf.Deg2Rad * .5f);
            float fraction = profile.PlaneMaximumViewportDimension;
            float distance = profile.AircraftMinimumDistance;
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
        private static bool SupportedAircraft(Game.Configs.SkirmishMissionViewProfile profile,SkirmishRoleKind role) =>
            (profile.AuthoredNonBaseAssault || profile.AuthoredGenericBaseAssault) ? SkirmishRoleIds.Category(role) == SkirmishPopulationCategory.Air ||
                role == SkirmishRoleKind.TransportPlane || role == SkirmishRoleKind.TransportHeli || role == SkirmishRoleKind.Drone :
                role == SkirmishRoleKind.TransportPlane;
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
