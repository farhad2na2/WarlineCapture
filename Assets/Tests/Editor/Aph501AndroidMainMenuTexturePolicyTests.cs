namespace Game.Tests.Editor
{
    using System;
    using System.Collections.Generic;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEditor;

    public sealed class Aph501AndroidMainMenuTexturePolicyTests
    {
        private const int ExpectedTrackedSpriteCount = 60;

        [TestCase("Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/scn02c_mode_card_frame_selected.png")]
        [TestCase("Assets\\Game\\Art\\UI\\Generated\\MainMenuBrightCommand\\Sprites\\scn02c_deploy_button.png")]
        public void ExactMainMenuSpriteFolderIsTracked(string path)
        {
            Assert.That(Aph501AndroidMainMenuTexturePolicy.IsTrackedSpritePath(path), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/Nested/frame.png")]
        [TestCase("Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites/frame.png.meta")]
        [TestCase("Assets/Game/Art/UI/Generated/MainMenuV15C/Sprites/frame.png")]
        [TestCase("Assets/Game/Art/UI/Generated/MatchHUD/Sprites/frame.png")]
        [TestCase("Assets/Game/Art/UI/Generated/FirstLaunch/Sprites/frame.png")]
        [TestCase("Assets/Game/Art/UI/Generated/MainMenuBrightCommand/TargetLock/reference.png")]
        [TestCase("Assets/Game/Art/OperationMap/ground.png")]
        public void UnrelatedAssetsAreNotTracked(string path)
        {
            Assert.That(Aph501AndroidMainMenuTexturePolicy.IsTrackedSpritePath(path), Is.False);
        }

        [Test]
        public void ExistingSpritesUseExactAndroidAstcPolicy()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { Aph501AndroidMainMenuTexturePolicy.SpriteFolder });
            var paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Aph501AndroidMainMenuTexturePolicy.IsTrackedSpritePath(path))
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

                TextureImporterPlatformSettings android =
                    importer.GetPlatformTextureSettings(Aph501AndroidMainMenuTexturePolicy.AndroidPlatform);
                Assert.That(android.overridden, Is.True, path);
                Assert.That(
                    android.maxTextureSize,
                    Is.EqualTo(Aph501AndroidMainMenuTexturePolicy.AndroidMaxTextureSize),
                    path);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
                Assert.That(
                    android.textureCompression,
                    Is.EqualTo(TextureImporterCompression.CompressedHQ),
                    path);
                Assert.That(
                    android.compressionQuality,
                    Is.EqualTo(Aph501AndroidMainMenuTexturePolicy.AndroidCompressionQuality),
                    path);
            }
        }
    }
}
