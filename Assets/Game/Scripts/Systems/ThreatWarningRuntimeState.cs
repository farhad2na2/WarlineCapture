using Unity.Entities;

namespace Game.Runtime
{
    public enum ThreatWarningType : byte
    {
        Ground = 1,
        Air = 2
    }

    public struct ThreatWarningRuntimeStateComponent : IComponentData
    {
        public byte HasPendingWarning;
        public ThreatWarningType PendingType;
        public float PendingEtaSeconds;
        public int PendingThreatCount;
        public uint Version;
    }

    public static class ThreatWarningRuntimeState
    {
        internal static EntityQuery CreateQuery(EntityManager entityManager, bool readOnly)
        {
            ComponentType componentType = readOnly
                ? ComponentType.ReadOnly<ThreatWarningRuntimeStateComponent>()
                : ComponentType.ReadWrite<ThreatWarningRuntimeStateComponent>();
            return entityManager.CreateEntityQuery(componentType);
        }

        internal static void EnsureSingleton(EntityManager entityManager, EntityQuery query)
        {
            if (query.CalculateEntityCount() != 0)
                return;

            Entity entity = entityManager.CreateEntity(typeof(ThreatWarningRuntimeStateComponent));
            entityManager.SetName(entity, nameof(ThreatWarningRuntimeStateComponent));
            entityManager.SetComponentData(entity, CreateResetState(0u));
        }

        public static bool TryRead(
            EntityManager entityManager,
            out ThreatWarningRuntimeStateComponent warningState)
        {
            using EntityQuery query = CreateQuery(entityManager, readOnly: true);
            return TryRead(entityManager, query, out warningState);
        }

        internal static bool TryRead(
            EntityManager entityManager,
            EntityQuery query,
            out ThreatWarningRuntimeStateComponent warningState)
        {
            if (!TryResolveSingleton(query, out Entity entity))
            {
                warningState = default;
                return false;
            }

            warningState = entityManager.GetComponentData<ThreatWarningRuntimeStateComponent>(entity);
            return true;
        }

        public static bool RequestWarning(
            EntityManager entityManager,
            ThreatWarningType type,
            float etaSeconds,
            int threatCount)
        {
            using EntityQuery query = CreateQuery(entityManager, readOnly: false);
            return RequestWarning(entityManager, query, type, etaSeconds, threatCount);
        }

        internal static bool RequestWarning(
            EntityManager entityManager,
            EntityQuery query,
            ThreatWarningType type,
            float etaSeconds,
            int threatCount)
        {
            if (!TryRead(entityManager, query, out ThreatWarningRuntimeStateComponent warningState))
                return false;

            warningState.HasPendingWarning = 1;
            warningState.PendingType = type;
            warningState.PendingEtaSeconds = etaSeconds < 0f ? 0f : etaSeconds;
            warningState.PendingThreatCount = threatCount < 1 ? 1 : threatCount;
            warningState.Version++;
            return TryWrite(entityManager, query, warningState);
        }

        public static bool ClearPendingWarning(EntityManager entityManager)
        {
            using EntityQuery query = CreateQuery(entityManager, readOnly: false);
            return ClearPendingWarning(entityManager, query);
        }

        internal static bool ClearPendingWarning(EntityManager entityManager, EntityQuery query)
        {
            if (!TryRead(entityManager, query, out ThreatWarningRuntimeStateComponent warningState))
                return false;

            warningState.HasPendingWarning = 0;
            warningState.Version++;
            return TryWrite(entityManager, query, warningState);
        }

        public static bool Reset(EntityManager entityManager)
        {
            using EntityQuery query = CreateQuery(entityManager, readOnly: false);
            return Reset(entityManager, query);
        }

        internal static bool Reset(EntityManager entityManager, EntityQuery query)
        {
            if (!TryRead(entityManager, query, out ThreatWarningRuntimeStateComponent warningState))
                return false;

            if (warningState.HasPendingWarning == 0 &&
                warningState.PendingType == ThreatWarningType.Ground &&
                warningState.PendingEtaSeconds == 0f &&
                warningState.PendingThreatCount == 0)
                return true;

            return TryWrite(entityManager, query, CreateResetState(warningState.Version + 1u));
        }

        private static bool TryWrite(
            EntityManager entityManager,
            EntityQuery query,
            ThreatWarningRuntimeStateComponent warningState)
        {
            if (!TryResolveSingleton(query, out Entity entity))
                return false;

            entityManager.SetComponentData(entity, warningState);
            return true;
        }

        private static bool TryResolveSingleton(EntityQuery query, out Entity entity)
        {
            if (query.CalculateEntityCount() != 1)
            {
                entity = Entity.Null;
                return false;
            }

            entity = query.GetSingletonEntity();
            return true;
        }

        private static ThreatWarningRuntimeStateComponent CreateResetState(uint version)
        {
            return new ThreatWarningRuntimeStateComponent
            {
                HasPendingWarning = 0,
                PendingType = ThreatWarningType.Ground,
                PendingEtaSeconds = 0f,
                PendingThreatCount = 0,
                Version = version
            };
        }
    }
}
