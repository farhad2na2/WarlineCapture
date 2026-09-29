using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.MapVariants
{
    // Demo map variants are standalone scenes; nothing in the campaign references them yet.
    public static class MapVariantMenu
    {
        [MenuItem("Game/Map Variants/Build Refinery District")]
        public static void BuildRefineryDistrict() =>
            Run(MapVariantRefineryDistrict.MapId, MapVariantRefineryDistrict.Build, MapVariantRefineryDistrict.Views);

        [MenuItem("Game/Map Variants/Build Ash Line Port")]
        public static void BuildAshLinePort() =>
            Run(MapVariantLogisticsPort.MapId, MapVariantLogisticsPort.Build, MapVariantLogisticsPort.Views);

        [MenuItem("Game/Map Variants/Build City-Edge Airfield")]
        public static void BuildCityEdgeAirfield() =>
            Run(MapVariantCityAirfield.MapId, MapVariantCityAirfield.Build, MapVariantCityAirfield.Views);

        [MenuItem("Game/Map Variants/Build Frontier (2048x1024)")]
        public static void BuildFrontier() =>
            Run(MapVariantFrontier.MapId, MapVariantFrontier.Build, MapVariantFrontier.Views);

        private static void Run(
            string mapId,
            Func<MapVariantBuilder> build,
            Func<MapVariantBuilder, IEnumerable<MapVariantView>> views)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("[MapVariants] Exit play mode before building map variants.");

            var started = DateTime.UtcNow;
            Scene scene = MapVariantBuilder.NewScene();
            MapVariantBuilder builder = build();
            MapVariantAuditReport report = builder.Audit();
            builder.Save(scene, report);
            List<string> captures = builder.Capture(views(builder).ToList());

            foreach (string issue in report.issues)
                Debug.LogWarning($"[MapVariants] map={mapId} issue={issue}");
            string summary =
                $"map={mapId} placements={report.placements} reserved={report.reservedPlacements} roads={report.roadCells} " +
                $"rejected={report.rejectedCandidates} floating={report.floating} buried={report.buried} " +
                $"detached={report.detachedParts} unsupported={report.unsupportedAttachments} overlaps={report.overlaps} " +
                $"onRoad={report.roadIntrusions} drift={report.transformDrift} outside={report.outsideWorld} " +
                $"captures={captures.Count} scene={builder.ScenePath} seconds={(DateTime.UtcNow - started).TotalSeconds:F0}";
            if (!report.Passed)
                throw new InvalidOperationException($"[MapVariants] result=Failed {summary}");
            Debug.Log($"[MapVariants] result=Passed {summary}");
        }
    }
}
