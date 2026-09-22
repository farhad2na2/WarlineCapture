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
    public static class CH02M02SupplyLineNarrativeBuilder
    {
        public const string Path = "Assets/Game/Configs/Narrative/Chapter02/CH02M02_SupplyLine_Narrative.asset";
        [MenuItem("Game/Campaign/Supply Line/Build Captioned Narrative")]
        public static void BuildAndInstall()=>Build(false);
        public static void BuildCaptionedArtAndInstall()=>Build(false);
        private static void Build(bool voices)
        {
            CH02M02SupplyLineMediaImporter.ConfigureArt();
            System.IO.Directory.CreateDirectory("Assets/Game/Configs/Narrative/Chapter02"); AssetDatabase.Refresh();
            SupplyLineNarrativeLine[][] lines = {CH02M02SupplyLineCopy.Brief,CH02M02SupplyLineCopy.Comms,CH02M02SupplyLineCopy.Debrief};
            string[] stages = {"brief","comms","debrief"};
            var basis = AssetDatabase.LoadAllAssetsAtPath(M02EstablishBaseNarrativeConfigBuilder.NarrativePath).OfType<NarrativeSequenceConfig>().ToArray();
            for (int i=0;i<stages.Length;i++) Configure(stages[i],lines[i],basis.Single(s => s.SequenceId.EndsWith("."+(stages[i]=="opening" ? "brief" : stages[i]),StringComparison.Ordinal)));
            AddPersian(); AssetDatabase.SaveAssets();
            Scene scene = EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            MenuBootstrapView bootstrap = UnityEngine.Object.FindAnyObjectByType<MenuBootstrapView>(FindObjectsInactive.Include);
            if (bootstrap == null) throw new InvalidOperationException("Menu bootstrap missing.");
            var merged = (bootstrap.CampaignMissionNarrativeConfigs ?? System.Array.Empty<NarrativeSequenceConfig>())
                .Where(s => s != null && !s.SequenceId.StartsWith("seq.ch02.m02.",StringComparison.Ordinal))
                .Concat(AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>()).OrderBy(s => s.SequenceId,StringComparer.Ordinal).ToArray();
            SerializedObject menu = new(bootstrap); SerializedProperty configs = menu.FindProperty("campaignMissionNarrativeConfigs"); configs.arraySize = merged.Length;
            for(int i=0;i<merged.Length;i++) configs.GetArrayElementAtIndex(i).objectReferenceValue = merged[i];
            menu.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("[CH02M02SupplyLineNarrativeBuilder] result=Passed sequences=3 presentation=CaptionedCheckpoint locales=en,fa-IR voices="+voices);
        }

        private static void Configure(string stage,SupplyLineNarrativeLine[] lines,NarrativeSequenceConfig basis)
        {
            string sequenceId = stage=="opening" ? "seq.ch02.open.broken_grid" : "seq.ch02.m02."+stage;
            bool debrief=stage.StartsWith("debrief",StringComparison.Ordinal);
            NarrativeSequenceConfig sequence = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<NarrativeSequenceConfig>().FirstOrDefault(s => s.SequenceId == sequenceId);
            if(sequence == null)
            {
                sequence = ScriptableObject.CreateInstance<NarrativeSequenceConfig>(); sequence.name = "CH02M02_SupplyLine_"+stage;
                if(AssetDatabase.LoadMainAssetAtPath(Path) == null) AssetDatabase.CreateAsset(sequence,Path); else AssetDatabase.AddObjectToAsset(sequence,Path);
            }
            EditorUtility.CopySerialized(basis,sequence);
            sequence.name="CH02M02_SupplyLine_"+stage;
            SerializedObject data = new(sequence);
            data.FindProperty("sequenceId").stringValue = sequenceId;
            data.FindProperty("entryStateId").stringValue = "SupplyLine-"+stage+"-0";
            data.FindProperty("defaultSkipDestinationId").stringValue = "SupplyLine-"+stage+"-complete";
            SerializedProperty states = data.FindProperty("states"); states.arraySize = lines.Length+1;
            for(int i=0;i<states.arraySize;i++)
            {
                bool complete = i==lines.Length; SerializedProperty state = states.GetArrayElementAtIndex(i);
                string completionId = "SupplyLine-"+stage+"-complete";
                S(state,"stateId",complete ? completionId : "SupplyLine-"+stage+"-"+i);
                state.FindPropertyRelative("kind").intValue = (int)(complete ? debrief ? NarrativeStateKind.RouteArrival : NarrativeStateKind.RouteHandoff : NarrativeStateKind.PanelDialogue);
                S(state,"continueStateId",complete ? string.Empty : i+1==lines.Length ? completionId : "SupplyLine-"+stage+"-"+(i+1));
                S(state,"skipStateId",complete ? string.Empty : completionId);
                S(state,"completionPayloadId",complete ? "request.supply_line."+(stage=="brief" ? "interactive_brief" : stage)+".complete" : string.Empty);
                state.FindPropertyRelative("routeRole").intValue = (int)(complete && debrief ? NarrativeRouteRole.DebriefArrival : NarrativeRouteRole.None);
                state.FindPropertyRelative("reducedMotionSupported").boolValue = true;
                state.FindPropertyRelative("motionPreset").intValue = (int)NarrativeMotionPreset.Static;
                state.FindPropertyRelative("musicCue").intValue = (int)NarrativeMusicCue.Briefing;
                state.FindPropertyRelative("ambienceCue").intValue = (int)NarrativeAmbienceCue.CityConflict;
                state.FindPropertyRelative("eventCue").intValue = (int)NarrativeEventCue.Radio;
                state.FindPropertyRelative("evidenceIds").arraySize = 0; state.FindPropertyRelative("missionContextFlags").arraySize = 0;
                string panel=PanelId(stage,i);
                state.FindPropertyRelative("panel16x9").objectReferenceValue = null;
                state.FindPropertyRelative("panel20x9").objectReferenceValue = null;
                state.FindPropertyRelative("durationSeconds").floatValue = complete ? 0 : Duration(lines[i]);
                SerializedProperty authored = state.FindPropertyRelative("lines"); authored.arraySize = complete ? 0 : 1;
                if(complete) continue;
                SerializedProperty line = authored.GetArrayElementAtIndex(0); SupplyLineNarrativeLine copy = lines[i];
                S(line,"lineId",copy.Id); S(line,"textKey",copy.Key); S(line,"englishFallback",copy.English);
                line.FindPropertyRelative("speaker").intValue = (int)copy.Speaker;
                line.FindPropertyRelative("voiceClip").objectReferenceValue = null;
                line.FindPropertyRelative("femaleVoiceClip").objectReferenceValue = null;
                line.FindPropertyRelative("neutralVoiceClip").objectReferenceValue = null;
                line.FindPropertyRelative("startSeconds").floatValue = 0;
                line.FindPropertyRelative("deadlineSeconds").floatValue = Duration(copy);
                line.FindPropertyRelative("essentialCaption").boolValue = true;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            for(int i=0;i<sequence.States.Count;i++)
            {
                string panel=i==lines.Length ? null : PanelId(stage,i);
                SetPanelReference(sequence.States[i],"panel16x9Reference",panel,false);
                SetPanelReference(sequence.States[i],"panel20x9Reference",panel,true);
            }
            EditorUtility.SetDirty(sequence);
        }

        private static string PanelId(string stage,int index) => (stage switch
        {
            "opening" => "O", "brief" => "B", "comms" => "C", "debrief" => "D",
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        })+(index+1).ToString("00");

        private static string ArtForPanel(string panel) => panel.StartsWith("B",StringComparison.Ordinal)?"SupplyChain":panel=="D01"?"EmergencyServices":"Manifest";
        private static void SetPanelReference(NarrativeStateRecord state,string field,string panel,bool wide)
        {
            AssetReferenceSprite reference=null;
            if(panel!=null)
            {
                var settings=AddressableAssetSettingsDefaultObject.GetSettings(true)??throw new InvalidOperationException("Addressables settings missing.");
                var group=settings.FindGroup("Supply Line Narrative")??settings.CreateGroup("Supply Line Narrative",false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
                var schema=group.GetSchema<BundledAssetGroupSchema>();schema.BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                string art=ArtForPanel(panel);var sprite=CH02M02SupplyLineMediaImporter.Panel(art,wide);
                string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite));
                settings.CreateOrMoveEntry(guid,group,false,false).SetAddress("narrative.supply_line."+art,false);
                reference=new AssetReferenceSprite(guid){SubObjectName=sprite.name};
                EditorUtility.SetDirty(schema);EditorUtility.SetDirty(group);EditorUtility.SetDirty(settings);
            }
            typeof(NarrativeStateRecord).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(state,reference);
        }

        private static void AddPersian()
        {
            NarrativeLocaleConfig locale = AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
            SerializedObject data = new(locale); SerializedProperty entries = data.FindProperty("text");
            for(int i=entries.arraySize-1;i>=0;i--)
                if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue.StartsWith("narrative.supply_line.",StringComparison.Ordinal)) entries.DeleteArrayElementAtIndex(i);
            SerializedProperty voices=data.FindProperty("voices");
            for(int i=voices.arraySize-1;i>=0;i--)
                if(voices.GetArrayElementAtIndex(i).FindPropertyRelative("lineId").stringValue.StartsWith("supply_line-",StringComparison.Ordinal)) voices.DeleteArrayElementAtIndex(i);
            foreach(SupplyLineNarrativeLine line in CH02M02SupplyLineCopy.Brief.Concat(CH02M02SupplyLineCopy.Comms).Concat(CH02M02SupplyLineCopy.Debrief))
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize++);
                S(entry,"key",line.Key); S(entry,"value",line.Persian);
                var voice=voices.GetArrayElementAtIndex(voices.arraySize++);
                S(voice,"lineId",line.Id);
                voice.FindPropertyRelative("voiceClip").objectReferenceValue=null;
                voice.FindPropertyRelative("femaleVoiceClip").objectReferenceValue=null;
                voice.FindPropertyRelative("neutralVoiceClip").objectReferenceValue=null;
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(locale);
        }
        private static float Duration(in SupplyLineNarrativeLine line)=>Mathf.Max(8,Mathf.Max(line.English.Length,line.Persian.Length)/14f);
        private static void S(SerializedProperty property,string field,string value) => property.FindPropertyRelative(field).stringValue=value;
    }
}
