using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class MaterialsScenarioRecoveryStartupSystemHelper
    {
        private const string DepotId = "Building_Ammunition_Depot";
        private const string OilSourceId = "Building_OilPump";
        private const string OilHaulerId = "Unit_Veh_Truck_Tray";

        private readonly EntityManager entityManager;
        private FixedString64Bytes lastInvalidConstructionId;

        public FixedString64Bytes LastInvalidConstructionId => lastInvalidConstructionId;

        public MaterialsScenarioRecoveryStartupSystemHelper(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public MaterialsScenarioRecoveryValidationResult Validate(ResourceExchangeRecipeConfigSet exchangeConfig)
        {
            lastInvalidConstructionId = default;
            if (!TryResolveStartup(out Entity startupEntity, out CustomGameStartupStateComponent startupState,
                    out InitialUnitsSpawnConfig initialConfig))
            {
                return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Invalid(
                    MaterialsScenarioRecoveryValidationCode.MissingStartupState);
            }

            if (!TryResolveControls(out DynamicBuffer<FactionControlEntry> controls))
                return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Invalid(
                    MaterialsScenarioRecoveryValidationCode.MissingFactionControls);

            TryResolveBuildingCatalog(
                out DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
                out DynamicBuffer<BuildingConfiguredUnitReadModel> units,
                out bool hasBuildingCatalog);
            if (!hasBuildingCatalog)
                return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Invalid(
                    MaterialsScenarioRecoveryValidationCode.CatalogNotReady);

            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> initialBuildings =
                entityManager.HasBuffer<InitialUnitsFactionBuildingSpawnEntry>(startupEntity)
                    ? entityManager.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(startupEntity, true)
                    : default;
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> initialUnits =
                entityManager.HasBuffer<CustomGameFactionUnitSourceSpawnEntry>(startupEntity)
                    ? entityManager.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(startupEntity, true)
                    : default;

            MaterialsScenarioRecoveryPathCode aggregatePaths = MaterialsScenarioRecoveryPathCode.None;
            for (int controlIndex = 0; controlIndex < controls.Length; controlIndex++)
            {
                FactionControlEntry control = controls[controlIndex];
                for (int duplicateIndex = 0; duplicateIndex < controlIndex; duplicateIndex++)
                {
                    if (controls[duplicateIndex].FactionId == control.FactionId)
                    {
                        return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Invalid(
                            MaterialsScenarioRecoveryValidationCode.DuplicateFaction,
                            control.FactionId,
                            controlIndex,
                            aggregatePaths);
                    }
                }

                bool isPlayer = control.IsPlayerFaction != 0;
                int startingMaterials = isPlayer ? initialConfig.InitialMaterials : initialConfig.InitialAiMaterials;
                int materialsCapacity = isPlayer ? initialConfig.MaterialsCapacity : initialConfig.AiMaterialsCapacity;
                int minimumRequiredMaterials;
                int startingMaterialsRequirement;
                if (control.AIControlled != 0)
                {
                    if (!TryResolveAIConstructionDemand(
                            control.FactionId,
                            spawnables,
                            hasBuildingCatalog,
                            out minimumRequiredMaterials,
                            out startingMaterialsRequirement,
                            out MaterialsScenarioRecoveryValidationCode planValidationCode))
                    {
                        return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Invalid(
                            planValidationCode,
                            control.FactionId,
                            controlIndex + 1,
                            aggregatePaths);
                    }
                }
                else
                {
                    minimumRequiredMaterials = ResolveMinimumMaterialsCost(spawnables, hasBuildingCatalog);
                    startingMaterialsRequirement = minimumRequiredMaterials;
                }

                bool materialsRequired = startingMaterialsRequirement > 0;
                bool hasSeededDepot =
                    (initialConfig.CreateFactionBases != 0 && IsIdentity(initialConfig.BaseCoreBuildingPrefabLookupKey, DepotId)) ||
                    HasInitialBuilding(initialBuildings, control.FactionId, DepotId);
                bool hasSeededOilSource = HasInitialBuilding(initialBuildings, control.FactionId, OilSourceId);
                bool hasSeededOilHauler = HasInitialUnit(initialUnits, control.FactionId, OilHaulerId);

                bool canRebuildDepot = TryResolveBuilding(spawnables, hasBuildingCatalog, DepotId, out BuildingConfiguredSpawnableReadModel depot);
                bool canRebuildOilSource = TryResolveBuilding(spawnables, hasBuildingCatalog, OilSourceId, out BuildingConfiguredSpawnableReadModel oilSource);
                bool canAcquireOilHauler = TryResolveUnit(units, hasBuildingCatalog, OilHaulerId, out BuildingConfiguredUnitReadModel oilHauler);
                int rebuildMaterials = SaturatingAdd(depot.MaterialsCost, oilSource.MaterialsCost);
                int rebuildCredits = SaturatingAdd(SaturatingAdd(depot.Price, oilSource.Price), oilHauler.Price);
                int startingCredits = ResolveStartingCredits(control.FactionId, isPlayer, initialConfig.InitialDollars);

                MaterialsScenarioRecoveryValidationResult factionResult =
                    MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Evaluate(
                        new MaterialsScenarioRecoveryValidationInput(
                            control.FactionId,
                            materialsRequired,
                            minimumRequiredMaterials,
                            startingMaterialsRequirement,
                            startingMaterials,
                            materialsCapacity,
                            hasSeededDepot,
                            hasSeededOilSource,
                            hasSeededOilHauler,
                            canRebuildDepot,
                            canRebuildOilSource,
                            canAcquireOilHauler,
                            startingMaterials >= rebuildMaterials && startingCredits >= rebuildCredits,
                            HasExchangeImport(
                                exchangeConfig,
                                startupState.GameModeId,
                                control.AIControlled != 0,
                                materialsCapacity,
                                startingCredits)));
                if (!factionResult.IsValid)
                {
                    return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Invalid(
                        factionResult.Code,
                        control.FactionId,
                        controlIndex + 1,
                        factionResult.Paths);
                }

                aggregatePaths |= factionResult.Paths;
            }

            return MaterialsScenarioRecoveryPolicyUtilitySystemHelper.Valid(
                0,
                aggregatePaths,
                controls.Length);
        }

        private bool TryResolveStartup(
            out Entity entity,
            out CustomGameStartupStateComponent state,
            out InitialUnitsSpawnConfig config)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            if (query.CalculateEntityCount() != 1)
            {
                entity = Entity.Null;
                state = default;
                config = default;
                return false;
            }

            entity = query.GetSingletonEntity();
            state = entityManager.GetComponentData<CustomGameStartupStateComponent>(entity);
            config = entityManager.GetComponentData<InitialUnitsSpawnConfig>(entity);
            return true;
        }

        private bool TryResolveControls(out DynamicBuffer<FactionControlEntry> controls)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FactionControlConfigTag>(),
                ComponentType.ReadOnly<FactionControlEntry>());
            if (query.CalculateEntityCount() != 1)
            {
                controls = default;
                return false;
            }

            controls = entityManager.GetBuffer<FactionControlEntry>(query.GetSingletonEntity(), true);
            return controls.Length > 0;
        }

        private void TryResolveBuildingCatalog(
            out DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            out DynamicBuffer<BuildingConfiguredUnitReadModel> units,
            out bool resolved)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingConfiguredSpawnableReadModel>(),
                ComponentType.ReadOnly<BuildingConfiguredUnitReadModel>());
            if (query.CalculateEntityCount() != 1)
            {
                spawnables = default;
                units = default;
                resolved = false;
                return;
            }

            Entity entity = query.GetSingletonEntity();
            spawnables = entityManager.GetBuffer<BuildingConfiguredSpawnableReadModel>(entity, true);
            units = entityManager.GetBuffer<BuildingConfiguredUnitReadModel>(entity, true);
            resolved = spawnables.Length > 0;
        }

        private int ResolveStartingCredits(byte factionId, bool isPlayer, int configuredPlayerCredits)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();
            ComponentTypeHandle<FactionEconomy> economyType =
                entityManager.GetComponentTypeHandle<FactionEconomy>(true);
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                NativeArray<FactionEconomy> economies = chunks[chunkIndex].GetNativeArray(ref economyType);
                for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                {
                    if (economies[entityIndex].FactionId == factionId)
                        return Math.Max(0, economies[entityIndex].Money);
                }
            }

            return isPlayer ? Math.Max(0, configuredPlayerCredits) : 0;
        }

        private static int ResolveMinimumMaterialsCost(
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            bool hasCatalog)
        {
            if (!hasCatalog)
                return 1;

            int minimum = int.MaxValue;
            for (int i = 0; i < spawnables.Length; i++)
            {
                BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
                if (candidate.CanRequest != 0 && candidate.MaterialsCost > 0)
                    minimum = Math.Min(minimum, candidate.MaterialsCost);
            }

            return minimum == int.MaxValue ? 0 : minimum;
        }

        private bool TryResolveAIConstructionDemand(
            byte factionId,
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            bool hasCatalog,
            out int maximumSingleBuildMaterials,
            out int totalPlanMaterials,
            out MaterialsScenarioRecoveryValidationCode code)
        {
            maximumSingleBuildMaterials = 0;
            totalPlanMaterials = 0;
            code = MaterialsScenarioRecoveryValidationCode.InvalidConstructionPlan;
            if (!hasCatalog)
                return false;

            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<AIBuildPlan>(),
                ComponentType.ReadOnly<AIBuildPlanEntry>());
            ComponentTypeHandle<AIBuildPlan> planType =
                entityManager.GetComponentTypeHandle<AIBuildPlan>(true);
            BufferTypeHandle<AIBuildPlanEntry> entryType =
                entityManager.GetBufferTypeHandle<AIBuildPlanEntry>(true);
            int matchingPlanCount = 0;
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<AIBuildPlan> plans = chunk.GetNativeArray(ref planType);
                BufferAccessor<AIBuildPlanEntry> entriesByPlan = chunk.GetBufferAccessor(ref entryType);
                for (int planIndex = 0; planIndex < plans.Length; planIndex++)
                {
                    AIBuildPlan plan = plans[planIndex];
                    if (plan.Enabled == 0 || plan.FactionId != factionId)
                        continue;

                    matchingPlanCount++;
                    DynamicBuffer<AIBuildPlanEntry> entries = entriesByPlan[planIndex];
                    for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                    {
                        if (!TryResolveBuilding(
                                spawnables,
                                hasCatalog,
                                entries[entryIndex].BuildingId,
                                out BuildingConfiguredSpawnableReadModel building))
                        {
                            lastInvalidConstructionId = entries[entryIndex].BuildingId;
                            code = MaterialsScenarioRecoveryValidationCode.MissingConstructionDefinition;
                            return false;
                        }

                        int materialsCost = Math.Max(0, building.MaterialsCost);
                        maximumSingleBuildMaterials = Math.Max(maximumSingleBuildMaterials, materialsCost);
                        totalPlanMaterials = SaturatingAdd(totalPlanMaterials, materialsCost);
                    }
                }
            }

            if (matchingPlanCount > 1)
            {
                lastInvalidConstructionId = new FixedString64Bytes("duplicate-plan");
                return false;
            }

            code = MaterialsScenarioRecoveryValidationCode.Valid;
            return true;
        }

        private static bool HasInitialBuilding(
            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildings,
            byte factionId,
            string identity)
        {
            if (!buildings.IsCreated)
                return false;

            for (int i = 0; i < buildings.Length; i++)
            {
                InitialUnitsFactionBuildingSpawnEntry building = buildings[i];
                if (building.FactionId == factionId && IsIdentity(building.PrefabLookupKey, identity))
                    return true;
            }

            return false;
        }

        private static bool HasInitialUnit(
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> units,
            byte factionId,
            string identity)
        {
            if (!units.IsCreated)
                return false;

            for (int i = 0; i < units.Length; i++)
            {
                CustomGameFactionUnitSourceSpawnEntry unit = units[i];
                if (unit.FactionId == factionId && unit.Count > 0 && IsIdentity(unit.SourceKey, identity))
                    return true;
            }

            return false;
        }

        private static bool TryResolveBuilding(
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            bool hasCatalog,
            string identity,
            out BuildingConfiguredSpawnableReadModel model)
        {
            if (hasCatalog)
            {
                for (int i = 0; i < spawnables.Length; i++)
                {
                    BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
                    if (candidate.CanRequest != 0 && IsIdentity(candidate.BuildingId, identity))
                    {
                        model = candidate;
                        return true;
                    }
                }
            }

            model = default;
            return false;
        }

        private static bool TryResolveBuilding(
            DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
            bool hasCatalog,
            in FixedString64Bytes identity,
            out BuildingConfiguredSpawnableReadModel model)
        {
            if (hasCatalog)
            {
                string expected = identity.ToString();
                for (int i = 0; i < spawnables.Length; i++)
                {
                    BuildingConfiguredSpawnableReadModel candidate = spawnables[i];
                    if (candidate.CanRequest != 0 && IsIdentity(candidate.BuildingId, expected))
                    {
                        model = candidate;
                        return true;
                    }
                }
            }

            model = default;
            return false;
        }

        private static bool TryResolveUnit(
            DynamicBuffer<BuildingConfiguredUnitReadModel> units,
            bool hasCatalog,
            string identity,
            out BuildingConfiguredUnitReadModel model)
        {
            if (hasCatalog)
            {
                for (int i = 0; i < units.Length; i++)
                {
                    BuildingConfiguredUnitReadModel candidate = units[i];
                    if (candidate.CanRequest != 0 && IsIdentity(candidate.UnitId, identity))
                    {
                        model = candidate;
                        return true;
                    }
                }
            }

            model = default;
            return false;
        }

        private static bool HasExchangeImport(
            ResourceExchangeRecipeConfigSet config,
            in FixedString64Bytes scenarioTag,
            bool isAIControlled,
            int materialsCapacity,
            int startingCredits)
        {
            if (config == null ||
                !ResourceExchangeStartupProjectionSystemHelper.TryResolveGate(config, scenarioTag, out ResourceExchangeScenarioGateConfigEntry gate) ||
                !gate.ExchangeEnabled ||
                gate.MaxQueueItems <= 0 ||
                (isAIControlled && !gate.AllowAiExchange))
            {
                return false;
            }

            for (int i = 0; i < config.Recipes.Count; i++)
            {
                ResourceExchangeRecipeConfigEntry recipe = config.Recipes[i];
                if (recipe != null &&
                    string.Equals(recipe.MissionTag, scenarioTag.ToString(), StringComparison.Ordinal) &&
                    recipe.RouteType == ResourceExchangeRouteType.Import &&
                    recipe.InputResource == ResourceExchangeResourceKind.Credits &&
                    recipe.OutputResource == ResourceExchangeResourceKind.Materials &&
                    ResourceExchangeRecipeConfigValidator.ValidateRecipe(recipe) == ResourceExchangeReason.None)
                {
                    int output = (int)Math.Floor(
                        recipe.InputAmountMin *
                        Math.Max(0f, recipe.OutputPerInput) *
                        (1f - recipe.FeePercent));
                    if (recipe.InputAmountMin <= startingCredits &&
                        output > 0 &&
                        output <= materialsCapacity)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsIdentity(in FixedString128Bytes value, string expected)
        {
            return string.Equals(value.ToString(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsIdentity(in FixedString64Bytes value, string expected)
        {
            return string.Equals(value.ToString(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static int SaturatingAdd(int left, int right)
        {
            long sum = (long)Math.Max(0, left) + Math.Max(0, right);
            return sum >= int.MaxValue ? int.MaxValue : (int)sum;
        }

    }
}
