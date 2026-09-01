using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class NarrativeCommanderIdentityView : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private TMP_InputField callsignInput;
        [SerializeField] private TMP_InputField displayNameInput;

        [Header("Portraits")]
        [SerializeField] private Image selectedPortraitImage;
        [SerializeField] private Sprite defaultPortrait;
        [SerializeField] private Button[] portraitButtons;
        [SerializeField] private Image[] portraitImages;
        [SerializeField] private Behaviour[] portraitSelectionImages;

        [Header("Actions")]
        [SerializeField] private Button continueButton;

        [Header("Accessibility Labels")]
        [SerializeField] private TMP_Text callsignAccessibilityLabel;
        [SerializeField] private TMP_Text displayNameAccessibilityLabel;
        [SerializeField] private TMP_Text[] portraitAccessibilityLabels;
        [SerializeField] private TMP_Text continueAccessibilityLabel;

        private Action<int> portraitSelectionHandler;
        private Action continueHandler;
        private UnityAction[] portraitListeners;
        private int selectedPortraitIndex = -1;
        private bool eventsWired;

        public string CallsignText => callsignInput != null ? callsignInput.text : string.Empty;
        public string DisplayNameText => displayNameInput != null ? displayNameInput.text : string.Empty;
        public int SelectedPortraitIndex => selectedPortraitIndex;
        public Sprite SelectedPortrait => selectedPortraitImage != null ? selectedPortraitImage.sprite : null;
        public int PortraitOptionCount => PortraitCount;

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

        public void BindIntents(Action<int> portraitSelected, Action continueRequested)
        {
            portraitSelectionHandler = portraitSelected;
            continueHandler = continueRequested;
            WireEvents();
        }

        public void UnbindIntents()
        {
            portraitSelectionHandler = null;
            continueHandler = null;
        }

        public void SetIdentity(string callsign, string displayName, int portraitIndex)
        {
            callsignInput?.SetTextWithoutNotify(callsign ?? string.Empty);
            displayNameInput?.SetTextWithoutNotify(displayName ?? string.Empty);
            SetPortraitSelection(portraitIndex);
        }

        public void SetPortraitSelection(int portraitIndex)
        {
            int count = PortraitCount;
            selectedPortraitIndex = count > 0 ? Mathf.Clamp(portraitIndex, 0, count - 1) : -1;

            if (selectedPortraitImage != null)
            {
                Sprite portrait = selectedPortraitIndex >= 0 && portraitImages != null &&
                                  selectedPortraitIndex < portraitImages.Length
                    ? portraitImages[selectedPortraitIndex]?.sprite
                    : null;
                selectedPortraitImage.sprite = portrait != null ? portrait : defaultPortrait;
            }

            ApplyPortraitSelectionVisuals();
        }

        public void SetControlsInteractable(bool interactable)
        {
            if (callsignInput != null)
                callsignInput.interactable = interactable;
            if (displayNameInput != null)
                displayNameInput.interactable = interactable;
            if (continueButton != null)
                continueButton.interactable = interactable;

            ApplyPortraitSelectionVisuals(interactable);
        }

        public void SetAccessibilityLabels(
            string callsignLabel,
            string displayNameLabel,
            string continueLabel,
            string[] portraitLabels = null)
        {
            SetText(callsignAccessibilityLabel, callsignLabel);
            SetText(displayNameAccessibilityLabel, displayNameLabel);
            SetText(continueAccessibilityLabel, continueLabel);

            int count = portraitAccessibilityLabels?.Length ?? 0;
            for (int i = 0; i < count; i++)
            {
                string label = portraitLabels != null && i < portraitLabels.Length
                    ? portraitLabels[i]
                    : string.Empty;
                SetText(portraitAccessibilityLabels[i], label);
            }
        }

        private int PortraitCount => Mathf.Max(
            portraitButtons?.Length ?? 0,
            Mathf.Max(portraitImages?.Length ?? 0, portraitSelectionImages?.Length ?? 0));

        private void WireEvents()
        {
            if (eventsWired)
                return;

            continueButton?.onClick.AddListener(HandleContinue);
            int count = portraitButtons?.Length ?? 0;
            portraitListeners = count > 0 ? new UnityAction[count] : null;
            for (int i = 0; i < count; i++)
            {
                Button button = portraitButtons[i];
                if (button == null)
                    continue;

                int capturedIndex = i;
                UnityAction listener = () => portraitSelectionHandler?.Invoke(capturedIndex);
                portraitListeners[i] = listener;
                button.onClick.AddListener(listener);
            }

            eventsWired = true;
        }

        private void UnwireEvents()
        {
            if (!eventsWired)
                return;

            continueButton?.onClick.RemoveListener(HandleContinue);
            int count = Mathf.Min(portraitButtons?.Length ?? 0, portraitListeners?.Length ?? 0);
            for (int i = 0; i < count; i++)
            {
                if (portraitButtons[i] != null && portraitListeners[i] != null)
                    portraitButtons[i].onClick.RemoveListener(portraitListeners[i]);
            }

            portraitListeners = null;
            eventsWired = false;
        }

        private void HandleContinue()
        {
            continueHandler?.Invoke();
        }

        private void ApplyPortraitSelectionVisuals(bool controlsInteractable = true)
        {
            int buttonCount = portraitButtons?.Length ?? 0;
            for (int i = 0; i < buttonCount; i++)
            {
                if (portraitButtons[i] != null)
                    portraitButtons[i].interactable = controlsInteractable && i != selectedPortraitIndex;
            }

            int imageCount = portraitSelectionImages?.Length ?? 0;
            for (int i = 0; i < imageCount; i++)
            {
                Behaviour selection = portraitSelectionImages[i];
                if (selection is V3SelectionFrameView frame)
                    frame.SetVisible(i == selectedPortraitIndex);
                else if (selection != null)
                    selection.enabled = i == selectedPortraitIndex;
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }
    }
}
