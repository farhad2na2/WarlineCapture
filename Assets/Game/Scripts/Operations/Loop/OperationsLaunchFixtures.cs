using System;
using Game.Operations.Contracts;
using Game.Operations.Tactical;

namespace Game.Operations.Loop
{
    /// <summary>
    /// Greybox graphs already proven by package 2. They let the launch loop run a
    /// real tactical session. They are not authored O001–O003 mission content.
    /// </summary>
    public static class OperationsLaunchFixtures
    {
        public const string OldQuarterHash = "ops-greybox-d01-v1";
        public const string CivicCenterHash = "ops-greybox-d02-v1";

        public static bool TryCompile(string mapId, out OperationsCompiledTactical definition, out string contentHash, out string error)
        {
            definition = null;
            contentHash = string.Empty;
            error = string.Empty;
            OperationsTacticalAuthoring authoring;
            if (mapId == OperationsMapGreyboxCatalog.OldQuarterMapId)
            {
                authoring = OldQuarter();
                contentHash = OldQuarterHash;
            }
            else if (mapId == OperationsMapGreyboxCatalog.CivicCenterMapId)
            {
                authoring = CivicCenter();
                contentHash = CivicCenterHash;
            }
            else
            {
                error = "missing_map";
                return false;
            }

            OperationsTacticalCompileResult compiled = OperationsTacticalCompiler.Compile(authoring);
            if (!compiled.Accepted)
            {
                error = compiled.Error;
                return false;
            }

            definition = compiled.Definition;
            return true;
        }

        public static string Anchor(string mapId, string alias)
        {
            if (!OperationsMapGreyboxCatalog.TryGet(mapId, out OperationsMapGreybox map) ||
                !map.TryGetByAlias(alias, out OperationsGreyboxAnchor anchor))
                throw new InvalidOperationException("missing_anchor:" + alias);
            return anchor.AnchorId;
        }

        public static string[] ApproachRoutes(string mapId)
        {
            if (!OperationsMapGreyboxCatalog.TryGet(mapId, out OperationsMapGreybox map))
                return Array.Empty<string>();
            var routes = new string[map.Routes.Length];
            for (int index = 0; index < map.Routes.Length; index++)
                routes[index] = map.Routes[index].RouteId;
            return routes;
        }

        private static OperationsTacticalAuthoring OldQuarter()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.d01.greybox",
                MapId = map.MapId,
                PartialNodeIds = new[] { "scan_clinic" },
                MandatoryEvidenceIds = new[] { "site.d01.evidence" },
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "scan_clinic",
                        Rule = OperationsObjectiveRuleKind.Scan,
                        TargetIds = new[] { "site.d01.clinic" },
                        DurationTicks = OperationsTacticalRules.ScanSeconds,
                        RadiusMeters = (int)OperationsTacticalRules.ScanMeters,
                        HiddenUntilObserved = true
                    },
                    Interact("interact_evidence", "site.d01.evidence", 8),
                    OptionalHold(map),
                    Extract(new[] { "scan_clinic", "interact_evidence" })
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map.MapId, "spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor(map.MapId, "spawn.player")),
                    Site("site.d01.clinic", "role.neutral.clinic", Anchor(map.MapId, "site.clinic"), 100),
                    Evidence("site.d01.evidence", Anchor(map.MapId, "site.evidence")),
                    Hostile("hostile.d01.rifle.01", Anchor(map.MapId, "spawn.enemy_b"), 0),
                    Hostile("hostile.d01.rifle.a.01", Anchor(map.MapId, "spawn.enemy_a"), 1)
                },
                Waves = new[]
                {
                    new OperationsTacticalWaveAuthoring
                    {
                        Group = 1,
                        Trigger = OperationsWaveTriggerKind.FirstRequiredCompletion,
                        WarningSeconds = 30
                    }
                }
            };
        }

        private static OperationsTacticalAuthoring CivicCenter()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.d02.greybox",
                MapId = map.MapId,
                ForcePackage = OperationsForcePackageKind.Service,
                Materials = 240,
                DeadlineTicks = 900,
                PartialNodeIds = new[] { "hold_plaza" },
                Nodes = new[]
                {
                    Hold("hold_plaza", Anchor(map.MapId, "site.plaza"), 8, null),
                    Repair("repair_clinic", "site.d02.clinic"),
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "escort_cargo",
                        Rule = OperationsObjectiveRuleKind.Escort,
                        TargetIds = new[] { "unit.d02.cargo.01" },
                        TargetCount = 1,
                        LegalRouteIds = new[] { "route.main", "route.safe" }
                    }
                },
                Spawns = new[]
                {
                    Rifle("unit.d02.rifle.01", Anchor(map.MapId, "spawn.player")),
                    Rifle("unit.d02.rifle.02", Anchor(map.MapId, "spawn.player")),
                    RepairSpecialist("unit.d02.repair.01", Anchor(map.MapId, "site.service_stand")),
                    Site("site.d02.clinic", "role.neutral.clinic", Anchor(map.MapId, "site.clinic"), 40),
                    Cargo("unit.d02.cargo.01", Anchor(map.MapId, "site.cargo_start"))
                }
            };
        }

        private static OperationsTacticalNodeAuthoring OptionalHold(OperationsMapGreybox map)
        {
            OperationsTacticalNodeAuthoring hold = Hold("hold_courtyard", Anchor(map.MapId, "site.courtyard"), 20, null);
            hold.Optional = true;
            return hold;
        }

        private static OperationsTacticalNodeAuthoring Hold(string nodeId, string zoneAnchorId, int seconds, string[] prerequisites)
        {
            string[] required = prerequisites ?? Array.Empty<string>();
            return new OperationsTacticalNodeAuthoring
            {
                NodeId = nodeId,
                Rule = OperationsObjectiveRuleKind.Hold,
                ZoneAnchorId = zoneAnchorId,
                DurationTicks = seconds,
                RadiusMeters = (int)OperationsTacticalRules.HoldMeters,
                Prerequisites = required,
                Activation = required.Length == 0
                    ? OperationsActivationPolicyKind.Launch
                    : OperationsActivationPolicyKind.Prerequisites
            };
        }

        private static OperationsTacticalNodeAuthoring Interact(string nodeId, string targetId, int seconds)
        {
            return new OperationsTacticalNodeAuthoring
            {
                NodeId = nodeId,
                Rule = OperationsObjectiveRuleKind.Interact,
                TargetIds = new[] { targetId },
                DurationTicks = seconds,
                RadiusMeters = (int)OperationsTacticalRules.InteractMeters
            };
        }

        private static OperationsTacticalNodeAuthoring Repair(string nodeId, string siteId)
        {
            return new OperationsTacticalNodeAuthoring
            {
                NodeId = nodeId,
                Rule = OperationsObjectiveRuleKind.Repair,
                TargetIds = new[] { siteId },
                DurationTicks = OperationsTacticalRules.RepairSeconds,
                RadiusMeters = (int)OperationsTacticalRules.RepairMeters
            };
        }

        private static OperationsTacticalNodeAuthoring Extract(string[] prerequisites)
        {
            return new OperationsTacticalNodeAuthoring
            {
                NodeId = "extract_force",
                Rule = OperationsObjectiveRuleKind.Extract,
                TargetCount = 2,
                Prerequisites = prerequisites,
                Activation = OperationsActivationPolicyKind.Prerequisites
            };
        }

        private static OperationsTacticalSpawnAuthoring Rifle(string objectId, string anchorId) => new()
        {
            ObjectId = objectId,
            RoleId = "role.friendly.rifle",
            RosterRole = OperationsRosterRoleKind.RifleInfantry,
            Faction = OperationsTacticalFaction.Player,
            Body = OperationsTacticalBodyKind.Infantry,
            AnchorId = anchorId,
            Health = 100
        };

        private static OperationsTacticalSpawnAuthoring RepairSpecialist(string objectId, string anchorId) => new()
        {
            ObjectId = objectId,
            RoleId = "role.friendly.repair",
            RosterRole = OperationsRosterRoleKind.RepairSpecialist,
            Faction = OperationsTacticalFaction.Player,
            Body = OperationsTacticalBodyKind.Infantry,
            AnchorId = anchorId,
            Health = 100
        };

        private static OperationsTacticalSpawnAuthoring Hostile(string objectId, string anchorId, int group) => new()
        {
            ObjectId = objectId,
            RoleId = "role.hostile.rifle",
            RosterRole = OperationsRosterRoleKind.RifleInfantry,
            Faction = OperationsTacticalFaction.Hostile,
            Body = OperationsTacticalBodyKind.Infantry,
            AnchorId = anchorId,
            Group = group,
            Health = 100
        };

        private static OperationsTacticalSpawnAuthoring Site(string objectId, string roleId, string anchorId, int health) => new()
        {
            ObjectId = objectId,
            RoleId = roleId,
            RosterRole = OperationsRosterRoleKind.None,
            Faction = OperationsTacticalFaction.Neutral,
            Body = OperationsTacticalBodyKind.Site,
            AnchorId = anchorId,
            Health = health
        };

        private static OperationsTacticalSpawnAuthoring Evidence(string objectId, string anchorId) => new()
        {
            ObjectId = objectId,
            RoleId = "role.neutral.evidence",
            RosterRole = OperationsRosterRoleKind.None,
            Faction = OperationsTacticalFaction.Neutral,
            Body = OperationsTacticalBodyKind.Evidence,
            AnchorId = anchorId,
            Health = 100
        };

        private static OperationsTacticalSpawnAuthoring Cargo(string objectId, string anchorId) => new()
        {
            ObjectId = objectId,
            RoleId = "role.friendly.cargo",
            RosterRole = OperationsRosterRoleKind.CargoTruck,
            Faction = OperationsTacticalFaction.Player,
            Body = OperationsTacticalBodyKind.Cargo,
            AnchorId = anchorId,
            Health = 100,
            HasCargo = true
        };
    }
}
