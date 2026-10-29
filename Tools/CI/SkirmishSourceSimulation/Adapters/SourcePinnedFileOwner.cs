using System;using System.Collections.Generic;using System.IO;using System.Security.Cryptography;using Newtonsoft.Json.Linq;
namespace Game.Runtime { internal sealed partial class AutonomousWorld {
#if WARLINE_SOURCE_RELOCATABLE_PINNED_INPUTS
        internal static bool PinnedMappingActive => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_PINNED_ROOT_ORIGINAL")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_PINNED_ROOT_LOCAL")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_PINNED_ROOT_MAP"));
        static List<(string original,string local)> PinnedRootPairs()
        {
            string from=Environment.GetEnvironmentVariable("WARLINE_PINNED_ROOT_ORIGINAL"),to=Environment.GetEnvironmentVariable("WARLINE_PINNED_ROOT_LOCAL"),multi=Environment.GetEnvironmentVariable("WARLINE_PINNED_ROOT_MAP");
            if (!string.IsNullOrEmpty(multi) && (!string.IsNullOrEmpty(from)||!string.IsNullOrEmpty(to))) throw new NotSupportedException("Choose one relocation format");
            var rows=new List<(string original,string local)>();
            if (!string.IsNullOrEmpty(multi)) { if(multi.Length>16384)throw new NotSupportedException("Bounded relocation mapping required");var values=JArray.Parse(multi,new Newtonsoft.Json.Linq.JsonLoadSettings{DuplicatePropertyNameHandling=Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Error});if(values.Count<1||values.Count>16)throw new NotSupportedException("Bounded relocation mapping required");foreach(var token in values){var row=token as JObject;if(row==null||row.Count!=2||row["original"]?.Type!=JTokenType.String||row["local"]?.Type!=JTokenType.String)throw new NotSupportedException("Exact relocation mapping required");rows.Add(((string)row["original"],(string)row["local"]));} }
            else if(!string.IsNullOrEmpty(from)||!string.IsNullOrEmpty(to))rows.Add((from,to));
            foreach(var pair in rows){if(string.IsNullOrEmpty(pair.original)||string.IsNullOrEmpty(pair.local)||!Path.IsPathRooted(pair.original)||!Path.IsPathRooted(pair.local)||Path.GetFullPath(pair.original)!=pair.original||Path.GetFullPath(pair.local)!=pair.local||pair.original==Path.DirectorySeparatorChar.ToString()||pair.local==Path.DirectorySeparatorChar.ToString()||!Directory.Exists(pair.local))throw new NotSupportedException("Exact absolute relocation roots required");for(var at=new DirectoryInfo(pair.local);at!=null;at=at.Parent)if((at.Attributes&FileAttributes.ReparsePoint)!=0)throw new NotSupportedException("Symlink in relocation root");}
            bool Overlap(string a,string b)=>a==b||a.StartsWith(b+Path.DirectorySeparatorChar,StringComparison.Ordinal)||b.StartsWith(a+Path.DirectorySeparatorChar,StringComparison.Ordinal);
            for(int i=0;i<rows.Count;i++){if(Overlap(rows[i].original,rows[i].local))throw new NotSupportedException("Overlapping relocation roots");for(int j=0;j<i;j++)if(Overlap(rows[i].original,rows[j].original)||Overlap(rows[i].local,rows[j].local)||Overlap(rows[i].original,rows[j].local)||Overlap(rows[i].local,rows[j].original))throw new NotSupportedException("Overlapping relocation roots");}
            return rows;
        }
        internal static string HistoricalPinnedRoot(string actual){foreach(var p in PinnedRootPairs())if(actual==p.local)return p.original;return actual;}
        internal static string ResolvePinnedPath(string path)
        {
            if (!Path.IsPathRooted(path)) throw new ArgumentException("Absolute pinned source path required");
            foreach(var pair in PinnedRootPairs())
            {
                string prefix=pair.original+Path.DirectorySeparatorChar;
                if(path!=pair.original&&!path.StartsWith(prefix,StringComparison.Ordinal))continue;
                if(Path.GetFullPath(path)!=path)throw new NotSupportedException("Canonical historical source input required");
                string openedPath=path==pair.original?pair.local:Path.Combine(pair.local,path.Substring(prefix.Length));
                if(openedPath!=pair.local&&!openedPath.StartsWith(pair.local+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new NotSupportedException("Relocated source input escaped root");
                for(string at=openedPath;!string.IsNullOrEmpty(at);at=Path.GetDirectoryName(at)){if((File.GetAttributes(at)&FileAttributes.ReparsePoint)!=0)throw new NotSupportedException("Symlink in relocated source input");string parent=Path.GetDirectoryName(at);if(parent==at)break;}
                return openedPath;
            }
            return path;
        }
#else
        internal static bool PinnedMappingActive => false;
        internal static string HistoricalPinnedRoot(string actual) => actual;
        internal static string ResolvePinnedPath(string path)
        {
            if (!Path.IsPathRooted(path)) throw new ArgumentException("Absolute pinned source path required");
            return path;
        }
#endif
        internal static void VerifyPinnedFile(string path, string expected, long maximum)
        {
            string openedPath = ResolvePinnedPath(path);
            var info = new FileInfo(openedPath);
            if (!info.Exists || info.Length < 1 || info.Length > maximum || (info.Attributes & FileAttributes.ReparsePoint) != 0) throw new NotSupportedException("Bounded regular source input required");
            using var stream = File.OpenRead(openedPath);
            if (stream.Length != info.Length || stream.Length > maximum) throw new InvalidOperationException("Source input size changed: " + path);
            using var sha = SHA256.Create();
            string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            if (stream.Length != info.Length || actual != expected) throw new InvalidOperationException("Source input hash changed: " + path);
        }
        internal static byte[] PinnedFile(string path, string expected, long maximum)
        {
            string openedPath = ResolvePinnedPath(path);
            var info = new FileInfo(openedPath);
            if (!info.Exists || info.Length < 1 || info.Length > maximum || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new NotSupportedException("Bounded regular source input required");
            byte[] bytes = File.ReadAllBytes(openedPath);
            if (bytes.LongLength != info.Length || Hash(bytes) != expected) throw new InvalidOperationException("Source input hash changed: " + path);
            return bytes;
        }
} }
