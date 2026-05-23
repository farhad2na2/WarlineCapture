using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeOwnershipSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public readonly struct Context
    {
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingVisualSystem BuildingVisualSystem;
        public readonly FactionVisualSettings FactionVisualSettings;
        public readonly MaterialPropertyBlock MarkerPropertyBlock;

        public Context(
            TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingVisualSystem buildingVisualSystem,
            FactionVisualSettings factionVisualSettings,
            MaterialPropertyBlock markerPropertyBlock)
        {
            TryGetEntityManager = tryGetEntityManager;
            BuildingVisualSystem = buildingVisualSystem;
            FactionVisualSettings = factionVisualSettings;
            MarkerPropertyBlock = markerPropertyBlock;
        }
    }

    public void SetRuntimeBuildingOwnerFaction(Context context, RuntimeBuildingData building, byte? ownerFactionId)
    {
        if (building == null)
            return;

        building.HasOwnerFaction = ownerFactionId.HasValue;
        building.OwnerFactionId = ownerFactionId.GetValueOrDefault();
        UpdateRuntimeGateFriendlyPassFaction(context, building, ownerFactionId);
        UpdateRuntimeCombatFaction(context, building);
        ApplyRuntimeBuildingMarkerColor(context, building);
    }

    private static void UpdateRuntimeCombatFaction(Context context, RuntimeBuildingData building)
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
    }

    private static void UpdateRuntimeGateFriendlyPassFaction(Context context, RuntimeBuildingData building, byte? ownerFactionId)
    {
        if (building?.Definition == null ||
            building.BlockerEntity == Entity.Null ||
            !BuildingBarrierSystem.IsWallGateDefinition(building.Definition) ||
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

    private static void ApplyRuntimeBuildingMarkerColor(Context context, RuntimeBuildingData building)
    {
        if (context.BuildingVisualSystem == null)
            return;

        Color factionColor = ResolveFactionColor(context.FactionVisualSettings, building.OwnerFactionId);
        context.BuildingVisualSystem.ApplyMarkerColor(building.FactionMarkerRenderers, factionColor, context.MarkerPropertyBlock);
    }

    private static Color ResolveFactionColor(FactionVisualSettings factionVisualSettings, byte ownerFactionId)
    {
        if (factionVisualSettings != null)
            return factionVisualSettings.GetColor(ownerFactionId);

        return ownerFactionId == 0
            ? new Color(0.12f, 0.72f, 1f, 1f)
            : new Color(0.92f, 0.2f, 0.16f, 1f);
    }
}
