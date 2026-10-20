#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Game.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace Game.Tests.Editor
{
    // Real owned temporary filesystem only. Never changes the player's persistent root.
    public static class SkirmishProfileNamespaceTests
    {
        static JObject Capture(string root,bool original)
        {
            var context=typeof(SkirmishS002AriaRunHarness).GetNestedType("ProductionCanaryContext",BindingFlags.NonPublic);
            var journal=context.GetNestedType("ProfileByteJournal",BindingFlags.NonPublic);
            var method=journal.GetMethod("SnapshotScoped",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.IsNotNull(method,"Actual scoped writer required");
            try{return (JObject)method.Invoke(null,new object[]{root,"S005",new byte[65536],original});}
            catch(TargetInvocationException failure) when(failure.InnerException!=null)
            {ExceptionDispatchInfo.Capture(failure.InnerException).Throw();throw;}
        }
        static void Refuses(string root,bool original)=>Assert.Throws<InvalidDataException>(()=>Capture(root,original));
        static string NewDirectory(string root,string name){string path=Path.Combine(root,name);Directory.CreateDirectory(path);return path;}
        static void Link(string root,string name,string target,bool directory,List<Tuple<string,bool>> links,List<IDisposable> sockets,ref bool cleanupSafe)
        {
            string link=Path.Combine(root,name);links.Add(Tuple.Create(link,directory));
            if(directory)
            {
                var method=typeof(SkirmishS002AriaHarnessTests).GetMethod("CreateOwnedProfileJunction",BindingFlags.NonPublic|BindingFlags.Static);
                Assert.IsNotNull(method,"Existing proved directory junction helper required");
                var args=new object[]{link,target,cleanupSafe};
                try{method.Invoke(null,args);}
                catch{cleanupSafe=false;throw;}
                finally{cleanupSafe&=(bool)args[2];}
            }
            else
            {
                Assert.Less(System.Text.Encoding.UTF8.GetByteCount(Path.GetFullPath(link)),108,"Actual full AF_UNIX path must fit");
                var type=typeof(SkirmishS002AriaHarnessTests).GetNestedType("OwnedProfileSocket",BindingFlags.NonPublic);
                Assert.IsNotNull(type,"Existing proved file-reparse socket helper required");
                var ctor=type.GetConstructor(new[]{typeof(string)});Assert.IsNotNull(ctor);
                sockets.Add((IDisposable)ctor.Invoke(new object[]{link}));
            }
            Assert.IsTrue((File.GetAttributes(link)&FileAttributes.ReparsePoint)!=0);
        }

        static JObject CaptureV3(string root,string catalog,bool original)
        {
            var context=typeof(SkirmishS002AriaRunHarness).GetNestedType("ProductionCanaryContext",BindingFlags.NonPublic);
            var journal=context.GetNestedType("ProfileByteJournal",BindingFlags.NonPublic);
            var method=journal.GetMethod("SnapshotScopedV3",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.IsNotNull(method);
            try{return (JObject)method.Invoke(null,new object[]{root,catalog,new byte[65536],original});}
            catch(TargetInvocationException failure) when(failure.InnerException!=null)
            {ExceptionDispatchInfo.Capture(failure.InnerException).Throw();throw;}
        }
        public static void RunCrossCatalogOriginalProfileValidation()
        {
            Assert.AreEqual(RuntimePlatform.WindowsEditor,Application.platform);
            string temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string owned=Path.GetFullPath(Path.Combine(temp,"wpc-"+Guid.NewGuid().ToString("N")));
            Assert.IsTrue(owned.StartsWith(temp,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(owned).StartsWith("wpc-",StringComparison.Ordinal));
            Directory.CreateDirectory(owned);
            var links=new List<Tuple<string,bool>>();var sockets=new List<IDisposable>();bool cleanupSafe=true;int controls=0;
            try
            {
                string original=NewDirectory(owned,"original");
                string foreign=Path.Combine(original,"skirmish-s005-native.json");
                File.WriteAllText(foreign,"ABCD");File.WriteAllText(Path.Combine(original,"profile.json"),"shared");
                var before=CaptureV3(original,"S002",true);
                Assert.AreEqual("OriginalCanonicalCatalogSaveFiles_KnownEngineSiblingsExcluded",(string)before["namespace_scope"]);
                Assert.AreEqual(20,((JArray)before["files"]).Count);controls++;
                var rows=((JArray)before["files"]).OfType<JObject>().ToArray();
                Assert.AreEqual(1,rows.Count(v=>(string)v["name"]=="skirmish-s005-native.json"&&(bool)v["exists"]));
                foreach(string suffix in new[]{".tmp",".bak",".lock"})
                    Assert.IsFalse((bool)rows.Single(v=>(string)v["name"]=="skirmish-s005-native.json"+suffix)["exists"]);
                controls++;
                for(int i=0;i<3;i++)Assert.IsTrue(JToken.DeepEquals(before,CaptureV3(original,"S002",true)));controls++;
                File.WriteAllText(foreign,"WXYZ");
                Assert.IsFalse(JToken.DeepEquals(before["files"],CaptureV3(original,"S002",true)["files"]));controls++;
                File.WriteAllText(foreign+".tmp","temporary");
                Assert.IsFalse(JToken.DeepEquals(before["directory_entries"],CaptureV3(original,"S002",true)["directory_entries"]));controls++;
                File.Delete(foreign);File.Delete(foreign+".tmp");
                Assert.AreEqual(16,((JArray)CaptureV3(original,"S002",true)["files"]).Count);controls++;
                foreach(string name in new[]{"skirmish-s000-native.json","skirmish-s121-native.json","skirmish-S005-native.json","skirmish-s005-native.json.extra","unknown.json"})
                {
                    string path=Path.Combine(original,name);File.WriteAllText(path,"refuse");
                    try{Assert.Throws<InvalidDataException>(()=>CaptureV3(original,"S002",true));}
                    finally{File.Delete(path);}controls++;
                }
                File.WriteAllText(foreign,"owned must refuse");
                Assert.Throws<InvalidDataException>(()=>CaptureV3(original,"S002",false));controls++;
                File.Delete(foreign);Directory.CreateDirectory(foreign);
                Assert.Throws<InvalidDataException>(()=>CaptureV3(original,"S002",true));Directory.Delete(foreign);controls++;
                string limit=NewDirectory(owned,"limit");
                for(int i=6;i<39;i++)File.WriteAllText(Path.Combine(limit,"skirmish-s"+i.ToString("D3")+"-native.json"),"");
                Assert.Throws<InvalidDataException>(()=>CaptureV3(limit,"S002",true));controls++;
                string large=NewDirectory(owned,"large");string largeFile=Path.Combine(large,"skirmish-s005-native.json");
                using(var stream=File.Create(largeFile))stream.SetLength(8388609);
                Assert.Throws<InvalidDataException>(()=>CaptureV3(large,"S002",true));controls++;
                string total=NewDirectory(owned,"total");
                foreach(string id in new[]{"s005","s006","s007"})
                    using(var stream=File.Create(Path.Combine(total,"skirmish-"+id+"-native.json")))stream.SetLength(6291456);
                Assert.Throws<InvalidDataException>(()=>CaptureV3(total,"S002",true));controls++;
                string absent=Path.Combine(owned,"absent");var absentSnapshot=CaptureV3(absent,"S002",true);
                Directory.CreateDirectory(absent);var emptySnapshot=CaptureV3(absent,"S002",true);
                Assert.IsFalse((bool)absentSnapshot["directory_exists"]);Assert.IsTrue((bool)emptySnapshot["directory_exists"]);controls++;
                string target=NewDirectory(owned,"target");string sentinel=Path.Combine(target,"sentinel.txt");File.WriteAllText(sentinel,"preserved");
                string reparse=NewDirectory(owned,"rp");
                Link(reparse,"skirmish-s005-native.json",target,true,links,sockets,ref cleanupSafe);
                Assert.Throws<InvalidDataException>(()=>CaptureV3(reparse,"S002",true));
                Assert.AreEqual("preserved",File.ReadAllText(sentinel));controls++;
                Assert.AreEqual(18,controls);
            }
            finally
            {
                foreach(var socket in sockets)socket.Dispose();
                if(cleanupSafe)
                {
                    foreach(var link in links)
                    {
                        string full=Path.GetFullPath(link.Item1);
                        Assert.IsTrue(full.StartsWith(owned+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));
                        FileAttributes attributes;
                        try{attributes=File.GetAttributes(full);}catch(FileNotFoundException){continue;}catch(DirectoryNotFoundException){continue;}
                        Assert.IsTrue((attributes&FileAttributes.ReparsePoint)!=0);
                        if(link.Item2)Directory.Delete(full,false);else File.Delete(full);
                    }
                    string fullOwned=Path.GetFullPath(owned);
                    Assert.IsTrue(fullOwned.StartsWith(temp,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(fullOwned).StartsWith("wpc-",StringComparison.Ordinal));
                    Assert.IsTrue((File.GetAttributes(fullOwned)&FileAttributes.ReparsePoint)==0);
                    Directory.Delete(fullOwned,true);
                }
                else Debug.LogWarning("Preserved owned profile fixture after uncertain junction helper: "+owned);
            }
            Debug.Log("[CrossCatalogOriginalProfileControls] result=Passed controls=18 actualFilesystem=1 nativeQueries=0 missionReady=0");
        }

        public static void RunOriginalProfileNamespaceValidation()
        {
            Assert.AreEqual(RuntimePlatform.WindowsEditor,Application.platform,"Actual Windows filesystem fixture required");
            string temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string owned=Path.GetFullPath(Path.Combine(temp,"wpn-"+Guid.NewGuid().ToString("N")));
            Assert.IsTrue(owned.StartsWith(temp,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(owned).StartsWith("wpn-",StringComparison.Ordinal));
            Directory.CreateDirectory(owned);var links=new List<Tuple<string,bool>>();var sockets=new List<IDisposable>();bool cleanupSafe=true;int controls=0;
            try
            {
                string original=NewDirectory(owned,"original");string game=Path.Combine(original,"profile.json");
                byte[] bytes=System.Text.Encoding.UTF8.GetBytes("{\"fixture\":\"original-save-bytes\"}");File.WriteAllBytes(game,bytes);
                string result=Path.Combine(original,"TestResults.xml"),cache=NewDirectory(original,"Unity");
                File.WriteAllText(result,"<tests>before</tests>");File.WriteAllText(Path.Combine(cache,"engine.cache"),"before");
                JObject first=null;
                for(int stage=0;stage<3;stage++)
                {
                    File.WriteAllText(result,"<tests>"+stage+"</tests>");File.WriteAllText(Path.Combine(cache,"engine.cache"),"stage"+stage);
                    var snapshot=Capture(original,true);
                    Assert.AreEqual("OriginalCurrentCatalogSaveFiles_KnownEngineSiblingsExcluded",(string)snapshot["namespace_scope"]);
                    Assert.IsTrue(JToken.DeepEquals(snapshot["directory_entries"],new JArray("profile.json")));
                    Assert.IsTrue(JToken.DeepEquals(snapshot["total_directory_entries"],new JArray("TestResults.xml","Unity","profile.json")));
                    Assert.IsTrue(JToken.DeepEquals(snapshot["excluded_non_save_entries"],new JArray(
                        new JObject{["name"]="TestResults.xml",["kind"]="File"},new JObject{["name"]="Unity",["kind"]="Directory"})));
                    if(first==null)first=snapshot;else Assert.IsTrue(JToken.DeepEquals(first["files"],snapshot["files"]),"Actual save bytes/existence must remain identical across3 stages");
                    CollectionAssert.AreEqual(bytes,File.ReadAllBytes(game));
                }
                controls++;
                string test=NewDirectory(owned,"owned-file");File.WriteAllText(Path.Combine(test,"TestResults.xml"),"not-owned-save");Refuses(test,false);controls++;
                test=NewDirectory(owned,"owned-dir");Directory.CreateDirectory(Path.Combine(test,"Unity"));Refuses(test,false);controls++;
                test=NewDirectory(owned,"wrong-results-type");Directory.CreateDirectory(Path.Combine(test,"TestResults.xml"));Refuses(test,true);controls++;
                test=NewDirectory(owned,"wrong-unity-type");File.WriteAllText(Path.Combine(test,"Unity"),"wrong");Refuses(test,true);controls++;
                test=NewDirectory(owned,"unknown-file");File.WriteAllText(Path.Combine(test,"unknown.json"),"unknown");Refuses(test,true);controls++;
                test=NewDirectory(owned,"unknown-dir");Directory.CreateDirectory(Path.Combine(test,"Unknown"));Refuses(test,true);controls++;
                test=NewDirectory(owned,"other-catalog");File.WriteAllText(Path.Combine(test,"skirmish-s006-native.json"),"unsupported-namespace");Refuses(test,true);controls++;
                test=NewDirectory(owned,"save-directory");Directory.CreateDirectory(Path.Combine(test,"profile.json"));Refuses(test,true);controls++;
                string targetFile=Path.Combine(owned,"link-target.txt");File.WriteAllText(targetFile,"owned-target");string targetDir=NewDirectory(owned,"link-target-dir");
                string sentinel=Path.Combine(targetDir,"sentinel.txt");File.WriteAllText(sentinel,"preserved");
                test=NewDirectory(owned,"rr");Link(test,"TestResults.xml",targetFile,false,links,sockets,ref cleanupSafe);Refuses(test,true);controls++;
                test=NewDirectory(owned,"ru");Link(test,"Unity",targetDir,true,links,sockets,ref cleanupSafe);Refuses(test,true);controls++;
                test=NewDirectory(owned,"rp");Link(test,"root",targetDir,true,links,sockets,ref cleanupSafe);Refuses(Path.Combine(test,"root"),true);controls++;
                test=NewDirectory(owned,"rs");Link(test,"profile.json",targetFile,false,links,sockets,ref cleanupSafe);Refuses(test,true);controls++;
                Assert.AreEqual("owned-target",File.ReadAllText(targetFile));Assert.AreEqual("preserved",File.ReadAllText(sentinel));
                Assert.AreEqual(13,controls);
            }
            finally
            {
                foreach(var socket in sockets)socket.Dispose();
                if(cleanupSafe)
                {
                foreach(var link in links)
                {
                    string full=Path.GetFullPath(link.Item1);
                    Assert.IsTrue(full.StartsWith(owned+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Exact owned temporary link only");
                    FileAttributes attributes;
                    try{attributes=File.GetAttributes(full);}catch(FileNotFoundException){continue;}catch(DirectoryNotFoundException){continue;}
                    Assert.IsTrue((attributes&FileAttributes.ReparsePoint)!=0,"Refuse cleanup of replaced non-link");
                    if(link.Item2)Directory.Delete(full,false);else File.Delete(full);
                }
                string fullOwned=Path.GetFullPath(owned);
                Assert.IsTrue(fullOwned.StartsWith(temp,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(fullOwned).StartsWith("wpn-",StringComparison.Ordinal));
                Assert.IsTrue((File.GetAttributes(fullOwned)&FileAttributes.ReparsePoint)==0,"Refuse recursive cleanup of replaced root");
                Directory.Delete(fullOwned,true);
                }
                else Debug.LogWarning("Preserved owned fixture after junction helper timeout: "+owned);
            }
            Debug.Log("[OriginalProfileSaveNamespaceControls] result=Passed controls=13 actualFilesystem=1 originalStages=3 ownedExclusions=0 reparseRefusals=4 nativeQueries=0 missionReady=0");
        }
    }
}
#endif
