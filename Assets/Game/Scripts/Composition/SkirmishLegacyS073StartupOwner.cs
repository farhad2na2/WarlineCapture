using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Unity.Entities;

namespace Game.Composition
{
    // Shared production seam for the original S073 legacy startup and queued
    // session owners. Callers must validate external source identities before
    // passing the source-derived values here.
    internal static class SkirmishLegacyS073StartupOwner
    {
        internal readonly struct StartupResult
        {
            internal readonly CustomGameStartupSystemHelper.Result LegacyOwner;
            internal readonly SkirmishPresetRuntimeValues Preset;

            internal StartupResult(CustomGameStartupSystemHelper.Result legacyOwner, SkirmishPresetRuntimeValues preset)
            {
                LegacyOwner = legacyOwner;
                Preset = preset;
            }
        }

        internal static StartupResult InitializeFromSource(
            EntityManager em,
            LegacyInitialSpawnData source,
            IReadOnlyDictionary<string, Entity> prefabs,
            int reinforcementInfantryTarget,
            int attackSquadSize)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (prefabs == null) throw new ArgumentNullException(nameof(prefabs));

            var initialized = new CustomGameStartupSystemHelper(em).InitializeFromLegacyData(source, prefabs);
            if (!initialized.Initialized)
                throw new InvalidOperationException("Original legacy startup owner rejected S073 source data");

            var preset = SkirmishPresetRuntimeProjection.FromSourceReadback(
                em, source, reinforcementInfantryTarget, attackSquadSize);
            return new StartupResult(initialized, preset);
        }

        internal static bool TryQueueCapturedSelection(
            EntityManager em,
            int scenarioIndex,
            int seed)
        {
            if (!TryQueueCapturedSelectionBeforeStartup(em, scenarioIndex, seed)) return false;
            SkirmishLegacyLaunchOwner.ApplySeed(em);
            return true;
        }

        // Native composition queues first, prepares the map, initializes startup
        // configuration, then applies the seed. Keep those boundaries separable
        // for source-host diagnostics that reproduce that order.
        internal static bool TryQueueCapturedSelectionBeforeStartup(
            EntityManager em,
            int scenarioIndex,
            int seed)
        {
            if (em == null) throw new ArgumentNullException(nameof(em));
            if (scenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex ||
                seed != QuickGameConfig.Defaults.MapSeed)
                throw new InvalidOperationException("S073 launch selection differs from the retained native scenario/seed");

            QuickGameConfig config = QuickGameConfig.Defaults;
            config.ScenarioIndex = scenarioIndex;
            return SkirmishLegacyLaunchOwner.TryQueueIndustrialBasin(em, config);
        }
    }
}
