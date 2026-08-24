using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI.Runtime
{
    internal sealed partial class MatchHudAssistantUiSystemHelper
    {
        private void MirrorPanelOpen(bool open, bool force = false)
        {
            if (!force && _mirroredPanelOpen == open)
                return;

            _mirroredPanelOpen = open;
            _panelOpenChanged?.Invoke(open);
        }

        private void DestroyPopupInstance()
        {
            if (_popupView != null)
                _popupView.UnbindActions();
            DestroyObject(_popupInstance);
            _popupInstance = null;
            _popupView = null;
            _panelUiSystem.Unbind();
        }

        private void CaptureUiOnly()
        {
            _captureGameplayUiClick?.Invoke();
            ClearSelectedButton();
        }

        private void LogMissingButton()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_loggedMissingButton)
                return;
            _loggedMissingButton = true;
            Debug.LogError("[ARIA] Match HUD prefab is missing HeaderContent/AriaAssistantButton; runtime button creation is disabled.");
#endif
        }

        private void LogInvalidButton()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_loggedInvalidButton)
                return;
            _loggedInvalidButton = true;
            Debug.LogError("[ARIA] HeaderContent/AriaAssistantButton must contain a Button plus TMP State and AlertCue children.");
#endif
        }

        private static bool ContainsRect(RectTransform rect, Vector2 screenPosition, Camera eventCamera)
        {
            return rect != null &&
                   rect.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
        }

        private static Camera ResolveEventCamera(Component component)
        {
            Canvas canvas = component != null ? component.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;
            return canvas.worldCamera;
        }

        private static void ClearSelectedButton()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(null);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
