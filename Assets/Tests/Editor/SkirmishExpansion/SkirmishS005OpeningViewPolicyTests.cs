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
            public Fixture()
            {
                var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
                Assert.IsTrue(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out _));
                Assert.IsTrue(SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,155926,
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
        public static void RunFocusedValidation()
        {
            var t=new SkirmishS005OpeningViewPolicyTests();
            t.ActualS005SetupGetsOneRoleDerivedNormalCameraRequestWithoutIdentityChange();
            t.PaidForeignDeadMountedDisabledOrDuplicateOriginalCannotSupplyOpeningFocus();
            t.LateOrChangedNativeSessionCannotClaimTheOpeningIntroduction();t.ScopedPolicyCannotBeAppliedToGenericOrCampaignProfiles();
            t.MissingOrUnconfiguredLogicalProfilePreservesExistingStartup();
            Debug.Log("[SkirmishS005OpeningViewPolicyTests] result=Passed tests=5 normalNativeViewAccepted=0");
        }
    }
}
#endif
