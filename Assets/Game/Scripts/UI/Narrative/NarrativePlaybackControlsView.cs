using System;
using Game.Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class NarrativePlaybackControlsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup skipGroup;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text skipLabel;

        private Action skipHandler;
        private bool inputBound;

        private void Awake()
        {
            EnsureInputBinding();
        }

        private void OnDestroy()
        {
            if (skipButton != null && inputBound)
                skipButton.onClick.RemoveListener(HandleSkip);
            inputBound = false;
            skipHandler = null;
        }

        public void BindSkip(Action handler)
        {
            EnsureInputBinding();
            skipHandler = handler;
        }

        public void UnbindSkip()
        {
            skipHandler = null;
        }

        public void SetSkipState(bool visible, bool interactable, string accessibleLabel)
        {
            if (skipGroup != null)
            {
                skipGroup.alpha = visible ? 1f : 0f;
                skipGroup.blocksRaycasts = visible && interactable;
                skipGroup.interactable = visible && interactable;
            }

            if (skipButton != null)
                skipButton.interactable = interactable;
            if (skipLabel != null)
                skipLabel.text = string.IsNullOrWhiteSpace(accessibleLabel)
                    ? GameLocalization.Get("ui.common.skip", "SKIP")
                    : accessibleLabel;
        }

        private void HandleSkip()
        {
            skipHandler?.Invoke();
        }

        private void EnsureInputBinding()
        {
            if (inputBound || skipButton == null)
                return;
            skipButton.onClick.AddListener(HandleSkip);
            inputBound = true;
        }
    }
}
