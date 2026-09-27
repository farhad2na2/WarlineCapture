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
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class CH03M04EvidenceChainNarrativeBuilder
    {
        public const string Path = "Assets/Game/Configs/Narrative/Chapter03/CH03M04_EvidenceChain_Narrative.asset";

        [MenuItem("Game/Campaign/Evidence Chain/Build Narrative")]
        public static void BuildAndInstall()
        {
            CH03M04EvidenceChainMediaImporter.ConfigureArt();
            CH03M04EvidenceChainMediaImporter.ConfigureVoices();
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Narrative/Chapter03");
            AssetDatabase.Refresh();
            EvidenceChainNarrativeLine[][] lines =
                {CH03M04EvidenceChainCopy.Brief, CH03M04EvidenceChainCopy.Comms, CH03M04EvidenceChainCopy.Debrief};
            string[] stages = {"brief", "comms", "debrief"};
            var basis = AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath)
                .OfType<NarrativeSequenceConfig>().ToArray();
            for (int i = 0; i < stages.Length; i++)
                Configure(stages[i], lines[i], basis.Single(sequence =>
                    sequence.SequenceId.EndsWith("." + stages[i], StringComparison.Ordinal)));
            AddPersian(); AssetDatabase.SaveAssets();
            CampaignMissionComicCoverageValidation.ValidateMissionDistinct(Path, "seq.ch03.m04.");
            Scene scene = EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath, OpenSceneMode.Single);
            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException("Menu bootstrap missing.");
            var merged = (bootstrap.CampaignMissionNarrativeConfigs ?? Array.Empty<NarrativeSequenceConfig>())
                .Where(sequence => sequence != null && !sequence.SequenceId.StartsWith("seq.ch03.m04.", StringComparison.Ordinal))
                .Concat(AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>())
                .OrderBy(sequence => sequence.SequenceId, StringComparer.Ordinal).ToArray();
            var menu = new SerializedObject(bootstrap); var configs = menu.FindProperty("campaignMissionNarrativeConfigs");
            configs.arraySize = merged.Length;
            for (int i = 0; i < merged.Length; i++) configs.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            menu.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[EvidenceChainNarrative] result=Passed sequences=3 dialogueStates=7 locales=en,fa-IR voices=local");
        }

        private static void Configure(string stage, EvidenceChainNarrativeLine[] lines, NarrativeSequenceConfig basis)
        {
            string sequenceId = "seq.ch03.m04." + stage; bool debrief = stage == "debrief";
            var sequence = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>()
                .FirstOrDefault(item => item.SequenceId == sequenceId);
            if (sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<NarrativeSequenceConfig>();
                if (AssetDatabase.LoadMainAssetAtPath(Path) == null) AssetDatabase.CreateAsset(sequence, Path);
                else AssetDatabase.AddObjectToAsset(sequence, Path);
            }
            EditorUtility.CopySerialized(basis, sequence); sequence.name = "CH03M04_EvidenceChain_" + stage;
            var data = new SerializedObject(sequence);
            data.FindProperty("sequenceId").stringValue = sequenceId;
            data.FindProperty("entryStateId").stringValue = "EvidenceChain-" + stage + "-0";
            data.FindProperty("defaultSkipDestinationId").stringValue = "EvidenceChain-" + stage + "-complete";
            var states = data.FindProperty("states"); states.arraySize = lines.Length + 1;
            for (int i = 0; i < states.arraySize; i++)
            {
                bool complete = i == lines.Length;
                var state = states.GetArrayElementAtIndex(i);
                string completion = "EvidenceChain-" + stage + "-complete";
                S(state, "stateId", complete ? completion : "EvidenceChain-" + stage + "-" + i);
                state.FindPropertyRelative("kind").intValue = (int)(complete
                    ? (debrief ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff)
                    : NarrativeStateKind.PanelDialogue);
                S(state, "continueStateId", complete ? string.Empty :
                    i + 1 == lines.Length ? completion : "EvidenceChain-" + stage + "-" + (i + 1));
                S(state, "skipStateId", complete ? string.Empty : completion);
                S(state, "completionPayloadId", complete ? "request.evidence_chain." +
                    (stage == "brief" ? "interactive_brief" : stage) + ".complete" : string.Empty);
                state.FindPropertyRelative("routeRole").intValue = (int)(complete && debrief
                    ? NarrativeRouteRole.DebriefArrival : NarrativeRouteRole.None);
                state.FindPropertyRelative("reducedMotionSupported").boolValue = true;
                state.FindPropertyRelative("motionPreset").intValue = (int)NarrativeMotionPreset.Static;
                state.FindPropertyRelative("musicCue").intValue = (int)NarrativeMusicCue.Briefing;
                state.FindPropertyRelative("ambienceCue").intValue = (int)NarrativeAmbienceCue.CityConflict;
                state.FindPropertyRelative("eventCue").intValue = (int)NarrativeEventCue.Radio;
                state.FindPropertyRelative("evidenceIds").arraySize = 0;
                state.FindPropertyRelative("missionContextFlags").arraySize = 0;
                state.FindPropertyRelative("panel16x9").objectReferenceValue = null;
                state.FindPropertyRelative("panel20x9").objectReferenceValue = null;
                state.FindPropertyRelative("durationSeconds").floatValue = complete ? 0 : Duration(lines[i]);
                var authored = state.FindPropertyRelative("lines"); authored.arraySize = complete ? 0 : 1;
                if (complete) continue;
                var line = authored.GetArrayElementAtIndex(0); var copy = lines[i];
                S(line, "lineId", copy.Id); S(line, "textKey", copy.Key); S(line, "englishFallback", copy.English);
                line.FindPropertyRelative("speaker").intValue = (int)copy.Speaker;
                line.FindPropertyRelative("voiceClip").objectReferenceValue = CH03M04EvidenceChainMediaImporter.Voice(copy.Id, false);
                line.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                line.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
                line.FindPropertyRelative("startSeconds").floatValue = 0;
                line.FindPropertyRelative("deadlineSeconds").floatValue = Duration(copy);
                line.FindPropertyRelative("essentialCaption").boolValue = true;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < sequence.States.Count; i++)
            {
                string panelId = i < lines.Length ? CH03M04EvidenceChainMediaImporter.PanelId(lines[i].Id) : null;
                SetPanelReference(sequence.States[i], "panel16x9Reference", panelId, false);
                SetPanelReference(sequence.States[i], "panel20x9Reference", panelId, true);
            }
            EditorUtility.SetDirty(sequence);
        }

        private static void SetPanelReference(NarrativeStateRecord state, string field, string panelId, bool wide)
        {
            AssetReferenceSprite reference = null;
            if (panelId != null)
            {
                var settings = AddressableAssetSettingsDefaultObject.GetSettings(true)
                    ?? throw new InvalidOperationException("Addressables settings missing.");
                var group = settings.FindGroup("Evidence Chain Narrative") ?? settings.CreateGroup(
                    "Evidence Chain Narrative", false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                string path = $"{CH03M04EvidenceChainMediaImporter.ArtRoot}/{panelId}.png";
                string guid = AssetDatabase.AssetPathToGUID(path);
                settings.CreateOrMoveEntry(guid, group, false, false)
                    .SetAddress($"narrative.evidence_chain.{panelId.ToLowerInvariant()}", false);
                reference = new AssetReferenceSprite(guid)
                    {SubObjectName = CH03M04EvidenceChainMediaImporter.Panel(panelId, wide).name};
                EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings);
            }
            typeof(NarrativeStateRecord).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(state, reference);
        }

        private static void AddPersian()
        {
            var locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(
                M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
            var data = new SerializedObject(locale); var entries = data.FindProperty("text");
            for (int i = entries.arraySize - 1; i >= 0; i--)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue
                    .StartsWith("narrative.evidence_chain.", StringComparison.Ordinal))
                    entries.DeleteArrayElementAtIndex(i);
            var voices = data.FindProperty("voices");
            for (int i = voices.arraySize - 1; i >= 0; i--)
                if (voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue
                    .StartsWith("evidence_chain-", StringComparison.Ordinal))
                    voices.DeleteArrayElementAtIndex(i);
            foreach (var line in CH03M04EvidenceChainMediaImporter.Lines)
            {
                var entry = entries.GetArrayElementAtIndex(entries.arraySize++);
                S(entry, "key", line.Key); S(entry, "value", line.Persian);
                var voice = voices.GetArrayElementAtIndex(voices.arraySize++);
                S(voice, "lineId", line.Id);
                voice.FindPropertyRelative("voiceClip").objectReferenceValue =
                    CH03M04EvidenceChainMediaImporter.Voice(line.Id, true);
                voice.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                voice.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(locale);
        }

        private static float Duration(in EvidenceChainNarrativeLine line)
        {
            var en = CH03M04EvidenceChainMediaImporter.Voice(line.Id, false);
            var fa = CH03M04EvidenceChainMediaImporter.Voice(line.Id, true);
            return Mathf.Max(8f, Mathf.Max(en ? en.length : 0, fa ? fa.length : 0) + .5f,
                Mathf.Max(line.English.Length, line.Persian.Length) / 14f);
        }
        private static void S(SerializedProperty property, string field, string value) =>
            property.FindPropertyRelative(field).stringValue = value;
    }
}
