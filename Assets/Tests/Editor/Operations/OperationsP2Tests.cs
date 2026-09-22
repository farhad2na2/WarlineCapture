#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsP2Tests
    {
        [Test] public void TwoMapFixturesCompileAndDiffer() => OperationsP2Checks.TwoMapFixturesCompileAndDiffer();
        [Test] public void CompilerRejectsBadGraphs() => OperationsP2Checks.CompilerRejectsBadGraphs();
        [Test] public void CompilerRejectsBadAnchors() => OperationsP2Checks.CompilerRejectsBadAnchors();
        [Test] public void CompilerRejectsBadBudgetsAndWaves() => OperationsP2Checks.CompilerRejectsBadBudgetsAndWaves();
        [Test] public void SpawnOwnershipBindsSessionAndRole() => OperationsP2Checks.SpawnOwnershipBindsSessionAndRole();
        [Test] public void ScanRequiresRangeLosAndExclusiveSite() => OperationsP2Checks.ScanRequiresRangeLosAndExclusiveSite();
        [Test] public void HoldResetsWhenContestedOrEmpty() => OperationsP2Checks.HoldResetsWhenContestedOrEmpty();
        [Test] public void InteractResetsAndCarriesEvidence() => OperationsP2Checks.InteractResetsAndCarriesEvidence();
        [Test] public void RepairSpendsOncePausesAndRetriesRestored() => OperationsP2Checks.RepairSpendsOncePausesAndRetriesRestored();
        [Test] public void EscortRequiresGoRouteAndUnload() => OperationsP2Checks.EscortRequiresGoRouteAndUnload();
        [Test] public void ExtractRequiresTwoOriginalInfantry() => OperationsP2Checks.ExtractRequiresTwoOriginalInfantry();
        [Test] public void WaveScheduleIsFiniteAndWarned() => OperationsP2Checks.WaveScheduleIsFiniteAndWarned();
        [Test] public void OutcomePrecedenceDeathsBeforeVictory() => OperationsP2Checks.OutcomePrecedenceDeathsBeforeVictory();
        [Test] public void ConcludeWithdrawDeadlineAndPause() => OperationsP2Checks.ConcludeWithdrawDeadlineAndPause();
        [Test] public void RulesIgnoreMissionId() => OperationsP2Checks.RulesIgnoreMissionId();
        [Test] public void OptionalNodesDoNotGateVictory() => OperationsP2Checks.OptionalNodesDoNotGateVictory();
    }
}
#endif
