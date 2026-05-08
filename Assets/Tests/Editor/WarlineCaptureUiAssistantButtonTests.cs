using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiAssistantButtonTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Components/PREFAB-04_AssistantButton.prefab";
    private const string WaveformIconPath = "Assets/Game/Art/UI/Generated/Assistant/Icons/aria_waveform_icon.png";
    private const string StateSetPath = "Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_set.png";

    [Test]
    public void AssistantButtonAssets_HaveExpectedDimensionsAlphaAndVariance()
    {
        AssertPngAsset(WaveformIconPath, 256, 256, requireTransparentPixels: true);
        AssertPngAsset(StateSetPath, 1200, 128, requireTransparentPixels: true);
    }

    [Test]
    public void AssistantButtonPrefab_HasReusableAnimatedStateHierarchy()
    {
        GameObject prefab = LoadPrefab();
        AssistantButtonView view = prefab.GetComponent<AssistantButtonView>();
        Assert.NotNull(view);
        Assert.NotNull(view.Button);
        Assert.NotNull(prefab.GetComponent<Animator>());
        Assert.AreEqual(Selectable.Transition.Animation, view.Button.transition);
        Assert.NotNull(view.StateBackground);
        Assert.NotNull(view.WaveformIcon);
        Assert.NotNull(view.LabelText);
        Assert.NotNull(view.StateText);
        Assert.NotNull(view.CueText);
        Assert.AreEqual("ARIA", view.LabelText.text);
        Assert.AreEqual("IDLE", view.StateText.text);
        Assert.AreEqual(WaveformIconPath, AssetDatabase.GetAssetPath(view.WaveformIcon.sprite));

        var serializedObject = new SerializedObject(view);
        SerializedProperty stateSprites = serializedObject.FindProperty("stateSprites");
        Assert.NotNull(stateSprites);
        Assert.AreEqual(5, stateSprites.arraySize);
        for (int i = 0; i < stateSprites.arraySize; i++)
            Assert.NotNull(stateSprites.GetArrayElementAtIndex(i).objectReferenceValue);
    }

    [Test]
    public void AssistantButtonPrefab_StatesExposeNonColorTextCues()
    {
        GameObject instance = Object.Instantiate(LoadPrefab());
        try
        {
            AssistantButtonView view = instance.GetComponent<AssistantButtonView>();
            AssertState(view, AssistantButtonVisualState.Idle, "IDLE", "~");
            AssertState(view, AssistantButtonVisualState.Recommendation, "NEXT", ">");
            AssertState(view, AssistantButtonVisualState.Critical, "WARN", "!");
            AssertState(view, AssistantButtonVisualState.Takeover, "CTRL", "[]");
            AssertState(view, AssistantButtonVisualState.Muted, "OFF", "/");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, $"Missing assistant button prefab at {PrefabPath}");
        return prefab;
    }

    private static void AssertState(AssistantButtonView view, AssistantButtonVisualState state, string expectedLabel, string expectedCue)
    {
        view.SetState(state);
        Assert.AreEqual(expectedLabel, view.StateText.text);
        Assert.AreEqual(expectedCue, view.CueText.text);
    }

    private static void AssertPngAsset(string projectRelativePath, int expectedWidth, int expectedHeight, bool requireTransparentPixels)
    {
        Assert.IsTrue(File.Exists(projectRelativePath), $"Missing PNG asset: {projectRelativePath}");
        byte[] bytes = File.ReadAllBytes(projectRelativePath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Assert.IsTrue(texture.LoadImage(bytes), $"Could not decode PNG asset: {projectRelativePath}");
            Assert.AreEqual(expectedWidth, texture.width);
            Assert.AreEqual(expectedHeight, texture.height);

            Color32[] pixels = texture.GetPixels32();
            int transparent = 0;
            int opaque = 0;
            int minLuma = 255;
            int maxLuma = 0;
            foreach (Color32 pixel in pixels)
            {
                if (pixel.a < 8)
                    transparent++;
                if (pixel.a > 160)
                    opaque++;

                int luma = (pixel.r + pixel.g + pixel.b) / 3;
                minLuma = Mathf.Min(minLuma, luma);
                maxLuma = Mathf.Max(maxLuma, luma);
            }

            if (requireTransparentPixels)
                Assert.Greater(transparent, pixels.Length / 10, "Asset must have meaningful alpha and transparent corners/empty space.");

            Assert.Greater(opaque, pixels.Length / 20, "Asset must contain visible production art.");
            Assert.Greater(maxLuma - minLuma, 80, "Asset must have enough pixel variance to be more than a flat placeholder.");
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }
}
