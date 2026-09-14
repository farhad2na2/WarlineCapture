using System;
using System.Linq;
using Game.Composition;
using Game.Configs;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class M04ComicAudioValidation
    {
        public const string RadioPath="Assets/Game/Audio/Narrative/Shared/narrative_radio_squelch_01.wav";
        public static void Install()
        {
            M04AirliftNarrativeBuilder.InstallComicVoices();
            var importer=(AudioImporter)AssetImporter.GetAtPath(RadioPath);
            importer.forceToMono=true;importer.userData="source=procedural-filtered-noise; recordedSpeech=false; generator=Tools/Audio/generate_narrative_radio_squelch.py";
            importer.SaveAndReimport();
            string path=FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var data=new SerializedObject(root.GetComponent<NarrativeSequenceAudioView>());
                data.FindProperty("radioCue").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(RadioPath);
                data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Validate();
        }
        public static void Validate()
        {
            var locale=AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath);
            var selection=new FirstLaunchNarrativePortraitVoiceSelectionPresentationSystemHelper();selection.SetLocale(locale);
            int lines=0;
            foreach(var sequence in AssetDatabase.LoadAllAssetsAtPath(M04AirliftNarrativeBuilder.Path).OfType<NarrativeSequenceConfig>())
                foreach(var state in sequence.States)foreach(var line in state.Lines)
                {
                    var english=M04AirliftMediaImporter.Voice(line.LineId,false);var persian=M04AirliftMediaImporter.Voice(line.LineId,true);
                    if(english==null || persian==null || english==persian || line.VoiceClip!=english || selection.ResolveVoiceClip(line)!=persian)
                        throw new InvalidOperationException("Missing or mismatched M4 localized comic voice: "+line.LineId);
                    if(Mathf.Max(english.length,persian.length)+.9f>state.DurationSeconds || line.DeadlineSeconds>state.DurationSeconds)
                        throw new InvalidOperationException("M4 comic cuts off narration: "+line.LineId);
                    lines++;
                }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            var audio=prefab.GetComponent<NarrativeSequenceAudioView>();
            if(lines!=7 || AssetDatabase.GetAssetPath(audio.RadioCue)!=RadioPath || audio.RadioCue.length>1 || audio.EventSource.loop)
                throw new InvalidOperationException("M4/shared radio mapping is not speech-free and one-shot.");
            Debug.Log("[M04ComicAudioBindings] result=Passed localizedClips=14 sequences=3 voiceWindows=7 speechFreeRadio=1");
        }
    }
}
