using Game.Composition;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeMenuSceneInstaller
    {
        public const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

        [MenuItem("Game/Narrative/First Launch/Install Into Menu Scene")]
        public static void Install()
        {
            // V3 is the production first-launch presentation. Building only the legacy
            // base prefab here reintroduced old chrome whenever the Menu scene was
            // reinstalled and left stale instance overrides in player-facing captures.
            FirstLaunchNarrativeV3PrefabBuilder.Build();
            FirstLaunchNarrativeConfigBuilder.Build();
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            MenuBootstrapView bootstrap = Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null || bootstrap.UiCanvas == null)
                throw new UnityException("Menu scene is missing MenuBootstrapView or its UI Canvas.");

            Transform existing = bootstrap.UiCanvas.transform.Find("NarrativeLayer");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
            Transform existingLanguage = bootstrap.UiCanvas.transform.Find("FirstLaunchLanguageLayer");
            if (existingLanguage != null)
                Object.DestroyImmediate(existingLanguage.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, bootstrap.UiCanvas.transform) as GameObject;
            if (instance == null)
                throw new UnityException("Failed to instantiate FirstLaunch narrative prefab.");
            instance.name = "NarrativeLayer";
            RectTransform rect = instance.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();

            GameObject languagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.LanguageChoicePrefabPath);
            GameObject languageInstance = PrefabUtility.InstantiatePrefab(languagePrefab, bootstrap.UiCanvas.transform) as GameObject;
            if (languageInstance == null)
                throw new UnityException("Failed to instantiate FirstLaunch language choice prefab.");
            languageInstance.name = "FirstLaunchLanguageLayer";
            RectTransform languageRect = languageInstance.GetComponent<RectTransform>();
            languageRect.anchorMin = Vector2.zero;
            languageRect.anchorMax = Vector2.one;
            languageRect.offsetMin = Vector2.zero;
            languageRect.offsetMax = Vector2.zero;
            languageRect.SetAsLastSibling();

            SerializedObject serialized = new(bootstrap);
            Set(serialized, "firstLaunchNarrativeView", instance.GetComponent<NarrativeSequenceView>());
            Set(serialized, "firstLaunchNarrativeConfig", AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath));
            Set(serialized, "firstLaunchSpeakerCatalog", AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath));
            Set(serialized, "firstLaunchPunctuationProfile", AssetDatabase.LoadAssetAtPath<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath));
            Set(serialized, "firstLaunchLanguageChoiceView", languageInstance.GetComponent<FirstLaunchLanguageChoiceView>());
            Set(serialized, "firstLaunchPersianLocale", AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(FirstLaunchNarrativeConfigBuilder.PersianLocalePath));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (bootstrap.ShellEcsPresentation == null || bootstrap.ShellView == null ||
                !bootstrap.ShellView.TryGetRegion(UIShellRegionId.PopupLayer, out UIShellRegionView popupRegion))
                throw new UnityException("Menu scene is missing its shell presentation or popup region.");
            CampaignMissionHudResultBinder resultBinder =
                bootstrap.ShellEcsPresentation.GetComponent<CampaignMissionHudResultBinder>() ??
                bootstrap.ShellEcsPresentation.gameObject.AddComponent<CampaignMissionHudResultBinder>();
            GameObject resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/UI/Popups/MissionResultPopup.prefab");
            if (popupRegion.ContentRoot == null || resultPrefab == null)
                throw new UnityException("Menu popup region or mission result prefab is not configured.");
            resultBinder.Configure(popupRegion.ContentRoot, resultPrefab, popupRegion);
            SerializedObject shellPresentation = new(bootstrap.ShellEcsPresentation);
            Set(shellPresentation, "missionHudResultBinder", resultBinder);
            shellPresentation.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[FirstLaunchNarrativeMenuSceneInstaller] FirstLaunch narrative installed as the final Menu canvas layer.");
        }

        private static void Set(SerializedObject serialized, string name, Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new UnityException($"Missing MenuBootstrapView serialized property {name}.");
            property.objectReferenceValue = value;
        }
    }
}
