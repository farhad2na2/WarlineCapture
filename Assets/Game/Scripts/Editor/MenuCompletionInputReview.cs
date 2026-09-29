using System;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    // The fixture prepares an isolated completed save. All journey actions use
    // physical Game View input; this observer never activates a UI control.
    public static class MenuCompletionInputReview
    {
        private static string previousRoot, previousLocale, output;
        private static double started;
        private static SaveService saves;
        private static int credits, settlements, firstScene = -1;
        private static bool archiveSeen, playbackSeen, playbackReturned, rotated;
        private static readonly HashSet<UIRoute> routes = new();
        private static UIRoute lastRoute;
        private static bool lastArchive, lastPlaying, active;
        private static InputSettings.EditorInputBehaviorInPlayMode editorInput;
        private static InputSettings.BackgroundBehavior backgroundInput;

        public static void Run()
        {
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Input review requires an idle Editor and a clean active scene.");
            output="Design/AgentReports/MenuHeaderMonetization/After/input-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(output);
            previousRoot=Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT");
            previousLocale=GameLocalization.CurrentLocaleCode;
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",Path.Combine(Path.GetTempPath(),"warline-home-input-"+Guid.NewGuid().ToString("N")));
            saves=SaveService.CreateDefault();
            var profile=saves.LoadProfile(); profile.firstLaunchStatus=FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched=true; profile.firstLaunchLanguage="English";
            profile.firstLaunchCommanderDisplayName="Commander"; profile.firstLaunchCommanderPortraitIndex=0;
            saves.SaveProfile(profile);
            var store=new CampaignMissionProgressStore(saves);
            for(int i=0;i<CampaignMissionSequence.RegisteredMissionCount;i++)
            {
                store.EnsureAvailable(CampaignMissionSequence.IdAt(i));
                store.Settle(CampaignMissionSequence.IdAt(i),"input-fixture-"+i,i,true,3,60000,null);
            }
            profile=saves.LoadProfile(); credits=profile.credits;settlements=profile.missionsCompleted;
            routes.Clear();archiveSeen=playbackSeen=playbackReturned=rotated=lastArchive=lastPlaying=false;firstScene=-1;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            var input=InputSystem.settings;editorInput=input.editorInputBehaviorInPlayMode;backgroundInput=input.backgroundBehavior;
            input.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            input.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            AssetDatabase.DisallowAutoRefresh();started=EditorApplication.timeSinceStartup;
            active=true;EditorApplication.quitting+=Cleanup;
            EditorApplication.update+=Observe;EditorApplication.EnterPlaymode();
            Debug.Log("[MenuCompletionInput] awaiting physical Choose Mission, review/back, Story Archive/play/close, Operations/back, Skirmish/back");
        }
        private static void Observe()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup-started>540)throw new TimeoutException("Physical input review timed out.");
                if(!UiShellRuntimeGateway.TryReadShellState(out var shell))return;
                if(shell.ActiveRoute==UIRoute.Match)throw new InvalidOperationException("Unexpected mission deployment.");
                if(UiShellRuntimeGateway.ReadAriaPlay().Active)throw new InvalidOperationException("Unexpected ARIA activation.");
                var profile=saves.LoadProfile();
                if(profile.credits!=credits || profile.missionsCompleted!=settlements)throw new InvalidOperationException("Unexpected settlement during story/navigation review.");
                routes.Add(shell.ActiveRoute);
                if(lastRoute!=shell.ActiveRoute){Debug.Log("[MenuCompletionInput] route="+shell.ActiveRoute);Shot("route-"+shell.ActiveRoute);lastRoute=shell.ActiveRoute;}
                var archive=UnityEngine.Object.FindAnyObjectByType<MainMenuStoryArchiveView>();
                bool open=archive!=null && archive.IsOpen, playing=archive!=null && archive.IsPlaying;
                archiveSeen|=open;playbackSeen|=playing;
                if(lastPlaying && !playing && open)playbackReturned=true;
                if(open!=lastArchive || playing!=lastPlaying){Debug.Log("[MenuCompletionInput] archive="+open+" playing="+playing);Shot(playing?"story-playing":open?"story-chooser":"story-closed");lastArchive=open;lastPlaying=playing;}
                if(shell.ActiveRoute==UIRoute.MainMenu)
                {
                    var home=UnityEngine.Object.FindAnyObjectByType<MainMenuCampaignCardView>();
                    if(home!=null && home.CompletionVisible)
                    {
                        if(firstScene<0){firstScene=home.AftermathIndex;Shot("home-initial");}
                        else if(routes.Contains(UIRoute.Campaign) && home.AftermathIndex!=firstScene)rotated=true;
                    }
                    if(archiveSeen && playbackSeen && playbackReturned && !open && rotated && routes.Contains(UIRoute.Campaign) && routes.Contains(UIRoute.MissionBriefing) && routes.Contains(UIRoute.Operations) && routes.Contains(UIRoute.QuickCustomSetup))
                        Complete(true,"physicalInput=True isolatedSave=True chooseReviewReturn=True archivePlaybackReturn=True operationsReturn=True skirmishReturn=True noDeployment=True noAria=True noSettlement=True returnRotates=True");
                }
            }
            catch(Exception error){Debug.LogException(error);Complete(false,error.Message);}
        }
        private static void Shot(string name)=>ScreenCapture.CaptureScreenshot(output+"/"+name+".png");
        private static void Complete(bool passed,string detail)
        {
            Cleanup();
            Debug.Log("[MenuCompletionInput] result="+(passed?"Passed":"Failed")+" "+detail);
            MissionEditorValidationExit.Complete(passed);
        }
        private static void Cleanup()
        {
            if(!active)return;active=false;
            EditorApplication.quitting-=Cleanup;
            EditorApplication.update-=Observe;
            InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;InputSystem.settings.backgroundBehavior=backgroundInput;
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",previousRoot);
            GameLocalization.SetLocale(previousLocale,false);
        }
    }
}
