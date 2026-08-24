using System;
using System.IO;
using System.Text;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class M01FirstContactProgressStoreTests
{
    private const string PassMarker = "[M01FirstContactProgressStoreValidation] result=Passed tests=15";
    private const string M01 = "saga.ch01.m01.first_contact";
    private const string M02 = "saga.ch01.m02.next";

    public static void RunFocusedValidation()
    {
        try
        {
            M01FirstContactProgressStoreTests tests = new();
            tests.NewProfileHasNoInventedProgress();
            tests.OlderProfileMigratesAdditively();
            tests.EntriesNormalizeAndSortDeterministically();
            tests.EnsureAvailableIsIdempotent();
            tests.FirstClearSettlementPersists();
            tests.DuplicateSettlementTokenIsIgnored();
            tests.ReplaySettlementCountsOnce();
            tests.BestMetricsOnlyImprove();
            tests.NextMissionRevealIsPersisted();
            tests.PendingResumeRoundTrips();
            tests.RestartPreservesProgressSettingsAndQuickGame();
            tests.FutureEntrySchemaDoesNotInventProgress();
            tests.FutureProfileSchemaDoesNotInventProgress();
            tests.CorruptProfileDoesNotInventProgress();
            tests.InterruptedAtomicReplacePreservesPriorProfile();
            Debug.Log(PassMarker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M01FirstContactProgressStoreValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test] public void NewProfileHasNoInventedProgress() => WithContext(context =>
        Assert.AreEqual(0, context.Store.ReadAll().Length));

    [Test] public void OlderProfileMigratesAdditively() => WithContext(context =>
    {
        File.WriteAllText(context.ProfilePath, "{\"profileSchemaVersion\":2,\"commanderName\":\"Legacy\"}");
        PlayerProfileSaveData profile = context.Service.LoadProfile();
        Assert.AreEqual("Legacy", profile.commanderName);
        Assert.AreEqual(0, profile.campaignMissionProgress.Length);
    });

    [Test] public void EntriesNormalizeAndSortDeterministically() => WithContext(context =>
    {
        context.Service.SaveProfile(new PlayerProfileSaveData { campaignMissionProgress = new[]
        {
            Entry(M02, true), Entry(M01, true), Entry(M01, false), null
        }});
        CampaignMissionProgressSaveData[] entries = context.Store.ReadAll();
        Assert.AreEqual(2, entries.Length);
        Assert.AreEqual(M01, entries[0].missionId);
        Assert.AreEqual(M02, entries[1].missionId);
    });

    [Test] public void EnsureAvailableIsIdempotent() => WithContext(context =>
    {
        Assert.IsTrue(context.Store.EnsureAvailable(M01));
        Assert.IsFalse(context.Store.EnsureAvailable(M01));
        Assert.IsTrue(context.Store.ReadAll()[0].available);
    });

    [Test] public void FirstClearSettlementPersists() => WithContext(context =>
    {
        Assert.IsTrue(context.Store.Settle(M01, "session-a", 1, true, 2, 180000, null));
        CampaignMissionProgressSaveData entry = context.Store.ReadAll()[0];
        Assert.IsTrue(entry.firstClearCompleted);
        Assert.IsTrue(entry.firstClearRewardSettled);
        Assert.AreEqual(2, entry.bestStars);
    });

    [Test] public void DuplicateSettlementTokenIsIgnored() => WithContext(context =>
    {
        Assert.IsTrue(context.Store.Settle(M01, "session-a", 1, true, 2, 180000, null));
        Assert.IsFalse(context.Store.Settle(M01, "session-a", 1, false, 3, 100000, null));
        Assert.AreEqual(0, context.Store.ReadAll()[0].successfulReplayCount);
    });

    [Test] public void ReplaySettlementCountsOnce() => WithContext(context =>
    {
        Assert.IsTrue(context.Store.Settle(M01, "replay", 2, false, 1, 230000, null));
        Assert.AreEqual(1, context.Store.ReadAll()[0].successfulReplayCount);
    });

    [Test] public void BestMetricsOnlyImprove() => WithContext(context =>
    {
        context.Store.Settle(M01, "first", 1, true, 2, 180000, null);
        context.Store.Settle(M01, "replay", 2, false, 1, 220000, null);
        CampaignMissionProgressSaveData entry = context.Store.ReadAll()[0];
        Assert.AreEqual(2, entry.bestStars);
        Assert.AreEqual(180000, entry.bestCompletionMilliseconds);
    });

    [Test] public void NextMissionRevealIsPersisted() => WithContext(context =>
    {
        context.Store.Settle(M01, "first", 1, true, 3, 100000, M02);
        CampaignMissionProgressSaveData[] entries = context.Store.ReadAll();
        Assert.AreEqual(M02, entries[1].missionId);
        Assert.IsTrue(entries[1].available);
    });

    [Test] public void PendingResumeRoundTrips() => WithContext(context =>
    {
        Assert.IsTrue(context.Store.SetPendingResume(M01, true, 3));
        CampaignMissionProgressSaveData entry = context.Store.ReadAll()[0];
        Assert.IsTrue(entry.pendingResume);
        Assert.AreEqual(3, entry.lastAttemptOrdinal);
    });

    [Test] public void RestartPreservesProgressSettingsAndQuickGame() => WithContext(context =>
    {
        context.Service.SaveSettings(new SettingsSaveData { masterVolume = 42f });
        context.Service.SaveQuickGame(new QuickGameSaveData { enemyCount = 7 });
        context.Store.Settle(M01, "first", 1, true, 3, 100000, null);
        SaveService restarted = new(new JsonSaveRepository(context.Root));
        Assert.AreEqual(3, new CampaignMissionProgressStore(restarted).ReadAll()[0].bestStars);
        Assert.AreEqual(42f, restarted.LoadSettings().masterVolume);
        Assert.AreEqual(7, restarted.LoadQuickGame().enemyCount);
    });

    [Test] public void FutureEntrySchemaDoesNotInventProgress() => WithContext(context =>
    {
        CampaignMissionProgressSaveData entry = Entry(M01, true);
        entry.schemaVersion = CampaignMissionProgressStore.CurrentEntrySchemaVersion + 1;
        context.Service.SaveProfile(new PlayerProfileSaveData { campaignMissionProgress = new[] { entry } });
        Assert.AreEqual(0, context.Store.ReadAll().Length);
    });

    [Test] public void FutureProfileSchemaDoesNotInventProgress() => WithContext(context =>
    {
        File.WriteAllText(context.ProfilePath,
            "{\"profileSchemaVersion\":999,\"campaignMissionProgress\":[{\"missionId\":\"" + M01 + "\",\"available\":true}]}");
        Assert.AreEqual(0, context.Store.ReadAll().Length);
    });

    [Test] public void CorruptProfileDoesNotInventProgress() => WithContext(context =>
    {
        File.WriteAllText(context.ProfilePath, "{not-json");
        Assert.AreEqual(0, context.Store.ReadAll().Length);
    });

    [Test] public void InterruptedAtomicReplacePreservesPriorProfile() => WithContext(context =>
    {
        context.Service.SaveProfile(new PlayerProfileSaveData { commanderName = "Prior" });
        string backupPath = context.ProfilePath + ".bak";
        Directory.CreateDirectory(backupPath);
        Exception failure = Assert.Catch(() =>
            context.Service.SaveProfile(new PlayerProfileSaveData { commanderName = "Rejected" }));
        Assert.That(failure, Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        Directory.Delete(backupPath, true);
        Assert.AreEqual("Prior", context.Service.LoadProfile().commanderName);
        Assert.IsFalse(File.Exists(context.ProfilePath + ".tmp"));
    });

    private static CampaignMissionProgressSaveData Entry(string missionId, bool available) => new()
    {
        missionId = missionId,
        available = available
    };

    private static void WithContext(Action<Context> action)
    {
        string root = Path.Combine(Path.GetTempPath(), "M01Progress", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { action(new Context(root)); }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private sealed class Context
    {
        public Context(string root)
        {
            Root = root;
            Service = new SaveService(new JsonSaveRepository(root));
            Store = new CampaignMissionProgressStore(Service);
        }
        public string Root { get; }
        public SaveService Service { get; }
        public CampaignMissionProgressStore Store { get; }
        public string ProfilePath => Path.Combine(Root, SaveService.ProfileFileName);
    }
}
