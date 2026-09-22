using System;

namespace Game.Operations.Tactical
{
    public sealed class OperationsGreyboxAnchor
    {
        public OperationsGreyboxAnchor(string anchorId, string alias, float x, float z)
        {
            AnchorId = anchorId;
            Alias = alias;
            X = x;
            Z = z;
        }

        public string AnchorId { get; }
        public string Alias { get; }
        public float X { get; }
        public float Z { get; }
    }

    public sealed class OperationsGreyboxRoute
    {
        public OperationsGreyboxRoute(string routeId, string[] anchorIds)
        {
            RouteId = routeId;
            AnchorIds = anchorIds ?? Array.Empty<string>();
        }

        public string RouteId { get; }
        public string[] AnchorIds { get; }
    }

    /// <summary>
    /// Abstract district layout used to keep package 2 rules off a single floor plan.
    /// Coordinates are meters on a plan. This is not a Unity scene and does not import Demo 2 art.
    /// </summary>
    public sealed class OperationsMapGreybox
    {
        public OperationsMapGreybox(
            string mapId,
            int districtNumber,
            OperationsGreyboxAnchor[] anchors,
            OperationsGreyboxRoute[] routes,
            float[] blockerAx,
            float[] blockerAz,
            float[] blockerBx,
            float[] blockerBz)
        {
            MapId = mapId;
            DistrictNumber = districtNumber;
            Anchors = anchors ?? Array.Empty<OperationsGreyboxAnchor>();
            Routes = routes ?? Array.Empty<OperationsGreyboxRoute>();
            BlockerAx = blockerAx ?? Array.Empty<float>();
            BlockerAz = blockerAz ?? Array.Empty<float>();
            BlockerBx = blockerBx ?? Array.Empty<float>();
            BlockerBz = blockerBz ?? Array.Empty<float>();
        }

        public string MapId { get; }
        public int DistrictNumber { get; }
        public OperationsGreyboxAnchor[] Anchors { get; }
        public OperationsGreyboxRoute[] Routes { get; }
        public float[] BlockerAx { get; }
        public float[] BlockerAz { get; }
        public float[] BlockerBx { get; }
        public float[] BlockerBz { get; }

        public bool TryGetByAlias(string alias, out OperationsGreyboxAnchor anchor)
        {
            for (int index = 0; index < Anchors.Length; index++)
            {
                if (string.Equals(Anchors[index].Alias, alias, StringComparison.Ordinal))
                {
                    anchor = Anchors[index];
                    return true;
                }
            }

            anchor = null;
            return false;
        }

        public bool TryGetById(string anchorId, out OperationsGreyboxAnchor anchor)
        {
            for (int index = 0; index < Anchors.Length; index++)
            {
                if (string.Equals(Anchors[index].AnchorId, anchorId, StringComparison.Ordinal))
                {
                    anchor = Anchors[index];
                    return true;
                }
            }

            anchor = null;
            return false;
        }

        public bool TryGetRoute(string routeId, out OperationsGreyboxRoute route)
        {
            for (int index = 0; index < Routes.Length; index++)
            {
                if (string.Equals(Routes[index].RouteId, routeId, StringComparison.Ordinal))
                {
                    route = Routes[index];
                    return true;
                }
            }

            route = null;
            return false;
        }

        public float DistanceByAlias(string fromAlias, string toAlias)
        {
            if (!TryGetByAlias(fromAlias, out OperationsGreyboxAnchor from) ||
                !TryGetByAlias(toAlias, out OperationsGreyboxAnchor to))
                throw new InvalidOperationException("Greybox alias is missing.");
            return OperationsTacticalRules.Distance(from.X, from.Z, to.X, to.Z);
        }
    }

    public static class OperationsMapGreyboxCatalog
    {
        public const string OldQuarterMapId = "opmap.operations.old_quarter";
        public const string CivicCenterMapId = "opmap.operations.civic_center";

        private static readonly OperationsMapGreybox[] Maps =
        {
            CreateOldQuarter(),
            CreateCivicCenter()
        };

        public static OperationsMapGreybox OldQuarter => Maps[0];
        public static OperationsMapGreybox CivicCenter => Maps[1];

        public static bool TryGet(string mapId, out OperationsMapGreybox map)
        {
            for (int index = 0; index < Maps.Length; index++)
            {
                if (string.Equals(Maps[index].MapId, mapId, StringComparison.Ordinal))
                {
                    map = Maps[index];
                    return true;
                }
            }

            map = null;
            return false;
        }

        public static bool TryDistrictForMap(string mapId, out int districtNumber)
        {
            if (TryGet(mapId, out OperationsMapGreybox map))
            {
                districtNumber = map.DistrictNumber;
                return true;
            }

            districtNumber = 0;
            return false;
        }

        private static OperationsMapGreybox CreateOldQuarter()
        {
            OperationsGreyboxAnchor[] anchors =
            {
                Anchor(1, "player_spawn", "spawn.player", 0f, 0f),
                Anchor(1, "enemy_a", "spawn.enemy_a", 48f, 6f),
                Anchor(1, "enemy_b", "spawn.enemy_b", 48f, -8f),
                Anchor(1, "staging_a", "spawn.staging_a", 36f, 6f),
                Anchor(1, "staging_b", "spawn.staging_b", 36f, -8f),
                Anchor(1, "exit", "exit.ground", 0f, 14f),
                Anchor(1, "clinic", "site.clinic", 6f, 2f),
                Anchor(1, "courtyard", "site.courtyard", 2f, 8f),
                Anchor(1, "archive", "site.archive", 6f, -4f),
                Anchor(1, "viewpoint", "site.viewpoint", 16f, 8f),
                Anchor(1, "lane", "site.lane", 8f, -4f),
                Anchor(1, "evidence", "site.evidence", 5f, 2f),
                Anchor(1, "far_post", "site.far_post", 0f, 30f)
            };
            OperationsGreyboxRoute[] routes =
            {
                new("route.main", Ids(anchors, "player_spawn", "courtyard", "exit")),
                new("route.safe", Ids(anchors, "player_spawn", "clinic", "courtyard", "exit")),
                new("route.flank", Ids(anchors, "viewpoint", "archive", "exit"))
            };
            return new OperationsMapGreybox(
                OldQuarterMapId,
                1,
                anchors,
                routes,
                new[] { 10f, 2f },
                new[] { -1f, -0.2f },
                new[] { 10f, 2f },
                new[] { 5f, -3f });
        }

        private static OperationsMapGreybox CreateCivicCenter()
        {
            OperationsGreyboxAnchor[] anchors =
            {
                Anchor(2, "player_spawn", "spawn.player", 0f, 0f),
                Anchor(2, "enemy_a", "spawn.enemy_a", 64f, 16f),
                Anchor(2, "enemy_b", "spawn.enemy_b", 64f, -16f),
                Anchor(2, "staging_a", "spawn.staging_a", 52f, 16f),
                Anchor(2, "staging_b", "spawn.staging_b", 52f, -16f),
                Anchor(2, "exit", "exit.ground", 36f, 0f),
                Anchor(2, "plaza", "site.plaza", 18f, 0f),
                Anchor(2, "clinic", "site.clinic", 28f, 10f),
                Anchor(2, "annex", "site.annex", 28f, -10f),
                Anchor(2, "service_stand", "site.service_stand", 22f, 10f),
                Anchor(2, "cargo_start", "site.cargo_start", 8f, 14f),
                Anchor(2, "cargo_mid", "site.cargo_mid", 20f, 14f),
                Anchor(2, "cargo_exit", "site.cargo_exit", 36f, 6f)
            };
            OperationsGreyboxRoute[] routes =
            {
                new("route.main", Ids(anchors, "cargo_start", "cargo_mid", "cargo_exit")),
                new("route.safe", Ids(anchors, "cargo_start", "plaza", "cargo_exit")),
                new("route.flank", Ids(anchors, "annex", "exit"))
            };
            return new OperationsMapGreybox(
                CivicCenterMapId,
                2,
                anchors,
                routes,
                new[] { 12f },
                new[] { 8f },
                new[] { 12f },
                new[] { 20f });
        }

        private static OperationsGreyboxAnchor Anchor(int district, string slug, string alias, float x, float z) =>
            new("anchor.operations.d" + district.ToString("00") + "." + slug, alias, x, z);

        private static string[] Ids(OperationsGreyboxAnchor[] anchors, params string[] slugs)
        {
            var ids = new string[slugs.Length];
            for (int slugIndex = 0; slugIndex < slugs.Length; slugIndex++)
            {
                string suffix = "." + slugs[slugIndex];
                string found = null;
                for (int index = 0; index < anchors.Length; index++)
                {
                    if (anchors[index].AnchorId.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        found = anchors[index].AnchorId;
                        break;
                    }
                }

                if (found == null)
                    throw new InvalidOperationException("Greybox route anchor is missing: " + slugs[slugIndex]);
                ids[slugIndex] = found;
            }

            return ids;
        }
    }
}
