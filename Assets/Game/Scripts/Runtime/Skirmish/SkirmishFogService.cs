using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public static partial class SkirmishFogService
    {
        private struct ContactSample
        {
            public Entity Entity;
            public FixedString64Bytes StableObjectId;
            public byte FactionId;
            public byte Alive;
            public byte Presentable;
            public byte DetectorDomain;
            public int Health;
            public int MaxHealth;
            public int2 Cell;
            public float3 Position;
        }
        private struct ObserverSample
        {
            public Entity Entity;
            public byte FactionId;
            public FixedString64Bytes StableObjectId;
            public float AcquisitionRadius;
            public int ThreatRadiusCells;
            public byte ThreatKind;
            public int2 Cell;
            public float3 Position;
            public UnitScanOrder Scan;
            public byte ActiveScan;
        }

        // Spawn/adoption may call this repeatedly. It initializes knowledge without
        // aging contacts; only the positive-time simulation update advances sight.
        public static void Project(EntityManager em, Entity session)
        {
            if (!TrySession(em, session, out var state, out var fog)) return;
            float now = (float)em.WorldUnmanaged.Time.ElapsedTime;
            using var query = OwnedContacts(em);
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(state.SessionId)) continue;
                var sample = Sample(em, entity, owned);
                EnsureKnowledge(em, entity);
                for (byte observer = 1; observer <= 2; observer++)
                {
                    var knowledge = GetKnowledge(em, entity, observer);
                    if (owned.FactionId == observer || fog.DevelopmentFullVision != 0 || fog.SharedFog == 0)
                        Observe(ref knowledge, sample, now, Entity.Null, 0, 0, 3);
                    Store(em, entity, knowledge);
                }
                ProjectPlayer(em, entity);
            }
        }

        public static bool IsVisible(EntityManager em, Entity contact) => IsVisibleToFaction(em, contact, 1);

        public static bool IsVisibleToFaction(EntityManager em, Entity contact, byte observerFaction)
        {
            if (!em.Exists(contact)) return false;
            if (em.HasComponent<SkirmishAttemptOwnedComponent>(contact))
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(contact).FactionId == observerFaction) return true;
                if (TryGetKnowledge(em, contact, observerFaction, out var knowledge))
                    return knowledge.Sight == SkirmishContactSight.Visible;
                // Old native checkpoints lack the optional buffer. Do not grant the
                // opposing observer the player's previously projected knowledge.
                return observerFaction == 1 && Current(em, contact) == SkirmishContactSight.Visible;
            }
            return Current(em, contact) == SkirmishContactSight.Visible;
        }

        public static bool TryGetKnowledge(EntityManager em, Entity contact, byte observerFaction,
            out SkirmishContactKnowledgeElement knowledge)
        {
            knowledge = default;
            if (!em.Exists(contact) || !em.HasBuffer<SkirmishContactKnowledgeElement>(contact)) return false;
            var buffer = em.GetBuffer<SkirmishContactKnowledgeElement>(contact, true);
            for (int i = 0; i < buffer.Length; i++)
                if (buffer[i].ObserverFactionId == observerFaction) { knowledge = buffer[i]; return true; }
            return false;
        }

        // Called only after the native Scan system has revealed the actual contact.
        // A stale pooled source, other attempt, or Campaign actor cannot confer sight.
        public static bool ObserveFromScan(EntityManager em, Entity target, Entity source, int requestId, int frame)
        {
            if (em.WorldUnmanaged.Time.DeltaTime <= 0 || !em.Exists(source) || !em.Exists(target) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(source) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(target) || !em.HasComponent<Faction>(source)) return false;
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(source);
            var targetOwned = em.GetComponentData<SkirmishAttemptOwnedComponent>(target);
            if (owned.CapacityReleased != 0 || !owned.SessionId.Equals(targetOwned.SessionId) ||
                owned.FactionId < 1 || owned.FactionId > 2 ||
                em.GetComponentData<Faction>(source).Id != owned.FactionId || !Alive(em, source) || !Alive(em, target)) return false;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity session = entities[i];
                var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                if (!state.SessionId.Equals(owned.SessionId) || !CanAdvance(em, session, state)) continue;
                EnsureKnowledge(em, target);
                var knowledge = GetKnowledge(em, target, owned.FactionId);
                Observe(ref knowledge, Sample(em, target, targetOwned), (float)em.WorldUnmanaged.Time.ElapsedTime,
                    source, requestId, frame, 2);
                knowledge.ObservationSourceStableObjectId = owned.StableObjectId;
                Store(em, target, knowledge);
                // Scan runs after the shared update in some native schedules. Persist
                // this actual emission before a hidden actor can be finalized.
                EnsureLedger(em, session, state.SessionId);
                CopyObservedContact(em, session, targetOwned.StableObjectId, targetOwned.FactionId, knowledge);
                ProjectPlayer(em, target);
                return true;
            }
            return false;
        }

        public static bool TryGetActiveScanObserverFaction(EntityManager em, Entity source, out byte faction)
        {
            faction = 0;
            if (!em.Exists(source) || !em.HasComponent<SkirmishAttemptOwnedComponent>(source) ||
                !em.HasComponent<Faction>(source) || !Alive(em, source)) return false;
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(source);
            if (owned.CapacityReleased != 0 || owned.FactionId < 1 || owned.FactionId > 2 ||
                em.GetComponentData<Faction>(source).Id != owned.FactionId) return false;
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var state = em.GetComponentData<SkirmishExpandedSessionComponent>(entities[i]);
                if (!state.SessionId.Equals(owned.SessionId) || !CanAdvance(em, entities[i], state)) continue;
                faction = owned.FactionId;
                return true;
            }
            return false;
        }

        public static void UpdateSession(EntityManager em, Entity session)
        {
            if (em.WorldUnmanaged.Time.DeltaTime <= 0 || !TrySession(em, session, out var state, out var fog) ||
                !CanAdvance(em, session, state)) return;
            float now = (float)em.WorldUnmanaged.Time.ElapsedTime;
            float cellSize = 0;
            using (var grids = em.CreateEntityQuery(typeof(GridConfig)))
                if (grids.CalculateEntityCount() == 1) cellSize = math.max(0, grids.GetSingleton<GridConfig>().CellSize);
            using var contacts = new NativeList<ContactSample>(Allocator.Temp);
            using var observers = new NativeList<ObserverSample>(Allocator.Temp);
            using var query = OwnedContacts(em);
            using var entities = query.ToEntityArray(Allocator.Temp);
            // EntityManager access is linear in the roster. Pair comparisons below
            // use copied data, not component queries or structural writes.
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(state.SessionId)) continue;
                var contact = Sample(em, entity, owned);
                contacts.Add(contact);
                if (TrySampleObserver(em, contact, owned, cellSize, now, out var observer)) observers.Add(observer);
            }
            for (int i = 0; i < contacts.Length; i++)
            {
                var contact = contacts[i];
                EnsureKnowledge(em, contact.Entity);
                for (byte faction = 1; faction <= 2; faction++)
                {
                    var knowledge = GetKnowledge(em, contact.Entity, faction);
                    bool visible = CurrentObservation(contact, fog, observers, knowledge, faction,
                        out Entity source, out FixedString64Bytes sourceStableId, out int request, out int frame, out byte kind);
                    if (visible)
                    {
                        Observe(ref knowledge, contact, now, source, request, frame, kind);
                        knowledge.ObservationSourceStableObjectId = sourceStableId;
                    }
                    else if (knowledge.Sight != SkirmishContactSight.Unknown)
                    {
                        knowledge.Sight = now - knowledge.LastObservedTimeSeconds >= math.max(0, fog.LastSeenExpireSeconds)
                            ? SkirmishContactSight.Unknown : SkirmishContactSight.LastSeen;
                        // LastSeen position/health provenance remain frozen. Hidden
                        // contact movement or death does not refresh this record.
                    }
                    Store(em, contact.Entity, knowledge);
                }
                ProjectPlayer(em, contact.Entity);
            }
            RefreshContactLedger(em, session, state.SessionId, contacts, now, fog.LastSeenExpireSeconds);
        }

        // Own-unit acquisition metric only. This does not establish visible contact.
        public static float ReadOwnAcquisitionRadius(EntityManager em, Entity entity, float cellSize)
        {
            float radius = 0;
            if (em.HasComponent<UnitCombat>(entity))
                {
                radius = math.max(0, em.GetComponentData<UnitCombat>(entity).AggroRangeCells) * cellSize;
                if (cellSize > 0 && em.HasComponent<UnitAttack>(entity))
                {
                    float range = math.max(0, em.GetComponentData<UnitAttack>(entity).Range);
                    radius = math.max(radius, math.ceil(range / cellSize) * cellSize);
                }
            }
            return radius;
        }

        private static bool TrySampleObserver(EntityManager em, ContactSample contact,
            SkirmishAttemptOwnedComponent owned, float cellSize, float now, out ObserverSample observer)
        {
            observer = default;
            Entity entity = contact.Entity;
            if (contact.Alive == 0 || owned.CapacityReleased != 0 ||
                owned.FactionId < 1 || owned.FactionId > 2 ||
                !em.HasComponent<Faction>(entity) || em.GetComponentData<Faction>(entity).Id != owned.FactionId) return false;
            observer = new ObserverSample { Entity = entity, FactionId = owned.FactionId, StableObjectId = owned.StableObjectId,
                Cell = contact.Cell, Position = contact.Position };
            // Reuse native acquisition/detector radii. No new vision radius,
            // stat boost, or invented terrain LOS is introduced here.
            observer.AcquisitionRadius = ReadOwnAcquisitionRadius(em, entity, cellSize);
            if (em.HasComponent<ThreatDetector>(entity))
            {
                var detector = em.GetComponentData<ThreatDetector>(entity);
                observer.ThreatKind = detector.Kind;
                observer.ThreatRadiusCells = math.max(0, detector.RadiusCells);
            }
            if (em.HasComponent<UnitScanOrder>(entity))
            {
                observer.Scan = em.GetComponentData<UnitScanOrder>(entity);
                observer.ActiveScan = (byte)(observer.Scan.SourceEntity == entity && observer.Scan.HasStarted != 0 &&
                    (observer.Scan.DurationSeconds <= 0 || now - observer.Scan.StartedTimeSeconds < observer.Scan.DurationSeconds) &&
                    Chebyshev(observer.Cell, observer.Scan.CenterCell) <= math.max(1, observer.Scan.RadiusCells) ? 1 : 0);
            }
            return true;
        }

        // Shared by normal positive-time projection and the native death finalizer.
        // It reads copied current samples, preserving native acquisition/detector
        // metrics and only sustaining a previously emitted still-active Scan.
        private static bool CurrentObservation(ContactSample contact, SkirmishFogStateComponent fog,
            NativeList<ObserverSample> observers, SkirmishContactKnowledgeElement knowledge, byte faction,
            out Entity source, out FixedString64Bytes sourceStableId, out int request, out int frame, out byte kind)
        {
            bool visible = contact.FactionId == faction || fog.DevelopmentFullVision != 0 || fog.SharedFog == 0;
            source = Entity.Null;
            sourceStableId = default;
            request = 0; frame = 0;
            kind = visible ? (byte)3 : (byte)1;
            if (!visible && contact.Presentable != 0)
            {
                // Sustain only a scan that actually emitted this contact.
                // This never turns an unobserved contact visible pre-reveal.
                for (int j = 0; j < observers.Length; j++)
                {
                    var observer = observers[j];
                    if (observer.FactionId != faction || observer.ActiveScan == 0 || knowledge.ObservationKind != 2 ||
                        knowledge.ObservationSource != observer.Entity || !knowledge.ObservationSourceStableObjectId.Equals(observer.StableObjectId) ||
                        knowledge.ScanRequestId != observer.Scan.RequestId ||
                        Chebyshev(contact.Cell, observer.Scan.CenterCell) > math.max(1, observer.Scan.RadiusCells)) continue;
                    visible = true; source = observer.Entity; sourceStableId = observer.StableObjectId; request = observer.Scan.RequestId;
                    frame = observer.Scan.StartedFrame; kind = 2; break;
                }
                if (!visible)
                    for (int j = 0; j < observers.Length; j++)
                    {
                        var observer = observers[j];
                        if (observer.FactionId != faction) continue;
                        float radius = observer.AcquisitionRadius;
                        bool acquired = radius > 0 && math.distancesq(observer.Position.xz, contact.Position.xz) <= radius * radius;
                        // Native detectors use Chebyshev grid radius, unlike
                        // combat's horizontal Euclidean acquisition reach.
                        bool detected = contact.Alive != 0 && contact.DetectorDomain != 0 && observer.ThreatKind == contact.DetectorDomain &&
                            observer.ThreatRadiusCells > 0 && Chebyshev(observer.Cell, contact.Cell) <= observer.ThreatRadiusCells;
                        if (!acquired && !detected) continue;
                        visible = true; source = observer.Entity; sourceStableId = observer.StableObjectId; break;
                    }
            }
            return visible;
        }

        public static void Reveal(EntityManager em, Entity contact) => ManualSight(em, contact, SkirmishContactSight.Visible);
        public static void Hide(EntityManager em, Entity contact) => ManualSight(em, contact, SkirmishContactSight.Unknown);
        private static void ManualSight(EntityManager em, Entity contact, SkirmishContactSight sight)
        {
            if (!em.Exists(contact)) return;
            if (em.HasComponent<SkirmishAttemptOwnedComponent>(contact))
            {
                EnsureKnowledge(em, contact);
                var knowledge = GetKnowledge(em, contact, 1);
                if (sight == SkirmishContactSight.Visible)
                    Observe(ref knowledge, Sample(em, contact, em.GetComponentData<SkirmishAttemptOwnedComponent>(contact)),
                        (float)em.WorldUnmanaged.Time.ElapsedTime, Entity.Null, 0, 0, 3);
                else knowledge.Sight = sight;
                Store(em, contact, knowledge);
            }
            WriteSight(em, contact, sight);
        }
        private static bool TrySession(EntityManager em, Entity session, out SkirmishExpandedSessionComponent state,
            out SkirmishFogStateComponent fog)
        {
            state = default; fog = default;
            if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishFogStateComponent>(session)) return false;
            state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            fog = em.GetComponentData<SkirmishFogStateComponent>(session);
            return state.IsLegacy == 0;
        }
        private static bool CanAdvance(EntityManager em, Entity session, SkirmishExpandedSessionComponent state)
        {
            return state.IsLegacy == 0 && state.Phase == SkirmishSessionPhase.Playing &&
                (!em.HasComponent<SkirmishObjectiveClockComponent>(session) ||
                 em.GetComponentData<SkirmishObjectiveClockComponent>(session).Paused == 0);
        }
        private static EntityQuery OwnedContacts(EntityManager em) => em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() },
            Options = EntityQueryOptions.IncludeDisabledEntities
        });
        private static bool Alive(EntityManager em, Entity entity) => !em.HasComponent<Disabled>(entity) &&
            !em.HasComponent<UnitDeathAnimationComponent>(entity) && !em.HasComponent<UnitTransportPassenger>(entity) &&
            (!em.HasComponent<UnitHealth>(entity) || em.GetComponentData<UnitHealth>(entity).Current > 0);
        private static ContactSample Sample(EntityManager em, Entity entity, SkirmishAttemptOwnedComponent owned) => new()
        {
            Entity = entity, StableObjectId = owned.StableObjectId, FactionId = owned.FactionId, Alive = (byte)(Alive(em, entity) ? 1 : 0),
            Presentable = (byte)(!em.HasComponent<Disabled>(entity) && !em.HasComponent<UnitTransportPassenger>(entity) ? 1 : 0),
            DetectorDomain = em.HasComponent<RuntimeBuildingCombatInfo>(entity) ? (byte)0 :
                em.HasComponent<UnitAirMovement>(entity) ? (byte)2 :
                em.HasComponent<UnitMovementBehavior>(entity) && em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0 ? (byte)1 : (byte)0,
            Health = em.HasComponent<UnitHealth>(entity) ? em.GetComponentData<UnitHealth>(entity).Current : 0,
            MaxHealth = em.HasComponent<UnitHealth>(entity) ? em.GetComponentData<UnitHealth>(entity).Max : 0,
            Cell = em.HasComponent<UnitGrid>(entity) ? em.GetComponentData<UnitGrid>(entity).Cell : default,
            Position = em.HasComponent<LocalTransform>(entity) ? em.GetComponentData<LocalTransform>(entity).Position : default
        };
        private static int Chebyshev(int2 a, int2 b) { int2 d = math.abs(a - b); return math.max(d.x, d.y); }
        private static void Observe(ref SkirmishContactKnowledgeElement knowledge, ContactSample sample, float now,
            Entity source, int request, int frame, byte kind)
        {
            knowledge.Sight = SkirmishContactSight.Visible;
            knowledge.LastSeenCell = sample.Cell;
            knowledge.LastSeenPosition = sample.Position;
            knowledge.LastSeenHealth = sample.Health;
            knowledge.LastSeenMax = sample.MaxHealth;
            knowledge.LastObservedTimeSeconds = now;
            knowledge.ObservationSource = source;
            knowledge.ObservationSourceStableObjectId = default;
            knowledge.ScanRequestId = request;
            knowledge.ObservationFrame = frame;
            knowledge.ObservationKind = kind;
        }
        private static void EnsureKnowledge(EntityManager em, Entity entity)
        {
            if (em.HasBuffer<SkirmishContactKnowledgeElement>(entity)) return;
            SkirmishContactSight previous = Current(em, entity);
            // Previous Visible saves contain an observed actor pose/health. Earlier
            // LastSeen saves have no frozen-pose/time record, so do not invent one.
            var initial = new SkirmishContactKnowledgeElement { ObserverFactionId = 1,
                Sight = SkirmishContactSight.Unknown };
            if (previous == SkirmishContactSight.Visible && em.HasComponent<SkirmishAttemptOwnedComponent>(entity))
                Observe(ref initial, Sample(em, entity, em.GetComponentData<SkirmishAttemptOwnedComponent>(entity)),
                    (float)em.WorldUnmanaged.Time.ElapsedTime, Entity.Null, 0, 0, 3);
            var buffer = em.AddBuffer<SkirmishContactKnowledgeElement>(entity);
            buffer.Add(initial);
            buffer.Add(new SkirmishContactKnowledgeElement { ObserverFactionId = 2, Sight = SkirmishContactSight.Unknown });
        }
        private static SkirmishContactKnowledgeElement GetKnowledge(EntityManager em, Entity entity, byte faction)
        {
            if (TryGetKnowledge(em, entity, faction, out var knowledge)) return knowledge;
            return new SkirmishContactKnowledgeElement { ObserverFactionId = faction, Sight = SkirmishContactSight.Unknown };
        }
        private static void Store(EntityManager em, Entity entity, SkirmishContactKnowledgeElement knowledge)
        {
            var buffer = em.GetBuffer<SkirmishContactKnowledgeElement>(entity);
            for (int i = 0; i < buffer.Length; i++) if (buffer[i].ObserverFactionId == knowledge.ObserverFactionId)
            { buffer[i] = knowledge; return; }
            // Only the two authored participants have contact knowledge.
            if (buffer.Length < 2) buffer.Add(knowledge);
        }
        private static void ProjectPlayer(EntityManager em, Entity entity) => WriteSight(em, entity, GetKnowledge(em, entity, 1).Sight);
        private static SkirmishContactSight Current(EntityManager em, Entity entity) =>
            em.HasComponent<SkirmishContactSightComponent>(entity) ? em.GetComponentData<SkirmishContactSightComponent>(entity).Sight : SkirmishContactSight.Unknown;
        private static void WriteSight(EntityManager em, Entity entity, SkirmishContactSight sight)
        {
            // Expanded combat policy is a domain rule, not player perspective.
            // Combat consumers explicitly consult the attacker's faction knowledge.
            if (!em.HasComponent<SkirmishAttemptOwnedComponent>(entity) && em.HasComponent<CombatTargetPolicy>(entity))
            {
                var policy = em.GetComponentData<CombatTargetPolicy>(entity);
                policy.Visible = (byte)(sight == SkirmishContactSight.Visible ? 1 : 0);
                em.SetComponentData(entity, policy);
            }
            var component = new SkirmishContactSightComponent { Sight = sight };
            if (em.HasComponent<SkirmishContactSightComponent>(entity))
            {
                if (Current(em, entity) != sight) em.SetComponentData(entity, component);
            }
            else em.AddComponentData(entity, component);
        }
    }
}
