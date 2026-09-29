using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor.Build;
using UnityEngine;

namespace Game.Editor.MapVariants
{
    /// <summary>Deploys the loose DOTS scene files for the scoped native acceptance player.</summary>
    internal sealed class MapVariantPreparedEntitySceneBuildProcessor : BuildPlayerProcessor
    {
        [Serializable] private sealed class Report
        {
            public string entitySceneGuid, entityContentPath;
        }

        public override void PrepareForBuild(BuildPlayerContext context)
        {
            string[] args=Environment.GetCommandLineArgs();
            string Value(string key)
            {
                int i=Array.IndexOf(args,key);
                return i>=0 && i+1<args.Length ? args[i+1] : null;
            }
            string player=Value("-buildPlayerPath");
            if(!args.Contains("-runTests") || Value("-assemblyNames")!="Game.Tests.MapPreparation.Native" ||
               Value("-testPlatform")!="StandaloneOSX" || string.IsNullOrEmpty(player) ||
               !Path.GetFullPath(player).StartsWith(Path.GetFullPath("Build/MapVariantPreparedPlayer")+Path.DirectorySeparatorChar,StringComparison.Ordinal)) return;

            var files=new SortedDictionary<string,string>(StringComparer.Ordinal);
            string filter=Value("-testFilter") ?? "";
            bool frontierOnly=filter.Contains("Frontier_PackedFullMapLoadReloadDamageAndActualUnitRoutes") ||
                filter.Contains("Frontier_SwitchWithExistingDenseCity");
            string[] maps=frontierOnly ? new[]{"Frontier"} : MapVariantPreparedCandidateBuilder.MediumMaps;
            foreach(string map in maps)
            {
                string reportPath=Path.GetFullPath("Design/MapVariants/Preparation/"+map+"/Candidate/runtime-content.json");
                if(!File.Exists(reportPath)) throw new FileNotFoundException("Prepared native content report missing",reportPath);
                var report=JsonUtility.FromJson<Report>(File.ReadAllText(reportPath));
                if(string.IsNullOrWhiteSpace(report.entitySceneGuid) || string.IsNullOrWhiteSpace(report.entityContentPath))
                    throw new InvalidDataException("Prepared native content report incomplete: "+reportPath);
                string sceneRoot=Path.GetFullPath(Path.Combine(report.entityContentPath,"EntityScenes"));
                if(!Directory.Exists(sceneRoot)) throw new DirectoryNotFoundException(sceneRoot);
                string header=Path.Combine(sceneRoot,report.entitySceneGuid+".entityheader");
                if(!File.Exists(header)) throw new FileNotFoundException("Prepared entity header missing",header);
                foreach(string source in Directory.GetFiles(sceneRoot,"*",SearchOption.TopDirectoryOnly))
                {
                    string name=Path.GetFileName(source);
                    if(!name.EndsWith(".entityheader",StringComparison.Ordinal) && !name.EndsWith(".entities",StringComparison.Ordinal)) continue;
                    string destination="EntityScenes/"+name;
                    if(files.TryGetValue(destination,out string previous))
                    {
                        if(!Hash(source).SequenceEqual(Hash(previous)))
                            throw new InvalidDataException("Prepared native scene collision: "+destination+" from "+previous+" and "+source);
                    }
                    else files.Add(destination,source);
                }
            }
            foreach(var file in files)
                context.AddAdditionalPathToStreamingAssets(file.Value,file.Key);
            Debug.Log("[MapVariantNativeScenes] result=Passed files="+files.Count+" scope="+(frontierOnly?"FrontierNativePlayer":"MediumNativePlayer"));
        }

        private static byte[] Hash(string path)
        {
            using var stream=File.OpenRead(path);
            using var sha=SHA256.Create();
            return sha.ComputeHash(stream);
        }
    }
}
