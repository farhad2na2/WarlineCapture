using UnityEngine;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class UIShellContentView
    {
        [SerializeField] private GameObject pauseMenuPopupPrefab;
        [SerializeField] private GameObject settingsPopupPrefab;
        private GameObject _pauseMenuPopupInstance;
        private GameObject _settingsPopupInstance;
        private SettingsPopupView _settingsPopupView;

    }
}
