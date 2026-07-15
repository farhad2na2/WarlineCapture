namespace Game.Tests.Editor
{
    using System;
    using System.Collections.Generic;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEditor;

    public sealed class Aph501AndroidSupportingUiTexturePolicyTests
    {
        private const int ExpectedTrackedSpriteCount = 143;

        [TestCase("Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_panel_frame_large.png")]
        [TestCase("Assets/Game/Art/UI/Generated/MatchHUD/SquadTray/SquadTray_Card1_RifleSquad.png")]
        [TestCase("Assets\\Game\\Art\\UI\\Generated\\MainMenuV15C\\LayeredOneGo\\scn02_operations_thumbnail_art.png")]
        public void SupportingUiRootsAreTracked(string path)
        {
            Assert.That(Aph501AndroidSupportingUiTexturePolicy.IsTrackedSpritePath(path), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Assets/Game/Art/UI/Generated/MatchHUD/../FirstLaunch/frame.png")]
        [TestCase("Assets/Game/Art/UI/Generated/MatchHUD/frame.png.meta")]
        [TestCase("Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/frame.png")]
        [TestCase("Assets/Game/Art/UI/Generated/FirstLaunch/Sprites/frame.png")]
        [TestCase("Assets/Game/Art/OperationMap/ground.png")]
        public void UnrelatedAssetsAreNotTracked(string path)
        {
            Assert.That(Aph501AndroidSupportingUiTexturePolicy.IsTrackedSpritePath(path), Is.False);
        }

        [Test]
        public void ExistingSpritesUseExactAndroidAstcPolicy()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[]
                {
                    Aph501AndroidSupportingUiTexturePolicy.MatchHudRoot,
                    Aph501AndroidSupportingUiTexturePolicy.MainMenuV15CRoot,
                });
            var paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Aph501AndroidSupportingUiTexturePolicy.IsTrackedSpritePath(path))
                    paths.Add(path);
            }

            paths.Sort(StringComparer.Ordinal);
            Assert.That(paths.Count, Is.EqualTo(ExpectedTrackedSpriteCount));
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.mipmapEnabled, Is.False, $"UI mipmaps must be disabled: {path}");

                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(
                    Aph501AndroidSupportingUiTexturePolicy.AndroidPlatform);
                Assert.That(android.overridden, Is.True, path);
                Assert.That(
                    android.maxTextureSize,
                    Is.EqualTo(Aph501AndroidSupportingUiTexturePolicy.AndroidMaxTextureSize),
                    path);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6), path);
                Assert.That(
                    android.textureCompression,
                    Is.EqualTo(TextureImporterCompression.CompressedHQ),
                    path);
                Assert.That(
                    android.compressionQuality,
                    Is.EqualTo(Aph501AndroidSupportingUiTexturePolicy.AndroidCompressionQuality),
                    path);
            }
        }
    }
}
