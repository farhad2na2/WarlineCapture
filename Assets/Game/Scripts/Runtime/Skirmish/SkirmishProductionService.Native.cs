using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static partial class SkirmishProductionService
    {
        // Native production owns placement, transport and timing. This receipt owns
        // mission costs/capacity and adopts each real delivered entity exactly once.
        internal static bool BindNativeQueue(EntityManager em, Entity session, uint id, int producerId)
        {
            if (producerId <= 0 || !TryGetReservation(em, session, id, out int index, out var receipt) ||
                receipt.Phase != SkirmishReservationPhase.Reserved || receipt.ProducerRuntimeId != 0) return false;
            receipt.ProducerRuntimeId = producerId;
            var receipts = em.GetBuffer<SkirmishProductionReservation>(session);
            receipts[index] = receipt;
            return true;
        }

        internal static bool CanDeliverNative(EntityManager em, Entity session, uint id, int producerId,
            byte faction, FixedString64Bytes sourceKey, FixedString64Bytes attemptId)
        {
            if (!em.Exists(session) || !em.HasComponent<SkirmishExpandedSessionComponent>(session) || !NativeAttemptActive(em, session, attemptId) ||
                !TryGetReservation(em, session, id, out _, out var receipt) ||
                receipt.ProducerRuntimeId != producerId || receipt.FactionId != faction ||
                receipt.DeliveredMembers >= receipt.MemberCount ||
                (receipt.Phase != SkirmishReservationPhase.Reserved && receipt.Phase != SkirmishReservationPhase.Producing))
                return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            return sourceKey.ToString() == PrefabKey(setup, receipt.Role);
        }

        internal static void StartNativeQueue(EntityManager em, Entity session, uint id, FixedString64Bytes attemptId)
        {
            if (!em.Exists(session) || !NativeAttemptActive(em, session, attemptId) || !TryGetReservation(em, session, id, out int index, out var receipt) ||
                receipt.ProducerRuntimeId == 0 || receipt.Phase != SkirmishReservationPhase.Reserved) return;
            receipt.Phase = SkirmishReservationPhase.Producing;
            var receipts = em.GetBuffer<SkirmishProductionReservation>(session);
            receipts[index] = receipt;
        }

        internal static bool AdoptNativeMember(EntityManager em, Entity session, uint id, int producerId, Entity member, FixedString64Bytes attemptId)
        {
            if (!em.Exists(member) || em.HasComponent<SkirmishAttemptOwnedComponent>(member) ||
                !em.HasComponent<Faction>(member) || !em.HasComponent<UnitSourcePrefabKey>(member) ||
                !CanDeliverNative(em, session, id, producerId, em.GetComponentData<Faction>(member).Id,
                    em.GetComponentData<UnitSourcePrefabKey>(member).Value, attemptId)) return false;
            TryGetReservation(em, session, id, out int index, out var receipt);
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (!SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays, receipt.Role, out var overlay)) return false;
            var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            int supply = receipt.SupplyCost / receipt.MemberCount;
            ReadStocks(em, session, receipt.FactionId, out int materials, out int fuel, out var capacity, out uint next);
            SkirmishCapacityLedger.PromoteLive(ref capacity, receipt.Category, 1, supply);
            SkirmishRuntimeActorOwnership.DetachFromSourceScene(em, member);
            em.AddComponentData(member, new SkirmishAttemptOwnedComponent {
                SessionId = sessionId, StableObjectId = new FixedString64Bytes("production." + id + "." + receipt.DeliveredMembers),
                FactionId = receipt.FactionId, ReservationId = id
            });
            em.AddComponentData(member, new SkirmishUnitRoleComponent {
                Role = receipt.Role, Category = receipt.Category, SupplyCost = supply
            });
            if (!em.HasComponent<SkirmishSharedActorTag>(member)) em.AddComponent<SkirmishSharedActorTag>(member);
            if (!em.HasComponent<SkirmishVisualSpawnedComponent>(member))
                em.AddComponentData(member, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
            SkirmishRosterProjectionSystem.WriteOverlay(em, member, overlay);
            if (receipt.ProductionGroupId == 0)
                receipt.ProductionGroupId = SkirmishArmyGroupSystem.OpenGroup(em, session, receipt.FactionId, receipt.Role, receipt.Category);
            SkirmishArmyGroupSystem.BindMember(em, session, member, receipt.ProductionGroupId);
            receipt.DeliveredMembers++;
            receipt.Phase = receipt.DeliveredMembers == receipt.MemberCount
                ? SkirmishReservationPhase.Live : SkirmishReservationPhase.Producing;
            var receipts = em.GetBuffer<SkirmishProductionReservation>(session);
            receipts[index] = receipt;
            WriteStocks(em, session, receipt.FactionId, materials, fuel, capacity, next);
            if (em.HasComponent<SkirmishResearchStateComponent>(session))
                SkirmishResearchEffects.ApplyCompleted(em, session, receipt.FactionId);
            return true;
        }

        private static bool NativeAttemptActive(EntityManager em, Entity session, FixedString64Bytes attemptId)
        {
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (attemptId.Length == 0 || !state.SessionId.Equals(attemptId) || state.Phase != SkirmishSessionPhase.Playing) return false;
            if (em.HasComponent<SkirmishObjectiveClockComponent>(session) &&
                em.GetComponentData<SkirmishObjectiveClockComponent>(session).Paused != 0) return false;
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            return gameplay.IsEmptyIgnoreFilter || (gameplay.CalculateEntityCount() == 1 &&
                gameplay.GetSingleton<RuntimeGameplayStateComponent>().SimulationActive != 0);
        }

        private static int CountLogisticsSupport(EntityManager em, Entity session, byte faction)
        {
            var sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent));
            using var units = query.ToEntityArray(Allocator.Temp);
            int count = 0;
            foreach (var unit in units)
            {
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(unit);
                if (owner.SessionId.Equals(sessionId) && owner.FactionId == faction && owner.CapacityReleased == 0 &&
                    SkirmishArmyGroupSystem.IsAlive(em, unit) &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(unit).Category == SkirmishPopulationCategory.LogisticsSupport)
                    count++;
            }
            if (em.HasBuffer<SkirmishProductionReservation>(session))
                foreach (var receipt in em.GetBuffer<SkirmishProductionReservation>(session))
                    if (receipt.FactionId == faction && receipt.Category == SkirmishPopulationCategory.LogisticsSupport &&
                        (receipt.Phase == SkirmishReservationPhase.Reserved || receipt.Phase == SkirmishReservationPhase.Producing))
                        count += UndeliveredMembers(receipt);
            return count;
        }

        private static int UndeliveredMembers(SkirmishProductionReservation receipt) =>
            System.Math.Max(0, receipt.MemberCount - receipt.DeliveredMembers);
        private static int UndeliveredSupply(SkirmishProductionReservation receipt) =>
            receipt.MemberCount > 0 ? receipt.SupplyCost / receipt.MemberCount * UndeliveredMembers(receipt) : 0;
        private static int UndeliveredPayment(SkirmishProductionReservation receipt) =>
            receipt.MemberCount > 0 ? (int)((long)receipt.MaterialsPaid * UndeliveredMembers(receipt) / receipt.MemberCount) : 0;
    }
}
