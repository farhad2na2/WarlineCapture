#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishMissionLogicalBindingTests
    {
        private static SkirmishResolvedSetup Setup()=>new SkirmishResolvedSetup {
            ObjectiveKind=SkirmishObjectiveKind.FrontlineControl,
            Forces=new[]{new SkirmishResolvedForceEntry {FactionId=1,RoleId=SkirmishRoleIds.Rifle,RoleKind=SkirmishRoleKind.Rifle,Quantity=4},
                         new SkirmishResolvedForceEntry {FactionId=2,RoleId=SkirmishRoleIds.Rifle,RoleKind=SkirmishRoleKind.Rifle,Quantity=4}},
            RoleOverlays=Array.Empty<SkirmishRoleOverlay>() };
        private static SkirmishRoleFramingRequirement Framing(SkirmishRoleKind role)=>new SkirmishRoleFramingRequirement {
            Role=role,ObservePlayer=true,ObserveEnemy=true,MinimumViewportHeight=.02f,MaximumViewportDimension=.4f,SafeEdgeFraction=.03f,
            MinimumSubjectElevation=-10,MaximumSubjectElevation=100,
            Stages=SkirmishViewCaptureStage.Opening|SkirmishViewCaptureStage.MinimumZoom|SkirmishViewCaptureStage.MaximumZoom|
                SkirmishViewCaptureStage.MatchExitRestored|SkirmishViewCaptureStage.FollowParked|SkirmishViewCaptureStage.FollowCruising|SkirmishViewCaptureStage.ManualViewRestored,
            Evidence=SkirmishViewEvidenceRequirement.NativePng|SkirmishViewEvidenceRequirement.ActualMeshViewportBounds|
                SkirmishViewEvidenceRequirement.ActualRenderKindAndRoot|SkirmishViewEvidenceRequirement.ObstructionCandidateInventory };
        private static SkirmishMissionLogicalVariant Variant()=>new SkirmishMissionLogicalVariant {
            OpeningFocusRole=SkirmishRoleKind.Rifle,OpeningFocusStableActorId=SkirmishRoleIds.Rifle+".1.0.0",OpeningFocusSmoothSeconds=2.25f,
            SourceMinimumElevation=-10,SourceMaximumElevation=20,Framing=new[]{Framing(SkirmishRoleKind.Rifle)} };

        [Test] public void MissingActualLogicalAssetRejectsNonBaWhileBaRemainsUnchanged()
        {
            Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(null,Setup(),out _,out _));
            Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(null,
                new SkirmishResolvedSetup {CatalogId="S005",ObjectiveKind=SkirmishObjectiveKind.BaseAssault},out var compiled,out _));Assert.IsNull(compiled);
            Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(null,
                new SkirmishResolvedSetup {CatalogId="S026",ObjectiveKind=SkirmishObjectiveKind.BaseAssault},out _,out _));
        }
        [Test] public void FramingRequiresActualOpeningAndRecruitRolesWithNormalViewEvidence()
        {
            var setup=Setup();var variant=Variant();Assert.IsTrue(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant.Framing[0].Evidence&=~SkirmishViewEvidenceRequirement.ObstructionCandidateInventory;
            Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant=Variant();variant.Framing[0].ObserveEnemy=false;Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant=Variant();variant.Framing[0].Stages&=~SkirmishViewCaptureStage.MaximumZoom;Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
        }
        [Test] public void FocusMustBeExactOwnedOpeningMemberAndUsesEstablishedSmoothFloor()
        {
            var setup=Setup();var variant=Variant();Assert.AreEqual(2.25f,RuntimeCameraFocusRequestUtility.MinimumMissionIntroSmoothTimeSeconds);
            variant.OpeningFocusStableActorId=SkirmishRoleIds.Rifle+".2.1.0";Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant.OpeningFocusStableActorId=SkirmishRoleIds.Rifle+".1.0.4";Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant=Variant();variant.OpeningFocusSmoothSeconds=0;Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
        }
        [Test] public void AirborneRoleNeedsDistinctElevationAndFollowRestorationStages()
        {
            var setup=Setup();setup.Forces=new[]{setup.Forces[0],new SkirmishResolvedForceEntry { FactionId=1,RoleId=SkirmishRoleIds.Drone,RoleKind=SkirmishRoleKind.Drone,Quantity=1 }};
            var variant=Variant();variant.Framing=new[]{Framing(SkirmishRoleKind.Rifle),Framing(SkirmishRoleKind.Drone)};
            Assert.IsTrue(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant.Framing[1].MaximumSubjectElevation=variant.Framing[1].MinimumSubjectElevation;Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant.Framing[1]=Framing(SkirmishRoleKind.Drone);variant.Framing[1].Stages&=~SkirmishViewCaptureStage.FollowCruising;
            Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
        }
        [Test] public void CameraRoleDigestIsOrderIndependentButBindsElevationFocusAndPixels()
        {
            var view=ScriptableObject.CreateInstance<SkirmishMissionViewProfile>();
            try
            {
                var variant=Variant();variant.View=view;variant.Framing=new[]{Framing(SkirmishRoleKind.Rifle),Framing(SkirmishRoleKind.Drone)};
                string baseline=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
                Array.Reverse(variant.Framing);Assert.AreEqual(baseline,SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant));
                variant.SourceMaximumElevation++;Assert.AreNotEqual(baseline,SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant));variant.SourceMaximumElevation--;
                variant.OpeningFocusStableActorId+=".changed";Assert.AreNotEqual(baseline,SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant));
                variant=Variant();variant.View=view;string original=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
                variant.Framing[0].MinimumViewportHeight=.03f;Assert.AreNotEqual(original,SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant));
            }
            finally {UnityEngine.Object.DestroyImmediate(view);}
        }
        [Test] public void DuplicateUnknownAndNonfiniteFramingDeclarationsReject()
        {
            var setup=Setup();var variant=Variant();variant.Framing=new[]{Framing(SkirmishRoleKind.Rifle),Framing(SkirmishRoleKind.Rifle)};
            Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant=Variant();variant.Framing[0].Role=(SkirmishRoleKind)255;Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
            variant=Variant();variant.Framing[0].MinimumViewportHeight=float.NaN;Assert.IsFalse(SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant,setup,out _));
        }
        [Test] public void SourceAndAreaLogicalDomainsRemainDistinctAndNativeReadIsFailClosed()
        {
            using var world=new World("MissingMissionLogicalProvider");var em=world.EntityManager;var session=em.CreateEntity();
            int before=em.UniversalQuery.CalculateEntityCount();
            Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out _));
            Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryQueueOpeningRoleFocus(em,session,out _));
            Assert.AreEqual(before,em.UniversalQuery.CalculateEntityCount());
        }
        [Test] public void S005EngineeringProfileIdentityAndExistingSourceDerivedValuesRemainIntact()
        {
            var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
            Assert.IsNotNull(definition);var profile=SkirmishMissionViewProfile.CreateS005EngineeringCandidate(definition);
            try
            {
                Assert.IsFalse(profile.AuthoredNonBaseAssault);Assert.IsTrue(profile.IsValid);
                Assert.AreEqual(definition.CampaignMap.SurfaceMetadata.MinimumHeight,profile.GroundReferenceY);
                Assert.AreEqual(55,profile.Pitch);Assert.AreEqual(46,profile.NormalFieldOfView);
                profile.AuthoredNonBaseAssault=true;profile.ObjectiveKind=SkirmishObjectiveKind.BaseAssault;Assert.IsFalse(profile.IsValid);
            }
            finally {UnityEngine.Object.DestroyImmediate(profile);}
        }
        [Test] public void GenericBaseAssaultProfileAppliesScopedZoomAndRejectsCachedValueDrift()
        {
            var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
            var profile=SkirmishMissionViewProfile.CreateS005EngineeringCandidate(definition);
            try
            {
                profile.CatalogId="S026";profile.DefinitionId="skirmish.s026";
                profile.AuthoredGenericBaseAssault=true;profile.ObjectiveKind=SkirmishObjectiveKind.BaseAssault;
                Assert.IsTrue(profile.IsValid);
                var state=new SelectionRuntimeConfigStartupSystemHelper.State();
                SkirmishMissionViewProfileApplication.Apply(profile,ref state);
                Assert.AreEqual(profile.MinHeight,state.MinZoomHeight);Assert.AreEqual(profile.MaxHeight,state.MaxZoomHeight);
                Assert.AreEqual(profile.NormalHeight,state.NormalModeZoomHeight);Assert.AreEqual(profile.Pitch,state.NormalModePitch);
                var variant=Variant();variant.View=profile;
                variant.ReviewedViewFingerprint=SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
                var constructor=typeof(SkirmishCompiledMissionLogicalBinding).GetConstructors(
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)[0];
                var compiled=(SkirmishCompiledMissionLogicalBinding)constructor.Invoke(new object[]{null,variant,new string('a',64)});
                Assert.IsTrue(compiled.MatchesCurrentViewValues());
                profile.NormalFieldOfView+=1;Assert.IsTrue(profile.IsValid);Assert.IsFalse(compiled.MatchesCurrentViewValues());
                profile.CatalogId="S005";profile.DefinitionId="skirmish.s005";Assert.IsFalse(profile.IsValid);
            }
            finally {UnityEngine.Object.DestroyImmediate(profile);}
        }
        [Test] public void ElevatedRoleFocusUsesExistingGroundPlaneCameraAndAbsoluteHeight()
        {
            using var world=new World("ElevatedMissionRoleFocus");
            var system=world.GetOrCreateSystemManaged<RtsCameraSystem>();
            var go=new GameObject("ElevatedMissionViewCamera");var camera=go.AddComponent<Camera>();
            try
            {
                var actor=new Vector3(30,9,80);const float height=44,pitch=55,yaw=35;
                Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(actor,height,pitch,yaw,out var ground));
                Assert.AreEqual(0,ground.y);
                system.ApplyPerspectiveCameraModeInstant(camera,height,pitch,yaw,46);
                system.SetSmoothFocusTarget(ground,true,2.25f);
                system.MoveCameraGroundCenterTo(camera,system.SmoothFocusTarget);
                var viewport=camera.WorldToViewportPoint(actor);
                Assert.Greater(viewport.z,0);Assert.AreEqual(.5f,viewport.x,.0001f);Assert.AreEqual(.5f,viewport.y,.0001f);
                Assert.AreEqual(height,camera.transform.position.y);Assert.AreEqual(ground,system.SmoothFocusTarget);
                Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(actor,9,pitch,yaw,out _));
                Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(actor,height,0,yaw,out _));
            }
            finally {UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void ActualQueuedGenericProviderRejectsViewAndLayoutDrift()
        {
            SkirmishGenericMissionDispatchTests.WithViewBoundQueuedSession((em,session)=>
            {
                Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var compiled));
                Assert.IsNull(compiled.Areas); // Generic BA does not require non-BA geometry.
                float fov=compiled.View.NormalFieldOfView;compiled.View.NormalFieldOfView+=1;
                Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out _));
                compiled.View.NormalFieldOfView=fov;
                Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out _));
                var definition=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition;
                var data=new UnityEditor.SerializedObject(definition.MapLayout);
                var origin=data.FindProperty("worldBinding").FindPropertyRelative("OriginX");
                origin.floatValue+=1;data.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out _));
            });
        }
        [Test] public void RenderBoundsObstructionCandidatesDoNotClaimRasterVisibility()
        {
            Assert.IsTrue(Game.Editor.SkirmishMissionViewVisibilityObservation.TrySegment(new Bounds(new Vector3(0,0,5),Vector3.one*2),
                Vector3.zero,new Vector3(0,0,10),out float entry,out float distance));Assert.AreEqual(4,entry,.001f);Assert.AreEqual(10,distance,.001f);
            Assert.IsFalse(Game.Editor.SkirmishMissionViewVisibilityObservation.TrySegment(new Bounds(new Vector3(0,0,20),Vector3.one*2),
                Vector3.zero,new Vector3(0,0,10),out _,out _));
            Assert.IsFalse(Game.Editor.SkirmishMissionViewVisibilityObservation.TrySegment(new Bounds(Vector3.zero,Vector3.one),
                Vector3.zero,new Vector3(float.NaN,0,10),out _,out _));
        }
        [Test] public void MissingNativeMeshRetainsFailureWithoutClaimingClearPixels()
        {
            using var world=new World("MissingNativeVisibilitySample");
            var sample=Game.Editor.SkirmishMissionViewVisibilityObservation.Capture(world.EntityManager,Entity.Null,null);
            Assert.IsFalse(sample.Actor.Valid);Assert.IsNotEmpty(sample.Actor.Failure);
            Assert.IsFalse(sample.RasterOcclusionMeasured);Assert.IsFalse(sample.ReadablePixelsAccepted);
            Assert.AreEqual(0,sample.PotentialObstructions.Length);
        }
    }
}
#endif
