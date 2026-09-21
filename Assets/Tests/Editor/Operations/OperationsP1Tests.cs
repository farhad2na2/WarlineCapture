#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsP1Tests
    {
        [Test] public void DeterministicDayTraceMatchesRules() => OperationsP1Checks.DeterministicDayTraceMatchesRules();
        [Test] public void SignedClampAndOutcomeDeltas() => OperationsP1Checks.SignedClampAndOutcomeDeltas();
        [Test] public void SixActionsRespectLimitsAndAp() => OperationsP1Checks.SixActionsRespectLimitsAndAp();
        [Test] public void FloorRecoveryRemainsPossible() => OperationsP1Checks.FloorRecoveryRemainsPossible();
        [Test] public void OfferDirectorStableAndSlotGated() => OperationsP1Checks.OfferDirectorStableAndSlotGated();
        [Test] public void IncidentsCapCooldownAndExpiry() => OperationsP1Checks.IncidentsCapCooldownAndExpiry();
        [Test] public void AdjacencyPressureIsSimultaneous() => OperationsP1Checks.AdjacencyPressureIsSimultaneous();
        [Test] public void DuplicateCommandDoesNotDoubleSpend() => OperationsP1Checks.DuplicateCommandDoesNotDoubleSpend();
        [Test] public void ConflictingSettlementIsRejected() => OperationsP1Checks.ConflictingSettlementIsRejected();
        [Test] public void CrashPreservesPriorRevision() => OperationsP1Checks.CrashPreservesPriorRevision();
        [Test] public void StaleWriterAndUnknownSchemaRejected() => OperationsP1Checks.StaleWriterAndUnknownSchemaRejected();
        [Test] public void TechnicalFailureRefundsOnceAndReservedResumes() => OperationsP1Checks.TechnicalFailureRefundsOnceAndReservedResumes();
        [Test] public void RewardsStayInsideOperationsEnvelope() => OperationsP1Checks.RewardsStayInsideOperationsEnvelope();
        [Test] public void MigrationRoundTripPreservesCity() => OperationsP1Checks.MigrationRoundTripPreservesCity();
        [Test] public void CityWinWithoutPurchases() => OperationsP1Checks.CityWinWithoutPurchases();
        [Test] public void WithdrawAppliesConsequencesWithoutRefund() => OperationsP1Checks.WithdrawAppliesConsequencesWithoutRefund();
    }
}
#endif
