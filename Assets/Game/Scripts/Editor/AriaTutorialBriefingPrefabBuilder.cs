#if UNITY_EDITOR
using System;
using System.IO;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    /// <summary>
    /// Enforces the single-panel ARIA contract. Tutorial guidance belongs to
    /// the permanent Match HUD ARIA panel; POP-13 must never contain a second
    /// tutorial surface.
    /// </summary>
    public static class AriaTutorialBriefingPrefabBuilder
    {
        public const string PrefabPath =
            "Assets/Game/Prefabs/UI/Shell/Popups/POP13_ARIACommandAssistantPopup.prefab";
        public const string PortraitPath = V3UiFoundationBuilder.SharedAriaPortraitPath;

        [MenuItem("Game/UI/Rebuild ARIA Tutorial Briefing")]
        [MenuItem("Game/UI/V3/Build Tutorial Presentation V3")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                throw new InvalidOperationException($"Missing ARIA popup prefab at {PrefabPath}.");

            try
            {
                Transform deprecatedSurface = root.transform.Find("TutorialBriefingSurface");
                if (deprecatedSurface != null)
                    Object.DestroyImmediate(deprecatedSurface.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("[AriaTutorialBriefingPrefabBuilder] result=Passed surface=embedded-match-hud actions=2 skip=absent");
        }

        public static void BuildAndValidate()
        {
            Build();
            Validate();
            Debug.Log("[AriaTutorialBriefingV3Validation] result=Passed");
        }

        [MenuItem("Game/UI/Capture ARIA Tutorial Briefing")]
        public static void CapturePreview()
        {
            MatchHudV3PrefabBuilder.CaptureTutorialPresentationReview();
        }

        public static void Validate()
        {
            GameObject popup = RequireAsset<GameObject>(PrefabPath);
            if (popup.transform.Find("TutorialBriefingSurface") != null ||
                popup.GetComponentInChildren<AriaTutorialBriefingView>(true) != null)
            {
                throw new InvalidOperationException(
                    "POP13 must not contain a second ARIA tutorial panel.");
            }

            GameObject matchHud = RequireAsset<GameObject>(MatchHudV3PrefabBuilder.PrefabPath);
            Transform aria = FindDeepChild(matchHud.transform, "AriaAssistantButton");
            AriaTutorialBriefingView view =
                aria != null ? aria.GetComponent<AriaTutorialBriefingView>() : null;
            if (view == null || !view.TryBindHierarchy())
                throw new InvalidOperationException(
                    "The permanent Match HUD ARIA panel must own the tutorial presentation.");
            if (view.CloseButton != null)
                throw new InvalidOperationException("The embedded ARIA tutorial must not contain Skip.");
            if (AssetDatabase.GetAssetPath(view.PortraitImage.sprite) != PortraitPath)
                throw new InvalidOperationException("The embedded ARIA panel must use the shared V3 portrait.");
            if (view.ShowMeButton.transform.parent != view.DoItButton.transform.parent)
                throw new InvalidOperationException("ARIA tutorial actions must share one embedded action row.");

            Debug.Log("[AriaTutorialBriefingV3Validation] result=Passed panels=1 actions=2 skip=absent portrait=shared");
        }

        public static UiAssistantPanelModel CreateTargetLockPreviewModel(bool rightToLeft = false)
        {
            return new UiAssistantPanelModel(
                1,
                false,
                0,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantTargetLockModel.Empty,
                UiAssistantNarrationModel.Empty,
                true,
                rightToLeft ? "گروه تفنگدار را انتخاب کنید" : "Select the Rifle Squad",
                rightToLeft
                    ? "برای انتخاب، روی کارت گروه تفنگدار بزنید. سپس برای حرکت به نشانگر، حرکت را بزنید."
                    : "Tap the <color=#00D1F3>Rifle Squad</color> unit card to select.\n" +
                      "Then tap <color=#00D1F3>MOVE</color> to send them to the marker.",
                "HIGH",
                "DO IT",
                true,
                true,
                false,
                false,
                "PLAYER CONTROL",
                string.Empty,
                recommendationKind: 1,
                recommendationTargetKind: 6,
                tutorialStep: 1,
                tutorialStepCount: 5,
                tutorialRightToLeft: rightToLeft);
        }

        public static UiAssistantPanelModel CreateCommandAssistantPreviewModel()
        {
            return new UiAssistantPanelModel(
                2,
                false,
                0,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantGoalRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                UiAssistantMessageRowModel.Empty,
                new UiAssistantMessageRowModel(
                    true, 30, "Hostile infantry squad detected near market stalls.",
                    "They are moving between cover positions.", 3, 1, 1, true, false),
                UiAssistantMessageRowModel.Empty,
                new UiAssistantTargetLockModel(
                    true, 2, 1, "ENEMY INFANTRY SQUAD", "RIFLE SQUAD", "140m", "HIGH",
                    "HOSTILE", "READY", "Moving between cover positions."),
                new UiAssistantNarrationModel(
                    (byte)UiAssistantNarrationStateKind.Presented, 3, "ARIA VOICE",
                    "MOVE ORDER CONFIRMED.", string.Empty, true),
                true,
                "TACTICAL REPORTS",
                "Hostile infantry squad detected near market stalls.\nThey are moving between cover positions.",
                "HIGH",
                "SHOW ME",
                true,
                false,
                false,
                false,
                "PLAYER CONTROL",
                string.Empty,
                recommendationKind: 1,
                recommendationTargetKind: 1);
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"Missing ARIA tutorial V3 asset at {path}.");
            return asset;
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root == null)
                return null;
            if (root.name == targetName)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), targetName);
                if (found != null)
                    return found;
            }
            return null;
        }

    }
}
#endif
