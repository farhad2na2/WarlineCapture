using System;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    public static class AudioRuntimeConfigAssetBuilder
    {
        public const string CatalogJsonPath = "Assets/Game/Audio/Config/audio_event_catalog_v0_1.json";
        public const string EventCatalogAssetPath = "Assets/Game/Audio/Events/AudioEventCatalogConfig.asset";
        public const string MixerBusAssetPath = "Assets/Game/Audio/Mixers/AudioMixerBusConfig.asset";

        [MenuItem("WarlineCapture/Audio/Build Runtime Config Assets")]
        public static void BuildDefaultAssetsMenu()
        {
            BuildDefaultAssets();
        }

        public static BuildResult BuildDefaultAssets(bool forceRegenerate = true)
        {
            TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogJsonPath);
            if (jsonAsset == null)
                throw new InvalidOperationException($"Missing audio catalog JSON at {CatalogJsonPath}.");

            AudioCatalogJson catalog = JsonUtility.FromJson<AudioCatalogJson>(jsonAsset.text);
            if (catalog == null || catalog.events == null || catalog.buses == null)
                throw new InvalidOperationException($"Invalid audio catalog JSON at {CatalogJsonPath}.");

            EnsureFolder("Assets/Game/Audio/Events");
            EnsureFolder("Assets/Game/Audio/Mixers");

            AudioEventCatalogConfig eventCatalog = LoadOrCreate<AudioEventCatalogConfig>(EventCatalogAssetPath, forceRegenerate);
            AudioMixerBusConfig mixerBusConfig = LoadOrCreate<AudioMixerBusConfig>(MixerBusAssetPath, forceRegenerate);

            PopulateEventCatalog(eventCatalog, catalog);
            PopulateMixerBuses(mixerBusConfig, catalog);

            EditorUtility.SetDirty(eventCatalog);
            EditorUtility.SetDirty(mixerBusConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new BuildResult(catalog.events.Length, catalog.buses.Length);
        }

        private static void PopulateEventCatalog(AudioEventCatalogConfig target, AudioCatalogJson catalog)
        {
            SerializedObject serialized = new(target);
            SerializedProperty events = serialized.FindProperty("events");
            events.ClearArray();

            for (int eventIndex = 0; eventIndex < catalog.events.Length; eventIndex++)
            {
                AudioEventJson eventJson = catalog.events[eventIndex];
                if (string.IsNullOrWhiteSpace(eventJson.eventId))
                    throw new InvalidOperationException($"Audio event at index {eventIndex} has no eventId.");

                events.InsertArrayElementAtIndex(eventIndex);
                SerializedProperty eventProperty = events.GetArrayElementAtIndex(eventIndex);
                eventProperty.FindPropertyRelative("eventId").stringValue = eventJson.eventId;
                eventProperty.FindPropertyRelative("busId").stringValue = eventJson.busId;
                eventProperty.FindPropertyRelative("priority").enumValueIndex = (int)ParsePriority(eventJson.priority, eventJson.eventId);
                eventProperty.FindPropertyRelative("cooldownMilliseconds").intValue = Math.Max(0, eventJson.cooldownMs);
                eventProperty.FindPropertyRelative("volumeDecibels").floatValue = eventJson.volumeDb;

                SerializedProperty pitch = eventProperty.FindPropertyRelative("pitchVariance");
                pitch.vector2Value = new Vector2(eventJson.pitchVariance.min, eventJson.pitchVariance.max);

                SerializedProperty playback = eventProperty.FindPropertyRelative("playback");
                playback.FindPropertyRelative("loop").boolValue = eventJson.playback.loop;
                playback.FindPropertyRelative("spatial").boolValue = eventJson.playback.spatial;
                playback.FindPropertyRelative("maxInstances").intValue = Math.Max(1, eventJson.playback.maxInstances);
                playback.FindPropertyRelative("allowRuntimeLoad").boolValue = eventJson.playback.allowRuntimeLoad;

                SerializedProperty clips = eventProperty.FindPropertyRelative("clips");
                clips.ClearArray();
                for (int clipIndex = 0; clipIndex < eventJson.clips.Length; clipIndex++)
                {
                    AudioClipJson clipJson = eventJson.clips[clipIndex];
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipJson.assetPath);
                    if (clip == null)
                        throw new InvalidOperationException($"Missing AudioClip for {eventJson.eventId}: {clipJson.assetPath}");

                    clips.InsertArrayElementAtIndex(clipIndex);
                    SerializedProperty clipProperty = clips.GetArrayElementAtIndex(clipIndex);
                    clipProperty.FindPropertyRelative("clip").objectReferenceValue = clip;
                    clipProperty.FindPropertyRelative("weight").intValue = Math.Max(0, clipJson.weight);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PopulateMixerBuses(AudioMixerBusConfig target, AudioCatalogJson catalog)
        {
            SerializedObject serialized = new(target);
            SerializedProperty buses = serialized.FindProperty("buses");
            buses.ClearArray();

            for (int busIndex = 0; busIndex < catalog.buses.Length; busIndex++)
            {
                AudioBusJson busJson = catalog.buses[busIndex];
                if (string.IsNullOrWhiteSpace(busJson.busId))
                    throw new InvalidOperationException($"Audio bus at index {busIndex} has no busId.");

                buses.InsertArrayElementAtIndex(busIndex);
                SerializedProperty busProperty = buses.GetArrayElementAtIndex(busIndex);
                busProperty.FindPropertyRelative("busId").stringValue = busJson.busId;
                busProperty.FindPropertyRelative("parentBusId").stringValue = string.IsNullOrWhiteSpace(busJson.parentBusId)
                    ? "Master"
                    : busJson.parentBusId;
                busProperty.FindPropertyRelative("mixerGroup").objectReferenceValue = null;
                busProperty.FindPropertyRelative("volumeSettingKey").stringValue = busJson.busId;
                busProperty.FindPropertyRelative("defaultVolumeDecibels").floatValue = busJson.defaultVolumeDb;
                busProperty.FindPropertyRelative("canDuck").boolValue = busJson.ducks != null && busJson.ducks.Length > 0;

                SerializedProperty duckTargets = busProperty.FindPropertyRelative("duckTargetBusIds");
                duckTargets.ClearArray();
                if (busJson.ducks == null)
                    continue;

                for (int duckIndex = 0; duckIndex < busJson.ducks.Length; duckIndex++)
                {
                    duckTargets.InsertArrayElementAtIndex(duckIndex);
                    duckTargets.GetArrayElementAtIndex(duckIndex).stringValue = busJson.ducks[duckIndex];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioEventPriority ParsePriority(string value, string eventId)
        {
            if (Enum.TryParse(value, ignoreCase: false, out AudioEventPriority priority))
                return priority;

            throw new InvalidOperationException($"Audio event {eventId} has invalid priority '{value}'.");
        }

        private static T LoadOrCreate<T>(string path, bool forceRegenerate) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                throw new InvalidOperationException($"Invalid asset folder path: {path}");

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        public readonly struct BuildResult
        {
            public BuildResult(int eventCount, int busCount)
            {
                EventCount = eventCount;
                BusCount = busCount;
            }

            public int EventCount { get; }
            public int BusCount { get; }
        }

        [Serializable]
        private sealed class AudioCatalogJson
        {
            public AudioBusJson[] buses;
            public AudioEventJson[] events;
        }

        [Serializable]
        private sealed class AudioBusJson
        {
            public string busId;
            public string parentBusId;
            public float defaultVolumeDb;
            public string[] ducks;
        }

        [Serializable]
        private sealed class AudioEventJson
        {
            public string eventId;
            public string busId;
            public string priority;
            public int cooldownMs;
            public float volumeDb;
            public PitchVarianceJson pitchVariance;
            public PlaybackJson playback;
            public AudioClipJson[] clips;
        }

        [Serializable]
        private struct PitchVarianceJson
        {
            public float min;
            public float max;
        }

        [Serializable]
        private struct PlaybackJson
        {
            public bool loop;
            public bool spatial;
            public int maxInstances;
            public bool allowRuntimeLoad;
        }

        [Serializable]
        private sealed class AudioClipJson
        {
            public string assetPath;
            public int weight;
        }
    }
}
