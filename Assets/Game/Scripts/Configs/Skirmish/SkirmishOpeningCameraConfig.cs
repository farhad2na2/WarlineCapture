using System;
using UnityEngine;

namespace Game.Configs
{
    /// <summary>
    /// Presentation-only opening camera data for one Skirmish mission (Resources key
    /// "SkirmishOpeningCamera_" + CatalogId). It names existing layout anchors and
    /// Unity-measured points on the existing Campaign geometry; it never adds geometry,
    /// never feeds a layout/view/binding fingerprint and never issues an order.
    /// The config applies only to the exact layout it was measured against.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/SkirmishExpansion/Mission Opening Camera")]
    public sealed class SkirmishOpeningCameraConfig : ScriptableObject
    {
        public int SchemaVersion = 1;
        public string CatalogId, LayoutId, LayoutContentHash;
        // Screen-up direction for the whole match: from -> toward. A role id names a
        // layout anchor; an empty role id uses the measured world point (x, z) instead.
        public string YawFromAnchorRoleId, YawTowardAnchorRoleId;
        public Vector2 YawFromWorldXZ, YawTowardWorldXZ;
        // Optional fixed screen-up for the whole match (degrees clockwise from map north, 0 =
        // north up, matching the north-up minimap). Overrides the from/toward yaw when set.
        public bool UseFixedYaw;
        public float FixedYawDegrees;
        // Points the opening frame must show together with the actual own opening group.
        public string[] FrameAnchorRoleIds = Array.Empty<string>();
        public Vector2[] FrameWorldXZ = Array.Empty<Vector2>();
        public string[] FrameWorldLabels = Array.Empty<string>();
        // Frame the live player base (its objective-role structure) instead of an anchor. The
        // S002 Barracks stands at about (1001, 303), not on the base.player anchor (949, 345).
        public bool FrameActualPlayerBase;
        // Optional approach point (the side trouble comes from): ApproachFrameMetres from the
        // live player base (FrameActualPlayerBase) or else from the From anchor, toward the
        // Toward anchor. Appended to the frame points.
        public string ApproachFromAnchorRoleId, ApproachTowardAnchorRoleId;
        public float ApproachFrameMetres;
        // Compose inside the HUD-free play area (SkirmishCameraFraming.OpeningFitViewport)
        // instead of the legacy band that let the squad cards cover the frame's lower edge.
        public bool FrameInsideHudPlayArea;
        // Where measured points came from (dump path, block size); review evidence only.
        public string Provenance;

        public static SkirmishOpeningCameraConfig Load(string catalogId) =>
            string.IsNullOrEmpty(catalogId) ? null : Resources.Load<SkirmishOpeningCameraConfig>("SkirmishOpeningCamera_" + catalogId);

        public bool Matches(string catalogId, SkirmishMapLayoutConfig layout) =>
            SchemaVersion == 1 && layout != null && !string.IsNullOrEmpty(CatalogId) && CatalogId == catalogId &&
            LayoutId == layout.LayoutId && LayoutContentHash == layout.ContentHash;

        public bool TryResolveYaw(SkirmishMapLayoutConfig layout, out float yaw)
        {
            yaw = 0f;
            if (UseFixedYaw)
            {
                if (!float.IsFinite(FixedYawDegrees)) return false;
                yaw = Mathf.Repeat(FixedYawDegrees, 360f);
                return true;
            }
            if (!TryPoint(layout, YawFromAnchorRoleId, YawFromWorldXZ, out var from) ||
                !TryPoint(layout, YawTowardAnchorRoleId, YawTowardWorldXZ, out var toward)) return false;
            var direction = toward - from;
            if (!(direction.sqrMagnitude > 1f)) return false;
            yaw = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360f);
            return float.IsFinite(yaw);
        }

        public bool HasApproach => !string.IsNullOrEmpty(ApproachTowardAnchorRoleId) && ApproachFrameMetres > 0f;

        /// <summary>
        /// The approach point: <see cref="ApproachFrameMetres"/> (at most the full distance) from
        /// <paramref name="from"/> toward the Toward anchor.
        /// </summary>
        public bool TryResolveApproach(SkirmishMapLayoutConfig layout, Vector2 from, out Vector2 point)
        {
            point = default;
            if (!HasApproach || !float.IsFinite(ApproachFrameMetres) || !float.IsFinite(from.x) || !float.IsFinite(from.y) ||
                !TryPoint(layout, ApproachTowardAnchorRoleId, default, out var toward)) return false;
            var direction = toward - from;
            float length = direction.magnitude;
            if (!(length > 1f)) return false;
            point = from + direction / length * Mathf.Min(ApproachFrameMetres, length);
            return float.IsFinite(point.x) && float.IsFinite(point.y);
        }

        /// <summary>The viewport the opening is composed in (fractions, y up).</summary>
        public Rect OpeningViewport => FrameInsideHudPlayArea ? SkirmishCameraFraming.OpeningFitViewport : SkirmishCameraFraming.LegacySafeViewport;

        /// <summary>Frame points in map world x/z (anchors projected through the layout's world binding), then the approach point.</summary>
        public bool TryResolveFramePoints(SkirmishMapLayoutConfig layout, out Vector2[] points) =>
            TryResolveFramePoints(layout, false, default, out points);

        /// <summary>
        /// Frame points in map world x/z: anchors, measured world points, then (when
        /// <see cref="FrameActualPlayerBase"/>) the live base <paramref name="playerBaseXZ"/>, then the
        /// approach point. A config that frames the live base fails without it, so the caller falls
        /// back to its group-only view instead of framing the wrong place.
        /// </summary>
        public bool TryResolveFramePoints(SkirmishMapLayoutConfig layout, bool hasPlayerBase, Vector2 playerBaseXZ, out Vector2[] points)
        {
            points = null;
            if (FrameActualPlayerBase && (!hasPlayerBase || !float.IsFinite(playerBaseXZ.x) || !float.IsFinite(playerBaseXZ.y))) return false;
            int anchorCount = FrameAnchorRoleIds?.Length ?? 0, worldCount = FrameWorldXZ?.Length ?? 0;
            int baseCount = FrameActualPlayerBase ? 1 : 0;
            Vector2 approach = default;
            if (HasApproach)
            {
                Vector2 from = playerBaseXZ;
                if (!FrameActualPlayerBase &&
                    (string.IsNullOrEmpty(ApproachFromAnchorRoleId) || !TryPoint(layout, ApproachFromAnchorRoleId, default, out from))) return false;
                if (!TryResolveApproach(layout, from, out approach)) return false;
            }
            var result = new Vector2[anchorCount + worldCount + baseCount + (HasApproach ? 1 : 0)];
            for (int i = 0; i < anchorCount; i++)
                if (string.IsNullOrEmpty(FrameAnchorRoleIds[i]) || !TryPoint(layout, FrameAnchorRoleIds[i], default, out result[i])) return false;
            for (int i = 0; i < worldCount; i++)
            {
                var point = FrameWorldXZ[i];
                if (!float.IsFinite(point.x) || !float.IsFinite(point.y)) return false;
                result[anchorCount + i] = point;
            }
            if (FrameActualPlayerBase) result[anchorCount + worldCount] = playerBaseXZ;
            if (HasApproach) result[result.Length - 1] = approach;
            points = result;
            return true;
        }

        static bool TryPoint(SkirmishMapLayoutConfig layout, string roleId, Vector2 world, out Vector2 point)
        {
            point = world;
            if (string.IsNullOrEmpty(roleId)) return float.IsFinite(world.x) && float.IsFinite(world.y);
            if (layout == null || !layout.TryGetAnchor(roleId, out var anchor)) return false;
            if (layout.HasWorldBinding)
            {
                if (!layout.TryProjectLocalToMap(anchor.WorldX, anchor.WorldZ, out float x, out float z)) return false;
                point = new Vector2(x, z);
            }
            else point = new Vector2(anchor.WorldX, anchor.WorldZ);
            return float.IsFinite(point.x) && float.IsFinite(point.y);
        }
    }
}
