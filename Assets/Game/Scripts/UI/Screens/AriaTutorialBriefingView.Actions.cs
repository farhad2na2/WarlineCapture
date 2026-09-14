using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private Button continueButton;
        public Button ContinueButton
        {
            get
            {
                if (continueButton != null || doItButton == null) return continueButton;
                continueButton = Instantiate(doItButton, doItButton.transform.parent);
                continueButton.name = "ContinueLessonButton";
                continueButton.onClick = new Button.ButtonClickedEvent();
                continueButton.onClick.AddListener(() =>
                {
                    if (continueButton.IsActive() && continueButton.IsInteractable()) RequestExecuteRecommendation();
                });
                var label = continueButton.GetComponentInChildren<TMPro.TMP_Text>(true);
                UiLocalizedText.Set(label, "CONTINUE");
                continueButton.gameObject.SetActive(false);
                return continueButton;
            }
        }

        public void SetContinueAvailable(bool available)
        {
            doItButton.gameObject.SetActive(false);
            doItButton.interactable = false;
            var button = ContinueButton;
            if (button == null) return;
            if (available) showMeButton.gameObject.SetActive(false);
            button.interactable = available;
            button.gameObject.SetActive(available);
        }

        private bool IsContinueLabel(string label) => label == "CONTINUE" ||
            label == "mission.m03.action.continue" ||
            label == UiLocalizedText.CatalogLabel("CONTINUE") ||
            label == UiShellRuntimeGateway.Localization.Get("mission.m03.action.continue", "CONTINUE");
    }
}
