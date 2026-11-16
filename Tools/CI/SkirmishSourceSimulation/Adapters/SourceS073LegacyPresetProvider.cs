using System;
using System.Runtime.CompilerServices;
using Game.Components;
using Game.Simulation;
using Unity.Entities;

namespace Game.Runtime
{
    // CLI implementation for the provider seam declared by the same shared
    // partial policy. Values enter only from the pinned source readback after it
    // has been compared with the actual imported startup owner output.
    internal static partial class SkirmishPresetRuntimeProjection
    {
        sealed class BoundSourceValues
        {
            internal SkirmishPresetRuntimeValues Values;
        }

        static readonly ConditionalWeakTable<EntityManager, BoundSourceValues> boundSourceValues = new();

        internal static void BindValidatedSourceReadback(
            EntityManager em,
            LegacyInitialSpawnData source,
            int reinforcementInfantryTarget,
            int attackSquadSize) => BindValidatedSourceReadback(
                em, source, reinforcementInfantryTarget, attackSquadSize, null);

        internal static void BindValidatedSourceReadback(
            EntityManager em,
            LegacyInitialSpawnData source,
            int reinforcementInfantryTarget,
            int attackSquadSize,
            uint? expectedQueuedSeed)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            SkirmishPresetRuntimeValues values = FromSourceReadback(
                em, source, reinforcementInfantryTarget, attackSquadSize, expectedQueuedSeed);
            boundSourceValues.Remove(em);
            boundSourceValues.Add(em, new BoundSourceValues { Values = values });
        }

        internal static SkirmishPresetRuntimeValues ResolveBoundSourceValues(EntityManager em)
        {
            if (em == null || !boundSourceValues.TryGetValue(em, out BoundSourceValues values))
                throw new NotSupportedException("Pinned external S073 preset readback must be bound to this source EntityManager");
            return values.Values;
        }

        internal static InitialUnitsSpawnConfig ResolveLegacyInitialOwnerValues(
            EntityManager em,
            InitialUnitsSpawnConfig fallback)
        {
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.CalculateEntityCount() != 1 ||
                matches.GetSingleton<SkirmishMatchState>().Phase >= SkirmishPhase.Playing)
                return fallback;
            if (!boundSourceValues.TryGetValue(em, out BoundSourceValues values))
                throw new NotSupportedException("Pinned external S073 preset readback must be bound to this source EntityManager");
            return values.Values.InitialUnits;
        }

        internal static void ReleaseBoundSourceReadback(EntityManager em)
        {
            if (em != null) boundSourceValues.Remove(em);
        }
    }
}
