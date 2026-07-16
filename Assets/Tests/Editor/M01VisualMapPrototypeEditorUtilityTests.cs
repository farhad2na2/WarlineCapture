using Game.Editor;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class M01VisualMapPrototypeEditorUtilityTests
{
    private static readonly string[] CaptureFileNames =
    {
        "m01_gameplay_overview.png",
        "m01_old_market_approach.png",
        "m01_bombing_aftermath.png",
        "m01_top_down_plan.png",
        "m01_visual_prototype_contact_sheet.png"
    };

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new M01VisualMapPrototypeEditorUtilityTests();
            tests.RequiredPaletteAssets_AreAvailable();
            passed++;
            tests.GeneratedScene_HasOwnedRootsAnchorsAndReferenceDensity();
            passed++;
            tests.ReviewCaptureSet_IsCompleteAndReadable();
            passed++;

            Debug.Log($"[M01VisualMapValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[M01VisualMapValidation] result=Failed passed={passed}");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RequiredPaletteAssets_AreAvailable()
    {
        CollectionAssert.IsEmpty(M01VisualMapPrototypeEditorUtility.ValidateRequiredAssets());
    }

    [Test]
    public void GeneratedScene_HasOwnedRootsAnchorsAndReferenceDensity()
    {
        Assert.NotNull(
            AssetDatabase.LoadAssetAtPath<SceneAsset>(M01VisualMapPrototypeEditorUtility.ScenePath),
            "The generated M01 visual prototype scene must be tracked.");

        Scene scene = EditorSceneManager.OpenScene(M01VisualMapPrototypeEditorUtility.ScenePath, OpenSceneMode.Single);
        GameObject root = FindSceneObject(scene, "M01_VisualPrototype_Root");
        Assert.NotNull(root);
        Assert.NotNull(FindDescendant(root.transform, "_M01VisualGenerated"));
        Assert.NotNull(FindDescendant(root.transform, "_M01AuthoredStoryOverrides"));
        Assert.NotNull(FindDescendant(root.transform, "_M01ReviewCameras"));
        Assert.NotNull(FindDescendant(root.transform, "OldMarketClockTower"));
        Assert.NotNull(FindDescendant(root.transform, "DestroyedAidTruck"));
        Assert.NotNull(FindDescendant(root.transform, "M01_Review_GameplayOverview"));
        Assert.NotNull(FindDescendant(
            root.transform,
            $"GENERATOR_{M01VisualMapPrototypeEditorUtility.GeneratorVersion}_SEED_{M01VisualMapPrototypeEditorUtility.GenerationSeed}"));
        Assert.GreaterOrEqual(root.GetComponentsInChildren<Renderer>(true).Length, 900);
    }

    [Test]
    public void ReviewCaptureSet_IsCompleteAndReadable()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string captureRoot = Path.Combine(projectRoot, M01VisualMapPrototypeEditorUtility.CaptureDirectory);
        for (int i = 0; i < CaptureFileNames.Length; i++)
        {
            string path = Path.Combine(captureRoot, CaptureFileNames[i]);
            Assert.IsTrue(File.Exists(path), $"Missing M01 review capture: {CaptureFileNames[i]}");

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), $"Unreadable M01 review capture: {CaptureFileNames[i]}");
                int expectedWidth = CaptureFileNames[i].Contains("contact_sheet", StringComparison.Ordinal) ? 3254 : 1600;
                int expectedHeight = CaptureFileNames[i].Contains("contact_sheet", StringComparison.Ordinal) ? 1854 : 900;
                Assert.AreEqual(expectedWidth, texture.width, CaptureFileNames[i]);
                Assert.AreEqual(expectedHeight, texture.height, CaptureFileNames[i]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        string manifestPath = Path.Combine(captureRoot, "m01_visual_prototype_capture_manifest.md");
        Assert.IsTrue(File.Exists(manifestPath), "The M01 capture manifest must accompany the review images.");
        string manifest = File.ReadAllText(manifestPath);
        StringAssert.Contains(M01VisualMapPrototypeEditorUtility.GeneratorVersion, manifest);
        StringAssert.Contains(M01VisualMapPrototypeEditorUtility.GenerationSeed.ToString(), manifest);
        StringAssert.Contains("Semantic fingerprint:", manifest);
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        IReadOnlyList<GameObject> roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Count; i++)
        {
            if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                return roots[i];
        }

        return null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (string.Equals(transforms[i].name, name, StringComparison.Ordinal))
                return transforms[i];
        }

        return null;
    }
}
#endif
