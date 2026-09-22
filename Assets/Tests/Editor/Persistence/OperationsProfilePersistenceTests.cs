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
        // JsonUtility may materialize a null nested run as an empty run object.
        // A normal first-launch profile must still be eligible for new-run creation.
        service.SaveProfile(service.LoadProfile());
        var commands = new OperationsProfileCommandService(service);
        Assert.That(OperationsSaveMigration.HasActiveRun(commands.Read()), Is.False);
        var create = new OperationsCommand("cmd.operations.aabb0001", 0,
            OperationsCommandKind.NewRun, "", "", "");
        Assert.That(commands.TryNewRun(create, 1102, OperationsDifficultyKind.Regular,
            out var created, out var error), Is.True, error);
        Assert.That(created.Accepted, Is.True, created.ReasonCode.ToString());
        var before = commands.Read();
        Assert.That(OperationsSaveMigration.HasActiveRun(before), Is.True);
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
    public void FailedLaunch_RefundsExactlyOnceAcrossServiceRestart_AndAllowsRedeployment()
    {
        var commands = new OperationsProfileCommandService(service);
        Assert.That(commands.TryNewRun(new OperationsCommand("cmd.operations.fail0001", 0,
            OperationsCommandKind.NewRun, "", "", ""), 1102, OperationsDifficultyKind.Regular,
            out var created, out var error), Is.True, error);
        Assert.That(created.Accepted, Is.True);
        var before = commands.Read();
        var offer = Array.Find(before.activeRun.offers, item => item.missionId == "operation.o001");
        Assert.That(commands.TrySubmit(new OperationsCommand("cmd.operations.fail0002", before.profileRevision,
            OperationsCommandKind.Deploy, offer.districtId, offer.offerId, ""), out var deployed, out error), Is.True, error);
        Assert.That(deployed.Accepted, Is.True);
        var reserved = commands.Read();
        Assert.That(reserved.activeRun.actionPoints, Is.EqualTo(before.activeRun.actionPoints - 1));
        var refund = new OperationsCommand("cmd.operations.fail0003", reserved.profileRevision,
            OperationsCommandKind.TechnicalFailure, "", "", "");
        Assert.That(commands.TrySubmit(refund, out var refunded, out error), Is.True, error);
        Assert.That(refunded.Accepted, Is.True, refunded.ReasonCode.ToString());
        commands = new OperationsProfileCommandService(new SaveService(new JsonSaveRepository(root)));
        var restored = commands.Read();
        Assert.That(restored.activeRun.actionPoints, Is.EqualTo(before.activeRun.actionPoints));
        Assert.That(restored.pendingDeployment.reserved, Is.False);
        string bytes = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(commands.TrySubmit(refund, out var retry, out error), Is.True, error);
        Assert.That(retry, Is.EqualTo(refunded));
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes));
        Assert.That(commands.TrySubmit(new OperationsCommand("cmd.operations.fail0004", restored.profileRevision,
            OperationsCommandKind.Deploy, offer.districtId, offer.offerId, ""), out var redeployed, out error), Is.True, error);
        Assert.That(redeployed.Accepted, Is.True, redeployed.ReasonCode.ToString());
        Assert.That(commands.Read().activeRun.actionPoints, Is.EqualTo(before.activeRun.actionPoints - 1));
    }

    [Test]
    public void CheckpointPublicationPreservesStrategyAndRetainsPreviousImage()
    {
        var commands = new OperationsProfileCommandService(service);
        Assert.That(commands.TryNewRun(new OperationsCommand("cmd.operations.snap0001", 0,
            OperationsCommandKind.NewRun, "", "", ""), 1102, OperationsDifficultyKind.Regular, out _, out _), Is.True);
        var run = commands.Read();
        var offer = Array.Find(run.activeRun.offers, item => item.missionId == "operation.o001");
        Assert.That(commands.TrySubmit(new OperationsCommand("cmd.operations.snap0002", run.profileRevision,
            OperationsCommandKind.Deploy, offer.districtId, offer.offerId, ""), out var deployed, out _), Is.True);
        Assert.That(deployed.Accepted, Is.True);
        var reserved = commands.Read();
        string session = reserved.pendingDeployment.sessionId;
        Assert.That(service.TrySaveOperationsCheckpoint(session, "first-image", out _), Is.True);
        Assert.That(service.TrySaveOperationsCheckpoint(session, "second-image", out _), Is.True);
        var restarted = new SaveService(new JsonSaveRepository(root));
        var archive = restarted.LoadOperationsCheckpoint(session);
        Assert.That(archive.current, Is.EqualTo("second-image"));
        Assert.That(archive.previous, Is.EqualTo("first-image"));
        Assert.That(commands.Read().profileRevision, Is.EqualTo(reserved.profileRevision));
        Assert.That(commands.Read().activeRun.actionPoints, Is.EqualTo(reserved.activeRun.actionPoints));
        string bytes = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(service.TrySaveOperationsCheckpoint(session, "second-image", out _), Is.True);
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes));
        Assert.That(service.TrySaveOperationsCheckpoint("wrong-attempt", "bad-image", out _), Is.False);
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes));

        var futureProfile = service.LoadProfile();
        futureProfile.operationsAttemptJson = UnityEngine.JsonUtility.ToJson(new SaveService.OperationsCheckpointArchive
        { schema = 99, sessionId = session, current = "future-image", previous = "retained-image" });
        repository.SaveAtomic(SaveService.ProfileFileName, futureProfile);
        bytes = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(service.TrySaveOperationsCheckpoint(session, "replacement-image", out string reason), Is.False);
        Assert.That(reason, Is.EqualTo("checkpoint_schema_incompatible"));
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes), "Do not overwrite recovery evidence from a newer writer.");
    }

    [Test]
    public void ExplicitRestartPreservesReservationAndCountsOnce_RejectsChangedContentAndSavedProgress()
    {
        var commands = new OperationsProfileCommandService(service);
        Assert.That(commands.TryNewRun(new OperationsCommand("cmd.operations.restart0001", 0,
            OperationsCommandKind.NewRun, "", "", ""), 1102, OperationsDifficultyKind.Regular, out _, out _), Is.True);
        var run = commands.Read();
        var offer = Array.Find(run.activeRun.offers, item => item.missionId == "operation.o001");
        Assert.That(commands.TrySubmit(new OperationsCommand("cmd.operations.restart0002", run.profileRevision,
            OperationsCommandKind.Deploy, offer.districtId, offer.offerId, ""), out var deployed, out _), Is.True);
        Assert.That(deployed.Accepted, Is.True);
        var reserved = commands.Read();
        string session = reserved.pendingDeployment.sessionId;
        Assert.That(service.TryBeginOperationsAttempt(session, "content-a", null, out _), Is.True);
        Assert.That(service.LoadOperationsCheckpoint(session).restartCount, Is.Zero);
        service = new SaveService(new JsonSaveRepository(root));
        Assert.That(service.TryBeginOperationsAttempt(session, "content-a", "restart-1", out _), Is.True);
        string bytes = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(service.TryBeginOperationsAttempt(session, "content-a", "restart-1", out _), Is.True);
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes));
        Assert.That(service.LoadOperationsCheckpoint(session).restartCount, Is.EqualTo(1));
        Assert.That(commands.Read().activeRun.actionPoints, Is.EqualTo(reserved.activeRun.actionPoints));
        Assert.That(commands.Read().pendingDeployment.sessionId, Is.EqualTo(session));
        Assert.That(commands.Read().profileRevision, Is.EqualTo(reserved.profileRevision));
        Assert.That(service.TryBeginOperationsAttempt(session, "changed-content", "restart-2", out _), Is.False);
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes));
        Assert.That(service.TrySaveOperationsCheckpoint(session, "tactical-progress", out _), Is.True);
        Assert.That(service.LoadOperationsCheckpoint(session).restartCount, Is.EqualTo(1));
        bytes = repository.ReadRaw(SaveService.ProfileFileName);
        Assert.That(service.TryBeginOperationsAttempt(session, "content-a", "restart-2", out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("checkpoint_requires_resume"));
        Assert.That(repository.ReadRaw(SaveService.ProfileFileName), Is.EqualTo(bytes));
        Assert.That(commands.TrySubmit(new OperationsCommand("cmd.operations.restart0003", reserved.profileRevision,
            OperationsCommandKind.Withdraw, "", "", ""), out var withdrawn, out _), Is.True);
        Assert.That(withdrawn.Accepted, Is.True);
        Assert.That(commands.Read().pendingDeployment.reserved, Is.False);
        Assert.That(commands.Read().activeRun.actionPoints, Is.EqualTo(reserved.activeRun.actionPoints));
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
