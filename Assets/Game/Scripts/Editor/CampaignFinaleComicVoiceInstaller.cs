using System;
using System.Linq;
using Game.Catalog.Contracts;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Installs local recordings without rebuilding comics, scenes or campaign routes.</summary>
    public static class CampaignFinaleComicVoiceInstaller
    {
        public readonly struct Copy
        {
            public readonly string Id, Key, English, Persian;
            public readonly NarrativeSpeakerId Speaker;
            public Copy(string id, string key, NarrativeSpeakerId speaker, string english, string persian)
            { Id = id; Key = key; Speaker = speaker; English = english; Persian = persian; }
        }

        public static Copy[] NetworkCollapseCopy() => CH05M03NetworkCollapseNarrativeCopy.Brief
            .Concat(CH05M03NetworkCollapseNarrativeCopy.Comms).Concat(CH05M03NetworkCollapseNarrativeCopy.Debrief)
            .Select(c => new Copy(c.Id, c.Key, c.Speaker, c.English, c.Persian)).ToArray();
        public static Copy[] LastCorridorCopy() => CH05M04LastCorridorNarrativeCopy.Brief
            .Concat(CH05M04LastCorridorNarrativeCopy.Comms).Concat(CH05M04LastCorridorNarrativeCopy.Debrief)
            .Select(c => new Copy(c.Id, c.Key, c.Speaker, c.English, c.Persian)).ToArray();
        public static Copy[] CommandNodeCopy() => new[] {
            CH05M05CommandNodeNarrativeCopy.Brief, CH05M05CommandNodeNarrativeCopy.Comms,
            CH05M05CommandNodeNarrativeCopy.Debrief, CH05M05CommandNodeNarrativeCopy.Close,
            CH05M05CommandNodeNarrativeCopy.Epilogue, CH05M05CommandNodeNarrativeCopy.TrustHigh,
            CH05M05CommandNodeNarrativeCopy.TrustLow, CH05M05CommandNodeNarrativeCopy.EvidenceHigh,
            CH05M05CommandNodeNarrativeCopy.EvidenceLow, CH05M05CommandNodeNarrativeCopy.InfrastructureHigh,
            CH05M05CommandNodeNarrativeCopy.InfrastructureLow, CH05M05CommandNodeNarrativeCopy.Postscript }
            .SelectMany(c => c).Select(c => new Copy(c.Id, c.Key, c.Speaker, c.English, c.Persian)).ToArray();

        private static string[] Variants(NarrativeSpeakerId speaker) => speaker == NarrativeSpeakerId.Commander
            ? new[] { "", "_female", "_neutral" } : new[] { "" };
        public static string VoicePath(string root, string id, bool persian, string variant = "") =>
            root + "/" + (persian ? "fa" : "en") + "/" + id + variant + ".wav";
        public static AudioClip Voice(string root, string id, bool persian, string variant = "") =>
            AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(root, id, persian, variant));
        public static bool HasCompleteRecordings(string root, Copy[] copies) => copies.All(c =>
            new[] { false, true }.All(fa => Variants(c.Speaker).All(v => System.IO.File.Exists(VoicePath(root, c.Id, fa, v)))));

        public static float Duration(string root, string id, NarrativeSpeakerId speaker, string english, string persian)
        {
            float longest = Variants(speaker).SelectMany(v => new[] { false, true }.Select(fa =>
                Voice(root, id, fa, v)?.length ?? 0)).Max();
            return Mathf.Max(8f, longest + .5f, Mathf.Max(english.Length, persian.Length) / 14f);
        }

        [MenuItem("Game/Campaign/Install Missing Finale Comic Voices")]
        public static void InstallAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Install comic voices outside Play Mode");
            CH05M03NetworkCollapseNarrativeBuilder.InstallComicVoices();
            CH05M04LastCorridorNarrativeBuilder.InstallComicVoices();
            CH05M05CommandNodeNarrativeBuilder.InstallComicVoices();
            CampaignFinaleComicVoiceValidation.Validate();
            Debug.Log("[CampaignFinaleComicVoiceInstall] result=Passed lines=55 clips=126 locales=2 commanderVariants=3 runtimeNetworkTts=0");
        }

        public static void Install(string narrativePath, string root, string manifest, Copy[] copies)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Install comic voices outside Play Mode");
            if (!HasCompleteRecordings(root, copies))
                throw new InvalidOperationException("Complete bilingual recordings required: " + root);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // Check every caption and locale before modifying any binding.
            var sequences = AssetDatabase.LoadAllAssetsAtPath(narrativePath).OfType<NarrativeSequenceConfig>().ToArray();
            var authored = sequences.SelectMany(s => s.States.SelectMany(state => state.Lines)).ToArray();
            if (authored.Length != copies.Length || authored.Select(l => l.LineId).Distinct().Count() != copies.Length)
                throw new InvalidOperationException("Missing or duplicate narrative lines: " + narrativePath);
            var locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath)
                ?? throw new InvalidOperationException("Persian narrative locale missing");
            foreach (var copy in copies)
            {
                var line = authored.Single(l => l.LineId == copy.Id);
                if (line.TextKey != copy.Key || line.EnglishFallback != copy.English || line.Speaker != copy.Speaker
                    || locale.Text.Count(t => t.Key == copy.Key && t.Value == copy.Persian) != 1)
                    throw new InvalidOperationException("Canonical comic caption mismatch: " + copy.Id);
                foreach (bool fa in new[] { false, true }) foreach (string variant in Variants(copy.Speaker))
                {
                    string path = VoicePath(root, copy.Id, fa, variant);
                    var importer = AssetImporter.GetAtPath(path) as AudioImporter
                        ?? throw new InvalidOperationException("Audio importer missing: " + path);
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    settings.quality = .7f;
                    settings.preloadAudioData = false;
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = true;
                    importer.loadInBackground = true;
                    importer.ambisonic = false;
                    importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale="
                        + (fa ? "fa-IR" : "en-US") + "; manifest=" + manifest + "; runtimeNetworkTts=false";
                    importer.SaveAndReimport();
                    var clip = Voice(root, copy.Id, fa, variant);
                    if (clip == null || clip.length < .25f || clip.channels != 1)
                        throw new InvalidOperationException("Invalid imported voice: " + path);
                }
            }
            foreach (var sequence in sequences)
            {
                var data = new SerializedObject(sequence);
                var states = data.FindProperty("states");
                for (int i = 0; i < states.arraySize; i++)
                {
                    var state = states.GetArrayElementAtIndex(i);
                    var lines = state.FindPropertyRelative("lines");
                    float duration = state.FindPropertyRelative("durationSeconds").floatValue;
                    for (int j = 0; j < lines.arraySize; j++)
                    {
                        var line = lines.GetArrayElementAtIndex(j);
                        var copy = copies.Single(c => c.Id == line.FindPropertyRelative("lineId").stringValue);
                        Bind(line, root, copy, false);
                        float deadline = line.FindPropertyRelative("startSeconds").floatValue
                            + Duration(root, copy.Id, copy.Speaker, copy.English, copy.Persian);
                        line.FindPropertyRelative("deadlineSeconds").floatValue = deadline;
                        duration = Mathf.Max(duration, deadline);
                    }
                    if (lines.arraySize > 0) state.FindPropertyRelative("durationSeconds").floatValue = duration;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(sequence);
            }
            var localized = new SerializedObject(locale);
            var voices = localized.FindProperty("voices");
            var ids = copies.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            for (int i = voices.arraySize - 1; i >= 0; i--)
                if (ids.Contains(voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue))
                    voices.DeleteArrayElementAtIndex(i);
            foreach (var copy in copies)
            {
                var voice = voices.GetArrayElementAtIndex(voices.arraySize++);
                voice.FindPropertyRelative("lineId").stringValue = copy.Id;
                Bind(voice, root, copy, true);
            }
            localized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(locale);
            AssetDatabase.SaveAssetIfDirty(locale);
            foreach (var sequence in sequences) AssetDatabase.SaveAssetIfDirty(sequence);
            int clips = copies.Sum(c => Variants(c.Speaker).Length * 2);
            Debug.Log($"[CampaignComicVoiceBindings] result=Passed asset={narrativePath} lines={copies.Length} clips={clips} locales=2 preload=0");
        }

        private static void Bind(SerializedProperty line, string root, Copy copy, bool persian)
        {
            line.FindPropertyRelative("voiceClip").objectReferenceValue = Voice(root, copy.Id, persian);
            line.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = copy.Speaker == NarrativeSpeakerId.Commander
                ? Voice(root, copy.Id, persian, "_female") : null;
            line.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = copy.Speaker == NarrativeSpeakerId.Commander
                ? Voice(root, copy.Id, persian, "_neutral") : null;
        }
    }
}
