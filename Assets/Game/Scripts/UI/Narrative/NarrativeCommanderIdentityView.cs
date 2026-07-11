using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class NarrativeCommanderIdentityView : MonoBehaviour
    {
        private const string FallbackCallsign = "COMMANDER";
        private const string FallbackDisplayName = "Commander";

        [Header("Identity")]
        [SerializeField] private TMP_InputField callsignInput;
        [SerializeField] private TMP_InputField displayNameInput;
        [SerializeField] private string defaultCallsign = FallbackCallsign;
        [SerializeField] private string defaultDisplayName = FallbackDisplayName;

        [Header("Portraits")]
        [SerializeField] private Image selectedPortraitImage;
        [SerializeField] private Sprite defaultPortrait;
        [SerializeField] private Button[] portraitButtons;
        [SerializeField] private Image[] portraitImages;
        [SerializeField] private Image[] portraitSelectionImages;
        [SerializeField] private int defaultPortraitIndex;

        [Header("Actions")]
        [SerializeField] private Button continueButton;

        [Header("Accessibility Labels")]
        [SerializeField] private TMP_Text callsignAccessibilityLabel;
        [SerializeField] private TMP_Text displayNameAccessibilityLabel;
        [SerializeField] private TMP_Text[] portraitAccessibilityLabels;
        [SerializeField] private TMP_Text continueAccessibilityLabel;

        private Action<NarrativeUiAction> actionHandler;
        private UnityAction[] portraitListeners;
        private string sequenceId;
        private string stateId;
        private string lineId;
        private ulong transitionToken;
        private int selectedPortraitIndex = -1;
        private bool commitRequested;
        private bool eventsWired;

        public NarrativeCommanderIdentityData SelectedIdentity => new()
        {
            Callsign = ReadOrDefault(callsignInput, defaultCallsign, FallbackCallsign),
            DisplayName = ReadOrDefault(displayNameInput, defaultDisplayName, FallbackDisplayName)
        };

        public int SelectedPortraitIndex => selectedPortraitIndex;
        public Sprite SelectedPortrait => selectedPortraitImage != null ? selectedPortraitImage.sprite : null;
        public bool CommitRequested => commitRequested;

        private void Awake()
        {
            InitializeControls();
            WireEvents();
        }

        private void OnEnable()
        {
            InitializeControls();
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
            InitializeControls();
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

        public void ApplyIdentity(in NarrativeCommanderIdentityData identity, int portraitIndex = 0)
        {
            SetInputText(callsignInput, Normalize(identity.Callsign, defaultCallsign, FallbackCallsign));
            SetInputText(displayNameInput, Normalize(identity.DisplayName, defaultDisplayName, FallbackDisplayName));
            SelectPortrait(portraitIndex);
            commitRequested = false;
            SetControlsInteractable(true);
        }

        public void ResetToDefaults()
        {
            SetInputText(callsignInput, Normalize(defaultCallsign, FallbackCallsign, FallbackCallsign));
            SetInputText(displayNameInput, Normalize(defaultDisplayName, FallbackDisplayName, FallbackDisplayName));
            SelectPortrait(defaultPortraitIndex);
            commitRequested = false;
            SetControlsInteractable(true);
        }

        public void SelectPortrait(int portraitIndex)
        {
            int count = PortraitCount;
            selectedPortraitIndex = count > 0 ? Mathf.Clamp(portraitIndex, 0, count - 1) : -1;

            if (selectedPortraitImage != null)
            {
                Sprite portrait = selectedPortraitIndex >= 0 && portraitImages != null && selectedPortraitIndex < portraitImages.Length
                    ? portraitImages[selectedPortraitIndex]?.sprite
                    : null;
                selectedPortraitImage.sprite = portrait != null ? portrait : defaultPortrait;
            }

            ApplyPortraitSelectionVisuals();
        }

        public void SetAccessibilityLabels(
            string callsignLabel,
            string displayNameLabel,
            string continueLabel,
            string[] portraitLabels = null)
        {
            SetText(callsignAccessibilityLabel, callsignLabel, "Commander callsign");
            SetText(displayNameAccessibilityLabel, displayNameLabel, "Commander display name");
            SetText(continueAccessibilityLabel, continueLabel, "Continue with commander identity");

            if (portraitAccessibilityLabels == null)
                return;

            for (int i = 0; i < portraitAccessibilityLabels.Length; i++)
            {
                string label = portraitLabels != null && i < portraitLabels.Length
                    ? portraitLabels[i]
                    : $"Commander portrait {i + 1}";
                SetText(portraitAccessibilityLabels[i], label, $"Commander portrait {i + 1}");
            }
        }

        private int PortraitCount => Mathf.Max(
            portraitButtons?.Length ?? 0,
            Mathf.Max(portraitImages?.Length ?? 0, portraitSelectionImages?.Length ?? 0));

        private void InitializeControls()
        {
            if (callsignInput != null)
            {
                callsignInput.readOnly = false;
                if (string.IsNullOrWhiteSpace(callsignInput.text))
                    SetInputText(callsignInput, Normalize(defaultCallsign, FallbackCallsign, FallbackCallsign));
            }

            if (displayNameInput != null)
            {
                displayNameInput.readOnly = false;
                if (string.IsNullOrWhiteSpace(displayNameInput.text))
                    SetInputText(displayNameInput, Normalize(defaultDisplayName, FallbackDisplayName, FallbackDisplayName));
            }

            if (selectedPortraitIndex < 0)
                SelectPortrait(defaultPortraitIndex);

            ApplyDefaultAccessibilityLabels();
        }

        private void WireEvents()
        {
            if (eventsWired)
                return;

            if (continueButton != null)
                continueButton.onClick.AddListener(HandleContinue);

            int count = portraitButtons?.Length ?? 0;
            portraitListeners = count > 0 ? new UnityAction[count] : null;
            for (int i = 0; i < count; i++)
            {
                Button button = portraitButtons[i];
                if (button == null)
                    continue;

                int capturedIndex = i;
                UnityAction listener = () => SelectPortrait(capturedIndex);
                portraitListeners[i] = listener;
                button.onClick.AddListener(listener);
            }

            eventsWired = true;
        }

        private void UnwireEvents()
        {
            if (!eventsWired)
                return;

            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinue);

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
            if (commitRequested || actionHandler == null)
                return;

            commitRequested = true;
            SetControlsInteractable(false);
            actionHandler.Invoke(new NarrativeUiAction
            {
                SequenceId = sequenceId,
                StateId = stateId,
                LineId = lineId,
                Kind = NarrativeUiActionKind.CommitCommanderIdentity,
                TransitionToken = transitionToken
            });
        }

        private void SetControlsInteractable(bool interactable)
        {
            if (callsignInput != null)
                callsignInput.interactable = interactable;
            if (displayNameInput != null)
                displayNameInput.interactable = interactable;
            if (continueButton != null)
                continueButton.interactable = interactable;

            ApplyPortraitSelectionVisuals(interactable);
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
                if (portraitSelectionImages[i] != null)
                    portraitSelectionImages[i].enabled = i == selectedPortraitIndex;
            }
        }

        private static string ReadOrDefault(TMP_InputField input, string configuredDefault, string hardFallback)
        {
            return Normalize(input != null ? input.text : null, configuredDefault, hardFallback);
        }

        private static string Normalize(string value, string configuredDefault, string hardFallback)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
            if (!string.IsNullOrWhiteSpace(configuredDefault))
                return configuredDefault.Trim();
            return hardFallback;
        }

        private static void SetInputText(TMP_InputField input, string value)
        {
            input?.SetTextWithoutNotify(value ?? string.Empty);
        }

        private static void SetText(TMP_Text text, string value, string fallback)
        {
            if (text != null)
                text.text = string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private void ApplyDefaultAccessibilityLabels()
        {
            SetTextIfBlank(callsignAccessibilityLabel, "Commander callsign");
            SetTextIfBlank(displayNameAccessibilityLabel, "Commander display name");
            SetTextIfBlank(continueAccessibilityLabel, "Continue with commander identity");

            int count = portraitAccessibilityLabels?.Length ?? 0;
            for (int i = 0; i < count; i++)
                SetTextIfBlank(portraitAccessibilityLabels[i], $"Commander portrait {i + 1}");
        }

        private static void SetTextIfBlank(TMP_Text text, string fallback)
        {
            if (text != null && string.IsNullOrWhiteSpace(text.text))
                text.text = fallback;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if ((callsignInput == null && displayNameInput == null) || selectedPortraitImage == null ||
                continueButton == null || callsignAccessibilityLabel == null ||
                displayNameAccessibilityLabel == null || continueAccessibilityLabel == null)
            {
                Debug.LogWarning($"[{nameof(NarrativeCommanderIdentityView)}] Missing required serialized reference on {name}.", this);
            }
        }
#endif
    }
}
