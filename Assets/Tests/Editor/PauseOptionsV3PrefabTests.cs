#if UNITY_EDITOR
using System;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public sealed class PauseOptionsV3PrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Popups/PauseMenuPopup.prefab";

    public static void RunFocusedValidation()
    {
        PauseOptionsV3PrefabTests suite = new();
        int passed = 0;
        try
        {
            suite.PauseOptions_UsesTargetHierarchyAndConstantBorders();
            passed++;
            suite.PauseOptions_PreservesRealShellActionsAndRuntimeOverlays();
            passed++;
            suite.PauseOptions_UsesOnlySharedV3AtlasArt();
            passed++;
            suite.PauseOptions_AllButtonsExposePointerTargets();
            passed++;
            Game.Editor.PauseOptionsV3PrefabBuilder.Validate();
            Debug.Log($"[PauseOptionsV3PrefabTests] result=Passed tests={passed} gradients=procedural borders=3 restart=queued help=interactive");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[PauseOptionsV3PrefabTests] result=Failed tests={passed}\n{exception}");
            throw;
        }
    }

    [Test]
    public void PauseOptions_UsesTargetHierarchyAndConstantBorders()
    {
        GameObject prefab = Load();
        string[] required =
        {
            "V3Composition", "Scrim", "PauseOptionsRoot", "Header", "ActionColumn",
            "StatusColumn", "ObjectiveRow", "SquadsAliveRow", "CivilianRiskRow",
            "AutosaveRow", "RestartConfirmation", "HelpPanel"
        };
        for (int index = 0; index < required.Length; index++)
            Assert.NotNull(Find(prefab.transform, required[index]), required[index]);

        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.GreaterOrEqual(gradients.Length, 15);
        for (int index = 0; index < gradients.Length; index++)
        {
            SerializedObject serialized = new(gradients[index]);
            float width = serialized.FindProperty("borderWidth").floatValue;
            if (width > .001f)
                Assert.AreEqual(3f, width, .001f, gradients[index].name);
        }

        Assert.AreEqual(1, prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true).Length);
    }

    [Test]
    public void PauseOptions_PreservesRealShellActionsAndRuntimeOverlays()
    {
        GameObject prefab = Load();
        PauseOptionsV3PopupView view = prefab.GetComponent<PauseOptionsV3PopupView>();
        Assert.NotNull(view);
        AssertAction(view.CloseButton, UiActionKind.ClosePause);
        AssertAction(view.ResumeButton, UiActionKind.ClosePause);
        AssertAction(view.SettingsButton, UiActionKind.OpenSettings);
        AssertAction(view.ExitButton, UiActionKind.MatchMenu);
        Assert.NotNull(view.RestartButton);
        Assert.NotNull(view.HelpButton);
        Assert.NotNull(view.RestartConfirmation);
        Assert.NotNull(view.HelpPanel);
        Assert.IsFalse(view.RestartConfirmation.activeSelf);
        Assert.IsFalse(view.HelpPanel.activeSelf);
    }

    [Test]
    public void PauseOptions_UsesOnlySharedV3AtlasArt()
    {
        GameObject prefab = Load();
        Image[] images = prefab.GetComponentsInChildren<Image>(true);
        for (int index = 0; index < images.Length; index++)
        {
            Sprite sprite = images[index].sprite;
            if (sprite == null)
                continue;
            string path = AssetDatabase.GetAssetPath(sprite);
            bool approved = path.StartsWith("Assets/Game/Art/UI/V3Shared/", StringComparison.Ordinal) ||
                            path.StartsWith("Assets/Game/Art/UI/Generated/V3Shared/", StringComparison.Ordinal) ||
                            IsOwnedBySingleV3Atlas(path);
            Assert.IsTrue(approved,
                $"Pause menu image {images[index].name} uses a sprite outside the shared V3 atlas set: {path}.");
        }
    }

    [Test]
    public void PauseOptions_AllButtonsExposePointerTargets()
    {
        GameObject prefab = Load();
        Button[] buttons = prefab.GetComponentsInChildren<Button>(true);
        Assert.GreaterOrEqual(buttons.Length, 9);
        for (int index = 0; index < buttons.Length; index++)
        {
            Assert.NotNull(buttons[index].targetGraphic, buttons[index].name);
            Assert.IsTrue(buttons[index].targetGraphic.raycastTarget, buttons[index].name);
        }
    }

    private static bool IsOwnedBySingleV3Atlas(string assetPath)
    {
        int owners = 0;
        string[] atlasGuids = AssetDatabase.FindAssets(
            "t:SpriteAtlas",
            new[] { "Assets/Game/Art/UI/V3Shared/Atlases" });
        for (int atlasIndex = 0; atlasIndex < atlasGuids.Length; atlasIndex++)
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                AssetDatabase.GUIDToAssetPath(atlasGuids[atlasIndex]));
            foreach (UnityEngine.Object packable in SpriteAtlasExtensions.GetPackables(atlas))
            {
                if (!string.Equals(AssetDatabase.GetAssetPath(packable), assetPath, StringComparison.Ordinal))
                    continue;
                owners++;
                if (owners > 1)
                    return false;
            }
        }

        return owners == 1;
    }

    private static void AssertAction(Button button, UiActionKind expected)
    {
        Assert.NotNull(button);
        UIShellActionButtonView action = button.GetComponent<UIShellActionButtonView>();
        Assert.NotNull(action, button.name);
        Assert.AreEqual(expected, action.ActionKind, button.name);
        Assert.AreEqual(0, action.PayloadId, button.name);
    }

    private static GameObject Load()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, PrefabPath);
        return prefab;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = Find(root.GetChild(index), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
