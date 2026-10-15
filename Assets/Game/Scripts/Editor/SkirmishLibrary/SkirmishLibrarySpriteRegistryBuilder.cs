#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Rebuilds the runtime sprite registry from preview, plate, zone and blur folders.
    /// Zone and blur PNGs are generated during Rebuild. Player builds load them through this registry.
    /// Prefab build, preview capture, and imports of those PNGs all call Rebuild.
    /// </summary>
    public static class SkirmishLibrarySpriteRegistryBuilder
    {
        public const string AssetPath = "Assets/Game/Resources/SkirmishLibrary/SkirmishLibrarySpriteRegistry.asset";
        private static bool _rebuilding;

        public static bool IsRebuilding => _rebuilding;

        [MenuItem("Game/UI/V3/Rebuild Skirmish Library Sprite Registry")]
        public static void Rebuild()
        {
            if (_rebuilding)
                return;
            _rebuilding = true;
            try
            {
                SkirmishLibraryZoneComposer.Generate();
                EnsureFolder("Assets/Game/Resources/SkirmishLibrary");
                SkirmishLibrarySpriteRegistry registry = AssetDatabase.LoadAssetAtPath<SkirmishLibrarySpriteRegistry>(AssetPath);
                if (registry == null)
                {
                    registry = ScriptableObject.CreateInstance<SkirmishLibrarySpriteRegistry>();
                    AssetDatabase.CreateAsset(registry, AssetPath);
                }

                var entries = new List<SkirmishLibrarySpriteRegistry.Entry>();
                Collect(entries, SkirmishLibraryPreviewAssets.Root + "/Previews");
                Collect(entries, SkirmishLibraryPreviewAssets.Root + "/Plates");
                Collect(entries, SkirmishLibraryPreviewAssets.Root + "/Theatre");
                Collect(entries, SkirmishLibraryPreviewAssets.Root + "/Zones");
                Collect(entries, SkirmishLibraryPreviewAssets.Root + "/Blurs");
                SkirmishLibrarySpriteRegistry.Entry[] next = entries.ToArray();
                if (registry.Matches(next))
                    return;
                registry.Replace(next);
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                _rebuilding = false;
            }
        }

        private static void Collect(List<SkirmishLibrarySpriteRegistry.Entry> entries, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absolute = Path.Combine(projectRoot ?? string.Empty, folder);
            if (!Directory.Exists(absolute))
                return;
            string[] files = Directory.GetFiles(absolute, "*.png");
            System.Array.Sort(files, System.StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = folder + "/" + Path.GetFileName(files[i]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                    continue;
                string logical = Path.GetFileNameWithoutExtension(assetPath);
                entries.Add(new SkirmishLibrarySpriteRegistry.Entry { Key = assetPath, Sprite = sprite });
                entries.Add(new SkirmishLibrarySpriteRegistry.Entry { Key = logical, Sprite = sprite });
            }
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;
            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    public sealed class SkirmishLibrarySpriteRegistryPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] imported,
            string[] deleted,
            string[] moved,
            string[] movedFrom)
        {
            if (SkirmishLibrarySpriteRegistryBuilder.IsRebuilding)
                return;
            if (!TouchesLibraryArt(imported) && !TouchesLibraryArt(deleted) &&
                !TouchesLibraryArt(moved) && !TouchesLibraryArt(movedFrom))
                return;
            SkirmishLibrarySpriteRegistryBuilder.Rebuild();
        }

        private static bool TouchesLibraryArt(string[] paths)
        {
            if (paths == null)
                return false;
            const string root = "Assets/Game/Art/SkirmishLibrary/";
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i] ?? string.Empty;
                if (!path.StartsWith(root, System.StringComparison.Ordinal))
                    continue;
                if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
#endif
