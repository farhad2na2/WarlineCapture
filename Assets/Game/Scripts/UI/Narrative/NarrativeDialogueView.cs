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
        [SerializeField] private RectTransform dialogueRect;
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
        [SerializeField] private bool useAuthoredHeight;
        [SerializeField, Min(0.1f)] private float authoredFontScale = 1f;

        private NarrativeDialoguePhase phase;
        private Action<NarrativeDialoguePhase> inputHandler;
        private bool subtitlesVisible = true;
        private bool inputBound;
        private const float StandardHeight = 292f;
        private const float ExpandedHeight = 376f;
        private const float DialogueTextTopInset = 155f;
        private const float DialogueTextBottomInset = 78f;
        private const float DialogueTextHeightPadding = 10f;
        private const float DialogueTextHorizontalInsets = 466f;
        private const float FallbackTextWidth = 1074f;
        private const float FallbackMaximumHeight = 720f;

        public NarrativeDialoguePhase Phase => phase;
        public Sprite CurrentPortraitSprite => portraitImage != null ? portraitImage.sprite : null;
        public bool IsPortraitVisible => portraitImage != null && portraitImage.gameObject.activeSelf;

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
                ariaIconImage.color = Color.white;
            }
        }

        public void PrepareLine(string resolvedText, NarrativeSubtitleStyle style)
        {
            Canvas.ForceUpdateCanvases();
            subtitlesVisible = style.Visible;
            if (dialogueText != null)
            {
                float resolvedFontSize = style.FontSize * Mathf.Max(0.1f, authoredFontScale);
                dialogueText.text = resolvedText ?? string.Empty;
                dialogueText.fontSize = resolvedFontSize;
                if (dialogueText.enableAutoSizing)
                {
                    dialogueText.fontSizeMax = resolvedFontSize;
                    dialogueText.fontSizeMin = Mathf.Min(18f, resolvedFontSize);
                }
                dialogueText.maxVisibleCharacters = style.InstantText ? int.MaxValue : 0;
                dialogueText.overflowMode = TextOverflowModes.Overflow;
            }

            if (dialogueRect != null && !useAuthoredHeight)
            {
                Vector2 size = dialogueRect.sizeDelta;
                size.y = CalculateRequiredHeight(resolvedText, style);
                dialogueRect.sizeDelta = size;
            }

            if (frameImage != null)
            {
                Color color = frameImage.color;
                color.a = Mathf.Clamp01(style.BackgroundOpacity);
                frameImage.color = color;
                frameImage.type = Image.Type.Sliced;
            }

            if (pointerImage != null)
                pointerImage.gameObject.SetActive(false);

            SetPhase(style.InstantText ? NarrativeDialoguePhase.AdvanceReady : NarrativeDialoguePhase.Revealing);
        }

        private float CalculateRequiredHeight(string resolvedText, in NarrativeSubtitleStyle style)
        {
            float minimumHeight = style.FontSize >= 60f ? ExpandedHeight : StandardHeight;
            if (dialogueText == null)
                return minimumHeight;

            float textWidth = dialogueRect != null
                ? dialogueRect.rect.width - DialogueTextHorizontalInsets
                : dialogueText.rectTransform.rect.width;
            if (textWidth <= 1f)
                textWidth = FallbackTextWidth;

            dialogueRect?.ForceUpdateRectTransforms();
            dialogueText.rectTransform.ForceUpdateRectTransforms();
            dialogueText.ForceMeshUpdate(true, true);
            float preferredTextHeight = CalculateRenderedTextHeight(resolvedText, textWidth);
            float requiredHeight = DialogueTextTopInset + DialogueTextBottomInset +
                                   preferredTextHeight + DialogueTextHeightPadding;
            return Mathf.Clamp(Mathf.Ceil(requiredHeight), minimumHeight, CalculateMaximumHeight());
        }

        private float CalculateRenderedTextHeight(string resolvedText, float textWidth)
        {
            TMP_TextInfo textInfo = dialogueText.textInfo;
            if (textInfo != null && textInfo.lineCount > 0)
            {
                TMP_LineInfo first = textInfo.lineInfo[0];
                TMP_LineInfo last = textInfo.lineInfo[textInfo.lineCount - 1];
                float renderedHeight = first.ascender - last.descender;
                if (renderedHeight > 1f)
                    return renderedHeight;
            }

            return dialogueText.GetPreferredValues(resolvedText ?? string.Empty, textWidth, 0f).y;
        }

        private float CalculateMaximumHeight()
        {
            if (dialogueRect == null || dialogueRect.parent is not RectTransform parentRect || parentRect.rect.height <= 1f)
                return FallbackMaximumHeight;

            float scale = Mathf.Max(0.01f, Mathf.Abs(dialogueRect.localScale.y));
            float availableHeight = parentRect.rect.height / scale - dialogueRect.anchoredPosition.y - 24f;
            return Mathf.Max(ExpandedHeight, availableHeight);
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
            if (pointerImage != null)
                pointerImage.gameObject.SetActive(nextPhase == NarrativeDialoguePhase.AdvanceReady);
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
