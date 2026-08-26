using System;
using System.Collections.Generic;
using System.Linq;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using Game.Narrative.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class M02EstablishBaseNarrativeConfigBuilder
    {
        public const string NarrativePath =
            "Assets/Game/Configs/Narrative/Chapter01/M02_EstablishBase_Narrative.asset";
        public const string BriefSequenceId = "seq.ch01.m02.brief";
        public const string CommsSequenceId = "seq.ch01.m02.comms";
        public const string DebriefSequenceId = "seq.ch01.m02.debrief";
        public const string M03MissionId = "saga.ch01.m03.radar_warning";
        public const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

        [MenuItem("Game/Campaign/M02/Build And Install Narrative")]
        public static void BuildAndInstall()
        {
            Build();
            InstallIntoMenu();
            Debug.Log("[M02EstablishBaseNarrativeBuilder] result=Passed scope=BuildAndInstall sequences=3");
        }

        public static void Build()
        {
            M02EstablishBaseNarrativeArtImporter.ConfigureProvisionalArtImports();
            EnsureFolder("Assets/Game/Configs/Narrative/Chapter01");
            Configure(
                Sequence(BriefSequenceId, "M02_EstablishBase_Brief"),
                BriefSequenceId,
                "M02-BRIEF",
                NarrativeRouteRole.None,
                "request.m02.interactive_brief.complete",
                new[] { "story_archive.seq.ch01.m02.brief" },
                new[] { "story.m02.forward_post_civic_purpose" },
                M02EstablishBaseNarrativeArtImporter.BriefPanelPath,
                (NarrativeSpeakerId.Dalia, "brief.dalia",
                    "The abandoned JRC forward post is the only command point left on this district edge. Restore it, then hold its defense lane."),
                (NarrativeSpeakerId.Aria, "brief.aria",
                    "Once the Barracks and rifle squad are operational, the post can restore local response channels."),
                (NarrativeSpeakerId.Samira, "brief.samira",
                    "This post protects the clinic route and municipal crews still moving through Old Market. Reopening it is a civic lifeline, not only a military position."));
            Configure(
                Sequence(CommsSequenceId, "M02_EstablishBase_Comms"),
                CommsSequenceId,
                "M02-COMMS",
                NarrativeRouteRole.None,
                "request.m02.comms.complete",
                new[]
                {
                    "story_archive.seq.ch01.m02.comms",
                    "evidence.m02.municipal_access_list"
                },
                new[]
                {
                    "story.m02.municipal_access_list_recovered",
                    "story.m02.access_list_stolen_before_attack"
                },
                M02EstablishBaseNarrativeArtImporter.CommsPanelPath,
                (NarrativeSpeakerId.Dalia, "comms.dalia",
                    "Hold the post. The incoming cell cannot be allowed through to the clinic route."),
                (NarrativeSpeakerId.Aria, "comms.aria",
                    "A municipal access list was recovered from the attackers. Its copy timestamp predates the first strike."),
                (NarrativeSpeakerId.Samira, "comms.samira",
                    "That list maps substations, maintenance gates, and service tunnels. It was stolen before the attack."));
            Configure(
                Sequence(DebriefSequenceId, "M02_EstablishBase_Debrief"),
                DebriefSequenceId,
                "M02-DEBRIEF",
                NarrativeRouteRole.DebriefArrival,
                "request.m02.debrief.complete",
                new[]
                {
                    "story_archive.seq.ch01.m02.debrief",
                    "evidence.m02.municipal_access_list"
                },
                new[]
                {
                    "story.m02.forward_post_operational",
                    "story.dalia.field_lead_accepted",
                    "story.m02.warning_sector_dark",
                    "campaign.highlight." + M03MissionId
                },
                M02EstablishBaseNarrativeArtImporter.DebriefPanelPath,
                (NarrativeSpeakerId.Samira, "debrief.samira",
                    "The forward post is operational. Clinic and municipal response channels are back online."),
                (NarrativeSpeakerId.Dalia, "debrief.dalia",
                    "Commander, Dalia Rahim. I accept the field-lead role. I will direct the ground response from this post."),
                (NarrativeSpeakerId.Aria, "debrief.aria",
                    "The warning sector toward the next operation has gone dark. Armored movement is approaching through it."));
            AssetDatabase.SaveAssets();
        }

        public static void InstallIntoMenu()
        {
            AssetDatabase.ImportAsset(NarrativePath, ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            NarrativeSequenceConfig[] m02 = AssetDatabase.LoadAllAssetsAtPath(NarrativePath)
                .OfType<NarrativeSequenceConfig>()
                .OrderBy(sequence => sequence.SequenceId, StringComparer.Ordinal)
                .ToArray();
            if (m02.Length != 3)
                throw new InvalidOperationException("M02 narrative must contain exactly three sequences.");
            foreach (NarrativeSequenceConfig sequence in m02)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        sequence,
                        out string guid,
                        out long localId) ||
                    string.IsNullOrWhiteSpace(guid) || localId == 0)
                {
                    throw new InvalidOperationException(
                        $"M02 narrative sequence {sequence.SequenceId} has no persistent asset identity.");
                }
            }

            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(
                FindObjectsInactive.Include);
            if (bootstrap == null)
                throw new InvalidOperationException("Menu scene is missing MenuBootstrapView.");

            NarrativeSequenceConfig[] existing = bootstrap.CampaignMissionNarrativeConfigs ??
                Array.Empty<NarrativeSequenceConfig>();
            NarrativeSequenceConfig[] merged = existing
                .Where(sequence => sequence != null &&
                    !sequence.SequenceId.StartsWith("seq.ch01.m02.", StringComparison.Ordinal))
                .Concat(m02)
                .OrderBy(sequence => sequence.SequenceId, StringComparer.Ordinal)
                .ToArray();
            SerializedObject serialized = new(bootstrap);
            SerializedProperty configs = serialized.FindProperty("campaignMissionNarrativeConfigs");
            if (configs == null)
                throw new InvalidOperationException(
                    "MenuBootstrapView is missing campaignMissionNarrativeConfigs.");
            configs.arraySize = merged.Length;
            for (int index = 0; index < merged.Length; index++)
                configs.GetArrayElementAtIndex(index).objectReferenceValue = merged[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static NarrativeSequenceConfig Sequence(string id, string name)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(NarrativePath))
            {
                if (asset is NarrativeSequenceConfig sequence && sequence.SequenceId == id)
                    return sequence;
            }

            NarrativeSequenceConfig created = ScriptableObject.CreateInstance<NarrativeSequenceConfig>();
            created.name = name;
            if (AssetDatabase.LoadMainAssetAtPath(NarrativePath) == null)
                AssetDatabase.CreateAsset(created, NarrativePath);
            else
                AssetDatabase.AddObjectToAsset(created, NarrativePath);
            return created;
        }

        private static void Configure(
            NarrativeSequenceConfig target,
            string sequenceId,
            string prefix,
            NarrativeRouteRole routeRole,
            string payloadId,
            string[] evidenceIds,
            string[] contextFlags,
            string panelPath,
            params (NarrativeSpeakerId speaker, string key, string text)[] lines)
        {
            SerializedObject serialized = new(target);
            string dialogueId = prefix + "-DIALOGUE";
            string completionId = prefix + "-COMPLETE";
            Set(serialized.FindProperty("sequenceId"), sequenceId);
            Set(serialized.FindProperty("entryStateId"), dialogueId);
            Set(serialized.FindProperty("defaultSkipDestinationId"), completionId);

            SerializedProperty states = serialized.FindProperty("states");
            states.arraySize = 2;
            PopulateState(
                states.GetArrayElementAtIndex(0),
                dialogueId,
                NarrativeStateKind.PanelDialogue,
                completionId,
                completionId,
                NarrativeRouteRole.None,
                string.Empty,
                Array.Empty<string>(),
                Array.Empty<string>());
            BindProvisionalPanel(states.GetArrayElementAtIndex(0), panelPath);

            SerializedProperty authoredLines = states.GetArrayElementAtIndex(0).FindPropertyRelative("lines");
            authoredLines.arraySize = lines.Length;
            for (int index = 0; index < lines.Length; index++)
            {
                SerializedProperty line = authoredLines.GetArrayElementAtIndex(index);
                Set(line.FindPropertyRelative("lineId"), prefix.ToLowerInvariant() + ".line." + (index + 1));
                Set(line.FindPropertyRelative("textKey"), "narrative.m02." + lines[index].key);
                Set(line.FindPropertyRelative("englishFallback"), lines[index].text);
                line.FindPropertyRelative("speaker").enumValueIndex = (int)lines[index].speaker;
                line.FindPropertyRelative("startSeconds").floatValue = index * 4f;
                line.FindPropertyRelative("deadlineSeconds").floatValue = index * 4f + 3.75f;
                line.FindPropertyRelative("essentialCaption").boolValue = true;
            }

            states.GetArrayElementAtIndex(0).FindPropertyRelative("durationSeconds").floatValue = lines.Length * 4f;
            NarrativeStateKind completionKind = routeRole == NarrativeRouteRole.DebriefArrival
                ? NarrativeStateKind.RouteArrival
                : NarrativeStateKind.RouteHandoff;
            PopulateState(
                states.GetArrayElementAtIndex(1),
                completionId,
                completionKind,
                string.Empty,
                string.Empty,
                routeRole,
                payloadId,
                evidenceIds,
                contextFlags);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void BindProvisionalPanel(SerializedProperty state, string panelPath)
        {
            Sprite panel = AssetDatabase.LoadAssetAtPath<Sprite>(panelPath);
            if (panel == null)
                throw new InvalidOperationException($"M02 provisional narrative panel is missing: {panelPath}");
            state.FindPropertyRelative("panel16x9").objectReferenceValue = panel;
            state.FindPropertyRelative("panel20x9").objectReferenceValue = panel;
        }

        private static void PopulateState(
            SerializedProperty state,
            string id,
            NarrativeStateKind kind,
            string next,
            string skip,
            NarrativeRouteRole routeRole,
            string payload,
            string[] evidence,
            string[] flags)
        {
            Set(state.FindPropertyRelative("stateId"), id);
            state.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            Set(state.FindPropertyRelative("continueStateId"), next);
            Set(state.FindPropertyRelative("skipStateId"), skip);
            state.FindPropertyRelative("reducedMotionSupported").boolValue = true;
            state.FindPropertyRelative("motionPreset").enumValueIndex = (int)NarrativeMotionPreset.Static;
            state.FindPropertyRelative("musicCue").enumValueIndex = (int)NarrativeMusicCue.Briefing;
            state.FindPropertyRelative("ambienceCue").enumValueIndex = (int)NarrativeAmbienceCue.CityConflict;
            state.FindPropertyRelative("eventCue").enumValueIndex = (int)NarrativeEventCue.Radio;
            state.FindPropertyRelative("routeRole").enumValueIndex = (int)routeRole;
            Set(state.FindPropertyRelative("completionPayloadId"), payload);
            SetStrings(state.FindPropertyRelative("evidenceIds"), evidence);
            SetStrings(state.FindPropertyRelative("missionContextFlags"), flags);
            if (state.FindPropertyRelative("lines").arraySize != 0)
                state.FindPropertyRelative("lines").arraySize = 0;
        }

        private static void SetStrings(SerializedProperty array, IReadOnlyList<string> values)
        {
            array.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
                Set(array.GetArrayElementAtIndex(index), values[index]);
        }

        private static void Set(SerializedProperty property, string value) => property.stringValue = value;

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
