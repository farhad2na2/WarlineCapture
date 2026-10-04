using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal static class MatchHudAssistantReferenceUiSystemHelper
    {
        public static RectTransform ResolveObjectivesPanel(
            GameObject headerContent,
            RectTransform assistantButton)
        {
            if (headerContent == null || assistantButton == null || assistantButton.parent == null)
                return null;

            MatchHudObjectivesElapsedView elapsedView =
                headerContent.GetComponentInChildren<MatchHudObjectivesElapsedView>(true);
            Transform objectiveRoot = elapsedView != null ? elapsedView.transform : null;
            while (objectiveRoot != null && objectiveRoot.parent != assistantButton.parent)
                objectiveRoot = objectiveRoot.parent;

            return objectiveRoot as RectTransform;
        }

        public static RectTransform ResolveButton(GameObject headerContent, out Button button)
        {
            button = null;
            if (headerContent == null)
                return null;

            Button[] authoredButtons = headerContent.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < authoredButtons.Length; i++)
            {
                Button candidate = authoredButtons[i];
                if (candidate == null ||
                    candidate.GetComponent<AriaTutorialBriefingView>() == null ||
                    candidate.GetComponent<Canvas>() == null ||
                    candidate.GetComponent<GraphicRaycaster>() == null ||
                    candidate.transform is not RectTransform candidateRoot)
                {
                    continue;
                }

                button = candidate;
                return candidateRoot;
            }

            return null;
        }

        public static bool TryResolveButtonText(
            RectTransform buttonRoot,
            out TMP_Text stateText,
            out TMP_Text cueText)
        {
            stateText = null;
            cueText = null;
            if (buttonRoot == null)
                return false;

            // Runtime ARIA explanations may add direct text children during play.
            // Bind the authored access labels by their stable hierarchy roles.
            stateText = buttonRoot.Find("State")?.GetComponent<TMP_Text>();
            cueText = buttonRoot.Find("AlertCue")?.GetComponent<TMP_Text>();
            return stateText != null && cueText != null;
        }
    }
}
