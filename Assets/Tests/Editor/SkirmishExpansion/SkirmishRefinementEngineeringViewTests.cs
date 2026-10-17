#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishRefinementEngineeringViewTests
    {
        static void WithViewInputs(string id,Action<SkirmishScenarioDefinitionConfig,SkirmishMissionLogicalAuthoringInputs,EntityManager,
            List<UnityEngine.Object>,IReadOnlyList<SkirmishSetupMatrixRow>> action)
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition(id,(definition,owned)=>
            {
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
                Assert.IsTrue(SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,
                    new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var inputs,out var reasons),string.Join(";",reasons));
                using var world=new World("SyntheticEngineeringViewRoleRegistry");var em=world.EntityManager;
                var registry=em.CreateEntity();em.AddBuffer<UnitPrefabRegistryEntry>(registry);
                foreach(var role in inputs.RequiredFramingRoles.Where(r=>SkirmishRoleIds.Category(r)==SkirmishPopulationCategory.Air))
                {
                    var prefab=em.CreateEntity(typeof(UnitAirMovement),typeof(UnitSourcePrefabKey));
                    em.SetComponentData(prefab,new UnitAirMovement{CruiseHeight=role==SkirmishRoleKind.Drone?25:15,RunwayTaxiSpeed=5});
                    em.SetComponentData(prefab,new UnitSourcePrefabKey{Value=new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(role))});
                    var entries=em.GetBuffer<UnitPrefabRegistryEntry>(registry);entries.Add(new UnitPrefabRegistryEntry{Prefab=prefab});
                }
                action(definition,inputs,em,owned,matrix);
            });
        }
        static Dictionary<SkirmishRoleKind,float> Targets(SkirmishMissionLogicalAuthoringInputs inputs)=>
            inputs.RequiredFramingRoles.Where(r=>SkirmishRoleIds.Category(r)==SkirmishPopulationCategory.Air)
                .ToDictionary(r=>r,r=>r==SkirmishRoleKind.Drone?.006f:.012f); // Explicit synthetic fixture targets, not mission acceptance.
        [Test]public void ActualLowAndHighSourcesProduceRelativeEngineeringIntentAndExactCaptureRequirements()
        {
            var elevations=new List<float>();
            foreach(string id in new[]{"S026","S049"})WithViewInputs(id,(definition,inputs,em,owned,matrix)=>
            {
                var variant=SkirmishGroundSourceFitSurvey.CreateRefinementEngineeringView(definition,inputs,em,null,Targets(inputs));owned.Add(variant.View);
                elevations.Add(inputs.SourceMinimumElevation);
                Assert.AreEqual(inputs.SourceMinimumElevation,variant.View.GroundReferenceY);
                Assert.AreEqual(12,variant.View.MinHeight-variant.View.GroundReferenceY,.0001f);
                Assert.AreEqual(18,variant.View.NormalHeight-variant.View.GroundReferenceY,.0001f);
                Assert.AreEqual(20,variant.View.MaxHeight-variant.View.GroundReferenceY,.0001f);
                Assert.AreEqual(.03f,variant.View.InfantryMinimumViewportHeight);Assert.IsTrue(variant.View.IsValid);
                CollectionAssert.AreEquivalent(inputs.RequiredFramingRoles,variant.Framing.Select(r=>r.Role));
                Assert.IsTrue(inputs.ActualOpeningPlayerActorIds.Contains(variant.OpeningFocusStableActorId));
                foreach(var role in variant.Framing)
                {
                    Assert.AreEqual(inputs.SourceMinimumElevation,role.MinimumSubjectElevation);
                    Assert.AreEqual(15,(byte)role.Evidence);Assert.IsTrue((role.Stages&SkirmishViewCaptureStage.MaximumZoom)!=0);
                    if(SkirmishRoleIds.Category(role.Role)==SkirmishPopulationCategory.Air)
                    {Assert.Greater(role.MaximumSubjectElevation,inputs.SourceMaximumElevation);Assert.IsTrue((role.Stages&SkirmishViewCaptureStage.FollowCruising)!=0);}
                }
                Assert.AreEqual(SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant),variant.ReviewedViewFingerprint);
            });
            Assert.Greater(elevations.Distinct().Count(),1,"Control must exercise actual differing source elevations");
        }
        [Test]public void AllThreeEngineeringVariantsAttachThroughActualNormalCompiler()
        {
            WithViewInputs("S026",(definition,inputs,em,owned,matrix)=>
            {
                var variants=new List<SkirmishMissionLogicalVariant>();
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Assert.IsTrue(SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var current,out var reasons),string.Join(";",reasons));
                    var variant=SkirmishGroundSourceFitSurvey.CreateRefinementEngineeringView(definition,current,em,null,Targets(current));owned.Add(variant.View);variants.Add(variant);
                }
                Assert.IsTrue(SkirmishGroundSourceFitSurvey.TryAttachRefinementLogicalBinding(definition,variants.ToArray(),matrix,out var binding,out string error),error);
                owned.Add(binding);Assert.AreSame(binding,definition.MissionLogicalBinding);
                foreach(var variant in variants)
                    Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,variant.Difficulty,variant.Size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out _,out var reasons),string.Join(";",reasons));
            });
        }
        [Test]public void MissingRoleTargetsOrChangedSourceCannotBeSilentlyAccepted()
        {
            WithViewInputs("S026",(definition,inputs,em,owned,matrix)=>
            {
                Assert.IsTrue(inputs.RequiredFramingRoles.Any(r=>SkirmishRoleIds.Category(r)==SkirmishPopulationCategory.Air));
                Assert.Throws<InvalidOperationException>(()=>SkirmishGroundSourceFitSurvey.CreateRefinementEngineeringView(definition,inputs,em,null,null));
                inputs.SourceMinimumElevation+=1;
                Assert.Throws<InvalidOperationException>(()=>SkirmishGroundSourceFitSurvey.CreateRefinementEngineeringView(definition,inputs,em,null,Targets(inputs)));
                Assert.IsNull(definition.MissionLogicalBinding);
            });
        }
        [Test]public void MalformedVariantSetRefusesBeforeBindingMutation()
        {
            WithViewInputs("S026",(definition,inputs,em,owned,matrix)=>
            {
                Assert.IsFalse(SkirmishGroundSourceFitSurvey.TryAttachRefinementLogicalBinding(definition,new SkirmishMissionLogicalVariant[3],matrix,out _,out _));
                Assert.IsNull(definition.MissionLogicalBinding);
            });
        }
        [Test]public void ExistingBilingualBriefingDisclosesOnlyBtCeDefenderAdvantage()
        {
            foreach(var kind in new[]{SkirmishObjectiveKind.Breakthrough,SkirmishObjectiveKind.ConvoyEscort})
            {
                var setup=new SkirmishResolvedSetup{ObjectiveKind=kind};
                StringAssert.Contains("two additional watchtowers",new SkirmishSetupBriefing(setup,false).Roster);
                StringAssert.Contains("دو برج نگهبانی اضافه",new SkirmishSetupBriefing(setup,true).Roster);
            }
            foreach(var kind in new[]{SkirmishObjectiveKind.BaseAssault,SkirmishObjectiveKind.FrontlineControl})
                Assert.AreEqual(string.Empty,SkirmishSetupBriefing.DefenderAdvantage(kind,false));
        }
        [Test]public void FinalizationBindsEachActualVariantBeforeGeometryAndMeasurementFingerprint()
        {
            SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>
            {
                var definition=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition;
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));
                foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar})
                {
                    Assert.IsTrue(SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,SkirmishDifficultyId.Regular,size,1,
                        new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out var input,out var reasons),string.Join(";",reasons));
                    var source=definition.MissionLogicalBinding.Variants[0].Areas;
                    var draft=JsonUtility.FromJson<SkirmishObjectiveAreaConfig>(JsonUtility.ToJson(source));draft.Binding=default;
                    string unbound=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(draft);
                    var bound=SkirmishGroundSourceFitSurvey.BindRefinementAreaGeometry(draft,input);
                    string geometry=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(bound);
                    Assert.AreNotEqual(unbound,geometry);Assert.AreEqual(input.ActualAreaBinding.LayoutBindingFingerprint,bound.Binding.LayoutBindingFingerprint);
                    bound.Measurement=source.Measurement;bound.Measurement.GeometryFingerprint=geometry;
                    Assert.IsTrue(SkirmishObjectiveAreaCompiler.TryCompile(bound,input.ActualAreaBinding,input.ObjectiveKind,out var compiled,out string error),error);
                    Assert.AreEqual(geometry,compiled.GeometryFingerprint);Assert.AreEqual(default(SkirmishObjectiveAreaBinding),draft.Binding);
                }
            });
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishRefinementEngineeringViewTests();t.ActualLowAndHighSourcesProduceRelativeEngineeringIntentAndExactCaptureRequirements();
            t.AllThreeEngineeringVariantsAttachThroughActualNormalCompiler();t.MissingRoleTargetsOrChangedSourceCannotBeSilentlyAccepted();
            t.MalformedVariantSetRefusesBeforeBindingMutation();t.FinalizationBindsEachActualVariantBeforeGeometryAndMeasurementFingerprint();
            t.ExistingBilingualBriefingDisclosesOnlyBtCeDefenderAdvantage();
            Debug.Log("[SkirmishRefinementEngineeringViewTests] result=Passed controls=6 nativeCameraAccepted=0 highLowCameraBehavior=Pending");
        }
    }
    public static class SkirmishLogicalRefinementPreparationValidation
    {
        public static void Run()
        {
            var failures=new List<string>();
            void Part(string label,Action action)
            {try{action();}catch(Exception error){failures.Add(label+":"+error);Debug.LogError("[SkirmishLogicalRefinementPreparation] failed="+label+" "+error);}}
            Part("enemy13",SkirmishEnemyAirCombined13Tests.RunFocusedValidation);
            Part("approach180",SkirmishObjectiveApproachTowerTests.RunFocusedValidation);
            Part("view6",SkirmishRefinementEngineeringViewTests.RunFocusedValidation);
            Part("typedSpawn5",SkirmishTypedSpawnConstraintTests.RunFocusedValidation);
            if(failures.Count>0)throw new InvalidOperationException(string.Join("\n",failures));
            Debug.Log("[SkirmishLogicalRefinementPreparation] result=Passed enemy=5 approach=3 view=6 typedSpawn=5 canonicalSizeVectors=180 nativeMissionAcceptance=Pending");
        }
    }
}
#endif
