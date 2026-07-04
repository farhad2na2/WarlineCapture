using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    internal static class MatchHudCanvasBatchingUtility
    {
        public static void EnsureLocalCanvas(GameObject root, bool needsRaycaster)
        {
            if (root == null)
                return;

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
                canvas = root.AddComponent<Canvas>();

            canvas.overrideSorting = false;
            canvas.pixelPerfect = false;

            if (!needsRaycaster)
                return;

            GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = root.AddComponent<GraphicRaycaster>();

            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }
    }
}
