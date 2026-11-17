using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    // Gate 3 "available" model (lead-s002-gate3): role.convoy_logic was listed for S002 only because it
    // shares its runtime prefab with role.logistics_truck; army profile G does not allow it, so the
    // production catalog has no convoy recipe. Support trucks haul on their own and are reported apart.
    public sealed class SkirmishGateUnitsModelTests
    {
        [Test]
        public void SharedTruckPrefabDoesNotListConvoyLogic()
        {
            Assert.AreEqual(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.LogisticsTruck),
                SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.ConvoyLogic),
                "the collision this model guards against");
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateCatalogListsRole(SkirmishRoleKind.ConvoyLogic, SkirmishRoleKind.LogisticsTruck));
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateCatalogListsRole(SkirmishRoleKind.LogisticsTruck, SkirmishRoleKind.LogisticsTruck));
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateCatalogListsRole(SkirmishRoleKind.Marksman, SkirmishRoleKind.Marksman));
        }

        [Test]
        public void OnlyLogisticsSupportIsReportedAsAutomatic()
        {
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.LogisticsTruck));
            Assert.IsTrue(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.Tanker));
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.Radar), "radar must be ordered by the player");
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.ConvoyLogic));
            Assert.IsFalse(SkirmishS002AriaRunHarness.GateSupportRole(SkirmishRoleKind.Marksman));
        }
    }
}
