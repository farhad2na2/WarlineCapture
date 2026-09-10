using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class BuildPlacementConfirmationBarView
    {
        private void CacheSerializedLayout()
        {
            if (root == null)
                root = transform as RectTransform;
            if (root == null)
                return;

            _canvasGroup ??= GetComponent<CanvasGroup>();
            Image background = GetComponent<Image>();
            if (background != null && panelFrameSprite != null)
            {
                ApplySprite(background, panelFrameSprite, Image.Type.Sliced);
                if (background.color != Color.white)
                    background.color = Color.white;
                if (!background.raycastTarget)
                    background.raycastTarget = true;
            }
        }

        private bool HasRequiredLayoutReferences()
        {
            return titleText != null &&
                   statusText != null &&
                   costText != null &&
                   durationText != null &&
                   instructionText != null &&
                   cancelButton != null &&
                   rotateButton != null &&
                   confirmButton != null;
        }

        private void EnsureValidityPanel(RectTransform parent)
        {
            if (_validityPanel != null || validityPanelPrefab == null || parent == null)
                return;
            _validityPanel = BuildPlacementValidityPanelView.Ensure(validityPanelPrefab, parent);
        }

        private void SuppressThreatPanel()
        {
            if (_suppressedThreatPanel == null)
            {
                RectTransform[] candidates = transform.root.GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < candidates.Length; i++)
                {
                    RectTransform candidate = candidates[i];
                    if (candidate != null && candidate.name == "ThreatJumpPanel")
                    {
                        _suppressedThreatPanel = candidate.gameObject;
                        break;
                    }
                }
            }

            if (_suppressedThreatPanel == null)
                return;
            if (!_restoreThreatPanelWhenHidden)
                _restoreThreatPanelWhenHidden = _suppressedThreatPanel.activeSelf;
            if (_suppressedThreatPanel.activeSelf)
                _suppressedThreatPanel.SetActive(false);
        }

        private void RestoreThreatPanel()
        {
            if (_suppressedThreatPanel != null && _restoreThreatPanelWhenHidden &&
                !_suppressedThreatPanel.activeSelf)
            {
                _suppressedThreatPanel.SetActive(true);
            }
            _suppressedThreatPanel = null;
            _restoreThreatPanelWhenHidden = false;
        }

        private static void ApplyDefaultPlacementAnchors(RectTransform rect)
        {
            if (rect == null)
                return;

            // The prefab owns a full 1672x941 responsive section. Its visible
            // panel is the only hit target and begins after the squad tray.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

    }
}
