using Game.Configs;
using Game.Catalog.Contracts;
using Game.UI.Runtime;
using Game.Narrative.Contracts;
using UnityEditor;
using TMPro;
using UnityEngine;

namespace Game.Editor
{
    public static class FirstLaunchSetupRetirement
    {
        [MenuItem("Game/Narrative/First Launch/Remove Retired Guidance Choice")]
        public static void Apply()
        {
            // Migrate in place so reviewed recordings, timing and panel bindings stay intact.
            var config = AssetDatabase.LoadAssetAtPath<NarrativeSequenceConfig>(FirstLaunchNarrativeConfigBuilder.SequencePath);
            var serialized = new SerializedObject(config);
            var states = serialized.FindProperty("states");
            for (int i = states.arraySize - 1; i >= 0; i--)
            {
                var state = states.GetArrayElementAtIndex(i);
                if (state.FindPropertyRelative("kind").intValue == (int)NarrativeStateKind.InteractiveGuidance)
                    states.DeleteArrayElementAtIndex(i);
                else if (state.FindPropertyRelative("continueStateId").stringValue == "first_launch.guidance_choice")
                    state.FindPropertyRelative("continueStateId").stringValue = "FL-P09";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            string path = FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var retired = root.transform.Find("SafeArea/GuidanceChoiceSurface");
                if (retired != null) Object.DestroyImmediate(retired.gameObject);
                var view = new SerializedObject(root.GetComponent<NarrativeSequenceView>());
                view.FindProperty("guidanceChoiceView").objectReferenceValue = null;
                var step = root.transform.Find("SafeArea/CommanderIdentitySurface/AuthenticationHeader/Step")?.GetComponent<TMP_Text>();
                if (step != null)
                {
                    step.text = "2 / 2";
                    var labels = view.FindProperty("localizedTextTargets");
                    for (int i = 0; i < labels.arraySize; i++)
                        if (labels.GetArrayElementAtIndex(i).objectReferenceValue == step)
                            labels.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
                view.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[FirstLaunchSetupRetirement] result=Passed guidance screen removed; story/audio preserved");
        }
    }
}
