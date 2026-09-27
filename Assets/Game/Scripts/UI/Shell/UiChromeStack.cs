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
        public const int ModalChrome = 32820;

        public static void RaiseAboveTutorial(GameObject root)
        {
            if (root == null)
                return;

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
                canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = ModalChrome;
        }
    }
}
