using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeOwnershipSystem
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
