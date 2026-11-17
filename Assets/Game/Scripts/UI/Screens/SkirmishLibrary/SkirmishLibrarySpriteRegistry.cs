using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Serialized plate and preview sprites. The art lives under Assets/Game/Art, which
    /// Resources.Load cannot see, so the screen resolves PreviewSpriteKey and PlatePreviewKey here.
    /// </summary>
    public sealed class SkirmishLibrarySpriteRegistry : ScriptableObject
    {
        public const string ResourceName = "SkirmishLibrary/SkirmishLibrarySpriteRegistry";

        [Serializable]
        public struct Entry
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        private static SkirmishLibrarySpriteRegistry _loaded;
        private System.Collections.Generic.Dictionary<string, Sprite> _lookup;

        public static SkirmishLibrarySpriteRegistry Load()
        {
            if (_loaded != null)
                return _loaded;
            _loaded = Resources.Load<SkirmishLibrarySpriteRegistry>(ResourceName);
            return _loaded;
        }

        public bool TryGetSprite(string key, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(key))
                return false;
            EnsureLookup();
            if (_lookup.TryGetValue(key, out sprite) && sprite != null)
                return true;
            string canonical = SkirmishLibraryPreviewAssets.CanonicalPath(key);
            if (!string.IsNullOrEmpty(canonical) &&
                !string.Equals(canonical, key, StringComparison.Ordinal) &&
                _lookup.TryGetValue(canonical, out sprite) &&
                sprite != null)
                return true;
            sprite = null;
            return false;
        }

        public bool TryGetTexture(string key, out Texture2D texture)
        {
            texture = null;
            if (!TryGetSprite(key, out Sprite sprite) || sprite == null)
                return false;
            texture = sprite.texture;
            return texture != null;
        }

        public bool Matches(Entry[] next)
        {
            Entry[] left = entries ?? Array.Empty<Entry>();
            Entry[] right = next ?? Array.Empty<Entry>();
            if (left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (!string.Equals(left[i].Key, right[i].Key, StringComparison.Ordinal) || left[i].Sprite != right[i].Sprite)
                    return false;
            }

            return true;
        }

        public void Replace(Entry[] next)
        {
            entries = next ?? Array.Empty<Entry>();
            _lookup = null;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;
            _lookup = new System.Collections.Generic.Dictionary<string, Sprite>(StringComparer.Ordinal);
            if (entries == null)
                return;
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.IsNullOrEmpty(entries[i].Key) || entries[i].Sprite == null)
                    continue;
                _lookup[entries[i].Key] = entries[i].Sprite;
            }
        }
    }
}
