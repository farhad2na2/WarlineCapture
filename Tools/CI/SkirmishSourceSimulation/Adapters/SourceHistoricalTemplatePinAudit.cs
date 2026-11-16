using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // The audit describes historical capture inputs, not a current native capture.
    // Only these reviewed gameplay-code changes may differ from that capture;
    // every other source and asset still has to match its original pin.
    internal static class SourceHistoricalTemplatePinAudit
    {
        private static readonly HashSet<string> AllowedCaptureCodeDrift = new HashSet<string>(StringComparer.Ordinal)
        {
            "Assets/Game/Scripts/Composition/Skirmish/SkirmishCheckpointCompositionSystemHelper.cs",
            "Assets/Game/Scripts/Configs/Skirmish/SkirmishPublicationValidator.cs",
            "Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.cs",
            "Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs",
            "Assets/Game/Scripts/Runtime/Skirmish/GroundMissionContextFactory.cs",
            "Assets/Game/Scripts/Simulation/GroundMissionContext.cs",
            "Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs",
            "Assets/Game/Scripts/Systems/BuildingProductionContextCompositionSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingPlacementConstructionTransaction.cs",
            "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
            "Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingRuntimeCreationCompositionSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingRuntimeContextFactoryCompositionSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.Canonical.cs",
            "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs",
            "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs"
        };

        private static JObject ReadPinnedObject(JObject descriptor, long maximum)
        {
            if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
                descriptor["sha256"]?.Type != JTokenType.String ||
                !Regex.IsMatch((string)descriptor["sha256"], "^[0-9a-f]{64}$"))
                throw new InvalidDataException("Exact pinned template evidence descriptor required");
            return JObject.Parse(Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(
                (string)descriptor["path"], (string)descriptor["sha256"], maximum)),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        internal static JObject ValidatePinnedCapture(JObject templateDescriptor, JObject auditDescriptor, string sourceRoot)
        {
            var reference = ReadPinnedObject(templateDescriptor, 4 * 1024 * 1024);
            var audit = ReadPinnedObject(auditDescriptor, 4 * 1024 * 1024);
            if (!JToken.DeepEquals(audit["descriptor"], templateDescriptor) || reference["path"]?.Type != JTokenType.String ||
                reference["sha256"]?.Type != JTokenType.String)
                throw new InvalidDataException("Historical audit must bind the exact template descriptor");
            var domain = ReadPinnedObject(new JObject { ["path"] = reference["path"], ["sha256"] = reference["sha256"] }, 32 * 1024 * 1024);
            var pins = domain["source_pins"] as JObject ?? throw new InvalidDataException("Captured template source pins required");
            ValidateCapture(pins, audit, sourceRoot);
            return domain;
        }

        // The caller must read both capturedPins and audit from independently pinned artifacts.
        internal static void ValidateCapture(JObject capturedPins, JObject audit, string sourceRoot)
        {
            if (capturedPins == null || audit == null || (string)audit["kind"] != "S003TemplateHistoricalPinAudit" ||
                (int?)audit["counts"]?["capture"]?["total"] != capturedPins.Count ||
                (int?)audit["counts"]?["capture"]?["missing"] != 0 || !(audit["differences"] is JArray differences))
                throw new InvalidDataException("Exact historical template capture audit required");
            sourceRoot = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var changed = new Dictionary<string, List<JObject>>(StringComparer.Ordinal);
            foreach (var token in differences)
            {
                if (!(token is JObject row)) throw new InvalidDataException("Invalid historical pin audit entry");
                if ((string)row["closure"] != "capture") continue;
                string path = (string)row["path"];
                if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path) || Path.GetFullPath(path) != path ||
                    !path.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    !AllowedCaptureCodeDrift.Contains(path.Substring(sourceRoot.Length + 1).Replace(Path.DirectorySeparatorChar, '/')) ||
                    (string)row["state"] != "changed" || row["expected_sha256"]?.Type != JTokenType.String ||
                    row["current_sha256"]?.Type != JTokenType.String ||
                    !Regex.IsMatch((string)row["expected_sha256"], "^[0-9a-f]{64}$") ||
                    !Regex.IsMatch((string)row["current_sha256"], "^[0-9a-f]{64}$") ||
                    (string)row["expected_sha256"] == (string)row["current_sha256"])
                    throw new InvalidDataException("Unapproved historical template capture drift");
                if (!changed.TryGetValue(path, out var rows)) changed[path] = rows = new List<JObject>();
                rows.Add(row);
            }
            int declaredCount = (int?)audit["counts"]?["capture"]?["changed"] ?? -1;
            if (declaredCount < 0 || declaredCount > capturedPins.Count || changed.Values.Sum(rows => rows.Count) != declaredCount)
                throw new InvalidDataException("Historical template drift count differs");
            var verified = new Dictionary<string, string>(StringComparer.Ordinal);
            void VerifyOnce(string path, string expected)
            {
                if (verified.TryGetValue(path, out var prior))
                {
                    if (prior != expected) throw new InvalidDataException("Conflicting normalized historical template pin");
                    return;
                }
                AutonomousWorld.PinnedFile(path, expected, 64 * 1024 * 1024);
                verified.Add(path, expected);
            }
            foreach (var pin in capturedPins.Properties())
            {
                if (pin.Value.Type != JTokenType.String || !Regex.IsMatch((string)pin.Value, "^[0-9a-f]{64}$"))
                    throw new InvalidDataException("Historical template source hash required");
                string path = SourceConfigurationQualification.ResolveCurrentS003CapturePin(pin.Name, sourceRoot);
                string capturedHash = (string)pin.Value;
                if (changed.TryGetValue(path, out var rows) && rows.Count != 0)
                {
                    int at = rows.FindIndex(row => (string)row["expected_sha256"] == capturedHash);
                    if (at < 0) throw new InvalidDataException("Historical template drift did not match captured pin");
                    var row = rows[at]; rows.RemoveAt(at);
                    VerifyOnce(path, (string)row["current_sha256"]);
                }
                else VerifyOnce(path, capturedHash);
            }
            if (changed.Values.Any(rows => rows.Count != 0))
                throw new InvalidDataException("Historical template audit lists unconsumed drift");
        }
    }
}
