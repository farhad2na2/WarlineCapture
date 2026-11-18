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
        // Points the opening frame must show together with the actual own opening group.
        public string[] FrameAnchorRoleIds = Array.Empty<string>();
        public Vector2[] FrameWorldXZ = Array.Empty<Vector2>();
        public string[] FrameWorldLabels = Array.Empty<string>();
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
            if (!TryPoint(layout, YawFromAnchorRoleId, YawFromWorldXZ, out var from) ||
                !TryPoint(layout, YawTowardAnchorRoleId, YawTowardWorldXZ, out var toward)) return false;
            var direction = toward - from;
            if (!(direction.sqrMagnitude > 1f)) return false;
            yaw = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360f);
            return float.IsFinite(yaw);
        }

        /// <summary>Frame points in map world x/z (anchors projected through the layout's world binding).</summary>
        public bool TryResolveFramePoints(SkirmishMapLayoutConfig layout, out Vector2[] points)
        {
            points = null;
            int anchorCount = FrameAnchorRoleIds?.Length ?? 0, worldCount = FrameWorldXZ?.Length ?? 0;
            var result = new Vector2[anchorCount + worldCount];
            for (int i = 0; i < anchorCount; i++)
                if (!TryPoint(layout, FrameAnchorRoleIds[i], default, out result[i]) || string.IsNullOrEmpty(FrameAnchorRoleIds[i])) return false;
            for (int i = 0; i < worldCount; i++)
            {
                var point = FrameWorldXZ[i];
                if (!float.IsFinite(point.x) || !float.IsFinite(point.y)) return false;
                result[anchorCount + i] = point;
            }
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
