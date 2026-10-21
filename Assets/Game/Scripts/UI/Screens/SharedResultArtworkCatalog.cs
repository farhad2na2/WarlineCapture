using System;
using UnityEngine;
namespace Game.UI.Runtime
{
    [CreateAssetMenu(menuName = "Warline/UI/Shared result artwork")]
    public sealed class SharedResultArtworkCatalog : ScriptableObject
    {
        [Serializable] public struct Entry { public string MissionId; public Texture Artwork; }
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private Texture backdrop;
        public Texture Backdrop => backdrop;
        public Texture Find(string missionId)
        {
            foreach (var entry in entries)
                if (string.Equals(entry.MissionId, missionId, StringComparison.Ordinal)) return entry.Artwork;
            return null; // Missing art never borrows a different mission's story.
        }
#if UNITY_EDITOR
        public void Configure(Entry[] value, Texture background) { entries = value; backdrop = background; }
#endif
        static SharedResultArtworkCatalog loaded;
        public static SharedResultArtworkCatalog Load() => loaded != null ? loaded : loaded = Resources.Load<SharedResultArtworkCatalog>("UI/SharedResultArtwork");
    }
}
