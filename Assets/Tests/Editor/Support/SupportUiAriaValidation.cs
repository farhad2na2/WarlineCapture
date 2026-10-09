#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportUiAriaValidation
    {
        public static void RunFocusedValidation()
        {
            try
            {
                var t=new SupportUiAriaValidation();t.PreviewAndCameraNeverSpendOrIssueOrders();t.ApprovalQueuesExactlyOneAction();
                t.ChangedTargetDeclineAndExpiryCannotCommit();t.InterruptionCancelsPreviewAndConsent();t.RejectedCommitReturnsToPreview();t.PointerPicksProjectedMilitaryHitbox();t.FlightStatusShowsReservedCostWithoutMutation();t.PlayerControlsIgnoreEnemySupportSession();t.ActivePlayerSessionOverridesInactiveBinding();t.AmbiguousPlayerSessionsRejectInput();t.PausedSessionDoesNotSkipOtherConsentExpiry();t.TerminalCleanupOnlyRemovesOwnedSmoke();t.AriaInboxRoutesConsentAndStopOnlyToPlayer();
                Debug.Log("[SupportUiAriaValidation] result=Passed tests=13");ValidationExit.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportUiAriaValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        [Test] public void PlayerControlsIgnoreEnemySupportSession()=>WithGateway((f,store)=>
        {
            var enemy=f.Em.Instantiate(f.Root);f.Em.RemoveComponent<UiShellRootComponent>(enemy);
            var session=f.Em.GetComponentData<SupportSessionComponent>(enemy);session.FactionId=2;f.Em.SetComponentData(enemy,session);
            Assert.IsTrue(UiShellRuntimeGateway.TryReadSupport(out _));
            Assert.IsTrue(UiShellRuntimeGateway.SelectSupport(1));
            Assert.AreEqual(SupportAbilityKind.Smoke,f.Em.GetComponentData<SupportInputStateComponent>(f.Root).Selected);
            Assert.AreEqual(SupportAbilityKind.None,f.Em.GetComponentData<SupportInputStateComponent>(enemy).Selected);
        });
        [Test] public void ActivePlayerSessionOverridesInactiveBinding()=>WithGateway((f,store)=>
        {
            var inactive=f.Em.Instantiate(f.Root);f.Em.RemoveComponent<UiShellRootComponent>(inactive);
            var session=f.Em.GetComponentData<SupportSessionComponent>(inactive);session.Active=0;f.Em.SetComponentData(inactive,session);
            Assert.IsTrue(UiShellRuntimeGateway.SelectSupport(1));
            Assert.AreEqual(SupportAbilityKind.Smoke,f.Em.GetComponentData<SupportInputStateComponent>(f.Root).Selected);
            Assert.AreEqual(SupportAbilityKind.None,f.Em.GetComponentData<SupportInputStateComponent>(inactive).Selected);
        });
        [Test] public void AmbiguousPlayerSessionsRejectInput()=>WithGateway((f,store)=>
        {
            var duplicate=f.Em.Instantiate(f.Root);f.Em.RemoveComponent<UiShellRootComponent>(duplicate);
            Assert.IsFalse(UiShellRuntimeGateway.TryReadSupport(out _));
            Assert.IsFalse(UiShellRuntimeGateway.SelectSupport(1));
            foreach(var root in new[]{f.Root,duplicate})
                Assert.AreEqual(SupportAbilityKind.None,f.Em.GetComponentData<SupportInputStateComponent>(root).Selected);
            Assert.AreEqual(2,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);
        });
        [Test] public void PausedSessionDoesNotSkipOtherConsentExpiry()=>WithGateway((f,store)=>
        {
            var other=f.Em.Instantiate(f.Root);f.Em.RemoveComponent<UiShellRootComponent>(other);
            var session=f.Em.GetComponentData<SupportSessionComponent>(f.Root);session.Active=0;f.Em.SetComponentData(f.Root,session);
            session=f.Em.GetComponentData<SupportSessionComponent>(other);session.FactionId=2;session.SimulationSeconds=20;f.Em.SetComponentData(other,session);
            f.Em.SetComponentData(f.Root,new SupportInputStateComponent {Phase=3});
            f.Em.SetComponentData(other,new SupportProposalComponent {ProposalId=7,ExpiresAt=15});
            f.Em.GetBuffer<SupportRequestElement>(f.Root).Add(f.Request());
            f.World.CreateSystem<SupportAttemptCleanupSystem>().Update(f.World.Unmanaged);
            Assert.AreEqual(0,f.Em.GetComponentData<SupportInputStateComponent>(f.Root).Phase);
            Assert.AreEqual(0,f.Em.GetBuffer<SupportRequestElement>(f.Root).Length);
            Assert.AreEqual(1,f.Em.GetComponentData<SupportProposalComponent>(other).Consumed);
        });
        [Test] public void TerminalCleanupOnlyRemovesOwnedSmoke()=>WithGateway((f,store)=>
        {
            var other=f.Em.Instantiate(f.Root);f.Em.RemoveComponent<UiShellRootComponent>(other);
            var session=f.Em.GetComponentData<SupportSessionComponent>(f.Root);session.Active=0;f.Em.SetComponentData(f.Root,session);
            f.Em.AddComponentData(other,new CampaignMissionRuntimeComponent {Outcome=Game.Missions.Contracts.MissionOutcomeKind.Victory});
            Entity Zone(Entity root)
            {var entity=f.Em.CreateEntity(typeof(SupportSmokeZoneComponent));f.Em.SetComponentData(entity,new SupportSmokeZoneComponent {Root=root,SessionToken="support-test",AttemptOrdinal=1,ExpiresAt=15});return entity;}
            var pausedZone=Zone(f.Root);var terminalZone=Zone(other);
            f.World.CreateSystem<SupportAttemptCleanupSystem>().Update(f.World.Unmanaged);
            Assert.IsTrue(f.Em.Exists(pausedZone));Assert.IsFalse(f.Em.Exists(terminalZone));
        });
        [Test] public void AriaInboxRoutesConsentAndStopOnlyToPlayer()=>WithGateway((f,store)=>
        {
            Preview();var enemy=f.Em.Instantiate(f.Root);f.Em.RemoveComponent<UiShellRootComponent>(enemy);
            var session=f.Em.GetComponentData<SupportSessionComponent>(enemy);session.FactionId=2;f.Em.SetComponentData(enemy,session);
            var inbox=f.Em.CreateEntity();f.Em.AddBuffer<AssistantCommandIntentRequestElement>(inbox);f.Em.AddBuffer<AssistantCommandIntentResultElement>(inbox);
            f.Em.GetBuffer<AssistantCommandIntentRequestElement>(inbox).Add(Packet(f,AssistantCommandIntentKind.ProposeSupport));
            var system=f.World.CreateSystem<AssistantSupportIntentSystem>();system.Update(f.World.Unmanaged);
            Assert.AreEqual(0,f.Em.GetBuffer<AssistantCommandIntentRequestElement>(inbox).Length);
            Assert.AreEqual(AssistantCommandIntentStatus.Accepted,f.Em.GetBuffer<AssistantCommandIntentResultElement>(inbox)[0].Status);
            Assert.AreEqual(7,f.Em.GetComponentData<SupportProposalComponent>(f.Root).ProposalId);
            Assert.AreEqual(0,f.Em.GetComponentData<SupportProposalComponent>(enemy).ProposalId);
            f.Em.SetComponentData(enemy,new SupportProposalComponent {ProposalId=9});
            f.Em.GetBuffer<AssistantCommandIntentRequestElement>(inbox).Add(new AssistantCommandIntentRequestElement {Kind=AssistantCommandIntentKind.StopAssistantControl});system.Update(f.World.Unmanaged);
            Assert.AreEqual(1,f.Em.GetComponentData<SupportProposalComponent>(f.Root).Consumed);
            Assert.AreEqual(0,f.Em.GetComponentData<SupportProposalComponent>(enemy).Consumed);
            Assert.AreEqual(1,f.Em.GetBuffer<AssistantCommandIntentRequestElement>(inbox).Length,"Stop must remain queued for the ordinary assistant owner.");
        });
        private static void WithGateway(Action<SupportValidationFixture,Entity> action)
        {
            var old=World.DefaultGameObjectInjectionWorld;
            using var f=new SupportValidationFixture();var store=f.Store(2);
            f.Em.AddComponentData(f.Root,new SupportFuelAvailabilityComponent {Total=2,Version=1});
            f.Em.AddComponent<UiShellRootComponent>(f.Root);
            World.DefaultGameObjectInjectionWorld=f.World;UiShellEcsGateway.RegisterAsRuntimeGateway();
            try{action(f,store);}
            finally{World.DefaultGameObjectInjectionWorld=old;UiShellEcsGateway.RegisterAsRuntimeGateway();}
        }
        [Test] public void FlightStatusShowsReservedCostWithoutMutation()=>WithGateway((f,store)=>
        {
            var request=SupportStrikeValidation.Prepare(f,out var target);f.Store(2);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            Assert.IsTrue(UiShellRuntimeGateway.TryReadSupport(out var model));
            Assert.AreEqual(2,model.ExecutionKind);Assert.AreEqual(1,model.ExecutionPhase);Assert.AreEqual(4,model.ReservedFuel);Assert.AreEqual(3,model.ExecutionSeconds);
            Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);Assert.AreEqual(2,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);
            Assert.IsTrue(UiShellRuntimeGateway.TryReadSupport(out var unchanged));Assert.AreEqual(model.ReservedFuel,unchanged.ReservedFuel);
        });
        private static void Preview()
        {
            Assert.IsTrue(UiShellRuntimeGateway.SelectSupport(1));Assert.IsTrue(UiShellRuntimeGateway.BeginSupportTargeting());
            Assert.IsTrue(UiShellRuntimeGateway.PreviewSupport(Vector3.zero));
        }
        [Test] public void PreviewAndCameraNeverSpendOrIssueOrders()=>WithGateway((f,store)=>
        {
            var camera=f.Em.CreateEntity(typeof(RuntimeCameraFocusRequestComponent));Preview();
            UiShellRuntimeGateway.ShowSupportTarget();
            Assert.AreEqual(1,f.Em.GetComponentData<RuntimeCameraFocusRequestComponent>(camera).Requested);
            using var orders=f.Em.CreateEntityQuery(typeof(UnitMoveOrderRequestElement));Assert.AreEqual(0,orders.CalculateEntityCount());
            Assert.AreEqual(2,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);
            Assert.AreEqual(0,f.Em.GetBuffer<SupportRequestElement>(f.Root).Length);
            Assert.IsTrue(UiShellRuntimeGateway.ProposeSupport());UiShellRuntimeGateway.DeclineSupport();
            Assert.IsFalse(UiShellRuntimeGateway.ApproveSupport());UiShellRuntimeGateway.CancelSupport();
            Assert.AreEqual(0,f.Em.GetComponentData<SupportInputStateComponent>(f.Root).Phase);
            Assert.AreEqual(2,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[0].ChargesRemaining);
        });
        [Test] public void ApprovalQueuesExactlyOneAction()=>WithGateway((f,store)=>
        {
            Preview();Assert.IsTrue(UiShellRuntimeGateway.ProposeSupport());Assert.IsTrue(UiShellRuntimeGateway.ApproveSupport());
            Assert.IsFalse(UiShellRuntimeGateway.ApproveSupport());Assert.AreEqual(1,f.Em.GetBuffer<SupportRequestElement>(f.Root).Length);
            Assert.AreEqual(2,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels,"Consent alone cannot spend before the simulation owner.");
            var request=f.Em.GetBuffer<SupportRequestElement>(f.Root)[0];Assert.AreEqual(SupportRequestSource.Aria,request.Source);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            Assert.AreEqual(1,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);
            Assert.AreEqual(SupportRejectionReason.AlreadyProcessed,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
        });
        private static AssistantCommandIntentRequestElement Packet(SupportValidationFixture f,AssistantCommandIntentKind kind)
        {
            var preview=f.Em.GetComponentData<SupportPreviewComponent>(f.Root);
            return new AssistantCommandIntentRequestElement {Kind=kind,SupportSessionToken="support-test",SupportAttemptOrdinal=1,
                SupportKind=SupportAbilityKind.Smoke,SupportPreviewId=preview.PreviewId,SupportAbilityVersion=1,SupportCatalogRevision=1,
                SupportFuelCost=1,SupportProposalId=7,SupportConsentVersion=9,WorldPosition=preview.Request.Position};
        }
        [Test] public void ChangedTargetDeclineAndExpiryCannotCommit()=>WithGateway((f,store)=>
        {
            Preview();var intent=Packet(f,AssistantCommandIntentKind.ProposeSupport);
            Assert.AreEqual(SupportRejectionReason.None,AssistantSupportIntentSystem.Process(f.Em,f.Root,intent));
            intent.Kind=AssistantCommandIntentKind.DeclineSupport;
            Assert.AreEqual(SupportRejectionReason.None,AssistantSupportIntentSystem.Process(f.Em,f.Root,intent));
            intent.Kind=AssistantCommandIntentKind.ApproveSupport;
            Assert.AreEqual(SupportRejectionReason.ConsentRequired,AssistantSupportIntentSystem.Process(f.Em,f.Root,intent));
            Assert.IsTrue(UiShellRuntimeGateway.PreviewSupport(new Vector3(2,0,0)));intent=Packet(f,AssistantCommandIntentKind.ProposeSupport);
            Assert.AreEqual(SupportRejectionReason.None,AssistantSupportIntentSystem.Process(f.Em,f.Root,intent));
            var s=f.Em.GetComponentData<SupportSessionComponent>(f.Root);s.SimulationSeconds=15;f.Em.SetComponentData(f.Root,s);
            intent.Kind=AssistantCommandIntentKind.ApproveSupport;
            Assert.AreEqual(SupportRejectionReason.ConsentExpired,AssistantSupportIntentSystem.Process(f.Em,f.Root,intent));
            s.SimulationSeconds=0;f.Em.SetComponentData(f.Root,s);
            Assert.IsTrue(UiShellRuntimeGateway.PreviewSupport(new Vector3(4,0,0)));
            Assert.AreEqual(SupportRejectionReason.ConsentRequired,AssistantSupportIntentSystem.Process(f.Em,f.Root,intent));
            Assert.AreEqual(0,f.Em.GetBuffer<SupportRequestElement>(f.Root).Length);
            Assert.AreEqual(2,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);
        });
        [Test] public void InterruptionCancelsPreviewAndConsent()=>WithGateway((f,store)=>
        {
            Preview();Assert.IsTrue(UiShellRuntimeGateway.ProposeSupport());
            var s=f.Em.GetComponentData<SupportSessionComponent>(f.Root);s.Active=0;f.Em.SetComponentData(f.Root,s);
            f.World.CreateSystem<SupportAttemptCleanupSystem>().Update(f.World.Unmanaged);
            Assert.AreEqual(0,f.Em.GetComponentData<SupportInputStateComponent>(f.Root).Phase);
            Assert.AreEqual(0,f.Em.GetComponentData<SupportPreviewComponent>(f.Root).PreviewId);
            Assert.IsFalse(UiShellRuntimeGateway.ApproveSupport());Assert.AreEqual(2,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);
        });
        [Test] public void PointerPicksProjectedMilitaryHitbox()=>WithGateway((f,store)=>
        {
            SupportStrikeValidation.Prepare(f,out var target);var stock=f.Em.GetComponentData<BuildingResourceStorageComponent>(store);stock.StoredFuelBarrels=10;f.Em.SetComponentData(store,stock);
            f.Em.SetComponentData(f.Root,new SupportFuelAvailabilityComponent {Total=10});
            f.Em.AddComponentData(target,new UnitSelectionHitbox {Center=new float3(0,2,0),Extents=new float3(2,2,4)});
            var oldCamera=Camera.main;bool oldEnabled=oldCamera!=null&&oldCamera.enabled;if(oldCamera!=null)oldCamera.enabled=false;
            var go=new GameObject("Support pointer camera",typeof(Camera));go.tag="MainCamera";
            try
            {
                var camera=go.GetComponent<Camera>();camera.transform.position=new Vector3(0,20,-25);camera.transform.LookAt(new Vector3(0,2,0));
                Assert.IsTrue(UiShellRuntimeGateway.SelectSupport(2));Assert.IsTrue(UiShellRuntimeGateway.BeginSupportTargeting());
                var screen=camera.WorldToScreenPoint(new Vector3(0,2,0));
                Assert.IsTrue(UiShellRuntimeGateway.PreviewSupportPointer(screen,new Vector3(20,0,20)),"Raised military hitbox must be picked even when terrain raycast lands behind it.");
                Assert.AreEqual(target,f.Em.GetComponentData<SupportPreviewComponent>(f.Root).Request.Target);
                Assert.AreEqual(10,f.Em.GetComponentData<BuildingResourceStorageComponent>(store).StoredFuelBarrels);
                Assert.AreEqual(0,f.Em.GetBuffer<SupportRequestElement>(f.Root).Length);
            }
            finally {UnityEngine.Object.DestroyImmediate(go);if(oldCamera!=null)oldCamera.enabled=oldEnabled;}
        });
        [Test] public void RejectedCommitReturnsToPreview()=>WithGateway((f,store)=>
        {
            Preview();Assert.IsTrue(UiShellRuntimeGateway.ConfirmSupport());var request=f.Em.GetBuffer<SupportRequestElement>(f.Root)[0];
            var stock=f.Em.GetComponentData<BuildingResourceStorageComponent>(store);stock.StoredFuelBarrels=0;f.Em.SetComponentData(store,stock);
            Assert.AreEqual(SupportRejectionReason.InsufficientFuel,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            Assert.AreEqual(3,f.Em.GetComponentData<SupportInputStateComponent>(f.Root).Phase);
            Assert.AreEqual(SupportRejectionReason.InsufficientFuel,f.Em.GetComponentData<SupportPreviewComponent>(f.Root).Reason);
            Assert.AreEqual(2,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[0].ChargesRemaining);
        });
    }
}
#endif
