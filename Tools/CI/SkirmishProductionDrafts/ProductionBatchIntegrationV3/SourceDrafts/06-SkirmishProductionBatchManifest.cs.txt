using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Game.Editor
{
    public static class SkirmishProductionBatchManifest
    {
        public static void Validate(JObject manifest)
        {
            var sources=manifest["source_manifest"] as JArray;
            var builds=manifest["build_manifest"] as JArray;
            if(sources==null||sources.Count==0||builds==null||builds.Count==0)throw new InvalidDataException("Complete manifest rows absent.");
            ValidateRows(sources,false);ValidateRows(builds,true);
            using(var bytes=new MemoryStream())
            {
                using(var writer=new BinaryWriter(bytes,new UTF8Encoding(false),true))
                    foreach(var row in sources.Cast<JObject>().OrderBy(r=>((string)r["path"]).Replace('/','\\'),StringComparer.Ordinal))
                    {
                        writer.Write((string)row["path"]);writer.Write(32);
                        string hash=(string)row["sha256"];
                        for(int i=0;i<64;i+=2)writer.Write(Convert.ToByte(hash.Substring(i,2),16));
                    }
                if(SkirmishProductionCanaryRequest.Hash(bytes.ToArray())!=(string)manifest["candidate_sha256"])
                    throw new InvalidDataException("Shared source rows do not derive the exact native candidate identity.");
            }
            var ordered=new JArray(builds.Cast<JObject>().OrderBy(r=>(string)r["path"],StringComparer.Ordinal));
            if(SkirmishProductionCanaryRequest.Hash(Encoding.UTF8.GetBytes(SkirmishProductionCanaryRequest.Canonical(ordered)))!=(string)manifest["build_sha256"])
                throw new InvalidDataException("Shared DLL rows do not derive the exact build identity.");
        }
        private static void ValidateRows(JArray rows,bool builds)
        {
            var paths=new HashSet<string>(StringComparer.Ordinal);
            foreach(var token in rows)
            {
                var row=token as JObject;
                if(row==null||row.Count!=2||row["path"]?.Type!=JTokenType.String||row["sha256"]?.Type!=JTokenType.String)
                    throw new InvalidDataException("Malformed shared manifest row.");
                string path=(string)row["path"],hash=(string)row["sha256"];
                if(!paths.Add(path)||!Regex.IsMatch(hash,"^[a-f0-9]{64}$")||Path.IsPathRooted(path)||path.Contains("\\")||path.Contains(":")||path.Split('/').Contains(".."))
                    throw new InvalidDataException("Unsafe or duplicate shared manifest path/hash.");
                if(builds? !path.StartsWith("Library/ScriptAssemblies/",StringComparison.Ordinal)||!path.EndsWith(".dll",StringComparison.Ordinal):
                    !(path.StartsWith("Assets/",StringComparison.Ordinal)||path.StartsWith("Packages/",StringComparison.Ordinal)||path.StartsWith("ProjectSettings/",StringComparison.Ordinal)))
                    throw new InvalidDataException("Manifest row is outside its native census roots.");
            }
        }
    }
}
