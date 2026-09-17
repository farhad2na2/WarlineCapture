using System;
using System.Linq;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M01ComicVoiceImporter
    {
        public const string Root = "Assets/Game/Audio/Narrative/M01FirstContact/Voice";
        public static void Install()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in AssetDatabase.FindAssets("t:AudioClip", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .72f;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true; importer.loadInBackground = true;
                importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale=" + (path.Contains("/fa/") ? "fa-IR; delivery=fa-conversational-v1" : "en-US") + "; runtimeNetworkTts=false";
                importer.SaveAndReimport();
            }
            ApplyBindings(true);
            V3UiLocalizationCatalogBuilder.ApplyConfiguredUiTables();
            AssetDatabase.SaveAssets();
            Debug.Log("[M01ComicVoiceImport] result=Passed lines=9 clips=18 captions=2 timing=longest-locale");
        }

        public static void ApplyBindings(bool required)
        {
            if (!required && AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/en/m01-brief.line.1.wav") == null) return;
            var translations = V3UiLocalizationCatalogBuilder.ReadUiStringConfigs().Where(e => e.key.StartsWith("narrative.m01.", StringComparison.Ordinal)).ToDictionary(e => e.key);
            var locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
            var localized = new SerializedObject(locale);
            var voices = localized.FindProperty("voices"); var text = localized.FindProperty("text");
            for (int i = voices.arraySize - 1; i >= 0; i--)
                if (voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue.StartsWith("m01-", StringComparison.Ordinal)) voices.DeleteArrayElementAtIndex(i);
            for (int i = text.arraySize - 1; i >= 0; i--)
                if (text.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue.StartsWith("narrative.m01.", StringComparison.Ordinal)) text.DeleteArrayElementAtIndex(i);
            int count = 0;
            foreach (var sequence in AssetDatabase.LoadAllAssetsAtPath(M01FirstContactNarrativeConfigBuilder.NarrativePath).OfType<NarrativeSequenceConfig>())
            {
                var serialized = new SerializedObject(sequence); var states = serialized.FindProperty("states");
                for (int s = 0; s < states.arraySize; s++)
                {
                    var state = states.GetArrayElementAtIndex(s); var lines = state.FindPropertyRelative("lines"); float cursor = 0;
                    for (int i = 0; i < lines.arraySize; i++)
                    {
                        var line = lines.GetArrayElementAtIndex(i); string id = line.FindPropertyRelative("lineId").stringValue;
                        string key = line.FindPropertyRelative("textKey").stringValue;
                        var en = RequireClip(id, "en"); var fa = RequireClip(id, "fa");
                        line.FindPropertyRelative("voiceClip").objectReferenceValue = en;
                        line.FindPropertyRelative("startSeconds").floatValue = cursor;
                        cursor += Mathf.Max(en.length, fa.length) + .4f;
                        line.FindPropertyRelative("deadlineSeconds").floatValue = cursor;
                        var voice = voices.GetArrayElementAtIndex(voices.arraySize++);
                        voice.FindPropertyRelative("lineId").stringValue = id;
                        voice.FindPropertyRelative("voiceClip").objectReferenceValue = fa;
                        voice.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                        voice.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
                        var caption = text.GetArrayElementAtIndex(text.arraySize++);
                        caption.FindPropertyRelative("key").stringValue = key;
                        caption.FindPropertyRelative("value").stringValue = translations[key].translations.Single(t => t.locale == "fa-IR").value;
                        count++;
                    }
                    if (lines.arraySize > 0) state.FindPropertyRelative("durationSeconds").floatValue = cursor + .5f;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(sequence);
            }
            if (count != 9) throw new InvalidOperationException("M1 comic coverage changed: " + count);
            localized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(locale);
        }

        private static AudioClip RequireClip(string id, string locale) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/{locale}/{id}.wav") ?? throw new InvalidOperationException("Missing M1 comic voice: " + locale + "/" + id);
    }
}
