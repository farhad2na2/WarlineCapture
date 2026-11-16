using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            return Read(descriptor, out binaryDirectory, null);
        }

        // The current S003 capture is admitted only through its current strict capture and
        // source-host importer receipts. The receipt hashes are supplied by the descriptor;
        // generated build/output hashes are never embedded in this source file.
        internal static JObject Read(JObject descriptor, out string binaryDirectory, JObject expectedCaptureGraph)
        {
            return Read(descriptor, out binaryDirectory, expectedCaptureGraph, null, null, null, null);
        }

        internal static JObject Read(JObject descriptor, out string binaryDirectory, JObject expectedCaptureGraph,
            JObject request, Game.Configs.SkirmishResolvedSetup setup, JObject context, string sourceRoot)
        {
            bool derivedS003 = descriptor["s003_derived_target_proof"] != null;
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
            bool currentS003 = focused && descriptor["current_capture_proof"] != null;
            bool refreshedS003 = focused && descriptor["refreshed_capture_proof"] != null;
            bool independentS003 = focused && descriptor["independent_capture_proof"] != null;
            if((currentS003?1:0)+(refreshedS003?1:0)+(independentS003?1:0)>1 || derivedS003 && (!independentS003 || request == null || setup == null || context == null || string.IsNullOrEmpty(sourceRoot)))throw new InvalidDataException("Only one focused capture proof is allowed; derived proof requires actual source context");
            string[] selected = currentS003||refreshedS003||independentS003 ? CurrentS003SelectedFields : focused ?
                new[]{"schema_version","kind","authority","source_scenes","unity_version","template_namespace_id","requested_prefab_keys","requested_prefab_keys_sha256","opening_prefabs","entities","unsupported_values","replay_ready","fresh_world_allocator_exported","full_mission_initialized","native_visual_readiness","geometry_exported","consumer_rule"} :
                new[]{"schema_version","kind","authority","opening_prefabs","entities"};
            if(focused)
            {
                var original=Parse(File.ReadAllBytes(source));
                foreach(string name in selected)if(!JToken.DeepEquals(domain[name],original[name]))throw new InvalidDataException("Focused projection changed native field/namespace: "+name);
                if(currentS003)ValidateCurrentS003Proof(descriptor,manifest,domain,source,sourceSha,expectedCaptureGraph);
                else if(refreshedS003)ValidateRefreshedS003Proof(descriptor,manifest,domain,source,sourceSha,expectedCaptureGraph);
                else if(independentS003){if(derivedS003)SourceDerivedTemplateEvidence.Validate(descriptor,request,setup,context,domain,sourceRoot);ValidateIndependentS003Proof(descriptor,manifest,domain,source,sourceSha,expectedCaptureGraph,derivedS003);}
                else if(sourceSha!=QualifiedFocusedPrefabSha&&sourceSha!="83aec3d79c2ac069527c438be1056df9639e40f325b4a3a3c479e090996dd70f"&&sourceSha!="15f69a537018de732331af82f4591a30dbc674a9689c40481e1b3f7324732bcc"&&sourceSha!="fa8933a86f1ada977e9f10b53a0af770c79b43e8c73da49da385a192985f8969"&&sourceSha!="05bfd096a7dab53ecb813d80b474ce33db0b411da2fe7405a1cbc00a3a340bad")throw new InvalidDataException("Unqualified focused native source identity");
                if((bool?)domain["replay_ready"]!=false||(bool?)domain["full_mission_initialized"]!=false||(bool?)domain["geometry_exported"]!=false)throw new InvalidDataException("Focused diagnostic native scope must remain explicit");
            }
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

        private static readonly string[] CurrentS003SelectedFields = {
            "schema_version","kind","authority","mission_id","source_scenes","source_pins","unity_version",
            "template_namespace_id","requested_prefab_keys","requested_prefab_keys_sha256",
            "source_key_descriptor_receipt_sha256","native_metadata_capture_sha256",
            "native_metadata_capture_receipt_sha256","compiler_seed","compiler_setup_hash","opening_prefabs",
            "entities","unsupported_values","unsupported_values_preserved_verbatim","diagnostic_status",
            "replay_ready","fresh_world_allocator_exported","full_mission_initialized","utility_world_created",
            "utility_world_disposed","mission_world_created","native_visual_readiness","source_fit_certified",
            "native_context_qualified","mission_ready","geometry_exported","consumer_rule"
        };

        private static void ValidateIndependentS003Proof(JObject descriptor,JObject manifest,JObject domain,string sourcePath,string sourceSha,JObject expectedCaptureGraph,bool derivedS003)
        {
            var proof=descriptor["independent_capture_proof"] as JObject;
            string[] names={"schema_version","kind","source_graph","capture_audit","metadata_capture","metadata_audit","metadata_log","importer_receipt","importer_log"};
            if(proof==null||proof.Count!=names.Length||proof.Properties().Any(p=>!names.Contains(p.Name))||(int?)proof["schema_version"]!=1||(string)proof["kind"]!="S003IndependentFocusedTemplateSourceProof"||expectedCaptureGraph==null||!JToken.DeepEquals(proof["source_graph"],expectedCaptureGraph)||!JToken.DeepEquals(manifest["independent_capture_proof"],proof))throw new InvalidDataException("Exact independently audited S003 proof and capture graph required");
            foreach(string name in names.Skip(2))ExactDescriptor(proof[name] as JObject,"independent S003 "+name);
            var graph=ReadProof(proof,"source_graph",16*1024*1024);var capture=ReadProof(proof,"capture_audit",4*1024*1024);var metadata=ReadProof(proof,"metadata_capture",64*1024*1024);var metadataAudit=ReadProof(proof,"metadata_audit",4*1024*1024);var importer=ReadProof(proof,"importer_receipt",16*1024*1024);
            string marker="[S003RefreshedPrefabImportControl] result=Passed roots=19 entities=647 unsupported=32 omitted=26 negatives=3 sourceProjection=Passed world=0 readiness=0";
            string importerLog=Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)proof["importer_log"]["path"],(string)proof["importer_log"]["sha256"],4*1024*1024));
            if(importerLog.Split('\n').Count(line=>line.TrimEnd('\r')==marker)!=1||(string)importer["marker"]!=marker||(string)importer["status"]!="PassedDisposableS003ImporterFixture"||(string)importer["capture_sha256"]!=sourceSha||(bool?)importer["movement_source_projection_checked"]!=true||(bool?)importer["world_allocated"]!=false||(bool?)importer["native_context_qualified"]!=false||(bool?)importer["mission_ready"]!=false)throw new InvalidDataException("Current S003 importer evidence mismatch");
            if((string)capture["status"]!="PassedDataOnlyCapture"||(int?)capture["wrapper_exit_code"]!=0||(bool?)capture["required_marker_present"]!=true||(string)capture["capture"]?["path"]!=sourcePath||(string)capture["capture"]?["sha256"]!=sourceSha||(int?)capture["roots"]!=19||(int?)capture["entities"]!=647||(int?)capture["unsupported_presentation_values"]!=32||(bool?)capture["import_admitted"]!=false||(bool?)capture["mission_ready"]!=false||(string)capture["metadata_capture_sha256"]!=(string)proof["metadata_capture"]["sha256"]||(string)domain["native_metadata_capture_sha256"]!=(string)proof["metadata_capture"]["sha256"])throw new InvalidDataException("Independent S003 capture scope or metadata binding differs");
            if((string)metadataAudit["status"]!="Passed"||(int?)metadataAudit["wrapper_exit_observed"]!=0||(int?)metadataAudit["marker_count"]!=1||(string)metadataAudit["capture_sha256"]!=(string)proof["metadata_capture"]["sha256"]||!JToken.DeepEquals(metadata["source_graph"],proof["source_graph"])||(int?)metadata["context"]?["seed"]!=104732||(uint?)metadata["context"]?["setup_hash"]!=2394754492u||(bool?)metadata["mission_ready"]!=false||(int?)graph["schema_version"]!=1)throw new InvalidDataException("Independent S003 metadata and graph binding differs");
            var captureLogDescriptor=capture["editor_log"] as JObject;ExactDescriptor(captureLogDescriptor,"independent S003 capture log");
            string captureLog=Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)captureLogDescriptor["path"],(string)captureLogDescriptor["sha256"],64*1024*1024));
            string captureMarker="[S003RefreshedPrefabReference] result=Collected roots=19 entities=647 unsupported=32 world=0 readiness=0";
            string metadataLog=Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)proof["metadata_log"]["path"],(string)proof["metadata_log"]["sha256"],64*1024*1024));
            string metadataMarker="[SkirmishS003MetadataCapture] result=Passed catalog=S003 seed=104732 scope=DataOnly native_context_qualified=false mission_ready=false";
            if(captureLog.Split('\n').Count(line=>line.TrimEnd('\r').EndsWith(captureMarker,StringComparison.Ordinal))!=1||!captureLog.Contains("Game.Editor.SkirmishBakedActorSnapshotExporter.RunS003RefreshedPrefabReferenceData")||metadataLog.Split('\n').Count(line=>line.TrimEnd('\r').EndsWith(metadataMarker,StringComparison.Ordinal))!=1||(string)metadataAudit["log_sha256"]!=(string)proof["metadata_log"]["sha256"]||(string)capture["independent_metadata_audit_sha256"]!=(string)proof["metadata_audit"]["sha256"])throw new InvalidDataException("Independent capture logs or metadata audit linkage changed");
            string root=Path.GetFullPath(Directory.GetCurrentDirectory()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);var pins=new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);var capturedPins=domain["source_pins"] as JObject;var sourcePins=importer["source_pins"] as JObject;
            if(capturedPins==null||sourcePins==null||(int?)capture["source_pin_entries_verified"]!=capturedPins.Count||(int?)importer["source_pins_rechecked"]!=sourcePins.Count)throw new InvalidDataException("Independent capture and importer source closures required");
            string actualImporter=Path.Combine(root,"Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs");
            if((string)importer["compiled_importer"]!=actualImporter||sourcePins[actualImporter]?.Type!=JTokenType.String)throw new InvalidDataException("Actual S003 importer source pin required");
            if(!derivedS003){foreach(var pin in capturedPins.Properties())SourceConfigurationQualification.AdmitCurrentS003CapturePin(pins,pin.Name,root,(string)pin.Value);
            foreach(var pin in sourcePins.Properties())AutonomousWorld.PinnedFile(pin.Name,(string)pin.Value,128*1024*1024);}
            if((string)domain["mission_id"]!="S003"||(int?)domain["compiler_seed"]!=104732||(uint?)domain["compiler_setup_hash"]!=2394754492u||(bool?)domain["mission_ready"]!=false||(bool?)domain["source_fit_certified"]!=false||(bool?)domain["native_context_qualified"]!=false||(int?)manifest["unsupported_values_preserved"]!=32||(bool?)manifest["world_created"]!=false)throw new InvalidDataException("Independent S003 source-only scope changed");
            ValidateUnsupportedVfxOnly((JArray)domain["unsupported_values"]);
        }

        private static JObject ReadProof(JObject proof,string name,int limit)
        {
            var d=(JObject)proof[name];return Parse(AutonomousWorld.PinnedFile((string)d["path"],(string)d["sha256"],limit));
        }

        private static void ValidateRefreshedS003Proof(JObject descriptor,JObject manifest,JObject domain,string sourcePath,string sourceSha,JObject expectedCaptureGraph)
        {
            var proof=descriptor["refreshed_capture_proof"] as JObject;string[] names={"schema_version","kind","source_graph","capture_receipt","importer_control_receipt","importer_build","importer_control_log"};
            if(proof==null||proof.Count!=names.Length||proof.Properties().Any(p=>!names.Contains(p.Name))||(int?)proof["schema_version"]!=1||(string)proof["kind"]!="S003RefreshedFocusedTemplateCaptureQualification"||!JToken.DeepEquals(proof["source_graph"],expectedCaptureGraph)||!JToken.DeepEquals(manifest["refreshed_capture_proof"],proof))throw new InvalidDataException("Exact refreshed S003 proof and capture graph required");
            foreach(string name in names.Skip(2)){var d=proof[name] as JObject;ExactDescriptor(d,"refreshed S003 "+name);AutonomousWorld.PinnedFile((string)d["path"],(string)d["sha256"],16*1024*1024);}
            if(sourceSha!="5b54cf049e12aad9f360433991690486c25a9e9cd9b1e6393cf066e01383d0f2"||(string)proof["capture_receipt"]["sha256"]!="f309192cd8fa2aaebbfa85f6ac23909a7f78b35312952515c432566132f8a23b"||(string)proof["importer_control_receipt"]["sha256"]!="658ff82e3aebcd273f3fe0c24b3095deed94245238b427852d054a2274c0003f"||(string)proof["importer_build"]["sha256"]!="bc31dad97bde678764db446a71731781da6bd4f9b3aa323aa1beaa193ebca0f8"||(string)proof["importer_control_log"]["sha256"]!="9259543ce703646b55fbb9e8d3b799fa82a803a417651c3fc1938bd43b50adf0")throw new InvalidDataException("Exact checked refreshed S003 artifacts required");
            var capture=Parse(AutonomousWorld.PinnedFile((string)proof["capture_receipt"]["path"],(string)proof["capture_receipt"]["sha256"],4*1024*1024));var control=Parse(AutonomousWorld.PinnedFile((string)proof["importer_control_receipt"]["path"],(string)proof["importer_control_receipt"]["sha256"],4*1024*1024));var build=Parse(AutonomousWorld.PinnedFile((string)proof["importer_build"]["path"],(string)proof["importer_build"]["sha256"],16*1024*1024));
            if((string)capture["status"]!="PassedDataOnlyCapture"||(int?)capture["wrapper_exit_code"]!=0||(bool?)capture["required_marker_present"]!=true||(string)capture["capture"]?["path"]!=sourcePath||(string)capture["capture"]?["sha256"]!=sourceSha||(int?)capture["roots"]!=19||(int?)capture["entities"]!=647||(int?)capture["unsupported_presentation_values"]!=32||(bool?)capture["import_admitted"]!=false||(bool?)capture["mission_ready"]!=false||(string)control["status"]!="PassedDisposableImporterControl"||(string)control["capture_sha256"]!=sourceSha||(string)control["capture_receipt_sha256"]!=(string)proof["capture_receipt"]["sha256"]||(string)control["build_sha256"]!=(string)proof["importer_build"]["sha256"]||(bool?)control["movement_projection_compared"]!=true||(bool?)control["world_allocated"]!=false||(bool?)control["runtime_admission"]!=false||(bool?)control["mission_ready"]!=false||(string)build["status"]!="Passed"||(string)build["mission"]!="S003")throw new InvalidDataException("Refreshed S003 capture/importer scope mismatch");
            string root=Path.GetFullPath(Directory.GetCurrentDirectory()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);string importer=Path.Combine(root,"Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs");string importerSha=(string)build["source_and_reference_pins"]?[importer];if(string.IsNullOrEmpty(importerSha))throw new InvalidDataException("Importer source pin missing");AutonomousWorld.PinnedFile(importer,importerSha,4*1024*1024);
            var pins=new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);var map=domain["source_pins"] as JObject;if(map==null||(int?)capture["source_pin_entries_verified"]!=map.Count)throw new InvalidDataException("Refreshed capture pin closure missing");foreach(var pin in map.Properties())SourceConfigurationQualification.AdmitCurrentS003CapturePin(pins,pin.Name,root,(string)pin.Value);
            if((string)domain["mission_id"]!="S003"||(int?)domain["compiler_seed"]!=104732||(uint?)domain["compiler_setup_hash"]!=2394754492u||(bool?)domain["replay_ready"]!=false||(bool?)domain["mission_ready"]!=false||(bool?)domain["source_fit_certified"]!=false||(bool?)domain["native_context_qualified"]!=false||(int?)manifest["unsupported_values_preserved"]!=32||(bool?)manifest["world_created"]!=false)throw new InvalidDataException("Refreshed source-only capture scope changed");ValidateUnsupportedVfxOnly((JArray)domain["unsupported_values"]);
        }

        private static void ValidateCurrentS003Proof(JObject descriptor,JObject manifest,JObject domain,
            string sourcePath,string sourceSha,JObject expectedCaptureGraph)
        {
            var proof=descriptor["current_capture_proof"] as JObject;
            string[] proofFields={"schema_version","kind","source_graph","strict_capture_receipt","importer_control_receipt"};
            if(proof==null||proof.Properties().Count()!=proofFields.Length||proof.Properties().Any(p=>!proofFields.Contains(p.Name))||
                (int?)proof["schema_version"]!=1||(string)proof["kind"]!="S003FocusedTemplateCaptureQualification")
                throw new InvalidDataException("Exact current S003 focused-capture proof bundle required");
            var graphDescriptor=proof["source_graph"] as JObject;
            var strictDescriptor=proof["strict_capture_receipt"] as JObject;
            var importerDescriptor=proof["importer_control_receipt"] as JObject;
            ExactDescriptor(graphDescriptor,"current S003 source graph");
            ExactDescriptor(strictDescriptor,"current S003 strict capture receipt");
            ExactDescriptor(importerDescriptor,"current S003 importer control receipt");
            if(expectedCaptureGraph==null||!JToken.DeepEquals(graphDescriptor,expectedCaptureGraph))
                throw new InvalidDataException("Focused prefab proof graph differs from validated qualification capture graph");
            var graph=Parse(AutonomousWorld.PinnedFile((string)graphDescriptor["path"],(string)graphDescriptor["sha256"],16*1024*1024));
            var strict=Parse(AutonomousWorld.PinnedFile((string)strictDescriptor["path"],(string)strictDescriptor["sha256"],4*1024*1024));
            if((string)strict["kind"]!="S003FocusedPrefabCaptureReceipt"||
                (string)strict["validation_status"]!="PassedDiagnosticSourceOnly"||
                (string)strict["capture_path"]!=sourcePath||(string)strict["capture_sha256"]!=sourceSha||
                (string)strict["graph_sha256"]!=(string)graphDescriptor["sha256"]||
                (int?)strict["seed"]!=104732||(uint?)strict["setup_hash"]!=2394754492u||
                (int?)strict["root_count"]!=19||(int?)strict["entity_count"]!=647||
                (int?)strict["source_pin_count"]!=((JObject)domain["source_pins"]).Count||
                (int?)strict["unsupported_count"]!=32||(int?)strict["unclassified_unsupported_count"]!=0||
                (bool?)strict["unsupported_values_preserved_verbatim"]!=true||
                (int?)strict["required_marker_count"]!=1||(int?)strict["wrapper_exit_code"]!=0||
                (bool?)strict["utility_world_created"]!=true||(bool?)strict["utility_world_disposed"]!=true||
                (bool?)strict["mission_world_created"]!=false||(bool?)strict["replay_ready"]!=false||
                (bool?)strict["native_context_qualified"]!=false||(bool?)strict["source_fit_certified"]!=false||
                (bool?)strict["mission_ready"]!=false)
                throw new InvalidDataException("Current S003 strict capture receipt does not prove the exact source-only target");
            // Visual readiness belongs to the byte-identical projected capture domain; the strict summary receipt has no such field.
            if((string)domain["mission_id"]!="S003"||(int?)domain["compiler_seed"]!=104732||
                (uint?)domain["compiler_setup_hash"]!=2394754492u||(string)domain["diagnostic_status"]!="CollectedSourceOnly"||
                ((JArray)domain["requested_prefab_keys"])?.Count!=19||((JObject)domain["opening_prefabs"])?.Count!=19||
                ((JArray)domain["entities"])?.Count!=647||((JArray)domain["unsupported_values"])?.Count!=32||
                (string)domain["requested_prefab_keys_sha256"]!=(string)strict["key_descriptor_sha256"]||
                (string)domain["source_key_descriptor_receipt_sha256"]!=(string)strict["key_descriptor_receipt_sha256"]||
                (string)domain["native_metadata_capture_sha256"]!=(string)strict["native_metadata_sha256"]||
                (string)domain["native_metadata_capture_receipt_sha256"]!=(string)strict["native_metadata_receipt_sha256"]||
                (bool?)domain["unsupported_values_preserved_verbatim"]!=true||
                (bool?)domain["utility_world_created"]!=true||(bool?)domain["utility_world_disposed"]!=true||
                (bool?)domain["mission_world_created"]!=false||(bool?)domain["replay_ready"]!=false||
                (bool?)domain["full_mission_initialized"]!=false||(bool?)domain["native_visual_readiness"]!=false||
                (bool?)domain["source_fit_certified"]!=false||(bool?)domain["native_context_qualified"]!=false||
                (bool?)domain["mission_ready"]!=false||(bool?)domain["geometry_exported"]!=false)
                throw new InvalidDataException("Current S003 focused capture scope or target changed");
            if((int?)graph["schema_version"]!=1||!(graph["source_pins"] is JObject)||!(graph["nodes"] is JArray))
                throw new InvalidDataException("Current S003 source graph closure is malformed");
            ValidateUnsupportedVfxOnly((JArray)domain["unsupported_values"]);
            if(!JToken.DeepEquals(manifest["current_capture_proof"],proof)||
                (string)manifest["consumer_read_domain"]!="ExactTypedPrefabClosureOnly"||
                (int?)manifest["unsupported_values_preserved"]!=32||(bool?)manifest["world_created"]!=false)
                throw new InvalidDataException("Current S003 exact projection manifest does not bind its qualification proof");
            ValidateCurrentImporterControl(importerDescriptor,strictDescriptor,sourcePath,sourceSha,domain,strict);
        }

        private static void ValidateCurrentImporterControl(JObject descriptor,JObject strictDescriptor,string sourcePath,string sourceSha,JObject domain,JObject strict)
        {
            var receipt=Parse(AutonomousWorld.PinnedFile((string)descriptor["path"],(string)descriptor["sha256"],8*1024*1024));
            var capture=receipt["capture"] as JObject;
            if((string)receipt["kind"]!="S003FocusedTemplateSourceHostControlsReceipt"||
                (string)receipt["status"]!="Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly"||
                capture==null||(string)capture["path"]!=sourcePath||(string)capture["sha256"]!=sourceSha||
                (string)capture["strict_receipt_path"]!=(string)strictDescriptor["path"]||
                (string)capture["strict_receipt_sha256"]!=(string)strictDescriptor["sha256"]||
                (int?)receipt["raw_capture_source_pin_count"]!=((int?)strict["source_pin_count"]??0))
                throw new InvalidDataException("Current S003 shared-importer receipt does not bind the strict capture");
            var keys=receipt["key_descriptor"] as JObject;
            if(keys==null||keys["path"]?.Type!=JTokenType.String||keys["receipt_path"]?.Type!=JTokenType.String||
                !RegexHash((string)keys["sha256"])||!RegexHash((string)keys["receipt_sha256"])||
                (string)keys["sha256"]!=(string)strict["key_descriptor_sha256"]||
                (string)keys["receipt_sha256"]!=(string)strict["key_descriptor_receipt_sha256"]||
                !JToken.DeepEquals(keys["keys"],domain["requested_prefab_keys"]))
                throw new InvalidDataException("Current S003 ordered prefab keys differ from strict capture identity");
            JToken keyDocument=ParseToken(AutonomousWorld.PinnedFile((string)keys["path"],(string)keys["sha256"],1024*1024));
            JObject keyReceipt=Parse(AutonomousWorld.PinnedFile((string)keys["receipt_path"],(string)keys["receipt_sha256"],1024*1024));
            if((string)keyReceipt["kind"]!="S003FocusedPrefabKeyDescriptorReceipt"||
                (string)keyReceipt["status"]!="PassedSourceOnly"||
                (string)keyReceipt["descriptor"]?["sha256"]!=(string)keys["sha256"]||
                !JToken.DeepEquals(keyDocument,domain["requested_prefab_keys"])||
                !JToken.DeepEquals(keyReceipt["descriptor"]?["keys_in_metadata_order"],domain["requested_prefab_keys"]))
                throw new InvalidDataException("Current S003 key descriptor receipt is not bound to ordered capture keys");
            var imported=receipt["import_result"] as JObject;
            if(imported==null||(string)imported["kind"]!="S003FocusedTemplateSourceHostImportControl"||
                (string)imported["status"]!="Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly"||
                (int?)imported["roots_imported"]!=19||(int?)imported["entities_imported"]!=647||
                (int?)imported["components_imported"]!=3965||(int?)imported["unsupported_leaf_references_in_source"]!=32||
                (int?)imported["omitted_presentation_components"]!=26||
                (int?)imported["preflight"]?["roots"]!=19||(int?)imported["preflight"]?["entities"]!=647||
                (int?)imported["preflight"]?["decoded_components"]!=3965||
                (int?)imported["preflight"]?["unsupported_unconsumed_values"]!=32||
                (bool?)imported["preflight_no_identity_allocation"]!=true||
                (bool?)imported["unsupported_non_vfx_rejected_before_allocation"]!=true||
                (bool?)imported["foreign_required_key_rejected_before_allocation"]!=true||
                (bool?)imported["inconsistent_disabled_state_rejected_before_allocation"]!=true||
                (bool?)imported["partial_identity_allocation_rollback"]!=true||
                (bool?)imported["successful_import_cleanup"]!=true||
                (bool?)imported["mission_initialized"]!=false||
                (bool?)receipt["unity_world_created"]!=false||(bool?)receipt["mission_manager_created"]!=false||
                (bool?)receipt["source_authority_rewritten"]!=false||(bool?)receipt["runtime_replay_ready"]!=false||
                (bool?)receipt["native_visual_readiness"]!=false||(bool?)receipt["source_fit_certified"]!=false||
                (bool?)receipt["native_context_qualified"]!=false||(bool?)receipt["mission_ready"]!=false)
                throw new InvalidDataException("Current S003 shared-importer control facts changed");
            string[] expectedPresentation={"Game.Components.UnitAttackImpactVfxReference","Game.Components.UnitMuzzleFlashVfxReference",
                "Game.Components.GroundMissileLauncherVfxReferenceComponent","Game.Components.AirMissileLauncherVfxReferenceComponent"};
            var presentation=imported["excluded_presentation_component_types"] as JArray;
            if(presentation==null||presentation.Count!=expectedPresentation.Length||
                !new System.Collections.Generic.HashSet<string>(presentation.Values<string>(),StringComparer.Ordinal).SetEquals(expectedPresentation))
                throw new InvalidDataException("Current S003 importer presentation omission set changed");
            var sourceBuild=receipt["source_build"] as JObject;
            if(sourceBuild==null||sourceBuild["path"]?.Type!=JTokenType.String||!RegexHash((string)sourceBuild["sha256"]))
                throw new InvalidDataException("Current S003 source-host build descriptor is incomplete");
            var build=Parse(AutonomousWorld.PinnedFile((string)sourceBuild["path"],(string)sourceBuild["sha256"],16*1024*1024));
            var pins=build["source_and_reference_pins"] as JObject;
            string importerPath=(string)sourceBuild["importer_path"],importerSha=(string)sourceBuild["importer_sha256"];
            if((string)build["kind"]!="SkirmishSourceSimulationBuild"||(string)build["status"]!="Passed"||
                (string)build["mission"]!="S003"||(int?)build["compile_exit"]!=0||(bool?)build["native_or_player_launched"]!=false||pins==null||
                (int?)sourceBuild["pin_count"]!=pins.Count||!Path.IsPathRooted(importerPath)||
                !importerPath.Replace('\\','/').EndsWith("/Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs",StringComparison.Ordinal)||
                (string)pins[importerPath]!=importerSha||!RegexHash(importerSha)||
                Hash(AutonomousWorld.PinnedFile(importerPath,importerSha,4*1024*1024))!=importerSha)
                throw new InvalidDataException("Current S003 importer proof is not bound to a passed live source build");
            foreach(var pin in pins.Properties())
            {
                string pinPath=pin.Name,pinSha=(string)pin.Value;
                if(!Path.IsPathRooted(pinPath)||Path.GetFullPath(pinPath)!=pinPath||!RegexHash(pinSha))
                    throw new InvalidDataException("Canonical current S003 source-build pin required: "+pinPath);
                AutonomousWorld.PinnedFile(pinPath,pinSha,64*1024*1024);
            }
            var response=build["compiler_response"] as JObject;var sourceExe=build["executable"] as JObject;
            if(response==null||!RegexHash((string)response["sha256"])||sourceExe==null||!RegexHash((string)sourceExe["sha256"])||
                (string)sourceBuild["source_response_sha256"]!=(string)response["sha256"]||
                (string)sourceBuild["executable_sha256"]!=(string)sourceExe["sha256"])
                throw new InvalidDataException("Current S003 source-build compiler response/executable identity differs");
            string responsePath=(string)response["path"],sourceExePath=(string)sourceExe["path"];
            string responseText=Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(responsePath,(string)response["sha256"],16*1024*1024));
            AutonomousWorld.PinnedFile(sourceExePath,(string)sourceExe["sha256"],128*1024*1024);
            int importerMentions=responseText.Split(new[]{importerPath},StringSplitOptions.None).Length-1;
            if(importerMentions!=1)throw new InvalidDataException("Current S003 source build must compile the pinned importer exactly once");
            var executable=receipt["control_executable"] as JObject;
            ExactDescriptor(executable,"current S003 importer control executable");
            string exePath=(string)executable["path"];
            if((string)sourceBuild["executable_sha256"]!=(string)build["executable"]?["sha256"]||
                (string)sourceBuild["source_response_sha256"]!=(string)build["compiler_response"]?["sha256"])
                throw new InvalidDataException("Current S003 importer control/build outputs differ");
            AutonomousWorld.PinnedFile(exePath,(string)executable["sha256"],64*1024*1024);
            string controlDirectory=Path.GetDirectoryName(exePath);
            RequireAdjacentHash(controlDirectory,"compile.rsp",(string)receipt["control_compile_response_sha256"]);
            RequireAdjacentHash(controlDirectory,"compiler.log",(string)receipt["control_compile_log_sha256"]);
            RequireAdjacentHash(controlDirectory,"run.log",(string)receipt["control_run_log_sha256"]);
            string controlPath=Path.Combine(controlDirectory,"control.json");var controlInfo=new FileInfo(controlPath);
            if(!controlInfo.Exists||controlInfo.Length<1||controlInfo.Length>4*1024*1024||(controlInfo.Attributes&FileAttributes.ReparsePoint)!=0)
                throw new InvalidDataException("Current S003 importer control output must be a bounded regular file");
            JObject control=Parse(File.ReadAllBytes(controlPath));
            if(!JToken.DeepEquals(control,imported))throw new InvalidDataException("Current S003 importer control JSON differs from its receipt");
            string runLog=File.ReadAllText(Path.Combine(controlDirectory,"run.log"),Encoding.UTF8);
            string marker="[S003FocusedTemplateSourceHostControls] result=Passed roots=19 entities=647 components=3965 unsupportedLeaves=32 omittedVfxComponents=26 cleanup=Passed preflightRejected=2 world=0 mission=0 readiness=0";
            if(runLog.Split(new[]{marker},StringSplitOptions.None).Length!=2)throw new InvalidDataException("Current S003 importer pass marker is missing or duplicated");
        }

        private static void ValidateUnsupportedVfxOnly(JArray values)
        {
            string objectRef="Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]";
            var allowed=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal){
                "Game.Components.UnitAttackImpactVfxReference/Prefab","Game.Components.UnitMuzzleFlashVfxReference/Prefab",
                "Game.Components.AirMissileLauncherVfxReferenceComponent/AirTargetImpactPrefab",
                "Game.Components.AirMissileLauncherVfxReferenceComponent/AirburstExplosionPrefab",
                "Game.Components.AirMissileLauncherVfxReferenceComponent/InterceptExplosionPrefab",
                "Game.Components.AirMissileLauncherVfxReferenceComponent/LaunchFlashPrefab",
                "Game.Components.AirMissileLauncherVfxReferenceComponent/LaunchSmokePrefab",
                "Game.Components.AirMissileLauncherVfxReferenceComponent/MissileTrailPrefab",
                "Game.Components.AirMissileLauncherVfxReferenceComponent/MissileVisualPrefab",
                "Game.Components.GroundMissileLauncherVfxReferenceComponent/ExplosionPrefab",
                "Game.Components.GroundMissileLauncherVfxReferenceComponent/LaunchFlashPrefab",
                "Game.Components.GroundMissileLauncherVfxReferenceComponent/LaunchSmokePrefab",
                "Game.Components.GroundMissileLauncherVfxReferenceComponent/MissileTrailPrefab",
                "Game.Components.GroundMissileLauncherVfxReferenceComponent/MissileVisualPrefab"};
            var seen=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach(var token in values)
            {
                var row=token as JObject;string path=(string)row?["path"],reason=(string)row?["reason"],type=(string)row?["type"];
                int slash=path?.IndexOf('/')??-1;string componentField=slash<0?"":path.Substring(slash+1);
                if(row==null||!allowed.Contains(componentField)||!seen.Add(path)||reason!="unity_object_ref_requires_bound_source_identity"||type!=objectRef)
                    throw new InvalidDataException("Current S003 unsupported value is outside exact unresolved VFX reference scope");
            }
            if(values.Count!=32)throw new InvalidDataException("Current S003 exact unsupported VFX reference count changed");
        }

        private static void ExactDescriptor(JObject value,string label)
        {
            if(value==null||value.Properties().Count()!=2||value["path"]?.Type!=JTokenType.String||
                value["sha256"]?.Type!=JTokenType.String||!RegexHash((string)value["sha256"]))
                throw new InvalidDataException("Exact path/hash descriptor required: "+label);
        }
        private static bool RegexHash(string value)=>value!=null&&value.Length==64&&value.All(c=>(c>='0'&&c<='9')||(c>='a'&&c<='f'));
        private static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        private static JToken ParseToken(byte[] bytes)=>JToken.Parse(Encoding.UTF8.GetString(bytes),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
        private static void RequireAdjacentHash(string directory,string name,string expected)
        {
            string path=Path.Combine(directory,name);if(!RegexHash(expected))throw new InvalidDataException("Current S003 importer control artifact hash missing: "+name);AutonomousWorld.PinnedFile(path,expected,64*1024*1024);
        }

        private static JObject Parse(byte[] bytes) => JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
    }
}
