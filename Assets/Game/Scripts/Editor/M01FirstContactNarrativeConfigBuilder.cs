using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.Narrative.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M01FirstContactNarrativeConfigBuilder
    {
        public const string NarrativePath =
            "Assets/Game/Configs/Narrative/Chapter01/M01_FirstContact_Narrative.asset";
        public const string BriefSequenceId = "seq.ch01.m01.brief";
        public const string CommsSequenceId = "seq.ch01.m01.comms";
        public const string DebriefSequenceId = "seq.ch01.m01.debrief";
        public const string M02MissionId = "saga.ch01.m02.establish_base";

        public static void Build()
        {
            EnsureFolder("Assets/Game/Configs/Narrative/Chapter01");
            Configure(Sequence(BriefSequenceId, "M01_FirstContact_Brief"), BriefSequenceId, "M01-BRIEF",
                NarrativeRouteRole.None, "request.m01.interactive_brief.complete",
                new[] { "story_archive.seq.ch01.m01.brief", "evidence.first_launch.ash_patrol_profiles" },
                new[] { "story.m01.courier_warden_broker_confirmed" },
                (NarrativeSpeakerId.Dalia, "brief.dalia", "First contact is confirmed: three armed Ash Line operatives are blocking the clinic corridor in Old Market."),
                (NarrativeSpeakerId.Aria, "brief.aria", "Threat profiles: Courier carries orders, Warden controls the crossing, and Broker coordinates supplies."),
                (NarrativeSpeakerId.Samira, "brief.samira", "Civilians are sheltering beyond the patrol. Clear the marked corridor; do not pursue outside the district."));
            Configure(Sequence(CommsSequenceId, "M01_FirstContact_Comms"), CommsSequenceId, "M01-COMMS",
                NarrativeRouteRole.None, "request.m01.comms.complete",
                new[] { "story_archive.seq.ch01.m01.comms" }, new[] { "story.m01.corridor_active" },
                (NarrativeSpeakerId.Dalia, "comms.dalia", "Command squad, move to cover and confirm the armed patrol before engaging."),
                (NarrativeSpeakerId.Aria, "comms.aria", "Courier, Warden, and Broker remain inside the marked corridor. Objective updates reflect verified mission state."),
                (NarrativeSpeakerId.Samira, "comms.samira", "The evacuation path is holding. Keep the command squad intact."));
            Configure(Sequence(DebriefSequenceId, "M01_FirstContact_Debrief"), DebriefSequenceId, "M01-DEBRIEF",
                NarrativeRouteRole.DebriefArrival, "request.m01.debrief.complete",
                new[] { "story_archive.seq.ch01.m01.debrief", "evidence.aria.revoked_credential_fragment" },
                new[] { "story.m01.corridor_secured", "story.aria.revoked_credential_clue_found", "campaign.highlight." + M02MissionId },
                (NarrativeSpeakerId.Dalia, "debrief.dalia", "The corridor is secure. The patrol was coordinated, not opportunistic."),
                (NarrativeSpeakerId.Aria, "debrief.aria", "Recovered traffic includes a fragment of a revoked civic-relay credential. The source identity is unresolved."),
                (NarrativeSpeakerId.Samira, "debrief.samira", "Civilians are moving to safety. Command Base is online; the next operation is ready for review."));
            AssetDatabase.SaveAssets();
        }

        private static NarrativeSequenceConfig Sequence(string id, string name)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(NarrativePath))
                if (asset is NarrativeSequenceConfig sequence && sequence.SequenceId == id) return sequence;
            NarrativeSequenceConfig created = ScriptableObject.CreateInstance<NarrativeSequenceConfig>();
            created.name = name;
            if (AssetDatabase.LoadMainAssetAtPath(NarrativePath) == null)
                AssetDatabase.CreateAsset(created, NarrativePath);
            else AssetDatabase.AddObjectToAsset(created, NarrativePath);
            return created;
        }

        private static void Configure(NarrativeSequenceConfig target, string sequenceId, string prefix,
            NarrativeRouteRole routeRole, string payloadId, string[] evidenceIds,
            string[] contextFlags, params (NarrativeSpeakerId speaker, string key, string text)[] lines)
        {
            SerializedObject serialized = new(target);
            string dialogueId = prefix + "-DIALOGUE", completionId = prefix + "-COMPLETE";
            Set(serialized.FindProperty("sequenceId"), sequenceId);
            Set(serialized.FindProperty("entryStateId"), dialogueId);
            Set(serialized.FindProperty("defaultSkipDestinationId"), completionId);
            SerializedProperty states = serialized.FindProperty("states"); states.arraySize = 2;
            PopulateState(states.GetArrayElementAtIndex(0), dialogueId, NarrativeStateKind.PanelDialogue,
                completionId, completionId, NarrativeRouteRole.None, string.Empty, Array.Empty<string>(), Array.Empty<string>());
            SerializedProperty authoredLines = states.GetArrayElementAtIndex(0).FindPropertyRelative("lines");
            authoredLines.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty line = authoredLines.GetArrayElementAtIndex(i);
                Set(line.FindPropertyRelative("lineId"), prefix.ToLowerInvariant() + ".line." + (i + 1));
                Set(line.FindPropertyRelative("textKey"), "narrative.m01." + lines[i].key);
                Set(line.FindPropertyRelative("englishFallback"), lines[i].text);
                line.FindPropertyRelative("speaker").enumValueIndex = (int)lines[i].speaker;
                line.FindPropertyRelative("startSeconds").floatValue = i * 4f;
                line.FindPropertyRelative("deadlineSeconds").floatValue = i * 4f + 3.75f;
                line.FindPropertyRelative("essentialCaption").boolValue = true;
            }
            states.GetArrayElementAtIndex(0).FindPropertyRelative("durationSeconds").floatValue = lines.Length * 4f;
            NarrativeStateKind completionKind = routeRole == NarrativeRouteRole.DebriefArrival
                ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff;
            PopulateState(states.GetArrayElementAtIndex(1), completionId, completionKind, string.Empty, string.Empty,
                routeRole, payloadId, evidenceIds, contextFlags);
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }

        private static void PopulateState(SerializedProperty state, string id, NarrativeStateKind kind,
            string next, string skip, NarrativeRouteRole routeRole, string payload, string[] evidence, string[] flags)
        {
            Set(state.FindPropertyRelative("stateId"), id); state.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            Set(state.FindPropertyRelative("continueStateId"), next); Set(state.FindPropertyRelative("skipStateId"), skip);
            state.FindPropertyRelative("reducedMotionSupported").boolValue = true;
            state.FindPropertyRelative("motionPreset").enumValueIndex = (int)NarrativeMotionPreset.Static;
            state.FindPropertyRelative("musicCue").enumValueIndex = (int)NarrativeMusicCue.Briefing;
            state.FindPropertyRelative("ambienceCue").enumValueIndex = (int)NarrativeAmbienceCue.CityConflict;
            state.FindPropertyRelative("eventCue").enumValueIndex = (int)NarrativeEventCue.Radio;
            state.FindPropertyRelative("routeRole").enumValueIndex = (int)routeRole;
            Set(state.FindPropertyRelative("completionPayloadId"), payload);
            SetStrings(state.FindPropertyRelative("evidenceIds"), evidence);
            SetStrings(state.FindPropertyRelative("missionContextFlags"), flags);
            if (state.FindPropertyRelative("lines").arraySize != 0) state.FindPropertyRelative("lines").arraySize = 0;
        }

        private static void SetStrings(SerializedProperty array, IReadOnlyList<string> values)
        { array.arraySize = values.Count; for (int i = 0; i < values.Count; i++) Set(array.GetArrayElementAtIndex(i), values[i]); }
        private static void Set(SerializedProperty property, string value) => property.stringValue = value;
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/"); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
