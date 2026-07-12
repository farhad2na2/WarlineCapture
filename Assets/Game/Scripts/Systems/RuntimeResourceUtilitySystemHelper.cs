using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RuntimeResourceUtilitySystemHelper
    {
        private EntityManager _entityManager;
        private Entity _playerEconomyEntity;
        private int _pendingInitialDollars;
        private bool _isConfigured;

        public int CurrentDollars => TryGetPlayerEconomy(out FactionEconomy economy) ? economy.Money : 0;

        public void SetInitialDollars(int dollars)
        {
            _pendingInitialDollars = Mathf.Max(0, dollars);
            if (!TryGetPlayerEconomy(out FactionEconomy economy))
                return;

            economy.Money = _pendingInitialDollars;
            _entityManager.SetComponentData(_playerEconomyEntity, economy);
        }

        public void Configure(EntityManager entityManager)
        {
            _entityManager = entityManager;
            _playerEconomyEntity = Entity.Null;
            _isConfigured = true;
            EnsurePlayerEconomy();
        }

        public void AddDollars(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount == 0 || !TryGetPlayerEconomy(out FactionEconomy economy))
                return;

            economy.Money = economy.Money >= int.MaxValue - amount
                ? int.MaxValue
                : economy.Money + amount;
            _entityManager.SetComponentData(_playerEconomyEntity, economy);
        }

        public bool TrySpendDollars(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (amount <= 0)
                return true;
            if (!TryGetPlayerEconomy(out FactionEconomy economy) || economy.Money < amount)
                return false;

            economy.Money -= amount;
            _entityManager.SetComponentData(_playerEconomyEntity, economy);
            return true;
        }

        public CitizenResourceCompositionSystemHelper.Context CreateCitizenResourceContext()
        {
            return new CitizenResourceCompositionSystemHelper.Context(
                () => CurrentDollars,
                SetDollars);
        }

        private void SetDollars(int value)
        {
            if (!TryGetPlayerEconomy(out FactionEconomy economy))
                return;

            economy.Money = Mathf.Max(0, value);
            _entityManager.SetComponentData(_playerEconomyEntity, economy);
        }

        private void EnsurePlayerEconomy()
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
                _playerEconomyEntity = resolvedEntity;
                resolvedEconomy.Money = _pendingInitialDollars;
                _entityManager.SetComponentData(resolvedEntity, resolvedEconomy);
                EnsureCompanionComponents(resolvedEntity);
                return;
            }

            _playerEconomyEntity = _entityManager.CreateEntity(
                typeof(FactionEconomy),
                typeof(FactionEconomyPolicy),
                typeof(FactionTacticalMaterialsComponent));
            _entityManager.SetComponentData(_playerEconomyEntity, new FactionEconomy
            {
                FactionId = FactionIdentity.PlayerFactionId,
                Money = _pendingInitialDollars
            });
            _entityManager.SetComponentData(_playerEconomyEntity, new FactionEconomyPolicy
            {
                Enabled = 0,
                IncomeMultiplier = 1f
            });
            _entityManager.SetComponentData(_playerEconomyEntity, new FactionTacticalMaterialsComponent
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

        private bool TryGetPlayerEconomy(out FactionEconomy economy)
        {
            economy = default;
            if (!IsEntityManagerAvailable())
                return false;
            if (_playerEconomyEntity == Entity.Null ||
                !_entityManager.Exists(_playerEconomyEntity) ||
                !_entityManager.HasComponent<FactionEconomy>(_playerEconomyEntity))
            {
                EnsurePlayerEconomy();
            }

            if (_playerEconomyEntity == Entity.Null ||
                !_entityManager.Exists(_playerEconomyEntity) ||
                !_entityManager.HasComponent<FactionEconomy>(_playerEconomyEntity))
                return false;

            economy = _entityManager.GetComponentData<FactionEconomy>(_playerEconomyEntity);
            return FactionIdentity.IsPlayerControlled(economy.FactionId);
        }

        private bool IsEntityManagerAvailable()
        {
            return _isConfigured && _entityManager.WorldUnmanaged.IsCreated;
        }
    }
}
