using System;
using UnityEngine;
namespace Game.UI.Runtime
{
    // The home recommendation and mission selector resolve the same authored mission illustration.
    public sealed class MissionAtlasMissionArtworkCatalog : ScriptableObject
    {
        public const string ResourceName="UI/MissionAtlasMissionArtwork";
        [Serializable] public struct Entry { public string MissionId; public Texture2D Texture; }
        [SerializeField] private Entry[] entries=Array.Empty<Entry>();
        private static MissionAtlasMissionArtworkCatalog loaded;
        public static MissionAtlasMissionArtworkCatalog Load()=>loaded!=null?loaded:loaded=Resources.Load<MissionAtlasMissionArtworkCatalog>(ResourceName);
        public Texture2D Find(string id)
        {foreach(var entry in entries)if(entry.MissionId==id)return entry.Texture;return null;}
        public void Configure(Entry[] values)=>entries=values??Array.Empty<Entry>();
    }
}
