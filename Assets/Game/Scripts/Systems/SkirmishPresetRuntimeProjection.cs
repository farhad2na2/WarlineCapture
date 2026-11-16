using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal readonly struct SkirmishPresetRuntimeValues
    {
        internal readonly Entity PlanEntity;
        internal readonly InitialUnitsSpawnConfig InitialUnits;
        internal readonly InitialUnitsFactionSpawnEntry[] Factions;
        internal readonly int ReinforcementInfantryTarget;
        internal readonly int AttackSquadSize;

        internal SkirmishPresetRuntimeValues(
            Entity planEntity,
            InitialUnitsSpawnConfig initialUnits,
            InitialUnitsFactionSpawnEntry[] factions,
            int reinforcementInfantryTarget,
            int attackSquadSize)
        {
            PlanEntity = planEntity;
            InitialUnits = initialUnits;
            Factions = factions;
            ReinforcementInfantryTarget = reinforcementInfantryTarget;
            AttackSquadSize = attackSquadSize;
        }
    }

    internal readonly struct SkirmishPresetInitialValues
    {
        internal readonly Entity PlanEntity;
        internal readonly InitialUnitsSpawnConfig InitialUnits;
        internal readonly InitialUnitsFactionSpawnEntry[] Factions;

        internal SkirmishPresetInitialValues(
            Entity planEntity,
            InitialUnitsSpawnConfig initialUnits,
            InitialUnitsFactionSpawnEntry[] factions)
        {
            PlanEntity = planEntity;
            InitialUnits = initialUnits;
            Factions = factions;
        }
    }

    // Reads the real startup owner output. The native wrapper supplies the two
    // source-authored preset scalars after Resources.Load; the CLI supplies the
    // same scalars from its pinned source readback and verifies the startup rows.
    internal static partial class SkirmishPresetRuntimeProjection
    {
        // Native callers keep Resources.Load as their authority. Project the exact
        // serialized preset fields consumed by the shared startup policy; do not
        // substitute ECS startup rows, which may be absent or duplicated while
        // scenes stream.
        internal static SkirmishPresetRuntimeValues FromNativePreset(
            InitialUnitsSpawnConfig initial,
            InitialUnitsFactionSpawnEntry[] factions,
            int reinforcementInfantryTarget,
            int attackSquadSize)
        {
            return new SkirmishPresetRuntimeValues(Entity.Null, initial, factions,
                reinforcementInfantryTarget, attackSquadSize);
        }

        internal static SkirmishPresetRuntimeValues FromCurrentStartup(
            EntityManager em,
            int reinforcementInfantryTarget,
            int attackSquadSize)
        {
            ValidateScalars(reinforcementInfantryTarget, attackSquadSize);
            SkirmishPresetInitialValues initial = FromCurrentStartupInitial(em);
            return new SkirmishPresetRuntimeValues(
                initial.PlanEntity, initial.InitialUnits, initial.Factions,
                reinforcementInfantryTarget, attackSquadSize);
        }

        internal static SkirmishPresetInitialValues FromCurrentStartupInitial(EntityManager em)
        {
            using var plans = em.CreateEntityQuery(
                ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            if (plans.CalculateEntityCount() != 1)
                throw new InvalidOperationException("Unique source-owned Skirmish startup plan required");

            Entity plan = plans.GetSingletonEntity();
            if (!em.HasBuffer<InitialUnitsFactionSpawnEntry>(plan))
                throw new InvalidOperationException("Source-owned faction spawn rows required");
            InitialUnitsSpawnConfig initial = em.GetComponentData<InitialUnitsSpawnConfig>(plan);
            DynamicBuffer<InitialUnitsFactionSpawnEntry> buffer = em.GetBuffer<InitialUnitsFactionSpawnEntry>(plan);
            if (buffer.Length == 0)
                throw new InvalidOperationException("Nonempty source-owned faction spawn rows required");

            var factions = new InitialUnitsFactionSpawnEntry[buffer.Length];
            var ids = new HashSet<byte>();
            for (int i = 0; i < buffer.Length; i++)
            {
                factions[i] = buffer[i];
                if (!ids.Add(factions[i].FactionId))
                    throw new InvalidOperationException("Unique source-owned faction IDs required");
            }

            return new SkirmishPresetInitialValues(plan, initial, factions);
        }

        internal static SkirmishPresetRuntimeValues FromSourceReadback(
            EntityManager em,
            LegacyInitialSpawnData source,
            int reinforcementInfantryTarget,
            int attackSquadSize,
            uint? expectedQueuedSeed = null)
        {
            if (source == null)
                throw new InvalidOperationException("Pinned external legacy spawn readback required");

            SkirmishPresetRuntimeValues values = FromCurrentStartup(
                em, reinforcementInfantryTarget, attackSquadSize);
            InitialUnitsSpawnConfig config = values.InitialUnits;
            uint expectedRandomSeed = Math.Max(1u, source.RandomSeed);
            if (expectedQueuedSeed.HasValue)
            {
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1)
                    throw new InvalidOperationException("Unique S073 startup session required for the original ApplySeed projection");
                SkirmishMatchState session = matches.GetSingleton<SkirmishMatchState>();
                if (session.ScenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex ||
                    (session.Phase != SkirmishPhase.Queued && session.Phase != SkirmishPhase.Preparing) ||
                    (uint)session.Seed != expectedQueuedSeed.Value)
                    throw new InvalidOperationException("Expected seed must come from the actual S073 startup session");
                expectedRandomSeed = expectedQueuedSeed.Value;
            }
            if (config.BlockerCount != source.BlockerCount ||
                config.SpawnRadiusCells != Math.Max(0, source.SpawnRadiusCells) ||
                config.RandomSeed != expectedRandomSeed ||
                config.InitialDollars != Math.Max(0, source.InitialDollars) ||
                config.InitialMaterials != Math.Max(0, source.InitialMaterials) ||
                config.MaterialsCapacity != Math.Max(0, source.MaterialsCapacity) ||
                config.InitialAiMaterials != Math.Max(0, source.InitialAiMaterials) ||
                config.AiMaterialsCapacity != Math.Max(0, source.AiMaterialsCapacity) ||
                config.InitialOil != Math.Max(0, source.InitialOil) ||
                config.InitialFuel != Math.Max(0, source.InitialFuel) ||
                config.CreateFactionBases != (source.CreateFactionBases ? (byte)1 : (byte)0))
                throw new InvalidOperationException("Materialized startup values differ from the pinned legacy readback");

            if (source.Factions == null || source.Factions.Length != values.Factions.Length)
                throw new InvalidOperationException("Materialized faction row count differs from the pinned legacy readback");
            for (int i = 0; i < source.Factions.Length; i++)
            {
                LegacyInitialFactionData authored = source.Factions[i];
                InitialUnitsFactionSpawnEntry actual = values.Factions[i];
                if (authored == null || authored.FactionId != actual.FactionId ||
                    authored.SpawnX != actual.SpawnCell.x || authored.SpawnY != actual.SpawnCell.y)
                    throw new InvalidOperationException("Materialized faction spawn row differs from pinned legacy readback at index " + i);
            }

            return values;
        }

        private static void ValidateScalars(int reinforcementInfantryTarget, int attackSquadSize)
        {
            if (reinforcementInfantryTarget < 1)
                throw new InvalidOperationException("Source-authored reinforcement target is required");
            if (attackSquadSize < 4 || attackSquadSize > 8)
                throw new InvalidOperationException("Source-authored attack squad size is outside its configured range");
        }
    }
}
