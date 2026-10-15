using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    // AOT-safe component codecs. No Entity handle or native pointer crosses the save boundary.
    // The recovery owner validates/recreates the complete roster before applying these parts.
    internal static partial class SkirmishNativeCheckpointParts
    {
        [Serializable] internal sealed class Part
        {
            public string Type;
            public ulong Layout;
            public string Data;
            public int Count;
            public bool Enabled = true;
            public Reference[] References;
        }
        [Serializable] internal sealed class Reference { public int Offset; public string ObjectId; }
        private sealed class Entry
        {
            public Func<EntityManager, Entity, bool> Present;
            public Func<EntityManager, Entity, Func<Entity,string>, Part> Capture;
            public Action<EntityManager, Entity, Part, Func<string,Entity>> Apply;
            public Func<Part,bool> Validate;
            public Action<EntityManager,Entity> Remove;
        }
        private static readonly Dictionary<string,Entry> Codecs = Create();
        private static Dictionary<string,Entry> Create()
        {
            var codecs = new Dictionary<string,Entry>(StringComparer.Ordinal);
            void Register<T>(Func<EntityManager, T, Func<Entity,string>, T> normalize = null) where T : unmanaged, IComponentData
            {
                TypeIndex index=TypeManager.GetTypeIndex<T>();
                var info=TypeManager.GetTypeInfo(index);
                if (index.HasNativeContainer || info.HasBlobAssetRefs || info.HasUnityObjectRefs || info.HasWeakAssetRefs)
                    throw new InvalidOperationException("Checkpoint codec cannot serialize world-owned memory: " + typeof(T).Name);
                bool tag=info.IsZeroSized;
                codecs.Add(typeof(T).FullName, new Entry {
                    Present=(em,e)=>em.HasComponent<T>(e),
                    Capture=(em,e,id)=> {
                        T value = tag ? default : em.GetComponentData<T>(e);
                        if (normalize != null) value = normalize(em, value, id);
                        var part = Build<T>(tag ? Array.Empty<byte>() : Pack(value),1,index,id);
                        part.Enabled = !index.IsEnableable || em.IsComponentEnabled(e, ComponentType.ReadWrite<T>());
                        return part;
                    },
                    Apply=(em,e,p,resolve)=> {
                        if (tag) {
                            if (!em.HasComponent<T>(e)) em.AddComponent<T>(e);
                            if (index.IsEnableable) em.SetComponentEnabled(e, ComponentType.ReadWrite<T>(), p.Enabled);
                            return;
                        }
                        T value=Unpack<T>(Patch(p,resolve));
                        if (em.HasComponent<T>(e)) em.SetComponentData(e,value); else em.AddComponentData(e,value);
                        if (index.IsEnableable) em.SetComponentEnabled(e, ComponentType.ReadWrite<T>(), p.Enabled);
                    },
                    Validate=p=>Valid<T>(p,index,tag,false),
                    Remove=(em,e)=> { if(em.HasComponent<T>(e)) em.RemoveComponent<T>(e); }
                });
            }
            void RegisterBuffer<T>() where T : unmanaged, IBufferElementData
            {
                TypeIndex index=TypeManager.GetTypeIndex<T>();
                if (index.HasNativeContainer || TypeManager.GetTypeInfo(index).HasBlobAssetRefs ||
                    TypeManager.GetTypeInfo(index).HasUnityObjectRefs || TypeManager.GetTypeInfo(index).HasWeakAssetRefs)
                    throw new InvalidOperationException("Checkpoint buffer cannot serialize world-owned memory: " + typeof(T).Name);
                codecs.Add(typeof(T).FullName,new Entry {
                    Present=(em,e)=>em.HasBuffer<T>(e),
                    Capture=(em,e,id)=> {
                        using var values=em.GetBuffer<T>(e,true).ToNativeArray(Allocator.Temp);
                        if(typeof(T)==typeof(SupportReceiptElement))
                        {
                            var receipts=values.Reinterpret<SupportReceiptElement>();
                            for(int i=0;i<receipts.Length;i++)
                            {
                                var receipt=receipts[i];
                                if(receipt.Effect!=Entity.Null && !em.Exists(receipt.Effect))
                                {receipt.Effect=Entity.Null;receipts[i]=receipt;}
                            }
                        }
                        if(typeof(T)==typeof(SkirmishContactKnowledgeElement))
                        {
                            var knowledge=values.Reinterpret<SkirmishContactKnowledgeElement>();
                            for(int i=0;i<knowledge.Length;i++)
                            {
                                var value=knowledge[i];
                                if(value.ObservationSource!=Entity.Null)
                                {
                                    Entity source=value.ObservationSource;
                                    bool valid=em.Exists(source) && !em.HasComponent<Disabled>(source) &&
                                        !em.HasComponent<UnitTransportPassenger>(source) && !em.HasComponent<UnitDeathAnimationComponent>(source) &&
                                        em.HasComponent<SkirmishAttemptOwnedComponent>(source) &&
                                        em.HasComponent<SkirmishAttemptOwnedComponent>(e);
                                    if(valid)
                                    {
                                        var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(source);
                                        var contact=em.GetComponentData<SkirmishAttemptOwnedComponent>(e);
                                        valid=owner.CapacityReleased==0 && owner.SessionId.Equals(contact.SessionId) &&
                                            owner.StableObjectId.Equals(value.ObservationSourceStableObjectId) &&
                                            (!em.HasComponent<UnitHealth>(source) || em.GetComponentData<UnitHealth>(source).Current>0);
                                    }
                                    if(!valid || string.IsNullOrEmpty(id(source)))
                                    { value.ObservationSource=Entity.Null; knowledge[i]=value; }
                                }
                            }
                        }
                        var part = Build<T>(values.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()).ToArray(),values.Length,index,id);
                        part.Enabled = !index.IsEnableable || em.IsComponentEnabled(e, ComponentType.ReadWrite<T>());
                        return part;
                    },
                    Apply=(em,e,p,resolve)=> {
                        var bytes=Patch(p,resolve);
                        using var values=new NativeArray<byte>(bytes,Allocator.Temp);
                        var buffer=em.HasBuffer<T>(e) ? em.GetBuffer<T>(e) : em.AddBuffer<T>(e);
                        buffer.CopyFrom(values.Reinterpret<T>(1));
                        if (index.IsEnableable) em.SetComponentEnabled(e, ComponentType.ReadWrite<T>(), p.Enabled);
                    },
                    Validate=p=>Valid<T>(p,index,false,true),
                    Remove=(em,e)=> { if(em.HasBuffer<T>(e)) em.RemoveComponent<T>(e); }
                });
            }
            Register<SkirmishSupportOwnerComponent>();
            Register<SupportSessionComponent>();
            Register<SupportMissionPolicyComponent>();
            Register<SupportInputStateComponent>();
            Register<SupportPreviewComponent>();
            Register<SupportProposalComponent>();
            Register<SupportFuelScopeComponent>();
            Register<SupportFuelAvailabilityComponent>();
            Register<SupportSmokeZoneComponent>();
            Register<SupportAirRouteComponent>();
            Register<SupportFlightComponent>();
            Register<SupportFlightCleanupComponent>();
            RegisterBuffer<SupportFuelReservationElement>();
            Register<SupportRangedCoverComponent>();
            RegisterBuffer<SupportAbilityStateElement>();
            RegisterBuffer<SupportGroundRegionElement>();
            RegisterBuffer<SupportRequestElement>();
            RegisterBuffer<SupportReceiptElement>();
            Register<LocalTransform>();
            Register<Faction>();
            Register<FactionEconomy>();
            Register<FactionEconomyPolicy>();
            Register<FactionTacticalMaterialsComponent>();
            Register<FactionMaterialFabricationTelemetryComponent>();
            Register<FactionFuelLogisticsTelemetryComponent>();
            Register<SkirmishSharedMaterialsInitialized>();
            Register<SkirmishSharedSupplyInitialized>();
            Register<UnitHealth>();
            Register<UnitGrid>();
            Register<UnitPrevWorldPos>();
            Register<UnitMoveVisualComponent>();
            Register<UnitIdleWanderComponent>();
            Register<UnitAttackCooldownComponent>();
            Register<UnitAttackTraceComponent>();
            Register<UnitAttackAnimationComponent>();
            Register<UnitDeathAnimationComponent>();
            Register<UnitTarget>();
            Register<UnitLongDistanceMove>();
            Register<UnitCombat>();
            Register<UnitAttack>();
            Register<UnitMovementBehavior>();
            Register<UnitRotationHold>();
            Register<ScanIntelLastSeen>();
            Register<UnitAirComponent>();
            Register<UnitAirMovement>();
            Register<UnitSpawnTransitTag>();
            Register<UnitFuelConsumptionState>();
            Register<UnitVehicleKinematics>();
            Register<UnitVehicleMovement>();
            Register<UnitResourceHauler>();
            Register<UnitResourceHaulOrder>();
            Register<UnitResourceHaulStatus>();
            Register<UnitResourceHaulReservation>();
            Register<UnitTransportPassenger>();
            Register<UnitTransportCapacity>();
            Register<UnitTransportCargoCapacity>();
            Register<UnitTransportPlaneDoorState>();
            Register<UnitTransportPlaneDoorOpenRequest>();
            Register<UnitTransportRopeDisembarkRequest>();
            Register<UnitTransportAirdropRequest>();
            Register<UnitTransportCargoPassenger>();
            Register<UnitTransportBoardingTarget>();
            Register<UnitTransportDeployOrder>();
            Register<UnitTransportDeployAttackTarget>();
            Register<UnitTransportRopeDropComponent>();
            Register<UnitTransportRopeLandingClearance>();
            Register<UnitTransportParachuteDropComponent>();
            Register<UnitTransportCargoDropComponent>();
            Register<UnitTransportAirdropSettleComponent>();
            Register<UnitTransportDropVisualSource>();
            Register<UnitTransportAirdropVisualCleanup>();
            Register<UnitTransportRopeDisperseComponent>();
            Register<UnitScanOrder>();
            Register<AttackMoveOrder>();
            Register<EngageTarget>();
            Register<CombatTargetPolicy>();
            Register<RuntimeBuildingCombatInfo>();
            Register<OperationMapBuildingIdentity>();
            Register<BuildingResourceStorageComponent>();
            Register<BuildingResourceProductionProfileComponent>();
            Register<MaterialFabricationComponent>();
            Register<MaterialFabricationCommandQueueComponent>();
            Register<GroundMissileLauncherStateComponent>();
            Register<GroundMissileInFlightComponent>();
            Register<GroundMissileProjectileComponent>();
            Register<GroundMissileProjectileTrailComponent>();
            Register<GroundMissileImpactRequestComponent>();
            Register<GroundMissileFlyingRocketVisualComponent>();
            Register<AirMissileProjectileComponent>();
            Register<AirMissileProjectileTrailComponent>();
            Register<AirMissileImpactRequestComponent>();
            Register<AirMissileFlyingVisualComponent>();
            Register<MissileInterceptionTargetComponent>();
            Register<MissileInterceptedComponent>();
            Register<AirMissileLauncherStateComponent>();
            Register<AirMissileLauncherTargetComponent>();
            Register<SkirmishAttemptOwnedComponent>();
            Register<SkirmishUnitRoleComponent>();
            Register<SkirmishRoleOverlayComponent>();
            Register<SkirmishObjectiveRoleComponent>();
            Register<SkirmishConvoyObjectiveTruckComponent>();
            Register<SkirmishStructureIdentityComponent>();
            Register<SkirmishMoveIntentComponent>(NormalizeExpiredMoveTarget);
            Register<SkirmishArmyGroupMembershipComponent>();
            Register<SkirmishContactSightComponent>();
            RegisterBuffer<SkirmishContactKnowledgeElement>();
            RegisterBuffer<SkirmishContactLedgerElement>();
            Register<SkirmishContactLedgerStateComponent>();
            Register<SkirmishUpgradeStampComponent>();
            Register<SkirmishExpandedSessionComponent>();
            Register<SkirmishObjectiveClockComponent>();
            Register<SkirmishObjectiveStateComponent>();
            Register<SkirmishBaseAssaultFactComponent>();
            Register<SkirmishCapacityComponent>();
            Register<SkirmishEconomyStockComponent>();
            Register<SkirmishVehicleFuelRemainderComponent>();
            Register<SkirmishResearchStateComponent>();
            Register<SkirmishEnemyResearchStateComponent>();
            Register<SkirmishEnemyStockComponent>();
            Register<SkirmishEnemyCapacityComponent>();
            Register<SkirmishEnemyStrategyComponent>();
            Register<SkirmishEnemyCapabilityStateV1Component>();
            RegisterBuffer<SkirmishEnemyCapabilityOperationV1>();
            RegisterBuffer<SkirmishEnemyTransportPlanV1>();
            Register<SkirmishEnemyPlannerV1Component>();
            Register<SkirmishEnemyProductionIntentV1Component>();
            Register<SkirmishEnemyConstructionIntentV1Component>(SkirmishEnemyConstructionCheckpoint.NormalizeCapture);
            RegisterBuffer<SkirmishEnemyGroupAssignmentV1>();
            Register<SkirmishArmySelectionComponent>();
            Register<SkirmishFogStateComponent>();
            Register<SkirmishResultComponent>();
            Register<SkirmishMatchState>();
            Register<HoldPositionOrderTag>();
            Register<ManualControlledTag>();
            Register<ManualMoveOrderTag>();
            Register<ManualMoveGroupMemberTag>();
            Register<SelectedUnitTag>();
            Register<Disabled>();
            RegisterBuffer<UnitTransportPassengerElement>();
            RegisterBuffer<SkirmishProductionReservation>();
            RegisterBuffer<SkirmishResearchQueueItem>();
            RegisterBuffer<SkirmishArmyGroupRecord>();
            RegisterBuffer<SkirmishArmyDrawerSlot>();
            RegisterBuffer<SkirmishReadinessRequest>();
            RegisterBuffer<MaterialFabricationRequestComponent>();
            RegisterBuffer<MaterialFabricationResultComponent>();
            return codecs;
        }
        private static SkirmishMoveIntentComponent NormalizeExpiredMoveTarget(
            EntityManager em, SkirmishMoveIntentComponent intent, Func<Entity,string> id)
        {
            Entity target = intent.AttackTarget;
            if (target == Entity.Null || !string.IsNullOrEmpty(id(target))) return intent;
            // Combat can destroy a target between the last order update and Pause.
            // Normalize the captured value only; never mutate the paused actor or
            // discard a live target, passenger carrier, producer or paid receipt.
            if (!em.Exists(target) || em.HasComponent<UnitHealth>(target) &&
                em.GetComponentData<UnitHealth>(target).Current <= 0)
            {
                intent.AttackTarget = Entity.Null;
                intent.Engaged = 0;
                return intent;
            }
            throw new InvalidOperationException("Checkpoint reference has no stable identity: SkirmishMoveIntentComponent" +
                " target=" + target + " health=" + (em.HasComponent<UnitHealth>(target)
                    ? em.GetComponentData<UnitHealth>(target).Current.ToString() : "absent") +
                " owned=" + em.HasComponent<SkirmishAttemptOwnedComponent>(target));
        }
        internal static Part[] Capture(EntityManager em,Entity entity,Func<Entity,string> id)
        {
            var parts=new List<Part>();
            foreach(var entry in Codecs.Values) if(entry.Present(em,entity)) parts.Add(entry.Capture(em,entity,id));
            return parts.ToArray();
        }
        internal static bool Validate(Part[] parts,ISet<string> ids)
        {
            if(parts==null || ids==null || parts.Length>Codecs.Count) return false;
            var types=new HashSet<string>();
            try {
                foreach(var part in parts) {
                    if(part==null || !types.Add(part.Type) || !Codecs.TryGetValue(part.Type,out var entry) || !entry.Validate(part)) return false;
                    foreach(var reference in part.References)
                        if(!string.IsNullOrEmpty(reference.ObjectId) && !ids.Contains(reference.ObjectId)) return false;
                }
                return true;
            } catch(Exception e) when(e is ArgumentException || e is FormatException || e is OverflowException) { return false; }
        }
        internal static bool TryReadComponent<T>(Part[] parts, out T value) where T : unmanaged, IComponentData
        {
            value = default;
            if (parts == null) return false;
            foreach (var part in parts)
            {
                if (part?.Type != typeof(T).FullName) continue;
                try
                {
                    if (!Codecs.TryGetValue(part.Type, out var codec) || !codec.Validate(part)) return false;
                    value = Unpack<T>(Convert.FromBase64String(part.Data));
                    return true;
                }
                catch (Exception exception) when (exception is ArgumentException || exception is FormatException || exception is OverflowException)
                { return false; }
            }
            return false;
        }
        internal static bool TryReadBuffer<T>(Part[] parts,out T[] values) where T:unmanaged,IBufferElementData
        {
            values=null;
            if(parts==null)return false;
            foreach(var part in parts) if(part?.Type==typeof(T).FullName)
            {
                try
                {
                    if(!Codecs.TryGetValue(part.Type,out var codec) || !codec.Validate(part))return false;
                    using var bytes=new NativeArray<byte>(Convert.FromBase64String(part.Data),Allocator.Temp);
                    values=bytes.Reinterpret<T>(1).ToArray();return true;
                }
                catch(Exception ex) when(ex is ArgumentException || ex is FormatException || ex is OverflowException){return false;}
            }
            return false;
        }
        internal static void Apply(EntityManager em,Entity entity,Part[] parts,Func<string,Entity> resolve)
        {
            if (!em.Exists(entity)) throw new ArgumentException("Checkpoint destination does not exist.");
            if (resolve == null) throw new ArgumentNullException(nameof(resolve));
            // Resolve every handle before structural changes. A failed lookup must leave
            // the destination's components and buffers untouched.
            var references = new Dictionary<string, Entity>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var resolvedEntities = new HashSet<Entity>();
            if (parts == null) throw new ArgumentException("Checkpoint parts are missing.");
            foreach (var part in parts)
            {
                if (part?.References == null) throw new ArgumentException("Checkpoint references are missing.");
                foreach (var reference in part.References)
                {
                    if (reference == null || reference.ObjectId == null)
                        throw new ArgumentException("Checkpoint reference is invalid.");
                    if (reference.ObjectId.Length == 0 || !ids.Add(reference.ObjectId)) continue;
                    Entity resolved = resolve(reference.ObjectId);
                    if (resolved == Entity.Null || !em.Exists(resolved) || !resolvedEntities.Add(resolved))
                        throw new ArgumentException("Checkpoint reference was not reconstructed: " + reference.ObjectId);
                    references.Add(reference.ObjectId, resolved);
                }
            }
            if (!Validate(parts, ids)) throw new ArgumentException("Checkpoint component preflight failed.");
            Entity ResolveValidated(string id) => references[id];
            // Call only after every record and reference has passed recovery-owner preflight.
            var present=new HashSet<string>();
            foreach(var part in parts) present.Add(part.Type);
            foreach(var pair in Codecs) if(!present.Contains(pair.Key)) pair.Value.Remove(em,entity);
            foreach(var part in parts) Codecs[part.Type].Apply(em,entity,part,ResolveValidated);
            // Navigation buffers contain world-owned indices. Replan the restored destination.
            if(em.HasComponent<UnitPathFollow>(entity)) em.RemoveComponent<UnitPathFollow>(entity);
            if(em.HasComponent<UnitPathRange>(entity)) em.RemoveComponent<UnitPathRange>(entity);
            if(em.HasComponent<UnitTarget>(entity)) {
                var request=new UnitPathRequest { Goal=em.GetComponentData<UnitTarget>(entity).Cell };
                if(em.HasComponent<UnitPathRequest>(entity)) em.SetComponentData(entity,request); else em.AddComponentData(entity,request);
            } else if(em.HasComponent<UnitPathRequest>(entity)) em.RemoveComponent<UnitPathRequest>(entity);
        }
        private static unsafe Part Build<T>(byte[] data,int count,TypeIndex index,Func<Entity,string> id) where T:unmanaged
        {
            var references=new List<Reference>();
            var offsets=TypeManager.GetEntityOffsets(index,out int offsetCount);
            fixed(byte* bytes=data) for(int item=0;item<count;item++) for(int i=0;i<offsetCount;i++) {
                int offset=item*UnsafeUtility.SizeOf<T>()+offsets[i].Offset;
                Entity entity=UnsafeUtility.ReadArrayElement<Entity>(bytes+offset,0);
                string key=entity==Entity.Null ? "" : id(entity);
                if(entity!=Entity.Null && string.IsNullOrEmpty(key)) throw new InvalidOperationException("Checkpoint reference has no stable identity: "+typeof(T).Name);
                references.Add(new Reference { Offset=offset,ObjectId=key });
                UnsafeUtility.WriteArrayElement(bytes+offset,0,Entity.Null);
            }
            return new Part { Type=typeof(T).FullName,Layout=TypeManager.GetTypeInfo(index).StableTypeHash,
                Data=Convert.ToBase64String(data),Count=count,References=references.ToArray() };
        }
        private static unsafe bool Valid<T>(Part part,TypeIndex index,bool tag,bool buffer) where T:unmanaged
        {
            if(part.Type!=typeof(T).FullName || part.Layout!=TypeManager.GetTypeInfo(index).StableTypeHash ||
                part.Count<0 || part.Count>65536 || (!buffer && part.Count!=1) || part.References==null) return false;
            byte[] data=Convert.FromBase64String(part.Data ?? "");
            if(data.Length!=(tag ? 0 : checked(part.Count*UnsafeUtility.SizeOf<T>()))) return false;
            var offsets=TypeManager.GetEntityOffsets(index,out int count);
            if(part.References.Length!=count*part.Count) return false;
            int r=0;
            fixed(byte* bytes=data) for(int item=0;item<part.Count;item++) for(int i=0;i<count;i++) {
                int offset=item*UnsafeUtility.SizeOf<T>()+offsets[i].Offset;
                var reference=part.References[r++];
                if(reference==null || reference.Offset!=offset || reference.ObjectId==null ||
                    UnsafeUtility.ReadArrayElement<Entity>(bytes+offset,0)!=Entity.Null) return false;
            }
            return true;
        }
        private static unsafe byte[] Patch(Part part,Func<string,Entity> resolve)
        {
            byte[] data=Convert.FromBase64String(part.Data ?? "");
            fixed(byte* bytes=data) foreach(var reference in part.References) {
                Entity entity=string.IsNullOrEmpty(reference.ObjectId) ? Entity.Null : resolve(reference.ObjectId);
                if(!string.IsNullOrEmpty(reference.ObjectId) && entity==Entity.Null) throw new InvalidOperationException("Checkpoint actor was not reconstructed.");
                UnsafeUtility.WriteArrayElement(bytes+reference.Offset,0,entity);
            }
            return data;
        }
        private static byte[] Pack<T>(T value) where T:unmanaged
        {
            using var values=new NativeArray<T>(new[] { value },Allocator.Temp);
            return values.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()).ToArray();
        }
        private static T Unpack<T>(byte[] bytes) where T:unmanaged
        {
            if(bytes.Length!=UnsafeUtility.SizeOf<T>()) throw new InvalidOperationException("Checkpoint component layout changed.");
            using var values=new NativeArray<byte>(bytes,Allocator.Temp);return values.Reinterpret<T>(1)[0];
        }
    }
}
