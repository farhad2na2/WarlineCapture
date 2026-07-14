using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [Flags]
    internal enum MaterialsScenarioRecoveryPathCode : byte
    {
        None = 0,
        StartingMaterials = 1 << 0,
        SeededFabricationChain = 1 << 1,
        RebuildableFabricationChain = 1 << 2,
        ExchangeImport = 1 << 3,
        MaterialsNotRequired = 1 << 4
    }

    internal enum MaterialsScenarioRecoveryValidationCode : byte
    {
        Valid = 0,
        MissingStartupState = 1,
        MissingFactionControls = 2,
        DuplicateFaction = 3,
        MissingMaterialsCapacity = 4,
        NoRecoveryPath = 5,
        InvalidConstructionPlan = 6,
        MissingConstructionDefinition = 7,
        CatalogNotReady = 8
    }

    internal readonly struct MaterialsScenarioRecoveryValidationInput
    {
        public readonly byte FactionId;
        public readonly bool MaterialsRequired;
        public readonly int MinimumRequiredMaterials;
        public readonly int StartingMaterialsRequirement;
        public readonly int StartingMaterials;
        public readonly int MaterialsCapacity;
        public readonly bool HasSeededDepot;
        public readonly bool HasSeededOilSource;
        public readonly bool HasSeededOilHauler;
        public readonly bool CanRebuildDepot;
        public readonly bool CanRebuildOilSource;
        public readonly bool CanAcquireOilHauler;
        public readonly bool CanAffordRebuildChain;
        public readonly bool ExchangeImportEnabled;

        public MaterialsScenarioRecoveryValidationInput(
            byte factionId,
            bool materialsRequired,
            int minimumRequiredMaterials,
            int startingMaterialsRequirement,
            int startingMaterials,
            int materialsCapacity,
            bool hasSeededDepot,
            bool hasSeededOilSource,
            bool hasSeededOilHauler,
            bool canRebuildDepot,
            bool canRebuildOilSource,
            bool canAcquireOilHauler,
            bool canAffordRebuildChain,
            bool exchangeImportEnabled)
        {
            FactionId = factionId;
            MaterialsRequired = materialsRequired;
            MinimumRequiredMaterials = minimumRequiredMaterials;
            StartingMaterialsRequirement = startingMaterialsRequirement;
            StartingMaterials = startingMaterials;
            MaterialsCapacity = materialsCapacity;
            HasSeededDepot = hasSeededDepot;
            HasSeededOilSource = hasSeededOilSource;
            HasSeededOilHauler = hasSeededOilHauler;
            CanRebuildDepot = canRebuildDepot;
            CanRebuildOilSource = canRebuildOilSource;
            CanAcquireOilHauler = canAcquireOilHauler;
            CanAffordRebuildChain = canAffordRebuildChain;
            ExchangeImportEnabled = exchangeImportEnabled;
        }
    }

    internal readonly struct MaterialsScenarioRecoveryValidationResult
    {
        public readonly bool IsValid;
        public readonly MaterialsScenarioRecoveryValidationCode Code;
        public readonly MaterialsScenarioRecoveryPathCode Paths;
        public readonly byte FactionId;
        public readonly int ValidatedFactionCount;

        public MaterialsScenarioRecoveryValidationResult(
            bool isValid,
            MaterialsScenarioRecoveryValidationCode code,
            MaterialsScenarioRecoveryPathCode paths,
            byte factionId,
            int validatedFactionCount)
        {
            IsValid = isValid;
            Code = code;
            Paths = paths;
            FactionId = factionId;
            ValidatedFactionCount = validatedFactionCount;
        }
    }

    internal sealed class MaterialsScenarioRecoveryValidationSystemHelper
    {
        private const string DepotId = "Building_Ammunition_Depot";
        private const string OilSourceId = "Building_OilPump";
        private const string OilHaulerId = "Unit_Veh_Truck_Tray";

        private readonly EntityManager entityManager;
        private FixedString64Bytes lastInvalidConstructionId;

        public FixedString64Bytes LastInvalidConstructionId => lastInvalidConstructionId;

        public MaterialsScenarioRecoveryValidationSystemHelper(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        public MaterialsScenarioRecoveryValidationResult Validate(ResourceExchangeRecipeConfigSet exchangeConfig)
        {
            lastInvalidConstructionId = default;
            if (!TryResolveStartup(out Entity startupEntity, out CustomGameStartupStateComponent startupState,
                    out InitialUnitsSpawnConfig initialConfig))
            {
                return Invalid(MaterialsScenarioRecoveryValidationCode.MissingStartupState);
            }

            if (!TryResolveControls(out DynamicBuffer<FactionControlEntry> controls))
                return Invalid(MaterialsScenarioRecoveryValidationCode.MissingFactionControls);

            TryResolveBuildingCatalog(
                out DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables,
                out DynamicBuffer<BuildingConfiguredUnitReadModel> units,
                out bool hasBuildingCatalog);
            if (!hasBuildingCatalog)
                return Invalid(MaterialsScenarioRecoveryValidationCode.CatalogNotReady);

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
                        return new MaterialsScenarioRecoveryValidationResult(
                            false,
                            MaterialsScenarioRecoveryValidationCode.DuplicateFaction,
                            aggregatePaths,
                            control.FactionId,
                            controlIndex);
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
                        return new MaterialsScenarioRecoveryValidationResult(
                            false,
                            planValidationCode,
                            aggregatePaths,
                            control.FactionId,
                            controlIndex + 1);
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

                MaterialsScenarioRecoveryValidationResult factionResult = Evaluate(
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
                    return new MaterialsScenarioRecoveryValidationResult(
                        false,
                        factionResult.Code,
                        factionResult.Paths,
                        control.FactionId,
                        controlIndex + 1);
                }

                aggregatePaths |= factionResult.Paths;
            }

            return new MaterialsScenarioRecoveryValidationResult(
                true,
                MaterialsScenarioRecoveryValidationCode.Valid,
                aggregatePaths,
                0,
                controls.Length);
        }

        internal static MaterialsScenarioRecoveryValidationResult Evaluate(
            in MaterialsScenarioRecoveryValidationInput input)
        {
            if (!input.MaterialsRequired)
            {
                return Valid(input.FactionId, MaterialsScenarioRecoveryPathCode.MaterialsNotRequired);
            }

            int minimumRequiredMaterials = Math.Max(1, input.MinimumRequiredMaterials);
            if (input.MaterialsCapacity < minimumRequiredMaterials)
            {
                return new MaterialsScenarioRecoveryValidationResult(
                    false,
                    MaterialsScenarioRecoveryValidationCode.MissingMaterialsCapacity,
                    MaterialsScenarioRecoveryPathCode.None,
                    input.FactionId,
                    1);
            }

            MaterialsScenarioRecoveryPathCode paths = MaterialsScenarioRecoveryPathCode.None;
            int startingMaterialsRequirement = Math.Max(minimumRequiredMaterials, input.StartingMaterialsRequirement);
            if (input.StartingMaterials >= startingMaterialsRequirement)
                paths |= MaterialsScenarioRecoveryPathCode.StartingMaterials;
            if (input.HasSeededDepot && input.HasSeededOilSource && input.HasSeededOilHauler)
                paths |= MaterialsScenarioRecoveryPathCode.SeededFabricationChain;
            if (input.CanRebuildDepot && input.CanRebuildOilSource && input.CanAcquireOilHauler && input.CanAffordRebuildChain)
                paths |= MaterialsScenarioRecoveryPathCode.RebuildableFabricationChain;
            if (input.ExchangeImportEnabled)
                paths |= MaterialsScenarioRecoveryPathCode.ExchangeImport;

            return paths != MaterialsScenarioRecoveryPathCode.None
                ? Valid(input.FactionId, paths)
                : new MaterialsScenarioRecoveryValidationResult(
                    false,
                    MaterialsScenarioRecoveryValidationCode.NoRecoveryPath,
                    MaterialsScenarioRecoveryPathCode.None,
                    input.FactionId,
                    1);
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

        private static MaterialsScenarioRecoveryValidationResult Valid(
            byte factionId,
            MaterialsScenarioRecoveryPathCode paths)
        {
            return new MaterialsScenarioRecoveryValidationResult(
                true,
                MaterialsScenarioRecoveryValidationCode.Valid,
                paths,
                factionId,
                1);
        }

        private static MaterialsScenarioRecoveryValidationResult Invalid(
            MaterialsScenarioRecoveryValidationCode code)
        {
            return new MaterialsScenarioRecoveryValidationResult(
                false,
                code,
                MaterialsScenarioRecoveryPathCode.None,
                0,
                0);
        }
    }
}
