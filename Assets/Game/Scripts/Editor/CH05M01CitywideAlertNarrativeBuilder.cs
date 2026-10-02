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
    public static class CH05M01CitywideAlertNarrativeBuilder
    {
        public const string Path = "Assets/Game/Configs/Narrative/Chapter05/CH05M01_CitywideAlert_Narrative.asset";
        public const string ArtRoot = "Assets/Game/Resources/FutureMissionComics";
        public const string VoiceRoot = "Assets/Game/Audio/Narrative/CH05M01CitywideAlert/Voice";
        private static readonly string[] PanelNames = {"CH05M01_CitywideAlert", "CH05M01_CitywideAlert_Comms", "CH05M01_CitywideAlert_Debrief", "CH05M01_CitywideAlert_Opening"};

        [MenuItem("Game/Campaign/Citywide Alert/Install Comic Voices")]
        public static void InstallComicVoices()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var copies = Lines();
            foreach (var copy in copies) foreach (bool persian in new[] {false, true})
            {
                string path = VoicePath(copy.Id, persian);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter
                    ?? throw new InvalidOperationException("Missing final Citywide Alert voice: " + path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.quality = .7f; settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true; importer.loadInBackground = true; importer.ambisonic = false;
                importer.userData = "status=ELEVENLABS_PAID_CREATOR_COMMERCIAL_LICENSE; provider=ElevenLabs; model=eleven_v3; locale="
                    + (persian ? "fa-IR" : "en-US") + "; manifest=citywide_alert_voice_manifest.json; runtimeNetworkTts=false";
                importer.SaveAndReimport();
                var clip = Voice(copy.Id, persian);
                if (clip == null || clip.length < .25f || clip.channels != 1)
                    throw new InvalidOperationException("Invalid Citywide Alert voice: " + path);
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
            if (count != 14) throw new InvalidOperationException("Expected fourteen Citywide Alert narrative bindings, got " + count);
            AddPersian(); AssetDatabase.SaveAssets();
            Debug.Log("[CitywideAlertComicVoiceInstall] result=Passed clips=28 lines=14 locales=2 preload=0 runtimeNetworkTts=0");
        }

        [MenuItem("Game/Campaign/Citywide Alert/Build Narrative")]
        public static void BuildAndInstall()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Narrative/Chapter05");
            ConfigureMedia();
            var basis = AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)
                .OfType<NarrativeSequenceConfig>().ToArray();
            CitywideAlertNarrativeLine[][] sets = {CH05M01CitywideAlertCopy.Brief, CH05M01CitywideAlertCopy.Comms, CH05M01CitywideAlertCopy.Debrief, CH05M01CitywideAlertCopy.Opening};
            string[] stages = {"brief", "comms", "debrief", "open"};
            for (int i = 0; i < stages.Length; i++)
                ConfigureSequence(stages[i], sets[i], PanelNames[i], basis.Single(x => x.SequenceId.EndsWith("." + (stages[i] == "open" ? "debrief" : stages[i]), StringComparison.Ordinal)));
            AddPersian(); AssetDatabase.SaveAssets();
            CampaignMissionComicCoverageValidation.ValidateMissionDistinct(Path, "seq.ch05.m01.");
            CampaignMissionComicCoverageValidation.ValidateMissionDistinct(Path, "seq.ch05.open.citywide_command");
            Scene scene = EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException("Menu bootstrap missing");
            var merged = (bootstrap.CampaignMissionNarrativeConfigs ?? Array.Empty<NarrativeSequenceConfig>())
                .Where(x => x != null && !x.SequenceId.StartsWith("seq.ch05.m01.", StringComparison.Ordinal) && x.SequenceId != "seq.ch05.open.citywide_command")
                .Concat(AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>())
                .OrderBy(x => x.SequenceId, StringComparer.Ordinal).ToArray();
            var menu = new SerializedObject(bootstrap); var configs = menu.FindProperty("campaignMissionNarrativeConfigs");
            configs.arraySize = merged.Length;
            for (int i = 0; i < merged.Length; i++) configs.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            menu.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[CitywideAlertNarrative] result=Passed sequences=4 dialogueStates=14 locales=en,fa-IR missionComics=3 chapterOpeningPanels=7");
        }

        private static void ConfigureMedia()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string openingArt = ArtRoot + "/CH05M01_CitywideAlert_Opening.png";
            if(!System.IO.File.Exists(openingArt))
            {
                if(!AssetDatabase.CopyAsset(ArtRoot + "/CH05M01_CitywideAlert.png",openingArt))
                    throw new InvalidOperationException("Citywide chapter opening art copy failed");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
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
                SpriteRect[] crops = Enumerable.Range(0, panel.EndsWith("_Opening", StringComparison.Ordinal) ? 7 : 3).SelectMany(index => new[]{false,true}.Select(wide =>
                    MakeRect(PanelName(panel, wide, index),
                        Crop(width,height,wide?20f/9f:16f/9f,index),previous))).ToArray();
                provider.SetSpriteRects(crops);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                    crops.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
                foreach (int index in Enumerable.Range(0, panel.EndsWith("_Opening", StringComparison.Ordinal) ? 7 : 3)) foreach (bool wide in new[] {false, true})
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
                    (persian ? "fa-IR" : "en-US") + "; manifest=citywide_alert_voice_manifest.json; runtimeNetworkTts=false";
                importer.SaveAndReimport();
                var clip = Voice(line.Id, persian);
                if (clip == null || clip.length < .25f || clip.channels != 1) throw new InvalidOperationException(path);
                clips++;
            }
            AudioRuntimeConfigAssetBuilder.BuildDefaultAssets();
            if(clips!=0&&clips!=28)throw new InvalidOperationException("Citywide Alert voices must be complete in both languages");
            Debug.Log($"[CitywideAlertMedia] result=Passed comics=4 dialogueCrops=14 voices={clips} mode="+(clips==28?"voiced":"captioned"));
        }

        private static void ConfigureSequence(string stage, CitywideAlertNarrativeLine[] lines, string panel, NarrativeSequenceConfig basis)
        {
            string sequenceId = stage == "open" ? "seq.ch05.open.citywide_command" : "seq.ch05.m01." + stage;
            var sequence = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>()
                .FirstOrDefault(x => x.SequenceId == sequenceId);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<NarrativeSequenceConfig>();
                if (AssetDatabase.LoadMainAssetAtPath(Path) == null) AssetDatabase.CreateAsset(sequence, Path);
                else AssetDatabase.AddObjectToAsset(sequence, Path);
            }
            EditorUtility.CopySerialized(basis, sequence);
            sequence.name = AssetDatabase.LoadMainAssetAtPath(Path) == sequence
                ? System.IO.Path.GetFileNameWithoutExtension(Path) : "CH05M01_CitywideAlert_" + stage;
            var data = new SerializedObject(sequence);
            data.FindProperty("sequenceId").stringValue = sequenceId;
            data.FindProperty("entryStateId").stringValue = "CitywideAlert-" + stage + "-0";
            data.FindProperty("defaultSkipDestinationId").stringValue = "CitywideAlert-" + stage + "-complete";
            var states = data.FindProperty("states"); states.arraySize = lines.Length + 1;
            for (int i = 0; i < states.arraySize; i++)
            {
                bool done = i == lines.Length; var state = states.GetArrayElementAtIndex(i);
                string complete = "CitywideAlert-" + stage + "-complete";
                S(state, "stateId", done ? complete : "CitywideAlert-" + stage + "-" + i);
                state.FindPropertyRelative("kind").intValue = (int)(done
                    ? stage == "debrief" ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff
                    : NarrativeStateKind.PanelDialogue);
                S(state, "continueStateId", done ? string.Empty : i + 1 == lines.Length ? complete : "CitywideAlert-" + stage + "-" + (i + 1));
                S(state, "skipStateId", done ? string.Empty : complete);
                S(state, "completionPayloadId", done ? "request.citywide_alert." + stage + ".complete" : string.Empty);
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
                var group = settings.FindGroup("Citywide Alert Narrative") ?? settings.CreateGroup("Citywide Alert Narrative", false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                string path = ArtRoot + "/" + panel + ".png";
                string guid = AssetDatabase.AssetPathToGUID(path);
                settings.CreateOrMoveEntry(guid, group, false, false).SetAddress("narrative.citywide_alert." + panel.ToLowerInvariant(), false);
                reference = new AssetReferenceSprite(guid) {SubObjectName = Panel(panel, wide, index).name};
                EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings);
            }
            typeof(NarrativeStateRecord).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(state, reference);
        }

        public static void SeedCopyLocalization()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath)
                ?? throw new InvalidOperationException("Game localization catalog missing");
            var tables=new System.Collections.Generic.List<GameLocaleTable>();
            foreach(var locale in catalog.Locales)
            {
                if(locale.LocaleCode!="en"&&locale.LocaleCode!="fa-IR"){tables.Add(locale);continue;}
                bool fa=locale.LocaleCode=="fa-IR";
                var entries=locale.Entries.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
                foreach(var line in Lines())entries[line.Key]=fa?line.Persian:line.English;
                tables.Add(new GameLocaleTable(locale.LocaleCode,locale.DisplayName,locale.ShortLabel,locale.RightToLeft,locale.FontAsset,
                    entries.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new GameLocalizedStringRecord(x.Key,x.Value))));
            }
            catalog.Configure(catalog.SourceLocaleCode,tables);EditorUtility.SetDirty(catalog);
        }
        private static void AddPersian()
        {
            var locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath)
                ?? throw new InvalidOperationException("Persian narrative locale missing");
            var data = new SerializedObject(locale); var text = data.FindProperty("text"); var voices = data.FindProperty("voices");
            for (int i = text.arraySize - 1; i >= 0; i--)
                if (text.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue.StartsWith("narrative.citywide_alert.", StringComparison.Ordinal))
                    text.DeleteArrayElementAtIndex(i);
            for (int i = voices.arraySize - 1; i >= 0; i--)
                if (voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue.StartsWith("citywide_alert-", StringComparison.Ordinal))
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
        private static CitywideAlertNarrativeLine[] Lines() => CH05M01CitywideAlertCopy.Brief.Concat(CH05M01CitywideAlertCopy.Comms)
            .Concat(CH05M01CitywideAlertCopy.Debrief).Concat(CH05M01CitywideAlertCopy.Opening).ToArray();
        private static string VoicePath(string id, bool persian) => VoiceRoot + "/" + (persian ? "fa" : "en") + "/" + id + ".wav";
        private static AudioClip Voice(string id, bool persian) => AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath(id, persian));
        private static Sprite Panel(string panel, bool wide, int index) => AssetDatabase.LoadAllAssetsAtPath(ArtRoot + "/" + panel + ".png")
            .OfType<Sprite>().FirstOrDefault(x => x.name == PanelName(panel, wide, index));
        private static string PanelName(string panel, bool wide, int index) => System.IO.Path.GetFileName(panel) + "-" + (char)('a'+index) + (wide ? "-20x9" : "-16x9");
        private static float Duration(in CitywideAlertNarrativeLine line) => Mathf.Max(8f, Mathf.Max(Voice(line.Id, false)?.length??0,
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
            // Progressive distinct framings retain the chapter artifact and established faces.
            return new Rect(index==0?(width-w)/2:0,(height-h)/2,w,h);
        }
        private static void S(SerializedProperty property, string field, string value) => property.FindPropertyRelative(field).stringValue = value;
    }
}
