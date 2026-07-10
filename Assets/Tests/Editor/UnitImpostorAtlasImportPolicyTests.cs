using NUnit.Framework;
using UnityEditor;

public sealed class UnitImpostorAtlasImportPolicyTests
{
    private const string ImpostorAtlasFolder = "Assets/Game/Textures/Generated/Impostors";

    [Test]
    public void ExistingAtlasesUseBoundedAndroidImportPolicy()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ImpostorAtlasFolder });
        Assert.That(guids.Length, Is.GreaterThan(0), "No generated impostor atlases were found.");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, $"Missing texture importer for {path}.");
            Assert.That(importer.mipmapEnabled, Is.True, $"Mipmaps must remain enabled for {path}.");
            Assert.That(importer.streamingMipmaps, Is.True, $"Mip streaming eligibility must be enabled for {path}.");
            Assert.That(importer.ignoreMipmapLimit, Is.True, $"Character impostors must preserve their bounded 1024 Android atlas quality: {path}.");

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True, $"Android override is missing for {path}.");
            Assert.That(android.maxTextureSize, Is.LessThanOrEqualTo(1024), $"Android atlas is oversized: {path}.");
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6), $"Android atlas is not ASTC 6x6: {path}.");
        }
    }
}
