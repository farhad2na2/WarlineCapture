using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;

namespace Game.Composition
{
    internal sealed partial class FirstLaunchNarrativeSequencePresentationSystemHelper
    {
        // Re-project the active line and crop without restarting progression, emitting a
        // handoff, changing the transition token, or resetting the commander's identity.
        internal void RefreshPresentation(IGameTextResolver resolver, NarrativeLocaleConfig locale, bool languageChanged)
        {
            if (!IsRunning || !states.TryGetValue(CurrentStateId, out var state)) return;
            if (languageChanged)
            {
                bool fullyRevealed = presentation.IsAdvanceReady;
                textResolver = resolver ?? FallbackGameTextResolver.Instance;
                portraitVoice.SetLocale(locale);
                view.ApplyLanguage(locale != null && locale.RightToLeft, textResolver);
                view.SetSkipState(!string.IsNullOrEmpty(state.SkipStateId), true, SkipLabel);
                view.ApplyLocation(FirstLaunchNarrativeModelUtilitySystemHelper.CreateLocation(state, textResolver));
                if (CurrentLineIndex >= 0 && CurrentLineIndex < state.Lines.Count)
                {
                    PresentLine(state, CurrentLineIndex, sequenceRuntime.TransitionToken);
                    if (fullyRevealed) presentation.CompleteText();
                    if (IsPaused) presentation.Pause();
                }
            }
            panels.Present(state, sequenceRuntime.TransitionToken);
        }
        public void SetReducedMotion(bool enabled)
        {
            settings.Accessibility.ReducedMotion = enabled;
            panelMotion?.SetReducedMotion(enabled);
        }

        public void SetSubtitlesEnabled(bool enabled)
        {
            settings.Narrative.SubtitlesEnabled = enabled;
            view?.DialogueView?.SetSubtitlesVisible(enabled);
        }

    }
}
