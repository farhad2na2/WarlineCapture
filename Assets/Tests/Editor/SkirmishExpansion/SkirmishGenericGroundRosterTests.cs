#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishGenericGroundRosterTests
    {
        [Test]
        public void GenericGroundMatchesAllSixteenAuthoritativeRolesAndNoExcludedRole()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            var roles = SkirmishRoleOverlayCatalog.CreateForArmy(army, false);
            Assert.AreEqual(16, roles.Length);
            var ids = new HashSet<string>();
            foreach (var role in roles)
            {
                Assert.IsTrue(ids.Add(role.RoleId), role.RoleId);
                Assert.IsTrue(army.Allows(role.RoleId), role.RoleId);
                Assert.IsFalse(role.CapabilityCertified, "An overlay is not native certification.");
            }
            CollectionAssert.AreEquivalent(army.AllowedRoleIds, ids);
            foreach (var excluded in army.ExcludedRoleIds) Assert.IsFalse(ids.Contains(excluded));
        }

        [Test]
        public void LegacyGroundTwelveRemainByteEquivalentValues()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            var legacy = SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            var preserved = SkirmishRoleOverlayCatalog.CreateForArmy(army, true);
            Assert.AreEqual(12, preserved.Length);
            for (int i=0; i<legacy.Length; i++)
                Assert.AreEqual(JsonUtility.ToJson(legacy[i]), JsonUtility.ToJson(preserved[i]));
            // Compiler-level S002/S003 identity preservation is separately guarded by
            // the actual immutable legacy12 normal/policyless baseline regression.
        }

        [Test]
        public void AllSixteenHavePaidProductionEligibilityAtRequiredReadiness()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            var roles = SkirmishRoleOverlayCatalog.CreateForArmy(army, false);
            foreach (var role in roles)
            {
                var request = Request(role);
                var stage = SkirmishProductionEligibility.RequiredReadiness(role.RoleKind);
                var decision = SkirmishProductionEligibility.Evaluate(request, army, stage, roles);
                Assert.IsTrue(decision.Accepted, role.RoleId + ": " + decision.Field);
                Assert.AreEqual(role.Producer, decision.Producer);
                Assert.AreEqual(role.MaterialsCost, decision.MaterialsCost);
                Assert.Greater(decision.MaterialsCost, 0, role.RoleId);
                if ((int)stage > (int)SkirmishReadinessStage.Field)
                {
                    var below = SkirmishProductionEligibility.Evaluate(request, army,
                        (SkirmishReadinessStage)((int)stage-1), roles);
                    Assert.IsFalse(below.Accepted, role.RoleId);
                    Assert.AreEqual(SkirmishReasonCode.MissingReadiness, below.Reason);
                }
            }
        }

        [Test]
        public void MissingProducersAndInsufficientStocksStillRejectEveryRole()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            var roles = SkirmishRoleOverlayCatalog.CreateForArmy(army, false);
            foreach (var role in roles)
            {
                var request = Request(role);
                request.BarracksPresent = request.GroundStagingPresent = request.HelipadPresent =
                    request.IntelStationPresent = request.AirportPresent = false;
                var absent = SkirmishProductionEligibility.Evaluate(request, army, SkirmishReadinessStage.FullArsenal, roles);
                Assert.IsFalse(absent.Accepted, role.RoleId);
                Assert.AreEqual(SkirmishReasonCode.MissingProducer, absent.Reason);
                request = Request(role); request.MaterialsAvailable = 0;
                var poor = SkirmishProductionEligibility.Evaluate(request, army, SkirmishReadinessStage.FullArsenal, roles);
                Assert.IsFalse(poor.Accepted, role.RoleId);
                Assert.AreEqual(SkirmishReasonCode.InsufficientMaterials, poor.Reason);
            }
        }

        [Test]
        public void FourFormerlyMissingRolesReuseExistingValuesAndProducerBindings()
        {
            var ground = SkirmishRoleOverlayCatalog.CreateGroundManeuverSlice();
            var combined = SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice();
            var catalog = ScriptableObject.CreateInstance<SkirmishRoleCatalogConfig>();
            try
            {
                catalog.ConfigureCanonicalGroundSlice();
                foreach (var kind in new[] { SkirmishRoleKind.ApcHeavy, SkirmishRoleKind.Siege,
                    SkirmishRoleKind.TransportHeli, SkirmishRoleKind.Drone })
                {
                    Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(ground, kind, out var projected));
                    Assert.IsTrue(SkirmishRoleOverlayCatalog.TryGet(combined, kind, out var original));
                    Assert.AreEqual(JsonUtility.ToJson(original), JsonUtility.ToJson(projected));
                    Assert.IsTrue(catalog.TryGet(projected.RoleId, out var binding));
                    Assert.AreEqual(projected.Producer, binding.Producer);
                    Assert.AreEqual(SkirmishProductionEligibility.RequiredReadiness(kind), binding.RequiredReadiness);
                }
            }
            finally { Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void OffensiveAirAndFixedWingRemainExcludedEvenWithFullArsenal()
        {
            var army = SkirmishArmyProfileConfig.ResolveCached(SkirmishArmyProfileId.GroundManeuver);
            var all = SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice();
            foreach (var role in all)
            {
                if (army.Allows(role.RoleId)) continue;
                var rejected = SkirmishProductionEligibility.Evaluate(Request(role), army,
                    SkirmishReadinessStage.FullArsenal, all);
                Assert.IsFalse(rejected.Accepted, role.RoleId);
                Assert.AreEqual(SkirmishReasonCode.UnsupportedRole, rejected.Reason);
            }
        }

        private static SkirmishProductionRequest Request(SkirmishRoleOverlay role) => new SkirmishProductionRequest
        {
            RoleId = role.RoleId, RoleKind = role.RoleKind, SquadCount = 1,
            BarracksPresent = true, GroundStagingPresent = true, HelipadPresent = true,
            IntelStationPresent = true, AirportPresent = true,
            EnforceStocks = true, MaterialsAvailable = 100000, FuelAvailable = 100000,
            InfantryCap = 1000, GroundCap = 1000, AirCap = 1000, SupplyCap = 10000
        };

        [Test]
        public void GenericGroundRosterValuesAffectBothHashesAndLegacyHashTextStaysUnchanged()
        {
            var setup = new SkirmishResolvedSetup
            {
                CatalogId = "S026", ArmyProfileId = SkirmishArmyProfileId.GroundManeuver,
                LayoutBindingFingerprint = new string('a', 64),
                RoleOverlays = SkirmishRoleOverlayCatalog.CreateGroundManeuverSlice()
            };
            uint normal = SkirmishSetupCompiler.ComputeHash(setup, 17);
            uint policyless = SkirmishSetupCompiler.ComputePolicylessHash(setup, 17);
            setup.RoleOverlays[0].MaterialsCost++;
            Assert.AreNotEqual(normal, SkirmishSetupCompiler.ComputeHash(setup, 17));
            Assert.AreNotEqual(policyless, SkirmishSetupCompiler.ComputePolicylessHash(setup, 17));
            setup.LayoutBindingFingerprint = string.Empty;
            uint legacy = SkirmishSetupCompiler.ComputeHash(setup, 17);
            uint legacyPolicyless = SkirmishSetupCompiler.ComputePolicylessHash(setup, 17);
            setup.RoleOverlays[0].MaterialsCost++;
            Assert.AreEqual(legacy, SkirmishSetupCompiler.ComputeHash(setup, 17));
            Assert.AreEqual(legacyPolicyless, SkirmishSetupCompiler.ComputePolicylessHash(setup, 17));
        }

        [Test]
        public void GenericEstablishedGroundCompilesRequiredHelipadAndExactStructureCountsForAllSizes()
        {
            var catalog = SkirmishExpansionCatalogConfig.Load();
            Assert.IsNotNull(catalog);
            var profiles = catalog.ToAuthoredSet();
            var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset");
            var template = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishLayout_S005.asset");
            Assert.IsNotNull(source); Assert.IsNotNull(template);
            var layout = Object.Instantiate(template);
            var definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
            try
            {
                var serialized = new SerializedObject(layout);
                serialized.FindProperty("layoutId").stringValue = "layout.skirmish.cc.ba";
                serialized.FindProperty("operationMapId").stringValue = "opmap.skirmish.city_crossroads";
                serialized.FindProperty("worldBinding").FindPropertyRelative("SourceHash").stringValue = source.ContentHash;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var features = new[] { "ground", "intel", "transport", "advanced_ground", "objective_ba" };
                definition.Configure("S026", profiles.ObjectiveBa, profiles.ArmyGround, profiles.StartEstablished,
                    profiles.Economy, profiles.Upgrades, profiles.Intel, profiles.Roles, layout,
                    SkirmishSizeId.Standard, features);
                definition.BindCampaignMap(source);
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoad(project, out var matrix, out string error), error);
                foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
                {
                    Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, 0,
                        new SkirmishContentManifest { RequiredFeatureIds = features }, matrix, out var setup, out var reasons),
                        string.Join(";", reasons));
                    Assert.IsTrue(setup.MeasuredLayoutBound);
                    Assert.AreEqual(16, setup.RoleOverlays.Length);
                    int player = 0, enemy = 0, helipads = 0;
                    foreach (var structure in setup.Structures)
                    {
                        if (structure.FactionId == 1) player++;
                        if (structure.FactionId == 2) enemy++;
                        if (structure.StructureId == SkirmishStructureIds.Helipad) helipads++;
                    }
                    Assert.AreEqual(setup.PlayerStartingStructures, player, size.ToString());
                    Assert.AreEqual(setup.EnemyStartingStructures, enemy, size.ToString());
                    Assert.AreEqual(size == SkirmishSizeId.Standard ? 10 : 11, player);
                    Assert.AreEqual(2, helipads);
                }
                // Only logical in-memory configs were created. This test establishes
                // compile/schema counts, never native placement/source fit or readiness.
            }
            finally { Object.DestroyImmediate(definition); Object.DestroyImmediate(layout); }
        }

        public static void RunFocusedValidation()
        {
            var tests = new SkirmishGenericGroundRosterTests();
            tests.GenericGroundMatchesAllSixteenAuthoritativeRolesAndNoExcludedRole();
            tests.LegacyGroundTwelveRemainByteEquivalentValues();
            tests.AllSixteenHavePaidProductionEligibilityAtRequiredReadiness();
            tests.MissingProducersAndInsufficientStocksStillRejectEveryRole();
            tests.FourFormerlyMissingRolesReuseExistingValuesAndProducerBindings();
            tests.OffensiveAirAndFixedWingRemainExcludedEvenWithFullArsenal();
            tests.GenericGroundRosterValuesAffectBothHashesAndLegacyHashTextStaysUnchanged();
            tests.GenericEstablishedGroundCompilesRequiredHelipadAndExactStructureCountsForAllSizes();
            Debug.Log("[SkirmishGenericGroundRosterTests] result=Passed tests=8 roles=16 nativeCapability=Pending");
        }
    }
}
#endif
