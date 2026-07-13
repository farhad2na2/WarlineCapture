using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingRuntimeOwnershipCompositionSystemHelper
    {
        public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

        public readonly struct Context
        {
            public readonly TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly FactionVisualSettings FactionVisualSettings;
            public readonly MaterialPropertyBlock MarkerPropertyBlock;
            public readonly BuildingFactionVisualSystem BuildingFactionVisualSystem;
            public readonly float FactionTintStrength;

            public Context(
                TryGetEntityManagerDelegate tryGetEntityManager,
                FactionVisualSettings factionVisualSettings,
                MaterialPropertyBlock markerPropertyBlock,
                BuildingFactionVisualSystem buildingFactionVisualSystem,
                float factionTintStrength)
            {
                TryGetEntityManager = tryGetEntityManager;
                FactionVisualSettings = factionVisualSettings;
                MarkerPropertyBlock = markerPropertyBlock;
                BuildingFactionVisualSystem = buildingFactionVisualSystem;
                FactionTintStrength = Mathf.Clamp01(factionTintStrength);
            }
        }

        public void SetRuntimeBuildingOwnerFaction(Context context, RuntimeBuildingEntity building, byte? ownerFactionId)
        {
            if (building == null)
                return;

            building.HasOwnerFaction = ownerFactionId.HasValue;
            building.OwnerFactionId = ownerFactionId.GetValueOrDefault();
            UpdateRuntimeGateFriendlyPassFaction(context, building, ownerFactionId);
            UpdateRuntimeCombatFaction(context, building);
            ApplyRuntimeBuildingFactionVisual(context, building);
        }

        private static void UpdateRuntimeCombatFaction(Context context, RuntimeBuildingEntity building)
        {
            if (building.CombatEntity == Entity.Null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em) ||
                !em.Exists(building.CombatEntity) ||
                !em.HasComponent<Faction>(building.CombatEntity))
            {
                return;
            }

            em.SetComponentData(building.CombatEntity, new Faction { Id = building.OwnerFactionId });
            if (em.HasComponent<BuildingResourceStorageComponent>(building.CombatEntity))
            {
                BuildingResourceStorageComponent storage =
                    em.GetComponentData<BuildingResourceStorageComponent>(building.CombatEntity);
                if (storage.OwnerFactionId != building.OwnerFactionId)
                {
                    storage.OwnerFactionId = building.OwnerFactionId;
                    storage.Version = storage.Version == uint.MaxValue ? 1u : storage.Version + 1u;
                    em.SetComponentData(building.CombatEntity, storage);
                }
            }

            if (em.HasComponent<MaterialFabricationComponent>(building.CombatEntity))
            {
                MaterialFabricationComponent fabrication =
                    em.GetComponentData<MaterialFabricationComponent>(building.CombatEntity);
                if (fabrication.OwnerFactionId != building.OwnerFactionId)
                {
                    fabrication.OwnerFactionId = building.OwnerFactionId;
                    fabrication.Version = fabrication.Version == uint.MaxValue ? 1u : fabrication.Version + 1u;
                    em.SetComponentData(building.CombatEntity, fabrication);
                }
            }

            if (em.HasComponent<RuntimeBuildingCombatInfo>(building.CombatEntity))
            {
                RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(building.CombatEntity);
                info.RuntimeBuildingId = building.Id;
                info.OwnerFactionId = building.OwnerFactionId;
                em.SetComponentData(building.CombatEntity, info);
            }
        }

        private static void UpdateRuntimeGateFriendlyPassFaction(Context context, RuntimeBuildingEntity building, byte? ownerFactionId)
        {
            if (building?.Definition == null ||
                building.BlockerEntity == Entity.Null ||
                !BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(building.Definition) ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em) ||
                !em.Exists(building.BlockerEntity))
            {
                return;
            }

            if (!ownerFactionId.HasValue)
            {
                if (em.HasComponent<FriendlyPassGridBlocker>(building.BlockerEntity))
                    em.RemoveComponent<FriendlyPassGridBlocker>(building.BlockerEntity);
                return;
            }

            var pass = new FriendlyPassGridBlocker { AllowedFactionId = ownerFactionId.Value };
            if (em.HasComponent<FriendlyPassGridBlocker>(building.BlockerEntity))
                em.SetComponentData(building.BlockerEntity, pass);
            else
                em.AddComponentData(building.BlockerEntity, pass);
        }

        private static void ApplyRuntimeBuildingFactionVisual(Context context, RuntimeBuildingEntity building)
        {
            if (context.BuildingFactionVisualSystem == null)
                return;

            context.BuildingFactionVisualSystem.ApplyOwnerFaction(
                new BuildingFactionVisualSystem.Context(
                    context.FactionVisualSettings,
                    context.MarkerPropertyBlock,
                    context.FactionTintStrength),
                building);
        }
    }
}
