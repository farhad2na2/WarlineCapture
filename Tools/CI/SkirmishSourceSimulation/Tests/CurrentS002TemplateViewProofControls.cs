using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Executable source-host controls for the additive current-S002 proof branch.
    internal static class CurrentS002TemplateViewProofControls
    {
        [DllImport("libc", EntryPoint="symlink", SetLastError=true)]
        private static extern int CreateSymlink(string target,string linkPath);

        internal static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length != 2) throw new ArgumentException("Current S002 descriptor and host source root required");
                JObject result = Run(args[0],args[1]);
                Console.WriteLine(result.ToString(Newtonsoft.Json.Formatting.None));
                Console.WriteLine("[CurrentS002TemplateViewProofControls] result=Passed positive=1 negative=" + (int)result["negative_count"] + " missionReady=False");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("[CurrentS002TemplateViewProofControls] result=Failed " + exception);
                return 1;
            }
        }

        internal static JObject Run(string descriptorPath,string hostSourceRepositoryRoot)
        {
            JObject descriptor = JObject.Parse(File.ReadAllText(descriptorPath), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            string binaryDirectory;
            JObject positive = GeneratedReferenceTemplateView.Read(descriptor, out binaryDirectory,hostSourceRepositoryRoot);
            Need((string)positive["kind"] == "AuthoredSkirmishPrefabReferenceData" &&
                 (int?)((JArray)positive["unsupported_values"]).Count == 14 &&
                 (bool?)positive["full_mission_initialized"] == false &&
                 (bool?)positive["replay_ready"] == false &&
                 (bool?)descriptor["mission_ready"] == false,
                 "Current S002 positive proof control failed");
            JObject manifest = JObject.Parse(File.ReadAllText((string)descriptor["projection_manifest_path"]), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            JObject original = JObject.Parse(File.ReadAllText((string)descriptor["source_reference_path"]), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });

            JObject wrongKind = (JObject)descriptor.DeepClone();
            wrongKind["current_capture_proof"]["kind"] = "S003FocusedTemplateCaptureQualification";
            NeedRejectProof(wrongKind, manifest, positive, original, hostSourceRepositoryRoot,
                "S003 proof cannot enter current S002 branch");

            JObject promoted = (JObject)descriptor.DeepClone();
            promoted["current_capture_proof"]["mission_ready"] = true;
            NeedRejectProof(promoted, manifest, positive, original, hostSourceRepositoryRoot,
                "Readiness promotion must reject");

            JObject pins = (JObject)original["source_pins"];
            JObject proof = (JObject)descriptor["current_capture_proof"];
            string keyPath = (string)proof["key_descriptor_path"];
            var ordinaryPin = pins.Properties().First(p => p.Name != keyPath);
            JObject changedPins = (JObject)pins.DeepClone();
            changedPins[ordinaryPin.Name] = new string('0', 64);
            NeedRejectPins(changedPins,hostSourceRepositoryRoot,keyPath,
                (string)proof["key_descriptor_sha256"],(int)proof["source_pin_count"]);

            JObject wrongKeyPin = (JObject)pins.DeepClone();
            wrongKeyPin[keyPath] = new string('0', 64);
            NeedRejectPins(wrongKeyPin,hostSourceRepositoryRoot,keyPath,
                (string)proof["key_descriptor_sha256"],(int)proof["source_pin_count"]);

            RunRootBoundaryControls(keyPath,(string)proof["key_descriptor_sha256"]);
            RunHistoricalLocatorControls(hostSourceRepositoryRoot);
            NeedRejectCapture("/private/tmp/s002-external-capture.json",hostSourceRepositoryRoot);
            bool refreshed=((string)proof["export_capture_path"]).StartsWith(hostSourceRepositoryRoot+Path.DirectorySeparatorChar,StringComparison.Ordinal);
            if(refreshed)
            {
                NeedRejectExport((string)proof["export_capture_path"],new string('0',64),(string)proof["capture_path"],hostSourceRepositoryRoot);
                NeedRejectExport((string)proof["export_capture_path"],(string)proof["export_capture_sha256"],Path.Combine(hostSourceRepositoryRoot,"other.json"),hostSourceRepositoryRoot);
            }
            return new JObject
            {
                ["kind"] = "CurrentS002TemplateViewProofControls",
                ["status"] = "Passed",
                ["negative_count"] = refreshed ? 15 : 13,
                ["positive_actual_view_read"] = true,
                ["wrong_s003_proof_rejected"] = true,
                ["readiness_promotion_rejected"] = true,
                ["changed_source_pin_rejected"] = true,
                ["changed_external_key_pin_rejected"] = true,
                ["wrong_root_rejected"] = true,
                ["parent_symlink_rejected"] = true,
                ["inside_root_key_rejected"] = true,
                ["historical_export_locator_inside_root_rejected"] = true,
                ["historical_export_locator_relative_rejected"] = true,
                ["historical_export_locator_noncanonical_rejected"] = true,
                ["relative_pin_parent_traversal_rejected"] = true,
                ["additional_absolute_external_pin_rejected"] = true,
                ["external_capture_rejected"] = true,
                ["mission_ready"] = false,
                ["replay_ready"] = false,
                ["native_visual_readiness"] = false
            };
        }

        private static void NeedRejectExport(string exportPath,string sha,string capturePath,string root)
        {
            try { GeneratedReferenceTemplateView.RequireS002ExportLocator(exportPath,sha,capturePath,root); }
            catch (InvalidDataException) { return; }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("Invalid current S002 export was accepted");
        }

        private static void NeedRejectCapture(string capturePath,string sourceRoot)
        {
            try { GeneratedReferenceTemplateView.RequireCurrentS002CapturePath(capturePath,sourceRoot); }
            catch (InvalidDataException) { return; }
            throw new InvalidOperationException("External current S002 capture artifact was accepted");
        }

        private static void RunHistoricalLocatorControls(string sourceRoot)
        {
            string missingExternal=Path.Combine("/private/tmp","historical-s002-export-"+Guid.NewGuid().ToString("N"),"capture.json");
            GeneratedReferenceTemplateView.RequireHistoricalS002ExportLocator(missingExternal,sourceRoot);
            NeedRejectHistoricalLocator(Path.Combine(sourceRoot,"Assets","capture.json"),sourceRoot);
            NeedRejectHistoricalLocator("relative/capture.json",sourceRoot);
            NeedRejectHistoricalLocator("/private/tmp/s002-locator/../capture.json",sourceRoot);
        }

        private static void NeedRejectHistoricalLocator(string path,string sourceRoot)
        {
            try { GeneratedReferenceTemplateView.RequireHistoricalS002ExportLocator(path,sourceRoot); }
            catch (InvalidDataException) { return; }
            throw new InvalidOperationException("Invalid historical S002 exporter locator was accepted: "+path);
        }

        private static void RunRootBoundaryControls(string externalKeyPath,string keySha)
        {
            string temp=Path.Combine("/private/tmp","s002-current-proof-controls-"+Guid.NewGuid().ToString("N"));
            string root=Path.Combine(temp,"trusted"),wrong=Path.Combine(temp,"wrong");
            Directory.CreateDirectory(Path.Combine(root,"Assets"));Directory.CreateDirectory(Path.Combine(root,"Packages"));Directory.CreateDirectory(Path.Combine(root,"ProjectSettings"));
            Directory.CreateDirectory(Path.Combine(wrong,"Assets"));Directory.CreateDirectory(Path.Combine(wrong,"Packages"));Directory.CreateDirectory(Path.Combine(wrong,"ProjectSettings"));
            string realDir=Path.Combine(root,"Assets","real"),linkDir=Path.Combine(root,"Assets","linked");Directory.CreateDirectory(realDir);
            string realPin=Path.Combine(realDir,"pin.txt");File.WriteAllText(realPin,"current source pin");
            string realSha=AutonomousWorld.Hash(File.ReadAllBytes(realPin));
            JObject positive=new JObject{{realPin,realSha}};
            for(int i=1;i<1000;i++)
            {
                string relative="Assets/pins/"+i.ToString("D4")+".txt";
                string pin=Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(pin));File.WriteAllText(pin,"isolated source pin "+i);
                positive[relative]=AutonomousWorld.Hash(File.ReadAllBytes(pin));
            }
            positive[externalKeyPath]=keySha;
            int expectedCount=1001;
            try
            {
                GeneratedReferenceTemplateView.VerifyCurrentS002SourcePins(positive,root,externalKeyPath,keySha,expectedCount);
                NeedRejectPins(positive,wrong,externalKeyPath,keySha,expectedCount);
                if(CreateSymlink(realDir,linkDir)!=0)throw new IOException("Could not create isolated parent-symlink negative control; errno="+Marshal.GetLastWin32Error());
                JObject symlinkPins=(JObject)positive.DeepClone();
                symlinkPins.Remove(realPin);symlinkPins[Path.Combine(linkDir,"pin.txt")]=realSha;
                NeedRejectPins(symlinkPins,root,externalKeyPath,keySha,expectedCount);
                string insideKey=Path.Combine(root,"Assets","external-keys.json");
                File.Copy(externalKeyPath,insideKey);
                JObject insidePins=(JObject)positive.DeepClone();insidePins.Remove(externalKeyPath);insidePins[insideKey]=keySha;
                NeedRejectPins(insidePins,root,insideKey,keySha,expectedCount);
                JObject traversalPins=(JObject)positive.DeepClone();traversalPins.Remove("Assets/pins/0001.txt");
                traversalPins["Assets/../Assets/pins/0001.txt"]=positive["Assets/pins/0001.txt"].DeepClone();
                NeedRejectPins(traversalPins,root,externalKeyPath,keySha,expectedCount);
                string unapprovedExternal=Path.Combine("/private/tmp","unapproved-s002-pin-"+Guid.NewGuid().ToString("N"));
                JObject externalPins=(JObject)positive.DeepClone();externalPins[unapprovedExternal]=realSha;
                NeedRejectPins(externalPins,root,externalKeyPath,keySha,expectedCount+1);
            }
            finally
            {
                if(Directory.Exists(linkDir))Directory.Delete(linkDir,false);
                if(Directory.Exists(temp))Directory.Delete(temp,true);
            }
        }

        private static void NeedRejectProof(JObject descriptor,JObject originalManifest,JObject domain,
            JObject originalCapture,string hostRoot,string message)
        {
            try
            {
                JObject manifest=(JObject)originalManifest.DeepClone();
                manifest["current_capture_proof"]=descriptor["current_capture_proof"].DeepClone();
                GeneratedReferenceTemplateView.ValidateCurrentS002Proof(descriptor,manifest,domain,originalCapture,
                    (string)descriptor["source_reference_path"],(string)descriptor["source_reference_sha256"],hostRoot);
            }
            catch (InvalidDataException) { return; }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException(message);
        }

        private static void NeedRejectPins(JObject pins,string projectRoot,string keyPath,string keySha,int count)
        {
            try { GeneratedReferenceTemplateView.VerifyCurrentS002SourcePins(pins,projectRoot,keyPath,keySha,count); }
            catch (InvalidDataException) { return; }
            catch (InvalidOperationException) { return; }
            catch (NotSupportedException) { return; }
            throw new InvalidOperationException("Mutated current S002 source pin was accepted");
        }

        private static void Need(bool condition,string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
