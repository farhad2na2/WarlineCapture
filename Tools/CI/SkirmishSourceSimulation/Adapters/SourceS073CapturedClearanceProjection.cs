using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal sealed class S073SourceClearanceProjectionAdapter
{
    private const string BridgeRelativePath = "Logs/SkirmishProduction/s073-source-clearance-identity-bridge-20261102-r1.json";
    private const string BridgeSha256 = "08b8431f6481f7ddabe6a044fb7f45d658021dce8380e533feb97dc19f3a4e53";
    private const string S002ProjectionRelativePath = "Logs/SkirmishProduction/s073-clearance-projection-input-20261102-r1/static-clearance-from-capture.json";
    private const string S002ProjectionSha256 = "8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065";
    private const string PhysicalRelativePath = "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json";
    private const string PhysicalSha256 = "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8";
    private const string PhysicalMapRelativePath = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
    private const string PhysicalMapSha256 = "8a03d36fd1d4d59a180c65d82f1c708fd70e7c291f49d480c80629a80ec728c0";
    private const string SelectedMapRelativePath = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
    private const string SelectedMapSha256 = "b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a";
    private const string StreamedCaptureRelativePath = "Logs/SkirmishProduction/s073-streamed-source-capture-20261101-r4/source-world.json";
    private const string StreamedCaptureSha256 = "1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7";
    private const string ProviderSourceRelativePath = "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs";
    private const string ProviderSourceSha256 = "82bf379f5bb3e5ae55705f608c3e4a26ed915afffeec7488c79b9448cecfb5a4";
    private const string CaptureSourceRelativePath = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs";
    private const string CaptureSourceSha256 = "f278e4383dfb4ae3ad3f00d7b1da4cedce75625cbdceeaff7bdf60d9d9614346";
    private const string SourceMapId = "opmap.skirmish.desert_base_01";
    private const int Width = 2048;
    private const int Height = 1024;
    private const float CellSize = 1f;
    private readonly float3[] centers;
    private readonly float3[] extents;
    private readonly string bridgePath;
    private int callbackInvocationCount;

    private S073SourceClearanceProjectionAdapter(string path, float3[] sourceCenters, float3[] sourceExtents)
    {
        bridgePath = path;
        centers = sourceCenters;
        extents = sourceExtents;
    }

    internal static S073SourceClearanceProjectionAdapter Load(string repositoryRoot, GridConfig sourceCopyGrid)
    {
        if (repositoryRoot == null) throw new ArgumentNullException(nameof(repositoryRoot));
        RequireGrid(sourceCopyGrid, "Disposable S073 source-copy grid");
        string bridgePath = Path.Combine(repositoryRoot, BridgeRelativePath);
        byte[] bridgeBytes = ReadPinnedBytes(bridgePath, BridgeSha256, 1024 * 1024);
        JObject bridge = JObject.Parse(Encoding.UTF8.GetString(bridgeBytes), new JsonLoadSettings
        {
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
        });
        if ((int?)bridge["schema_version"] != 1 ||
            (string)bridge["kind"] != "S073SourceClearanceIdentityBridge" ||
            (string)bridge["status"] != "IdentityBound" ||
            (string)bridge["scope"] != "SharedCampaignSourceFrameIdentityOnly_NoProviderParity_NoDeliveryOrReadinessClaim" ||
            (bool?)bridge["provider_parity_established"] != false ||
            (bool?)bridge["historical_capture_dependency_pins_revalidated"] != false ||
            (bool?)bridge["source_capture_current_producer_equivalence"] != false ||
            (bool?)bridge["capture_relocation_path_remapping_validated"] != false ||
            (bool?)bridge["delivery_established"] != false || (bool?)bridge["mission_ready"] != false ||
            (bool?)bridge["source_fit_certified"] != false)
            throw new InvalidDataException("Exact non-admitting S073 source-clearance identity bridge required");

        JObject geometry = bridge["source_geometry"] as JObject;
        JObject sourceBinding = bridge["s073_source_binding"] as JObject;
        JObject evidence = bridge["evidence"] as JObject;
        JObject physicalDescriptor = evidence?["physical_capture"] as JObject;
        JObject physicalMapDescriptor = evidence?["physical_map"] as JObject;
        JObject selectedMapDescriptor = evidence?["s073_selected_map"] as JObject;
        JObject streamedDescriptor = evidence?["s073_streamed_capture"] as JObject;
        JObject providerDescriptor = evidence?["provider_implementation"] as JObject;
        JObject captureSourceDescriptor = evidence?["capture_implementation"] as JObject;
        if (geometry == null || sourceBinding == null || physicalDescriptor == null ||
            physicalMapDescriptor == null || selectedMapDescriptor == null || streamedDescriptor == null ||
            providerDescriptor == null || captureSourceDescriptor == null ||
            (string)geometry["authority"] != "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain" ||
            (string)geometry["source_map_id"] != SourceMapId ||
            (string)geometry["source_capture_sha256"] != PhysicalSha256 ||
            (string)sourceBinding["source_map_id"] != SourceMapId ||
            (string)sourceBinding["logical_map_id"] != "opmap.skirmish.industrial_basin" ||
            (bool?)sourceBinding["mission_ready"] != false || (bool?)sourceBinding["source_fit_certified"] != false ||
            (string)physicalDescriptor["path"] != PhysicalRelativePath ||
            (string)physicalDescriptor["sha256"] != PhysicalSha256 ||
            (string)physicalMapDescriptor["path"] != PhysicalMapRelativePath ||
            (string)physicalMapDescriptor["sha256"] != PhysicalMapSha256 ||
            (string)selectedMapDescriptor["path"] != SelectedMapRelativePath ||
            (string)selectedMapDescriptor["sha256"] != SelectedMapSha256 ||
            (string)selectedMapDescriptor["source_map_id"] != SourceMapId ||
            (string)streamedDescriptor["path"] != StreamedCaptureRelativePath ||
            (string)streamedDescriptor["sha256"] != StreamedCaptureSha256 ||
            (string)providerDescriptor["path"] != ProviderSourceRelativePath ||
            (string)providerDescriptor["sha256"] != ProviderSourceSha256 ||
            (string)captureSourceDescriptor["path"] != CaptureSourceRelativePath ||
            (string)captureSourceDescriptor["sha256"] != CaptureSourceSha256)
            throw new InvalidDataException("S073 clearance bridge descriptors differ from pinned source inputs");

        JObject grid = geometry["grid"] as JObject;
        JArray frame = grid != null ? grid["origin"] as JArray : null;
        JArray sourceEnvelope = geometry["query_rect"] as JArray;
        if (grid == null || frame == null || frame.Count != 3 ||
            sourceEnvelope == null || sourceEnvelope.Count != 4 ||
            (int?)grid["width"] != Width || (int?)grid["height"] != Height ||
            (float?)grid["cell_size"] != CellSize || frame.Any(value => (float?)value != 0f) ||
            (float?)sourceEnvelope[0] != -118f || (float?)sourceEnvelope[1] != -118f ||
            (float?)sourceEnvelope[2] != 2284f || (float?)sourceEnvelope[3] != 1260f ||
            (int?)geometry["bounds_count"] != 90865 ||
            (int?)geometry["authored_transform_projection_count"] != 124499 ||
            (string)geometry["geometric_relocation"] != "None_ExactSharedCampaignSourceFrameRequired")
            throw new InvalidDataException("S073 source clearance geometry frame/cardinality changed");

        string physicalPath = Path.Combine(repositoryRoot, PhysicalRelativePath);
        VerifyFileHash(physicalPath, PhysicalSha256, 160 * 1024 * 1024);
        string physicalMapPath = Path.Combine(repositoryRoot, PhysicalMapRelativePath);
        ReadPinnedBytes(physicalMapPath, PhysicalMapSha256, 4 * 1024 * 1024);
        string selectedMapPath = Path.Combine(repositoryRoot, SelectedMapRelativePath);
        ReadPinnedBytes(selectedMapPath, SelectedMapSha256, 4 * 1024 * 1024);
        ReadPinnedBytes(Path.Combine(repositoryRoot, ProviderSourceRelativePath), ProviderSourceSha256, 4 * 1024 * 1024);
        ReadPinnedBytes(Path.Combine(repositoryRoot, CaptureSourceRelativePath), CaptureSourceSha256, 4 * 1024 * 1024);
        string streamedCapturePath = Path.Combine(repositoryRoot, StreamedCaptureRelativePath);
        VerifyFileHash(streamedCapturePath, StreamedCaptureSha256, 160 * 1024 * 1024);

        JObject sourceRecord = ReadCapturedBoundsRecord(physicalPath);
        if (sourceRecord == null || (string)sourceRecord["authority"] != (string)geometry["authority"] ||
            (sourceRecord["bounds"] as JArray)?.Count != 90865 ||
            (int?)sourceRecord["authored_transform_projection_count"] != 124499 ||
            (int?)sourceRecord["grid_width"] != Width || (int?)sourceRecord["grid_height"] != Height ||
            (float?)sourceRecord["cell_size"] != CellSize ||
            (float?)sourceRecord["minimum_extent"] != 0.25f || (float?)sourceRecord["maximum_extent"] != 20f ||
            (int?)sourceRecord["source_search_radius_cells"] != 96 ||
            (int?)sourceRecord["source_padding_cells"] != 2 ||
            !JToken.DeepEquals(sourceRecord["query_rect"], sourceEnvelope) ||
            (bool?)sourceRecord["runtime_presentation_activation_sampled"] != false)
            throw new InvalidDataException("Exact original shared Campaign source clearance record required");

        JArray rows = sourceRecord["bounds"] as JArray;
        var sourceCenters = new float3[rows.Count];
        var sourceExtents = new float3[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            JObject row = rows[i] as JObject;
            JArray c = row?["center"] as JArray;
            JArray e = row?["extents"] as JArray;
            if (c == null || e == null || c.Count != 3 || e.Count != 3)
                throw new InvalidDataException("Malformed captured source clearance bound row " + i);
            sourceCenters[i] = new float3((float)c[0], (float)c[1], (float)c[2]);
            sourceExtents[i] = new float3((float)e[0], (float)e[1], (float)e[2]);
            if (!math.all(math.isfinite(sourceCenters[i])) || !math.all(math.isfinite(sourceExtents[i])) ||
                math.any(sourceExtents[i] < 0f))
                throw new InvalidDataException("Invalid captured source clearance bound row " + i);
        }
        return new S073SourceClearanceProjectionAdapter(bridgePath, sourceCenters, sourceExtents);
    }

    internal JObject Evidence()
    {
        return new JObject
        {
            ["status"] = "BoundForDisposableCopyDiagnosticOnly",
            ["adapter_bound"] = true,
            ["identity_bridge_path"] = bridgePath,
            ["identity_bridge_sha256"] = BridgeSha256,
            ["source_bounds_count"] = centers.Length,
            ["source_provider_parity_qualified"] = false,
            ["historical_capture_dependency_pins_revalidated"] = false,
            ["source_capture_current_producer_equivalence"] = false,
            ["callback_invocation_count"] = callbackInvocationCount,
            ["callback_invoked"] = callbackInvocationCount > 0,
            ["delivery_invoked"] = false,
            ["scheduler_admitted"] = false,
            ["mission_ready"] = false
        };
    }

    internal void Reserve(SourceProductionGroundWorld world, int2 preferred, ref NativeBitArray reserved)
    {
        ReserveCore(world, preferred, ref reserved);
    }

    internal void ReserveCallback(SourceProductionGroundWorld world, int2 preferred, ref NativeBitArray reserved)
    {
        callbackInvocationCount++;
        ReserveCore(world, preferred, ref reserved);
    }

    internal JObject CompareAgainstS002(SourceProductionGroundWorld world, string repositoryRoot)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        string projectionPath = Path.Combine(repositoryRoot, S002ProjectionRelativePath);
        VerifyFileHash(projectionPath, S002ProjectionSha256, 32 * 1024 * 1024);
        var baseline = new GeneratedS002DropClearance(projectionPath, S002ProjectionSha256);
        var samples = new[]
        {
            new int2(0, 0), new int2(Width - 1, Height - 1),
            new int2(Width / 2, Height / 2), new int2(-20, 0),
            new int2(2067, 1043)
        };
        var rows = new JArray();
        foreach (int2 preferred in samples)
        {
            var s002Mask = CreateClearedMask();
            var s073Mask = CreateClearedMask();
            try
            {
                baseline.Reserve(world, preferred, ref s002Mask);
                Reserve(world, preferred, ref s073Mask);
                int mismatches = 0, s002SetCount = 0, s073SetCount = 0, firstMismatch = -1;
                for (int i = 0; i < checked(Width * Height); i++)
                {
                    bool expected = s002Mask.IsSet(i), actual = s073Mask.IsSet(i);
                    if (expected) s002SetCount++;
                    if (actual) s073SetCount++;
                    if (expected == actual) continue;
                    if (firstMismatch < 0) firstMismatch = i;
                    mismatches++;
                }
                if (mismatches != 0)
                    throw new InvalidDataException("S073 static source-clearance mask differs from strict S002 baseline at preferred=" + preferred +
                        " mismatches=" + mismatches + " first=" + firstMismatch);
                rows.Add(new JObject { ["preferred_cell"] = new JArray(preferred.x, preferred.y),
                    ["grid_mask_cells_compared"] = checked(Width * Height), ["s002_reserved_cells"] = s002SetCount,
                    ["s073_reserved_cells"] = s073SetCount, ["different_cells"] = mismatches,
                    ["scope"] = "CompiledSameWorldScratchMaskDiagnostic_NoDeliveryOrAdmission" });
            }
            finally
            {
                if (s002Mask.IsCreated) s002Mask.Dispose();
                if (s073Mask.IsCreated) s073Mask.Dispose();
            }
        }

        int rejectedOutOfEnvelope = CompareOutOfEnvelopeRejection(baseline, world);
        return new JObject
        {
            ["status"] = "Passed",
            ["compiled_differential_executed"] = true,
            ["baseline"] = "OriginalGeneratedS002DropClearance.Reserve",
            ["candidate"] = "S073SourceClearanceProjectionAdapter.Reserve",
            ["projection_path"] = projectionPath,
            ["projection_sha256"] = S002ProjectionSha256,
            ["preferred_cell_samples"] = rows,
            ["rejected_out_of_envelope_cases"] = rejectedOutOfEnvelope,
            ["same_source_world"] = true,
            ["full_masks_compared"] = true,
            ["delivery_invoked"] = false,
            ["source_provider_parity_qualified"] = false,
            ["mission_ready"] = false
        };
    }

    private int CompareOutOfEnvelopeRejection(GeneratedS002DropClearance baseline,
        SourceProductionGroundWorld world)
    {
        int2 preferred = new int2(-21, 0);
        var s002Mask = CreateClearedMask();
        var s073Mask = CreateClearedMask();
        try
        {
            bool s002Rejected = false, s073Rejected = false;
            try { baseline.Reserve(world, preferred, ref s002Mask); }
            catch (NotSupportedException) { s002Rejected = true; }
            try { Reserve(world, preferred, ref s073Mask); }
            catch (NotSupportedException) { s073Rejected = true; }
            if (!s002Rejected || !s073Rejected)
                throw new InvalidDataException("S002/S073 source-clearance adapters disagree on out-of-envelope query rejection");
            return 1;
        }
        finally
        {
            if (s002Mask.IsCreated) s002Mask.Dispose();
            if (s073Mask.IsCreated) s073Mask.Dispose();
        }
    }

    private static NativeBitArray CreateClearedMask()
    {
        var mask = new NativeBitArray(checked(Width * Height), Allocator.Temp);
        for (int i = 0; i < mask.Length; i++) mask.Set(i, false);
        return mask;
    }

    private void ReserveCore(SourceProductionGroundWorld world, int2 preferred, ref NativeBitArray reserved)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        RequireGrid(world.Grid, "S073 disposable copy clearance callback grid");
        if (!reserved.IsCreated || reserved.Length != checked(Width * Height))
            throw new InvalidOperationException("Full S073 disposable copy reservation mask required");
        GridConfig grid = world.Grid;
        const int searchRadius = 96;
        const int staticPadding = 2;
        const float maximumExtentCells = 20f;
        float safeCellSize = math.max(0.01f, grid.CellSize);
        float maxExtent = maximumExtentCells * safeCellSize;
        int searchPadding = searchRadius + staticPadding;
        int2 searchMin = preferred - new int2(searchPadding, searchPadding);
        int2 searchMax = preferred + new int2(searchPadding + 1, searchPadding + 1);
        float xMin = grid.Origin.x + searchMin.x * safeCellSize;
        float yMin = grid.Origin.z + searchMin.y * safeCellSize;
        float xMax = grid.Origin.x + searchMax.x * safeCellSize;
        float yMax = grid.Origin.z + searchMax.y * safeCellSize;
        const float sourceXMin = -118f, sourceYMin = -118f, sourceXMax = 2166f, sourceYMax = 1142f;
        if (xMin < sourceXMin || yMin < sourceYMin || xMax > sourceXMax || yMax > sourceYMax)
            throw new NotSupportedException("Local S073 drop query exceeds the complete captured source bounds envelope");

        for (int i = 0; i < centers.Length; i++)
        {
            float3 c = centers[i], e = extents[i];
            float extent = math.max(e.x, e.z);
            if (!math.all(math.isfinite(c)) || !math.all(math.isfinite(e)) ||
                extent < safeCellSize * 0.25f || extent > maxExtent) continue;
            float axMin = c.x - e.x, ayMin = c.z - e.z;
            float axMax = axMin + e.x * 2f, ayMax = ayMin + e.z * 2f;
            if (axMax < xMin || ayMax < yMin || axMin >= xMax || ayMin >= yMax) continue;
            int2 minCell = GridUtils.WorldToCell(grid, c - e);
            int2 maxCell = GridUtils.WorldToCell(grid, c + e);
            world.ReserveRect(ref reserved, minCell.x - staticPadding, minCell.y - staticPadding,
                maxCell.x + staticPadding + 1, maxCell.y + staticPadding + 1);
        }
    }

    private static void RequireGrid(GridConfig grid, string label)
    {
        if (grid.Width != Width || grid.Height != Height || grid.CellSize != CellSize ||
            grid.Origin.x != 0f || grid.Origin.y != 0f || grid.Origin.z != 0f)
            throw new NotSupportedException(label + " differs from the exact shared Campaign source frame");
    }

    private static byte[] ReadPinnedBytes(string path, string expectedSha, int maxBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 ||
            info.Length <= 0 || info.Length > maxBytes)
            throw new InvalidDataException("Pinned regular source input required: " + path);
        byte[] bytes = File.ReadAllBytes(path);
        if (Sha256(bytes) != expectedSha) throw new InvalidDataException("Pinned source input hash changed: " + path);
        return bytes;
    }

    private static JObject ReadCapturedBoundsRecord(string path)
    {
        using (var input = new StreamReader(File.OpenRead(path), Encoding.UTF8, true, 1024 * 1024))
        using (var reader = new Newtonsoft.Json.JsonTextReader(input)
        {
            MaxDepth = 32,
            DateParseHandling = Newtonsoft.Json.DateParseHandling.None
        })
        {
            while (reader.Read())
            {
                if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName ||
                    !string.Equals((string)reader.Value, "dropClearanceBounds", StringComparison.Ordinal)) continue;
                if (!reader.Read() || reader.TokenType != Newtonsoft.Json.JsonToken.StartObject)
                    throw new InvalidDataException("Captured source clearance record object required");
                return JObject.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
            }
        }
        throw new InvalidDataException("Captured source clearance record is missing");
    }

    private static void VerifyFileHash(string path, string expectedSha, long maxBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 ||
            info.Length <= 0 || info.Length > maxBytes)
            throw new InvalidDataException("Pinned regular source capture required: " + path);
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
        {
            string actual = string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            if (actual != expectedSha) throw new InvalidDataException("Pinned source capture hash changed: " + path);
        }
    }

    private static string Sha256(byte[] bytes)
    {
        using (var sha = SHA256.Create())
            return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }
}
