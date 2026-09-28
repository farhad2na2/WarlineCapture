using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiSupportGateway
    {
        private static World supportWorld;
        private static EntityQuery supportQuery;
        private static FixedString64Bytes supportMissionFixed;
        private static string supportMissionText=string.Empty;
        private static Entity supportTargetEntity;
        private static string supportTargetText=string.Empty;
        private static bool TrySupport(out EntityManager em,out Entity root)
        {
            em=default;root=Entity.Null;
            var world=World.DefaultGameObjectInjectionWorld;if(world==null || !world.IsCreated)return false;
            em=world.EntityManager;
            if(supportWorld!=world){supportWorld=world;supportQuery=em.CreateEntityQuery(typeof(SupportSessionComponent),typeof(SupportInputStateComponent));supportMissionFixed=default;supportMissionText=string.Empty;supportTargetEntity=Entity.Null;supportTargetText=string.Empty;}
            if(supportQuery.CalculateEntityCount()!=1)return false;
            root=supportQuery.GetSingletonEntity();return true;
        }
        bool IUiSupportGateway.TryReadSupport(out UiSupportModel model)
        {
            model=default;if(!TrySupport(out var em,out var root))return false;
            var input=em.GetComponentData<SupportInputStateComponent>(root);var session=em.GetComponentData<SupportSessionComponent>(root);
            var preview=em.GetComponentData<SupportPreviewComponent>(root);var proposal=em.GetComponentData<SupportProposalComponent>(root);
            var states=em.GetBuffer<SupportAbilityStateElement>(root);
            float available=em.HasComponent<SupportFuelAvailabilityComponent>(root)?em.GetComponentData<SupportFuelAvailabilityComponent>(root).Total:0;
            var catalog=em.GetComponentData<SupportCatalogComponent>(root);var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);
            UiSupportAbilityModel Card(SupportAbilityKind kind)
            {
                SupportTargetValidationUtilitySystemHelper.TryDefinition(catalog,kind,out var definition);
                SupportAbilityStateElement a=default;for(int i=0;i<states.Length;i++)if(states[i].Kind==kind){a=states[i];break;}
                byte mask=SupportTargetValidationUtilitySystemHelper.Mask(kind);
                var reason=(policy.AllowedMask & mask)==0?SupportRejectionReason.MissionRestricted:
                    ((policy.OwnedMask|policy.TestGrantMask)&mask)==0?SupportRejectionReason.NotUnlocked:
                    a.Enabled==0||kind==SupportAbilityKind.Paratroopers&&(policy.PopulationCeiling<=0||!em.HasComponent<SupportPayloadBindingsComponent>(root)||em.GetComponentData<SupportPayloadBindingsComponent>(root).InfantryPrefab==Entity.Null)?SupportRejectionReason.NotReady:session.Active==0?SupportRejectionReason.NotActive:
                    a.ChargesRemaining<=0?SupportRejectionReason.NoCharges:session.SimulationSeconds<a.CooldownUntil?SupportRejectionReason.Cooldown:
                    available<definition.FuelCost?SupportRejectionReason.InsufficientFuel:SupportRejectionReason.None;
                return new UiSupportAbilityModel((byte)kind,a.ChargesRemaining,definition.FuelCost,(int)math.ceil(math.max(0,a.CooldownUntil-session.SimulationSeconds)),reason==0,ReasonKey(reason));
            }
            var missionId=em.HasComponent<CampaignMissionRuntimeComponent>(root)?em.GetComponentData<CampaignMissionRuntimeComponent>(root).MissionId:default;
            if(!missionId.Equals(supportMissionFixed)){supportMissionFixed=missionId;supportMissionText=missionId.ToString();}
            string mission=supportMissionText;
            bool hasProposal=proposal.ProposalId!=0 && proposal.Consumed==0 && proposal.Declined==0 && session.SimulationSeconds<proposal.ExpiresAt;
            if(supportTargetEntity!=preview.Request.Target)
            {
                supportTargetEntity=preview.Request.Target;
                supportTargetText=em.Exists(supportTargetEntity)&&em.HasComponent<UnitDisplayInfo>(supportTargetEntity)?em.GetComponentData<UnitDisplayInfo>(supportTargetEntity).Name.ToString():string.Empty;
            }
            SupportReceiptElement execution=default;bool showExecution=false;int remaining=0;
            var receipts=em.GetBuffer<SupportReceiptElement>(root);
            for(int i=receipts.Length-1;i>=0;i--)
            {
                var receipt=receipts[i];if(receipt.Kind==SupportAbilityKind.Smoke)continue;
                bool flying=receipt.Effect!=Entity.Null&&em.Exists(receipt.Effect)&&em.HasComponent<SupportFlightComponent>(receipt.Effect);
                if(!flying&&session.SimulationSeconds-receipt.UpdatedAt>5)continue;
                if(!showExecution||receipt.Phase==SupportExecutionPhase.Approaching)
                {
                    execution=receipt;showExecution=true;
                    if(flying){var flight=em.GetComponentData<SupportFlightComponent>(receipt.Effect);remaining=(int)math.ceil(math.max(0,flight.ApproachSeconds-(session.SimulationSeconds-flight.StartedAt)));}
                }
                if(receipt.Phase==SupportExecutionPhase.Approaching)break;
            }
            var supply=em.HasComponent<SupportSupplyReadModelComponent>(root)?em.GetComponentData<SupportSupplyReadModelComponent>(root):default;
            model=new UiSupportModel((UiSupportPhase)input.Phase,(byte)input.Selected,input.Version,mission,ReasonKey(preview.Reason),
                available,preview.Radius,preview.Request.Position,preview.Valid!=0,hasProposal,
                Card(SupportAbilityKind.Smoke),Card(SupportAbilityKind.Strike),Card(SupportAbilityKind.Paratroopers),Card(SupportAbilityKind.Supply),policy.AllowedMask!=0,session.Active!=0,supportTargetText,showExecution?(byte)execution.Kind:(byte)0,(byte)execution.Phase,execution.ReservedFuel,remaining,ReasonKey(execution.Reason),supply.Remaining,supply.Ready!=0,supply.Claimed!=0,supply.Full!=0,CollectionReasonKey(em.HasComponent<SupportCollectionFeedbackComponent>(root)?em.GetComponentData<SupportCollectionFeedbackComponent>(root).Reason:SupportRejectionReason.None),(byte)policy.LessonKind);return true;
        }
        private static string CollectionReasonKey(SupportRejectionReason reason)=>reason==SupportRejectionReason.LandingBlocked?"support.supply.unreachable":ReasonKey(reason);
        private static readonly string[] SupportReasonKeys={
            "support.reason.0","support.reason.1","support.reason.2","support.reason.3","support.reason.4","support.reason.5",
            "support.reason.6","support.reason.7","support.reason.8","support.reason.9","support.reason.10","support.reason.11",
            "support.reason.12","support.reason.13","support.reason.14","support.reason.15","support.reason.16","support.reason.17",
            "support.reason.18","support.reason.19","support.reason.20","support.reason.21"};
        private static string ReasonKey(SupportRejectionReason reason)=>SupportReasonKeys[(int)reason];
        bool IUiSupportGateway.SelectSupport(byte kind)
        {
            if(kind<1 || kind>4 || !TrySupport(out var em,out var root))return false;
            var input=em.GetComponentData<SupportInputStateComponent>(root);input.Selected=(SupportAbilityKind)kind;input.Phase=1;input.Version++;
            em.SetComponentData(root,input);em.SetComponentData(root,default(SupportCollectionFeedbackComponent));em.SetComponentData(root,default(SupportPreviewComponent));em.SetComponentData(root,default(SupportProposalComponent));return true;
        }
        bool IUiSupportGateway.BeginSupportTargeting()
        {
            if(!TrySupport(out var em,out var root))return false;
            var input=em.GetComponentData<SupportInputStateComponent>(root);if(input.Selected==0)return false;
            if(!((IUiSupportGateway)this).TryReadSupport(out var model) || !model.Ability((byte)input.Selected).Available)return false;
            if(!TryEnqueueUiAction(UiActionKind.BeginSupportTargeting,0))return false;
            input.Phase=2;input.Version++;em.SetComponentData(root,input);em.SetComponentData(root,default(SupportPreviewComponent));
            return true;
        }
        bool IUiSupportGateway.BeginSupplyCollection()
        {
            if(!TrySupport(out var em,out var root)||!em.HasComponent<SupportSupplyReadModelComponent>(root))return false;
            var supply=em.GetComponentData<SupportSupplyReadModelComponent>(root);if(supply.Ready==0||supply.Remaining<=0||supply.Claimed!=0||!em.Exists(supply.Crate))return false;
            var crate=em.GetComponentData<SupportSupplyCrateComponent>(supply.Crate);Entity collector=Entity.Null;
            using var selected=em.CreateEntityQuery(typeof(SelectedUnitTag));using var entities=selected.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)if(SupportSupplyAdapterSystemHelper.EligibleCollector(em,entity,crate)){collector=entity;break;}
            if(collector==Entity.Null)
            {
                em.SetComponentData(root,new SupportCollectionFeedbackComponent {Reason=SupportRejectionReason.InvalidTargetType});
                return false;
            }
            em.SetComponentData(root,default(SupportCollectionFeedbackComponent));
            if(!TryEnqueueUiAction(UiActionKind.CollectSupply,0))return false;
            var input=em.GetComponentData<SupportInputStateComponent>(root);input.Phase=5;input.Collector=collector;input.Version++;em.SetComponentData(root,input);em.SetComponentData(root,default(SupportPreviewComponent));return true;
        }
        private static bool CollectSupplyPointer(EntityManager em,Entity root,Vector2 screenPosition)
        {
            var input=em.GetComponentData<SupportInputStateComponent>(root);var supply=em.GetComponentData<SupportSupplyReadModelComponent>(root);var camera=Camera.main;
            if(camera==null||supply.Ready==0||!em.Exists(supply.Crate))return false;var point=camera.WorldToScreenPoint(em.GetComponentData<SupportSupplyCrateComponent>(supply.Crate).LandingPosition);
            if(point.z<=0||((Vector2)point-screenPosition).sqrMagnitude>math.pow(math.max(24,Screen.height*40f/1080f),2))return false;
            var session=em.GetComponentData<SupportSessionComponent>(root);
            em.GetBuffer<SupportCollectRequestElement>(root).Add(new SupportCollectRequestElement {Collector=input.Collector,Crate=supply.Crate,SessionToken=session.SessionToken,AttemptOrdinal=session.AttemptOrdinal});input.Phase=4;input.Version++;em.SetComponentData(root,input);return true;
        }
        bool IUiSupportGateway.PreviewSupport(Vector3 position)=>PreviewSupport(position,default,false);
        bool IUiSupportGateway.PreviewSupportPointer(Vector2 screenPosition,Vector3 groundPosition)=>PreviewSupport(groundPosition,screenPosition,true);
        private bool PreviewSupport(Vector3 position,Vector2 screenPosition,bool pointer)
        {
            if(!TrySupport(out var em,out var root))return false;
            var input=em.GetComponentData<SupportInputStateComponent>(root);if(input.Phase==5)return pointer&&CollectSupplyPointer(em,root,screenPosition);if(input.Phase!=2 && input.Phase!=3)return false;
            var session=em.GetComponentData<SupportSessionComponent>(root);var states=em.GetBuffer<SupportAbilityStateElement>(root);
            uint version=0;foreach(var a in states)if(a.Kind==input.Selected)version=a.StateVersion;
            uint previewId=++input.Version;
            var request=new SupportRequestElement {SessionToken=session.SessionToken,AttemptOrdinal=session.AttemptOrdinal,Kind=input.Selected,
                Source=SupportRequestSource.Player,Position=position,ExpectedAbilityVersion=version,PreviewId=previewId};
            if(input.Selected==SupportAbilityKind.Strike)
            {
                request.TargetKind=SupportTargetKind.Entity;
                using var targets=em.CreateEntityQuery(typeof(SupportTargetEligibilityComponent),typeof(UnitHealth),typeof(Unity.Transforms.LocalTransform));
                using var chunks=targets.ToArchetypeChunkArray(Allocator.Temp);var et=em.GetEntityTypeHandle();float closest=float.MaxValue,bestDepth=float.MaxValue;
                foreach(var chunk in chunks)foreach(var entity in chunk.GetNativeArray(et))
                {
                    var knowledge=em.GetComponentData<SupportTargetEligibilityComponent>(entity);
                    if(pointer && (knowledge.CurrentlyVisible==0||em.GetComponentData<UnitHealth>(entity).Current<=0))continue;
                    var transform=em.GetComponentData<Unity.Transforms.LocalTransform>(entity);var point=transform.Position;
                    float distance;float radius;float depth=0;
                    if(pointer)
                    {
                        var camera=Camera.main;if(camera==null)continue;depth=camera.WorldToScreenPoint(point).z;if(depth<=0)continue;
                        radius=math.max(8,camera.pixelHeight*16f/1080f);
                        if(em.HasComponent<UnitSelectionHitbox>(entity))
                        {if(!FocusableUnitLookupCameraSystemHelper.TryGetSelectionHitboxScreenDistanceSq(camera,transform.ToMatrix(),em.GetComponentData<UnitSelectionHitbox>(entity),screenPosition,out distance,out _))continue;}
                        else {var screen=camera.WorldToScreenPoint(point);if(screen.z<=0)continue;distance=((Vector2)screen-screenPosition).sqrMagnitude;}
                    }
                    else
                    {
                        distance=math.distancesq(point.xz,((float3)position).xz);
                        radius=em.HasComponent<UnitSelectionHitbox>(entity)?math.max(2,math.cmax(em.GetComponentData<UnitSelectionHitbox>(entity).Extents.xz)):2;
                    }
                    if(distance>radius*radius || distance>closest || distance==closest&&depth>=bestDepth)continue;
                    closest=distance;bestDepth=depth;request.Target=entity;request.Position=point;request.TargetKnowledgeVersion=knowledge.SourceVersion;
                }
            }
            if(input.Selected is SupportAbilityKind.Paratroopers or SupportAbilityKind.Supply)
            {request.TargetKind=SupportTargetKind.LandingZone;if(SupportLandingUtilitySystemHelper.TryGrid(em,out _,out var grid))request.Cell=GridUtils.WorldToCell(grid,request.Position);}
            var reason=SupportTargetValidationUtilitySystemHelper.Validate(em,root,request);
            SupportTargetValidationUtilitySystemHelper.TryDefinition(em.GetComponentData<SupportCatalogComponent>(root),input.Selected,out var definition);
            em.SetComponentData(root,new SupportPreviewComponent {PreviewId=previewId,Request=request,Reason=reason,Valid=(byte)(reason==0?1:0),FuelCost=definition.FuelCost,Radius=definition.Radius});
            input.Phase=3;em.SetComponentData(root,input);em.SetComponentData(root,default(SupportProposalComponent));return reason==0;
        }
        bool IUiSupportGateway.ConfirmSupport()=>QueueSupport(SupportRequestSource.Player);
        private static bool QueueSupport(SupportRequestSource source)
        {
            if(!TrySupport(out var em,out var root))return false;
            var input=em.GetComponentData<SupportInputStateComponent>(root);var preview=em.GetComponentData<SupportPreviewComponent>(root);
            if(input.Phase!=3 || preview.Valid==0)return false;
            var request=preview.Request;request.Source=source;
            if(source==SupportRequestSource.Aria)
            {var p=em.GetComponentData<SupportProposalComponent>(root);request.ProposalId=p.ProposalId;request.ConsentVersion=p.ConsentVersion;}
            var session=em.GetComponentData<SupportSessionComponent>(root);request.RequestId=session.NextRequestId++;em.SetComponentData(root,session);
            em.GetBuffer<SupportRequestElement>(root).Add(request);input.Phase=4;input.Version++;em.SetComponentData(root,input);return true;
        }
        void IUiSupportGateway.CancelSupport()
        {
            if(!TrySupport(out var em,out var root))return;
            var input=em.GetComponentData<SupportInputStateComponent>(root);input.Phase=0;input.Collector=Entity.Null;input.Version++;em.SetComponentData(root,input);
            em.SetComponentData(root,default(SupportPreviewComponent));em.SetComponentData(root,default(SupportProposalComponent));
        }
        bool IUiSupportGateway.ProposeSupport()
        {
            if(!TrySupport(out var em,out var root))return false;
            var preview=em.GetComponentData<SupportPreviewComponent>(root);
            if(preview.Valid==0 || preview.PreviewId==0 || em.GetComponentData<SupportInputStateComponent>(root).Phase!=3)return false;
            var session=em.GetComponentData<SupportSessionComponent>(root);var old=em.GetComponentData<SupportProposalComponent>(root);
            // Declined exact actions are not proposed again automatically.
            if(old.Declined!=0 && SupportTargetValidationUtilitySystemHelper.SameAction(old.Request,preview.Request))return false;
            em.SetComponentData(root,new SupportProposalComponent {ProposalId=preview.PreviewId,ConsentVersion=preview.PreviewId,
                CatalogRevision=session.CatalogRevision,Request=preview.Request,FuelCost=preview.FuelCost,ExpiresAt=session.SimulationSeconds+15});return true;
        }
        bool IUiSupportGateway.ApproveSupport()
        {
            if(!TrySupport(out var em,out var root))return false;var p=em.GetComponentData<SupportProposalComponent>(root);
            if(p.ProposalId==0 || p.Consumed!=0 || p.Declined!=0 || em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds>=p.ExpiresAt)return false;
            if(em.GetComponentData<SupportInputStateComponent>(root).Phase!=3)return false;
            p.Approved=1;em.SetComponentData(root,p);return QueueSupport(SupportRequestSource.Aria);
        }
        void IUiSupportGateway.DeclineSupport()
        { if(TrySupport(out var em,out var root)){var p=em.GetComponentData<SupportProposalComponent>(root);p.Declined=1;p.Consumed=1;em.SetComponentData(root,p);} }
        void IUiSupportGateway.ShowSupportTarget()
        {
            if(!TrySupport(out var em,out var root))return;
            var preview=em.GetComponentData<SupportPreviewComponent>(root);var input=em.GetComponentData<SupportInputStateComponent>(root);
            if(input.Phase==5&&em.HasComponent<SupportSupplyReadModelComponent>(root)){var supply=em.GetComponentData<SupportSupplyReadModelComponent>(root);if(em.Exists(supply.Crate))preview.Request.Position=em.GetComponentData<SupportSupplyCrateComponent>(supply.Crate).LandingPosition;else return;}
            else if(preview.PreviewId==0)return;
            using var query=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));if(query.CalculateEntityCount()!=1)return;
            em.SetComponentData(query.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent { Requested=1,Smooth=1,World=preview.Request.Position,SmoothTimeSeconds=.25f });
        }
    }
}
