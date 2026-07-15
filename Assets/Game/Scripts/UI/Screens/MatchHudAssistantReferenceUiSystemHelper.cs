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

            TMP_Text[] authoredText = buttonRoot.GetComponentsInChildren<TMP_Text>(true);
            int directTextCount = 0;
            int highestSiblingIndex = -1;
            int secondHighestSiblingIndex = -1;
            for (int i = 0; i < authoredText.Length; i++)
            {
                TMP_Text candidate = authoredText[i];
                if (candidate == null || candidate.transform.parent != buttonRoot)
                    continue;

                directTextCount++;
                int siblingIndex = candidate.transform.GetSiblingIndex();
                if (siblingIndex > highestSiblingIndex)
                {
                    secondHighestSiblingIndex = highestSiblingIndex;
                    stateText = cueText;
                    highestSiblingIndex = siblingIndex;
                    cueText = candidate;
                }
                else if (siblingIndex > secondHighestSiblingIndex)
                {
                    secondHighestSiblingIndex = siblingIndex;
                    stateText = candidate;
                }
            }

            return directTextCount == 3 && stateText != null && cueText != null;
        }
    }
}
