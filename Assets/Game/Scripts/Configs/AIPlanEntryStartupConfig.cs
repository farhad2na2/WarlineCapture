using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Config/AI Plan Entry Startup")]
    public sealed class AIPlanEntryStartupConfig : ScriptableObject
    {
        [SerializeField] private List<string> fallbackBuildingIds = new();
        [SerializeField] private List<string> fallbackProductionUnitIds = new();

        public IReadOnlyList<string> FallbackBuildingIds => fallbackBuildingIds;
        public IReadOnlyList<string> FallbackProductionUnitIds => fallbackProductionUnitIds;
    }
}
