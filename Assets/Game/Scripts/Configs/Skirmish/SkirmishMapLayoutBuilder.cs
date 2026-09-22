using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishMapLayoutBuilder
    {
        public const string DesertBaseLayoutId = "layout.skirmish.db.ba";
        public const string DesertBaseMapId = "opmap.skirmish.desert_base_01";
        public const string DesertBaseContentHash = "measured.db.ba.v1";
        public const string MainRouteId = "route.skirmish.db.main";
        public const string NorthFlankRouteId = "route.skirmish.db.flank_a";
        public const string SouthSweepRouteId = "route.skirmish.db.flank_b";

        public const float InfantryMetresPerSecond = 4f;
        public const float GroundMetresPerSecond = 8f;
        public const float AirMetresPerSecond = 16f;

        public static void ApplyDesertBaseAssault(SkirmishMapLayoutConfig layout)
        {
            if (layout == null)
                return;

            var anchors = new List<SkirmishLayoutAnchorConfig>
            {
                Anchor("anchor.skirmish.db.base_player", "base.player", 0.12f, 0.50f),
                Anchor("anchor.skirmish.db.base_enemy", "base.enemy", 0.88f, 0.50f),
                Anchor("anchor.skirmish.db.staging_player", "staging.player", 0.18f, 0.50f),
                Anchor("anchor.skirmish.db.staging_enemy", "staging.enemy", 0.82f, 0.50f),
                Anchor("anchor.skirmish.db.economy_player", "economy.player", 0.10f, 0.24f),
                Anchor("anchor.skirmish.db.economy_enemy", "economy.enemy", 0.90f, 0.76f),
                Anchor("anchor.skirmish.db.service_player", "service.player", 0.12f, 0.68f),
                Anchor("anchor.skirmish.db.service_enemy", "service.enemy", 0.88f, 0.32f),
                Anchor("anchor.skirmish.db.air_player", "air.player", 0.07f, 0.78f),
                Anchor("anchor.skirmish.db.air_enemy", "air.enemy", 0.93f, 0.22f),
                Anchor("anchor.skirmish.db.supply_expansion_a", "supply.expansion.a", 0.36f, 0.22f),
                Anchor("anchor.skirmish.db.supply_expansion_b", "supply.expansion.b", 0.64f, 0.78f),
                Anchor("anchor.skirmish.db.highway_near", "highway.near", 0.32f, 0.50f),
                Anchor("anchor.skirmish.db.highway_mid", "highway.mid", 0.50f, 0.50f),
                Anchor("anchor.skirmish.db.highway_far", "highway.far", 0.68f, 0.50f),
                Anchor("anchor.skirmish.db.ruins_own", "ruins.own", 0.20f, 0.85f),
                Anchor("anchor.skirmish.db.ruins_west", "ruins.west", 0.35f, 0.92f),
                Anchor("anchor.skirmish.db.ruins_north", "ruins.north", 0.50f, 0.95f),
                Anchor("anchor.skirmish.db.ruins_east", "ruins.east", 0.65f, 0.92f),
                Anchor("anchor.skirmish.db.ruins_far", "ruins.far", 0.80f, 0.85f),
                Anchor("anchor.skirmish.db.sweep_own", "sweep.own", 0.24f, 0.18f),
                Anchor("anchor.skirmish.db.sweep_west", "sweep.west", 0.40f, 0.08f),
                Anchor("anchor.skirmish.db.sweep_south", "sweep.south", 0.60f, 0.08f),
                Anchor("anchor.skirmish.db.sweep_east", "sweep.east", 0.76f, 0.18f)
            };

            SkirmishLayoutAnchorConfig stagingPlayer = FindRole(anchors, "staging.player");
            SkirmishLayoutAnchorConfig stagingEnemy = FindRole(anchors, "staging.enemy");
            SkirmishLayoutAnchorConfig basePlayer = FindRole(anchors, "base.player");
            SkirmishLayoutAnchorConfig baseEnemy = FindRole(anchors, "base.enemy");
            SkirmishLayoutAnchorConfig economyPlayer = FindRole(anchors, "economy.player");
            SkirmishLayoutAnchorConfig economyEnemy = FindRole(anchors, "economy.enemy");
            SkirmishLayoutAnchorConfig servicePlayer = FindRole(anchors, "service.player");
            SkirmishLayoutAnchorConfig serviceEnemy = FindRole(anchors, "service.enemy");
            SkirmishLayoutAnchorConfig airPlayer = FindRole(anchors, "air.player");
            SkirmishLayoutAnchorConfig airEnemy = FindRole(anchors, "air.enemy");
            SkirmishLayoutAnchorConfig expansionA = FindRole(anchors, "supply.expansion.a");
            SkirmishLayoutAnchorConfig expansionB = FindRole(anchors, "supply.expansion.b");

            var pads = new[]
            {
                Pad("pad.skirmish.db.base_player", basePlayer, "base.player", SkirmishLegalPadKind.BaseBarracks, 16f, 12f, 1),
                Pad("pad.skirmish.db.base_enemy", baseEnemy, "base.enemy", SkirmishLegalPadKind.BaseBarracks, 16f, 12f, 2),
                Pad("pad.skirmish.db.staging_player", stagingPlayer, "staging.player", SkirmishLegalPadKind.GroundStaging, 20f, 14f, 1),
                Pad("pad.skirmish.db.staging_enemy", stagingEnemy, "staging.enemy", SkirmishLegalPadKind.GroundStaging, 20f, 14f, 2),
                OffsetPad("pad.skirmish.db.spawn_infantry_player", stagingPlayer, "spawn.infantry.player", SkirmishLegalPadKind.InfantrySpawn, -14f, -22f, 12f, 10f, 1),
                OffsetPad("pad.skirmish.db.spawn_infantry_enemy", stagingEnemy, "spawn.infantry.enemy", SkirmishLegalPadKind.InfantrySpawn, 14f, 22f, 12f, 10f, 2),
                OffsetPad("pad.skirmish.db.spawn_vehicle_player", stagingPlayer, "spawn.vehicle.player", SkirmishLegalPadKind.VehicleSpawn, 24f, 16f, 14f, 12f, 1),
                OffsetPad("pad.skirmish.db.spawn_vehicle_enemy", stagingEnemy, "spawn.vehicle.enemy", SkirmishLegalPadKind.VehicleSpawn, -24f, -16f, 14f, 12f, 2),
                OffsetPad("pad.skirmish.db.rally_player", stagingPlayer, "rally.player", SkirmishLegalPadKind.Rally, 44f, 0f, 10f, 8f, 1),
                OffsetPad("pad.skirmish.db.rally_enemy", stagingEnemy, "rally.enemy", SkirmishLegalPadKind.Rally, -44f, 0f, 10f, 8f, 2),
                Pad("pad.skirmish.db.service_player", servicePlayer, "service.player", SkirmishLegalPadKind.Service, 12f, 10f, 1),
                Pad("pad.skirmish.db.service_enemy", serviceEnemy, "service.enemy", SkirmishLegalPadKind.Service, 12f, 10f, 2),
                Pad("pad.skirmish.db.air_player", airPlayer, "air.player", SkirmishLegalPadKind.AirReturn, 16f, 16f, 1),
                Pad("pad.skirmish.db.air_enemy", airEnemy, "air.enemy", SkirmishLegalPadKind.AirReturn, 16f, 16f, 2),
                Pad("pad.skirmish.db.supply_player", economyPlayer, "economy.player", SkirmishLegalPadKind.Supply, 14f, 12f, 1),
                Pad("pad.skirmish.db.supply_enemy", economyEnemy, "economy.enemy", SkirmishLegalPadKind.Supply, 14f, 12f, 2),
                Pad("pad.skirmish.db.supply_expansion_a", expansionA, "supply.expansion.a", SkirmishLegalPadKind.SupplyExpansion, 14f, 12f, 0),
                Pad("pad.skirmish.db.supply_expansion_b", expansionB, "supply.expansion.b", SkirmishLegalPadKind.SupplyExpansion, 14f, 12f, 0)
            };

            var routes = new[]
            {
                Route(
                    MainRouteId,
                    SkirmishMeasuredRouteKind.MainHighway,
                    new[]
                    {
                        "anchor.skirmish.db.staging_player",
                        "anchor.skirmish.db.highway_near",
                        "anchor.skirmish.db.highway_mid",
                        "anchor.skirmish.db.highway_far",
                        "anchor.skirmish.db.staging_enemy"
                    },
                    anchors,
                    12f,
                    true,
                    true),
                Route(
                    NorthFlankRouteId,
                    SkirmishMeasuredRouteKind.FlankNorthRuins,
                    new[]
                    {
                        "anchor.skirmish.db.staging_player",
                        "anchor.skirmish.db.ruins_own",
                        "anchor.skirmish.db.ruins_west",
                        "anchor.skirmish.db.ruins_north",
                        "anchor.skirmish.db.ruins_east",
                        "anchor.skirmish.db.ruins_far",
                        "anchor.skirmish.db.staging_enemy"
                    },
                    anchors,
                    6f,
                    true,
                    false),
                Route(
                    SouthSweepRouteId,
                    SkirmishMeasuredRouteKind.FlankSouthSweep,
                    new[]
                    {
                        "anchor.skirmish.db.staging_player",
                        "anchor.skirmish.db.sweep_own",
                        "anchor.skirmish.db.sweep_west",
                        "anchor.skirmish.db.sweep_south",
                        "anchor.skirmish.db.sweep_east",
                        "anchor.skirmish.db.staging_enemy"
                    },
                    anchors,
                    12f,
                    true,
                    true)
            };

            layout.ApplyMeasured(
                DesertBaseLayoutId,
                DesertBaseMapId,
                1,
                DesertBaseContentHash,
                SkirmishMapLayoutConfig.DesertBaseWidthMetres,
                SkirmishMapLayoutConfig.DesertBaseDepthMetres,
                SkirmishMapLayoutConfig.DesertBaseOriginX,
                SkirmishMapLayoutConfig.DesertBaseOriginZ,
                SkirmishMapLayoutConfig.DesertBaseCellSize,
                anchors.ToArray(),
                routes,
                pads);
        }

        public static bool ShouldBindRegularStandard(SkirmishResolvedSetup setup)
        {
            return setup != null &&
                   (setup.CatalogId == "S002" || setup.CatalogId == "S003" || setup.CatalogId == "S004") &&
                   setup.DifficultyId == SkirmishDifficultyId.Regular &&
                   setup.SizeId == SkirmishSizeId.Standard;
        }

        public static void BindRegularStandard(
            SkirmishResolvedSetup setup,
            SkirmishMapLayoutConfig layout,
            List<SkirmishCompileReason> reasons)
        {
            if (setup == null || layout == null)
            {
                reasons?.Add(new SkirmishCompileReason(SkirmishReasonCode.MissingReference, "mapLayout", "Measured layout is missing."));
                return;
            }

            if (!ShouldBindRegularStandard(setup))
                return;

            if (!SkirmishMapLayoutValidation.TryValidateDesertBaseAssault(layout, reasons))
                return;

            setup.MeasuredLayoutBound = true;
            setup.WorldWidthMetres = layout.WorldWidthMetres;
            setup.WorldDepthMetres = layout.WorldDepthMetres;
            setup.GridOriginX = layout.OriginX;
            setup.GridOriginZ = layout.OriginZ;
            setup.CellSize = layout.CellSize;
            setup.DefaultRouteId = MainRouteId;

            if (layout.TryGetPad(1, SkirmishLegalPadKind.BaseBarracks, out SkirmishLegalPadConfig playerBase))
            {
                setup.PlayerBaseWorldX = playerBase.CenterX;
                setup.PlayerBaseWorldZ = playerBase.CenterZ;
            }

            if (layout.TryGetPad(2, SkirmishLegalPadKind.BaseBarracks, out SkirmishLegalPadConfig enemyBase))
            {
                setup.EnemyBaseWorldX = enemyBase.CenterX;
                setup.EnemyBaseWorldZ = enemyBase.CenterZ;
            }

            if (layout.TryGetPad(1, SkirmishLegalPadKind.GroundStaging, out SkirmishLegalPadConfig playerStaging))
            {
                setup.PlayerStagingWorldX = playerStaging.CenterX;
                setup.PlayerStagingWorldZ = playerStaging.CenterZ;
            }

            if (layout.TryGetPad(2, SkirmishLegalPadKind.GroundStaging, out SkirmishLegalPadConfig enemyStaging))
            {
                setup.EnemyStagingWorldX = enemyStaging.CenterX;
                setup.EnemyStagingWorldZ = enemyStaging.CenterZ;
            }

            if (layout.TryGetPad(1, SkirmishLegalPadKind.VehicleSpawn, out SkirmishLegalPadConfig playerSpawn))
            {
                setup.PlayerSpawnPadX = playerSpawn.CenterX;
                setup.PlayerSpawnPadZ = playerSpawn.CenterZ;
            }

            if (layout.TryGetPad(1, SkirmishLegalPadKind.Rally, out SkirmishLegalPadConfig playerRally))
            {
                setup.PlayerRallyPadX = playerRally.CenterX;
                setup.PlayerRallyPadZ = playerRally.CenterZ;
            }

            if (layout.TryGetPad(2, SkirmishLegalPadKind.VehicleSpawn, out SkirmishLegalPadConfig enemySpawn))
            {
                setup.EnemySpawnPadX = enemySpawn.CenterX;
                setup.EnemySpawnPadZ = enemySpawn.CenterZ;
            }

            if (layout.TryGetPad(2, SkirmishLegalPadKind.Rally, out SkirmishLegalPadConfig enemyRally))
            {
                setup.EnemyRallyPadX = enemyRally.CenterX;
                setup.EnemyRallyPadZ = enemyRally.CenterZ;
            }

            if (setup.Forces != null)
            {
                for (int i = 0; i < setup.Forces.Length; i++)
                {
                    SkirmishResolvedForceEntry force = setup.Forces[i];
                    if (!TryPadForForce(layout, force, out SkirmishLegalPadConfig pad))
                    {
                        reasons?.Add(new SkirmishCompileReason(
                            SkirmishReasonCode.BlockedSpawn,
                            "mapLayout",
                            force.RoleId + " has no legal pad."));
                        continue;
                    }

                    force.SpawnWorldX = pad.CenterX;
                    force.SpawnWorldZ = pad.CenterZ;
                    setup.Forces[i] = force;
                }
            }

            if (setup.Structures != null)
            {
                for (int i = 0; i < setup.Structures.Length; i++)
                {
                    SkirmishResolvedStructureEntry structure = setup.Structures[i];
                    if (!TryPadForStructure(layout, structure, out SkirmishLegalPadConfig pad))
                    {
                        reasons?.Add(new SkirmishCompileReason(
                            SkirmishReasonCode.BlockedSpawn,
                            "mapLayout",
                            structure.StructureId + " has no legal pad."));
                        continue;
                    }

                    structure.SpawnWorldX = pad.CenterX;
                    structure.SpawnWorldZ = pad.CenterZ;
                    setup.Structures[i] = structure;
                }
            }
        }

        public static bool TryPadForForce(
            SkirmishMapLayoutConfig layout,
            SkirmishResolvedForceEntry force,
            out SkirmishLegalPadConfig pad)
        {
            pad = default;
            if (layout == null)
                return false;
            SkirmishPopulationCategory category = SkirmishRoleIds.Category(force.RoleKind);
            SkirmishLegalPadKind kind = category == SkirmishPopulationCategory.Infantry
                ? SkirmishLegalPadKind.InfantrySpawn
                : category == SkirmishPopulationCategory.Air
                    ? SkirmishLegalPadKind.AirReturn
                    : SkirmishLegalPadKind.VehicleSpawn;
            return layout.TryGetPad(force.FactionId, kind, out pad);
        }

        public static bool TryPadForStructure(
            SkirmishMapLayoutConfig layout,
            SkirmishResolvedStructureEntry structure,
            out SkirmishLegalPadConfig pad)
        {
            pad = default;
            if (layout == null)
                return false;
            SkirmishLegalPadKind kind = structure.StructureId == SkirmishStructureIds.GroundStaging
                ? SkirmishLegalPadKind.GroundStaging
                : structure.StructureId == SkirmishStructureIds.Helipad
                    ? SkirmishLegalPadKind.AirReturn
                    : SkirmishLegalPadKind.BaseBarracks;
            return layout.TryGetPad(structure.FactionId, kind, out pad);
        }

        public static float PolylineLength(IReadOnlyList<SkirmishLayoutAnchorConfig> waypoints)
        {
            if (waypoints == null || waypoints.Count < 2)
                return 0f;
            float length = 0f;
            for (int i = 1; i < waypoints.Count; i++)
            {
                float dx = waypoints[i].WorldX - waypoints[i - 1].WorldX;
                float dz = waypoints[i].WorldZ - waypoints[i - 1].WorldZ;
                length += (float)System.Math.Sqrt(dx * dx + dz * dz);
            }

            return length;
        }

        private static SkirmishLayoutAnchorConfig Anchor(string id, string role, float u, float v)
        {
            SkirmishMapLayoutConfig.ProjectNormalized(
                u,
                v,
                SkirmishMapLayoutConfig.DesertBaseOriginX,
                SkirmishMapLayoutConfig.DesertBaseOriginZ,
                SkirmishMapLayoutConfig.DesertBaseWidthMetres,
                SkirmishMapLayoutConfig.DesertBaseDepthMetres,
                out float x,
                out float z);
            return new SkirmishLayoutAnchorConfig
            {
                AnchorId = id,
                RoleId = role,
                NormalizedU = u,
                NormalizedV = v,
                WorldX = x,
                WorldZ = z
            };
        }

        private static SkirmishLegalPadConfig Pad(
            string id,
            SkirmishLayoutAnchorConfig anchor,
            string role,
            SkirmishLegalPadKind kind,
            float width,
            float depth,
            byte faction)
        {
            return new SkirmishLegalPadConfig
            {
                PadId = id,
                AnchorId = anchor.AnchorId,
                RoleId = role,
                Kind = kind,
                CenterX = anchor.WorldX,
                CenterZ = anchor.WorldZ,
                WidthMetres = width,
                DepthMetres = depth,
                FactionId = faction
            };
        }

        private static SkirmishLegalPadConfig OffsetPad(
            string id,
            SkirmishLayoutAnchorConfig anchor,
            string role,
            SkirmishLegalPadKind kind,
            float offsetX,
            float offsetZ,
            float width,
            float depth,
            byte faction)
        {
            SkirmishLegalPadConfig pad = Pad(id, anchor, role, kind, width, depth, faction);
            pad.CenterX = anchor.WorldX + offsetX;
            pad.CenterZ = anchor.WorldZ + offsetZ;
            return pad;
        }

        private static SkirmishLayoutRouteConfig Route(
            string id,
            SkirmishMeasuredRouteKind kind,
            string[] waypointIds,
            List<SkirmishLayoutAnchorConfig> anchors,
            float width,
            bool vehicles,
            bool aircraft)
        {
            var points = new List<SkirmishLayoutAnchorConfig>(waypointIds.Length);
            for (int i = 0; i < waypointIds.Length; i++)
            {
                for (int j = 0; j < anchors.Count; j++)
                {
                    if (anchors[j].AnchorId != waypointIds[i])
                        continue;
                    points.Add(anchors[j]);
                    break;
                }
            }

            float length = PolylineLength(points);
            return new SkirmishLayoutRouteConfig
            {
                RouteId = id,
                WaypointAnchorIds = waypointIds,
                Kind = kind,
                LengthMetres = length,
                InfantryTransitSeconds = Transit(length, InfantryMetresPerSecond),
                GroundTransitSeconds = Transit(length, GroundMetresPerSecond),
                AirTransitSeconds = Transit(length, AirMetresPerSecond),
                InfantryFirstContactSeconds = Transit(length * 0.5f, InfantryMetresPerSecond),
                GroundFirstContactSeconds = Transit(length * 0.5f, GroundMetresPerSecond),
                ProvisionalTimes = true,
                WidthMetres = width,
                AllowsVehicles = vehicles,
                AllowsAircraft = aircraft
            };
        }

        private static float Transit(float metres, float metresPerSecond)
        {
            return metresPerSecond <= 0f ? 0f : metres / metresPerSecond;
        }

        private static SkirmishLayoutAnchorConfig FindRole(List<SkirmishLayoutAnchorConfig> anchors, string role)
        {
            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].RoleId == role)
                    return anchors[i];
            }

            return default;
        }
    }
}
