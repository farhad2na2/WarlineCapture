using System;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Tests.Editor.Operations
{
    public static class OperationsP4Checks
    {
        public const int ExpectedCheckCount = 14;
        public const string PassMarker = "[OperationsP4Validation] result=Passed checks=14";

        public static void RunAll()
        {
            AuthoredMissionsCompile();
            LocalizedCopyCoversSlice();
            ManualO001VictoryPath();
            ManualO002VictoryPath();
            ManualO003VictoryPath();
            AriaVisibleControlWinsWired();
            PartialConcludeAndWithdraw();
            ProtectFailureIsDefeat();
            CheckpointRecoveryOnO003();
            DayReportProjection();
            MultiDayScaffold();
            ApproachesExposeTwoRoutes();
            DistrictChangesApplyPerFamily();
            OwnershipStaysOperations();
        }

        public static void AuthoredMissionsCompile()
        {
            Require(OperationsAuthoredMissions.TryCompile("operation.o001", out OperationsCompiledTactical o001, out string h1, out string e1), e1);
            Require(h1 == OperationsAuthoredMissions.O001Hash);
            Require(o001.DeadlineTicks == 720 && o001.PartialProgressMinimum == 2);
            Require(OperationsAuthoredMissions.TryCompile("operation.o002", out OperationsCompiledTactical o002, out string h2, out string e2), e2);
            Require(h2 == OperationsAuthoredMissions.O002Hash && o002.DeadlineTicks == 840);
            Require(OperationsAuthoredMissions.TryCompile("operation.o003", out OperationsCompiledTactical o003, out string h3, out string e3), e3);
            Require(h3 == OperationsAuthoredMissions.O003Hash && o003.Materials == 80);
            Require(o003.PartialMinimumComplete == 1);
        }

        public static void LocalizedCopyCoversSlice()
        {
            Require(OperationsLocalizedCopy.HasMissionKeys("o001"));
            Require(OperationsLocalizedCopy.HasMissionKeys("o002"));
            Require(OperationsLocalizedCopy.HasMissionKeys("o003"));
            Require(OperationsLocalizedCopy.Require("operations.o001.title", "en").Length > 0);
            Require(OperationsLocalizedCopy.Require("operations.o001.title", "fa").Length > 0);
            Require(OperationsLocalizedCopy.Require("operations.o002.approach.main", "en") !=
                    OperationsLocalizedCopy.Require("operations.o002.approach.safe", "en"));
        }

        public static void ManualO001VictoryPath()
        {
            OperationsLoopSession loop = Reach("operation.o001", 1401);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o001"));
            Finish(loop);
            Require(loop.MissionVictory("operation.o001"));
            AssertDistrict(loop, 1, 40, 46, 45, 48, 38, 22, 45);
            Require(loop.Credits == 120);
        }

        public static void ManualO002VictoryPath()
        {
            OperationsLoopSession loop = Reach("operation.o002", 1402);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o002"));
            Finish(loop);
            Require(loop.MissionVictory("operation.o002"));
            AssertDistrict(loop, 1, 44, 51, 47, 47, 20, 22, 59);
        }

        public static void ManualO003VictoryPath()
        {
            OperationsLoopSession loop = Reach("operation.o003", 1403);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o003"));
            Finish(loop);
            Require(loop.MissionVictory("operation.o003"));
        }

        public static void AriaVisibleControlWinsWired()
        {
            string[] missions = { "operation.o001", "operation.o002", "operation.o003" };
            for (int index = 0; index < missions.Length; index++)
            {
                OperationsLoopSession loop = Reach(missions[index], 1410 + index);
                OperationsAriaIntent[] plan = OperationsAriaObjectivePlanner.Plan(loop);
                Require(plan.Length > 0, missions[index] + " plan");
                Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, missions[index]), missions[index]);
                Require(loop.MissionOutcome == OperationsOutcomeKind.Victory, missions[index] + " " + loop.MissionOutcome);
            }
        }

        public static void PartialConcludeAndWithdraw()
        {
            OperationsLoopSession partial = Reach("operation.o001", 1420);
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.signal_a", out OperationsGreyboxAnchor a));
            Require(map.TryGetByAlias("site.signal_b", out OperationsGreyboxAnchor b));
            Require(partial.Move("unit.d01.rifle.01", a.AnchorId).Accepted);
            Require(partial.Move("unit.d01.rifle.02", b.AnchorId).Accepted);
            for (int step = 0; step < 8; step++)
                partial.Advance(1);
            Require(partial.Observe("unit.d01.rifle.01", "site.d01.signal_a").Accepted);
            Require(partial.Scan("unit.d01.rifle.01", "site.d01.signal_a").Accepted);
            Require(partial.Observe("unit.d01.rifle.02", "site.d01.signal_b").Accepted);
            Require(partial.Scan("unit.d01.rifle.02", "site.d01.signal_b").Accepted);
            for (int step = 0; step < 20; step++)
                partial.Advance(1);
            Require(partial.Extract("unit.d01.rifle.01").Accepted);
            Require(partial.Extract("unit.d01.rifle.02").Accepted);
            for (int step = 0; step < 30; step++)
                partial.Advance(1);
            Require(partial.ConcludeMission().Accepted, "conclude");
            Require(partial.MissionOutcome == OperationsOutcomeKind.Partial);
            Finish(partial);
            Require(!partial.MissionVictory("operation.o001"));

            OperationsLoopSession withdrawn = Reach("operation.o002", 1421);
            Require(withdrawn.WithdrawMission().Accepted);
            Require(withdrawn.MissionOutcome == OperationsOutcomeKind.Withdrawn);
            Finish(withdrawn);
            Require(withdrawn.Credits == 0);
            AssertDistrict(withdrawn, 1, 38, 45, 45, 53, 20, 20, 45);
        }

        public static void ProtectFailureIsDefeat()
        {
            OperationsLoopSession loop = Reach("operation.o002", 1430);
            Require(loop.DestroySite("site.d01.clinic").Accepted);
            for (int step = 0; step < 5 && !loop.MissionTerminal; step++)
                loop.Advance(1);
            Require(loop.MissionTerminal && loop.MissionOutcome == OperationsOutcomeKind.Defeat, loop.MissionOutcome.ToString());
        }

        public static void CheckpointRecoveryOnO003()
        {
            OperationsLoopSession loop = Reach("operation.o003", 1440);
            OperationsMapGreybox map = OperationsMapGreyboxCatalog.OldQuarter;
            Require(map.TryGetByAlias("site.pump_west", out OperationsGreyboxAnchor west));
            Require(loop.Move("unit.d01.rifle.01", west.AnchorId).Accepted);
            loop.Advance(3);
            Require(loop.Attack("unit.d01.rifle.01", "hostile.d01.pump.01").Accepted);
            loop.Advance(1);
            Require(loop.BeginCheckpoint().Accepted);
            Require(loop.PublishCheckpoint().Accepted);
            int tick = Tick(loop);
            loop.Advance(5);
            loop.Interrupt();
            Require(Tick(loop) == tick);
            Require(loop.HasMission || loop.Offers(OperationsRecoveryChoice.ResumeCheckpoint));
        }

        public static void DayReportProjection()
        {
            OperationsLoopSession loop = NewLoop(1450);
            Require(loop.OpenReport().Accepted);
            Require(OperationsDayReportProjection.TryRead(loop, out OperationsDayReportFrame report));
            Require(report.TitleKey == "operations.day_report.title");
            Require(report.DistrictSecurity.Length == 6);
            Require(report.Day == 1);
            Require(loop.ContinueReport().Accepted);
        }

        public static void MultiDayScaffold()
        {
            OperationsLoopSession loop = NewLoop(1460);
            var slice = new OperationsMultiDaySlice(loop);
            Require(slice.TryDeployAndWin("operation.o001", out string f1), f1);
            Require(slice.TryEndDay(out string f2), f2);
            Require(loop.Day >= 2);
            Require(slice.TryDeployAndWin("operation.o002", out string f3), f3);
            Require(slice.TryEndDay(out string f4), f4);
            Require(slice.TryDeployAndWin("operation.o003", out string f5), f5);
            Require(loop.MissionVictory("operation.o001"));
            Require(loop.MissionVictory("operation.o002"));
            Require(loop.MissionVictory("operation.o003"));
            Require(loop.Day >= 3);
        }

        public static void ApproachesExposeTwoRoutes()
        {
            OperationsLoopSession loop = NewLoop(1470);
            OpenBriefing(loop, "operation.o002");
            Require(loop.TryReadBriefing(out OperationsBriefingFrame briefing));
            Require(Contains(briefing.Approaches, "route.main"));
            Require(Contains(briefing.Approaches, "route.safe"));
            Require(OperationsLocalizedCopy.Require("operations.o002.approach.main", "en").Length > 0);
        }

        public static void DistrictChangesApplyPerFamily()
        {
            OperationsLoopSession recon = Reach("operation.o001", 1480);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(recon, "operation.o001"));
            Finish(recon);
            AssertDistrict(recon, 1, 40, 46, 45, 48, 38, 22, 45);

            OperationsLoopSession escort = Reach("operation.o002", 1481);
            Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(escort, "operation.o002"));
            Finish(escort);
            AssertDistrict(escort, 1, 44, 51, 47, 47, 20, 22, 59);
        }

        public static void OwnershipStaysOperations()
        {
            Require(OperationsAssemblyManifest.ContentAssemblyName == "Game.Operations.Content");
            Require(!OperationsAuthoredMissions.IsVerticalSlice("operation.o011"));
            Require(OperationsLaunchFixtures.TryCompileMission(
                "operation.o011",
                OperationsMapGreyboxCatalog.CivicCenterMapId,
                out _,
                out string hash,
                out string error), error);
            Require(hash == OperationsLaunchFixtures.CivicCenterHash);
        }

        static int _nextId = 0xC100;

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
                Require(OperationsAriaInputSkills.TryPlayVisibleControlWin(loop, "operation.o001"));
                Finish(loop);
                Require(loop.RequestEndDay(Id()).Accepted);
            }

            ReachActive(loop, missionId);
            return loop;
        }

        static void Finish(OperationsLoopSession loop)
        {
            Require(loop.BeginResult().Accepted);
            Require(loop.CompleteResult().Accepted);
            string settleId = Id();
            Require(loop.BeginSettlement(settleId).Accepted);
            Require(loop.CompleteSettlement(settleId).Accepted);
            Require(loop.BeginReturn().Accepted);
            Require(loop.CompleteReturn().Accepted);
        }

        static int Tick(OperationsLoopSession loop)
        {
            Require(loop.TryMissionTick(out int tick));
            return tick;
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

        static void AssertDistrict(OperationsLoopSession loop, int number, params int[] expected)
        {
            var district = loop.District(number);
            Require(district.Security == expected[0], "S=" + district.Security);
            Require(district.Trust == expected[1], "T=" + district.Trust);
            Require(district.Infrastructure == expected[2], "I=" + district.Infrastructure);
            Require(district.EnemyInfluence == expected[3], "E=" + district.EnemyInfluence);
            Require(district.IntelConfidence == expected[4], "C=" + district.IntelConfidence);
            Require(district.Heat == expected[5], "H=" + district.Heat);
            Require(district.SupplyReadiness == expected[6], "L=" + district.SupplyReadiness);
        }

        static bool Contains(string[] values, string needle)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == needle)
                    return true;
            }

            return false;
        }

        static void Require(bool condition, string message = "require")
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
