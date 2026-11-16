using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>Exact authored structure-pad binding and cell-footprint containment for Skirmish grants.</summary>
    public static class SkirmishStartingGrantPlacementConstraint
    {
        public static bool TryResolvePad(SkirmishMapLayoutConfig layout, SkirmishResolvedSetup setup,
            int structureIndex, out SkirmishLegalPadConfig pad)
        {
            pad = default;
            if (layout == null || setup?.Structures == null || structureIndex < 0 || structureIndex >= setup.Structures.Length)
                return false;
            var structure = setup.Structures[structureIndex];
            if (string.IsNullOrEmpty(structure.StructureId) || string.IsNullOrEmpty(structure.SpawnAnchorId) ||
                structure.FactionId < 1 || structure.FactionId > 2)
                return false;

            int ordinal = 0;
            for (int i = 0; i < structureIndex; i++)
                if (setup.Structures[i].FactionId == structure.FactionId &&
                    setup.Structures[i].StructureId == structure.StructureId) ordinal++;
            var bindings = layout.StructureInstancePads.Where(row => row.SizeId == setup.SizeId &&
                row.DifficultyId == setup.DifficultyId && row.FactionId == structure.FactionId &&
                row.StructureId == structure.StructureId && row.InstanceOrdinal == ordinal).ToArray();
            if (bindings.Length != 1 || bindings[0].AnchorId != structure.SpawnAnchorId ||
                string.IsNullOrEmpty(bindings[0].PadId)) return false;

            var pads = layout.Pads.Where(item => item.PadId == bindings[0].PadId &&
                item.AnchorId == bindings[0].AnchorId && item.FactionId == structure.FactionId &&
                item.Kind == SkirmishMapLayoutBuilder.PadKindForStructure(structure.StructureId)).ToArray();
            var anchors = layout.Anchors.Where(item => item.AnchorId == bindings[0].AnchorId).ToArray();
            if (pads.Length != 1 || anchors.Length != 1 ||
                Math.Abs(anchors[0].WorldX - pads[0].CenterX) > .001f ||
                Math.Abs(anchors[0].WorldZ - pads[0].CenterZ) > .001f ||
                !Finite(anchors[0].NormalizedU) || !Finite(anchors[0].NormalizedV) ||
                !layout.TryProjectToMap(anchors[0].NormalizedU, anchors[0].NormalizedV, out float ax, out float az) ||
                !Finite(ax) || !Finite(az) || !layout.TryProjectLocalToMap(pads[0].CenterX, pads[0].CenterZ, out float px, out float pz) ||
                Math.Abs(ax - px) > .001f || Math.Abs(az - pz) > .001f) return false;
            pad = pads[0];
            return Finite(pad.CenterX) && Finite(pad.CenterZ) && Finite(pad.WidthMetres) &&
                Finite(pad.DepthMetres) && pad.WidthMetres > 0 && pad.DepthMetres > 0 && Finite(px) && Finite(pz);
        }

        public static bool ContainsFootprint(SkirmishMapLayoutConfig layout, SkirmishLegalPadConfig pad,
            GridConfig grid, OperationMapBoundsConfig sourceBounds, RectInt candidate)
        {
            if (layout == null || grid.Width <= 0 || grid.Height <= 0 || !Finite(grid.CellSize) || grid.CellSize <= 0 ||
                !sourceBounds.TryValidate(out _) ||
                !Finite(layout.OriginX) || !Finite(layout.OriginZ) || !Finite(layout.WorldWidthMetres) ||
                !Finite(layout.WorldDepthMetres) || layout.WorldWidthMetres <= 0 || layout.WorldDepthMetres <= 0 ||
                candidate.width <= 0 || candidate.height <= 0 || candidate.x < 0 || candidate.y < 0 ||
                candidate.xMax > grid.Width || candidate.yMax > grid.Height) return false;
            float hx = pad.WidthMetres * .5f, hz = pad.DepthMetres * .5f;
            var quad = new Vector2[4];
            Vector2[] local = { new(pad.CenterX - hx, pad.CenterZ - hz), new(pad.CenterX + hx, pad.CenterZ - hz),
                new(pad.CenterX + hx, pad.CenterZ + hz), new(pad.CenterX - hx, pad.CenterZ + hz) };
            for (int i = 0; i < quad.Length; i++)
                if (!layout.TryProjectLocalToMap(local[i].x, local[i].y, out float x, out float z) || !Finite(x) || !Finite(z)) return false;
                else quad[i] = new Vector2(x, z);
            var frame = new Vector2[4];
            float frameMaxX = layout.OriginX + layout.WorldWidthMetres;
            float frameMaxZ = layout.OriginZ + layout.WorldDepthMetres;
            Vector2[] frameLocal = { new(layout.OriginX, layout.OriginZ), new(frameMaxX, layout.OriginZ),
                new(frameMaxX, frameMaxZ), new(layout.OriginX, frameMaxZ) };
            for (int i = 0; i < frame.Length; i++)
                if (!layout.TryProjectLocalToMap(frameLocal[i].x, frameLocal[i].y, out float x, out float z) || !Finite(x) || !Finite(z)) return false;
                else frame[i] = new Vector2(x, z);

            float minX = grid.Origin.x + candidate.x * grid.CellSize;
            float minZ = grid.Origin.z + candidate.y * grid.CellSize;
            float maxX = minX + candidate.width * grid.CellSize;
            float maxZ = minZ + candidate.height * grid.CellSize;
            var playableMin = sourceBounds.PlayableMin;
            var playableMax = sourceBounds.PlayableMax;
            if (minX < playableMin.x || minZ < playableMin.z || maxX > playableMax.x || maxZ > playableMax.z)
                return false;
            var corners = new[] { new Vector2(minX, minZ), new Vector2(maxX, minZ),
                new Vector2(maxX, maxZ), new Vector2(minX, maxZ) };
            foreach (var corner in corners)
                if (!InsideOrEdge(quad, corner) || !InsideOrEdge(frame, corner)) return false;
            return true;
        }

        public static bool RequiresTypedConstraint(bool authoredStartingGrant, bool explicitBoundLaunch) =>
            authoredStartingGrant && explicitBoundLaunch;

        public static bool IsCurrentGrantBinding(bool grantOwnerExists, bool setupRequiresBinding,
            bool requestRequiresTypedPad, bool launchIdentityMatches, bool exactPadBindingExists)
        {
            if (!grantOwnerExists) return false;
            if (!setupRequiresBinding) return !requestRequiresTypedPad;
            return requestRequiresTypedPad && launchIdentityMatches && exactPadBindingExists;
        }

        public static bool AllowsCandidate(bool requireTypedPad, SkirmishMapLayoutConfig layout,
            SkirmishLegalPadConfig pad, GridConfig grid, OperationMapBoundsConfig sourceBounds, RectInt candidate) =>
            !requireTypedPad || ContainsFootprint(layout, pad, grid, sourceBounds, candidate);

        private static bool InsideOrEdge(Vector2[] polygon, Vector2 point)
        {
            float sign = 0;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 edge = polygon[(i + 1) % polygon.Length] - polygon[i];
                Vector2 offset = point - polygon[i];
                float cross = edge.x * offset.y - edge.y * offset.x;
                if (Mathf.Abs(cross) <= .0001f) continue;
                if (sign == 0) sign = Mathf.Sign(cross);
                else if (Mathf.Sign(cross) != sign) return false;
            }
            return sign != 0;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
