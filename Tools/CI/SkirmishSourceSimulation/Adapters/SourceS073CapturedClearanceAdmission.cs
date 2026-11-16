using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Globalization;
using Newtonsoft.Json.Linq;

internal sealed class SourceS073HostAbsentPresentationCameraCapability
{
    internal const string Issuer = "SourceS073LegacyNormalRun_NoPresentationCameraConstructed";
    private readonly string issuer;
    private readonly string evidenceScope;

    private SourceS073HostAbsentPresentationCameraCapability(string sourceIssuer, string scope)
    {
        issuer = sourceIssuer;
        evidenceScope = scope;
    }

    // Issued by the source-host composition path, which creates no presentation
    // Camera and treats map camera metadata as inert data.
    internal static SourceS073HostAbsentPresentationCameraCapability IssueForSourceHost()
    {
        return new SourceS073HostAbsentPresentationCameraCapability(Issuer,
            "SourceS073LegacyNormalRun_ExplicitNoPresentationCameraConstructionPath_NoNativeCameraClaim");
    }

    internal bool IsValidForSourceHost => issuer == Issuer &&
        evidenceScope == "SourceS073LegacyNormalRun_ExplicitNoPresentationCameraConstructionPath_NoNativeCameraClaim";

    internal JObject Provenance() => new JObject
    {
        ["authority"] = issuer,
        ["scope"] = evidenceScope,
        ["presentation_camera_present"] = false,
        ["native_camera_observed"] = false,
        ["native_state_copied"] = false,
        ["mission_ready"] = false
    };
}

internal static class SourceS073CapturedClearanceAdmission
{
    private const string ReceiptRelativePath = "Logs/SkirmishProduction/s073-reserve-mask-cells-20261107-r1/receipt.json";
    private const string ReceiptSha256 = "2cba65e0eaac0c86ca400fdfb5799375a0363c183400f9c33cd4e6572b0ff6d6";
    private const string RunLogRelativePath = "Logs/SkirmishProduction/s073-reserve-mask-cells-20261107-r1/run.log";
    private const string RunLogSha256 = "516d97271d61f1c41be542b518e3c31bcafb7efad4c102e00532c0f03ba98ae6";
    private const string ExtractedAdapterRelativePath = "Logs/SkirmishProduction/s073-reserve-mask-cells-20261107-r1/generated/ExtractedS073SourceClearanceProjectionAdapter.cs";
    private const string ExtractedAdapterSha256 = "5c798a1c4474bb95f8875af76db4e980202520c8891a001341f0e8b7352625b4";
    private const string ProjectionRelativePath = "Logs/SkirmishProduction/s073-clearance-projection-input-20261102-r1/static-clearance-from-capture.json";
    private const string ProjectionSha256 = "8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065";

    internal static JObject RequireSevenCaseCapturedProjectionParity(string repositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot)) throw new ArgumentException(nameof(repositoryRoot));
        string root = Path.GetFullPath(repositoryRoot);
        string receiptPath = Path.Combine(root, ReceiptRelativePath);
        string runLogPath = Path.Combine(root, RunLogRelativePath);
        string extractedAdapterPath = Path.Combine(root, ExtractedAdapterRelativePath);
        string currentAdapterPath = Path.Combine(root, "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073CapturedClearanceProjection.cs");
        string projectionPath = Path.Combine(root, ProjectionRelativePath);

        VerifyFileHash(receiptPath, ReceiptSha256, 1024 * 1024);
        VerifyFileHash(runLogPath, RunLogSha256, 1024 * 1024);
        VerifyFileHash(extractedAdapterPath, ExtractedAdapterSha256, 2 * 1024 * 1024);
        VerifyFileHash(currentAdapterPath, ExtractedAdapterSha256, 2 * 1024 * 1024);
        VerifyFileHash(projectionPath, ProjectionSha256, 32 * 1024 * 1024);

        JObject receipt = JObject.Parse(File.ReadAllText(receiptPath));
        if ((string)receipt["kind"] != "SourceHostOriginalReserveMaskVsS073ProjectionAdapter" ||
            (string)receipt["status"] != "Passed" ||
            (int?)receipt["compile_exit"] != 0 || (int?)receipt["run_exit"] != 0 ||
            (bool?)receipt["runtime_executed"] != true || (bool?)receipt["unity_run"] != false ||
            (bool?)receipt["mask_comparison_pass_marker"] != true ||
            (string)receipt["captured_projection_sha256"] != ProjectionSha256 ||
            (int?)receipt["captured_bound_count"] != 90865 ||
            (int?)receipt["grid_mask_cells"] != 2048 * 1024 ||
            (string)receipt["scope"] != "Historical pregame captured bounds; no provider parity, mission admission or readiness claim")
            throw new InvalidDataException("Pinned seven-case captured-projection parity receipt required");

        JObject sourceHashes = receipt["source_hashes"] as JObject ??
            throw new InvalidDataException("Seven-case parity source hashes are missing");
        if ((string)sourceHashes["s073_projection_adapter"] !=
            "5c798a1c4474bb95f8875af76db4e980202520c8891a001341f0e8b7352625b4")
            throw new InvalidDataException("Seven-case parity receipt does not retain the original S073 projection adapter source");
        VerifyReceiptSourceHash(root, sourceHashes, "reserve_owner",
            "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
            "43e7db5e3bfc1f21386d6ec10b6d233c56bbae9dc630a7ff2bfb280be4aecbbf");
        VerifyReceiptSourceHash(root, sourceHashes, "collector_owner",
            "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs",
            "82bf379f5bb3e5ae55705f608c3e4a26ed915afffeec7488c79b9448cecfb5a4");
        VerifyReceiptSourceHash(root, sourceHashes, "provider_contract",
            "Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs",
            "2642b83d55de9cc7d7828593862d231f53ae40edb62997c80c035f3c7e3e3c81");
        VerifyReceiptSourceHash(root, sourceHashes, "grid_utils",
            "Assets/Game/Scripts/Components/GridComponents.cs",
            "eecfe09c12836db541242b45133db5e75659acb269469db9fdda884a1510ff02");

        string runLog = File.ReadAllText(runLogPath);
        string[] requiredCases =
        {
            "cell=int2(1001, 430) original_reserved=43917 s073_reserved=43917 differing_cells=0",
            "cell=int2(999, 428) original_reserved=43355 s073_reserved=43355 differing_cells=0",
            "cell=int2(1024, 512) original_reserved=42143 s073_reserved=42143 differing_cells=0",
            "cell=int2(-20, -20) original_reserved=0 s073_reserved=0 differing_cells=0",
            "cell=int2(2067, 1043) original_reserved=8440 s073_reserved=8440 differing_cells=0",
            "cell=int2(0, 0) original_reserved=0 s073_reserved=0 differing_cells=0",
            "cell=int2(2047, 1023) original_reserved=10905 s073_reserved=10905 differing_cells=0"
        };
        if (requiredCases.Any(row => !runLog.Contains(row)) ||
            !runLog.Contains("result=Passed input_bounds=90865 preferred_count=7 total_reserved=148760 mask_cells=2097152 differing_cells=0 provider_parity=false mission_ready=false"))
            throw new InvalidDataException("Seven original-source full-mask parity cases are incomplete or changed");

        return new JObject
        {
            ["kind"] = "S073CapturedStaticProjectionParityAdmission",
            ["status"] = "PinnedSevenCaseFullMaskParityPassed",
            ["receipt_path"] = ReceiptRelativePath,
            ["receipt_sha256"] = ReceiptSha256,
            ["run_log_sha256"] = RunLogSha256,
            ["captured_projection_sha256"] = ProjectionSha256,
            ["captured_bound_count"] = 90865,
            ["preferred_case_count"] = 7,
            ["mask_cells_per_case"] = 2048 * 1024,
            ["provider_parity_established"] = false,
            ["current_producer_equivalence_established"] = false,
            ["native_state_copied"] = false,
            ["mission_ready"] = false
        };
    }

    private static void VerifyReceiptSourceHash(string root, JObject hashes, string key, string relativePath, string expected)
    {
        string fromReceipt = (string)hashes[key];
        if (fromReceipt != expected) throw new InvalidDataException("Seven-case parity receipt source hash changed: " + key);
        VerifyFileHash(Path.Combine(root, relativePath), expected, 16 * 1024 * 1024);
    }

    private static void VerifyFileHash(string path, string expectedSha256, long maxBytes)
    {
        var info = new FileInfo(path);
        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length <= 0 || info.Length > maxBytes)
            throw new InvalidDataException("Pinned regular diagnostic evidence required: " + path);
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
        {
            string actual = string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            if (actual != expectedSha256) throw new InvalidDataException("Pinned diagnostic evidence hash changed: " + path);
        }
    }
}
