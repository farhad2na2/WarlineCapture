using UnityEngine;

namespace Game.UI.Runtime
{
    internal static class MainMenuNavigationSessionState
    {
        public static MainMenuNavigationTabId ActiveTab { get; private set; } = MainMenuNavigationTabId.Leaderboards;

        public static void Select(MainMenuNavigationTabId tabId)
        {
            ActiveTab = tabId;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
            ActiveTab = MainMenuNavigationTabId.Leaderboards;
        }
    }
}
