using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Reflection;
using Game.Composition;

namespace Game.Editor
{
    public class BuildScript
    {
        internal const BuildOptions ReleaseAndroidBuildOptions =
            BuildOptions.DetailedBuildReport;

        internal const BuildOptions CleanReleaseAndroidBuildOptions =
            ReleaseAndroidBuildOptions |
            BuildOptions.CleanBuildCache;

        public static void BuildWindows()
        {
            SwitchBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            var path = "Build/Windows";
            CreateDirectory(path);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                locationPathName = $"{path}/Build.exe"
            };
            ExecuteBuild(buildPlayerOptions);
            ZipBuild(path);
        }

        public static void BuildWebGL()
        {
            SwitchBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            var path = "Build/WebGL";
            CreateDirectory(path);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
                locationPathName = $"{path}"
            };
            ExecuteBuild(buildPlayerOptions);
            ZipBuild(path);
        }

        public static void BuildIOS()
        {
            SwitchBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            var path = "Build/iOS";
            CreateDirectory(path);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.iOS,
                options = BuildOptions.None,
                locationPathName = $"{path}"
            };
            ExecuteBuild(buildPlayerOptions);
            ZipBuild(path);
        }

        public static void BuildAndroid()
        {
            var arg = Environment.GetCommandLineArgs();
            var buildType = GetArgument(arg, "-buildType");
            bool cleanBuildCache = arg.Any(value =>
                string.Equals(value, "-cleanBuild", StringComparison.OrdinalIgnoreCase));
            BuildAndroid(buildType, cleanBuildCache);
        }

        public static void BuildProductionAndroidApk()
        {
            BuildAndroid("APK", cleanBuildCache: true);
            string outputPath = $"Build/AndroidAPK/{ResolveBuildOutputName()}.apk";
            var package = new FileInfo(outputPath);
            UnityEngine.Debug.Log(
                "[ProductionAndroidPackage] result=Passed " +
                $"output={package.FullName} bytes={package.Length} cacheMode=clean");
        }

        private static void BuildAndroid(string buildType, bool cleanBuildCache, Newtonsoft.Json.Linq.JObject preparedContent = null)
        {
            AndroidBuildReportProvenance buildProvenance =
                AndroidBuildReportGenerator.CaptureGitProvenance();
            SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigureGradleUserHome();

            var outputDirectory = buildType switch
            {
                "APK" => "Build/AndroidAPK",
                "AAB" => "Build/AndroidAAB",
                _ => "Unknown"
            };

            if (outputDirectory == "Unknown")
            {
                throw new ArgumentException("BuildAndroid requires -buildType APK or -buildType AAB.");
            }

            CreateDirectory(outputDirectory);
            var extension = buildType == "AAB" ? "aab" : "apk";
            var outputPath = $"{outputDirectory}/{ResolveBuildOutputName()}.{extension}";
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            ConfigureAndroidBuild(buildType == "AAB");
            if(preparedContent==null)
            {
                OperationMapAddressablesBuildReportBuilder.Run();
                ReleasePostAddressablesMemory();
            }
            else ValidatePreparedAndroidContent(preparedContent,scoped:true);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject(GetEnabledScenes()),
                target = BuildTarget.Android,
                options = cleanBuildCache
                    ? CleanReleaseAndroidBuildOptions
                    : ReleaseAndroidBuildOptions,
                locationPathName = outputPath
            };

            UnityEngine.Debug.Log(
                $"[BuildScript] Android cache mode: {(cleanBuildCache ? "clean" : "incremental")}");

            BuildReport report = ExecuteBuild(buildPlayerOptions);
            OperationMapEntityScenePackageGate.Validate(outputPath);
            AndroidBuildReportGenerator.GenerateAndWriteReports(
                report,
                buildType,
                buildProvenance);
        }


        // Explicit two-Editor path only; the normal BuildAndroid entry remains unchanged.
        public static void PrepareAndroidApkContent()
        {
            RequirePreparedAndroidEditor();
            string receiptPath=PreparedFreshPath("WARLINE_ANDROID_PREPARED_CONTENT_RECEIPT");
            ConfigureGradleUserHome();ConfigureAndroidBuild(false);
            var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if(settings==null || EditorUtility.IsDirty(settings))throw new InvalidOperationException("Clean existing Addressables settings required");
            string reportPath=OperationMapAddressablesBuildReportBuilder.ResolveOutputPath();
            RequirePreparedFreshReport(reportPath);
            string settingsOutput=OperationMapAddressablesBuildReportBuilder.BuildAndPublish();
            MaterializePreparedPlayerLink(settings);
            var sourceManifest=PreparedSourceManifest();
            var receipt=new Newtonsoft.Json.Linq.JObject {
                ["schema"]=1,["scope"]="PreparedAndroidAddressables_NoPlayerOrAcceptance",
                ["editorVersion"]=UnityEngine.Application.unityVersion,["target"]="Android",["processId"]=PreparedProcessId(),
                ["settingsPath"]=AssetDatabase.GetAssetPath(settings),["profile"]=settings.activeProfileId,
                ["settingsSerialized"]=CapturePreparedSettingsJson(settings),
                ["builder"]=settings.ActivePlayerDataBuilderIndex,["contentSettingsPath"]=Path.GetFullPath(settingsOutput),
                ["buildPath"]=Path.GetFullPath(UnityEngine.AddressableAssets.Addressables.BuildPath),
                ["reportPath"]=Path.GetFullPath(reportPath),["reportSha256"]=PreparedFileHash(reportPath),
                ["sourceManifest"]=sourceManifest,["playerLink"]=CapturePreparedPlayerLink(settings),
                ["outputs"]=PreparedTree(UnityEngine.AddressableAssets.Addressables.BuildPath),
                ["buildPlayerInvoked"]=false,["packageAccepted"]=false
            };
            OperationMapAddressablesBuildReportBuilder.ValidateContentBuildOutput(settingsOutput);
            using(var stream=new FileStream(receiptPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
                writer.Write(receipt.ToString(Newtonsoft.Json.Formatting.Indented));
            UnityEngine.Debug.Log("[PreparedAndroidAddressables] result=Prepared buildPlayerInvoked=0 receipt="+receiptPath+" sha256="+PreparedFileHash(receiptPath));
        }

        public static void BuildAndroidApkFromPreparedContent()
        {
            RequirePreparedAndroidEditor();
            string receiptPath=Environment.GetEnvironmentVariable("WARLINE_ANDROID_PREPARED_CONTENT_RECEIPT");
            string expected=Environment.GetEnvironmentVariable("WARLINE_ANDROID_PREPARED_CONTENT_SHA256");
            if(string.IsNullOrWhiteSpace(receiptPath)||!Path.IsPathRooted(receiptPath)||
                expected==null||!System.Text.RegularExpressions.Regex.IsMatch(expected,"\\A[a-f0-9]{64}\\z")||PreparedFileHash(receiptPath)!=expected)
                throw new InvalidOperationException("Exact actual prepared receipt required");
            var receipt=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(receiptPath));
            string observationPath=PreparedFreshPath("WARLINE_ANDROID_PREPARED_SOURCE_OBSERVATION");
            string playerObservationPath=PreparedFreshPath("WARLINE_ANDROID_PREPARED_PLAYER_OBSERVATION");
            if(string.Equals(observationPath,playerObservationPath,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Distinct observation paths required");
            var sourceObservation=ValidatePreparedAndroidContent(receipt);
            using(var stream=new FileStream(observationPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
                writer.Write(sourceObservation.ToString(Newtonsoft.Json.Formatting.Indented));
            var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            string settingsPath=AssetDatabase.GetAssetPath(settings),settingsFileHash=PreparedFileHash(settingsPath);
            if(EditorUtility.IsDirty(settings)||AddressablesPlayerBuildProcessor.BuildAddressablesOverride!=null)
                throw new InvalidOperationException("Clean unobserved Addressables settings required");
            try
            {
                WithPreparedNoRebuild(settings,()=>{
                    bool cleanBuildCache=Environment.GetCommandLineArgs().Any(value=>string.Equals(value,"-cleanBuild",StringComparison.OrdinalIgnoreCase));
                    BuildAndroid("APK",cleanBuildCache:cleanBuildCache,preparedContent:receipt);
                });
                if(!Newtonsoft.Json.Linq.JToken.DeepEquals(receipt["outputs"],PreparedTree((string)receipt["buildPath"])))
                    throw new InvalidOperationException("Prepared payload changed during player build");
            }
            finally
            {
                if(UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings!=settings||PreparedFileHash(settingsPath)!=settingsFileHash)
                    throw new InvalidOperationException("Prepared settings scope did not restore exactly; retain evidence and require owned shutdown recovery");
            }
            var playerLink=CapturePreparedPlayerLink(settings);
            var playerObservation=ValidatePreparedPostPlayerSource((Newtonsoft.Json.Linq.JObject)receipt["sourceManifest"],PreparedSourceManifest(),
                (Newtonsoft.Json.Linq.JObject)receipt["playerLink"],playerLink,sourceObservation);
            playerObservation["status"]="CapturedPostPlayerSource_NoMissionAcceptance";
            playerObservation["preparedReceiptSha256"]=expected;playerObservation["prebuildSourceObservationSha256"]=PreparedFileHash(observationPath);
            playerObservation["processId"]=PreparedProcessId();playerObservation["strictPackageGatesPassed"]=true;
            using(var stream=new FileStream(playerObservationPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))
                writer.Write(playerObservation.ToString(Newtonsoft.Json.Formatting.Indented));
            UnityEngine.Debug.Log("[PreparedAndroidPlayer] result=Passed strictPackageGates=1 contentRebuilt=0 settingsRestored=1");
        }
        private static int PreparedProcessId(){using var process=System.Diagnostics.Process.GetCurrentProcess();return process.Id;}

        internal static void WithPreparedNoRebuild(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings,Action build)
        {
            if(settings==null||build==null||AddressablesPlayerBuildProcessor.BuildAddressablesOverride!=null)
                throw new InvalidOperationException("Unobserved prepared settings scope required");
            var previous=settings.BuildAddressablesWithPlayerBuild;string serialized=EditorJsonUtility.ToJson(settings);
            Func<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings,UnityEditor.AddressableAssets.Build.AddressablesPlayerBuildResult> refuse=
                _=>throw new InvalidOperationException("Prepared player phase must not rebuild Addressables");
            try{
                // Public setter only changes the in-memory enum; no SetDirty/SaveAssets.
                settings.BuildAddressablesWithPlayerBuild=UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                AddressablesPlayerBuildProcessor.BuildAddressablesOverride=refuse;build();
            }
            finally{
                bool owned=AddressablesPlayerBuildProcessor.BuildAddressablesOverride==refuse;
                if(owned)AddressablesPlayerBuildProcessor.BuildAddressablesOverride=null;
                settings.BuildAddressablesWithPlayerBuild=previous;
                if(!owned||EditorJsonUtility.ToJson(settings)!=serialized)
                    throw new InvalidOperationException("Prepared callback/settings ownership or restoration failed");
            }
        }

        private static void RequirePreparedAndroidEditor()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
                throw new InvalidOperationException("Idle current Android Editor required");
            if(UnityEngine.Application.unityVersion!="6000.5.8f1")throw new InvalidOperationException("Reviewed installed Unity6000.5.8f1 Addressables processor required");
        }
        private static string PreparedFreshPath(string variable)
        {
            string path=Environment.GetEnvironmentVariable(variable);
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new InvalidOperationException("Fresh absolute owned receipt path required");
            path=Path.GetFullPath(path);
            if(!Directory.Exists(Path.GetDirectoryName(path))||File.Exists(path))throw new InvalidOperationException("Fresh receipt and existing parent required");
            RequirePreparedOrdinaryPath(Path.GetDirectoryName(path));
            return path;
        }
        internal static void RequirePreparedFreshReport(string reportPath)
        {
            if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OperationMapAddressablesBuildReportBuilder.OutputPathEnvironmentVariable))||
                File.Exists(reportPath)||!Directory.Exists(Path.GetDirectoryName(reportPath)))
                throw new InvalidOperationException("Explicit fresh owned Design/AgentReports path and existing parent required");
            RequirePreparedOrdinaryPath(Path.GetDirectoryName(reportPath));
        }
        internal static string PreparedFileHash(string path,bool checkAncestry=true)
        {
            if(checkAncestry)RequirePreparedOrdinaryPath(path);
            using var stream=File.OpenRead(Path.GetFullPath(path));using var sha=System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        internal static Newtonsoft.Json.Linq.JArray PreparedTree(string directory,Func<string,bool> include=null,Func<string,string> fileHash=null)
        {
            string root=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);var files=new System.Collections.Generic.List<string>();
            RequirePreparedOrdinaryPath(root);
            void Walk(string current){
                RequirePreparedNonReparse(File.GetAttributes(current));
                foreach(string file in Directory.GetFiles(current)){
                    RequirePreparedNonReparse(File.GetAttributes(file));
                    if(include==null||include(file)){
                        string identity=Path.GetFullPath(file);
                        files.Add(identity);
                    }
                }
                var children=Directory.GetDirectories(current);RequireUniquePreparedPaths(children);foreach(string child in children)Walk(child);
            }
            Walk(root);RequireUniquePreparedPaths(files);files.Sort(StringComparer.Ordinal);
            if(files.Count==0||files.Count>100000)throw new InvalidOperationException("Bounded nonempty prepared tree required");
            return new Newtonsoft.Json.Linq.JArray(files.Select(file=>new Newtonsoft.Json.Linq.JObject {
                ["path"]=file.Substring(root.Length+1).Replace('\\','/'),["bytes"]=new FileInfo(file).Length,["sha256"]=fileHash==null?PreparedFileHash(file,checkAncestry:false):fileHash(file)}));
        }
        internal static void RequireUniquePreparedPaths(System.Collections.Generic.IEnumerable<string> paths)
        {
            var found=new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(string path in paths)if(!found.Add(Path.GetFullPath(path)))throw new InvalidOperationException("Prepared path case collision refused");
        }
        internal static void RequirePreparedNonReparse(FileAttributes attributes)
        {if((attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Prepared path reparse refused");}
        private static void RequirePreparedOrdinaryPath(string path)
        {
            string current=Path.GetFullPath(path);
            while(!string.IsNullOrEmpty(current)){
                RequirePreparedNonReparse(File.GetAttributes(current));
                current=Path.GetDirectoryName(current);
            }
        }
        private static Newtonsoft.Json.Linq.JObject PreparedSourceManifest()
        {
#if UNITY_EDITOR_WIN
            // Reuse the existing same-assembly CNG provider/buffer across every
            // file, without caching any path, byte, digest or manifest decision.
            // Native/read failures remain failures; never retry midstream with
            // another backend. Non-Windows retains the original managed path.
            using var digest=new SkirmishCandidateSourceHash.FileDigestSession();
            return PreparedSourceManifestWithHasher(digest.ComputeFile);
#else
            return PreparedSourceManifestWithHasher(null);
#endif
        }
        // The null path is the original SHA256.Create oracle. Installed-Editor
        // controls must compare complete manifests before adopting this change.
        internal static Newtonsoft.Json.Linq.JObject PreparedSourceManifestWithHasher(Func<string,string> fileHash)
        {
            var result=new Newtonsoft.Json.Linq.JObject();
            foreach(string directory in new[]{"Assets","Packages","ProjectSettings"})
                result[directory]=PreparedTree(directory,fileHash:fileHash);
            return result;
        }
        private static Newtonsoft.Json.Linq.JObject ValidatePreparedAndroidContent(Newtonsoft.Json.Linq.JObject receipt,bool scoped=false)
        {
            var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if(settings==null||receipt.Value<int>("schema")!=1||(string)receipt["scope"]!="PreparedAndroidAddressables_NoPlayerOrAcceptance"||
                (string)receipt["editorVersion"]!=UnityEngine.Application.unityVersion||(string)receipt["target"]!="Android"||
                receipt.Value<bool>("buildPlayerInvoked")||receipt.Value<bool>("packageAccepted")||
                (string)receipt["settingsPath"]!=AssetDatabase.GetAssetPath(settings)||(string)receipt["profile"]!=settings.activeProfileId||
                receipt.Value<int>("builder")!=settings.ActivePlayerDataBuilderIndex||
                Path.GetFullPath((string)receipt["buildPath"])!=Path.GetFullPath(UnityEngine.AddressableAssets.Addressables.BuildPath))
                throw new InvalidOperationException("Actual prepared target/profile/builder identity differs");
            ValidatePreparedSettings((Newtonsoft.Json.Linq.JObject)receipt["settingsSerialized"],CapturePreparedSettingsJson(settings),scoped);
            if(!OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(true,out string error))throw new InvalidOperationException(error);
            var link=(Newtonsoft.Json.Linq.JObject)receipt["playerLink"];
            string expectedLink=Path.Combine(settings.ConfigFolder,"link.xml").Replace('\\','/');
            if((string)link["assetPath"]!=expectedLink||(string)link["metaPath"]!=expectedLink+".meta"||
                (string)link["outputPath"]!=Path.GetFullPath(Path.Combine(UnityEngine.AddressableAssets.Addressables.BuildPath,"AddressablesLink/link.xml")))
                throw new InvalidOperationException("Prepared link origin differs from actual settings/output");
            var sourceObservation=ValidatePreparedSourceLifecycle((Newtonsoft.Json.Linq.JObject)receipt["sourceManifest"],PreparedSourceManifest(),link);
            RequirePreparedEqual(receipt["outputs"],PreparedTree((string)receipt["buildPath"]),"outputs");
            RequirePreparedEqual(receipt["reportSha256"],new Newtonsoft.Json.Linq.JValue(PreparedFileHash((string)receipt["reportPath"])),"report");
            string contentSettings=Path.GetFullPath((string)receipt["contentSettingsPath"]),buildRoot=Path.GetFullPath((string)receipt["buildPath"])+Path.DirectorySeparatorChar;
            if(!contentSettings.StartsWith(buildRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Prepared runtime settings origin differs");
            OperationMapAddressablesBuildReportBuilder.ValidateContentBuildOutput(contentSettings);
            return sourceObservation;
        }

        internal static void RequirePreparedEqual(Newtonsoft.Json.Linq.JToken expected,Newtonsoft.Json.Linq.JToken actual,string kind)
        {if(expected==null||actual==null||!Newtonsoft.Json.Linq.JToken.DeepEquals(expected,actual))throw new InvalidOperationException("Prepared "+kind+" bytes differ");}
        // The installed public getter initializes its serialized lazy cache.
        // Capture the same initialized state in BOTH Editors; never substitute
        // a receipt value or remove any field from whole-settings comparison.
        internal static Newtonsoft.Json.Linq.JObject CapturePreparedSettingsJson(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings)
        {
            if(settings==null)throw new InvalidOperationException("Actual persisted prepared settings required");
            string path=AssetDatabase.GetAssetPath(settings);
            if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Persisted prepared settings path required");
            string assetBefore=PreparedFileHash(path),metaBefore=PreparedFileHash(path+".meta");
            bool dirtyBefore=EditorUtility.IsDirty(settings);
            _=settings.currentHash;
            var captured=Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(settings));
            if(EditorUtility.IsDirty(settings)!=dirtyBefore||PreparedFileHash(path)!=assetBefore||PreparedFileHash(path+".meta")!=metaBefore)
                throw new InvalidOperationException("Prepared settings capture changed asset/meta bytes or dirty state");
            return captured;
        }

        internal static void ValidatePreparedSettings(Newtonsoft.Json.Linq.JObject expected,Newtonsoft.Json.Linq.JObject actual,bool scoped)
        {
            if(expected==null||actual==null)throw new InvalidOperationException("Actual serialized settings objects required");
            var normalized=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();
            var serialized=normalized["MonoBehaviour"] as Newtonsoft.Json.Linq.JObject;
            if(serialized?["m_BuildAddressablesWithPlayerBuild"]?.Type!=Newtonsoft.Json.Linq.JTokenType.Integer)
                throw new InvalidOperationException("Actual serialized MonoBehaviour settings option required");
            if(scoped)serialized["m_BuildAddressablesWithPlayerBuild"]=(int)UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            RequirePreparedEqual(normalized,actual,"serialized settings");
        }
        private static Newtonsoft.Json.Linq.JObject CapturePreparedPlayerLink(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings)
        {
            string asset=Path.Combine(settings.ConfigFolder,"link.xml").Replace('\\','/'),meta=asset+".meta";
            string output=Path.GetFullPath(Path.Combine(UnityEngine.AddressableAssets.Addressables.BuildPath,"AddressablesLink/link.xml"));
            bool present=File.Exists(asset);if(present!=File.Exists(meta))throw new InvalidOperationException("Partial prepared player link pair");
            string xmlHash=null,metaHash=null,guid=null,metaBase64=null;
            if(present){
                xmlHash=PreparedFileHash(asset);metaHash=PreparedFileHash(meta);guid=AssetDatabase.AssetPathToGUID(asset);
                var match=System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(meta),@"(?m)^guid: ([a-f0-9]{32})\r?$");
                if(xmlHash!=PreparedFileHash(output)||match.Count!=1||match[0].Groups[1].Value!=guid)
                    throw new InvalidOperationException("Actual generated link output/GUID identity required");
                metaBase64=Convert.ToBase64String(File.ReadAllBytes(meta));
            }
            return new Newtonsoft.Json.Linq.JObject { ["present"]=present,["assetPath"]=asset,["metaPath"]=meta,["outputPath"]=output,
                ["xmlSha256"]=xmlHash,["metaSha256"]=metaHash,["metaGuid"]=guid,["metaBase64"]=metaBase64 };
        }
        private static void MaterializePreparedPlayerLink(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings)
        {
            string asset=Path.Combine(settings.ConfigFolder,"link.xml"),meta=asset+".meta";
            string output=Path.Combine(UnityEngine.AddressableAssets.Addressables.BuildPath,"AddressablesLink/link.xml");
            if(!Directory.Exists(settings.ConfigFolder)||!File.Exists(output))throw new InvalidOperationException("Actual prepared link XML and existing settings folder required");
            RequirePreparedOrdinaryPath(output);RequirePreparedOrdinaryPath(settings.ConfigFolder);
            if(File.Exists(asset)!=File.Exists(meta))throw new InvalidOperationException("Foreign partial player link refused");
            if(!File.Exists(asset)){
                // Exact installed processor operations, using real built XML, never a placeholder.
                File.Copy(output,asset,false);
                AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.DontDownloadFromCacheServer);
            }
            var actual=CapturePreparedPlayerLink(settings);
            if(!actual.Value<bool>("present"))throw new InvalidOperationException("Actual imported prepared link pair required");
        }
        internal static void RequirePreparedMetaTemplate(Newtonsoft.Json.Linq.JObject first,Newtonsoft.Json.Linq.JObject second)
        {
            string Normalize(Newtonsoft.Json.Linq.JObject row){
                byte[] bytes=Convert.FromBase64String((string)row["metaBase64"]);
                using var sha=System.Security.Cryptography.SHA256.Create();
                string digest=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
                if(digest!=(string)row["metaSha256"])throw new InvalidOperationException("Actual meta byte/hash join required");
                string text=new System.Text.UTF8Encoding(false,true).GetString(bytes);
                var match=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^guid: ([a-f0-9]{32})\r?$");
                if(match.Count!=1||match[0].Groups[1].Value!=(string)row["metaGuid"])throw new InvalidOperationException("Exact actual meta GUID required");
                var group=match[0].Groups[1];return text.Substring(0,group.Index)+new string('0',32)+text.Substring(group.Index+group.Length);
            }
            if(Normalize(first)!=Normalize(second))throw new InvalidOperationException("Non-GUID player meta change refused");
        }
        internal static Newtonsoft.Json.Linq.JObject ValidatePreparedPostPlayerSource(Newtonsoft.Json.Linq.JObject expected,Newtonsoft.Json.Linq.JObject observed,
            Newtonsoft.Json.Linq.JObject firstLink,Newtonsoft.Json.Linq.JObject actualLink,Newtonsoft.Json.Linq.JObject prebuild)
        {
            if(!firstLink.Value<bool>("present")||!actualLink.Value<bool>("present")||
                new[]{"assetPath","metaPath","outputPath","xmlSha256"}.Any(k=>(string)firstLink[k]!=(string)actualLink[k]))
                throw new InvalidOperationException("Same real prepared XML and exact imported pair required");
            RequirePreparedMetaTemplate(firstLink,actualLink);
            var normalized=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();
            string asset=(string)actualLink["assetPath"],meta=(string)actualLink["metaPath"];
            if(!asset.StartsWith("Assets/",StringComparison.Ordinal)||meta!=asset+".meta")throw new InvalidOperationException("Exact player link paths required");
            var oldRows=(Newtonsoft.Json.Linq.JArray)normalized["Assets"];var newRows=(Newtonsoft.Json.Linq.JArray)observed["Assets"];
            Newtonsoft.Json.Linq.JToken Row(Newtonsoft.Json.Linq.JArray rows,string path){var found=rows.Where(r=>(string)r["path"]==path.Substring(7)).ToArray();if(found.Length!=1)throw new InvalidOperationException("Exactly one real player source row required");return found[0];}
            var oldXml=Row(oldRows,asset);var newXml=Row(newRows,asset);var oldMeta=Row(oldRows,meta);var newMeta=Row(newRows,meta);
            if((string)oldXml["sha256"]!=(string)firstLink["xmlSha256"]||(string)newXml["sha256"]!=(string)actualLink["xmlSha256"]||
                (string)oldMeta["sha256"]!=(string)firstLink["metaSha256"]||(string)newMeta["sha256"]!=(string)actualLink["metaSha256"]||
                (long)newMeta["bytes"]!=Convert.FromBase64String((string)actualLink["metaBase64"]).LongLength)
                throw new InvalidOperationException("Actual player link/source byte joins required");
            oldMeta["sha256"]=actualLink["metaSha256"].DeepClone();
            RequirePreparedEqual(normalized,observed,"post-player source outside exact GUID-only meta change");
            return new Newtonsoft.Json.Linq.JObject { ["schema"]=1,["sourceManifestObserved"]=observed.DeepClone(),["playerLinkObserved"]=actualLink.DeepClone(),
                ["matchesPreparedASourceManifest"]=Newtonsoft.Json.Linq.JToken.DeepEquals(expected,observed),
                ["sourceManifestUnchangedFromBPrebuild"]=Newtonsoft.Json.Linq.JToken.DeepEquals(prebuild["sourceManifestObserved"],observed),
                ["playerLinkTransition"]="ExactPreparedXmlAndGuidOnlyMetaTemplate",["createdPaths"]=prebuild["removedPaths"].DeepClone(),
                ["metaGuidChanged"]=(string)firstLink["metaGuid"]!=(string)actualLink["metaGuid"],["missionAccepted"]=false };
        }
        internal static Newtonsoft.Json.Linq.JObject ValidatePreparedSourceLifecycle(Newtonsoft.Json.Linq.JObject expected,Newtonsoft.Json.Linq.JObject observed,Newtonsoft.Json.Linq.JObject link)
        {
            var normalized=(Newtonsoft.Json.Linq.JObject)expected.DeepClone();
            if(!expected.Properties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(new[]{"Assets","Packages","ProjectSettings"}))
                throw new InvalidOperationException("Exact prepared source roots required");
            string asset=(string)link["assetPath"],meta=(string)link["metaPath"];
            if(string.IsNullOrWhiteSpace(asset)||!asset.StartsWith("Assets/",StringComparison.Ordinal)||meta!=asset+".meta")throw new InvalidOperationException("Exact player link asset pair required");
            var keys=new[]{asset.Substring(7),meta.Substring(7)};
            var prior=(Newtonsoft.Json.Linq.JArray)normalized["Assets"];var actual=(Newtonsoft.Json.Linq.JArray)observed["Assets"];
            Newtonsoft.Json.Linq.JToken Row(Newtonsoft.Json.Linq.JArray rows,string path){var found=rows.Where(r=>(string)r["path"]==path).ToArray();if(found.Length>1)throw new InvalidOperationException("Duplicate prepared source row");return found.SingleOrDefault();}
            var oldRows=keys.Select(k=>Row(prior,k)).ToArray();var newRows=keys.Select(k=>Row(actual,k)).ToArray();
            bool removed=false;
            if(link.Value<bool>("present")){
                if(oldRows.Any(r=>r==null)||(string)oldRows[0]["sha256"]!=(string)link["xmlSha256"]||(string)oldRows[1]["sha256"]!=(string)link["metaSha256"]||
                    !System.Text.RegularExpressions.Regex.IsMatch((string)link["metaGuid"]??"","\\A[a-f0-9]{32}\\z"))throw new InvalidOperationException("Pinned actual generated link pair required");
                if(newRows.All(r=>r==null)){foreach(var row in oldRows)row.Remove();removed=true;}
                else if(newRows.Any(r=>r==null))throw new InvalidOperationException("Partial player link cleanup refused");
            }
            else if(oldRows.Any(r=>r!=null)||newRows.Any(r=>r!=null))throw new InvalidOperationException("Absent A pair grants no lifecycle waiver");
            RequirePreparedEqual(normalized,observed,"source inventory after exact link lifecycle");
            return new Newtonsoft.Json.Linq.JObject { ["schema"]=1,["sourceManifestObserved"]=observed.DeepClone(),
                ["wholeSourceManifestUnchanged"]=!removed,["playerLinkTransition"]=removed?"ExactPairAbsentAfterEditorInitialization":"Unchanged",
                ["removedPaths"]=removed?new Newtonsoft.Json.Linq.JArray(asset,meta):new Newtonsoft.Json.Linq.JArray(),
                ["missionAccepted"]=false };
        }

        [Serializable]
        private sealed class AndroidMemoryBoundaryPoint
        {
            public long privateBytes;
            public long workingSetBytes;
            public long unityAllocatedBytes;
            public long unityReservedBytes;
            public long monoUsedBytes;
            public long managedHeapBytes;
        }

        [Serializable]
        private sealed class AndroidMemoryBoundaryReport
        {
            public int schemaVersion = 1;
            public string status;
            public string utc;
            public int processId;
            public double releaseMilliseconds;
            public AndroidMemoryBoundaryPoint before;
            public AndroidMemoryBoundaryPoint after;
            public bool buildPlayerInvoked;
            public bool sceneSetupUnchanged;
            public bool memoryReductionRequired;
        }

        private static AndroidMemoryBoundaryPoint ReadAndroidMemoryBoundaryPoint()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            process.Refresh();
            return new AndroidMemoryBoundaryPoint {
                privateBytes = process.PrivateMemorySize64,
                workingSetBytes = process.WorkingSet64,
                unityAllocatedBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                unityReservedBytes = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong(),
                monoUsedBytes = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),
                managedHeapBytes = GC.GetTotalMemory(false)
            };
        }

        private static AndroidMemoryBoundaryReport ReleasePostAddressablesMemory()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            var report = new AndroidMemoryBoundaryReport {
                utc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                processId = process.Id,
                before = ReadAndroidMemoryBoundaryPoint()
            };
            var timer = System.Diagnostics.Stopwatch.StartNew();
            GC.Collect();
            // Parameterless overload retains script/static references in Unity6000.5.
            EditorUtility.UnloadUnusedAssetsImmediate();
            GC.Collect();
            timer.Stop();
            report.after = ReadAndroidMemoryBoundaryPoint();
            report.releaseMilliseconds = timer.Elapsed.TotalMilliseconds;
            report.status = "CollectedPostAddressablesMemory_NoBuildOrAcceptance";
            UnityEngine.Debug.Log("[AndroidPrePlayerMemoryBoundary] " + UnityEngine.JsonUtility.ToJson(report));
            return report;
        }

        // Diagnostic stops at the real synchronous content/report -> player boundary.
        public static void RunAndroidAddressablesMemoryBoundaryDiagnostic()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Idle Android Editor required for memory boundary diagnostic.");
            string output = Environment.GetEnvironmentVariable("WARLINE_ANDROID_MEMORY_BOUNDARY_OUTPUT");
            if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                throw new InvalidOperationException("Fresh absolute owned diagnostic output required.");
            output = Path.GetFullPath(output);
            string contentReport = Environment.GetEnvironmentVariable("WARLINE_ANDROID_MEMORY_BOUNDARY_CONTENT_REPORT");
            if (string.IsNullOrWhiteSpace(contentReport) || !Path.IsPathRooted(contentReport))
                throw new InvalidOperationException("Fresh absolute owned Design/AgentReports output required.");
            contentReport = Path.GetFullPath(contentReport);
            if (!Directory.Exists(Path.GetDirectoryName(output)) || !Directory.Exists(Path.GetDirectoryName(contentReport)) ||
                File.Exists(output) || File.Exists(contentReport) ||
                string.Equals(output, contentReport, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Existing parent and two fresh diagnostic paths required.");
            var before = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
            const string reportVariable = OperationMapAddressablesBuildReportBuilder.OutputPathEnvironmentVariable;
            string previous = Environment.GetEnvironmentVariable(reportVariable);
            AndroidMemoryBoundaryReport report;
            try
            {
                Environment.SetEnvironmentVariable(reportVariable, contentReport);
                // Enforce the existing Design/AgentReports JSON guard BEFORE content work.
                OperationMapAddressablesBuildReportBuilder.ResolveOutputPath();
                OperationMapAddressablesBuildReportBuilder.Run();
                report = ReleasePostAddressablesMemory();
            }
            finally { Environment.SetEnvironmentVariable(reportVariable, previous); }
            var after = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
            report.sceneSetupUnchanged = before.Length == after.Length && before.Zip(after, (a, b) =>
                a.path == b.path && a.isLoaded == b.isLoaded && a.isActive == b.isActive).All(equal => equal);
            if (!report.sceneSetupUnchanged)
                throw new InvalidOperationException("Diagnostic must preserve actual scene setup.");
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                writer.Write(UnityEngine.JsonUtility.ToJson(report, true));
            UnityEngine.Debug.Log("[AndroidAddressablesMemoryBoundaryDiagnostic] result=Collected buildPlayerInvoked=0 sceneSetupUnchanged=1 memoryReductionRequired=0");
        }

        public static void BuildDenseCityCandidateAndroidApk()
        {
            BuildDenseCityCandidateAndroidApk(profilerBuild: false);
        }

        public static void BuildDenseCityCandidateAndroidProfilerApk()
        {
            BuildDenseCityCandidateAndroidApk(profilerBuild: true);
        }

        [MenuItem("Tools/Validation/Build Dense City Candidate Android Profiler APK")]
        private static void BuildDenseCityCandidateAndroidProfilerApkFromGui()
        {
            const string variableName = "GRADLE_USER_HOME";
            string previousGradleUserHome =
                Environment.GetEnvironmentVariable(variableName);
            string shortGradleUserHome = Path.GetFullPath(".g");
            CreateDirectory(shortGradleUserHome);

            try
            {
                Environment.SetEnvironmentVariable(variableName, shortGradleUserHome);
                UnityEngine.Debug.Log(
                    $"[BuildScript] GUI short-path {variableName}: {shortGradleUserHome}");
                BuildDenseCityCandidateAndroidProfilerApk();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    variableName,
                    previousGradleUserHome);
            }
        }

        private static void BuildDenseCityCandidateAndroidApk(bool profilerBuild)
        {
            AndroidBuildReportProvenance buildProvenance =
                AndroidBuildReportGenerator.CaptureGitProvenance();

            SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigureGradleUserHome();
            ConfigureAndroidBuild(buildAppBundle: false);
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .BuildDenseCityCandidateEmbeddedAndroidContent();

            string outputDirectory = profilerBuild
                ? "Build/AndroidDenseCandidateProfiler"
                : "Build/AndroidDenseCandidate";
            CreateDirectory(outputDirectory);
            string outputSuffix = profilerBuild
                ? "DenseCityCandidate-Profiler"
                : "DenseCityCandidate";
            string outputPath =
                $"{outputDirectory}/{ResolveBuildOutputName()}-{outputSuffix}.apk";
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            string projectRoot = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath,
                ".."));
            string[] scenes =
                OperationMapDenseCityCandidateAndroidPackageDeployment.ResolvePlayerScenes(
                    GetEnabledScenes(),
                    path => File.Exists(Path.GetFullPath(Path.Combine(projectRoot, path))) &&
                            !string.IsNullOrWhiteSpace(
                                AssetDatabase.AssetPathToGUID(path)));
            using var deployment =
                OperationMapDenseCityCandidateAndroidPackageDeployment.Begin(projectRoot);
            using var entitySceneOverride =
                OperationMapEntitySceneBuildAdditions.UseCurrentProcessSceneOverride(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            string denseGuid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.Android,
                extraScriptingDefines =
                    OperationMapDenseCityCandidateAndroidPackageDeployment
                        .GetRuntimeScriptingDefines(denseGuid),
                options = ResolveDenseCityCandidateAndroidBuildOptions(profilerBuild),
                locationPathName = outputPath
            };

            UnityEngine.Debug.Log(
                "[BuildScript] Dense candidate Android package: " +
                $"profilerBuild={(profilerBuild ? 1 : 0)} " +
                "cacheMode=clean " +
                $"revision={buildProvenance.ExactCommit} dirty={buildProvenance.Dirty}");
            BuildReport report = ExecuteBuild(buildPlayerOptions);

            string productionGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
            OperationMapDenseCityCandidateAndroidPackageDeployment.ValidatePackage(
                outputPath,
                denseGuid,
                productionGuid);
            UnityEngine.Debug.Log(
                "[DenseCityCandidateAndroidPackage] result=Passed " +
                $"output={outputPath} bytes={report.summary.totalSize} " +
                $"entitySceneGuid={denseGuid} productionEntitySceneIncluded=0");
        }

        internal static BuildOptions ResolveDenseCityCandidateAndroidBuildOptions(
            bool profilerBuild)
        {
            BuildOptions options = profilerBuild
                ? BuildOptions.Development
                : ReleaseAndroidBuildOptions;
            return options | BuildOptions.CleanBuildCache;
        }

        public static void ValidateDenseCityCandidateAndroidApk()
        {
            const string packagePath =
                "Build/AndroidDenseCandidate/WarlineCapture-DenseCityCandidate.apk";
            string denseGuid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            string productionGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
            OperationMapDenseCityCandidateAndroidPackageDeployment.ValidatePackage(
                packagePath,
                denseGuid,
                productionGuid);
            var package = new FileInfo(packagePath);
            UnityEngine.Debug.Log(
                "[DenseCityCandidateAndroidPackageGate] result=Passed " +
                $"output={package.FullName} bytes={package.Length} " +
                $"entitySceneGuid={denseGuid} productionEntitySceneIncluded=0");
        }

        public static void BuildAndroidProfilerApk()
        {
            BuildAndroidProfilerApk(disableBurstAot: false);
        }

        public static void BuildAndroidProfilerNoBurstApk()
        {
            BuildAndroidProfilerApk(disableBurstAot: true);
        }

        private static void BuildAndroidProfilerApk(bool disableBurstAot)
        {
            SwitchBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigureGradleUserHome();
            ConfigureAndroidBuild(false);
            OperationMapAddressablesBuildReportBuilder.Run();
            using var burstAotScope = disableBurstAot
                ? TryDisableBurstAotForAndroidBuild()
                : null;

            const string outputDirectory = "Build/AndroidProfiler";
            CreateDirectory(outputDirectory);
            var outputSuffix = disableBurstAot ? "Profiler-NoBurst" : "Profiler";
            var outputPath = $"{outputDirectory}/{ResolveBuildOutputName()}-{outputSuffix}.apk";
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject(GetEnabledScenes()),
                target = BuildTarget.Android,
                options =
                    BuildOptions.Development |
                    BuildOptions.ConnectWithProfiler,
                locationPathName = outputPath
            };

            UnityEngine.Debug.Log($"[BuildScript] Android profiler APK configured: development=1 autoconnectProfiler=1 scriptDebugging=0 deepProfiling=0 burstAot={(disableBurstAot ? 0 : 1)}");
            ExecuteBuild(buildPlayerOptions);
        }

        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(x => x.enabled)
                .Select(x => x.path)
                .ToArray();
        }

        private static string ResolveBuildOutputName()
        {
            string productName = PlayerSettings.productName;
            if (string.IsNullOrWhiteSpace(productName))
                return "Game";

            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
            string sanitized = new(productName
                .Select(character => invalidFileNameChars.Contains(character) ? '_' : character)
                .ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "Game" : sanitized;
        }

        private static void ZipBuild(string path)
        {
            var zipPath = $"{path}.zip";
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(path, zipPath);
        }

        private static BuildReport ExecuteBuild(BuildPlayerOptions buildPlayerOptions)
        {
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Build failed. target={buildPlayerOptions.target} result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings}");
            }

            UnityEngine.Debug.Log(
                $"Build succeeded. target={buildPlayerOptions.target} output={summary.outputPath} size={summary.totalSize} warnings={summary.totalWarnings}");
            return report;
        }

        private static void ConfigureAndroidBuild(bool buildAppBundle)
        {
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.enableFrameTimingStats = true;
            //PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            UnityEngine.Debug.Log(
                "[BuildScript] Android build configured: architectures=ARM64 frameTimingStats=1");
        }

        private static void ConfigureGradleUserHome()
        {
            const string variableName = "GRADLE_USER_HOME";
            var configuredPath = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                UnityEngine.Debug.Log($"[BuildScript] Using existing {variableName}: {configuredPath}");
                return;
            }

            var projectGradleHome = Path.GetFullPath(Path.Combine("Library", "GradleCache"));
            CreateDirectory(projectGradleHome);
            Environment.SetEnvironmentVariable(variableName, projectGradleHome);
            UnityEngine.Debug.Log($"[BuildScript] Redirected {variableName} to {projectGradleHome}");
        }

        private static IDisposable TryDisableBurstAotForAndroidBuild()
        {
            Type settingsType = Type.GetType("Unity.Burst.Editor.BurstPlatformAotSettings, Unity.Burst.Editor");
            if (settingsType == null)
            {
                UnityEngine.Debug.LogWarning("[BuildScript] Burst AOT settings type not found; continuing without disabling Burst AOT.");
                return null;
            }

            MethodInfo getOrCreateSettings = settingsType.GetMethod(
                "GetOrCreateSettings",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo save = settingsType.GetMethod(
                "Save",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo enableBurstCompilation = settingsType.GetField(
                "EnableBurstCompilation",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (getOrCreateSettings == null || save == null || enableBurstCompilation == null)
            {
                UnityEngine.Debug.LogWarning("[BuildScript] Burst AOT settings members not found; continuing without disabling Burst AOT.");
                return null;
            }

            object target = (BuildTarget?)BuildTarget.Android;
            object settings = getOrCreateSettings.Invoke(null, new[] { target });
            bool original = (bool)enableBurstCompilation.GetValue(settings);
            if (!original)
            {
                UnityEngine.Debug.Log("[BuildScript] Android Burst AOT was already disabled.");
                return null;
            }

            enableBurstCompilation.SetValue(settings, false);
            save.Invoke(settings, new[] { target });
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[BuildScript] Android Burst AOT temporarily disabled for profiler APK build.");
            return new RestoreBurstAotSettings(settings, save, enableBurstCompilation, target, original);
        }

        private sealed class RestoreBurstAotSettings : IDisposable
        {
            private readonly object settings;
            private readonly MethodInfo save;
            private readonly FieldInfo enableBurstCompilation;
            private readonly object target;
            private readonly bool original;
            private bool restored;

            public RestoreBurstAotSettings(
                object settings,
                MethodInfo save,
                FieldInfo enableBurstCompilation,
                object target,
                bool original)
            {
                this.settings = settings;
                this.save = save;
                this.enableBurstCompilation = enableBurstCompilation;
                this.target = target;
                this.original = original;
            }

            public void Dispose()
            {
                if (restored)
                    return;

                restored = true;
                enableBurstCompilation.SetValue(settings, original);
                save.Invoke(settings, new[] { target });
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[BuildScript] Android Burst AOT setting restored after profiler APK build.");
            }
        }

        private static void SwitchBuildTarget(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            if (EditorUserBuildSettings.activeBuildTarget != buildTarget &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget))
            {
                throw new InvalidOperationException(
                    $"Failed to switch active build target. targetGroup={buildTargetGroup} target={buildTarget}");
            }

            UnityEngine.Debug.Log($"[BuildScript] Active build target: {EditorUserBuildSettings.activeBuildTarget}");
        }

        private static string GetArgument(string[] args, string argumentName)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == argumentName && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
