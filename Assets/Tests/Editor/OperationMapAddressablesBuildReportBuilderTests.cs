using System;
using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.Layout;

public sealed class OperationMapAddressablesBuildReportBuilderTests
{
    public static void RunPreparedSettingsCurrentHashInitialization()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle installed Editor required");
        string receiptPath=PreparedSettingsObservedPath("WARLINE_PREPARED_SETTINGS_RECEIPT",false),output=PreparedSettingsObservedPath("WARLINE_PREPARED_SETTINGS_HASH_OBSERVATION_OUTPUT",true);
        string receiptHash=Environment.GetEnvironmentVariable("WARLINE_PREPARED_SETTINGS_RECEIPT_SHA256");
        if(receiptHash==null||!System.Text.RegularExpressions.Regex.IsMatch(receiptHash,"\\A[a-f0-9]{64}\\z")||BuildScript.PreparedFileHash(receiptPath)!=receiptHash)throw new InvalidOperationException("Exact original prepared receipt bytes required");
        var receipt=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(receiptPath));
        if(receipt.Value<int>("schema")!=1||(string)receipt["scope"]!="PreparedAndroidAddressables_NoPlayerOrAcceptance"||(string)receipt["target"]!="Android"||(string)receipt["editorVersion"]!=UnityEngine.Application.unityVersion||receipt.Value<bool>("buildPlayerInvoked")||receipt.Value<bool>("packageAccepted"))throw new InvalidOperationException("Actual original prepared receipt identity required");
        var expected=receipt["settingsSerialized"]as Newtonsoft.Json.Linq.JObject;if(expected==null)throw new InvalidOperationException("Actual expected settings object required");
        var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;if(settings==null)throw new InvalidOperationException("Actual persisted default settings required");
        string path=AssetDatabase.GetAssetPath(settings);if(string.IsNullOrEmpty(path)||(string)receipt["settingsPath"]!=path)throw new InvalidOperationException("Actual persisted settings path differs");
        var callback=global::AddressablesPlayerBuildProcessor.BuildAddressablesOverride;
        Newtonsoft.Json.Linq.JObject Capture()=>new Newtonsoft.Json.Linq.JObject{
            ["assetPath"]=path,["assetGuid"]=AssetDatabase.AssetPathToGUID(path),["assetSha256"]=BuildScript.PreparedFileHash(path),["metaSha256"]=BuildScript.PreparedFileHash(path+".meta"),
            ["profile"]=settings.activeProfileId,["builder"]=settings.ActivePlayerDataBuilderIndex,["publicPlayerBuildOption"]=(int)settings.BuildAddressablesWithPlayerBuild,
            ["entityId"]=settings.GetEntityId().ToString(),["dirty"]=EditorUtility.IsDirty(settings),["serialized"]=Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(settings))};
        Newtonsoft.Json.Linq.JObject Identity(Newtonsoft.Json.Linq.JObject point){var result=(Newtonsoft.Json.Linq.JObject)point.DeepClone();result.Remove("serialized");return result;}
        var before=Capture();
        // Public installed lazy getter derives its value from actual settings.
        // No field assignment, reflection, FromJson, SetDirty or asset save.
        string firstHash=settings.currentHash.ToString();var after=Capture();
        string secondHash=settings.currentHash.ToString();var repeat=Capture();
        var changes=PreparedSettingsDiff(before["serialized"],after["serialized"]);bool onlyCacheChanged=true;
        foreach(Newtonsoft.Json.Linq.JObject row in changes)if((string)row["path"]!="/MonoBehaviour/m_currentHash/Hash")onlyCacheChanged=false;
        bool rawStable=Newtonsoft.Json.Linq.JToken.DeepEquals(Identity(before),Identity(after))&&Newtonsoft.Json.Linq.JToken.DeepEquals(Identity(before),Identity(repeat));
        bool repeatStable=firstHash==secondHash&&Newtonsoft.Json.Linq.JToken.DeepEquals(after,repeat);
        bool matches=Newtonsoft.Json.Linq.JToken.DeepEquals(expected,after["serialized"]);
        bool owned=ReferenceEquals(settings,UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings)&&ReferenceEquals(callback,global::AddressablesPlayerBuildProcessor.BuildAddressablesOverride)&&BuildScript.PreparedFileHash(receiptPath)==receiptHash;
        string expectedHash=(string)expected["MonoBehaviour"]?["m_currentHash"]?["Hash"],observedHash=(string)after["serialized"]?["MonoBehaviour"]?["m_currentHash"]?["Hash"];
        bool getterMatchesSerialized=firstHash==observedHash&&System.Text.RegularExpressions.Regex.IsMatch(firstHash,"\\A[a-f0-9]{32}\\z")&&firstHash!=new string('0',32);
        using var process=System.Diagnostics.Process.GetCurrentProcess();
        var report=new Newtonsoft.Json.Linq.JObject{["schema"]=1,["scope"]="PersistedAddressablesCurrentHashInitialization_NoBuildOrAcceptance",["editorVersion"]=UnityEngine.Application.unityVersion,["processId"]=process.Id,
            ["receiptPath"]=receiptPath,["receiptSha256"]=receiptHash,["expectedSettings"]=expected.DeepClone(),["expectedHash"]=expectedHash,
            ["before"]=before,["after"]=after,["repeat"]=repeat,["firstGetterHash"]=firstHash,["secondGetterHash"]=secondHash,
            ["beforeWasZeroCache"]=(string)before["serialized"]?["MonoBehaviour"]?["m_currentHash"]?["Hash"]==new string('0',32),
            ["differencesBeforeToAfter"]=changes,["differencesExpectedToAfter"]=PreparedSettingsDiff(expected,after["serialized"]),
            ["onlySerializedCacheChanged"]=onlyCacheChanged,["rawIdentityDirtyStable"]=rawStable,["repeatStable"]=repeatStable,["expectedSettingsMatch"]=matches,["getterMatchesSerialized"]=getterMatchesSerialized,["ownedSettingsAndCallbackStable"]=owned,
            ["publicGetterInvoked"]=true,["settingsNormalized"]=false,["assetWritesInvoked"]=false,["contentBuildInvoked"]=false,["playerBuildInvoked"]=false,["missionAccepted"]=false};
        using(var stream=new System.IO.FileStream(output,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read))using(var writer=new System.IO.StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented));
        if(!onlyCacheChanged||!rawStable||!repeatStable||!matches||!getterMatchesSerialized||!owned)throw new InvalidOperationException("Actual getter initialization did not preserve exact prepared settings; diagnostic retained");
        UnityEngine.Debug.Log("[PreparedSettingsCurrentHashInitialization] result=Collected fullSettingsEqual=1 repeatStable=1 rawBytesUnchanged=1 contentBuild=0 playerBuild=0 acceptance=0");
    }

    public static void RunPreparedSettingsHashAndStrictValidation()
    {
        // First prove the actual persisted cross-Editor cache transition. A
        // failed proof must stop before helper/strict-control success markers.
        RunPreparedSettingsCurrentHashInitialization();
        string output=PreparedSettingsObservedPath("WARLINE_PREPARED_SETTINGS_HASH_OBSERVATION_OUTPUT",false);
        var observed=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(output));
        var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        string path=AssetDatabase.GetAssetPath(settings),asset=BuildScript.PreparedFileHash(path),meta=BuildScript.PreparedFileHash(path+".meta");bool dirty=EditorUtility.IsDirty(settings);
        var actual=BuildScript.CapturePreparedSettingsJson(settings);var repeat=BuildScript.CapturePreparedSettingsJson(settings);
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(observed["expectedSettings"],actual));
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(observed["after"]["serialized"],actual));
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(actual,repeat));
        Assert.AreEqual(asset,BuildScript.PreparedFileHash(path));Assert.AreEqual(meta,BuildScript.PreparedFileHash(path+".meta"));Assert.AreEqual(dirty,EditorUtility.IsDirty(settings));
        UnityEngine.Debug.Log("[PreparedSettingsHashCaptureHelperControls] result=Passed controls=1 fullSettingsEqual=1 rawBytesUnchanged=1");
        RunPreparedContentFocusedValidation();
        UnityEngine.Debug.Log("[PreparedSettingsHashAndStrictControls] result=Passed initialization=1 helper=1 strict=20 actualContentBuild=0 actualPlayerBuild=0 acceptance=0");
    }

    private static Newtonsoft.Json.Linq.JArray PreparedSettingsDiff(Newtonsoft.Json.Linq.JToken expected,Newtonsoft.Json.Linq.JToken actual)
    {
        var rows=new Newtonsoft.Json.Linq.JArray();int nodes=0;
        string Escape(string value)=>value.Replace("~","~0").Replace("/","~1");
        void Walk(Newtonsoft.Json.Linq.JToken a,bool ap,Newtonsoft.Json.Linq.JToken b,bool bp,string path,int depth){
            if(++nodes>100000||depth>128||rows.Count>=20000)throw new InvalidOperationException("Bounded exact settings differences required");
            if(ap&&bp&&Newtonsoft.Json.Linq.JToken.DeepEquals(a,b))return;
            if(ap&&bp&&a.Type==Newtonsoft.Json.Linq.JTokenType.Object&&b.Type==Newtonsoft.Json.Linq.JTokenType.Object){
                var keys=new SortedSet<string>(StringComparer.Ordinal);foreach(var p in ((Newtonsoft.Json.Linq.JObject)a).Properties())keys.Add(p.Name);foreach(var p in ((Newtonsoft.Json.Linq.JObject)b).Properties())keys.Add(p.Name);
                foreach(string key in keys){var pa=((Newtonsoft.Json.Linq.JObject)a).Property(key);var pb=((Newtonsoft.Json.Linq.JObject)b).Property(key);Walk(pa?.Value,pa!=null,pb?.Value,pb!=null,path+"/"+Escape(key),depth+1);}return;
            }
            if(ap&&bp&&a.Type==Newtonsoft.Json.Linq.JTokenType.Array&&b.Type==Newtonsoft.Json.Linq.JTokenType.Array){
                var aa=(Newtonsoft.Json.Linq.JArray)a;var bb=(Newtonsoft.Json.Linq.JArray)b;
                for(int i=0;i<Math.Max(aa.Count,bb.Count);i++)Walk(i<aa.Count?aa[i]:null,i<aa.Count,i<bb.Count?bb[i]:null,i<bb.Count,path+"/"+i.ToString(System.Globalization.CultureInfo.InvariantCulture),depth+1);return;
            }
            rows.Add(new Newtonsoft.Json.Linq.JObject{["path"]=path,["expectedPresent"]=ap,["actualPresent"]=bp,["expectedType"]=ap?a.Type.ToString():"Missing",["actualType"]=bp?b.Type.ToString():"Missing",["expected"]=ap?a.DeepClone():null,["actual"]=bp?b.DeepClone():null});
        }
        Walk(expected,expected!=null,actual,actual!=null,"",0);return rows;
    }
    [Test] public void PreparedSettingsDiffExactHasNoRows()
    {var value=Newtonsoft.Json.Linq.JObject.Parse("{\"MonoBehaviour\":{\"value\":null,\"items\":[1,2]}}");Assert.AreEqual(0,PreparedSettingsDiff(value,value.DeepClone()).Count);}
    [Test] public void PreparedSettingsDiffDistinguishesMissingAndNull()
    {var rows=PreparedSettingsDiff(Newtonsoft.Json.Linq.JObject.Parse("{}"),Newtonsoft.Json.Linq.JObject.Parse("{\"value\":null}"));Assert.AreEqual(1,rows.Count);Assert.AreEqual("/value",(string)rows[0]["path"]);Assert.IsFalse((bool)rows[0]["expectedPresent"]);Assert.AreEqual("Null",(string)rows[0]["actualType"]);}
    [Test] public void PreparedSettingsDiffKeepsArrayOrderAndTypes()
    {var rows=PreparedSettingsDiff(Newtonsoft.Json.Linq.JArray.Parse("[1,2]"),Newtonsoft.Json.Linq.JArray.Parse("[2,1,null]"));Assert.AreEqual(3,rows.Count);Assert.AreEqual("/0",(string)rows[0]["path"]);Assert.AreEqual("/2",(string)rows[2]["path"]);Assert.AreEqual(1,PreparedSettingsDiff(new Newtonsoft.Json.Linq.JValue(1),new Newtonsoft.Json.Linq.JValue("1")).Count);}
    [Test] public void PreparedSettingsDiffEscapesExactPropertyNames()
    {var rows=PreparedSettingsDiff(Newtonsoft.Json.Linq.JObject.Parse("{\"a/b~c\":1}"),Newtonsoft.Json.Linq.JObject.Parse("{\"a/b~c\":2}"));Assert.AreEqual("/a~1b~0c",(string)rows[0]["path"]);Assert.AreEqual(1,(int)rows[0]["expected"]);Assert.AreEqual(2,(int)rows[0]["actual"]);}
    private static string PreparedSettingsObservedPath(string variable,bool fresh)
    {
        string value=Environment.GetEnvironmentVariable(variable),root=System.IO.Path.GetFullPath("Logs/SkirmishProduction/Offline")+System.IO.Path.DirectorySeparatorChar;
        if(string.IsNullOrWhiteSpace(value)||!System.IO.Path.IsPathRooted(value))throw new InvalidOperationException("Explicit absolute observed settings path required");
        value=System.IO.Path.GetFullPath(value);
        if(!value.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(value))||System.IO.Directory.Exists(value)||(!fresh&&!System.IO.File.Exists(value))||(fresh&&System.IO.File.Exists(value)))throw new InvalidOperationException("Exact owned settings input/fresh output required");
        for(string parent=fresh?System.IO.Path.GetDirectoryName(value):value;!string.IsNullOrEmpty(parent);parent=System.IO.Path.GetDirectoryName(parent))BuildScript.RequirePreparedNonReparse(System.IO.File.GetAttributes(parent));
        return value;
    }
    public static void RunPreparedSettingsCrossEditorCapture()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle installed Editor required");
        string receiptPath=PreparedSettingsObservedPath("WARLINE_PREPARED_SETTINGS_RECEIPT",false),output=PreparedSettingsObservedPath("WARLINE_PREPARED_SETTINGS_OBSERVATION_OUTPUT",true);
        string receiptHash=Environment.GetEnvironmentVariable("WARLINE_PREPARED_SETTINGS_RECEIPT_SHA256");
        if(receiptHash==null||!System.Text.RegularExpressions.Regex.IsMatch(receiptHash,"\\A[a-f0-9]{64}\\z")||BuildScript.PreparedFileHash(receiptPath)!=receiptHash)throw new InvalidOperationException("Exact original prepared receipt bytes required");
        var receipt=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(receiptPath));
        if(receipt.Value<int>("schema")!=1||(string)receipt["scope"]!="PreparedAndroidAddressables_NoPlayerOrAcceptance"||(string)receipt["target"]!="Android"||(string)receipt["editorVersion"]!=UnityEngine.Application.unityVersion||receipt.Value<bool>("buildPlayerInvoked")||receipt.Value<bool>("packageAccepted"))throw new InvalidOperationException("Actual original prepared receipt identity required");
        var expected=receipt["settingsSerialized"]as Newtonsoft.Json.Linq.JObject;if(expected==null)throw new InvalidOperationException("Actual expected settings object required");
        var t=new OperationMapAddressablesBuildReportBuilderTests();t.PreparedSettingsDiffExactHasNoRows();t.PreparedSettingsDiffDistinguishesMissingAndNull();t.PreparedSettingsDiffKeepsArrayOrderAndTypes();t.PreparedSettingsDiffEscapesExactPropertyNames();
        var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;if(settings==null)throw new InvalidOperationException("Actual persisted default settings required");
        string path=AssetDatabase.GetAssetPath(settings);if(string.IsNullOrEmpty(path)||(string)receipt["settingsPath"]!=path)throw new InvalidOperationException("Actual persisted settings path differs");
        var callback=global::AddressablesPlayerBuildProcessor.BuildAddressablesOverride;
        Newtonsoft.Json.Linq.JObject Capture(){
            return new Newtonsoft.Json.Linq.JObject{["assetPath"]=path,["assetGuid"]=AssetDatabase.AssetPathToGUID(path),["assetSha256"]=BuildScript.PreparedFileHash(path),["metaSha256"]=BuildScript.PreparedFileHash(path+".meta"),["profile"]=settings.activeProfileId,["builder"]=settings.ActivePlayerDataBuilderIndex,["publicPlayerBuildOption"]=(int)settings.BuildAddressablesWithPlayerBuild,["entityId"]=settings.GetEntityId().ToString(),["dirty"]=EditorUtility.IsDirty(settings),["serialized"]=Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(settings))};
        }
        var before=Capture();var after=Capture();
        bool stable=Newtonsoft.Json.Linq.JToken.DeepEquals(before,after)&&ReferenceEquals(settings,UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings)&&ReferenceEquals(callback,global::AddressablesPlayerBuildProcessor.BuildAddressablesOverride)&&BuildScript.PreparedFileHash(receiptPath)==receiptHash;
        var differences=PreparedSettingsDiff(expected,before["serialized"]);var afterDifferences=PreparedSettingsDiff(expected,after["serialized"]);
        using var process=System.Diagnostics.Process.GetCurrentProcess();
        var report=new Newtonsoft.Json.Linq.JObject{["schema"]=1,["scope"]="PersistedAddressablesSettingsCrossEditorCapture_NoBuildOrAcceptance",["editorVersion"]=UnityEngine.Application.unityVersion,["processId"]=process.Id,["receiptPath"]=receiptPath,["receiptSha256"]=receiptHash,["expectedProcessId"]=receipt["processId"],["expectedProfile"]=receipt["profile"],["expectedBuilder"]=receipt["builder"],["expectedSettings"]=expected.DeepClone(),["actualBefore"]=before,["actualAfter"]=after,["differencesBefore"]=differences,["differencesAfter"]=afterDifferences,["expectedMatchesActual"]=Newtonsoft.Json.Linq.JToken.DeepEquals(expected,before["serialized"]),["captureStable"]=stable,["callbackUnchanged"]=ReferenceEquals(callback,global::AddressablesPlayerBuildProcessor.BuildAddressablesOverride),["contentBuildInvoked"]=false,["playerBuildInvoked"]=false,["settingsNormalized"]=false,["missionAccepted"]=false};
        using(var stream=new System.IO.FileStream(output,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read))using(var writer=new System.IO.StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented));
        if(!stable)throw new InvalidOperationException("Settings capture changed; exact diagnostic retained without equivalence claim");
        UnityEngine.Debug.Log("[PreparedSettingsCrossEditorCapture] result=Collected helperControls=4 stable=1 differences="+differences.Count+" contentBuild=0 playerBuild=0 acceptance=0");
    }

    private static string PreparedManagedOracle(string path)
    {using var stream=System.IO.File.OpenRead(path);using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    [Test] public void PreparedCngKnownVectors()
    {
        using var f=new PreparedFixture();using var digest=new SkirmishCandidateSourceHash.FileDigestSession();
        string empty=f.Write("empty.bin",""),abc=f.Write("abc.bin","abc");
        Assert.AreEqual("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",digest.ComputeFile(empty));
        Assert.AreEqual("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",digest.ComputeFile(abc));
        Assert.AreEqual(PreparedManagedOracle(empty),digest.ComputeFile(empty));Assert.AreEqual(PreparedManagedOracle(abc),digest.ComputeFile(abc));
    }
    [Test] public void PreparedCngChunkBoundaries()
    {
        using var f=new PreparedFixture();using var digest=new SkirmishCandidateSourceHash.FileDigestSession();
        foreach(int length in new[]{1,63,64,65,65535,65536,65537,131071}){
            var data=new byte[length];for(int i=0;i<data.Length;i++)data[i]=(byte)((i*37+11)&255);
            string path=System.IO.Path.Combine(f.Root,"binary.bin");System.IO.File.WriteAllBytes(path,data);
            string actual=digest.ComputeFile(path);Assert.AreEqual(PreparedManagedOracle(path),actual);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(actual,"\\A[a-f0-9]{64}\\z"));
        }
    }
    [Test] public void PreparedCngConsecutiveFileResetAndNoCache()
    {
        using var f=new PreparedFixture();using var digest=new SkirmishCandidateSourceHash.FileDigestSession();
        string path=f.Write("changing.bin","A");
        foreach(string value in new[]{"A","","B","A",""}){System.IO.File.WriteAllBytes(path,System.Text.Encoding.UTF8.GetBytes(value));Assert.AreEqual(PreparedManagedOracle(path),digest.ComputeFile(path));}
    }
    [Test] public void PreparedCngCompleteTempTreeMatchesManaged()
    {
        using var f=new PreparedFixture();f.Write("image.png","binary");f.Write("Nested/\u03a9-font.ttf","font");f.Write("zero.bytes","");
        using var digest=new SkirmishCandidateSourceHash.FileDigestSession();
        var expected=BuildScript.PreparedTree(f.Root);var actual=BuildScript.PreparedTree(f.Root,fileHash:digest.ComputeFile);
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(expected,actual));
        CollectionAssert.AreEqual(System.Text.Encoding.UTF8.GetBytes(expected.ToString(Newtonsoft.Json.Formatting.None)),System.Text.Encoding.UTF8.GetBytes(actual.ToString(Newtonsoft.Json.Formatting.None)));
    }
    [Test] public void PreparedCngIoAndDisposedRefusals()
    {
        using var f=new PreparedFixture();using var digest=new SkirmishCandidateSourceHash.FileDigestSession();
        Assert.Throws<System.IO.FileNotFoundException>(()=>digest.ComputeFile(System.IO.Path.Combine(f.Root,"missing.bin")));
        string path=f.Write("locked.bin","locked");
        using(var held=new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.ReadWrite,System.IO.FileShare.None))
            Assert.Throws<System.IO.IOException>(()=>digest.ComputeFile(path));
        digest.Dispose();Assert.Throws<ObjectDisposedException>(()=>digest.ComputeFile(path));
    }
    [Test] public void PreparedCngMutationAndExtraRefused()
    {
        using var f=new PreparedFixture();f.Write("input.bin","original");using var digest=new SkirmishCandidateSourceHash.FileDigestSession();
        var expected=BuildScript.PreparedTree(f.Root,fileHash:digest.ComputeFile);f.Write("input.bin","mutated");
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root,fileHash:digest.ComputeFile),"CNG mutation"));
        f.Write("input.bin","original");f.Write("extra.bin","extra");
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root,fileHash:digest.ComputeFile),"CNG extra"));
        PreparedCaseAndReparseRefusals();
    }
    public static void RunPreparedCngManifestValidation()
    {
#if !UNITY_EDITOR_WIN
        throw new InvalidOperationException("Installed Windows Editor CNG validation required");
#else
        string output=Environment.GetEnvironmentVariable("WARLINE_PREPARED_CNG_MANIFEST_OUTPUT");
        string root=System.IO.Path.GetFullPath("Logs/SkirmishProduction/Offline")+System.IO.Path.DirectorySeparatorChar;
        if(string.IsNullOrWhiteSpace(output)||!System.IO.Path.IsPathRooted(output))throw new InvalidOperationException("Fresh absolute CNG manifest output required");
        output=System.IO.Path.GetFullPath(output);
        if(!output.StartsWith(root,StringComparison.OrdinalIgnoreCase)||System.IO.File.Exists(output)||!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(output)))throw new InvalidOperationException("Fresh owned CNG manifest output required");
        for(string parent=System.IO.Path.GetDirectoryName(output);!string.IsNullOrEmpty(parent);parent=System.IO.Path.GetDirectoryName(parent))BuildScript.RequirePreparedNonReparse(System.IO.File.GetAttributes(parent));
        var controls=new OperationMapAddressablesBuildReportBuilderTests();controls.PreparedCngKnownVectors();controls.PreparedCngChunkBoundaries();controls.PreparedCngConsecutiveFileResetAndNoCache();
        controls.PreparedCngCompleteTempTreeMatchesManaged();controls.PreparedCngIoAndDisposedRefusals();controls.PreparedCngMutationAndExtraRefused();
        UnityEngine.Debug.Log("[PreparedCngFileControls] result=Passed controls=6 actualContentBuild=0 actualPlayerBuild=0");
        var timer=System.Diagnostics.Stopwatch.StartNew();Newtonsoft.Json.Linq.JObject manifest;
        using(var digest=new SkirmishCandidateSourceHash.FileDigestSession())manifest=BuildScript.PreparedSourceManifestWithHasher(digest.ComputeFile);
        timer.Stop();long files=0,bytes=0;foreach(var property in manifest.Properties())foreach(Newtonsoft.Json.Linq.JObject row in (Newtonsoft.Json.Linq.JArray)property.Value){checked{files++;bytes+=(long)row["bytes"];}}
        var report=new Newtonsoft.Json.Linq.JObject{["schema"]=1,["scope"]="PreparedSourceManifestWindowsCng_IndependentEqualityPending",["algorithm"]="SHA256",["backend"]="ExistingFileDigestSession",["files"]=files,["bytes"]=bytes,["elapsedSeconds"]=timer.Elapsed.TotalSeconds,["sourceManifest"]=manifest,["independentEqualityPassed"]=false,["actualContentBuild"]=false,["actualPlayerBuild"]=false,["missionAccepted"]=false};
        using(var stream=new System.IO.FileStream(output,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.Read))using(var writer=new System.IO.StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented));
        UnityEngine.Debug.Log("[PreparedCngManifestControls] result=Passed controls=6 fullManifestExported=1 independentEqualityPending=1 actualContentBuild=0 actualPlayerBuild=0");
#endif
    }

    private sealed class PreparedFixture : IDisposable
    {
        internal readonly string Root;
        internal PreparedFixture(){Root=System.IO.Path.GetFullPath("Logs/SkirmishProduction/Offline/PreparedContentControlTemp/"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(Root);}
        internal string Write(string name,string value){string p=System.IO.Path.Combine(Root,name);System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p));System.IO.File.WriteAllText(p,value);return p;}
        public void Dispose(){string parent=System.IO.Path.GetFullPath("Logs/SkirmishProduction/Offline/PreparedContentControlTemp")+System.IO.Path.DirectorySeparatorChar;
            if(!Root.StartsWith(parent,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Owned fixture path required");
            BuildScript.RequirePreparedNonReparse(System.IO.File.GetAttributes(Root));System.IO.Directory.Delete(Root,true);}
    }
    private static Newtonsoft.Json.Linq.JObject PreparedSources(Newtonsoft.Json.Linq.JArray assets)
        =>new Newtonsoft.Json.Linq.JObject{["Assets"]=assets,["Packages"]=new Newtonsoft.Json.Linq.JArray(),["ProjectSettings"]=new Newtonsoft.Json.Linq.JArray()};
    private static Newtonsoft.Json.Linq.JObject PreparedLink(bool present=true)
        =>new Newtonsoft.Json.Linq.JObject{["present"]=present,["assetPath"]="Assets/Config/link.xml",["metaPath"]="Assets/Config/link.xml.meta",
            ["outputPath"]="unused-by-pure-lifecycle-control",["xmlSha256"]=new string('a',64),["metaSha256"]=new string('b',64),["metaGuid"]=new string('c',32)};
    private static Newtonsoft.Json.Linq.JObject PreparedLinkSources()
        =>PreparedSources(new Newtonsoft.Json.Linq.JArray(
            new Newtonsoft.Json.Linq.JObject{["path"]="Config/link.xml",["bytes"]=3,["sha256"]=new string('a',64)},
            new Newtonsoft.Json.Linq.JObject{["path"]="Config/link.xml.meta",["bytes"]=4,["sha256"]=new string('b',64)},
            new Newtonsoft.Json.Linq.JObject{["path"]="image.png",["bytes"]=2,["sha256"]=new string('d',64)}));
    [Test] public void PreparedTreeIncludesBinaryInputs()
    {using var f=new PreparedFixture();f.Write("image.png","binary-a");f.Write("model.fbx","binary-b");f.Write("font.ttf","binary-c");Assert.AreEqual(3,BuildScript.PreparedTree(f.Root).Count);}
    [Test] public void PreparedActualOutputMutationAndExtraFileRefused()
    {using var f=new PreparedFixture();f.Write("catalog.bin","original");var expected=BuildScript.PreparedTree(f.Root);f.Write("catalog.bin","changed");
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root),"output"));
        f.Write("catalog.bin","original");f.Write("extra.bundle","extra");Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root),"output"));}
    [Test] public void PreparedMissingOutputRefused()
    {using var f=new PreparedFixture();string p=f.Write("catalog.bin","original");f.Write("settings.json","settings");var expected=BuildScript.PreparedTree(f.Root);System.IO.File.Delete(p);
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(expected,BuildScript.PreparedTree(f.Root),"output"));}
    [Test] public void PreparedCaseAndReparseRefusals()
    {using var f=new PreparedFixture();Assert.Throws<InvalidOperationException>(()=>BuildScript.RequireUniquePreparedPaths(new[]{System.IO.Path.Combine(f.Root,"A"),System.IO.Path.Combine(f.Root,"a")}));
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedNonReparse(System.IO.FileAttributes.Directory|System.IO.FileAttributes.ReparsePoint));BuildScript.RequirePreparedNonReparse(System.IO.FileAttributes.Normal);}
    [Test] public void PreparedExactLinkPairUnchanged()
    {var a=PreparedLinkSources();var result=BuildScript.ValidatePreparedSourceLifecycle(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),PreparedLink());Assert.IsTrue((bool)result["wholeSourceManifestUnchanged"]);}
    [Test] public void PreparedExactLinkPairRemovedIsDistinctObservedManifest()
    {var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();((Newtonsoft.Json.Linq.JArray)b["Assets"]).RemoveAt(0);((Newtonsoft.Json.Linq.JArray)b["Assets"]).RemoveAt(0);
        var result=BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink());Assert.IsFalse((bool)result["wholeSourceManifestUnchanged"]);Assert.AreEqual(2,((Newtonsoft.Json.Linq.JArray)result["removedPaths"]).Count);
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(b,result["sourceManifestObserved"]));Assert.AreEqual(3,((Newtonsoft.Json.Linq.JArray)a["Assets"]).Count);}
    [Test] public void PreparedPartialPairRefused()
    {foreach(int index in new[]{0,1}){var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();((Newtonsoft.Json.Linq.JArray)b["Assets"]).RemoveAt(index);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink()));}}
    [Test] public void PreparedMutatedPairRefused()
    {foreach(int index in new[]{0,1}){var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();b["Assets"][index]["sha256"]=new string('e',64);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink()));}}
    [Test] public void PreparedOtherSourceDriftRefused()
    {var a=PreparedLinkSources();var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();b["Assets"][2]["sha256"]=new string('e',64);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,b,PreparedLink()));}
    [Test] public void PreparedAbsentOriginalGrantsNoWaiver()
    {var a=PreparedSources(new Newtonsoft.Json.Linq.JArray());Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,PreparedLinkSources(),PreparedLink(false)));}
    [Test] public void PreparedPinnedLinkHashAndGuidRequired()
    {var a=PreparedLinkSources();var link=PreparedLink();link["xmlSha256"]=new string('e',64);Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),link));
        link=PreparedLink();link["metaGuid"]="bad";Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSourceLifecycle(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),link));}
    [Test] public void PreparedSerializedSettingsOnlyScopedEnumMayDiffer()
    {var persisted=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        Assert.IsNotNull(persisted);
        string persistedBefore=EditorJsonUtility.ToJson(persisted);
        var persistedJson=Newtonsoft.Json.Linq.JObject.Parse(persistedBefore);
        BuildScript.ValidatePreparedSettings(persistedJson,Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(persisted)),false);
        Assert.AreEqual(persistedBefore,EditorJsonUtility.ToJson(persisted));
        WithPreparedSettings(settings=>{
        settings.BuildAddressablesWithPlayerBuild=UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
        var expected=Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(settings));
        var original=expected.DeepClone();
        Assert.IsInstanceOf<Newtonsoft.Json.Linq.JObject>(expected["MonoBehaviour"]);
        Assert.AreEqual(Newtonsoft.Json.Linq.JTokenType.Integer,expected["MonoBehaviour"]["m_BuildAddressablesWithPlayerBuild"].Type);
        BuildScript.ValidatePreparedSettings(expected,Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(settings)),false);
        BuildScript.WithPreparedNoRebuild(settings,()=>{
            var actual=Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(settings));
            Assert.AreEqual((int)UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer,(int)actual["MonoBehaviour"]["m_BuildAddressablesWithPlayerBuild"]);
            BuildScript.ValidatePreparedSettings(expected,actual,true);
            Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSettings(expected,actual,false));
            var foreign=(Newtonsoft.Json.Linq.JObject)actual.DeepClone();foreign["MonoBehaviour"]["m_Name"]="foreign";
            Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSettings(expected,foreign,true));
            foreign=(Newtonsoft.Json.Linq.JObject)actual.DeepClone();foreign["foreignRootField"]=1;
            Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSettings(expected,foreign,true));
        });
        Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(original,expected));
        BuildScript.ValidatePreparedSettings(expected,Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(settings)),false);
        Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSettings(new Newtonsoft.Json.Linq.JObject{["m_BuildAddressablesWithPlayerBuild"]=1},expected,false));
        var malformed=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();malformed["MonoBehaviour"]["m_BuildAddressablesWithPlayerBuild"]="1";
        Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedSettings(malformed,expected,false));
    });}
    public static void RunPreparedSettingsAndCngFocusedValidation()
    {
        RunPreparedSettingsSerializationControls();
        RunPreparedCngManifestValidation();
        RunPreparedContentFocusedValidation();
        UnityEngine.Debug.Log("[PreparedSettingsAndCngControls] result=Passed settingsCapture=1 cngFileControls=6 legacyPreparedControls=20 independentManifestEqualityPending=1 contentBuild=0 playerBuild=0 acceptance=0");
    }
    public static void RunPreparedSettingsSerializationControls()
    {
        new OperationMapAddressablesBuildReportBuilderTests().PreparedSerializedSettingsOnlyScopedEnumMayDiffer();
        UnityEngine.Debug.Log("[PreparedSettingsSerializationControls] result=Passed actualEditorJsonUtilityCapture=1 persistedReadOnlyCapture=1 scopedEnumOnly=1 contentBuild=0 playerBuild=0 acceptance=0");
    }
    private static void WithPreparedSettings(Action<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings> action)
    {Assert.IsNull(AddressablesPlayerBuildProcessor.BuildAddressablesOverride);var settings=UnityEngine.ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings>();
        try{action(settings);}finally{UnityEngine.Object.DestroyImmediate(settings);}}
    [Test] public void PreparedSentinelRefusesRebuildAndRestores()
    {WithPreparedSettings(settings=>{var prior=settings.BuildAddressablesWithPlayerBuild;BuildScript.WithPreparedNoRebuild(settings,()=>{
        Assert.AreEqual(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer,settings.BuildAddressablesWithPlayerBuild);
        Assert.Throws<InvalidOperationException>(()=>AddressablesPlayerBuildProcessor.BuildAddressablesOverride(settings));});Assert.AreEqual(prior,settings.BuildAddressablesWithPlayerBuild);Assert.IsNull(AddressablesPlayerBuildProcessor.BuildAddressablesOverride);});}
    [Test] public void PreparedBuildExceptionStillRestoresScope()
    {WithPreparedSettings(settings=>{var prior=settings.BuildAddressablesWithPlayerBuild;Assert.Throws<InvalidOperationException>(()=>BuildScript.WithPreparedNoRebuild(settings,()=>throw new InvalidOperationException("injected build failure")));
        Assert.AreEqual(prior,settings.BuildAddressablesWithPlayerBuild);Assert.IsNull(AddressablesPlayerBuildProcessor.BuildAddressablesOverride);});}
    [Test] public void PreparedForeignCallbackNeverReplaced()
    {WithPreparedSettings(settings=>{Func<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings,UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult> foreign=_=>null;
        AddressablesPlayerBuildProcessor.BuildAddressablesOverride=foreign;try{Assert.Throws<InvalidOperationException>(()=>BuildScript.WithPreparedNoRebuild(settings,()=>Assert.Fail("foreign callback must refuse before build")));Assert.AreSame(foreign,AddressablesPlayerBuildProcessor.BuildAddressablesOverride);}
        finally{if(AddressablesPlayerBuildProcessor.BuildAddressablesOverride==foreign)AddressablesPlayerBuildProcessor.BuildAddressablesOverride=null;}});}
    [Test] public void PreparedForeignReplacementPreservedAndScopeFails()
    {WithPreparedSettings(settings=>{var prior=settings.BuildAddressablesWithPlayerBuild;Func<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings,UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult> foreign=_=>null;
        try{Assert.Throws<InvalidOperationException>(()=>BuildScript.WithPreparedNoRebuild(settings,()=>AddressablesPlayerBuildProcessor.BuildAddressablesOverride=foreign));Assert.AreSame(foreign,AddressablesPlayerBuildProcessor.BuildAddressablesOverride);Assert.AreEqual(prior,settings.BuildAddressablesWithPlayerBuild);}
        finally{if(AddressablesPlayerBuildProcessor.BuildAddressablesOverride==foreign)AddressablesPlayerBuildProcessor.BuildAddressablesOverride=null;}});}
    [Test] public void PreparedReportHashMismatchRefused()
    {using var f=new PreparedFixture();string p=f.Write("report.json","original");var hash=new Newtonsoft.Json.Linq.JValue(BuildScript.PreparedFileHash(p));f.Write("report.json","other");Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedEqual(hash,new Newtonsoft.Json.Linq.JValue(BuildScript.PreparedFileHash(p)),"report"));}
    [Test] public void PreparedFreshReportGuardBeforeBuild()
    {using var f=new PreparedFixture();string key=OperationMapAddressablesBuildReportBuilder.OutputPathEnvironmentVariable,prior=Environment.GetEnvironmentVariable(key);string p=System.IO.Path.Combine(f.Root,"fresh.json");
        try{Environment.SetEnvironmentVariable(key,p);BuildScript.RequirePreparedFreshReport(p);f.Write("fresh.json","exists");Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedFreshReport(p));
            Environment.SetEnvironmentVariable(key,null);Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedFreshReport(System.IO.Path.Combine(f.Root,"other.json")));}
        finally{Environment.SetEnvironmentVariable(key,prior);}}
    private static Newtonsoft.Json.Linq.JObject PreparedRealMeta(PreparedFixture f,string file,string guid,string extra="userData: ")
    {
        string path=f.Write(file,"fileFormatVersion: 2\nguid: "+guid+"\nDefaultImporter:\n  "+extra+"\n");var link=PreparedLink();
        link["metaGuid"]=guid;link["metaSha256"]=BuildScript.PreparedFileHash(path);link["metaBase64"]=Convert.ToBase64String(System.IO.File.ReadAllBytes(path));return link;
    }
    [Test] public void PreparedPostPlayerOnlyGuidMetaChangeAllowed()
    {
        using var f=new PreparedFixture();var first=PreparedRealMeta(f,"a.meta",new string('a',32));var second=PreparedRealMeta(f,"b.meta",new string('b',32));
        var a=PreparedLinkSources();a["Assets"][1]["sha256"]=first["metaSha256"].DeepClone();a["Assets"][1]["bytes"]=Convert.FromBase64String((string)first["metaBase64"]).Length;
        var b=(Newtonsoft.Json.Linq.JObject)a.DeepClone();b["Assets"][1]["sha256"]=second["metaSha256"].DeepClone();
        var pre=(Newtonsoft.Json.Linq.JObject)a.DeepClone();((Newtonsoft.Json.Linq.JArray)pre["Assets"]).RemoveAt(0);((Newtonsoft.Json.Linq.JArray)pre["Assets"]).RemoveAt(0);
        var preProof=BuildScript.ValidatePreparedSourceLifecycle(a,pre,first);
        var result=BuildScript.ValidatePreparedPostPlayerSource(a,b,first,second,preProof);
        Assert.IsTrue((bool)result["metaGuidChanged"]);Assert.IsFalse((bool)result["matchesPreparedASourceManifest"]);Assert.IsFalse((bool)result["sourceManifestUnchangedFromBPrebuild"]);
        Assert.AreEqual(2,((Newtonsoft.Json.Linq.JArray)result["createdPaths"]).Count);Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(b,result["sourceManifestObserved"]));
    }
    [Test] public void PreparedPostPlayerRejectsNonGuidMetaAndForeignXml()
    {
        using var f=new PreparedFixture();var first=PreparedRealMeta(f,"a.meta",new string('a',32));var foreign=PreparedRealMeta(f,"b.meta",new string('b',32),"userData: foreign");
        Assert.Throws<InvalidOperationException>(()=>BuildScript.RequirePreparedMetaTemplate(first,foreign));
        var second=PreparedRealMeta(f,"c.meta",new string('b',32));second["xmlSha256"]=new string('e',64);
        var a=PreparedLinkSources();Assert.Throws<InvalidOperationException>(()=>BuildScript.ValidatePreparedPostPlayerSource(a,(Newtonsoft.Json.Linq.JObject)a.DeepClone(),first,second,new Newtonsoft.Json.Linq.JObject()));
    }
    public static void RunPreparedContentFocusedValidation()
    {
        var t=new OperationMapAddressablesBuildReportBuilderTests();t.PreparedTreeIncludesBinaryInputs();t.PreparedActualOutputMutationAndExtraFileRefused();t.PreparedMissingOutputRefused();t.PreparedCaseAndReparseRefusals();
        t.PreparedExactLinkPairUnchanged();t.PreparedExactLinkPairRemovedIsDistinctObservedManifest();t.PreparedPartialPairRefused();t.PreparedMutatedPairRefused();t.PreparedOtherSourceDriftRefused();t.PreparedAbsentOriginalGrantsNoWaiver();t.PreparedPinnedLinkHashAndGuidRequired();
        t.PreparedSerializedSettingsOnlyScopedEnumMayDiffer();t.PreparedSentinelRefusesRebuildAndRestores();t.PreparedBuildExceptionStillRestoresScope();t.PreparedForeignCallbackNeverReplaced();t.PreparedForeignReplacementPreservedAndScopeFails();t.PreparedReportHashMismatchRefused();t.PreparedFreshReportGuardBeforeBuild();
        t.PreparedPostPlayerOnlyGuidMetaChangeAllowed();t.PreparedPostPlayerRejectsNonGuidMetaAndForeignXml();
        UnityEngine.Debug.Log("[PreparedContentSplitControls] result=Passed controls=20 actualContentBuild=0 actualPlayerBuild=0 acceptance=0");
    }

    [Test]
    public void RuntimeSettings_AcceptBuiltInCatalogWithUpdatesDisabled()
    {
        const string json =
            "{\"m_DisableCatalogUpdateOnStart\":true," +
            "\"m_CatalogLocations\":[{\"m_InternalId\":" +
            "\"{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin\"}]}";

        Assert.That(
            OperationMapAddressablesBuildReportBuilder.TryValidateRuntimeSettings(
                json,
                out string error),
            Is.True,
            error);
    }

    [TestCase(false, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin")]
    [TestCase(true, "https://content.example/catalog.bin")]
    public void RuntimeSettings_RejectCatalogUpdateOrRemoteLocation(
        bool disableUpdates,
        string internalId)
    {
        string json =
            $"{{\"m_DisableCatalogUpdateOnStart\":{disableUpdates.ToString().ToLowerInvariant()}," +
            $"\"m_CatalogLocations\":[{{\"m_InternalId\":\"{internalId}\"}}]}}";

        Assert.That(
            OperationMapAddressablesBuildReportBuilder.TryValidateRuntimeSettings(
                json,
                out string error),
            Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void Create_AttributesBundleClosurePartitionsEntitiesAndDuplicates()
    {
        BuildLayout layout = CreateLayout(reverseAssets: false);

        OperationMapAddressablesBuildReport report =
            OperationMapAddressablesBuildReportBuilder.Create(layout);

        Assert.AreEqual(1, report.Maps.Length);
        Assert.AreEqual(3, report.Maps[0].BundleCount);
        Assert.AreEqual(600ul, report.Maps[0].BundleBytes);
        Assert.AreEqual(600ul, report.AggregateBundleBytes);
        Assert.AreEqual(1, report.Partitions.Length);
        Assert.AreEqual(2, report.Partitions[0].EntryCount);
        Assert.AreEqual(2, report.Partitions[0].BundleCount);
        Assert.AreEqual(2, report.RequiredAddresses.Length);
        Assert.AreEqual(1, report.EntitiesArtifacts.Length);
        Assert.AreEqual(44ul, report.EntitiesArtifacts[0].Bytes);
        Assert.AreEqual(1, report.DuplicateDependencies.Length);
        Assert.AreEqual(2, report.DuplicateDependencies[0].BundleCount);
        Assert.AreEqual(12ul, report.DuplicateDependencies[0].DuplicateBytes);
    }

    [Test]
    public void Serialize_IsByteIdenticalWhenInputOrderChanges()
    {
        string first = OperationMapAddressablesBuildReportBuilder.Serialize(
            OperationMapAddressablesBuildReportBuilder.Create(CreateLayout(reverseAssets: false)));
        string second = OperationMapAddressablesBuildReportBuilder.Serialize(
            OperationMapAddressablesBuildReportBuilder.Create(CreateLayout(reverseAssets: true)));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void Create_RejectsDuplicateStableAddresses()
    {
        BuildLayout layout = CreateLayout(reverseAssets: false);
        layout.Groups[0].Bundles[1].Files[0].Assets[0].AddressableName =
            layout.Groups[0].Bundles[0].Files[0].Assets[0].AddressableName;

        Assert.Throws<InvalidOperationException>(
            () => OperationMapAddressablesBuildReportBuilder.Create(layout));
    }

    [Test]
    public void DuplicateValidation_RejectsProjectOwnedDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new("project-guid", "Assets/Shared.asset", 2, 12)
        };

        Assert.IsFalse(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error));
        StringAssert.Contains("project-guid", error);
        StringAssert.Contains("Assets/Shared.asset", error);
    }

    [Test]
    public void DuplicateValidation_RejectsRuntimePackageDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new("package-guid", "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader", 2, 12)
        };

        Assert.IsFalse(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error),
            error);
        StringAssert.Contains("Lit.shader", error);
    }

    [Test]
    public void DuplicateValidation_AllowsEditorOnlyPackageDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new(
                "package-guid",
                "Packages/com.unity.shadergraph/Editor/Resources/Shaders/FallbackError.shader",
                2,
                12),
            new(
                "mixed-case-package-guid",
                "Packages/com.example.rendering/eDiToR/Shaders/FallbackError.shader",
                2,
                12)
        };

        Assert.IsTrue(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error),
            error);
    }

    [Test]
    public void Publish_DoesNotRewriteIdenticalContent()
    {
        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "operation-map-addressables-build-report-test.json");
        try
        {
            Assert.IsTrue(OperationMapAddressablesBuildReportBuilder.Publish(path, "report\n"));
            DateTime firstWrite = System.IO.File.GetLastWriteTimeUtc(path);
            Assert.IsFalse(OperationMapAddressablesBuildReportBuilder.Publish(path, "report\n"));
            Assert.AreEqual(firstWrite, System.IO.File.GetLastWriteTimeUtc(path));
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    private static BuildLayout CreateLayout(bool reverseAssets)
    {
        BuildLayout layout = new()
        {
            BuildResultHash = "0123456789abcdef0123456789abcdef",
            BuildTarget = BuildTarget.Android
        };
        BuildLayout.Group group = new() { Name = "Operation Map" };
        layout.Groups.Add(group);

        BuildLayout.Bundle dependency = CreateBundle("shared", 300);
        BuildLayout.Bundle first = CreateBundle("first", 100);
        BuildLayout.Bundle second = CreateBundle("second", 200);
        first.Dependencies = new List<BuildLayout.Bundle> { dependency };
        second.Dependencies = new List<BuildLayout.Bundle> { dependency };
        group.Bundles.Add(first);
        group.Bundles.Add(second);
        group.Bundles.Add(dependency);

        const string partition = "operation-map-partition-skirmish-desert-base-01-region-p000-p000";
        BuildLayout.ExplicitAsset firstAsset = CreateAsset(
            first,
            "operation-map/opmap.skirmish.desert_base_01/a",
            partition);
        BuildLayout.ExplicitAsset secondAsset = CreateAsset(
            second,
            "operation-map/opmap.skirmish.desert_base_01/b",
            partition);
        if (reverseAssets)
        {
            first.Files[0].Assets.Add(secondAsset);
            secondAsset.File = first.Files[0];
            secondAsset.Bundle = first;
            second.Files[0].Assets.Add(firstAsset);
            firstAsset.File = second.Files[0];
            firstAsset.Bundle = second;
        }
        else
        {
            first.Files[0].Assets.Add(firstAsset);
            second.Files[0].Assets.Add(secondAsset);
        }

        first.Files[0].SubFiles.Add(new BuildLayout.SubFile
        {
            Name = "scene.entityheader",
            Size = 44
        });
        AddDuplicateImplicitAsset(first, "duplicate-guid", 12);
        AddDuplicateImplicitAsset(second, "duplicate-guid", 12);
        layout.DuplicatedAssets.Add(new BuildLayout.AssetDuplicationData
        {
            AssetGuid = "duplicate-guid",
            DuplicatedObjects = new List<BuildLayout.ObjectDuplicationData>
            {
                new()
                {
                    LocalIdentifierInFile = 1,
                    IncludedInBundleFiles = new List<BuildLayout.File>
                    {
                        first.Files[0],
                        second.Files[0]
                    }
                }
            }
        });
        return layout;
    }

    private static BuildLayout.Bundle CreateBundle(string name, ulong bytes)
    {
        BuildLayout.Bundle bundle = new() { Name = name, FileSize = bytes };
        BuildLayout.File file = new() { Name = name + ".file", Bundle = bundle };
        bundle.Files.Add(file);
        return bundle;
    }

    private static BuildLayout.ExplicitAsset CreateAsset(
        BuildLayout.Bundle bundle,
        string address,
        string partition)
    {
        return new BuildLayout.ExplicitAsset
        {
            AddressableName = address,
            AssetPath = "Assets/" + address.GetHashCode() + ".asset",
            Bundle = bundle,
            File = bundle.Files[0],
            Labels = new[]
            {
                OperationMapAddressablesLayoutBuilder.PackLabel,
                partition
            }
        };
    }

    private static void AddDuplicateImplicitAsset(
        BuildLayout.Bundle bundle,
        string guid,
        ulong bytes)
    {
        bundle.Files[0].OtherAssets.Add(new BuildLayout.DataFromOtherAsset
        {
            AssetGuid = guid,
            AssetPath = "Assets/Shared.asset",
            File = bundle.Files[0],
            SerializedSize = bytes
        });
    }
}
