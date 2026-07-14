import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI import aph502_texture_importer_classification as classification


class Aph502TextureImporterClassificationTests(unittest.TestCase):
    HEAD = "0123456789abcdef0123456789abcdef01234567"

    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.content_path = self.root / "content.json"
        self.report_path = self.root / "report.json"
        self.write_json(
            self.content_path,
            {
                "status": "complete",
                "baselineCommit": self.HEAD,
                "summary": {"textureAssetCount": 0},
                "assets": [],
            },
        )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def test_complete_export_is_accepted_with_exact_clean_provenance(self) -> None:
        self.write_report()

        paths, details, final_buckets_accepted = self.read_evidence()

        self.assertTrue(final_buckets_accepted)
        self.assertEqual(
            {"Assets/Textures/A.png", "Assets/Textures/Z.png"},
            paths,
        )
        report_details = details["buildReports"][0]
        self.assertTrue(report_details["acceptedForCurrentRevision"])
        self.assertTrue(report_details["completeTexturePathExportMarker"])
        self.assertTrue(report_details["completeTexturePathExport"])
        self.assertEqual(2, report_details["completeTextureRows"])

    def test_missing_completion_marker_rejects_all_inclusion_paths(self) -> None:
        report = self.report_document()
        report.pop("allIncludedTexturePathsExported")
        report.pop("buildReportIncludedTextures")
        self.write_json(self.report_path, report)

        paths, details, final_buckets_accepted = self.read_evidence()

        self.assertFalse(final_buckets_accepted)
        self.assertEqual(set(), paths)
        report_details = details["buildReports"][0]
        self.assertFalse(report_details["acceptedForCurrentRevision"])
        self.assertFalse(report_details["completeTexturePathExportMarker"])
        self.assertFalse(report_details["completeTexturePathExport"])

    def test_complete_export_rejects_non_deterministic_or_invalid_rows(self) -> None:
        invalid_rows = {
            "unsorted": [self.texture_row("Assets/Textures/Z.png"), self.texture_row("Assets/Textures/A.png")],
            "duplicate": [self.texture_row("Assets/Textures/A.png"), self.texture_row("Assets/Textures/A.png")],
            "backslash": [self.texture_row("Assets\\Textures\\A.png")],
            "dot_prefix": [self.texture_row("./Assets/Textures/A.png")],
            "non_texture": [
                {
                    "sourceAssetPath": "Assets/Textures/A.png",
                    "packedBytes": 1,
                    "objectTypes": ["UnityEngine.Sprite"],
                }
            ],
        }

        for case_name, rows in invalid_rows.items():
            with self.subTest(case=case_name):
                report = self.report_document()
                report["buildReportIncludedTextures"] = rows
                self.write_json(self.report_path, report)

                paths, details, final_buckets_accepted = self.read_evidence()

                self.assertFalse(final_buckets_accepted)
                self.assertEqual(set(), paths)
                self.assertFalse(details["buildReports"][0]["completeTexturePathExport"])

    def test_complete_export_rejects_dirty_mismatched_or_dirty_worktree_provenance(self) -> None:
        cases = (
            ("dirty_report", {"dirty": True}, False),
            ("revision_mismatch", {"exactCommit": "f" * 40}, False),
            ("schema_mismatch", {"schemaVersion": 2}, False),
            ("dirty_worktree", {}, True),
        )

        for case_name, overrides, tracked_worktree_dirty in cases:
            with self.subTest(case=case_name):
                report = self.report_document()
                report.update(overrides)
                self.write_json(self.report_path, report)

                paths, details, final_buckets_accepted = self.read_evidence(tracked_worktree_dirty)

                self.assertFalse(final_buckets_accepted)
                self.assertEqual(set(), paths)
                self.assertFalse(details["buildReports"][0]["acceptedForCurrentRevision"])

    def test_internally_inconsistent_residency_inventory_fails_closed(self) -> None:
        self.write_json(
            self.content_path,
            {
                "status": "complete",
                "baselineCommit": self.HEAD,
                "summary": {"textureAssetCount": 2},
                "assets": [
                    {
                        "assetPath": "Assets/Textures/A.png",
                        "assetType": "Texture2D",
                    }
                ],
            },
        )
        self.write_report()

        paths, details, final_buckets_accepted = self.read_evidence()

        self.assertFalse(final_buckets_accepted)
        self.assertEqual(set(), paths)
        content_details = details["contentResidency"]
        self.assertFalse(content_details["completeInventory"])
        self.assertFalse(content_details["acceptedForCurrentRevision"])
        self.assertIn(
            "summary-texture-asset-count-mismatch:2!=1",
            content_details["validationErrors"],
        )

    def test_missing_or_malformed_evidence_fails_closed_without_raising(self) -> None:
        missing_content = self.root / "missing-content.json"
        malformed_report = self.root / "malformed-report.json"
        malformed_report.write_text("{not-json", encoding="utf-8")

        paths, details, final_buckets_accepted = classification.revision_checked_evidence(
            self.HEAD,
            False,
            missing_content,
            (malformed_report,),
        )

        self.assertFalse(final_buckets_accepted)
        self.assertEqual(set(), paths)
        self.assertIn("file-missing", details["contentResidency"]["validationErrors"])
        self.assertTrue(
            any(
                error.startswith("json-invalid:")
                for error in details["buildReports"][0]["validationErrors"]
            )
        )

    def test_category_candidate_and_overlap_counts_are_derived_from_rows(self) -> None:
        paths = (
            "Assets/Game/Art/UI/A_Normal.png.meta",
            "Assets/World/Grass.png.meta",
            "Assets/VFX/Smoke.png.meta",
            "Assets/Game/Art/UI/Generated/References/Logo.png.meta",
        )
        for path in paths:
            texture_type = 8 if "/UI/" in path else 0
            sprite_mode = 1 if "/UI/" in path else 0
            absolute = self.root / path
            absolute.parent.mkdir(parents=True, exist_ok=True)
            absolute.write_text(
                "fileFormatVersion: 2\n"
                "TextureImporter:\n"
                f"  spriteMode: {sprite_mode}\n"
                f"  textureType: {texture_type}\n",
                encoding="utf-8",
            )

        rows = classification.classify_rows(
            self.root,
            paths,
            {"Assets/World/Grass.png"},
        )
        summary = classification.summarize_rows(rows)

        self.assertEqual(len(paths), summary["trackedTextureImporterCount"])
        self.assertEqual(len(paths), sum(summary["chosenSemanticCounts"].values()))
        self.assertEqual(1, summary["chosenSemanticCounts"]["UI"])
        self.assertEqual(1, summary["chosenSemanticCounts"]["world albedo"])
        self.assertEqual(1, summary["chosenSemanticCounts"]["VFX"])
        self.assertEqual(1, summary["chosenSemanticCounts"]["generated source/reference"])
        self.assertEqual(2, summary["semanticCandidateCounts"]["UI"])
        self.assertEqual(1, summary["semanticCandidateCounts"]["world normal/mask"])
        self.assertEqual(2, summary["ambiguityCount"])
        self.assertEqual(2, sum(summary["semanticOverlapCounts"].values()))
        self.assertEqual(1, summary["evidenceStatusCounts"]["accepted current inclusion"])

    def test_generated_reports_are_deterministic_and_staleness_is_detected(self) -> None:
        data = classification.inventory()
        report_root = self.root / "reports"
        (report_root / classification.JSON_REPORT_PATH.parent).mkdir(parents=True)

        classification.write_reports(report_root, data)

        self.assertEqual([], classification.report_check_errors(report_root, data))
        markdown_path = report_root / classification.MARKDOWN_REPORT_PATH
        markdown_path.write_text(
            markdown_path.read_text(encoding="utf-8") + "stale\n",
            encoding="utf-8",
        )
        self.assertIn(
            f"generated-report-stale:{classification.MARKDOWN_REPORT_PATH.as_posix()}",
            classification.report_check_errors(report_root, data),
        )

    def read_evidence(
        self,
        tracked_worktree_dirty: bool = False,
    ) -> tuple[set[str], dict[str, object], bool]:
        return classification.revision_checked_evidence(
            self.HEAD,
            tracked_worktree_dirty,
            self.content_path,
            (self.report_path,),
        )

    def write_report(self) -> None:
        self.write_json(self.report_path, self.report_document())

    def report_document(self) -> dict[str, object]:
        return {
            "schemaVersion": 1,
            "taskId": "APH-500",
            "status": "complete",
            "exactCommit": self.HEAD,
            "dirty": False,
            "releaseBuildType": "release",
            "buildTarget": "Android",
            "detailedBuildReport": True,
            "allIncludedTexturePathsExported": True,
            "buildReportIncludedAssets": [self.texture_row("Assets/Textures/TopTable.png")],
            "buildReportIncludedTextures": [
                self.texture_row("Assets/Textures/A.png"),
                self.texture_row("Assets/Textures/Z.png"),
            ],
        }

    @staticmethod
    def texture_row(path: str) -> dict[str, object]:
        return {
            "sourceAssetPath": path,
            "packedBytes": 1,
            "objectTypes": ["UnityEngine.Texture2D"],
        }

    @staticmethod
    def write_json(path: Path, document: dict[str, object]) -> None:
        path.write_text(json.dumps(document), encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
