using System;
using System.IO;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    /// <summary>
    /// Host/Editor checks for Operations ARIA evidence harness.
    /// Proves recordable planner-driven wins and schema scaffolding; does not claim AriaWon/playable.
    /// </summary>
    public static class OperationsAriaEvidenceChecks
    {
        public const int ExpectedCheckCount = 8;
        public const string PassMarker = "[OperationsAriaEvidenceValidation] result=Passed checks=8";

        public static void RunAll()
        {
            PublicObservationExposesTargets();
            PlannerDoesNotSwitchOnMissionId();
            UnassistedO001Victory();
            UnassistedO002Victory();
            UnassistedO003Victory();
            EvidenceSchemaFields();
            VerticalSliceEvidenceRecords();
            ScaffoldPathsAreAcceptanceShaped();
        }

        public static void PublicObservationExposesTargets()
        {
            OperationsLoopSession loop = Reach("operation.o001", 2101);
            Require(loop.TryNode("scan_signals", out OperationsTacticalNodeState node));
            Require(node.TargetIds.Length == 3, "targets");
            Require(loop.CopyPublicActors().Length > 0, "actors");
            Require(loop.TryResolveMoveAnchor("site.d01.signal_a", out string anchor) && anchor.Length > 0, "anchor");
            OperationsAriaIntent[] plan = OperationsAriaObjectivePlanner.Plan(loop);
            Require(plan.Length > 0, "plan");
            bool hasAction = false;
            for (int index = 0; index < plan.Length; index++)
            {
                if (plan[index].Skill == OperationsAriaSkillKind.Move ||
                    plan[index].Skill == OperationsAriaSkillKind.Observe ||
                    plan[index].Skill == OperationsAriaSkillKind.Scan)
                    hasAction = true;
            }

            Require(hasAction, "scan_action");
        }

        public static void PlannerDoesNotSwitchOnMissionId()
        {
            string source = ReadContentSource("OperationsAriaObjectivePlanner.cs");
            Require(!source.Contains("operation.o001"), "planner_mission_id");
            Require(!source.Contains("PlayO001"), "planner_script");
            string harness = ReadContentSource("OperationsAriaEvidenceHarness.cs");
            Require(harness.Contains("TryPlayUnassistedWin") || harness.Contains("PlayWithTrace"), "harness_unassisted");
            Require(!harness.Contains("TryPlayVisibleControlWin"), "harness_no_scripted_win");
        }

        public static void UnassistedO001Victory()
        {
            OperationsLoopSession loop = Reach("operation.o001", 2102);
            Require(OperationsAriaInputSkills.TryPlayUnassistedWin(loop));
            Require(loop.MissionOutcome == OperationsOutcomeKind.Victory);
        }

        public static void UnassistedO002Victory()
        {
            OperationsLoopSession loop = Reach("operation.o002", 2103);
            Require(OperationsAriaInputSkills.TryPlayUnassistedWin(loop));
            Require(loop.MissionOutcome == OperationsOutcomeKind.Victory);
            Require(loop.TryNode("protect_clinic", out OperationsTacticalNodeState protect) &&
                    protect.Phase == OperationsTacticalNodePhase.Complete, "protect_latched");
            Require(loop.TryNode("hold_clinic", out OperationsTacticalNodeState hold) &&
                    hold.Phase == OperationsTacticalNodePhase.Complete, "hold_complete");
            Require(loop.TryNode("escort_trucks", out OperationsTacticalNodeState escort) &&
                    escort.TargetCount == 2 &&
                    escort.ProgressCount <= escort.TargetCount, "escort_progress");
        }

        public static void UnassistedO003Victory()
        {
            OperationsLoopSession loop = Reach("operation.o003", 2104);
            Require(OperationsAriaInputSkills.TryPlayUnassistedWin(loop));
            Require(loop.MissionOutcome == OperationsOutcomeKind.Victory);
            Require(loop.TryNode("protect_clinic_pumps", out OperationsTacticalNodeState protect) &&
                    protect.Phase == OperationsTacticalNodePhase.Complete, "o003_protect_latched");
        }

        public static void EvidenceSchemaFields()
        {
            Require(OperationsAriaEvidenceHarness.TryRunMission("operation.o001", 1102, "en", out OperationsAriaEvidenceRecord record, out string failure), failure);
            string json = record.ToJson();
            Require(json.Contains("\"mission_id\": \"operation.o001\""));
            Require(json.Contains("\"seed\": 1102"));
            Require(json.Contains("\"difficulty\": \"Regular\""));
            Require(json.Contains("\"language\": \"en\""));
            Require(json.Contains("\"input_source\": \"ARIA\""));
            Require(json.Contains("\"terminal_outcome\""));
            Require(json.Contains("\"victory\": true"));
            Require(json.Contains("\"before_district\""));
            Require(json.Contains("\"after_district\""));
            Require(json.Contains("PENDING_LIVE_BUILD"));
            Require(record.Status == "HostUnassistedVictoryRecorded");
            Require(record.WatchVirtualTouch == "PendingSeam");
            Require(record.SettledRevision != -1, "settled_revision");
            Require(!json.Contains("\"settled_revision\": -1"), "settled_revision_sentinel");
        }

        public static void VerticalSliceEvidenceRecords()
        {
            Require(OperationsAriaEvidenceHarness.TryRunVerticalSliceRegular("en", out OperationsAriaEvidenceRecord[] en, out string failureEn), failureEn);
            Require(en.Length == 3);
            Require(en[0].MissionId == "operation.o001" && en[0].Victory && en[0].SettledRevision != -1);
            Require(en[1].MissionId == "operation.o002" && en[1].Victory && en[1].SettledRevision != -1);
            Require(en[2].MissionId == "operation.o003" && en[2].Victory && en[2].SettledRevision != -1);

            Require(OperationsAriaEvidenceHarness.TryRunVerticalSliceRegular("fa", out OperationsAriaEvidenceRecord[] fa, out string failureFa), failureFa);
            Require(fa.Length == 3 && fa[0].Language == "fa" && fa[0].Victory);
        }

        public static void ScaffoldPathsAreAcceptanceShaped()
        {
            Require(OperationsAriaEvidenceHarness.EvidenceRelativePath("operation.o001", "Regular", 1102) ==
                    "Design/AgentReports/Operations/host-aria-evidence/operation.o001/Regular/1102");
            Require(OperationsAriaEvidenceHarness.ResultFileName("en") == "result.en.json");
            Require(OperationsAriaEvidenceHarness.ResultFileName("fa") == "result.fa.json");
        }

        static int _nextId = 0xE200;

        static string Id() => "cmd.operations." + (_nextId++).ToString("x8");

        static OperationsLoopSession NewLoop(int seed) =>
            OperationsLoopSession.Create(seed, new byte[] { 9, 9, 9 }, new byte[] { 8, 8 });

        static void OpenBriefing(OperationsLoopSession loop, string missionId)
        {
            Require(loop.TryOffer(missionId, out OperationsOfferSaveData offer), missionId);
            int number = DistrictNumber(offer.districtId);
            Require(loop.OpenDistrict(number).Accepted);
            Require(loop.OpenBriefing(offer.offerId).Accepted);
        }

        static void ReachActive(OperationsLoopSession loop, string missionId)
        {
            OpenBriefing(loop, missionId);
            string deployId = Id();
            Require(loop.BeginDeploy(deployId).Accepted);
            Require(loop.CompleteAttempt(deployId).Accepted);
            Require(loop.BeginLaunch().Accepted);
            Require(loop.CompleteLaunch().Accepted);
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted);
            Require(loop.CompleteActive().Accepted);
        }

        static OperationsLoopSession Reach(string missionId, int seed)
        {
            OperationsLoopSession loop = NewLoop(seed);
            if (missionId == "operation.o003" && !loop.TryOffer(missionId, out _))
            {
                ReachActive(loop, "operation.o001");
                Require(OperationsAriaInputSkills.TryPlayUnassistedWin(loop));
                Require(loop.BeginResult().Accepted);
                Require(loop.CompleteResult().Accepted);
                string settleId = Id();
                Require(loop.BeginSettlement(settleId).Accepted);
                Require(loop.CompleteSettlement(settleId).Accepted);
                Require(loop.BeginReturn().Accepted);
                Require(loop.CompleteReturn().Accepted);
                Require(loop.RequestEndDay(Id()).Accepted);
            }

            ReachActive(loop, missionId);
            return loop;
        }

        static int DistrictNumber(string districtId)
        {
            for (int number = 1; number <= 6; number++)
            {
                if (OperationsIdentityRules.DistrictId(number) == districtId)
                    return number;
            }

            throw new InvalidOperationException(districtId);
        }

        static string ReadContentSource(string fileName)
        {
            string root = FindRepoRoot();
            string path = Path.Combine(root, "Assets", "Game", "Scripts", "Operations", "Content", fileName);
            return File.ReadAllText(path);
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
