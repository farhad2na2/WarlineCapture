#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class MobileVisualQualityCaptureMatrixTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        RunCase(CurrentSixteenByNinePlanMatchesAph809Inventory, ref passed);
        RunCase(CandidateTwentyByNinePlanContainsOnlyComparisonArtifacts, ref passed);
        RunCase(OptionsRejectInvalidAspectRevisionAndDirtyEvidence, ref passed);
        RunCase(OptionsRejectWrongRootFrameRateAndGitEvidence, ref passed);
        RunCase(CameraContractRoundTripsAndAppliesExactPose, ref passed);
        RunCase(CameraContractRejectsAliasDriftAndInvalidDistanceOrder, ref passed);
        RunCase(SessionMetadataCarriesAph809AndAph505Provenance, ref passed);
        Debug.Log($"[MobileVisualQualityCaptureMatrixTests] result=Passed tests={passed}");
    }

    private static void RunCase(Action test, ref int passed)
    {
        test();
        passed++;
    }

    [Test]
    public static void CurrentSixteenByNinePlanMatchesAph809Inventory()
    {
        MobileVisualQualityCaptureMatrix.Options options = CreateOptions("current", "16:9");
        IReadOnlyList<(string rowId, string role)> artifacts =
            MobileVisualQualityCaptureMatrix.ExpectedArtifacts(options);

        Assert.AreEqual(1920, options.Width);
        Assert.AreEqual(1080, options.Height);
        Assert.AreEqual("16x9", options.AspectToken);
        Assert.AreEqual(MobileVisualQualityCaptureMatrix.ArtifactDirectory, options.ArtifactDirectory);
        Assert.AreEqual(13, artifacts.Count);
        Assert.AreEqual(("menu-main-16x9", "capture"), artifacts[0]);
        Assert.Contains(("graphics-tier-gameplay-zoom-16x9", "current"), (System.Collections.IList)artifacts);
        Assert.Contains(("static-map-near-16x9", "capture"), (System.Collections.IList)artifacts);
        Assert.Contains(("mip-streaming-far-16x9", "capture"), (System.Collections.IList)artifacts);
        Assert.AreEqual(
            "graphics-tier-gameplay-zoom-16x9_current.png",
            MobileVisualQualityCaptureMatrix.ArtifactFileName(
                "graphics-tier-gameplay-zoom-16x9",
                "current"));
    }

    [Test]
    public static void CandidateTwentyByNinePlanContainsOnlyComparisonArtifacts()
    {
        MobileVisualQualityCaptureMatrix.Options options = CreateOptions("candidate", "20:9");
        IReadOnlyList<(string rowId, string role)> artifacts =
            MobileVisualQualityCaptureMatrix.ExpectedArtifacts(options);

        Assert.AreEqual(2400, options.Width);
        Assert.AreEqual(1080, options.Height);
        Assert.AreEqual("20x9", options.AspectToken);
        Assert.AreEqual(3, artifacts.Count);
        CollectionAssert.AreEqual(
            new[]
            {
                ("graphics-tier-gameplay-zoom-20x9", "candidate"),
                ("graphics-tier-max-zoom-out-20x9", "candidate"),
                ("graphics-tier-night-20x9", "candidate")
            },
            artifacts);
    }

    [Test]
    public static void OptionsRejectInvalidAspectRevisionAndDirtyEvidence()
    {
        Dictionary<string, string> values = EnvironmentValues("16:9");
        values[MobileVisualQualityCaptureMatrix.AspectEnvironmentVariable] = "21:9";
        Assert.Throws<InvalidOperationException>(() => CreateOptions("current", values));

        values = EnvironmentValues("16:9");
        values[MobileVisualQualityCaptureMatrix.RevisionEnvironmentVariable] = "ABC";
        Assert.Throws<InvalidOperationException>(() => CreateOptions("current", values));

        values = EnvironmentValues("16:9");
        values[MobileVisualQualityCaptureMatrix.DirtyEnvironmentVariable] = "true";
        Assert.Throws<InvalidOperationException>(() => CreateOptions("current", values));
    }

    [Test]
    public static void OptionsRejectWrongRootFrameRateAndGitEvidence()
    {
        Dictionary<string, string> values = EnvironmentValues("16:9");
        Assert.Throws<InvalidOperationException>(() =>
            MobileVisualQualityCaptureMatrix.CreateOptions(
                "current",
                "/private/tmp/not-accepted",
                key => values.TryGetValue(key, out string value) ? value : null));

        values[MobileVisualQualityCaptureMatrix.FrameRateModeEnvironmentVariable] = "uncapped";
        Assert.Throws<InvalidOperationException>(() => CreateOptions("current", values));

        MobileVisualQualityCaptureMatrix.Options options = CreateOptions("current", "16:9");
        Assert.DoesNotThrow(() =>
            MobileVisualQualityCaptureMatrix.ValidateGitEvidence(options, options.Revision, false));
        Assert.Throws<InvalidOperationException>(() =>
            MobileVisualQualityCaptureMatrix.ValidateGitEvidence(options, new string('b', 40), false));
        Assert.Throws<InvalidOperationException>(() =>
            MobileVisualQualityCaptureMatrix.ValidateGitEvidence(options, options.Revision, true));
    }

    [Test]
    public static void CameraContractRoundTripsAndAppliesExactPose()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"warline-aph809-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "camera.json");
        GameObject cameraObject = new("MatrixCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            MobileVisualQualityCaptureMatrix.CameraContract contract = ValidContract("16:9");
            MobileVisualQualityCaptureMatrix.SaveCameraContract(path, contract);
            MobileVisualQualityCaptureMatrix.CameraContract loaded =
                MobileVisualQualityCaptureMatrix.LoadCameraContract(path, "16:9");

            MobileVisualQualityCaptureMatrix.ApplyAndRequirePose(camera, loaded.medium);
            MobileVisualQualityCaptureMatrix.CameraPose applied =
                MobileVisualQualityCaptureMatrix.CapturePose(camera);
            Assert.True(MobileVisualQualityCaptureMatrix.PoseMatches(applied, loaded.medium, 0.001f));
            Assert.True(MobileVisualQualityCaptureMatrix.PoseMatches(loaded.gameplayZoom, loaded.medium, 0.001f));
            Assert.True(MobileVisualQualityCaptureMatrix.PoseMatches(loaded.maximumZoomOut, loaded.far, 0.001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Test]
    public static void CameraContractRejectsAliasDriftAndInvalidDistanceOrder()
    {
        MobileVisualQualityCaptureMatrix.CameraContract contract = ValidContract("20:9");
        contract.gameplayZoom.position[0] += 1f;
        Assert.Throws<InvalidDataException>(() =>
            MobileVisualQualityCaptureMatrix.ValidateCameraContract(contract, "20:9"));

        contract = ValidContract("20:9");
        contract.near.position[1] = contract.medium.position[1];
        Assert.Throws<InvalidDataException>(() =>
            MobileVisualQualityCaptureMatrix.ValidateCameraContract(contract, "20:9"));
    }

    [Test]
    public static void SessionMetadataCarriesAph809AndAph505Provenance()
    {
        MobileVisualQualityCaptureMatrix.Options options = CreateOptions("candidate", "20:9");
        options.CandidatePaths = new[] { "Assets/World/A.png", "Assets/World/B.png" };
        MobileVisualQualityCaptureMatrix.ArtifactMetadata artifact = new()
        {
            rowId = "graphics-tier-night-20x9",
            role = "candidate",
            path = "/private/tmp/graphics-tier-night-20x9_candidate.png",
            revision = options.Revision,
            deviceProfile = options.DeviceProfile,
            frameRateMode = options.FrameRateMode,
            qualityTier = "candidate",
            cameraPosition = new[] { 10f, 24f, 30f },
            cameraRotation = new[] { 58f, 10f, 0f },
            state = "night-current-vs-candidate"
        };

        MobileVisualQualityCaptureMatrix.SessionMetadata metadata =
            MobileVisualQualityCaptureMatrix.CreateSessionMetadata(options, new[] { artifact });

        Assert.AreEqual("APH-809", metadata.taskId);
        Assert.AreEqual(options.Revision, metadata.revision);
        Assert.False(metadata.dirty);
        Assert.AreEqual("20:9", metadata.aspect);
        Assert.AreEqual("candidate", metadata.profile);
        Assert.AreEqual(1, metadata.artifactCount);
        Assert.AreEqual("APH-505", metadata.aph505EvidenceFragment.taskId);
        Assert.AreEqual("capture-session", metadata.aph505EvidenceFragment.status);
        Assert.AreEqual("candidate", metadata.aph505EvidenceFragment.beforeAfterRole);
        CollectionAssert.AreEqual(new[] { "near", "medium", "far" }, metadata.aph505EvidenceFragment.capturedViews);
        CollectionAssert.AreEqual(options.CandidatePaths, metadata.aph505EvidenceFragment.candidatePaths);
        Assert.False(metadata.aph505EvidenceFragment.beforeAfterPairsComplete);
        Assert.False(metadata.aph505EvidenceFragment.accepted);
    }

    private static MobileVisualQualityCaptureMatrix.Options CreateOptions(string profile, string aspect)
    {
        return CreateOptions(profile, EnvironmentValues(aspect));
    }

    private static MobileVisualQualityCaptureMatrix.Options CreateOptions(
        string profile,
        Dictionary<string, string> values)
    {
            return MobileVisualQualityCaptureMatrix.CreateOptions(
                profile,
                MobileVisualQualityCaptureMatrix.ArtifactDirectory,
                key => values.TryGetValue(key, out string value) ? value : null);
    }

    private static Dictionary<string, string> EnvironmentValues(string aspect)
    {
        return new Dictionary<string, string>
        {
            [MobileVisualQualityCaptureMatrix.AspectEnvironmentVariable] = aspect,
            [MobileVisualQualityCaptureMatrix.RevisionEnvironmentVariable] = new string('a', 40),
            [MobileVisualQualityCaptureMatrix.DirtyEnvironmentVariable] = "false",
            [MobileVisualQualityCaptureMatrix.DeviceProfileEnvironmentVariable] = "editor-windowed",
            [MobileVisualQualityCaptureMatrix.FrameRateModeEnvironmentVariable] = "60fps"
        };
    }

    private static MobileVisualQualityCaptureMatrix.CameraContract ValidContract(string aspect)
    {
        MobileVisualQualityCaptureMatrix.CameraPose near = Pose(10f);
        MobileVisualQualityCaptureMatrix.CameraPose medium = Pose(24f);
        MobileVisualQualityCaptureMatrix.CameraPose far = Pose(45f);
        return new MobileVisualQualityCaptureMatrix.CameraContract
        {
            aspect = aspect,
            near = near,
            medium = medium,
            far = far,
            gameplayZoom = Pose(24f),
            maximumZoomOut = Pose(45f)
        };
    }

    private static MobileVisualQualityCaptureMatrix.CameraPose Pose(float height)
    {
        return new MobileVisualQualityCaptureMatrix.CameraPose
        {
            position = new[] { 100f, height, 200f },
            rotation = new[] { 58f, 10f, 0f },
            fieldOfView = 36f,
            orthographic = false,
            orthographicSize = 24f
        };
    }
}
#endif
