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

        private Action<FirstLaunchNarrativeLanguage> selectionHandler;
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
        }

        private void EnsureBindings()
        {
            if (bound)
                return;

            englishButton?.onClick.AddListener(SelectEnglish);
            persianButton?.onClick.AddListener(SelectPersian);
            bound = true;
        }

        private void SelectEnglish() => Select(FirstLaunchNarrativeLanguage.English);
        private void SelectPersian() => Select(FirstLaunchNarrativeLanguage.Persian);

        private void Select(FirstLaunchNarrativeLanguage language)
        {
            if (group == null || !group.interactable)
                return;

            group.interactable = false;
            selectionHandler?.Invoke(language);
        }
    }
}
