using System;
using Game.Components;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct ScenarioAnchorRequirementConfig
    {
        [SerializeField] private string anchorId;
        [SerializeField] private OperationMapAnchorKind kind;

        public ScenarioAnchorRequirementConfig(string anchorId, OperationMapAnchorKind kind)
        {
            this.anchorId = anchorId;
            this.kind = kind;
        }

        public string AnchorId => anchorId;
        public OperationMapAnchorKind Kind => kind;

        public bool TryValidate(out string error)
        {
            if (!OperationMapIdentityRules.IsValidAnchorId(anchorId))
            {
                error = $"Invalid required operation-map anchor id: '{anchorId ?? "<null>"}'.";
                return false;
            }

            if (kind == OperationMapAnchorKind.None)
            {
                error = $"Required operation-map anchor '{anchorId}' must declare a kind.";
                return false;
            }

            error = null;
            return true;
        }
    }

    [CreateAssetMenu(menuName = "Game/Operation Maps/Scenario Setup")]
    public sealed class ScenarioSetupConfig : ScriptableObject
    {
        [SerializeField] private string scenarioId;
        [SerializeField] private string operationMapId;
        [SerializeField] private ScenarioAnchorRequirementConfig[] requiredAnchors =
            Array.Empty<ScenarioAnchorRequirementConfig>();

        public string ScenarioId => scenarioId;
        public string OperationMapId => operationMapId;
        public ReadOnlySpan<ScenarioAnchorRequirementConfig> RequiredAnchors => requiredAnchors;

        public bool TryValidateIdentity(out string error)
        {
            if (!OperationMapIdentityRules.IsValidScenarioId(scenarioId))
            {
                error = $"Invalid scenario id: '{scenarioId ?? "<null>"}'.";
                return false;
            }

            if (!OperationMapIdentityRules.IsValidOperationMapId(operationMapId))
            {
                error = $"Invalid operation-map id: '{operationMapId ?? "<null>"}'.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryValidate(out string error)
        {
            if (!TryValidateIdentity(out error))
                return false;

            if (requiredAnchors == null)
            {
                error = $"Scenario '{scenarioId}' required-anchor collection is missing.";
                return false;
            }

            for (int index = 0; index < requiredAnchors.Length; index++)
            {
                if (!requiredAnchors[index].TryValidate(out error))
                    return false;

                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(
                            requiredAnchors[index].AnchorId,
                            requiredAnchors[previous].AnchorId,
                            StringComparison.Ordinal))
                    {
                        error = $"Scenario '{scenarioId}' has duplicate required anchor " +
                                $"'{requiredAnchors[index].AnchorId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
