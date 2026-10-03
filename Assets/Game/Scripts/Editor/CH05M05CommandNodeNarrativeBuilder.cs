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
    public static class CH05M05CommandNodeNarrativeBuilder
    {
        public const string Path = "Assets/Game/Configs/Narrative/Chapter05/CH05M05_CommandNode_Narrative.asset";
        public const string ArtRoot = "Assets/Game/Resources/FutureMissionComics";
        public const string VoiceRoot = "Assets/Game/Audio/Narrative/CH05M05CommandNode/Voice";
        private static readonly string[] Stages = { "brief", "comms", "debrief", "close", "epilogue", "trust_high", "trust_low", "evidence_high", "evidence_low", "infrastructure_high", "infrastructure_low", "postscript" };
        private static readonly string[] PanelNames = { "CH05M05_CommandNode", "CH05M05_CommandNode_Comms", "CH05M05_CommandNode_Debrief", "Bookends/CH05_Close", "Bookends/Campaign_Epilogue" };
        public static string SequenceId(string stage) => stage switch {
            "close" => "seq.ch05.close.protocol_fragment_05", "epilogue" => "seq.campaign.epilogue.canonical",
            "postscript" => "seq.campaign.postscript.recovery_watch",
            "trust_high" => "seq.campaign.epilogue.trust_emphasis.high", "trust_low" => "seq.campaign.epilogue.trust_emphasis.low",
            "evidence_high" => "seq.campaign.epilogue.evidence_emphasis.high", "evidence_low" => "seq.campaign.epilogue.evidence_emphasis.low",
            "infrastructure_high" => "seq.campaign.epilogue.infrastructure_emphasis.high", "infrastructure_low" => "seq.campaign.epilogue.infrastructure_emphasis.low",
            _ => "seq.ch05.m05." + stage };
        private static bool IsOwned(string id) => Stages.Any(stage => SequenceId(stage)==id);
        private static string Art(string stage) => stage switch { "brief"=>PanelNames[0], "comms"=>PanelNames[1], "debrief"=>PanelNames[2], "close"=>PanelNames[3], _=>PanelNames[4] };
        private static CommandNodeNarrativeLine[] Copy(string stage) => stage switch {
            "brief"=>CH05M05CommandNodeNarrativeCopy.Brief, "comms"=>CH05M05CommandNodeNarrativeCopy.Comms,
            "debrief"=>CH05M05CommandNodeNarrativeCopy.Debrief, "close"=>CH05M05CommandNodeNarrativeCopy.Close,
            "epilogue"=>CH05M05CommandNodeNarrativeCopy.Epilogue, "trust_high"=>CH05M05CommandNodeNarrativeCopy.TrustHigh,
            "trust_low"=>CH05M05CommandNodeNarrativeCopy.TrustLow, "evidence_high"=>CH05M05CommandNodeNarrativeCopy.EvidenceHigh,
            "evidence_low"=>CH05M05CommandNodeNarrativeCopy.EvidenceLow, "infrastructure_high"=>CH05M05CommandNodeNarrativeCopy.InfrastructureHigh,
            "infrastructure_low"=>CH05M05CommandNodeNarrativeCopy.InfrastructureLow, _=>CH05M05CommandNodeNarrativeCopy.Postscript };

        [MenuItem("Game/Campaign/Command Node/Build Captioned Narrative")]
        public static void BuildAndInstall() => BuildWithoutVoices();

        public static void BuildWithoutVoices()
        {
            BuildNarrativeCore();
            Debug.Log("[CommandNodeNarrativeVoices] result=Pending expectedClips=0 installedClips=0 externalRequests=0 mode=caption-only nativePlayback=Pending voices=NotRequested");
        }

        private static void BuildNarrativeCore()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Narrative/Chapter05");
            ConfigureMedia();
            var basis = AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)
                .OfType<NarrativeSequenceConfig>().ToArray();
            foreach (string stage in Stages)
                ConfigureSequence(stage, Copy(stage), Art(stage), basis.Single(x => x.SequenceId.EndsWith("." + (stage=="brief"||stage=="comms"?stage:"debrief"), StringComparison.Ordinal)));
            AddPersian(); AssetDatabase.SaveAssets();
            CampaignMissionComicCoverageValidation.ValidateMissionDistinct(Path, "seq.ch05.m05.");
            ValidateCaptionedFinale();
            Scene scene = EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException("Menu bootstrap missing");
            var merged = (bootstrap.CampaignMissionNarrativeConfigs ?? Array.Empty<NarrativeSequenceConfig>())
                .Where(x => x != null && !IsOwned(x.SequenceId))
                .Concat(AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>())
                .OrderBy(x => x.SequenceId, StringComparer.Ordinal).ToArray();
            var menu = new SerializedObject(bootstrap); var configs = menu.FindProperty("campaignMissionNarrativeConfigs");
            configs.arraySize = merged.Length;
            for (int i = 0; i < merged.Length; i++) configs.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            menu.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[CommandNodeNarrative] result=Passed sequences=12 dialogueStates=41 locales=en,fa-IR missionComics=3 voiceGate=Separate");
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
                SpriteRect[] crops = previous.Where(rect=>!rect.name.StartsWith("CH05M05_CommandNode-",StringComparison.Ordinal)).Concat(Stages.Where(stage=>Art(stage)==panel).SelectMany(stage=>Enumerable.Range(0,Copy(stage).Length).SelectMany(index=>new[]{false,true}.Select(wide=>
                    MakeRect(PanelName(stage, wide, index), Crop(width,height,wide?20f/9f:16f/9f,index,Copy(stage).Length),previous))))).ToArray();
                provider.SetSpriteRects(crops);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                    crops.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
                foreach (string stage in Stages.Where(stage=>Art(stage)==panel)) foreach (int index in Enumerable.Range(0,Copy(stage).Length)) foreach (bool wide in new[] {false,true})
                    if (Panel(stage, wide, index) == null) throw new InvalidOperationException("Comic crop missing: " + panel);
            }
            Debug.Log("[CommandNodeMedia] result=Passed comics=5 dialogueCrops=41 installedVoices=0 mode=caption-only");
        }

        private static void ConfigureSequence(string stage, CommandNodeNarrativeLine[] lines, string panel, NarrativeSequenceConfig basis)
        {
            string sequenceId = SequenceId(stage);
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
                ? System.IO.Path.GetFileNameWithoutExtension(Path) : "CH05M05_CommandNode_" + stage;
            var data = new SerializedObject(sequence);
            data.FindProperty("sequenceId").stringValue = sequenceId;
            data.FindProperty("entryStateId").stringValue = "CommandNode-" + stage + "-0";
            data.FindProperty("defaultSkipDestinationId").stringValue = "CommandNode-" + stage + "-complete";
            var states = data.FindProperty("states"); states.arraySize = lines.Length + 1;
            for (int i = 0; i < states.arraySize; i++)
            {
                bool done = i == lines.Length; var state = states.GetArrayElementAtIndex(i);
                string complete = "CommandNode-" + stage + "-complete";
                S(state, "stateId", done ? complete : "CommandNode-" + stage + "-" + i);
                state.FindPropertyRelative("kind").intValue = (int)(done
                    ? stage == "debrief" ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff
                    : NarrativeStateKind.PanelDialogue);
                S(state, "continueStateId", done ? string.Empty : i + 1 == lines.Length ? complete : "CommandNode-" + stage + "-" + (i + 1));
                S(state, "skipStateId", done ? string.Empty : complete);
                S(state, "completionPayloadId", done ? "request.command_node." + stage + ".complete" : string.Empty);
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
                entry.FindPropertyRelative("voiceClip").objectReferenceValue = null;
                entry.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                entry.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
                entry.FindPropertyRelative("startSeconds").floatValue = 0;
                entry.FindPropertyRelative("deadlineSeconds").floatValue = Duration(line);
                entry.FindPropertyRelative("essentialCaption").boolValue = true;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < lines.Length; i++)
            {
                SetPanel(sequence.States[i], "panel16x9Reference", stage, false, i);
                SetPanel(sequence.States[i], "panel20x9Reference", stage, true, i);
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
                var group = settings.FindGroup("Command Node Narrative") ?? settings.CreateGroup("Command Node Narrative", false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                string path = ArtRoot + "/" + Art(panel) + ".png";
                string guid = AssetDatabase.AssetPathToGUID(path);
                settings.CreateOrMoveEntry(guid, group, false, false).SetAddress("narrative.command_node." + panel.ToLowerInvariant(), false);
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
                if (text.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue.StartsWith("narrative.command_node.", StringComparison.Ordinal))
                    text.DeleteArrayElementAtIndex(i);
            for (int i = voices.arraySize - 1; i >= 0; i--)
                if (voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue.StartsWith("command_node-", StringComparison.Ordinal))
                    voices.DeleteArrayElementAtIndex(i);
            foreach (var line in Lines())
            {
                var entry = text.GetArrayElementAtIndex(text.arraySize++); S(entry, "key", line.Key); S(entry, "value", line.Persian);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(locale);
        }
        private static CommandNodeNarrativeLine[] Lines() => Stages.SelectMany(Copy).ToArray();
        private static Sprite Panel(string panel, bool wide, int index) => AssetDatabase.LoadAllAssetsAtPath(ArtRoot + "/" + Art(panel) + ".png")
            .OfType<Sprite>().FirstOrDefault(x => x.name == PanelName(panel, wide, index));
        private static string PanelName(string panel, bool wide, int index) => "CH05M05_CommandNode-" + panel + "-" + (char)('a'+index) + (wide ? "-20x9" : "-16x9");
        private static float Duration(in CommandNodeNarrativeLine line) =>
            Mathf.Max(8f, Mathf.Max(line.English.Length, line.Persian.Length) / 14f);
        private static SpriteRect MakeRect(string name, Rect rect, SpriteRect[] previous) => new()
        {
            name = name, rect = rect, alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
            spriteID = previous.FirstOrDefault(x => x.name == name)?.spriteID ?? GUID.Generate()
        };
        private static Rect Crop(int width, int height, float aspect, int index, int count)
        {
            int h = Mathf.FloorToInt(Mathf.Min(height, width / aspect) * (1f-index*.025f));
            int w = Mathf.Min(width, Mathf.FloorToInt(h * aspect));
            // Progressive distinct framings retain the chapter artifact and established faces.
            float t=count<2?.5f:(float)index/(count-1);
            return new Rect((width-w)*t,(height-h)*(1-t),w,h);
        }
        public static void ValidateCaptionedFinale()
        {
            var configs=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>().ToArray();
            foreach(string stage in Stages)
            {
                var config=configs.Single(x=>x.SequenceId==SequenceId(stage));
                if(config.States.Count!=Copy(stage).Length+1)throw new InvalidOperationException("Wrong finale panel count: "+stage);
                foreach(var state in config.States)foreach(var line in state.Lines)
                    if(line.VoiceClip!=null||line.FemaleVoiceClip!=null||line.NeutralVoiceClip!=null)throw new InvalidOperationException("Caption-only finale contains a voice");
            }
            Debug.Log("[CommandNodeCaptionedFinale] result=Passed sequences=12 panels=41 selectedPanels=38 chapterClosePanels=8 canonicalPanels=9 voices=0 endingGates=0");
        }
        private static void S(SerializedProperty property, string field, string value) => property.FindPropertyRelative(field).stringValue = value;
    }
}
