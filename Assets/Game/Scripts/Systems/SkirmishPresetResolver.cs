using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class SkirmishPresetResolver
    {
        internal static SkirmishPresetConfig Load(EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
            int index = query.CalculateEntityCount() == 1 ? query.GetSingleton<SkirmishMatchState>().ScenarioIndex : 0;
            var preset = SkirmishPresetConfig.Load(index);
            if (preset == null) throw new System.InvalidOperationException("Missing selected Skirmish preset: " + index);
            return preset;
        }
    }
}
