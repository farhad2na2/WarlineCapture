using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeConfigBuilder
    {
        public const string SequencePath = "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchSequence.asset";
        public const string SpeakerPath = "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchSpeakers.asset";
        public const string PunctuationPath = "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchPunctuation.asset";
        public const string PersianLocalePath = "Assets/Game/Configs/Narrative/FirstLaunch/FirstLaunchPersianLocale.asset";
        private const string PersianCatalogPath = "Assets/Game/Data/Narrative/FirstLaunch/first_launch_persian_text_catalog.json";
        private const string PersianVoiceRoot = "Assets/Game/Audio/Narrative/FirstLaunch/Voice/fa/";
        public const string PanelAddressPrefix = "narrative.first_launch.panel";
        private const string PanelGroupName = "FirstLaunch Narrative Panels";
        private const float DialogueTailHoldSeconds = 0.25f;
        private static AddressableAssetSettings addressableSettings;
        private static AddressableAssetGroup panelGroup;

        private static readonly LineDefinition[] Lines =
        {
            new("FL-P02", "p02_radio", NarrativeSpeakerId.Radio, 17.8f, 25.5f, "Multiple explosions reported across Old Market. Power is failing. The main road to the clinic is blocked."),
            new("FL-P03", "p03_radio", NarrativeSpeakerId.Radio, 25.8f, 34.5f, "Joint Response Command, this is District Dispatch. Do you copy? The district relay is down. The command channel has gone silent."),
            new("FL-P04", "p04_dalia", NarrativeSpeakerId.Dalia, 35f, 46.5f, "District Dispatch, Major Dalia Rahim, JRC Field Command. We found the convoy survivors. Extraction is underway."),
            new("FL-P04", "p04_samira", NarrativeSpeakerId.Samira, 47f, 58.5f, "Field Command, this is Engineer Samira Haddad with Civil Infrastructure. Families and road crews are trapped beyond the clinic route."),
            new("FL-P05", "p05_aria", NarrativeSpeakerId.Aria, 59f, 68.5f, "I'm ARIA, the Civic Relay assistant. Relay integrity is degraded. Command channels remain offline."),
            new("FL-P06", "p06_aria", NarrativeSpeakerId.Aria, 69f, 78.5f, "Emergency continuity protocol is now active. One verified command authority remains."),
            new("FL-P07", "p07_aria", NarrativeSpeakerId.Aria, 79f, 88.5f, "Old Market is the immediate priority. Armed units are closing in on the blocked clinic route."),
            new("FL-P09", "p09_aria", NarrativeSpeakerId.Aria, 89f, 96.5f, "Commander identity confirmed. I will provide tactical support. You retain command authority."),
            new("FL-P10", "p10_aria", NarrativeSpeakerId.Aria, 97f, 104.5f, "The failures were deliberate: power, roads, then command."),
            new("FL-P11", "p11_dalia", NarrativeSpeakerId.Dalia, 105f, 113.5f, "Two JRC squads still active. The nearest forward post has gone dark."),
            new("FL-P12", "p12_samira", NarrativeSpeakerId.Samira, 114f, 122.5f, "Civilians, clinic staff, and municipal crews are still trapped beyond the roadblock."),
            new("FL-P13", "p13_aria", NarrativeSpeakerId.Aria, 123f, 130.5f, "We do not yet have enough evidence to identify who is behind the attack."),
            new("FL-P14", "p14_commander", NarrativeSpeakerId.Commander, 131f, 138.5f, "Link the response teams. Secure the clinic corridor. Confirm targets before engaging."),
            new("FL-P15", "p15_dalia", NarrativeSpeakerId.Dalia, 139f, 148.5f, "Confirmed. Armed Ash Line patrol approaching Old Market. No heavy weapons observed."),
            new("FL-P16", "p16_aria", NarrativeSpeakerId.Aria, 149f, 158.5f, "Three armed hostiles confirmed. Civilians are still behind the barriers. Engage only within the marked corridor."),
            new("FL-P17", "p17_dalia", NarrativeSpeakerId.Dalia, 159f, 166.5f, "Field units standing by. You have tactical control, Commander."),
            new("FL-P18", "p18_aria", NarrativeSpeakerId.Aria, 167f, 176.5f, "Select the rifle squad. Move them into cover. Secure the corridor.")
        };

        [MenuItem("Game/Narrative/First Launch/Build Config Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SequencePath));
            try
            {
                ConfigureAddressables();
                BuildSequence();
                BuildSpeakers();
                BuildPunctuation();
                BuildPersianLocale();
                EditorUtility.SetDirty(addressableSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[FirstLaunchNarrativeConfigBuilder] Built Addressables-backed sequence, speaker, and punctuation assets.");
            }
            finally
            {
                addressableSettings = null;
                panelGroup = null;
            }
        }

        internal static void EnsureV3AriaPortrait()
        {
            NarrativeSpeakerCatalog catalog = AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(SpeakerPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SpeakerPath));
                BuildSpeakers();
                return;
            }

            Sprite v3Aria = Load<Sprite>(V3UiFoundationBuilder.SharedAriaPortraitPath);
            SerializedObject serialized = new(catalog);
            SerializedProperty speakers = serialized.FindProperty("speakers");
            bool found = false;
            for (int i = 0; speakers != null && i < speakers.arraySize; i++)
            {
                SerializedProperty speaker = speakers.GetArrayElementAtIndex(i);
                if (speaker.FindPropertyRelative("speakerId").enumValueIndex != (int)NarrativeSpeakerId.Aria)
                    continue;

                speaker.FindPropertyRelative("identitySprite").objectReferenceValue = v3Aria;
                found = true;
                break;
            }

            if (!found)
                throw new MissingReferenceException("First Launch speaker catalog is missing ARIA.");

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void BuildPersianLocale()
        {
            TextAsset source = Load<TextAsset>(PersianCatalogPath);
            PersianCatalogDto catalog = JsonUtility.FromJson<PersianCatalogDto>(source.text);
            if (catalog == null || catalog.text == null || catalog.lines == null)
                throw new InvalidOperationException($"Invalid Persian narrative catalog: {PersianCatalogPath}");

            List<NarrativeLocaleTextRecord> localizedText = new(catalog.text.Length + catalog.lines.Length);
            for (int i = 0; i < catalog.text.Length; i++)
            {
                PersianTextDto entry = catalog.text[i];
                localizedText.Add(new NarrativeLocaleTextRecord(entry.key, entry.value));
            }
            for (int i = 0; i < catalog.lines.Length; i++)
            {
                PersianLineDto line = catalog.lines[i];
                localizedText.Add(new NarrativeLocaleTextRecord(line.key, line.text));
            }

            List<NarrativeLocaleVoiceRecord> localizedVoices = new(catalog.lines.Length);
            for (int i = 0; i < catalog.lines.Length; i++)
            {
                string lineId = catalog.lines[i].lineId;
                localizedVoices.Add(new NarrativeLocaleVoiceRecord(
                    lineId,
                    AssetDatabase.LoadAssetAtPath<AudioClip>($"{PersianVoiceRoot}{lineId}.wav"),
                    lineId == "p14_commander"
                        ? AssetDatabase.LoadAssetAtPath<AudioClip>($"{PersianVoiceRoot}p14_commander_female.wav")
                        : null,
                    lineId == "p14_commander"
                        ? AssetDatabase.LoadAssetAtPath<AudioClip>($"{PersianVoiceRoot}p14_commander_neutral.wav")
                        : null));
            }

            NarrativeLocaleConfig locale = GetOrCreateAsset<NarrativeLocaleConfig>(PersianLocalePath);
            Set(locale, "localeId", catalog.locale);
            Set(locale, "language", FirstLaunchNarrativeLanguage.Persian);
            Set(locale, "rightToLeft", true);
            Set(locale, "text", localizedText);
            Set(locale, "voices", localizedVoices);
            EditorUtility.SetDirty(locale);
        }

        private static void BuildSequence()
        {
            NarrativeSequenceConfig config = GetOrCreateAsset<NarrativeSequenceConfig>(SequencePath);
            Set(config, "sequenceId", "seq.first_launch.complete_vision_slice");
            Set(config, "entryStateId", "FL-P01");
            Set(config, "defaultSkipDestinationId", "first_launch.m01_handoff");
            List<NarrativeStateRecord> states = new();

            string[] opening = { "FL-P01", "FL-P02", "FL-P03", "FL-P04", "FL-P05", "FL-P06", "FL-P07" };
            foreach (string id in opening)
                states.Add(CreatePanelState(id, NextOpening(id), "first_launch.m01_handoff"));
            NarrativeStateRecord identity = CreateInteractiveState("first_launch.commander_identity", NarrativeStateKind.InteractiveIdentity, "first_launch.guidance_choice", "first_launch.m01_handoff", "FL-P08");
            Set(identity, "routeRole", NarrativeRouteRole.CommanderIdentity);
            states.Add(identity);
            NarrativeStateRecord guidance = CreateInteractiveState("first_launch.guidance_choice", NarrativeStateKind.InteractiveGuidance, "FL-P09", "first_launch.m01_handoff", null);
            Set(guidance, "routeRole", NarrativeRouteRole.GuidanceChoice);
            states.Add(guidance);
            for (int panel = 9; panel <= 18; panel++)
            {
                string id = $"FL-P{panel:00}";
                states.Add(CreatePanelState(id, panel == 18 ? "first_launch.m01_handoff" : $"FL-P{panel + 1:00}", "first_launch.m01_handoff"));
            }
            NarrativeStateRecord handoff = CreateBareState("first_launch.m01_handoff", NarrativeStateKind.RouteHandoff, "first_launch.gameplay_placeholder", "first_launch.m01_handoff");
            Set(handoff, "routeRole", NarrativeRouteRole.MissionHandoff);
            SetCompletion(handoff, "first_launch.m01_handoff_completion");
            states.Add(handoff);
            NarrativeStateRecord gameplay = CreateBareState("first_launch.gameplay_placeholder", NarrativeStateKind.ReviewOnlyPlaceholder, "FL-P19", "FL-P19");
            Set(gameplay, "routeRole", NarrativeRouteRole.ReviewerGameplay);
            states.Add(gameplay);
            for (int panel = 19; panel <= 22; panel++)
            {
                string id = $"FL-P{panel:00}";
                NarrativeStateRecord debrief = CreatePanelState(id, panel == 22 ? "first_launch.command_base_reveal" : $"FL-P{panel + 1:00}", "first_launch.command_base_reveal");
                if (panel == 19)
                    Set(debrief, "routeRole", NarrativeRouteRole.DebriefOpening);
                states.Add(debrief);
            }
            NarrativeStateRecord arrival = CreateBareState("first_launch.command_base_reveal", NarrativeStateKind.RouteArrival, string.Empty, string.Empty);
            Set(arrival, "routeRole", NarrativeRouteRole.DebriefArrival);
            SetCompletion(
                arrival,
                "first_launch.m01_debrief_completion",
                new[] { "evidence.aria.revoked_credential_fragment" },
                new[] { "story.m01.corridor_secured", "story.aria.revoked_credential_clue_found" });
            states.Add(arrival);
            Set(config, "states", states);
            EditorUtility.SetDirty(config);
        }

        private static NarrativeStateRecord CreatePanelState(string id, string next, string skip)
        {
            NarrativeStateRecord state = CreateBareState(id, NarrativeStateKind.PanelDialogue, next, skip);
            SetPanelReferences(state, id);
            Set(state, "motionPreset", ResolveMotion(id));
            Set(state, "durationSeconds", ResolveDuration(id));
            Set(state, "musicCue", ResolveMusic(id));
            Set(state, "ambienceCue", ResolveAmbience(id));
            Set(state, "vehicleCue", ResolveVehicle(id));
            Set(state, "eventCue", ResolveEvent(id));
            if (id == "FL-P01")
            {
                Set(state, "locationTitleKey", "narrative.first_launch.location.sahrin.name");
                Set(state, "locationTitleFallback", "SAHRIN");
                Set(state, "locationSubtitleKey", "narrative.first_launch.location.old_market.context");
                Set(state, "locationSubtitleFallback", "OLD MARKET / 10:00 LOCAL");
            }
            List<NarrativeDialogueLineRecord> records = new();
            foreach (LineDefinition definition in Lines)
            {
                if (definition.StateId != id)
                    continue;
                NarrativeDialogueLineRecord line = new();
                Set(line, "lineId", definition.LineId);
                Set(line, "textKey", $"narrative.first_launch.line.{definition.LineId}");
                Set(line, "englishFallback", definition.Text);
                Set(line, "speaker", definition.Speaker);
                Set(line, "voiceClip", Load<AudioClip>($"Assets/Game/Audio/Narrative/FirstLaunch/Voice/{definition.LineId}.wav"));
                Set(line, "femaleVoiceClip", definition.Speaker == NarrativeSpeakerId.Commander
                    ? Load<AudioClip>("Assets/Game/Audio/Narrative/FirstLaunch/Voice/p14_commander_female.wav")
                    : null);
                Set(line, "neutralVoiceClip", definition.Speaker == NarrativeSpeakerId.Commander
                    ? Load<AudioClip>("Assets/Game/Audio/Narrative/FirstLaunch/Voice/p14_commander_neutral.wav")
                    : null);
                float stateStart = ResolveTimelineStart(id);
                Set(line, "startSeconds", definition.Start - stateStart);
                Set(line, "deadlineSeconds", definition.Deadline - stateStart - DialogueTailHoldSeconds);
                records.Add(line);
            }
            Set(state, "lines", records);
            return state;
        }

        private static NarrativeStateRecord CreateInteractiveState(string id, NarrativeStateKind kind, string next, string skip, string panelId)
        {
            NarrativeStateRecord state = CreateBareState(id, kind, next, skip);
            Set(state, "motionPreset", NarrativeMotionPreset.StaticInteractive);
            if (!string.IsNullOrEmpty(panelId))
                SetPanelReferences(state, panelId);
            return state;
        }

        private static void SetPanelReferences(NarrativeStateRecord state, string panelId)
        {
            Set(state, "panel16x9", null);
            Set(state, "panel20x9", null);
            Set(state, "panel16x9Reference", BuildPanelReference(panelId, "16x9"));
            Set(state, "panel20x9Reference", BuildPanelReference(panelId, "20x9"));
        }

        private static AssetReferenceSprite BuildPanelReference(string panelId, string aspect)
        {
            string assetPath = $"Assets/Game/Art/Narrative/FirstLaunch/Panels/{aspect}/{panelId}.png";
            Sprite sprite = Load<Sprite>(assetPath);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = addressableSettings.CreateOrMoveEntry(guid, panelGroup, false, false);
            entry.SetAddress($"{PanelAddressPrefix}.{aspect}.{panelId}", false);
            AssetReferenceSprite reference = new(guid) { SubObjectName = sprite.name };
            return reference;
        }

        private static void ConfigureAddressables()
        {
            addressableSettings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (addressableSettings == null)
                throw new InvalidOperationException("Unable to create Addressables settings.");
            panelGroup = addressableSettings.FindGroup(PanelGroupName) ?? addressableSettings.CreateGroup(
                PanelGroupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        private static NarrativeStateRecord CreateBareState(string id, NarrativeStateKind kind, string next, string skip)
        {
            NarrativeStateRecord state = new();
            Set(state, "stateId", id);
            Set(state, "kind", kind);
            Set(state, "continueStateId", next);
            Set(state, "skipStateId", skip);
            Set(state, "reducedMotionSupported", true);
            Set(state, "lines", new List<NarrativeDialogueLineRecord>());
            Set(state, "musicCue", NarrativeMusicCue.Briefing);
            Set(state, "ambienceCue", NarrativeAmbienceCue.CityConflict);
            Set(state, "vehicleCue", NarrativeVehicleCue.None);
            Set(state, "eventCue", NarrativeEventCue.None);
            Set(state, "routeRole", NarrativeRouteRole.None);
            Set(state, "completionPayloadId", string.Empty);
            Set(state, "evidenceIds", Array.Empty<string>());
            Set(state, "missionContextFlags", Array.Empty<string>());
            return state;
        }

        private static void SetCompletion(
            NarrativeStateRecord state,
            string payloadId,
            string[] evidenceIds = null,
            string[] missionContextFlags = null)
        {
            Set(state, "completionPayloadId", payloadId);
            Set(state, "evidenceIds", evidenceIds ?? Array.Empty<string>());
            Set(state, "missionContextFlags", missionContextFlags ?? Array.Empty<string>());
        }

        private static void BuildSpeakers()
        {
            NarrativeSpeakerCatalog catalog = GetOrCreateAsset<NarrativeSpeakerCatalog>(SpeakerPath);
            List<NarrativeSpeakerRecord> records = new()
            {
                Speaker(NarrativeSpeakerId.Radio, "DISTRICT DISPATCH", "EMERGENCY OPERATIONS", NarrativeSpeakerTreatment.Radio, FirstLaunchNarrativeDialogueAssetImporter.RadioPortraitPath, new Color(0.76f, 0.72f, 0.6f), "District emergency dispatcher"),
                Speaker(NarrativeSpeakerId.Dalia, "DALIA RAHIM", "JRC FIELD COMMAND", NarrativeSpeakerTreatment.HumanPortrait, FirstLaunchNarrativeDialogueAssetImporter.DaliaPortraitPath, new Color(0.82f, 0.68f, 0.42f)),
                Speaker(NarrativeSpeakerId.Samira, "SAMIRA HADDAD", "CIVIL INFRASTRUCTURE", NarrativeSpeakerTreatment.HumanPortrait, FirstLaunchNarrativeDialogueAssetImporter.SamiraPortraitPath, new Color(0.72f, 0.62f, 0.42f)),
                Speaker(NarrativeSpeakerId.Aria, "ARIA", "CIVIC RELAY ASSISTANT", NarrativeSpeakerTreatment.AriaIcon, V3UiFoundationBuilder.SharedAriaPortraitPath, new Color(0.2f, 0.92f, 1f)),
                Speaker(NarrativeSpeakerId.Commander, "COMMANDER", "JOINT RESPONSE AUTHORITY", NarrativeSpeakerTreatment.Commander, CommanderFallbackPortrait(), new Color(0.86f, 0.82f, 0.7f))
            };
            Set(catalog, "speakers", records);
            EditorUtility.SetDirty(catalog);
        }

        private static NarrativeSpeakerRecord Speaker(NarrativeSpeakerId id, string name, string role, NarrativeSpeakerTreatment treatment, string spritePath, Color color, string accessibleLabel = null)
        {
            return Speaker(id, name, role, treatment, string.IsNullOrEmpty(spritePath) ? null : Load<Sprite>(spritePath), color, accessibleLabel);
        }

        private static NarrativeSpeakerRecord Speaker(NarrativeSpeakerId id, string name, string role, NarrativeSpeakerTreatment treatment, Sprite identitySprite, Color color, string accessibleLabel = null)
        {
            NarrativeSpeakerRecord record = new();
            string key = id.ToString().ToLowerInvariant();
            Set(record, "speakerId", id);
            Set(record, "nameKey", $"narrative.first_launch.speaker.{key}.name");
            Set(record, "nameFallback", name);
            Set(record, "roleKey", $"narrative.first_launch.speaker.{key}.role");
            Set(record, "roleFallback", role);
            Set(record, "accessibleLabelKey", $"narrative.first_launch.speaker.{key}.accessible_label");
            Set(record, "accessibleLabelFallback", string.IsNullOrEmpty(accessibleLabel) ? $"{name}, {role}" : accessibleLabel);
            Set(record, "treatment", treatment);
            Set(record, "identitySprite", identitySprite);
            Set(record, "accentColor", color);
            return record;
        }

        private static Sprite CommanderFallbackPortrait()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                FirstLaunchNarrativeDialogueAssetImporter.CommanderPortraitSheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == "commander_07_faceless")
                    return sprite;
            }

            throw new InvalidOperationException("Missing commander_07_faceless sprite in the Commander portrait sheet.");
        }

        private static void BuildPunctuation()
        {
            NarrativePunctuationConfig profile = GetOrCreateAsset<NarrativePunctuationConfig>(PunctuationPath);
            EditorUtility.SetDirty(profile);
        }

        private static string NextOpening(string id) => id == "FL-P07" ? "first_launch.commander_identity" : $"FL-P{int.Parse(id.Substring(4)) + 1:00}";
        private static float ResolveDuration(string id) => id switch
        {
            "FL-P01" => 15f,
            "FL-P02" => 8f,
            "FL-P03" => 9f,
            "FL-P04" => 24f,
            "FL-P05" or "FL-P06" or "FL-P07" => 10f,
            "FL-P09" or "FL-P10" => 8f,
            "FL-P11" or "FL-P12" => 9f,
            "FL-P13" or "FL-P14" => 8f,
            "FL-P15" or "FL-P16" => 10f,
            "FL-P17" => 8f,
            "FL-P18" => 10f,
            "FL-P19" => 4f,
            "FL-P20" => 5f,
            "FL-P21" => 4f,
            "FL-P22" => 5f,
            _ => 0f
        };

        private static float ResolveTimelineStart(string id) => id switch
        {
            "FL-P02" => 17.5f,
            "FL-P03" => 25.5f,
            "FL-P04" => 34.5f,
            "FL-P05" => 58.5f,
            "FL-P06" => 68.5f,
            "FL-P07" => 78.5f,
            "FL-P09" => 88.5f,
            "FL-P10" => 96.5f,
            "FL-P11" => 104.5f,
            "FL-P12" => 113.5f,
            "FL-P13" => 122.5f,
            "FL-P14" => 130.5f,
            "FL-P15" => 138.5f,
            "FL-P16" => 148.5f,
            "FL-P17" => 158.5f,
            "FL-P18" => 166.5f,
            _ => 0f
        };
        private static NarrativeMotionPreset ResolveMotion(string id) => id switch { "FL-P02" => NarrativeMotionPreset.StaticImpact, "FL-P03" or "FL-P10" or "FL-P16" or "FL-P19" => NarrativeMotionPreset.DriftRight, "FL-P06" or "FL-P13" or "FL-P21" => NarrativeMotionPreset.DriftLeft, "FL-P22" => NarrativeMotionPreset.PullBack, _ => NarrativeMotionPreset.PushIn };

        private static NarrativeMusicCue ResolveMusic(string id) => id is "FL-P02" or "FL-P03" or "FL-P07" or "FL-P10" or "FL-P15" or "FL-P16" or "FL-P17" or "FL-P18"
            ? NarrativeMusicCue.Conflict
            : NarrativeMusicCue.Briefing;

        private static NarrativeAmbienceCue ResolveAmbience(string id) => id switch
        {
            "FL-P01" => NarrativeAmbienceCue.CityDay,
            "FL-P02" or "FL-P03" or "FL-P15" or "FL-P16" or "FL-P17" or "FL-P18" => NarrativeAmbienceCue.Battlefield,
            _ => NarrativeAmbienceCue.CityConflict
        };

        private static NarrativeVehicleCue ResolveVehicle(string id) => id is "FL-P04" or "FL-P11" or "FL-P15" or "FL-P17"
            ? NarrativeVehicleCue.Engine
            : NarrativeVehicleCue.None;

        private static NarrativeEventCue ResolveEvent(string id) => id switch
        {
            "FL-P02" or "FL-P10" or "FL-P15" => NarrativeEventCue.Attack,
            "FL-P03" or "FL-P04" => NarrativeEventCue.Radio,
            "FL-P05" => NarrativeEventCue.AriaBoot,
            "FL-P06" => NarrativeEventCue.Blackout,
            "FL-P07" or "FL-P16" => NarrativeEventCue.SmallArms,
            "FL-P09" or "FL-P18" => NarrativeEventCue.Transition,
            _ => NarrativeEventCue.None
        };

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"Missing asset {path}");
        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (info == null)
                throw new MissingFieldException(target.GetType().FullName, field);
            info.SetValue(target, value);
        }

        private readonly struct LineDefinition
        {
            public readonly string StateId, LineId, Text;
            public readonly NarrativeSpeakerId Speaker;
            public readonly float Start, Deadline;
            public LineDefinition(string stateId, string lineId, NarrativeSpeakerId speaker, float start, float deadline, string text) { StateId = stateId; LineId = lineId; Speaker = speaker; Start = start; Deadline = deadline; Text = text; }
        }

        [Serializable]
        private sealed class PersianCatalogDto
        {
            public string locale;
            public PersianTextDto[] text;
            public PersianLineDto[] lines;
        }

        [Serializable]
        private sealed class PersianTextDto
        {
            public string key;
            public string value;
        }

        [Serializable]
        private sealed class PersianLineDto
        {
            public string lineId;
            public string key;
            public string text;
        }
    }
}
