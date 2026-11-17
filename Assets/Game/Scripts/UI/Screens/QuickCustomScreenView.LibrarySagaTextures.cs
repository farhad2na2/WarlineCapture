using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private const int SagaTextureCacheCapacity = 64;
        private sealed class SagaTextureEntry
        {
            public Sprite Sprite;
            public LinkedListNode<string> Recency;
        }
        private static readonly Dictionary<string, SagaTextureEntry> SagaTextureCache = new();
        private static readonly LinkedList<string> SagaTextureRecency = new();

        // Exact coordinate and colour bits preserve the original raster output.
        // Locale/selection refreshes often reconstruct identical coasts.
        private static string SagaTextureKey(string kind, float a, float b, Color color,
            params Vector2[][] polygons)
        {
            var key = new StringBuilder(kind);
            void Number(float value) => key.Append(':').Append(BitConverter.SingleToInt32Bits(value));
            Number(a); Number(b); Number(color.r); Number(color.g); Number(color.b);
            foreach (var polygon in polygons)
            {
                key.Append('|');
                if (polygon == null) continue;
                foreach (var point in polygon) { Number(point.x); Number(point.y); }
            }
            return key.ToString();
        }

        private static bool TrySagaTexture(string key, out Sprite sprite)
        {
            sprite = null;
            if (!SagaTextureCache.TryGetValue(key, out var entry)) return false;
            if (entry.Sprite == null)
            {
                SagaTextureRecency.Remove(entry.Recency);
                SagaTextureCache.Remove(key);
                return false;
            }
            SagaTextureRecency.Remove(entry.Recency);
            SagaTextureRecency.AddLast(entry.Recency);
            sprite = entry.Sprite;
            return true;
        }

        private static void RememberSagaTexture(string key, Sprite sprite)
        {
            while (SagaTextureCache.Count >= SagaTextureCacheCapacity)
            {
                string oldest = SagaTextureRecency.First.Value;
                var retired = SagaTextureCache[oldest];
                SagaTextureCache.Remove(oldest);
                SagaTextureRecency.RemoveFirst();
                ReleaseSagaTexture(retired.Sprite);
            }
            SagaTextureCache[key] = new SagaTextureEntry
            { Sprite = sprite, Recency = SagaTextureRecency.AddLast(key) };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSagaTextureCache()
        {
            foreach (var entry in SagaTextureCache.Values) ReleaseSagaTexture(entry.Sprite);
            SagaTextureCache.Clear();
            SagaTextureRecency.Clear();
        }

        private static void ReleaseSagaTexture(Sprite sprite)
        {
            if (sprite == null) return;
            var texture = sprite.texture;
            if (Application.isPlaying)
            { UnityEngine.Object.Destroy(sprite); UnityEngine.Object.Destroy(texture); }
            else
            { UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static void PlaceSagaTerrainShade(RectTransform parent, Sprite sprite)
        {
            var shade = new GameObject("TerrainShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image))
                .GetComponent<UnityEngine.UI.Image>();
            shade.transform.SetParent(parent, false);
            shade.sprite = sprite;
            shade.type = UnityEngine.UI.Image.Type.Simple;
            shade.color = Color.white;
            shade.raycastTarget = false;
            Stretch(shade.rectTransform);
            shade.rectTransform.localScale = Vector3.one;
        }
    }
}
