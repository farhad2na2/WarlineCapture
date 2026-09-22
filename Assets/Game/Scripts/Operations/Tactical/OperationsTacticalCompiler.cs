using System;
using System.Collections.Generic;
using Game.Operations.Contracts;

namespace Game.Operations.Tactical
{
    public static class OperationsTacticalCompiler
    {
        private static readonly string[] RequiredAliases =
        {
            "spawn.player",
            "spawn.enemy_a",
            "spawn.enemy_b",
            "spawn.staging_a",
            "spawn.staging_b",
            "exit.ground"
        };

        public static OperationsTacticalCompileResult Compile(OperationsTacticalAuthoring authoring)
        {
            if (authoring == null)
                return Fail("graph:missing");
            if (string.IsNullOrWhiteSpace(authoring.GraphId))
                return Fail("graph:id");
            if (!OperationsMapGreyboxCatalog.TryGet(authoring.MapId, out OperationsMapGreybox map))
                return Fail("missing_map:" + (authoring.MapId ?? string.Empty));
            if (authoring.ForcePackage == OperationsForcePackageKind.None ||
                authoring.EnemyPackage == OperationsEnemyPackageKind.None)
                return Fail("budget:package");

            OperationsTacticalNodeAuthoring[] nodes = authoring.Nodes ?? Array.Empty<OperationsTacticalNodeAuthoring>();
            if (nodes.Length == 0)
                return Fail("graph:empty");

            string structure = ValidateStructure(authoring.GraphId, nodes);
            if (structure != null)
                return Fail(structure);

            List<OperationsTacticalSpawnAuthoring> spawns = new(authoring.Spawns ?? Array.Empty<OperationsTacticalSpawnAuthoring>());
            List<OperationsTacticalWaveAuthoring> waves = new(authoring.Waves ?? Array.Empty<OperationsTacticalWaveAuthoring>());
            if (authoring.GenerateDefaultEnemySchedule)
            {
                string scheduleError = AppendDefaultSchedule(map, authoring.EnemyPackage, spawns, waves);
                if (scheduleError != null)
                    return Fail(scheduleError);
            }

            string waveError = ValidateWaves(nodes, spawns, waves);
            if (waveError != null)
                return Fail(waveError);

            string verbError = ValidateVerbs(nodes);
            if (verbError != null)
                return Fail(verbError);

            string anchorError = ValidateAnchors(map, nodes, spawns);
            if (anchorError != null)
                return Fail(anchorError);

            string budgetError = ValidateBudgets(authoring, map, nodes, spawns, waves);
            if (budgetError != null)
                return Fail(budgetError);

            return new OperationsTacticalCompileResult(true, string.Empty, Build(authoring, map, nodes, spawns, waves));
        }

        private static string ValidateStructure(string graphId, OperationsTacticalNodeAuthoring[] nodes)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int required = 0;
            for (int index = 0; index < nodes.Length; index++)
            {
                OperationsTacticalNodeAuthoring node = nodes[index];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                    return "graph:node";
                if (!ids.Add(node.NodeId))
                    return "graph:duplicate_node:" + node.NodeId;
                if (node.Rule == OperationsObjectiveRuleKind.None)
                    return "graph:rule:" + node.NodeId;
                if (!node.Optional)
                    required++;
            }

            if (required == 0)
                return "graph:no_required";

            for (int index = 0; index < nodes.Length; index++)
            {
                OperationsTacticalNodeAuthoring node = nodes[index];
                string[] prerequisites = node.Prerequisites ?? Array.Empty<string>();
                for (int prerequisite = 0; prerequisite < prerequisites.Length; prerequisite++)
                {
                    string requiredId = prerequisites[prerequisite];
                    if (!ids.Contains(requiredId))
                        return "graph:missing_prerequisite:" + node.NodeId + ":" + requiredId;
                    if (string.Equals(requiredId, node.NodeId, StringComparison.Ordinal))
                        return "graph:cycle:" + node.NodeId;
                    if (!node.Optional && Find(nodes, requiredId).Optional)
                        return "optional_gates_required:" + node.NodeId + ":" + requiredId;
                }
            }

            if (HasCycle(nodes))
                return "graph:cycle:" + graphId;

            for (int index = 0; index < nodes.Length; index++)
            {
                if (!IsReachable(nodes, nodes[index].NodeId))
                    return "unreachable:" + nodes[index].NodeId;
            }

            var scanTargets = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].Rule != OperationsObjectiveRuleKind.Scan)
                    continue;
                string[] targets = nodes[index].TargetIds ?? Array.Empty<string>();
                for (int target = 0; target < targets.Length; target++)
                {
                    if (!scanTargets.Add(targets[target]))
                        return "duplicate_scan_target:" + targets[target];
                }
            }

            return null;
        }

        private static string ValidateVerbs(OperationsTacticalNodeAuthoring[] nodes)
        {
            for (int index = 0; index < nodes.Length; index++)
            {
                OperationsTacticalNodeAuthoring node = nodes[index];
                if (!OperationsTacticalRules.IsPackageVerb(node.Rule))
                    return "verb_not_in_package:" + node.NodeId + ":" + node.Rule;
                int radius = node.RadiusMeters;
                int duration = node.DurationTicks;
                switch (node.Rule)
                {
                    case OperationsObjectiveRuleKind.Scan:
                        if (radius != (int)OperationsTacticalRules.ScanMeters || duration != OperationsTacticalRules.ScanSeconds)
                            return "radius:" + node.NodeId;
                        break;
                    case OperationsObjectiveRuleKind.Hold:
                        if (radius != (int)OperationsTacticalRules.HoldMeters || duration <= 0 || string.IsNullOrEmpty(node.ZoneAnchorId))
                            return "radius:" + node.NodeId;
                        break;
                    case OperationsObjectiveRuleKind.Interact:
                        if (radius != (int)OperationsTacticalRules.InteractMeters || duration <= 0)
                            return "radius:" + node.NodeId;
                        break;
                    case OperationsObjectiveRuleKind.Repair:
                        if (radius != (int)OperationsTacticalRules.RepairMeters || duration != OperationsTacticalRules.RepairSeconds)
                            return "radius:" + node.NodeId;
                        break;
                    case OperationsObjectiveRuleKind.Escort:
                        if (node.TargetCount < 1)
                            return "budget:escort_cargo:" + node.NodeId;
                        break;
                    case OperationsObjectiveRuleKind.Extract:
                        if (node.TargetCount < OperationsTacticalRules.ExtractMinimumInfantry)
                            return "budget:extract_infantry:" + node.NodeId;
                        break;
                    case OperationsObjectiveRuleKind.Clear:
                        if (node.RoleIds == null || node.RoleIds.Length == 0)
                            return "missing_anchor:" + node.NodeId;
                        break;
                    case OperationsObjectiveRuleKind.Protect:
                        if (node.TargetIds == null || node.TargetIds.Length == 0)
                            return "missing_anchor:" + node.NodeId;
                        break;
                }

                if ((node.Rule == OperationsObjectiveRuleKind.Scan ||
                     node.Rule == OperationsObjectiveRuleKind.Interact ||
                     node.Rule == OperationsObjectiveRuleKind.Repair ||
                     node.Rule == OperationsObjectiveRuleKind.Escort) &&
                    (node.TargetIds == null || node.TargetIds.Length == 0))
                    return "missing_anchor:" + node.NodeId;
            }

            return null;
        }

        private static string ValidateWaves(
            OperationsTacticalNodeAuthoring[] nodes,
            List<OperationsTacticalSpawnAuthoring> spawns,
            List<OperationsTacticalWaveAuthoring> waves)
        {
            for (int index = 0; index < waves.Count; index++)
            {
                OperationsTacticalWaveAuthoring wave = waves[index];
                if (wave == null || wave.Group < 1)
                    return "budget:wave";
                int members = CountGroup(spawns, wave.Group);
                if (members == 0)
                    return "budget:wave_empty:" + wave.Group;
                if (wave.WarningSeconds < OperationsTacticalRules.MinimumWarningSeconds)
                    return "warning:" + wave.Group + ":" + wave.WarningSeconds;
            }

            for (int index = 0; index < nodes.Length; index++)
            {
                OperationsTacticalNodeAuthoring node = nodes[index];
                if (node.Optional || node.Rule != OperationsObjectiveRuleKind.Clear)
                    continue;
                string[] roles = node.RoleIds ?? Array.Empty<string>();
                for (int roleIndex = 0; roleIndex < roles.Length; roleIndex++)
                {
                    string roleId = roles[roleIndex];
                    int total = 0;
                    int late = 0;
                    for (int spawnIndex = 0; spawnIndex < spawns.Count; spawnIndex++)
                    {
                        OperationsTacticalSpawnAuthoring spawn = spawns[spawnIndex];
                        if (!string.Equals(spawn.RoleId, roleId, StringComparison.Ordinal))
                            continue;
                        total++;
                        if (spawn.Group > 0 && SpawnsAfterCompletion(waves, spawn.Group, node.NodeId, nodes))
                            late++;
                    }

                    if (total > 0 && late > 0)
                        return "wave_clear_cycle:" + node.NodeId + ":" + roleId;
                }
            }

            return null;
        }

        private static bool SpawnsAfterCompletion(
            List<OperationsTacticalWaveAuthoring> waves,
            int group,
            string clearNodeId,
            OperationsTacticalNodeAuthoring[] nodes)
        {
            OperationsTacticalWaveAuthoring wave = FindWave(waves, group);
            if (wave == null)
                return false;
            string first = DesignatedFirst(nodes);
            switch (wave.Trigger)
            {
                case OperationsWaveTriggerKind.NodeCompletion:
                    return string.Equals(wave.TriggerNodeId, clearNodeId, StringComparison.Ordinal) ||
                           IsDescendant(nodes, clearNodeId, wave.TriggerNodeId);
                case OperationsWaveTriggerKind.NodeActivation:
                    return !string.Equals(wave.TriggerNodeId, clearNodeId, StringComparison.Ordinal) &&
                           IsDescendant(nodes, clearNodeId, wave.TriggerNodeId);
                case OperationsWaveTriggerKind.FirstRequiredCompletion:
                    return string.Equals(first, clearNodeId, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private static string ValidateAnchors(
            OperationsMapGreybox map,
            OperationsTacticalNodeAuthoring[] nodes,
            List<OperationsTacticalSpawnAuthoring> spawns)
        {
            for (int index = 0; index < RequiredAliases.Length; index++)
            {
                if (!map.TryGetByAlias(RequiredAliases[index], out _))
                    return "missing_anchor:" + RequiredAliases[index];
            }

            var seenAnchors = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < map.Anchors.Length; index++)
            {
                string anchorId = map.Anchors[index].AnchorId;
                if (!OperationsIdentityRules.IsValidAnchorId(anchorId))
                    return "bad_anchor:" + anchorId;
                if (!seenAnchors.Add(anchorId))
                    return "duplicate_anchor:" + anchorId;
                if (!AnchorMatchesDistrict(anchorId, map.DistrictNumber))
                    return "anchor_district:" + anchorId;
            }

            for (int index = 0; index < spawns.Count; index++)
            {
                string anchorError = ClassifyAnchor(map, spawns[index].AnchorId);
                if (anchorError != null)
                    return anchorError;
            }

            for (int index = 0; index < nodes.Length; index++)
            {
                OperationsTacticalNodeAuthoring node = nodes[index];
                if (!string.IsNullOrEmpty(node.ZoneAnchorId))
                {
                    string zoneError = ClassifyAnchor(map, node.ZoneAnchorId);
                    if (zoneError != null)
                        return zoneError;
                }

                string[] routes = RouteIds(node);
                for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
                {
                    if (!map.TryGetRoute(routes[routeIndex], out OperationsGreyboxRoute route))
                        return "missing_anchor:" + routes[routeIndex];
                    if (route.AnchorIds.Length < 2)
                        return "missing_anchor:" + routes[routeIndex];
                    for (int point = 0; point < route.AnchorIds.Length; point++)
                    {
                        string pointError = ClassifyAnchor(map, route.AnchorIds[point]);
                        if (pointError != null)
                            return pointError;
                    }
                }
            }

            return null;
        }

        private static string ValidateBudgets(
            OperationsTacticalAuthoring authoring,
            OperationsMapGreybox map,
            OperationsTacticalNodeAuthoring[] nodes,
            List<OperationsTacticalSpawnAuthoring> spawns,
            List<OperationsTacticalWaveAuthoring> waves)
        {
            OperationsForcePackageSchema force = OperationsForcePackageSchema.Create(authoring.ForcePackage);
            OperationsThreatProfileSchema threat = OperationsThreatProfileSchema.Create(authoring.EnemyPackage);
            var objects = new HashSet<string>(StringComparer.Ordinal);
            var singletonRoles = new HashSet<string>(StringComparer.Ordinal);
            var playerCounts = new Dictionary<OperationsRosterRoleKind, int>();
            var hostileCounts = new Dictionary<OperationsRosterRoleKind, int>();
            int playerInfantry = 0;
            int cargo = 0;
            int repairSpecialists = 0;

            for (int index = 0; index < spawns.Count; index++)
            {
                OperationsTacticalSpawnAuthoring spawn = spawns[index];
                if (spawn == null || string.IsNullOrWhiteSpace(spawn.ObjectId) || spawn.ObjectId.Length > OperationsIdentityRules.MaximumIdLength)
                    return "budget:object";
                if (!objects.Add(spawn.ObjectId))
                    return "duplicate_role:" + spawn.ObjectId;
                if (!OperationsIdentityRules.IsValidRoleId(spawn.RoleId))
                    return "bad_anchor:" + spawn.RoleId;
                if (spawn.Health <= 0)
                    return "budget:health:" + spawn.ObjectId;
                if (spawn.Group < 0)
                    return "budget:wave";
                if (spawn.Group > 0 && FindWave(waves, spawn.Group) == null)
                    return "missing_wave:" + spawn.Group;

                bool singleton = spawn.Body == OperationsTacticalBodyKind.Site ||
                                 spawn.Body == OperationsTacticalBodyKind.Evidence;
                if (singleton && !singletonRoles.Add(spawn.RoleId))
                    return "duplicate_role:" + spawn.RoleId;

                if (spawn.Faction == OperationsTacticalFaction.Player &&
                    spawn.Body == OperationsTacticalBodyKind.Infantry)
                {
                    AddCount(playerCounts, spawn.RosterRole);
                    playerInfantry++;
                    if (spawn.RosterRole == OperationsRosterRoleKind.RepairSpecialist)
                        repairSpecialists++;
                }
                else if (spawn.Faction == OperationsTacticalFaction.Hostile)
                {
                    AddCount(hostileCounts, spawn.RosterRole);
                }
                else if (spawn.Body == OperationsTacticalBodyKind.Cargo)
                {
                    cargo++;
                }
            }

            if (!CountsFit(playerCounts, force.Lines))
                return "budget:force";
            if (!CountsFit(hostileCounts, threat.Lines))
                return "budget:enemy";

            int repairNodes = 0;
            int escortNeeded = 0;
            bool needsExtract = false;
            bool needsRepair = false;
            for (int index = 0; index < nodes.Length; index++)
            {
                OperationsTacticalNodeAuthoring node = nodes[index];
                if (node.Rule == OperationsObjectiveRuleKind.Repair)
                {
                    repairNodes++;
                    needsRepair = true;
                }

                if (node.Rule == OperationsObjectiveRuleKind.Escort)
                    escortNeeded += node.TargetCount;
                if (node.Rule == OperationsObjectiveRuleKind.Extract)
                    needsExtract = true;
                string[] targets = node.TargetIds ?? Array.Empty<string>();
                for (int target = 0; target < targets.Length; target++)
                {
                    if (!objects.Contains(targets[target]) && node.Rule != OperationsObjectiveRuleKind.Hold)
                        return "missing_anchor:" + targets[target];
                }
            }

            int repairMaterials = repairNodes * OperationsTacticalRules.RepairMaterialCost;
            if (repairNodes >= 3)
                repairMaterials = Math.Max(repairMaterials, OperationsTacticalRules.ThreeSiteMaterialFloor);
            if (authoring.Materials < repairMaterials)
                return "repair_materials:" + authoring.Materials;
            if (needsRepair && repairSpecialists < 1)
                return "budget:repair_specialist";
            if (needsExtract && playerInfantry < OperationsTacticalRules.ExtractMinimumInfantry)
                return "budget:extract_infantry";
            if (escortNeeded > cargo)
                return "budget:escort_cargo";

            string[] evidence = authoring.MandatoryEvidenceIds ?? Array.Empty<string>();
            for (int index = 0; index < evidence.Length; index++)
            {
                if (!objects.Contains(evidence[index]))
                    return "missing_anchor:" + evidence[index];
            }

            string[] partials = authoring.PartialNodeIds ?? Array.Empty<string>();
            for (int index = 0; index < partials.Length; index++)
            {
                if (Find(nodes, partials[index]) == null)
                    return "graph:missing_prerequisite:" + partials[index];
            }

            if (!string.IsNullOrEmpty(authoring.PartialProgressNodeId) &&
                Find(nodes, authoring.PartialProgressNodeId) == null)
                return "graph:missing_prerequisite:" + authoring.PartialProgressNodeId;

            if (authoring.DeadlineTicks > 0)
            {
                int longest = LongestRequiredTicks(map, nodes);
                if (authoring.DeadlineTicks < longest)
                    return "budget:deadline:" + authoring.DeadlineTicks + "<" + longest;
            }

            return null;
        }

        private static OperationsCompiledTactical Build(
            OperationsTacticalAuthoring authoring,
            OperationsMapGreybox map,
            OperationsTacticalNodeAuthoring[] nodes,
            List<OperationsTacticalSpawnAuthoring> spawns,
            List<OperationsTacticalWaveAuthoring> waves)
        {
            var compiledNodes = new OperationsCompiledNode[nodes.Length];
            for (int index = 0; index < nodes.Length; index++)
                compiledNodes[index] = BuildNode(map, nodes[index]);

            var compiledSpawns = new OperationsCompiledSpawn[spawns.Count];
            for (int index = 0; index < spawns.Count; index++)
                compiledSpawns[index] = BuildSpawn(map, spawns[index], nodes);

            var compiledWaves = new OperationsCompiledWave[waves.Count];
            for (int index = 0; index < waves.Count; index++)
            {
                OperationsTacticalWaveAuthoring wave = waves[index];
                compiledWaves[index] = new OperationsCompiledWave
                {
                    Group = wave.Group,
                    Trigger = wave.Trigger,
                    TriggerNodeId = wave.TriggerNodeId ?? string.Empty,
                    WarningSeconds = wave.WarningSeconds,
                    ElapsedTicks = wave.ElapsedTicks
                };
            }

            map.TryGetByAlias("exit.ground", out OperationsGreyboxAnchor exitAnchor);
            return new OperationsCompiledTactical
            {
                GraphId = authoring.GraphId,
                MapId = map.MapId,
                DistrictNumber = map.DistrictNumber,
                Materials = authoring.Materials,
                ExitX = exitAnchor.X,
                ExitZ = exitAnchor.Z,
                DeadlineTicks = authoring.DeadlineTicks,
                PartialNodeIds = authoring.PartialNodeIds ?? Array.Empty<string>(),
                PartialMinimumComplete = authoring.PartialMinimumComplete,
                PartialProgressNodeId = authoring.PartialProgressNodeId ?? string.Empty,
                PartialProgressMinimum = authoring.PartialProgressMinimum,
                PartialExtractMinimum = authoring.PartialExtractMinimum,
                MandatoryEvidenceIds = authoring.MandatoryEvidenceIds ?? Array.Empty<string>(),
                LaunchRestoredSiteIds = authoring.LaunchRestoredSiteIds ?? Array.Empty<string>(),
                FirstRequiredNodeId = DesignatedFirst(nodes),
                FinalRequiredNodeId = DesignatedFinal(nodes),
                Nodes = compiledNodes,
                Spawns = compiledSpawns,
                Waves = compiledWaves,
                BlockerAx = map.BlockerAx,
                BlockerAz = map.BlockerAz,
                BlockerBx = map.BlockerBx,
                BlockerBz = map.BlockerBz
            };
        }

        private static OperationsCompiledNode BuildNode(OperationsMapGreybox map, OperationsTacticalNodeAuthoring node)
        {
            float zoneX = 0f;
            float zoneZ = 0f;
            if (!string.IsNullOrEmpty(node.ZoneAnchorId) && map.TryGetById(node.ZoneAnchorId, out OperationsGreyboxAnchor zone))
            {
                zoneX = zone.X;
                zoneZ = zone.Z;
            }

            string[] routeIds = RouteIds(node);
            var routes = new OperationsCompiledRoute[routeIds.Length];
            for (int index = 0; index < routeIds.Length; index++)
            {
                map.TryGetRoute(routeIds[index], out OperationsGreyboxRoute route);
                var xs = new float[route.AnchorIds.Length];
                var zs = new float[route.AnchorIds.Length];
                for (int point = 0; point < route.AnchorIds.Length; point++)
                {
                    map.TryGetById(route.AnchorIds[point], out OperationsGreyboxAnchor anchor);
                    xs[point] = anchor.X;
                    zs[point] = anchor.Z;
                }

                routes[index] = new OperationsCompiledRoute
                {
                    RouteId = route.RouteId,
                    Xs = xs,
                    Zs = zs
                };
            }

            return new OperationsCompiledNode
            {
                NodeId = node.NodeId,
                Rule = node.Rule,
                TargetIds = CompileTargetIds(node),
                TargetCount = node.TargetCount,
                DurationTicks = node.DurationTicks,
                RadiusMeters = node.RadiusMeters,
                Prerequisites = node.Prerequisites ?? Array.Empty<string>(),
                Activation = node.Activation,
                Join = node.Join,
                Optional = node.Optional,
                ZoneAnchorId = node.ZoneAnchorId ?? string.Empty,
                ZoneX = zoneX,
                ZoneZ = zoneZ,
                LegalRoutes = routes,
                HiddenUntilObserved = node.HiddenUntilObserved
            };
        }

        private static string[] CompileTargetIds(OperationsTacticalNodeAuthoring node)
        {
            if (node.Rule == OperationsObjectiveRuleKind.Clear)
            {
                string[] roles = node.RoleIds ?? Array.Empty<string>();
                if (roles.Length > 0)
                    return roles;
            }

            return node.TargetIds ?? Array.Empty<string>();
        }

        private static OperationsCompiledSpawn BuildSpawn(
            OperationsMapGreybox map,
            OperationsTacticalSpawnAuthoring spawn,
            OperationsTacticalNodeAuthoring[] nodes)
        {
            map.TryGetById(spawn.AnchorId, out OperationsGreyboxAnchor anchor);
            string stagingAlias = spawn.Group == 2 ? "spawn.staging_b" : "spawn.staging_a";
            map.TryGetByAlias(stagingAlias, out OperationsGreyboxAnchor staging);
            bool hidden = false;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (!nodes[index].HiddenUntilObserved)
                    continue;
                string[] targets = nodes[index].TargetIds ?? Array.Empty<string>();
                for (int target = 0; target < targets.Length; target++)
                {
                    if (string.Equals(targets[target], spawn.ObjectId, StringComparison.Ordinal))
                        hidden = true;
                }
            }

            bool infantry = spawn.Faction == OperationsTacticalFaction.Player &&
                            spawn.Body == OperationsTacticalBodyKind.Infantry;
            return new OperationsCompiledSpawn
            {
                ObjectId = spawn.ObjectId,
                RoleId = spawn.RoleId,
                RosterRole = spawn.RosterRole,
                Faction = spawn.Faction,
                Body = spawn.Body,
                AnchorId = spawn.AnchorId,
                Group = spawn.Group,
                X = anchor.X,
                Z = anchor.Z,
                StagingX = staging.X,
                StagingZ = staging.Z,
                Health = spawn.Health,
                HasCargo = spawn.HasCargo || spawn.Body == OperationsTacticalBodyKind.Cargo,
                HiddenUntilObserved = hidden,
                Commandable = infantry,
                OriginalInfantry = infantry && spawn.Group == 0
            };
        }

        private static string AppendDefaultSchedule(
            OperationsMapGreybox map,
            OperationsEnemyPackageKind enemyPackage,
            List<OperationsTacticalSpawnAuthoring> spawns,
            List<OperationsTacticalWaveAuthoring> waves)
        {
            for (int index = 0; index < spawns.Count; index++)
            {
                if (spawns[index].Faction == OperationsTacticalFaction.Hostile)
                    return "budget:explicit_hostile_with_schedule";
            }

            if (!map.TryGetByAlias("spawn.enemy_a", out OperationsGreyboxAnchor enemyA) ||
                !map.TryGetByAlias("spawn.enemy_b", out OperationsGreyboxAnchor enemyB))
                return "missing_anchor:spawn.enemy";

            OperationsThreatProfileSchema threat = OperationsThreatProfileSchema.Create(enemyPackage);
            bool anyA = false;
            bool anyB = false;
            for (int line = 0; line < threat.Lines.Length; line++)
            {
                OperationsForceRoleLine roleLine = threat.Lines[line];
                OperationsTacticalRules.SplitBudget(roleLine.Count, out int initial, out int waveA, out int waveB);
                string slug = RoleSlug(roleLine.Role);
                AddSplit(spawns, map, enemyA, roleLine.Role, slug, 0, initial);
                AddSplit(spawns, map, enemyA, roleLine.Role, slug, 1, waveA);
                AddSplit(spawns, map, enemyB, roleLine.Role, slug, 2, waveB);
                anyA |= waveA > 0;
                anyB |= waveB > 0;
            }

            if (anyA)
            {
                waves.Add(new OperationsTacticalWaveAuthoring
                {
                    Group = 1,
                    Trigger = OperationsWaveTriggerKind.FirstRequiredCompletion,
                    WarningSeconds = OperationsTacticalRules.WaveAWarningSeconds
                });
            }

            if (anyB)
            {
                waves.Add(new OperationsTacticalWaveAuthoring
                {
                    Group = 2,
                    Trigger = OperationsWaveTriggerKind.FinalRequiredActivation,
                    WarningSeconds = OperationsTacticalRules.WaveBWarningSeconds
                });
            }

            return null;
        }

        private static void AddSplit(
            List<OperationsTacticalSpawnAuthoring> spawns,
            OperationsMapGreybox map,
            OperationsGreyboxAnchor anchor,
            OperationsRosterRoleKind role,
            string slug,
            int group,
            int count)
        {
            string groupLabel = group == 0 ? "i" : group == 1 ? "a" : "b";
            for (int index = 0; index < count; index++)
            {
                spawns.Add(new OperationsTacticalSpawnAuthoring
                {
                    ObjectId = "hostile.d" + map.DistrictNumber.ToString("00") + "." + slug + "." + groupLabel + "." + (index + 1).ToString("00"),
                    RoleId = "role.hostile." + slug,
                    RosterRole = role,
                    Faction = OperationsTacticalFaction.Hostile,
                    Body = IsVehicle(role) ? OperationsTacticalBodyKind.Vehicle : OperationsTacticalBodyKind.Infantry,
                    AnchorId = anchor.AnchorId,
                    Group = group,
                    Health = 100
                });
            }
        }

        private static int LongestRequiredTicks(OperationsMapGreybox map, OperationsTacticalNodeAuthoring[] nodes)
        {
            var memo = new Dictionary<string, int>(StringComparer.Ordinal);
            int longest = 0;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].Optional)
                    continue;
                int length = RequiredPath(map, nodes, nodes[index].NodeId, memo);
                if (length > longest)
                    longest = length;
            }

            return longest;
        }

        private static int RequiredPath(
            OperationsMapGreybox map,
            OperationsTacticalNodeAuthoring[] nodes,
            string nodeId,
            Dictionary<string, int> memo)
        {
            if (memo.TryGetValue(nodeId, out int cached))
                return cached;
            OperationsTacticalNodeAuthoring node = Find(nodes, nodeId);
            int bestPrefix = 0;
            string[] prerequisites = node.Prerequisites ?? Array.Empty<string>();
            for (int index = 0; index < prerequisites.Length; index++)
            {
                OperationsTacticalNodeAuthoring prerequisite = Find(nodes, prerequisites[index]);
                if (prerequisite == null || prerequisite.Optional)
                    continue;
                int length = RequiredPath(map, nodes, prerequisite.NodeId, memo);
                if (length > bestPrefix)
                    bestPrefix = length;
            }

            int mine = MinimumTicks(map, node);
            memo[nodeId] = bestPrefix + mine;
            return memo[nodeId];
        }

        private static int MinimumTicks(OperationsMapGreybox map, OperationsTacticalNodeAuthoring node)
        {
            switch (node.Rule)
            {
                case OperationsObjectiveRuleKind.Scan:
                    return OperationsTacticalRules.ScanSeconds * Math.Max(1, (node.TargetIds ?? Array.Empty<string>()).Length);
                case OperationsObjectiveRuleKind.Interact:
                case OperationsObjectiveRuleKind.Hold:
                    return node.DurationTicks;
                case OperationsObjectiveRuleKind.Repair:
                    return OperationsTacticalRules.RepairSeconds;
                case OperationsObjectiveRuleKind.Escort:
                    return EscortTravelTicks(map, node) + OperationsTacticalRules.EscortUnloadSeconds;
                case OperationsObjectiveRuleKind.Extract:
                    map.TryGetByAlias("spawn.player", out OperationsGreyboxAnchor spawn);
                    map.TryGetByAlias("exit.ground", out OperationsGreyboxAnchor exit);
                    float distance = OperationsTacticalRules.Distance(spawn.X, spawn.Z, exit.X, exit.Z);
                    int ticks = (int)Math.Ceiling(distance / OperationsTacticalRules.InfantryMetersPerTick);
                    return Math.Max(1, ticks);
                default:
                    return 0;
            }
        }

        private static int EscortTravelTicks(OperationsMapGreybox map, OperationsTacticalNodeAuthoring node)
        {
            string[] routes = RouteIds(node);
            if (routes.Length == 0 || !map.TryGetRoute(routes[0], out OperationsGreyboxRoute route))
                return 1;
            float distance = 0f;
            for (int index = 1; index < route.AnchorIds.Length; index++)
            {
                map.TryGetById(route.AnchorIds[index - 1], out OperationsGreyboxAnchor from);
                map.TryGetById(route.AnchorIds[index], out OperationsGreyboxAnchor to);
                distance += OperationsTacticalRules.Distance(from.X, from.Z, to.X, to.Z);
            }

            return Math.Max(1, (int)Math.Ceiling(distance / OperationsTacticalRules.CargoMetersPerTick));
        }

        private static bool CountsFit(Dictionary<OperationsRosterRoleKind, int> actual, OperationsForceRoleLine[] lines)
        {
            foreach (KeyValuePair<OperationsRosterRoleKind, int> pair in actual)
            {
                int allowed = 0;
                for (int index = 0; index < lines.Length; index++)
                {
                    if (lines[index].Role == pair.Key)
                        allowed = lines[index].Count;
                }

                if (pair.Value > allowed)
                    return false;
            }

            return true;
        }

        private static void AddCount(Dictionary<OperationsRosterRoleKind, int> counts, OperationsRosterRoleKind role)
        {
            counts.TryGetValue(role, out int current);
            counts[role] = current + 1;
        }

        private static string ClassifyAnchor(OperationsMapGreybox map, string anchorId)
        {
            if (!OperationsIdentityRules.IsValidAnchorId(anchorId))
                return "bad_anchor:" + anchorId;
            if (!AnchorMatchesDistrict(anchorId, map.DistrictNumber))
                return "anchor_district:" + anchorId;
            if (!map.TryGetById(anchorId, out _))
                return "missing_anchor:" + anchorId;
            return null;
        }

        private static bool AnchorMatchesDistrict(string anchorId, int districtNumber)
        {
            string token = ".d" + districtNumber.ToString("00") + ".";
            return anchorId.IndexOf(token, StringComparison.Ordinal) >= 0;
        }

        private static string[] RouteIds(OperationsTacticalNodeAuthoring node)
        {
            if (node.LegalRouteIds != null && node.LegalRouteIds.Length > 0)
                return node.LegalRouteIds;
            if (!string.IsNullOrEmpty(node.RouteId))
                return new[] { node.RouteId };
            return Array.Empty<string>();
        }

        private static int CountGroup(List<OperationsTacticalSpawnAuthoring> spawns, int group)
        {
            int count = 0;
            for (int index = 0; index < spawns.Count; index++)
            {
                if (spawns[index].Group == group)
                    count++;
            }

            return count;
        }

        private static OperationsTacticalWaveAuthoring FindWave(List<OperationsTacticalWaveAuthoring> waves, int group)
        {
            for (int index = 0; index < waves.Count; index++)
            {
                if (waves[index].Group == group)
                    return waves[index];
            }

            return null;
        }

        private static OperationsTacticalNodeAuthoring Find(OperationsTacticalNodeAuthoring[] nodes, string nodeId)
        {
            for (int index = 0; index < nodes.Length; index++)
            {
                if (string.Equals(nodes[index].NodeId, nodeId, StringComparison.Ordinal))
                    return nodes[index];
            }

            return null;
        }

        private static bool HasCycle(OperationsTacticalNodeAuthoring[] nodes)
        {
            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < nodes.Length; index++)
            {
                if (Visit(nodes, nodes[index].NodeId, state))
                    return true;
            }

            return false;
        }

        private static bool Visit(OperationsTacticalNodeAuthoring[] nodes, string nodeId, Dictionary<string, int> state)
        {
            if (state.TryGetValue(nodeId, out int mark))
                return mark == 1;
            state[nodeId] = 1;
            OperationsTacticalNodeAuthoring node = Find(nodes, nodeId);
            string[] prerequisites = node.Prerequisites ?? Array.Empty<string>();
            for (int index = 0; index < prerequisites.Length; index++)
            {
                if (Visit(nodes, prerequisites[index], state))
                    return true;
            }

            state[nodeId] = 2;
            return false;
        }

        private static bool IsReachable(OperationsTacticalNodeAuthoring[] nodes, string nodeId)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int index = 0; index < nodes.Length; index++)
                {
                    OperationsTacticalNodeAuthoring node = nodes[index];
                    if (reachable.Contains(node.NodeId))
                        continue;
                    if (node.Activation == OperationsActivationPolicyKind.Launch)
                    {
                        reachable.Add(node.NodeId);
                        changed = true;
                        continue;
                    }

                    string[] prerequisites = node.Prerequisites ?? Array.Empty<string>();
                    if (prerequisites.Length == 0)
                        continue;
                    bool ready = node.Join == OperationsGraphJoinKind.AnyOf
                        ? AnyReachable(prerequisites, reachable)
                        : AllReachable(prerequisites, reachable);
                    if (ready)
                    {
                        reachable.Add(node.NodeId);
                        changed = true;
                    }
                }
            }

            return reachable.Contains(nodeId);
        }

        private static bool AnyReachable(string[] prerequisites, HashSet<string> reachable)
        {
            for (int index = 0; index < prerequisites.Length; index++)
            {
                if (reachable.Contains(prerequisites[index]))
                    return true;
            }

            return false;
        }

        private static bool AllReachable(string[] prerequisites, HashSet<string> reachable)
        {
            for (int index = 0; index < prerequisites.Length; index++)
            {
                if (!reachable.Contains(prerequisites[index]))
                    return false;
            }

            return true;
        }

        private static bool IsDescendant(OperationsTacticalNodeAuthoring[] nodes, string ancestorId, string candidateId)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>();
            stack.Push(ancestorId);
            while (stack.Count > 0)
            {
                string current = stack.Pop();
                for (int index = 0; index < nodes.Length; index++)
                {
                    string[] prerequisites = nodes[index].Prerequisites ?? Array.Empty<string>();
                    for (int prerequisite = 0; prerequisite < prerequisites.Length; prerequisite++)
                    {
                        if (!string.Equals(prerequisites[prerequisite], current, StringComparison.Ordinal))
                            continue;
                        if (!seen.Add(nodes[index].NodeId))
                            continue;
                        if (string.Equals(nodes[index].NodeId, candidateId, StringComparison.Ordinal))
                            return true;
                        stack.Push(nodes[index].NodeId);
                    }
                }
            }

            return false;
        }

        private static string DesignatedFirst(OperationsTacticalNodeAuthoring[] nodes)
        {
            int bestDepth = int.MaxValue;
            string best = null;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].Optional)
                    continue;
                int depth = Depth(nodes, nodes[index].NodeId);
                if (depth < bestDepth)
                {
                    bestDepth = depth;
                    best = nodes[index].NodeId;
                }
            }

            return best ?? string.Empty;
        }

        private static string DesignatedFinal(OperationsTacticalNodeAuthoring[] nodes)
        {
            int bestDepth = -1;
            string best = null;
            for (int index = 0; index < nodes.Length; index++)
            {
                if (nodes[index].Optional)
                    continue;
                int depth = Depth(nodes, nodes[index].NodeId);
                if (depth >= bestDepth)
                {
                    bestDepth = depth;
                    best = nodes[index].NodeId;
                }
            }

            return best ?? string.Empty;
        }

        private static int Depth(OperationsTacticalNodeAuthoring[] nodes, string nodeId)
        {
            OperationsTacticalNodeAuthoring node = Find(nodes, nodeId);
            string[] prerequisites = node.Prerequisites ?? Array.Empty<string>();
            int best = 0;
            for (int index = 0; index < prerequisites.Length; index++)
            {
                OperationsTacticalNodeAuthoring prerequisite = Find(nodes, prerequisites[index]);
                if (prerequisite == null || prerequisite.Optional)
                    continue;
                int depth = Depth(nodes, prerequisite.NodeId) + 1;
                if (depth > best)
                    best = depth;
            }

            return best;
        }

        private static string RoleSlug(OperationsRosterRoleKind role) => role switch
        {
            OperationsRosterRoleKind.RifleInfantry => "rifle",
            OperationsRosterRoleKind.ReconInfantry => "recon",
            OperationsRosterRoleKind.SupportInfantry => "support",
            OperationsRosterRoleKind.RepairSpecialist => "repair",
            OperationsRosterRoleKind.AntiArmorInfantry => "anti_armor",
            OperationsRosterRoleKind.AntiAirInfantry => "anti_air",
            OperationsRosterRoleKind.Apc => "apc",
            OperationsRosterRoleKind.Tank => "tank",
            OperationsRosterRoleKind.TransportHelicopter => "transport_helicopter",
            OperationsRosterRoleKind.LightVehicle => "light_vehicle",
            OperationsRosterRoleKind.CargoTruck => "cargo",
            OperationsRosterRoleKind.Civilian => "civilian",
            _ => "unit"
        };

        private static bool IsVehicle(OperationsRosterRoleKind role) =>
            role == OperationsRosterRoleKind.Apc ||
            role == OperationsRosterRoleKind.Tank ||
            role == OperationsRosterRoleKind.TransportHelicopter ||
            role == OperationsRosterRoleKind.LightVehicle ||
            role == OperationsRosterRoleKind.CargoTruck;

        private static OperationsTacticalCompileResult Fail(string error) =>
            new(false, error, null);
    }
}
