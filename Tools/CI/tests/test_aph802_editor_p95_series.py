import json
import math
import tempfile
import unittest
from copy import deepcopy
from datetime import datetime, timezone
from pathlib import Path

from Tools.CI.aph802_editor_p95_series import (
    EXPECTED_RUNNER,
    SeriesValidationError,
    build_series,
    render_markdown,
    write_series_pair,
)


COMMIT = "a" * 40
NOW = datetime(2026, 7, 11, 13, 0, 0, tzinfo=timezone.utc)


class Aph802EditorP95SeriesTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.inputs = self.root / "runs"
        self.inputs.mkdir()

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def test_five_same_revision_pairs_compute_required_statistics_and_outlier(self) -> None:
        paths = self.write_series([4.0, 5.0, 6.0, 7.0, 8.0], outlier_index=4)
        series = build_series(paths, COMMIT, NOW, max_age_hours=4)

        stats = series["statistics"]
        self.assertEqual(5, series["acceptedRunCount"])
        self.assertEqual(4.0, stats["minimumP95FrameMs"])
        self.assertEqual(8.0, stats["maximumP95FrameMs"])
        self.assertEqual(6.0, stats["meanP95FrameMs"])
        self.assertEqual(6.0, stats["medianP95FrameMs"])
        self.assertAlmostEqual(math.sqrt(2.5), stats["sampleStandardDeviationMs"])
        self.assertAlmostEqual(math.sqrt(2.5) / 6.0, stats["coefficientOfVariation"])
        self.assertAlmostEqual(math.sqrt(2.5) * 100.0 / 6.0, stats["coefficientOfVariationPercent"])
        self.assertEqual(["aph802-run-05"], [row["artifactId"] for row in series["declaredOutliers"]])
        self.assertIn("Sample standard deviation", render_markdown(series))

    def test_writes_new_summary_pair_without_modifying_source_pairs(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        pair_paths = [item for json_path in paths for item in (json_path, json_path.with_suffix(".md"))]
        before = {path: path.read_bytes() for path in pair_paths}
        series = build_series(paths, COMMIT, NOW, max_age_hours=4)
        output_json = self.root / "summary" / "aph802-series.json"
        output_md = self.root / "summary" / "aph802-series.md"

        write_series_pair(series, output_json, output_md)

        self.assertTrue(output_json.is_file())
        self.assertTrue(output_md.is_file())
        self.assertEqual(before, {path: path.read_bytes() for path in before})
        self.assertEqual(series, json.loads(output_json.read_text(encoding="utf-8")))

    def test_rejects_fewer_than_five_accepted_runs(self) -> None:
        paths = self.write_series([4, 5, 6, 7])
        with self.assertRaisesRegex(SeriesValidationError, "at least 5 accepted"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_preserves_rejected_pair_but_excludes_it_from_statistics(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8, 100])
        run = self.read(paths[5])
        run["decision"] = {
            "status": "Rejected",
            "rejectionReasons": ["documented editor interaction during measurement"],
        }
        self.write_json(paths[5], run)

        series = build_series(paths, COMMIT, NOW, max_age_hours=4)

        self.assertEqual(5, series["acceptedRunCount"])
        self.assertEqual(1, series["rejectedRunCount"])
        self.assertEqual(8, series["statistics"]["maximumP95FrameMs"])
        self.assertEqual("Rejected", series["runs"][5]["status"])

    def test_rejects_missing_markdown_pair(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        paths[2].with_suffix(".md").unlink()
        with self.assertRaisesRegex(SeriesValidationError, "missing Markdown pair"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_mixed_commit(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        self.rewrite(paths[4], exactCommit="b" * 40)
        self.rewrite_markdown(paths[4])
        with self.assertRaisesRegex(SeriesValidationError, "mixed or unexpected commit"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_mixed_environment(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        run = self.read(paths[4])
        run["environment"]["machineId"] = "different-machine"
        self.write_json(paths[4], run)
        with self.assertRaisesRegex(SeriesValidationError, "mixed environment"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_mixed_runner_semantics(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        run = self.read(paths[4])
        run["runner"]["measurementSemantics"] = "different percentile window"
        self.write_json(paths[4], run)
        with self.assertRaisesRegex(SeriesValidationError, "mixed runner"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_mixed_fixture(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        run = self.read(paths[4])
        run["fixture"]["fixtureId"] = "different-fixture"
        self.write_json(paths[4], run)
        with self.assertRaisesRegex(SeriesValidationError, "mixed fixture"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_stale_and_future_inputs(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        self.rewrite(paths[0], capturedAtUtc="2026-07-10T01:00:00Z")
        with self.assertRaisesRegex(SeriesValidationError, "stale input"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

        self.rewrite(paths[0], capturedAtUtc="2026-07-11T14:00:00Z")
        with self.assertRaisesRegex(SeriesValidationError, "future capturedAtUtc"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_accepted_run_that_violates_fixture_contract(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        run = self.read(paths[3])
        run["measurements"]["unitCount"] = 699
        self.write_json(paths[3], run)
        with self.assertRaisesRegex(SeriesValidationError, "violates fixture contract: unit count"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_malformed_environment_and_measurement_types(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        run = self.read(paths[1])
        run["environment"]["unityVersion"] = ""
        self.write_json(paths[1], run)
        with self.assertRaisesRegex(SeriesValidationError, "environment.unityVersion"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

        run["environment"]["unityVersion"] = "6000.5.2f1"
        run["measurements"]["frameCount"] = 1000.5
        self.write_json(paths[1], run)
        with self.assertRaisesRegex(SeriesValidationError, "frameCount must be an integer"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_undeclared_outlier_metadata(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        run = self.read(paths[2])
        run["outlier"]["reason"] = "suspicious run"
        self.write_json(paths[2], run)
        with self.assertRaisesRegex(SeriesValidationError, "undeclared outlier"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

    def test_rejects_mismatched_markdown_marker_and_existing_output(self) -> None:
        paths = self.write_series([4, 5, 6, 7, 8])
        paths[0].with_suffix(".md").write_text(
            f"APH802-Artifact-Id: wrong-id\nAPH802-Exact-Commit: {COMMIT}\n",
            encoding="utf-8",
        )
        with self.assertRaisesRegex(SeriesValidationError, "Markdown artifact id does not match"):
            build_series(paths, COMMIT, NOW, max_age_hours=4)

        self.rewrite_markdown(paths[0])
        series = build_series(paths, COMMIT, NOW, max_age_hours=4)
        output_json = self.root / "summary.json"
        output_md = self.root / "summary.md"
        output_json.write_text("existing", encoding="utf-8")
        with self.assertRaisesRegex(SeriesValidationError, "already exists"):
            write_series_pair(series, output_json, output_md)

    def write_series(self, values, outlier_index=None):
        paths = []
        for index, value in enumerate(values):
            artifact_id = f"aph802-run-{index + 1:02d}"
            path = self.inputs / f"{artifact_id}.json"
            run = self.make_run(artifact_id, value, declared_outlier=index == outlier_index)
            self.write_json(path, run)
            self.rewrite_markdown(path)
            paths.append(path)
        return paths

    def make_run(self, artifact_id, p95, declared_outlier=False):
        return {
            "schema": "WarlineCapture.APH802EditorP95Run.v1",
            "artifactId": artifact_id,
            "capturedAtUtc": "2026-07-11T12:00:00Z",
            "exactCommit": COMMIT,
            "environment": {
                "unityVersion": "6000.5.2f1",
                "os": "macOS 15.5",
                "machineId": "ci-mac-01",
                "graphicsDevice": "Apple M2 Max",
                "qualityLevel": "MobileHigh",
                "resolution": {"width": 1920, "height": 1080},
                "captureMode": "windowed-batchmode",
                "commandLine": "invoke_unity_macos.sh -executeMethod baseline",
                "cacheStatePolicy": "warm-library-fresh-process",
            },
            "runner": {
                "executeMethod": EXPECTED_RUNNER,
                "measurementSemantics": "four-second stable Match frame delta p95",
            },
            "fixture": {
                "fixtureId": "match-standard-733u-628b-v1",
                "observationSeconds": 4.0,
                "minimumUnitCount": 700,
                "minimumRuntimeBuildingCount": 600,
                "readyGate": "MatchHudReady",
                "stableGate": "spawn-progressing-zero",
                "allocatedBytesBudget": 0,
            },
            "measurements": {
                "frameCount": 1000,
                "p95FrameMs": p95,
                "unitCount": 733,
                "runtimeBuildingCount": 628,
                "allocatedBytesCurrentThread": 0,
                "readyGatePassed": True,
                "stableGatePassed": True,
            },
            "decision": {"status": "Accepted", "rejectionReasons": []},
            "outlier": {
                "declared": declared_outlier,
                "rule": "Tukey 1.5 IQR" if declared_outlier else "",
                "reason": "above declared upper fence" if declared_outlier else "",
            },
        }

    def read(self, path):
        return json.loads(path.read_text(encoding="utf-8"))

    def rewrite(self, path, **overrides):
        run = self.read(path)
        run.update(overrides)
        self.write_json(path, run)

    def rewrite_markdown(self, path):
        run = self.read(path)
        path.with_suffix(".md").write_text(
            f"# Run\n\nAPH802-Artifact-Id: {run['artifactId']}\n"
            f"APH802-Exact-Commit: {run['exactCommit']}\n",
            encoding="utf-8",
        )

    @staticmethod
    def write_json(path, run):
        path.write_text(json.dumps(run, indent=2, sort_keys=True) + "\n", encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
