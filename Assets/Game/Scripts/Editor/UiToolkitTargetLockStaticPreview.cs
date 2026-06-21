using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class UiToolkitTargetLockStaticPreview
{
    private const string MainMenuUxmlPath = "Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml";
    private const string UiBuilderMenuPath = "Window/UI Toolkit/UI Builder";

    [MenuItem("Game/UI Toolkit/Target Lock/Open SCN-02 Main Menu Static Preview")]
    public static void OpenScn02MainMenuStaticPreview()
    {
        VisualTreeAsset mainMenuAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxmlPath);
        if (mainMenuAsset == null)
            throw new InvalidOperationException($"Missing SCN-02 UXML at {MainMenuUxmlPath}.");

        Selection.activeObject = mainMenuAsset;
        EditorGUIUtility.PingObject(mainMenuAsset);

        bool openedBuilder = EditorApplication.ExecuteMenuItem(UiBuilderMenuPath);
        AssetDatabase.OpenAsset(mainMenuAsset);

        Debug.Log(
            $"[UiToolkitTargetLockStaticPreview] Opened SCN-02 Main Menu for static UI Builder review. " +
            $"target=4800x2160 uiBuilderMenuOpened={openedBuilder} playMode={EditorApplication.isPlaying}");
    }
}
