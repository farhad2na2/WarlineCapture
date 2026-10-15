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
    public static class SkirmishMissionLogicalBindingSubjectTestFixture
    {
        public static void WithActiveBreakthrough(Action<EntityManager,Entity> assertion)
            =>WithActive(SkirmishObjectiveKind.Breakthrough,"S037",3037,assertion);
        public static void WithActiveConvoy(Action<EntityManager,Entity> assertion)
            =>WithActive(SkirmishObjectiveKind.ConvoyEscort,"S043",3043,assertion);
        private static void WithActive(SkirmishObjectiveKind kind,string catalogId,int index,Action<EntityManager,Entity> assertion)
        {
            SkirmishGenericMissionDispatchTests.WithSourceBoundUnqueuedDefinition(catalogId,(definition,owned)=>
            {
                var profiles=SkirmishExpansionCatalogConfig.Load().ToAuthoredSet();
                var objective=UnityEngine.Object.Instantiate(profiles.ObjectiveBa);owned.Add(objective);
                var serialized=new SerializedObject(objective);
                serialized.FindProperty("kind").intValue=(int)kind;
                serialized.FindProperty("objectiveId").stringValue=kind==SkirmishObjectiveKind.Breakthrough?"BT":"CE";
                serialized.FindProperty("standardDeadlineSeconds").intValue=1080;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                definition.Configure(catalogId,objective,profiles.ArmyGround,profiles.StartField,
                    profiles.Economy,profiles.Upgrades,profiles.Intel,
                    kind==SkirmishObjectiveKind.ConvoyEscort?RequireConvoyRoles():profiles.Roles,definition.MapLayout,
                    featureIds:new[]{"ground","intel","transport","advanced_ground",kind==SkirmishObjectiveKind.Breakthrough?"objective_bt":"objective_ce"});
                SkirmishObjectiveApproachTowerTests.AttachSyntheticDefensePads(definition.MapLayout);
                if(kind==SkirmishObjectiveKind.ConvoyEscort)AttachSyntheticConvoyBays(definition.MapLayout);
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out string error),error);
                var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};
                Assert.IsTrue(SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(definition,
                    SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104729,manifest,matrix,out var inputs,out var reasons),string.Join(";",reasons));
                AttachExplicitSyntheticLogicalBinding(definition,inputs,owned,kind);
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,
                    SkirmishSizeId.Standard,104729,manifest,matrix,out var setup,out reasons),string.Join(";",reasons));

                var actualCatalog=SkirmishExpansionCatalogConfig.Load();
                var authored=UnityEngine.Object.Instantiate(actualCatalog);owned.Add(authored);
                serialized=new SerializedObject(authored);var definitions=serialized.FindProperty("definitions");
                definitions.arraySize=1;definitions.GetArrayElementAtIndex(0).objectReferenceValue=definition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var library=ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();owned.Add(library);

                library.Configure(new[]{new SkirmishBattleCatalogEntry{ScenarioId=catalogId,DefinitionId=definition.DefinitionId,
                    ContentVersion=definition.ContentVersion,Status=SkirmishBattleCatalogStatus.Playable,PlayableScenarioIndex=index}});
                using var authoredScope=SkirmishExpansionCatalogConfig.UseEditorValidationCatalog(authored);
                using var libraryScope=SkirmishBattleCatalogConfig.UseEditorValidationCatalog(library);
                using var world=new World("ActuallyProviderBoundSyntheticFrontline");world.SetTime(new TimeData(1,.1f));var em=world.EntityManager;
                var config=QuickGameConfig.Defaults;config.ScenarioIndex=index;config.MapSeed=104729;
                Assert.IsTrue(SkirmishLaunchProjection.TryQueue(em,config));
                using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));var session=sessions.GetSingletonEntity();
                Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var compiled));
                Assert.AreEqual(setup.SetupHash,em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup.SetupHash);
                if(!em.HasComponent<SkirmishObjectiveStateComponent>(session))em.AddComponentData(session,new SkirmishObjectiveStateComponent{Kind=setup.ObjectiveKind,State=SkirmishObjectiveStateKind.Active});
                var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);native.SpawnComplete=1;native.Phase=SkirmishSessionPhase.Playing;em.SetComponentData(session,native);
                if(!em.HasComponent<SkirmishObjectiveClockComponent>(session))em.AddComponentData(session,new SkirmishObjectiveClockComponent{DeadlineSeconds=setup.DeadlineSeconds});
                var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);clock.Playing=1;clock.Paused=0;clock.ElapsedSeconds=0;em.SetComponentData(session,clock);
                // Exercise the ACTUAL immutable compiled original plan and native
                // designation hook. These are disposable component fixtures, not
                // instantiated baked prefabs or a source-fit certificate.
                Assert.IsTrue(SkirmishOriginalObjectiveSpawnHooks.TryPlan(setup,out var plan,out error),error);
                Assert.AreEqual(kind==SkirmishObjectiveKind.Breakthrough?12:3,plan.Length);
                foreach(var original in plan)
                {
                    var force=setup.Forces[original.ForceIndex];var actor=em.CreateEntity();
                    em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{SessionId=native.SessionId,
                        StableObjectId=new FixedString64Bytes(original.StableActorId),FactionId=1});
                    em.AddComponent<SkirmishSharedActorTag>(actor);em.AddComponentData(actor,new Faction{Id=1});
                    em.AddComponentData(actor,new SkirmishUnitRoleComponent{Role=force.RoleKind,
                        Category=SkirmishRoleIds.Category(force.RoleKind),SupplyCost=force.RoleKind==SkirmishRoleKind.Rifle?1:0});
                    em.AddComponentData(actor,new UnitSourcePrefabKey{Value=new FixedString64Bytes(force.RuntimePrefabKey)});
                    var source=definition.CampaignMap.Bounds;
                    em.AddComponentData(actor,LocalTransform.FromPosition(new Unity.Mathematics.float3(
                        (source.WorldMin.x+source.WorldMax.x)*.5f,inputs.SourceMinimumElevation,
                        (source.WorldMin.z+source.WorldMax.z)*.5f)));
                    if(kind==SkirmishObjectiveKind.ConvoyEscort)
                    {
                        Assert.IsTrue(SkirmishConvoySpawnHooks.TryResolveDefinition(setup,out var truck,out error),error);
                        em.AddComponentData(actor,new UnitHealth{Current=truck.ChassisConfig.MaxHealth,Max=truck.ChassisConfig.MaxHealth});
                        em.AddComponentData(actor,new UnitMove{Speed=truck.ChassisConfig.Speed});
                        em.AddComponentData(actor,new UnitFootprint{Size=new Unity.Mathematics.int2(3,3)});
                        em.AddComponentData(actor,new UnitMovementBehavior{UsesVehicleMotion=1,AllowIdleWander=1});
                        byte slot=(byte)(Array.FindIndex(plan,row=>row.StableActorId==original.StableActorId)+1);
                        Assert.IsTrue(SkirmishConvoyTruckRuntime.TrySpecializeFreshInstance(em,actor,truck,native.SessionId,
                            original.StableActorId,slot,out error),error);
                        Assert.IsTrue(SkirmishConvoyTruckRuntime.ValidateSpecialized(em,actor,truck,out error),error);
                    }
                    else em.AddComponentData(actor,new UnitHealth{Current=100,Max=100});
                    Assert.IsTrue(SkirmishOriginalObjectiveSpawnHooks.TryTagActualOpeningMember(em,actor,native.SessionId,
                        setup,original.ForceIndex,original.MemberIndex,out error),error);
                }
                Assert.IsTrue(SkirmishNativeObjectiveLifecycleHooks.TryAttachAfterActualSpawn(em,session,compiled.Areas,
                    compiled.Areas.LogicalFingerprint,compiled.ViewFingerprint,out error),error);
                assertion(em,session);
            });
        }

        private static SkirmishRoleCatalogConfig RequireConvoyRoles()
        {
            var definition=SkirmishConvoyTruckDefinition.Load();
            Assert.IsNotNull(definition,"Root must materialize the actual mode-owned logical Cargo Truck definition before CE fixture execution.");
            Assert.IsTrue(definition.IsValid);
            var roles=Resources.Load<SkirmishRoleCatalogConfig>("SkirmishConvoyRoleCatalog");Assert.IsNotNull(roles);
            Assert.IsTrue(roles.TryGet(SkirmishRoleIds.ConvoyLogic,out var role));
            Assert.AreEqual(SkirmishRoleKind.ConvoyLogic,role.Kind);return roles;
        }

        private static void AttachSyntheticConvoyBays(SkirmishMapLayoutConfig layout)
        {
            // Synthetic non-overlapping local bays for compiler/identity controls.
            // No assertion of real terrain, placement, road or departure clearance.
            var data=new SerializedObject(layout);var anchors=data.FindProperty("anchors");var pads=data.FindProperty("pads");
            for(int i=0;i<3;i++)
            {
                string role="convoy.origin."+(i+1);float u=.2f+i*.2f,v=.65f;
                float x=layout.OriginX+u*layout.WorldWidthMetres,z=layout.OriginZ+v*layout.WorldDepthMetres;
                int index=anchors.arraySize;anchors.arraySize++;var anchor=anchors.GetArrayElementAtIndex(index);
                anchor.FindPropertyRelative("AnchorId").stringValue=role;anchor.FindPropertyRelative("RoleId").stringValue=role;
                anchor.FindPropertyRelative("NormalizedU").floatValue=u;anchor.FindPropertyRelative("NormalizedV").floatValue=v;
                anchor.FindPropertyRelative("WorldX").floatValue=x;anchor.FindPropertyRelative("WorldZ").floatValue=z;
                index=pads.arraySize;pads.arraySize++;var pad=pads.GetArrayElementAtIndex(index);
                pad.FindPropertyRelative("PadId").stringValue=role;pad.FindPropertyRelative("AnchorId").stringValue=role;
                pad.FindPropertyRelative("RoleId").stringValue=role;pad.FindPropertyRelative("Kind").intValue=(int)SkirmishLegalPadKind.ObjectiveTruckSpawn;
                pad.FindPropertyRelative("CenterX").floatValue=x;pad.FindPropertyRelative("CenterZ").floatValue=z;
                pad.FindPropertyRelative("WidthMetres").floatValue=layout.WorldWidthMetres*.05f;
                pad.FindPropertyRelative("DepthMetres").floatValue=layout.WorldDepthMetres*.05f;
                pad.FindPropertyRelative("FactionId").intValue=1;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachExplicitSyntheticLogicalBinding(SkirmishScenarioDefinitionConfig definition,
            SkirmishMissionLogicalAuthoringInputs inputs,List<UnityEngine.Object> owned,SkirmishObjectiveKind kind)
        {
            var binding=ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();owned.Add(binding);
            var source=definition.CampaignMap;var expected=inputs.ActualAreaBinding;
            var areas=new SkirmishObjectiveAreaConfig{AreaSetId="synthetic-subject-fixture",ObjectiveKind=kind,
                Binding=expected,Areas=new SkirmishObjectiveArea[kind==SkirmishObjectiveKind.Breakthrough?3:1]};
            float width=expected.SourceMaxX-expected.SourceMinX,depth=expected.SourceMaxZ-expected.SourceMinZ;
            for(int i=0;i<areas.Areas.Length;i++)
            {
                float x=expected.SourceMinX+width*(.2f+i*.2f),z=expected.SourceMinZ+depth*.4f;
                areas.Areas[i]=new SkirmishObjectiveArea{StableAreaId="fixture.subject."+i,Role=kind==SkirmishObjectiveKind.Breakthrough?
                    (SkirmishObjectiveAreaRole)((int)SkirmishObjectiveAreaRole.BreakthroughCorridorA+i):SkirmishObjectiveAreaRole.ConvoyDestination,
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
            variant.Framing=inputs.RequiredFramingRoles.Select(role=>new SkirmishRoleFramingRequirement{Role=role,ObservePlayer=true,ObserveEnemy=role!=SkirmishRoleKind.ConvoyLogic,
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
