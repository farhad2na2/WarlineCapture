#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishStaticAuthoringBudgetTests
    {
        [TestCase(SkirmishSizeId.Standard)][TestCase(SkirmishSizeId.War)][TestCase(SkirmishSizeId.LargeWar)]
        public void ActualPackagedLegacyRosterEqualsNormalCompiler(SkirmishSizeId size)
        {
            var catalog=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);
            Assert.IsNotNull(catalog);var definition=catalog.ToAuthoredSet().DefinitionS005;Assert.IsNotNull(definition);
            Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out var error),error);
            var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
            Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,size,155926,manifest,matrix,out var setup,out var reasons),string.Join(";",reasons));
            Assert.IsTrue(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,SkirmishDifficultyId.Regular,size,155926,manifest,matrix,out var budget,out reasons),string.Join(";",reasons));
            Assert.IsTrue(budget.NormalCompilerPassed);Assert.IsTrue(budget.ExactCanonicalQuantities,string.Join(";",budget.CanonicalBudgetErrors));
            CollectionAssert.AreEqual(setup.Forces,budget.Forces);CollectionAssert.AreEqual(setup.Structures,budget.Structures);
        }
        [Test] public void MissingLogicalProofStillRejectsNormalCompileButExposesExactStaticRoster()
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition("S031",(definition,owned)=>
            {
                var catalog=Resources.Load<SkirmishExpansionCatalogConfig>(SkirmishExpansionCatalogConfig.ResourceName);var p=catalog.ToAuthoredSet();
                var objective=UnityEngine.Object.Instantiate(p.ObjectiveBa);owned.Add(objective);
                JsonUtility.FromJsonOverwrite("{\"objectiveId\":\"FC\",\"kind\":2,\"standardDeadlineSeconds\":1200,\"warDeadlineSeconds\":1500,\"largeWarDeadlineSeconds\":1800}",objective);
                definition.Configure("S031",objective,p.ArmyGround,p.StartField,p.Economy,p.Upgrades,p.Intel,p.Roles,definition.MapLayout,
                    SkirmishSizeId.Standard,new[]{"ground","intel","transport","advanced_ground","objective_fc"});
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out var error),error);
                var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
                Assert.IsFalse(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,manifest,matrix,out var rejected,out var normalReasons));
                Assert.IsNull(rejected);Assert.IsTrue(normalReasons.Any(r=>r.Field=="missionLogicalBinding"));
                Assert.IsTrue(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,manifest,matrix,out var budget,out var reasons));
                Assert.IsFalse(budget.NormalCompilerPassed);Assert.IsTrue(reasons.Any(r=>r.Field=="missionLogicalBinding"));
                Assert.IsTrue(budget.ExactCanonicalQuantities,string.Join(";",budget.CanonicalBudgetErrors));
                Assert.AreEqual(12,budget.Forces.Where(r=>r.FactionId==1&&SkirmishRoleIds.Category(r.RoleKind)==SkirmishPopulationCategory.Infantry).Sum(r=>r.Quantity));
                Assert.IsNull(typeof(SkirmishStaticAuthoringBudget).GetProperty("SetupHash"));
                Assert.IsNull(typeof(SkirmishStaticAuthoringBudget).GetField("SetupHash"));
                var copy=budget.Forces;int original=copy[0].Quantity;copy[0].Quantity=999;
                Assert.AreEqual(original,budget.Forces[0].Quantity,"Caller mutations cannot change captured authoring data");
            });
        }
        [Test] public void MissingDefinitionAndMatrixRefuseWithoutBudget()
        {
            Assert.IsFalse(SkirmishSetupCompiler.TryInspectStaticAuthoringBudget(null,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,null,null,out var budget,out var reasons));
            Assert.IsNull(budget);Assert.IsNotEmpty(reasons);
        }
        public static void RunFocusedValidation()
        {
            var tests=new SkirmishStaticAuthoringBudgetTests();foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})tests.ActualPackagedLegacyRosterEqualsNormalCompiler(size);
            tests.MissingLogicalProofStillRejectsNormalCompileButExposesExactStaticRoster();tests.MissingDefinitionAndMatrixRefuseWithoutBudget();
            Debug.Log("[SkirmishStaticAuthoringBudgetTests] result=Passed controls=5 sourceFitCertified=0");
        }
    }
}
#endif
