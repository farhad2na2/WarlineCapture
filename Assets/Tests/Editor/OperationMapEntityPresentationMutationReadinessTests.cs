using Game.Editor;
using NUnit.Framework;

public sealed class OperationMapEntityPresentationMutationReadinessTests
{
    [Test]
    public void TryEvaluateMutationReadiness_AcceptsGreenPhase0AEvidence()
    {
        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryEvaluateMutationReadiness(
                gameplayBuildingCount: 432,
                gameplayVehicleCount: 22,
                renderOnlyEntityCount: 9090,
                rejectedUnresolvedCount: 0,
                vehicleAlreadyReadyCount: 22,
                vehicleCleanupRequiredCount: 0,
                buildingAttachmentOrphanCount: 0,
                buildingAttachmentSharedCount: 0,
                buildingAttachmentDualStateCount: 0,
                out OperationMapEntityPresentationMutationReadiness readiness,
                out string rejectionReason),
            Is.True,
            rejectionReason);
        Assert.That(
            readiness,
            Is.EqualTo(
                OperationMapEntityPresentationMutationReadiness
                    .CandidateTransactionReadyPendingMutation));
        Assert.That(rejectionReason, Is.Null);
    }

    [Test]
    public void TryEvaluateMutationReadiness_FailsClosedOnRejectedOwnersOrVehicleCleanup()
    {
        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryEvaluateMutationReadiness(
                432, 22, 9090, rejectedUnresolvedCount: 1, 22, 0, 0, 0, 0,
                out OperationMapEntityPresentationMutationReadiness readiness,
                out string rejectionReason),
            Is.False);
        Assert.That(readiness, Is.EqualTo(OperationMapEntityPresentationMutationReadiness.NotReady));
        Assert.That(rejectionReason, Does.Contain("rejected-unresolved"));

        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryEvaluateMutationReadiness(
                432, 22, 9090, 0, 22, vehicleCleanupRequiredCount: 1, 0, 0, 0,
                out readiness,
                out rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.Contain("vehicle-ecs-cleanup"));
    }

    [Test]
    public void TryEvaluateMutationReadiness_FailsClosedOnAttachmentConflictsOrCountMismatch()
    {
        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryEvaluateMutationReadiness(
                432, 22, 9090, 0, 22, 0, buildingAttachmentOrphanCount: 3, 0, 0,
                out _,
                out string rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.Contain("attachment-orphans"));

        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryEvaluateMutationReadiness(
                432, 22, 9090, 0, vehicleAlreadyReadyCount: 21, 0, 0, 0, 0,
                out _,
                out rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.Contain("vehicle-ecs-ready-count-mismatch"));
    }
}
