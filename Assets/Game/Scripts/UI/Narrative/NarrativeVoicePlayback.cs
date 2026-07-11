using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class NarrativeVoicePlayback
    {
        private readonly AudioSource source;

        public NarrativeVoicePlayback(AudioSource source)
        {
            this.source = source;
        }

        public bool IsPlaying => source != null && source.isPlaying;
        public AudioClip Clip => source != null ? source.clip : null;
        public float ProgressSeconds => source == null || source.clip == null || source.clip.frequency <= 0
            ? 0f
            : (float)source.timeSamples / source.clip.frequency;

        public void Play(AudioClip clip, AudioSettingsModel settings)
        {
            if (source == null)
                return;

            Stop();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = settings.VoiceEnabled
                ? Mathf.Clamp01(settings.VoiceVolume / 100f)
                : 0f;

            if (clip != null)
                source.Play();
        }

        public void Pause()
        {
            if (source != null && source.isPlaying)
                source.Pause();
        }

        public void Resume()
        {
            if (source != null && source.clip != null)
                source.UnPause();
        }

        public void Stop()
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
        }
    }
}
