using System.IO;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class OperationMapMinimapRasterBakerTests
{
    [Test]
    public void CurrentRaster_HasExpectedDimensionsAndSourceIdentity()
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            OperationMapMinimapRasterBaker.OutputPath);
        TextureImporter importer = AssetImporter.GetAtPath(
            OperationMapMinimapRasterBaker.OutputPath) as TextureImporter;
        OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            OperationMapAddressablesLayoutBuilder.DefinitionPath);
        MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
            OperationMapAddressablesLayoutBuilder.MapSurfacePath);

        Assert.That(texture, Is.Not.Null);
        Assert.That(texture.width, Is.EqualTo(OperationMapMinimapRasterBaker.Resolution));
        Assert.That(texture.height, Is.EqualTo(OperationMapMinimapRasterBaker.Resolution));
        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.isReadable, Is.False);
        Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear));
        TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
        Assert.That(android.overridden, Is.True);
        Assert.That(android.maxTextureSize, Is.EqualTo(OperationMapMinimapRasterBaker.Resolution));
        Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6));
        Assert.That(
            importer.userData,
            Is.EqualTo(OperationMapMinimapRasterBaker.BuildImporterUserData(definition, surface)));
    }

    [Test]
    public void IdenticalBake_PreservesPngBytesAndTimestamp()
    {
        string absolutePath = Path.GetFullPath(OperationMapMinimapRasterBaker.OutputPath);
        byte[] before = File.ReadAllBytes(absolutePath);
        long timestamp = File.GetLastWriteTimeUtc(absolutePath).Ticks;

        OperationMapMinimapRasterBaker.Run();

        Assert.That(File.ReadAllBytes(absolutePath), Is.EqualTo(before));
        Assert.That(File.GetLastWriteTimeUtc(absolutePath).Ticks, Is.EqualTo(timestamp));
    }
}
