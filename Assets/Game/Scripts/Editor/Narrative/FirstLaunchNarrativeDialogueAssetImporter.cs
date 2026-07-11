using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeDialogueAssetImporter
    {
        public const string FramePath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Frames/dialogue_frame_body.png";
        public const string PointerPath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Frames/dialogue_pointer_right.png";
        public const string DaliaPortraitPath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_dalia.png";
        public const string SamiraPortraitPath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_samira.png";
        public const string AriaIconPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_focus_reticle.png";

        private static readonly string[] Paths =
        {
            FramePath,
            PointerPath,
            DaliaPortraitPath,
            SamiraPortraitPath,
            AriaIconPath
        };

        [MenuItem("Game/Narrative/First Launch/Configure Dialogue Art")]
        public static void Configure()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < Paths.Length; i++)
                    ConfigureTexture(Paths[i]);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[FirstLaunchNarrativeDialogueAssetImporter] Configured frame, pointer, portraits, and production ARIA icon.");
        }

        private static void ConfigureTexture(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new UnityException($"Narrative dialogue texture is missing or not importable: {path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = path == FramePath ? 2048 : 512;

            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            if (path == PointerPath)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.05f, 0.5f);
            }
            importer.SetTextureSettings(settings);

            importer.spriteBorder = path == FramePath
                ? new Vector4(150f, 125f, 150f, 125f)
                : Vector4.zero;
            importer.SaveAndReimport();
        }
    }
}
