using Game.Components;
using Game.Configs;
using Game.Tactical.Contracts;
using UnityEngine;

namespace Game.Runtime
{
    using CampRequestFailure = BuildingUiCommandSystemHelper.CampRequestFailure;

    internal static class BuildingCampItemCommandPolicySystemHelper
    {
        public static CampRequestFailure MapConstructionResourceFailure(
            FactionConstructionResourceMutationResult result)
        {
            return result switch
            {
                FactionConstructionResourceMutationResult.Applied => CampRequestFailure.None,
                FactionConstructionResourceMutationResult.InsufficientCredits => CampRequestFailure.InsufficientCredits,
                FactionConstructionResourceMutationResult.InsufficientMaterials => CampRequestFailure.InsufficientMaterials,
                FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials =>
                    CampRequestFailure.InsufficientCreditsAndMaterials,
                _ => CampRequestFailure.InvalidSelection
            };
        }

        public static string ResolveRequestId(GameObject prefab)
        {
            return prefab != null
                ? BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(prefab.name)
                : string.Empty;
        }

        public static int ResolveResultPrice(
            BuildingProductionRequestSystemHelper.Context context,
            in BuildingUiCampItemCommandRequestElement request)
        {
            if (BuildingProductionRequestSystemHelper.TryResolveConfiguredBuildingPrefab(
                    context,
                    request.ItemId.ToString(),
                    out GameObject prefab) &&
                context.ConfiguredDefinitionsByPrefab != null &&
                context.ConfiguredDefinitionsByPrefab.TryGetValue(prefab, out BuildingDefinition definition))
            {
                return Mathf.Max(0, definition?.MaterialsCost ?? 0);
            }

            return Mathf.Max(0, request.Price);
        }

        public static byte ToResultCode(CampRequestFailure failure, bool isConfiguredBuilding)
        {
            return failure switch
            {
                CampRequestFailure.None => isConfiguredBuilding
                    ? BuildingUiCampItemCommandResultElement.PlacementStarted
                    : BuildingUiCampItemCommandResultElement.ProductionQueued,
                CampRequestFailure.NotEnoughMoney => BuildingUiCampItemCommandResultElement.NotEnoughMoney,
                CampRequestFailure.MissingProducerBuilding =>
                    BuildingUiCampItemCommandResultElement.MissingProducerBuilding,
                CampRequestFailure.ProductionQueueFull => BuildingUiCampItemCommandResultElement.ProductionQueueFull,
                CampRequestFailure.GlobalProductionQueueFull =>
                    BuildingUiCampItemCommandResultElement.GlobalProductionQueueFull,
                _ => BuildingUiCampItemCommandResultElement.InvalidSelection
            };
        }

        public static CampRequestFailure ToRequestFailure(byte resultCode)
        {
            return resultCode switch
            {
                BuildingUiCampItemCommandResultElement.PlacementStarted => CampRequestFailure.None,
                BuildingUiCampItemCommandResultElement.ProductionQueued => CampRequestFailure.None,
                BuildingUiCampItemCommandResultElement.NotEnoughMoney => CampRequestFailure.NotEnoughMoney,
                BuildingUiCampItemCommandResultElement.MissingProducerBuilding =>
                    CampRequestFailure.MissingProducerBuilding,
                BuildingUiCampItemCommandResultElement.ProductionQueueFull => CampRequestFailure.ProductionQueueFull,
                BuildingUiCampItemCommandResultElement.GlobalProductionQueueFull =>
                    CampRequestFailure.GlobalProductionQueueFull,
                _ => CampRequestFailure.InvalidSelection
            };
        }

        public static TacticalCommandReasonCode ToReasonCode(byte resultCode)
        {
            return resultCode switch
            {
                BuildingUiCampItemCommandResultElement.PlacementStarted => TacticalCommandReasonCode.None,
                BuildingUiCampItemCommandResultElement.ProductionQueued => TacticalCommandReasonCode.None,
                BuildingUiCampItemCommandResultElement.NotEnoughMoney => TacticalCommandReasonCode.InsufficientResources,
                BuildingUiCampItemCommandResultElement.MissingProducerBuilding => TacticalCommandReasonCode.BuildUnavailable,
                BuildingUiCampItemCommandResultElement.ProductionQueueFull => TacticalCommandReasonCode.CommandUnavailable,
                BuildingUiCampItemCommandResultElement.GlobalProductionQueueFull =>
                    TacticalCommandReasonCode.CommandUnavailable,
                _ => TacticalCommandReasonCode.CommandUnavailable
            };
        }
    }
}
