#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsP3Tests
    {
        [Test] public void DashboardDeployRealMissionReturnsCorrectDistrict() => OperationsP3Checks.DashboardDeployRealMissionReturnsCorrectDistrict();
        [Test] public void SecondDistrictResultStaysOnLaunchedDistrict() => OperationsP3Checks.SecondDistrictResultStaysOnLaunchedDistrict();
        [Test] public void ModeLaunchIsExclusive() => OperationsP3Checks.ModeLaunchIsExclusive();
        [Test] public void ReserveRefundAndDuplicateDeploy() => OperationsP3Checks.ReserveRefundAndDuplicateDeploy();
        [Test] public void AppInterruptionAtEveryCommitBoundary() => OperationsP3Checks.AppInterruptionAtEveryCommitBoundary();
        [Test] public void CheckpointRestoresProgressWithoutDuplicateCargo() => OperationsP3Checks.CheckpointRestoresProgressWithoutDuplicateCargo();
        [Test] public void CorruptCheckpointRefundsWithoutTimerReset() => OperationsP3Checks.CorruptCheckpointRefundsWithoutTimerReset();
        [Test] public void MissingCheckpointOffersRestartOrWithdraw() => OperationsP3Checks.MissingCheckpointOffersRestartOrWithdraw();
        [Test] public void DuplicateConflictAndStaleSession() => OperationsP3Checks.DuplicateConflictAndStaleSession();
        [Test] public void PracticeGrantsNothingAndWithdrawDoesNotRefund() => OperationsP3Checks.PracticeGrantsNothingAndWithdrawDoesNotRefund();
        [Test] public void RootHeaderHistoryAndReportDoNotSimulate() => OperationsP3Checks.RootHeaderHistoryAndReportDoNotSimulate();
        [Test] public void FrozenResultRejectsWithdrawRewrite() => OperationsP3Checks.FrozenResultRejectsWithdrawRewrite();
    }
}
#endif
