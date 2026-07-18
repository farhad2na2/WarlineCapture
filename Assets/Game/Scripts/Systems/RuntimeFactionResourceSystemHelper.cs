using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RuntimeFactionResourceSystemHelper
    {
        private EntityManager _entityManager;
        private Entity _controlledFactionResourceEntity;
        private int _pendingInitialDollars;
        private bool _isConfigured;

        public int CurrentDollars => TryGetControlledFactionEconomy(out FactionEconomy economy)
            ? economy.Money
            : 0;

        public int CurrentMaterials => TryGetControlledFactionResources(
            out _,
            out FactionTacticalMaterialsComponent materials)
                ? materials.Current
                : 0;

        public void SetInitialDollars(int dollars)
        {
            _pendingInitialDollars = Mathf.Max(0, dollars);
            if (!TryGetControlledFactionEconomy(out FactionEconomy economy))
                return;

            economy.Money = _pendingInitialDollars;
            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
        }

        public void Configure(EntityManager entityManager)
        {
            _entityManager = entityManager;
            _controlledFactionResourceEntity = Entity.Null;
            _isConfigured = true;
            EnsureControlledFactionResources();
        }

        public void AddDollars(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0 || !TryGetControlledFactionEconomy(out FactionEconomy economy))
                return;

            economy.Money = economy.Money >= int.MaxValue - amount
                ? int.MaxValue
                : economy.Money + amount;
            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
        }

        public bool TrySpendDollars(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount <= 0)
                return true;

            return TrySpendConstructionResources(amount, 0) ==
                   FactionConstructionResourceMutationResult.Applied;
        }

        public bool TrySpendMaterials(int amount)
        {
            amount = Mathf.Max(0, amount);
            return amount == 0 ||
                   TrySpendConstructionResources(0, amount) ==
                   FactionConstructionResourceMutationResult.Applied;
        }

        public void RefundMaterials(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0)
                return;

            TryRestoreConstructionResources(0, amount);
        }

        public FactionConstructionResourceMutationResult TrySpendConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            if (!TryGetControlledFactionResources(
                    out FactionEconomy economy,
                    out FactionTacticalMaterialsComponent materials))
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                FactionConstructionResourceUtilitySystemHelper.TrySpend(
                    ref economy,
                    ref materials,
                    creditsCost,
                    materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
            if (materialsCost > 0)
                _entityManager.SetComponentData(_controlledFactionResourceEntity, materials);
            return result;
        }

        public FactionConstructionResourceMutationResult EvaluateConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            return TryGetControlledFactionResources(
                out FactionEconomy economy,
                out FactionTacticalMaterialsComponent materials)
                ? FactionConstructionResourceUtilitySystemHelper.Evaluate(
                    economy,
                    materials,
                    creditsCost,
                    materialsCost)
                : FactionConstructionResourceMutationResult.InvalidState;
        }

        public FactionConstructionResourceMutationResult TryRestoreConstructionResources(
            int creditsCost,
            int materialsCost)
        {
            if (!TryGetControlledFactionResources(
                    out FactionEconomy economy,
                    out FactionTacticalMaterialsComponent materials))
                return FactionConstructionResourceMutationResult.InvalidState;

            FactionConstructionResourceMutationResult result =
                FactionConstructionResourceUtilitySystemHelper.TryRollback(
                    ref economy,
                    ref materials,
                    creditsCost,
                    materialsCost);
            if (result != FactionConstructionResourceMutationResult.Applied)
                return result;

            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
            if (materialsCost > 0)
                _entityManager.SetComponentData(_controlledFactionResourceEntity, materials);
            return result;
        }

        public bool TryGetFactionResourceEntity(byte factionId, out Entity entity)
        {
            entity = Entity.Null;
            if (!TryGetControlledFactionEconomy(out FactionEconomy economy) ||
                economy.FactionId != factionId)
                return false;

            entity = _controlledFactionResourceEntity;
            return _entityManager.HasComponent<FactionTacticalMaterialsComponent>(entity);
        }

        public CitizenResourceCompositionSystemHelper.Context CreateCitizenResourceContext()
        {
            return new CitizenResourceCompositionSystemHelper.Context(
                () => CurrentDollars,
                SetDollars);
        }

        private void SetDollars(int value)
        {
            if (!TryGetControlledFactionEconomy(out FactionEconomy economy))
                return;

            economy.Money = Mathf.Max(0, value);
            _entityManager.SetComponentData(_controlledFactionResourceEntity, economy);
        }

        private void EnsureControlledFactionResources()
        {
            if (!IsEntityManagerAvailable())
                return;

            Entity resolvedEntity = Entity.Null;
            FactionEconomy resolvedEconomy = default;
            using (EntityQuery query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>()))
            using (Unity.Collections.NativeArray<ArchetypeChunk> chunks =
                   query.ToArchetypeChunkArray(Unity.Collections.Allocator.Temp))
            {
                EntityTypeHandle entityType = _entityManager.GetEntityTypeHandle();
                ComponentTypeHandle<FactionEconomy> economyType =
                    _entityManager.GetComponentTypeHandle<FactionEconomy>(true);
                for (int chunkIndex = 0; chunkIndex < chunks.Length && resolvedEntity == Entity.Null; chunkIndex++)
                {
                    ArchetypeChunk chunk = chunks[chunkIndex];
                    Unity.Collections.NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                    Unity.Collections.NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (!FactionIdentity.IsPlayerControlled(economies[i].FactionId))
                            continue;

                        resolvedEntity = entities[i];
                        resolvedEconomy = economies[i];
                        break;
                    }
                }
            }

            if (resolvedEntity != Entity.Null)
            {
                _controlledFactionResourceEntity = resolvedEntity;
                resolvedEconomy.Money = _pendingInitialDollars;
                _entityManager.SetComponentData(resolvedEntity, resolvedEconomy);
                EnsureCompanionComponents(resolvedEntity);
                return;
            }

            _controlledFactionResourceEntity = _entityManager.CreateEntity(
                typeof(FactionEconomy),
                typeof(FactionEconomyPolicy),
                typeof(FactionTacticalMaterialsComponent));
            _entityManager.SetComponentData(_controlledFactionResourceEntity, new FactionEconomy
            {
                FactionId = FactionIdentity.PlayerFactionId,
                Money = _pendingInitialDollars
            });
            _entityManager.SetComponentData(_controlledFactionResourceEntity, new FactionEconomyPolicy
            {
                Enabled = 0,
                IncomeMultiplier = 1f
            });
            _entityManager.SetComponentData(_controlledFactionResourceEntity, new FactionTacticalMaterialsComponent
            {
                FactionId = FactionIdentity.PlayerFactionId
            });
        }

        private void EnsureCompanionComponents(Entity entity)
        {
            if (!_entityManager.HasComponent<FactionEconomyPolicy>(entity))
            {
                _entityManager.AddComponentData(entity, new FactionEconomyPolicy
                {
                    Enabled = 0,
                    IncomeMultiplier = 1f
                });
            }

            if (!_entityManager.HasComponent<FactionTacticalMaterialsComponent>(entity))
            {
                _entityManager.AddComponentData(entity, new FactionTacticalMaterialsComponent
                {
                    FactionId = FactionIdentity.PlayerFactionId
                });
            }
        }

        private bool TryGetControlledFactionEconomy(out FactionEconomy economy)
        {
            economy = default;
            if (!IsEntityManagerAvailable())
                return false;
            if (_controlledFactionResourceEntity == Entity.Null ||
                !_entityManager.Exists(_controlledFactionResourceEntity) ||
                !_entityManager.HasComponent<FactionEconomy>(_controlledFactionResourceEntity))
            {
                EnsureControlledFactionResources();
            }

            if (_controlledFactionResourceEntity == Entity.Null ||
                !_entityManager.Exists(_controlledFactionResourceEntity) ||
                !_entityManager.HasComponent<FactionEconomy>(_controlledFactionResourceEntity))
                return false;

            economy = _entityManager.GetComponentData<FactionEconomy>(_controlledFactionResourceEntity);
            return FactionIdentity.IsPlayerControlled(economy.FactionId);
        }

        private bool TryGetControlledFactionResources(
            out FactionEconomy economy,
            out FactionTacticalMaterialsComponent materials)
        {
            materials = default;
            if (!TryGetControlledFactionEconomy(out economy) ||
                !_entityManager.HasComponent<FactionTacticalMaterialsComponent>(_controlledFactionResourceEntity))
                return false;

            materials = _entityManager.GetComponentData<FactionTacticalMaterialsComponent>(
                _controlledFactionResourceEntity);
            return materials.FactionId == economy.FactionId;
        }

        private bool IsEntityManagerAvailable()
        {
            return _isConfigured && _entityManager.WorldUnmanaged.IsCreated;
        }
    }
}
