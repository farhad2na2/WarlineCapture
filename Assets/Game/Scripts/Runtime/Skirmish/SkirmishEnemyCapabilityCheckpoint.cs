using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    internal static class SkirmishEnemyCapabilityCheckpoint
    {
        internal static bool HasAny(SkirmishNativeCheckpointParts.Part[] parts)
        {
            if(parts==null)return false;
            foreach(var part in parts)if(part?.Type==typeof(SkirmishEnemyCapabilityStateV1Component).FullName||
                part?.Type==typeof(SkirmishEnemyCapabilityOperationV1).FullName||part?.Type==typeof(SkirmishEnemyTransportPlanV1).FullName)return true;
            return false;
        }
        internal static bool Validate(SkirmishNativeCheckpointSnapshotService.Snapshot saved,SkirmishResolvedSetup setup)
        {
            if(setup.EnemyPolicy==null)return !HasAny(saved.SessionParts);
            if(!SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishEnemyCapabilityStateV1Component state)||
                !SkirmishNativeCheckpointParts.TryReadBuffer(saved.SessionParts,out SkirmishEnemyCapabilityOperationV1[] operations)||
                !SkirmishNativeCheckpointParts.TryReadBuffer(saved.SessionParts,out SkirmishEnemyTransportPlanV1[] plans)||
                !SkirmishNativeCheckpointParts.TryReadBuffer(saved.SessionParts,out SkirmishArmyGroupRecord[] groups)||
                !SkirmishNativeCheckpointParts.TryReadBuffer(saved.SessionParts,out SkirmishProductionReservation[] receipts)||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishObjectiveClockComponent clock)||
                !float.IsFinite(clock.ElapsedSeconds)||clock.ElapsedSeconds<0||!ValidState(state)||operations.Length>1024||plans.Length>4096)return false;
            var actors=new Dictionary<string,SkirmishNativeCheckpointSnapshotService.Actor>(StringComparer.Ordinal);
            foreach(var actor in saved.Actors)
            {
                if(HasAny(actor.Parts)||!SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts,out SkirmishAttemptOwnedComponent own)||
                    own.SessionId.ToString()!=saved.SessionId||!actors.TryAdd(own.StableObjectId.ToString(),actor))return false;
            }
            foreach(var map in saved.MapReferences)if(HasAny(map.Parts))return false;
            foreach(var bank in saved.Banks)if(HasAny(bank.Parts))return false;
            var ids=new HashSet<uint>();var sources=new HashSet<string>(StringComparer.Ordinal);var ownerGroups=new HashSet<uint>();
            foreach(var op in operations)
            {
                if(!ValidOperation(op,state)||op.StartedAt>clock.ElapsedSeconds||op.LastIssuedAt>clock.ElapsedSeconds||!ids.Add(op.OperationId)||!sources.Add(op.SourceStableId.ToString())||!ownerGroups.Add(op.GroupId))return false;
                bool foundGroup=false;foreach(var group in groups)if(group.GroupId==op.GroupId)
                {if(foundGroup||group.FactionId!=2||group.Role!=op.Role)return false;foundGroup=true;}
                if(!foundGroup)return false;
                int producerRuntimeId=0;
                if(op.ReservationId!=0)
                {
                    bool found=false;foreach(var receipt in receipts)if(receipt.ReservationId==op.ReservationId)
                    {if(found||receipt.FactionId!=2||receipt.Role!=op.Role||receipt.ProductionGroupId!=op.GroupId||receipt.DeliveredMembers<=0)return false;producerRuntimeId=receipt.ProducerRuntimeId;found=true;}
                    if(!found)return false;
                }
                else if(op.Kind!=SkirmishEnemyCapabilityKind.Transport)return false;
                else if((SkirmishGenericGroundEnemyRollout.Supports(setup)||SkirmishEnemyObjectivePolicyScope.Supports(setup))&&op.Role==SkirmishRoleKind.TransportHeli&&
                    !SkirmishGenericGroundEnemyRollout.IsAuthoredStartingTransport(setup,op.SourceStableId.ToString(),op.Role,setup.EnemyFaction)&&
                    !SkirmishEnemyObjectivePolicyScope.IsAuthoredStartingTransport(setup,op.SourceStableId.ToString(),op.Role,setup.EnemyFaction))return false;
                // Stable-only references can naturally outlive a casualty between
                // commander ticks. Missing actors are reconciled as lost, never recreated.
                if(actors.TryGetValue(op.SourceStableId.ToString(),out var source))
                {
                    if(!SkirmishNativeCheckpointParts.TryReadComponent(source.Parts,out SkirmishAttemptOwnedComponent own)||own.FactionId!=2||own.ReservationId!=op.ReservationId||
                       !SkirmishNativeCheckpointParts.TryReadComponent(source.Parts,out SkirmishUnitRoleComponent role)||role.Role!=op.Role||
                       !SkirmishNativeCheckpointParts.TryReadComponent(source.Parts,out SkirmishArmyGroupMembershipComponent membership)||membership.GroupId!=op.GroupId)return false;
                    if(op.ReservationId==0&&op.Role==SkirmishRoleKind.TransportHeli&&
                        (SkirmishGenericGroundEnemyRollout.Supports(setup)||SkirmishEnemyObjectivePolicyScope.Supports(setup))&&
                        SpawnableKeyText.Normalize(source.PrefabKey)!=SpawnableKeyText.Normalize(SkirmishRoleCatalogConfig.RuntimePrefabKey(op.Role)))return false;
                    if(op.Kind==SkirmishEnemyCapabilityKind.Transport&&
                        (!SkirmishNativeCheckpointParts.TryReadComponent(source.Parts,out UnitTransportCapacity capacity)||capacity.SoldierCapacity<=0))return false;
                    if(op.Kind==SkirmishEnemyCapabilityKind.CombatAir&&!SkirmishNativeCheckpointParts.TryReadComponent(source.Parts,out UnitAirComponent _))return false;
                }
                if(!op.TargetStableId.IsEmpty&&actors.TryGetValue(op.TargetStableId.ToString(),out var target))
                {if(!SkirmishNativeCheckpointParts.TryReadComponent(target.Parts,out SkirmishAttemptOwnedComponent own)||own.FactionId==2)return false;}
                if(!op.ProducerStableId.IsEmpty&&actors.TryGetValue(op.ProducerStableId.ToString(),out var producer))
                {if(!SkirmishNativeCheckpointParts.TryReadComponent(producer.Parts,out SkirmishAttemptOwnedComponent own)||own.FactionId!=2||own.IsStructure==0||producerRuntimeId<=0||
                    !SkirmishNativeCheckpointParts.TryReadComponent(producer.Parts,out RuntimeBuildingCombatInfo building)||building.RuntimeBuildingId!=producerRuntimeId)return false;}
            }
            var passengers=new HashSet<string>(StringComparer.Ordinal);
            var planGroups=new Dictionary<uint,uint>();var planOwners=new Dictionary<uint,uint>();
            foreach(var plan in plans)
            {
                if(plan.PassengerStableId.IsEmpty||plan.PassengerGroupId==0||!ids.Contains(plan.OperationId)||!passengers.Add(plan.PassengerStableId.ToString()))return false;
                bool transport=false;foreach(var op in operations)if(op.OperationId==plan.OperationId)transport=op.Kind==SkirmishEnemyCapabilityKind.Transport&&op.GroupId!=plan.PassengerGroupId&&op.Phase!=SkirmishEnemyCapabilityPhase.Cooldown;
                if(!transport)return false;
                if(planGroups.TryGetValue(plan.OperationId,out uint existing)&&existing!=plan.PassengerGroupId)return false;
                planGroups[plan.OperationId]=plan.PassengerGroupId;
                if(planOwners.TryGetValue(plan.PassengerGroupId,out uint owner)&&owner!=plan.OperationId)return false;
                planOwners[plan.PassengerGroupId]=plan.OperationId;
                bool groupFound=false;foreach(var group in groups)if(group.GroupId==plan.PassengerGroupId){if(group.FactionId!=2||group.Role!=SkirmishRoleKind.Rifle)return false;groupFound=true;}
                if(!groupFound)return false;
                if(actors.TryGetValue(plan.PassengerStableId.ToString(),out var actor))
                {if(!SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts,out SkirmishAttemptOwnedComponent own)||own.FactionId!=2||
                    !SkirmishNativeCheckpointParts.TryReadComponent(actor.Parts,out SkirmishArmyGroupMembershipComponent member)||member.GroupId!=plan.PassengerGroupId)return false;}
            }
            return true;
        }
        internal static bool ValidState(SkirmishEnemyCapabilityStateV1Component state)=>state.Version==SkirmishEnemyCapabilityService.FeatureVersion&&state.NextOperationId>0;
        internal static bool ValidOperation(SkirmishEnemyCapabilityOperationV1 op,SkirmishEnemyCapabilityStateV1Component state)=>
            ValidState(state)&&op.OperationId>0&&op.OperationId<state.NextOperationId&&op.GroupId>0&&!op.SourceStableId.IsEmpty&&
            Enum.IsDefined(typeof(SkirmishRoleKind),op.Role)&&op.Role!=SkirmishRoleKind.None&&Enum.IsDefined(typeof(SkirmishEnemyCapabilityKind),op.Kind)&&
            Enum.IsDefined(typeof(SkirmishEnemyCapabilityPhase),op.Phase)&&op.Retries<=3&&op.CommandAccepted<=1&&op.LastObservedAirborne<=1&&
            op.ScanRequestId>=0&&float.IsFinite(op.DestinationX)&&float.IsFinite(op.DestinationZ)&&Finite(op.StartedAt)&&Finite(op.LastIssuedAt)&&Finite(op.RetryAt)&&Finite(op.NextSurveyAt)&&
            (op.Phase!=SkirmishEnemyCapabilityPhase.Scanning||(op.Kind==SkirmishEnemyCapabilityKind.Recon&&op.ScanRequestId>0))&&
            ((op.Phase!=SkirmishEnemyCapabilityPhase.Boarding&&op.Phase!=SkirmishEnemyCapabilityPhase.Transit&&op.Phase!=SkirmishEnemyCapabilityPhase.Unloading&&op.Phase!=SkirmishEnemyCapabilityPhase.Aborting)||op.Kind==SkirmishEnemyCapabilityKind.Transport)&&
            (op.Phase!=SkirmishEnemyCapabilityPhase.Engaging||op.Kind==SkirmishEnemyCapabilityKind.CombatAir)&&
            (op.Kind!=SkirmishEnemyCapabilityKind.Transport||(IsCarrierRole(op.Role)&&op.Phase!=SkirmishEnemyCapabilityPhase.Scanning&&op.Phase!=SkirmishEnemyCapabilityPhase.Engaging))&&
            (op.Kind!=SkirmishEnemyCapabilityKind.Recon||op.Role==SkirmishRoleKind.Drone)&&
            (op.Kind!=SkirmishEnemyCapabilityKind.Radar||op.Role==SkirmishRoleKind.Radar)&&
            (op.Kind!=SkirmishEnemyCapabilityKind.CombatAir||op.Role==SkirmishRoleKind.Fighter||op.Role==SkirmishRoleKind.Strike||op.Role==SkirmishRoleKind.AttackHeli||op.Role==SkirmishRoleKind.AttackHeliLight);
        internal static bool IsCarrierRole(SkirmishRoleKind role)=>role==SkirmishRoleKind.Car||role==SkirmishRoleKind.ApcFast||role==SkirmishRoleKind.ApcArmored||role==SkirmishRoleKind.ApcHeavy||
            role==SkirmishRoleKind.TransportHeli||role==SkirmishRoleKind.TransportPlane;
        static bool Finite(float value)=>float.IsFinite(value)&&value>=0;
    }
}
