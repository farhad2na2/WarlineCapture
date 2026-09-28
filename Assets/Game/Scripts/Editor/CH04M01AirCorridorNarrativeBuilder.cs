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
    public static class CH04M01AirCorridorNarrativeBuilder
    {
        public const string Path = "Assets/Game/Configs/Narrative/Chapter04/CH04M01_AirCorridor_Narrative.asset";
        public const string ArtRoot = "Assets/Game/Resources/FutureMissionComics";
        public const string VoiceRoot = "Assets/Game/Audio/Narrative/CH04M01AirCorridor/Voice";
        private static readonly string[] PanelNames = {"CH04M01_AirCorridor", "CH04M01_AirCorridor_Comms", "CH04M01_AirCorridor_Debrief"};

        [MenuItem("Game/Campaign/Air Corridor/Build Narrative")]
        public static void BuildAndInstall()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Narrative/Chapter04");
            ConfigureMedia();
            var basis = AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)
                .OfType<NarrativeSequenceConfig>().ToArray();
            AirCorridorNarrativeLine[][] sets = {CH04M01AirCorridorCopy.Brief, CH04M01AirCorridorCopy.Comms, CH04M01AirCorridorCopy.Debrief};
            string[] stages = {"brief", "comms", "debrief"};
            for (int i = 0; i < stages.Length; i++)
                ConfigureSequence(stages[i], sets[i], PanelNames[i], basis.Single(x => x.SequenceId.EndsWith("." + stages[i], StringComparison.Ordinal)));
            AddPersian(); AssetDatabase.SaveAssets();
            CampaignMissionComicCoverageValidation.ValidateMissionDistinct(Path, "seq.ch04.m01.");
            Scene scene = EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException("Menu bootstrap missing");
            var merged = (bootstrap.CampaignMissionNarrativeConfigs ?? Array.Empty<NarrativeSequenceConfig>())
                .Where(x => x != null && !x.SequenceId.StartsWith("seq.ch04.m01.", StringComparison.Ordinal))
                .Concat(AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>())
                .OrderBy(x => x.SequenceId, StringComparer.Ordinal).ToArray();
            var menu = new SerializedObject(bootstrap); var configs = menu.FindProperty("campaignMissionNarrativeConfigs");
            configs.arraySize = merged.Length;
            for (int i = 0; i < merged.Length; i++) configs.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            menu.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[AirCorridorNarrative] result=Passed sequences=3 dialogueStates=6 locales=en,fa-IR voices=local comics=3");
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
                SpriteRect[] crops = {
                    MakeRect(panel + "-a-16x9", Crop(width, height, 16f/9f, false), previous),
                    MakeRect(panel + "-a-20x9", Crop(width, height, 20f/9f, false), previous),
                    MakeRect(panel + "-b-16x9", Crop(width, height, 16f/9f, true), previous),
                    MakeRect(panel + "-b-20x9", Crop(width, height, 20f/9f, true), previous)
                };
                provider.SetSpriteRects(crops);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                    crops.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
                foreach (bool alternate in new[] {false, true}) foreach (bool wide in new[] {false, true})
                    if (Panel(panel, wide, alternate) == null) throw new InvalidOperationException("Comic crop missing: " + panel);
            }
            int clips = 0;
            foreach (var line in Lines()) foreach (bool persian in new[] {false, true})
            {
                string path = VoicePath(line.Id, persian);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter ?? throw new InvalidOperationException(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.quality = .7f; settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true; importer.loadInBackground = true;
                importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale=" +
                    (persian ? "fa-IR" : "en-US") + "; manifest=air_corridor_voice_manifest.json; runtimeNetworkTts=false";
                importer.SaveAndReimport();
                var clip = Voice(line.Id, persian);
                if (clip == null || clip.length < .25f || clip.channels != 1) throw new InvalidOperationException(path);
                clips++;
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            Debug.Log($"[AirCorridorMedia] result=Passed comics=3 crops=6 voices={clips}");
        }

        private static void ConfigureSequence(string stage, AirCorridorNarrativeLine[] lines, string panel, NarrativeSequenceConfig basis)
        {
            string sequenceId = "seq.ch04.m01." + stage;
            var sequence = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>()
                .FirstOrDefault(x => x.SequenceId == sequenceId);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<NarrativeSequenceConfig>();
                if (AssetDatabase.LoadMainAssetAtPath(Path) == null) AssetDatabase.CreateAsset(sequence, Path);
                else AssetDatabase.AddObjectToAsset(sequence, Path);
            }
            EditorUtility.CopySerialized(basis, sequence); sequence.name = "CH04M01_AirCorridor_" + stage;
            var data = new SerializedObject(sequence);
            data.FindProperty("sequenceId").stringValue = sequenceId;
            data.FindProperty("entryStateId").stringValue = "AirCorridor-" + stage + "-0";
            data.FindProperty("defaultSkipDestinationId").stringValue = "AirCorridor-" + stage + "-complete";
            var states = data.FindProperty("states"); states.arraySize = lines.Length + 1;
            for (int i = 0; i < states.arraySize; i++)
            {
                bool done = i == lines.Length; var state = states.GetArrayElementAtIndex(i);
                string complete = "AirCorridor-" + stage + "-complete";
                S(state, "stateId", done ? complete : "AirCorridor-" + stage + "-" + i);
                state.FindPropertyRelative("kind").intValue = (int)(done
                    ? stage == "debrief" ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff
                    : NarrativeStateKind.PanelDialogue);
                S(state, "continueStateId", done ? string.Empty : i + 1 == lines.Length ? complete : "AirCorridor-" + stage + "-" + (i + 1));
                S(state, "skipStateId", done ? string.Empty : complete);
                S(state, "completionPayloadId", done ? "request.air_corridor." + stage + ".complete" : string.Empty);
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
                SetPanel(sequence.States[i], "panel16x9Reference", panel, false, i != 0);
                SetPanel(sequence.States[i], "panel20x9Reference", panel, true, i != 0);
            }
            SetPanel(sequence.States[lines.Length], "panel16x9Reference", null, false, false);
            SetPanel(sequence.States[lines.Length], "panel20x9Reference", null, true, false);
            EditorUtility.SetDirty(sequence);
        }

        private static void SetPanel(NarrativeStateRecord state, string field, string panel, bool wide, bool alternate)
        {
            AssetReferenceSprite reference = null;
            if (panel != null)
            {
                var settings = AddressableAssetSettingsDefaultObject.GetSettings(true) ?? throw new InvalidOperationException("Addressables missing");
                var group = settings.FindGroup("Air Corridor Narrative") ?? settings.CreateGroup("Air Corridor Narrative", false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                string path = ArtRoot + "/" + panel + ".png";
                string guid = AssetDatabase.AssetPathToGUID(path);
                settings.CreateOrMoveEntry(guid, group, false, false).SetAddress("narrative.air_corridor." + panel.ToLowerInvariant(), false);
                reference = new AssetReferenceSprite(guid) {SubObjectName = Panel(panel, wide, alternate).name};
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
                if (text.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue.StartsWith("narrative.air_corridor.", StringComparison.Ordinal))
                    text.DeleteArrayElementAtIndex(i);
            for (int i = voices.arraySize - 1; i >= 0; i--)
                if (voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue.StartsWith("air_corridor-", StringComparison.Ordinal))
                    voices.DeleteArrayElementAtIndex(i);
            foreach (var line in Lines())
            {
                var entry = text.GetArrayElementAtIndex(text.arraySize++); S(entry, "key", line.Key); S(entry, "value", line.Persian);
                var voice = voices.GetArrayElementAtIndex(voices.arraySize++); S(voice, "lineId", line.Id);
                voice.FindPropertyRelative("voiceClip").objectReferenceValue = Voice(line.Id, true);
                voice.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                voice.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(locale);
        }
        private static AirCorridorNarrativeLine[] Lines() => CH04M01AirCorridorCopy.Brief.Concat(CH04M01AirCorridorCopy.Comms)
            .Concat(CH04M01AirCorridorCopy.Debrief).ToArray();
        private static string VoicePath(string id, bool persian) => VoiceRoot + "/" + (persian ? "fa" : "en") + "/" + id + ".wav";
        private static AudioClip Voice(string id, bool persian) => AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id, persian));
        private static Sprite Panel(string panel, bool wide, bool alternate) => AssetDatabase.LoadAllAssetsAtPath(ArtRoot + "/" + panel + ".png")
            .OfType<Sprite>().FirstOrDefault(x => x.name == panel + (alternate ? "-b" : "-a") + (wide ? "-20x9" : "-16x9"));
        private static float Duration(in AirCorridorNarrativeLine line) => Mathf.Max(8f, Mathf.Max(Voice(line.Id, false).length,
            Voice(line.Id, true).length) + .5f, Mathf.Max(line.English.Length, line.Persian.Length) / 14f);
        private static SpriteRect MakeRect(string name, Rect rect, SpriteRect[] previous) => new()
        {
            name = name, rect = rect, alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
            spriteID = previous.FirstOrDefault(x => x.name == name)?.spriteID ?? GUID.Generate()
        };
        private static Rect Crop(int width, int height, float aspect, bool alternate)
        {
            int h = Mathf.FloorToInt(Mathf.Min(height, width / aspect) * (alternate ? .86f : 1f));
            int w = Mathf.Min(width, Mathf.FloorToInt(h * aspect));
            return new Rect(alternate ? Mathf.Max(0, width-w-8) : (width-w)/2,
                alternate ? Mathf.Max(0, height-h-8) : (height-h)/2, w, h);
        }
        private static void S(SerializedProperty property, string field, string value) => property.FindPropertyRelative(field).stringValue = value;
    }
}
