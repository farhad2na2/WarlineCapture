using System;
using Game.Operations.Contracts;
using Game.Operations.Tactical;

namespace Game.Operations.Loop
{
    /// <summary>
    /// Package 4 authored O001–O003 graphs on D01 Old Quarter.
    /// Shared rule systems only; no per-mission controllers.
    /// </summary>
    public static class OperationsAuthoredMissions
    {
        public const string O001Hash = "ops-authored-o001-v1";
        public const string O002Hash = "ops-authored-o002-v1";
        public const string O003Hash = "ops-authored-o003-v1";

        public static bool IsVerticalSlice(string missionId) =>
            missionId == "operation.o001" ||
            missionId == "operation.o002" ||
            missionId == "operation.o003";

        public static bool TryCompile(
            string missionId,
            out OperationsCompiledTactical definition,
            out string contentHash,
            out string error)
        {
            definition = null;
            contentHash = string.Empty;
            error = string.Empty;
            OperationsTacticalAuthoring authoring;
            switch (missionId)
            {
                case "operation.o001":
                    authoring = O001();
                    contentHash = O001Hash;
                    break;
                case "operation.o002":
                    authoring = O002();
                    contentHash = O002Hash;
                    break;
                case "operation.o003":
                    authoring = O003();
                    contentHash = O003Hash;
                    break;
                default:
                    error = "missing_mission";
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

        public static string[] ApproachRoutes(string missionId)
        {
            if (missionId == "operation.o002")
                return new[] { "route.main", "route.safe" };
            if (IsVerticalSlice(missionId))
                return new[] { "route.main", "route.safe" };
            return Array.Empty<string>();
        }

        private static OperationsTacticalAuthoring O001()
        {
            string mapId = OperationsMapGreyboxCatalog.OldQuarterMapId;
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.o001",
                MapId = mapId,
                ForcePackage = OperationsForcePackageKind.Light,
                EnemyPackage = OperationsEnemyPackageKind.Cell,
                Materials = 80,
                DeadlineTicks = 720,
                PartialProgressNodeId = "scan_signals",
                PartialProgressMinimum = 2,
                PartialExtractMinimum = 2,
                MandatoryEvidenceIds = new[] { "site.d01.relay_evidence" },
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "scan_signals",
                        Rule = OperationsObjectiveRuleKind.Scan,
                        TargetIds = new[] { "site.d01.signal_a", "site.d01.signal_b", "site.d01.signal_c" },
                        TargetCount = 3,
                        DurationTicks = OperationsTacticalRules.ScanSeconds,
                        RadiusMeters = (int)OperationsTacticalRules.ScanMeters,
                        HiddenUntilObserved = true
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "interact_relay",
                        Rule = OperationsObjectiveRuleKind.Interact,
                        TargetIds = new[] { "site.d01.relay_evidence" },
                        DurationTicks = 15,
                        RadiusMeters = (int)OperationsTacticalRules.InteractMeters,
                        Prerequisites = new[] { "scan_signals" },
                        Activation = OperationsActivationPolicyKind.Prerequisites
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "extract_force",
                        Rule = OperationsObjectiveRuleKind.Extract,
                        TargetCount = 2,
                        Prerequisites = new[] { "interact_relay" },
                        Activation = OperationsActivationPolicyKind.Prerequisites
                    }
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor("spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor("spawn.player")),
                    Rifle("unit.d01.rifle.03", Anchor("spawn.player")),
                    Recon("unit.d01.recon.01", Anchor("spawn.player")),
                    Recon("unit.d01.recon.02", Anchor("spawn.player")),
                    Site("site.d01.signal_a", "role.neutral.signal_a", Anchor("site.signal_a"), 100),
                    Site("site.d01.signal_b", "role.neutral.signal_b", Anchor("site.signal_b"), 100),
                    Site("site.d01.signal_c", "role.neutral.signal_c", Anchor("site.signal_c"), 100),
                    Evidence("site.d01.relay_evidence", Anchor("site.evidence")),
                    Hostile("hostile.d01.rifle.i.01", "role.hostile.rifle", Anchor("spawn.enemy_b"), 0),
                    Hostile("hostile.d01.rifle.a.01", "role.hostile.rifle", Anchor("spawn.enemy_a"), 1),
                    Hostile("hostile.d01.rifle.b.01", "role.hostile.rifle", Anchor("spawn.enemy_b"), 2)
                },
                Waves = new[]
                {
                    new OperationsTacticalWaveAuthoring
                    {
                        Group = 1,
                        Trigger = OperationsWaveTriggerKind.NodeCompletion,
                        TriggerNodeId = "scan_signals",
                        WarningSeconds = 30
                    },
                    new OperationsTacticalWaveAuthoring
                    {
                        Group = 2,
                        Trigger = OperationsWaveTriggerKind.NodeCompletion,
                        TriggerNodeId = "interact_relay",
                        WarningSeconds = 45
                    }
                }
            };
        }

        private static OperationsTacticalAuthoring O002()
        {
            string mapId = OperationsMapGreyboxCatalog.OldQuarterMapId;
            string clinic = Anchor("site.clinic");
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.o002",
                MapId = mapId,
                ForcePackage = OperationsForcePackageKind.Service,
                EnemyPackage = OperationsEnemyPackageKind.Raiders,
                Materials = 240,
                DeadlineTicks = 840,
                PartialProgressNodeId = "escort_trucks",
                PartialProgressMinimum = 1,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "scan_junction",
                        Rule = OperationsObjectiveRuleKind.Scan,
                        TargetIds = new[] { "site.d01.junction" },
                        TargetCount = 1,
                        DurationTicks = OperationsTacticalRules.ScanSeconds,
                        RadiusMeters = (int)OperationsTacticalRules.ScanMeters,
                        HiddenUntilObserved = true
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "escort_trucks",
                        Rule = OperationsObjectiveRuleKind.Escort,
                        TargetIds = new[] { "unit.d01.cargo.01", "unit.d01.cargo.02", "unit.d01.cargo.03" },
                        TargetCount = 2,
                        LegalRouteIds = new[] { "route.main", "route.safe" },
                        Prerequisites = new[] { "scan_junction" },
                        Activation = OperationsActivationPolicyKind.Prerequisites
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "hold_clinic",
                        Rule = OperationsObjectiveRuleKind.Hold,
                        ZoneAnchorId = clinic,
                        DurationTicks = 30,
                        RadiusMeters = (int)OperationsTacticalRules.HoldMeters,
                        Prerequisites = new[] { "escort_trucks" },
                        Activation = OperationsActivationPolicyKind.Prerequisites
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "protect_clinic",
                        Rule = OperationsObjectiveRuleKind.Protect,
                        TargetIds = new[] { "site.d01.clinic" },
                        Activation = OperationsActivationPolicyKind.Launch
                    }
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor("spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor("spawn.player")),
                    Rifle("unit.d01.rifle.03", Anchor("spawn.player")),
                    Rifle("unit.d01.rifle.04", Anchor("spawn.player")),
                    Recon("unit.d01.recon.01", Anchor("spawn.player")),
                    Cargo("unit.d01.cargo.01", "role.friendly.cargo", Anchor("site.cargo_start")),
                    Cargo("unit.d01.cargo.02", "role.friendly.cargo", Anchor("site.cargo_start")),
                    Cargo("unit.d01.cargo.03", "role.friendly.cargo", Anchor("site.cargo_start")),
                    Site("site.d01.junction", "role.neutral.junction", Anchor("site.junction"), 100),
                    Site("site.d01.clinic", "role.neutral.clinic", clinic, 100),
                    Hostile("hostile.d01.rifle.i.01", "role.hostile.rifle", Anchor("spawn.enemy_b"), 0),
                    Hostile("hostile.d01.rifle.a.01", "role.hostile.rifle", Anchor("spawn.enemy_a"), 1)
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

        private static OperationsTacticalAuthoring O003()
        {
            string mapId = OperationsMapGreyboxCatalog.OldQuarterMapId;
            string court = Anchor("site.service_court");
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.o003",
                MapId = mapId,
                ForcePackage = OperationsForcePackageKind.Service,
                EnemyPackage = OperationsEnemyPackageKind.Raiders,
                Materials = 80,
                DeadlineTicks = 900,
                PartialNodeIds = new[] { "repair_pump_west", "repair_pump_east" },
                PartialMinimumComplete = 1,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "clear_pump_guards",
                        Rule = OperationsObjectiveRuleKind.Clear,
                        RoleIds = new[] { "role.hostile.pump_guard" },
                        Activation = OperationsActivationPolicyKind.Launch
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "repair_pump_west",
                        Rule = OperationsObjectiveRuleKind.Repair,
                        TargetIds = new[] { "site.d01.pump_west" },
                        DurationTicks = OperationsTacticalRules.RepairSeconds,
                        RadiusMeters = (int)OperationsTacticalRules.RepairMeters,
                        Prerequisites = new[] { "clear_pump_guards" },
                        Activation = OperationsActivationPolicyKind.Prerequisites
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "repair_pump_east",
                        Rule = OperationsObjectiveRuleKind.Repair,
                        TargetIds = new[] { "site.d01.pump_east" },
                        DurationTicks = OperationsTacticalRules.RepairSeconds,
                        RadiusMeters = (int)OperationsTacticalRules.RepairMeters,
                        Prerequisites = new[] { "clear_pump_guards" },
                        Activation = OperationsActivationPolicyKind.Prerequisites
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "hold_service_court",
                        Rule = OperationsObjectiveRuleKind.Hold,
                        ZoneAnchorId = court,
                        DurationTicks = 60,
                        RadiusMeters = (int)OperationsTacticalRules.HoldMeters,
                        Prerequisites = new[] { "repair_pump_west", "repair_pump_east" },
                        Activation = OperationsActivationPolicyKind.Prerequisites
                    },
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "protect_clinic_pumps",
                        Rule = OperationsObjectiveRuleKind.Protect,
                        TargetIds = new[] { "site.d01.clinic", "site.d01.pump_west", "site.d01.pump_east" },
                        Activation = OperationsActivationPolicyKind.Launch
                    }
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor("spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor("spawn.player")),
                    Rifle("unit.d01.rifle.03", Anchor("spawn.player")),
                    RepairSpecialist("unit.d01.repair.01", Anchor("spawn.player")),
                    RepairSpecialist("unit.d01.repair.02", Anchor("spawn.player")),
                    Site("site.d01.clinic", "role.neutral.clinic", Anchor("site.clinic"), 100),
                    Site("site.d01.pump_west", "role.neutral.pump_west", Anchor("site.pump_west"), 25),
                    Site("site.d01.pump_east", "role.neutral.pump_east", Anchor("site.pump_east"), 25),
                    Hostile("hostile.d01.pump.01", "role.hostile.pump_guard", Anchor("site.pump_west"), 0),
                    Hostile("hostile.d01.pump.02", "role.hostile.pump_guard", Anchor("site.pump_east"), 0),
                    Hostile("hostile.d01.rifle.a.01", "role.hostile.rifle", Anchor("spawn.enemy_a"), 1)
                },
                Waves = new[]
                {
                    new OperationsTacticalWaveAuthoring
                    {
                        Group = 1,
                        Trigger = OperationsWaveTriggerKind.NodeCompletion,
                        TriggerNodeId = "clear_pump_guards",
                        WarningSeconds = 30
                    }
                }
            };
        }

        private static string Anchor(string alias) =>
            OperationsLaunchFixtures.Anchor(OperationsMapGreyboxCatalog.OldQuarterMapId, alias);

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

        private static OperationsTacticalSpawnAuthoring Recon(string objectId, string anchorId) => new()
        {
            ObjectId = objectId,
            RoleId = "role.friendly.recon",
            RosterRole = OperationsRosterRoleKind.ReconInfantry,
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

        private static OperationsTacticalSpawnAuthoring Hostile(string objectId, string roleId, string anchorId, int group) => new()
        {
            ObjectId = objectId,
            RoleId = roleId,
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

        private static OperationsTacticalSpawnAuthoring Cargo(string objectId, string roleId, string anchorId) => new()
        {
            ObjectId = objectId,
            RoleId = roleId,
            RosterRole = OperationsRosterRoleKind.CargoTruck,
            Faction = OperationsTacticalFaction.Player,
            Body = OperationsTacticalBodyKind.Cargo,
            AnchorId = anchorId,
            Health = 100,
            HasCargo = true
        };
    }
}
