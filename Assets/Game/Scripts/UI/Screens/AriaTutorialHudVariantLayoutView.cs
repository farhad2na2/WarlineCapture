using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Compatibility shim for older POP13 prefabs. Tutorial guidance now stays in
    /// the permanent Match HUD ARIA panel, so opening a command-assistant surface
    /// must never move the header or hide that panel.
    /// </summary>
    [DefaultExecutionOrder(2000)]
    [DisallowMultipleComponent]
    public sealed class AriaTutorialHudVariantLayoutView : MonoBehaviour
    {
        private MainMenuV3SectionLayoutView _tutorialLayout;
        private RectTransform _resourceStrip;
        private RectTransform _settingsButton;
        private RectTransform _pauseButton;
        private RectTransform _embeddedAria;
        private RectTransform _takeoverSurface;
        private RectState _resourceState;
        private RectState _settingsState;
        private RectState _pauseState;
        private bool _ariaWasActive;
        private bool _captured;

        public void RefreshLayout()
        {
            RestoreHeader();
        }

        public void RestoreLayout()
        {
            RestoreHeader();
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases -= RefreshLayout;
            Canvas.willRenderCanvases += RefreshLayout;
            RefreshLayout();
        }

        private void LateUpdate()
        {
            RefreshLayout();
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= RefreshLayout;
            RestoreLayout();
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= RefreshLayout;
            RestoreLayout();
        }

        private bool TryCaptureHeader()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return false;

            RectTransform[] rects = canvas.rootCanvas.GetComponentsInChildren<RectTransform>(true);
            _embeddedAria = FindOutsideSelf(rects, "AriaAssistantButton");
            Transform activeHeader = _embeddedAria != null ? _embeddedAria.parent : null;
            _resourceStrip = FindNamedRect(activeHeader, "ResourceStrip");
            _settingsButton = FindNamedRect(activeHeader, "SettingsButton");
            _pauseButton = FindNamedRect(activeHeader, "PauseButton");
            if (_resourceStrip == null || _settingsButton == null ||
                _pauseButton == null || _embeddedAria == null)
            {
                return false;
            }

            _tutorialLayout = GetComponent<MainMenuV3SectionLayoutView>();
            _resourceState = RectState.Capture(_resourceStrip);
            _settingsState = RectState.Capture(_settingsButton);
            _pauseState = RectState.Capture(_pauseButton);
            _ariaWasActive = _embeddedAria.gameObject.activeSelf;
            _captured = true;
            return true;
        }

        private void RestoreHeader()
        {
            if (!_captured)
                return;

            _resourceState.Restore(_resourceStrip);
            _settingsState.Restore(_settingsButton);
            _pauseState.Restore(_pauseButton);
            if (_embeddedAria != null)
                _embeddedAria.gameObject.SetActive(_ariaWasActive);
            _captured = false;
        }

        private RectTransform FindOutsideSelf(RectTransform[] candidates, string targetName)
        {
            RectTransform inactiveFallback = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                RectTransform candidate = candidates[i];
                if (candidate != null && candidate.name == targetName &&
                    !candidate.IsChildOf(transform))
                {
                    if (candidate.gameObject.activeInHierarchy)
                        return candidate;
                    inactiveFallback ??= candidate;
                }
            }
            return inactiveFallback;
        }

        private static RectTransform FindNamedRect(Transform root, string targetName)
        {
            if (root == null)
                return null;
            RectTransform[] candidates = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] != null && candidates[i].name == targetName)
                    return candidates[i];
            }
            return null;
        }

        private static void ApplyTopLeft(
            RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private readonly struct RectState
        {
            private readonly Vector2 _anchorMin;
            private readonly Vector2 _anchorMax;
            private readonly Vector2 _pivot;
            private readonly Vector2 _anchoredPosition;
            private readonly Vector2 _sizeDelta;
            private readonly Vector3 _localScale;
            private readonly Quaternion _localRotation;

            private RectState(RectTransform rect)
            {
                _anchorMin = rect.anchorMin;
                _anchorMax = rect.anchorMax;
                _pivot = rect.pivot;
                _anchoredPosition = rect.anchoredPosition;
                _sizeDelta = rect.sizeDelta;
                _localScale = rect.localScale;
                _localRotation = rect.localRotation;
            }

            public static RectState Capture(RectTransform rect) => new(rect);

            public void Restore(RectTransform rect)
            {
                if (rect == null)
                    return;
                rect.anchorMin = _anchorMin;
                rect.anchorMax = _anchorMax;
                rect.pivot = _pivot;
                rect.anchoredPosition = _anchoredPosition;
                rect.sizeDelta = _sizeDelta;
                rect.localScale = _localScale;
                rect.localRotation = _localRotation;
            }
        }
    }
}
