using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    // The single source-owned transition used when the native composition
    // requests play and by bounded source hosts that already own the EntityManager.
    internal static class RuntimeGameplayStartTransition
    {
        internal static void ResetForGameplayStart(EntityManager entityManager)
        {
            Entity entity;
            using (EntityQuery query = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<RuntimeGameplayStateComponent>()))
            {
                if (query.CalculateEntityCount() > 1)
                    throw new System.InvalidOperationException("Unique runtime gameplay state required");
                entity = query.IsEmptyIgnoreFilter
                    ? entityManager.CreateEntity(typeof(RuntimeGameplayStateComponent))
                    : query.GetSingletonEntity();
            }

            EnsureStateComponents(entityManager, entity);
            RuntimeGameplayStateComponent gameplay = entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity);
            gameplay.PlayRequested = 1;
            gameplay.SimulationActive = 0;
            gameplay.SelectionModeActive = 0;
            gameplay.BuildModeActive = 0;
            gameplay.FullscreenMapOpen = 0;
            gameplay.FullscreenMapIsoMode = 0;
            gameplay.SuppressNextWorldClick = 1;
            entityManager.SetComponentData(entity, gameplay);
            entityManager.SetComponentData(entity, default(RuntimeCameraInputComponent));
            entityManager.SetComponentData(entity, default(RuntimeCameraFocusRequestComponent));
        }

        // Source-only diagnostic activation after the shared loading predicate
        // has been evaluated against the same EntityManager's original owners.
        // This intentionally carries no native physical-scene/readiness claim.
        internal static bool TryActivateSourceHostDiagnostic(EntityManager entityManager, bool startComplete)
        {
            if (!startComplete)
                return false;

            using (EntityQuery query = entityManager.CreateEntityQuery(
                       ComponentType.ReadWrite<RuntimeGameplayStateComponent>()))
            {
                if (query.CalculateEntityCount() != 1)
                    return false;
                Entity entity = query.GetSingletonEntity();
                RuntimeGameplayStateComponent state = entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity);
                if (state.PlayRequested == 0 || state.SimulationActive != 0)
                    return false;
                state.SimulationActive = 1;
                entityManager.SetComponentData(entity, state);
                return true;
            }
        }

        private static void EnsureStateComponents(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<RuntimeCameraInputComponent>(entity))
                entityManager.AddComponent<RuntimeCameraInputComponent>(entity);
            if (!entityManager.HasComponent<RuntimeCameraFocusRequestComponent>(entity))
                entityManager.AddComponent<RuntimeCameraFocusRequestComponent>(entity);
        }
    }
}
