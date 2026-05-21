using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureScn02LayerCanvasBuilderTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_LayerCanvasTest.prefab";
    private const string SettingsGearPath = "Assets/Game/Art/UI/Generated/MainMenu/SourceAssetsBatch01/Icons/settings_gear_icon.png";
    private const string TopResourceFramePath = "Assets/Game/Art/UI/Generated/MainMenu/SourceAssetsBatch01/Frames/top_resource_bar_frame_full.png";

    [Test]
    public void BuildLayerCanvasTest_CreatesPrefabAndAlphaSprites()
    {
        WarlineCaptureScn02LayerCanvasBuilder.BuildLayerCanvasTest();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        RectTransform rootRect = prefab.GetComponent<RectTransform>();
        Assert.NotNull(rootRect);
        Assert.AreEqual(new Vector2(3840f, 2160f), rootRect.sizeDelta);

        Assert.NotNull(prefab.transform.Find("GeneratedLayerArtRoot/TopResourceBarFrameFull"));
        Assert.NotNull(prefab.transform.Find("GeneratedLayerArtRoot/SettingsGearIcon"));
        Assert.NotNull(prefab.transform.Find("GeneratedLayerArtRoot/DeployCommandButtonFrame"));
        Assert.NotNull(prefab.transform.Find("LiveTextRoot/DeployCommandLabel"));

        AssertImportedSprite(TopResourceFramePath);
        AssertImportedSprite(SettingsGearPath);
        AssertHasTransparentPixels(SettingsGearPath);

        Image topFrame = prefab.transform.Find("GeneratedLayerArtRoot/TopResourceBarFrameFull").GetComponent<Image>();
        Assert.NotNull(topFrame.sprite);
        Assert.AreEqual(TopResourceFramePath, AssetDatabase.GetAssetPath(topFrame.sprite));
        Assert.AreEqual(Image.Type.Simple, topFrame.type, "Top bar is intentionally fixed, not sliced or stretched from repeated slots.");
    }

    private static void AssertImportedSprite(string path)
    {
        Assert.IsTrue(File.Exists(path), path);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Assert.NotNull(sprite, path);
    }

    private static void AssertHasTransparentPixels(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Assert.IsTrue(texture.LoadImage(bytes), path);
        Color32[] pixels = texture.GetPixels32();
        Object.DestroyImmediate(texture);

        bool hasTransparent = false;
        foreach (Color32 pixel in pixels)
        {
            if (pixel.a < 12)
            {
                hasTransparent = true;
                break;
            }
        }

        Assert.IsTrue(hasTransparent, $"{path} should have transparent pixels after chroma-key conversion.");
    }
}
