using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishMapLayoutValidation
    {
        public const float HighwayInfantryContactMin = 45f;
        public const float HighwayInfantryContactMax = 75f;
        public const float FlankInfantryContactMin = 75f;
        public const float FlankInfantryContactMax = 105f;
        public const float ProjectionEpsilon = 0.05f;

        private static readonly string[] RequiredRoles =
        {
            "base.player",
            "base.enemy",
            "staging.player",
            "staging.enemy",
            "economy.player",
            "economy.enemy",
            "service.player",
            "service.enemy"
        };

        public static bool TryValidateDesertBaseAssault(
            SkirmishMapLayoutConfig layout,
            List<SkirmishCompileReason> reasons)
        {
            reasons ??= new List<SkirmishCompileReason>();
            int start = reasons.Count;
            if (layout == null)
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "mapLayout", "Layout is null."));
                return false;
            }

            if (layout.LayoutId != SkirmishMapLayoutBuilder.DesertBaseLayoutId)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InvalidIdentity, "layoutId", layout.LayoutId));
            if (layout.OperationMapId != SkirmishMapLayoutBuilder.DesertBaseMapId)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.UnsupportedMap, "operationMapId", layout.OperationMapId));
            if (layout.WorldWidthMetres <= 0f || layout.WorldDepthMetres <= 0f)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, "worldBounds", "Envelope is missing."));
            if (layout.CellSize <= 0f)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, "cellSize", "Cell size must be positive."));

            for (int i = 0; i < RequiredRoles.Length; i++)
            {
                if (!layout.TryGetAnchor(RequiredRoles[i], out _))
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, "mapLayout", RequiredRoles[i]));
            }

            SkirmishLayoutAnchorConfig[] anchors = layout.Anchors;
            for (int i = 0; i < anchors.Length; i++)
            {
                SkirmishLayoutAnchorConfig anchor = anchors[i];
                if (string.IsNullOrEmpty(anchor.AnchorId) || anchor.AnchorId.Length > 60)
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InvalidIdentity, "anchorId", anchor.AnchorId));
                SkirmishMapLayoutConfig.ProjectNormalized(
                    anchor.NormalizedU,
                    anchor.NormalizedV,
                    layout.OriginX,
                    layout.OriginZ,
                    layout.WorldWidthMetres,
                    layout.WorldDepthMetres,
                    out float expectedX,
                    out float expectedZ);
                if (Abs(anchor.WorldX - expectedX) > ProjectionEpsilon ||
                    Abs(anchor.WorldZ - expectedZ) > ProjectionEpsilon)
                {
                    reasons.Add(new SkirmishCompileReason(
                        SkirmishReasonCode.InsufficientAnchor,
                        anchor.AnchorId,
                        "World transform does not match the measured envelope."));
                }

                if (!InsideEnvelope(layout, anchor.WorldX, anchor.WorldZ, 0f, 0f))
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.BlockedSpawn, anchor.AnchorId, "Anchor leaves the envelope."));
            }

            SkirmishLegalPadConfig[] pads = layout.Pads;
            if (pads == null || pads.Length == 0)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, "pads", "Legal pads are missing."));
            else
            {
                RequirePad(layout, 1, SkirmishLegalPadKind.BaseBarracks, reasons);
                RequirePad(layout, 2, SkirmishLegalPadKind.BaseBarracks, reasons);
                RequirePad(layout, 1, SkirmishLegalPadKind.GroundStaging, reasons);
                RequirePad(layout, 2, SkirmishLegalPadKind.GroundStaging, reasons);
                RequirePad(layout, 1, SkirmishLegalPadKind.InfantrySpawn, reasons);
                RequirePad(layout, 2, SkirmishLegalPadKind.InfantrySpawn, reasons);
                RequirePad(layout, 1, SkirmishLegalPadKind.VehicleSpawn, reasons);
                RequirePad(layout, 2, SkirmishLegalPadKind.VehicleSpawn, reasons);
                for (int i = 0; i < pads.Length; i++)
                {
                    SkirmishLegalPadConfig pad = pads[i];
                    if (!InsideEnvelope(layout, pad.CenterX, pad.CenterZ, pad.WidthMetres, pad.DepthMetres))
                        reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.BlockedSpawn, pad.PadId, "Pad leaves the envelope."));
                    for (int j = i + 1; j < pads.Length; j++)
                    {
                        if (Overlaps(pad, pads[j]))
                            reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.BlockedSpawn, pad.PadId, "Overlaps " + pads[j].PadId));
                    }
                }
            }

            if (!layout.TryGetRoute(SkirmishMeasuredRouteKind.MainHighway, out SkirmishLayoutRouteConfig highway) ||
                !layout.TryGetRoute(SkirmishMeasuredRouteKind.FlankNorthRuins, out SkirmishLayoutRouteConfig north) ||
                !layout.TryGetRoute(SkirmishMeasuredRouteKind.FlankSouthSweep, out SkirmishLayoutRouteConfig south))
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, "routes", "Highway and both flanks are required."));
            }
            else
            {
                ValidateRoute(layout, highway, HighwayInfantryContactMin, HighwayInfantryContactMax, 12f, reasons);
                ValidateRoute(layout, north, FlankInfantryContactMin, FlankInfantryContactMax, 6f, reasons);
                ValidateRoute(layout, south, FlankInfantryContactMin, FlankInfantryContactMax, 12f, reasons);
                if (SharesInteriorWaypoint(highway, north) || SharesInteriorWaypoint(highway, south) || SharesInteriorWaypoint(north, south))
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, "routes", "Approaches must keep independent interiors."));
            }

            return reasons.Count == start;
        }

        private static void RequirePad(
            SkirmishMapLayoutConfig layout,
            byte faction,
            SkirmishLegalPadKind kind,
            List<SkirmishCompileReason> reasons)
        {
            if (!layout.TryGetPad(faction, kind, out _))
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.BlockedSpawn, "pads", faction + ":" + kind));
        }

        private static void ValidateRoute(
            SkirmishMapLayoutConfig layout,
            SkirmishLayoutRouteConfig route,
            float contactMin,
            float contactMax,
            float minWidth,
            List<SkirmishCompileReason> reasons)
        {
            if (route.WaypointAnchorIds == null || route.WaypointAnchorIds.Length < 2)
            {
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, route.RouteId, "Route needs two or more waypoints."));
                return;
            }

            var points = new List<SkirmishLayoutAnchorConfig>(route.WaypointAnchorIds.Length);
            for (int i = 0; i < route.WaypointAnchorIds.Length; i++)
            {
                if (!layout.TryGetAnchorById(route.WaypointAnchorIds[i], out SkirmishLayoutAnchorConfig waypoint))
                {
                    reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, route.RouteId, route.WaypointAnchorIds[i]));
                    continue;
                }

                points.Add(waypoint);
            }

            float length = SkirmishMapLayoutBuilder.PolylineLength(points);
            if (Abs(length - route.LengthMetres) > 1f)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientAnchor, route.RouteId, "Stored length does not match waypoints."));
            if (route.WidthMetres + 0.01f < minWidth)
                reasons.Add(new SkirmishCompileReason(SkirmishReasonCode.InsufficientRunway, route.RouteId, "Route is narrower than the allowed footprint."));
            if (route.InfantryFirstContactSeconds < contactMin - 0.5f ||
                route.InfantryFirstContactSeconds > contactMax + 0.5f)
            {
                reasons.Add(new SkirmishCompileReason(
                    SkirmishReasonCode.InsufficientAnchor,
                    route.RouteId,
                    "Infantry first-contact is outside the documented envelope."));
            }
        }

        private static bool SharesInteriorWaypoint(SkirmishLayoutRouteConfig a, SkirmishLayoutRouteConfig b)
        {
            if (a.WaypointAnchorIds == null || b.WaypointAnchorIds == null)
                return false;
            for (int i = 1; i < a.WaypointAnchorIds.Length - 1; i++)
            {
                for (int j = 1; j < b.WaypointAnchorIds.Length - 1; j++)
                {
                    if (a.WaypointAnchorIds[i] == b.WaypointAnchorIds[j])
                        return true;
                }
            }

            return false;
        }

        private static bool InsideEnvelope(
            SkirmishMapLayoutConfig layout,
            float x,
            float z,
            float width,
            float depth)
        {
            float minX = layout.OriginX + width * 0.5f;
            float maxX = layout.OriginX + layout.WorldWidthMetres - width * 0.5f;
            float minZ = layout.OriginZ + depth * 0.5f;
            float maxZ = layout.OriginZ + layout.WorldDepthMetres - depth * 0.5f;
            return x >= minX && x <= maxX && z >= minZ && z <= maxZ;
        }

        private static bool Overlaps(SkirmishLegalPadConfig a, SkirmishLegalPadConfig b)
        {
            return Abs(a.CenterX - b.CenterX) * 2f < a.WidthMetres + b.WidthMetres &&
                   Abs(a.CenterZ - b.CenterZ) * 2f < a.DepthMetres + b.DepthMetres;
        }

        private static float Abs(float value) => value < 0f ? -value : value;
    }
}
