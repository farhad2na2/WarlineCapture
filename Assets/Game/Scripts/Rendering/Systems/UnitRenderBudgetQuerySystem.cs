using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetSources
    {
        public readonly struct Context
        {
            public readonly EntityQuery UnitQuery;
            public readonly EntityQuery AllUnitGridQuery;
            public readonly EntityQuery SelectedUnitQuery;
            public readonly EntityQuery SpawnConfigQuery;
            public readonly EntityQuery SpawnProgressQuery;
            public readonly EntityQuery SpawnInitializedQuery;

            public Context(
                EntityQuery unitQuery,
                EntityQuery allUnitGridQuery,
                EntityQuery selectedUnitQuery,
                EntityQuery spawnConfigQuery,
                EntityQuery spawnProgressQuery,
                EntityQuery spawnInitializedQuery)
            {
                UnitQuery = unitQuery;
                AllUnitGridQuery = allUnitGridQuery;
                SelectedUnitQuery = selectedUnitQuery;
                SpawnConfigQuery = spawnConfigQuery;
                SpawnProgressQuery = spawnProgressQuery;
                SpawnInitializedQuery = spawnInitializedQuery;
            }
        }

        public Context Create(ref SystemState state)
        {
            EntityQuery unitQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadOnly<UnitMovementBehavior>(),
                },
                None = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<Disabled>(),
                }
            });
            EntityQuery allUnitGridQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitGrid>());
            EntityQuery selectedUnitQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<SelectedUnitTag>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Disabled>()
                }
            });
            EntityQuery spawnConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            EntityQuery spawnProgressQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
            EntityQuery spawnInitializedQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());

            return new Context(
                unitQuery,
                allUnitGridQuery,
                selectedUnitQuery,
                spawnConfigQuery,
                spawnProgressQuery,
                spawnInitializedQuery);
        }
    }
}
