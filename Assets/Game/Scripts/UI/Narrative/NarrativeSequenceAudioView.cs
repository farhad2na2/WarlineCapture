using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed class NarrativeSequenceAudioView : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource vehicleSource;
        [SerializeField] private AudioSource eventSource;

        [Header("Continuous beds")]
        [SerializeField] private AudioClip briefingMusic;
        [SerializeField] private AudioClip conflictMusic;
        [SerializeField] private AudioClip cityDayAmbience;
        [SerializeField] private AudioClip cityConflictAmbience;
        [SerializeField] private AudioClip battlefieldAmbience;
        [SerializeField] private AudioClip vehicleEngine;

        [Header("Narrative events")]
        [SerializeField] private AudioClip attackCue;
        [SerializeField] private AudioClip smallArmsCue;
        [SerializeField] private AudioClip radioCue;
        [SerializeField] private AudioClip blackoutCue;
        [SerializeField] private AudioClip ariaBootCue;
        [SerializeField] private AudioClip transitionCue;

        public AudioSource MusicSource => musicSource;
        public AudioSource AmbienceSource => ambienceSource;
        public AudioSource VehicleSource => vehicleSource;
        public AudioSource EventSource => eventSource;
        public AudioClip BriefingMusic => briefingMusic;
        public AudioClip ConflictMusic => conflictMusic;
        public AudioClip CityDayAmbience => cityDayAmbience;
        public AudioClip CityConflictAmbience => cityConflictAmbience;
        public AudioClip BattlefieldAmbience => battlefieldAmbience;
        public AudioClip VehicleEngine => vehicleEngine;
        public AudioClip AttackCue => attackCue;
        public AudioClip SmallArmsCue => smallArmsCue;
        public AudioClip RadioCue => radioCue;
        public AudioClip BlackoutCue => blackoutCue;
        public AudioClip AriaBootCue => ariaBootCue;
        public AudioClip TransitionCue => transitionCue;

        public void ApplyClips(AudioClip music, AudioClip ambience, AudioClip vehicle, AudioClip eventClip)
        {
            EnsureLoop(musicSource, music);
            EnsureLoop(ambienceSource, ambience);
            EnsureLoop(vehicleSource, vehicle);

            if (eventSource != null)
            {
                eventSource.Stop();
                eventSource.clip = eventClip;
                if (eventClip != null)
                    eventSource.Play();
            }
        }

        public void ApplyVolumes(float music, float ambience, float vehicle, float eventVolume)
        {
            if (musicSource != null)
                musicSource.volume = Mathf.Clamp01(music);
            if (ambienceSource != null)
                ambienceSource.volume = Mathf.Clamp01(ambience);
            if (vehicleSource != null)
                vehicleSource.volume = Mathf.Clamp01(vehicle);
            if (eventSource != null)
                eventSource.volume = Mathf.Clamp01(eventVolume);
        }

        public void Pause()
        {
            PauseSource(musicSource);
            PauseSource(ambienceSource);
            PauseSource(vehicleSource);
            PauseSource(eventSource);
        }

        public void Resume()
        {
            ResumeSource(musicSource);
            ResumeSource(ambienceSource);
            ResumeSource(vehicleSource);
            ResumeSource(eventSource);
        }

        public void StopAll()
        {
            StopSource(musicSource);
            StopSource(ambienceSource);
            StopSource(vehicleSource);
            StopSource(eventSource);
        }

        private static void EnsureLoop(AudioSource source, AudioClip clip)
        {
            if (source == null)
                return;
            if (clip == null)
            {
                StopSource(source);
                return;
            }
            if (source.clip == clip && source.isPlaying)
                return;

            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.Play();
        }

        private static void PauseSource(AudioSource source)
        {
            if (source != null && source.isPlaying)
                source.Pause();
        }

        private static void ResumeSource(AudioSource source)
        {
            if (source != null && source.clip != null)
                source.UnPause();
        }

        private static void StopSource(AudioSource source)
        {
            if (source == null)
                return;
            source.Stop();
            source.clip = null;
        }
    }
}
