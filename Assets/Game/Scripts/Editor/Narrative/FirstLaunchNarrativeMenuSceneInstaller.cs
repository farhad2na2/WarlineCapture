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
            FirstLaunchNarrativePresentationPrefabBuilder.Build();
            FirstLaunchNarrativeConfigBuilder.Build();
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            MenuBootstrapView bootstrap = Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null || bootstrap.UiCanvas == null)
                throw new UnityException("Menu scene is missing MenuBootstrapView or its UI Canvas.");

            Transform existing = bootstrap.UiCanvas.transform.Find("NarrativeLayer");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

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

            SerializedObject serialized = new(bootstrap);
            Set(serialized, "firstLaunchNarrativeView", instance.GetComponent<NarrativeSequenceView>());
            Set(serialized, "firstLaunchNarrativeConfig", AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath));
            Set(serialized, "firstLaunchSpeakerCatalog", AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath));
            Set(serialized, "firstLaunchPunctuationProfile", AssetDatabase.LoadAssetAtPath<NarrativePunctuationProfile>(FirstLaunchNarrativeConfigBuilder.PunctuationPath));
            serialized.ApplyModifiedPropertiesWithoutUndo();

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
