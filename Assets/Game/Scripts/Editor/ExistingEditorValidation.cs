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
    public static class ExistingEditorValidation
    {
        public static async void Run(string executeMethod,string logFile)
        {
            int status=1;
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            using var writer=new StreamWriter(logFile,false) {AutoFlush=true};
            object gate=new object();
            Application.LogCallback capture=(message,stack,type)=>
            {lock(gate){writer.WriteLine(message);if(!string.IsNullOrEmpty(stack))writer.WriteLine(stack);}};
            Application.logMessageReceivedThreaded+=capture;
            try
            {
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
                object invocation=method.Invoke(null,null);
                if(invocation is Task<int> operation)status=await operation;
                else
                {
                    object result=exit?.GetProperty("LastExitCode",BindingFlags.Public|BindingFlags.Static)?.GetValue(null);
                    status=result is int code?code:0;
                }
                Debug.Log("[ExistingEditorValidation] result="+(status==0?"Passed":"Failed")+" executeMethod="+executeMethod);
            }
            catch(Exception exception)
            {
                Debug.LogException(exception is TargetInvocationException invocation && invocation.InnerException!=null?invocation.InnerException:exception);
                Debug.LogError("[ExistingEditorValidation] result=Failed executeMethod="+executeMethod);
            }
            finally
            {
                Application.logMessageReceivedThreaded-=capture;
                lock(gate)writer.Flush();
                File.WriteAllText(logFile+".exit",status.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
}
#endif
