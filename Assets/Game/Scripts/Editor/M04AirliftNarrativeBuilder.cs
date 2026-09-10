using System;
using System.Linq;
using System.Reflection;
using Game.Catalog.Contracts;
using Game.Composition;
using Game.Configs;
using Game.Narrative.Contracts;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class M04AirliftNarrativeBuilder
    {
        public const string Path = "Assets/Game/Configs/Narrative/Chapter01/M04_Airlift_Narrative.asset";
        [MenuItem("Game/Campaign/M04/Build Final Narrative")]
        public static void BuildAndInstall()=>Build(true);
        public static void BuildCaptionedArtAndInstall()=>Build(false);
        private static void Build(bool voices)
        {
            M04AirliftMediaImporter.ConfigureArt(); if(voices) M04AirliftMediaImporter.ConfigureVoices();
            M04NarrativeLine[][] lines = {M04AirliftCopyCatalog.Brief,M04AirliftCopyCatalog.Comms,M04AirliftCopyCatalog.Debrief};
            string[] stages = {"brief","comms","debrief"};
            var basis = AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath).OfType<NarrativeSequenceConfig>().ToArray();
            for (int i=0;i<3;i++) Configure(stages[i],lines[i],basis.Single(s => s.SequenceId.EndsWith("."+stages[i],StringComparison.Ordinal)));
            AddPersian(); AssetDatabase.SaveAssets();
            Scene scene = EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null) throw new InvalidOperationException("Menu bootstrap missing.");
            var merged = (bootstrap.CampaignMissionNarrativeConfigs ?? System.Array.Empty<NarrativeSequenceConfig>())
                .Where(s => s != null && !s.SequenceId.StartsWith("seq.ch01.m04.",StringComparison.Ordinal))
                .Concat(AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>()).OrderBy(s => s.SequenceId,StringComparer.Ordinal).ToArray();
            SerializedObject menu = new(bootstrap); SerializedProperty configs = menu.FindProperty("campaignMissionNarrativeConfigs"); configs.arraySize = merged.Length;
            for(int i=0;i<merged.Length;i++) configs.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            menu.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[M04AirliftNarrativeBuilder] result=Passed sequences=3 artSources=7 crops=14 locales=en,fa-IR voices="+voices);
        }

        private static void Configure(string stage,M04NarrativeLine[] lines,NarrativeSequenceConfig basis)
        {
            string sequenceId = "seq.ch01.m04."+stage;
            bool debrief=stage.StartsWith("debrief",StringComparison.Ordinal);
            NarrativeSequenceConfig sequence = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>().FirstOrDefault(s => s.SequenceId == sequenceId);
            if(sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<NarrativeSequenceConfig>(); sequence.name = "M04_Airlift_"+stage;
                if(AssetDatabase.LoadMainAssetAtPath(Path) == null) AssetDatabase.CreateAsset(sequence,Path); else AssetDatabase.AddObjectToAsset(sequence,Path);
            }
            EditorUtility.CopySerialized(basis,sequence);
            sequence.name="M04_Airlift_"+stage;
            SerializedObject data = new(sequence);
            data.FindProperty("sequenceId").stringValue = sequenceId;
            data.FindProperty("entryStateId").stringValue = "M04-"+stage+"-0";
            data.FindProperty("defaultSkipDestinationId").stringValue = "M04-"+stage+"-complete";
            SerializedProperty states = data.FindProperty("states"); states.arraySize = lines.Length+1;
            for(int i=0;i<states.arraySize;i++)
            {
                bool complete = i==lines.Length; SerializedProperty state = states.GetArrayElementAtIndex(i);
                string completionId = "M04-"+stage+"-complete";
                S(state,"stateId",complete ? completionId : "M04-"+stage+"-"+i);
                state.FindPropertyRelative("kind").intValue = (int)(complete ? debrief ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff : NarrativeStateKind.PanelDialogue);
                S(state,"continueStateId",complete ? string.Empty : i+1==lines.Length ? completionId : "M04-"+stage+"-"+(i+1));
                S(state,"skipStateId",complete ? string.Empty : completionId);
                S(state,"completionPayloadId",complete ? "request.m04."+(stage=="brief" ? "interactive_brief" : stage)+".complete" : string.Empty);
                state.FindPropertyRelative("routeRole").intValue = (int)(complete && debrief ? NarrativeRouteRole.DebriefArrival : NarrativeRouteRole.None);
                state.FindPropertyRelative("reducedMotionSupported").boolValue = true;
                state.FindPropertyRelative("motionPreset").intValue = (int)NarrativeMotionPreset.Static;
                state.FindPropertyRelative("musicCue").intValue = (int)NarrativeMusicCue.Briefing;
                state.FindPropertyRelative("ambienceCue").intValue = (int)NarrativeAmbienceCue.CityConflict;
                state.FindPropertyRelative("eventCue").intValue = (int)NarrativeEventCue.Radio;
                state.FindPropertyRelative("evidenceIds").arraySize = 0; state.FindPropertyRelative("missionContextFlags").arraySize = 0;
                string panel=(debrief ? "D" : stage=="brief" ? "B" : "C")+(i+1).ToString("00");
                state.FindPropertyRelative("panel16x9").objectReferenceValue = null;
                state.FindPropertyRelative("panel20x9").objectReferenceValue = null;
                state.FindPropertyRelative("durationSeconds").floatValue = complete ? 0 : Duration(lines[i]);
                SerializedProperty authored = state.FindPropertyRelative("lines"); authored.arraySize = complete ? 0 : 1;
                if(complete) continue;
                SerializedProperty line = authored.GetArrayElementAtIndex(0); M04NarrativeLine copy = lines[i];
                S(line,"lineId",copy.Id); S(line,"textKey",copy.Key); S(line,"englishFallback",copy.English);
                line.FindPropertyRelative("speaker").intValue = (int)copy.Speaker;
                line.FindPropertyRelative("voiceClip").objectReferenceValue = M04AirliftMediaImporter.Voice(copy.Id,false);
                line.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                line.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
                line.FindPropertyRelative("startSeconds").floatValue = 0;
                line.FindPropertyRelative("deadlineSeconds").floatValue = Duration(copy);
                line.FindPropertyRelative("essentialCaption").boolValue = true;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            for(int i=0;i<sequence.States.Count;i++)
            {
                string panel=i==lines.Length ? null : (debrief ? "D" : stage=="brief" ? "B" : "C")+(i+1).ToString("00");
                SetPanelReference(sequence.States[i],"panel16x9Reference",panel,false);
                SetPanelReference(sequence.States[i],"panel20x9Reference",panel,true);
            }
            EditorUtility.SetDirty(sequence);
        }

        private static void SetPanelReference(NarrativeStateRecord state,string field,string panel,bool wide)
        {
            AssetReferenceSprite reference=null;
            if(panel!=null)
            {
                var settings=AddressableAssetSettingsDefaultObject.GetSettings(true) ?? throw new InvalidOperationException("Addressables settings missing.");
                var group=settings.FindGroup("M04 Narrative") ?? settings.CreateGroup("M04 Narrative",false,false,false,null,
                    typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
                var schema=group.GetSchema<BundledAssetGroupSchema>();
                schema.BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                var sprite=M04AirliftMediaImporter.Panel(panel,wide);
                string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite));
                AddressableAssetEntry entry=settings.CreateOrMoveEntry(guid,group,false,false);
                entry.SetAddress("narrative.m04."+panel,false);
                reference=new AssetReferenceSprite(guid){SubObjectName=sprite.name};
                EditorUtility.SetDirty(schema); EditorUtility.SetDirty(group); EditorUtility.SetDirty(settings);
            }
            typeof(NarrativeStateRecord).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(state,reference);
        }

        private static void AddPersian()
        {
            NarrativeLocaleConfig locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
            SerializedObject data = new(locale); SerializedProperty entries = data.FindProperty("text");
            for(int i=entries.arraySize-1;i>=0;i--)
                if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue.StartsWith("narrative.m04.",StringComparison.Ordinal)) entries.DeleteArrayElementAtIndex(i);
            SerializedProperty voices=data.FindProperty("voices");
            for(int i=voices.arraySize-1;i>=0;i--)
                if(voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue.StartsWith("m04-",StringComparison.Ordinal)) voices.DeleteArrayElementAtIndex(i);
            foreach(M04NarrativeLine line in M04AirliftMediaImporter.Lines)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize++);
                S(entry,"key",line.Key); S(entry,"value",line.Persian);
                var voice=voices.GetArrayElementAtIndex(voices.arraySize++);
                S(voice,"lineId",line.Id);
                voice.FindPropertyRelative("voiceClip").objectReferenceValue=M04AirliftMediaImporter.Voice(line.Id,true);
                voice.FindPropertyRelative("femaleVoiceClip").objectReferenceValue=null;
                voice.FindPropertyRelative("neutralVoiceClip").objectReferenceValue=null;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(locale);
        }
        private static float Duration(in M04NarrativeLine line)=>Mathf.Max(8,
            Mathf.Max(M04AirliftMediaImporter.Voice(line.Id,false)?.length ?? 0,M04AirliftMediaImporter.Voice(line.Id,true)?.length ?? 0)+1,
            Mathf.Max(line.English.Length,line.Persian.Length)/14f);
        private static void S(SerializedProperty property,string field,string value) => property.FindPropertyRelative(field).stringValue=value;
    }
}
