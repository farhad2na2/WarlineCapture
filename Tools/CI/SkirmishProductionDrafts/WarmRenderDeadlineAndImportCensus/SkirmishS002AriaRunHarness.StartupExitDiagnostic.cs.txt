#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        static string startupExitDiagnosticPath;
        static bool startupExitDiagnosticActive;
        static bool startupExitObservedStartupPass;

        // Diagnostic only: no ARIA, purchase, terminal outcome or readiness claim.
        // The existing harness remains the sole Play/Exit/EditorApplication.Exit owner.
        public static void RunS005NormalStartupExitDiagnostic()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||startupExitDiagnosticActive)
                throw new InvalidOperationException("Startup exit diagnostic requires an idle owned Editor.");
            string requested=Environment.GetEnvironmentVariable("WARLINE_S005_EXIT_DIAGNOSTIC_OUTPUT");
            if(string.IsNullOrWhiteSpace(requested)||!Path.IsPathRooted(requested))
                throw new InvalidOperationException("Set a fresh absolute persistent diagnostic output path.");
            startupExitDiagnosticPath=Path.GetFullPath(requested);
            string projectTemp=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp"))+Path.DirectorySeparatorChar;
            if(startupExitDiagnosticPath.StartsWith(projectTemp,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unity removes project Temp during shutdown; use persistent Logs.");
            Directory.CreateDirectory(Path.GetDirectoryName(startupExitDiagnosticPath));
            using(var stream=new FileStream(startupExitDiagnosticPath,FileMode.CreateNew,FileAccess.Write)){}
            startupExitDiagnosticActive=true;
            EditorApplication.playModeStateChanged+=ObserveStartupExitTransition;
            EditorApplication.quitting+=ObserveStartupExitQuitting;
            Application.logMessageReceived+=ObserveStartupExitLog;
            CaptureStartupExitCensus("BeforeLaunch");
            Environment.SetEnvironmentVariable("WARLINE_ARIA_QUIT","1");
            RunEditorJourney("en",false,false,"S005",155926);
            // EnterPlaymode is asynchronous; this chooses the existing startup-only
            // branch before its first Tick. RunEditorJourney clears stale diagnostics.
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY","1");
            if(EditorApplication.isPlaying||stage!=0)
                throw new InvalidOperationException("Startup-only flag was not armed before native startup; diagnostic invalid.");
            Debug.Log("[S005StartupExitDiagnostic] phase=Requested diagnosticOnly=1 normalMenu=1 nativeOutcomeClaim=0");
        }

        static void ObserveStartupExitTransition(PlayModeStateChange change)
        {
            if(!startupExitDiagnosticActive)return;
            CaptureStartupExitCensus(change.ToString());
            if(change==PlayModeStateChange.EnteredPlayMode&&Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY")!="1")
                throw new InvalidOperationException("Startup-only branch was not armed on entering Play.");
            if(change==PlayModeStateChange.EnteredEditMode&&startupExitObservedStartupPass)
                Debug.Log("[S005StartupExitDiagnostic] result=ObservedStartupAndEditMode diagnosticOnly=1 wrapperExitStillRequired=1");
        }
        static void ObserveStartupExitLog(string message,string trace,LogType type)
        {
            if(message.StartsWith("[SkirmishS003Startup] result=Passed reason=sharedStartupDiagnostic",StringComparison.Ordinal))
                startupExitObservedStartupPass=true;
        }
        static void ObserveStartupExitQuitting()
        {if(startupExitDiagnosticActive)CaptureStartupExitCensus("ApplicationQuitting");}

        static void CaptureStartupExitCensus(string phase)
        {
            var worlds=new JArray();
            foreach(var world in World.All)
            {
                if(!world.IsCreated)continue;
                var em=world.EntityManager;
                using var sessions=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
                using var actors=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
                worlds.Add(new JObject { ["name"]=world.Name,["sequence"]=world.SequenceNumber.ToString(),
                    ["nativeSessions"]=sessions.CalculateEntityCount(),["ownedActors"]=actors.CalculateEntityCount() });
            }
            var scenes=new JArray();
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                scenes.Add(new JObject { ["handle"]=scene.handle.ToString(),["name"]=scene.name,["path"]=scene.path,
                    ["loaded"]=scene.isLoaded,["dirty"]=scene.isDirty,["roots"]=scene.rootCount,
                    ["preview"]=EditorSceneManager.IsPreviewScene(scene) });
            }
            var row=new JObject { ["phase"]=phase,["utc"]=DateTime.UtcNow.ToString("O"),
                ["playing"]=EditorApplication.isPlaying,["playTransition"]=EditorApplication.isPlayingOrWillChangePlaymode,
                ["compiling"]=EditorApplication.isCompiling,["updating"]=EditorApplication.isUpdating,
                ["previewSceneCount"]=EditorSceneManager.previewSceneCount,["worlds"]=worlds,["scenes"]=scenes,
                ["enterPlayModeOptionsEnabled"]=EditorSettings.enterPlayModeOptionsEnabled,
                ["enterPlayModeOptions"]=(int)EditorSettings.enterPlayModeOptions,["readinessClaim"]=false };
            row["frame"]=Time.frameCount;row["renderedFrame"]=Time.renderedFrameCount;
            row["relevantUnityArguments"]=CaptureRelevantStartupUnityArguments(Environment.GetCommandLineArgs());
            try
            {
                var stored=EditorSettings.refreshImportMode;var active=AssetDatabase.ActiveRefreshImportMode;
                row["storedRefreshImportMode"]=stored.ToString();row["storedRefreshImportModeValue"]=(int)stored;
                row["activeRefreshImportMode"]=active.ToString();row["activeRefreshImportModeValue"]=(int)active;
                row["refreshImportModeCaptured"]=true;
            }
            catch(Exception error){row["refreshImportModeCaptured"]=false;row["refreshImportModeError"]=error.GetType().Name+": "+error.Message;}
            row["startupPassObserved"]=startupExitObservedStartupPass;
            row["startupOnlyArmed"]=Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY")=="1";
            File.AppendAllText(startupExitDiagnosticPath,row.ToString(Formatting.None)+"\n");
            Debug.Log("[S005StartupExitDiagnostic] phase="+phase+" diagnosticOnly=1 readinessClaim=0");
        }
        static JArray CaptureRelevantStartupUnityArguments(string[] args)
        {
            var result=new JArray();
            for(int i=0;i<args.Length;i++)
            {
                string flag=args[i];
                if(flag.Equals("-refreshImportMode",StringComparison.OrdinalIgnoreCase))
                {
                    result.Add("-refreshImportMode");string value=i+1<args.Length?args[++i]:"";
                    result.Add(value=="InProcess"||value=="OutOfProcess"||value=="OutOfProcessPerQueue"?value:"<unrecognized-mode>");
                }
                else if(flag.Equals("-executeMethod",StringComparison.OrdinalIgnoreCase))
                {
                    result.Add("-executeMethod");string value=i+1<args.Length?args[++i]:"";
                    result.Add(value.StartsWith("Game.Editor.SkirmishS002AriaRunHarness.",StringComparison.Ordinal)&&System.Text.RegularExpressions.Regex.IsMatch(value,"^[A-Za-z0-9_.]+$")?value:"<unrecognized-method>");
                }
                else if(flag=="-batchmode"||flag=="-nographics"||flag=="-quit"||flag=="-force-d3d11"||flag=="-force-d3d12"||flag=="-force-vulkan")result.Add(flag);
            }
            return result; // No project path, log path, environment or unknown arguments.
        }
    }
}
#endif
