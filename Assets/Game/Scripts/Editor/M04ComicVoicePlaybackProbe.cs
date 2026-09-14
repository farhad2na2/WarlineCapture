using System;
using System.Collections.Generic;
using System.Linq;
using Game.Composition;
using Game.Catalog.Contracts;
using Game.Configs;
using Game.UI.Runtime;
using Game.UI.Contracts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    // Exercises the shipping sequence player, locale resolver and UI prefab at real playback speed.
    [InitializeOnLoad]
    public static class M04ComicVoicePlaybackProbe
    {
        private const string Active="Warline.M04.ComicVoiceProbe";
        private static readonly Dictionary<string,float> Progress=new();
        private static readonly HashSet<string> Started=new();
        private static FirstLaunchNarrativeSequencePresentationSystemHelper player;
        private static NarrativeSequenceView view;
        private static GameObject canvas;
        private static bool prepared,completeSequence,finished;
        private static int sequenceIndex;
        private static double started,lastTick;
        private static AudioClip previousClip;
        private static string previousState,error;
        static M04ComicVoicePlaybackProbe(){if(SessionState.GetBool(Active,false)){EditorApplication.update+=Tick;Application.logMessageReceived+=Observe;}}
        public static void Run()
        {
            M04ComicAudioValidation.Validate();SessionState.SetBool(Active,true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished || !EditorApplication.isPlaying)return;
            try
            {
                if(error!=null)throw new InvalidOperationException(error);
                double now=EditorApplication.timeSinceStartup;
                if(!prepared)
                {
                    started=lastTick=now;prepared=true;sequenceIndex=0;Started.Clear();Progress.Clear();
                    new GameObject("ComicAudioListener",typeof(AudioListener));
                    canvas=new GameObject("ComicQA",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
                    canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                    var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(4800,2160);
                    GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath),"fa-IR",false);
                    StartSequence();return;
                }
                if(now-started>420)throw new TimeoutException("M4 comic voice playback exceeded seven minutes");
                player.Tick((float)Math.Min(now-lastTick,.1));lastTick=now;
                SampleVoice();
                if(!completeSequence)return;
                player.Cancel();UnityEngine.Object.Destroy(view.gameObject);sequenceIndex++;
                if(sequenceIndex<6){StartSequence();return;}
                if(Started.Count!=14)throw new InvalidOperationException("Expected 14 unique localized lines, got "+Started.Count);
                foreach(var pair in Progress)
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(pair.Key);
                    if(pair.Value<clip.length-.3f)throw new InvalidOperationException("Narration ended early: "+pair.Key+" heard="+pair.Value+" length="+clip.length);
                }
                Finish(true,"all 14 localized story clips played once to completion; no English clip in Farsi; speech-free non-looping radio; three sequences per language");
            }
            catch(Exception exception){Debug.LogException(exception);Finish(false,exception.Message);}
        }
        private static void StartSequence()
        {
            bool fa=sequenceIndex<3;string stage=new[]{"brief","comms","debrief"}[sequenceIndex%3];
            GameLocalization.SetLocale(fa?"fa-IR":"en",false);MainMenuV3PrefabBuilder.SetGameViewResolution(fa?2400:1920,1080);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            view=UnityEngine.Object.Instantiate(prefab,canvas.transform).GetComponent<NarrativeSequenceView>();
            var sequence=AssetDatabase.LoadAllAssetsAtPath(M04AirliftNarrativeBuilder.Path).OfType<NarrativeSequenceConfig>().Single(s=>s.SequenceId=="seq.ch01.m04."+stage);
            var locale=fa?AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath):null;
            player=new FirstLaunchNarrativeSequencePresentationSystemHelper();
            var resolver=new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(FallbackGameTextResolver.Instance);
            if(!player.Initialize(sequence,AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath),
                AssetDatabase.LoadAssetAtPath<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath),view,resolver,Game.UI.Runtime.SettingsService.Defaults,locale))
                throw new InvalidOperationException("M4 comic player initialization failed");
            completeSequence=false;previousClip=null;previousState=null;
            player.HandoffRequested+=_=>completeSequence=true;
            if(!player.Start())throw new InvalidOperationException("M4 comic failed to start");
            Debug.Log("[M04ComicVoicePlayback] start="+sequence.SequenceId+" locale="+GameLocalization.CurrentLocaleCode);
        }
        private static void SampleVoice()
        {
            var source=view.VoiceSource;var radio=view.SequenceAudioView.EventSource;
            if(radio.clip!=null && (AssetDatabase.GetAssetPath(radio.clip)!=M04ComicAudioValidation.RadioPath || radio.loop))
                throw new InvalidOperationException("Wrong or looping comic radio effect");
            if(source.clip==null || !source.isPlaying || source.timeSamples<=0)return;
            string path=AssetDatabase.GetAssetPath(source.clip);string language=sequenceIndex<3?"fa":"en";
            if(!path.StartsWith(M04AirliftMediaImporter.VoiceRoot+"/"+language+"/",StringComparison.Ordinal) || source.loop || source.mute || source.volume<=0)
                throw new InvalidOperationException("Wrong language or inaudible/looping voice: "+path);
            float progress=(float)source.timeSamples/source.clip.frequency;
            if(previousClip!=source.clip || previousState!=player.CurrentStateId)
            {
                if(!Started.Add(path))throw new InvalidOperationException("Comic line repeated: "+path);
                previousClip=source.clip;previousState=player.CurrentStateId;Debug.Log("[M04ComicVoicePlayback] playing="+path);
            }
            if(Progress.TryGetValue(path,out float old) && progress+.1f<old)throw new InvalidOperationException("Comic voice restarted: "+path);
            Progress[path]=progress;
        }
        private static void Observe(string message,string stack,LogType type)
        {
            if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))return;
            if(type is LogType.Exception or LogType.Assert)error=message;
        }
        private static void Finish(bool pass,string detail)
        {
            if(finished)return;finished=true;player?.Cancel();SessionState.SetBool(Active,false);
            EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;
            Debug.Log("[M04ComicVoicePlayback] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
