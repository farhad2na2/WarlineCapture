using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Immutable asset-domain projection; gameplay source hashes do not invalidate it.
    internal static class GeneratedReferenceTemplateView
    {
        // Exact diagnostic native bytes; only the declared typed consumer subset may qualify. Full Unity replay remains false.
        internal const string QualifiedFocusedPrefabSha="21e7474adfe2682a638bc6c23708f48b9611c526051871c0d93b4cb18a6ae4aa";
        internal const string QualifiedS002ScopedPrefabSha="ed7f792922db66d8d59fb5919a860fefea803b123ef893e226dc18fce3fb24ca";
        private const string QualifiedS002ScopedPrefabReceiptSha="2d32f0d1a46d18f4fb2ef6d5a8af113c56f937835703a383fcf86a0a7eb41d49";
        private const string CurrentS002KeysSha="f2d08edd3a4e495448d3f4df2ad6608d0180e6428be3d32f4844744dcbf23f09";
        private static readonly string[] CurrentS002Keys={"Unit_Chr_Soldier_Male_02_Alt_04","Unit_Chr_Soldier_Male_01","Unit_Chr_Soldier_Female_01","Unit_Chr_Soldier_Female_02_Alt_02","Unit_Chr_Ghillie_Male_01","Unit_Veh_Light_Armored_Car","Unit_Veh_APC_Fast","Unit_Veh_APC_Slow","Unit_Veh_Tank_USA","Unit_Veh_Radar_Tank","Unit_Veh_Truck_Tray","Unit_Veh_Truck_Tanker"};
        internal static JObject Read(JObject descriptor, out string binaryDirectory)
        {
            return Read(descriptor,out binaryDirectory,(string)null);
        }

        internal static JObject Read(JObject descriptor,out string binaryDirectory,string hostSourceRepositoryRoot)
        {
            return ReadCore(descriptor,out binaryDirectory,hostSourceRepositoryRoot,null);
        }

        internal static JObject Read(JObject descriptor,out string binaryDirectory,string hostSourceRepositoryRoot,JObject initialization)
        {
            return ReadCore(descriptor,out binaryDirectory,hostSourceRepositoryRoot,initialization);
        }

        private static JObject ReadCore(JObject descriptor,out string binaryDirectory,string hostSourceRepositoryRoot,JObject initialization)
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
            bool currentS002=focused&&descriptor["current_capture_proof"]!=null;
            bool s074Capture=focused&&descriptor["s074_capture_proof"]!=null;
            bool sourceOnlyCapture=focused&&descriptor["source_only_capture_proof"]!=null;
            if(focused&&new[]{currentS002,s074Capture,sourceOnlyCapture}.Count(x=>x)>1)throw new InvalidDataException("Focused template provenance modes must be mutually exclusive");
            string[] selected = focused?new[]{"schema_version","kind","authority","source_scenes","unity_version","template_namespace_id","requested_prefab_keys","requested_prefab_keys_sha256","opening_prefabs","entities","unsupported_values","replay_ready","fresh_world_allocator_exported","full_mission_initialized","native_visual_readiness","geometry_exported","consumer_rule"}:new[]{"schema_version","kind","authority","opening_prefabs","entities"};
            JObject originalFocused=null;
            if(focused){originalFocused=Parse(File.ReadAllBytes(source));if(!currentS002&&!s074Capture&&!sourceOnlyCapture&&sourceSha!=QualifiedFocusedPrefabSha&&sourceSha!="83aec3d79c2ac069527c438be1056df9639e40f325b4a3a3c479e090996dd70f"&&sourceSha!="15f69a537018de732331af82f4591a30dbc674a9689c40481e1b3f7324732bcc"&&sourceSha!="fa8933a86f1ada977e9f10b53a0af770c79b43e8c73da49da385a192985f8969"&&sourceSha!="31b939d930cf5d9e6d8eac1b3325e7272756cedac916fb8d36a17734954a011e"&&sourceSha!=QualifiedS002ScopedPrefabSha)throw new InvalidDataException("Unqualified focused native source identity");foreach(string name in selected)if(!JToken.DeepEquals(domain[name],originalFocused[name]))throw new InvalidDataException("Focused projection changed native field/namespace: "+name);if((bool?)domain["replay_ready"]!=false||(bool?)domain["full_mission_initialized"]!=false||(bool?)domain["geometry_exported"]!=false)throw new InvalidDataException("Focused diagnostic native scope must remain explicit");if(currentS002)ValidateCurrentS002Proof(descriptor,manifest,domain,originalFocused,source,sourceSha,hostSourceRepositoryRoot);if(s074Capture)ValidateS074CaptureProof(descriptor,manifest,domain,originalFocused,source,sourceSha,hostSourceRepositoryRoot,initialization);if(sourceOnlyCapture)ValidateGenericSourceOnlyCaptureProof(descriptor,manifest,domain,originalFocused,source,sourceSha,hostSourceRepositoryRoot,initialization);}
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
            if(sourceSha==QualifiedS002ScopedPrefabSha)
            {
                string receiptPath=(string)descriptor["capture_receipt_path"];
                string receiptSha=(string)descriptor["capture_receipt_sha256"];
                if(receiptSha!=QualifiedS002ScopedPrefabReceiptSha||
                    (string)descriptor["capture_validation_status"]!="Passed"||
                    (string)descriptor["consumer_qualification_scope"]==null||
                    (bool?)descriptor["source_fit_ready"]!=false||
                    (bool?)descriptor["native_visual_readiness"]!=false||
                    (bool?)descriptor["replay_ready"]!=false||(bool?)descriptor["mission_ready"]!=false||
                    (string)manifest["capture_validation_status"]!="Passed"||
                    (string)manifest["capture_receipt_sha256"]!=QualifiedS002ScopedPrefabReceiptSha||
                    (string)manifest["capture_receipt_path"]!=receiptPath||
                    (int?)manifest["unsupported_presentation_omission_count"]!=14||
                    (bool?)manifest["source_fit_ready"]!=false||
                    (bool?)manifest["native_visual_readiness"]!=false||
                    (bool?)manifest["replay_ready"]!=false||(bool?)manifest["mission_ready"]!=false)
                    throw new InvalidDataException("S002 focused capture qualification receipt or readiness scope mismatch");
                JObject receipt=Parse(AutonomousWorld.PinnedFile(receiptPath,receiptSha,1024*1024));
                if((string)receipt["kind"]!="S002FocusedPrefabCaptureReceipt"||
                    (string)receipt["validation_status"]!="Passed"||(int?)receipt["wrapper_exit_code"]!=0||
                    (bool?)receipt["required_marker_present"]!=true||
                    (string)receipt["capture_sha256"]!=QualifiedS002ScopedPrefabSha||
                    (int?)receipt["source_pin_count"]!=7967||(int?)receipt["prefab_count"]!=12||
                    (int?)receipt["entity_count"]!=352||
                    (int?)receipt["unsupported_presentation_omission_count"]!=14||
                    (bool?)receipt["mission_ready"]!=false||(bool?)receipt["replay_ready"]!=false||
                    (bool?)receipt["full_mission"]!=false||
                    (bool?)receipt["native_visual_readiness"]!=false)
                    throw new InvalidDataException("S002 focused capture receipt content mismatch");
            }
            binaryDirectory = Path.GetDirectoryName(source);
            return domain;
        }

        // Current-source diagnostic packages intentionally pin the outer T5
        // descriptor file. The source host verifies those exact bytes, then
        // passes the contained descriptor through the existing validation path.
        internal static JObject ReadCurrentS002PackageDescriptor(JObject packageDescriptor,out string binaryDirectory,string hostSourceRepositoryRoot)
        {
            if(packageDescriptor==null||packageDescriptor.Properties().Count()!=2||
                packageDescriptor["path"]?.Type!=JTokenType.String||packageDescriptor["sha256"]?.Type!=JTokenType.String)
                throw new InvalidDataException("Exact outer current S002 template descriptor required");
            JObject inner=Parse(AutonomousWorld.PinnedFile((string)packageDescriptor["path"],(string)packageDescriptor["sha256"],1024*1024));
            if(inner["path"]?.Type!=JTokenType.String||inner["sha256"]?.Type!=JTokenType.String||
                inner["source_reference_path"]?.Type!=JTokenType.String||inner["source_reference_sha256"]?.Type!=JTokenType.String||
                (string)inner["current_capture_proof"]?["kind"]!="S002CurrentFocusedTemplateCaptureProof")
                throw new InvalidDataException("Pinned current S002 wrapper must contain the full focused-template descriptor");
            return Read(inner,out binaryDirectory,hostSourceRepositoryRoot);
        }

        internal static void ValidateCurrentS002Proof(JObject descriptor,JObject manifest,JObject domain,
            JObject original,string sourcePath,string sourceSha,string hostSourceRepositoryRoot)
        {
            var proof=descriptor["current_capture_proof"] as JObject;
            string[] fields={"schema_version","kind","capture_path","capture_sha256","export_capture_path",
                "export_capture_sha256","finalization_receipt_path","finalization_receipt_sha256","capture_log_path","capture_log_sha256","key_descriptor_path",
                "key_descriptor_sha256","source_pin_count","unsupported_presentation_omission_count",
                "source_fit_ready","native_visual_readiness","replay_ready","mission_ready"};
            if(proof==null||proof.Properties().Count()!=fields.Length||proof.Properties().Any(p=>!fields.Contains(p.Name))||
                (int?)proof["schema_version"]!=1||(string)proof["kind"]!="S002CurrentFocusedTemplateCaptureProof"||
                (string)proof["capture_path"]!=sourcePath||(string)proof["capture_sha256"]!=sourceSha||
                (int?)proof["source_pin_count"]<1000||(int?)proof["unsupported_presentation_omission_count"]!=14||
                (bool?)proof["source_fit_ready"]!=false||(bool?)proof["native_visual_readiness"]!=false||
                (bool?)proof["replay_ready"]!=false||(bool?)proof["mission_ready"]!=false||
                !JToken.DeepEquals(manifest["current_capture_proof"],proof)||
                (string)manifest["consumer_read_domain"]!="ExactTypedPrefabClosureOnly"||
                (bool?)manifest["source_fit_ready"]!=false||(bool?)manifest["native_visual_readiness"]!=false||
                (bool?)manifest["replay_ready"]!=false||(bool?)manifest["mission_ready"]!=false||
                (int?)manifest["unsupported_presentation_omission_count"]!=14)
                throw new InvalidDataException("Exact current S002 capture proof and bounded readiness scope required");
            ValidateCurrentS002CaptureShape(domain,original);
            string receiptPath=(string)proof["finalization_receipt_path"],receiptSha=(string)proof["finalization_receipt_sha256"];
            string exportPath=(string)proof["export_capture_path"],exportSha=(string)proof["export_capture_sha256"];
            string logPath=(string)proof["capture_log_path"],logSha=(string)proof["capture_log_sha256"];
            string keyPath=(string)proof["key_descriptor_path"],keySha=(string)proof["key_descriptor_sha256"];
            JObject sourcePins=original["source_pins"] as JObject;
            if(String.IsNullOrEmpty(hostSourceRepositoryRoot)||!Path.IsPathRooted(hostSourceRepositoryRoot))
                throw new InvalidDataException("Actual AutonomousWorld.SourceRepositoryRoot is required for current S002 proof");
            string projectRoot=ValidateTrustedCurrentS002Root(hostSourceRepositoryRoot);
            string slicePath=(string)descriptor["path"],manifestPath=(string)descriptor["projection_manifest_path"];
            RequireCurrentS002CapturePath(sourcePath,projectRoot);
            RequireS002ExportLocator(exportPath,exportSha,sourcePath,projectRoot);
            if(!IsUnderRoot(receiptPath,projectRoot)||!IsUnderRoot(logPath,projectRoot)||
                !IsUnderRoot(slicePath,projectRoot)||!IsUnderRoot(manifestPath,projectRoot)||
                HasReparseComponent(receiptPath)||HasReparseComponent(logPath)||
                HasReparseComponent(keyPath)||HasReparseComponent(slicePath)||HasReparseComponent(manifestPath)||
                keySha!=CurrentS002KeysSha)
                throw new InvalidDataException("Current S002 receipt/log must be project-local and exact ordered key hash is required");
            if((string)proof["capture_sha256"]!=exportSha)
                throw new InvalidDataException("Durable capture hash must equal the original exporter hash recorded at finalization");
            VerifyCurrentS002SourcePins(sourcePins,projectRoot,keyPath,keySha,(int)proof["source_pin_count"]);
            JObject receipt=Parse(AutonomousWorld.PinnedFile(receiptPath,receiptSha,2*1024*1024));
            if((string)receipt["kind"]!="S002CurrentFocusedTemplateCaptureFinalization"||
                (string)receipt["validation_status"]!="PassedCurrentPinnedSourceOnly"||
                (string)receipt["capture_path"]!=sourcePath||(string)receipt["capture_sha256"]!=sourceSha||
                (string)receipt["export_capture_path"]!=exportPath||(string)receipt["export_capture_sha256"]!=exportSha||
                (string)receipt["capture_log_path"]!=(string)proof["capture_log_path"]||
                (string)receipt["capture_log_sha256"]!=(string)proof["capture_log_sha256"]||
                (string)receipt["key_descriptor_path"]!=(string)proof["key_descriptor_path"]||
                (string)receipt["key_descriptor_sha256"]!=(string)proof["key_descriptor_sha256"]||
                (int?)receipt["source_pin_count"]!=(int?)proof["source_pin_count"]||
                (int?)receipt["wrapper_exit_code"]!=0||
                (string)receipt["required_method"]!="Game.Editor.SkirmishBakedActorSnapshotExporter.RunS002FocusedPrefabReferenceData"||
                (string)receipt["required_marker"]!="[SkirmishS002FocusedPrefabReferenceData] result=Passed prefabs=12 entities=352 unsupportedUnconsumedPresentation=14 replayReady=False fullMission=0"||
                (bool?)receipt["mission_ready"]!=false||(bool?)receipt["replay_ready"]!=false||
                (bool?)receipt["full_mission"]!=false||(bool?)receipt["native_visual_readiness"]!=false||
                (bool?)receipt["source_fit_ready"]!=false)
                throw new InvalidDataException("Current S002 strict capture finalization receipt mismatch");
            JObject strict=receipt["strict_finalizer"] as JObject;
            if(strict==null||(string)strict["kind"]!="S002FocusedPrefabCaptureReceipt"||
                (string)strict["validation_status"]!="Passed"||(string)strict["capture_sha256"]!=sourceSha||
                (string)strict["editor_log_sha256"]!=(string)proof["capture_log_sha256"]||
                (int?)strict["source_pin_count"]!=(int?)proof["source_pin_count"]||
                (int?)strict["wrapper_exit_code"]!=0||(bool?)strict["required_marker_present"]!=true||
                (int?)strict["prefab_count"]!=12||(int?)strict["entity_count"]!=352||
                (int?)strict["unsupported_presentation_omission_count"]!=14||
                (bool?)strict["mission_ready"]!=false||(bool?)strict["replay_ready"]!=false||
                (bool?)strict["full_mission"]!=false||(bool?)strict["native_visual_readiness"]!=false)
                throw new InvalidDataException("Current S002 strict finalizer facts mismatch");
            string log=Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(logPath,logSha,32*1024*1024));
            if(log.Split(new[]{"\r\n","\n"},StringSplitOptions.None).Count(x=>x== (string)receipt["required_marker"])!=1||
                !log.Contains((string)receipt["required_method"])||!log.Contains(Path.GetDirectoryName(exportPath)))
                throw new InvalidDataException("Current S002 capture log does not bind its exact method, path, and marker");
            byte[] keyBytes=AutonomousWorld.PinnedFile(keyPath,keySha,1024*1024);
            JToken keyToken=JToken.Parse(Encoding.UTF8.GetString(keyBytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if(!JToken.DeepEquals(keyToken,domain["requested_prefab_keys"])||
                (string)domain["requested_prefab_keys_sha256"]!=keySha||
                (string)original["source_pins"]?[keyPath]!=keySha)
                throw new InvalidDataException("Current S002 external key descriptor is not bound to capture source pins");
            JArray keys=keyToken as JArray;
            if(keys==null||keys.Count!=CurrentS002Keys.Length||!keys.Values<string>().SequenceEqual(CurrentS002Keys,StringComparer.Ordinal)||
                keys.Values<string>().Distinct(StringComparer.Ordinal).Count()!=CurrentS002Keys.Length)
                throw new InvalidDataException("Current S002 exact ordered key descriptor required");
            ValidateCurrentS002Omissions((JArray)domain["unsupported_values"],
                new HashSet<string>(((JArray)domain["entities"]).OfType<JObject>().Select(e=>(string)e["id"]),StringComparer.Ordinal));
        }

        internal static void VerifyCurrentS002SourcePins(JObject pins,string projectRoot,string keyPath,string keySha,int expectedCount)
        {
            if(pins==null||pins.Properties().Count()!=expectedCount||expectedCount<1000)
                throw new InvalidDataException("Complete current S002 source pin map required");
            projectRoot=ValidateTrustedCurrentS002Root(projectRoot);
            keyPath=Path.GetFullPath(keyPath);
            if(IsUnderRoot(keyPath,projectRoot)||HasReparseComponent(keyPath))
                throw new InvalidDataException("The one external key descriptor must be outside the current source checkout and symlink-free");
            int external=0;
            foreach(var pin in pins.Properties())
            {
                string rawPinPath=pin.Name,pinSha=(string)pin.Value;
                if(!IsSha256(pinSha))
                    throw new InvalidDataException("Canonical S002 source pin SHA required");
                string pinPath;
                if(Path.IsPathRooted(rawPinPath))
                {
                    pinPath=Path.GetFullPath(rawPinPath);
                    if(!StringComparer.Ordinal.Equals(pinPath,rawPinPath)||HasReparseComponent(pinPath))
                        throw new InvalidDataException("Canonical absolute S002 source pin required");
                    if(StringComparer.Ordinal.Equals(pinPath,keyPath))
                    {
                        external++;
                        if(pinSha!=keySha||keySha!=CurrentS002KeysSha)throw new InvalidDataException("Exact external S002 key path/hash binding required");
                    }
                    else if(!IsUnderRoot(pinPath,projectRoot))
                        throw new InvalidDataException("Unapproved external source pin: "+pinPath);
                }
                else
                {
                    string[] segments=rawPinPath.Split('/');
                    if(String.IsNullOrEmpty(rawPinPath)||rawPinPath.IndexOf('\\')>=0||rawPinPath.IndexOf(':')>=0||
                        segments.Any(s=>String.IsNullOrEmpty(s)||s=="."||s==".."))
                        throw new InvalidDataException("Canonical root-relative S002 source pin required");
                    pinPath=Path.GetFullPath(Path.Combine(projectRoot,rawPinPath.Replace('/',Path.DirectorySeparatorChar)));
                    string canonicalRelative=Path.GetRelativePath(projectRoot,pinPath).Replace('\\','/');
                    if(!StringComparer.Ordinal.Equals(canonicalRelative,rawPinPath)||!IsUnderRoot(pinPath,projectRoot)||HasReparseComponent(pinPath))
                        throw new InvalidDataException("S002 root-relative source pin escaped or aliased the trusted checkout");
                }
                AutonomousWorld.PinnedFile(pinPath,pinSha,64*1024*1024);
            }
            if(external!=1||!pins.ContainsKey(keyPath))
                throw new InvalidDataException("Exactly one external S002 key descriptor pin required");
        }

        internal static void RequireCurrentS002CapturePath(string capturePath,string projectRoot)
        {
            projectRoot=ValidateTrustedCurrentS002Root(projectRoot);
            if(!IsUnderRoot(capturePath,projectRoot)||HasReparseComponent(capturePath))
                throw new InvalidDataException("Current S002 durable capture artifact must be inside the trusted source checkout");
        }

        internal static void RequireHistoricalS002ExportLocator(string exportPath,string projectRoot)
        {
            projectRoot=ValidateTrustedCurrentS002Root(projectRoot);
            if(String.IsNullOrEmpty(exportPath)||!Path.IsPathRooted(exportPath)||
                !StringComparer.Ordinal.Equals(Path.GetFullPath(exportPath),exportPath)||
                IsUnderRoot(exportPath,projectRoot))
                throw new InvalidDataException("Original S002 exporter locator must be canonical, absolute, and outside the trusted checkout");
        }

        internal static void RequireS002ExportLocator(string exportPath,string exportSha,string capturePath,string projectRoot)
        {
            projectRoot=ValidateTrustedCurrentS002Root(projectRoot);
            string freshExport=Path.Combine(projectRoot,"Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/prefab-reference-data.json");
            string freshCapture=Path.Combine(projectRoot,"T Logs/SkirmishProduction/s002-focused-templates-refresh-20261105-T1/durable-prefab-reference-data.json");
            string pilotExport=Path.Combine(projectRoot,"Logs/SkirmishProduction/five-mission-pilot-s002-focused-prefab-20261106-T2/prefab-reference-data.json");
            string pilotCapture=Path.Combine(projectRoot,"T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s002-focused-prefab-reference-r2.json");
            if((StringComparer.Ordinal.Equals(exportPath,freshExport)&&StringComparer.Ordinal.Equals(capturePath,freshCapture)) ||
               (StringComparer.Ordinal.Equals(exportPath,pilotExport)&&StringComparer.Ordinal.Equals(capturePath,pilotCapture)))
            {
                if(HasReparseComponent(exportPath)||HasReparseComponent(capturePath))
                    throw new InvalidDataException("Current S002 capture paths must not contain links");
                AutonomousWorld.PinnedFile(exportPath,exportSha,256*1024*1024);
                return;
            }
            RequireHistoricalS002ExportLocator(exportPath,projectRoot);
        }

        private static void ValidateCurrentS002CaptureShape(JObject domain,JObject original)
        {
            if((string)domain["authority"]!="ActualSourceAssetsAndNativeBakers_NotMissionState"||
                !JToken.DeepEquals(domain["source_scenes"],new JArray("Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"))||
                !JToken.DeepEquals(domain["requested_prefab_keys"],new JArray(CurrentS002Keys))||
                (string)domain["requested_prefab_keys_sha256"]!=CurrentS002KeysSha||
                (bool?)domain["fresh_world_allocator_exported"]!=false||
                (bool?)domain["full_mission_initialized"]!=false||(bool?)domain["geometry_exported"]!=false||
                (bool?)domain["native_visual_readiness"]!=false||(bool?)domain["replay_ready"]!=false)
                throw new InvalidDataException("Current S002 authored scope or false readiness fields changed");
            JObject roots=domain["opening_prefabs"] as JObject;
            JArray entities=domain["entities"] as JArray;
            if(roots==null||entities==null||roots.Properties().Count()!=12||entities.Count!=352||
                !roots.Properties().Select(p=>p.Name).SequenceEqual(CurrentS002Keys,StringComparer.Ordinal))
                throw new InvalidDataException("Current S002 exact ordered root/entity counts required");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(JObject entity in entities)
            {
                string id=(string)entity["id"];
                if(!CanonicalEntityId(id)||!ids.Add(id))throw new InvalidDataException("Current S002 unique canonical entity identifiers required");
            }
            foreach(var root in roots.Properties())
                if(root.Value.Type!=JTokenType.String||!ids.Contains((string)root.Value))
                    throw new InvalidDataException("Current S002 root does not reference a captured entity");
            foreach(string field in new[]{"replay_ready","fresh_world_allocator_exported","full_mission_initialized","native_visual_readiness","geometry_exported"})
                if((bool?)original[field]!=false)throw new InvalidDataException("Current S002 source capture readiness promotion rejected: "+field);
        }

        private static bool CanonicalEntityId(string value)
        {
            string[] parts=(value??"").Split(':');
            return parts.Length==2&&CanonicalUnsigned(parts[0])&&CanonicalUnsigned(parts[1])&&
                ((parts[0]=="0")== (parts[1]=="0"));
        }

        internal static string ValidateTrustedCurrentS002Root(string root)
        {
            if(String.IsNullOrEmpty(root)||!Path.IsPathRooted(root))throw new InvalidDataException("Actual current source repository root required");
            string full=Path.GetFullPath(root);
            if(!Directory.Exists(Path.Combine(full,"Assets"))||!Directory.Exists(Path.Combine(full,"Packages"))||
                !Directory.Exists(Path.Combine(full,"ProjectSettings"))||HasReparseComponent(full))
                throw new InvalidDataException("Trusted current host source root must be a real, symlink-free Unity checkout");
            return full;
        }

        private static bool IsUnderRoot(string path,string root)
        {
            if(!Path.IsPathRooted(path))return false;
            string full=Path.GetFullPath(path),basePath=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            return full.StartsWith(basePath,StringComparison.Ordinal);
        }

        private static bool HasReparseComponent(string path)
        {
            if(!Path.IsPathRooted(path))return true;
            string full=Path.GetFullPath(path),current=Path.GetPathRoot(full);
            if(String.IsNullOrEmpty(current))return true;
            foreach(string part in full.Substring(current.Length).Split(Path.DirectorySeparatorChar))
            {
                if(part.Length==0)continue;
                current=Path.Combine(current,part);
                if(!File.Exists(current)&&!Directory.Exists(current))return true;
                if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)return true;
            }
            return false;
        }

        private static bool IsSha256(string value)
        {
            return value!=null&&value.Length==64&&value.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
        }

        private static void ValidateCurrentS002Omissions(JArray rows,HashSet<string> entityIds)
        {
            if(rows==null||rows.Count!=14)throw new InvalidDataException("Exact 14 presentation-only omissions required");
            var allowed=new HashSet<string>(StringComparer.Ordinal){
                "Game.Components.UnitAttackImpactVfxReference/Prefab",
                "Game.Components.UnitMuzzleFlashVfxReference/Prefab"};
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(JObject row in rows)
            {
                string path=(string)row["path"],type=(string)row["type"];
                string[] parts=(path??"").Split('/');
                if(parts.Length!=3||!allowed.Contains(parts[1]+"/"+parts[2])||
                    (string)row["reason"]!="unity_object_ref_requires_bound_source_identity"||
                    type==null||!type.StartsWith("Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,",StringComparison.Ordinal))
                    throw new InvalidDataException("Unsupported non-presentation S002 template omission");
                string[] entity=parts[0].Split(':');
                if(entity.Length!=2||!CanonicalUnsigned(entity[0])||!CanonicalUnsigned(entity[1])||
                    ((entity[0]=="0")!=(entity[1]=="0"))||!entityIds.Contains(parts[0])||!seen.Add(path))
                    throw new InvalidDataException("Noncanonical or duplicate S002 omission path");
            }
        }

        private static bool CanonicalUnsigned(string value)
        {
            if(String.IsNullOrEmpty(value)||(value.Length>1&&value[0]=='0'))return false;
            return value.All(c=>c>='0'&&c<='9');
        }

        private const string S074FocusedRawCaptureSha="e6c68c4bc85bf959a2de5611f37e199c0b61eb737019369c582f92b75b91c7a9";
        private const string S074FocusedContextSha="d24e4d2208ea5554ea666f97a76f02716343360c926f0ad85a081c12f69cd142";
        private const string S074FocusedQualificationSha="734f825529f446f333b9a0dd4fab195485b228e0fb45d7eb167f9e137c7dbf67";
        private const string S074FocusedGraphSha="edc4081763a3252716679fc4e36dae2b8341aa6e9c8426fa552e143096d510cb";
        private const string S074FocusedBuildingMetadataSha="3126b611ab2a9c42386d9c0d6ad0e879614f6a9b5d39419f05b1c6c839e2c9a3";
        private const string S074FocusedUnitMetadataSha="cb1967036408bab62d47fa73d3b62c3136ec61d3cf779e2fb43a94dd2448dc49";
        private const string S074FocusedUnitKeysSha="2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7";
        private const string S074FocusedCaptureLogSha="6be195f37bb178b07c97a4fe043a37bd6747c68e71fe19d0dfb70e3ce6799eac";
        private const string S074FocusedWrapperLogSha="61629cf94be315882abacdc98faeee508742d5da7a73ba3bca94f45ecd4f0011";

        internal static void VerifyS074SourcePins(JObject pins,string sourceRoot,int expectedCount)
        {
            if(expectedCount!=8087&&expectedCount!=8091)throw new InvalidDataException("Exact S074 prefab/death pin domain count required");
            if(pins==null||pins.Count!=expectedCount||!Path.IsPathRooted(sourceRoot)||!Directory.Exists(sourceRoot))throw new InvalidDataException("Exact S074 captured source pin domain required");
            string root=Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar);
            for(var parent=new DirectoryInfo(root);parent!=null;parent=parent.Parent)if((parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("S074 source repository root symlink forbidden");
            foreach(var pin in pins.Properties())
            {
                string path=pin.Name,full;
                if(pin.Value.Type!=JTokenType.String||!IsSha256((string)pin.Value))throw new InvalidDataException("Canonical in-root S074 captured source pin required: "+path);
                if(Path.IsPathRooted(path)){full=Path.GetFullPath(path);if(full!=path)throw new InvalidDataException("Noncanonical absolute S074 source pin: "+path);}
                else{if(path.Contains("\\")||path.Split('/').Any(part=>part.Length==0||part=="."||part=="..")||!(path.StartsWith("Assets/",StringComparison.Ordinal)||path.StartsWith("Packages/",StringComparison.Ordinal)||path.StartsWith("Library/",StringComparison.Ordinal)||path.StartsWith("ProjectSettings/",StringComparison.Ordinal)||path.StartsWith("T Logs/",StringComparison.Ordinal)))throw new InvalidDataException("Invalid relative S074 source pin: "+path);full=Path.GetFullPath(Path.Combine(root,path.Replace('/',Path.DirectorySeparatorChar)));}
                if(!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("S074 source pin escaped repository root: "+path);
                var info=new FileInfo(full);if(!info.Exists||(info.Attributes&FileAttributes.ReparsePoint)!=0||info.Length>512L*1024*1024)throw new InvalidDataException("Bounded regular S074 source pin required: "+path);
                for(var parent=info.Directory;parent!=null&&parent.FullName.Length>=root.Length;parent=parent.Parent)if((parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("S074 source pin path symlink forbidden: "+path);
                using(var stream=File.OpenRead(full))using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=(string)pin.Value)throw new InvalidDataException("S074 captured source pin changed: "+path);
            }
        }

        private static void RequireSameDescriptor(JToken actual,JToken expected,string name)
        {
            var a=actual as JObject;var b=expected as JObject;
            if(a==null||b==null||a.Count!=2||b.Count!=2||a.Properties().Any(p=>p.Name!="path"&&p.Name!="sha256")||b.Properties().Any(p=>p.Name!="path"&&p.Name!="sha256")||!JToken.DeepEquals(a,b))throw new InvalidDataException("S074 focused capture is not bound to current initialization descriptor: "+name);
        }

        private static void RequireProofPath(JObject descriptor,string expected,string name)
        {
            if(descriptor==null||(string)descriptor["path"]!=expected||!Path.IsPathRooted(expected)||Path.GetFullPath(expected)!=expected||!IsSha256((string)descriptor["sha256"]))throw new InvalidDataException("Exact S074 proof path required: "+name);
        }

        private static string RequireGenericProofFile(JObject descriptor,string sourceRoot,string name,int maximum)
        {
            if(descriptor==null||descriptor.Count!=2||descriptor.Properties().Any(p=>p.Name!="path"&&p.Name!="sha256")||
                descriptor["path"]?.Type!=JTokenType.String||!IsSha256((string)descriptor["sha256"]))
                throw new InvalidDataException("Exact generic focused-capture proof descriptor required: "+name);
            string root=Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar),path=(string)descriptor["path"];
            if(!Path.IsPathRooted(path)||Path.GetFullPath(path)!=path||!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal))
                throw new InvalidDataException("Generic focused-capture proof artifact must be a canonical repository file: "+name);
            AutonomousWorld.PinnedFile(path,(string)descriptor["sha256"],maximum);
            return path;
        }

        private static Dictionary<string,int> GenericUnsupportedVfxFieldCounts(JObject raw)
        {
            const string objectRef="Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]";
            var allowed=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal){
                ["Game.Components.UnitAttackImpactVfxReference"]=new HashSet<string>(new[]{"Prefab"},StringComparer.Ordinal),
                ["Game.Components.UnitMuzzleFlashVfxReference"]=new HashSet<string>(new[]{"Prefab"},StringComparer.Ordinal),
                ["Game.Components.GroundMissileLauncherVfxReferenceComponent"]=new HashSet<string>(new[]{"LauncherBackfirePrefab","RocketTrailPrefab","ImpactExplosionPrefab","ImpactSmokePrefab"},StringComparer.Ordinal),
                ["Game.Components.AirMissileLauncherVfxReferenceComponent"]=new HashSet<string>(new[]{"MissileVisualPrefab","LaunchFlashPrefab","LaunchSmokePrefab","MissileTrailPrefab","AirburstExplosionPrefab","AirTargetImpactPrefab","InterceptExplosionPrefab"},StringComparer.Ordinal)};
            var entities=(raw["entities"] as JArray)?.OfType<JObject>().ToArray();
            var records=raw["unsupported_values"] as JArray;
            if(entities==null||records==null)throw new InvalidDataException("Generic focused capture entities and unsupported rows required");
            var byId=entities.ToDictionary(e=>(string)e["id"],StringComparer.Ordinal);
            var seen=new HashSet<string>(StringComparer.Ordinal);var counts=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(JObject row in records)
            {
                string pathText=(string)row["path"]??"";string[] parts=pathText.Split('/');
                if(parts.Length!=3||!byId.TryGetValue(parts[0],out var entity)||!allowed.TryGetValue(parts[1],out var fields)||
                    !fields.Contains(parts[2])||(string)row["reason"]!="unity_object_ref_requires_bound_source_identity"||
                    (string)row["type"]!=objectRef||!seen.Add(pathText))
                    throw new InvalidDataException("Generic focused capture has unsupported value outside the exact existing importer presentation allowlist");
                ExactInitialValueCodec.EntityId(parts[0]);
                var component=entity["components"]?[parts[1]] as JObject;
                var value=component?["value"] as JObject;
                var field=value?["fields"]?[parts[1]+"::"+parts[2]] as JObject;
                if((string)value?["codec"]!="struct-fields-v1"||(string)field?["codec"]!="unsupported-v1"||
                    (string)field?["type"]!=objectRef||(string)field?["reason"]!=(string)row["reason"])
                    throw new InvalidDataException("Generic focused unsupported row differs from its captured component field");
                string countKey=parts[1]+"/"+parts[2];counts[countKey]=counts.TryGetValue(countKey,out int count)?count+1:1;
            }
            return counts;
        }

        private static Dictionary<string,int> GenericPresentationComponentCounts(JObject raw)
        {
            string[] names={"Game.Components.UnitAttackImpactVfxReference","Game.Components.UnitMuzzleFlashVfxReference","Game.Components.GroundMissileLauncherVfxReferenceComponent","Game.Components.AirMissileLauncherVfxReferenceComponent"};
            var counts=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(JObject entity in (JArray)raw["entities"])foreach(string name in names)if(entity["components"]?[name]!=null)counts[name]=counts.TryGetValue(name,out int count)?count+1:1;
            return counts;
        }

        private static bool ExactCountObject(JToken token,Dictionary<string,int> expected)
        {
            var actual=token as JObject;
            return actual!=null&&actual.Count==expected.Count&&actual.Properties().All(p=>p.Value.Type==JTokenType.Integer&&
                expected.TryGetValue(p.Name,out int count)&&(int)p.Value==count);
        }

        internal static void VerifyGenericFocusedSourcePins(JObject pins,string sourceRoot)
        {
            if(pins==null||pins.Count==0||!Path.IsPathRooted(sourceRoot)||!Directory.Exists(sourceRoot))
                throw new InvalidDataException("Complete generic focused-capture source pin map required");
            string root=Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar);
            for(var parent=new DirectoryInfo(root);parent!=null;parent=parent.Parent)
                if((parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Generic focused-capture source root symlink forbidden");
            foreach(var pin in pins.Properties())
            {
                string value=pin.Name,full;
                if(pin.Value.Type!=JTokenType.String||!IsSha256((string)pin.Value))throw new InvalidDataException("Canonical generic focused source pin required: "+value);
                if(Path.IsPathRooted(value)){full=Path.GetFullPath(value);if(full!=value)throw new InvalidDataException("Noncanonical generic source pin: "+value);}
                else
                {
                    if(value.Contains("\\")||value.Split('/').Any(part=>part.Length==0||part=="."||part=="..")||
                        !(value.StartsWith("Assets/",StringComparison.Ordinal)||value.StartsWith("Packages/",StringComparison.Ordinal)||
                          value.StartsWith("Library/",StringComparison.Ordinal)||value.StartsWith("ProjectSettings/",StringComparison.Ordinal)||
                          value.StartsWith("T Logs/",StringComparison.Ordinal)))throw new InvalidDataException("Generic source pin outside repository asset domains: "+value);
                    full=Path.GetFullPath(Path.Combine(root,value.Replace('/',Path.DirectorySeparatorChar)));
                }
                if(!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Generic source pin escaped repository root: "+value);
                var info=new FileInfo(full);if(!info.Exists||(info.Attributes&FileAttributes.ReparsePoint)!=0||info.Length>512L*1024*1024)
                    throw new InvalidDataException("Bounded regular generic source pin required: "+value);
                for(var parent=info.Directory;parent!=null&&parent.FullName.Length>=root.Length;parent=parent.Parent)
                    if((parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Generic source pin path symlink forbidden: "+value);
                byte[] bytes=AutonomousWorld.PinnedFile(full,(string)pin.Value,512L*1024*1024);
                if(AutonomousWorld.Hash(bytes)!=(string)pin.Value)throw new InvalidDataException("Generic captured source pin changed: "+value);
            }
        }

        private static void ValidateGenericSourceOnlyCaptureProof(JObject descriptor,JObject manifest,JObject domain,JObject raw,
            string source,string sourceSha,string sourceRoot,JObject initialization)
        {
            string repoRoot=Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar);
            if(initialization==null||sourceSha!=(string)descriptor["source_reference_sha256"]||
                Path.GetFullPath(source)!=source||!Path.IsPathRooted(source)||!source.StartsWith(repoRoot+Path.DirectorySeparatorChar,StringComparison.Ordinal))
                throw new InvalidDataException("Generic source-only focused capture requires a pinned initialization and raw source");
            var proofDescriptor=descriptor["source_only_capture_proof"] as JObject;
            string proofPath=RequireGenericProofFile(proofDescriptor,sourceRoot,"capture proof",2*1024*1024);
            JObject proof=Parse(AutonomousWorld.PinnedFile(proofPath,(string)proofDescriptor["sha256"],2*1024*1024));
            bool actualUnitOwner=proof["unit_owner_evidence"]!=null;
            string[] proofFields={"schema_version","kind","status","scope","raw_capture","raw_capture_result","raw_capture_wrapper_exit_code","raw_capture_pass_marker_present","wrapper_log","unit_key_input","source_context","configuration_qualification","source_config_graph","building_definitions","unit_production_metadata","authoring_result","capture_request","target","source_pin_count","source_pins_verified_current","unsupported_vfx_rows_preserved","exact_unconsumed_fields","presentation_components_omitted_by_existing_importer","raw_capture_replay_ready","runtime_import_attempted","runtime_admission","native_visual_readiness","source_fit_certified","mission_ready"};
            if(actualUnitOwner)proofFields=proofFields.Concat(new[]{"unit_owner_evidence"}).ToArray();
            if(proof.Count!=proofFields.Length||proof.Properties().Any(p=>!proofFields.Contains(p.Name))||
                (int?)proof["schema_version"]!=1||(string)proof["kind"]!="FocusedPrefabSourceOnlyCaptureProof"||
                (string)proof["status"]!="ProjectedSourceOnlyFromFailedRawCapture"||
                (string)proof["raw_capture_result"]!="Failed_UnconsumedPresentationUnityObjectRefs"||
                (int?)proof["raw_capture_wrapper_exit_code"]!=0||(bool?)proof["raw_capture_pass_marker_present"]!=false||
                (bool?)proof["source_pins_verified_current"]!=true||(int?)proof["source_pin_count"]!=(raw["source_pins"] as JObject)?.Count||
                (int?)proof["unsupported_vfx_rows_preserved"]!=(raw["unsupported_values"] as JArray)?.Count||
                (bool?)proof["raw_capture_replay_ready"]!=false||(bool?)proof["runtime_import_attempted"]!=false||
                (bool?)proof["runtime_admission"]!=false||(bool?)proof["native_visual_readiness"]!=false||
                (bool?)proof["source_fit_certified"]!=false||(bool?)proof["mission_ready"]!=false)
                throw new InvalidDataException("Generic focused capture proof scope or false gates changed");
            var rawDescriptor=proof["raw_capture"] as JObject;
            if(rawDescriptor==null||rawDescriptor.Count!=2||(string)rawDescriptor["path"]!=source||(string)rawDescriptor["sha256"]!=sourceSha||
                (string)domain["raw_capture_status"]!="Failed_UnconsumedPresentationUnityObjectRefs"||
                (bool?)domain["replay_ready"]!=false||!JToken.DeepEquals(domain["capture_proof"],proofDescriptor))
                throw new InvalidDataException("Generic focused projection changed the retained failed raw capture identity/status");
            RequireGenericProofFile(rawDescriptor,sourceRoot,"raw capture",128*1024*1024);
            var contextDesc=proof["source_context"] as JObject;var qualificationDesc=proof["configuration_qualification"] as JObject;
            var graphDesc=proof["source_config_graph"] as JObject;var buildingDesc=proof["building_definitions"] as JObject;
            var unitDesc=proof["unit_production_metadata"] as JObject;var authorDesc=proof["authoring_result"] as JObject;
            foreach(var pair in new[]{Tuple.Create((JToken)contextDesc,(JToken)initialization["metadata_compiler_context"],"metadata compiler context"),
                Tuple.Create((JToken)qualificationDesc,(JToken)initialization["configuration_qualification"],"configuration qualification"),
                Tuple.Create((JToken)graphDesc,(JToken)initialization["source_config_graph"],"source config graph"),
                Tuple.Create((JToken)buildingDesc,(JToken)initialization["building_definitions"],"building definitions"),
                Tuple.Create((JToken)unitDesc,(JToken)initialization["unit_production_metadata"],"unit production metadata")})
                RequireSameDescriptor(pair.Item1,pair.Item2,pair.Item3);
            string[] artifactNames={"source_context","configuration_qualification","source_config_graph","building_definitions","unit_production_metadata","authoring_result","capture_request","unit_key_input","wrapper_log"};
            foreach(string name in artifactNames)RequireGenericProofFile(proof[name] as JObject,sourceRoot,name,32*1024*1024);
            JObject context=SourceConfigurationQualification.Context(initialization);
            var targetContext=context["target_context"] as JObject;var target=proof["target"] as JObject;
            string[] targetNames={"catalog_id","seed","size","difficulty","setup_hash","operation_map_id","source_content_hash"};
            if(targetContext==null||target==null||target.Count!=targetNames.Length||target.Properties().Any(p=>!targetNames.Contains(p.Name))||
                targetNames.Any(name=>!JToken.DeepEquals(target[name],targetContext[name]))||
                (string)initialization["mission_id"]!=(string)targetContext["catalog_id"])
                throw new InvalidDataException("Generic focused-capture target differs from current original compiler context");
            if((string)manifest["raw_capture_status"]!="Failed_UnconsumedPresentationUnityObjectRefs"||
                (int?)manifest["unsupported_values_preserved"]!=(raw["unsupported_values"] as JArray)?.Count||
                (bool?)manifest["runtime_import_attempted"]!=false||(bool?)manifest["source_fit_certified"]!=false||
                (bool?)manifest["native_visual_readiness"]!=false||(bool?)manifest["mission_ready"]!=false||
                !JToken.DeepEquals(manifest["capture_proof"],proofDescriptor))
                throw new InvalidDataException("Generic focused projection manifest promoted or changed source proof");
            var contextFile=Parse(AutonomousWorld.PinnedFile((string)contextDesc["path"],(string)contextDesc["sha256"],4*1024*1024));
            var graphFile=Parse(AutonomousWorld.PinnedFile((string)graphDesc["path"],(string)graphDesc["sha256"],16*1024*1024));
            var buildingFile=Parse(AutonomousWorld.PinnedFile((string)buildingDesc["path"],(string)buildingDesc["sha256"],8*1024*1024));
            var unitFile=Parse(AutonomousWorld.PinnedFile((string)unitDesc["path"],(string)unitDesc["sha256"],8*1024*1024));
            var authorFile=Parse(AutonomousWorld.PinnedFile((string)authorDesc["path"],(string)authorDesc["sha256"],8*1024*1024));
            if((string)graphFile["authority"]!="AuthoredConfigAssets"||(bool?)graphFile["compiled_setup"]!=false||
                (bool?)graphFile["world_initialized"]!=false||(bool?)graphFile["native_world_state_used"]!=false||
                (string)contextFile["target_context"]?["catalog_id"]!=(string)target["catalog_id"]||
                (long?)contextFile["target_context"]?["setup_hash"]!=(long?)target["setup_hash"]||
                (string)buildingFile["catalog_id"]!=(string)target["catalog_id"]||(long?)buildingFile["setup_hash"]!=(long?)target["setup_hash"]||
                (string)unitFile["catalog_id"]!=(string)target["catalog_id"]||(long?)unitFile["setup_hash"]!=(long?)target["setup_hash"]||
                (bool?)buildingFile["native_S050_capture"]!=false||(bool?)buildingFile["full_mission_initialized"]!=false||
                (string)authorFile["catalog_id"]!=(string)target["catalog_id"]||(long?)authorFile["resolved_setup"]?["setup_hash"]!=(long?)target["setup_hash"]||
                (bool?)authorFile["compiler_accepted"]!=true||(bool?)authorFile["mission_ready"]!=false)
                throw new InvalidDataException("Generic source authoring/metadata target linkage changed");
            if(actualUnitOwner)ValidateActualUnitOwnerProof(proof,contextFile,unitFile,authorFile,sourceRoot);
            else if((bool?)unitFile["native_S050_capture"]!=false||(bool?)unitFile["full_mission_initialized"]!=false)
                throw new InvalidDataException("Legacy derived unit metadata must retain its explicit non-native source-only scope");
            var keyDesc=proof["unit_key_input"] as JObject;
            string keyPath=(string)keyDesc?["path"],keySha=(string)keyDesc?["sha256"];
            byte[] keyBytes=AutonomousWorld.PinnedFile(keyPath,keySha,1024*1024);
            JArray keys=ParseArray(keyBytes);
            var unitRows=unitFile["units"] as JArray;
            if(unitRows==null)throw new InvalidDataException("Current unit metadata rows required for focused roots");
            var requiredKeys=unitRows.OfType<JObject>().Select(row=>(string)row["prefab"]?["key"]??(string)row["key"]).ToArray();
            if(actualUnitOwner&&unitRows.OfType<JObject>().Any(row=>(bool?)row["authoring_catalog_properties"]?["fields"]?["CanRequest"]?["value"]!=true))
                throw new InvalidDataException("Every direct owner unit key must be explicitly CanRequest=true");
            if(requiredKeys.Length==0||requiredKeys.Distinct(StringComparer.Ordinal).Count()!=requiredKeys.Length||
                !keys.Values<string>().SequenceEqual(requiredKeys,StringComparer.Ordinal)||
                !JToken.DeepEquals(raw["requested_prefab_keys"],keys)||
                (string)raw["requested_prefab_keys_sha256"]!=keySha||
                (string)raw["source_pins"]?[keyPath]!=keySha)
                throw new InvalidDataException("Hash-bound focused keys must exactly match current unit production metadata");
            var captureRequest=Parse(AutonomousWorld.PinnedFile((string)proof["capture_request"]["path"],(string)proof["capture_request"]["sha256"],2*1024*1024));
            bool captureRequestMatches=(string)captureRequest["execute_method"]=="Game.Editor.SkirmishBakedActorSnapshotExporter.RunFocusedPrefabReferenceData"&&
                (string)captureRequest["focused_unit_key_descriptor"]?["sha256"]==keySha&&
                !string.IsNullOrEmpty((string)captureRequest["focused_unit_key_descriptor"]?["path"])&&
                JToken.DeepEquals(captureRequest["focused_unit_prefab_keys"],keys);
            if(actualUnitOwner)captureRequestMatches=captureRequestMatches&&
                (string)captureRequest["death_readset_unit_key_descriptor"]?["sha256"]==keySha&&
                JToken.DeepEquals(captureRequest["death_readset_unit_prefab_keys"],keys);
            if(!captureRequestMatches||!actualUnitOwner&&
                ((string)captureRequest["focused_unit_key_descriptor"]?["path"]!=keyPath||
                 (string)captureRequest["capture_output_directory"]!=Path.GetDirectoryName(source)))
                throw new InvalidDataException("Original focused capture request differs from retained source artifact");
            VerifyGenericFocusedSourcePins(raw["source_pins"] as JObject,sourceRoot);
            var fieldCounts=GenericUnsupportedVfxFieldCounts(raw);var componentCounts=GenericPresentationComponentCounts(raw);
            if(!ExactCountObject(proof["exact_unconsumed_fields"],fieldCounts)||
                !ExactCountObject(proof["presentation_components_omitted_by_existing_importer"],componentCounts))
                throw new InvalidDataException("Generic focused VFX proof counts differ from actual retained capture");
            var logBytes=AutonomousWorld.PinnedFile((string)proof["wrapper_log"]["path"],(string)proof["wrapper_log"]["sha256"],32*1024*1024);
            string log=Encoding.UTF8.GetString(logBytes);int unsupportedCount=(raw["unsupported_values"] as JArray)?.Count??-1;
            if(!log.Contains("InvalidOperationException: Focused prefab reference contains unsupported values: "+unsupportedCount)||
                !log.Contains("ExitCode: 0")||log.Contains("[SkirmishFocusedPrefabReferenceData] result=Passed")||
                !log.Contains("Game.Editor.SkirmishBakedActorSnapshotExporter.RunFocusedPrefabReferenceData")||!log.Contains(keySha))
                throw new InvalidDataException("Generic focused raw failure log no longer binds invocation and failure status");
            if(actualUnitOwner&&(!LoggedArguments(log,"-skirmishFocusedPrefabReferenceData",Path.GetDirectoryName(source))||
                !LoggedArguments(log,"-skirmishFocusedPrefabKeys",keyPath,"-skirmishFocusedPrefabKeysSha",keySha)))
                throw new InvalidDataException("Actual owner capture must bind the exact key descriptor path and bytes used by Unity");
            // Let the exact compiled importer perform a no-world decode pass here,
            // in addition to rechecking provenance. Entity identities are not allocated.
            var preflight=GeneratedReferenceTemplateImporter.Preflight(domain,Path.GetDirectoryName(source),requiredKeys);
            if((int?)preflight["roots"]!=keys.Count||(int?)preflight["entities"]!=(raw["entities"] as JArray)?.Count||
                (int?)preflight["omitted_presentation_components"]!=componentCounts.Values.Sum())
                throw new InvalidDataException("Compiled importer source-only preflight differs from focused capture proof");
        }

        private static bool LoggedArguments(string log,params string[] values)
        {
            string[] lines=log.Replace("\r","").Split('\n').Select(x=>x.Trim()).ToArray();
            for(int i=0;i+values.Length<=lines.Length;i++)if(values.Select((value,index)=>lines[i+index]==value).All(x=>x))return true;
            return false;
        }

        private static JObject ReadGenericProofDocument(JObject descriptor,string sourceRoot,string name,int maximum)
        {
            string path=RequireGenericProofFile(descriptor,sourceRoot,name,maximum);
            return Parse(AutonomousWorld.PinnedFile(path,(string)descriptor["sha256"],maximum));
        }

        private static void ValidateActualUnitOwnerProof(JObject proof,JObject context,JObject units,JObject author,string sourceRoot)
        {
            var owner=proof["unit_owner_evidence"] as JObject;
            string[] fields={"mode","identity_readback_sha256","identity_count","requestable_unit_count","keys",
                "compiler_result","physical_input_request","logical_layout_binding_fingerprint","compiled_layout_fingerprint",
                "compiler_source_policy_hash","physical_map_content_hash","source_pin_count","identity_readback"};
            if(owner==null||owner.Count!=fields.Length||owner.Properties().Any(p=>!fields.Contains(p.Name))||
                (string)owner["mode"]!="OriginalConfiguredAuthoringFactories_CompilerSelected"||
                (int?)owner["identity_count"]!=16||(int?)owner["requestable_unit_count"]!=16||
                (int?)owner["source_pin_count"]!=(units["source_pins"] as JObject)?.Count||
                (bool?)units["full_mission_initialized"]!=false||(bool?)units["native_visual_readiness"]!=false||
                (bool?)units["baked_actor_footprint_substituted"]!=false||(bool?)units["ui_catalog_runtime_policy_sampled"]!=false||
                (bool?)units["runtime_camera_basis_exported"]!=false||
                (units["mission_ready"]!=null&&(bool?)units["mission_ready"]!=false)||
                (units["runtime_admission"]!=null&&(bool?)units["runtime_admission"]!=false)||
                (units["source_fit_certified"]!=null&&(bool?)units["source_fit_certified"]!=false)||
                (units["native_acceptance"]!=null&&(bool?)units["native_acceptance"]!=false)||
                (units["world_allocated"]!=null&&(bool?)units["world_allocated"]!=false))
                throw new InvalidDataException("Direct unit owner provenance fields or non-readiness scope changed");
            var compiler=units["compiler_selection"] as JObject;
            if((string)units["kind"]!="AuthoredSkirmishUnitProductionMetadata"||
                (string)units["authority"]!="OriginalConfiguredAuthoringFactories_NotMissionState"||
                compiler==null||(string)compiler["status"]!="ConfigurationCompiled"||
                (bool?)compiler["runtime_admission"]!=false||(bool?)compiler["source_fit_certified"]!=false||
                (bool?)compiler["mission_ready"]!=false||(bool?)compiler["native_acceptance"]!=false||
                (bool?)compiler["world_allocated"]!=false||
                (string)compiler["catalog_id"]!=(string)units["catalog_id"]||
                (long?)compiler["seed"]!=(long?)units["seed"]||(long?)compiler["setup_hash"]!=(long?)units["setup_hash"]||
                (string)compiler["size"]!=(string)author["size"]||
                (string)compiler["difficulty"]!=(string)author["difficulty"]||
                (long?)compiler["seed"]!=(long?)author["seed"]||
                (string)compiler["catalog_id"]!="S050"||(long?)compiler["seed"]!=104779)
                throw new InvalidDataException("Exact compiler-selected direct unit owner context required");
            VerifyGenericFocusedSourcePins(units["source_pins"] as JObject,sourceRoot);
            var resultRef=compiler["result"] as JObject;var requestRef=compiler["physical_input_request"] as JObject;
            var authorDesc=proof["authoring_result"] as JObject;
            if(!JToken.DeepEquals(owner["compiler_result"],resultRef)||!JToken.DeepEquals(resultRef,authorDesc)||
                (string)owner["physical_input_request"]?["path"]!=(string)requestRef?["path"]||
                (string)owner["physical_input_request"]?["sha256"]!=(string)requestRef?["sha256"]||
                !IsSha256((string)resultRef?["sha256"])||!IsSha256((string)requestRef?["sha256"])||
                (string)units["source_pins"]?[(string)resultRef?["path"]]!=(string)resultRef?["sha256"]||
                (string)units["source_pins"]?[(string)requestRef?["path"]]!=(string)requestRef?["sha256"])
                throw new InvalidDataException("Direct unit owner must pin the exact compiler result and physical request");
            var physical=ReadGenericProofDocument(requestRef,sourceRoot,"selected physical input request",2*1024*1024);
            var target=context["target_context"] as JObject;
            string definition=(string)target?["definition_id"], policyPrefix=definition+".v"+(string)target?["content_version"]+"/enemy-policy-v1/";
            string policy=(string)compiler["source_content_hash"];
            if(target==null||(string)compiler["operation_map_id"]!=(string)target["operation_map_id"]||
                (string)author["operation_map_id"]!=(string)target["operation_map_id"]||
                (string)compiler["layout_fingerprint"]!=(string)target["layout_binding_fingerprint"]||
                (string)owner["logical_layout_binding_fingerprint"]!=(string)target["layout_binding_fingerprint"]||
                (string)author["layout_fingerprint"]!=(string)target["layout_fingerprint"]||
                (string)owner["compiled_layout_fingerprint"]!=(string)target["layout_fingerprint"]||
                (string)author["source_content_hash"]!=(string)target["source_content_hash"]||
                (string)owner["physical_map_content_hash"]!=(string)target["source_content_hash"]||
                policy==null||!policy.StartsWith(policyPrefix,StringComparison.Ordinal)||!IsSha256(policy.Substring(policyPrefix.Length))||
                (string)owner["compiler_source_policy_hash"]!=policy||
                (string)physical["selection"]?["path"]!=(string)resultRef["path"]||
                (string)physical["selection"]?["sha256"]!=(string)resultRef["sha256"]||
                (string)physical["operation_map_id"]!=(string)author["operation_map_id"]||
                (string)physical["content_hash"]!=(string)author["source_content_hash"]||
                (bool?)physical["source_fit_certified"]!=false||(bool?)physical["mission_ready"]!=false||
                (string)author["status"]!="ConfigurationCompiled"||(bool?)author["compiler_accepted"]!=true||
                (bool?)author["native_authoring_inputs_returned"]!=true||
                (bool?)author["world_allocated"]!=false||(bool?)author["source_fit_verified"]!=false||
                (bool?)author["mission_ready"]!=false||(bool?)author["native_acceptance"]!=false||
                (bool?)author["catalog_mutated"]!=false||(bool?)author["native_playable"]!=false||
                (bool?)author["validation_passed"]!=false||(bool?)author["partial_authoring_capture_for_missing_binding"]!=false||
                (author["compiler_reasons"] as JArray)?.Count!=0||(author["authoring_capture_reasons"] as JArray)?.Count!=0)
                throw new InvalidDataException("Direct owner compiler, logical binding, map source and false gates differ");
            var identityDesc=owner["identity_readback"] as JObject;
            JObject identity=ReadGenericProofDocument(identityDesc,sourceRoot,"direct unit identity readback",4*1024*1024);
            byte[] unitBytes=AutonomousWorld.PinnedFile((string)proof["unit_production_metadata"]?["path"],
                (string)proof["unit_production_metadata"]?["sha256"],8*1024*1024);
            if((string)identity["kind"]!="AuthoredUnitPrefabIdentityReadback"||
                (string)identity["metadata_sha256"]!=AutonomousWorld.Hash(unitBytes)||
                (string)owner["identity_readback_sha256"]!=(string)identity["metadata_sha256"]||
                (string)identity["catalog_id"]!=(string)units["catalog_id"]||
                (long?)identity["seed"]!=(long?)units["seed"]||(long?)identity["setup_hash"]!=(long?)units["setup_hash"]||
                (int?)identity["units"]!=16||(bool?)identity["runtime_templates_verified"]!=false||
                (bool?)identity["mission_ready"]!=false)
                throw new InvalidDataException("Independent identity readback must bind exact direct owner bytes and context");
            var rows=units["units"] as JArray;var ids=identity["identities"] as JArray;var keys=owner["keys"] as JArray;
            if(rows==null||rows.Count!=16||ids==null||ids.Count!=16||keys==null||keys.Count!=16)
                throw new InvalidDataException("Exact 16-row direct unit-owner and identity domains required");
            var identityByKey=ids.OfType<JObject>().ToDictionary(x=>(string)x["key"],StringComparer.Ordinal);
            for(int i=0;i<16;i++)
            {
                var row=rows[i] as JObject;var prefab=row?["prefab"] as JObject;string key=(string)row?["key"];
                if((int?)row?["index"]!=i||(bool?)row?["has_definition_metadata"]!=true||
                    (bool?)row?["has_production_metadata"]!=true||
                    (bool?)row?["authoring_catalog_properties"]?["fields"]?["CanRequest"]?["value"]!=true||
                    (string)keys[i]!=key||(string)prefab?["key"]!=key||
                    (string)prefab?["codec"]!="source-asset-reference-v1"||
                    (string)prefab?["type"]!="UnityEngine.GameObject"||
                    !Guid.TryParseExact((string)prefab?["guid"],"N",out var parsedGuid)||parsedGuid==Guid.Empty||
                    ((long?)prefab?["local_id"]??0L)<=0||
                    !identityByKey.TryGetValue(key,out var id)||(string)id["path"]!=(string)prefab["path"]||
                    (string)id["guid"]!=(string)prefab["guid"]||(long?)id["local_id"]!=(long?)prefab["local_id"])
                    throw new InvalidDataException("Direct owner requestable prefab differs from independent identity row at index "+i);
                string asset=(string)prefab["path"];
                if(string.IsNullOrEmpty(asset)||!asset.StartsWith("Assets/Game/Prefabs/",StringComparison.Ordinal)||!asset.EndsWith(".prefab",StringComparison.Ordinal))
                    throw new InvalidDataException("Direct owner prefab path outside unit prefab domain");
                string absolute=Path.GetFullPath(Path.Combine(sourceRoot,asset.Replace('/',Path.DirectorySeparatorChar)));
                if((string)units["source_pins"]?[absolute]==null||(string)units["source_pins"]?[absolute+".meta"]==null)
                    throw new InvalidDataException("Direct owner prefab asset and .meta pins required");
            }
        }

        private static JArray ParseArray(byte[] bytes)
        {
            using(var text=new StringReader(Encoding.UTF8.GetString(bytes)))using(var reader=new JsonTextReader(text){DateParseHandling=DateParseHandling.None})
            {
                var value=JArray.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
                if(reader.Read())throw new InvalidDataException("Exactly one key array required");return value;
            }
        }

        private static void ValidateS074UnsupportedVfxRows(JObject raw)
        {
            const string objectRef="Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]";
            var allowed=new Dictionary<string,HashSet<string>>(StringComparer.Ordinal){
                ["Game.Components.UnitAttackImpactVfxReference"]=new HashSet<string>(new[]{"Prefab"},StringComparer.Ordinal),
                ["Game.Components.UnitMuzzleFlashVfxReference"]=new HashSet<string>(new[]{"Prefab"},StringComparer.Ordinal),
                ["Game.Components.GroundMissileLauncherVfxReferenceComponent"]=new HashSet<string>(new[]{"LauncherBackfirePrefab","RocketTrailPrefab","ImpactExplosionPrefab","ImpactSmokePrefab"},StringComparer.Ordinal),
                ["Game.Components.AirMissileLauncherVfxReferenceComponent"]=new HashSet<string>(new[]{"MissileVisualPrefab","LaunchFlashPrefab","LaunchSmokePrefab","MissileTrailPrefab","AirburstExplosionPrefab","AirTargetImpactPrefab","InterceptExplosionPrefab"},StringComparer.Ordinal)};
            var entities=new HashSet<string>(((JArray)raw["entities"]).Values<JObject>().Select(x=>(string)x["id"]),StringComparer.Ordinal);
            var seen=new HashSet<string>(StringComparer.Ordinal);var counts=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(JObject row in (JArray)raw["unsupported_values"])
            {
                string[] parts=((string)row["path"]??"").Split('/');
                if(parts.Length!=3||!entities.Contains(parts[0])||!allowed.TryGetValue(parts[1],out var fields)||!fields.Contains(parts[2])||
                    (string)row["reason"]!="unity_object_ref_requires_bound_source_identity"||(string)row["type"]!=objectRef||!seen.Add((string)row["path"]))throw new InvalidDataException("S074 unsupported value outside exact unconsumed VFX read domain");
                var entity=((JArray)raw["entities"]).OfType<JObject>().Single(x=>(string)x["id"]==parts[0]);var component=entity["components"]?[parts[1]] as JObject;
                var field=component?["value"]?["fields"]?[parts[1]+"::"+parts[2]] as JObject;
                if((string)component?["value"]?["codec"]!="struct-fields-v1"||(string)field?["codec"]!="unsupported-v1"||(string)field?["type"]!=objectRef||(string)field?["reason"]!=(string)row["reason"])throw new InvalidDataException("S074 unsupported VFX value differs from captured component bytes");
                string key=parts[1]+"/"+parts[2];counts[key]=counts.TryGetValue(key,out int count)?count+1:1;
            }
            var expected=new Dictionary<string,int>(StringComparer.Ordinal){
                ["Game.Components.UnitAttackImpactVfxReference/Prefab"]=10,
                ["Game.Components.UnitMuzzleFlashVfxReference/Prefab"]=9,
                ["Game.Components.GroundMissileLauncherVfxReferenceComponent/LauncherBackfirePrefab"]=1,
                ["Game.Components.GroundMissileLauncherVfxReferenceComponent/RocketTrailPrefab"]=1,
                ["Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactExplosionPrefab"]=1,
                ["Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactSmokePrefab"]=1};
            if(counts.Count!=expected.Count||expected.Any(p=>!counts.TryGetValue(p.Key,out int count)||count!=p.Value))throw new InvalidDataException("S074 unconsumed VFX field counts changed");
            var presentationCounts=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(JObject entity in (JArray)raw["entities"])foreach(string component in allowed.Keys)if(entity["components"]?[component]!=null)presentationCounts[component]=presentationCounts.TryGetValue(component,out int count)?count+1:1;
            if(presentationCounts.Count!=3||presentationCounts["Game.Components.UnitAttackImpactVfxReference"]!=10||presentationCounts["Game.Components.UnitMuzzleFlashVfxReference"]!=9||presentationCounts["Game.Components.GroundMissileLauncherVfxReferenceComponent"]!=1)throw new InvalidDataException("S074 importer presentation-only component domain changed");
        }

        private static void ValidateS074CaptureProof(JObject descriptor,JObject manifest,JObject domain,JObject raw,string source,string sourceSha,string sourceRoot,JObject initialization)
        {
            if(initialization==null||(string)initialization["mission_id"]!="S074"||sourceSha!=S074FocusedRawCaptureSha||
                Path.GetFullPath(source)!=source||!Path.IsPathRooted(source)||!File.Exists(source))throw new InvalidDataException("Exact retained failed S074 focused capture required");
            var proofDescriptor=descriptor["s074_capture_proof"] as JObject;
            if(proofDescriptor==null||proofDescriptor.Count!=2||proofDescriptor.Properties().Any(p=>p.Name!="path"&&p.Name!="sha256")||proofDescriptor["sha256"]?.Type!=JTokenType.String)throw new InvalidDataException("Exact S074 focused capture proof descriptor required");
            var proof=Parse(AutonomousWorld.PinnedFile((string)proofDescriptor["path"],(string)proofDescriptor["sha256"],1024*1024));
            string[] proofFields={"schema_version","kind","status","scope","raw_capture","raw_capture_result","raw_capture_wrapper_exit_code","raw_capture_pass_marker_present","raw_capture_log","wrapper_log","source_context","configuration_qualification","source_config_graph","building_definitions","unit_production_metadata","unit_key_input","target","source_pin_count","source_pins_verified_current","unsupported_vfx_rows_preserved","exact_unconsumed_fields","presentation_components_omitted_by_existing_importer","raw_capture_replay_ready","runtime_import_attempted","native_visual_readiness","source_fit_certified","mission_ready","runtime_admission"};
            if(proof.Count!=proofFields.Length||proof.Properties().Any(p=>!proofFields.Contains(p.Name))||(int?)proof["schema_version"]!=1||
                ((string)proof["kind"]!="S074FocusedPrefabSourceOnlyCaptureProof"&&((string)proof["kind"]!="S074TenMetrePlayerVehicleFocusedPrefabSourceOnlyCaptureProof"||(long?)proof["target"]?["setup_hash"]!=3241215135L)&&(string)proof["kind"]!="S074BothVehicleTenMetreFocusedPrefabSourceOnlyCaptureProof"&&(string)proof["kind"]!="S074SourceOwnerFocusedPrefabSourceOnlyCaptureProof")||(string)proof["status"]!="PassedSourceOnlyProjectionOfFailedRawCapture"||
                (string)proof["raw_capture_result"]!="Failed_UnconsumedPresentationUnityObjectRefs"||(int?)proof["raw_capture_wrapper_exit_code"]!=0||
                (bool?)proof["raw_capture_pass_marker_present"]!=false||(int?)proof["source_pin_count"]!=8087||(bool?)proof["source_pins_verified_current"]!=true||
                (int?)proof["unsupported_vfx_rows_preserved"]!=23||(bool?)proof["raw_capture_replay_ready"]!=false||
                (bool?)proof["runtime_import_attempted"]!=false||(bool?)proof["native_visual_readiness"]!=false||
                (bool?)proof["source_fit_certified"]!=false||(bool?)proof["mission_ready"]!=false||(bool?)proof["runtime_admission"]!=false)
                throw new InvalidDataException("S074 focused capture proof scope changed");
            string[] targetFields={"mission_id","seed","size","difficulty","setup_hash","operation_map_id"};var target=proof["target"] as JObject;
            bool origin=(long?)target?["setup_hash"]==1716123768L;
            bool sixMetre=(long?)target?["setup_hash"]==1815748444L;
            bool eightMetre=(long?)target?["setup_hash"]==1559121027L;
            bool tenMetre=(long?)target?["setup_hash"]==3241215135L;
            bool receiptCandidate=initialization["s074_candidate_author"] is JObject;
            long expectedSetup=receiptCandidate?(long?)target?["setup_hash"]??0L:tenMetre?3241215135L:eightMetre?1559121027L:sixMetre?1815748444L:origin?1716123768L:1298808707L;
            if(target==null||target.Count!=targetFields.Length||target.Properties().Any(p=>!targetFields.Contains(p.Name))||
                (string)target["mission_id"]!="S074"||(int?)target["seed"]!=104803||(string)target["size"]!="Standard"||
                (string)target["difficulty"]!="Regular"||expectedSetup<=0||expectedSetup>uint.MaxValue||(long?)target["setup_hash"]!=expectedSetup||
                (string)target["operation_map_id"]!="opmap.skirmish.industrial_basin")throw new InvalidDataException("Exact S074 focused capture target required");
            var contextDescriptor=proof["source_context"] as JObject;var qualificationDescriptor=proof["configuration_qualification"] as JObject;
            var graphDescriptor=proof["source_config_graph"] as JObject;var buildingDescriptor=proof["building_definitions"] as JObject;var unitDescriptor=proof["unit_production_metadata"] as JObject;var keyDescriptor=proof["unit_key_input"] as JObject;
            foreach(var pair in new[]{Tuple.Create((JToken)contextDescriptor,(JToken)initialization["metadata_compiler_context"],"metadata_compiler_context"),Tuple.Create((JToken)qualificationDescriptor,(JToken)initialization["configuration_qualification"],"configuration_qualification"),Tuple.Create((JToken)graphDescriptor,(JToken)initialization["source_config_graph"],"source_config_graph"),Tuple.Create((JToken)buildingDescriptor,(JToken)initialization["building_definitions"],"building_definitions"),Tuple.Create((JToken)unitDescriptor,(JToken)initialization["unit_production_metadata"],"unit_production_metadata")})RequireSameDescriptor(pair.Item1,pair.Item2,pair.Item3);
            string contextSha=receiptCandidate?(string)contextDescriptor["sha256"]:tenMetre?"1256a8ce7dc04b144997592f7e0d87d549b22087285bdb2d46e334513c0cc374":eightMetre?"d54c71cf26affe91826a7fdcf6c76d1f505f5b54cddb867869b16406c6b89cfb":sixMetre?"c0f9d1e6c01467826463f9458cf92e8f8125ad7fd4e5bf66a208e0bf63095b91":origin?"254fa7f2cf9bb76a0cf8e26e73f5cbcfd2118e06f14d5b56a84b0004f2911d6c":S074FocusedContextSha;
            string qualificationSha=receiptCandidate?(string)qualificationDescriptor["sha256"]:tenMetre?"85ed7bfceed8b6844ec9f64ce1694634681ace0fb44997b5cb5f181dff829161":eightMetre?"e39c22f7778f0ce472ec15d1df606096c3fa5cc243b80267cb6bee01ff8665a2":sixMetre?"0894e2b1669698bf3ad295a92ecff7eb3a1a443cea16cda0a9233c35714a689f":origin?"0887f2afb948fd2155334eba30b2fccf8f68130528b302e864f30b52f7f72c6e":S074FocusedQualificationSha;
            string buildingSha=receiptCandidate?(string)buildingDescriptor["sha256"]:tenMetre?"fa1490f2868c259e776f300b326e14c9c923946881a20306876a9d2f9ede2e55":eightMetre?"f2ef38857b83bb4a91f83a57e98923d8851b026439eb795e68ef37f3ce622e75":sixMetre?"b761c4ab95553645e0bac71dabd7900685eca946bbce70b1f65a0ee4e424b26e":origin?"14c9979f850f12e315a1f585aa65f48ac50ea7df4760c6c8658ac95d2922cd54":S074FocusedBuildingMetadataSha;
            string unitSha=receiptCandidate?(string)unitDescriptor["sha256"]:tenMetre?"ec2e62a8ff49414080fb1be8de097e89a0554228f5384a76c5d5ea8066ba3ece":eightMetre?"629847db8002ee5d6310b7f17036ba497a654d89ae05e54c050fd2a2d8e0065b":sixMetre?"feafdbab61c143907db24d42e880cfddfefd950aa4ea20865eb46f2e42b6843d":origin?"cfc835a7f37566fee12f0770669dabe5a579446ef7c8d79c8535c7d6e3d5a365":S074FocusedUnitMetadataSha;
            if((string)contextDescriptor["sha256"]!=contextSha||(string)qualificationDescriptor["sha256"]!=qualificationSha||
                (string)graphDescriptor["sha256"]!=S074FocusedGraphSha||(string)buildingDescriptor["sha256"]!=buildingSha||
                (string)unitDescriptor["sha256"]!=unitSha||keyDescriptor==null||keyDescriptor.Count!=2||
                (string)keyDescriptor["sha256"]!=S074FocusedUnitKeysSha)throw new InvalidDataException("S074 source-context or selected-unit owner pins changed");
            string repo=Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar);
            if(receiptCandidate)
            {
                foreach(var item in new[]{Tuple.Create(contextDescriptor,"source context"),Tuple.Create(qualificationDescriptor,"configuration qualification"),Tuple.Create(buildingDescriptor,"building metadata"),Tuple.Create(unitDescriptor,"unit metadata")})
                {
                    string path=(string)item.Item1["path"];
                    if(string.IsNullOrEmpty(path)||!path.StartsWith(repo+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Repository source-owner proof required: "+item.Item2);
                    RequireProofPath(item.Item1,path,item.Item2);
                    AutonomousWorld.PinnedFile(path,(string)item.Item1["sha256"],16*1024*1024);
                }
            }
            else
            {
            RequireProofPath(contextDescriptor,Path.Combine(repo,tenMetre?"T Logs/SkirmishProduction/s074-ten-metre-player-vehicle-factory-metadata-20261114-r1/s074/source-factory-context.json":eightMetre?"T Logs/SkirmishProduction/s074-eight-metre-factory-metadata-20261114-r1/s074/source-factory-context.json":sixMetre?"T Logs/SkirmishProduction/s074-six-metre-factory-metadata-20261114-r4/s074/source-factory-context.json":origin?"T Logs/SkirmishProduction/s074-source-origin-factory-export-20261107-r1/s074/source-factory-context.json":"T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/exporter/s074/source-factory-context.json"),"source context");
            RequireProofPath(qualificationDescriptor,Path.Combine(repo,tenMetre?"T Logs/SkirmishProduction/s074-ten-metre-player-vehicle-source-qualification-20261114-r1/qualification.json":eightMetre?"T Logs/SkirmishProduction/s074-eight-metre-source-qualification-20261114-r3/qualification.json":sixMetre?"T Logs/SkirmishProduction/s074-six-metre-source-qualification-20261114-r1/qualification.json":origin?"T Logs/SkirmishProduction/s074-source-origin-runtime-inputs-20261107-r1/qualification.json":"T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/qualification/configuration-qualification.json"),"configuration qualification");
            RequireProofPath(graphDescriptor,Path.Combine(repo,"T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json"),"source graph");
            RequireProofPath(buildingDescriptor,Path.Combine(repo,tenMetre?"T Logs/SkirmishProduction/s074-ten-metre-player-vehicle-factory-metadata-20261114-r1/projection/building-definitions.json":eightMetre?"T Logs/SkirmishProduction/s074-eight-metre-factory-metadata-20261114-r1/projection/building-definitions.json":sixMetre?"T Logs/SkirmishProduction/s074-six-metre-factory-metadata-20261114-r4/projection/building-definitions.json":origin?"T Logs/SkirmishProduction/s074-source-origin-factory-projection-20261107-r1/building-definitions.json":"T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/projection/building-definitions.json"),"building metadata");
            RequireProofPath(unitDescriptor,Path.Combine(repo,tenMetre?"T Logs/SkirmishProduction/s074-ten-metre-player-vehicle-factory-metadata-20261114-r1/projection/unit-production-metadata.json":eightMetre?"T Logs/SkirmishProduction/s074-eight-metre-factory-metadata-20261114-r1/projection/unit-production-metadata.json":sixMetre?"T Logs/SkirmishProduction/s074-six-metre-factory-metadata-20261114-r4/projection/unit-production-metadata.json":origin?"T Logs/SkirmishProduction/s074-source-origin-factory-projection-20261107-r1/unit-production-metadata.json":"T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9/projection/unit-production-metadata.json"),"unit metadata");
            }
            RequireProofPath(graphDescriptor,Path.Combine(repo,"T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json"),"source graph");
            RequireProofPath(keyDescriptor,Path.Combine(repo,"T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/s074-unit-16-keys.json"),"unit key input");
            JObject context;
#if WARLINE_SOURCE_S073
            throw new NotSupportedException("S074 focused capture requires its qualified ground source profile");
#else
            context=SourceConfigurationQualification.Context(initialization);
#endif
            var actualTarget=context["target_context"] as JObject;
            if(actualTarget==null||(string)actualTarget["catalog_id"]!="S074"||(int?)actualTarget["seed"]!=104803||
                (string)actualTarget["size"]!="Standard"||(string)actualTarget["difficulty"]!="Regular")throw new InvalidDataException("S074 qualified compiler context target mismatch");
            if((long?)actualTarget["setup_hash"]!=expectedSetup||(string)actualTarget["operation_map_id"]!="opmap.skirmish.industrial_basin"||
                (string)actualTarget["source_content_hash"]!="cbbaa8366cbfb5e0ad9bc6bf70a98b48503df10e3d0c5c45757295af841a7a6d")throw new InvalidDataException("S074 logical source context mismatch");
            var selection=initialization["selection"] as JObject;
            if(selection==null||(int?)selection["seed"]!=104803||(int?)selection["size_id"]!=1||(int?)selection["difficulty_id"]!=2)throw new InvalidDataException("S074 selected setup differs from exact template capture");
            var proofCapture=proof["raw_capture"] as JObject;if(proofCapture==null||proofCapture.Count!=2||(string)proofCapture["path"]!=source||(string)proofCapture["sha256"]!=sourceSha)throw new InvalidDataException("S074 raw capture locator changed");
            if((string)proof["raw_capture_log"]?["sha256"]!=S074FocusedCaptureLogSha||(string)proof["wrapper_log"]?["sha256"]!=S074FocusedWrapperLogSha)throw new InvalidDataException("S074 raw capture failure-log pins changed");
            string rootPath=Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar);
            RequireProofPath(proofCapture,Path.Combine(rootPath,"Logs/SkirmishProduction/s074-owner-data-capture-20261107-r1/prefab-units-16/prefab-reference-data.json"),"raw capture");
            RequireProofPath(proof["raw_capture_log"] as JObject,Path.Combine(rootPath,"T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/prefab-units-capture.log"),"capture log");
            RequireProofPath(proof["wrapper_log"] as JObject,Path.Combine(rootPath,"T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/prefab-units-wrapper.log"),"wrapper log");
            byte[] captureLog=AutonomousWorld.PinnedFile((string)proof["raw_capture_log"]["path"],S074FocusedCaptureLogSha,32*1024*1024);
            byte[] wrapperLog=AutonomousWorld.PinnedFile((string)proof["wrapper_log"]["path"],S074FocusedWrapperLogSha,32*1024*1024);
            string captureText=Encoding.UTF8.GetString(captureLog),wrapperText=Encoding.UTF8.GetString(wrapperLog);
            if(!captureText.Contains("InvalidOperationException: Focused prefab reference contains unsupported values: 23")||!captureText.Contains("ExitCode: 0")||captureText.Contains("[SkirmishFocusedPrefabReferenceData] result=Passed")||
                !wrapperText.Contains("Game.Editor.SkirmishBakedActorSnapshotExporter.RunFocusedPrefabReferenceData")||!wrapperText.Contains(S074FocusedUnitKeysSha))throw new InvalidDataException("S074 failed raw capture evidence changed");
            JObject graph=Parse(AutonomousWorld.PinnedFile((string)graphDescriptor["path"],S074FocusedGraphSha,16*1024*1024));
            if((string)graph["authority"]!="AuthoredConfigAssets"||(bool?)graph["compiled_setup"]!=false||(bool?)graph["world_initialized"]!=false||(bool?)graph["native_world_state_used"]!=false)throw new InvalidDataException("S074 current authored graph authority changed");
            JObject metadata=Parse(AutonomousWorld.PinnedFile((string)unitDescriptor["path"],unitSha,4*1024*1024));
            if((string)metadata["catalog_id"]!="S074"||(int?)metadata["seed"]!=104803||(long?)metadata["setup_hash"]!=expectedSetup||(metadata["units"] as JArray)?.Count!=16)throw new InvalidDataException("S074 current unit metadata identity changed");
            var actualKeys=((JArray)metadata["units"]).Values<JObject>().Select(x=>(string)x["key"]).ToArray();var captureKeys=(raw["requested_prefab_keys"] as JArray)?.Values<string>().ToArray();
            if(!actualKeys.SequenceEqual(captureKeys,StringComparer.Ordinal)||(string)raw["requested_prefab_keys_sha256"]!=S074FocusedUnitKeysSha)throw new InvalidDataException("S074 focused capture roots differ from current unit metadata");
            if((string)raw["kind"]!="AuthoredSkirmishPrefabReferenceData"||(string)raw["authority"]!="ActualSourceAssetsAndNativeBakers_NotMissionState"||
                (bool?)raw["replay_ready"]!=false||(bool?)raw["full_mission_initialized"]!=false||(bool?)raw["native_visual_readiness"]!=false||(bool?)raw["geometry_exported"]!=false||
                (raw["opening_prefabs"] as JObject)?.Count!=16||(raw["entities"] as JArray)?.Count!=528||(raw["unsupported_values"] as JArray)?.Count!=23)throw new InvalidDataException("Failed S074 raw capture scope changed");
            VerifyS074SourcePins(raw["source_pins"] as JObject,sourceRoot,8087);
            ValidateS074UnsupportedVfxRows(raw);
            if(!JToken.DeepEquals(manifest["s074_capture_proof"],proofDescriptor)||(string)manifest["raw_capture_status"]!="Failed_UnconsumedPresentationUnityObjectRefs"||
                (int?)manifest["unsupported_values_preserved"]!=23||(bool?)manifest["runtime_import_attempted"]!=false||(bool?)manifest["source_fit_certified"]!=false||
                (bool?)manifest["native_visual_readiness"]!=false||(bool?)manifest["mission_ready"]!=false)throw new InvalidDataException("S074 projection manifest promoted or changed capture proof");
            if((int?)proof["presentation_components_omitted_by_existing_importer"]?["Game.Components.UnitAttackImpactVfxReference"]!=10||
                (int?)proof["presentation_components_omitted_by_existing_importer"]?["Game.Components.UnitMuzzleFlashVfxReference"]!=9||
                (int?)proof["presentation_components_omitted_by_existing_importer"]?["Game.Components.GroundMissileLauncherVfxReferenceComponent"]!=1||
                ((JObject)proof["presentation_components_omitted_by_existing_importer"]).Count!=3)throw new InvalidDataException("S074 exact presentation-only importer omission set changed");
        }

        // S003 callers can pass a qualified graph; this S026-capable source
        // adapter intentionally has no such graph projection and fails closed.
        internal static JObject Read(JObject descriptor, out string binaryDirectory, JObject expectedCaptureGraph)
        {
            if (expectedCaptureGraph != null)
                throw new InvalidDataException("Capture graph projection is not part of this source profile");
            return Read(descriptor, out binaryDirectory);
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
