using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Immutable asset-domain projection; gameplay source hashes do not invalidate it.
    internal static class GeneratedReferenceTemplateView
    {
        // Exact diagnostic native bytes; only the declared typed consumer subset may qualify. Full Unity replay remains false.
        internal const string QualifiedFocusedPrefabSha="21e7474adfe2682a638bc6c23708f48b9611c526051871c0d93b4cb18a6ae4aa";
        internal static JObject Read(JObject descriptor, out string binaryDirectory)
        {
            string path = (string)descriptor["path"], sha = (string)descriptor["sha256"];
            JObject domain = Parse(AutonomousWorld.PinnedFile(path, sha, 32 * 1024 * 1024));
            binaryDirectory = Path.GetDirectoryName(path);
            bool focused=(string)domain["kind"]=="AuthoredSkirmishPrefabReferenceData";
            if(focused&&descriptor["source_reference_path"]==null)throw new InvalidDataException("Exact focused prefab consumer projection/provenance required; full native replay remains unqualified");
            if (descriptor["source_reference_path"] == null) return domain;

            string source = (string)descriptor["source_reference_path"];
            string sourceSha = (string)descriptor["source_reference_sha256"];
            if(sourceSha==QualifiedFocusedPrefabSha&&!focused)throw new InvalidDataException("Focused diagnostic namespace cannot be relabelled as another template schema");
            var info = new FileInfo(source);
            if (!Path.IsPathRooted(source) || !info.Exists || info.Length < 1 || info.Length > 128 * 1024 * 1024 ||
                (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Bounded regular original asset reference required");
            using (var stream = File.OpenRead(source))
            using (var hasher = SHA256.Create())
                if (BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != sourceSha)
                    throw new InvalidDataException("Original asset reference changed beneath projection");

            JObject manifest = Parse(AutonomousWorld.PinnedFile((string)descriptor["projection_manifest_path"],
                (string)descriptor["projection_manifest_sha256"], 1024 * 1024));
            string[] selected = focused?new[]{"schema_version","kind","authority","source_scenes","unity_version","template_namespace_id","requested_prefab_keys","requested_prefab_keys_sha256","opening_prefabs","entities","unsupported_values","replay_ready","fresh_world_allocator_exported","full_mission_initialized","native_visual_readiness","geometry_exported","consumer_rule"}:new[]{"schema_version","kind","authority","opening_prefabs","entities"};
            if(focused){if(sourceSha!=QualifiedFocusedPrefabSha&&sourceSha!="83aec3d79c2ac069527c438be1056df9639e40f325b4a3a3c479e090996dd70f"&&sourceSha!="15f69a537018de732331af82f4591a30dbc674a9689c40481e1b3f7324732bcc"&&sourceSha!="fa8933a86f1ada977e9f10b53a0af770c79b43e8c73da49da385a192985f8969")throw new InvalidDataException("Unqualified focused native source identity");var original=Parse(File.ReadAllBytes(source));foreach(string name in selected)if(!JToken.DeepEquals(domain[name],original[name]))throw new InvalidDataException("Focused projection changed native field/namespace: "+name);if((bool?)domain["replay_ready"]!=false||(bool?)domain["full_mission_initialized"]!=false||(bool?)domain["geometry_exported"]!=false)throw new InvalidDataException("Focused diagnostic native scope must remain explicit");}
            if ((int?)manifest["schema_version"] != 1 ||
                (string)manifest["kind"] != "PinnedExactTemplateDomainProjection" ||
                (string)manifest["source_path"] != source || (string)manifest["source_sha256"] != sourceSha ||
                (long?)manifest["source_bytes"] != info.Length ||
                (string)manifest["slice_path"] != path || (string)manifest["slice_sha256"] != sha ||
                (long?)manifest["slice_bytes"] != new FileInfo(path).Length ||
                !((JArray)manifest["selected_fields"]).Values<string>().SequenceEqual(selected) ||
                (int?)manifest["root_count"] != ((JObject)domain["opening_prefabs"]).Count ||
                (int?)manifest["entity_count"] != ((JArray)domain["entities"]).Count ||
                (string)domain["source_reference_sha256"] != sourceSha ||
                (string)domain["derived_read_domain"] != "ExactTypedPrefabClosureOnly")
                throw new InvalidDataException("Pinned asset-domain projection identity mismatch");
            binaryDirectory = Path.GetDirectoryName(source);
            return domain;
        }

        // Separate S026 diagnostic: the native capture retains its failed exporter status.
        internal static JObject ReadS026Diagnostic(JObject descriptor, string captureRoot, out string binaryDirectory)
        {
            const string sha = "3aa03b05a979e5c78bd51c01dc2b17f9bf8fff41d79828035a4d3eae3a580519";
            if (descriptor == null || descriptor.Count != 2 || descriptor.Properties().Any(p => (p.Name != "path" && p.Name != "sha256") || p.Value.Type != JTokenType.String) || (string)descriptor["sha256"] != sha || !Path.IsPathRooted(captureRoot)) throw new InvalidDataException("Exact S026 diagnostic prefab descriptor required");
            string root = Path.GetFullPath(captureRoot).TrimEnd(Path.DirectorySeparatorChar), path=(string)descriptor["path"];
            var domain=Parse(AutonomousWorld.PinnedFile(path,sha,32*1024*1024));
            if ((string)domain["kind"] != "AuthoredSkirmishPrefabReferenceData" || (string)domain["authority"] != "ActualSourceAssetsAndNativeBakers_NotMissionState" || (bool?)domain["replay_ready"] != false || (bool?)domain["full_mission_initialized"] != false || (bool?)domain["native_visual_readiness"] != false || (bool?)domain["geometry_exported"] != false || (string)domain["requested_prefab_keys_sha256"] != "2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7") throw new InvalidDataException("S026 diagnostic prefab scope changed");
            var pins=domain["source_pins"] as JObject;if(pins==null||pins.Count!=7755)throw new InvalidDataException("Complete prefab source pins required");
            VerifyS026RelativePins(pins,root);
            binaryDirectory=Path.GetDirectoryName(path);return domain;
        }

        // Native JSON is immutable; project-local paths are resolved against an explicit relocated capture root.
        internal static void VerifyS026RelativePins(JObject pins,string root){
            if(!Path.IsPathRooted(root)||!Directory.Exists(root))throw new InvalidDataException("Explicit captured project root required");root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);for(var parent=new DirectoryInfo(root);parent!=null;parent=parent.Parent)if((parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Captured project root symlink forbidden");
            int external=0;string producerRoot=null;foreach(var pin in pins.Properties()){if(pin.Value.Type!=JTokenType.String)throw new InvalidDataException("Source pin digest required");string rel=pin.Name;if(Path.IsPathRooted(rel)){string[] domains={"/Assets/","/Packages/","/Library/PackageCache/"};string marker=domains.FirstOrDefault(x=>rel.Contains(x));if(marker==null){if(!rel.EndsWith("/s026-unit-keys.json",StringComparison.Ordinal)||(string)pin.Value!="2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7"||++external!=1)throw new InvalidDataException("Unexpected external source pin");continue;}int at=rel.IndexOf(marker,StringComparison.Ordinal);string captured=rel.Substring(0,at);if(producerRoot==null)producerRoot=captured;else if(producerRoot!=captured)throw new InvalidDataException("Inconsistent captured project origin");rel=rel.Substring(at+1);}
                if(!(rel.StartsWith("Assets/",StringComparison.Ordinal)||rel.StartsWith("Packages/",StringComparison.Ordinal)||rel.StartsWith("Library/PackageCache/",StringComparison.Ordinal)||rel.StartsWith("ProjectSettings/",StringComparison.Ordinal))||rel.Contains("\\")||rel.Split('/').Any(x=>x==""||x=="."||x==".."))throw new InvalidDataException("Invalid relative source pin");string file=Path.GetFullPath(Path.Combine(root,rel));if(!file.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Source pin escaped project root");var info=new FileInfo(file);if(!info.Exists||(info.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Source pin missing");for(var parent=info.Directory;parent!=null&&parent.FullName.Length>=root.Length;parent=parent.Parent)if((parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Source pin path symlink forbidden");using(var stream=File.OpenRead(file))using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=(string)pin.Value)throw new InvalidDataException("Captured source pin changed: "+rel);
            }if(external!=1||producerRoot==null)throw new InvalidDataException("Captured project origin or key identity missing");
        }
        private static JObject Parse(byte[] bytes) => JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
    }
}
