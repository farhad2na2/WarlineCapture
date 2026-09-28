#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportRuntimeValidation
    {
        public static void RunFocusedValidation()
        {
            try
            {
                var tests=new SupportRuntimeValidation();tests.FuelCopiesCommitAtomically();tests.RejectionsLeaveCostsAndEffectsUnchanged();
                tests.DuplicateAndCompetingRequestsSpendOnce();tests.ConsentBindsExactAction();tests.AuthoredFuelScopeExcludesDemoStock();
                Debug.Log("[SupportRuntimeValidation] result=Passed tests=5");ValidationExit.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportRuntimeValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        [Test] public void FuelCopiesCommitAtomically()
        {
            using var f=new SupportValidationFixture();var a=f.Store(10,3,4);
            Assert.AreEqual(3,SupportFuelTransactionSystem.Available(f.Em,1));
            var before=f.Em.GetComponentData<BuildingResourceStorageComponent>(a);
            Assert.IsFalse(SupportFuelTransactionSystem.TryConsumeImmediate(f.Em,1,4));Assert.AreEqual(before,f.Em.GetComponentData<BuildingResourceStorageComponent>(a));
            Assert.IsTrue(SupportFuelTransactionSystem.TryConsumeImmediate(f.Em,1,3));var after=f.Em.GetComponentData<BuildingResourceStorageComponent>(a);
            Assert.AreEqual(7,after.StoredFuelBarrels);Assert.AreEqual(3,after.ReservedFuelOutboundBarrels);Assert.AreEqual(4,after.CivilianFuelReserveBarrels);
            var b=f.Store(.4f);var c=f.Store(.6f);Assert.IsTrue(SupportFuelTransactionSystem.TryConsumeImmediate(f.Em,1,1));
            Assert.AreEqual(0,f.Em.GetComponentData<BuildingResourceStorageComponent>(b).StoredFuelBarrels,.0001);
            Assert.AreEqual(0,f.Em.GetComponentData<BuildingResourceStorageComponent>(c).StoredFuelBarrels,.0001);
        }
        [Test] public void RejectionsLeaveCostsAndEffectsUnchanged()
        {
            using var f=new SupportValidationFixture();var store=f.Store(2);var request=f.Request();
            var stock=f.Em.GetComponentData<BuildingResourceStorageComponent>(store);var ability=f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[0];
            void Reject(SupportRejectionReason expected)
            {Assert.AreEqual(expected,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));Assert.AreEqual(stock,f.Em.GetComponentData<BuildingResourceStorageComponent>(store));Assert.AreEqual(ability,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[0]);}
            request.AttemptOrdinal=0;Reject(SupportRejectionReason.WrongAttempt);request.AttemptOrdinal=1;
            var s=f.Em.GetComponentData<SupportSessionComponent>(f.Root);s.Active=0;f.Em.SetComponentData(f.Root,s);Reject(SupportRejectionReason.NotActive);s.Active=1;f.Em.SetComponentData(f.Root,s);
            request.Position=new float3(60,0,0);Reject(SupportRejectionReason.InvalidGround);request.Position=new float3(40,0,0);Reject(SupportRejectionReason.NotVisible);
            request.Position=float3.zero;var regions=f.Em.GetBuffer<SupportGroundRegionElement>(f.Root);var region=regions[0];region.Protected=1;regions[0]=region;Reject(SupportRejectionReason.ProtectedTarget);
            region.Protected=0;regions[0]=region;request.ExpectedAbilityVersion=2;Reject(SupportRejectionReason.StalePreview);
        }
        [Test] public void AuthoredFuelScopeExcludesDemoStock()
        {
            using var f=new SupportValidationFixture();var authored=f.Store(5,1,2);var unrelated=f.Store(100);
            f.Em.AddComponentData(f.Root,new SupportFuelScopeComponent {Storage=authored,Required=1});
            Assert.AreEqual(2,SupportFuelTransactionSystem.Available(f.Em,1));
            Assert.IsFalse(SupportFuelTransactionSystem.TryConsumeImmediate(f.Em,1,3));
            Assert.AreEqual(5,f.Em.GetComponentData<BuildingResourceStorageComponent>(authored).StoredFuelBarrels);
            Assert.IsTrue(SupportFuelTransactionSystem.TryConsumeImmediate(f.Em,1,2));
            Assert.AreEqual(3,f.Em.GetComponentData<BuildingResourceStorageComponent>(authored).StoredFuelBarrels);
            Assert.AreEqual(100,f.Em.GetComponentData<BuildingResourceStorageComponent>(unrelated).StoredFuelBarrels);
            f.Em.DestroyEntity(authored);
            Assert.AreEqual(0,SupportFuelTransactionSystem.Available(f.Em,1));
            Assert.IsFalse(SupportFuelTransactionSystem.TryConsumeImmediate(f.Em,1,1));
        }
        [Test] public void DuplicateAndCompetingRequestsSpendOnce()
        {
            using var f=new SupportValidationFixture();var store=f.Store(1);var request=f.Request();
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            Assert.AreEqual(SupportRejectionReason.AlreadyProcessed,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            request.RequestId=2;Assert.AreEqual(SupportRejectionReason.StalePreview,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            Assert.AreEqual(0,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);Assert.AreEqual(1,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[0].ChargesRemaining);
            using var zones=f.Em.CreateEntityQuery(typeof(SupportSmokeZoneComponent));Assert.AreEqual(1,zones.CalculateEntityCount());
            Assert.AreEqual(2,f.Em.GetBuffer<SupportReceiptElement>(f.Root).Length);
            for(uint i=3;i<90;i++){request.RequestId=i;SupportAbilityRequestSystem.Process(f.Em,f.Root,request);}
            Assert.AreEqual(64,f.Em.GetBuffer<SupportReceiptElement>(f.Root).Length);
            Assert.AreEqual(SupportRejectionReason.AlreadyProcessed,SupportAbilityRequestSystem.Process(f.Em,f.Root,f.Request()));
        }
        [Test] public void ConsentBindsExactAction()
        {
            using var f=new SupportValidationFixture();f.Store(2);var request=f.Request();request.Source=SupportRequestSource.Aria;request.ProposalId=1;request.ConsentVersion=3;
            Assert.AreEqual(SupportRejectionReason.ConsentRequired,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));
            var proposal=new SupportProposalComponent {ProposalId=1,ConsentVersion=3,Request=request,FuelCost=1,CatalogRevision=1,ExpiresAt=15,Approved=1};f.Em.SetComponentData(f.Root,proposal);
            Assert.AreEqual(SupportRejectionReason.None,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));
            request.Position=new float3(1,0,0);Assert.AreEqual(SupportRejectionReason.StalePreview,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));request.Position=float3.zero;
            var session=f.Em.GetComponentData<SupportSessionComponent>(f.Root);session.SimulationSeconds=15;f.Em.SetComponentData(f.Root,session);
            Assert.AreEqual(SupportRejectionReason.ConsentExpired,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));
            session.SimulationSeconds=1;f.Em.SetComponentData(f.Root,session);proposal.Declined=1;f.Em.SetComponentData(f.Root,proposal);
            Assert.AreEqual(SupportRejectionReason.ConsentRequired,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));
        }
    }
}
#endif
