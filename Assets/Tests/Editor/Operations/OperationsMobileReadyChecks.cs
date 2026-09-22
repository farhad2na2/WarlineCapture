using System;
using System.IO;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Focused host checks for O001–O003 mobile-ready presentation + pacing.
    /// Does not claim playable regression or AriaWon.
    /// </summary>
    public static class OperationsMobileReadyChecks
    {
        public const int ExpectedCheckCount = 8;
        public const string PassMarker = "[OperationsMobileReadyValidation] result=Passed checks=8";

        public static void RunAll()
        {
            DeadlinesAndPartialsPreserved();
            ScanRepairHoldPacingCompressed();
            HoldRefreshFreezesWithoutReissue();
            LocalizedObjectiveChromeNotRawIds();
            PresentationDrawsWorldAndHidesDebug();
            VictorySellsOutcomeWithoutBotChrome();
            CaptureHarnessStillWired();
            OwnershipStaysOperations();
        }

        public static void DeadlinesAndPartialsPreserved()
        {
            Require(OperationsAuthoredMissions.TryCompile("operation.o001", out OperationsCompiledTactical o001, out _, out string e1), e1);
            Require(o001.DeadlineTicks == 720 && o001.PartialProgressMinimum == 2);
            Require(OperationsAuthoredMissions.TryCompile("operation.o002", out OperationsCompiledTactical o002, out _, out string e2), e2);
            Require(o002.DeadlineTicks == 840 && o002.PartialProgressMinimum == 1);
            Require(OperationsAuthoredMissions.TryCompile("operation.o003", out OperationsCompiledTactical o003, out _, out string e3), e3);
            Require(o003.DeadlineTicks == 900 && o003.PartialMinimumComplete == 1);
            Require(CatalogDeadline("operation.o001") == 720);
            Require(CatalogDeadline("operation.o002") == 840);
            Require(CatalogDeadline("operation.o003") == 900);
        }

        public static void ScanRepairHoldPacingCompressed()
        {
            Require(OperationsTacticalRules.ScanSeconds <= 8, "scan_pacing");
            Require(OperationsTacticalRules.RepairSeconds <= 20, "repair_pacing");
            Require(OperationsTacticalRules.HoldRefreshSeconds <= 8, "hold_refresh");
            Require(OperationsAuthoredMissions.TryCompile("operation.o002", out OperationsCompiledTactical o002, out _, out _));
            Require(FindNode(o002, "hold_clinic").DurationTicks <= 15, "o002_hold");
            Require(OperationsAuthoredMissions.TryCompile("operation.o003", out OperationsCompiledTactical o003, out _, out _));
            Require(FindNode(o003, "hold_service_court").DurationTicks <= 24, "o003_hold");
        }

        public static void HoldRefreshFreezesWithoutReissue()
        {
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.CivicCenter;
            var authoring = new OperationsTacticalAuthoring
            {
                GraphId = "graph.operations.hold_refresh",
                MapId = map.MapId,
                Nodes = new[]
                {
                    new OperationsTacticalNodeAuthoring
                    {
                        NodeId = "hold_plaza",
                        Rule = OperationsObjectiveRuleKind.Hold,
                        ZoneAnchorId = Anchor(map, "site.plaza"),
                        DurationTicks = 12,
                        RadiusMeters = (int)OperationsTacticalRules.HoldMeters
                    }
                },
                Spawns = new[]
                {
                    new OperationsTacticalSpawnAuthoring
                    {
                        ObjectId = "unit.d02.rifle.01",
                        RoleId = "role.friendly.rifle",
                        RosterRole = OperationsRosterRoleKind.RifleInfantry,
                        Faction = OperationsTacticalFaction.Player,
                        Body = OperationsTacticalBodyKind.Infantry,
                        AnchorId = Anchor(map, "site.plaza"),
                        Health = 100
                    }
                }
            };
            OperationsTacticalCompileResult compile = OperationsTacticalCompiler.Compile(authoring);
            Require(compile.Accepted, compile.Error);
            var session = new OperationsTacticalSession(compile.Definition, OperationsP0Checks.CreateLaunch());
            Require(session.IssueHold("unit.d02.rifle.01", Anchor(map, "site.plaza")).Accepted);
            session.Advance(OperationsTacticalRules.HoldRefreshSeconds);
            int progress = Node(session, "hold_plaza").ProgressTicks;
            Require(progress > 0);
            session.Advance(OperationsTacticalRules.HoldRefreshSeconds + 2);
            Require(Node(session, "hold_plaza").ProgressTicks == progress, "stale_hold_froze");
            Require(session.IssueHold("unit.d02.rifle.01", Anchor(map, "site.plaza")).Accepted);
            session.Advance(1);
            Require(Node(session, "hold_plaza").ProgressTicks == progress + 1, "refresh_resumes");
        }

        public static void LocalizedObjectiveChromeNotRawIds()
        {
            Require(OperationsLocalizedCopy.TryGet("operations.objective.scan_signals", "en", out string en) &&
                    en.IndexOf("scan_signals", StringComparison.Ordinal) < 0);
            Require(OperationsLocalizedCopy.TryGet("operations.objective.scan_signals", "fa", out string fa) && fa.Length > 0);
            OperationsLoopSession loop = ReachActive("operation.o001", 2210);
            Require(loop.TryReadHud(out OperationsHudFrame hud));
            Require(hud.Required.Length > 0);
            string presentation = ReadPresentationSource();
            Require(presentation.Contains("BuildLocalizedObjectives"), "localized_builder");
            Require(presentation.Contains("operations.objective."), "objective_keys");
            Require(!presentation.Contains("seed=\" + Seed") && !presentation.Contains("seed=\").Append"), "no_seed_concat");
        }

        public static void PresentationDrawsWorldAndHidesDebug()
        {
            string presentation = ReadPresentationSource();
            string world = ReadCaptureSource("OperationsTacticalWorldShell.cs");
            Require(world.Contains("OperationsTacticalWorldShell"), "world_shell");
            Require(world.Contains("using Game.Operations.Contracts;"), "contracts_using");
            Require(world.Contains("CreatePrimitive"), "primitives");
            Require(world.Contains("Selection"), "selection");
            Require(world.Contains("Universal Render Pipeline/Unlit"), "urp_unlit");
            Require(world.Contains("_BaseColor"), "base_color");
            Require(world.Contains("UniversalAdditionalCameraData"), "urp_camera");
            Require(!world.Contains("Sprites/Default"), "no_builtin_sprite_shader");
            Require(presentation.Contains("OperationsTacticalWorldShell"), "presenter_uses_world");
            Require(presentation.Contains("PhonePanelRect"), "phone_mock");
            Require(presentation.Contains("ShowDebugChrome = false") || presentation.Contains("ShowDebugChrome=false"), "debug_off");
            Require(!presentation.Contains("Ops-owned win screen (Watch shared-UI seam not opened)"), "no_dev_footer");
        }

        public static void VictorySellsOutcomeWithoutBotChrome()
        {
            string presentation = ReadPresentationSource();
            Require(presentation.Contains("DrawVictoryCard"), "victory_card");
            Require(presentation.Contains("reward_credits"), "credits");
            Require(presentation.Contains("result.victory"), "localized_victory");
            Require(!presentation.Contains("intents="), "no_intent_chrome");
            Require(!presentation.Contains("result_hash="), "no_hash_chrome");
        }

        public static void CaptureHarnessStillWired()
        {
            string root = FindRepoRoot();
            Require(File.Exists(Path.Combine(root, "Tools", "Operations", "check_aria_playmode_capture.py")));
            Require(File.Exists(Path.Combine(root, "Tools", "Operations", "Invoke-OperationsAriaPlayModeCapture.ps1")));
            Require(File.Exists(Path.Combine(root, "Assets", "Game", "Scripts", "Operations", "Capture", "OperationsTacticalWorldShell.cs")));
        }

        public static void OwnershipStaysOperations()
        {
            string presentation = ReadPresentationSource();
            string world = ReadCaptureSource("OperationsTacticalWorldShell.cs");
            Require(!presentation.Contains("MatchSceneView") && !world.Contains("MatchSceneView"));
            Require(!presentation.Contains("AriaPlayCapability") && !world.Contains("AriaPlayCapability"));
            Require(!presentation.Contains("SaveDataModel") && !world.Contains("SkirmishExpansion"));
            Require(presentation.Contains("namespace Game.Operations.Capture"));
            Require(world.Contains("namespace Game.Operations.Capture"));
        }

        static OperationsCompiledNode FindNode(OperationsCompiledTactical compiled, string nodeId)
        {
            for (int index = 0; index < compiled.Nodes.Length; index++)
            {
                if (compiled.Nodes[index].NodeId == nodeId)
                    return compiled.Nodes[index];
            }

            throw new InvalidOperationException("missing_node:" + nodeId);
        }

        static int CatalogDeadline(string missionId)
        {
            for (int index = 0; index < OperationsCatalogIndex.Entries.Length; index++)
            {
                if (OperationsCatalogIndex.Entries[index].MissionId == missionId)
                    return OperationsCatalogIndex.Entries[index].HardDeadlineSeconds;
            }

            throw new InvalidOperationException("missing_catalog:" + missionId);
        }

        static OperationsTacticalNodeState Node(OperationsTacticalSession session, string nodeId)
        {
            Require(session.TryGetNode(nodeId, out OperationsTacticalNodeState state));
            return state;
        }

        static string Anchor(OperationsMapGreybox map, string alias)
        {
            Require(map.TryGetByAlias(alias, out OperationsGreyboxAnchor anchor));
            return anchor.AnchorId;
        }

        static OperationsLoopSession ReachActive(string missionId, int seed)
        {
            OperationsLoopSession loop = OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });
            Require(loop.TryOffer(missionId, out OperationsOfferSaveData offer));
            int district = 1;
            for (int number = 1; number <= 6; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == offer.districtId)
                    district = number;
            }

            Require(loop.OpenDistrict(district).Accepted);
            Require(loop.OpenBriefing(offer.offerId).Accepted);
            string deployId = "cmd.operations." + seed.ToString("x8");
            Require(loop.BeginDeploy(deployId).Accepted && loop.CompleteAttempt(deployId).Accepted);
            Require(loop.BeginLaunch().Accepted && loop.CompleteLaunch().Accepted);
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted && loop.CompleteActive().Accepted);
            return loop;
        }

        static string ReadPresentationSource() => ReadCaptureSource("OperationsAriaPlayModePresentation.cs");

        static string ReadCaptureSource(string fileName)
        {
            return File.ReadAllText(Path.Combine(
                FindRepoRoot(),
                "Assets",
                "Game",
                "Scripts",
                "Operations",
                "Capture",
                fileName));
        }

        static string FindRepoRoot()
        {
            string directory = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(directory))
            {
                if (File.Exists(Path.Combine(directory, "AGENTS.md")) &&
                    Directory.Exists(Path.Combine(directory, "Assets", "Game", "Scripts", "Operations")))
                    return directory;
                directory = Directory.GetParent(directory)?.FullName;
            }

            throw new InvalidOperationException("repo_root");
        }

        static void Require(bool condition, string message = "require")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
