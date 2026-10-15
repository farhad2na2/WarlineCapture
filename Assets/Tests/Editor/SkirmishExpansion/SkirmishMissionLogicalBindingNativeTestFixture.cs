#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Synthetic disposable logical/actor fixture with a GENUINE definition ->
    // existing source -> compiler -> ordinary dispatch -> provider binding.
    // No persistent assets, physical geometry, readiness or native spawn/fit proof.
    public static class SkirmishMissionLogicalBindingNativeTestFixture
    {
        public static void WithActiveFrontline(Action<EntityManager,Entity> assertion)
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition("S031",(definition,owned)=>
            {
                var profiles=SkirmishExpansionCatalogConfig.Load().ToAuthoredSet();
                var objective=UnityEngine.Object.Instantiate(profiles.ObjectiveBa);owned.Add(objective);
                var serialized=new SerializedObject(objective);
                serialized.FindProperty("kind").intValue=(int)SkirmishObjectiveKind.FrontlineControl;
                serialized.FindProperty("objectiveId").stringValue="FC";
                serialized.FindProperty("standardDeadlineSeconds").intValue=1200;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                definition.Configure("S031",objective,profiles.ArmyGround,profiles.StartField,
                    profiles.Economy,profiles.Upgrades,profiles.Intel,profiles.Roles,definition.MapLayout,
                    featureIds:new[]{"ground","intel","transport","advanced_ground","objective_fc"});
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
                var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
                Assert.IsTrue(SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,
                    SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104729,manifest,matrix,out var inputs,out var reasons),string.Join(";",reasons));
                AttachExplicitSyntheticLogicalBinding(definition,inputs,owned);
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard,104729,manifest,matrix,out var setup,out reasons),string.Join(";",reasons));

                var actualCatalog=SkirmishExpansionCatalogConfig.Load();
                var authored=UnityEngine.Object.Instantiate(actualCatalog);owned.Add(authored);
                serialized=new SerializedObject(authored);var definitions=serialized.FindProperty("definitions");
                definitions.arraySize=1;definitions.GetArrayElementAtIndex(0).objectReferenceValue=definition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var library=ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();owned.Add(library);
                const int index=3031;
                library.Configure(new[]{new SkirmishBattleCatalogEntry{ScenarioId="S031",DefinitionId=definition.DefinitionId,
                    ContentVersion=definition.ContentVersion,Status=SkirmishBattleCatalogStatus.Playable,PlayableScenarioIndex=index}});
                using var authoredScope=SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(authored);
                using var libraryScope=SkirmishBattleCatalogConfig.UseEditorValidationCatalog(library);
                using var world=new World("ActuallyProviderBoundSyntheticFrontline");world.SetTime(new TimeData(1,.1f));var em=world.EntityManager;
                var config=QuickGameConfig.Defaults;config.ScenarioIndex=index;config.MapSeed=104729;
                Assert.IsTrue(SkirmishLaunchProjection.TryQueue(em,config));
                using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));var session=sessions.GetSingletonEntity();
                Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var compiled));
                Assert.AreEqual(setup.SetupHash,em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup.SetupHash);
                var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);native.SpawnComplete=1;native.Phase=SkirmishSessionPhase.Playing;em.SetComponentData(session,native);
                // Queue alone does not run session initialization in this synthetic actor fixture.
                if(!em.HasComponent<SkirmishObjectiveStateComponent>(session))
                    em.AddComponentData(session,new SkirmishObjectiveStateComponent{Kind=setup.ObjectiveKind,State=SkirmishObjectiveStateKind.Active});
                // Synthetic actor fixture mirrors the actual after-spawn clock lifecycle.
                if(!em.HasComponent<SkirmishObjectiveClockComponent>(session))
                    em.AddComponentData(session,new SkirmishObjectiveClockComponent{DeadlineSeconds=setup.DeadlineSeconds});
                var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);clock.Playing=1;clock.Paused=0;clock.ElapsedSeconds=0;em.SetComponentData(session,clock);
                // Logical actor fixtures follow the actual compiled immutable
                // base roles; this does not claim real shared-prefab spawning.
                foreach(var structure in setup.Structures)
                {
                    if(!structure.DesignatedBase)continue;
                    var actor=em.CreateEntity(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishObjectiveRoleComponent),
                        typeof(SkirmishStructureIdentityComponent),typeof(Faction),typeof(UnitHealth),typeof(SkirmishSharedActorTag),typeof(LocalTransform));
                    em.SetComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=native.SessionId,
                        StableObjectId=new FixedString64Bytes(structure.StructureId+"."+structure.FactionId),FactionId=structure.FactionId,IsStructure=1});
                    em.SetComponentData(actor,new SkirmishObjectiveRoleComponent{FactionId=structure.FactionId,
                        StableObjectId=new FixedString64Bytes(structure.ObjectiveRoleId),Role=structure.FactionId==1?SkirmishObjectiveRoleKind.PlayerBase:SkirmishObjectiveRoleKind.EnemyBase});
                    em.SetComponentData(actor,new SkirmishStructureIdentityComponent{StructureId=new FixedString64Bytes(structure.StructureId),
                        Producer=SkirmishStructureIds.ProducerFor(structure.StructureId),DesignatedBase=1});
                    em.SetComponentData(actor,new Faction{Id=structure.FactionId});em.SetComponentData(actor,new UnitHealth{Current=800,Max=800});
                    float x=structure.FactionId==1?setup.PlayerBaseWorldX:setup.EnemyBaseWorldX;
                    float z=structure.FactionId==1?setup.PlayerBaseWorldZ:setup.EnemyBaseWorldZ;
                    em.SetComponentData(actor,LocalTransform.FromPosition(new Unity.Mathematics.float3(x,0,z)));
                }
                Assert.IsTrue(SkirmishNativeObjectiveLifecycleHooks.TryAttachAfterActualSpawn(em,session,compiled.Areas,
                    compiled.Areas.LogicalFingerprint,compiled.ViewFingerprint,out error),error);
                assertion(em,session);
            });
        }

        private static void AttachExplicitSyntheticLogicalBinding(SkirmishScenarioDefinitionConfig definition,
            SkirmishMissionLogicalAuthoringInputs inputs,List<UnityEngine.Object> owned)
        {
            var binding=ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();owned.Add(binding);
            var source=definition.CampaignMap;var expected=inputs.ActualAreaBinding;
            var areas=new SkirmishObjectiveAreaConfig{AreaSetId="synthetic-frontline-fixture",ObjectiveKind=SkirmishObjectiveKind.FrontlineControl,
                Binding=expected,Areas=new SkirmishObjectiveArea[3]};
            float width=expected.SourceMaxX-expected.SourceMinX,depth=expected.SourceMaxZ-expected.SourceMinZ;
            for(int i=0;i<3;i++)
            {
                float x=expected.SourceMinX+width*(.2f+i*.2f),z=expected.SourceMinZ+depth*.4f;
                areas.Areas[i]=new SkirmishObjectiveArea{StableAreaId="fixture.fc."+i,Role=(SkirmishObjectiveAreaRole)((int)SkirmishObjectiveAreaRole.FrontlineA+i),
                    Shape=SkirmishObjectiveAreaShape.AxisAlignedRectangle,Points=new[]{new SkirmishObjectiveAreaPoint(x,z),new SkirmishObjectiveAreaPoint(x+width*.05f,z+depth*.05f)}};
            }
            areas.Measurement=new SkirmishObjectiveAreaEvidence{ArtifactId="synthetic-unit-fixture-not-native-measurement",
                ArtifactSha256=new string('d',64),GeometryFingerprint=SkirmishObjectiveAreaCompiler.ComputeGeometryFingerprint(areas)};
            Assert.IsTrue(SkirmishObjectiveAreaCompiler.TryCompile(areas,expected,areas.ObjectiveKind,out var compiledAreas,out string error),error);
            var view=ScriptableObject.CreateInstance<SkirmishMissionViewProfile>();owned.Add(view);
            view.AuthoredNonBaseAssault=true;view.ObjectiveKind=areas.ObjectiveKind;view.CatalogId=definition.CatalogId;
            view.DefinitionId=definition.DefinitionId;view.DefinitionVersion=definition.ContentVersion;
            view.CampaignSourceContentHash=source.ContentHash;view.LayoutContentHash=definition.MapLayout.ContentHash;
            view.GroundReferenceY=inputs.SourceMinimumElevation;view.Pitch=55;view.Yaw=35;view.MaximumGroundCenterSlantDistance=500;
            view.MinHeight=15;view.NormalHeight=view.BuildHeight=view.MaxHeight=Mathf.Max(50,view.GroundReferenceY+30);
            view.NormalFieldOfView=46;view.BuildFieldOfView=50;view.InfantryMinimumViewportHeight=.017f;view.PlaneMaximumViewportDimension=.45f;
            view.AircraftPitch=50;view.AircraftFieldOfView=60;view.AircraftMinimumDistance=80;view.AircraftMaximumDistance=300;
            Assert.IsTrue(view.IsValid);
            var variant=new SkirmishMissionLogicalVariant{Size=inputs.Size,Difficulty=inputs.Difficulty,View=view,Areas=areas,
                ReviewedAreaLogicalFingerprint=compiledAreas.LogicalFingerprint,ReviewedGeometryFingerprint=compiledAreas.GeometryFingerprint,
                SourceMinimumElevation=inputs.SourceMinimumElevation,SourceMaximumElevation=inputs.SourceMaximumElevation,
                OpeningFocusRole=SkirmishRoleKind.Rifle,OpeningFocusStableActorId=inputs.ActualOpeningPlayerActorIds.First(id=>id.StartsWith(SkirmishRoleIds.Rifle+".",StringComparison.Ordinal)),
                OpeningFocusSmoothSeconds=2.25f};
            variant.Framing=inputs.RequiredFramingRoles.Select(role=>new SkirmishRoleFramingRequirement{Role=role,ObservePlayer=true,ObserveEnemy=true,
                MinimumSubjectElevation=inputs.SourceMinimumElevation,MaximumSubjectElevation=inputs.SourceMaximumElevation+100,
                MinimumViewportHeight=.017f,MaximumViewportDimension=.45f,SafeEdgeFraction=.03f,
                Stages=SkirmishViewCaptureStage.Opening|SkirmishViewCaptureStage.MinimumZoom|SkirmishViewCaptureStage.MaximumZoom|
                    SkirmishViewCaptureStage.MatchExitRestored|SkirmishViewCaptureStage.FollowParked|SkirmishViewCaptureStage.FollowCruising|SkirmishViewCaptureStage.ManualViewRestored,
                Evidence=SkirmishViewEvidenceRequirement.NativePng|SkirmishViewEvidenceRequirement.ActualMeshViewportBounds|
                    SkirmishViewEvidenceRequirement.ActualRenderKindAndRoot|SkirmishViewEvidenceRequirement.ObstructionCandidateInventory}).ToArray();
            variant.ReviewedViewFingerprint=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
            binding.CatalogId=definition.CatalogId;binding.DefinitionId=definition.DefinitionId;binding.DefinitionVersion=definition.ContentVersion;
            binding.ObjectiveKind=areas.ObjectiveKind;binding.SourceOperationMapId=source.OperationMapId;binding.SourceSceneGuid=source.SourceSceneReference.AssetGUID;
            binding.SourceContentHash=source.ContentHash;binding.SourceSemanticFingerprint=inputs.SourceSemanticFingerprint;binding.LayoutId=definition.MapLayout.LayoutId;
            binding.ReviewedLayoutSemanticFingerprint=inputs.LayoutSemanticFingerprint;binding.Variants=new[]{variant};definition.BindMissionLogicalBinding(binding);
        }
    }
}
#endif
