using System;

namespace Game.Configs
{
    /// <summary>
    /// Per-mission HUD guidance (owner designer playtest 2026-11-17). Data only: a first-order
    /// prompt drawn from the mission brief, shown at match start, and an optional rendezvous
    /// marker on an anchor that already exists in the mission's map layout. Missions without a
    /// row show nothing. It never issues orders and adds no geometry.
    /// </summary>
    public readonly struct SkirmishMissionHudGuidance
    {
        public readonly string CatalogId;
        public readonly string FirstOrderKey, FirstOrderEnglish;
        public readonly string RendezvousAnchorId;
        public readonly string RendezvousLabelKey, RendezvousLabelEnglish;

        public SkirmishMissionHudGuidance(string catalogId, string firstOrderKey, string firstOrderEnglish,
            string rendezvousAnchorId, string rendezvousLabelKey, string rendezvousLabelEnglish)
        {
            CatalogId = catalogId; FirstOrderKey = firstOrderKey; FirstOrderEnglish = firstOrderEnglish;
            RendezvousAnchorId = rendezvousAnchorId; RendezvousLabelKey = rendezvousLabelKey; RendezvousLabelEnglish = rendezvousLabelEnglish;
        }

        public bool HasFirstOrder => !string.IsNullOrEmpty(FirstOrderKey);
        public bool HasRendezvous => !string.IsNullOrEmpty(RendezvousAnchorId);
    }

    public static class SkirmishMissionHudGuidanceCatalog
    {
        /// <summary>The first-order prompt shows for this long from match start (or until dismissed).</summary>
        public const float FirstOrderSeconds = 45f;

        // S026 City Crossroads: brief "advance infantry along the main avenue to the
        // crossroads". City Crossroads reuses the Desert Base source and its draft layout has no
        // junction anchor, so the rendezvous sits on the main-route midpoint
        // (route.skirmish.s026.main, waypoint 03 of 00-06, ~197 m of 394.6 m) and is labelled
        // RENDEZVOUS only. Move RendezvousAnchorId (and the label) here once a road junction
        // anchor is verified; no code change is needed.
        private static readonly SkirmishMissionHudGuidance[] Rows =
        {
            new("S026", "skirmish.s026.first_order", "Advance your infantry along the main avenue to the crossroads.",
                "anchor.skirmish.s026.candidate_route_tank_primary_waypoint_03",
                "skirmish.s026.rendezvous", "RENDEZVOUS"),
        };

        public static bool TryGet(string catalogId, out SkirmishMissionHudGuidance guidance)
        {
            foreach (var row in Rows)
                if (string.Equals(row.CatalogId, catalogId, StringComparison.Ordinal)) { guidance = row; return true; }
            guidance = default;
            return false;
        }

        public static bool ShowFirstOrder(in SkirmishMissionHudGuidance guidance, float elapsedSeconds) =>
            guidance.HasFirstOrder && elapsedSeconds >= 0f && elapsedSeconds < FirstOrderSeconds;
    }
}
