using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class NarrativeGuidanceChoiceView : MonoBehaviour
    {
        [Header("Choices")]
        [SerializeField] private Button fullButton;
        [SerializeField] private Button contextualButton;
        [SerializeField] private Button minimalButton;
        [SerializeField] private Image fullSelectionImage;
        [SerializeField] private Image contextualSelectionImage;
        [SerializeField] private Image minimalSelectionImage;

        [Header("Actions")]
        [SerializeField] private Button continueButton;

        [Header("Accessibility Labels")]
        [SerializeField] private TMP_Text fullAccessibilityLabel;
        [SerializeField] private TMP_Text contextualAccessibilityLabel;
        [SerializeField] private TMP_Text minimalAccessibilityLabel;
        [SerializeField] private TMP_Text continueAccessibilityLabel;

        private Action<NarrativeUiAction> actionHandler;
        private string sequenceId;
        private string stateId;
        private string lineId;
        private ulong transitionToken;
        private NarrativeGuidanceMode selectedGuidance = NarrativeGuidanceMode.Full;
        private bool commitRequested;
        private bool eventsWired;

        public NarrativeGuidanceMode SelectedGuidance => selectedGuidance;
        public bool CommitRequested => commitRequested;

        private void Awake()
        {
            ApplySelectionVisuals();
            ApplyDefaultAccessibilityLabels();
            WireEvents();
        }

        private void OnEnable()
        {
            ApplySelectionVisuals();
            ApplyDefaultAccessibilityLabels();
            WireEvents();
        }

        private void OnDisable()
        {
            UnwireEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
            actionHandler = null;
        }

        public void BindActions(Action<NarrativeUiAction> handler)
        {
            actionHandler = handler;
            commitRequested = false;
            SetControlsInteractable(true);
            WireEvents();
        }

        public void UnbindActions()
        {
            actionHandler = null;
        }

        public void Bind(Action<NarrativeUiAction> handler)
        {
            BindActions(handler);
        }

        public void Unbind()
        {
            UnbindActions();
        }

        public void SetActionContext(
            string nextSequenceId,
            string nextStateId,
            string nextLineId,
            ulong nextTransitionToken)
        {
            sequenceId = nextSequenceId;
            stateId = nextStateId;
            lineId = nextLineId;
            transitionToken = nextTransitionToken;
        }

        public void SetSelectedGuidance(NarrativeGuidanceMode guidance)
        {
            selectedGuidance = IsSupported(guidance) ? guidance : NarrativeGuidanceMode.Full;
            ApplySelectionVisuals(!commitRequested);
        }

        public void ResetToDefault()
        {
            selectedGuidance = NarrativeGuidanceMode.Full;
            commitRequested = false;
            SetControlsInteractable(true);
        }

        public void SetAccessibilityLabels(
            string fullLabel,
            string contextualLabel,
            string minimalLabel,
            string continueLabel)
        {
            SetText(fullAccessibilityLabel, fullLabel, "Full guidance");
            SetText(contextualAccessibilityLabel, contextualLabel, "Contextual guidance");
            SetText(minimalAccessibilityLabel, minimalLabel, "Minimal guidance");
            SetText(continueAccessibilityLabel, continueLabel, "Continue with selected guidance");
        }

        private void WireEvents()
        {
            if (eventsWired)
                return;

            fullButton?.onClick.AddListener(HandleFullSelected);
            contextualButton?.onClick.AddListener(HandleContextualSelected);
            minimalButton?.onClick.AddListener(HandleMinimalSelected);
            continueButton?.onClick.AddListener(HandleContinue);
            eventsWired = true;
        }

        private void UnwireEvents()
        {
            if (!eventsWired)
                return;

            fullButton?.onClick.RemoveListener(HandleFullSelected);
            contextualButton?.onClick.RemoveListener(HandleContextualSelected);
            minimalButton?.onClick.RemoveListener(HandleMinimalSelected);
            continueButton?.onClick.RemoveListener(HandleContinue);
            eventsWired = false;
        }

        private void HandleFullSelected()
        {
            SetSelectedGuidance(NarrativeGuidanceMode.Full);
        }

        private void HandleContextualSelected()
        {
            SetSelectedGuidance(NarrativeGuidanceMode.Contextual);
        }

        private void HandleMinimalSelected()
        {
            SetSelectedGuidance(NarrativeGuidanceMode.Minimal);
        }

        private void HandleContinue()
        {
            if (commitRequested || actionHandler == null)
                return;

            commitRequested = true;
            SetControlsInteractable(false);
            actionHandler.Invoke(new NarrativeUiAction
            {
                SequenceId = sequenceId,
                StateId = stateId,
                LineId = lineId,
                Kind = NarrativeUiActionKind.CommitGuidance,
                TransitionToken = transitionToken
            });
        }

        private void SetControlsInteractable(bool interactable)
        {
            if (continueButton != null)
                continueButton.interactable = interactable;
            ApplySelectionVisuals(interactable);
        }

        private void ApplySelectionVisuals(bool controlsInteractable = true)
        {
            SetChoiceState(fullButton, fullSelectionImage, NarrativeGuidanceMode.Full, controlsInteractable);
            SetChoiceState(contextualButton, contextualSelectionImage, NarrativeGuidanceMode.Contextual, controlsInteractable);
            SetChoiceState(minimalButton, minimalSelectionImage, NarrativeGuidanceMode.Minimal, controlsInteractable);
        }

        private void SetChoiceState(
            Button button,
            Image selectionImage,
            NarrativeGuidanceMode mode,
            bool controlsInteractable)
        {
            bool selected = selectedGuidance == mode;
            if (button != null)
                button.interactable = controlsInteractable;
            if (selectionImage != null)
                selectionImage.enabled = selected;
        }

        private void ApplyDefaultAccessibilityLabels()
        {
            SetTextIfBlank(fullAccessibilityLabel, "Full guidance");
            SetTextIfBlank(contextualAccessibilityLabel, "Contextual guidance");
            SetTextIfBlank(minimalAccessibilityLabel, "Minimal guidance");
            SetTextIfBlank(continueAccessibilityLabel, "Continue with selected guidance");
        }

        private static bool IsSupported(NarrativeGuidanceMode guidance)
        {
            return guidance == NarrativeGuidanceMode.Full ||
                   guidance == NarrativeGuidanceMode.Contextual ||
                   guidance == NarrativeGuidanceMode.Minimal;
        }

        private static void SetText(TMP_Text text, string value, string fallback)
        {
            if (text != null)
                text.text = string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static void SetTextIfBlank(TMP_Text text, string fallback)
        {
            if (text != null && string.IsNullOrWhiteSpace(text.text))
                text.text = fallback;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (fullButton == null || contextualButton == null || minimalButton == null || continueButton == null ||
                fullSelectionImage == null || contextualSelectionImage == null || minimalSelectionImage == null ||
                fullAccessibilityLabel == null || contextualAccessibilityLabel == null ||
                minimalAccessibilityLabel == null || continueAccessibilityLabel == null)
            {
                Debug.LogWarning($"[{nameof(NarrativeGuidanceChoiceView)}] Missing required serialized reference on {name}.", this);
            }
        }
#endif
    }
}
