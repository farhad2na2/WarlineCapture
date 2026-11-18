using System;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>Resolved Skirmish ground camera values. Heights are absolute world Y.</summary>
    public readonly struct SkirmishGroundFraming
    {
        public readonly float GroundY, CloseHeight, TacticalHeight, WideHeight, BuildHeight;
        public readonly float Pitch, Yaw, FieldOfView, BuildFieldOfView;
        public readonly bool HasMissionYaw;
        public SkirmishGroundFraming(float groundY, float close, float tactical, float wide, float build,
            float pitch, float yaw, float fieldOfView, float buildFieldOfView, bool hasMissionYaw)
        {
            GroundY = groundY; CloseHeight = close; TacticalHeight = tactical; WideHeight = wide; BuildHeight = build;
            Pitch = pitch; Yaw = yaw; FieldOfView = fieldOfView; BuildFieldOfView = buildFieldOfView; HasMissionYaw = hasMissionYaw;
        }
        public bool IsValid => float.IsFinite(GroundY) && CloseHeight > GroundY && TacticalHeight >= CloseHeight &&
            WideHeight >= TacticalHeight && BuildHeight >= TacticalHeight && BuildHeight <= WideHeight &&
            Pitch > 0f && Pitch < 90f && float.IsFinite(Yaw) && FieldOfView > 1f && FieldOfView < 179f &&
            BuildFieldOfView > 1f && BuildFieldOfView < 179f;
    }

    /// <summary>
    /// Skirmish-wide presentation framing (owner request 2026-11-17: "camera too close,
    /// especially when a helicopter is flying"). Pure math: no orders, no selection, no
    /// gameplay data. The authored mission view profile stays the reviewed engineering
    /// floor (its Min height remains the close/select zoom); this policy only widens the
    /// default, objective/reinforcement and aircraft views on the existing geometry.
    /// </summary>
    public static class SkirmishCameraFraming
    {
        public const int Version = 1;
        // Ground. Heights are above the source minimum elevation (GroundReferenceY).
        public const float TacticalHeightAboveGround = 34f;   // default: group + home landmark + direction
        public const float WideHeightAboveGround = 48f;       // HUD zoom-out, objective focus, reinforcements
        public const float MinimumCloseHeightAboveGround = 10f;
        public const float TacticalPitch = 50f;
        public const float MinimumFieldOfView = 46f;
        // Centre-line slant guard kept below the shared 64 m enemy impostor distance.
        public const float MaximumCenterSlantDistance = 62f;
        // Opening composition safe area (viewport fractions): leaves the squad cards
        // at the bottom and the resource/objective strip at the top uncovered.
        public const float SafeMinU = .06f, SafeMaxU = .94f, SafeMinV = .12f, SafeMaxV = .90f, SafeTolerance = .005f;
        // Aircraft. The aircraft and its rotor fill at most this share of the view,
        // the ground below (or at most MaximumGroundDrop below it) and a lead point
        // along its heading stay in frame, and every view ray descends.
        public const float AircraftMaximumViewportFraction = .30f;
        public const float AircraftMinimumDistance = 36f;
        public const float AircraftClearanceAboveTop = 10f;
        public const float AircraftMaximumGroundDrop = 36f;
        public const float AircraftLeadMetres = 24f;
        public const float AircraftSafeFraction = .82f;
        public const float AircraftDescendingRayMarginDegrees = 12f;
        // Follow (owner 2026-11-18: whole aircraft, rotor, heading and the intended
        // landing patch readable together). The view looks at the airframe itself, so
        // the airframe sits at screen centre, clear of the HUD strips and its shadow.
        // The intended landing patch stays inside a vertical band that keeps it off the
        // squad cards and the top strip; it widens the view up to this distance, beyond
        // which only the bearing to the patch is kept on the frame edge.
        public const float AircraftPatchSafeFractionVertical = .70f;
        public const float AircraftFollowPatchMaximumDistance = 90f;

        public static SkirmishGroundFraming Resolve(SkirmishMissionViewProfile profile, float? missionYaw = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            float ground = profile.GroundReferenceY;
            float pitch = Mathf.Clamp(Mathf.Min(profile.Pitch, TacticalPitch), 35f, 80f);
            float slantCeiling = ground + MaximumCenterSlantDistance * Mathf.Sin(pitch * Mathf.Deg2Rad);
            float wide = Mathf.Max(profile.MaxHeight, Mathf.Min(ground + WideHeightAboveGround, slantCeiling));
            float tactical = Mathf.Min(Mathf.Max(profile.NormalHeight, ground + TacticalHeightAboveGround), wide);
            float close = Mathf.Min(Mathf.Max(profile.MinHeight, ground + MinimumCloseHeightAboveGround), tactical);
            float build = Mathf.Clamp(Mathf.Max(profile.BuildHeight, tactical), tactical, wide);
            float fov = Mathf.Max(profile.NormalFieldOfView, MinimumFieldOfView);
            float buildFov = Mathf.Max(profile.BuildFieldOfView, fov);
            bool hasYaw = missionYaw.HasValue && float.IsFinite(missionYaw.Value);
            float yaw = hasYaw ? Mathf.Repeat(missionYaw.Value, 360f) : profile.Yaw;
            return new SkirmishGroundFraming(ground, close, tactical, wide, build, pitch, yaw, fov, buildFov, hasYaw);
        }

        // ---------------------------------------------------------------- projection
        static void Axes(float pitch, float yaw, out Vector3 right, out Vector3 up, out Vector3 forward)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            right = rotation * Vector3.right; up = rotation * Vector3.up; forward = rotation * Vector3.forward;
        }

        /// <summary>Perspective viewport position (0..1) of a world point; false behind the camera.</summary>
        public static bool TryViewport(Vector3 camera, float pitch, float yaw, float fieldOfView, float aspect, Vector3 point, out Vector2 viewport)
        {
            Axes(pitch, yaw, out var right, out var up, out var forward);
            var relative = point - camera;
            float depth = Vector3.Dot(relative, forward);
            float tanV = Mathf.Tan(fieldOfView * .5f * Mathf.Deg2Rad), tanH = tanV * aspect;
            viewport = default;
            if (!(depth > .01f)) return false;
            viewport = new Vector2(.5f + .5f * Vector3.Dot(relative, right) / (depth * tanH),
                .5f + .5f * Vector3.Dot(relative, up) / (depth * tanV));
            return float.IsFinite(viewport.x) && float.IsFinite(viewport.y);
        }

        static bool TryHitPlane(Vector3 camera, Vector3 right, Vector3 up, Vector3 forward, float tanH, float tanV,
            Vector2 viewport, float planeY, out Vector3 hit)
        {
            var direction = forward + right * ((viewport.x * 2f - 1f) * tanH) + up * ((viewport.y * 2f - 1f) * tanV);
            hit = default;
            if (!(direction.y < -.0001f)) return false;
            float t = (planeY - camera.y) / direction.y;
            if (!(t > 0f)) return false;
            hit = camera + direction * t;
            return true;
        }

        /// <summary>Camera position whose centre ray meets <paramref name="groundPoint"/> from absolute height.</summary>
        public static Vector3 CameraForGroundCenter(Vector3 groundPoint, float absoluteHeight, float pitch, float yaw)
        {
            Axes(pitch, yaw, out _, out _, out var forward);
            float t = (absoluteHeight - groundPoint.y) / -forward.y;
            return groundPoint - forward * t;
        }

        /// <summary>Point on the y=0 plane under the centre ray (the camera system's ground-centre convention).</summary>
        public static Vector3 ZeroPlaneGroundCenter(Vector3 camera, float pitch, float yaw)
        {
            Axes(pitch, yaw, out _, out _, out var forward);
            var point = camera + forward * (camera.y / -forward.y);
            point.y = 0f;
            return point;
        }

        // ------------------------------------------------------------------ opening
        /// <summary>
        /// Composes the opening frame: the actual own group plus authored/measured landmark
        /// points must sit inside the HUD-safe viewport. Uses the lowest height between the
        /// tactical and wide heights that fits; otherwise the wide height, centred.
        /// </summary>
        public static bool TryComposeOpening(in SkirmishGroundFraming framing, Vector3 group, Vector2[] landmarksXZ,
            float aspect, out Vector3 camera, out bool allFramed)
        {
            camera = default; allFramed = false;
            if (!framing.IsValid || !float.IsFinite(aspect) || aspect <= 0f ||
                !float.IsFinite(group.x) || !float.IsFinite(group.y) || !float.IsFinite(group.z)) return false;
            int count = 1 + (landmarksXZ?.Length ?? 0);
            var points = new Vector3[count];
            points[0] = group;
            for (int i = 1; i < count; i++)
            {
                var xz = landmarksXZ[i - 1];
                if (!float.IsFinite(xz.x) || !float.IsFinite(xz.y)) return false;
                points[i] = new Vector3(xz.x, group.y, xz.y);
            }
            float low = Mathf.Max(framing.TacticalHeight, group.y + 4f), high = Mathf.Max(framing.WideHeight, low);
            if (TryCenter(framing, points, aspect, low, out camera, out bool fits) && fits) { allFramed = true; return true; }
            if (!TryCenter(framing, points, aspect, high, out var wide, out bool wideFits)) return false;
            if (!wideFits) { camera = wide; return true; }
            for (int i = 0; i < 14; i++)
            {
                float mid = (low + high) * .5f;
                if (TryCenter(framing, points, aspect, mid, out var candidate, out bool ok) && ok) { high = mid; wide = candidate; }
                else low = mid;
            }
            camera = wide; allFramed = true;
            return true;
        }

        static bool TryCenter(in SkirmishGroundFraming framing, Vector3[] points, float aspect, float height, out Vector3 camera, out bool fits)
        {
            fits = false;
            Axes(framing.Pitch, framing.Yaw, out var right, out var up, out var forward);
            float tanV = Mathf.Tan(framing.FieldOfView * .5f * Mathf.Deg2Rad), tanH = tanV * aspect;
            float planeY = points[0].y;
            var centroid = Vector3.zero;
            foreach (var point in points) centroid += point;
            centroid /= points.Length;
            camera = CameraForGroundCenter(centroid, height, framing.Pitch, framing.Yaw);
            var safeCenter = new Vector2((SafeMinU + SafeMaxU) * .5f, (SafeMinV + SafeMaxV) * .5f);
            for (int iteration = 0; iteration < 10; iteration++)
            {
                if (!ViewportBounds(framing, points, aspect, camera, out var min, out var max)) return false;
                var boxCenter = (min + max) * .5f;
                if (!TryHitPlane(camera, right, up, forward, tanH, tanV, boxCenter, planeY, out var boxHit) ||
                    !TryHitPlane(camera, right, up, forward, tanH, tanV, safeCenter, planeY, out var safeHit)) return false;
                var shift = boxHit - safeHit; shift.y = 0f;
                camera += shift;
                if (shift.sqrMagnitude < .0004f) break;
            }
            if (!ViewportBounds(framing, points, aspect, camera, out var finalMin, out var finalMax)) return false;
            fits = finalMin.x >= SafeMinU - SafeTolerance && finalMax.x <= SafeMaxU + SafeTolerance &&
                finalMin.y >= SafeMinV - SafeTolerance && finalMax.y <= SafeMaxV + SafeTolerance;
            return float.IsFinite(camera.x) && float.IsFinite(camera.y) && float.IsFinite(camera.z);
        }

        static bool ViewportBounds(in SkirmishGroundFraming framing, Vector3[] points, float aspect, Vector3 camera, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.MaxValue, float.MaxValue); max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in points)
            {
                if (!TryViewport(camera, framing.Pitch, framing.Yaw, framing.FieldOfView, aspect, point, out var viewport)) return false;
                min = Vector2.Min(min, viewport); max = Vector2.Max(max, viewport);
            }
            return true;
        }

        // ----------------------------------------------------------------- aircraft
        /// <summary>
        /// Aircraft view: whole measured aircraft bounds, the ground below it and (when
        /// airborne) a lead point along its heading, seen from above at a fixed yaw.
        /// Returns false when the fit exceeds <paramref name="maximumDistance"/>; callers
        /// keep their existing refusal semantics.
        /// </summary>
        public static bool TryAircraftPose(Bounds aircraft, float groundY, Vector2 headingXZ, bool airborne,
            float pitch, float yaw, float fieldOfView, float maximumViewportFraction, float aspect, float nearClip,
            float minimumDistance, float maximumDistance, out Vector3 position, out Quaternion rotation, out Vector3 lookAt)
            => TryAircraftFollowPose(aircraft, groundY, headingXZ, airborne, null, pitch, yaw, fieldOfView, maximumViewportFraction,
                aspect, nearClip, minimumDistance, maximumDistance, out position, out rotation, out lookAt, out _);

        /// <summary>
        /// Aircraft follow view. Looks at the airframe centre (never a ground point under it),
        /// so the whole aircraft and rotor sit mid-screen at no more than
        /// <paramref name="maximumViewportFraction"/> of the view; the ground below it, a lead
        /// point along its heading and, when given, the intended landing patch stay in frame.
        /// A patch that would need more than <see cref="AircraftFollowPatchMaximumDistance"/>
        /// keeps only its bearing on the frame edge (<paramref name="patchFramed"/> false).
        /// Returns false when the aircraft fit alone exceeds <paramref name="maximumDistance"/>.
        /// The pose is translation-invariant: moving the aircraft moves the camera by the same amount.
        /// </summary>
        public static bool TryAircraftFollowPose(Bounds aircraft, float groundY, Vector2 headingXZ, bool airborne, Vector3? landingPatch,
            float pitch, float yaw, float fieldOfView, float maximumViewportFraction, float aspect, float nearClip,
            float minimumDistance, float maximumDistance, out Vector3 position, out Quaternion rotation, out Vector3 lookAt, out bool patchFramed)
        {
            position = default; rotation = default; lookAt = default; patchFramed = false;
            if (!Finite(aircraft.center) || !Finite(aircraft.extents) || aircraft.extents.x <= 0f || aircraft.extents.y <= 0f ||
                aircraft.extents.z <= 0f || !float.IsFinite(groundY) || !float.IsFinite(pitch) || !float.IsFinite(yaw) ||
                !(fieldOfView > 1f && fieldOfView < 179f) || !(aspect > 0f) || !float.IsFinite(aspect) ||
                !(nearClip > 0f) || !float.IsFinite(nearClip) || !(maximumDistance > 0f) || !float.IsFinite(minimumDistance) ||
                !(maximumViewportFraction > 0f && maximumViewportFraction < 1f)) return false;
            // Every view ray descends: the aircraft is always seen from above, never its underside.
            pitch = Mathf.Clamp(Mathf.Max(pitch, fieldOfView * .5f + AircraftDescendingRayMarginDegrees), 1f, 89f);
            Axes(pitch, yaw, out var right, out var up, out var forward);
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            float tanV = Mathf.Tan(fieldOfView * .5f * Mathf.Deg2Rad), tanH = tanV * aspect;

            float floorY = Mathf.Max(groundY, aircraft.min.y - AircraftMaximumGroundDrop);
            var below = new Vector3(aircraft.center.x, Mathf.Min(floorY, aircraft.min.y), aircraft.center.z);
            bool lead = airborne && headingXZ.sqrMagnitude > .0001f && float.IsFinite(headingXZ.x) && float.IsFinite(headingXZ.y);
            var ahead = lead ? below + new Vector3(headingXZ.x, 0f, headingXZ.y).normalized * AircraftLeadMetres : below;
            var center = aircraft.center;

            float distance = Mathf.Max(minimumDistance, AircraftMinimumDistance);
            for (int i = 0; i < 8; i++)
            {
                // Aircraft size share about its own centre, which is also the look-at.
                var local = Vector3.Scale(aircraft.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float localDepth = Vector3.Dot(local, forward);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(local, right)) / (tanH * maximumViewportFraction) - localDepth);
                distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(local, up)) / (tanV * maximumViewportFraction) - localDepth);
                distance = Require(distance, local, right, up, forward, tanH, tanV, nearClip);
            }
            distance = Require(distance, below - center, right, up, forward, tanH, tanV, nearClip);
            distance = Require(distance, ahead - center, right, up, forward, tanH, tanV, nearClip);
            // Camera stays above the aircraft top by a fixed clearance.
            float sinPitch = Mathf.Sin(pitch * Mathf.Deg2Rad);
            distance = Mathf.Max(distance, (aircraft.max.y + AircraftClearanceAboveTop - center.y) / sinPitch);
            if (!float.IsFinite(distance) || distance > maximumDistance) return false;

            if (landingPatch.HasValue && Finite(landingPatch.Value))
            {
                var relative = landingPatch.Value - center;
                float needed = PatchDistance(relative, right, up, forward, tanH, tanV);
                float ceiling = Mathf.Min(maximumDistance, Mathf.Max(distance, AircraftFollowPatchMaximumDistance));
                if (needed <= ceiling)
                {
                    distance = Mathf.Max(distance, Mathf.Max(needed, nearClip * 2f - Vector3.Dot(relative, forward)));
                    patchFramed = true;
                }
                else distance = Mathf.Max(distance, ceiling); // bearing to a far patch rides the frame edge
            }
            position = center - forward * distance; lookAt = center;
            return Finite(position);
        }

        /// <summary>
        /// Distance from the look-at that brings <paramref name="relative"/> inside the patch band.
        /// Linear in the offset: scaling the offset by k scales the result by k.
        /// </summary>
        static float PatchDistance(Vector3 relative, Vector3 right, Vector3 up, Vector3 forward, float tanH, float tanV)
        {
            float depth = Vector3.Dot(relative, forward);
            return Mathf.Max(Mathf.Abs(Vector3.Dot(relative, right)) / (tanH * AircraftSafeFraction) - depth,
                Mathf.Abs(Vector3.Dot(relative, up)) / (tanV * AircraftPatchSafeFractionVertical) - depth);
        }

        /// <summary>Largest fraction of the view (max of width and height share) a world box covers; false if it crosses the camera.</summary>
        public static bool TryViewportShare(Vector3 camera, float pitch, float yaw, float fieldOfView, float aspect, Bounds box, out float share, out bool inside)
        {
            share = 0f; inside = false;
            var min = new Vector2(float.MaxValue, float.MaxValue); var max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var corner = box.center + Vector3.Scale(box.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                if (!TryViewport(camera, pitch, yaw, fieldOfView, aspect, corner, out var viewport)) return false;
                min = Vector2.Min(min, viewport); max = Vector2.Max(max, viewport);
            }
            share = Mathf.Max(max.x - min.x, max.y - min.y);
            inside = min.x >= 0f && max.x <= 1f && min.y >= 0f && max.y <= 1f;
            return true;
        }

        static float Require(float distance, Vector3 relative, Vector3 right, Vector3 up, Vector3 forward, float tanH, float tanV, float nearClip)
        {
            float depth = Vector3.Dot(relative, forward);
            distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(relative, right)) / (tanH * AircraftSafeFraction) - depth);
            distance = Mathf.Max(distance, Mathf.Abs(Vector3.Dot(relative, up)) / (tanV * AircraftSafeFraction) - depth);
            return Mathf.Max(distance, nearClip * 2f - depth);
        }

        static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
