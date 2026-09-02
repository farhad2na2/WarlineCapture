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
        public const string RadioPortraitPath = "Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/portrait_radio_dispatch.png";
        public const string AriaPortraitPath = V3UiFoundationBuilder.SharedAriaPortraitPath;
        public const string CommanderPortraitSheetPath = "Assets/Game/Art/Narrative/FirstLaunch/Commander/commander_portrait_choices.png";

        private static readonly string[] Paths =
        {
            FramePath,
            PointerPath,
            DaliaPortraitPath,
            SamiraPortraitPath,
            RadioPortraitPath,
            AriaPortraitPath
        };

        [MenuItem("Game/Narrative/First Launch/Configure Dialogue Art")]
        public static void Configure()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < Paths.Length; i++)
                    ConfigureTexture(Paths[i]);
                ConfigureCommanderPortraitSheet();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[FirstLaunchNarrativeDialogueAssetImporter] Configured frame, pointer, and complete speaker portrait set.");
        }

        private static void ConfigureCommanderPortraitSheet()
        {
            AssetDatabase.ImportAsset(CommanderPortraitSheetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(CommanderPortraitSheetPath) as TextureImporter;
            if (importer == null)
                throw new UnityException($"Commander portrait sheet is missing or not importable: {CommanderPortraitSheetPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
#pragma warning disable CS0618
            importer.spritesheet = new[]
            {
                Portrait("commander_01", 34f, 459f, 372f, 421f),
                Portrait("commander_02", 420f, 459f, 405f, 421f),
                Portrait("commander_03", 836f, 459f, 349f, 421f),
                Portrait("commander_04", 1191f, 459f, 424f, 421f),
                Portrait("commander_05", 166f, 37f, 412f, 384f),
                Portrait("commander_06", 598f, 37f, 364f, 384f),
                Portrait("commander_07_faceless", 1002f, 37f, 419f, 384f)
            };
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static SpriteMetaData Portrait(string name, float x, float y, float width, float height)
        {
            return new SpriteMetaData
            {
                name = name,
                rect = new Rect(x, y, width, height),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
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
            bool isPortrait = path == DaliaPortraitPath ||
                              path == SamiraPortraitPath ||
                              path == RadioPortraitPath ||
                              path == AriaPortraitPath;
            importer.alphaIsTransparency = !isPortrait;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = isPortrait
                ? TextureImporterCompression.CompressedHQ
                : TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = path == FramePath || path == AriaPortraitPath ? 2048 : 512;

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
                ? new Vector4(174f, 126f, 174f, 126f)
                : Vector4.zero;
            importer.SaveAndReimport();
        }
    }
}
