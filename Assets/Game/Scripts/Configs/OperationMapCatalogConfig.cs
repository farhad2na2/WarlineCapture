using System;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Operation Maps/Operation Map Catalog")]
    public sealed class OperationMapCatalogConfig : ScriptableObject
    {
        [SerializeField] private OperationMapDefinition[] definitions =
            Array.Empty<OperationMapDefinition>();

        public ReadOnlySpan<OperationMapDefinition> Definitions => definitions;

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
    }
}
