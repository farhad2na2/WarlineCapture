namespace Game.Tests.Editor
{
    using System.IO;
    using Game.Editor;
    using NUnit.Framework;
    using UnityEditor;

    public sealed class Aph503TextureStreamingGuardTests
    {
        [TestCase("Assets/Game/Art/UI/Icons/Move.png", TextureImporterType.Sprite, "sprite/UI")]
        [TestCase("Assets/Game/Art/UI/Backgrounds/Command.png", TextureImporterType.Default, "UI texture")]
        [TestCase("Assets/Game/Fonts/WarlineSDF.png", TextureImporterType.Default, "font")]
        [TestCase("Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.png", TextureImporterType.Default, "animation-data")]
        [TestCase("Assets/Game/Art/SpriteAtlases/CommandAtlas.png", TextureImporterType.Default, "sprite-atlas")]
        [TestCase("Assets/Game/Art/Generated/Match/TargetLockV03.png", TextureImporterType.Default, "reference/source")]
        [TestCase("Assets/Design/References/MainMenuMockup.png", TextureImporterType.Default, "reference/source")]
        public void ProtectedTextureClassesDisableMipStreaming(
            string path,
            TextureImporterType textureType,
            string expectedReason)
        {
            TextureStreamingGuardDecision decision = Aph503TextureStreamingGuard.Evaluate(path, textureType);

            Assert.That(decision.MustDisableStreaming, Is.True, path);
            StringAssert.Contains(expectedReason, decision.Reason);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Packages/com.warline/Texture.png")]
        [TestCase("Assets/Game/Textures/../UI/Unsafe.png")]
        [TestCase("Assets/Game/Textures/NoExtension")]
        public void UnclassifiablePathsFailClosedWithActionableReason(string path)
        {
            TextureStreamingGuardDecision decision = Aph503TextureStreamingGuard.Evaluate(
                path,
                TextureImporterType.Default);

            Assert.That(decision.MustDisableStreaming, Is.True);
            StringAssert.Contains("failed closed", decision.Reason);
        }

        [Test]
        public void ExplicitRuntimeImpostorAtlasAllowlistRemainsStreamingEligible()
        {
            TextureStreamingGuardDecision decision = Aph503TextureStreamingGuard.Evaluate(
                "Assets/Game/Textures/Generated/Impostors/ImpostorAtlas_Unit_Chr_Rifleman.png",
                TextureImporterType.Default);

            Assert.That(decision.MustDisableStreaming, Is.False);
            StringAssert.Contains("impostor atlas allowlist", decision.Reason);
        }

        [TestCase("Assets/Game/Textures/Terrain/Grass_Albedo.png")]
        [TestCase("Assets/Game/VFX/Textures/ExplosionSmoke.png")]
        [TestCase("Assets/Game/Textures/Generated/TerrainRuntimeAtlas.png")]
        [TestCase("Assets/Game/Art/Atlases/WorldEnvironmentAtlas.png")]
        public void OrdinaryWorldTexturesAreNotChangedByThisGuard(string path)
        {
            TextureStreamingGuardDecision decision = Aph503TextureStreamingGuard.Evaluate(
                path,
                TextureImporterType.Default);

            Assert.That(decision.MustDisableStreaming, Is.False, decision.Reason);
        }

        [Test]
        public void GuardSourceMutatesOnlyStreamingFlagDuringTexturePreprocess()
        {
            const string sourcePath = "Assets/Game/Scripts/Editor/Aph503TextureStreamingGuard.cs";
            string source = File.ReadAllText(sourcePath);

            StringAssert.Contains("AssetPostprocessor", source);
            StringAssert.Contains("OnPreprocessTexture", source);
            StringAssert.Contains("importer.streamingMipmaps = false", source);
            StringAssert.DoesNotContain("SaveAndReimport", source);
            StringAssert.DoesNotContain("AssetDatabase.ImportAsset", source);
        }
    }
}
