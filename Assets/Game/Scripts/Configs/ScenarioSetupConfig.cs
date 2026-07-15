using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Operation Maps/Scenario Setup")]
    public sealed class ScenarioSetupConfig : ScriptableObject
    {
        [SerializeField] private string scenarioId;
        [SerializeField] private string operationMapId;

        public string ScenarioId => scenarioId;
        public string OperationMapId => operationMapId;

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
    }
}
