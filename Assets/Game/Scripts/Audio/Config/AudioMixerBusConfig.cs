using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Configs
{
    [Serializable]
    public sealed class AudioMixerBusEntry
    {
        [SerializeField] private string busId;
        [SerializeField] private string parentBusId = "Master";
        [SerializeField] private AudioMixerGroup mixerGroup;
        [SerializeField] private string volumeSettingKey;
        [SerializeField] private float defaultVolumeDecibels;
        [SerializeField] private bool canDuck;
        [SerializeField] private List<string> duckTargetBusIds = new();

        public string BusId => busId;
        public string ParentBusId => parentBusId;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public string VolumeSettingKey => volumeSettingKey;
        public float DefaultVolumeDecibels => defaultVolumeDecibels;
        public bool CanDuck => canDuck;
        public IReadOnlyList<string> DuckTargetBusIds => duckTargetBusIds;
    }

    [CreateAssetMenu(menuName = "Game/Audio/Mixer Bus Config", fileName = "AudioMixerBusConfig")]
    public sealed class AudioMixerBusConfig : ScriptableObject
    {
        [SerializeField] private List<AudioMixerBusEntry> buses = new();

        public IReadOnlyList<AudioMixerBusEntry> Buses => buses;
    }
}
