using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Runtime;

namespace Game.Editor
{
    public static class M01HudResultPrefabBuilder
    {
        private const string AppCanvasPath = "Assets/Game/Prefabs/UI/Shell/UIShellAppCanvas.prefab";
        private const string HudPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string ResultPath = "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab";
        private const string Marker = "[M01HudResultPrefabBuilder] result=Passed prefabs=3";

        public static void RunFocusedValidation()
        {
            ConfigureResult();
            ConfigureHud();
            ConfigureCanvas();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log(Marker);
        }

        private static void ConfigureResult()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ResultPath);
            try
            {
                MissionResultPopupView view = root.GetComponent<MissionResultPopupView>() ??
                                              root.AddComponent<MissionResultPopupView>();
                Transform continueRoot = Find(root.transform, "ContinueButton");
                Transform retryRoot = Find(root.transform, "ReplayButton");
                RectTransform continueRect = continueRoot.GetComponent<RectTransform>();
                RectTransform retryRect = retryRoot.GetComponent<RectTransform>();
                retryRect.anchorMin = continueRect.anchorMin;
                retryRect.anchorMax = continueRect.anchorMax;
                retryRect.pivot = continueRect.pivot;
                retryRect.anchoredPosition = continueRect.anchoredPosition;
                retryRect.sizeDelta = continueRect.sizeDelta;
                TMP_Text missionName = Component<TMP_Text>(root.transform, "MissionNameText");
                missionName.enableAutoSizing = true;
                missionName.fontSizeMin = 18f;
                TMP_Text summary = Component<TMP_Text>(root.transform, "ConsequenceText");
                summary.color = new Color32(205, 233, 225, 255);
                summary.enableAutoSizing = true;
                summary.fontSizeMin = 18f;
                Image consequenceBackground = Find(root.transform, "ConsequenceRow").GetComponent<Image>();
                if (consequenceBackground != null)
                    consequenceBackground.color = new Color32(20, 27, 29, 245);
                Transform rewardsPanel = Find(root.transform, "RewardsPanel");
                TMP_Text rewards = rewardsPanel.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(candidate => candidate.name == "AuthoritativeRewardsText");
                if (rewards == null)
                {
                    TMP_Text template = ChildComponent<TMP_Text>(root.transform, "CreditsReward", "ValueText");
                    rewards = UnityEngine.Object.Instantiate(template, rewardsPanel, false);
                    rewards.name = "AuthoritativeRewardsText";
                }
                RectTransform rewardsRect = rewards.rectTransform;
                rewardsRect.anchorMin = new Vector2(0.18f, 0.08f);
                rewardsRect.anchorMax = new Vector2(0.98f, 0.92f);
                rewardsRect.offsetMin = Vector2.zero;
                rewardsRect.offsetMax = Vector2.zero;
                rewards.alignment = TextAlignmentOptions.Center;
                rewards.enableAutoSizing = true;
                rewards.fontSizeMin = 16f;
                rewards.fontSizeMax = 34f;
                rewards.overflowMode = TextOverflowModes.Ellipsis;
                Transform objectivesPanel = Find(root.transform, "ObjectivesPanel");
                foreach (string rowName in new[]
                         {
                             "Objective_DestroyHostilePatrol", "Objective_KeepCommandSquadAlive",
                             "Objective_CityConsequenceNeutral"
                         })
                    Find(root.transform, rowName).gameObject.SetActive(false);
                TMP_Text stats = objectivesPanel.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(candidate => candidate.name == "AuthoritativeStatsText");
                if (stats == null)
                {
                    stats = UnityEngine.Object.Instantiate(rewards, objectivesPanel, false);
                    stats.name = "AuthoritativeStatsText";
                }
                RectTransform statsRect = stats.rectTransform;
                statsRect.anchorMin = new Vector2(0.08f, 0.08f);
                statsRect.anchorMax = new Vector2(0.98f, 0.92f);
                statsRect.offsetMin = Vector2.zero;
                statsRect.offsetMax = Vector2.zero;
                stats.alignment = TextAlignmentOptions.Center;
                stats.fontSizeMin = 14f;
                stats.fontSizeMax = 28f;
                TMP_Text statsTitle = objectivesPanel.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(candidate => candidate.name == "SectionTitleText");
                if (statsTitle != null) statsTitle.text = "MISSION STATS";
                view.Configure(
                    Component<TMP_Text>(root.transform, "TitleText"),
                    missionName,
                    summary,
                    ChildComponent<TMP_Text>(root.transform, "MissionMetaText", null),
                    ChildComponent<TMP_Text>(root.transform, "UnitsLostCard", "ValueText"),
                    ChildComponent<TMP_Text>(root.transform, "EnemiesDefeatedCard", "ValueText"),
                    rewards,
                    stats,
                    new[] { Find(root.transform, "Star_1").gameObject,
                            Find(root.transform, "Star_2").gameObject,
                            Find(root.transform, "Star_3").gameObject },
                    continueRoot.GetComponent<Button>(),
                    continueRoot.GetComponentInChildren<TMP_Text>(true),
                    retryRoot.GetComponent<Button>(),
                    retryRoot.GetComponentInChildren<TMP_Text>(true),
                    new[]
                    {
                        Find(root.transform, "CommanderXpReward").gameObject,
                        Find(root.transform, "CreditsReward").gameObject,
                        Find(root.transform, "MaterialsReward").gameObject,
                        Find(root.transform, "IntelReward").gameObject
                    });
                PrefabUtility.SaveAsPrefabAsset(root, ResultPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureHud()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
            try
            {
                MatchHudObjectivesElapsedView view = root.GetComponentInChildren<MatchHudObjectivesElapsedView>(true);
                if (view == null) throw new InvalidOperationException("HUD elapsed view missing.");
                SerializedObject serialized = new(view);
                serialized.FindProperty("elapsedText").objectReferenceValue =
                    Find(root.transform, "Elapsed").GetComponent<TMP_Text>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureCanvas()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(AppCanvasPath);
            try
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                CampaignMissionHudResultBinder binder = root.GetComponent<CampaignMissionHudResultBinder>() ??
                                                        root.AddComponent<CampaignMissionHudResultBinder>();
                GameObject popup = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPath);
                binder.Configure(Find(root.transform, "ModalOverlay").GetComponent<RectTransform>(), popup);
                PrefabUtility.SaveAsPrefabAsset(root, AppCanvasPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Validate()
        {
            GameObject result = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPath);
            GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            GameObject canvas = AssetDatabase.LoadAssetAtPath<GameObject>(AppCanvasPath);
            if (result == null || result.GetComponent<MissionResultPopupView>() == null ||
                hud == null || hud.GetComponentInChildren<MatchHudObjectivesElapsedView>(true) == null ||
                canvas == null || canvas.GetComponent<CampaignMissionHudResultBinder>() == null ||
                canvas.GetComponentsInChildren<MonoBehaviour>(true).Any(component => component == null))
                throw new InvalidOperationException("M01 HUD/result prefab validation failed.");
        }

        private static Transform Find(Transform root, string name)
        {
            Transform found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == name);
            return found != null ? found : throw new InvalidOperationException($"Missing {name}.");
        }

        private static T Component<T>(Transform root, string name) where T : Component =>
            Find(root, name).GetComponent<T>() ?? throw new InvalidOperationException($"Missing {typeof(T).Name} on {name}.");

        private static T ChildComponent<T>(Transform root, string parent, string child) where T : Component
        {
            Transform parentTransform = Find(root, parent);
            if (string.IsNullOrEmpty(child))
                return parentTransform.GetComponent<T>() ?? parentTransform.GetComponentInChildren<T>(true);
            Transform childTransform = parentTransform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == child);
            return childTransform != null ? childTransform.GetComponent<T>() : null;
        }
    }
}
