using System;
using System.Collections.Generic;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    internal struct SkirmishNativeTransientOwner : IComponentData
    {
        public FixedString64Bytes SessionId;
    }

    // Checkpoint-local aliases reconstruct native missile state through the fresh
    // launcher's baked hierarchy. Render handles and Parent values never cross worlds.
    internal static class SkirmishNativeCheckpointTransientState
    {
        [Serializable] internal sealed class Visual
        {
            public string Id, LauncherId, ParentId;
            public byte Kind, Detached;
            public int Slot;
            public SkirmishNativeCheckpointParts.Part[] Parts;
        }

        [Serializable] internal sealed class Transient
        {
            public string Id;
            public byte Kind;
            public SkirmishNativeCheckpointParts.Part[] Parts;
        }

        [Serializable] internal sealed class DropVisual
        {
            public string Id, CarrierSourceKey;
            public byte PassengerKind;
            public SkirmishNativeCheckpointParts.Part[] Parts;
        }

        internal sealed class Bindings
        {
            internal readonly Dictionary<string, Entity> Identities = new(StringComparer.Ordinal);
            internal readonly List<Entity> Created = new();
            internal readonly List<Entity> Detached = new();
        }

        internal static void Capture(EntityManager em, IReadOnlyList<Entity> actors,
            Dictionary<Entity, string> identities, HashSet<string> used,
            out Visual[] visuals, out Transient[] transients)
            => Capture(em, actors, identities, used, out visuals, out transients, out _);

        internal static void Capture(EntityManager em, IReadOnlyList<Entity> actors,
            Dictionary<Entity, string> identities, HashSet<string> used,
            out Visual[] visuals, out Transient[] transients, out DropVisual[] drops)
        {
            using var support = em.CreateEntityQuery(new EntityQueryDesc
            {
                Any = new[] { ComponentType.ReadOnly<SupportFlightComponent>(),
                    ComponentType.ReadOnly<SupportSupplyCrateComponent>(),
                    ComponentType.ReadOnly<SupportPassengerOwnerComponent>() }
            });
            using var supportEffects = support.ToEntityArray(Allocator.Temp);
            foreach(var effect in supportEffects)
                if(!identities.ContainsKey(effect))throw new InvalidOperationException("NativeUnsupportedSupportEffects");
            using(var nativeSupportCapacity=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<SkirmishSupportPassengerCapacity>()},Options=EntityQueryOptions.IncludeDisabledEntities}))
                if(nativeSupportCapacity.CalculateEntityCount()!=0)throw new InvalidOperationException("NativeUnsupportedSupportCapacityRecovery");
            using var cleanupQuery=em.CreateEntityQuery(new EntityQueryDesc {Any=new[] {
                ComponentType.ReadOnly<SupportFlightCleanupComponent>(),ComponentType.ReadOnly<SupportFuelReservationElement>(),
                ComponentType.ReadOnly<SupportPassengerCleanupComponent>(),ComponentType.ReadOnly<SkirmishSupportPassengerCapacity>(),ComponentType.ReadOnly<SupportPassengerReservationElement>() }});
            using var cleanupEffects=cleanupQuery.ToEntityArray(Allocator.Temp);
            foreach(var effect in cleanupEffects)
                if(!identities.ContainsKey(effect))throw new InvalidOperationException("NativeUnsupportedSupportCleanup");

            using var supportRoots = em.CreateEntityQuery(typeof(SupportSessionComponent));
            using var roots = supportRoots.ToEntityArray(Allocator.Temp);
            foreach (var root in roots)
                if (em.GetComponentData<SupportSessionComponent>(root).Active != 0 && !identities.ContainsKey(root))
                    throw new InvalidOperationException("NativeUnsupportedSupportSession");
            var dropRecords = new List<DropVisual>();
            var dropEntities = new List<Entity>();
            var visualRecords = new List<Visual>();
            var visualEntities = new List<Entity>();
            var transientRecords = new List<Transient>();
            var transientEntities = new List<Entity>();
            string Add(Entity entity, string id)
            {
                if (entity == Entity.Null) return string.Empty;
                if (!em.Exists(entity)) throw new InvalidOperationException("NativeMissileHierarchyMissing:" + id);
                if (identities.TryGetValue(entity, out string existing)) return existing;
                if (!used.Add(id)) throw new InvalidOperationException("NativeMissileAliasDuplicate:" + id);
                identities.Add(entity, id);
                return id;
            }
            void VisualSlot(Entity launcher, Entity visual, byte kind, int slot)
            {
                if (slot < 0 || visual == Entity.Null || !em.Exists(visual) || !em.HasComponent<LocalTransform>(visual))
                    throw new InvalidOperationException("NativeMissileSlotMissing");
                string owner = identities[launcher];
                Entity parent = em.HasComponent<Parent>(visual) ? em.GetComponentData<Parent>(visual).Value : Entity.Null;
                if (kind == 1 && em.HasComponent<GroundMissileFlyingRocketVisualComponent>(visual))
                    parent = em.GetComponentData<GroundMissileFlyingRocketVisualComponent>(visual).OriginalParent;
                if (kind == 2 && em.HasComponent<AirMissileFlyingVisualComponent>(visual))
                    parent = em.GetComponentData<AirMissileFlyingVisualComponent>(visual).OriginalParent;
                string id = "missile-visual/" + kind + "/" + owner + "/" + slot;
                string actual = Add(visual, id);
                if (actual != id) throw new InvalidOperationException("NativeMissileSlotAliasedTwice");
                visualRecords.Add(new Visual { Id = id, LauncherId = owner, Kind = kind, Slot = slot,
                    ParentId = Add(parent, "missile-parent/" + kind + "/" + owner + "/" + slot),
                    Detached = (byte)(em.HasComponent<Parent>(visual) ? 0 : 1) });
                visualEntities.Add(visual);
            }
            foreach (Entity actor in actors)
            {
                if (em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(actor))
                    foreach (var slot in em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(actor, true))
                        VisualSlot(actor, slot.Rocket, 1, slot.SlotIndex);
                if (em.HasBuffer<AirMissileLauncherMissileVisualComponent>(actor))
                    foreach (var slot in em.GetBuffer<AirMissileLauncherMissileVisualComponent>(actor, true))
                        VisualSlot(actor, slot.Missile, 2, slot.SlotIndex);
            }
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                Any = new[] { ComponentType.ReadOnly<GroundMissileProjectileComponent>(),
                    ComponentType.ReadOnly<AirMissileProjectileComponent>(),
                    ComponentType.ReadOnly<GroundMissileImpactRequestComponent>(),
                    ComponentType.ReadOnly<AirMissileImpactRequestComponent>() }
            });
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                // An air missile can be the launcher's baked visual entity.
                if (identities.ContainsKey(entity)) continue;
                byte kind = em.HasComponent<GroundMissileProjectileComponent>(entity) ? (byte)1 :
                    em.HasComponent<AirMissileProjectileComponent>(entity) ? (byte)2 :
                    em.HasComponent<GroundMissileImpactRequestComponent>(entity) ? (byte)3 : (byte)4;
                string id = "missile-state/" + transientRecords.Count;
                Add(entity, id);
                transientEntities.Add(entity);
                transientRecords.Add(new Transient { Id = id, Kind = kind });
            }
            using var dropQuery = em.CreateEntityQuery(typeof(UnitTransportAirdropVisualCleanup));
            using var dropCandidates = dropQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in dropCandidates)
            {
                if (!em.HasComponent<UnitTransportDropVisualSource>(entity)) throw new InvalidOperationException("NativeDropVisualSourceMissing");
                var origin = em.GetComponentData<UnitTransportDropVisualSource>(entity);
                string id = "drop-visual/" + dropRecords.Count;
                Add(entity, id);
                dropEntities.Add(entity);
                dropRecords.Add(new DropVisual { Id = id, CarrierSourceKey = origin.CarrierSourceKey.ToString(), PassengerKind = origin.PassengerKind });
            }
            string Identity(Entity entity) => identities.TryGetValue(entity, out string id) ? id : null;
            for (int i = 0; i < visualRecords.Count; i++)
                visualRecords[i].Parts = SkirmishNativeCheckpointParts.Capture(em, visualEntities[i], Identity);
            for (int i = 0; i < transientRecords.Count; i++)
                transientRecords[i].Parts = SkirmishNativeCheckpointParts.Capture(em, transientEntities[i], Identity);
            for (int i = 0; i < dropRecords.Count; i++)
                dropRecords[i].Parts = SkirmishNativeCheckpointParts.Capture(em, dropEntities[i], Identity);
            drops = dropRecords.ToArray();
            visuals = visualRecords.ToArray();
            transients = transientRecords.ToArray();
        }

        internal static Bindings Rebuild(EntityManager em, string sessionId, Visual[] visuals,
            Transient[] transients, IReadOnlyDictionary<string, Entity> actors, DropVisual[] drops = null)
        {
            var result = new Bindings();
            try
            {
                void Bind(string id, Entity entity)
                {
                    if (string.IsNullOrEmpty(id)) return;
                    if (!em.Exists(entity)) throw new ArgumentException("Native missile hierarchy was not reconstructed.");
                    if (actors.TryGetValue(id, out Entity actor))
                    {
                        if (actor != entity) throw new ArgumentException("Native missile parent differs from saved identity.");
                        return;
                    }
                    if (result.Identities.TryGetValue(id, out Entity previous))
                    {
                        if (previous != entity) throw new ArgumentException("Native missile alias is ambiguous.");
                    }
                    else result.Identities.Add(id, entity);
                }
                var usedVisuals = new HashSet<Entity>();
                foreach (var saved in visuals)
                {
                    if (!actors.TryGetValue(saved.LauncherId, out Entity launcher))
                        throw new ArgumentException("Native missile launcher was not reconstructed.");
                    Entity visual = Entity.Null;
                    if (saved.Kind == 1 && em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(launcher))
                        foreach (var slot in em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(launcher, true))
                            if (slot.SlotIndex == saved.Slot) { if (visual != Entity.Null) throw new ArgumentException("Duplicate rocket slot."); visual = slot.Rocket; }
                    if (saved.Kind == 2 && em.HasBuffer<AirMissileLauncherMissileVisualComponent>(launcher))
                        foreach (var slot in em.GetBuffer<AirMissileLauncherMissileVisualComponent>(launcher, true))
                            if (slot.SlotIndex == saved.Slot) { if (visual != Entity.Null) throw new ArgumentException("Duplicate missile slot."); visual = slot.Missile; }
                    if (visual == Entity.Null || !usedVisuals.Add(visual) || !em.HasComponent<LocalTransform>(visual))
                        throw new ArgumentException("Native missile visual slot differs from save.");
                    Bind(saved.Id, visual);
                    Bind(saved.ParentId, em.HasComponent<Parent>(visual) ? em.GetComponentData<Parent>(visual).Value : Entity.Null);
                    if (saved.Detached != 0) result.Detached.Add(visual);
                }
                foreach (var saved in transients)
                {
                    Entity entity = em.CreateEntity();
                    result.Created.Add(entity);
                    em.AddComponentData(entity, new SkirmishNativeTransientOwner { SessionId = new FixedString64Bytes(sessionId) });
                    Bind(saved.Id, entity);
                }
                foreach (var saved in drops ?? Array.Empty<DropVisual>())
                {
                    Entity prefab = ResolveDropPrefab(em, saved.CarrierSourceKey, saved.PassengerKind);
                    if (prefab == Entity.Null) throw new ArgumentException("Native drop visual prefab is unavailable.");
                    Entity entity = em.Instantiate(prefab);
                    result.Created.Add(entity);
                    em.AddComponentData(entity, new SkirmishNativeTransientOwner { SessionId = new FixedString64Bytes(sessionId) });
                    Bind(saved.Id, entity);
                }
                return result;
            }
            catch
            {
                DestroyCreated(em, result);
                throw;
            }
        }

        internal static bool PublishAliases(Visual[] visuals, Transient[] transients,
            HashSet<string> ids, IReadOnlyDictionary<string, byte> actorKinds, DropVisual[] drops = null)
        {
            drops ??= Array.Empty<DropVisual>();
            if (drops.Length > 65536) return false;
            for (int i = 0; i < drops.Length; i++)
            {
                var drop = drops[i];
                if (drop == null || drop.Id != "drop-visual/" + i || !ids.Add(drop.Id) ||
                    string.IsNullOrEmpty(drop.CarrierSourceKey) || drop.CarrierSourceKey.Length > 63 ||
                    (drop.PassengerKind != UnitTransportPassengerKind.Soldier && drop.PassengerKind != UnitTransportPassengerKind.Vehicle)) return false;
            }
            if (visuals == null || transients == null || visuals.Length > 65536 || transients.Length > 65536) return false;
            var parents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var visual in visuals)
            {
                if (visual == null || visual.Kind < 1 || visual.Kind > 2 || visual.Detached > 1 ||
                    visual.Slot < 0 || visual.Slot > 4096 || !actorKinds.TryGetValue(visual.LauncherId ?? "", out byte structure) ||
                    structure != 0 || visual.Id != "missile-visual/" + visual.Kind + "/" + visual.LauncherId + "/" + visual.Slot ||
                    !ids.Add(visual.Id) || visual.ParentId == null) return false;
                if (visual.ParentId.Length != 0 && !ids.Contains(visual.ParentId)) parents.Add(visual.ParentId);
            }
            foreach (string parent in parents)
            {
                bool matches = false;
                foreach (var visual in visuals)
                    if (parent == "missile-parent/" + visual.Kind + "/" + visual.LauncherId + "/" + visual.Slot) matches = true;
                if (!matches || !ids.Add(parent)) return false;
            }
            for (int i = 0; i < transients.Length; i++)
                if (transients[i] == null || transients[i].Kind < 1 || transients[i].Kind > 4 ||
                    transients[i].Id != "missile-state/" + i || !ids.Add(transients[i].Id)) return false;
            foreach (var drop in drops)
            {
                if (!SkirmishNativeCheckpointParts.Validate(drop.Parts, ids) ||
                    !OnlyParts(drop.Parts, typeof(LocalTransform), typeof(UnitTransportAirdropVisualCleanup), typeof(UnitTransportDropVisualSource), typeof(Disabled)) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(drop.Parts, out LocalTransform _) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(drop.Parts, out UnitTransportAirdropVisualCleanup _) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(drop.Parts, out UnitTransportDropVisualSource origin) ||
                    origin.CarrierSourceKey.ToString() != drop.CarrierSourceKey || origin.PassengerKind != drop.PassengerKind) return false;
            }
            foreach (var visual in visuals)
            {
                if (!SkirmishNativeCheckpointParts.Validate(visual.Parts, ids) ||
                    !(visual.Kind == 1
                        ? OnlyParts(visual.Parts, typeof(LocalTransform), typeof(Disabled), typeof(GroundMissileFlyingRocketVisualComponent))
                        : OnlyParts(visual.Parts, typeof(LocalTransform), typeof(Disabled), typeof(AirMissileFlyingVisualComponent),
                            typeof(AirMissileProjectileComponent), typeof(AirMissileProjectileTrailComponent),
                            typeof(MissileInterceptionTargetComponent), typeof(MissileInterceptedComponent))) ||
                    !SkirmishNativeCheckpointParts.TryReadComponent(visual.Parts, out LocalTransform _) ||
                    (visual.ParentId.Length != 0 && !ids.Contains(visual.ParentId))) return false;
                if (visual.Kind == 1 && SkirmishNativeCheckpointParts.TryReadComponent(visual.Parts, out GroundMissileFlyingRocketVisualComponent _)
                    && visual.Detached == 0) return false;
                if (visual.Kind == 2 && SkirmishNativeCheckpointParts.TryReadComponent(visual.Parts, out AirMissileFlyingVisualComponent _)
                    && visual.Detached == 0) return false;
                if (SkirmishNativeCheckpointParts.TryReadComponent(visual.Parts, out GroundMissileFlyingRocketVisualComponent ground) &&
                    (ground.SlotIndex != visual.Slot || !ReferenceIs<GroundMissileFlyingRocketVisualComponent>(visual.Parts, 0, visual.LauncherId) ||
                     !ReferenceIs<GroundMissileFlyingRocketVisualComponent>(visual.Parts, 1, visual.ParentId))) return false;
                if (SkirmishNativeCheckpointParts.TryReadComponent(visual.Parts, out AirMissileFlyingVisualComponent air) &&
                    (air.SlotIndex != visual.Slot || !ReferenceIs<AirMissileFlyingVisualComponent>(visual.Parts, 0, visual.LauncherId) ||
                     !ReferenceIs<AirMissileFlyingVisualComponent>(visual.Parts, 1, visual.ParentId))) return false;
            }
            foreach (var transient in transients)
            {
                if (!SkirmishNativeCheckpointParts.Validate(transient.Parts, ids)) return false;
                Type primary = transient.Kind switch {
                    1 => typeof(GroundMissileProjectileComponent), 2 => typeof(AirMissileProjectileComponent),
                    3 => typeof(GroundMissileImpactRequestComponent), _ => typeof(AirMissileImpactRequestComponent) };
                Type trail = transient.Kind == 1 ? typeof(GroundMissileProjectileTrailComponent) : typeof(AirMissileProjectileTrailComponent);
                if (!OnlyParts(transient.Parts, primary, trail, typeof(LocalTransform),
                    typeof(MissileInterceptionTargetComponent), typeof(MissileInterceptedComponent))) return false;
                bool correct = transient.Kind switch {
                    1 => SkirmishNativeCheckpointParts.TryReadComponent(transient.Parts, out GroundMissileProjectileComponent _),
                    2 => SkirmishNativeCheckpointParts.TryReadComponent(transient.Parts, out AirMissileProjectileComponent _),
                    3 => SkirmishNativeCheckpointParts.TryReadComponent(transient.Parts, out GroundMissileImpactRequestComponent _),
                    4 => SkirmishNativeCheckpointParts.TryReadComponent(transient.Parts, out AirMissileImpactRequestComponent _),
                    _ => false };
                if (!correct) return false;
            }
            return true;
        }

        private static bool ReferenceIs<T>(SkirmishNativeCheckpointParts.Part[] parts, int index, string id) where T : unmanaged, IComponentData
        {
            foreach (var part in parts)
                if (part.Type == typeof(T).FullName)
                    return index < part.References.Length && part.References[index].ObjectId == id;
            return false;
        }

        private static bool OnlyParts(SkirmishNativeCheckpointParts.Part[] parts, params Type[] allowed)
        {
            foreach (var part in parts)
            {
                bool found = false;
                foreach (var type in allowed) if (part.Type == type.FullName) { found = true; break; }
                if (!found) return false;
            }
            return true;
        }

        private static Entity ResolveDropPrefab(EntityManager em, string sourceKey, byte kind)
        {
            Entity found = Entity.Null;
            void Candidate(Entity prefab)
            {
                if (prefab == Entity.Null || !em.Exists(prefab)) return;
                if (!em.HasComponent<Prefab>(prefab)) throw new ArgumentException("Drop visual registry contains a non-prefab.");
                if (found != Entity.Null && found != prefab) throw new ArgumentException("Drop visual source is ambiguous.");
                found = prefab;
            }
            using var registryQuery = em.CreateEntityQuery(typeof(UnitTransportAirdropVisualPrefabRegistryEntry));
            using var registries = registryQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity registry in registries)
                foreach (var entry in em.GetBuffer<UnitTransportAirdropVisualPrefabRegistryEntry>(registry, true))
                    if (entry.SourceKey.ToString() == sourceKey)
                        Candidate(kind == UnitTransportPassengerKind.Vehicle ? entry.VehicleEmergencyDropVisualPrefab : entry.SoldierParachuteVisualPrefab);
            if (found != Entity.Null) return found;
            using var query = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<UnitSourcePrefabKey>(), ComponentType.ReadOnly<UnitTransportAirdropVisualPrefabs>() },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities });
            using var carriers = query.ToEntityArray(Allocator.Temp);
            foreach (Entity carrier in carriers)
                if (em.GetComponentData<UnitSourcePrefabKey>(carrier).Value.ToString() == sourceKey)
                {
                    var prefabs = em.GetComponentData<UnitTransportAirdropVisualPrefabs>(carrier);
                    Candidate(kind == UnitTransportPassengerKind.Vehicle ? prefabs.VehicleEmergencyDropVisualPrefab : prefabs.SoldierParachuteVisualPrefab);
                }
            return found;
        }

        internal static void DestroySession(EntityManager em, FixedString64Bytes sessionId)
        {
            using var query = em.CreateEntityQuery(new EntityQueryDesc { Any = new[] {
                ComponentType.ReadOnly<SkirmishNativeTransientOwner>(),
                ComponentType.ReadOnly<GroundMissileProjectileComponent>(), ComponentType.ReadOnly<AirMissileProjectileComponent>(),
                ComponentType.ReadOnly<GroundMissileImpactRequestComponent>(), ComponentType.ReadOnly<AirMissileImpactRequestComponent>() } });
            using var entities = query.ToEntityArray(Allocator.Temp);
            bool Owned(Entity source) => em.Exists(source) && em.HasComponent<SkirmishAttemptOwnedComponent>(source) &&
                em.GetComponentData<SkirmishAttemptOwnedComponent>(source).SessionId.Equals(sessionId);
            foreach (Entity entity in entities)
            {
                bool owned = em.HasComponent<SkirmishNativeTransientOwner>(entity) &&
                    em.GetComponentData<SkirmishNativeTransientOwner>(entity).SessionId.Equals(sessionId);
                if (em.HasComponent<GroundMissileProjectileComponent>(entity)) owned |= Owned(em.GetComponentData<GroundMissileProjectileComponent>(entity).Source);
                if (em.HasComponent<AirMissileProjectileComponent>(entity)) owned |= Owned(em.GetComponentData<AirMissileProjectileComponent>(entity).Source);
                if (em.HasComponent<GroundMissileImpactRequestComponent>(entity)) owned |= Owned(em.GetComponentData<GroundMissileImpactRequestComponent>(entity).Source);
                if (em.HasComponent<AirMissileImpactRequestComponent>(entity)) owned |= Owned(em.GetComponentData<AirMissileImpactRequestComponent>(entity).Source);
                if (!owned) continue;
                // Baked air visual children belong to their launcher's linked hierarchy.
                if (em.HasComponent<AirMissileFlyingVisualComponent>(entity))
                {
                    if (em.HasComponent<AirMissileProjectileComponent>(entity)) em.RemoveComponent<AirMissileProjectileComponent>(entity);
                    if (em.HasComponent<MissileInterceptionTargetComponent>(entity)) em.RemoveComponent<MissileInterceptionTargetComponent>(entity);
                }
                else em.DestroyEntity(entity);
            }
        }

        internal static void DestroyCreated(EntityManager em, Bindings bindings)
        {
            if (bindings == null) return;
            foreach (Entity entity in bindings.Created) if (em.Exists(entity)) em.DestroyEntity(entity);
            bindings.Created.Clear();
        }
    }
}
