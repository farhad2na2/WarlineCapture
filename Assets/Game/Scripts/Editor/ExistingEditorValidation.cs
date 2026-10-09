#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    // Wrapper-owned live validation: never opens, quits or changes projects.
    [InitializeOnLoad]
    public static class ExistingEditorValidation
    {
        public static bool IsRunning { get; private set; }
        private const string AbortOwnedPlay = "Warline.ExistingValidation.AbortOwnedPlay";
        private sealed class Capture
        {
            public StreamWriter Writer;
            public object Gate = new object();
            public string LogFile, Method;
            public bool OwnsPlay, Aborted;
            public Task<int> Operation;
            public Application.LogCallback Callback;
        }
        private static Capture active;
        static ExistingEditorValidation()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            if (SessionState.GetBool(AbortOwnedPlay, false))
                EditorApplication.delayCall += () =>
                {
                    SessionState.SetBool(AbortOwnedPlay, false);
                    MissionEditorValidationExit.Complete(false);
                };
        }
        private static void BeforeReload()
        {
            Capture run = active;
            if (run == null || !IsRunning) return;
            run.Aborted = true;
            Application.logMessageReceivedThreaded -= run.Callback;
            lock (run.Gate)
            {
                run.Writer.WriteLine("[ExistingEditorValidation] result=Failed reason=domain-reload executeMethod="+run.Method);
                run.Writer.Flush();
                run.Writer.Dispose();
            }
            File.WriteAllText(run.LogFile+".exit", "1");
            if (run.OwnsPlay)
            {
                SessionState.SetBool(AbortOwnedPlay, true);
                SessionState.SetBool("Warline.AirCorridor.Input", false);
                SessionState.SetBool("Warline.SteelPush.Input", false);
                SessionState.SetBool("Warline.M03.LaunchProbe.Active", false);
            }
            MissionEditorValidationExit.LastCompletion = 1;
            EditorApplication.update -= PollCompletion;
            active = null;
            IsRunning = false;
        }
        public static void Run(string executeMethod,string logFile)
        {
            if (IsRunning)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFile));
                File.WriteAllText(logFile, "[ExistingEditorValidation] result=Failed reason=validation-already-running executeMethod="+executeMethod+Environment.NewLine);
                File.WriteAllText(logFile+".exit", "1");
                return;
            }
            IsRunning = true;
            MissionEditorValidationExit.LastCompletion = null;
            int status=1;
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            var writer=new StreamWriter(logFile,false) {AutoFlush=true};
            var run = new Capture { Writer=writer, LogFile=logFile, Method=executeMethod };
            active = run;
            object gate=run.Gate;
            Application.LogCallback capture=(message,stack,type)=>
            {lock(gate){if(run.Aborted)return;writer.WriteLine(message);if(!string.IsNullOrEmpty(stack))writer.WriteLine(stack);}};
            run.Callback = capture;
            Application.logMessageReceivedThreaded+=capture;
            try
            {
                if(EditorUtility.scriptCompilationFailed)throw new InvalidOperationException("Editor script compilation failed; refusing stale assemblies.");
                if(EditorApplication.isCompiling || EditorApplication.isUpdating)throw new InvalidOperationException("Editor is still importing or compiling.");
                int separator=executeMethod.LastIndexOf('.');
                if(separator<=0)throw new ArgumentException("A fully qualified static executeMethod is required.");
                string typeName=executeMethod.Substring(0,separator),methodName=executeMethod.Substring(separator+1);
                var assemblies=AppDomain.CurrentDomain.GetAssemblies();
                var type=assemblies.Select(a=>a.GetType(typeName,false)).FirstOrDefault(t=>t!=null) ?? throw new MissingMemberException(typeName);
                var method=type.GetMethod(methodName,BindingFlags.Public|BindingFlags.Static,null,Type.EmptyTypes,null) ?? throw new MissingMethodException(executeMethod);
                var exit=assemblies.Select(a=>a.GetType("ValidationExit",false)).FirstOrDefault(t=>t!=null);
                exit?.GetMethod("ClearLastExitCode",BindingFlags.Public|BindingFlags.Static)?.Invoke(null,null);
                Debug.Log("[ExistingEditorValidation] project="+Application.dataPath+" executeMethod="+executeMethod);
                bool wasPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
                object invocation=method.Invoke(null,null);
                run.OwnsPlay = !wasPlaying && EditorApplication.isPlayingOrWillChangePlaymode;
                if(invocation is Task<int> operation)
                {
                    run.Operation = operation;
                    EditorApplication.update += PollCompletion;
                    return;
                }
                if (run.OwnsPlay)
                {
                    // Entering and leaving Play mode replaces Unity's synchronization
                    // context. An awaited Task.Delay can strand its continuation on
                    // the old context even though the native probe has completed.
                    EditorApplication.update += PollCompletion;
                    return;
                }
                else
                {
                    object result=exit?.GetProperty("LastExitCode",BindingFlags.Public|BindingFlags.Static)?.GetValue(null);
                    status=MissionEditorValidationExit.LastCompletion ?? (result is int code?code:0);
                }
                if (run.Aborted) status = 1;
                Debug.Log("[ExistingEditorValidation] result="+(status==0?"Passed":"Failed")+" executeMethod="+executeMethod);
            }
            catch(Exception exception)
            {
                Debug.LogException(exception is TargetInvocationException invocation && invocation.InnerException!=null?invocation.InnerException:exception);
                Debug.LogError("[ExistingEditorValidation] result=Failed executeMethod="+executeMethod);
            }
            CompleteCapture(run, status);
        }

        private static void PollCompletion()
        {
            var run = active;
            if (run == null || run.Aborted)
            {
                EditorApplication.update -= PollCompletion;
                return;
            }
            if (run.Operation != null)
            {
                if (!run.Operation.IsCompleted) return;
                int status = 1;
                try { status = run.Operation.GetAwaiter().GetResult(); }
                catch (Exception exception) { Debug.LogException(exception); }
                Debug.Log("[ExistingEditorValidation] result="+(status==0?"Passed":"Failed")+" executeMethod="+run.Method);
                CompleteCapture(run, status);
                return;
            }
            if (!MissionEditorValidationExit.LastCompletion.HasValue ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            int completion = MissionEditorValidationExit.LastCompletion.Value;
            Debug.Log("[ExistingEditorValidation] result="+(completion==0?"Passed":"Failed")+" executeMethod="+run.Method);
            CompleteCapture(run, completion);
        }

        private static void CompleteCapture(Capture run, int status)
        {
            EditorApplication.update -= PollCompletion;
            Application.logMessageReceivedThreaded -= run.Callback;
            lock (run.Gate)
            {
                if (!run.Aborted)
                {
                    run.Writer.Flush();
                    run.Writer.Dispose();
                    run.Aborted = true;
                    File.WriteAllText(run.LogFile+".exit",status.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            if (ReferenceEquals(active, run)) active = null;
            IsRunning = false;
        }
    }
}
#endif
