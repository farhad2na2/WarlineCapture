using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class NarrativeDialoguePresentationSystemHelper
    {
        private readonly NarrativeSequenceView view;
        private readonly NarrativeDialogueRevealPresentationSystemHelper reveal = new();
        private readonly NarrativeVoicePlaybackPresentationSystemHelper voice;
        private float elapsed;
        private float readyElapsed;
        private float tailHold;
        private bool paused;
        private bool autoAdvance;
        private bool autoAdvanceRequested;
        private int appliedVisibleCharacters = -1;
        private AudioClip completionClip;
        private bool audiblePlaybackObserved, naturalCompletionEligible, completionRecorded;

        public NarrativeDialoguePresentationSystemHelper(NarrativeSequenceView view)
        {
            this.view = view;
            voice = new NarrativeVoicePlaybackPresentationSystemHelper(view != null ? view.VoiceSource : null);
        }

        public bool IsPaused => paused;
        public bool IsAdvanceReady => view != null && view.DialogueView.Phase == NarrativeDialoguePhase.AdvanceReady;

        public void StartDialogue(
            string resolvedText,
            in NarrativeSpeakerPresentationModel speaker,
            AudioClip voiceClip,
            float availableSeconds,
            in NarrativePunctuationPresentationModel punctuation,
            in UISettingsModel settings)
        {
            if (view == null)
                return;

            Cancel();
            NarrativeSubtitleStyle style = NarrativeSubtitleStyleUtilitySystemHelper.Resolve(settings);
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
            completionClip = voiceClip;
            audiblePlaybackObserved = completionRecorded = false;
            naturalCompletionEligible = voiceClip != null;
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
            var source = view.VoiceSource;
            if (naturalCompletionEligible)
            {
                if (source == null || !source.isActiveAndEnabled || source.clip != completionClip || source.mute || source.volume <= 0f)
                    naturalCompletionEligible = false;
                else if (source.isPlaying && source.timeSamples > 0)
                    audiblePlaybackObserved = true;
                else if (!source.isPlaying && audiblePlaybackObserved && elapsed < completionClip.length)
                    naturalCompletionEligible = false;
            }
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
            // Editor frame stalls and audio loading can put elapsed time ahead of
            // actual playback. Advance only after the voice source has finished.
            bool voiceComplete = clip == null || !voice.IsPlaying && elapsed >= clip.length;
            if (!voiceComplete)
                return;

            // Observe the exact natural finish in the production tick. Editor sampling
            // may miss the last audio frame and timeSamples resets when playback ends.
            if (!completionRecorded && naturalCompletionEligible && audiblePlaybackObserved && clip == completionClip)
            {
                view.RecordNaturalVoiceCompletion(clip);
                completionRecorded = true;
            }

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
            naturalCompletionEligible = false;
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
            naturalCompletionEligible = false;
            completionClip = null;
            audiblePlaybackObserved = completionRecorded = false;
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
