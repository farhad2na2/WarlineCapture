using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishEnemyGroundRolloutTests
    {
        private static SkirmishResolvedSetup Supported() => new SkirmishResolvedSetup {
            CatalogId = "S017", DefinitionId = "skirmish.s017", Seed = 713,
            ObjectiveKind = SkirmishObjectiveKind.BaseAssault,
            ArmyProfileId = SkirmishArmyProfileId.CombinedArms,
            StartPackageId = SkirmishStartPackageId.FieldBase,
            Readiness = SkirmishReadinessStage.Field };

        // Golden hashes come from the original pre-policy compiler's canonical hash
        // inputs and the unchanged authored matrix; this is not a self-comparison
        // against ComputePolicylessHash from the modified compiler.
        [TestCase("S002", SkirmishSizeId.Standard, 0x56CF3C8Bu)]
        [TestCase("S002", SkirmishSizeId.War, 0xC1AC4C1Eu)]
        [TestCase("S002", SkirmishSizeId.LargeWar, 0xC59CA169u)]
        [TestCase("S003", SkirmishSizeId.Standard, 0x0EA5BD22u)]
        [TestCase("S003", SkirmishSizeId.War, 0xAD60B105u)]
        [TestCase("S003", SkirmishSizeId.LargeWar, 0x7FFA8B7Bu)]
        [TestCase("S004", SkirmishSizeId.Standard, 0x2E6B4FBEu)]
        [TestCase("S004", SkirmishSizeId.War, 0x9B589E6Cu)]
        [TestCase("S004", SkirmishSizeId.LargeWar, 0x9BEAAC24u)]
        public void ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity(
            string catalogId, SkirmishSizeId size, uint expectedHash)
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out var error), error);
            var catalog = SkirmishExpansionCatalogConfig.Load(); Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGetDefinition(catalogId, out var definition));
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                size, 104734, new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds },
                matrix, out var setup, out var reasons), string.Join(";", reasons));
            Assert.IsNull(setup.EnemyPolicy);
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
            Assert.AreEqual(definition.ContentHash, setup.ContentHash);
            Assert.AreEqual(expectedHash, setup.SetupHash, catalogId + "/" + size);
            Assert.IsTrue(SkirmishSetupMatrixTable.TryFind(matrix, catalogId, size, out var row));
            Assert.AreEqual(row.MaterialsEach, setup.MaterialsEach);
            Assert.AreEqual(row.OilEach, setup.OilEach);
            Assert.AreEqual(row.UsableFuelEach, setup.UsableFuelEach);
            Assert.AreEqual(row.PlayerAir, setup.PlayerAir); Assert.AreEqual(row.EnemyAir, setup.EnemyAir);
            Assert.AreEqual(row.PlayerInfantry, setup.PlayerInfantry); Assert.AreEqual(row.EnemyInfantry, setup.EnemyInfantry);
            Assert.AreEqual(row.PlayerGround, setup.PlayerGround); Assert.AreEqual(row.EnemyGround, setup.EnemyGround);
        }

        [Test]
        public void TypedGroundPilotAcceptsDifferentCatalogAndSeedWithoutChangingStockOrRoles()
        {
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out var error), error);
            var catalog = SkirmishExpansionCatalogConfig.Load(); Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGetDefinition("S005", out var source));
            Assert.IsTrue(SkirmishSetupMatrixTable.TryFind(matrix, "S005", SkirmishSizeId.Standard, out var originalRow));
            var row = new SkirmishSetupMatrixRow();
            foreach (var field in typeof(SkirmishSetupMatrixRow).GetFields(BindingFlags.Instance | BindingFlags.Public))
                field.SetValue(row, field.GetValue(originalRow));
            row.CatalogId = "S017";
            var definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
            try
            {
                // Isolated alternate logical identity on the same authored Campaign layout;
                // not a new exposed mission or physical map.
                definition.Configure("S017", source.ObjectiveConfig, source.ArmyProfileConfig,
                    source.StartPackageConfig, source.EconomyConfig, source.UpgradeCatalog,
                    source.IntelConfig, source.RoleCatalog, source.MapLayout, featureIds: source.RequiredFeatureIds);
                foreach (int seed in new[] { 713, 99127 })
                {
                    Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular,
                        SkirmishSizeId.Standard, seed,
                        new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds },
                        new List<SkirmishSetupMatrixRow> { row }, out var setup, out var reasons), string.Join(";", reasons));
                    Assert.IsTrue(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
                    Assert.IsNotNull(setup.EnemyPolicy);
                    Assert.AreEqual(definition.ContentHash, setup.EnemyPolicy.SourceContentHash);
                    Assert.AreEqual(definition.ContentHash + "/enemy-policy-v1/" + setup.EnemyPolicy.Fingerprint, setup.ContentHash);
                    Assert.AreNotEqual(SkirmishSetupCompiler.ComputePolicylessHash(setup, seed), setup.SetupHash);
                    Assert.AreEqual(originalRow.MaterialsEach, setup.MaterialsEach);
                    Assert.AreEqual(originalRow.OilEach, setup.OilEach);
                    Assert.AreEqual(originalRow.UsableFuelEach, setup.UsableFuelEach);
                    Assert.AreEqual(originalRow.PlayerInfantry, setup.PlayerInfantry);
                    Assert.AreEqual(originalRow.EnemyInfantry, setup.EnemyInfantry);
                    Assert.AreEqual(originalRow.PlayerGround, setup.PlayerGround);
                    Assert.AreEqual(originalRow.EnemyGround, setup.EnemyGround);
                    Assert.AreEqual(22, setup.RoleOverlays.Length);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        [Test]
        public void UnsupportedCapabilityInputsDoNotSelectNewCommander()
        {
            Assert.IsTrue(SkirmishSetupCompiler.SupportsGroundCommanderRollout(Supported()));
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(null));
            var setup = Supported(); setup.ArmyProfileId = SkirmishArmyProfileId.AirMobile;
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup), "AirMobile zero-air Field is still unsupported");
            setup = Supported(); setup.ArmyProfileId = SkirmishArmyProfileId.GroundManeuver;
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
            setup = Supported(); setup.StartPackageId = SkirmishStartPackageId.EstablishedBase;
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
            setup = Supported(); setup.Readiness = SkirmishReadinessStage.Established;
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
            setup = Supported(); setup.PlayerAir = 1;
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
            setup = Supported(); setup.EnemyAir = 1;
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
            setup = Supported(); setup.ObjectiveKind = SkirmishObjectiveKind.None;
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
        }

        [Test]
        public void ExcludedPolicylessNativeSaveRetainsIdentityAndNoNewPlannerParts()
        {
            using var world = new World(nameof(ExcludedPolicylessNativeSaveRetainsIdentityAndNoNewPlannerParts));
            var em = world.EntityManager;
            SkirmishNativeCheckpointCodecTests.Capture(em, out var setup, out _);
            setup.CatalogId = "S003"; setup.DefinitionId = "skirmish.s003";
            setup.ObjectiveKind = SkirmishObjectiveKind.BaseAssault;
            setup.ArmyProfileId = SkirmishArmyProfileId.AirMobile;
            setup.StartPackageId = SkirmishStartPackageId.FieldBase;
            setup.Readiness = SkirmishReadinessStage.Field;
            Assert.IsNull(setup.EnemyPolicy);
            Assert.IsFalse(SkirmishSetupCompiler.SupportsGroundCommanderRollout(setup));
            setup.SetupHash = SkirmishSetupCompiler.ComputeHash(setup, setup.Seed);
            using var query = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            var session = query.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.CatalogId = setup.CatalogId; state.DefinitionId = setup.DefinitionId;
            state.SetupHash = setup.SetupHash; state.ObjectiveKind = setup.ObjectiveKind;
            state.Phase = SkirmishSessionPhase.Playing; state.SpawnComplete = 1;
            em.SetComponentData(session, state);
            Assert.IsTrue(SkirmishEnemyPlannerService.EnsureSession(em, session, setup));
            Assert.IsFalse(em.HasComponent<SkirmishEnemyPlannerV1Component>(session));
            Assert.IsFalse(em.HasComponent<SkirmishEnemyProductionIntentV1Component>(session));
            Assert.IsFalse(em.HasBuffer<SkirmishEnemyGroupAssignmentV1>(session));
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session,
                _ => "prefab", out var saved, out var failure), failure);
            var directory = Path.Combine(Path.GetTempPath(), "warline-ground-rollout-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "match.json");
            try
            {
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryWriteAtomic(path, saved, setup));
                string bytes = File.ReadAllText(path);
                Assert.IsTrue(SkirmishNativeCheckpointCodec.TryRead(path, setup, out var decoded));
                Assert.AreEqual(saved.SetupHash, decoded.SetupHash);
                Assert.AreEqual(bytes, File.ReadAllText(path));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishEnemyGroundRolloutTests();
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S002", SkirmishSizeId.Standard, 0x56CF3C8Bu);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S002", SkirmishSizeId.War, 0xC1AC4C1Eu);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S002", SkirmishSizeId.LargeWar, 0xC59CA169u);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S003", SkirmishSizeId.Standard, 0x0EA5BD22u);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S003", SkirmishSizeId.War, 0xAD60B105u);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S003", SkirmishSizeId.LargeWar, 0x7FFA8B7Bu);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S004", SkirmishSizeId.Standard, 0x2E6B4FBEu);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S004", SkirmishSizeId.War, 0x9B589E6Cu);
            suite.ExistingExcludedPackagedSetupsKeepExactPrePolicyIdentity("S004", SkirmishSizeId.LargeWar, 0x9BEAAC24u);
            suite.TypedGroundPilotAcceptsDifferentCatalogAndSeedWithoutChangingStockOrRoles();
            suite.UnsupportedCapabilityInputsDoNotSelectNewCommander();
            suite.ExcludedPolicylessNativeSaveRetainsIdentityAndNoNewPlannerParts();
            Debug.Log("[SkirmishEnemyGroundRolloutTests] result=Passed tests=12 scope=typed-ground-rollout-identity nativePlayerWins=0");
        }
    }
}
