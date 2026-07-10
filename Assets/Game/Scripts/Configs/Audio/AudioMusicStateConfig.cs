using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public sealed class AudioMusicStateEntry
    {
        [SerializeField] private string stateId;
        [SerializeField] private string eventId;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.5f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.5f;
        [SerializeField] private bool loop = true;
        [SerializeField, Min(0f)] private float minimumPlaySeconds = 2f;

        public string StateId => stateId;
        public string EventId => eventId;
        public float FadeInSeconds => fadeInSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
        public bool Loop => loop;
        public float MinimumPlaySeconds => minimumPlaySeconds;
    }

    [CreateAssetMenu(menuName = "Game/Audio/Music State Config", fileName = "AudioMusicStateConfig")]
    public sealed class AudioMusicStateConfig : ScriptableObject
    {
        [SerializeField] private List<AudioMusicStateEntry> states = new();

        public IReadOnlyList<AudioMusicStateEntry> States => states;
    }
}
