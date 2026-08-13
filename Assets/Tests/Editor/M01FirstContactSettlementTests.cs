using System;
using System.IO;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class M01FirstContactSettlementTests
{
    private const string M01 = "saga.ch01.m01.first_contact";
    private const string M02 = "saga.ch01.m02.establish_base";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(test => test.FirstClearGrantsConfiguredRewardsAndRevealsM02()); passed++;
            Run(test => test.RepeatedCurrentTokenReturnsPriorSuccessWithoutGrant()); passed++;
            Run(test => test.OlderTokenAfterNewerReplayCannotGrantAgain()); passed++;
            Run(test => test.ReplayUsesReducedConfiguredReward()); passed++;
            Run(test => test.RestartPreservesSettlementHistoryAndBestMetrics()); passed++;
            Run(test => test.LegacyLastTokenMigratesIntoHistory()); passed++;
            Run(test => test.InterruptedSavePreservesPriorRewardsAndProgress()); passed++;
            Run(test => test.FirstClearAndReplayRequireDifferentReturnRoutes()); passed++;
            Run(test => test.RetryReturnRouteUsesPreservedLaunchOrigin()); passed++;
            Run(test => test.DefeatNeverSettlesOrGrants()); passed++;
            Run(test => test.ReplayBeforeFirstClearFailsClosed()); passed++;
            Run(test => test.IntelAndUnknownCustomRewardsFailClosed()); passed++;
            Run(test => test.PendingResumeClearsOnlyAfterAcceptedVictory()); passed++;
            Debug.Log($"[M01FirstContactSettlementValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01FirstContactSettlementValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    private static void Run(Action<M01FirstContactSettlementTests> test) =>
        WithFixture(fixture => test(new M01FirstContactSettlementTests { _fixture = fixture }));

    private Fixture _fixture;

    [Test]
    public void FirstClearGrantsConfiguredRewardsAndRevealsM02()
    {
        CampaignMissionSettlementResultElement response = _fixture.Settle("first", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 3, 90000);
        Assert.That(response.Accepted, Is.EqualTo(1));
        Assert.That(response.ReasonCode.ToString(), Is.EqualTo("settled"));
        PlayerProfileSaveData profile = _fixture.Service.LoadProfile();
        Assert.That(profile.commanderXp, Is.EqualTo(260));
        Assert.That(profile.credits, Is.EqualTo(1200));
        Assert.That(profile.intel, Is.Zero);
        CampaignMissionProgressSaveData[] entries = _fixture.Store.ReadAll();
        Assert.That(Array.Find(entries, entry => entry.missionId == M01).firstClearCompleted, Is.True);
        Assert.That(Array.Find(entries, entry => entry.missionId == M02).available, Is.True);
    }

    [Test]
    public void RepeatedCurrentTokenReturnsPriorSuccessWithoutGrant()
    {
        _fixture.Settle("repeat", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 2, 120000);
        CampaignMissionSettlementResultElement repeated = _fixture.Settle("repeat", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 3, 60000);
        Assert.That(repeated.Accepted, Is.EqualTo(1));
        Assert.That(repeated.ReasonCode.ToString(), Is.EqualTo("already-settled"));
        Assert.That(_fixture.Service.LoadProfile().credits, Is.EqualTo(1200));
    }

    [Test]
    public void OlderTokenAfterNewerReplayCannotGrantAgain()
    {
        _fixture.Settle("old", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 2, 120000);
        _fixture.Settle("new", 2, MissionRunKind.Replay,
            MissionLaunchOriginKind.CampaignOperations, MissionReturnDestinationKind.CampaignOperations, 3, 70000);
        CampaignMissionSettlementResultElement repeated = _fixture.Settle("old", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 2, 120000);
        Assert.That(repeated.ReasonCode.ToString(), Is.EqualTo("already-settled"));
        Assert.That(_fixture.Service.LoadProfile().credits, Is.EqualTo(1450));
        Assert.That(_fixture.Store.ReadAll()[0].settledTokens.Length, Is.EqualTo(2));
    }

    [Test]
    public void ReplayUsesReducedConfiguredReward()
    {
        _fixture.Settle("first", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 1, 180000);
        _fixture.Settle("replay", 2, MissionRunKind.Replay,
            MissionLaunchOriginKind.CampaignOperations, MissionReturnDestinationKind.CampaignOperations, 3, 60000);
        PlayerProfileSaveData profile = _fixture.Service.LoadProfile();
        Assert.That(profile.commanderXp, Is.EqualTo(260));
        Assert.That(profile.credits, Is.EqualTo(1450));
        Assert.That(_fixture.Store.ReadAll()[0].successfulReplayCount, Is.EqualTo(1));
    }

    [Test]
    public void RestartPreservesSettlementHistoryAndBestMetrics()
    {
        _fixture.Settle("persist", 3, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 2, 111000);
        CampaignMissionProgressStore restarted = new(new SaveService(new JsonSaveRepository(_fixture.RootPath)));
        CampaignMissionSettlementReceipt duplicate = restarted.SettleWithRewards(
            M01, "persist", 3, true, 3, 50000, M02, _fixture.FirstRewards);
        Assert.That(duplicate.IsDuplicate, Is.True);
        Assert.That(restarted.ReadAll()[0].bestCompletionMilliseconds, Is.EqualTo(111000));
    }

    [Test]
    public void LegacyLastTokenMigratesIntoHistory()
    {
        _fixture.Service.SaveProfile(new PlayerProfileSaveData { campaignMissionProgress = new[]
        {
            new CampaignMissionProgressSaveData
            {
                schemaVersion = 1, missionId = M01, available = true,
                firstClearCompleted = true, firstClearRewardSettled = true, lastSettledToken = "legacy:4"
            }
        }});
        CampaignMissionProgressSaveData entry = _fixture.Store.ReadAll()[0];
        Assert.That(entry.schemaVersion, Is.EqualTo(2));
        CollectionAssert.AreEqual(new[] { "legacy:4" }, entry.settledTokens);
    }

    [Test]
    public void InterruptedSavePreservesPriorRewardsAndProgress()
    {
        _fixture.Settle("safe", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CommandBase, 2, 120000);
        string profilePath = Path.Combine(_fixture.RootPath, SaveService.ProfileFileName);
        using (FileStream lockFile = new(profilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.Throws<IOException>(() => _fixture.Store.SettleWithRewards(
                M01, "blocked", 2, false, 3, 60000, M02, _fixture.ReplayRewards));
        PlayerProfileSaveData profile = _fixture.Service.LoadProfile();
        Assert.That(profile.credits, Is.EqualTo(1200));
        Assert.That(_fixture.Store.ReadAll()[0].settledTokens.Length, Is.EqualTo(1));
    }

    [Test]
    public void FirstClearAndReplayRequireDifferentReturnRoutes()
    {
        Assert.That(_fixture.Settle("wrong-first", 1, MissionRunKind.FirstClear,
            MissionLaunchOriginKind.FirstLaunch, MissionReturnDestinationKind.CampaignOperations, 1, 1)
            .ReasonCode.ToString(), Is.EqualTo("invalid-return-route"));
        Assert.That(_fixture.Settle("wrong-replay", 1, MissionRunKind.Replay,
            MissionLaunchOriginKind.CampaignOperations, MissionReturnDestinationKind.CommandBase, 1, 1)
            .ReasonCode.ToString(), Is.EqualTo("invalid-return-route"));
        Assert.That(_fixture.Service.LoadProfile().credits, Is.Zero);
    }

    [Test]
    public void RetryReturnRouteUsesPreservedLaunchOrigin()
    {
        CampaignMissionAttemptFactsComponent facts = new()
        {
            ElapsedMilliseconds = 90000, HostileTotalCount = 3, HostileDefeatedCount = 3,
            CommandSquadSpawned = 1, CommandSquadAlive = 1
        };
        CampaignMissionRuntimeComponent firstLaunch = RuntimeAtSecureCorridor(MissionLaunchOriginKind.FirstLaunch);
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(in firstLaunch, in facts, out var firstResult), Is.True);
        Assert.That(firstResult.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CommandBase));
        CampaignMissionRuntimeComponent campaign = RuntimeAtSecureCorridor(MissionLaunchOriginKind.CampaignOperations);
        Assert.That(CampaignMissionRuntimeSystem.TryEvaluate(in campaign, in facts, out var replayResult), Is.True);
        Assert.That(replayResult.ReturnDestination, Is.EqualTo(MissionReturnDestinationKind.CampaignOperations));
    }

    [Test]
    public void DefeatNeverSettlesOrGrants()
    {
        CampaignMissionSettlementResultElement response = _fixture.Settle("defeat", 1,
            MissionRunKind.FirstClear, MissionLaunchOriginKind.FirstLaunch,
            MissionReturnDestinationKind.CommandBase, 0, 90000, MissionOutcomeKind.Defeat);
        Assert.That(response.Accepted, Is.Zero);
        Assert.That(_fixture.Service.LoadProfile().credits, Is.Zero);
    }

    [Test]
    public void ReplayBeforeFirstClearFailsClosed()
    {
        CampaignMissionSettlementResultElement response = _fixture.Settle("early", 1, MissionRunKind.Replay,
            MissionLaunchOriginKind.CampaignOperations, MissionReturnDestinationKind.CampaignOperations, 2, 90000);
        Assert.That(response.Accepted, Is.Zero);
        Assert.That(response.ReasonCode.ToString(), Is.EqualTo("replay-before-first-clear"));
    }

    [Test]
    public void IntelAndUnknownCustomRewardsFailClosed()
    {
        Assert.Throws<ArgumentException>(() => _fixture.Store.SettleWithRewards(
            M01, "intel", 1, true, 1, 1, M02,
            new[] { new CampaignMissionRewardGrant(MissionRewardKind.Intel, string.Empty, 1) }));
        Assert.Throws<ArgumentException>(() => _fixture.Store.SettleWithRewards(
            M01, "custom", 1, true, 1, 1, M02,
            new[] { new CampaignMissionRewardGrant(MissionRewardKind.None, "reward.unknown", 1) }));
    }

    [Test]
    public void PendingResumeClearsOnlyAfterAcceptedVictory()
    {
        _fixture.Store.SetPendingResume(M01, true, 5);
        _fixture.Settle("defeat", 5, MissionRunKind.FirstClear, MissionLaunchOriginKind.FirstLaunch,
            MissionReturnDestinationKind.CommandBase, 0, 1, MissionOutcomeKind.Defeat);
        Assert.That(_fixture.Store.ReadAll()[0].pendingResume, Is.True);
        _fixture.Settle("victory", 5, MissionRunKind.FirstClear, MissionLaunchOriginKind.FirstLaunch,
            MissionReturnDestinationKind.CommandBase, 1, 2);
        Assert.That(_fixture.Store.ReadAll()[0].pendingResume, Is.False);
    }

    private static void WithFixture(Action<Fixture> action)
    {
        string root = Path.Combine(Path.GetTempPath(), "M01Settlement", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { using Fixture fixture = new(root); action(fixture); }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static CampaignMissionRuntimeComponent RuntimeAtSecureCorridor(MissionLaunchOriginKind origin) => new()
    {
        MissionId = new FixedString64Bytes(M01),
        ScenarioId = new FixedString64Bytes("scenario.ch01.m01.first_contact"),
        OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01"),
        SessionToken = new FixedString64Bytes("retry-route"),
        Phase = MissionPhaseKind.SecureCorridor,
        LaunchOrigin = origin,
        RunKind = MissionRunKind.Retry,
        Version = 3,
        SourceVersion = 2,
        AttemptOrdinal = 2,
        DeterministicSeed = 7001
    };

    private sealed class Fixture : IDisposable
    {
        private readonly BlobAssetReference<CampaignMissionCatalogBlob> _blob;
        public Fixture(string root)
        {
            RootPath = root;
            Service = new SaveService(new JsonSaveRepository(root));
            Store = new CampaignMissionProgressStore(Service);
            _blob = CreateBlob();
        }

        public string RootPath { get; }
        public SaveService Service { get; }
        public CampaignMissionProgressStore Store { get; }
        public CampaignMissionRewardGrant[] FirstRewards => new[]
        {
            new CampaignMissionRewardGrant(MissionRewardKind.None, "reward.commander_xp", 260),
            new CampaignMissionRewardGrant(MissionRewardKind.Credits, string.Empty, 1200)
        };
        public CampaignMissionRewardGrant[] ReplayRewards => new[]
        {
            new CampaignMissionRewardGrant(MissionRewardKind.Credits, string.Empty, 250)
        };

        public CampaignMissionSettlementResultElement Settle(
            string session, int attempt, MissionRunKind runKind, MissionLaunchOriginKind origin,
            MissionReturnDestinationKind destination, byte stars, int milliseconds,
            MissionOutcomeKind outcome = MissionOutcomeKind.Victory)
        {
            CampaignMissionSettlementRequestElement request = new()
            {
                SourceVersion = 4, MissionId = new FixedString64Bytes(M01),
                SessionToken = new FixedString64Bytes(session), AttemptOrdinal = attempt, Outcome = outcome
            };
            CampaignMissionRuntimeComponent runtime = new()
            {
                MissionId = request.MissionId, SessionToken = request.SessionToken, AttemptOrdinal = attempt,
                SourceVersion = 3, Version = 4, RunKind = runKind, LaunchOrigin = origin
            };
            CampaignMissionResultComponent result = new()
            {
                MissionId = request.MissionId, SessionToken = request.SessionToken, AttemptOrdinal = attempt,
                SourceVersion = 4, Outcome = outcome, ReturnDestination = destination,
                Stars = stars, ElapsedMilliseconds = milliseconds
            };
            ref CampaignMissionDefinitionBlob definition = ref _blob.Value.Missions[0];
            return CampaignMissionProgressSettlementSystem.Settle(
                Store, in request, in runtime, in result, ref definition);
        }

        public void Dispose() { if (_blob.IsCreated) _blob.Dispose(); }

        private static BlobAssetReference<CampaignMissionCatalogBlob> CreateBlob()
        {
            BlobBuilder builder = new(Allocator.Temp);
            ref CampaignMissionCatalogBlob catalog = ref builder.ConstructRoot<CampaignMissionCatalogBlob>();
            BlobBuilderArray<CampaignMissionDefinitionBlob> missions = builder.Allocate(ref catalog.Missions, 1);
            ref CampaignMissionDefinitionBlob mission = ref missions[0];
            mission.MissionId = new FixedString64Bytes(M01);
            BlobBuilderArray<CampaignMissionRewardBlob> first = builder.Allocate(ref mission.FirstClearRewards, 2);
            first[0] = new CampaignMissionRewardBlob
            {
                Kind = MissionRewardKind.None,
                RewardConfigId = new FixedString64Bytes("reward.commander_xp"), Amount = 260
            };
            first[1] = new CampaignMissionRewardBlob { Kind = MissionRewardKind.Credits, Amount = 1200 };
            BlobBuilderArray<CampaignMissionRewardBlob> replay = builder.Allocate(ref mission.ReplayRewards, 1);
            replay[0] = new CampaignMissionRewardBlob { Kind = MissionRewardKind.Credits, Amount = 250 };
            BlobAssetReference<CampaignMissionCatalogBlob> blob =
                builder.CreateBlobAssetReference<CampaignMissionCatalogBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }
}
