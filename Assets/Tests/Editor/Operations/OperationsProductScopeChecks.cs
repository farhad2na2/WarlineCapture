using System;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Strategic;

namespace Game.Tests.Editor.Operations
{
    // These are isolated model fixtures. They do not establish native input readiness.
    public static class OperationsProductScopeChecks
    {
        public const string PassMarker = "[OperationsProductScope] result=Passed filtering,checkpoint,completion,replay,upgrade,reward-dedup,legacy,unknown";
        static int sequence = 0xE000;
        static string Id() => OperationsStableIds.Generated("cmd", sequence++);
        static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException(reason); }

        public static void RunAll()
        {
            var legacy = OperationsSaveMigration.CreateEmpty();
            legacy.activeRun = new OperationsRunSaveData { scopeId = "", runId = "run.operations.00000001" };
            Require(OperationsSaveMigration.Migrate(legacy).Data.activeRun.scopeId == OperationsContentScope.Full, "legacy scope");
            var future = OperationsSaveMigration.CreateEmpty();
            future.activeRun = new OperationsRunSaveData { scopeId = "operations.future.v9" };
            Require(OperationsSaveMigration.Migrate(future).Disposition == OperationsSaveDispositionKind.ReadOnlyUnknown, "unknown scope");
            Require(future.activeRun.scopeId == "operations.future.v9", "future preserved");

            var loop = OperationsLoopSession.Create(9111, new byte[] { 7, 8 }, new byte[] { 9 }, OperationsContentScope.Intro);
            Require(loop.ScopeId == OperationsContentScope.Intro, "intro identity");
            Require(!loop.OpenDistrict(2).Accepted, "intro district boundary");
            Require(!loop.TryOffer("operation.o011", out _), "intro offer boundary");
            Require(!loop.TryOffer("operation.o004", out _), "intro mission boundary");
            Activate(loop, "operation.o001");
            Require(loop.BeginCheckpoint().Accepted && loop.PublishCheckpoint().Accepted, "intro checkpoint");
            loop.Interrupt();
            Require(loop.HasMission && !loop.CheckpointCorrupt, "intro checkpoint resume");
            WinAndFinish(loop, "operation.o001");
            Require(!loop.IntroCompleted, "one victory is not completion");
            Require(loop.RequestEndDay(Id()).Accepted, "intro end day");
            Activate(loop, "operation.o002");
            WinAndFinish(loop, "operation.o002");
            Require(!loop.IntroCompleted, "two victories are not completion");
            Require(loop.RequestEndDay(Id()).Accepted, "intro next day");
            Activate(loop, "operation.o003");
            Require(loop.TryMaterials(out int materials) && materials == 80, "scenario repair materials");
            WinAndFinish(loop, "operation.o003");
            Require(loop.IntroCompleted, "all three victories complete intro");
            var completed = OperationsStrategicSession.FromCommittedJson(loop.CommittedJson);
            Require(!completed.CityCompleted && completed.Incidents.Length == 0, "intro excludes city completion and pressure");
            Require(completed.Save.firstClearRewardIds.Length == 3, "intro first clears");
            for (int i = 0; i < 12; i++)
                Require(loop.RequestEndDay(Id()).Accepted, "intro recovery day");
            Require(loop.TryOffer("operation.o001", out _), "free replay retained");
            Require(!loop.TryOffer("operation.o004", out _), "later content remains excluded");
            Require(loop.CampaignEnvelope[0] == 7 && loop.QuickGameEnvelope[0] == 9, "foreign envelopes preserved");

            var upgrade = OperationsStrategicSession.FromCommittedJson(loop.CommittedJson);
            Require(upgrade.SubmitNewRun(new OperationsCommand(Id(), upgrade.Revision,
                OperationsCommandKind.NewRun, "", "", ""), 9111, OperationsDifficultyKind.Regular,
                OperationsContentScope.Full).Accepted, "full new run");
            Require(!upgrade.IntroCompleted && !upgrade.CityCompleted, "new full city state");
            Require(!OperationsCityWorld.FromSave(upgrade.Save).HasVictory("operation.o001"), "intro victory not full city victory");
            Require(upgrade.Save.firstClearRewardIds.Length == 3, "account first clears retained");
            Require(upgrade.Save.runSummaries.Length == 1 &&
                upgrade.Save.runSummaries[0].scopeId == OperationsContentScope.Intro, "intro archived separately");
            Require(upgrade.Save.activeRun.runId != completed.Save.activeRun.runId, "scope run identities differ");
        }

        static void Activate(OperationsLoopSession loop, string mission)
        {
            Require(loop.TryOffer(mission, out var offer), "offer " + mission);
            Require(loop.OpenDistrict(1).Accepted && loop.OpenBriefing(offer.offerId).Accepted, "briefing");
            string deploy = Id();
            Require(loop.BeginDeploy(deploy).Accepted && loop.CompleteAttempt(deploy).Accepted, "deploy");
            Require(loop.BeginLaunch().Accepted && loop.CompleteLaunch().Accepted, "launch");
            Require(loop.ScopeId == OperationsContentScope.Intro, "launch scope");
            Require(loop.BeginActive(true, true, true, loop.ContentHash).Accepted && loop.CompleteActive().Accepted, "active");
        }
        static void WinAndFinish(OperationsLoopSession loop, string mission)
        {
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, mission), "model win " + mission);
            Require(loop.BeginResult().Accepted && loop.CompleteResult().Accepted, "result");
            Require(loop.TryCommittedResult(out var result) && result.ScopeId == OperationsContentScope.Intro, "result scope");
            string settlement = Id();
            Require(loop.BeginSettlement(settlement).Accepted && loop.CompleteSettlement(settlement).Accepted, "settlement");
            int credits = loop.Credits;
            Require(loop.CompleteSettlement(settlement).Accepted && loop.Credits == credits, "duplicate settlement");
            Require(loop.BeginReturn().Accepted && loop.CompleteReturn().Accepted, "return");
        }
    }
}
