from __future__ import annotations

import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI import aph510_android_category_comparison as comparison


BASELINE_REVISION = "1" * 40
CANDIDATE_REVISION = "2" * 40


class Aph510AndroidCategoryComparisonTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.baseline_root = self.root / "baseline"
        self.candidate_root = self.root / "candidate"
        self.baseline_metrics = {
            "installedSizeBytes": 1_000,
            "peakAllocatedMemoryMB": 100.0,
            "peakPssMemoryMB": 120.0,
            "frameP95Ms": 20.0,
            "frameP99Ms": 25.0,
            "startupP95Ms": 1_000.0,
            "textureMemoryBytes": 800,
            "meshMemoryBytes": 600,
            "audioMemoryBytes": 400,
            "graphicsDriverMemoryBytes": 700,
            "ioReadBytes": 10_000,
            "ioWriteBytes": 2_000,
        }
        self.candidate_metrics = {
            "installedSizeBytes": 900,
            "peakAllocatedMemoryMB": 90.0,
            "peakPssMemoryMB": 100.0,
            "frameP95Ms": 18.0,
            "frameP99Ms": 22.0,
            "startupP95Ms": 900.0,
            "textureMemoryBytes": 700,
            "meshMemoryBytes": 500,
            "audioMemoryBytes": 350,
            "graphicsDriverMemoryBytes": 650,
            "ioReadBytes": 9_000,
            "ioWriteBytes": 1_800,
        }
        self.baseline_path, self.baseline_identity = self.create_bundle(
            self.baseline_root,
            "baseline",
            BASELINE_REVISION,
            self.baseline_metrics,
        )
        self.candidate_path, self.candidate_identity = self.create_bundle(
            self.candidate_root,
            "candidate",
            CANDIDATE_REVISION,
            self.candidate_metrics,
        )
        self.limits_path = self.root / "limits.json"
        self.write_json(self.limits_path, self.limits_document())

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    @staticmethod
    def write_json(path: Path, value: object) -> str:
        path.parent.mkdir(parents=True, exist_ok=True)
        content = json.dumps(value, indent=2, sort_keys=True) + "\n"
        path.write_text(content, encoding="utf-8")
        return hashlib.sha256(content.encode("utf-8")).hexdigest()

    @staticmethod
    def descriptor(path: Path, root: Path, *, sized: bool = False) -> dict[str, object]:
        result: dict[str, object] = {
            "path": path.relative_to(root).as_posix(),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        }
        if sized:
            result["sizeBytes"] = path.stat().st_size
        return result

    @staticmethod
    def profile_document() -> dict[str, object]:
        return {
            "schemaVersion": 1,
            "taskId": "APH-804",
            "device": {
                "serial": "DEVICE-01",
                "manufacturer": "Example",
                "model": "ReferencePhone",
                "deviceCodeName": "reference",
                "soc": "Reference SOC",
                "androidRelease": "16",
                "sdkLevel": 36,
                "resolutionWidth": 2400,
                "resolutionHeight": 1080,
            },
            "build": {
                "packageName": "com.warlinecapture.game",
                "buildType": "release",
                "scriptingBackend": "IL2CPP",
                "architecture": "ARM64",
                "qualityTier": "Mobile",
                "requestedFrameRate": 60,
                "actualFrameRate": 60,
            },
            "capture": {
                "warmupSeconds": 60,
                "sustainedSampleSeconds": 600,
            },
        }

    @staticmethod
    def identity_document(
        revision: str,
        apk_sha256: str,
        aab_sha256: str,
        profile_sha256: str,
    ) -> dict[str, object]:
        return {
            "exactCommit": revision,
            "dirty": False,
            "apkSha256": apk_sha256,
            "aabSha256": aab_sha256,
            "buildProfileSha256": profile_sha256,
            "buildType": "release",
            "packageName": "com.warlinecapture.game",
            "scriptingBackend": "IL2CPP",
            "targetArchitecture": "ARM64",
            "qualityTier": "Mobile",
            "requestedFrameRate": 60,
            "actualFrameRate": 60,
            "deviceProfile": "reference-android-60fps-v1",
            "deviceSerial": "DEVICE-01",
            "deviceManufacturer": "Example",
            "deviceModel": "ReferencePhone",
            "deviceCodeName": "reference",
            "soc": "Reference SOC",
            "androidRelease": "16",
            "sdkLevel": 36,
            "resolutionWidth": 2400,
            "resolutionHeight": 1080,
            "scenario": "match-steady-state",
            "warmupSeconds": 60,
            "sampleDurationSeconds": 600,
            "graphicsApi": "Vulkan",
        }

    @staticmethod
    def build_report(
        identity: dict[str, object],
        package_type: str,
        artifact: dict[str, object],
    ) -> dict[str, object]:
        return {
            "schemaVersion": 1,
            "taskId": "APH-500",
            "status": "complete",
            "exactCommit": identity["exactCommit"],
            "dirty": False,
            "releaseBuildType": "release",
            "packageType": package_type,
            "buildTarget": "Android",
            "scriptingBackend": "IL2CPP",
            "targetArchitecture": "ARM64",
            "detailedBuildReport": True,
            "artifactPath": artifact["path"],
            "artifactBytes": artifact["sizeBytes"],
            "artifactSha256": artifact["sha256"],
            "buildReportIncludedAssets": [
                {
                    "sourceAssetPath": "Assets/Test.asset",
                    "packedBytes": 1,
                    "objectTypes": ["UnityEngine.Object"],
                }
            ],
        }

    def create_bundle(
        self,
        root: Path,
        role: str,
        revision: str,
        metrics: dict[str, int | float],
    ) -> tuple[Path, dict[str, object]]:
        root.mkdir(parents=True)
        apk_path = root / "WarlineCapture.apk"
        aab_path = root / "WarlineCapture.aab"
        apk_path.write_bytes(f"{role}-apk-artifact".encode("ascii"))
        aab_path.write_bytes(f"{role}-aab-artifact".encode("ascii"))
        apk = self.descriptor(apk_path, root, sized=True)
        aab = self.descriptor(aab_path, root, sized=True)

        profile_path = root / "build_profile.json"
        profile_sha = self.write_json(profile_path, self.profile_document())
        identity = self.identity_document(
            revision,
            str(apk["sha256"]),
            str(aab["sha256"]),
            profile_sha,
        )

        sources: dict[str, dict[str, object]] = {
            "buildProfile": self.descriptor(profile_path, root),
        }
        for name, package_type, artifact in (
            ("apkBuildReport", "APK", apk),
            ("aabBuildReport", "AAB", aab),
        ):
            path = root / f"{name}.json"
            self.write_json(path, self.build_report(identity, package_type, artifact))
            sources[name] = self.descriptor(path, root)

        measurement_groups = {
            "devicePerformance": (
                "device-performance",
                (
                    "installedSizeBytes",
                    "peakAllocatedMemoryMB",
                    "peakPssMemoryMB",
                    "frameP95Ms",
                    "frameP99Ms",
                    "startupP95Ms",
                ),
            ),
            "categoryResidency": (
                "category-residency",
                (
                    "textureMemoryBytes",
                    "meshMemoryBytes",
                    "audioMemoryBytes",
                    "graphicsDriverMemoryBytes",
                ),
            ),
            "io": ("io", ("ioReadBytes", "ioWriteBytes")),
        }
        for source_name, (kind, names) in measurement_groups.items():
            path = root / f"{source_name}.json"
            self.write_json(
                path,
                {
                    "schemaVersion": 1,
                    "taskId": "APH-510",
                    "kind": kind,
                    "identity": identity,
                    "measurements": {name: metrics[name] for name in names},
                },
            )
            sources[source_name] = self.descriptor(path, root)

        manifest = {
            "schemaVersion": 1,
            "taskId": "APH-510",
            "role": role,
            "identity": identity,
            "artifacts": {"apk": apk, "aab": aab},
            "sources": sources,
        }
        manifest_path = root / "manifest.json"
        self.write_json(manifest_path, manifest)
        return manifest_path, identity

    def limits_document(self) -> dict[str, object]:
        candidate_values = {
            "apkSizeBytes": (self.candidate_root / "WarlineCapture.apk").stat().st_size,
            "aabSizeBytes": (self.candidate_root / "WarlineCapture.aab").stat().st_size,
            **self.candidate_metrics,
        }
        return {
            "schemaVersion": 1,
            "taskId": "APH-510",
            "limits": {
                metric: {
                    "comparison": "lessThanOrEqual",
                    "value": candidate_values[metric],
                    "status": "tracked-budget",
                }
                for metric in comparison.METRIC_NAMES
            },
        }

    def compare(self, *, expected_revision: str = CANDIDATE_REVISION) -> dict[str, object]:
        return comparison.compare_paths(
            baseline_path=self.baseline_path,
            candidate_path=self.candidate_path,
            limits_path=self.limits_path,
            baseline_artifact_root=self.baseline_root,
            candidate_artifact_root=self.candidate_root,
            expected_candidate_revision=expected_revision,
            expected_candidate_apk_sha256=str(self.candidate_identity["apkSha256"]),
            expected_candidate_aab_sha256=str(self.candidate_identity["aabSha256"]),
        )

    def rewrite_candidate_source(
        self,
        source_name: str,
        value: dict[str, object],
        *,
        update_hash: bool = True,
    ) -> None:
        manifest = json.loads(self.candidate_path.read_text(encoding="utf-8"))
        source_path = self.candidate_root / manifest["sources"][source_name]["path"]
        self.write_json(source_path, value)
        if update_hash:
            manifest["sources"][source_name]["sha256"] = hashlib.sha256(
                source_path.read_bytes()
            ).hexdigest()
            self.write_json(self.candidate_path, manifest)

    def test_complete_same_artifact_comparison_passes(self) -> None:
        result = self.compare()

        self.assertEqual("Passed", result["result"])
        self.assertTrue(result["acceptanceReady"])
        self.assertEqual(14, result["summary"]["comparisonCount"])
        self.assertEqual(14, result["summary"]["acceptedCount"])
        rows = {row["metric"]: row for row in result["comparisons"]}
        self.assertEqual(-100, rows["installedSizeBytes"]["delta"])
        self.assertEqual(-10, rows["frameP95Ms"]["deltaPercent"])
        self.assertEqual(
            self.candidate_identity["apkSha256"],
            result["candidate"]["identity"]["apkSha256"],
        )

    def test_mixed_artifact_source_is_rejected(self) -> None:
        path = self.candidate_root / "devicePerformance.json"
        source = json.loads(path.read_text(encoding="utf-8"))
        source["identity"]["apkSha256"] = "f" * 64
        self.rewrite_candidate_source("devicePerformance", source)

        with self.assertRaisesRegex(comparison.ComparisonValidationError, "mixed or stale: apkSha256"):
            self.compare()

    def test_mixed_revision_source_and_stale_candidate_are_rejected(self) -> None:
        path = self.candidate_root / "categoryResidency.json"
        source = json.loads(path.read_text(encoding="utf-8"))
        source["identity"]["exactCommit"] = "3" * 40
        self.rewrite_candidate_source("categoryResidency", source)

        with self.assertRaisesRegex(comparison.ComparisonValidationError, "mixed or stale: exactCommit"):
            self.compare()

        with self.assertRaisesRegex(comparison.ComparisonValidationError, "exactCommit mismatch"):
            self.compare(expected_revision="4" * 40)

    def test_baseline_candidate_device_identity_mismatch_is_rejected(self) -> None:
        manifest = json.loads(self.candidate_path.read_text(encoding="utf-8"))
        manifest["identity"]["deviceProfile"] = "different-device-profile"
        for source_name in comparison.SOURCE_KINDS:
            source_path = self.candidate_root / manifest["sources"][source_name]["path"]
            source = json.loads(source_path.read_text(encoding="utf-8"))
            source["identity"]["deviceProfile"] = "different-device-profile"
            self.write_json(source_path, source)
            manifest["sources"][source_name]["sha256"] = hashlib.sha256(
                source_path.read_bytes()
            ).hexdigest()
        self.write_json(self.candidate_path, manifest)

        with self.assertRaisesRegex(comparison.ComparisonValidationError, "deviceProfile"):
            self.compare()

    def test_missing_category_measurement_is_rejected(self) -> None:
        path = self.candidate_root / "categoryResidency.json"
        source = json.loads(path.read_text(encoding="utf-8"))
        del source["measurements"]["audioMemoryBytes"]
        self.rewrite_candidate_source("categoryResidency", source)

        with self.assertRaisesRegex(comparison.ComparisonValidationError, "audioMemoryBytes"):
            self.compare()

    def test_null_measurement_required_limit_is_explicit_non_acceptance(self) -> None:
        limits = json.loads(self.limits_path.read_text(encoding="utf-8"))
        limits["limits"]["textureMemoryBytes"] = {
            "comparison": "lessThanOrEqual",
            "value": None,
            "status": "measurement-required",
        }
        self.write_json(self.limits_path, limits)

        result = self.compare()
        row = next(
            item for item in result["comparisons"] if item["metric"] == "textureMemoryBytes"
        )

        self.assertEqual("NotAccepted", result["result"])
        self.assertFalse(result["acceptanceReady"])
        self.assertFalse(row["accepted"])
        self.assertEqual("measurement-required", row["decision"])
        self.assertIn("measurement-required / null", comparison.render_markdown(result))

    def test_tampered_hashed_source_is_rejected(self) -> None:
        path = self.candidate_root / "io.json"
        path.write_bytes(path.read_bytes() + b" ")

        with self.assertRaisesRegex(comparison.ComparisonValidationError, "sha256 does not match file"):
            self.compare()

    def test_missing_source_hash_is_rejected(self) -> None:
        manifest = json.loads(self.candidate_path.read_text(encoding="utf-8"))
        del manifest["sources"]["io"]["sha256"]
        self.write_json(self.candidate_path, manifest)

        with self.assertRaisesRegex(comparison.ComparisonValidationError, "sha256"):
            self.compare()

    def test_json_and_markdown_output_are_byte_deterministic(self) -> None:
        first = self.compare()
        second = self.compare()

        first_json = comparison.render_json(first)
        first_markdown = comparison.render_markdown(first)
        self.assertEqual(first_json, comparison.render_json(second))
        self.assertEqual(first_markdown, comparison.render_markdown(second))
        self.assertTrue(first_json.endswith("\n"))
        self.assertTrue(first_markdown.endswith("\n"))
        self.assertEqual(
            hashlib.sha256(first_json.encode("utf-8")).hexdigest(),
            hashlib.sha256(comparison.render_json(second).encode("utf-8")).hexdigest(),
        )


if __name__ == "__main__":
    unittest.main()
