using System;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M02EstablishBaseNarrativeLocaleBuilder
    {
        public const string PersianLocalePath =
            "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchPersianLocale.asset";

        public static void BuildPersianLocale()
        {
            NarrativeLocaleConfig locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(PersianLocalePath);
            if (locale == null)
                throw new InvalidOperationException($"Missing Persian narrative locale: {PersianLocalePath}");

            SerializedObject serialized = new(locale);
            SerializedProperty text = serialized.FindProperty("text");
            SerializedProperty voices = serialized.FindProperty("voices");
            RemoveM02Text(text);
            RemoveM02Voices(voices);

            foreach (M02NarrativeLocalizedLine line in M02EstablishBaseNarrativeVoiceImporter.AllLines())
            {
                int textIndex = text.arraySize++;
                SerializedProperty textEntry = text.GetArrayElementAtIndex(textIndex);
                textEntry.FindPropertyRelative("key").stringValue = line.TextKey;
                textEntry.FindPropertyRelative("value").stringValue = line.Persian;

                int voiceIndex = voices.arraySize++;
                SerializedProperty voiceEntry = voices.GetArrayElementAtIndex(voiceIndex);
                voiceEntry.FindPropertyRelative("lineId").stringValue = line.LineId;
                voiceEntry.FindPropertyRelative("voiceClip").objectReferenceValue =
                    RequireClip(M02EstablishBaseNarrativeVoiceImporter.GetPersianClipPath(line.LineId));
                voiceEntry.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                voiceEntry.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(locale);
            AssetDatabase.SaveAssets();
            Debug.Log("[M02EstablishBaseNarrativeLocaleBuilder] result=Passed locale=fa-IR text=9 voices=9");
        }

        private static void RemoveM02Text(SerializedProperty entries)
        {
            for (int index = entries.arraySize - 1; index >= 0; index--)
            {
                string key = entries.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue;
                if (key.StartsWith("narrative.m02.", StringComparison.Ordinal))
                    entries.DeleteArrayElementAtIndex(index);
            }
        }

        private static void RemoveM02Voices(SerializedProperty entries)
        {
            for (int index = entries.arraySize - 1; index >= 0; index--)
            {
                string lineId = entries.GetArrayElementAtIndex(index).FindPropertyRelative("lineId").stringValue;
                if (lineId.StartsWith("m02-", StringComparison.Ordinal))
                    entries.DeleteArrayElementAtIndex(index);
            }
        }

        private static AudioClip RequireClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            return clip != null ? clip : throw new InvalidOperationException($"Missing M02 Persian voice: {path}");
        }
    }
}
