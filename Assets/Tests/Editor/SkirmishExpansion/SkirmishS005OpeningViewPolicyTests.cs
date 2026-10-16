#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishS005OpeningViewPolicyTests
    {
        private sealed class Fixture:IDisposable
        {
            public readonly World World=new World("S005OpeningLogicalViewFixture");
            public EntityManager Em=>World.EntityManager;
            public readonly Entity Session,Actor;
            public readonly SkirmishResolvedSetup Setup;
            public readonly SkirmishMissionViewProfile View;
            public Fixture(int seed=155926)
            {
                var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,seed,
                    new SkirmishContentManifest {RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out Setup,out var reasons),string.Join(";",reasons));
                View=SkirmishMissionViewProfile.CreateS005EngineeringCandidate(definition);
                View.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.S005FirstOpeningOwnedRifle;
                Session=Em.CreateEntity();
                var native=new SkirmishExpandedSessionComponent {CatalogId=Setup.CatalogId,DefinitionId=Setup.DefinitionId,
                    ContentVersion=Setup.ContentVersion,SetupHash=Setup.SetupHash,ObjectiveKind=Setup.ObjectiveKind,SessionId="s005-opening-fixture",
                    Phase=SkirmishSessionPhase.Playing,SpawnComplete=1,SizeId=Setup.SizeId,DifficultyId=Setup.DifficultyId,Seed=Setup.Seed};
                Em.AddComponentData(Session,native);Em.AddComponentData(Session,new SkirmishObjectiveClockComponent {Playing=1});
                Em.AddComponentObject(Session,new SkirmishResolvedSetupRecord {Setup=Setup});
                Em.AddComponentObject(Session,new SkirmishLaunchDefinitionRecord(definition,Setup,false));
                string id=null;
                for(int i=0;i<Setup.Forces.Length;i++)if(Setup.Forces[i].FactionId==1&&Setup.Forces[i].RoleKind==SkirmishRoleKind.Rifle&&Setup.Forces[i].Quantity>0)
                {id=Setup.Forces[i].RoleId+".1."+i+".0";break;}
                Assert.IsNotNull(id);Actor=Em.CreateEntity();
                Em.AddComponentData(Actor,new SkirmishAttemptOwnedComponent {SessionId=native.SessionId,StableObjectId=new FixedString64Bytes(id),FactionId=1});
                Em.AddComponentData(Actor,new Faction {Id=1});Em.AddComponentData(Actor,new UnitHealth {Current=100,Max=100});
                Em.AddComponentData(Actor,new SkirmishUnitRoleComponent {Role=SkirmishRoleKind.Rifle,Category=SkirmishRoleIds.Category(SkirmishRoleKind.Rifle)});
                Em.AddComponent<SkirmishSharedActorTag>(Actor);Em.AddComponentData(Actor,LocalTransform.FromPosition(new float3(30,9,80)));
            }
            public void Dispose(){World.Dispose();UnityEngine.Object.DestroyImmediate(View);}
        }
        [Test] public void ActualS005SetupGetsOneRoleDerivedNormalCameraRequestWithoutIdentityChange()
        {
            using var f=new Fixture();uint hash=f.Setup.SetupHash;
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out string error),error);
            using var requests=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));var request=requests.GetSingleton<RuntimeCameraFocusRequestComponent>();
            Assert.AreEqual(1,request.Requested);Assert.AreEqual(1,request.UseExplicitPerspective);Assert.AreEqual(f.View.NormalHeight,request.Perspective.x);
            Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(new Vector3(30,9,80),f.View.NormalHeight,f.View.Pitch,f.View.Yaw,out var ground));
            Assert.Less(math.distance(request.World,(float3)ground),.0001f);Assert.IsTrue(SkirmishS005OpeningViewPolicy.OwnsIntroduction(f.Em,f.Session));
            request.Requested=0;f.Em.SetComponentData(requests.GetSingletonEntity(),request);
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out error),error);
            Assert.AreEqual(0,requests.GetSingleton<RuntimeCameraFocusRequestComponent>().Requested);Assert.AreEqual(hash,f.Setup.SetupHash);
            Assert.AreEqual(new float3(30,9,80),f.Em.GetComponentData<LocalTransform>(f.Actor).Position);
        }
        [Test] public void PaidForeignDeadMountedDisabledOrDuplicateOriginalCannotSupplyOpeningFocus()
        {
            for(int mutation=0;mutation<7;mutation++)
            {
                using var f=new Fixture();var owner=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(f.Actor);
                if(mutation==0){owner.ReservationId=1;f.Em.SetComponentData(f.Actor,owner);}
                if(mutation==1)f.Em.SetComponentData(f.Actor,new Faction {Id=2});
                if(mutation==2)f.Em.SetComponentData(f.Actor,new UnitHealth {Current=0,Max=100});
                if(mutation==3)f.Em.AddComponentData(f.Actor,new Parent {Value=f.Em.CreateEntity()});
                if(mutation==4)f.Em.AddComponent<Disabled>(f.Actor);
                if(mutation==5)f.Em.AddComponent<UnitTransportCargoPassenger>(f.Actor);
                if(mutation==6){var duplicate=f.Em.CreateEntity();f.Em.AddComponentData(duplicate,owner);}
                Assert.IsFalse(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out _),"mutation "+mutation);
                Assert.IsFalse(f.Em.HasComponent<SkirmishS005OpeningFocusOwner>(f.Session));
            }
        }
        [Test] public void LateOrChangedNativeSessionCannotClaimTheOpeningIntroduction()
        {
            using var f=new Fixture();var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.ElapsedSeconds=1;f.Em.SetComponentData(f.Session,clock);
            Assert.IsFalse(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out _));
            clock.ElapsedSeconds=0;f.Em.SetComponentData(f.Session,clock);
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out _));
            var native=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);native.SessionId="foreign-attempt";f.Em.SetComponentData(f.Session,native);
            Assert.IsFalse(SkirmishS005OpeningViewPolicy.OwnsIntroduction(f.Em,f.Session));
        }
        [Test] public void ScopedPolicyCannotBeAppliedToGenericOrCampaignProfiles()
        {
            using var f=new Fixture();Assert.IsTrue(f.View.IsValid);
            f.View.AuthoredGenericBaseAssault=true;Assert.IsFalse(f.View.IsValid);
            f.View.AuthoredGenericBaseAssault=false;f.View.CatalogId="S026";f.View.DefinitionId="skirmish.s026";Assert.IsFalse(f.View.IsValid);
        }
        [Test] public void MissingOrUnconfiguredLogicalProfilePreservesExistingStartup()
        {
            using var f=new Fixture();
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueScopedCandidate(f.Em,f.Session,null,out _));
            f.View.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.None;
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueScopedCandidate(f.Em,f.Session,f.View,out _));
            Assert.IsFalse(f.Em.HasComponent<SkirmishS005OpeningFocusOwner>(f.Session));
            using var requests=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            Assert.AreEqual(0,requests.CalculateEntityCount());
        }
        static Entity[] Formation(Fixture f)
        {
            f.View.OpeningFocusStableActorId="role.rifle.1.0.4";
            var template=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(f.Actor);
            var members=new Entity[SkirmishRoleIds.InfantrySquadMembers];
            for(int i=0;i<members.Length;i++)
            {
                var actor=f.Em.Instantiate(f.Actor);members[i]=actor;var own=template;
                own.StableObjectId=new FixedString64Bytes("role.rifle.1.0."+(4+i));f.Em.SetComponentData(actor,own);
                f.Em.SetComponentData(actor,LocalTransform.FromPosition(100+i*2,2,200+i*4));
                f.Em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent {GroupId=77,Domain=SkirmishPopulationCategory.Infantry});
            }
            f.Em.AddBuffer<SkirmishArmyGroupRecord>(f.Session).Add(new SkirmishArmyGroupRecord {
                GroupId=77,FactionId=1,Role=SkirmishRoleKind.Rifle,Domain=SkirmishPopulationCategory.Infantry,MemberCount=members.Length,AliveCount=members.Length});
            return members;
        }
        [Test] public void ExplicitOwnedHintUsesCompleteActualFormationCentroidAndQueuesOnlyOnce()
        {
            using var f=new Fixture();var members=Formation(f);uint hash=f.Setup.SetupHash;
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out string error),error);
            Assert.IsTrue(SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(new Vector3(103,2,206),f.View.NormalHeight,f.View.Pitch,f.View.Yaw,out var ground));
            using var requests=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));var entity=requests.GetSingletonEntity();
            var request=f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(entity);
            Assert.Less(math.distance(request.World,(float3)ground),.0001f);
            Assert.AreEqual("role.rifle.1.0.4",f.Em.GetComponentData<SkirmishS005OpeningFocusOwner>(f.Session).ActorId.ToString());
            request.Requested=0;f.Em.SetComponentData(entity,request);
            f.Em.SetComponentData(members[0],LocalTransform.FromPosition(500,2,500));
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out error),error);
            Assert.AreEqual(0,f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(entity).Requested);
            Assert.AreEqual(hash,f.Setup.SetupHash);Assert.AreEqual(0,f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session)[0].Selected);
            Assert.AreEqual(SkirmishGroupOrderKind.None,f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session)[0].LastOrder);
        }
        [Test] public void HintFormationRejectsIncompleteForeignPaidDuplicateMountedAndInvalidMembers()
        {
            for(int mutation=0;mutation<18;mutation++)
            {
                using var f=new Fixture();var members=Formation(f);var actor=members[1];var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(mutation==0)f.Em.DestroyEntity(actor);
                if(mutation==1){own.SessionId="foreign";f.Em.SetComponentData(actor,own);}
                if(mutation==2){own.ReservationId=1;f.Em.SetComponentData(actor,own);}
                if(mutation==3){own.StableObjectId="role.rifle.1.0.4";f.Em.SetComponentData(actor,own);}
                if(mutation==4)f.Em.AddComponent<UnitTransportPassenger>(actor);
                if(mutation==5)f.Em.AddComponent<Disabled>(actor);
                if(mutation==6)f.Em.SetComponentData(actor,new UnitHealth {Current=0,Max=100});
                if(mutation==7)f.Em.SetComponentData(actor,LocalTransform.FromPosition(float.NaN,0,0));
                if(mutation==8)f.Em.SetComponentData(actor,new Faction {Id=2});
                if(mutation==9)f.Em.SetComponentData(actor,new SkirmishArmyGroupMembershipComponent {GroupId=78,Domain=SkirmishPopulationCategory.Infantry});
                if(mutation==10){var groups=f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session);var record=groups[0];record.AliveCount=3;groups[0]=record;}
                if(mutation==11)f.Em.Instantiate(actor);
                if(mutation==12)f.View.OpeningFocusStableActorId="role.rifle.1.0.999";
                if(mutation==13){var member=f.Em.GetComponentData<SkirmishArmyGroupMembershipComponent>(members[0]);member.GroupId=0;f.Em.SetComponentData(members[0],member);}
                if(mutation==14)f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session).Add(f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session)[0]);
                if(mutation==15)f.Em.AddComponentData(actor,new Parent {Value=f.Actor});
                if(mutation==16)f.Em.AddComponent<PostTransformMatrix>(actor);
                if(mutation==17)f.Em.SetComponentData(actor,new SkirmishUnitRoleComponent {Role=SkirmishRoleKind.Rifle,Category=SkirmishPopulationCategory.Ground});
                Assert.IsFalse(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out _),"formation mutation "+mutation);
                Assert.IsFalse(f.Em.HasComponent<SkirmishS005OpeningFocusOwner>(f.Session));
                using var requests=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));Assert.AreEqual(0,requests.CalculateEntityCount());
            }
        }
        [Test] public void GroundCandidatePreservesEffectiveAircraftPoseAndRejectsOverrideOnGenericProfiles()
        {
            using var f=new Fixture();var bounds=new Bounds(new Vector3(100,20,100),new Vector3(30,8,20));
            Assert.IsTrue(SkirmishMissionViewProfileApplication.TryAircraftPose(bounds,f.View,20f/9f,.3f,out var before,out var rotation));
            f.View.UseSeparateAircraftYaw=true;f.View.AircraftYaw=f.View.Yaw;f.View.Yaw=307;
            Assert.IsTrue(SkirmishMissionViewProfileApplication.TryAircraftPose(bounds,f.View,20f/9f,.3f,out var after,out var nextRotation));
            Assert.Less(Vector3.Distance(before,after),.0001f);Assert.Less(Quaternion.Angle(rotation,nextRotation),.0001f);
            Assert.AreEqual(50,f.View.AircraftPitch);Assert.AreEqual(60,f.View.AircraftFieldOfView);Assert.AreEqual(80,f.View.AircraftMinimumDistance);
            Assert.AreEqual(300,f.View.AircraftMaximumDistance);Assert.AreEqual(.45f,f.View.PlaneMaximumViewportDimension);
            f.View.AircraftYaw=float.NaN;Assert.IsFalse(f.View.IsValid);f.View.AircraftYaw=35;
            f.View.OpeningFocusStableActorId=null;f.View.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.None;
            f.View.AuthoredGenericBaseAssault=true;f.View.CatalogId="S026";f.View.DefinitionId="skirmish.s026";
            f.View.ObjectiveKind=SkirmishObjectiveKind.BaseAssault;Assert.IsFalse(f.View.IsValid);
            f.View.UseSeparateAircraftYaw=false;Assert.IsTrue(f.View.IsValid);
        }
        [Test] public void AllThreePacketSeedsAdmitHintAgainstActualCompiledForces()
        {
            var candidate=SkirmishMissionViewProfile.Load("S005");Assert.IsNotNull(candidate);Assert.IsTrue(candidate.IsValid);
            Assert.AreEqual("role.rifle.1.0.4",candidate.OpeningFocusStableActorId);Assert.AreEqual(20,candidate.NormalHeight);
            Assert.AreEqual(65,candidate.Pitch);Assert.AreEqual(307,candidate.Yaw);Assert.AreEqual(46,candidate.NormalFieldOfView);
            Assert.IsTrue(candidate.UseSeparateAircraftYaw);Assert.AreEqual(35,candidate.EffectiveAircraftYaw);
            foreach(int seed in new[]{104734,130368,155926})
            {
                using var f=new Fixture(seed);Formation(f);
                f.View.NormalHeight=candidate.NormalHeight;f.View.MinHeight=candidate.MinHeight;f.View.MaxHeight=candidate.MaxHeight;
                f.View.BuildHeight=candidate.BuildHeight;f.View.Pitch=candidate.Pitch;f.View.Yaw=candidate.Yaw;
                f.View.NormalFieldOfView=candidate.NormalFieldOfView;f.View.UseSeparateAircraftYaw=candidate.UseSeparateAircraftYaw;
                f.View.AircraftYaw=candidate.AircraftYaw;f.View.GroundReferenceY=candidate.GroundReferenceY;
                f.View.MaximumGroundCenterSlantDistance=candidate.MaximumGroundCenterSlantDistance;
                Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueValidated(f.Em,f.Session,f.View,out string error),"seed "+seed+": "+error);
            }
        }
        public static void RunFocusedValidation()
        {
            var t=new SkirmishS005OpeningViewPolicyTests();
            t.ActualS005SetupGetsOneRoleDerivedNormalCameraRequestWithoutIdentityChange();
            t.PaidForeignDeadMountedDisabledOrDuplicateOriginalCannotSupplyOpeningFocus();
            t.LateOrChangedNativeSessionCannotClaimTheOpeningIntroduction();t.ScopedPolicyCannotBeAppliedToGenericOrCampaignProfiles();
            t.MissingOrUnconfiguredLogicalProfilePreservesExistingStartup();
            t.ExplicitOwnedHintUsesCompleteActualFormationCentroidAndQueuesOnlyOnce();
            t.HintFormationRejectsIncompleteForeignPaidDuplicateMountedAndInvalidMembers();
            t.GroundCandidatePreservesEffectiveAircraftPoseAndRejectsOverrideOnGenericProfiles();
            t.AllThreePacketSeedsAdmitHintAgainstActualCompiledForces();
            Debug.Log("[SkirmishS005OpeningViewPolicyTests] result=Passed tests=9 normalNativeViewAccepted=0");
        }
    }
}
#endif
