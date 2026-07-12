using System;
using Game.Narrative.Contracts;
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

        private Action<NarrativeGuidanceMode> selectionHandler;
        private Action continueHandler;
        private NarrativeGuidanceMode selectedGuidance;
        private bool eventsWired;

        public NarrativeGuidanceMode SelectedGuidance => selectedGuidance;

        private void Awake()
        {
            WireEvents();
        }

        private void OnEnable()
        {
            WireEvents();
        }

        private void OnDisable()
        {
            UnwireEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
            UnbindIntents();
        }

        public void BindIntents(Action<NarrativeGuidanceMode> selected, Action continueRequested)
        {
            selectionHandler = selected;
            continueHandler = continueRequested;
            WireEvents();
        }

        public void UnbindIntents()
        {
            selectionHandler = null;
            continueHandler = null;
        }

        public void SetSelectedGuidance(NarrativeGuidanceMode guidance)
        {
            selectedGuidance = guidance;
            ApplySelectionVisuals();
        }

        public void SetControlsInteractable(bool interactable)
        {
            if (continueButton != null)
                continueButton.interactable = interactable;
            ApplySelectionVisuals(interactable);
        }

        public void SetAccessibilityLabels(
            string fullLabel,
            string contextualLabel,
            string minimalLabel,
            string continueLabel)
        {
            SetText(fullAccessibilityLabel, fullLabel);
            SetText(contextualAccessibilityLabel, contextualLabel);
            SetText(minimalAccessibilityLabel, minimalLabel);
            SetText(continueAccessibilityLabel, continueLabel);
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
            selectionHandler?.Invoke(NarrativeGuidanceMode.Full);
        }

        private void HandleContextualSelected()
        {
            selectionHandler?.Invoke(NarrativeGuidanceMode.Contextual);
        }

        private void HandleMinimalSelected()
        {
            selectionHandler?.Invoke(NarrativeGuidanceMode.Minimal);
        }

        private void HandleContinue()
        {
            continueHandler?.Invoke();
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

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }
    }
}
