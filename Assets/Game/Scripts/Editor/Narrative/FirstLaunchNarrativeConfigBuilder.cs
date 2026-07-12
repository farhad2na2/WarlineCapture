using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Configs;
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
        public const string PanelAddressPrefix = "narrative.first_launch.panel";
        private const string PanelGroupName = "FirstLaunch Narrative Panels";
        private const float DialogueTailHoldSeconds = 0.25f;
        private static AddressableAssetSettings addressableSettings;
        private static AddressableAssetGroup panelGroup;

        private static readonly LineDefinition[] Lines =
        {
            new("FL-P02", "p02_radio", NarrativeSpeakerId.Radio, 17.8f, 25.5f, "Multiple explosions across Old Market. Power is failing. The main clinic road is blocked."),
            new("FL-P03", "p03_radio", NarrativeSpeakerId.Radio, 25.8f, 34.5f, "Joint Response Command, respond. The district relay is down. The command channel is silent."),
            new("FL-P04", "p04_dalia", NarrativeSpeakerId.Dalia, 35f, 46.5f, "Major Dalia Rahim, J R C field command. Convoy survivors located. Extraction is underway."),
            new("FL-P04", "p04_samira", NarrativeSpeakerId.Samira, 47f, 58.5f, "Engineer Samira Haddad, civil infrastructure. Families and road crews are trapped beyond the clinic route."),
            new("FL-P05", "p05_aria", NarrativeSpeakerId.Aria, 59f, 68.5f, "I am ARIA, the Civic Relay assistant. Relay integrity is partial. Command channels are offline."),
            new("FL-P06", "p06_aria", NarrativeSpeakerId.Aria, 69f, 78.5f, "Emergency continuity protocol active. One valid command authority remains."),
            new("FL-P07", "p07_aria", NarrativeSpeakerId.Aria, 79f, 88.5f, "Old Market is the immediate priority. Armed movement is closing on the blocked clinic route."),
            new("FL-P09", "p09_aria", NarrativeSpeakerId.Aria, 89f, 96.5f, "Commander identity confirmed. I will advise. Authority remains yours."),
            new("FL-P10", "p10_aria", NarrativeSpeakerId.Aria, 97f, 104.5f, "This was coordinated. Power, roads, and command failed in sequence."),
            new("FL-P11", "p11_dalia", NarrativeSpeakerId.Dalia, 105f, 113.5f, "Two J R C squads are still responding. The nearest forward post is dark."),
            new("FL-P12", "p12_samira", NarrativeSpeakerId.Samira, 114f, 122.5f, "Civilians, clinic staff, and municipal crews remain behind the closure."),
            new("FL-P13", "p13_aria", NarrativeSpeakerId.Aria, 123f, 130.5f, "The pattern suggests coordination. Attribution is not yet reliable."),
            new("FL-P14", "p14_commander", NarrativeSpeakerId.Commander, 131f, 138.5f, "Connect the response. Protect the clinic corridor. Confirm targets before engagement."),
            new("FL-P15", "p15_dalia", NarrativeSpeakerId.Dalia, 139f, 148.5f, "Confirmed armed Ash Line patrol approaching Old Market. No heavy weapons observed."),
            new("FL-P16", "p16_aria", NarrativeSpeakerId.Aria, 149f, 158.5f, "Three armed hostiles confirmed. Civilians remain behind the barriers. Keep fire inside the marked corridor."),
            new("FL-P17", "p17_dalia", NarrativeSpeakerId.Dalia, 159f, 166.5f, "Field units are ready. Tactical control is yours, Commander."),
            new("FL-P18", "p18_aria", NarrativeSpeakerId.Aria, 167f, 176.5f, "Select the rifle squad. Move to cover. Secure the corridor.")
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
            states.Add(CreateInteractiveState("first_launch.commander_identity", NarrativeStateKind.InteractiveIdentity, "first_launch.guidance_choice", "first_launch.m01_handoff", "FL-P08"));
            states.Add(CreateInteractiveState("first_launch.guidance_choice", NarrativeStateKind.InteractiveGuidance, "FL-P09", "first_launch.m01_handoff", null));
            for (int panel = 9; panel <= 18; panel++)
            {
                string id = $"FL-P{panel:00}";
                states.Add(CreatePanelState(id, panel == 18 ? "first_launch.m01_handoff" : $"FL-P{panel + 1:00}", "first_launch.m01_handoff"));
            }
            states.Add(CreateBareState("first_launch.m01_handoff", NarrativeStateKind.RouteHandoff, "first_launch.gameplay_placeholder", "first_launch.m01_handoff"));
            states.Add(CreateBareState("first_launch.gameplay_placeholder", NarrativeStateKind.ReviewOnlyPlaceholder, "FL-P19", "FL-P19"));
            for (int panel = 19; panel <= 22; panel++)
            {
                string id = $"FL-P{panel:00}";
                states.Add(CreatePanelState(id, panel == 22 ? "first_launch.command_base_reveal" : $"FL-P{panel + 1:00}", "first_launch.command_base_reveal"));
            }
            states.Add(CreateBareState("first_launch.command_base_reveal", NarrativeStateKind.RouteArrival, string.Empty, string.Empty));
            Set(config, "states", states);
            EditorUtility.SetDirty(config);
        }

        private static NarrativeStateRecord CreatePanelState(string id, string next, string skip)
        {
            NarrativeStateRecord state = CreateBareState(id, NarrativeStateKind.PanelDialogue, next, skip);
            SetPanelReferences(state, id);
            Set(state, "motionPreset", ResolveMotion(id));
            Set(state, "durationSeconds", ResolveDuration(id));
            if (id == "FL-P01")
            {
                Set(state, "locationTitleKey", "narrative.first_launch.location.sahrin.name");
                Set(state, "locationTitleFallback", "SAHRIN");
                Set(state, "locationSubtitleKey", "narrative.first_launch.location.old_market.context");
                Set(state, "locationSubtitleFallback", "OLD MARKET / 06:42 LOCAL");
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
            return state;
        }

        private static void BuildSpeakers()
        {
            NarrativeSpeakerCatalog catalog = GetOrCreateAsset<NarrativeSpeakerCatalog>(SpeakerPath);
            List<NarrativeSpeakerRecord> records = new()
            {
                Speaker(NarrativeSpeakerId.Radio, "DISTRICT RADIO", "EMERGENCY BROADCAST", NarrativeSpeakerTreatment.Radio, null, new Color(0.76f, 0.72f, 0.6f)),
                Speaker(NarrativeSpeakerId.Dalia, "DALIA RAHIM", "JRC FIELD COMMAND", NarrativeSpeakerTreatment.HumanPortrait, FirstLaunchNarrativeDialogueAssetImporter.DaliaPortraitPath, new Color(0.82f, 0.68f, 0.42f)),
                Speaker(NarrativeSpeakerId.Samira, "SAMIRA HADDAD", "CIVIL INFRASTRUCTURE", NarrativeSpeakerTreatment.HumanPortrait, FirstLaunchNarrativeDialogueAssetImporter.SamiraPortraitPath, new Color(0.72f, 0.62f, 0.42f)),
                Speaker(NarrativeSpeakerId.Aria, "ARIA", "CIVIC RELAY ASSISTANT", NarrativeSpeakerTreatment.AriaIcon, FirstLaunchNarrativeDialogueAssetImporter.AriaIconPath, new Color(0.2f, 0.92f, 1f)),
                Speaker(NarrativeSpeakerId.Commander, "COMMANDER", "JOINT RESPONSE AUTHORITY", NarrativeSpeakerTreatment.Commander, null, new Color(0.86f, 0.82f, 0.7f))
            };
            Set(catalog, "speakers", records);
            EditorUtility.SetDirty(catalog);
        }

        private static NarrativeSpeakerRecord Speaker(NarrativeSpeakerId id, string name, string role, NarrativeSpeakerTreatment treatment, string spritePath, Color color)
        {
            NarrativeSpeakerRecord record = new();
            string key = id.ToString().ToLowerInvariant();
            Set(record, "speakerId", id);
            Set(record, "nameKey", $"narrative.first_launch.speaker.{key}.name");
            Set(record, "nameFallback", name);
            Set(record, "roleKey", $"narrative.first_launch.speaker.{key}.role");
            Set(record, "roleFallback", role);
            Set(record, "accessibleLabelKey", $"narrative.first_launch.speaker.{key}.accessible_label");
            Set(record, "accessibleLabelFallback", $"{name}, {role}");
            Set(record, "treatment", treatment);
            Set(record, "identitySprite", string.IsNullOrEmpty(spritePath) ? null : Load<Sprite>(spritePath));
            Set(record, "accentColor", color);
            return record;
        }

        private static void BuildPunctuation()
        {
            NarrativePunctuationProfile profile = GetOrCreateAsset<NarrativePunctuationProfile>(PunctuationPath);
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
    }
}
