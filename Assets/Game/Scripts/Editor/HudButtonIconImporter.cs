using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class HudButtonIconImporter
    {
        public static void Configure()
        {
            const string path="Assets/Game/Resources/HudButtonIcons/hud-action-icons-v01.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new System.InvalidOperationException("HUD action icon sheet is not imported");
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
    }
}
