using System;
using System.Collections.Generic;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Operations.Content
{
    /// <summary>
    /// Fat-thumb escort/repair control contract for O002/O003.
    /// Go / Hold / route chips and clinic/pump warnings — content + enablement only.
    /// Programmer 2 presentation shell renders chips; do not invent a second void HUD here.
    /// </summary>
    public enum OperationsFatThumbChipKind : byte
    {
        Go = 0,
        Hold = 1,
        Route = 2,
        Repair = 3
    }

    public readonly struct OperationsFatThumbChip
    {
        public OperationsFatThumbChip(
            OperationsFatThumbChipKind kind,
            string labelKey,
            string routeId,
            bool enabled,
            bool selected)
        {
            Kind = kind;
            LabelKey = labelKey ?? string.Empty;
            RouteId = routeId ?? string.Empty;
            Enabled = enabled;
            Selected = selected;
        }

        public OperationsFatThumbChipKind Kind { get; }
        public string LabelKey { get; }
        public string RouteId { get; }
        public bool Enabled { get; }
        public bool Selected { get; }
    }

    public readonly struct OperationsEscortRepairControlFrame
    {
        public OperationsEscortRepairControlFrame(
            string missionId,
            OperationsFatThumbChip[] chips,
            string warningKey,
            bool warningVisible,
            string warningSeverityKey)
        {
            MissionId = missionId ?? string.Empty;
            Chips = chips ?? Array.Empty<OperationsFatThumbChip>();
            WarningKey = warningKey ?? string.Empty;
            WarningVisible = warningVisible;
            WarningSeverityKey = warningSeverityKey ?? string.Empty;
        }

        public string MissionId { get; }
        public OperationsFatThumbChip[] Chips { get; }
        public string WarningKey { get; }
        public bool WarningVisible { get; }
        public string WarningSeverityKey { get; }
    }

    public static class OperationsEscortRepairControls
    {
        public static bool TryRead(OperationsLoopSession loop, out OperationsEscortRepairControlFrame frame)
        {
            frame = default;
            if (loop == null || !loop.HasMission || loop.Phase != OperationsLoopPhase.Active || loop.MissionTerminal)
                return false;

            string missionId = loop.MissionId;
            if (missionId == "operation.o002")
                return TryReadO002(loop, out frame);
            if (missionId == "operation.o003")
                return TryReadO003(loop, out frame);
            return false;
        }

        static bool TryReadO002(OperationsLoopSession loop, out OperationsEscortRepairControlFrame frame)
        {
            bool escortActive = NodeActive(loop, "escort_trucks");
            bool holdActive = NodeActive(loop, "hold_clinic");
            string[] routes = LegalRoutes(loop, "escort_trucks");
            var chips = new List<OperationsFatThumbChip>(4 + routes.Length);

            chips.Add(new OperationsFatThumbChip(
                OperationsFatThumbChipKind.Go,
                "operations.controls.escort.go",
                string.Empty,
                escortActive,
                false));
            chips.Add(new OperationsFatThumbChip(
                OperationsFatThumbChipKind.Hold,
                "operations.controls.escort.hold",
                string.Empty,
                escortActive || holdActive,
                false));

            for (int index = 0; index < routes.Length; index++)
            {
                string routeId = routes[index];
                string labelKey = routeId == "route.safe"
                    ? "operations.controls.route.safe"
                    : "operations.controls.route.main";
                chips.Add(new OperationsFatThumbChip(
                    OperationsFatThumbChipKind.Route,
                    labelKey,
                    routeId,
                    escortActive,
                    false));
            }

            bool clinicAlive = SiteAlive(loop, "site.d01.clinic");
            frame = new OperationsEscortRepairControlFrame(
                "operation.o002",
                chips.ToArray(),
                "operations.warning.clinic",
                true,
                clinicAlive ? "operations.warning.severity.watch" : "operations.warning.severity.critical");
            return true;
        }

        static bool TryReadO003(OperationsLoopSession loop, out OperationsEscortRepairControlFrame frame)
        {
            bool repairWest = NodeActive(loop, "repair_pump_west");
            bool repairEast = NodeActive(loop, "repair_pump_east");
            bool holdCourt = NodeActive(loop, "hold_service_court");
            bool repairEnabled = repairWest || repairEast;

            var chips = new List<OperationsFatThumbChip>(4)
            {
                new OperationsFatThumbChip(
                    OperationsFatThumbChipKind.Repair,
                    "operations.controls.repair",
                    string.Empty,
                    repairEnabled,
                    false),
                new OperationsFatThumbChip(
                    OperationsFatThumbChipKind.Hold,
                    "operations.controls.defend.hold",
                    string.Empty,
                    holdCourt,
                    false),
                new OperationsFatThumbChip(
                    OperationsFatThumbChipKind.Go,
                    "operations.controls.escort.go",
                    string.Empty,
                    false,
                    false)
            };

            bool pumpsThreatened = !SiteAlive(loop, "site.d01.pump_west") ||
                                   !SiteAlive(loop, "site.d01.pump_east") ||
                                   SiteDamaged(loop, "site.d01.pump_west") ||
                                   SiteDamaged(loop, "site.d01.pump_east");
            bool clinicAlive = SiteAlive(loop, "site.d01.clinic");

            string warningKey = pumpsThreatened
                ? "operations.warning.pumps"
                : "operations.warning.clinic";
            string severity = !clinicAlive || pumpsThreatened
                ? "operations.warning.severity.critical"
                : "operations.warning.severity.watch";

            frame = new OperationsEscortRepairControlFrame(
                "operation.o003",
                chips.ToArray(),
                warningKey,
                true,
                severity);
            return true;
        }

        static string[] LegalRoutes(OperationsLoopSession loop, string nodeId)
        {
            if (!loop.TryNode(nodeId, out OperationsTacticalNodeState state))
                return OperationsAuthoredMissions.ApproachRoutes(loop.MissionId);
            if (state.LegalRouteIds != null && state.LegalRouteIds.Length > 0)
                return state.LegalRouteIds;
            return OperationsAuthoredMissions.ApproachRoutes(loop.MissionId);
        }

        static bool NodeActive(OperationsLoopSession loop, string nodeId) =>
            loop.TryNode(nodeId, out OperationsTacticalNodeState state) &&
            state.Phase == OperationsTacticalNodePhase.Active;

        static bool SiteAlive(OperationsLoopSession loop, string siteId) =>
            loop.TryActor(siteId, out OperationsTacticalActorState actor) && actor.Alive;

        static bool SiteDamaged(OperationsLoopSession loop, string siteId) =>
            loop.TryActor(siteId, out OperationsTacticalActorState actor) &&
            actor.Alive &&
            actor.Health < 100;
    }
}
