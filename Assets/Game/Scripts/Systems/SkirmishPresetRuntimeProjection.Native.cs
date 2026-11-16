using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    // Native-only authority selection at InitialUnitsSpawnSystem's original call
    // boundary. The source host supplies the same explicit values through its
    // validated external readback adapter instead of emulating Resources.Load.
    internal static partial class SkirmishPresetRuntimeProjection
    {
        internal static InitialUnitsSpawnConfig ResolveLegacyInitialOwnerValues(
            EntityManager em,
            InitialUnitsSpawnConfig fallback)
        {
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.CalculateEntityCount() != 1 ||
                matches.GetSingleton<SkirmishMatchState>().Phase >= SkirmishPhase.Playing)
                return fallback;

            var preset = SkirmishPresetResolver.Load(em);
            return SkirmishWorldSetup.ProjectNativePreset(preset).InitialUnits;
        }
    }
}
