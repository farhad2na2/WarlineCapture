#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Configs;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace Game.Editor
{
    // Cache preparation, never a mission success/readiness entry. Must run after
    // code/profile import and BEFORE the complete candidate source/build snapshot.
    public static class S005SceneArtifactWarmup
    {
        [Serializable] sealed class Row { public string rootScene,sceneGuid,scenePath,buildGuid,artifactHash,sceneCacheGuid,sceneCachePath; public long elapsedStopwatchTicks; }
        [Serializable] sealed class Receipt { public int schemaVersion=2; public string scope="EditorDefaultSceneSystemArtifactWarmupOnly",unityVersion,buildConfigurationSource="IsolatedNativeSceneSystem.OnCreate"; public bool defaultWorldPreserved=true; public long stopwatchFrequency; public Row[] scenes; public bool playStarted=false,missionCertified=false; }
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Idle owned Editor required.");
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=Environment.GetEnvironmentVariable("WARLINE_S005_SCENE_WARMUP_OUTPUT");
            string durable=Path.Combine(project,"Logs","SkirmishProduction")+Path.DirectorySeparatorChar;
            if(string.IsNullOrEmpty(output)||!Path.IsPathRooted(output)||
                !Path.GetFullPath(output).StartsWith(durable,StringComparison.OrdinalIgnoreCase)||File.Exists(output))
                throw new InvalidOperationException("Fresh absolute Logs/SkirmishProduction warmup receipt required.");
            var catalog=SkirmishExpansionCatalogConfig.Load();
            if(catalog==null||!catalog.TryGetDefinition("S005",out var definition)||definition.CampaignMap==null)
                throw new InvalidOperationException("Actual packaged S005 Campaign source required.");
            string source=AssetDatabase.GUIDToAssetPath(definition.CampaignMap.SourceSceneReference.AssetGUID);
            var roots=new[]{"Assets/Game/Scenes/Match.unity",source};
            var rows=new List<Row>();var seen=new HashSet<string>(StringComparer.Ordinal);
            var api=typeof(SceneSystem).Assembly.GetType("Unity.Scenes.EntityScenesPaths",true);
            var method=api.GetMethod("GetSubSceneArtifactHash",BindingFlags.Static|BindingFlags.NonPublic);
            if(method==null)throw new MissingMethodException("Installed Entities artifact API differs.");
            var parameters=method.GetParameters();
            if(parameters.Length!=4||parameters[0].ParameterType!=typeof(Hash128)||parameters[1].ParameterType!=typeof(Hash128)||
                parameters[2].ParameterType!=typeof(bool)||!parameters[3].ParameterType.IsEnum||method.ReturnType!=typeof(Hash128))
                throw new InvalidOperationException("Installed Entities artifact signature differs.");
            var sync=Enum.Parse(parameters[3].ParameterType,"Synchronous");
            var noImport=Enum.Parse(parameters[3].ParameterType,"NoImport");
            var previousDefault=World.DefaultGameObjectInjectionWorld;
            Hash128 build;
            using(var nativeWorld=new World("S005SceneArtifactWarmupNativeKey",WorldFlags.Game))
            {
                var sceneSystem=nativeWorld.GetOrCreateSystem<SceneSystem>();
                build=nativeWorld.EntityManager.GetComponentData<SceneSystemData>(sceneSystem).BuildConfigurationGUID;
                if(!ReferenceEquals(previousDefault,World.DefaultGameObjectInjectionWorld))
                    throw new InvalidOperationException("Warmup changed the default world.");
                if(build.IsValid)throw new InvalidOperationException("Installed native Editor SceneSystem default key changed; re-audit before warming.");
            }
            if(!ReferenceEquals(previousDefault,World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Warmup disposal changed the default world.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string root in roots)
            {
                if(string.IsNullOrEmpty(root)||!File.Exists(root))throw new InvalidOperationException("Actual root scene unavailable.");
                var matches=Regex.Matches(File.ReadAllText(root),@"_SceneAsset:\s*\{fileID:\s*102900000,\s*guid:\s*([0-9a-f]{32}),\s*type:\s*3\}");
                if(matches.Count!=1)throw new InvalidOperationException("Bounded warmup expects exactly one actual SubScene reference per current root: "+root);
                string guid=matches[0].Groups[1].Value;
                if(root==source&&guid!=definition.CampaignMap.NavigationMetadata.AuthoredSubSceneGuid)
                    throw new InvalidOperationException("Actual source SubScene and compiled navigation binding differ.");
                if(!seen.Add(guid))throw new InvalidOperationException("Expected two distinct actual root SubScenes.");
                string scene=AssetDatabase.GUIDToAssetPath(guid);
                if(string.IsNullOrEmpty(scene)||!File.Exists(scene))throw new InvalidOperationException("Actual referenced SubScene missing.");
                var timer=Stopwatch.StartNew();
                var artifact=(Hash128)method.Invoke(null,new object[]{new Hash128(guid),build,true,sync});
                timer.Stop();
                if(!artifact.IsValid)throw new InvalidOperationException("Synchronous native SubScene artifact import unavailable: "+guid);
                var observed=(Hash128)method.Invoke(null,new object[]{new Hash128(guid),build,true,noImport});
                if(!observed.IsValid||!observed.Equals(artifact))throw new InvalidOperationException("Native artifact cache failed immediate no-import verification.");
                var cacheType=typeof(SceneSystem).Assembly.GetType("Unity.Scenes.SceneWithBuildConfigurationGUIDs",true);
                var ensure=cacheType.GetMethod("EnsureExistsFor",BindingFlags.Public|BindingFlags.Static);
                var cacheArgs=new object[]{new Hash128(guid),build,true,false};
                var cache=(Hash128)ensure.Invoke(null,cacheArgs);
                string cachePath=(string)cacheType.GetMethod("GetSceneWithBuildSettingsPath",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{cache});
                if((bool)cacheArgs[3]||!File.Exists(cachePath)||!File.Exists(cachePath+".meta"))
                    throw new InvalidOperationException("Exact Editor-world scene cache was not fully materialized by synchronous import.");
                rows.Add(new Row{rootScene=root,sceneGuid=guid,scenePath=scene,buildGuid=build.ToString(),artifactHash=artifact.ToString(),
                    sceneCacheGuid=cache.ToString(),sceneCachePath=cachePath,elapsedStopwatchTicks=timer.ElapsedTicks});
            }
            if(rows.Count!=2||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Warmup scope changed.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Receipt{unityVersion=Application.unityVersion,
                stopwatchFrequency=Stopwatch.Frequency,scenes=rows.ToArray()},true));
            using(var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))file.Write(bytes,0,bytes.Length);
            UnityEngine.Debug.Log("[S005SceneArtifactWarmup] result=Passed scenes=2 keySource=NativeEditorSceneSystem defaultWorldPreserved=1 playStarted=0 missionCertified=0 snapshotAfterWrapperExit=1 receipt="+output);
        }
    }
}
#endif
