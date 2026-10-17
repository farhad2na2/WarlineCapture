using System;
using System.Collections.Generic;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    // Explicit wire2 payload owner. No Entity value is serialized; capture and
    // preparation are read-only, and commit only installs paid-work state.
    internal static class SkirmishNativeRepairCheckpoint
    {
        [Serializable] internal sealed class Payload
        {
            public int Version=1,DefinitionVersion;
            public uint SetupHash;
            public string SessionId,CatalogId,DefinitionId,ContentHash,OperationMapId,LayoutId,LayoutBindingFingerprint,
                MissionLogicalBindingFingerprint,SourceId,SourceContentHash,SourceIdentityHash,SourceSceneGuid,LayoutSemanticFingerprint;
            public Row[] Rows;
        }
        [Serializable] internal sealed class Row
        {
            public string ActorId,ActorPrefabKey,FacilityId,FacilityPrefabKey,PadId;
            public byte Faction,IncomingDamage,NewMovement;
            public SkirmishRoleKind Role;
            public SkirmishPopulationCategory Category;
            public VehicleRepairPhase Phase;
            public uint RequestId;
            public float3 AcceptedPosition,PadPosition,FacilityPosition;
            public long FractionNumerator,FractionDenominator,DurationTicks,RemainingTicks;
            public double LastClockSeconds,FractionalTicks;
            public int PaidMaterials,RefundDue,RefundedMaterials,LastHealth,LastMaximum;
        }
        internal sealed class Prepared
        {
            internal ulong World;
            internal Entity Session;
            internal string PayloadJson,SavedJson;
            internal SkirmishResolvedSetup Setup;
            internal SkirmishLaunchDefinitionRecord Launch;
            internal readonly Dictionary<string,Entity> Actors=new(StringComparer.Ordinal);
            internal readonly Dictionary<byte,Entity> Banks=new();
            internal readonly Dictionary<byte,SkirmishNativeCheckpointParts.Part[]> ExpectedBanks=new();
            internal readonly List<Entity> Added=new();
            internal bool Committed,RolledBack;
        }

        internal static bool TryCapture(EntityManager em,Entity session,out Payload payload,out string error)
        {
            payload=null;error="NativeRepairCaptureIdentityInvalid";
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(!NoPendingRequests(em,session,native.SessionId))return false;
            var rows=new List<Row>();
            using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<VehicleRepairStateComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in entities)
            {
                var state=em.GetComponentData<VehicleRepairStateComponent>(actor);
                bool owned=em.HasComponent<SkirmishAttemptOwnedComponent>(actor)&&em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId==native.SessionId;
                if(state.Session!=session&&!owned)continue;
                if(state.Session!=session||state.AttemptId!=native.SessionId||state.SetupHash!=native.SetupHash||
                    !ActorIdentity(em,actor,state.ActorStableId.ToString(),native.SessionId,state.Faction,out var role)||
                    !em.HasComponent<UnitSourcePrefabKey>(actor)||!UniqueBank(em,state.Faction,out var bank)||bank!=state.MaterialsBank)return false;
                var row=FromState(state);row.Role=role.Role;row.Category=role.Category;
                row.ActorPrefabKey=em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString();
                if(row.Phase==VehicleRepairPhase.Working)
                {
                    if(!FacilityIdentity(em,state.ServiceFacility,row.FacilityId,native.SessionId,row.Faction,out var position)||
                        !Near(position,row.FacilityPosition)||!em.HasComponent<UnitSourcePrefabKey>(state.ServiceFacility))return false;
                    row.FacilityPrefabKey=em.GetComponentData<UnitSourcePrefabKey>(state.ServiceFacility).Value.ToString();
                }
                else if(em.Exists(state.ServiceFacility)&&em.HasComponent<UnitSourcePrefabKey>(state.ServiceFacility))
                    row.FacilityPrefabKey=em.GetComponentData<UnitSourcePrefabKey>(state.ServiceFacility).Value.ToString();
                if(!Schema(row))return false;rows.Add(row);
            }
            if(rows.Count==0){error=null;return true;} // Exact legacy wire1 path.
            if(rows.Count>16384||!em.HasComponent<SkirmishObjectiveClockComponent>(session)||!TryLaunch(em,session,out var launch,out var setup)||!Header(launch,setup,native,out var value))return false;
            foreach(var row in rows)if(!PadMatches(row,launch.Definition))return false;
            rows.Sort((a,b)=>StringComparer.Ordinal.Compare(a.ActorId,b.ActorId));
            var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var row in rows)if(!ids.Add(row.ActorId))return false;
            value.Rows=rows.ToArray();payload=value;error=null;return true;
        }

        internal static bool Validate(Payload value,SkirmishNativeCheckpointSnapshotService.Snapshot saved,
            SkirmishResolvedSetup setup,out string error)
        {
            error="NativeRepairPayloadInvalid";
            if(saved==null||setup==null||!SkirmishNativeRepairCheckpointGuard.NoSerializedRepair(saved))return false;
            if(value==null){error=null;return true;}
            if(value.Version!=1||value.Rows==null||value.Rows.Length==0||value.Rows.Length>16384||
                value.SessionId!=saved.SessionId||value.CatalogId!=setup.CatalogId||value.DefinitionId!=setup.DefinitionId||
                value.DefinitionVersion!=setup.ContentVersion||value.SetupHash!=setup.SetupHash||saved.SetupHash!=setup.SetupHash||
                value.ContentHash!=setup.ContentHash||value.OperationMapId!=setup.OperationMapId||value.LayoutId!=setup.LayoutId||
                value.LayoutBindingFingerprint!=setup.LayoutBindingFingerprint||value.MissionLogicalBindingFingerprint!=setup.MissionLogicalBindingFingerprint||
                !Text(value.SourceId,256)||!Text(value.SourceContentHash,256)||!Text(value.SourceIdentityHash,256)||
                !Text(value.SourceSceneGuid,64)||!Text(value.LayoutSemanticFingerprint,64)||saved.Actors==null||saved.Banks==null||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishExpandedSessionComponent native)||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.SessionParts,out SkirmishObjectiveClockComponent clock)||
                native.IsLegacy!=0||native.SessionId.ToString()!=value.SessionId||native.SetupHash!=value.SetupHash||
                native.Phase!=SkirmishSessionPhase.Playing||!float.IsFinite(clock.ElapsedSeconds)||clock.ElapsedSeconds<0)return false;
            if(!Read(saved.SessionParts,out SkirmishObjectiveStateComponent objective)||objective.Terminal!=0||
                objective.Outcome!=SkirmishOutcomeKind.None||objective.State!=SkirmishObjectiveStateKind.Active)return false;
            var actors=new Dictionary<string,SkirmishNativeCheckpointSnapshotService.Actor>(StringComparer.Ordinal);
            foreach(var actor in saved.Actors)if(actor==null||!actors.TryAdd(actor.Id,actor))return false;
            var ids=new HashSet<string>(StringComparer.Ordinal);var paid=new Dictionary<byte,long>();
            foreach(var row in value.Rows)
            {
                if(!Schema(row)||!ids.Add(row.ActorId)||row.LastClockSeconds>clock.ElapsedSeconds||
                    !actors.TryGetValue(row.ActorId,out var actor)||actor.IsStructure!=0||actor.PrefabKey!=row.ActorPrefabKey||
                    !Read(actor.Parts,out SkirmishAttemptOwnedComponent own)||own.SessionId.ToString()!=value.SessionId||
                    own.StableObjectId.ToString()!=row.ActorId||own.IsStructure!=0||own.FactionId!=row.Faction||
                    !Read(actor.Parts,out Faction faction)||faction.Id!=row.Faction||
                    !Read(actor.Parts,out SkirmishUnitRoleComponent role)||role.Role!=row.Role||role.Category!=row.Category||
                    !Read(actor.Parts,out UnitHealth hp)||hp.Max<=0||hp.Current<0||hp.Current>hp.Max||
                    !Read(actor.Parts,out LocalTransform actorTransform)||!math.all(math.isfinite(actorTransform.Position))||
                    !Read(actor.Parts,out UnitMovementBehavior movement)||movement.UsesVehicleMotion==0||
                    Has<UnitAirComponent>(actor.Parts)||Has<UnitAirMovement>(actor.Parts))return false;
                if((row.Phase==VehicleRepairPhase.Working||row.Phase==VehicleRepairPhase.AccountingFailure)&&
                    (own.CapacityReleased!=0||hp.Current<=0||Has<UnitDeathAnimationComponent>(actor.Parts)))return false;
                if(row.Phase==VehicleRepairPhase.Working)
                {
                    if(!actors.TryGetValue(row.FacilityId,out var facility)||facility.IsStructure!=1||facility.PrefabKey!=row.FacilityPrefabKey||
                        !Read(facility.Parts,out SkirmishAttemptOwnedComponent facilityOwn)||facilityOwn.SessionId.ToString()!=value.SessionId||
                        facilityOwn.StableObjectId.ToString()!=row.FacilityId||facilityOwn.IsStructure!=1||facilityOwn.FactionId!=row.Faction||
                        !Read(facility.Parts,out Faction facilityFaction)||facilityFaction.Id!=row.Faction||
                        !Read(facility.Parts,out SkirmishStructureIdentityComponent identity)||identity.StructureId.ToString()!=SkirmishStructureIds.GroundStaging||
                        !Read(facility.Parts,out UnitHealth facilityHp)||facilityHp.Current<=0||
                        !Read(facility.Parts,out LocalTransform transform)||!Near(transform.Position,row.FacilityPosition)||
                        Has<Parent>(facility.Parts)||Has<UnitDeathAnimationComponent>(facility.Parts))return false;
                }
                paid.TryGetValue(row.Faction,out long prior);paid[row.Faction]=prior+row.PaidMaterials-row.RefundedMaterials;
            }
            foreach(var entry in paid)
            {
                SkirmishNativeCheckpointSnapshotService.Bank bank=null;
                foreach(var candidate in saved.Banks)if(candidate!=null&&candidate.Faction==entry.Key){if(bank!=null)return false;bank=candidate;}
                if(bank==null||!Read(bank.Parts,out FactionTacticalMaterialsComponent materials)||materials.FactionId!=entry.Key||
                    !FactionTacticalMaterialsUtilitySystemHelper.CanAfford(materials,0)||materials.LifetimeRepairSpent<entry.Value||
                    !Read(bank.Parts,out FactionEconomy economy)||economy.FactionId!=entry.Key)return false;
            }
            error=null;return true;
        }

        internal static bool TryPrepare(EntityManager em,Entity session,Payload value,
            SkirmishNativeCheckpointSnapshotService.Snapshot saved,SkirmishResolvedSetup setup,out Prepared prepared,out string error)
        {
            prepared=null;
            if(!Validate(value,saved,setup,out error))return false;
            if(value==null)return true;
            error="NativeRepairFreshBindingInvalid";
            if(!TryLaunch(em,session,out var launch,out var current)||!ReferenceEquals(current,setup)||
                !SourceMatches(value,launch,setup)||!NoPendingRequests(em,session,new FixedString64Bytes(value.SessionId)))return false;
            var result=new Prepared{World=em.WorldUnmanaged.SequenceNumber,Session=session,Setup=setup,Launch=launch,
                PayloadJson=JsonUtility.ToJson(value),SavedJson=SkirmishNativeLegacyBa7Json.EncodeInner(saved)};
            using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var actors=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId.ToString()!=value.SessionId)continue;
                if(!result.Actors.TryAdd(own.StableObjectId.ToString(),actor))return false;
            }
            foreach(var row in value.Rows)
            {
                if(!result.Actors.TryGetValue(row.ActorId,out var actor)||em.HasComponent<VehicleRepairStateComponent>(actor)||
                    em.HasComponent<VehicleRepairRequestComponent>(actor)||!em.HasComponent<SkirmishSharedActorTag>(actor)||
                    !em.HasComponent<UnitSourcePrefabKey>(actor)||em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString()!=row.ActorPrefabKey||
                    !PadMatches(row,launch.Definition))return false;
                if(row.Phase==VehicleRepairPhase.Working&&(!result.Actors.TryGetValue(row.FacilityId,out var facility)||
                    !em.HasComponent<SkirmishSharedActorTag>(facility)||!em.HasComponent<UnitSourcePrefabKey>(facility)||
                    em.GetComponentData<UnitSourcePrefabKey>(facility).Value.ToString()!=row.FacilityPrefabKey))return false;
                if(!UniqueBank(em,row.Faction,out var bank))return false;result.Banks[row.Faction]=bank;
                if(!result.ExpectedBanks.ContainsKey(row.Faction))
                {
                    SkirmishNativeCheckpointSnapshotService.Bank oldBank=null;
                    foreach(var candidate in saved.Banks)if(candidate.Faction==row.Faction)oldBank=candidate;
                    if(oldBank==null)return false;
                    result.ExpectedBanks[row.Faction]=SkirmishNativeCheckpointParts.RebaseClocks(oldBank.Parts,
                        (float)(em.WorldUnmanaged.Time.ElapsedTime-saved.WorldTime),Time.time-saved.UnityTime,unchecked(Time.frameCount-saved.Frame));
                }
            }
            prepared=result;error=null;return true;
        }

        internal static bool TryCommit(EntityManager em,Entity session,Prepared prepared,out string error)
        {
            error="NativeRepairCommitBindingChanged";
            if(prepared==null){error=null;return true;}
            if(prepared.Committed||prepared.RolledBack||prepared.World!=em.WorldUnmanaged.SequenceNumber||prepared.Session!=session||
                !TryLaunch(em,session,out var launch,out var setup)||!ReferenceEquals(launch,prepared.Launch)||!ReferenceEquals(setup,prepared.Setup))return false;
            var value=JsonUtility.FromJson<Payload>(prepared.PayloadJson);
            if(!SkirmishNativeLegacyBa7Json.TryDecodeInner(prepared.SavedJson,out var saved))return false;
            if(!Validate(value,saved,setup,out error)||!SourceMatches(value,launch,setup)||
                !Read(saved.SessionParts,out SkirmishObjectiveClockComponent savedClock)||
                !em.HasComponent<SkirmishObjectiveClockComponent>(session)||
                em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds!=savedClock.ElapsedSeconds||
                !NoPendingRequests(em,session,new FixedString64Bytes(value.SessionId)))return false;
            var staged=new Dictionary<Entity,VehicleRepairStateComponent>();
            foreach(var row in value.Rows)
            {
                if(!prepared.Actors.TryGetValue(row.ActorId,out var actor)||!ActorIdentity(em,actor,row.ActorId,new FixedString64Bytes(value.SessionId),row.Faction,out var role)||
                    role.Role!=row.Role||role.Category!=row.Category||em.HasComponent<VehicleRepairStateComponent>(actor)||
                    !em.HasComponent<UnitSourcePrefabKey>(actor)||em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString()!=row.ActorPrefabKey||
                    !em.HasComponent<UnitMovementBehavior>(actor)||em.GetComponentData<UnitMovementBehavior>(actor).UsesVehicleMotion==0||
                    !UniqueBank(em,row.Faction,out var bank)||bank!=prepared.Banks[row.Faction]||!PadMatches(row,launch.Definition))return false;
                Entity facility=Entity.Null;
                if(prepared.Actors.TryGetValue(row.FacilityId,out var candidate)&&em.Exists(candidate))facility=candidate;
                if(row.Phase==VehicleRepairPhase.Working&&(!FacilityIdentity(em,facility,row.FacilityId,new FixedString64Bytes(value.SessionId),row.Faction,out var position)||
                    !Near(position,row.FacilityPosition)||!em.HasComponent<UnitSourcePrefabKey>(facility)||
                    em.GetComponentData<UnitSourcePrefabKey>(facility).Value.ToString()!=row.FacilityPrefabKey))return false;
                var state=ToState(row,session,value,bank,facility);
                // Validate applied native Parts, not just the prior payload.
                SkirmishNativeCheckpointSnapshotService.Actor savedActor=null;
                foreach(var item in saved.Actors)if(item.Id==row.ActorId)savedActor=item;
                if(savedActor==null||!Read(savedActor.Parts,out UnitHealth oldHp)||!em.HasComponent<UnitHealth>(actor)||
                    em.GetComponentData<UnitHealth>(actor).Current!=oldHp.Current||em.GetComponentData<UnitHealth>(actor).Max!=oldHp.Max||
                    !Read(savedActor.Parts,out LocalTransform oldTransform)||!em.HasComponent<LocalTransform>(actor)||
                    !Near(em.GetComponentData<LocalTransform>(actor).Position,oldTransform.Position))return false;
                SkirmishNativeCheckpointSnapshotService.Bank savedBank=null;
                foreach(var item in saved.Banks)if(item.Faction==row.Faction)savedBank=item;
                if(savedBank==null||!Read(savedBank.Parts,out FactionTacticalMaterialsComponent oldMaterials)||
                    JsonUtility.ToJson(em.GetComponentData<FactionTacticalMaterialsComponent>(bank))!=JsonUtility.ToJson(oldMaterials)||
                    !Read(prepared.ExpectedBanks[row.Faction],out FactionEconomy oldEconomy)||!em.HasComponent<FactionEconomy>(bank)||
                    JsonUtility.ToJson(em.GetComponentData<FactionEconomy>(bank))!=JsonUtility.ToJson(oldEconomy))return false;
                staged.Add(actor,state);
            }
            // All rows preflight before the first component is installed.
            try
            {
                foreach(var pair in staged){em.AddComponentData(pair.Key,pair.Value);prepared.Added.Add(pair.Key);}
                prepared.Committed=true;error=null;return true;
            }
            catch(Exception exception)when(exception is ArgumentException||exception is InvalidOperationException)
            {Rollback(em,prepared);error="NativeRepairCommit:"+exception.Message;return false;}
        }
        internal static void Rollback(EntityManager em,Prepared prepared)
        {
            if(prepared==null||prepared.RolledBack||em.WorldUnmanaged.SequenceNumber!=prepared.World)return;
            foreach(var actor in prepared.Added)if(em.Exists(actor)&&em.HasComponent<VehicleRepairStateComponent>(actor))em.RemoveComponent<VehicleRepairStateComponent>(actor);
            prepared.Added.Clear();prepared.RolledBack=true;prepared.Committed=false;
        }

        private static bool Header(SkirmishLaunchDefinitionRecord launch,SkirmishResolvedSetup setup,in SkirmishExpandedSessionComponent native,out Payload value)
        {
            value=null;var definition=launch.Definition;var source=definition?.CampaignMap;
            if(source==null||definition.MapLayout==null)return false;
            value=new Payload{SessionId=native.SessionId.ToString(),CatalogId=setup.CatalogId,DefinitionId=setup.DefinitionId,
                DefinitionVersion=setup.ContentVersion,SetupHash=setup.SetupHash,ContentHash=setup.ContentHash,OperationMapId=setup.OperationMapId,
                LayoutId=setup.LayoutId,LayoutBindingFingerprint=setup.LayoutBindingFingerprint,MissionLogicalBindingFingerprint=setup.MissionLogicalBindingFingerprint,
                SourceId=source.OperationMapId,SourceContentHash=source.ContentHash,SourceIdentityHash=source.SourceIdentityHash,
                SourceSceneGuid=SkirmishNativeRepairSourceIdentity.SceneGuid(source),LayoutSemanticFingerprint=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(definition.MapLayout)};
            return true;
        }
        private static bool SourceMatches(Payload value,SkirmishLaunchDefinitionRecord launch,SkirmishResolvedSetup setup)
        {
            var source=launch.Definition?.CampaignMap;
            return source!=null&&value.SourceId==source.OperationMapId&&value.SourceContentHash==source.ContentHash&&
                value.SourceIdentityHash==source.SourceIdentityHash&&value.SourceSceneGuid==SkirmishNativeRepairSourceIdentity.SceneGuid(source)&&
                value.LayoutSemanticFingerprint==SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(launch.Definition.MapLayout)&&
                value.SetupHash==setup.SetupHash&&value.ContentHash==setup.ContentHash;
        }
        private static bool TryLaunch(EntityManager em,Entity session,out SkirmishLaunchDefinitionRecord launch,out SkirmishResolvedSetup setup)
        {
            launch=null;setup=null;
            if(!em.Exists(session)||!em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            launch=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            return launch!=null&&launch.Matches(setup,em.GetComponentData<SkirmishExpandedSessionComponent>(session));
        }
        private static bool ActorIdentity(EntityManager em,Entity actor,string id,FixedString64Bytes attempt,byte faction,out SkirmishUnitRoleComponent role)
        {
            role=default;
            if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<Faction>(actor)||
                !em.HasComponent<SkirmishUnitRoleComponent>(actor)||!em.HasComponent<SkirmishSharedActorTag>(actor))return false;
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            return own.SessionId==attempt&&own.StableObjectId.ToString()==id&&own.IsStructure==0&&own.FactionId==faction&&
                em.GetComponentData<Faction>(actor).Id==faction&&Eligible(role.Role,role.Category);
        }
        private static bool FacilityIdentity(EntityManager em,Entity actor,string id,FixedString64Bytes attempt,byte faction,out float3 position)
        {
            position=default;
            if(!em.Exists(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<Faction>(actor)||
                !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<SkirmishStructureIdentityComponent>(actor)||
                !em.HasComponent<LocalTransform>(actor)||em.HasComponent<Parent>(actor)||!em.HasComponent<UnitHealth>(actor)||
                em.GetComponentData<UnitHealth>(actor).Current<=0||em.HasComponent<UnitDeathAnimationComponent>(actor))return false;
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);position=em.GetComponentData<LocalTransform>(actor).Position;
            return own.SessionId==attempt&&own.StableObjectId.ToString()==id&&own.IsStructure!=0&&own.FactionId==faction&&
                own.CapacityReleased==0&&em.GetComponentData<Faction>(actor).Id==faction&&math.all(math.isfinite(position))&&
                em.GetComponentData<SkirmishStructureIdentityComponent>(actor).StructureId.ToString()==SkirmishStructureIds.GroundStaging;
        }
        private static bool UniqueBank(EntityManager em,byte faction,out Entity bank)
        {
            bank=Entity.Null;using var query=em.CreateEntityQuery(typeof(FactionEconomy),typeof(FactionTacticalMaterialsComponent));
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)if(em.GetComponentData<FactionEconomy>(entity).FactionId==faction&&em.GetComponentData<FactionTacticalMaterialsComponent>(entity).FactionId==faction)
            {if(bank!=Entity.Null)return false;bank=entity;}
            return bank!=Entity.Null;
        }
        private static bool NoPendingRequests(EntityManager em,Entity session,FixedString64Bytes attempt)
        {
            using var query=em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<VehicleRepairRequestComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var entities=query.ToEntityArray(Allocator.Temp);
            foreach(var actor in entities)if(em.GetComponentData<VehicleRepairRequestComponent>(actor).Session==session||
                em.HasComponent<SkirmishAttemptOwnedComponent>(actor)&&em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).SessionId==attempt)return false;
            return true;
        }
        private static bool PadMatches(Row row,SkirmishScenarioDefinitionConfig definition)=>definition.MapLayout.TryGetPad(row.Faction,SkirmishLegalPadKind.Service,out var pad)&&
            pad.PadId==row.PadId&&definition.MapLayout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z)&&Near(new float3(x,0,z),row.PadPosition);
        private static bool Eligible(SkirmishRoleKind role,SkirmishPopulationCategory category)=>SkirmishRoleIds.Category(role)==category&&
            (category==SkirmishPopulationCategory.Ground||category==SkirmishPopulationCategory.LogisticsSupport||category==SkirmishPopulationCategory.ObjectiveSupport&&role==SkirmishRoleKind.ConvoyLogic);
        private static bool Text(string value,int limit)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=limit&&value.IndexOf('\0')<0;
        private static bool Id(string value)=>Text(value,61)&&Encoding.UTF8.GetByteCount(value)<=61;
        private static bool Near(float3 a,float3 b)=>math.all(math.isfinite(a))&&math.all(math.isfinite(b))&&math.distancesq(a,b)<=.000001f;
        private static bool Read<T>(SkirmishNativeCheckpointParts.Part[] parts,out T value)where T:unmanaged,IComponentData=>SkirmishNativeCheckpointParts.TryReadComponent(parts,out value);
        private static bool Has<T>(SkirmishNativeCheckpointParts.Part[] parts)where T:unmanaged,IComponentData
        {if(parts!=null)foreach(var part in parts)if(part?.Type==typeof(T).FullName)return true;return false;}
        private static bool Schema(Row row)
        {
            if(row==null||!Id(row.ActorId)||!Id(row.FacilityId)||!Id(row.PadId)||!Text(row.ActorPrefabKey,256)||row.Faction<1||row.Faction>2||
                !Eligible(row.Role,row.Category)||row.RequestId==0||!Enum.IsDefined(typeof(VehicleRepairPhase),row.Phase)||row.Phase==VehicleRepairPhase.None||
                row.IncomingDamage>1||row.NewMovement>1||row.FractionDenominator<=0||row.FractionDenominator>4L*int.MaxValue||
                row.FractionNumerator<=0||row.FractionNumerator>row.FractionDenominator/4||row.DurationTicks<=0||row.RemainingTicks<0||row.RemainingTicks>row.DurationTicks||
                !double.IsFinite(row.LastClockSeconds)||row.LastClockSeconds<0||!double.IsFinite(row.FractionalTicks)||row.FractionalTicks<0||row.FractionalTicks>=1||
                row.LastMaximum<=0||row.LastHealth<=0||row.LastHealth>row.LastMaximum||!Near(row.AcceptedPosition,row.AcceptedPosition)||
                !Near(row.PadPosition,row.PadPosition)||!Near(row.FacilityPosition,row.FacilityPosition)||
                math.distancesq(row.AcceptedPosition.xz,row.PadPosition.xz)>144)return false;
            long materials=(80*row.FractionNumerator+row.FractionDenominator-1)/row.FractionDenominator;
            long ticks=(80*TimeSpan.TicksPerSecond*row.FractionNumerator+row.FractionDenominator-1)/row.FractionDenominator;
            if(row.PaidMaterials!=materials||row.DurationTicks!=ticks||row.RefundDue<0||row.RefundedMaterials<0||row.RefundedMaterials>row.RefundDue)return false;
            if(row.Phase==VehicleRepairPhase.Working)return row.RemainingTicks>0&&row.RefundDue==0&&row.RefundedMaterials==0&&Text(row.FacilityPrefabKey,256);
            int expected=row.Phase==VehicleRepairPhase.Cancelled||row.Phase==VehicleRepairPhase.AccountingFailure?
                VehicleRepairRules.Refund(row.PaidMaterials,row.DurationTicks-row.RemainingTicks,false):0;
            return row.RefundDue==expected&&(row.Phase==VehicleRepairPhase.AccountingFailure?row.RefundedMaterials<expected:row.RefundedMaterials==expected)&&
                (row.Phase!=VehicleRepairPhase.Completed||row.RemainingTicks==0&&row.IncomingDamage==0&&row.NewMovement==0);
        }
        private static Row FromState(in VehicleRepairStateComponent state)=>new Row{ActorId=state.ActorStableId.ToString(),FacilityId=state.ServiceFacilityStableId.ToString(),
            PadId=state.PadId.ToString(),Faction=state.Faction,IncomingDamage=state.IncomingDamage,NewMovement=state.NewMovement,Phase=state.Phase,RequestId=state.RequestId,
            AcceptedPosition=state.AcceptedPosition,PadPosition=state.PadPosition,FacilityPosition=state.ServiceFacilityPosition,FractionNumerator=state.FractionNumerator,
            FractionDenominator=state.FractionDenominator,DurationTicks=state.DurationTicks,RemainingTicks=state.RemainingTicks,LastClockSeconds=state.LastClockSeconds,
            FractionalTicks=state.FractionalTicks,PaidMaterials=state.PaidMaterials,RefundDue=state.RefundDue,RefundedMaterials=state.RefundedMaterials,LastHealth=state.LastHealth,LastMaximum=state.LastMaximum};
        private static VehicleRepairStateComponent ToState(Row row,Entity session,Payload value,Entity bank,Entity facility)=>new VehicleRepairStateComponent{
            Session=session,ServiceFacility=facility,MaterialsBank=bank,AttemptId=new FixedString64Bytes(value.SessionId),ActorStableId=new FixedString64Bytes(row.ActorId),
            PadId=new FixedString64Bytes(row.PadId),ServiceFacilityStableId=new FixedString64Bytes(row.FacilityId),Faction=row.Faction,IncomingDamage=row.IncomingDamage,NewMovement=row.NewMovement,
            Phase=row.Phase,RequestId=row.RequestId,SetupHash=value.SetupHash,AcceptedPosition=row.AcceptedPosition,PadPosition=row.PadPosition,ServiceFacilityPosition=row.FacilityPosition,
            FractionNumerator=row.FractionNumerator,FractionDenominator=row.FractionDenominator,DurationTicks=row.DurationTicks,RemainingTicks=row.RemainingTicks,
            LastClockSeconds=row.LastClockSeconds,FractionalTicks=row.FractionalTicks,PaidMaterials=row.PaidMaterials,RefundDue=row.RefundDue,RefundedMaterials=row.RefundedMaterials,
            LastHealth=row.LastHealth,LastMaximum=row.LastMaximum};
    }
}
