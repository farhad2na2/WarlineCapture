using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishSharedMaterialsTests
    {
        [Test]
        public void ConstructionAndExpandedOrdersShareOneBankWithoutRepeatedGrants()
        {
            using var world = new World(nameof(ConstructionAndExpandedOrdersShareOneBankWithoutRepeatedGrants));
            var em = world.EntityManager;
            Entity session = em.CreateEntity();
            var setup = new SkirmishResolvedSetup { MaterialsEach = 450, MaterialsCapacityEach = 800 };
            em.AddComponentData(session, new SkirmishEconomyStockComponent { Materials = 9999 });
            SkirmishMaterialsService.Initialize(em, session, setup);
            Entity bank = SkirmishMaterialsService.FindEconomy(em, 1);
            var economy = em.GetComponentData<FactionEconomy>(bank);
            var materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            Assert.AreEqual(FactionConstructionResourceMutationResult.Applied,
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 0, 40));
            em.SetComponentData(bank, economy);
            em.SetComponentData(bank, materials);
            Assert.AreEqual(410, SkirmishMaterialsService.Read(em, session, 1));
            SkirmishMaterialsService.Initialize(em, session, setup);
            Assert.AreEqual(410, SkirmishMaterialsService.Read(em, session, 1));
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 330, FactionTacticalMaterialsSpendKind.Production);
            Assert.AreEqual(330, em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current);
            Assert.AreEqual(450, SkirmishMaterialsService.Read(em, session, 2));
            Assert.AreEqual(9999, em.GetComponentData<SkirmishEconomyStockComponent>(session).Materials,
                "The compatibility stock must not be mirrored or used as a second bank.");
            materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            Assert.AreEqual(FactionConstructionResourceMutationResult.InsufficientMaterials,
                FactionConstructionResourceUtilitySystemHelper.TrySpend(ref economy, ref materials, 0, 400));
            Assert.AreEqual(330, materials.Current);
            Assert.AreEqual(120, materials.LifetimeSpent);
            Assert.AreEqual(80, materials.LifetimeProductionSpent);
            Assert.AreEqual(40, materials.LifetimeConstructionSpent);
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 90, FactionTacticalMaterialsSpendKind.Upgrade);
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 150, FactionTacticalMaterialsSpendKind.Production);
            materials = em.GetComponentData<FactionTacticalMaterialsComponent>(bank);
            Assert.AreEqual(300, materials.LifetimeSpent, "Construction, production and research share net spending.");
            Assert.AreEqual(20, materials.LifetimeProductionSpent, "The production refund only changes its category.");
            Assert.AreEqual(240, materials.LifetimeUpgradeSpent);
            uint version = materials.Version;
            SkirmishMaterialsService.WriteTransaction(em, session, 1, 150, FactionTacticalMaterialsSpendKind.Production);
            Assert.AreEqual(version, em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Version);
            Assert.Throws<InvalidOperationException>(() =>
                SkirmishMaterialsService.WriteTransaction(em, session, 1, 180, FactionTacticalMaterialsSpendKind.Production));
            Assert.AreEqual(150, em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current,
                "An unsupported extra refund cannot mutate the bank.");
        }

        [Test]
        public void CleanedExpandedSessionDoesNotSuppressTheNextMissionsMaterialsGrant()
        {
            using var world = new World(nameof(CleanedExpandedSessionDoesNotSuppressTheNextMissionsMaterialsGrant));
            var em = world.EntityManager;
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            SkirmishMaterialsService.Initialize(em, session, new SkirmishResolvedSetup { MaterialsEach = 450, MaterialsCapacityEach = 800 });
            em.SetComponentData(session, new SkirmishExpandedSessionComponent
            { Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Playing });
            SkirmishMaterialsService.Write(em, session, 1, 100);
            var nextMission = new InitialUnitsSpawnConfig
            { InitialMaterials = 600, MaterialsCapacity = 900, InitialAiMaterials = 500, AiMaterialsCapacity = 900 };
            FactionTacticalMaterialsStartupSystemHelper.ApplyInitialResourceTotals(em, nextMission);
            Assert.AreEqual(100, SkirmishMaterialsService.Read(em, session, 1));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent
            { Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Cleaning });
            FactionTacticalMaterialsStartupSystemHelper.ApplyInitialResourceTotals(em, nextMission);
            Assert.AreEqual(600, em.GetComponentData<FactionTacticalMaterialsComponent>(SkirmishMaterialsService.FindEconomy(em, 1)).Current);
        }

        [Test]
        public void AirMobileBriefingUsesPackagedLaunchFacts()
        {
            Assert.IsTrue(SkirmishSetupBriefing.TryLoad("S003", 104732, out var setup));
            var en = new SkirmishSetupBriefing(setup, false);
            StringAssert.Contains("18-minute", en.Summary);
            StringAssert.Contains("12 infantry", en.Roster);
            StringAssert.Contains("450 Materials", en.Economy);
            StringAssert.Contains("350 Fuel", en.Economy);
            var fa = new SkirmishSetupBriefing(setup, true);
            StringAssert.Contains("18", fa.Summary);
            Assert.AreNotEqual(en.Objective, fa.Objective);
        }

        [Test]
        public void InputReceiptsRejectDirectStaleAndReusedCommands()
        {
            try
            {
                AriaCommandEvidence.Begin();
                Assert.IsFalse(AriaCommandEvidence.TryReadAudit(0, 0, out int unobserved));
                Assert.AreEqual(-1, unobserved);
                AriaCommandEvidence.Accepted("unsupported direct order", 1);
                Assert.AreEqual(1, AriaCommandEvidence.Violations);
                AriaCommandEvidence.ObserveRelease(10, new Vector2(30, 40));
                Assert.AreEqual(0u, AriaCommandEvidence.ClaimRelease(10, new Vector2(99, 40)));
                uint receipt = AriaCommandEvidence.ClaimRelease(11, new Vector2(30, 40));
                Assert.AreNotEqual(0u, receipt);
                Assert.AreEqual(0u, AriaCommandEvidence.ClaimRelease(11));
                using (AriaCommandEvidence.Enter(receipt))
                {
                    AriaCommandEvidence.Accepted("queued world order", 1);
                    AriaCommandEvidence.Accepted("second member of same group", 1);
                }
                Assert.AreEqual(1, AriaCommandEvidence.Violations);
                using (AriaCommandEvidence.Enter(receipt)) AriaCommandEvidence.Accepted("reused receipt", 1);
                Assert.AreEqual(2, AriaCommandEvidence.Violations);
                AriaCommandEvidence.ObserveRelease(20, Vector2.zero);
                Assert.AreEqual(0u, AriaCommandEvidence.ClaimRelease(23));
                AriaCommandEvidence.Accepted("enemy counter", 2);
                Assert.AreEqual(4, AriaCommandEvidence.AcceptedCommands);
                Assert.AreEqual(2, AriaCommandEvidence.Violations);
                AriaCommandEvidence.ObserveRelease(30, Vector2.zero);
                uint previousAttempt = AriaCommandEvidence.ClaimRelease(30);
                AriaCommandEvidence.Begin();
                AriaCommandEvidence.ObserveRelease(31, Vector2.zero);
                uint nextAttempt = AriaCommandEvidence.ClaimRelease(31);
                Assert.AreNotEqual(previousAttempt, nextAttempt);
                using (AriaCommandEvidence.Enter(previousAttempt)) AriaCommandEvidence.Accepted("previous attempt", 1);
                using (AriaCommandEvidence.Enter(nextAttempt)) AriaCommandEvidence.Accepted("fresh attempt", 1);
                Assert.AreEqual(1, AriaCommandEvidence.Violations);
                Assert.IsTrue(AriaCommandEvidence.TryReadAudit(1, 2, out int observed));
                Assert.AreEqual(3, observed, "Direct commands and unexpected samples are both violations.");
                Assert.IsFalse(AriaCommandEvidence.TryReadAudit(2, 0, out _), "Missing release observation is unknown.");
                AriaCommandEvidence.End();
                Assert.IsFalse(AriaCommandEvidence.TryReadAudit(1, 0, out _));
                AriaCommandEvidence.Begin();
                AriaCommandEvidence.ObserveRelease(40, Vector2.zero);
                using (AriaCommandEvidence.Enter(AriaCommandEvidence.ClaimRelease(40)))
                    AriaCommandEvidence.Accepted("verified request", 1);
                Assert.IsTrue(AriaCommandEvidence.TryReadAudit(1, 0, out int clean));
                Assert.AreEqual(0, clean);
            }
            finally { AriaCommandEvidence.End(); }
        }

        public static void RunBroaderRegressionValidation()
        {
            var failures = new System.Collections.Generic.List<string>();
            void Run(Action action, string name)
            {
                try { action(); }
                catch (Exception exception) { failures.Add(name); Debug.LogError(name + " failed\n" + exception); }
            }
            Run(RunFocusedValidation, "SharedGameplay");
            Run(global::FactionTacticalMaterialsUtilitySystemHelperTests.RunFocusedValidation, "SharedMaterialsUtility");
            Run(SkirmishExpandedDefinitionTests.RunFocusedValidation, "Definitions");
            Run(SkirmishExpandedCatalogTests.RunFocusedValidation, "Catalog");
            Run(SkirmishExpandedObjectiveTests.RunFocusedValidation, "Objectives");
            Run(SkirmishExpandedVisualTests.RunFocusedValidation, "VisualBindings");
            Run(SkirmishExpandedAcceptanceTests.RunFocusedValidation, "EvidenceGates");
            Run(() => Debug.Log(global::SkirmishScenarioSourceBindingTests.RunFocusedValidation()), "SourceBindings");
            if (failures.Count > 0)
                throw new InvalidOperationException("[SkirmishPlayerReadyRegressions] result=Failed suites=" + string.Join(",", failures));
            Debug.Log("[SkirmishPlayerReadyRegressions] result=Passed");
        }

        public static void RunBroaderAndCampaignValidation()
        {
            RunBroaderRegressionValidation();
            global::M01FirstContactCampaignUiTests.RunFocusedValidation();
        }

        public static void RunFocusedValidation()
        {
            try
            {
                new SkirmishSharedMaterialsTests().InputReceiptsRejectDirectStaleAndReusedCommands();
                new SkirmishStartingBuildingsTests().StartingGrantsWaitForSharedResultsAndBindTheActualBaseOnce();
                new SkirmishStartingBuildingsTests().PhysicalSupplyGrantIsAtomicAndDoesNotRefillOnRepeatedStartup();
                new SkirmishStartingBuildingsTests().OilGrantStartsFabricationAndTheHaulChainBeforeRefineryOverflow();
                new SkirmishSharedMaterialsTests().AirMobileBriefingUsesPackagedLaunchFacts();
                new SkirmishSharedMaterialsTests().ConstructionAndExpandedOrdersShareOneBankWithoutRepeatedGrants();
                new SkirmishSharedMaterialsTests().CleanedExpandedSessionDoesNotSuppressTheNextMissionsMaterialsGrant();
                new SkirmishS002AriaHarnessTests().UnknownInputEvidenceCannotCountAsAriaWin();
                new UnitCombatFocusedEditModeTests().StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
                new UnitCombatFocusedEditModeTests().StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy();
                new AutomaticCombatFactionTargetingTests().AcquisitionAndRetaliationRespectAuthoredDomainsAndVisibility();
                new AutomaticCombatFactionTargetingTests().AttackMove_InterruptsCommandedPathForHostileButPlainMoveDoesNot();
                new AutomaticCombatFactionTargetingTests().AttackMove_AcquiresHostileBuildingButIgnoresNeutralDeadAndSuppressedBuildings();
                var towers = new global::BuildingDefenseAttackSystemTests();
                towers.ConstructedStructuresUseAttemptPolicyAndCleanupWithoutReplacingTheObjective();
                towers.GuardTowerDefense_MissionOverlayBindsBothFactionsAndPreservesLegacy();
                towers.GuardTowerDefense_PolicyAppliesAtAcquisitionAndBeforeCachedShot();
                towers.GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget();
                towers.GuardTowerDefense_IgnoresNeutralTargetsAndFiresAtHostileTarget();
                UnitMoveOrderSystemTests.RunFocusedValidation();
                AIFactionControlStartupSystemValidationTests.RunFocusedValidation();
                new SkirmishSharedCombatTests().RuntimeActorAndLinkedVisualSurviveSourceSceneUnload();
                new SkirmishSharedCombatTests().RegistryActorsNeverReceiveSupplementalStructureDamage();
                new SkirmishSharedCombatTests().AntiAirOverlayBindsTheActualMissileWeapon();
                SkirmishExpandedAriaTests.RunFocusedValidation();
                new SkirmishExpandedArmyTests().AcceptedArmyOrdersRequireInputEvidence();
                SkirmishExpandedArmyTests.RunFocusedValidation();
                SkirmishExpandedEconomyTests.RunFocusedValidation();
                SkirmishExpandedCheckpointTests.RunFocusedValidation();
                SkirmishS002AriaHarnessTests.RunFocusedValidation();
                Game.Editor.AriaTouchInputValidation.Run();
                var fuel = new global::VehicleFuelConsumptionSystemTests();
                fuel.VehicleFuelConsumption_DrainsDeliveredFuelStorageAfterGridMovement();
                fuel.VehicleFuelConsumption_DoesNotDrainRefineryOutput();
                fuel.VehicleFuelConsumption_UsesAirFuelCostForAirUnits();
                fuel.AircraftFuelSafetyReturn_ZeroFuelClearsOrdersAndReturnsHome();
                fuel.AircraftFuelSafetyReturn_WithUsableFuelKeepsActiveOrders();
                fuel.GroundVehicleFuelHold_ZeroFuelClearsMovementAndStopsKinematics();
                fuel.GroundVehicleFuelHold_WithUsableFuelKeepsMovement();
                var recipes = new SkirmishSharedProductionRecipeTests();
                recipes.RuntimeRecipeChangesProducerPacketAndPriceWithoutMutatingAuthoredDefaults();
                recipes.CatalogOverlayLeavesOriginalBuildingListAndRegistryUntouched();
                recipes.AuthoredAirMobileCatalogResolvesEveryMissionRecipe();
                var header = new global::UiShellEcsGatewayResourceHeaderTests();
                header.ExpandedResourceHeaderShowsAllOilAndOnlyUnreservedUsableFuel();
                header.MatchHudResourceValues_UsesVersionedUsableFuelSummaryWithoutTextFallback();
                header.MatchHudResourceValues_WarmedVersionChangesDoNotAllocate();
                Debug.Log("[SkirmishSharedMaterialsTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishSharedMaterialsTests] result=Failed\n" + exception);
                throw;
            }
        }
    }
}
