using System.Collections.Generic;
using UnityEngine;

namespace Game.Configs
{
    [CreateAssetMenu(menuName = "Game/Audio/Event Catalog Config", fileName = "AudioEventCatalogConfig")]
    public sealed class AudioEventCatalogConfig : ScriptableObject
    {
        [SerializeField] private List<AudioEventCatalogEntry> events = new();

        public IReadOnlyList<AudioEventCatalogEntry> Events => events;
    }
}
