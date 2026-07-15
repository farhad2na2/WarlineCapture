using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Game.Composition;
using Game.Runtime;
using UnityEngine;

namespace Game.Editor
{
    internal delegate bool MobileVisualQualityCameraRender(
        Camera camera,
        string path,
        int width,
        int height,
        bool requireGraphicsDevice,
        out string error);

    internal sealed class MobileVisualQualityCaptureMatrix
    {
        internal const string ModeEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_MODE";
        internal const string MatrixMode = "matrix";
        internal const string AspectEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_ASPECT";
        internal const string RevisionEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_REVISION";
        internal const string DirtyEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_DIRTY";
        internal const string DeviceProfileEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_DEVICE_PROFILE";
        internal const string FrameRateModeEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_FRAME_RATE_MODE";
        internal const string CameraContractEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_CAMERA_CONTRACT";
        internal const string CandidatePlanEnvironmentVariable = "WARLINE_MOBILE_VISUAL_CAPTURE_APH505_CANDIDATE_PLAN";
        internal const string ArtifactDirectory =
            "Design/AgentReports/Captures/ArchitecturePerformanceHardening/APH-809";

        private const int EnvironmentSettleFrames = 24;
        private const int ZoomSettleFrames = 90;
        private const float DayHour = 12f;
        private const float DuskHour = 21f;
        private const float NightHour = 23f;

        private enum Phase
        {
            NotStarted,
            WaitingForDay,
            WaitingForNear,
            WaitingForMedium,
            WaitingForFar,
            WaitingForDusk,
            WaitingForNight,
            CandidateWaitingForNight,
            Complete
        }

        [Serializable]
        internal sealed class Options
        {
            public string Aspect;
            public string AspectToken;
            public string Profile;
            public string ArtifactDirectory;
            public string CameraContractPath;
            public string MetadataPath;
            public string Revision;
            public bool Dirty;
            public string DeviceProfile;
            public string FrameRateMode;
            public string[] CandidatePaths = Array.Empty<string>();
            public int Width;
            public int Height;

            public bool IsCurrent => string.Equals(Profile, "current", StringComparison.Ordinal);
        }

        [Serializable]
        internal sealed class CameraPose
        {
            public float[] position = new float[3];
            public float[] rotation = new float[3];
            public float fieldOfView;
            public bool orthographic;
            public float orthographicSize;
        }

        [Serializable]
        internal sealed class CameraContract
        {
            public int schemaVersion = 1;
            public string aspect;
            public CameraPose gameplayZoom;
            public CameraPose maximumZoomOut;
            public CameraPose near;
            public CameraPose medium;
            public CameraPose far;
        }

        [Serializable]
        internal sealed class ArtifactMetadata
        {
            public string rowId;
            public string role;
            public string path;
            public string sha256;
            public int width;
            public int height;
            public string capturedAtUtc;
            public string revision;
            public string deviceProfile;
            public string frameRateMode;
            public string qualityTier;
            public float[] cameraPosition;
            public float[] cameraRotation;
            public string state;
        }

        [Serializable]
        internal sealed class Aph505EvidenceFragment
        {
            public int schemaVersion = 1;
            public string taskId = "APH-505";
            public string status = "capture-session";
            public string exactCommit;
            public bool dirty;
            public string[] candidatePaths;
            public string[] capturedViews = { "near", "medium", "far" };
            public string beforeAfterRole;
            public bool beforeAfterPairsComplete;
            public bool accepted;
        }

        [Serializable]
        internal sealed class SessionMetadata
        {
            public int schemaVersion = 1;
            public string taskId = "APH-809";
            public string revision;
            public bool dirty;
            public string deviceProfile;
            public string frameRateMode;
            public string aspect;
            public string profile;
            public string cameraContractPath;
            public int artifactCount;
            public ArtifactMetadata[] artifacts;
            public Aph505EvidenceFragment aph505EvidenceFragment;
        }

        [Serializable]
        private sealed class CandidatePlan
        {
            public string[] proposedCandidatePaths;
        }

        private readonly Options options;
        private readonly Action<MatchSceneView, float> applyTime;
        private readonly MobileVisualQualityCameraRender render;
        private readonly List<ArtifactMetadata> artifacts = new();
        private CameraContract cameraContract;
        private Phase phase;
        private int phaseFrame;
        private bool menuReady;

        private MobileVisualQualityCaptureMatrix(
            Options options,
            Action<MatchSceneView, float> applyTime,
            MobileVisualQualityCameraRender render)
        {
            this.options = options;
            this.applyTime = applyTime ?? throw new ArgumentNullException(nameof(applyTime));
            this.render = render ?? throw new ArgumentNullException(nameof(render));
            RequireCurrentGitEvidence(options);
            PrepareOutputFiles();
            if (!options.IsCurrent)
                cameraContract = LoadCameraContract(options.CameraContractPath, options.Aspect);
        }

        internal string MetadataPath => options.MetadataPath;
        internal string PhaseLabel => phase.ToString();

        internal static MobileVisualQualityCaptureMatrix TryCreateFromEnvironment(
            string profile,
            string artifactDirectory,
            Action<MatchSceneView, float> applyTime,
            MobileVisualQualityCameraRender render)
        {
            if (!string.Equals(
                    Environment.GetEnvironmentVariable(ModeEnvironmentVariable),
                    MatrixMode,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Options resolved = CreateOptions(
                profile,
                artifactDirectory,
                Environment.GetEnvironmentVariable);
            return new MobileVisualQualityCaptureMatrix(resolved, applyTime, render);
        }

        internal static Options CreateOptions(
            string profile,
            string artifactDirectory,
            Func<string, string> readEnvironment)
        {
            if (!string.Equals(profile, "current", StringComparison.Ordinal) &&
                !string.Equals(profile, "candidate", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Matrix capture profile must be current or candidate: {profile}");
            }

            string aspect = Require(readEnvironment, AspectEnvironmentVariable);
            int width;
            string aspectToken;
            if (string.Equals(aspect, "16:9", StringComparison.Ordinal))
            {
                width = 1920;
                aspectToken = "16x9";
            }
            else if (string.Equals(aspect, "20:9", StringComparison.Ordinal))
            {
                width = 2400;
                aspectToken = "20x9";
            }
            else
            {
                throw new InvalidOperationException($"Matrix capture aspect must be 16:9 or 20:9: {aspect}");
            }

            string revision = Require(readEnvironment, RevisionEnvironmentVariable).ToLowerInvariant();
            if (revision.Length != 40 || !IsLowerHex(revision))
                throw new InvalidOperationException("Matrix capture revision must be a lowercase 40-character Git commit.");

            string dirtyText = Require(readEnvironment, DirtyEnvironmentVariable);
            if (!bool.TryParse(dirtyText, out bool dirty) || dirty)
                throw new InvalidOperationException("Matrix capture requires WARLINE_MOBILE_VISUAL_CAPTURE_DIRTY=false.");

            string directory = string.IsNullOrWhiteSpace(artifactDirectory)
                ? ArtifactDirectory
                : artifactDirectory.Replace('\\', '/').TrimEnd('/');
            if (!string.Equals(directory, ArtifactDirectory, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Matrix capture artifacts must use the canonical project-relative directory: {ArtifactDirectory}");
            }
            string contractPath = readEnvironment(CameraContractEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(contractPath))
                contractPath = Path.Combine(directory, $"aph809_camera_contract_{aspectToken}.json");

            string candidatePlanPath = readEnvironment(CandidatePlanEnvironmentVariable);
            string frameRateMode = Require(readEnvironment, FrameRateModeEnvironmentVariable);
            if (!string.Equals(frameRateMode, "30fps", StringComparison.Ordinal) &&
                !string.Equals(frameRateMode, "60fps", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Matrix capture frame-rate mode must be 30fps or 60fps: {frameRateMode}");
            }
            return new Options
            {
                Aspect = aspect,
                AspectToken = aspectToken,
                Profile = profile,
                ArtifactDirectory = directory,
                CameraContractPath = contractPath,
                MetadataPath = Path.Combine(directory, $"aph809_{aspectToken}_{profile}_capture_session.json"),
                Revision = revision,
                Dirty = false,
                DeviceProfile = Require(readEnvironment, DeviceProfileEnvironmentVariable),
                FrameRateMode = frameRateMode,
                CandidatePaths = ReadCandidatePaths(candidatePlanPath),
                Width = width,
                Height = 1080
            };
        }

        internal bool TryCaptureMenu(MenuBootstrapView bootstrap)
        {
            if (menuReady)
                return true;
            if (!options.IsCurrent)
            {
                menuReady = true;
                return true;
            }
            if (bootstrap == null || bootstrap.UiCamera == null)
                return false;

            Capture(
                bootstrap.UiCamera,
                $"menu-main-{options.AspectToken}",
                "capture",
                "main-menu-idle",
                CapturePose(bootstrap.UiCamera));
            menuReady = true;
            return true;
        }

        internal bool Tick(MatchSceneView matchScene, int frame)
        {
            Camera camera = matchScene != null ? matchScene.WorldCamera : null;
            SelectionUiCameraSystemHelper cameraInput = matchScene?.MatchBootstrap?.SelectionUiCamera;
            if (camera == null || cameraInput == null)
                throw new InvalidOperationException("Matrix capture requires the typed match camera owners.");

            switch (phase)
            {
                case Phase.NotStarted:
                    applyTime(matchScene, DayHour);
                    SetPhase(Phase.WaitingForDay, frame);
                    break;

                case Phase.WaitingForDay:
                    if (!HasSettled(frame, EnvironmentSettleFrames))
                        break;
                    if (options.IsCurrent)
                    {
                        if (!cameraInput.RequestZoomInLevel())
                            throw new InvalidOperationException("Current matrix capture could not enter the near zoom level.");
                        SetPhase(Phase.WaitingForNear, frame);
                    }
                    else
                    {
                        ApplyAndRequirePose(camera, cameraContract.gameplayZoom);
                        CaptureGraphics(camera, "gameplay-zoom", cameraContract.gameplayZoom);
                        ApplyAndRequirePose(camera, cameraContract.maximumZoomOut);
                        CaptureGraphics(camera, "max-zoom-out", cameraContract.maximumZoomOut);
                        applyTime(matchScene, NightHour);
                        SetPhase(Phase.CandidateWaitingForNight, frame);
                    }
                    break;

                case Phase.WaitingForNear:
                    if (!HasSettled(frame, ZoomSettleFrames))
                        break;
                    CameraPose near = CapturePose(camera);
                    CaptureCurrentView(camera, "near", near);
                    cameraContract = new CameraContract { aspect = options.Aspect, near = near };
                    if (!cameraInput.RequestZoomOutLevel())
                        throw new InvalidOperationException("Current matrix capture could not return to the medium zoom level.");
                    SetPhase(Phase.WaitingForMedium, frame);
                    break;

                case Phase.WaitingForMedium:
                    if (!HasSettled(frame, ZoomSettleFrames))
                        break;
                    CameraPose medium = CapturePose(camera);
                    cameraContract.medium = medium;
                    cameraContract.gameplayZoom = ClonePose(medium);
                    CaptureGraphics(camera, "gameplay-zoom", medium);
                    Capture(camera, $"day-night-day-{options.AspectToken}", "capture", "day-12-00", medium);
                    CaptureCurrentView(camera, "medium", medium);
                    if (!cameraInput.RequestZoomOutLevel())
                        throw new InvalidOperationException("Current matrix capture could not enter the far zoom level.");
                    SetPhase(Phase.WaitingForFar, frame);
                    break;

                case Phase.WaitingForFar:
                    if (!HasSettled(frame, ZoomSettleFrames))
                        break;
                    CameraPose far = CapturePose(camera);
                    cameraContract.far = far;
                    cameraContract.maximumZoomOut = ClonePose(far);
                    ValidateCameraContract(cameraContract, options.Aspect);
                    SaveCameraContract(options.CameraContractPath, cameraContract);
                    CaptureGraphics(camera, "max-zoom-out", far);
                    CaptureCurrentView(camera, "far", far);
                    ApplyAndRequirePose(camera, cameraContract.medium);
                    applyTime(matchScene, DuskHour);
                    SetPhase(Phase.WaitingForDusk, frame);
                    break;

                case Phase.WaitingForDusk:
                    if (!HasSettled(frame, EnvironmentSettleFrames))
                        break;
                    ApplyAndRequirePose(camera, cameraContract.medium);
                    Capture(camera, $"day-night-dusk-{options.AspectToken}", "capture", "dusk-21-00", cameraContract.medium);
                    applyTime(matchScene, NightHour);
                    SetPhase(Phase.WaitingForNight, frame);
                    break;

                case Phase.WaitingForNight:
                    if (!HasSettled(frame, EnvironmentSettleFrames))
                        break;
                    ApplyAndRequirePose(camera, cameraContract.gameplayZoom);
                    Capture(camera, $"day-night-night-{options.AspectToken}", "capture", "night-23-00", cameraContract.gameplayZoom);
                    CaptureGraphics(camera, "night", cameraContract.gameplayZoom);
                    Finish();
                    break;

                case Phase.CandidateWaitingForNight:
                    if (!HasSettled(frame, EnvironmentSettleFrames))
                        break;
                    ApplyAndRequirePose(camera, cameraContract.gameplayZoom);
                    CaptureGraphics(camera, "night", cameraContract.gameplayZoom);
                    Finish();
                    break;
            }

            return phase == Phase.Complete;
        }

        internal static string ArtifactFileName(string rowId, string role)
        {
            if (string.IsNullOrWhiteSpace(rowId) || string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Matrix artifact row and role are required.");
            return $"{rowId}_{role}.png";
        }

        internal static CameraPose CapturePose(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            Vector3 position = camera.transform.position;
            Vector3 rotation = camera.transform.rotation.eulerAngles;
            return new CameraPose
            {
                position = new[] { position.x, position.y, position.z },
                rotation = new[] { rotation.x, rotation.y, rotation.z },
                fieldOfView = camera.fieldOfView,
                orthographic = camera.orthographic,
                orthographicSize = camera.orthographicSize
            };
        }

        internal static void ApplyAndRequirePose(Camera camera, CameraPose pose)
        {
            ValidatePose(pose, "camera pose");
            camera.transform.SetPositionAndRotation(
                new Vector3(pose.position[0], pose.position[1], pose.position[2]),
                Quaternion.Euler(pose.rotation[0], pose.rotation[1], pose.rotation[2]));
            camera.fieldOfView = pose.fieldOfView;
            camera.orthographic = pose.orthographic;
            camera.orthographicSize = pose.orthographicSize;

            CameraPose applied = CapturePose(camera);
            if (!PoseMatches(applied, pose, 0.001f))
                throw new InvalidOperationException("Camera did not accept the exact matrix capture pose.");
        }

        internal static bool PoseMatches(CameraPose left, CameraPose right, float tolerance)
        {
            if (left == null || right == null)
                return false;
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(left.position[i] - right.position[i]) > tolerance ||
                    Mathf.Abs(Mathf.DeltaAngle(left.rotation[i], right.rotation[i])) > tolerance)
                {
                    return false;
                }
            }
            return Mathf.Abs(left.fieldOfView - right.fieldOfView) <= tolerance &&
                   left.orthographic == right.orthographic &&
                   Mathf.Abs(left.orthographicSize - right.orthographicSize) <= tolerance;
        }

        internal static void SaveCameraContract(string path, CameraContract contract)
        {
            ValidateCameraContract(contract, contract?.aspect);
            EnsureParentDirectory(path);
            File.WriteAllText(path, JsonUtility.ToJson(contract, true));
        }

        internal static CameraContract LoadCameraContract(string path, string expectedAspect)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Candidate capture requires the current-profile camera contract.", path);
            CameraContract contract = JsonUtility.FromJson<CameraContract>(File.ReadAllText(path));
            ValidateCameraContract(contract, expectedAspect);
            return contract;
        }

        internal static void ValidateCameraContract(CameraContract contract, string expectedAspect)
        {
            if (contract == null || contract.schemaVersion != 1 ||
                !string.Equals(contract.aspect, expectedAspect, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Matrix camera contract schema or aspect does not match the capture.");
            }

            ValidatePose(contract.gameplayZoom, "gameplay zoom");
            ValidatePose(contract.maximumZoomOut, "maximum zoom out");
            ValidatePose(contract.near, "near");
            ValidatePose(contract.medium, "medium");
            ValidatePose(contract.far, "far");
            if (!PoseMatches(contract.gameplayZoom, contract.medium, 0.001f) ||
                !PoseMatches(contract.maximumZoomOut, contract.far, 0.001f))
            {
                throw new InvalidDataException("Matrix camera aliases must preserve exact gameplay/medium and maximum/far transforms.");
            }
            if (!(contract.near.position[1] < contract.medium.position[1] &&
                  contract.medium.position[1] < contract.far.position[1]))
            {
                throw new InvalidDataException("Matrix near, medium, and far camera heights must be strictly increasing.");
            }
        }

        private void CaptureCurrentView(Camera camera, string distance, CameraPose pose)
        {
            Capture(camera, $"static-map-{distance}-{options.AspectToken}", "capture", $"{distance}-chunk-readability", pose);
            Capture(camera, $"mip-streaming-{distance}-{options.AspectToken}", "capture", $"{distance}-settled", pose);
        }

        private void CaptureGraphics(Camera camera, string viewpoint, CameraPose pose)
        {
            string state = viewpoint switch
            {
                "gameplay-zoom" => "gameplay-zoom-current-vs-candidate",
                "max-zoom-out" => "max-zoom-out-current-vs-candidate",
                _ => "night-current-vs-candidate"
            };
            Capture(camera, $"graphics-tier-{viewpoint}-{options.AspectToken}", options.Profile, state, pose);
        }

        private void Capture(Camera camera, string rowId, string role, string state, CameraPose canonicalPose)
        {
            string fileName = ArtifactFileName(rowId, role);
            string path = Path.Combine(options.ArtifactDirectory, fileName);
            if (!render(camera, path, options.Width, options.Height, true, out string error))
                throw new InvalidOperationException(error);
            if (!File.Exists(path))
                throw new InvalidOperationException($"Matrix renderer did not emit the required PNG: {path}");

            string capturedAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            artifacts.Add(new ArtifactMetadata
            {
                rowId = rowId,
                role = role,
                path = path.Replace('\\', '/'),
                sha256 = ComputeSha256(path),
                width = options.Width,
                height = options.Height,
                capturedAtUtc = capturedAt,
                revision = options.Revision,
                deviceProfile = options.DeviceProfile,
                frameRateMode = options.FrameRateMode,
                qualityTier = options.Profile,
                cameraPosition = CloneVector(canonicalPose.position),
                cameraRotation = CloneVector(canonicalPose.rotation),
                state = state
            });
        }

        private void Finish()
        {
            int expected = options.IsCurrent ? 13 : 3;
            if (artifacts.Count != expected)
                throw new InvalidDataException($"Matrix capture emitted {artifacts.Count} artifacts; expected {expected}.");

            RequireCurrentGitEvidence(options);
            SessionMetadata metadata = CreateSessionMetadata(options, artifacts);
            EnsureParentDirectory(options.MetadataPath);
            File.WriteAllText(options.MetadataPath, JsonUtility.ToJson(metadata, true));
            phase = Phase.Complete;
        }

        internal static SessionMetadata CreateSessionMetadata(
            Options resolved,
            IReadOnlyList<ArtifactMetadata> capturedArtifacts)
        {
            ArtifactMetadata[] captured = new ArtifactMetadata[capturedArtifacts.Count];
            for (int i = 0; i < captured.Length; i++)
                captured[i] = capturedArtifacts[i];
            return new SessionMetadata
            {
                revision = resolved.Revision,
                dirty = resolved.Dirty,
                deviceProfile = resolved.DeviceProfile,
                frameRateMode = resolved.FrameRateMode,
                aspect = resolved.Aspect,
                profile = resolved.Profile,
                cameraContractPath = resolved.CameraContractPath.Replace('\\', '/'),
                artifactCount = captured.Length,
                artifacts = captured,
                aph505EvidenceFragment = new Aph505EvidenceFragment
                {
                    exactCommit = resolved.Revision,
                    dirty = resolved.Dirty,
                    candidatePaths = resolved.CandidatePaths,
                    beforeAfterRole = resolved.Profile,
                    beforeAfterPairsComplete = false,
                    accepted = false
                }
            };
        }

        private void PrepareOutputFiles()
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            foreach ((string rowId, string role) in ExpectedArtifacts(options))
                DeleteIfExists(Path.Combine(options.ArtifactDirectory, ArtifactFileName(rowId, role)));
            DeleteIfExists(options.MetadataPath);
            if (options.IsCurrent)
                DeleteIfExists(options.CameraContractPath);
        }

        internal static IReadOnlyList<(string rowId, string role)> ExpectedArtifacts(Options resolved)
        {
            List<(string rowId, string role)> expected = new();
            string token = resolved.AspectToken;
            if (resolved.IsCurrent)
            {
                expected.Add(($"menu-main-{token}", "capture"));
                expected.Add(($"day-night-day-{token}", "capture"));
                expected.Add(($"day-night-dusk-{token}", "capture"));
                expected.Add(($"day-night-night-{token}", "capture"));
                foreach (string distance in new[] { "near", "medium", "far" })
                {
                    expected.Add(($"static-map-{distance}-{token}", "capture"));
                    expected.Add(($"mip-streaming-{distance}-{token}", "capture"));
                }
            }
            foreach (string viewpoint in new[] { "gameplay-zoom", "max-zoom-out", "night" })
                expected.Add(($"graphics-tier-{viewpoint}-{token}", resolved.Profile));
            return expected;
        }

        private bool HasSettled(int frame, int requiredFrames)
        {
            return frame - phaseFrame >= requiredFrames;
        }

        private void SetPhase(Phase next, int frame)
        {
            phase = next;
            phaseFrame = frame;
        }

        private static string[] ReadCandidatePaths(string planPath)
        {
            if (string.IsNullOrWhiteSpace(planPath))
                return Array.Empty<string>();
            if (!File.Exists(planPath))
                throw new FileNotFoundException("APH-505 candidate plan was requested but is missing.", planPath);
            CandidatePlan plan = JsonUtility.FromJson<CandidatePlan>(File.ReadAllText(planPath));
            return plan?.proposedCandidatePaths ?? Array.Empty<string>();
        }

        private static string Require(Func<string, string> readEnvironment, string variable)
        {
            string value = readEnvironment(variable);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Matrix capture requires {variable}.");
            return value.Trim();
        }

        internal static void ValidateGitEvidence(
            Options resolved,
            string actualRevision,
            bool actualDirty)
        {
            if (resolved == null)
                throw new ArgumentNullException(nameof(resolved));
            if (!string.Equals(actualRevision, resolved.Revision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Matrix capture revision changed or does not match HEAD: expected={resolved.Revision} actual={actualRevision}");
            }
            if (actualDirty)
                throw new InvalidOperationException("Matrix capture requires a clean source tree.");
        }

        private static void RequireCurrentGitEvidence(Options resolved)
        {
            string revision = RunGit("rev-parse HEAD").Trim().ToLowerInvariant();
            string status = RunGit(
                $"status --porcelain --untracked-files=normal -- . :(exclude){ArtifactDirectory}/**");
            ValidateGitEvidence(resolved, revision, !string.IsNullOrWhiteSpace(status));
        }

        private static string RunGit(string arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (!process.Start())
                throw new InvalidOperationException($"Could not start git {arguments}.");
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(10000))
                throw new TimeoutException($"git {arguments} timed out.");
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"git {arguments} failed with exit code {process.ExitCode}: {standardError.Trim()}");
            }
            return standardOutput;
        }

        private static bool IsLowerHex(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                    return false;
            }
            return true;
        }

        private static CameraPose ClonePose(CameraPose pose)
        {
            return new CameraPose
            {
                position = CloneVector(pose.position),
                rotation = CloneVector(pose.rotation),
                fieldOfView = pose.fieldOfView,
                orthographic = pose.orthographic,
                orthographicSize = pose.orthographicSize
            };
        }

        private static float[] CloneVector(float[] values)
        {
            return new[] { values[0], values[1], values[2] };
        }

        private static void ValidatePose(CameraPose pose, string label)
        {
            if (pose?.position == null || pose.rotation == null ||
                pose.position.Length != 3 || pose.rotation.Length != 3)
            {
                throw new InvalidDataException($"Matrix {label} is incomplete.");
            }
            for (int i = 0; i < 3; i++)
            {
                if (!IsFinite(pose.position[i]) || !IsFinite(pose.rotation[i]))
                    throw new InvalidDataException($"Matrix {label} contains a non-finite transform.");
            }
            if (!IsFinite(pose.fieldOfView) || !IsFinite(pose.orthographicSize))
                throw new InvalidDataException($"Matrix {label} contains non-finite projection data.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void EnsureParentDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
