using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Operation Maps/Operation Map Catalog")]
    public sealed class OperationMapCatalogConfig : ScriptableObject
    {
        [SerializeField] private OperationMapDefinition[] definitions =
            Array.Empty<OperationMapDefinition>();
        [SerializeField] private OperationMapCatalogEntryConfig[] entries =
            Array.Empty<OperationMapCatalogEntryConfig>();

        public ReadOnlySpan<OperationMapDefinition> Definitions => definitions;
        public ReadOnlySpan<OperationMapCatalogEntryConfig> Entries => entries;

        public bool TryValidate(out string error)
        {
            if (definitions == null || definitions.Length == 0)
            {
                error = "Operation-map catalog requires at least one definition.";
                return false;
            }

            for (int index = 0; index < definitions.Length; index++)
            {
                OperationMapDefinition definition = definitions[index];
                if (definition == null)
                {
                    error = $"Operation-map catalog definition at index {index} is missing.";
                    return false;
                }

                if (!definition.TryValidateMetadata(out error))
                    return false;

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(
                            definition.OperationMapId,
                            definitions[previous].OperationMapId,
                            StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map catalog id: '{definition.OperationMapId}'.";
                        return false;
                    }
                }
            }

            if (entries == null || entries.Length != definitions.Length)
            {
                error = "Operation-map catalog requires exactly one content-pack entry per definition.";
                return false;
            }

            for (int index = 0; index < entries.Length; index++)
            {
                OperationMapCatalogEntryConfig entry = entries[index];
                if (!entry.TryValidate(out error))
                    return false;

                if (entry.Definition != definitions[index])
                {
                    error = $"Operation-map catalog entry at index {index} must reference the definition at the same index.";
                    return false;
                }

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(
                            entry.ContentPack.ContentPackId,
                            entries[previous].ContentPack.ContentPackId,
                            StringComparison.Ordinal))
                    {
                        error = $"Duplicate operation-map content-pack id: '{entry.ContentPack.ContentPackId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryResolve(
            string operationMapId,
            out OperationMapDefinition definition)
        {
            definition = null;
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId) || definitions == null)
                return false;

            for (int index = 0; index < definitions.Length; index++)
            {
                OperationMapDefinition candidate = definitions[index];
                if (candidate != null && string.Equals(
                        operationMapId,
                        candidate.OperationMapId,
                        StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryResolveEntry(
            string operationMapId,
            out OperationMapCatalogEntryConfig entry)
        {
            entry = default;
            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId) || entries == null)
                return false;

            for (int index = 0; index < entries.Length; index++)
            {
                OperationMapDefinition definition = entries[index].Definition;
                if (definition != null && string.Equals(
                        operationMapId,
                        definition.OperationMapId,
                        StringComparison.Ordinal))
                {
                    entry = entries[index];
                    return true;
                }
            }

            return false;
        }
    }
}
