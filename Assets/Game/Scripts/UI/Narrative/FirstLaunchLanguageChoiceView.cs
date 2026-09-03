using System;
using Game.Configs;
using Game.Narrative.Contracts;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class FirstLaunchLanguageChoiceView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button persianButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Behaviour englishSelectionImage;
        [SerializeField] private Behaviour persianSelectionImage;
        [Header("Localized shell copy")]
        [SerializeField] private TMP_Text[] localizedShellTextTargets;
        [SerializeField] private string[] localizedShellTextKeys;
        [SerializeField] private string[] localizedShellEnglishFallbacks;

        private Action<FirstLaunchNarrativeLanguage> selectionHandler;
        private FirstLaunchNarrativeLanguage selectedLanguage = FirstLaunchNarrativeLanguage.English;
        private TMP_FontAsset[] defaultShellFonts;
        private TextAlignmentOptions[] defaultShellAlignments;
        private bool bound;

        public bool IsVisible => group != null && group.alpha > 0f && group.interactable;

        private void Awake()
        {
            EnsureBindings();
        }

        private void OnDestroy()
        {
            if (bound)
            {
                englishButton?.onClick.RemoveListener(SelectEnglish);
                persianButton?.onClick.RemoveListener(SelectPersian);
                continueButton?.onClick.RemoveListener(ConfirmSelection);
            }

            selectionHandler = null;
            bound = false;
        }

        public void Bind(Action<FirstLaunchNarrativeLanguage> handler)
        {
            EnsureBindings();
            selectionHandler = handler;
        }

        public void Unbind()
        {
            selectionHandler = null;
        }

        public void SetVisible(bool visible)
        {
            if (group == null)
                return;

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (visible)
            {
                ApplySelectionVisuals();
                ApplySelectedLanguage();
            }
        }

        private void EnsureBindings()
        {
            if (bound)
                return;

            englishButton?.onClick.AddListener(SelectEnglish);
            persianButton?.onClick.AddListener(SelectPersian);
            continueButton?.onClick.AddListener(ConfirmSelection);
            bound = true;
        }

        private void SelectEnglish() => SetSelection(FirstLaunchNarrativeLanguage.English);
        private void SelectPersian() => SetSelection(FirstLaunchNarrativeLanguage.Persian);

        private void SetSelection(FirstLaunchNarrativeLanguage language)
        {
            if (group == null || !group.interactable)
                return;

            selectedLanguage = language;
            ApplySelectionVisuals();
            ApplySelectedLanguage();
        }

        private void ConfirmSelection()
        {
            if (group == null || !group.interactable)
                return;

            group.interactable = false;
            selectionHandler?.Invoke(selectedLanguage);
        }

        private void ApplySelectionVisuals()
        {
            SetSelectionVisible(englishSelectionImage, selectedLanguage == FirstLaunchNarrativeLanguage.English);
            SetSelectionVisible(persianSelectionImage, selectedLanguage == FirstLaunchNarrativeLanguage.Persian);
        }

        private void ApplySelectedLanguage()
        {
            string localeCode = selectedLanguage == FirstLaunchNarrativeLanguage.Persian
                ? GameLocalization.PersianLocaleCode
                : GameLocalization.EnglishLocaleCode;
            GameLocalization.SetLocale(localeCode, persist: false);
            CacheShellPresentationDefaults();

            int count = Mathf.Min(
                localizedShellTextTargets?.Length ?? 0,
                Mathf.Min(
                    localizedShellTextKeys?.Length ?? 0,
                    localizedShellEnglishFallbacks?.Length ?? 0));
            bool rightToLeft = GameLocalization.IsRightToLeft;
            TMP_FontAsset localeFont = GameLocalization.CurrentFontAsset as TMP_FontAsset;
            for (int i = 0; i < count; i++)
            {
                TMP_Text target = localizedShellTextTargets[i];
                if (target == null)
                    continue;

                target.font = rightToLeft && localeFont != null
                    ? localeFont
                    : defaultShellFonts[i];
                target.alignment = rightToLeft
                    ? Mirror(defaultShellAlignments[i])
                    : defaultShellAlignments[i];
                ApplyLocalizedText(
                    target,
                    GameLocalization.Get(
                        localizedShellTextKeys[i],
                        localizedShellEnglishFallbacks[i]),
                    rightToLeft);
            }
        }

        private void CacheShellPresentationDefaults()
        {
            if (defaultShellFonts != null)
                return;

            int count = localizedShellTextTargets?.Length ?? 0;
            defaultShellFonts = new TMP_FontAsset[count];
            defaultShellAlignments = new TextAlignmentOptions[count];
            for (int i = 0; i < count; i++)
            {
                TMP_Text target = localizedShellTextTargets[i];
                defaultShellFonts[i] = target != null ? target.font : null;
                defaultShellAlignments[i] = target != null
                    ? target.alignment
                    : TextAlignmentOptions.Center;
            }
        }

        private static void ApplyLocalizedText(TMP_Text target, string value, bool rightToLeft)
        {
            if (target is RTLTextMeshPro rtl)
            {
                rtl.Farsi = rightToLeft;
                rtl.PreserveNumbers = true;
                rtl.ForceFix = rightToLeft;
                rtl.text = value ?? string.Empty;
                return;
            }

            target.isRightToLeftText = rightToLeft;
            target.text = value ?? string.Empty;
        }

        private static TextAlignmentOptions Mirror(TextAlignmentOptions alignment)
        {
            return alignment switch
            {
                TextAlignmentOptions.TopLeft => TextAlignmentOptions.TopRight,
                TextAlignmentOptions.TopRight => TextAlignmentOptions.TopLeft,
                TextAlignmentOptions.Left => TextAlignmentOptions.Right,
                TextAlignmentOptions.Right => TextAlignmentOptions.Left,
                TextAlignmentOptions.BottomLeft => TextAlignmentOptions.BottomRight,
                TextAlignmentOptions.BottomRight => TextAlignmentOptions.BottomLeft,
                TextAlignmentOptions.BaselineLeft => TextAlignmentOptions.BaselineRight,
                TextAlignmentOptions.BaselineRight => TextAlignmentOptions.BaselineLeft,
                TextAlignmentOptions.MidlineLeft => TextAlignmentOptions.MidlineRight,
                TextAlignmentOptions.MidlineRight => TextAlignmentOptions.MidlineLeft,
                TextAlignmentOptions.CaplineLeft => TextAlignmentOptions.CaplineRight,
                TextAlignmentOptions.CaplineRight => TextAlignmentOptions.CaplineLeft,
                _ => alignment
            };
        }

        private static void SetSelectionVisible(Behaviour selection, bool visible)
        {
            if (selection is V3SelectionFrameView frame)
                frame.SetVisible(visible);
            else if (selection != null)
                selection.enabled = visible;
        }
    }
}
