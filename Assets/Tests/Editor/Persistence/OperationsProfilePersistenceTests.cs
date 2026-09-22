using System;
using System.IO;
using Game.Operations.Contracts;
using Game.Runtime;
using NUnit.Framework;

public sealed class OperationsProfilePersistenceTests
{
    private string root;
    private JsonSaveRepository repository;
    private SaveService service;

    [SetUp]
    public void SetUp()
    {
        root = Path.Combine(Path.GetTempPath(), "OperationsProfilePersistenceTests", Guid.NewGuid().ToString("N"));
        repository = new JsonSaveRepository(root);
        service = new SaveService(repository);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [Test]
    public void Commit_RoundTripsNestedStateAndRewardsTogether_PreservingCampaignAndQuickGame()
    {
        var profile = new PlayerProfileSaveData
        {
            credits = 700, commanderXp = 300, commanderName = "Existing commander",
            campaignMissionProgress = new[] { new CampaignMissionProgressSaveData
                { missionId = "saga.ch01.m01.first_contact", bestStars = 3, firstClearCompleted = true } }
        };
        service.SaveProfile(profile);
        service.SaveQuickGame(new QuickGameSaveData { presetId = "existing-skirmish" });
        string quickBefore = repository.ReadRaw(SaveService.QuickGameFileName);
        OperationsSaveData operations = Transaction();
        Assert.That(service.TryCommitOperations(profile.profileCommitRevision, 0, operations, "active-attempt",
            out PlayerProfileSaveData saved, out string reason), Is.True, reason);

        PlayerProfileSaveData loaded = new SaveService(new JsonSaveRepository(root)).LoadProfile();
        Assert.That(loaded.profileCommitRevision, Is.EqualTo(saved.profileCommitRevision));
        Assert.That(loaded.credits, Is.EqualTo(820));
        Assert.That(loaded.commanderXp, Is.EqualTo(350));
        Assert.That(loaded.operations.activeRun.districts[0].trust, Is.EqualTo(46));
        Assert.That(loaded.operations.receipts[0].transactionId, Is.EqualTo("txn.o001.1"));
        Assert.That(loaded.operationsAttemptJson, Is.EqualTo("active-attempt"));
        Assert.That(loaded.campaignMissionProgress[0].bestStars, Is.EqualTo(3));
        Assert.That(repository.ReadRaw(SaveService.QuickGameFileName), Is.EqualTo(quickBefore));
    }

    [Test]
    public void IdenticalRetry_DoesNotGrantAgainOrOverwriteNewerCampaignProgress()
    {
        OperationsSaveData transaction = Transaction();
        Assert.That(service.TryCommitOperations(0, 0, transaction, "result", out _, out _), Is.True);
        var campaign = new SaveService(new JsonSaveRepository(root));
        PlayerProfileSaveData updated = campaign.LoadProfile();
        updated.starsEarned = 3;
        campaign.SaveProfile(updated);
        string before = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(service.TryCommitOperations(0, 0, transaction, "result", out var saved, out _), Is.True);
        Assert.That(saved.credits, Is.EqualTo(120));
        Assert.That(saved.starsEarned, Is.EqualTo(3));
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(before));
    }

    [Test]
    public void StaleCampaignWriter_CannotEraseOperationsCommit()
    {
        PlayerProfileSaveData stale = service.LoadProfile();
        Assert.That(service.TryCommitOperations(0, 0, Transaction(), "result", out _, out _), Is.True);
        string before = repository.ReadRaw(SaveService.ProfileFileName);
        stale.credits = 1;
        Assert.Throws<InvalidOperationException>(() => service.SaveProfile(stale));
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(before));
    }

    [Test]
    public void StaleOperationsWriter_CannotEraseCampaignCommit()
    {
        var profile = service.LoadProfile();
        profile.starsEarned = 3;
        service.SaveProfile(profile);
        Assert.That(service.TryCommitOperations(0, 0, Transaction(), "result", out _, out string reason), Is.False);
        Assert.That(reason, Is.EqualTo("stale_profile"));
        Assert.That(service.LoadProfile().starsEarned, Is.EqualTo(3));
        Assert.That(service.LoadProfile().operations.profileRevision, Is.Zero);
    }

    [Test]
    public void ConflictingRetry_IsRejectedWithoutChangingRewardsOrAttempt()
    {
        Assert.That(service.TryCommitOperations(0, 0, Transaction(), "result", out _, out _), Is.True);
        var conflict = Transaction();
        conflict.operationsRewardCredits++;
        string before = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(service.TryCommitOperations(0, 0, conflict, "result", out _, out _), Is.False);
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(before));
    }

    [Test]
    public void FutureOperationsSchema_IsReadOnlyAndPreservedByteForByte()
    {
        Directory.CreateDirectory(root);
        string raw = "{\"profileSchemaVersion\":3,\"operations\":{\"schemaVersion\":999,\"futureState\":42}}";
        File.WriteAllText(repository.GetPath(SaveService.ProfileFileName), raw);
        var loaded = service.LoadProfile();
        Assert.That(loaded.operations.schemaVersion, Is.EqualTo(999));
        Assert.Throws<InvalidOperationException>(() => service.SaveProfile(loaded));
        Assert.Throws<InvalidOperationException>(() => service.TryCommitOperations(0, 0, Transaction(), "", out _, out _));
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(raw));
    }

    [Test]
    public void MissingOperationsEnvelope_MigratesWithoutResettingExistingPlayer()
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(repository.GetPath(SaveService.ProfileFileName), "{\"commanderName\":\"Legacy\",\"credits\":123}");
        var profile = service.LoadProfile();
        Assert.That(profile.operations, Is.Not.Null);
        service.SaveProfile(profile);
        var loaded = service.LoadProfile();
        Assert.That(loaded.commanderName, Is.EqualTo("Legacy"));
        Assert.That(loaded.credits, Is.EqualTo(123));
        Assert.That(loaded.firstLaunchStatus, Is.EqualTo(FirstLaunchProfileState.Completed));
    }

    [Test]
    public void StrategicDeploy_PersistsReservationAndChargesOnceAcrossServiceRestart()
    {
        var commands = new OperationsProfileCommandService(service);
        var create = new OperationsCommand("cmd.operations.aabb0001", 0,
            OperationsCommandKind.NewRun, "", "", "");
        Assert.That(commands.TryNewRun(create, 1102, OperationsDifficultyKind.Regular,
            out var created, out var error), Is.True, error);
        Assert.That(created.Accepted, Is.True, created.ReasonCode.ToString());
        var before = commands.Read();
        var offer = Array.Find(before.activeRun.offers, item => item.missionId == "operation.o001");
        Assert.That(offer, Is.Not.Null);
        var deploy = new OperationsCommand("cmd.operations.aabb0002", before.profileRevision,
            OperationsCommandKind.Deploy, offer.districtId, offer.offerId, "");
        Assert.That(commands.TrySubmit(deploy, out var reserved, out error), Is.True, error);
        Assert.That(reserved.Accepted, Is.True, reserved.ReasonCode.ToString());
        var restarted = new OperationsProfileCommandService(new SaveService(new JsonSaveRepository(root)));
        var after = restarted.Read();
        Assert.That(after.activeRun.actionPoints, Is.LessThan(before.activeRun.actionPoints));
        Assert.That(after.pendingDeployment.reserved, Is.True);
        string bytes = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(restarted.TrySubmit(deploy, out var retried, out error), Is.True, error);
        Assert.That(retried, Is.EqualTo(reserved));
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes));
    }

    [Test]
    public void RewardLedgerCannotGoBackwards()
    {
        Assert.That(service.TryCommitOperations(0, 0, Transaction(), "result", out var saved, out _), Is.True);
        var next = Transaction();
        next.profileRevision = 2;
        next.operationsRewardCredits = 0;
        Assert.That(service.TryCommitOperations(saved.profileCommitRevision, 1, next, "", out _, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("invalid_reward_ledger"));
        Assert.That(service.LoadProfile().credits, Is.EqualTo(120));
    }

    private static OperationsSaveData Transaction() => new()
    {
        profileRevision = 1, operationsRewardCredits = 120, operationsRewardCommanderXp = 50,
        activeRun = new OperationsRunSaveData
        {
            runId = "run.operations.1", revision = 1,
            districts = new[] { new OperationsDistrictSaveData { districtId = "district.operations.d01", trust = 46 } }
        },
        receipts = new[] { new OperationsReceiptSaveData { transactionId = "txn.o001.1", rewardCredits = 120 } }
    };
}
