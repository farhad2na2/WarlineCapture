using System;
using System.Collections.Generic;
using System.IO;
using Game.Operations.Contracts;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP2Checks
    {
        public const int ExpectedCheckCount = 16;
        public const string PassMarker = "[OperationsP2Validation] result=Passed checks=16";

        public static void RunAll()
        {
            TwoMapFixturesCompileAndDiffer();
            CompilerRejectsBadGraphs();
            CompilerRejectsBadAnchors();
            CompilerRejectsBadBudgetsAndWaves();
            SpawnOwnershipBindsSessionAndRole();
            ScanRequiresRangeLosAndExclusiveSite();
            HoldResetsWhenContestedOrEmpty();
            InteractResetsAndCarriesEvidence();
            RepairSpendsOncePausesAndRetriesRestored();
            EscortRequiresGoRouteAndUnload();
            ExtractRequiresTwoOriginalInfantry();
            WaveScheduleIsFiniteAndWarned();
            OutcomePrecedenceDeathsBeforeVictory();
            ConcludeWithdrawDeadlineAndPause();
            RulesIgnoreMissionId();
            OptionalNodesDoNotGateVictory();
        }

        public static void TwoMapFixturesCompileAndDiffer()
        {
            OperationsMapGreybox oldQuarter = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsMapGreybox civic = OperationsMapGreyboxCatalog.CivicCenter;
            Require(oldQuarter.MapId != civic.MapId);
            Require(oldQuarter.DistrictNumber == 1 && civic.DistrictNumber == 2);
            Require(oldQuarter.DistanceByAlias("spawn.player", "site.clinic") <= OperationsTacticalRules.ScanMeters);
            Require(civic.DistanceByAlias("spawn.player", "site.clinic") > OperationsTacticalRules.ScanMeters);
            RequireAliases(oldQuarter);
            RequireAliases(civic);

            OperationsTacticalSession quarter = PlayOldQuarter(OperationsP0Checks.CreateLaunch());
            Require(quarter.Outcome == OperationsOutcomeKind.Victory, quarter.Trace());
            Require(quarter.MapId == OperationsMapGreyboxCatalog.OldQuarterMapId);
            Require(Node(quarter, "hold_courtyard").Phase != OperationsTacticalNodePhase.Complete);
            int frozenTick = quarter.Tick;
            int frozenHostiles = quarter.SpawnedHostileCount;
            quarter.Advance(80);
            Require(quarter.Tick == frozenTick && quarter.SpawnedHostileCount == frozenHostiles);
            Require(quarter.AttemptReleased);
            Require(quarter.IssueWithdraw().Reason == OperationsTacticalRejectKind.Terminal);

            OperationsTacticalSession center = PlayCivicCenter();
            Require(center.Outcome == OperationsOutcomeKind.Victory, center.Trace());
            Require(center.MapId == OperationsMapGreyboxCatalog.CivicCenterMapId);
            Require(center.Materials == 200, "materials=" + center.Materials);
            Require(Node(center, "hold_plaza").Phase == OperationsTacticalNodePhase.Complete);
            Require(Node(center, "repair_clinic").Phase == OperationsTacticalNodePhase.Complete);
            Require(Node(center, "escort_cargo").Phase == OperationsTacticalNodePhase.Complete);
            Require(Node(center, "escort_cargo").CompletionTick >= 0 && Node(center, "repair_clinic").CompletionTick >= 0);
        }

        public static void CompilerRejectsBadGraphs()
        {
            OperationsTacticalCompileResult cycle = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.cycle",
                MapId = OperationsMapGreyboxCatalog.OldQuarterMapId,
                Nodes = new[]
                {
                    Scan("scan_a", "site.d01.clinic", new[] { "scan_b" }),
                    Scan("scan_b", "site.d01.archive", new[] { "scan_a" })
                }
            });
            Require(!cycle.Accepted && cycle.Error.StartsWith("graph:cycle:", StringComparison.Ordinal), cycle.Error);

            OperationsTacticalCompileResult unreachable = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.unreachable",
                MapId = OperationsMapGreyboxCatalog.OldQuarterMapId,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "later",
                        Rule = OperationsObjectiveRuleKind.Extract,
                        Activation = OperationsActivationPolicyKind.Prerequisites,
                        TargetCount = 2
                    },
                    Scan("scan_clinic", "site.d01.clinic", Array.Empty<string>())
                }
            });
            Require(!unreachable.Accepted && unreachable.Error.StartsWith("unreachable:", StringComparison.Ordinal), unreachable.Error);

            OperationsTacticalCompileResult duplicate = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.duplicate",
                MapId = OperationsMapGreyboxCatalog.OldQuarterMapId,
                Nodes = new[]
                {
                    Scan("scan_a", "site.d01.clinic", Array.Empty<string>()),
                    Scan("scan_b", "site.d01.clinic", Array.Empty<string>())
                }
            });
            Require(!duplicate.Accepted && duplicate.Error.StartsWith("duplicate_scan_target:", StringComparison.Ordinal), duplicate.Error);

            OperationsTacticalCompileResult gates = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.gates",
                MapId = OperationsMapGreyboxCatalog.OldQuarterMapId,
                Nodes = new[]
                {
                    OptionalScan(),
                    ExtractNode(new[] { "optional_scan" })
                }
            });
            Require(!gates.Accepted && gates.Error.StartsWith("optional_gates_required:", StringComparison.Ordinal), gates.Error);

            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalCompileResult clearCycle = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.clear",
                MapId = map.MapId,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "clear_guards",
                        Rule = OperationsObjectiveRuleKind.Clear,
                        RoleIds = new[] { "role.hostile.rifle" }
                    }
                },
                Spawns = new[] { Hostile("hostile.d01.rifle.a.01", Anchor(map, "spawn.enemy_a"), 1) },
                Waves = new[]
                {
                    new OperationsTacticalWaveAuthoring
                    {
                        Group = 1,
                        Trigger = OperationsWaveTriggerKind.NodeCompletion,
                        TriggerNodeId = "clear_guards",
                        WarningSeconds = 30
                    }
                }
            });
            Require(!clearCycle.Accepted && clearCycle.Error.StartsWith("wave_clear_cycle:", StringComparison.Ordinal), clearCycle.Error);

            OperationsTacticalCompileResult rescue = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.rescue",
                MapId = map.MapId,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "rescue_group",
                        Rule = OperationsObjectiveRuleKind.Rescue
                    }
                }
            });
            Require(!rescue.Accepted && rescue.Error.StartsWith("verb_not_in_package:", StringComparison.Ordinal), rescue.Error);
        }

        public static void CompilerRejectsBadAnchors()
        {
            OperationsTacticalCompileResult missingMap = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.missing",
                MapId = "opmap.operations.industrial_belt"
            });
            Require(!missingMap.Accepted && missingMap.Error.StartsWith("missing_map:", StringComparison.Ordinal), missingMap.Error);

            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalCompileResult district = CompileScan(map, new[]
            {
                Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40),
                Rifle("unit.d01.rifle.02", "anchor.operations.d02.plaza")
            });
            Require(!district.Accepted && district.Error.StartsWith("anchor_district:", StringComparison.Ordinal), district.Error);

            OperationsTacticalCompileResult missing = CompileScan(map, new[]
            {
                Rifle("unit.d01.rifle.01", "anchor.operations.d01.not_here"),
                Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40)
            });
            Require(!missing.Accepted && missing.Error.StartsWith("missing_anchor:", StringComparison.Ordinal), missing.Error);

            OperationsTacticalCompileResult bad = CompileScan(map, new[]
            {
                Rifle("unit.d01.rifle.01", "nope"),
                Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40)
            });
            Require(!bad.Accepted && bad.Error.StartsWith("bad_anchor:", StringComparison.Ordinal), bad.Error);
        }

        public static void CompilerRejectsBadBudgetsAndWaves()
        {
            OperationsTacticalRules.SplitBudget(18, out int initial, out int waveA, out int waveB);
            Require(initial == 9 && waveA == 4 && waveB == 5);
            OperationsTacticalRules.SplitBudget(2, out initial, out waveA, out waveB);
            Require(initial == 1 && waveA == 0 && waveB == 1);

            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            var hostiles = new List<OperationsTacticalSpawnAuthoring>
            {
                Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40)
            };
            for (int index = 0; index < 19; index++)
                hostiles.Add(Hostile("hostile.d01.rifle." + (index + 1).ToString("00"), Anchor(map, "spawn.enemy_a"), 0));
            OperationsTacticalCompileResult overBudget = CompileScan(map, hostiles.ToArray());
            Require(!overBudget.Accepted && overBudget.Error.StartsWith("budget:enemy", StringComparison.Ordinal), overBudget.Error);

            OperationsTacticalCompileResult warning = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.warning",
                MapId = map.MapId,
                Nodes = new[] { Scan("scan_clinic", "site.d01.clinic", Array.Empty<string>()) },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                    Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40),
                    Hostile("hostile.d01.rifle.a.01", Anchor(map, "spawn.enemy_a"), 1)
                },
                Waves = new[]
                {
                    new OperationsTacticalWaveAuthoring { Group = 1, WarningSeconds = 19 }
                }
            });
            Require(!warning.Accepted && warning.Error.StartsWith("warning:", StringComparison.Ordinal), warning.Error);

            OperationsMapGreybox civic = OperationsMapGreyboxCatalog.CivicCenter;
            var repairSpawns = new List<OperationsTacticalSpawnAuthoring>
            {
                RepairSpecialist("unit.d02.repair.01", Anchor(civic, "site.service_stand"))
            };
            var repairNodes = new List<OperationsTacticalNodeAuthoring>();
            string[] siteAnchors = { "site.clinic", "site.annex", "site.plaza" };
            for (int index = 0; index < siteAnchors.Length; index++)
            {
                string objectId = "site.d02.repair." + (index + 1).ToString("00");
                repairSpawns.Add(Site(objectId, "role.neutral.site" + (index + 1).ToString("00"), Anchor(civic, siteAnchors[index]), 40));
                repairNodes.Add(RepairNode("repair_" + (index + 1).ToString("00"), objectId));
            }

            OperationsTacticalCompileResult materials = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.materials",
                MapId = civic.MapId,
                ForcePackage = OperationsForcePackageKind.Service,
                Materials = 80,
                Nodes = repairNodes.ToArray(),
                Spawns = repairSpawns.ToArray()
            });
            Require(!materials.Accepted && materials.Error.StartsWith("repair_materials:", StringComparison.Ordinal), materials.Error);

            OperationsTacticalCompileResult deadline = OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.deadline",
                MapId = map.MapId,
                DeadlineTicks = 5,
                Nodes = new[] { Scan("scan_clinic", "site.d01.clinic", Array.Empty<string>()) },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                    Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40)
                }
            });
            Require(!deadline.Accepted && deadline.Error.StartsWith("budget:deadline:", StringComparison.Ordinal), deadline.Error);

            OperationsTacticalCompileResult duplicateRole = CompileScan(map, new[]
            {
                Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                Site("site.d01.clinic.a", "role.neutral.clinic", Anchor(map, "site.clinic"), 40),
                Site("site.d01.clinic.b", "role.neutral.clinic", Anchor(map, "site.courtyard"), 40)
            });
            Require(!duplicateRole.Accepted && duplicateRole.Error.StartsWith("duplicate_role:", StringComparison.Ordinal), duplicateRole.Error);
        }

        public static void SpawnOwnershipBindsSessionAndRole()
        {
            OperationsLaunchPayload launch = OperationsP0Checks.CreateLaunch();
            OperationsTacticalSession session = new(Must(OldQuarterAuthoring()), launch);
            OperationsTacticalActorState[] actors = session.CopyActors();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int spawned = 0;
            int reserved = 0;
            for (int index = 0; index < actors.Length; index++)
            {
                OperationsTacticalActorState actor = actors[index];
                Require(seen.Add(actor.ObjectId), actor.ObjectId);
                Require(actor.SessionId == launch.SessionId, actor.SessionId);
                Require(actor.RoleId.StartsWith("role.", StringComparison.Ordinal), actor.RoleId);
                if (actor.Spawned)
                    spawned++;
                else
                    reserved++;
            }

            Require(spawned >= 4 && reserved == 1, "spawned=" + spawned + " reserved=" + reserved);
            Require(session.TryGetActor("hostile.d01.rifle.a.01", out OperationsTacticalActorState wave));
            Require(!wave.Spawned && wave.Group == 1 && wave.RoleId == "role.hostile.rifle");
        }

        public static void ScanRequiresRangeLosAndExclusiveSite()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalSession session = Start(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.scan",
                MapId = map.MapId,
                Nodes = new[]
                {
                    Scan("scan_archive", "site.d01.archive", Array.Empty<string>()),
                    Scan("scan_far", "site.d01.far", Array.Empty<string>())
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor(map, "spawn.player")),
                    Site("site.d01.archive", "role.neutral.archive", Anchor(map, "site.archive"), 100),
                    Site("site.d01.far", "role.neutral.far", Anchor(map, "site.far_post"), 100)
                }
            });

            Require(session.IssueScan("unit.d01.rifle.01", "site.d01.archive").Reason == OperationsTacticalRejectKind.BlockedLineOfSight);
            Require(session.IssueScan("unit.d01.rifle.01", "site.d01.far").Reason == OperationsTacticalRejectKind.OutOfRange);
            Accept(session.IssueMove("unit.d01.rifle.01", Anchor(map, "site.lane")));
            session.Advance(8);
            Accept(session.IssueScan("unit.d01.rifle.01", "site.d01.archive"));
            session.Advance(OperationsTacticalRules.ScanSeconds - 1);
            Require(Node(session, "scan_archive").Phase != OperationsTacticalNodePhase.Complete);
            session.Advance(1);
            Require(Node(session, "scan_archive").Phase == OperationsTacticalNodePhase.Complete, session.Trace());
            Require(session.IssueScan("unit.d01.rifle.02", "site.d01.archive").Reason == OperationsTacticalRejectKind.AlreadyConsumed);
        }

        public static void HoldResetsWhenContestedOrEmpty()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            OperationsTacticalSession contested = Start(HoldAuthoring(map, true));
            Accept(contested.IssueHold("unit.d02.rifle.01", Anchor(map, "site.plaza")));
            contested.Advance(4);
            Require(Node(contested, "hold_plaza").ProgressTicks == 0);
            Accept(contested.ReportDeath("hostile.d02.rifle.01"));
            contested.Advance(1);
            Require(Node(contested, "hold_plaza").ProgressTicks == 1);
            // Re-issue Hold so AFK refresh window does not freeze the compressed hold.
            for (int step = 0; step < 7; step++)
            {
                Accept(contested.IssueHold("unit.d02.rifle.01", Anchor(map, "site.plaza")));
                contested.Advance(1);
            }
            Require(Node(contested, "hold_plaza").Phase == OperationsTacticalNodePhase.Complete);

            OperationsTacticalSession empty = Start(HoldAuthoring(map, false));
            Accept(empty.IssueHold("unit.d02.rifle.01", Anchor(map, "site.plaza")));
            empty.Advance(3);
            Require(Node(empty, "hold_plaza").ProgressTicks == 3);
            Accept(empty.IssueMove("unit.d02.rifle.01", Anchor(map, "spawn.player")));
            empty.Advance(2);
            Require(Node(empty, "hold_plaza").ProgressTicks == 0, "progress=" + Node(empty, "hold_plaza").ProgressTicks);

            OperationsTacticalSession vehicle = Start(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.vehicle",
                MapId = map.MapId,
                ForcePackage = OperationsForcePackageKind.Service,
                Nodes = new[] { HoldNode("hold_plaza", Anchor(map, "site.plaza"), 8) },
                Spawns = new[]
                {
                    Rifle("unit.d02.rifle.01", Anchor(map, "spawn.player")),
                    new OperationsTacticalSpawnAuthoring
                    {
                        ObjectId = "unit.d02.apc.01",
                        RoleId = "role.friendly.apc",
                        RosterRole = OperationsRosterRoleKind.Apc,
                        Faction = OperationsTacticalFaction.Player,
                        Body = OperationsTacticalBodyKind.Vehicle,
                        AnchorId = Anchor(map, "site.plaza"),
                        Health = 100
                    }
                }
            });
            Require(vehicle.IssueHold("unit.d02.apc.01", Anchor(map, "site.plaza")).Reason == OperationsTacticalRejectKind.NotEligible);
        }

        public static void InteractResetsAndCarriesEvidence()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalSession session = Start(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.interact",
                MapId = map.MapId,
                Nodes = new[]
                {
                    InteractNode("interact_evidence", "site.d01.evidence", 8),
                    HoldNode("hold_open", Anchor(map, "site.courtyard"), 500)
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                    Evidence("site.d01.evidence", Anchor(map, "site.evidence")),
                    Hostile("hostile.d01.rifle.01", Anchor(map, "spawn.enemy_b"), 0)
                }
            });

            Accept(session.IssueInteract("unit.d01.rifle.01", "site.d01.evidence"));
            session.Advance(4);
            Require(Actor(session, "unit.d01.rifle.01").ChannelTicks == 4);
            Accept(session.ReportMoved("hostile.d01.rifle.01", Anchor(map, "site.evidence")));
            session.Advance(1);
            Require(Actor(session, "unit.d01.rifle.01").ChannelTicks == 0);
            Accept(session.ReportDeath("hostile.d01.rifle.01"));
            session.Advance(7);
            Require(Node(session, "interact_evidence").Phase != OperationsTacticalNodePhase.Complete);
            session.Advance(1);
            Require(Node(session, "interact_evidence").Phase == OperationsTacticalNodePhase.Complete);
            Require(Actor(session, "unit.d01.rifle.01").CarriedObjectId == "site.d01.evidence");
            int evidenceActors = 0;
            OperationsTacticalActorState[] actors = session.CopyActors();
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].Body == OperationsTacticalBodyKind.Evidence)
                    evidenceActors++;
            }

            Accept(session.ReportDeath("unit.d01.rifle.01"));
            session.Advance(1);
            Require(Actor(session, "unit.d01.rifle.01").CarriedObjectId == string.Empty);
            Require(CountBody(session, OperationsTacticalBodyKind.Evidence) == evidenceActors);
        }

        public static void RepairSpendsOncePausesAndRetriesRestored()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            OperationsTacticalSession session = Start(RepairAuthoring(map, false, 40, true));
            Accept(session.IssueRepair("unit.d02.repair.01", "site.d02.clinic"));
            session.Advance(10);
            Require(session.Materials == 200, "materials=" + session.Materials);
            Require(Node(session, "repair_clinic").ProgressTicks == 10);
            Accept(session.ReportMoved("hostile.d02.rifle.01", Anchor(map, "site.clinic")));
            session.Advance(5);
            Require(Node(session, "repair_clinic").ProgressTicks == 10);
            Require(session.Materials == 200);
            Accept(session.ReportDeath("hostile.d02.rifle.01"));
            session.Advance(35);
            Require(Node(session, "repair_clinic").Phase == OperationsTacticalNodePhase.Complete, session.Trace());
            Require(Actor(session, "site.d02.clinic").Health >= OperationsTacticalRules.RepairHealthPercent);
            int spent = session.Materials;
            Require(session.IssueRepair("unit.d02.repair.01", "site.d02.clinic").Reason == OperationsTacticalRejectKind.AlreadyConsumed);
            Require(session.Materials == spent);

            OperationsTacticalSession retry = Start(RepairAuthoring(map, true, 80));
            Require(retry.Outcome == OperationsOutcomeKind.Victory, retry.Trace());
            Require(retry.Tick == 0);
            Require(retry.Materials == 240);
            Require(!Node(retry, "repair_clinic").MaterialsCharged);
        }

        public static void EscortRequiresGoRouteAndUnload()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            OperationsTacticalSession parked = Start(EscortAuthoring(map, Anchor(map, "site.cargo_exit")));
            parked.Advance(25);
            Require(Node(parked, "escort_cargo").Phase != OperationsTacticalNodePhase.Complete);

            OperationsTacticalSession session = Start(EscortAuthoring(map, Anchor(map, "site.cargo_start")));
            Accept(session.IssueEscortGo("route.main"));
            Require(session.IssueEscortGo("route.safe").Reason == OperationsTacticalRejectKind.RouteLocked);
            Accept(session.IssueEscortHold());
            session.Advance(6);
            Require(Node(session, "escort_cargo").Phase != OperationsTacticalNodePhase.Complete);
            Accept(session.IssueEscortGo("route.main"));
            session.Advance(40);
            Require(Node(session, "escort_cargo").Phase == OperationsTacticalNodePhase.Complete, session.Trace());
            Require(HasFact(session, OperationsTacticalFactKind.CargoDelivered, "unit.d02.cargo.01"));
        }

        public static void ExtractRequiresTwoOriginalInfantry()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalSession one = Start(ExtractAuthoring(map, false));
            Accept(one.IssueExtract("unit.d01.rifle.01"));
            one.Advance(8);
            Require(one.Outcome != OperationsOutcomeKind.Victory, one.Trace());
            Require(Node(one, "extract_force").Phase != OperationsTacticalNodePhase.Complete);

            OperationsTacticalSession moved = Start(ExtractAuthoring(map, false));
            Accept(moved.IssueMove("unit.d01.rifle.01", Anchor(map, "exit.ground")));
            Accept(moved.IssueMove("unit.d01.rifle.02", Anchor(map, "exit.ground")));
            moved.Advance(8);
            Require(Node(moved, "extract_force").Phase != OperationsTacticalNodePhase.Complete, moved.Trace());

            OperationsTacticalSession both = Start(ExtractAuthoring(map, true));
            Accept(both.IssueExtract("unit.d01.rifle.01"));
            Accept(both.IssueExtract("unit.d01.rifle.02"));
            both.Advance(1);
            Require(both.Outcome == OperationsOutcomeKind.Victory, both.Trace());
        }

        public static void WaveScheduleIsFiniteAndWarned()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalSession session = Start(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.waves",
                MapId = map.MapId,
                GenerateDefaultEnemySchedule = true,
                Nodes = new[]
                {
                    OptionalScan(),
                    Scan("required_scan", "site.d01.clinic", Array.Empty<string>()),
                    HoldNode("hold_open", Anchor(map, "site.courtyard"), 500, new[] { "required_scan" })
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor(map, "spawn.player")),
                    Site("site.d01.optional", "role.neutral.optional", Anchor(map, "site.evidence"), 100),
                    Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 100)
                }
            });

            Require(session.SpawnedHostileCount == 10, "initial=" + session.SpawnedHostileCount);
            Accept(session.IssueScan("unit.d01.rifle.01", "site.d01.optional"));
            session.Advance(OperationsTacticalRules.ScanSeconds);
            Require(Node(session, "optional_scan").Phase == OperationsTacticalNodePhase.Complete);
            Require(!session.IsWaveArmed(1), session.Trace());
            Accept(session.IssueScan("unit.d01.rifle.01", "site.d01.clinic"));
            session.Advance(OperationsTacticalRules.ScanSeconds);
            int armedAt = OperationsTacticalRules.ScanSeconds * 2;
            Require(session.Tick == armedAt, "tick=" + session.Tick);
            Require(session.IsWaveArmed(1) && !session.IsWaveSpawned(1));
            Require(session.SpawnedHostileCount == 10);
            session.Advance(OperationsTacticalRules.WaveAWarningSeconds - 1);
            Require(session.SpawnedHostileCount == 10, "early=" + session.SpawnedHostileCount);
            session.Advance(1);
            int waveATick = armedAt + OperationsTacticalRules.WaveAWarningSeconds;
            Require(session.Tick == waveATick && session.SpawnedHostileCount == 14, session.Trace() + " hostiles=" + session.SpawnedHostileCount);
            int waveBDelta = OperationsTacticalRules.WaveBWarningSeconds - OperationsTacticalRules.WaveAWarningSeconds;
            session.Advance(waveBDelta);
            int waveBTick = armedAt + OperationsTacticalRules.WaveBWarningSeconds;
            // One extra tick matches prior fixture cadence after wave B warning elapses.
            session.Advance(1);
            Require(session.Tick == waveBTick + 1 && session.SpawnedHostileCount == 20, "tick=" + session.Tick + " hostiles=" + session.SpawnedHostileCount);
            Accept(session.IssueWithdraw());
            session.Advance(40);
            Require(session.SpawnedHostileCount == 20 && session.Tick == waveBTick + 1);

            OperationsTacticalSession blocked = Start(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.staging",
                MapId = map.MapId,
                Nodes = new[]
                {
                    Scan("required_scan", "site.d01.clinic", Array.Empty<string>()),
                    HoldNode("hold_open", Anchor(map, "site.courtyard"), 500, new[] { "required_scan" })
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor(map, "spawn.player")),
                    Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 100),
                    Hostile("hostile.d01.rifle.a.01", Anchor(map, "spawn.enemy_a"), 1)
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
            });
            Accept(blocked.IssueScan("unit.d01.rifle.01", "site.d01.clinic"));
            blocked.Advance(OperationsTacticalRules.ScanSeconds);
            Accept(blocked.IssueMove("unit.d01.rifle.02", Anchor(map, "spawn.enemy_a")));
            blocked.Advance(30);
            Require(blocked.Tick == OperationsTacticalRules.ScanSeconds + 30, "tick=" + blocked.Tick);
            Require(blocked.TryGetActor("hostile.d01.rifle.a.01", out OperationsTacticalActorState spawned));
            Require(spawned.Spawned);
            Require(OperationsTacticalRules.Within(spawned.X, spawned.Z, 36f, 6f, 1f), "x=" + spawned.X + " z=" + spawned.Z);
        }

        public static void OutcomePrecedenceDeathsBeforeVictory()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalSession lived = Start(ExtractAuthoring(map, true));
            Accept(lived.IssueExtract("unit.d01.rifle.01"));
            Accept(lived.IssueExtract("unit.d01.rifle.02"));
            lived.Advance(1);
            Require(lived.Outcome == OperationsOutcomeKind.Victory);

            OperationsTacticalSession died = Start(ExtractAuthoring(map, true));
            Accept(died.IssueExtract("unit.d01.rifle.01"));
            Accept(died.IssueExtract("unit.d01.rifle.02"));
            Accept(died.ReportDeath("unit.d01.rifle.01"));
            Accept(died.ReportDeath("unit.d01.rifle.02"));
            died.Advance(1);
            Require(died.Outcome == OperationsOutcomeKind.Defeat, died.Trace());
            Require(died.TerminalReason == "no_commandable_units");

            OperationsMapGreybox civic = OperationsMapGreyboxCatalog.CivicCenter;
            OperationsTacticalSession repair = Start(RepairAuthoring(civic, false, 40));
            Accept(repair.IssueRepair("unit.d02.repair.01", "site.d02.clinic"));
            repair.Advance(OperationsTacticalRules.RepairSeconds - 1);
            Require(repair.Outcome == OperationsOutcomeKind.None, repair.Trace());
            Accept(repair.ReportSiteDestroyed("site.d02.clinic"));
            repair.Advance(1);
            Require(repair.Outcome == OperationsOutcomeKind.Defeat, repair.Trace());
            Require(repair.TerminalReason == "node_failed:repair_clinic");
            Require(Node(repair, "repair_clinic").Phase == OperationsTacticalNodePhase.Failed);
        }

        public static void ConcludeWithdrawDeadlineAndPause()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalSession conclude = Start(DeadlineAuthoring(map, 40));
            Require(conclude.IssueConclude().Reason == OperationsTacticalRejectKind.PreconditionFailed);
            Accept(conclude.IssueObserve("unit.d01.rifle.01", "site.d01.clinic"));
            Accept(conclude.IssueScan("unit.d01.rifle.01", "site.d01.clinic"));
            conclude.Advance(OperationsTacticalRules.ScanSeconds);
            Accept(conclude.IssueConclude());
            Require(conclude.Outcome == OperationsOutcomeKind.Partial);
            Require(conclude.TerminalReason == "conclude");
            Require(conclude.IssueConclude().Reason == OperationsTacticalRejectKind.Terminal);

            OperationsTacticalSession withdraw = Start(DeadlineAuthoring(map, 0));
            Accept(withdraw.IssueWithdraw());
            Require(withdraw.Outcome == OperationsOutcomeKind.Withdrawn && withdraw.Tick == 0);
            withdraw.Advance(10);
            Require(withdraw.Tick == 0);

            OperationsTacticalSession expired = Start(DeadlineAuthoring(map, 19));
            expired.Advance(19);
            Require(expired.Outcome == OperationsOutcomeKind.Defeat && expired.TerminalReason == "deadline", expired.Trace());

            OperationsTacticalSession partial = Start(DeadlineAuthoring(map, 19));
            Accept(partial.IssueObserve("unit.d01.rifle.01", "site.d01.clinic"));
            Accept(partial.IssueScan("unit.d01.rifle.01", "site.d01.clinic"));
            partial.Advance(19);
            Require(partial.Outcome == OperationsOutcomeKind.Partial && partial.TerminalReason == "deadline", partial.Trace());

            OperationsTacticalSession paused = Start(DeadlineAuthoring(map, 0));
            Accept(paused.IssueObserve("unit.d01.rifle.01", "site.d01.clinic"));
            Accept(paused.IssueScan("unit.d01.rifle.01", "site.d01.clinic"));
            paused.Advance(5);
            paused.SetPaused(true);
            paused.Advance(10);
            Require(paused.Tick == 5);
            Require(Actor(paused, "unit.d01.rifle.01").ChannelTicks == 5);
            paused.SetPaused(false);
            paused.Advance(10);
            Require(Node(paused, "scan_clinic").Phase == OperationsTacticalNodePhase.Complete);
        }

        public static void RulesIgnoreMissionId()
        {
            OperationsLaunchPayload first = OperationsP0Checks.CreateLaunch();
            OperationsLaunchPayload second = OperationsP0Checks.CreateLaunch("operation.o060");
            OperationsTacticalSession left = Start(ExtractAuthoring(OperationsMapGreyboxCatalog.OldQuarter, true), first);
            OperationsTacticalSession right = Start(ExtractAuthoring(OperationsMapGreyboxCatalog.OldQuarter, true), second);
            Accept(left.IssueExtract("unit.d01.rifle.01"));
            Accept(left.IssueExtract("unit.d01.rifle.02"));
            Accept(right.IssueExtract("unit.d01.rifle.01"));
            Accept(right.IssueExtract("unit.d01.rifle.02"));
            left.Advance(1);
            right.Advance(1);
            Require(left.Trace() == right.Trace(), left.Trace() + " vs " + right.Trace());
            Require(left.MissionId == "operation.o001" && right.MissionId == "operation.o060");

            Require(OperationsP0Checks.TryFindRepositoryRoot(
                new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory },
                out string root));
            string tactical = OperationsP0Checks.CombineProjectPath(root, "Assets/Game/Scripts/Operations/Tactical");
            string[] files = Directory.GetFiles(tactical, "*.cs");
            Require(files.Length >= 4);
            for (int index = 0; index < files.Length; index++)
            {
                string source = File.ReadAllText(files[index]);
                Require(source.IndexOf("operation.o", StringComparison.Ordinal) < 0, files[index]);
                Require(source.IndexOf("MissionId ==", StringComparison.Ordinal) < 0, files[index]);
                Require(source.IndexOf("missionId ==", StringComparison.Ordinal) < 0, files[index]);
                Require(source.IndexOf("TryParseMissionNumber", StringComparison.Ordinal) < 0, files[index]);
                Require(source.IndexOf("switch (launch", StringComparison.Ordinal) < 0, files[index]);
            }
        }

        public static void OptionalNodesDoNotGateVictory()
        {
            OperationsTacticalSession victory = PlayOldQuarter(OperationsP0Checks.CreateLaunch());
            Require(victory.Outcome == OperationsOutcomeKind.Victory);
            Require(Node(victory, "hold_courtyard").Phase == OperationsTacticalNodePhase.Active);

            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            OperationsTacticalSession failedOptional = Start(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.optional",
                MapId = map.MapId,
                ForcePackage = OperationsForcePackageKind.Service,
                Materials = 240,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "repair_optional",
                        Rule = OperationsObjectiveRuleKind.Repair,
                        TargetIds = new[] { "site.d02.clinic" },
                        DurationTicks = OperationsTacticalRules.RepairSeconds,
                        RadiusMeters = (int)OperationsTacticalRules.RepairMeters,
                        Optional = true
                    },
                    Scan("scan_annex", "site.d02.annex", Array.Empty<string>()),
                    HoldNode("hold_open", Anchor(map, "site.plaza"), 30, new[] { "scan_annex" })
                },
                Spawns = new[]
                {
                    Rifle("unit.d02.rifle.01", Anchor(map, "site.annex")),
                    RepairSpecialist("unit.d02.repair.01", Anchor(map, "site.service_stand")),
                    Site("site.d02.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40),
                    Site("site.d02.annex", "role.neutral.annex", Anchor(map, "site.annex"), 100)
                }
            });
            Accept(failedOptional.ReportSiteDestroyed("site.d02.clinic"));
            failedOptional.Advance(1);
            Require(failedOptional.Outcome == OperationsOutcomeKind.None, failedOptional.Trace());
            Require(Node(failedOptional, "repair_optional").Phase == OperationsTacticalNodePhase.Failed);
            Accept(failedOptional.IssueScan("unit.d02.rifle.01", "site.d02.annex"));
            failedOptional.Advance(OperationsTacticalRules.ScanSeconds);
            Require(failedOptional.Outcome == OperationsOutcomeKind.None, failedOptional.Trace());
            Require(Node(failedOptional, "scan_annex").Phase == OperationsTacticalNodePhase.Complete);
        }

        private static OperationsTacticalSession PlayOldQuarter(OperationsLaunchPayload launch)
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            OperationsTacticalSession session = new(Must(OldQuarterAuthoring()), launch);
            Accept(session.IssueObserve("unit.d01.rifle.01", "site.d01.clinic"));
            Accept(session.IssueScan("unit.d01.rifle.01", "site.d01.clinic"));
            Accept(session.IssueInteract("unit.d01.rifle.02", "site.d01.evidence"));
            bool extractA = false;
            bool extractB = false;
            for (int tick = 0; tick < 40 && !session.IsTerminal; tick++)
            {
                session.Advance(1);
                if (!extractB && Node(session, "interact_evidence").Phase == OperationsTacticalNodePhase.Complete)
                {
                    Accept(session.IssueExtract("unit.d01.rifle.02"));
                    extractB = true;
                }

                if (!extractA && Node(session, "scan_clinic").Phase == OperationsTacticalNodePhase.Complete)
                {
                    Accept(session.IssueExtract("unit.d01.rifle.01"));
                    extractA = true;
                }
            }

            return session;
        }

        private static OperationsTacticalSession PlayCivicCenter()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            OperationsTacticalSession session = Start(CivicAuthoring());
            Accept(session.IssueMove("unit.d02.rifle.01", Anchor(map, "site.plaza")));
            Accept(session.IssueRepair("unit.d02.repair.01", "site.d02.clinic"));
            Accept(session.IssueEscortGo("route.main"));
            for (int tick = 0; tick < 70 && !session.IsTerminal; tick++)
            {
                session.Advance(1);
                if (Node(session, "hold_plaza").Phase == OperationsTacticalNodePhase.Complete)
                    continue;
                if (session.TryGetActor("unit.d02.rifle.01", out OperationsTacticalActorState rifle))
                {
                    map.TryGetByAlias("site.plaza", out OperationsGreyboxAnchor plaza);
                    if (OperationsTacticalRules.Within(rifle.X, rifle.Z, plaza.X, plaza.Z, OperationsTacticalRules.HoldMeters))
                        Accept(session.IssueHold("unit.d02.rifle.01", plaza.AnchorId));
                }
            }

            return session;
        }

        private static OperationsTacticalAuthoring OldQuarterAuthoring()
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
                    InteractNode("interact_evidence", "site.d01.evidence", 8),
                    OptionalHold(map),
                    ExtractNode(new[] { "scan_clinic", "interact_evidence" })
                },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                    Rifle("unit.d01.rifle.02", Anchor(map, "spawn.player")),
                    Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 100),
                    Evidence("site.d01.evidence", Anchor(map, "site.evidence")),
                    Hostile("hostile.d01.rifle.01", Anchor(map, "spawn.enemy_b"), 0),
                    Hostile("hostile.d01.rifle.a.01", Anchor(map, "spawn.enemy_a"), 1)
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

        private static OperationsTacticalAuthoring CivicAuthoring()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.d02.greybox",
                MapId = map.MapId,
                ForcePackage = OperationsForcePackageKind.Service,
                Materials = 240,
                PartialNodeIds = new[] { "hold_plaza" },
                Nodes = new[]
                {
                    HoldNode("hold_plaza", Anchor(map, "site.plaza"), 8),
                    RepairNode("repair_clinic", "site.d02.clinic"),
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
                    Rifle("unit.d02.rifle.01", Anchor(map, "spawn.player")),
                    Rifle("unit.d02.rifle.02", Anchor(map, "spawn.player")),
                    RepairSpecialist("unit.d02.repair.01", Anchor(map, "site.service_stand")),
                    Site("site.d02.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 40),
                    Cargo("unit.d02.cargo.01", Anchor(map, "site.cargo_start"))
                }
            };
        }

        private static OperationsTacticalAuthoring HoldAuthoring(OperationsMapGreybox map, bool contested)
        {
            var spawns = new List<OperationsTacticalSpawnAuthoring>
            {
                Rifle("unit.d02.rifle.01", Anchor(map, contested ? "site.plaza" : "site.plaza"))
            };
            if (contested)
                spawns.Add(Hostile("hostile.d02.rifle.01", Anchor(map, "site.plaza"), 0));
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.hold",
                MapId = map.MapId,
                Nodes = new[] { HoldNode("hold_plaza", Anchor(map, "site.plaza"), 8) },
                Spawns = spawns.ToArray()
            };
        }

        private static OperationsTacticalAuthoring RepairAuthoring(OperationsMapGreybox map, bool restored, int health, bool keepOpen = false)
        {
            var nodes = new List<OperationsTacticalNodeAuthoring> { RepairNode("repair_clinic", "site.d02.clinic") };
            if (keepOpen)
                nodes.Add(HoldNode("hold_open", Anchor(map, "site.plaza"), 500));
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.repair",
                MapId = map.MapId,
                ForcePackage = OperationsForcePackageKind.Service,
                Materials = 240,
                LaunchRestoredSiteIds = restored ? new[] { "site.d02.clinic" } : Array.Empty<string>(),
                Nodes = nodes.ToArray(),
                Spawns = new[]
                {
                    RepairSpecialist("unit.d02.repair.01", Anchor(map, "site.service_stand")),
                    Site("site.d02.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), health),
                    Hostile("hostile.d02.rifle.01", Anchor(map, "spawn.enemy_b"), 0)
                }
            };
        }

        private static OperationsTacticalAuthoring EscortAuthoring(OperationsMapGreybox map, string cargoAnchor)
        {
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.escort",
                MapId = map.MapId,
                ForcePackage = OperationsForcePackageKind.Service,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "escort_cargo",
                        Rule = OperationsObjectiveRuleKind.Escort,
                        TargetIds = new[] { "unit.d02.cargo.01" },
                        LegalRouteIds = new[] { "route.main", "route.safe" }
                    }
                },
                Spawns = new[]
                {
                    Rifle("unit.d02.rifle.01", Anchor(map, "spawn.player")),
                    Cargo("unit.d02.cargo.01", cargoAnchor)
                }
            };
        }

        private static OperationsTacticalAuthoring ExtractAuthoring(OperationsMapGreybox map, bool atExit)
        {
            string anchor = Anchor(map, atExit ? "exit.ground" : "spawn.player");
            return new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.extract",
                MapId = map.MapId,
                Nodes = new[] { ExtractNode(Array.Empty<string>()) },
                Spawns = new[]
                {
                    Rifle("unit.d01.rifle.01", anchor),
                    Rifle("unit.d01.rifle.02", anchor)
                }
            };
        }

        private static OperationsTacticalAuthoring DeadlineAuthoring(OperationsMapGreybox map, int deadline)
        {
            var authoring = OldQuarterAuthoring();
            authoring.DeadlineTicks = deadline;
            authoring.Waves = Array.Empty<OperationsTacticalWaveAuthoring>();
            authoring.Spawns = new[]
            {
                Rifle("unit.d01.rifle.01", Anchor(map, "spawn.player")),
                Rifle("unit.d01.rifle.02", Anchor(map, "spawn.player")),
                Site("site.d01.clinic", "role.neutral.clinic", Anchor(map, "site.clinic"), 100),
                Evidence("site.d01.evidence", Anchor(map, "site.evidence"))
            };
            return authoring;
        }

        private static OperationsTacticalCompileResult CompileScan(OperationsMapGreybox map, OperationsTacticalSpawnAuthoring[] spawns)
        {
            return OperationsTacticalCompiler.Compile(new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.scan",
                MapId = map.MapId,
                Nodes = new[] { Scan("scan_clinic", "site.d01.clinic", Array.Empty<string>()) },
                Spawns = spawns
            });
        }

        private static OperationsTacticalNodeAuthoring Scan(string nodeId, string targetId, string[] prerequisites)
        {
            return new OperationsTacticalNodeAuthoring
            {
                NodeId = nodeId,
                Rule = OperationsObjectiveRuleKind.Scan,
                TargetIds = new[] { targetId },
                DurationTicks = OperationsTacticalRules.ScanSeconds,
                RadiusMeters = (int)OperationsTacticalRules.ScanMeters,
                Prerequisites = prerequisites,
                Activation = prerequisites.Length == 0
                    ? OperationsActivationPolicyKind.Launch
                    : OperationsActivationPolicyKind.Prerequisites
            };
        }

        private static OperationsTacticalNodeAuthoring OptionalScan()
        {
            OperationsTacticalNodeAuthoring node = Scan("optional_scan", "site.d01.optional", Array.Empty<string>());
            node.Optional = true;
            return node;
        }

        private static OperationsTacticalNodeAuthoring InteractNode(string nodeId, string targetId, int seconds)
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

        private static OperationsTacticalNodeAuthoring OptionalHold(OperationsMapGreybox map)
        {
            OperationsTacticalNodeAuthoring hold = HoldNode("hold_courtyard", Anchor(map, "site.courtyard"), 20);
            hold.Optional = true;
            return hold;
        }

        private static OperationsTacticalNodeAuthoring HoldNode(string nodeId, string zoneAnchorId, int seconds, string[] prerequisites = null)
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

        private static OperationsTacticalNodeAuthoring RepairNode(string nodeId, string siteId)
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

        private static OperationsTacticalNodeAuthoring ExtractNode(string[] prerequisites)
        {
            return new OperationsTacticalNodeAuthoring
            {
                NodeId = "extract_force",
                Rule = OperationsObjectiveRuleKind.Extract,
                TargetCount = 2,
                Prerequisites = prerequisites,
                Activation = prerequisites.Length == 0
                    ? OperationsActivationPolicyKind.Launch
                    : OperationsActivationPolicyKind.Prerequisites
            };
        }

        private static OperationsTacticalSpawnAuthoring Rifle(string objectId, string anchorId) =>
            new()
            {
                ObjectId = objectId,
                RoleId = "role.friendly.rifle",
                RosterRole = OperationsRosterRoleKind.RifleInfantry,
                Faction = OperationsTacticalFaction.Player,
                Body = OperationsTacticalBodyKind.Infantry,
                AnchorId = anchorId,
                Health = 100
            };

        private static OperationsTacticalSpawnAuthoring RepairSpecialist(string objectId, string anchorId) =>
            new()
            {
                ObjectId = objectId,
                RoleId = "role.friendly.repair",
                RosterRole = OperationsRosterRoleKind.RepairSpecialist,
                Faction = OperationsTacticalFaction.Player,
                Body = OperationsTacticalBodyKind.Infantry,
                AnchorId = anchorId,
                Health = 100
            };

        private static OperationsTacticalSpawnAuthoring Hostile(string objectId, string anchorId, int group) =>
            new()
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

        private static OperationsTacticalSpawnAuthoring Site(string objectId, string roleId, string anchorId, int health) =>
            new()
            {
                ObjectId = objectId,
                RoleId = roleId,
                RosterRole = OperationsRosterRoleKind.None,
                Faction = OperationsTacticalFaction.Neutral,
                Body = OperationsTacticalBodyKind.Site,
                AnchorId = anchorId,
                Health = health
            };

        private static OperationsTacticalSpawnAuthoring Evidence(string objectId, string anchorId) =>
            new()
            {
                ObjectId = objectId,
                RoleId = "role.neutral.evidence",
                RosterRole = OperationsRosterRoleKind.None,
                Faction = OperationsTacticalFaction.Neutral,
                Body = OperationsTacticalBodyKind.Evidence,
                AnchorId = anchorId,
                Health = 100
            };

        private static OperationsTacticalSpawnAuthoring Cargo(string objectId, string anchorId) =>
            new()
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

        private static void RequireAliases(OperationsMapGreybox map)
        {
            string[] aliases =
            {
                "spawn.player",
                "spawn.enemy_a",
                "spawn.enemy_b",
                "spawn.staging_a",
                "spawn.staging_b",
                "exit.ground"
            };
            for (int index = 0; index < aliases.Length; index++)
                Require(map.TryGetByAlias(aliases[index], out _), map.MapId + ":" + aliases[index]);
            Require(map.TryGetRoute("route.main", out _) && map.TryGetRoute("route.safe", out _) && map.TryGetRoute("route.flank", out _));
        }

        private static OperationsTacticalSession Start(OperationsTacticalAuthoring authoring) =>
            Start(authoring, OperationsP0Checks.CreateLaunch());

        private static OperationsTacticalSession Start(OperationsTacticalAuthoring authoring, OperationsLaunchPayload launch) =>
            new(Must(authoring), launch);

        private static OperationsCompiledTactical Must(OperationsTacticalAuthoring authoring)
        {
            OperationsTacticalCompileResult result = OperationsTacticalCompiler.Compile(authoring);
            if (!result.Accepted)
                throw new InvalidOperationException(result.Error);
            return result.Definition;
        }

        private static string Anchor(OperationsMapGreybox map, string alias)
        {
            if (!map.TryGetByAlias(alias, out OperationsGreyboxAnchor anchor))
                throw new InvalidOperationException(alias);
            return anchor.AnchorId;
        }

        private static OperationsTacticalNodeState Node(OperationsTacticalSession session, string nodeId)
        {
            if (!session.TryGetNode(nodeId, out OperationsTacticalNodeState state))
                throw new InvalidOperationException("missing node " + nodeId);
            return state;
        }

        private static OperationsTacticalActorState Actor(OperationsTacticalSession session, string objectId)
        {
            if (!session.TryGetActor(objectId, out OperationsTacticalActorState state))
                throw new InvalidOperationException("missing actor " + objectId);
            return state;
        }

        private static int CountBody(OperationsTacticalSession session, OperationsTacticalBodyKind body)
        {
            int count = 0;
            OperationsTacticalActorState[] actors = session.CopyActors();
            for (int index = 0; index < actors.Length; index++)
            {
                if (actors[index].Body == body)
                    count++;
            }

            return count;
        }

        private static bool HasFact(OperationsTacticalSession session, OperationsTacticalFactKind kind, string objectId)
        {
            OperationsTacticalFact[] facts = session.CopyFacts();
            for (int index = 0; index < facts.Length; index++)
            {
                if (facts[index].Kind == kind && facts[index].ObjectId == objectId)
                    return true;
            }

            return false;
        }

        private static void Accept(OperationsTacticalCommandResult result)
        {
            if (!result.Accepted)
                throw new InvalidOperationException(result.Reason.ToString());
        }

        private static void Require(bool condition, string message = "Operations P2 check failed.")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
