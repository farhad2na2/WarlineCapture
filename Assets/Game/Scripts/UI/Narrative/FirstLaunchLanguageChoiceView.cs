using System;
using Game.Narrative.Contracts;
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

        private Action<FirstLaunchNarrativeLanguage> selectionHandler;
        private FirstLaunchNarrativeLanguage selectedLanguage = FirstLaunchNarrativeLanguage.English;
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
                ApplySelectionVisuals();
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

        private static void SetSelectionVisible(Behaviour selection, bool visible)
        {
            if (selection is V3SelectionFrameView frame)
                frame.SetVisible(visible);
            else if (selection != null)
                selection.enabled = visible;
        }
    }
}
