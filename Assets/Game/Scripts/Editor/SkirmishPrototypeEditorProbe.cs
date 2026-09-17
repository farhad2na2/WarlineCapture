using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SkirmishPrototypeEditorProbe
    {
        private const string Key="Warline.SkirmishProbe";
        private static int stage;
        private static double next,deadline;
        static SkirmishPrototypeEditorProbe(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
        public static void Launch()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit play mode before launch.");
            System.Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT","/private/tmp/skirmish-prototype-profile");
            SessionState.SetBool(Key,true); stage=0;next=0;deadline=0;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            double now=EditorApplication.timeSinceStartup;
            if(deadline==0)deadline=now+150;
            if(now>deadline){Finish("Timed out at stage "+stage);return;}
            if(now<next)return;
            try
            {
                if(stage==0)
                {
                    GameLocalization.SetLocale("en",false);
                    if(!UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.QuickCustomSetup,false))return;
                    stage=1;next=now+2;return;
                }
                if(stage==1)
                {
                    var setup=UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>();if(setup==null)return;
                    Directory.CreateDirectory("/private/tmp/skirmish-prototype-evidence");
                    ScreenCapture.CaptureScreenshot("/private/tmp/skirmish-prototype-evidence/setup-en.png");
                    stage=2;next=now+1;return;
                }
                if(stage==2)
                {UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>().LaunchMatch();stage=3;next=now+5;return;}
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;
                using var query=world.EntityManager.CreateEntityQuery(typeof(SkirmishMatchState));
                if(query.CalculateEntityCount()!=1)return;
                var state=query.GetSingleton<SkirmishMatchState>();
                if(state.Phase!=SkirmishPhase.Playing)return;
                if(!UiShellRuntimeGateway.TryReadShellState(out var shell)||shell.CurrentMode!=UiShellMode.MatchHud||shell.IsTransitionRunning)return;
                if(stage==3){stage=4;next=now+2;return;}
                ScreenCapture.CaptureScreenshot("/private/tmp/skirmish-prototype-evidence/opening-en.png");
                Finish("Playing; seed="+state.Seed+" player="+state.PlayerMainBase+" enemy="+state.EnemyMainBase);
            }
            catch(Exception e){Finish(e.ToString());}
        }
        private static void Finish(string message)
        {SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log("[SkirmishPrototypeProbe] "+message);}
    }
}
