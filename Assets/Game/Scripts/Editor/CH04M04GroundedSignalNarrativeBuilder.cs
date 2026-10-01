using System;
using System.Linq;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using Game.Narrative.Contracts;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class CH04M04GroundedSignalNarrativeBuilder
    {
        public const string Path = "Assets/Game/Configs/Narrative/Chapter04/CH04M04_GroundedSignal_Narrative.asset";
        public const string ArtRoot = "Assets/Game/Resources/FutureMissionComics";
        public const string VoiceRoot = "Assets/Game/Audio/Narrative/CH04M04GroundedSignal/Voice";
        private static readonly string[] PanelNames = {"CH04M04_GroundedSignal", "CH04M04_GroundedSignal_Comms", "CH04M04_GroundedSignal_Debrief"};

        [MenuItem("Game/Campaign/Grounded Signal/Install Comic Voices")]
        public static void InstallComicVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var copies = Lines();
            foreach (var copy in copies) foreach (bool persian in new[] {false, true})
            {
                string path = VoicePath(copy.Id, persian);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter
                    ?? throw new InvalidOperationException("Missing final Grounded Signal voice: " + path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.quality = .7f; settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true; importer.loadInBackground = true; importer.ambisonic = false;
                importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale="
                    + (persian ? "fa-IR" : "en-US") + "; manifest=grounded_signal_voice_manifest.json; runtimeNetworkTts=false";
                importer.SaveAndReimport();
                var clip = Voice(copy.Id, persian);
                if (clip == null || clip.length < .25f || clip.channels != 1)
                    throw new InvalidOperationException("Invalid Grounded Signal voice: " + path);
            }
            int count = 0;
            foreach (var sequence in AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>())
            {
                var data = new SerializedObject(sequence); var states = data.FindProperty("states");
                for (int i = 0; i < states.arraySize; i++)
                {
                    var state = states.GetArrayElementAtIndex(i); var lines = state.FindPropertyRelative("lines");
                    float duration = 0;
                    for (int j = 0; j < lines.arraySize; j++)
                    {
                        var line = lines.GetArrayElementAtIndex(j);
                        var copy = copies.Single(c => c.Id == line.FindPropertyRelative("lineId").stringValue);
                        line.FindPropertyRelative("voiceClip").objectReferenceValue = Voice(copy.Id, false);
                        line.FindPropertyRelative("deadlineSeconds").floatValue = Duration(copy);
                        duration = Mathf.Max(duration, Duration(copy)); count++;
                    }
                    if (lines.arraySize > 0) state.FindPropertyRelative("durationSeconds").floatValue = duration;
                }
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(sequence);
            }
            if (count != 8) throw new InvalidOperationException("Expected eight Grounded Signal narrative bindings, got " + count);
            AddPersian(); AssetDatabase.SaveAssets();
            Debug.Log("[GroundedSignalComicVoiceInstall] result=Passed clips=16 lines=8 locales=2 preload=0 runtimeNetworkTts=0");
        }

        [MenuItem("Game/Campaign/Grounded Signal/Build Narrative")]
        public static void BuildAndInstall()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Narrative/Chapter04");
            ConfigureMedia(); InstallSpeakers();
            var basis = AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)
                .OfType<NarrativeSequenceConfig>().ToArray();
            GroundedSignalNarrativeLine[][] sets = {CH04M04GroundedSignalCopy.Brief, CH04M04GroundedSignalCopy.Comms, CH04M04GroundedSignalCopy.Debrief};
            string[] stages = {"brief", "comms", "debrief"};
            for (int i = 0; i < stages.Length; i++)
                ConfigureSequence(stages[i], sets[i], PanelNames[i], basis.Single(x => x.SequenceId.EndsWith("." + stages[i], StringComparison.Ordinal)));
            AddPersian(); AssetDatabase.SaveAssets();
            CampaignMissionComicCoverageValidation.ValidateMissionDistinct(Path, "seq.ch04.m04.");
            Scene scene = EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException("Menu bootstrap missing");
            var merged = (bootstrap.CampaignMissionNarrativeConfigs ?? Array.Empty<NarrativeSequenceConfig>())
                .Where(x => x != null && !x.SequenceId.StartsWith("seq.ch04.m04.", StringComparison.Ordinal))
                .Concat(AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>())
                .OrderBy(x => x.SequenceId, StringComparer.Ordinal).ToArray();
            var menu = new SerializedObject(bootstrap); var configs = menu.FindProperty("campaignMissionNarrativeConfigs");
            configs.arraySize = merged.Length;
            for (int i = 0; i < merged.Length; i++) configs.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            menu.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[GroundedSignalNarrative] result=Passed sequences=3 dialogueStates=8 locales=en,fa-IR comics=3");
        }

        private static void InstallSpeakers()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath)
                ?? throw new InvalidOperationException("Narrative speaker catalog missing");
            var data = new SerializedObject(catalog); var entries = data.FindProperty("speakers");
            foreach (var definition in new[] {
                (NarrativeSpeakerId.Karim, "karim", "Warrant Officer Karim Daher", "Transport specialist", "Portrait_Unit_Chr_Pilot_Male_01_CompactPistol_Card_512.png"),
                (NarrativeSpeakerId.Yusuf, "yusuf", "Chief Yusuf Darzi", "Hardware recovery specialist", "Portrait_Unit_Chr_Bombsuit_Male_01_BombSuit_Card_512.png") })
            {
                SerializedProperty entry = null;
                for (int i = 0; i < entries.arraySize; i++)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("speakerId").intValue == (int)definition.Item1)
                        entry = entries.GetArrayElementAtIndex(i);
                entry ??= entries.GetArrayElementAtIndex(entries.arraySize++);
                entry.FindPropertyRelative("speakerId").intValue = (int)definition.Item1;
                foreach (var pair in new[] {("name", definition.Item3), ("role", definition.Item4), ("accessibleLabel", definition.Item3 + ", " + definition.Item4)})
                {
                    entry.FindPropertyRelative(pair.Item1 + "Key").stringValue = "narrative.speaker." + definition.Item2 + "." + (pair.Item1 == "accessibleLabel" ? "name" : pair.Item1);
                    entry.FindPropertyRelative(pair.Item1 + "Fallback").stringValue = pair.Item2;
                }
                entry.FindPropertyRelative("treatment").intValue = (int)NarrativeSpeakerTreatment.HumanPortrait;
                var portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Portraits/Secondary/" + definition.Item5)
                    ?? throw new InvalidOperationException("Canonical specialist portrait missing: " + definition.Item5);
                entry.FindPropertyRelative("identitySprite").objectReferenceValue = portrait;
                entry.FindPropertyRelative("accentColor").colorValue = new Color(.91f, .66f, .27f);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog);
        }

        private static void ConfigureMedia()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string panel in PanelNames)
            {
                string path = ArtRoot + "/" + panel + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new InvalidOperationException(path);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.mipmapEnabled = false; importer.isReadable = false; importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                var factories = new SpriteDataProviderFactories(); factories.Init();
                var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var previous = provider.GetSpriteRects();
                SpriteRect[] crops = Enumerable.Range(0,3).SelectMany(index => new[]{false,true}.Select(wide =>
                    MakeRect(panel+"-"+(char)('a'+index)+(wide?"-20x9":"-16x9"),
                        Crop(width,height,wide?20f/9f:16f/9f,index),previous))).ToArray();
                provider.SetSpriteRects(crops);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                    crops.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
                foreach (int index in Enumerable.Range(0,3)) foreach (bool wide in new[] {false, true})
                    if (Panel(panel, wide, index) == null) throw new InvalidOperationException("Comic crop missing: " + panel);
            }
            int clips = 0;
            foreach (var line in Lines()) foreach (bool persian in new[] {false, true})
            {
                string path = VoicePath(line.Id, persian);
                if (!System.IO.File.Exists(path)) continue;
                var importer = AssetImporter.GetAtPath(path) as AudioImporter ?? throw new InvalidOperationException(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.quality = .7f; settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true; importer.loadInBackground = true;
                importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale=" +
                    (persian ? "fa-IR" : "en-US") + "; manifest=grounded_signal_voice_manifest.json; runtimeNetworkTts=false";
                importer.SaveAndReimport();
                var clip = Voice(line.Id, persian);
                if (clip == null || clip.length < .25f || clip.channels != 1) throw new InvalidOperationException(path);
                clips++;
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            if(clips!=0&&clips!=16)throw new InvalidOperationException("Grounded Signal voices must be complete in both languages");
            Debug.Log($"[GroundedSignalMedia] result=Passed comics=3 dialogueCrops=8 voices={clips} mode="+(clips==16?"voiced":"captioned"));
        }

        private static void ConfigureSequence(string stage, GroundedSignalNarrativeLine[] lines, string panel, NarrativeSequenceConfig basis)
        {
            string sequenceId = "seq.ch04.m04." + stage;
            var sequence = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>()
                .FirstOrDefault(x => x.SequenceId == sequenceId);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<NarrativeSequenceConfig>();
                if (AssetDatabase.LoadMainAssetAtPath(Path) == null) AssetDatabase.CreateAsset(sequence, Path);
                else AssetDatabase.AddObjectToAsset(sequence, Path);
            }
            EditorUtility.CopySerialized(basis, sequence); sequence.name = "CH04M04_GroundedSignal_" + stage;
            var data = new SerializedObject(sequence);
            data.FindProperty("sequenceId").stringValue = sequenceId;
            data.FindProperty("entryStateId").stringValue = "GroundedSignal-" + stage + "-0";
            data.FindProperty("defaultSkipDestinationId").stringValue = "GroundedSignal-" + stage + "-complete";
            var states = data.FindProperty("states"); states.arraySize = lines.Length + 1;
            for (int i = 0; i < states.arraySize; i++)
            {
                bool done = i == lines.Length; var state = states.GetArrayElementAtIndex(i);
                string complete = "GroundedSignal-" + stage + "-complete";
                S(state, "stateId", done ? complete : "GroundedSignal-" + stage + "-" + i);
                state.FindPropertyRelative("kind").intValue = (int)(done
                    ? stage == "debrief" ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff
                    : NarrativeStateKind.PanelDialogue);
                S(state, "continueStateId", done ? string.Empty : i + 1 == lines.Length ? complete : "GroundedSignal-" + stage + "-" + (i + 1));
                S(state, "skipStateId", done ? string.Empty : complete);
                S(state, "completionPayloadId", done ? "request.grounded_signal." + stage + ".complete" : string.Empty);
                state.FindPropertyRelative("routeRole").intValue = (int)(done && stage == "debrief" ? NarrativeRouteRole.DebriefArrival : NarrativeRouteRole.None);
                state.FindPropertyRelative("reducedMotionSupported").boolValue = true;
                state.FindPropertyRelative("motionPreset").intValue = (int)NarrativeMotionPreset.Static;
                state.FindPropertyRelative("musicCue").intValue = (int)NarrativeMusicCue.Briefing;
                state.FindPropertyRelative("ambienceCue").intValue = (int)NarrativeAmbienceCue.CityConflict;
                state.FindPropertyRelative("eventCue").intValue = (int)NarrativeEventCue.Radio;
                state.FindPropertyRelative("evidenceIds").arraySize = 0;
                state.FindPropertyRelative("missionContextFlags").arraySize = 0;
                state.FindPropertyRelative("panel16x9").objectReferenceValue = null;
                state.FindPropertyRelative("panel20x9").objectReferenceValue = null;
                state.FindPropertyRelative("durationSeconds").floatValue = done ? 0 : Duration(lines[i]);
                var authored = state.FindPropertyRelative("lines"); authored.arraySize = done ? 0 : 1;
                if (done) continue;
                var entry = authored.GetArrayElementAtIndex(0); var line = lines[i];
                S(entry, "lineId", line.Id); S(entry, "textKey", line.Key); S(entry, "englishFallback", line.English);
                entry.FindPropertyRelative("speaker").intValue = (int)line.Speaker;
                entry.FindPropertyRelative("voiceClip").objectReferenceValue = Voice(line.Id, false);
                entry.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                entry.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
                entry.FindPropertyRelative("startSeconds").floatValue = 0;
                entry.FindPropertyRelative("deadlineSeconds").floatValue = Duration(line);
                entry.FindPropertyRelative("essentialCaption").boolValue = true;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < lines.Length; i++)
            {
                SetPanel(sequence.States[i], "panel16x9Reference", panel, false, i);
                SetPanel(sequence.States[i], "panel20x9Reference", panel, true, i);
            }
            SetPanel(sequence.States[lines.Length], "panel16x9Reference", null, false, 0);
            SetPanel(sequence.States[lines.Length], "panel20x9Reference", null, true, 0);
            EditorUtility.SetDirty(sequence);
        }

        private static void SetPanel(NarrativeStateRecord state, string field, string panel, bool wide, int index)
        {
            AssetReferenceSprite reference = null;
            if (panel != null)
            {
                var settings = AddressableAssetSettingsDefaultObject.GetSettings(true) ?? throw new InvalidOperationException("Addressables missing");
                var group = settings.FindGroup("Grounded Signal Narrative") ?? settings.CreateGroup("Grounded Signal Narrative", false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                string path = ArtRoot + "/" + panel + ".png";
                string guid = AssetDatabase.AssetPathToGUID(path);
                settings.CreateOrMoveEntry(guid, group, false, false).SetAddress("narrative.grounded_signal." + panel.ToLowerInvariant(), false);
                reference = new AssetReferenceSprite(guid) {SubObjectName = Panel(panel, wide, index).name};
                EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings);
            }
            typeof(NarrativeStateRecord).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(state, reference);
        }

        private static void AddPersian()
        {
            var locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath)
                ?? throw new InvalidOperationException("Persian narrative locale missing");
            var data = new SerializedObject(locale); var text = data.FindProperty("text"); var voices = data.FindProperty("voices");
            for (int i = text.arraySize - 1; i >= 0; i--)
                if (text.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue.StartsWith("narrative.grounded_signal.", StringComparison.Ordinal))
                    text.DeleteArrayElementAtIndex(i);
            for (int i = voices.arraySize - 1; i >= 0; i--)
                if (voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue.StartsWith("grounded_signal-", StringComparison.Ordinal))
                    voices.DeleteArrayElementAtIndex(i);
            foreach (var line in Lines())
            {
                var entry = text.GetArrayElementAtIndex(text.arraySize++); S(entry, "key", line.Key); S(entry, "value", line.Persian);
                var voice = voices.GetArrayElementAtIndex(voices.arraySize++); S(voice, "lineId", line.Id);
                voice.FindPropertyRelative("voiceClip").objectReferenceValue = Voice(line.Id, true);
                voice.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                voice.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
            }
            foreach (var pair in new[] {("narrative.speaker.karim.name", "کریم ظاهر"), ("narrative.speaker.karim.role", "متخصص ترابری"),
                ("narrative.speaker.yusuf.name", "یوسف درزی"), ("narrative.speaker.yusuf.role", "متخصص بازیابی تجهیزات")})
            {
                SerializedProperty localized = null;
                for(int i=0;i<text.arraySize;i++)
                    if(text.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==pair.Item1) localized=text.GetArrayElementAtIndex(i);
                localized ??= text.GetArrayElementAtIndex(text.arraySize++);
                S(localized,"key",pair.Item1); S(localized,"value",pair.Item2);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(locale);
        }
        private static GroundedSignalNarrativeLine[] Lines() => CH04M04GroundedSignalCopy.Brief.Concat(CH04M04GroundedSignalCopy.Comms)
            .Concat(CH04M04GroundedSignalCopy.Debrief).ToArray();
        private static string VoicePath(string id, bool persian) => VoiceRoot + "/" + (persian ? "fa" : "en") + "/" + id + ".wav";
        private static AudioClip Voice(string id, bool persian) => AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id, persian));
        private static Sprite Panel(string panel, bool wide, int index) => AssetDatabase.LoadAllAssetsAtPath(ArtRoot + "/" + panel + ".png")
            .OfType<Sprite>().FirstOrDefault(x => x.name == panel + "-"+(char)('a'+index) + (wide ? "-20x9" : "-16x9"));
        private static float Duration(in GroundedSignalNarrativeLine line) => Mathf.Max(8f, Mathf.Max(Voice(line.Id, false)?.length??0,
            Voice(line.Id, true)?.length??0) + .5f, Mathf.Max(line.English.Length, line.Persian.Length) / 14f);
        private static SpriteRect MakeRect(string name, Rect rect, SpriteRect[] previous) => new()
        {
            name = name, rect = rect, alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
            spriteID = previous.FirstOrDefault(x => x.name == name)?.spriteID ?? GUID.Generate()
        };
        private static Rect Crop(int width, int height, float aspect, int index)
        {
            int h = Mathf.FloorToInt(Mathf.Min(height, width / aspect) * (1f-index*.06f));
            int w = Mathf.Min(width, Mathf.FloorToInt(h * aspect));
            // Preserve the character-focused left side in closer dialogue framings.
            return new Rect(index==0?(width-w)/2:0,(height-h)/2,w,h);
        }
        private static void S(SerializedProperty property, string field, string value) => property.FindPropertyRelative(field).stringValue = value;
    }
}
