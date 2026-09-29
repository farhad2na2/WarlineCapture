using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    // The native NUnit callback writes its real result locally. This keeps the
    // repository wrapper fail-closed when PlayerConnection cannot deliver it.
    [InitializeOnLoad]
    internal static class MapVariantNativeResultWatcher
    {
        private const string StartKey="MapPreparationNativeRun.StartUtcTicks";
        private static readonly string ResultPath=Path.GetFullPath("Library/MapVariantNativeRun/result.xml");
        static MapVariantNativeResultWatcher()
        {
            string[] args=Environment.GetCommandLineArgs();
            if(!args.Contains("-runTests") || Value(args,"-assemblyNames")!="Game.Tests.MapPreparation.Native" ||
               Value(args,"-testPlatform")!="StandaloneOSX" ||
               !Path.GetFullPath(Value(args,"-buildPlayerPath") ?? ".").StartsWith(Path.GetFullPath("Build/MapVariantPreparedPlayer")+Path.DirectorySeparatorChar,StringComparison.Ordinal)) return;
            if(string.IsNullOrEmpty(SessionState.GetString(StartKey,""))) SessionState.SetString(StartKey,DateTime.UtcNow.Ticks.ToString());
            EditorApplication.update+=CheckResult;
        }
        private static string Value(string[] args,string key)
        {
            int i=Array.IndexOf(args,key);return i>=0 && i+1<args.Length ? args[i+1] : null;
        }
        private static void CheckResult()
        {
            if(!File.Exists(ResultPath) || EditorApplication.isCompiling || EditorApplication.isUpdating || (DateTime.UtcNow-File.GetLastWriteTimeUtc(ResultPath)).TotalSeconds<15 || File.GetLastWriteTimeUtc(ResultPath).Ticks<long.Parse(SessionState.GetString(StartKey,"0"))) return;
            EditorApplication.update-=CheckResult;
            var xml=new XmlDocument();xml.Load(ResultPath);
            XmlElement run=xml.DocumentElement;
            int total=int.Parse(run.GetAttribute("total")),passed=int.Parse(run.GetAttribute("passed"));
            bool success=run.GetAttribute("result")=="Passed" && total>0 && passed==total && run.GetAttribute("failed")=="0" && run.GetAttribute("skipped")=="0";
            string output=Value(Environment.GetCommandLineArgs(),"-testResults");
            if(!string.IsNullOrEmpty(output)) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));File.Copy(ResultPath,output,true); }
            foreach(XmlNode failure in xml.SelectNodes("//test-case[failure]")) Debug.LogError("[MapPreparationNativeFailure] "+failure.Attributes?["fullname"]?.Value+" "+failure.SelectSingleNode("failure/message")?.InnerText);
            Debug.Log("[MapPreparationNativePlayer] result="+(success?"Passed":"Failed")+" tests="+total+" passed="+passed+" source=NativeNUnitCallback");
            EditorApplication.delayCall+=()=>EditorApplication.Exit(success?0:2);
        }
    }
}
