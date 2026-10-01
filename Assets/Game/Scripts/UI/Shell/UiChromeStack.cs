using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Screen-space overlay order. Tutorial markers and the ARIA hand stay above the HUD.
    /// The comic, Pause, and Settings stay above those markers.
    /// </summary>
    internal static class UiChromeStack
    {
        public const int TutorialOverlay = 32700;
        public const int TutorialCue = 32750;
        public const int AriaHand = 32760;
        // Unity stores overlay sorting in a signed 16-bit value. 32768 and above wrap
        // negative and draw behind the HUD. Stay just above the ARIA hand.
        public const int NarrativeChrome = 32764;
        public const int ModalChrome = 32766;

        public static void RaiseAboveTutorial(GameObject root, int sortingOrder = ModalChrome)
        {
            if (root == null)
                return;

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
                canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            // Graphics move to this nested canvas when sorting is overridden.
            // The parent canvas's raycaster cannot hit those graphics.
            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();
        }
    }
}
