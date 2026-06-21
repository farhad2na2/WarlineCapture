using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class UiToolkitTargetLockStaticPreview
{
    private const string MainMenuUxmlPath = "Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml";
    private const string CommanderProfileUxmlPath = "Assets/Game/UI Toolkit/SCN03_CommanderProfileContent/SCN03_CommanderProfileContent.uxml";
    private const string CommanderProfileUssPath = "Assets/Game/UI Toolkit/SCN03_CommanderProfileContent/SCN03_CommanderProfileContent.uss";
    private const string MatchHudUxmlPath = "Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uxml";
    private const string MatchHudUssPath = "Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uss";
    private const string BuildDrawerUxmlPath = "Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uxml";
    private const string BuildDrawerUssPath = "Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uss";
    private const string ArmoryUxmlPath = "Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uxml";
    private const string ArmoryUssPath = "Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uss";
    private const string ArmoryArtFolderPath = "Assets/Game/Art/UI/Generated/Armory/LayeredOneGo";
    private const string CommanderProfileArtFolderPath = "Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01";
    private const string MainMenuArtFolderPath = "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites";
    private const string MatchHudArtFolderPath = "Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02";
    private const string BuildDrawerArtFolderPath = "Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo";
    private const string SplashArtFolderPath = "Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV04Imagegen/Sprites";
    private const string UiBuilderMenuPath = "Window/UI Toolkit/UI Builder";

    [MenuItem("Game/UI Toolkit/Target Lock/Open SCN-02 Main Menu Static Preview")]
    public static void OpenScn02MainMenuStaticPreview()
    {
        OpenStaticPreview(MainMenuUxmlPath, "SCN-02 Main Menu");
    }

    [MenuItem("Game/UI Toolkit/Target Lock/Open SCN-03 Commander Profile Static Preview")]
    public static void OpenScn03CommanderProfileStaticPreview()
    {
        RefreshCommanderProfilePreviewAssets();
        OpenStaticPreview(CommanderProfileUxmlPath, "SCN-03 Commander Profile");
    }

    [MenuItem("Game/UI Toolkit/Target Lock/Open SCN-08 Match HUD Static Preview")]
    public static void OpenScn08MatchHudStaticPreview()
    {
        RefreshMatchHudPreviewAssets();
        OpenStaticPreview(MatchHudUxmlPath, "SCN-08 Match HUD");
    }

    [MenuItem("Game/UI Toolkit/Target Lock/Open SCN-09 Build Drawer Static Preview")]
    public static void OpenScn09BuildDrawerStaticPreview()
    {
        RefreshBuildDrawerPreviewAssets();
        OpenStaticPreview(BuildDrawerUxmlPath, "SCN-09 Build Drawer");
    }

    [MenuItem("Game/UI Toolkit/Target Lock/Open SCN-19 Armory Static Preview")]
    public static void OpenScn19ArmoryStaticPreview()
    {
        RefreshArmoryPreviewAssets();
        OpenStaticPreview(ArmoryUxmlPath, "SCN-19 Armory");
    }

    private static void RefreshArmoryPreviewAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ArmoryArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MainMenuArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MatchHudArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(SplashArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ArmoryUssPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ArmoryUxmlPath, ImportAssetOptions.ForceUpdate);
    }

    private static void RefreshCommanderProfilePreviewAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(CommanderProfileArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MainMenuArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ArmoryArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(CommanderProfileUssPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(CommanderProfileUxmlPath, ImportAssetOptions.ForceUpdate);
    }

    private static void RefreshMatchHudPreviewAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MatchHudArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MainMenuArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MatchHudUssPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MatchHudUxmlPath, ImportAssetOptions.ForceUpdate);
    }

    private static void RefreshBuildDrawerPreviewAssets()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(BuildDrawerArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(MatchHudArtFolderPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(BuildDrawerUssPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(BuildDrawerUxmlPath, ImportAssetOptions.ForceUpdate);
    }

    private static void OpenStaticPreview(string uxmlPath, string label)
    {
        VisualTreeAsset visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        if (visualTreeAsset == null)
            throw new InvalidOperationException($"Missing {label} UXML at {uxmlPath}.");

        Selection.activeObject = visualTreeAsset;
        EditorGUIUtility.PingObject(visualTreeAsset);

        bool openedBuilder = EditorApplication.ExecuteMenuItem(UiBuilderMenuPath);
        AssetDatabase.OpenAsset(visualTreeAsset);
        EditorApplication.delayCall += () =>
        {
            Selection.activeObject = null;
            SceneView.RepaintAll();
            EditorWindow.focusedWindow?.Repaint();
        };

        Debug.Log(
            $"[UiToolkitTargetLockStaticPreview] Opened {label} for static UI Builder review. " +
            $"target=4800x2160 uiBuilderMenuOpened={openedBuilder} playMode={EditorApplication.isPlaying}");
    }
}
