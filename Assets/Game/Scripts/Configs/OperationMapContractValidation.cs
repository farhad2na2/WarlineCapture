using System;

namespace Game.Configs
{
    public readonly struct OperationMapContractEvidence
    {
        public OperationMapContractEvidence(
            string operationMapId,
            int schemaVersion,
            int contentVersion,
            string sourceIdentityHash,
            string contentHash,
            string generatedMetadataHash)
        {
            OperationMapId = operationMapId;
            SchemaVersion = schemaVersion;
            ContentVersion = contentVersion;
            SourceIdentityHash = sourceIdentityHash;
            ContentHash = contentHash;
            GeneratedMetadataHash = generatedMetadataHash;
        }

        public string OperationMapId { get; }
        public int SchemaVersion { get; }
        public int ContentVersion { get; }
        public string SourceIdentityHash { get; }
        public string ContentHash { get; }
        public string GeneratedMetadataHash { get; }
    }

    public static class OperationMapContractValidation
    {
        public static bool TryValidate(
            OperationMapDefinition[] definitions,
            ScenarioSetupConfig[] scenarios,
            OperationMapContractEvidence[] evidence,
            out string error)
        {
            if (definitions == null || definitions.Length == 0)
            {
                error = "At least one operation-map definition is required.";
                return false;
            }

            if (scenarios == null)
            {
                error = "The scenario collection is required; use an empty array when no scenarios are registered.";
                return false;
            }

            if (evidence == null || evidence.Length != definitions.Length)
            {
                error = "Exactly one operation-map evidence record is required per definition.";
                return false;
            }

            for (int index = 0; index < evidence.Length; index++)
            {
                OperationMapContractEvidence record = evidence[index];
                if (!OperationMapIdentityRules.IsValidOperationMapId(record.OperationMapId) ||
                    record.SchemaVersion < 1 ||
                    record.ContentVersion < 1 ||
                    !OperationMapHashRules.IsValidSha256(record.SourceIdentityHash) ||
                    !OperationMapHashRules.IsValidSha256(record.ContentHash) ||
                    !OperationMapHashRules.IsValidSha256(record.GeneratedMetadataHash))
                {
                    error = $"Operation-map evidence at index {index} is invalid.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(record.OperationMapId, evidence[previous].OperationMapId, StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map evidence id: '{record.OperationMapId}'.";
                        return false;
                    }
                }
            }

            for (int index = 0; index < definitions.Length; index++)
            {
                OperationMapDefinition definition = definitions[index];
                if (definition == null)
                {
                    error = $"Operation-map definition at index {index} is missing.";
                    return false;
                }

                if (!definition.TryValidateMetadata(out error))
                {
                    error = $"Operation-map definition '{definition.OperationMapId ?? "<null>"}' is invalid: {error}";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(
                            definition.OperationMapId,
                            definitions[previous].OperationMapId,
                            StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map id: '{definition.OperationMapId}'.";
                        return false;
                    }
                }

                int evidenceIndex = FindEvidenceIndex(evidence, definition.OperationMapId);
                if (evidenceIndex < 0)
                {
                    error = $"Operation-map definition '{definition.OperationMapId}' has no evidence record.";
                    return false;
                }

                OperationMapContractEvidence expected = evidence[evidenceIndex];
                if (definition.SchemaVersion != expected.SchemaVersion ||
                    definition.ContentVersion != expected.ContentVersion ||
                    !string.Equals(definition.SourceIdentityHash, expected.SourceIdentityHash, StringComparison.Ordinal) ||
                    !string.Equals(definition.ContentHash, expected.ContentHash, StringComparison.Ordinal) ||
                    !string.Equals(
                        definition.GeneratedMetadataHash,
                        expected.GeneratedMetadataHash,
                        StringComparison.Ordinal))
                {
                    error = $"Operation-map definition '{definition.OperationMapId}' has stale version or hash evidence.";
                    return false;
                }

                if (!TryValidateSourceBinding(definitions, definition, out error))
                    return false;
            }

            for (int index = 0; index < scenarios.Length; index++)
            {
                ScenarioSetupConfig scenario = scenarios[index];
                if (scenario == null)
                {
                    error = $"Scenario setup at index {index} is missing.";
                    return false;
                }

                if (!scenario.TryValidate(out error))
                {
                    error = $"Scenario setup '{scenario.ScenarioId ?? "<null>"}' is invalid: {error}";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(scenario.ScenarioId, scenarios[previous].ScenarioId, StringComparison.Ordinal))
                    {
                        error = $"Duplicate scenario id: '{scenario.ScenarioId}'.";
                        return false;
                    }
                }

                int operationMapIndex = FindOperationMapIndex(definitions, scenario.OperationMapId);
                if (operationMapIndex < 0)
                {
                    error = $"Scenario '{scenario.ScenarioId}' references unresolved operation-map id " +
                            $"'{scenario.OperationMapId}'.";
                    return false;
                }

                if (!TryValidateRequiredAnchors(definitions[operationMapIndex], scenario, out error))
                    return false;
            }

            error = null;
            return true;
        }

        private static int FindOperationMapIndex(OperationMapDefinition[] definitions, string operationMapId)
        {
            for (int index = 0; index < definitions.Length; index++)
            {
                if (string.Equals(definitions[index].OperationMapId, operationMapId, StringComparison.Ordinal))
                    return index;
            }

            return -1;
        }

        private static bool TryValidateSourceBinding(
            OperationMapDefinition[] definitions,
            OperationMapDefinition logicalDefinition,
            out string error)
        {
            OperationMapSourceBindingConfig binding = logicalDefinition.SourceBinding;
            if (!binding.IsConfigured)
            {
                error = null;
                return true;
            }

            int sourceIndex = FindOperationMapIndex(definitions, binding.SourceOperationMapId);
            if (sourceIndex < 0)
            {
                error = $"Logical operation map '{logicalDefinition.OperationMapId}' references unresolved " +
                        $"physical source '{binding.SourceOperationMapId}'.";
                return false;
            }

            OperationMapDefinition source = definitions[sourceIndex];
            if (source.SourceBinding.IsConfigured ||
                !string.Equals(binding.SourceIdentityHash, source.SourceIdentityHash, StringComparison.Ordinal) ||
                !string.Equals(binding.SourceContentHash, source.ContentHash, StringComparison.Ordinal) ||
                !string.Equals(
                    logicalDefinition.SourceSceneReference.AssetGUID,
                    source.SourceSceneReference.AssetGUID,
                    StringComparison.Ordinal))
            {
                error = $"Logical operation map '{logicalDefinition.OperationMapId}' has stale or mismatched " +
                        $"physical-source identity, content hash, or scene reference.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateRequiredAnchors(
            OperationMapDefinition definition,
            ScenarioSetupConfig scenario,
            out string error)
        {
            ReadOnlySpan<OperationMapAnchorConfig> mapAnchors = definition.Anchors;
            ReadOnlySpan<ScenarioAnchorRequirementConfig> requirements = scenario.RequiredAnchors;
            for (int requirementIndex = 0; requirementIndex < requirements.Length; requirementIndex++)
            {
                ScenarioAnchorRequirementConfig requirement = requirements[requirementIndex];
                bool found = false;
                for (int anchorIndex = 0; anchorIndex < mapAnchors.Length; anchorIndex++)
                {
                    if (!string.Equals(
                            requirement.AnchorId,
                            mapAnchors[anchorIndex].AnchorId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    found = true;
                    if (mapAnchors[anchorIndex].Kind != requirement.Kind)
                    {
                        error = $"Scenario '{scenario.ScenarioId}' requires anchor " +
                                $"'{requirement.AnchorId}' as {requirement.Kind}, but operation map " +
                                $"'{definition.OperationMapId}' declares {mapAnchors[anchorIndex].Kind}.";
                        return false;
                    }

                    break;
                }

                if (!found)
                {
                    error = $"Scenario '{scenario.ScenarioId}' requires missing anchor " +
                            $"'{requirement.AnchorId}' on operation map '{definition.OperationMapId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static int FindEvidenceIndex(OperationMapContractEvidence[] evidence, string operationMapId)
        {
            for (int index = 0; index < evidence.Length; index++)
            {
                if (string.Equals(evidence[index].OperationMapId, operationMapId, StringComparison.Ordinal))
                    return index;
            }

            return -1;
        }
    }
}
