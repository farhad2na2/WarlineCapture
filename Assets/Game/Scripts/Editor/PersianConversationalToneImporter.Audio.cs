using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class PersianConversationalToneImporter
    {
        [Serializable] private sealed class VoicePayload { public VoiceRequest[] requests; }
        [Serializable] private sealed class VoiceRequest { public string assetPath; public string voiceId; }

        [MenuItem("Game/Localization/Install Conversational Farsi Recordings")]
        public static void InstallAudio()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var payload = JsonUtility.FromJson<VoicePayload>(File.ReadAllText(ReportRoot + "voice_payload_review.json"));
            if (payload.requests.Length != 261) throw new InvalidOperationException("Unexpected approved voice count.");
            foreach (var request in payload.requests)
            {
                var importer = AssetImporter.GetAtPath(request.assetPath) as AudioImporter;
                if (importer == null) throw new InvalidOperationException("Missing Farsi recording: " + request.assetPath);
                // Existing import profiles are already tuned per cast; configure only newly added M4 lessons.
                if (request.assetPath.Contains("/M04Airlift/") && request.assetPath.Contains("/tutorial-m04-"))
                {
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = .72f;
                    settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    settings.sampleRateOverride = 44100;
                    settings.preloadAudioData = false;
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = true;
                    importer.loadInBackground = true;
                    importer.ambisonic = false;
                    importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; voiceId=" + request.voiceId + "; locale=fa-IR; runtimeNetworkTts=false; delivery=fa-conversational-v1";
                    importer.SaveAndReimport();
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(request.assetPath);
                if (clip == null || clip.length <= 0 || clip.channels != 1)
                    throw new InvalidOperationException("Invalid imported Farsi clip: " + request.assetPath);
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets(false);
            RefreshComicTiming();
            AssetDatabase.SaveAssets();
            ValidateAudio();
            Debug.Log("[PersianConversationalAudioInstall] result=Passed clips=261 newM04TutorialEvents=12 comicTiming=updated");
        }

        public static void ValidateAudio()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioEventCatalogConfig>(AudioRuntimeConfigAssetBuilder.EventCatalogAssetPath);
            var serialized = new SerializedObject(catalog);
            var events = serialized.FindProperty("events");
            var expected = Enumerable.Range(1, 12).ToDictionary(i => $"vo.aria.tutorial.m04.{i:00}.fa",
                i => $"Assets/Game/Audio/Narrative/M04Airlift/Voice/fa/tutorial-m04-{i:00}.wav", StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < events.arraySize; i++)
            {
                var e = events.GetArrayElementAtIndex(i);
                string id = e.FindPropertyRelative("eventId").stringValue;
                if (!expected.TryGetValue(id, out string path)) continue;
                var clips = e.FindPropertyRelative("clips");
                if (clips.arraySize != 1 || AssetDatabase.GetAssetPath(clips.GetArrayElementAtIndex(0).FindPropertyRelative("clip").objectReferenceValue) != path)
                    throw new InvalidOperationException("Wrong M4 Farsi event binding: " + id);
                expected.Remove(id);
            }
            if (expected.Count != 0) throw new InvalidOperationException("Missing Farsi M4 events: " + string.Join(",", expected.Keys));
            Debug.Log("[PersianConversationalAudioValidation] result=Passed M4TutorialBindings=12");
        }

        private static void RefreshComicTiming()
        {
            var locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
            var voices = locale.Voices.ToDictionary(v => v.LineId, v => v.VoiceClip, StringComparer.Ordinal);
            // First-launch recordings obey their existing hard timing contract. Mission panels can expand
            // to fit either language, so a natural Farsi take is never cut off by the old English duration.
            foreach (string path in AssetDatabase.FindAssets("t:NarrativeSequenceConfig", new[] { "Assets/Game/Configs/Narrative/Chapter01" })
                .Select(AssetDatabase.GUIDToAssetPath).Distinct())
            foreach (var sequence in AssetDatabase.LoadAllAssetsAtPath(path).OfType<NarrativeSequenceConfig>())
            {
                var data = new SerializedObject(sequence);
                var states = data.FindProperty("states");
                bool changed = false;
                for (int s = 0; s < states.arraySize; s++)
                {
                    var state = states.GetArrayElementAtIndex(s);
                    var lines = state.FindPropertyRelative("lines");
                    float cursor = 0;
                    for (int i = 0; i < lines.arraySize; i++)
                    {
                        var line = lines.GetArrayElementAtIndex(i);
                        string id = line.FindPropertyRelative("lineId").stringValue;
                        if (!voices.TryGetValue(id, out var persian) || persian == null) continue;
                        var english = line.FindPropertyRelative("voiceClip").objectReferenceValue as AudioClip;
                        float slot = Mathf.Max(english != null ? english.length : 0, persian.length) + (lines.arraySize > 1 ? .35f : 1f);
                        var start = line.FindPropertyRelative("startSeconds");
                        var deadline = line.FindPropertyRelative("deadlineSeconds");
                        if (lines.arraySize > 1) start.floatValue = cursor;
                        deadline.floatValue = Mathf.Max(deadline.floatValue, start.floatValue + slot);
                        cursor = deadline.floatValue + .25f;
                        state.FindPropertyRelative("durationSeconds").floatValue = Mathf.Max(state.FindPropertyRelative("durationSeconds").floatValue, cursor);
                        changed = true;
                    }
                }
                if (changed) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(sequence); }
            }
        }
    }
}
