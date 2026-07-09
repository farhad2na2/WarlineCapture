using System;
using System.Collections.Generic;
using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Resource Exchange/Recipe Config Set")]
    public sealed class ResourceExchangeRecipeConfigSet : ScriptableObject
    {
        [SerializeField] private List<ResourceExchangeRecipeConfigEntry> recipes = new();
        [SerializeField] private List<ResourceExchangeScenarioGateConfigEntry> scenarioGates = new();

        public IReadOnlyList<ResourceExchangeRecipeConfigEntry> Recipes => recipes;
        public IReadOnlyList<ResourceExchangeScenarioGateConfigEntry> ScenarioGates => scenarioGates;
    }

    [Serializable]
    public sealed class ResourceExchangeRecipeConfigEntry
    {
        [SerializeField] private string recipeId = "exchange.export_oil_credits.standard";
        [SerializeField] private string displayName = "Export Oil";
        [SerializeField] private ResourceExchangeRouteType routeType = ResourceExchangeRouteType.Export;
        [SerializeField] private ResourceExchangeResourceKind inputResource = ResourceExchangeResourceKind.Oil;
        [SerializeField] private ResourceExchangeResourceKind outputResource = ResourceExchangeResourceKind.Credits;
        [SerializeField, Min(1)] private int inputAmountMin = 100;
        [SerializeField, Min(1)] private int inputAmountMax = 1000;
        [SerializeField, Min(1)] private int inputStep = 100;
        [SerializeField, Min(0.01f)] private float outputPerInput = 0.55f;
        [SerializeField, Range(0f, 0.95f)] private float feePercent = 0.15f;
        [SerializeField, Min(0f)] private float durationSecondsBase = 30f;
        [SerializeField, Min(0f)] private float durationSecondsPerStep = 2f;
        [SerializeField, Min(0)] private int rushTicketSecondsPerTicket = 30;
        [SerializeField, Min(0)] private int maxRushTickets = 3;
        [SerializeField] private bool requiresStorage = true;
        [SerializeField] private bool requiresTransportPlane = true;
        [SerializeField] private bool requiresTruckPresentation = true;
        [SerializeField] private string missionTag;
        [SerializeField] private ResourceExchangeReason disabledReason = ResourceExchangeReason.None;
        [SerializeField] private int sortOrder;

        public ResourceExchangeRecipeConfigEntry()
        {
        }

        public ResourceExchangeRecipeConfigEntry(
            string recipeId,
            ResourceExchangeRouteType routeType,
            ResourceExchangeResourceKind inputResource,
            ResourceExchangeResourceKind outputResource,
            int inputAmountMin = 100,
            int inputAmountMax = 1000,
            int inputStep = 100,
            float outputPerInput = 0.55f,
            float feePercent = 0.15f,
            float durationSecondsBase = 30f,
            float durationSecondsPerStep = 2f,
            int rushTicketSecondsPerTicket = 30,
            int maxRushTickets = 3,
            bool requiresStorage = true,
            bool requiresTransportPlane = true,
            bool requiresTruckPresentation = true,
            string displayName = "",
            string missionTag = "",
            ResourceExchangeReason disabledReason = ResourceExchangeReason.None,
            int sortOrder = 0)
        {
            this.recipeId = recipeId;
            this.routeType = routeType;
            this.inputResource = inputResource;
            this.outputResource = outputResource;
            this.inputAmountMin = inputAmountMin;
            this.inputAmountMax = inputAmountMax;
            this.inputStep = inputStep;
            this.outputPerInput = outputPerInput;
            this.feePercent = feePercent;
            this.durationSecondsBase = durationSecondsBase;
            this.durationSecondsPerStep = durationSecondsPerStep;
            this.rushTicketSecondsPerTicket = rushTicketSecondsPerTicket;
            this.maxRushTickets = maxRushTickets;
            this.requiresStorage = requiresStorage;
            this.requiresTransportPlane = requiresTransportPlane;
            this.requiresTruckPresentation = requiresTruckPresentation;
            this.displayName = string.IsNullOrWhiteSpace(displayName) ? recipeId : displayName;
            this.missionTag = missionTag;
            this.disabledReason = disabledReason;
            this.sortOrder = sortOrder;
        }

        public string RecipeId => recipeId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public ResourceExchangeRouteType RouteType => routeType;
        public ResourceExchangeResourceKind InputResource => inputResource;
        public ResourceExchangeResourceKind OutputResource => outputResource;
        public int InputAmountMin => Mathf.Max(0, inputAmountMin);
        public int InputAmountMax => Mathf.Max(0, inputAmountMax);
        public int InputStep => Mathf.Max(0, inputStep);
        public float OutputPerInput => Mathf.Max(0f, outputPerInput);
        public float FeePercent => Mathf.Clamp(feePercent, 0f, 0.95f);
        public float DurationSecondsBase => Mathf.Max(0f, durationSecondsBase);
        public float DurationSecondsPerStep => Mathf.Max(0f, durationSecondsPerStep);
        public int RushTicketSecondsPerTicket => Mathf.Max(0, rushTicketSecondsPerTicket);
        public int MaxRushTickets => Mathf.Max(0, maxRushTickets);
        public bool RequiresStorage => requiresStorage;
        public bool RequiresTransportPlane => requiresTransportPlane;
        public bool RequiresTruckPresentation => requiresTruckPresentation;
        public string MissionTag => missionTag ?? string.Empty;
        public ResourceExchangeReason DisabledReason => disabledReason;
        public int SortOrder => sortOrder;
    }

    [Serializable]
    public sealed class ResourceExchangeScenarioGateConfigEntry
    {
        [SerializeField] private string scenarioTag;
        [SerializeField] private bool exchangeEnabled;
        [SerializeField, Min(0)] private int maxQueueItems = 3;
        [SerializeField] private bool allowRush = true;
        [SerializeField] private bool allowWorldPresentation = true;
        [SerializeField] private ResourceExchangeReason disabledReason = ResourceExchangeReason.ExchangeUnavailable;

        public ResourceExchangeScenarioGateConfigEntry()
        {
        }

        public ResourceExchangeScenarioGateConfigEntry(
            string scenarioTag,
            bool exchangeEnabled,
            int maxQueueItems = 3,
            bool allowRush = true,
            bool allowWorldPresentation = true,
            ResourceExchangeReason disabledReason = ResourceExchangeReason.ExchangeUnavailable)
        {
            this.scenarioTag = scenarioTag;
            this.exchangeEnabled = exchangeEnabled;
            this.maxQueueItems = maxQueueItems;
            this.allowRush = allowRush;
            this.allowWorldPresentation = allowWorldPresentation;
            this.disabledReason = disabledReason;
        }

        public string ScenarioTag => scenarioTag ?? string.Empty;
        public bool ExchangeEnabled => exchangeEnabled;
        public int MaxQueueItems => Mathf.Max(0, maxQueueItems);
        public bool AllowRush => allowRush;
        public bool AllowWorldPresentation => allowWorldPresentation;
        public ResourceExchangeReason DisabledReason => disabledReason;
    }

    public static class ResourceExchangeRecipeConfigValidator
    {
        public const int MaximumInputAmountPerExchange = 100000;
        public const float MaximumOutputPerInput = 5f;
        public const float MinimumBaseDurationSeconds = 1f;
        public const int MaximumRushTicketsPerJob = 10;
        public const float MaximumRoundTripResourceRetention = 0.85f;

        public static ResourceExchangeReason ValidateRecipeSet(IReadOnlyList<ResourceExchangeRecipeConfigEntry> recipes)
        {
            if (recipes == null || recipes.Count == 0)
                return ResourceExchangeReason.InvalidRecipe;

            HashSet<string> recipeIds = new(StringComparer.Ordinal);
            for (int i = 0; i < recipes.Count; i++)
            {
                ResourceExchangeRecipeConfigEntry recipe = recipes[i];
                ResourceExchangeReason reason = ValidateRecipe(recipe);
                if (reason != ResourceExchangeReason.None)
                    return reason;

                if (!recipeIds.Add(recipe.RecipeId))
                    return ResourceExchangeReason.DuplicateRecipeId;
            }

            ResourceExchangeReason farmingRiskReason = ValidateRoundTripFarmingRisk(recipes);
            if (farmingRiskReason != ResourceExchangeReason.None)
                return farmingRiskReason;

            return ResourceExchangeReason.None;
        }

        public static ResourceExchangeReason ValidateRecipe(ResourceExchangeRecipeConfigEntry recipe)
        {
            if (recipe == null)
                return ResourceExchangeReason.InvalidRecipe;

            return ValidateRecipe(
                recipe.RecipeId,
                recipe.RouteType,
                recipe.InputResource,
                recipe.OutputResource,
                recipe.InputAmountMin,
                recipe.InputAmountMax,
                recipe.InputStep,
                recipe.OutputPerInput,
                recipe.FeePercent,
                recipe.DurationSecondsBase,
                recipe.DurationSecondsPerStep,
                recipe.RushTicketSecondsPerTicket,
                recipe.MaxRushTickets);
        }

        public static ResourceExchangeReason ValidateRecipe(
            string recipeId,
            ResourceExchangeRouteType routeType,
            ResourceExchangeResourceKind inputResource,
            ResourceExchangeResourceKind outputResource,
            int inputAmountMin,
            int inputAmountMax,
            int inputStep,
            float outputPerInput,
            float feePercent,
            float durationSecondsBase,
            float durationSecondsPerStep,
            int rushTicketSecondsPerTicket,
            int maxRushTickets)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return ResourceExchangeReason.MissingRecipeId;

            if (!IsValidRoute(routeType))
                return ResourceExchangeReason.InvalidRecipe;

            if (!IsRecipeResource(inputResource) || !IsRecipeResource(outputResource))
                return ResourceExchangeReason.InvalidResource;

            if (!IsAllowedRoute(routeType, inputResource, outputResource))
                return ResourceExchangeReason.InvalidResource;

            if (inputAmountMin <= 0 || inputAmountMax < inputAmountMin)
                return ResourceExchangeReason.InvalidRecipe;

            if (inputAmountMax > MaximumInputAmountPerExchange)
                return ResourceExchangeReason.InvalidRecipe;

            if (inputStep <= 0 || ((inputAmountMax - inputAmountMin) % inputStep) != 0)
                return ResourceExchangeReason.InputStepInvalid;

            if (outputPerInput <= 0f || float.IsNaN(outputPerInput) || float.IsInfinity(outputPerInput))
                return ResourceExchangeReason.InvalidRate;

            if (outputPerInput > MaximumOutputPerInput)
                return ResourceExchangeReason.InvalidRate;

            if (feePercent < 0f || feePercent >= 1f || float.IsNaN(feePercent) || float.IsInfinity(feePercent))
                return ResourceExchangeReason.InvalidRate;

            if (durationSecondsBase < 0f || durationSecondsPerStep < 0f ||
                float.IsNaN(durationSecondsBase) || float.IsNaN(durationSecondsPerStep) ||
                float.IsInfinity(durationSecondsBase) || float.IsInfinity(durationSecondsPerStep))
            {
                return ResourceExchangeReason.InvalidDuration;
            }

            if (durationSecondsBase < MinimumBaseDurationSeconds)
                return ResourceExchangeReason.InvalidDuration;

            if (rushTicketSecondsPerTicket < 0 || maxRushTickets < 0)
                return ResourceExchangeReason.InvalidRushRule;

            if (maxRushTickets > MaximumRushTicketsPerJob)
                return ResourceExchangeReason.InvalidRushRule;

            if (maxRushTickets > 0 && rushTicketSecondsPerTicket <= 0)
                return ResourceExchangeReason.InvalidRushRule;

            return ResourceExchangeReason.None;
        }

        public static ResourceExchangeReason ValidateRecipeAndScenarioGateSet(
            IReadOnlyList<ResourceExchangeRecipeConfigEntry> recipes,
            IReadOnlyList<ResourceExchangeScenarioGateConfigEntry> scenarioGates)
        {
            ResourceExchangeReason recipeReason = ValidateRecipeSet(recipes);
            if (recipeReason != ResourceExchangeReason.None)
                return recipeReason;

            return ValidateScenarioGateSet(scenarioGates, recipes);
        }

        public static ResourceExchangeReason ValidateScenarioGateSet(
            IReadOnlyList<ResourceExchangeScenarioGateConfigEntry> scenarioGates,
            IReadOnlyList<ResourceExchangeRecipeConfigEntry> recipes)
        {
            if (scenarioGates == null || scenarioGates.Count == 0)
                return ResourceExchangeReason.InvalidScenarioGate;

            HashSet<string> scenarioTags = new(StringComparer.Ordinal);
            for (int i = 0; i < scenarioGates.Count; i++)
            {
                ResourceExchangeScenarioGateConfigEntry gate = scenarioGates[i];
                if (gate == null)
                    return ResourceExchangeReason.InvalidScenarioGate;

                string scenarioTag = gate.ScenarioTag.Trim();
                if (!IsValidScenarioTag(scenarioTag) || !scenarioTags.Add(scenarioTag))
                    return ResourceExchangeReason.InvalidScenarioGate;

                if (gate.ExchangeEnabled && gate.MaxQueueItems <= 0)
                    return ResourceExchangeReason.InvalidScenarioGate;

                if (!gate.ExchangeEnabled && gate.DisabledReason == ResourceExchangeReason.None)
                    return ResourceExchangeReason.InvalidScenarioGate;
            }

            if (recipes == null || recipes.Count == 0)
                return ResourceExchangeReason.InvalidRecipe;

            for (int i = 0; i < recipes.Count; i++)
            {
                ResourceExchangeRecipeConfigEntry recipe = recipes[i];
                if (recipe == null)
                    return ResourceExchangeReason.InvalidRecipe;

                string missionTag = recipe.MissionTag.Trim();
                if (string.IsNullOrWhiteSpace(missionTag) || !scenarioTags.Contains(missionTag))
                    return ResourceExchangeReason.InvalidScenarioGate;
            }

            return ResourceExchangeReason.None;
        }

        public static bool IsValidScenarioTag(string scenarioTag)
        {
            if (string.IsNullOrWhiteSpace(scenarioTag))
                return false;

            return scenarioTag.StartsWith("chapter.", StringComparison.Ordinal) ||
                   scenarioTag.StartsWith("mission.", StringComparison.Ordinal) ||
                   scenarioTag.StartsWith("campaign.", StringComparison.Ordinal) ||
                   scenarioTag.StartsWith("skirmish.", StringComparison.Ordinal) ||
                   scenarioTag.StartsWith("custom.skirmish.", StringComparison.Ordinal) ||
                   scenarioTag.StartsWith("operation.", StringComparison.Ordinal);
        }

        public static bool IsValidResourceKind(ResourceExchangeResourceKind resourceKind)
        {
            return resourceKind == ResourceExchangeResourceKind.Credits ||
                   resourceKind == ResourceExchangeResourceKind.Materials ||
                   resourceKind == ResourceExchangeResourceKind.Oil ||
                   resourceKind == ResourceExchangeResourceKind.Fuel ||
                   resourceKind == ResourceExchangeResourceKind.RushTickets;
        }

        private static bool IsRecipeResource(ResourceExchangeResourceKind resourceKind)
        {
            return resourceKind == ResourceExchangeResourceKind.Credits ||
                   resourceKind == ResourceExchangeResourceKind.Materials ||
                   resourceKind == ResourceExchangeResourceKind.Oil ||
                   resourceKind == ResourceExchangeResourceKind.Fuel;
        }

        private static bool IsValidRoute(ResourceExchangeRouteType routeType)
        {
            return routeType == ResourceExchangeRouteType.Export ||
                   routeType == ResourceExchangeRouteType.Import;
        }

        private static bool IsAllowedRoute(
            ResourceExchangeRouteType routeType,
            ResourceExchangeResourceKind inputResource,
            ResourceExchangeResourceKind outputResource)
        {
            if (routeType == ResourceExchangeRouteType.Export)
            {
                return outputResource == ResourceExchangeResourceKind.Credits &&
                       (inputResource == ResourceExchangeResourceKind.Oil ||
                        inputResource == ResourceExchangeResourceKind.Materials ||
                        inputResource == ResourceExchangeResourceKind.Fuel);
            }

            return inputResource == ResourceExchangeResourceKind.Credits &&
                   (outputResource == ResourceExchangeResourceKind.Materials ||
                    outputResource == ResourceExchangeResourceKind.Fuel);
        }

        private static ResourceExchangeReason ValidateRoundTripFarmingRisk(
            IReadOnlyList<ResourceExchangeRecipeConfigEntry> recipes)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                ResourceExchangeRecipeConfigEntry exportRecipe = recipes[i];
                if (exportRecipe.RouteType != ResourceExchangeRouteType.Export ||
                    exportRecipe.OutputResource != ResourceExchangeResourceKind.Credits)
                {
                    continue;
                }

                for (int j = 0; j < recipes.Count; j++)
                {
                    ResourceExchangeRecipeConfigEntry importRecipe = recipes[j];
                    if (importRecipe.RouteType != ResourceExchangeRouteType.Import ||
                        importRecipe.InputResource != ResourceExchangeResourceKind.Credits ||
                        importRecipe.OutputResource != exportRecipe.InputResource)
                    {
                        continue;
                    }

                    float roundTripRetention =
                        EffectiveOutputPerInput(exportRecipe) *
                        EffectiveOutputPerInput(importRecipe);

                    if (roundTripRetention > MaximumRoundTripResourceRetention)
                        return ResourceExchangeReason.InvalidRate;
                }
            }

            return ResourceExchangeReason.None;
        }

        private static float EffectiveOutputPerInput(ResourceExchangeRecipeConfigEntry recipe)
        {
            return recipe.OutputPerInput * (1f - recipe.FeePercent);
        }
    }
}
