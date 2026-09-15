using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private bool _defenseTutorial;
        private void ApplyProgress(UiTutorialNarrationPhase narrationPhase)
        {
            if (progressText != null) progressText.gameObject.SetActive(_tutorialStepCount > 0);
            int step = Mathf.Max(1, _tutorialStep);
            if (_tutorialStepCount is not (8 or 12) && _tutorialStep == 2 && narrationPhase == UiTutorialNarrationPhase.WorldTarget)
                step = 3;
            else if (_tutorialStepCount is not (8 or 12) && _tutorialStep is 3 or 4)
                step = narrationPhase == UiTutorialNarrationPhase.WorldTarget ? 5 : 4;

            int count = Mathf.Max(step, _tutorialStepCount);
            if(_defenseTutorial) {step=step>7 ? step-1 : Mathf.Min(step,6);count=11;}
            SetLocalizedText(
                progressText,
                UiShellRuntimeGateway.Localization.Format("ui.aria.step", "STEP {0}/{1}", step, count));
        }

    }
}
