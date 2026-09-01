using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class NarrativeSequenceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Image panelImage;
        [SerializeField] private AspectRatioFitter panelAspectFitter;
        [SerializeField] private RectTransform panelMotionRoot;
        [SerializeField] private NarrativeDialogueView dialogueView;
        [SerializeField] private NarrativeLocationIntroView locationIntroView;
        [SerializeField] private NarrativePlaybackControlsView playbackControls;
        [SerializeField] private NarrativeCommanderIdentityView commanderIdentityView;
        [SerializeField] private NarrativeGuidanceChoiceView guidanceChoiceView;
        [SerializeField] private NarrativeSkipConfirmationView skipConfirmationView;
        [SerializeField] private NarrativeReviewerControlsView reviewerControlsView;
        [SerializeField] private GameObject safeAreaPreview;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private NarrativeSequenceAudioView sequenceAudioView;
        [Header("Localization")]
        [SerializeField] private TMP_FontAsset persianFont;
        [SerializeField] private TMP_Text[] localizedTextTargets;
        [SerializeField] private string[] localizedTextKeys;
        [SerializeField] private string[] localizedTextEnglishFallbacks;

        private TMP_Text[] languageTextTargets;
        private TMP_FontAsset[] defaultFonts;
        private TextAlignmentOptions[] defaultAlignments;

        public NarrativeDialogueView DialogueView => dialogueView;
        public NarrativePlaybackControlsView PlaybackControlsView => playbackControls;
        public AudioSource VoiceSource => voiceSource;
        public NarrativeSequenceAudioView SequenceAudioView => sequenceAudioView;
        public RectTransform PanelMotionRoot => panelMotionRoot != null ? panelMotionRoot : panelImage != null ? panelImage.rectTransform : null;
        public NarrativeCommanderIdentityView CommanderIdentityView => commanderIdentityView;
        public NarrativeGuidanceChoiceView GuidanceChoiceView => guidanceChoiceView;
        public NarrativeSkipConfirmationView SkipConfirmationView => skipConfirmationView;
        public NarrativeReviewerControlsView ReviewerControlsView => reviewerControlsView;
        public Sprite CurrentPanelSprite => panelImage != null ? panelImage.sprite : null;
        public NarrativeLocationIntroView LocationIntroView => locationIntroView;

        private void Awake()
        {
            SetVisible(false);
        }

        public void ApplyPanel(in NarrativePanelPresentationModel model)
        {
            if (panelImage != null)
            {
                panelImage.sprite = model.PanelSprite;
                panelImage.color = model.Tint.a <= 0f ? Color.white : model.Tint;
                if (panelAspectFitter != null && model.PanelSprite != null && model.PanelSprite.rect.height > 0f)
                    panelAspectFitter.aspectRatio = model.PanelSprite.rect.width / model.PanelSprite.rect.height;
            }
        }

        public void ClearPanel()
        {
            if (panelImage != null)
                panelImage.sprite = null;
        }

        public void ApplyLocation(in NarrativeLocationPresentationModel model)
        {
            locationIntroView?.Apply(model);
        }

        public void SetSafeAreaPreview(bool visible)
        {
            if (safeAreaPreview != null)
                safeAreaPreview.SetActive(visible);
        }

        public void SetVisible(bool visible)
        {
            if (rootGroup == null)
                return;

            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }

        public void SetSkipState(bool visible, bool interactable, string accessibleLabel)
        {
            playbackControls?.SetSkipState(visible, interactable, accessibleLabel);
        }

        public void SetInteractiveState(NarrativeInteractiveStateKind kind)
        {
            bool interactive = kind != NarrativeInteractiveStateKind.None;
            if (locationIntroView != null)
                locationIntroView.gameObject.SetActive(!interactive);
            if (playbackControls != null)
                playbackControls.gameObject.SetActive(!interactive);
            if (commanderIdentityView != null)
                commanderIdentityView.gameObject.SetActive(kind == NarrativeInteractiveStateKind.CommanderIdentity);
            if (guidanceChoiceView != null)
                guidanceChoiceView.gameObject.SetActive(kind == NarrativeInteractiveStateKind.GuidanceChoice);
        }

        public void ApplyLanguage(bool rightToLeft, IGameTextResolver textResolver)
        {
            CacheDefaultTextPresentation();
            for (int i = 0; i < languageTextTargets.Length; i++)
            {
                TMP_Text target = languageTextTargets[i];
                if (target == null)
                    continue;

                target.font = rightToLeft && persianFont != null ? persianFont : defaultFonts[i];
                target.alignment = rightToLeft
                    ? ToRightAligned(defaultAlignments[i])
                    : defaultAlignments[i];
            }

            int localizedCount = Mathf.Min(
                localizedTextTargets?.Length ?? 0,
                Mathf.Min(localizedTextKeys?.Length ?? 0, localizedTextEnglishFallbacks?.Length ?? 0));
            IGameTextResolver resolver = textResolver ?? FallbackGameTextResolver.Instance;
            for (int i = 0; i < localizedCount; i++)
            {
                if (localizedTextTargets[i] != null)
                    localizedTextTargets[i].text = resolver.Get(localizedTextKeys[i], localizedTextEnglishFallbacks[i]);
            }
        }

        private void CacheDefaultTextPresentation()
        {
            if (languageTextTargets != null)
                return;

            languageTextTargets = GetComponentsInChildren<TMP_Text>(true);
            defaultFonts = new TMP_FontAsset[languageTextTargets.Length];
            defaultAlignments = new TextAlignmentOptions[languageTextTargets.Length];
            for (int i = 0; i < languageTextTargets.Length; i++)
            {
                defaultFonts[i] = languageTextTargets[i] != null ? languageTextTargets[i].font : null;
                defaultAlignments[i] = languageTextTargets[i] != null
                    ? languageTextTargets[i].alignment
                    : TextAlignmentOptions.Left;
            }
        }

        private static TextAlignmentOptions ToRightAligned(TextAlignmentOptions alignment)
        {
            return alignment switch
            {
                TextAlignmentOptions.Left => TextAlignmentOptions.Right,
                TextAlignmentOptions.TopLeft => TextAlignmentOptions.TopRight,
                TextAlignmentOptions.BottomLeft => TextAlignmentOptions.BottomRight,
                _ => alignment
            };
        }

    }
}
