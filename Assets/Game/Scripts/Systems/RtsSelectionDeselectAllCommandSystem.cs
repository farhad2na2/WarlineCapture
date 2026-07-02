using Unity.Collections;
using Unity.Entities;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RtsSelectionDeselectAllCommandSystem : ISystem
    {
        private EntityQuery _commandQueueQuery;
        private EntityQuery _selectedUnitQuery;

        public void OnCreate(ref SystemState state)
        {
            _commandQueueQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
            _selectedUnitQuery = state.GetEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            state.RequireForUpdate(_commandQueueQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            ProcessPendingRequests(state.EntityManager, _commandQueueQuery, _selectedUnitQuery);
        }

        public static bool ProcessPendingRequests(EntityManager em)
        {
            using EntityQuery commandQueueQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
            using EntityQuery selectedUnitQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            return ProcessPendingRequests(em, commandQueueQuery, selectedUnitQuery);
        }

        private static bool ProcessPendingRequests(
            EntityManager em,
            EntityQuery commandQueueQuery,
            EntityQuery selectedUnitQuery)
        {
            if (commandQueueQuery.IsEmptyIgnoreFilter)
                return false;

            Entity commandEntity = commandQueueQuery.GetSingletonEntity();
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
                em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            RtsSelectionInputStateComponent inputState = em.GetComponentData<RtsSelectionInputStateComponent>(commandEntity);
            bool handledAny = false;

            for (int i = 0; i < commandRequests.Length;)
            {
                if (commandRequests[i].Kind != RtsSelectionCommandIntentKind.DeselectAll)
                {
                    i++;
                    continue;
                }

                commandRequests.RemoveAt(i);
                handledAny = true;
            }

            if (!handledAny)
                return false;

            ClearCommandMode(ref inputState);
            em.SetComponentData(commandEntity, inputState);
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedUnitQuery.ToArchetypeChunkArray(Allocator.Temp);
            EntityCommandBuffer ecb = new(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    NativeArray<Entity> selectedUnits = chunks[chunkIndex].GetNativeArray(entityType);
                    for (int i = 0; i < selectedUnits.Length; i++)
                    {
                        Entity entity = selectedUnits[i];
                        if (em.Exists(entity) && em.HasComponent<SelectedUnitTag>(entity))
                            ecb.RemoveComponent<SelectedUnitTag>(entity);
                    }
                }

                ecb.Playback(em);
            }
            finally
            {
                ecb.Dispose();
            }

            return true;
        }

        private static void ClearCommandMode(ref RtsSelectionInputStateComponent inputState)
        {
            inputState.ActiveCommandMode = (int)TacticalCommandMode.None;
            inputState.ActiveCommandModeFrame = 0;
            inputState.ActiveCommandModeOneShot = 0;
            inputState.ActiveCommandModeRequiresWorldTarget = 0;
            inputState.ActiveBoardCommandDirection = 0;
            inputState.ActiveBoardTransport = Entity.Null;
            inputState.BoardPassengerDragArmed = 0;
            inputState.IgnoreNextLeftMouseRelease = 0;
            inputState.SkipNextWorldReleaseAfterSelection = 0;
        }
    }
}
