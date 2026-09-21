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
    public static class CH02M01GridlockComicPlaybackProbe
    {
        private const string RevisionKey="Warline.Gridlock.ComicRevisions";
        private static bool Revisions=>SessionState.GetBool(RevisionKey,false);
        public static void RunRevisions(){SessionState.SetBool(RevisionKey,true);Run();}
        private const string Active="Warline.Gridlock.ComicProbe";
        private static readonly Dictionary<string,float> Progress=new();
        private static readonly HashSet<string> Started=new();
        private static FirstLaunchNarrativeSequencePresentationSystemHelper player;
        private static NarrativeSequenceView view;
        private static readonly CampaignMissionDebriefCompositionSystemHelper.PlaybackPresentationSystemHelper playback=new();
        private static GameObject canvas;
        private static bool prepared,completeSequence,finished;
        private static int sequenceIndex;
        private static double started,lastTick;
        private static AudioClip previousClip;
        private static string previousState,error;
        static CH02M01GridlockComicPlaybackProbe(){if(SessionState.GetBool(Active,false)){EditorApplication.update+=Tick;Application.logMessageReceived+=Observe;}}
        public static void Run()
        {
            CH02M01GridlockNarrativeBuilder.BuildCaptionedArtAndInstall();SessionState.SetBool(Active,true);
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
                if(now-started>600)throw new TimeoutException("Gridlock comic voice playback exceeded ten minutes");
                player.Tick((float)Math.Min(now-lastTick,.1));lastTick=now;
                playback.Tick();
                SampleVoice();
                if(!completeSequence)return;
                playback.Unbind();player.Cancel();UnityEngine.Object.Destroy(view.gameObject);sequenceIndex++;
                if(sequenceIndex<(Revisions?4:8)){StartSequence();return;}
                if(Started.Count!=(Revisions?4:24))throw new InvalidOperationException("Expected 24 localized panel captures, got "+Started.Count);
                Finish(true,(Revisions?"4 revised panel captures":"24 localized panel captures")+"; EN 16:9 FA 20:9; voice acceptance pending");
            }
            catch(Exception exception){Debug.LogException(exception);Finish(false,exception.Message);}
        }
        private static void StartSequence()
        {
            bool fa=sequenceIndex<(Revisions?2:4);string stage=Revisions?new[]{"brief","debrief"}[sequenceIndex%2]:new[]{"opening","brief","comms","debrief"}[sequenceIndex%4];
            GameLocalization.SetLocale(fa?"fa-IR":"en",false);MainMenuV3PrefabBuilder.SetGameViewResolution(fa?2400:1920,1080);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FirstLaunchNarrativePresentationPrefabBuilder.PrefabPath);
            view=UnityEngine.Object.Instantiate(prefab,canvas.transform).GetComponent<NarrativeSequenceView>();
            var sequence=AssetDatabase.LoadAllAssetsAtPath(CH02M01GridlockNarrativeBuilder.Path).OfType<NarrativeSequenceConfig>().Single(s=>s.SequenceId==(stage=="opening"?"seq.ch02.open.broken_grid":"seq.ch02.m01."+stage));
            var locale=fa?AssetDatabase.LoadAssetAtPath<NarrativeLocaleConfig>(M02EstablishBaseNarrativeLocaleBuilder.PersianLocalePath):null;
            player=new FirstLaunchNarrativeSequencePresentationSystemHelper();
            var resolver=new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(
                new FirstLaunchNarrativeLocaleTextCompositionSystemHelper(FallbackGameTextResolver.Instance,locale));
            if(!player.Initialize(sequence,AssetDatabase.LoadAssetAtPath<NarrativeSpeakerCatalog>(FirstLaunchNarrativeConfigBuilder.SpeakerPath),
                AssetDatabase.LoadAssetAtPath<NarrativePunctuationConfig>(FirstLaunchNarrativeConfigBuilder.PunctuationPath),view,resolver,Game.UI.Runtime.SettingsService.Defaults,locale))
                throw new InvalidOperationException("Gridlock comic player initialization failed");
            completeSequence=false;previousClip=null;previousState=null;
            player.HandoffRequested+=_=>completeSequence=true;
            if(!(Revisions?player.StartAt("Gridlock-"+stage+"-1"):player.Start()))throw new InvalidOperationException("Gridlock comic failed to start");
            playback.Bind(player,view);
            Debug.Log("[GridlockComicPlayback] start="+sequence.SequenceId+" locale="+GameLocalization.CurrentLocaleCode);
        }
        private static double stateSince;
        private static void SampleVoice()
        {
            string id=player.CurrentStateId;
            if(previousState!=id){previousState=id;stateSince=EditorApplication.timeSinceStartup;}
            if(string.IsNullOrEmpty(id) || id.EndsWith("complete",StringComparison.Ordinal) || EditorApplication.timeSinceStartup-stateSince<1.5)return;
            string key=(sequenceIndex<(Revisions?2:4)?"fa":"en")+"-"+id;
            if(Started.Contains(key)) {if(Revisions && EditorApplication.timeSinceStartup-stateSince>3)completeSequence=true;return;}
            Started.Add(key);
            System.IO.Directory.CreateDirectory("/private/tmp/warline-gridlock/comic-review-03");
            ScreenCapture.CaptureScreenshot("/private/tmp/warline-gridlock/comic-review-03/"+key+".png");
            Debug.Log("[GridlockComicPlayback] captured="+key+" aspect="+Screen.width+"x"+Screen.height);
        }
        private static void Observe(string message,string stack,LogType type)
        {
            if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))return;
            if(type is LogType.Exception or LogType.Assert)error=message;
        }
        private static void Finish(bool pass,string detail)
        {
            if(finished)return;finished=true;playback.Unbind();player?.Cancel();SessionState.SetBool(Active,false);SessionState.SetBool(RevisionKey,false);
            EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;
            Debug.Log("[GridlockComicPlayback] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
