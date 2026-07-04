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
            if (minimapCameraBridge != null && !minimapCameraBridge.activeSelf)
                minimapCameraBridge.SetActive(true);
        }

        public void ShowSelectedEntity(string displayName, string status)
        {
            if (selectedEntityNameText != null)
                SetText(selectedEntityNameText, displayName);
            if (selectedEntityStatusText != null)
                SetText(selectedEntityStatusText, status);
            if (selectedEntityPanel != null && !selectedEntityPanel.activeSelf)
                selectedEntityPanel.SetActive(true);
        }

        public void HideSelectedEntity()
        {
            if (selectedEntityPanel != null && selectedEntityPanel.activeSelf)
                selectedEntityPanel.SetActive(false);
        }

        public void ShowCommandMode(string mode)
        {
            HideInvalidCommand();
            if (commandModeText != null)
                SetText(commandModeText, mode);
            if (commandModeBanner != null && !commandModeBanner.activeSelf)
                commandModeBanner.SetActive(true);
        }

        public void HideCommandMode()
        {
            if (commandModeBanner != null && commandModeBanner.activeSelf)
                commandModeBanner.SetActive(false);
        }

        public void ShowInvalidCommand(string reason)
        {
            if (invalidCommandText != null)
                SetText(invalidCommandText, reason);
            if (invalidCommandToast != null && !invalidCommandToast.activeSelf)
                invalidCommandToast.SetActive(true);
        }

        public void HideInvalidCommand()
        {
            if (invalidCommandToast != null && invalidCommandToast.activeSelf)
                invalidCommandToast.SetActive(false);
        }

        public void SetWorldMarkersVisible(bool visible)
        {
            if (worldCommandMarkerLayer != null && worldCommandMarkerLayer.activeSelf != visible)
                worldCommandMarkerLayer.SetActive(visible);
        }

        private static void SetText(TMP_Text text, string value)
        {
            value ??= string.Empty;
            if (text.text != value)
                text.text = value;
        }
    }
}
