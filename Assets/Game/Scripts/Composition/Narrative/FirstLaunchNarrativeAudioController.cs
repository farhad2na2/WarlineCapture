using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.Composition
{
    internal static class FirstLaunchNarrativeAudioController
    {
        public static void EnterState(NarrativeSequenceView view, in UISettingsModel settings, string stateId)
        {
            NarrativeSequenceAudioView audio = view?.SequenceAudioView;
            if (audio == null)
                return;

            ApplyVolumes(view, settings);
            audio.ApplyClips(
                IsConflictState(stateId) ? audio.ConflictMusic : audio.BriefingMusic,
                ResolveAmbience(audio, stateId),
                IsVehicleState(stateId) ? audio.VehicleEngine : null,
                ResolveCue(audio, stateId));
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

        private static AudioClip ResolveAmbience(NarrativeSequenceAudioView audio, string stateId)
        {
            if (stateId == "FL-P01")
                return audio.CityDayAmbience;
            if (stateId == "FL-P02" || stateId == "FL-P03" || stateId == "FL-P15" ||
                stateId == "FL-P16" || stateId == "FL-P17" || stateId == "FL-P18")
                return audio.BattlefieldAmbience;
            return audio.CityConflictAmbience;
        }

        private static AudioClip ResolveCue(NarrativeSequenceAudioView audio, string stateId)
        {
            return stateId switch
            {
                "FL-P02" => audio.AttackCue,
                "FL-P03" or "FL-P04" => audio.RadioCue,
                "FL-P05" => audio.AriaBootCue,
                "FL-P06" => audio.BlackoutCue,
                "FL-P07" or "FL-P16" => audio.SmallArmsCue,
                "FL-P09" or "FL-P18" => audio.TransitionCue,
                "FL-P10" or "FL-P15" => audio.AttackCue,
                _ => null
            };
        }

        private static bool IsConflictState(string stateId)
        {
            return stateId == "FL-P02" || stateId == "FL-P03" || stateId == "FL-P07" ||
                   stateId == "FL-P10" || stateId == "FL-P15" || stateId == "FL-P16" ||
                   stateId == "FL-P17" || stateId == "FL-P18";
        }

        private static bool IsVehicleState(string stateId)
        {
            return stateId == "FL-P04" || stateId == "FL-P11" || stateId == "FL-P15" || stateId == "FL-P17";
        }
    }
}
