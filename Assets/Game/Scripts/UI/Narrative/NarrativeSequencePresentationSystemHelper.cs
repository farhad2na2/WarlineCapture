using Game.Configs;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class NarrativeSequencePresentationSystemHelper
    {
        private readonly NarrativeSequenceView view;
        private readonly NarrativeDialogueRevealSystemHelper reveal = new();
        private readonly NarrativeVoicePlaybackSystemHelper voice;
        private float elapsed;
        private float readyElapsed;
        private float tailHold;
        private bool paused;
        private bool autoAdvance;
        private bool autoAdvanceRequested;
        private int appliedVisibleCharacters = -1;

        public NarrativeSequencePresentationSystemHelper(NarrativeSequenceView view)
        {
            this.view = view;
            voice = new NarrativeVoicePlaybackSystemHelper(view != null ? view.VoiceSource : null);
        }

        public bool IsPaused => paused;
        public bool IsAdvanceReady => view != null && view.DialogueView.Phase == NarrativeDialoguePhase.AdvanceReady;

        public void StartDialogue(
            string resolvedText,
            in NarrativeSpeakerPresentationModel speaker,
            AudioClip voiceClip,
            float availableSeconds,
            NarrativePunctuationProfile punctuation,
            in UISettingsModel settings)
        {
            if (view == null || punctuation == null)
                return;

            Cancel();
            NarrativeSubtitleStyle style = NarrativeSubtitleStyleResolver.Resolve(settings);
            view.DialogueView.ApplySpeaker(speaker);
            view.DialogueView.SetAccessibilityText(resolvedText);
            view.DialogueView.PrepareLine(resolvedText, style);
            reveal.Prepare(
                resolvedText,
                availableSeconds,
                punctuation.CharactersPerSecond,
                punctuation.CommaPauseSeconds,
                punctuation.ClausePauseSeconds,
                punctuation.SentencePauseSeconds,
                punctuation.EllipsisPauseSeconds,
                style.InstantText);
            voice.Play(voiceClip, settings.Audio);
            elapsed = 0f;
            readyElapsed = 0f;
            tailHold = punctuation.TailHoldSeconds;
            autoAdvance = settings.Narrative.AutoAdvance;
            autoAdvanceRequested = false;
            appliedVisibleCharacters = -1;

            if (style.InstantText || reveal.VisibleCharacterCount == 0)
                view.DialogueView.CompleteLine();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (paused || view == null)
                return;

            elapsed += Mathf.Max(0f, unscaledDeltaTime);
            if (!IsAdvanceReady)
            {
                float revealClock = voice.ProgressSeconds > 0f ? voice.ProgressSeconds : elapsed;
                int visible = reveal.GetVisibleCharacterCount(revealClock);
                if (visible != appliedVisibleCharacters)
                {
                    view.DialogueView.SetVisibleCharacterCount(visible);
                    appliedVisibleCharacters = visible;
                }

                if (visible >= reveal.VisibleCharacterCount)
                    view.DialogueView.CompleteLine();
            }

            if (!IsAdvanceReady)
                return;

            AudioClip clip = voice.Clip;
            bool voiceComplete = clip == null || elapsed >= clip.length;
            if (!voiceComplete)
                return;

            readyElapsed += Mathf.Max(0f, unscaledDeltaTime);
            if (autoAdvance && readyElapsed >= tailHold)
                autoAdvanceRequested = true;
        }

        public void CompleteText()
        {
            if (view == null || IsAdvanceReady)
                return;
            view.DialogueView.CompleteLine();
        }

        public bool ConsumeAutoAdvanceRequest()
        {
            if (!autoAdvanceRequested)
                return false;
            autoAdvanceRequested = false;
            return true;
        }

        public void Pause()
        {
            if (paused)
                return;
            paused = true;
            voice.Pause();
        }

        public void Resume()
        {
            if (!paused)
                return;
            paused = false;
            voice.Resume();
        }

        public void Cancel()
        {
            voice.Stop();
            paused = false;
            autoAdvanceRequested = false;
            elapsed = 0f;
            readyElapsed = 0f;
            appliedVisibleCharacters = -1;
            if (view != null)
                view.DialogueView.SetPhase(NarrativeDialoguePhase.Hidden);
        }
    }
}
