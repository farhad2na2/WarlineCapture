using System;
using System.Collections.Generic;
using Game.Narrative.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class NarrativeLocaleTextRecord
    {
        [SerializeField] private string key;
        [SerializeField, TextArea] private string value;

        public string Key => key;
        public string Value => value;

        public NarrativeLocaleTextRecord(string localizedKey, string localizedValue)
        {
            key = localizedKey ?? string.Empty;
            value = localizedValue ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class NarrativeLocaleVoiceRecord
    {
        [SerializeField] private string lineId;
        [SerializeField] private AudioClip voiceClip;
        [SerializeField] private AudioClip femaleVoiceClip;
        [SerializeField] private AudioClip neutralVoiceClip;

        public string LineId => lineId;
        public AudioClip VoiceClip => voiceClip;
        public AudioClip FemaleVoiceClip => femaleVoiceClip;
        public AudioClip NeutralVoiceClip => neutralVoiceClip;

        public NarrativeLocaleVoiceRecord(
            string localizedLineId,
            AudioClip localizedVoiceClip,
            AudioClip localizedFemaleVoiceClip = null,
            AudioClip localizedNeutralVoiceClip = null)
        {
            lineId = localizedLineId ?? string.Empty;
            voiceClip = localizedVoiceClip;
            femaleVoiceClip = localizedFemaleVoiceClip;
            neutralVoiceClip = localizedNeutralVoiceClip;
        }
    }

    [CreateAssetMenu(menuName = "Game/Narrative/Locale Config", fileName = "NarrativeLocaleConfig")]
    public sealed class NarrativeLocaleConfig : ScriptableObject
    {
        [SerializeField] private string localeId = "en";
        [SerializeField] private FirstLaunchNarrativeLanguage language = FirstLaunchNarrativeLanguage.English;
        [SerializeField] private bool rightToLeft;
        [SerializeField] private List<NarrativeLocaleTextRecord> text = new();
        [SerializeField] private List<NarrativeLocaleVoiceRecord> voices = new();

        public string LocaleId => localeId;
        public FirstLaunchNarrativeLanguage Language => language;
        public bool RightToLeft => rightToLeft;
        public IReadOnlyList<NarrativeLocaleTextRecord> Text => text;
        public IReadOnlyList<NarrativeLocaleVoiceRecord> Voices => voices;
    }
}
