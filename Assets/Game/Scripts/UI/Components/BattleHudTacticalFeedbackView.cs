using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BattleHudTacticalFeedbackView : MonoBehaviour
    {
        [SerializeField] private GameObject selectedEntityPanel;
        [SerializeField] private GameObject commandModeBanner;
        [SerializeField] private GameObject worldCommandMarkerLayer;
        [SerializeField] private GameObject invalidCommandToast;
        [SerializeField] private GameObject minimapCameraBridge;
        [SerializeField] private TMP_Text selectedEntityNameText;
        [SerializeField] private TMP_Text selectedEntityStatusText;
        [SerializeField] private TMP_Text commandModeText;
        [SerializeField] private TMP_Text invalidCommandText;

        private void Awake()
        {
            HideSelectedEntity();
            HideCommandMode();
            HideInvalidCommand();
            SetWorldMarkersVisible(false);
            if (minimapCameraBridge != null)
                minimapCameraBridge.SetActive(true);
        }

        public void ShowSelectedEntity(string displayName, string status)
        {
            if (selectedEntityNameText != null)
                selectedEntityNameText.text = displayName;
            if (selectedEntityStatusText != null)
                selectedEntityStatusText.text = status;
            if (selectedEntityPanel != null)
                selectedEntityPanel.SetActive(true);
        }

        public void HideSelectedEntity()
        {
            if (selectedEntityPanel != null)
                selectedEntityPanel.SetActive(false);
        }

        public void ShowCommandMode(string mode)
        {
            HideInvalidCommand();
            if (commandModeText != null)
                commandModeText.text = mode;
            if (commandModeBanner != null)
                commandModeBanner.SetActive(true);
        }

        public void HideCommandMode()
        {
            if (commandModeBanner != null)
                commandModeBanner.SetActive(false);
        }

        public void ShowInvalidCommand(string reason)
        {
            if (invalidCommandText != null)
                invalidCommandText.text = reason;
            if (invalidCommandToast != null)
                invalidCommandToast.SetActive(true);
        }

        public void HideInvalidCommand()
        {
            if (invalidCommandToast != null)
                invalidCommandToast.SetActive(false);
        }

        public void SetWorldMarkersVisible(bool visible)
        {
            if (worldCommandMarkerLayer != null)
                worldCommandMarkerLayer.SetActive(visible);
        }
    }
}
