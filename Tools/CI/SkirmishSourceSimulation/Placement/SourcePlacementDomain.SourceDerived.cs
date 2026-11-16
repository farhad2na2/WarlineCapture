using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using Game.Simulation;

namespace Game.Runtime
{
    public sealed partial class SourcePlacementDomain
    {
        static float DerivedBits(JToken token)
        {
            string text = (string)token;
            if (text == null || text.Length != 8 || !uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint bits))
                throw new InvalidDataException("Exact captured float bits required");
            float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
            if (!math.isfinite(value)) throw new InvalidDataException("Finite captured source value required");
            return value;
        }

        static float3 DerivedVector(JToken row) => new float3(DerivedBits(row?["xBits"]), DerivedBits(row?["yBits"]), DerivedBits(row?["zBits"]));

        static bool[] DerivedMask(JToken token, string name, int count)
        {
            var row = token as JObject;
            if (row == null || (string)row["name"] != name || (bool?)row["present"] != true || (bool?)row["ownerReady"] != true ||
                (int?)row["actualLength"] != count || (int?)row["expectedLength"] != count || (int?)row["count"] != count ||
                (int?)row["byteCount"] != count || (string)row["encoding"] != "uint8-bool01-row-major-xfast")
                throw new InvalidDataException("Complete original source mask required: " + name);
            var bytes = Convert.FromBase64String((string)row["base64"]);
            if (bytes.Length != count || bytes.Any(x => x > 1) || CaptureBytesHash(bytes) != (string)row["sha256"])
                throw new InvalidDataException("Original source mask bytes changed: " + name);
            var mask = new bool[count];
            for (int i = 0; i < count; i++) mask[i] = bytes[i] != 0;
            return mask;
        }

        static bool SupportedDerivedCatalog(string value)
        {
            if (value == null || value.Length != 4 || value[0] != 'S' ||
                !int.TryParse(value.Substring(1), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out int number)) return false;
            return number >= 1 && number <= 120;
        }

        static JObject ReadDerivedControl(string path, string sha)
        {
            var bytes = SourcePackedCampaignGeometry.ReadPinnedFile(path, 8 * 1024 * 1024, sha);
            return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        // Physical exports pin the compiler request and its result as control
        // evidence. They are validated here, then excluded from the map-asset
        // closure; arbitrary T Logs or other non-asset paths remain rejected.
        static HashSet<string> ValidateCompilerSelectionControlEvidence(JObject capture, JObject recordedPins,
            string capturedRoot, string definitionSha, string operationMapId)
        {
            var selection = capture["compilerSelection"] as JObject;
            bool hasControlPins = recordedPins.Properties().Any(pin =>
            {
                string full = Path.IsPathRooted(pin.Name) ? Path.GetFullPath(pin.Name) : null;
                string prefix = Path.GetFullPath(capturedRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string relative = full != null && full.StartsWith(prefix, StringComparison.Ordinal)
                    ? full.Substring(prefix.Length) : (!Path.IsPathRooted(pin.Name) ? pin.Name : null);
                return relative != null && relative.StartsWith("T Logs/", StringComparison.Ordinal);
            });
            if (selection == null)
            {
                if (hasControlPins) throw new InvalidDataException("Unlinked T Logs control pin is not an asset dependency");
                return new HashSet<string>(StringComparer.Ordinal);
            }
            string[] selectionFields = { "path", "sha256", "catalogId", "seed", "operationMapId", "sourceContentHash", "status", "runtimeAdmission" };
            if (selection.Count != selectionFields.Length || selection.Properties().Any(p => !selectionFields.Contains(p.Name)) ||
                selection["path"]?.Type != JTokenType.String || selection["sha256"]?.Type != JTokenType.String ||
                !Digest((string)selection["sha256"]) || (string)selection["status"] != "ConfigurationCompiled" ||
                (string)selection["runtimeAdmission"] != "not claimed" || (string)selection["operationMapId"] != operationMapId ||
                (string)selection["sourceContentHash"] != (string)capture["sourceMetadata"]?["MonoBehaviour"]?["contentHash"])
                throw new InvalidDataException("Exact unqualified compiler selection receipt required");
            string catalogId = (string)selection["catalogId"];
            if (!SupportedDerivedCatalog(catalogId) || selection["seed"]?.Type != JTokenType.Integer ||
                (long)selection["seed"] < 1 || (long)selection["seed"] > int.MaxValue)
                throw new InvalidDataException("Supported compiler-selected catalog and positive bounded seed required");

            string root = Path.GetFullPath(capturedRoot).TrimEnd(Path.DirectorySeparatorChar);
            string prefixRoot = root + Path.DirectorySeparatorChar;
            string resultPath = Path.GetFullPath((string)selection["path"]);
            if (!resultPath.StartsWith(prefixRoot, StringComparison.Ordinal))
                throw new InvalidDataException("Compiler result must be inside the captured project root");
            string[] resultPinNames = recordedPins.Properties().Select(p => p.Name).Where(name =>
            {
                string full = Path.IsPathRooted(name) ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(root, name));
                return full == resultPath;
            }).ToArray();
            if (resultPinNames.Length == 0 || resultPinNames.Any(name => (string)recordedPins[name] != (string)selection["sha256"]))
                throw new InvalidDataException("Compiler result descriptor is not in the capture read pins");
            string resultPinName = resultPinNames.FirstOrDefault(name => Path.IsPathRooted(name)) ?? resultPinNames[0];
            var result = ReadDerivedControl(resultPath, (string)selection["sha256"]);
            if ((int?)result["schema_version"] != 1 || (string)result["kind"] != "SkirmishMissionAuthoringPreflightResult" ||
                (string)result["status"] != "ConfigurationCompiled" || (bool?)result["compiler_accepted"] != true ||
                (string)result["catalog_id"] != catalogId || result["seed"]?.Type != JTokenType.Integer ||
                (long)result["seed"] != (long)selection["seed"] || (string)result["operation_map_id"] != operationMapId ||
                (string)result["source_content_hash"] != (string)selection["sourceContentHash"])
                throw new InvalidDataException("Compiler result map/content identity differs from its receipt");
            foreach (string flag in new[] { "world_allocated", "source_fit_verified", "mission_ready", "native_acceptance", "native_playable", "validation_passed" })
                if (result[flag]?.Type != JTokenType.Boolean || (bool)result[flag])
                    throw new InvalidDataException("Compiler result must preserve false unqualified flag: " + flag);

            var controlNames = new List<string>();
            foreach (var pin in recordedPins.Properties())
            {
                string full = Path.IsPathRooted(pin.Name) ? Path.GetFullPath(pin.Name) : Path.GetFullPath(Path.Combine(root, pin.Name));
                string relative = full.StartsWith(prefixRoot, StringComparison.Ordinal) ? full.Substring(prefixRoot.Length) : null;
                if (relative != null && relative.StartsWith("T Logs/", StringComparison.Ordinal)) controlNames.Add(pin.Name);
            }
            if (!controlNames.Contains(resultPinName))
                throw new InvalidDataException("Selected compiler result must be a captured T Logs control pin");
            var resultInputs = result["input_pins"] as JObject;
            if (resultInputs == null || resultInputs.Count > 4096)
                throw new InvalidDataException("Exact compiler input pin closure required");
            var inputControlPins = new HashSet<string>(StringComparer.Ordinal);
            var compilerInputEvidencePins = new HashSet<string>(StringComparer.Ordinal);
            bool fullInputClosureCaptured = true;
            foreach (var input in resultInputs.Properties())
            {
                var descriptor = input.Value as JObject;
                if (descriptor == null || descriptor.Count != 2 || descriptor.Properties().Any(p => p.Name != "sha256" && p.Name != "maximum_bytes") ||
                    descriptor["sha256"]?.Type != JTokenType.String || !Digest((string)descriptor["sha256"]) ||
                    descriptor["maximum_bytes"]?.Type != JTokenType.Integer || (long)descriptor["maximum_bytes"] < 1 ||
                    (long)descriptor["maximum_bytes"] > 512L * 1024 * 1024)
                    throw new InvalidDataException("Exact compiler input pin descriptor required");
                string inputPath = Path.GetFullPath(Path.IsPathRooted(input.Name) ? input.Name : Path.Combine(root, input.Name));
                if (!inputPath.StartsWith(prefixRoot, StringComparison.Ordinal))
                    throw new InvalidDataException("Compiler input pin must be inside the captured project root");
                SourcePackedCampaignGeometry.ReadPinnedFile(inputPath, (long)descriptor["maximum_bytes"], (string)descriptor["sha256"]);
                string[] recordedNames = recordedPins.Properties().Select(p => p.Name).Where(name =>
                {
                    string full = Path.IsPathRooted(name) ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(root, name));
                    return full == inputPath;
                }).ToArray();
                if (recordedNames.Length == 0)
                {
                    fullInputClosureCaptured = false;
                    continue;
                }
                if (recordedNames.Any(name => (string)recordedPins[name] != (string)descriptor["sha256"]))
                    throw new InvalidDataException("Compiler input pin is missing from the captured read pins or has a different hash");
                foreach (string recordedName in recordedNames) compilerInputEvidencePins.Add(recordedName);
                string inputRelative = inputPath.Substring(prefixRoot.Length);
                if (inputRelative.StartsWith("T Logs/", StringComparison.Ordinal))
                {
                    foreach (string recordedName in recordedNames) inputControlPins.Add(recordedName);
                }
            }
            var physicalRequestPins = controlNames.Where(name =>
            {
                string full = Path.IsPathRooted(name) ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(root, name));
                return full != resultPath && full.StartsWith(prefixRoot + "T Logs/SkirmishProduction/", StringComparison.Ordinal) &&
                    full.EndsWith("/request.json", StringComparison.Ordinal);
            }).Where(name =>
            {
                string full = Path.IsPathRooted(name) ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(root, name));
                var candidate = ReadDerivedControl(full, (string)recordedPins[name]);
                var candidateSelection = candidate["selection"] as JObject;
                return (string)candidate["kind"] == "PinnedCampaignPhysicalInputExport" && candidateSelection != null &&
                    (string)candidateSelection["path"] == resultPath && (string)candidateSelection["sha256"] == (string)selection["sha256"];
            }).ToArray();
            var distinctPhysicalRequestPins = physicalRequestPins.GroupBy(name =>
            {
                return Path.IsPathRooted(name) ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(root, name));
            }, StringComparer.Ordinal).ToArray();
            string requestPinName = distinctPhysicalRequestPins.Length == 1
                ? distinctPhysicalRequestPins[0].FirstOrDefault(name => Path.IsPathRooted(name)) ?? distinctPhysicalRequestPins[0].First()
                : null;
            if (requestPinName == null || distinctPhysicalRequestPins[0].Any(name => (string)recordedPins[name] != (string)recordedPins[requestPinName]))
                throw new InvalidDataException("Compiler-selected physical request pin required");
            var allowedControlPins = new HashSet<string>(inputControlPins, StringComparer.Ordinal);
            foreach (string name in resultPinNames) allowedControlPins.Add(name);
            foreach (string name in distinctPhysicalRequestPins[0]) allowedControlPins.Add(name);
            bool legacyTwoControlPins = controlNames.Count == 2 && resultPinNames.Any(controlNames.Contains) && distinctPhysicalRequestPins[0].Any(controlNames.Contains);
            if (!legacyTwoControlPins && (!fullInputClosureCaptured || controlNames.Count != allowedControlPins.Count ||
                controlNames.Any(name => !allowedControlPins.Contains(name))))
                throw new InvalidDataException("Unexpected or unlinked T Logs control pin outside compiler input closure");
            string requestPath = Path.GetFullPath(Path.Combine(root, requestPinName));
            if (!requestPath.StartsWith(prefixRoot + "T Logs/SkirmishProduction/", StringComparison.Ordinal) ||
                !requestPath.EndsWith("/request.json", StringComparison.Ordinal))
                throw new InvalidDataException("Compiler-selected physical request path required");
            string requestSha = (string)recordedPins[requestPinName];
            var request = ReadDerivedControl(requestPath, requestSha);
            string[] requestFields = { "schema_version", "kind", "source_path", "source_sha256", "grid_path", "grid_sha256", "surface_path", "surface_sha256", "operation_map_id", "source_identity", "content_hash", "frame", "selection", "source_fit_certified", "mission_ready" };
            var requestSelection = request["selection"] as JObject;
            if (request.Count != requestFields.Length || request.Properties().Any(p => !requestFields.Contains(p.Name)) ||
                (int?)request["schema_version"] != 1 || (string)request["kind"] != "PinnedCampaignPhysicalInputExport" ||
                (string)request["source_sha256"] != definitionSha || (string)request["operation_map_id"] != operationMapId ||
                (string)request["content_hash"] != (string)selection["sourceContentHash"] ||
                (string)request["source_identity"] != (string)capture["sourceMetadata"]?["MonoBehaviour"]?["sourceIdentityHash"] ||
                (bool?)request["source_fit_certified"] != false || (bool?)request["mission_ready"] != false ||
                requestSelection == null || requestSelection.Count != 2 ||
                (string)requestSelection["path"] != resultPath || (string)requestSelection["sha256"] != (string)selection["sha256"])
                throw new InvalidDataException("Compiler request does not select the captured map and exact result");
            foreach (string name in resultPinNames) compilerInputEvidencePins.Add(name);
            foreach (string name in distinctPhysicalRequestPins[0]) compilerInputEvidencePins.Add(name);
            return compilerInputEvidencePins;
        }

        // This admits captured source geometry only. It does not claim an
        // active mission water call, live scene, source fit or readiness.
        public static SourcePlacementDomain LoadSourceDerivedCapture(string path, string sha, string sourceRoot,
            JObject externalInputs, string definitionSha, string operationMapId, int width, int height,
            float cell, float originX, float originY, float originZ)
        {
            if (!Digest(sha) || !Digest(definitionSha) || string.IsNullOrWhiteSpace(operationMapId) ||
                width < 1 || height < 1 || (long)width * height > 4194304 || !math.isfinite(cell) || cell <= 0 ||
                !math.all(math.isfinite(new float3(originX, originY, originZ))) || externalInputs == null)
                throw new InvalidDataException("Finite selected source identity and grid required");
            var bytes = SourcePackedCampaignGeometry.ReadPinnedFile(path, 256 * 1024 * 1024, sha);
            var capture = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            var placement = capture["headlessPlacementInputs"] as JObject;
            var fixedMap = capture["fixedMapReadDomain"] as JObject;
            var frame = fixedMap?["gridFrame"] as JObject;
            if ((string)capture["result"] != "Collected" || (bool?)capture["worldDisposed"] != true ||
                (string)capture["sourceSha256"] != definitionSha || (string)capture["operationMapId"] != operationMapId ||
                placement == null || fixedMap == null || frame == null ||
                (string)placement["kind"] != "OriginalCampaignStaticPlacementInputs" ||
                (string)placement["authority"] != "ActualNativeReadOnlySourceOwners_NotMissionState" ||
                (string)placement["sourceDefinitionSha256"] != definitionSha || (string)placement["operationMapId"] != operationMapId ||
                (int?)placement["gridWidth"] != width || (int?)placement["gridHeight"] != height || (float?)placement["gridCellSize"] != cell ||
                !JToken.DeepEquals(placement["gridOrigin"], new JArray(originX, originY, originZ)) ||
                (bool?)placement["authoredBuildingCacheReady"] != true || (bool?)placement["requiredRoadSidewalkMasksComplete"] != true ||
                (bool?)placement["actorOrMissionStateExported"] != false || (bool?)placement["geometryGeneratedOrModified"] != false ||
                (bool?)placement["liveOccupancyMeasured"] != false || (bool?)placement["sourceFitCertified"] != false || (bool?)placement["missionReady"] != false ||
                placement["structuralVersionBefore"]?.Type != JTokenType.Integer || placement["structuralVersionAfter"]?.Type != JTokenType.Integer ||
                !JToken.DeepEquals(placement["structuralVersionBefore"], placement["structuralVersionAfter"]) ||
                (string)fixedMap["kind"] != "OriginalCampaignFixedMapReadDomain" || (string)fixedMap["stage"] != "Collected" ||
                (string)fixedMap["sourceDefinitionSha256"] != definitionSha || (string)fixedMap["operationMapId"] != operationMapId ||
                (int?)frame["width"] != width || (int?)frame["height"] != height || (float?)frame["cellSize"] != cell ||
                math.any(CaptureFrameVector(frame["origin"]) != new float3(originX, originY, originZ)) ||
                (bool?)fixedMap["physicalWaterGeometryCaptured"] != true || (bool?)fixedMap["activeMissionCreateMaskExecuted"] != false ||
                (string)fixedMap["waterAuthority"] != "Original RenderBoundsPresentation.Reader.CollectWaterFootprints" ||
                !((string)fixedMap["directReaderType"] ?? "").StartsWith("Game.Rendering.RenderBoundsPresentation+Reader,", StringComparison.Ordinal) ||
                (bool?)fixedMap["gatewayReferenceUnchanged"] != true || !JToken.DeepEquals(fixedMap["structuralVersionBefore"], fixedMap["structuralVersionAfter"]) ||
                fixedMap["structuralVersionBefore"]?.Type != JTokenType.Integer || fixedMap["structuralVersionAfter"]?.Type != JTokenType.Integer ||
                (bool?)fixedMap["actorOrMissionStateExported"] != false || (bool?)fixedMap["sourceFitCertified"] != false || (bool?)fixedMap["missionReady"] != false)
                throw new InvalidDataException("Exact source-derived map, frame and collector provenance required");
            var rows = fixedMap["waterFootprints"] as JArray;
            if (rows == null || rows.Count > 262144 || (int?)fixedMap["waterFootprintCount"] != rows.Count)
                throw new InvalidDataException("Complete captured original water footprint domain required");
            var water = new SharedWaterGeometryKernel.Footprint[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; var matrix = row["worldToLocalRowMajor"] as JArray;
                if (matrix == null || matrix.Count != 16) throw new InvalidDataException("Exact captured water matrix required");
                float[] m = matrix.Select(x => DerivedBits(x["bits"])).ToArray();
                water[i] = new SharedWaterGeometryKernel.Footprint {
                    WorldToLocal = new float4x4(m[0],m[1],m[2],m[3],m[4],m[5],m[6],m[7],m[8],m[9],m[10],m[11],m[12],m[13],m[14],m[15]),
                    LocalCenter = DerivedVector(row["localBounds"]?["center"]), LocalExtents = DerivedVector(row["localBounds"]?["extents"]),
                    WorldCenter = DerivedVector(row["worldBounds"]?["center"]), WorldExtents = DerivedVector(row["worldBounds"]?["extents"]) };
            }
            string capturedRoot = (string)capture["capturedProjectRoot"];
            if (string.IsNullOrEmpty(capturedRoot) || !Path.IsPathRooted(capturedRoot) || !(capture["readPins"] is JObject recordedPins) || recordedPins.Count == 0)
                throw new InvalidDataException("Recorded source root and complete pin map required");
            var compilerControlPins = ValidateCompilerSelectionControlEvidence(capture, recordedPins, capturedRoot, definitionSha, operationMapId);
            var pins = new Dictionary<string,string>(StringComparer.Ordinal); int externalCount = 0;
            foreach (var pin in recordedPins.Properties())
            {
                string recorded = Path.IsPathRooted(pin.Name) ? pin.Name : Path.Combine(capturedRoot, pin.Name);
                string expected = (string)pin.Value;
                if (!Digest(expected)) throw new InvalidDataException("Captured dependency digest required");
                if (compilerControlPins.Contains(pin.Name))
                {
                    pins[recorded] = expected;
                    continue;
                }
                if (HistoricalProducerBinary(recorded, capturedRoot)) continue;
                string prefix = Path.GetFullPath(capturedRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string relative = Path.IsPathRooted(pin.Name) && Path.GetFullPath(pin.Name).StartsWith(prefix, StringComparison.Ordinal)
                    ? Path.GetFullPath(pin.Name).Substring(prefix.Length) : (!Path.IsPathRooted(pin.Name) ? pin.Name : null);
                if (relative != null) RequireCurrentCapturedDependency(sourceRoot, relative, expected, pins);
                else
                {
                    var descriptor = externalInputs[pin.Name] as JObject;
                    string externalPath = (string)descriptor?["path"];
                    if (descriptor == null || descriptor.Count != 2 || !Path.IsPathRooted(externalPath) ||
                        (string)descriptor["sha256"] != expected || SourcePackedCampaignGeometry.HashFile(externalPath, 512 * 1024 * 1024) != expected)
                        throw new InvalidDataException("Captured external dependency changed: " + pin.Name);
                    pins[externalPath] = expected; externalCount++;
                }
            }
            if (externalCount != externalInputs.Count) throw new InvalidDataException("Unexpected external captured dependency descriptor");
            int count = checked(width * height);
            var result = new SourcePlacementDomain(DerivedMask(placement["authoredBuildings"], "authoredBuildings", count),
                DerivedMask(placement["sidewalks"], "sidewalks", count),
                DerivedMask(placement["authoredRoadsIncludingSidewalks"], "authoredRoadsIncludingSidewalks", count),
                width, height, cell, new float3(originX, originY, originZ), sha, sha, 0, pins, water);
            result.VerifyOriginalOwnerAssetPins();
            return result;
        }
    }
}
