using System;
using Game.Missions.Contracts;
using Game.Tactical.Contracts;

namespace Game.Configs
{
    public static class MissionDefinitionContractValidation
    {
        public const string FirstContactMissionId = "saga.ch01.m01.first_contact";

        public static bool TryValidateCatalog(MissionDefinitionCatalogConfig catalog, out string error)
        {
            if (catalog == null)
            {
                error = "Mission-definition catalog is required.";
                return false;
            }

            ReadOnlySpan<MissionDefinitionCatalogEntryConfig> entries = catalog.Entries;
            if (entries.Length == 0)
            {
                error = "Mission-definition catalog requires at least one entry.";
                return false;
            }

            for (int index = 0; index < entries.Length; index++)
            {
                string missionId = entries[index].MissionId;
                if (!IsValidMissionId(missionId))
                {
                    error = $"Mission catalog entry at index {index} has invalid id '{missionId ?? "<null>"}'.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(missionId, entries[previous].MissionId, StringComparison.Ordinal))
                    {
                        error = $"Duplicate mission catalog id: '{missionId}'.";
                        return false;
                    }
                }
            }

            for (int index = 0; index < entries.Length; index++)
            {
                MissionDefinitionCatalogEntryConfig entry = entries[index];
                if (entry.Definition == null)
                {
                    error = $"Mission catalog entry '{entry.MissionId}' has no definition.";
                    return false;
                }

                if (!string.Equals(entry.MissionId, entry.Definition.MissionId, StringComparison.Ordinal))
                {
                    error = $"Mission catalog entry '{entry.MissionId}' does not match its definition id.";
                    return false;
                }

                if (!TryValidateDefinition(entry.Definition, out error))
                    return false;

                if (entry.Scenario == null || !entry.Scenario.TryValidate(out error))
                {
                    error ??= $"Mission catalog entry '{entry.MissionId}' has no valid canonical scenario.";
                    return false;
                }

                if (!string.Equals(
                        entry.Definition.ScenarioId,
                        entry.Scenario.ScenarioId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        entry.Definition.OperationMapId,
                        entry.Scenario.OperationMapId,
                        StringComparison.Ordinal))
                {
                    error = $"Mission catalog entry '{entry.MissionId}' does not close over its scenario identity.";
                    return false;
                }

            }

            error = null;
            return true;
        }

        public static bool TryValidateDefinition(MissionDefinitionConfig definition, out string error)
        {
            if (definition == null)
            {
                error = "Mission definition is required.";
                return false;
            }

            if (!IsValidMissionId(definition.MissionId))
            {
                error = $"Invalid mission id: '{definition.MissionId ?? "<null>"}'.";
                return false;
            }

            if (definition.SchemaVersion < 1)
            {
                error = $"Mission '{definition.MissionId}' requires a positive schema version.";
                return false;
            }

            if (!IsValidScopedId(definition.DisplayNameKey, "mission", 3, 8) ||
                !IsValidScopedId(definition.DisplaySummaryKey, "mission", 3, 8) ||
                !IsValidScopedId(definition.LocationNameKey, "mission", 3, 8))
            {
                error = $"Mission '{definition.MissionId}' has invalid display localization keys.";
                return false;
            }

            if (!OperationMapIdentityRules.IsValidScenarioId(definition.ScenarioId) ||
                !OperationMapIdentityRules.IsValidOperationMapId(definition.OperationMapId))
            {
                error = $"Mission '{definition.MissionId}' has invalid scenario or operation-map references.";
                return false;
            }

            if (!IsValidScopedId(definition.BriefingSequenceId, "seq", 4, 6) ||
                !IsValidScopedId(definition.CommsSequenceId, "seq", 4, 6) ||
                !IsValidScopedId(definition.DebriefSequenceId, "seq", 4, 6))
            {
                error = $"Mission '{definition.MissionId}' has invalid narrative sequence references.";
                return false;
            }

            if (!TryValidateObjectives(definition, out error) ||
                !TryValidateStars(definition, out error) ||
                !TryValidateRewards(definition, out error) ||
                !TryValidateCommands(definition, out error) ||
                !TryValidateReadiness(definition, out error))
                return false;

            if (string.Equals(definition.MissionId, FirstContactMissionId, StringComparison.Ordinal) &&
                (!definition.ReplayAllowed || !definition.RequireOperationMapReady ||
                 !definition.RequireGridReady || !definition.RequireUnitCatalogReady))
            {
                error = "M01 requires replay plus operation-map, grid, and unit-catalog readiness.";
                return false;
            }

            error = null;
            return true;
        }

        public static bool IsValidMissionId(string value)
        {
            if (!IsValidCharacters(value, 60))
                return false;
            string[] parts = value.Split('.');
            return parts.Length == 4 && parts[0] == "saga" &&
                IsNumbered(parts[1], "ch") && IsNumbered(parts[2], "m") && parts[3].Length >= 3;
        }

        private static bool TryValidateObjectives(MissionDefinitionConfig definition, out string error)
        {
            ReadOnlySpan<MissionObjectiveDefinitionConfig> objectives = definition.Objectives;
            if (objectives.Length == 0)
            {
                error = $"Mission '{definition.MissionId}' requires at least one objective.";
                return false;
            }

            for (int index = 0; index < objectives.Length; index++)
            {
                MissionObjectiveDefinitionConfig objective = objectives[index];
                if (!IsValidObjectiveId(objective.ObjectiveId) ||
                    !IsValidScopedId(objective.DisplayTextKey, "mission", 3, 8) ||
                    !HasValidObjectiveTarget(objective) || objective.RequiredCount < 1)
                {
                    error = $"Mission '{definition.MissionId}' has invalid objective at index {index}.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (objectives[previous].ObjectiveId == objective.ObjectiveId)
                    {
                        error = $"Mission '{definition.MissionId}' has duplicate objective '{objective.ObjectiveId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool HasValidObjectiveTarget(MissionObjectiveDefinitionConfig objective)
        {
            bool hasRole = !string.IsNullOrEmpty(objective.MissionRoleId);
            bool hasConfig = !string.IsNullOrEmpty(objective.TargetConfigId);
            switch (objective.Rule)
            {
                case MissionObjectiveRuleKind.DestroyMissionRole:
                case MissionObjectiveRuleKind.ProtectMissionRole:
                case MissionObjectiveRuleKind.DefendMissionRole:
                    return hasRole && !hasConfig &&
                        IsValidScopedId(objective.MissionRoleId, "role", 2, 7);
                case MissionObjectiveRuleKind.BuildStructure:
                    return !hasRole && hasConfig &&
                        IsValidGameplayConfigId(objective.TargetConfigId, "Building_");
                case MissionObjectiveRuleKind.ProduceUnit:
                    return !hasRole && hasConfig &&
                        IsValidGameplayConfigId(objective.TargetConfigId, "Unit_");
                default:
                    return false;
            }
        }

        private static bool IsValidGameplayConfigId(string value, string requiredPrefix)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 96 ||
                !value.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
                IsPlaceholderToken(value))
                return false;

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!char.IsLetterOrDigit(character) && character != '_')
                    return false;
            }

            return true;
        }

        private static bool TryValidateStars(MissionDefinitionConfig definition, out string error)
        {
            ReadOnlySpan<MissionStarDefinitionConfig> stars = definition.Stars;
            if (stars.Length is < 1 or > 3)
            {
                error = $"Mission '{definition.MissionId}' requires one to three star rules.";
                return false;
            }

            for (int index = 0; index < stars.Length; index++)
            {
                MissionStarDefinitionConfig star = stars[index];
                bool thresholdValid = star.Rule == MissionStarRuleKind.CompleteUnderMilliseconds
                    ? star.Threshold > 0
                    : star.Threshold == 0;
                if (star.StarIndex is < 1 or > 3 || star.Rule == MissionStarRuleKind.None || !thresholdValid ||
                    !IsValidScopedId(star.DisplayTextKey, "mission", 3, 8))
                {
                    error = $"Mission '{definition.MissionId}' has invalid star rule at index {index}.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (stars[previous].StarIndex == star.StarIndex)
                    {
                        error = $"Mission '{definition.MissionId}' has duplicate star index {star.StarIndex}.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateRewards(MissionDefinitionConfig definition, out string error)
        {
            if (!TryValidateRewardSet(definition, definition.FirstClearRewards, "first-clear", true, out error))
                return false;
            return TryValidateRewardSet(
                definition,
                definition.ReplayRewards,
                "replay",
                definition.ReplayAllowed,
                out error);
        }

        private static bool TryValidateRewardSet(
            MissionDefinitionConfig definition,
            ReadOnlySpan<MissionRewardDefinitionConfig> rewards,
            string label,
            bool required,
            out string error)
        {
            if (required && rewards.Length == 0)
            {
                error = $"Mission '{definition.MissionId}' requires an explicit {label} reward.";
                return false;
            }

            for (int index = 0; index < rewards.Length; index++)
            {
                MissionRewardDefinitionConfig reward = rewards[index];
                bool hasKind = reward.Kind != MissionRewardKind.None;
                bool hasConfig = !string.IsNullOrEmpty(reward.RewardConfigId);
                bool validConfig = !hasConfig ||
                    IsValidScopedId(reward.RewardConfigId, "reward", 2, 6);
                if (hasKind == hasConfig || !validConfig || reward.Amount < 1 ||
                    !IsValidScopedId(reward.DisplayTextKey, "mission", 3, 8) ||
                    IsPlaceholderToken(reward.RewardConfigId) || IsPlaceholderToken(reward.DisplayTextKey) ||
                    (definition.MissionId == FirstContactMissionId && IsIntelReward(reward)))
                {
                    error = hasKind == hasConfig
                        ? $"Mission '{definition.MissionId}' has ambiguous settlement identity for {label} reward at index {index}."
                        : $"Mission '{definition.MissionId}' has invalid {label} reward definition at index {index}.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    MissionRewardDefinitionConfig prior = rewards[previous];
                    if ((hasKind && prior.Kind == reward.Kind) ||
                        (hasConfig && prior.RewardConfigId == reward.RewardConfigId))
                    {
                        string identity = hasKind ? reward.Kind.ToString() : reward.RewardConfigId;
                        error = $"Mission '{definition.MissionId}' has duplicate {label} reward '{identity}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool IsIntelReward(MissionRewardDefinitionConfig reward) =>
            reward.Kind == MissionRewardKind.Intel ||
            string.Equals(reward.RewardConfigId, "reward.intel", StringComparison.Ordinal);

        private static bool IsPlaceholderToken(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            string[] parts = value.Split('.');
            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index] is "placeholder" or "todo" or "tbd")
                    return true;
            }
            return false;
        }

        private static bool TryValidateCommands(MissionDefinitionConfig definition, out string error)
        {
            ReadOnlySpan<TacticalCommandMode> commands = definition.CommandPolicy.AllowedCommands;
            if (commands.Length == 0)
            {
                error = $"Mission '{definition.MissionId}' requires at least one allowed command.";
                return false;
            }

            for (int index = 0; index < commands.Length; index++)
            {
                if (commands[index] == TacticalCommandMode.None)
                {
                    error = $"Mission '{definition.MissionId}' contains an invalid command.";
                    return false;
                }
                for (int previous = 0; previous < index; previous++)
                {
                    if (commands[previous] == commands[index])
                    {
                        error = $"Mission '{definition.MissionId}' contains duplicate command '{commands[index]}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateReadiness(MissionDefinitionConfig definition, out string error)
        {
            ReadOnlySpan<string> features = definition.RequiredFeatureIds;
            if (features.Length == 0)
            {
                error = $"Mission '{definition.MissionId}' requires explicit feature-readiness ids.";
                return false;
            }
            for (int index = 0; index < features.Length; index++)
            {
                if (!IsValidScopedId(features[index], "feature", 2, 7))
                {
                    error = $"Mission '{definition.MissionId}' has invalid readiness id at index {index}.";
                    return false;
                }
                for (int previous = 0; previous < index; previous++)
                {
                    if (features[previous] == features[index])
                    {
                        error = $"Mission '{definition.MissionId}' has duplicate readiness id '{features[index]}'.";
                        return false;
                    }
                }
            }
            error = null;
            return true;
        }

        private static bool IsValidScopedId(string value, string prefix, int minimumParts, int maximumParts)
        {
            if (!IsValidCharacters(value, 96))
                return false;
            string[] parts = value.Split('.');
            return parts.Length >= minimumParts && parts.Length <= maximumParts && parts[0] == prefix;
        }

        private static bool IsValidObjectiveId(string value) =>
            IsValidScopedId(value, "objective", 2, 7) || IsValidScopedId(value, "obj", 4, 7);

        private static bool IsValidCharacters(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length > maximumLength || value[0] == '.' || value[^1] == '.')
                return false;
            bool afterDot = false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character == '.')
                {
                    if (afterDot) return false;
                    afterDot = true;
                    continue;
                }
                if (!(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_') ||
                    (afterDot && character == '_'))
                    return false;
                afterDot = false;
            }
            return true;
        }

        private static bool IsNumbered(string value, string prefix) =>
            value.Length == prefix.Length + 2 && value.StartsWith(prefix, StringComparison.Ordinal) &&
            char.IsDigit(value[^2]) && char.IsDigit(value[^1]);
    }
}
