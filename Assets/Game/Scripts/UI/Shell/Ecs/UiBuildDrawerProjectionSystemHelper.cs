using System.Globalization;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    internal static class UiBuildDrawerProjectionSystemHelper
    {
        internal static FixedString32Bytes ToFixed32(string value) => new(Trim(value, 28));

        internal static FixedString64Bytes ToFixed64(string value) => new(Trim(value, 60));

        internal static FixedString128Bytes ToFixed128(string value) => new(Trim(value, 120));

        internal static string FormatCost(int cost) =>
            Mathf.Max(0, cost).ToString("N0", CultureInfo.InvariantCulture);

        internal static string FormatMaterialsCost(int materialsCost) =>
            materialsCost > 0 ? FormatCost(materialsCost) : string.Empty;

        internal static string FormatFuelCost(int fuelCost) =>
            fuelCost > 0 ? FormatCost(fuelCost) : string.Empty;

        internal static string FormatDuration(BuildDrawerCatalogItem item)
        {
            if (item.ProductionDurationSeconds <= 0f)
                return "-";

            int seconds = Mathf.CeilToInt(item.ProductionDurationSeconds);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        internal static string FormatRemaining(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        internal static string FormatPercent(float progress01)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        internal static string FormatFootprint(BuildDrawerCatalogItem item)
        {
            return item.Category == BuildDrawerCategory.Buildings
                ? $"{item.FootprintCells.x} x {item.FootprintCells.y}"
                : "-";
        }

        internal static string FormatPlacement(BuildDrawerCatalogItem item)
        {
            return item.Category == BuildDrawerCategory.Buildings
                ? $"{item.FootprintCells.x}x{item.FootprintCells.y}"
                : "-";
        }

        internal static string FormatRequirements(BuildDrawerCatalogItem item)
        {
            return item.Category switch
            {
                BuildDrawerCategory.Buildings => GameText.Get("build.drawer.requirements.buildings", "Valid footprint required."),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.requirements.aircraft", "Requires compatible air production."),
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.requirements.vehicles", "Requires compatible vehicle production."),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.requirements.soldiers", "Requires compatible recruitment building."),
                _ => string.Empty
            };
        }

        internal static string FormatReadyInstruction(BuildDrawerCatalogItem item)
        {
            return item.Category switch
            {
                BuildDrawerCategory.Buildings => GameText.Format("build.drawer.ready.buildings", "PLACE: choose a location for {0}.", item.DisplayName),
                BuildDrawerCategory.Vehicles => GameText.Format("build.drawer.ready.vehicles", "PRODUCE: add {0} to the vehicle queue.", item.DisplayName),
                BuildDrawerCategory.Aircrafts => GameText.Format("build.drawer.ready.aircraft", "PRODUCE: add {0} to the aircraft queue.", item.DisplayName),
                BuildDrawerCategory.Soldiers => GameText.Format("build.drawer.ready.soldiers", "RECRUIT: add {0} to the training queue.", item.DisplayName),
                _ => GameText.Format("build.drawer.ready.default", "Select {0}.", item.DisplayName)
            };
        }

        internal static string FormatInstructionFailureMessage(
            BuildDrawerCatalogItem item,
            BuildingUiCommandFailure failure,
            string requiredBuildingDisplayName,
            IBuildingUiCommand buildingUiCommand)
        {
            if (buildingUiCommand == null)
                return GameText.Get("build.drawer.failure.connecting", "Build drawer is still connecting. Try again in a moment.");

            return failure switch
            {
                BuildingUiCommandFailure.NotEnoughMoney =>
                    GameText.Format("build.drawer.failure.insufficient_credits", "Cannot {0} {1}: insufficient credits.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName),
                BuildingUiCommandFailure.InsufficientCredits =>
                    GameText.Format("build.drawer.failure.insufficient_credits", "Cannot {0} {1}: insufficient credits.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName),
                BuildingUiCommandFailure.InsufficientMaterials =>
                    GameText.Format("build.drawer.failure.insufficient_materials", "Cannot {0} {1}: insufficient materials.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName),
                BuildingUiCommandFailure.InsufficientCreditsAndMaterials =>
                    GameText.Format("build.drawer.failure.insufficient_credits_and_materials", "Cannot {0} {1}: insufficient credits and materials.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName),
                BuildingUiCommandFailure.MissingProducerBuilding when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    GameText.Format("build.drawer.failure.missing_producer_named", "Cannot {0} {1}: requires {2}.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName, requiredBuildingDisplayName),
                BuildingUiCommandFailure.MissingProducerBuilding =>
                    GameText.Format("build.drawer.failure.missing_producer", "Cannot {0} {1}: {2}.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName, FormatMissingProducerFallback(item.Category)),
                BuildingUiCommandFailure.ProductionQueueFull when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    GameText.Format("build.drawer.failure.queue_full_named", "Cannot {0} {1}: all {2} production slots are full.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName, requiredBuildingDisplayName),
                BuildingUiCommandFailure.ProductionQueueFull =>
                    GameText.Format("build.drawer.failure.queue_full", "Cannot {0} {1}: all compatible production slots are full.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName),
                BuildingUiCommandFailure.GlobalProductionQueueFull =>
                    GameText.Format("build.drawer.failure.global_queue_full", "Cannot {0} {1}: production queue limit reached ({2} max).", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName, FormatMaxQueuedUnitProductions(buildingUiCommand)),
                BuildingUiCommandFailure.InvalidSelection => GameText.Get("build.drawer.failure.invalid_selection", "Select a build drawer item first."),
                _ => GameText.Format("build.drawer.failure.unavailable", "Cannot {0} {1}: request unavailable.", FormatActionVerb(item.Category).ToLowerInvariant(), item.DisplayName)
            };
        }

        internal static string FormatEmptyCategoryInstruction(BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Buildings => GameText.Get("build.drawer.empty.buildings", "No requestable buildings are configured."),
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.empty.vehicles", "No requestable vehicles are configured."),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.empty.aircraft", "No requestable aircraft are configured."),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.empty.soldiers", "No requestable soldiers are configured."),
                _ => GameText.Get("build.drawer.empty.default", "No requestable items are configured.")
            };
        }

        internal static string FormatPlacementStatus(string rawStatus)
        {
            if (string.IsNullOrWhiteSpace(rawStatus))
                return GameText.Get("build.drawer.placement.invalid", "invalid placement");

            int separator = rawStatus.IndexOf(':');
            return separator >= 0 && separator + 1 < rawStatus.Length
                ? rawStatus.Substring(separator + 1).Trim()
                : rawStatus;
        }

        private static int FormatMaxQueuedUnitProductions(IBuildingUiCommand buildingUiCommand)
        {
            return Mathf.Max(0, buildingUiCommand != null ? buildingUiCommand.MaxQueuedUnitProductions : 25);
        }

        private static string FormatActionVerb(BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Buildings => GameText.Get("build.drawer.verb.place", "Place"),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.verb.recruit", "Recruit"),
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.verb.produce", "Produce"),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.verb.produce", "Produce"),
                _ => GameText.Get("build.drawer.verb.select", "Select")
            };
        }

        private static string FormatMissingProducerFallback(BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.missing_producer.vehicles", "no compatible vehicle producer is available"),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.missing_producer.aircraft", "no compatible air producer is available"),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.missing_producer.soldiers", "no compatible training building is available"),
                _ => GameText.Get("build.drawer.missing_producer.default", "required producer is missing")
            };
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
