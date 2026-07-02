using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Custom Game/Startup Config")]
    public sealed class CustomGameStartupConfig : ScriptableObject
    {
        [SerializeField] private string gameModeId = "custom.skirmish.quick";
        [SerializeField] private CustomGameMapConfig mapConfig;
        [SerializeField] private CustomGameFactionConfig factionConfig;
        [SerializeField] private CustomGameUnitRosterConfig unitRosterConfig;
        [SerializeField] private CustomGameVisualRegistryConfig visualRegistryConfig;

        public string GameModeId => gameModeId;
        public CustomGameMapConfig MapConfig => mapConfig;
        public CustomGameFactionConfig FactionConfig => factionConfig;
        public CustomGameUnitRosterConfig UnitRosterConfig => unitRosterConfig;
        public CustomGameVisualRegistryConfig VisualRegistryConfig => visualRegistryConfig;
    }
}
