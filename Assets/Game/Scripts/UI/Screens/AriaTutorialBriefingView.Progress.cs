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
            if (_tutorialStepCount == 5 && _tutorialStep == 2 && narrationPhase == UiTutorialNarrationPhase.WorldTarget)
                step = 3;
            else if (_tutorialStepCount == 5 && _tutorialStep is 3 or 4)
                step = narrationPhase == UiTutorialNarrationPhase.WorldTarget ? 5 : 4;

            int count = Mathf.Max(step, _tutorialStepCount);
            // M2's voice/action IDs retain the old nine-step contract. Its current
            // mission has five player lessons (IDs 2–6), ending with recruitment.
            // Change the displayed progress without renumbering those bindings.
            if (_tutorialStepCount == 9) { step = Mathf.Clamp(_tutorialStep - 1, 1, 5); count = 5; }
            if(_defenseTutorial) {step=step>9 ? step-3 : Mathf.Min(step,6);count=9;}
            SetLocalizedText(
                progressText,
                UiShellRuntimeGateway.Localization.Format("ui.aria.step", "STEP {0}/{1}", step, count));
        }

    }
}
