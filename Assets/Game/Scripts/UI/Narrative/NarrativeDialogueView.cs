using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class NarrativeDialogueView : MonoBehaviour
    {
        [Header("Structure")]
        [SerializeField] private CanvasGroup dialogueGroup;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image pointerImage;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image ariaIconImage;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text speakerRoleText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text accessibilityText;
        [SerializeField] private GameObject advanceIndicator;
        [SerializeField] private Button inputButton;

        private NarrativeDialoguePhase phase;
        private Action<NarrativeDialoguePhase> inputHandler;
        private bool subtitlesVisible = true;
        private bool inputBound;

        public NarrativeDialoguePhase Phase => phase;

        private void Awake()
        {
            EnsureInputBinding();
        }

        private void OnDestroy()
        {
            if (inputButton != null && inputBound)
                inputButton.onClick.RemoveListener(HandleInput);
            inputBound = false;
            inputHandler = null;
        }

        public void BindInput(Action<NarrativeDialoguePhase> handler)
        {
            EnsureInputBinding();
            inputHandler = handler;
        }

        public void UnbindInput()
        {
            inputHandler = null;
        }

        public void ApplySpeaker(in NarrativeSpeakerPresentationModel model)
        {
            if (speakerNameText != null)
                speakerNameText.text = model.DisplayName ?? string.Empty;
            if (speakerRoleText != null)
                speakerRoleText.text = model.Role ?? string.Empty;

            bool isAria = model.Treatment == Game.Catalog.Contracts.NarrativeSpeakerTreatment.AriaIcon;
            if (portraitImage != null)
            {
                portraitImage.gameObject.SetActive(!isAria && model.IdentitySprite != null);
                portraitImage.sprite = model.IdentitySprite;
            }

            if (ariaIconImage != null)
            {
                ariaIconImage.gameObject.SetActive(isAria);
                ariaIconImage.sprite = isAria ? model.IdentitySprite : null;
                ariaIconImage.color = model.AccentColor;
            }
        }

        public void PrepareLine(string resolvedText, NarrativeSubtitleStyle style)
        {
            subtitlesVisible = style.Visible;
            if (dialogueText != null)
            {
                dialogueText.text = resolvedText ?? string.Empty;
                dialogueText.fontSize = style.FontSize;
                if (dialogueText.enableAutoSizing)
                {
                    dialogueText.fontSizeMax = style.FontSize;
                    dialogueText.fontSizeMin = Mathf.Min(18f, style.FontSize);
                }
                dialogueText.maxVisibleCharacters = style.InstantText ? int.MaxValue : 0;
            }

            if (frameImage != null)
            {
                Color color = frameImage.color;
                color.a = Mathf.Clamp01(style.BackgroundOpacity);
                frameImage.color = color;
                frameImage.type = Image.Type.Sliced;
            }

            if (pointerImage != null)
                pointerImage.gameObject.SetActive(!style.ReducedMotion);

            SetPhase(style.InstantText ? NarrativeDialoguePhase.AdvanceReady : NarrativeDialoguePhase.Revealing);
        }

        public void SetVisibleCharacterCount(int count)
        {
            if (dialogueText != null && dialogueText.maxVisibleCharacters != count)
                dialogueText.maxVisibleCharacters = count;
        }

        public void CompleteLine()
        {
            if (dialogueText != null)
                dialogueText.maxVisibleCharacters = int.MaxValue;
            SetPhase(NarrativeDialoguePhase.AdvanceReady);
        }

        public void SetPhase(NarrativeDialoguePhase nextPhase)
        {
            phase = nextPhase;
            if (dialogueGroup != null)
            {
                dialogueGroup.alpha = nextPhase == NarrativeDialoguePhase.Hidden || !subtitlesVisible ? 0f : 1f;
                dialogueGroup.interactable = nextPhase != NarrativeDialoguePhase.Hidden;
                dialogueGroup.blocksRaycasts = nextPhase != NarrativeDialoguePhase.Hidden;
            }

            if (advanceIndicator != null)
                advanceIndicator.SetActive(nextPhase == NarrativeDialoguePhase.AdvanceReady);
        }

        public void SetAccessibilityText(string completeLine)
        {
            if (accessibilityText != null)
                accessibilityText.text = completeLine ?? string.Empty;
        }

        public void SetSubtitlesVisible(bool visible)
        {
            subtitlesVisible = visible;
            SetPhase(phase);
        }

        private void HandleInput()
        {
            inputHandler?.Invoke(phase);
        }

        private void EnsureInputBinding()
        {
            if (inputBound || inputButton == null)
                return;
            inputButton.onClick.AddListener(HandleInput);
            inputBound = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (dialogueGroup == null || frameImage == null || pointerImage == null || dialogueText == null || inputButton == null)
                Debug.LogWarning($"[{nameof(NarrativeDialogueView)}] Missing required serialized reference on {name}.", this);
        }
#endif
    }
}
