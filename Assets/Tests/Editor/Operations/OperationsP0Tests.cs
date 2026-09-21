#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsP0Tests
    {
        [Test] public void IdentityAcceptsCanonicalValues() => OperationsP0Checks.IdentityAcceptsCanonicalValues();
        [Test] public void IdentityRejectsLegacyAndMalformedValues() => OperationsP0Checks.IdentityRejectsLegacyAndMalformedValues();
        [Test] public void LaunchAndCommandContractsFailClosed() => OperationsP0Checks.LaunchAndCommandContractsFailClosed();
        [Test] public void CatalogSchemaMatchesSixtyMissions() => OperationsP0Checks.CatalogSchemaMatchesSixtyMissions();
        [Test] public void ObjectiveGraphRejectsCycles() => OperationsP0Checks.ObjectiveGraphRejectsCycles();
        [Test] public void ForceAndActionSchemasMatchStrategicRules() => OperationsP0Checks.ForceAndActionSchemasMatchStrategicRules();
        [Test] public void MigrationFixturesCoverEmptyUnknownAndCurrent() => OperationsP0Checks.MigrationFixturesCoverEmptyUnknownAndCurrent();
        [Test] public void RosterLedgerDoesNotInventExistingTypes() => OperationsP0Checks.RosterLedgerDoesNotInventExistingTypes();
        [Test] public void DashboardOrdinalsMatchVerifiedUiEnum() => OperationsP0Checks.DashboardOrdinalsMatchVerifiedUiEnum();
        [Test] public void AssemblyManifestForbidsSharedEdits() => OperationsP0Checks.AssemblyManifestForbidsSharedEdits();
        [Test] public void SharedIdentityStillRejectsOperationsNamespace() => OperationsP0Checks.SharedIdentityStillRejectsOperationsNamespace();
        [Test] public void HostFilesStayInsideOperationsOwnership() => OperationsP0Checks.HostFilesStayInsideOperationsOwnership();
    }
}
#endif
