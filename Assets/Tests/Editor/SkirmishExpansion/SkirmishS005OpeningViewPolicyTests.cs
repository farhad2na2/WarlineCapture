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
            public Fixture(int seed=155926,string catalogId="S005")
            {
                var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                    "Assets/Game/Configs/SkirmishExpansion/Scenarios/"+catalogId+"/SkirmishScenario_"+catalogId+".asset");
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,seed,
                    new SkirmishContentManifest {RequiredFeatureIds=definition.RequiredFeatureIds},matrix,out Setup,out var reasons),string.Join(";",reasons));
                View=catalogId=="S005"?SkirmishMissionViewProfile.CreateS005EngineeringCandidate(definition):UnityEngine.Object.Instantiate(SkirmishMissionViewProfile.Load(catalogId));
                if(catalogId=="S005")View.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.S005FirstOpeningOwnedRifle;
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
        [Test] public void GenericProfileIdentityIsDataDrivenAndDoesNotAdmitAnUnboundLegacyProfile()
        {
            using var f=new Fixture();
            f.View.ActiveOperationMapId="opmap.fixture";f.View.ActiveMapContentHash="content";f.View.ActiveMapSourceIdentityHash="source";
            f.View.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.FirstOpeningOwnedRifleGroup;
            for(int i=1;i<=120;i++)
            {
                f.View.CatalogId="S"+i.ToString("D3");f.View.DefinitionId="skirmish."+f.View.CatalogId.ToLowerInvariant();
                Assert.IsTrue(f.View.IsValid,f.View.CatalogId);
            }
            f.View.ActiveMapSourceIdentityHash=null;Assert.IsFalse(f.View.IsValid);
            f.View.ActiveMapSourceIdentityHash="source";f.View.CatalogId="S121";Assert.IsFalse(f.View.IsValid);
        }
        [Test] public void ActiveMapProfileRejectsWrongSourceContentGenerationAndDefinition()
        {
            using var f=new Fixture();var definition=f.Em.GetComponentObject<SkirmishLaunchDefinitionRecord>(f.Session).Definition;
            f.View.ActiveOperationMapId=f.Setup.OperationMapId;f.View.ActiveMapContentHash="actual-content";f.View.ActiveMapSourceIdentityHash="actual-source";
            f.View.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.FirstOpeningOwnedRifleGroup;
            Assert.IsNotNull(definition.ObjectiveConfig);
            Assert.AreEqual(definition.ObjectiveConfig.Kind,f.Setup.ObjectiveKind,"Active-map fixture must retain the actual compiled objective.");
            f.View.ObjectiveKind=f.Setup.ObjectiveKind;
            using var builder=new BlobBuilder(Allocator.Temp);ref var root=ref builder.ConstructRoot<OperationMapBlob>();
            root.OperationMapId=f.Setup.OperationMapId;root.ContentHash="actual-content";root.SourceIdentityHash="actual-source";
            var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
            try
            {
                var map=f.Em.CreateEntity();f.Em.AddComponentData(map,new ActiveOperationMapComponent {OperationMapId=f.Setup.OperationMapId,Generation=7});
                var metadata=new OperationMapMetadataComponent {Blob=blob,Generation=7,PhysicalSourceValidated=1};f.Em.AddComponentData(map,metadata);
                Assert.IsTrue(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View));
                f.View.ObjectiveKind=SkirmishObjectiveKind.None;
                Assert.IsFalse(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View),"Wrong objective cannot pass active-map source identity.");
                f.View.ObjectiveKind=f.Setup.ObjectiveKind;
                Assert.IsTrue(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View),"Restoring the actual objective must restore the positive match.");
                f.View.ActiveMapContentHash="foreign";Assert.IsFalse(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View));f.View.ActiveMapContentHash="actual-content";
                f.View.ActiveMapSourceIdentityHash="foreign";Assert.IsFalse(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View));f.View.ActiveMapSourceIdentityHash="actual-source";
                metadata.Generation=8;f.Em.SetComponentData(map,metadata);Assert.IsFalse(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View));
                metadata.Generation=7;metadata.PhysicalSourceValidated=0;f.Em.SetComponentData(map,metadata);Assert.IsFalse(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View));
                metadata.PhysicalSourceValidated=1;f.Em.SetComponentData(map,metadata);f.View.DefinitionVersion++;Assert.IsFalse(SkirmishMissionViewProfileApplication.MatchesSource(f.Em,f.Setup,definition,f.View));
            }
            finally {blob.Dispose();}
        }
        [Test] public void GenericOpeningUsesOnlyCompleteActualActivePlayerRifleGroup()
        {
            foreach(byte faction in new byte[]{1,2})for(int mutation=0;mutation<4;mutation++)
            {
                using var f=new Fixture();f.Setup.PlayerFaction=faction;
                for(int i=0;i<f.Setup.Forces.Length;i++)if(f.Setup.Forces[i].RoleKind==SkirmishRoleKind.Rifle&&f.Setup.Forces[i].FactionId==1)f.Setup.Forces[i].FactionId=faction;
                var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(f.Actor);string prefix=own.StableObjectId.ToString();prefix=prefix.Substring(0,prefix.LastIndexOf('.')+1).Replace(".1.","."+faction+".");
                var members=new Entity[4];
                for(int i=0;i<4;i++)
                {
                    var actor=i==0?f.Actor:f.Em.Instantiate(f.Actor);members[i]=actor;own.FactionId=faction;own.StableObjectId=new FixedString64Bytes(prefix+i);
                    f.Em.SetComponentData(actor,own);f.Em.SetComponentData(actor,new Faction {Id=faction});f.Em.SetComponentData(actor,LocalTransform.FromPosition(100+i*2,2,200+i*4));
                    if(!f.Em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))f.Em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent {GroupId=77,Domain=SkirmishPopulationCategory.Infantry});
                }
                f.Em.AddBuffer<SkirmishArmyGroupRecord>(f.Session).Add(new SkirmishArmyGroupRecord {GroupId=77,FactionId=faction,Role=SkirmishRoleKind.Rifle,Domain=SkirmishPopulationCategory.Infantry,MemberCount=4,AliveCount=4});
                f.View.OpeningFocusPolicy=SkirmishMissionOpeningFocusPolicy.FirstOpeningOwnedRifleGroup;
                if(mutation==1)f.Em.DestroyEntity(members[3]);
                if(mutation==2){var paid=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(members[2]);paid.ReservationId=1;f.Em.SetComponentData(members[2],paid);}
                if(mutation==3)f.Em.SetComponentData(members[2],new Faction {Id=(byte)(3-faction)});
                var native=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);
                bool found=SkirmishS005OpeningViewPolicy.TryFindOpeningFocus(f.Em,f.Session,native,f.Setup,f.View,out _,out var center);
                Assert.AreEqual(mutation==0,found);
                if(found)Assert.Less(math.distance(center,new float3(103,2,206)),.0001f);
                Assert.AreEqual(0,f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session)[0].Selected);
                Assert.AreEqual(SkirmishGroupOrderKind.None,f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session)[0].LastOrder);
            }
        }
        [Test] public void S002ProfilePinsExistingDefinitionLayoutAndActualCompatibilitySourceOnly()
        {
            var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset");
            var view=SkirmishMissionViewProfile.Load("S002");Assert.IsNotNull(view);Assert.IsTrue(view.IsValid);
            Assert.IsNull(definition.CampaignMap,"Camera opt-in must not bind a new physical Campaign source.");
            var setup=new SkirmishResolvedSetup {CatalogId=definition.CatalogId,DefinitionId=definition.DefinitionId,ContentVersion=definition.ContentVersion,
                OperationMapId=definition.OperationMapId,LayoutId=definition.MapLayoutId,ObjectiveKind=definition.ObjectiveConfig.Kind};
            Assert.IsTrue(view.MatchesActiveMap(setup,definition,view.ActiveOperationMapId,view.ActiveMapContentHash,view.ActiveMapSourceIdentityHash));
            Assert.IsFalse(view.Matches(setup,definition),"Active map profile cannot bypass actual native map identity through the old Campaign-only API.");
            setup.CatalogId="S003";Assert.IsFalse(view.MatchesActiveMap(setup,definition,view.ActiveOperationMapId,view.ActiveMapContentHash,view.ActiveMapSourceIdentityHash));
        }
        [Test] public void ProtectedLegacySessionDoesNotQueueOptInOpeningFocus()
        {
            using var f=new Fixture();var native=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);native.IsLegacy=1;f.Em.SetComponentData(f.Session,native);
            Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueAfterActualSpawn(f.Em,f.Session,out _));
            Assert.IsFalse(f.Em.HasComponent<SkirmishS005OpeningFocusOwner>(f.Session));
        }
        [Test] public void ActualS002SpawnReachesPublicProfileAndOpeningQueueOnlyAfterCompleteSource()
        {
            using var f=new Fixture(155923,"S002");f.Em.DestroyEntity(f.Actor);
            Assert.IsFalse(SkirmishMissionViewProfileApplication.TryResolve(f.Em,out _));
            using var builder=new BlobBuilder(Allocator.Temp);ref var root=ref builder.ConstructRoot<OperationMapBlob>();
            root.OperationMapId=f.View.ActiveOperationMapId;root.ContentHash=f.View.ActiveMapContentHash;root.SourceIdentityHash=f.View.ActiveMapSourceIdentityHash;
            var blob=builder.CreateBlobAssetReference<OperationMapBlob>(Allocator.Persistent);
            try
            {
                var map=f.Em.CreateEntity();f.Em.AddComponentData(map,new ActiveOperationMapComponent {OperationMapId=f.View.ActiveOperationMapId,Generation=9});
                var metadata=new OperationMapMetadataComponent {Blob=blob,Generation=9,PhysicalSourceValidated=0};f.Em.AddComponentData(map,metadata);
                Assert.IsFalse(SkirmishMissionViewProfileApplication.TryResolve(f.Em,out _));
                metadata.PhysicalSourceValidated=1;f.Em.SetComponentData(map,metadata);
                Assert.IsTrue(SkirmishMissionViewProfileApplication.TryResolve(f.Em,out var resolved));Assert.AreEqual("S002",resolved.CatalogId);
                Assert.IsTrue(SkirmishScenarioSpawnSystem.TrySpawnLedgers(f.Em,f.Session,f.Setup,out var reason,out _),reason.ToString());
                // EditMode spawn uses the existing bare-entity fallback, not a rendered prefab.
                // Complete only the fixture actor components; original IDs/groups come from real spawn.
                using(var actorsQuery=f.Em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent)))
                using(var actors=actorsQuery.ToEntityArray(Allocator.Temp))
                    for(int i=0;i<actors.Length;i++)
                    {
                        var actor=actors[i];if(f.Em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role!=SkirmishRoleKind.Rifle)continue;
                        Assert.IsTrue(f.Em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor));
                        f.Em.AddComponent<SkirmishSharedActorTag>(actor);f.Em.AddComponentData(actor,new UnitHealth {Current=100,Max=100});
                        f.Em.AddComponentData(actor,LocalTransform.FromPosition(100+i*2,2,200+i*4));
                    }
                Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueAfterActualSpawn(f.Em,f.Session,out var error),error);
                var owner=f.Em.GetComponentData<SkirmishS005OpeningFocusOwner>(f.Session);
                Assert.AreEqual("role.rifle.1.0.0",owner.ActorId.ToString());Assert.IsTrue(SkirmishS005OpeningViewPolicy.OwnsIntroduction(f.Em,f.Session));
                using var requests=f.Em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));var entity=requests.GetSingletonEntity();
                var request=f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(entity);Assert.AreEqual(1,request.Requested);Assert.AreEqual(resolved.NormalHeight,request.Perspective.x);
                request.Requested=0;f.Em.SetComponentData(entity,request);
                Assert.IsTrue(SkirmishS005OpeningViewPolicy.TryQueueAfterActualSpawn(f.Em,f.Session,out error),error);Assert.AreEqual(0,f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(entity).Requested);
                var native=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session);native.CatalogId="S003";f.Em.SetComponentData(f.Session,native);
                Assert.IsFalse(SkirmishS005OpeningViewPolicy.OwnsIntroduction(f.Em,f.Session));
                foreach(var group in f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session))
                {Assert.AreEqual(0,group.Selected);Assert.AreEqual(SkirmishGroupOrderKind.None,group.LastOrder);}
            }
            finally {blob.Dispose();}
        }
        public static void RunGenericOwnedOpeningValidation()
        {
            var t=new SkirmishS005OpeningViewPolicyTests();
            Game.Editor.SkirmishGroundSourceFitSurvey.RunCodedCameraSourceResolutionControls();
            Game.Editor.SkirmishS002AriaRunHarness.RunCodedOpeningSettleControls();
            t.ActualS002SpawnReachesPublicProfileAndOpeningQueueOnlyAfterCompleteSource();
            t.GenericProfileIdentityIsDataDrivenAndDoesNotAdmitAnUnboundLegacyProfile();
            t.ActiveMapProfileRejectsWrongSourceContentGenerationAndDefinition();
            t.GenericOpeningUsesOnlyCompleteActualActivePlayerRifleGroup();
            t.S002ProfilePinsExistingDefinitionLayoutAndActualCompatibilitySourceOnly();
            t.ProtectedLegacySessionDoesNotQueueOptInOpeningFocus();
            RunFocusedValidation();
            Debug.Log("[GenericOwnedOpeningViewControls] result=Passed newControls=6 legacyControls=9 nativeVisualAccepted=0");
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
