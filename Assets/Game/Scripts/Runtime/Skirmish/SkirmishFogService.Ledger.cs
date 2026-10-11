using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static partial class SkirmishFogService
    {
        public static bool TryGetLedger(EntityManager em, Entity session, FixedString64Bytes stableObjectId,
            byte observerFaction, out SkirmishContactLedgerElement record)
        {
            record = default;
            if (!em.Exists(session) || !em.HasBuffer<SkirmishContactLedgerElement>(session) ||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                !em.HasComponent<SkirmishContactLedgerStateComponent>(session) ||
                !em.GetComponentData<SkirmishContactLedgerStateComponent>(session).SessionId.Equals(
                    em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId)) return false;
            var ledger = em.GetBuffer<SkirmishContactLedgerElement>(session, true);
            int index = LedgerIndex(ledger, stableObjectId, observerFaction);
            if (index < 0) return false;
            record = ledger[index];
            return true;
        }

        private static void RefreshContactLedger(EntityManager em, Entity session, FixedString64Bytes sessionId,
            NativeList<ContactSample> contacts, float now, int expirySeconds)
        {
            EnsureLedger(em, session, sessionId);
            // Present actors are copied once. Missing contacts are processed from
            // immutable observed memory, never live health/despawn/position reads.
            using var present = new NativeParallelHashMap<FixedString64Bytes, byte>(contacts.Length + 1, Allocator.Temp);
            for (int i = 0; i < contacts.Length; i++)
            {
                var contact = contacts[i];
                if (contact.StableObjectId.IsEmpty) continue;
                present.TryAdd(contact.StableObjectId, 1);
                for (byte observer = 1; observer <= 2; observer++)
                {
                    if (contact.FactionId == observer || !TryGetKnowledge(em, contact.Entity, observer, out var knowledge)) continue;
                    CopyObservedContact(em, session, contact.StableObjectId, contact.FactionId, knowledge);
                }
            }
            var records = em.GetBuffer<SkirmishContactLedgerElement>(session);
            for (int i = records.Length - 1; i >= 0; i--)
            {
                var record = records[i];
                if (present.ContainsKey(record.ContactStableObjectId)) continue;
                if (now - record.LastObservedTimeSeconds >= expirySeconds)
                { records.RemoveAtSwapBack(i); continue; }
                record.Sight = SkirmishContactSight.LastSeen;
                records[i] = record;
            }
        }

        private static void CopyObservedContact(EntityManager em, Entity session, FixedString64Bytes contactId,
            byte contactFaction, SkirmishContactKnowledgeElement knowledge)
        {
            if (contactId.IsEmpty || contactFaction == knowledge.ObserverFactionId) return;
            var ledger = em.GetBuffer<SkirmishContactLedgerElement>(session);
            int index = LedgerIndex(ledger, contactId, knowledge.ObserverFactionId);
            if (knowledge.Sight == SkirmishContactSight.Unknown || knowledge.LastSeenMax <= 0 || knowledge.LastSeenHealth <= 0)
            {
                if (index >= 0) ledger.RemoveAtSwapBack(index);
                return;
            }
            var record = new SkirmishContactLedgerElement
            {
                ContactStableObjectId = contactId, ContactFactionId = contactFaction,
                ObserverFactionId = knowledge.ObserverFactionId, Sight = knowledge.Sight,
                LastSeenCell = knowledge.LastSeenCell, LastSeenPosition = knowledge.LastSeenPosition,
                LastSeenHealth = knowledge.LastSeenHealth, LastSeenMax = knowledge.LastSeenMax,
                LastObservedTimeSeconds = knowledge.LastObservedTimeSeconds,
                ObservationSourceStableObjectId = knowledge.ObservationSourceStableObjectId,
                ScanRequestId = knowledge.ScanRequestId, ObservationFrame = knowledge.ObservationFrame,
                ObservationKind = knowledge.ObservationKind
            };
            if (index >= 0) ledger[index] = record; else ledger.Add(record);
        }

        private static void EnsureLedger(EntityManager em, Entity session, FixedString64Bytes sessionId)
        {
            bool sameAttempt = em.HasComponent<SkirmishContactLedgerStateComponent>(session) &&
                em.GetComponentData<SkirmishContactLedgerStateComponent>(session).SessionId.Equals(sessionId);
            if (!em.HasBuffer<SkirmishContactLedgerElement>(session)) em.AddBuffer<SkirmishContactLedgerElement>(session);
            if (!sameAttempt)
            {
                em.GetBuffer<SkirmishContactLedgerElement>(session).Clear();
                var state = new SkirmishContactLedgerStateComponent { SessionId = sessionId };
                if (em.HasComponent<SkirmishContactLedgerStateComponent>(session)) em.SetComponentData(session, state);
                else em.AddComponentData(session, state);
            }
        }

        private static int LedgerIndex(DynamicBuffer<SkirmishContactLedgerElement> ledger,
            FixedString64Bytes stableObjectId, byte observerFaction)
        {
            for (int i = 0; i < ledger.Length; i++)
                if (ledger[i].ObserverFactionId == observerFaction && ledger[i].ContactStableObjectId.Equals(stableObjectId)) return i;
            return -1;
        }

        // Called by the actual native death finalizer. Current observation is
        // evaluated with the same copied-source helper as normal fog projection.
        public static void NotifyObservedDeath(EntityManager em, Entity actor)
        {
            if (em.WorldUnmanaged.Time.DeltaTime <= 0 || !em.Exists(actor) ||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(actor) ||
                !em.HasComponent<UnitHealth>(actor) || em.GetComponentData<UnitHealth>(actor).Current > 0) return;
            var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent));
            using var entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var state = em.GetComponentData<SkirmishExpandedSessionComponent>(entities[i]);
                if (!state.SessionId.Equals(owned.SessionId) || !CanAdvance(em, entities[i], state)) continue;
                float now = (float)em.WorldUnmanaged.Time.ElapsedTime;
                float cellSize = 0;
                using (var grids = em.CreateEntityQuery(typeof(GridConfig)))
                    if (grids.CalculateEntityCount() == 1) cellSize = math.max(0, grids.GetSingleton<GridConfig>().CellSize);
                using var observers = new NativeList<ObserverSample>(Allocator.Temp);
                using var contacts = OwnedContacts(em);
                using var contactEntities = contacts.ToEntityArray(Allocator.Temp);
                for (int c = 0; c < contactEntities.Length; c++)
                {
                    var candidateOwned = em.GetComponentData<SkirmishAttemptOwnedComponent>(contactEntities[c]);
                    if (!candidateOwned.SessionId.Equals(state.SessionId)) continue;
                    var candidate = Sample(em, contactEntities[c], candidateOwned);
                    if (TrySampleObserver(em, candidate, candidateOwned, cellSize, now, out var sample)) observers.Add(sample);
                }
                var contact = Sample(em, actor, owned);
                var fog = em.GetComponentData<SkirmishFogStateComponent>(entities[i]);
                for (byte observer = 1; observer <= 2; observer++)
                {
                    if (!TryGetKnowledge(em, actor, observer, out var knowledge)) continue;
                    // Prior Visible can be stale when movement/death/finalization
                    // occur before the next fog update. Only current native geometry
                    // or an already observed HP0 sample establishes known death.
                    bool observedDead = knowledge.Sight != SkirmishContactSight.Unknown && knowledge.LastSeenHealth <= 0;
                    bool currentlyObserved = CurrentObservation(contact, fog, observers, knowledge, observer,
                        out _, out _, out _, out _, out _);
                    if (!observedDead && !currentlyObserved) continue;
                    if (!em.HasBuffer<SkirmishContactLedgerElement>(entities[i])) continue;
                    var ledger = em.GetBuffer<SkirmishContactLedgerElement>(entities[i]);
                    int index = LedgerIndex(ledger, owned.StableObjectId, observer);
                    if (index >= 0) ledger.RemoveAtSwapBack(index);
                }
            }
        }
    }
}
