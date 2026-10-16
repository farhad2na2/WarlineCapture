// Review draft only. No camera mutation or implicit acceptance thresholds.
using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Editor
{
    [Serializable]
    public struct SkirmishMissionCameraFramingConstraints
    {
        public int SchemaVersion;
        public string CatalogId, LayoutBindingSha256;
        // Explicit authored world-Y limits, matching the existing zoom API. Source
        // elevations and actor distance must be measured as separate raw facts.
        public float MinimumHeight, MaximumHeight, MinimumFieldOfView, MaximumFieldOfView;
        public float MinimumInfantryHeightPixels, MinimumInfantryHeightFraction, MaximumPlaneWidthFraction, MaximumPlaneHeightFraction;
        public float SafeEdgePixels;
        public bool IsValid => SchemaVersion == 1 && !string.IsNullOrWhiteSpace(CatalogId) &&
            Hash(LayoutBindingSha256) &&
            Positive(MinimumHeight) && Positive(MaximumHeight) && MaximumHeight >= MinimumHeight &&
            Positive(MinimumFieldOfView) && MaximumFieldOfView >= MinimumFieldOfView && MaximumFieldOfView < 179 &&
            Positive(MinimumInfantryHeightPixels) && Positive(MinimumInfantryHeightFraction) && MinimumInfantryHeightFraction < 1 &&
            Positive(MaximumPlaneWidthFraction) && MaximumPlaneWidthFraction < 1 &&
            Positive(MaximumPlaneHeightFraction) && MaximumPlaneHeightFraction < 1 && Finite(SafeEdgePixels) && SafeEdgePixels >= 0;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value > 0;
        private static bool Hash(string value)
        { if (value == null || value.Length != 64) return false; foreach (char c in value) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false; return true; }
    }

    [Serializable]
    public struct SkirmishCameraRenderBoundsFact
    {
        public int EntityIndex, EntityVersion;
        public Bounds WorldBounds;
    }

    [Serializable]
    public sealed class SkirmishCameraActorMeasurement
    {
        public string SessionId, StableActorId, NativeRole, NativeCategory, Failure;
        public int EntityIndex, EntityVersion, Frame, RenderBoundsCount;
        public byte CurrentVisual, DesiredVisual;
        public Vector3 CameraPosition, CameraEuler, ActorPosition;
        public string ActorTransformSource;
        public float FieldOfView, OrthographicSize, CameraActorDistance, NearClip, FarClip;
        public bool Orthographic, Valid;
        public Rect CameraPixels, ProjectedPixels;
        public int ActiveRootIndex, ActiveRootVersion, TargetWidth, TargetHeight;
        public float Aspect;
        public float[] ProjectionColumnMajor, WorldToCameraColumnMajor;
        public SkirmishCameraRenderBoundsFact[] RenderBounds;
        public float HeightPixels => ProjectedPixels.height;
        public float WidthFraction => ProjectedPixels.width / CameraPixels.width;
        public float HeightFraction => ProjectedPixels.height / CameraPixels.height;
    }

    public static class SkirmishCameraFramingMeasurement
    {
        // All eight world AABB corners must lie within the depth interval. A
        // near-plane intersection cannot be accepted using folded projections.
        public static bool TryProject(Camera camera, Bounds bounds, out Rect pixels)
        {
            pixels = default;
            if (camera == null || camera.pixelWidth <= 0 || camera.pixelHeight <= 0 ||
                !Finite(bounds.center) || !Finite(bounds.extents) || bounds.extents.x <= 0 || bounds.extents.y <= 0 || bounds.extents.z <= 0) return false;
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 8; i++)
            {
                var point = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var projected = camera.WorldToScreenPoint(point);
                if (!Finite(projected) || projected.z <= camera.nearClipPlane || projected.z >= camera.farClipPlane) return false;
                minimum = Vector2.Min(minimum, projected); maximum = Vector2.Max(maximum, projected);
            }
            pixels = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
            return pixels.width > 0 && pixels.height > 0;
        }

        // Read after native presentation/transform/bounds updates. Caller must bind
        // this raw sample and PNG to the owned candidate/session and same frame.
        public static SkirmishCameraActorMeasurement Capture(EntityManager em, Entity actor, Camera camera)
        {
            var result = new SkirmishCameraActorMeasurement { Frame = Time.frameCount, EntityIndex = actor.Index, EntityVersion = actor.Version };
            if (camera == null || !em.Exists(actor) || !em.HasComponent<SkirmishAttemptOwnedComponent>(actor) ||
                !em.HasComponent<SkirmishUnitRoleComponent>(actor) || !em.HasComponent<LocalTransform>(actor) ||
                !em.HasComponent<UnitRenderVisualComponent>(actor)) return Fail(result, "MissingActualActorCameraOrVisualState");
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            var visual = em.GetComponentData<UnitRenderVisualComponent>(actor);
            result.SessionId = owner.SessionId.ToString(); result.StableActorId = owner.StableObjectId.ToString();
            result.NativeRole = em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role.ToString();
            result.NativeCategory = em.GetComponentData<SkirmishUnitRoleComponent>(actor).Category.ToString();
            result.CurrentVisual = visual.Current; result.DesiredVisual = visual.Desired;
            if (em.HasComponent<Parent>(actor)) { result.Failure = "ParentedActorWorldPositionUnavailable"; return result; }
            result.ActorTransformSource = "UnparentedLocalTransform";
            result.ActorPosition = em.GetComponentData<LocalTransform>(actor).Position;
            result.CameraPosition = camera.transform.position; result.CameraEuler = camera.transform.eulerAngles;
            result.FieldOfView = camera.fieldOfView; result.Orthographic = camera.orthographic; result.OrthographicSize = camera.orthographicSize;
            result.CameraPixels = camera.pixelRect; result.NearClip = camera.nearClipPlane; result.FarClip = camera.farClipPlane;
            result.CameraActorDistance = Vector3.Distance(result.CameraPosition, result.ActorPosition);
            result.Aspect = camera.aspect;
            result.TargetWidth = camera.targetTexture != null ? camera.targetTexture.width : Screen.width;
            result.TargetHeight = camera.targetTexture != null ? camera.targetTexture.height : Screen.height;
            result.ProjectionColumnMajor = new float[16]; result.WorldToCameraColumnMajor = new float[16];
            for (int i = 0; i < 16; i++) { result.ProjectionColumnMajor[i] = camera.projectionMatrix[i]; result.WorldToCameraColumnMajor[i] = camera.worldToCameraMatrix[i]; }
            if (visual.Current != visual.Desired) return Fail(result, "NativeVisualTransitionPending");
            Entity root = Entity.Null;
            if (visual.Current == (byte)UnitRenderVisualKind.Detail)
            {
                if (em.HasComponent<UnitDetailedVisualReference>(actor)) root = em.GetComponentData<UnitDetailedVisualReference>(actor).Root;
                else if (em.HasComponent<UnitModelInstanceReference>(actor)) root = em.GetComponentData<UnitModelInstanceReference>(actor).Instance;
            }
            else if (visual.Current == (byte)UnitRenderVisualKind.Mid && em.HasComponent<UnitMidLodInstanceReference>(actor))
                root = em.GetComponentData<UnitMidLodInstanceReference>(actor).Instance;
            else if (visual.Current == (byte)UnitRenderVisualKind.Low && em.HasComponent<UnitLowLodInstanceReference>(actor))
                root = em.GetComponentData<UnitLowLodInstanceReference>(actor).Instance;
            else return Fail(result, "NativeVisualIsFarImpostorOrUnknown");
            if (root == Entity.Null || !em.Exists(root)) return Fail(result, "NativeActiveMeshRootMissing");
            result.ActiveRootIndex = root.Index; result.ActiveRootVersion = root.Version;
            var rawBounds = new List<SkirmishCameraRenderBoundsFact>();
            var visited = new HashSet<Entity>(); var pending = new Queue<Entity>(); pending.Enqueue(root);
            Rect projected = default; bool found = false;
            while (pending.Count > 0)
            {
                var entity = pending.Dequeue();
                if (!visited.Add(entity) || !em.Exists(entity) || em.HasComponent<Disabled>(entity) || em.HasComponent<DisableRendering>(entity)) continue;
                if (em.HasBuffer<LinkedEntityGroup>(entity))
                    foreach (var child in em.GetBuffer<LinkedEntityGroup>(entity)) pending.Enqueue(child.Value);
                if (!em.HasComponent<WorldRenderBounds>(entity)) continue;
                var value = em.GetComponentData<WorldRenderBounds>(entity).Value;
                var worldBounds = new Bounds(value.Center, value.Extents * 2);
                rawBounds.Add(new SkirmishCameraRenderBoundsFact { EntityIndex = entity.Index, EntityVersion = entity.Version, WorldBounds = worldBounds });
                result.RenderBounds = rawBounds.ToArray(); // Preserve raw failed depth sample too.
                if (!TryProject(camera, worldBounds, out var next)) return Fail(result, "NativeBoundsInvalidOrDepthClipped");
                projected = found ? Rect.MinMaxRect(Mathf.Min(projected.xMin, next.xMin), Mathf.Min(projected.yMin, next.yMin),
                    Mathf.Max(projected.xMax, next.xMax), Mathf.Max(projected.yMax, next.yMax)) : next;
                found = true; result.RenderBoundsCount++;
            }
            if (!found) return Fail(result, "NativeActiveRenderBoundsMissing");
            result.ProjectedPixels = projected; result.Valid = true; return result;
        }

        // Geometry/constraint result only: occlusion, shader readability and comfort
        // still require the actual same-candidate PNG and user/device review.
        public static string[] Evaluate(SkirmishCameraActorMeasurement sample, in SkirmishMissionCameraFramingConstraints limits)
        {
            var failures = new List<string>();
            if (!limits.IsValid) return new[] { "AuthoredFramingConstraintsMissingOrInvalid" };
            if (sample == null || !sample.Valid) return new[] { sample == null ? "NativeSampleMissing" : sample.Failure };
            if (!Finite(sample.CameraPosition) || !float.IsFinite(sample.FieldOfView) ||
                !float.IsFinite(sample.CameraPixels.width) || !float.IsFinite(sample.CameraPixels.height) ||
                sample.CameraPixels.width <= 0 || sample.CameraPixels.height <= 0 ||
                !float.IsFinite(sample.ProjectedPixels.xMin) || !float.IsFinite(sample.ProjectedPixels.yMin) ||
                !float.IsFinite(sample.ProjectedPixels.xMax) || !float.IsFinite(sample.ProjectedPixels.yMax) ||
                sample.ProjectedPixels.width <= 0 || sample.ProjectedPixels.height <= 0) return new[] { "NativeSampleProjectionInvalid" };
            if (sample.CurrentVisual < (byte)UnitRenderVisualKind.Detail || sample.CurrentVisual > (byte)UnitRenderVisualKind.Low ||
                sample.CurrentVisual != sample.DesiredVisual) failures.Add("NativeMeshVisualNotSettled");
            if (sample.Orthographic) failures.Add("PerspectiveConstraintCannotEvaluateOrthographicView");
            if (sample.CameraPosition.y < limits.MinimumHeight || sample.CameraPosition.y > limits.MaximumHeight ||
                sample.FieldOfView < limits.MinimumFieldOfView || sample.FieldOfView > limits.MaximumFieldOfView) failures.Add("CameraOutsideAuthoredZoomOrFov");
            var safe = sample.CameraPixels; safe.xMin += limits.SafeEdgePixels; safe.yMin += limits.SafeEdgePixels;
            safe.xMax -= limits.SafeEdgePixels; safe.yMax -= limits.SafeEdgePixels;
            if (safe.width <= 0 || safe.height <= 0 || sample.ProjectedPixels.xMin < safe.xMin || sample.ProjectedPixels.xMax > safe.xMax ||
                sample.ProjectedPixels.yMin < safe.yMin || sample.ProjectedPixels.yMax > safe.yMax) failures.Add("ActorBoundsOutsideSafeFrame");
            if (sample.NativeCategory == "Infantry" && sample.CurrentVisual != (byte)UnitRenderVisualKind.Detail && sample.CurrentVisual != (byte)UnitRenderVisualKind.Mid)
                failures.Add("InfantryRequiresFullOrMidMesh");
            if (sample.NativeCategory == "Infantry" && sample.HeightPixels < Mathf.Max(limits.MinimumInfantryHeightPixels,
                limits.MinimumInfantryHeightFraction * sample.CameraPixels.height)) failures.Add("InfantryBelowAuthoredPixelHeight");
            if (sample.NativeRole == "TransportPlane" && (sample.WidthFraction > limits.MaximumPlaneWidthFraction || sample.HeightFraction > limits.MaximumPlaneHeightFraction))
                failures.Add("TransportPlaneExceedsAuthoredCoverage");
            return failures.ToArray();
        }
        private static SkirmishCameraActorMeasurement Fail(SkirmishCameraActorMeasurement value, string error) { value.Failure = error; return value; }
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
