namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        public void SuspendForCinematic()
        {
            _tutorialCinematicSuspended = true;
            _tutorialShowAtUnscaledTime = -1f;
            HideEmbeddedTutorial();
            ClosePanelWithoutInputCapture();
            if (_accessStateText != null) _accessStateText.gameObject.SetActive(false);
            if (_accessCueText != null) _accessCueText.gameObject.SetActive(false);
            if (_embeddedTutorialView?.ProgressText != null) _embeddedTutorialView.ProgressText.gameObject.SetActive(false);
        }

    }
}
