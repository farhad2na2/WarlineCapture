using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class BuildingUiCommandAdapter : IBuildingUiCommand, IBuildingPlacementFootprintQuery
    {
        private readonly BuildingUiCommandSystemHelper boundary;
        private readonly BuildingUiCommandSystemHelper.Context context;

        public BuildingUiCommandAdapter(BuildingUiCommandSystemHelper boundary, BuildingUiCommandSystemHelper.Context context)
        {
            this.boundary = boundary;
            this.context = context;
        }

        public Vector2Int ActivePlacementFootprint => context.GetPlacementFootprint?.Invoke() ?? Vector2Int.zero;
        public int CurrentDollars => boundary != null ? boundary.CurrentDollars(context) : 0;
        public bool HasPendingBuildingPlacement => boundary != null && boundary.HasPendingBuildingPlacement(context);
        public bool CanConfirmBuildingPlacement => boundary != null && boundary.CanConfirmBuildingPlacement(context);
        public string PlacementStatusText => boundary != null ? boundary.PlacementStatusText(context) : string.Empty;
        public int ActivePlacementCost => boundary != null ? boundary.ActivePlacementCost(context) : 0;
        public int ActivePlacementCreditsCost => boundary!=null ? BuildingUiPlacementCostReadModel.ActiveCredits(context) : 0;
        public float ActivePlacementDurationSeconds => boundary != null ? boundary.ActivePlacementDurationSeconds(context) : 0f;
        public int MaxQueuedUnitProductions => boundary != null ? boundary.MaxQueuedUnitProductions(context) : 25;

        public BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int materialsCost, out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return boundary != null
                ? Map(boundary.GetCampRequestFailure(context, prefab, materialsCost, out requiredBuildingDisplayName))
                : BuildingUiCommandFailure.InvalidSelection;
        }

        public BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int materialsCost, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
        {
            requiredBuildingDisplayName = string.Empty;
            return boundary != null
                ? Map(boundary.TryRequestCampItem(context, prefab, materialsCost, out requiredBuildingDisplayName, focusProducerOnSuccess))
                : BuildingUiCommandFailure.InvalidSelection;
        }

        public bool CancelProduction(int buildingId, int pendingProductionIndex) =>
            boundary != null && boundary.CancelProduction(context, buildingId, pendingProductionIndex);
        public bool ConfirmBuildingPlacement() => boundary != null && boundary.ConfirmBuildingPlacement(context);
        public void CancelBuildingPlacement() => boundary?.CancelBuildingPlacement(context);
        public bool RotateBuildingPlacement() => boundary != null && boundary.RotateBuildingPlacement(context);

        private static BuildingUiCommandFailure Map(BuildingUiCommandSystemHelper.CampRequestFailure failure)
        {
            return failure switch
            {
                BuildingUiCommandSystemHelper.CampRequestFailure.None => BuildingUiCommandFailure.None,
                BuildingUiCommandSystemHelper.CampRequestFailure.NotEnoughMoney => BuildingUiCommandFailure.NotEnoughMoney,
                BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding => BuildingUiCommandFailure.MissingProducerBuilding,
                BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection => BuildingUiCommandFailure.InvalidSelection,
                BuildingUiCommandSystemHelper.CampRequestFailure.ProductionQueueFull => BuildingUiCommandFailure.ProductionQueueFull,
                BuildingUiCommandSystemHelper.CampRequestFailure.InfantryLimit => BuildingUiCommandFailure.InfantryLimit,
                BuildingUiCommandSystemHelper.CampRequestFailure.LogisticsLimit => BuildingUiCommandFailure.LogisticsLimit,
                BuildingUiCommandSystemHelper.CampRequestFailure.GlobalProductionQueueFull => BuildingUiCommandFailure.GlobalProductionQueueFull,
                BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCredits => BuildingUiCommandFailure.InsufficientCredits,
                BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientMaterials => BuildingUiCommandFailure.InsufficientMaterials,
                BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCreditsAndMaterials => BuildingUiCommandFailure.InsufficientCreditsAndMaterials,
                _ => BuildingUiCommandFailure.InvalidSelection
            };
        }
    }
}
