// DRAFT ONLY: outside Assets. Import only with reviewed harness seams and native checks.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Editor
{
    public sealed class SkirmishProductionCanaryRequest
    {
        public readonly JObject Job, Claim;
        public readonly string ProjectRoot, JobPath, ClaimPath, ArtifactDirectory, JobHash;
        public string CatalogId { get { return (string)Job["catalog_id"]; } }
        public string Locale { get { return (string)Job["locale"] == "EN" ? "en" : "fa-IR"; } }
        public bool Manual { get { return (string)Job["control_mode"] == "manual"; } }
        public int Seed { get { return (int)Job["seed"]; } }
        public bool IsLegacyPreset => (int)Job["schema_version"] == 2;

        private SkirmishProductionCanaryRequest(string root, string jobPath, string claimPath, JObject job, JObject claim)
        {
            ProjectRoot = root; JobPath = jobPath; ClaimPath = claimPath;
            Job = job; Claim = claim;
            ArtifactDirectory = Path.GetFullPath((string)claim["artifact_directory"]);
            JobHash = Hash(Encoding.UTF8.GetBytes(Canonical(job)));
        }

        public static SkirmishProductionCanaryRequest Read(string projectRoot, string jobPath, string claimPath, bool verifySourceBytes = true, SkirmishProductionMatrixAuthority matrix = null)
        {
            var root = Path.GetFullPath(projectRoot);
            jobPath = Path.GetFullPath(jobPath); claimPath = Path.GetFullPath(claimPath);
            var job = ReadObject(jobPath); var claim = ReadObject(claimPath);
            bool legacy = job["schema_version"]?.Type == JTokenType.Integer && (int)job["schema_version"] == 2;
            bool counterpart = job["schema_version"]?.Type == JTokenType.Integer && (int)job["schema_version"] == 3;
            Exact(job, "schema_version job_id catalog_id " + (legacy ? "legacy_preset_index legacy_preset_resource legacy_preset_sha256" : "definition_id definition_version") + " candidate_sha256 build_sha256 setup_sha256 content_sha256 source_manifest packet_path packet_sha256 seed locale size difficulty control_mode required_stages timeout_seconds" + (counterpart ? " counterpart_authority" : ""));
            Exact(claim, "attempt_id token worker started_utc job_sha256 artifact_directory");
            Integer(job, "schema_version", 1, 3); if (!legacy) Integer(job, "definition_version", 1, int.MaxValue);
            Integer(job, "seed", 1, int.MaxValue); Integer(job, "timeout_seconds", 1, 14400);
            Require(Regex.IsMatch(Text(job, "job_id"), "^[A-Za-z0-9][A-Za-z0-9_.-]{0,79}$"), "Unsafe job ID.");
            var catalog = Text(job, "catalog_id");
            Require(matrix != null || catalog == "S004" || catalog == "S005", "Only S004/S005 canary dispatch exists without an explicit matrix plan.");
            if (legacy) { Require(matrix != null, "Legacy jobs require an exact complete sampling plan"); SkirmishLegacyProductionIdentity.ValidateJob(job); }
            else { Require(counterpart ? matrix != null && SkirmishLegacyProductionIdentity.IsProtected(catalog) : !SkirmishLegacyProductionIdentity.IsProtected(catalog), "Protected expanded counterpart requires exact schema3 measured authority and complete sampling plan"); Require(Text(job, "definition_id") == "skirmish." + catalog.ToLowerInvariant(), "Definition mismatch."); }
            Require(Text(job, "size") == "Standard" && Text(job, "difficulty") == "Regular", "Only Regular/Standard canary dispatch exists.");
            Require(new[] { "EN", "FA" }.Contains(Text(job, "locale")), "Locale must be EN/FA.");
            Require(new[] { "manual", "aria" }.Contains(Text(job, "control_mode")), "Unknown control mode.");
            int[] seeds = catalog == "S004" ? new[] { 104733, 130367, 155925 } : new[] { 104734, 130368, 155926 };
            if(matrix==null) Require(seeds.Contains((int)job["seed"]), "Seed outside bounded baseline canary.");
            else matrix.ValidateJob(job);
            foreach (var key in new[] { "candidate_sha256", "build_sha256", "setup_sha256", "content_sha256", "packet_sha256" })
                Require(IsHash(Text(job, key)), "Malformed " + key);
            var stages = job["required_stages"] as JArray;
            string[] expected = { "compile", "live_api", "source_fit", "normal_input", "terminal", "return" };
            Require(stages != null && stages.Count == 6 && stages.All(t => t.Type == JTokenType.String) &&
                stages.Select(t => (string)t).OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(s => s, StringComparer.Ordinal)), "Required stages mismatch.");
            var manifest = job["source_manifest"] as JArray;
            Require(manifest != null && manifest.Count > 0, "Source manifest absent.");
            var paths = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            var checkedDirectories = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            using var sourceDigest = verifySourceBytes ? new SkirmishCandidateSourceHash.FileDigestSession() : null;
            foreach (var token in manifest)
            {
                var entry = token as JObject; Require(entry != null, "Malformed source manifest row.");
                Exact(entry, "path sha256"); var relative = Text(entry, "path");
                Require(paths.Add(relative) && IsHash(Text(entry, "sha256")), "Duplicate source path/hash invalid.");
                var resolved = Relative(root, relative, checkedDirectories);
                if (verifySourceBytes)
                    Require(sourceDigest.ComputeFile(resolved) == (string)entry["sha256"], "Source file missing/changed: " + relative);
            }
            ValidateSourceManifestTopology(root, manifest);
            var packet = Text(job, "packet_path");
            Require(matrix != null || packet == "Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md", "Canary must use governing DB_BA packet without an explicit matrix plan.");
            Require(FileHash(Relative(root, packet)) == Text(job, "packet_sha256"), "Packet changed.");
            Require(Regex.IsMatch(Text(claim, "attempt_id"), "^attempt-[a-f0-9]{32}$") && Regex.IsMatch(Text(claim, "token"), "^[a-f0-9]{32}$"), "Malformed claim identity.");
            Require(Regex.IsMatch(Text(claim, "worker"), "^[A-Za-z0-9][A-Za-z0-9_.-]{0,79}$"), "Malformed worker.");
            DateTimeOffset started; Require(DateTimeOffset.TryParse(Text(claim, "started_utc"), out started), "Claim timestamp absent.");
            var result = new SkirmishProductionCanaryRequest(root, jobPath, claimPath, job, claim);
            Require(IsHash(Text(claim, "job_sha256")) && result.JobHash == (string)claim["job_sha256"], "Claim/job fingerprint mismatch.");
            var artifacts = result.ArtifactDirectory;
            var queueJob = Path.GetDirectoryName(artifacts);
            Require(Path.GetFileName(artifacts) == Text(claim, "attempt_id") && Path.GetFileName(queueJob) == Text(job, "job_id"), "Artifact path does not match job/attempt.");
            Require(Same(jobPath, Path.Combine(queueJob, "job.json")) && Same(claimPath, Path.Combine(queueJob, "claim.json")), "Read canonical live queue job/claim only.");
            Require(!Within(artifacts, Path.Combine(root, "Assets")), "Artifacts inside imported Assets.");
            Require(Directory.Exists(artifacts) && !File.Exists(Path.Combine(artifacts, "result.json")) && !File.Exists(Path.Combine(artifacts, "adapter-started.json")), "Attempt absent/already finished/started.");
            Require(Canonical(ReadObject(Path.Combine(artifacts, "claim.json"))) == Canonical(claim), "Immutable attempt claim differs from live claim.");
            if(counterpart)SkirmishProtectedCounterpartAuthority.Validate(job,root);
            return result;
        }

        public void VerifyNativePins(Func<string> actualCandidateHash)
        {
            Require(actualCandidateHash() == Text(Job, "candidate_sha256"), "Actual complete native candidate differs.");
            var folder = Path.Combine(ProjectRoot, "Library", "ScriptAssemblies");
            using var buildDigest = new SkirmishCandidateSourceHash.FileDigestSession();
            var rows = new JArray(Directory.GetFiles(folder, "*.dll").OrderBy(p => p, StringComparer.Ordinal).Select(path =>
                new JObject { ["path"] = path.Substring(ProjectRoot.Length + 1).Replace('\\', '/'), ["sha256"] = buildDigest.ComputeFile(path) }));
            Require(rows.Count > 0 && Hash(Encoding.UTF8.GetBytes(Canonical(rows))) == Text(Job, "build_sha256"), "Actual compiled DLL manifest differs; compile/load canary first.");
        }

        public static JObject CreateNativeSetupIdentity(string catalog, string definition, int version,
            string difficulty, string size, int seed, string actualLocale, uint nativeSetupHash, string nativeContentIdentity)
        {
            Require(actualLocale == "en" || actualLocale == "fa-IR", "Unknown actual native locale.");
            var value = new JObject { ["schema_version"] = 1, ["identity_kind"] = "SkirmishNativeSetupIdentity",
                ["catalog_id"] = catalog, ["definition_id"] = definition, ["definition_version"] = version,
                ["difficulty"] = difficulty, ["size"] = size, ["seed"] = seed,
                ["locale"] = actualLocale == "en" ? "EN" : "FA", ["native_setup_hash_uint32"] = nativeSetupHash,
                ["native_content_identity"] = nativeContentIdentity };
            ValidateNativeSetupIdentity(value); return value;
        }

        public static void ValidateNativeSetupIdentity(JObject value)
        {
            Exact(value, "schema_version identity_kind catalog_id definition_id definition_version difficulty size seed locale native_setup_hash_uint32 native_content_identity");
            Integer(value, "schema_version", 1, 1); Integer(value, "definition_version", 1, int.MaxValue); Integer(value, "seed", 1, int.MaxValue);
            Require(Text(value, "identity_kind") == "SkirmishNativeSetupIdentity", "Native setup identity kind differs.");
            var catalog = Text(value, "catalog_id");
            Require(Regex.IsMatch(catalog, "^S(?:00[1-9]|0[1-9][0-9]|1[01][0-9]|120)$") &&
                Text(value, "definition_id") == "skirmish." + catalog.ToLowerInvariant(), "Native catalog/definition differs.");
            Require(new[] { "EN", "FA" }.Contains(Text(value, "locale")) &&
                new[] { "Recruit", "Regular", "Veteran", "Commander" }.Contains(Text(value, "difficulty")) &&
                new[] { "Standard", "War", "LargeWar" }.Contains(Text(value, "size")), "Unknown native configuration.");
            Require(value["native_setup_hash_uint32"].Type == JTokenType.Integer &&
                (long)value["native_setup_hash_uint32"] >= 0 && (long)value["native_setup_hash_uint32"] <= uint.MaxValue,
                "Actual native uint32 SetupHash required.");
            Text(value, "native_content_identity");
        }

        public static JObject NativeContentIdentity(JObject setup)
        {
            if ((string)setup?["identity_kind"] == "SkirmishLegacyPresetNativeIdentity") return SkirmishLegacyProductionIdentity.ContentIdentity(setup);
            ValidateNativeSetupIdentity(setup);
            return new JObject { ["schema_version"] = 1, ["identity_kind"] = "SkirmishNativeContentIdentity",
                ["catalog_id"] = setup["catalog_id"].DeepClone(), ["definition_id"] = setup["definition_id"].DeepClone(),
                ["definition_version"] = setup["definition_version"].DeepClone(),
                ["native_content_identity"] = setup["native_content_identity"].DeepClone() };
        }

        public void VerifyNativeSetupIdentity(JObject actual)
        {
            if (IsLegacyPreset) { SkirmishLegacyProductionIdentity.VerifyJobIdentity(Job, actual); return; }
            ValidateNativeSetupIdentity(actual);
            foreach (var key in new[] { "catalog_id", "definition_id", "definition_version", "difficulty", "size", "seed", "locale" })
                Require(JToken.DeepEquals(Job[key], actual[key]), "Actual native configuration differs: " + key);
            Require(Hash(Encoding.UTF8.GetBytes(Canonical(actual))) == Text(Job, "setup_sha256"), "Actual native setup identity SHA256 differs.");
            Require(Hash(Encoding.UTF8.GetBytes(Canonical(NativeContentIdentity(actual)))) == Text(Job, "content_sha256"), "Actual native content identity SHA256 differs.");
        }

        public void ReserveAdapterStart()
        {
            // Short metadata transaction only. Full source/DLL checks happened
            // before this method; never hold this lock during hashing or Play.
            string queue = Path.GetDirectoryName(ArtifactDirectory);
            string temporary = Path.Combine(queue, ".metadata-lock-candidate-" + Guid.NewGuid().ToString("N"));
            string guard = Path.Combine(queue, ".metadata-lock");
            Directory.CreateDirectory(temporary);
            try { Directory.Move(temporary, guard); } // atomic exclusive destination
            catch
            {
                // This invocation owns only the uniquely named empty candidate.
                // Preserve an existing shared guard, regardless of its age.
                Directory.Delete(temporary, false);
                throw;
            }
            try
            {
                // Recheck current canonical ownership after acquiring the guard.
                // Read() cannot be used here: it also hashes source file bytes.
                var liveJob = ReadObject(JobPath); var liveClaim = ReadObject(ClaimPath);
                Require(Hash(Encoding.UTF8.GetBytes(Canonical(liveJob))) == JobHash,
                    "Canonical job changed before adapter reservation.");
                Require(Canonical(liveClaim) == Canonical(Claim) &&
                    Canonical(ReadObject(Path.Combine(ArtifactDirectory, "claim.json"))) == Canonical(Claim),
                    "Canonical claim replaced before adapter reservation.");
                Require(!File.Exists(Path.Combine(ArtifactDirectory, "result.json")), "Attempt finished before adapter reservation.");
                var record = new JObject { ["job_sha256"] = JobHash, ["attempt_id"] = (string)Claim["attempt_id"],
                    ["worker"] = (string)Claim["worker"], ["native_qualification"] = "pending" };
                using (var stream = new FileStream(Path.Combine(ArtifactDirectory, "adapter-started.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(record.ToString(Formatting.Indented));
            }
            finally { Directory.Delete(guard, false); }
            // A crash/partial write preserves the start artifact as incomplete.
        }

        public static JObject ReadObject(string path)
        {
            using (var text = File.OpenText(path))
            using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
            {
                var value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                Require(!reader.Read(), "Trailing JSON content."); return value;
            }
        }
        public static string Canonical(JToken value)
        {
            // Preserve custom JsonConvert behavior and the old child reparse
            // normalization outside the ordinary immutable JSON job fast path.
            if (JsonConvert.DefaultSettings != null) return CanonicalLegacy(value);
            using (var text = new StringWriter(System.Globalization.CultureInfo.InvariantCulture))
            using (var writer = new JsonTextWriter(text) { Formatting = Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii, AutoCompleteOnClose = false })
            {
                if (TryWriteCanonical(writer,value,0))
                {
                    writer.Flush();
                    return text.ToString();
                }
            }
            return CanonicalLegacy(value);
        }
        private static bool TryWriteCanonical(JsonWriter writer,JToken value,int depth)
        {
            // This limits optimization, not accepted JSON: deeper input uses the
            // unchanged oracle, including its existing parser-depth refusals.
            if (depth>=32) return false;
            if (value==null) { writer.WriteNull();return true; }
            if (value.GetType()==typeof(JObject))
            {
                writer.WriteStartObject();
                foreach (var property in ((JObject)value).Properties().OrderBy(p=>p.Name,StringComparer.Ordinal))
                {
                    if (!CanonicalStringIsStable(property.Name)) return false;
                    writer.WritePropertyName(property.Name);
                    if (!TryWriteCanonical(writer,property.Value,depth+1))return false;
                }
                writer.WriteEndObject();return true;
            }
            if (value.GetType()==typeof(JArray))
            {
                writer.WriteStartArray();
                foreach (var child in (JArray)value)
                    if (!TryWriteCanonical(writer,child,depth+1))return false;
                writer.WriteEndArray();return true;
            }
            if (value.GetType()!=typeof(JValue))return false;
            if (value.Type==JTokenType.String && !CanonicalStringIsStable((string)value))return false;
            if (value.Type!=JTokenType.String && value.Type!=JTokenType.Integer &&
                value.Type!=JTokenType.Boolean && value.Type!=JTokenType.Null)return false;
            value.WriteTo(writer);return true;
        }
        private static bool CanonicalStringIsStable(string value)
        {
            if (string.IsNullOrEmpty(value))return true;
            char first=value[0];
            if (char.IsDigit(first)||char.IsWhiteSpace(first)||first=='/'||first=='\\'||first=='+'||first=='-')
            {
                // Source hashes are common digit-leading strings, never dates.
                if (value.Length!=64 || value.Any(c=>!((c>='0'&&c<='9')||(c>='a'&&c<='f'))))return false;
            }
            for(int i=0;i<value.Length;i++)
                if(char.IsSurrogate(value[i]))
                {
                    if(!char.IsHighSurrogate(value[i])||i+1>=value.Length||!char.IsLowSurrogate(value[i+1]))return false;
                    i++;
                }
            return true;
        }


        private static string CanonicalLegacy(JToken value)
        {
            var obj = value as JObject;
            if (obj != null) value = new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, JToken.Parse(CanonicalLegacy(p.Value)))));
            else if (value is JArray) value = new JArray(((JArray)value).Select(t => JToken.Parse(CanonicalLegacy(t))));
            return JsonConvert.SerializeObject(value, Formatting.None, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });
        }
        public static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        public static string FileHash(string path) { using var digest = new SkirmishCandidateSourceHash.FileDigestSession(); return digest.ComputeFile(path); }
        private static string Text(JObject obj, string key) { Require(obj[key] != null && obj[key].Type == JTokenType.String && !string.IsNullOrEmpty((string)obj[key]), "Required text: " + key); return (string)obj[key]; }
        private static void Integer(JObject obj, string key, int min, int max) { Require(obj[key] != null && obj[key].Type == JTokenType.Integer && (long)obj[key] >= min && (long)obj[key] <= max, "Required integer/range: " + key); }
        private static void Exact(JObject obj, string names) { Require(obj.Properties().Select(p => p.Name).OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(names.Split(' ').OrderBy(s => s, StringComparer.Ordinal)), "Missing/unknown fields."); }
        private static bool IsHash(string value) { return Regex.IsMatch(value, "^[a-f0-9]{64}$"); }
        private static bool Same(string a, string b) { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
        private static bool Within(string path, string root) { return Same(path, root) || Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase); }
        // Recheck the same complete manifest after the potentially long byte
        // verification pass. The set is fresh and never accepted from a caller.
        private static void ValidateSourceManifestTopology(string root, JArray manifest)
        {
            var checkedDirectories = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var token in manifest)
            {
                var entry = token as JObject;
                Require(entry != null, "Malformed source manifest row.");
                Relative(root, Text(entry, "path"), checkedDirectories);
            }
        }

        private static string Relative(string root, string relative, System.Collections.Generic.HashSet<string> checkedDirectories = null, Action<string> observedAttributeRead = null)
        {
            Require(!Path.IsPathRooted(relative) && !relative.Contains(":") && !relative.Contains("\\") && !relative.Split('/').Contains(".."), "Unsafe relative source path.");
            var path = Path.GetFullPath(Path.Combine(root, relative)); Require(Within(path, root), "Source escapes project.");
            var current = new FileInfo(path) as FileSystemInfo;
            while (current != null && !Same(current.FullName, root))
            {
                // Scope is one synchronous Read only: never reuse across jobs/dispatches.
                // The leaf file remains fresh even when an ancestor was checked already.
                if (current is DirectoryInfo && checkedDirectories != null &&
                    checkedDirectories.Contains(current.FullName)) break;
                var attributes = current.Attributes;
                observedAttributeRead?.Invoke(current.FullName);
                Require((attributes & FileAttributes.ReparsePoint) == 0, "Reparse/symlink source rejected.");
                if (current is DirectoryInfo && checkedDirectories != null)
                    checkedDirectories.Add(current.FullName);
                current = current is FileInfo ? ((FileInfo)current).Directory : ((DirectoryInfo)current).Parent;
            }
            return path;
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new InvalidDataException(reason); }
    }
}
