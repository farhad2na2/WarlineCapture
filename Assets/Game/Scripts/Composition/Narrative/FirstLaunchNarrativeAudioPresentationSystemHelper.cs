using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    internal static class FirstLaunchNarrativeAudioPresentationSystemHelper
    {
        public static void EnterState(
            NarrativeSequenceView view,
            in UISettingsModel settings,
            NarrativeStateRecord state)
        {
            NarrativeSequenceAudioView audio = view?.SequenceAudioView;
            if (audio == null)
                return;

            ApplyVolumes(view, settings);
            audio.ApplyClips(
                state.MusicCue == NarrativeMusicCue.Conflict ? audio.ConflictMusic : audio.BriefingMusic,
                ResolveAmbience(audio, state.AmbienceCue),
                state.VehicleCue == NarrativeVehicleCue.Engine ? audio.VehicleEngine : null,
                ResolveCue(audio, state.EventCue));
        }

        public static void ApplyVolumes(NarrativeSequenceView view, in UISettingsModel settings)
        {
            NarrativeSequenceAudioView audio = view?.SequenceAudioView;
            if (audio == null)
                return;

            float master = Mathf.Clamp01(settings.Audio.MasterVolume / 100f);
            float music = settings.Audio.MusicEnabled
                ? master * Mathf.Clamp01(settings.Audio.MusicVolume / 100f) * 0.32f
                : 0f;
            float sound = settings.Audio.SoundEnabled
                ? master * Mathf.Clamp01(settings.Audio.SfxVolume / 100f)
                : 0f;
            audio.ApplyVolumes(music, sound * 0.55f, sound * 0.32f, sound * 0.28f);
        }

        private static AudioClip ResolveAmbience(
            NarrativeSequenceAudioView audio,
            NarrativeAmbienceCue cue)
        {
            return cue switch
            {
                NarrativeAmbienceCue.CityDay => audio.CityDayAmbience,
                NarrativeAmbienceCue.Battlefield => audio.BattlefieldAmbience,
                _ => audio.CityConflictAmbience
            };
        }

        private static AudioClip ResolveCue(NarrativeSequenceAudioView audio, NarrativeEventCue cue)
        {
            return cue switch
            {
                NarrativeEventCue.Attack => audio.AttackCue,
                NarrativeEventCue.SmallArms => audio.SmallArmsCue,
                NarrativeEventCue.Radio => audio.RadioCue,
                NarrativeEventCue.Blackout => audio.BlackoutCue,
                NarrativeEventCue.AriaBoot => audio.AriaBootCue,
                NarrativeEventCue.Transition => audio.TransitionCue,
                _ => null
            };
        }
    }
}
