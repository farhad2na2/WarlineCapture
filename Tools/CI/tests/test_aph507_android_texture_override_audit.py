from __future__ import annotations

import binascii
import hashlib
import json
import struct
import tempfile
import unittest
import zlib
from pathlib import Path

from Tools.CI import aph507_android_texture_override_audit as audit


class Aph507AndroidTextureOverrideAuditTests(unittest.TestCase):
    HEAD = "0123456789abcdef0123456789abcdef01234567"
    ASSET = "Assets/Textures/Oversized.png"

    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary_directory.name)
        self.meta_path = f"{self.ASSET}.meta"
        self.content_path = self.root / "Design/AgentReports/content.json"
        self.build_path = self.root / "Design/AgentReports/build.json"
        self.visual_path = self.root / "Design/AgentReports/visual.json"
        self.write_texture(self.ASSET, 4096, 4096, self.meta_text())
        self.write_json(
            self.content_path,
            {
                "status": "complete",
                "baselineCommit": "f" * 40,
                "summary": {"textureAssetCount": 1},
                "assets": [self.content_row(self.ASSET, 4096, 4096)],
            },
        )

    def tearDown(self) -> None:
        self.temporary_directory.cleanup()

    def test_android_override_selection_and_default_fallback_are_explicit(self) -> None:
        override = audit.parse_texture_importer(
            self.meta_text(
                default_limit=8192,
                android_limit=2048,
                android_override=1,
                android_format=50,
            )
        )
        fallback = audit.parse_texture_importer(
            self.meta_text(
                default_limit=4096,
                android_limit=1024,
                android_override=0,
                android_format=48,
            )
        )

        self.assertTrue(override["androidOverrideEnabled"])
        self.assertEqual("Android", override["effectiveSettingsSource"])
        self.assertEqual(2048, override["androidMaxTextureSize"])
        self.assertEqual("ASTC_6x6", override["androidTextureFormat"])
        self.assertFalse(fallback["androidOverrideEnabled"])
        self.assertEqual("DefaultTexturePlatform", fallback["effectiveSettingsSource"])
        self.assertEqual(4096, fallback["androidMaxTextureSize"])
        self.assertEqual("Automatic", fallback["androidTextureFormat"])

    def test_duplicate_platform_blocks_fail_closed(self) -> None:
        duplicate = (
            "  - serializedVersion: 4\n"
            "    buildTarget: DefaultTexturePlatform\n"
            "    maxTextureSize: 4096\n"
        )
        yaml_text = self.meta_text().replace("  spriteSheet:\n", duplicate + "  spriteSheet:\n")

        settings = audit.parse_texture_importer(yaml_text)

        self.assertIn("platform-block-duplicate:DefaultTexturePlatform", settings["validationErrors"])
        self.assertEqual(4096, settings["androidMaxTextureSize"])

    def test_png_dimensions_and_astc_mip_estimates_are_deterministic(self) -> None:
        path = self.root / "texture.png"
        path.write_bytes(self.png_bytes(4096, 2048))

        self.assertEqual((4096, 2048, "PNG"), audit.image_dimensions(path))
        first = audit.astc_payload_bytes(4096, 4096, "RGBA_ASTC6X6_SRGB", True)
        second = audit.astc_payload_bytes(4096, 4096, "RGBA_ASTC6X6_SRGB", True)
        reduced = audit.astc_payload_bytes(2048, 2048, "RGBA_ASTC6X6_SRGB", True)
        self.assertEqual(first, second)
        self.assertIsNotNone(first)
        self.assertGreater(first, reduced)
        self.assertEqual("balanced", audit.astc_quality_tier("RGBA_ASTC6X6_SRGB"))
        self.assertEqual("very-high", audit.astc_quality_tier("ASTC_4x4"))

    def test_complete_build_and_hash_verified_visual_proof_authorize_one_candidate(self) -> None:
        self.write_json(self.build_path, self.build_document([self.texture_row(self.ASSET)]))
        self.write_visual_evidence()

        data = self.inventory()

        self.assertEqual("ALLOW_ONLY_LISTED_BOUNDED_REDUCTIONS", data["decision"])
        self.assertTrue(data["limitReductionAuthorized"])
        self.assertEqual(1, data["summary"]["oversizedCandidateCount"])
        self.assertEqual(1, data["summary"]["authorizedLimitReductionCandidateCount"])
        candidate = data["candidates"][0]
        self.assertTrue(candidate["acceptedCurrentBuildReportInclusion"])
        self.assertTrue(candidate["acceptedCurrentVisualProof"])
        self.assertTrue(candidate["limitReductionAuthorized"])
        self.assertEqual([], candidate["authorizationBlockers"])
        self.assertEqual("RGBA_ASTC6X6_SRGB", candidate["astcFormatForEstimate"])
        self.assertEqual("historical content residency", candidate["astcFormatEvidence"])

    def test_missing_build_report_blocks_reduction_even_with_visual_proof(self) -> None:
        self.write_visual_evidence()

        data = self.inventory()

        self.assertFalse(data["limitReductionAuthorized"])
        self.assertIn(
            "no-current-complete-Android-BuildReport",
            data["globalAcceptanceBlockers"],
        )
        self.assertIn(
            "no-current-complete-BuildReport-inclusion",
            data["candidates"][0]["authorizationBlockers"],
        )

    def test_incomplete_or_unsorted_build_export_is_rejected(self) -> None:
        second = "Assets/Textures/Second.png"
        cases = {
            "missing-marker": self.build_document([self.texture_row(self.ASSET)]),
            "unsorted": self.build_document(
                [self.texture_row(second), self.texture_row(self.ASSET)]
            ),
            "duplicate": self.build_document(
                [self.texture_row(self.ASSET), self.texture_row(self.ASSET)]
            ),
            "invalid-package": self.build_document([self.texture_row(self.ASSET)]),
            "empty": self.build_document([]),
        }
        cases["missing-marker"].pop("allIncludedTexturePathsExported")
        cases["invalid-package"]["packageType"] = "WEB"

        for name, document in cases.items():
            with self.subTest(name=name):
                self.write_json(self.build_path, document)
                self.write_visual_evidence()

                data = self.inventory()

                self.assertFalse(data["limitReductionAuthorized"])
                self.assertFalse(
                    data["evidence"]["buildReports"][0]["acceptedForCurrentRevision"]
                )

    def test_visual_proof_rejects_hash_mismatch_and_quality_failure(self) -> None:
        self.write_json(self.build_path, self.build_document([self.texture_row(self.ASSET)]))
        document = self.visual_document()
        document["candidateResults"][0]["capturePairs"][0]["before"]["sha256"] = "0" * 64
        document["candidateResults"][0]["rejectionChecks"]["detailLoss"] = True
        self.write_json(self.visual_path, document)

        data = self.inventory()

        self.assertFalse(data["limitReductionAuthorized"])
        visual = data["evidence"]["visualProof"]
        self.assertFalse(visual["acceptedForCurrentRevision"])
        self.assertTrue(
            any("sha256-mismatch" in error for error in visual["validationErrors"])
        )
        self.assertTrue(
            any("rejection-checks-not-clear" in error for error in visual["validationErrors"])
        )

    def test_visual_proof_rejects_placeholder_png_even_when_hash_matches(self) -> None:
        self.write_json(self.build_path, self.build_document([self.texture_row(self.ASSET)]))
        document = self.visual_document()
        artifact = document["candidateResults"][0]["capturePairs"][0]["before"]
        path = self.root / artifact["path"]
        path.write_bytes(b"\x89PNG\r\n\x1a\nplaceholder")
        artifact["sha256"] = self.file_sha(path)
        self.write_json(self.visual_path, document)

        data = self.inventory()

        self.assertFalse(data["limitReductionAuthorized"])
        self.assertTrue(
            any(
                "png-missing-or-invalid" in error
                for error in data["evidence"]["visualProof"]["validationErrors"]
            )
        )

    def test_visual_astc_format_must_match_explicit_importer(self) -> None:
        (self.root / self.meta_path).write_text(
            self.meta_text(
                android_limit=4096,
                android_override=1,
                android_format=50,
            ),
            encoding="utf-8",
        )
        self.write_json(self.build_path, self.build_document([self.texture_row(self.ASSET)]))
        document = self.visual_document()
        document["candidateResults"][0]["beforeAstcFormat"] = "RGBA_ASTC4X4_SRGB"
        document["candidateResults"][0]["afterAstcFormat"] = "RGBA_ASTC4X4_SRGB"
        self.write_json(self.visual_path, document)

        data = self.inventory()

        self.assertTrue(data["evidence"]["visualProof"]["acceptedForCurrentRevision"])
        self.assertFalse(data["limitReductionAuthorized"])
        self.assertIn(
            "visual-ASTC-format-does-not-match-importer",
            data["candidates"][0]["authorizationBlockers"],
        )

    def test_missing_visual_proof_blocks_reduction_even_with_build_inclusion(self) -> None:
        self.write_json(self.build_path, self.build_document([self.texture_row(self.ASSET)]))

        data = self.inventory()

        self.assertFalse(data["limitReductionAuthorized"])
        self.assertIn(
            "no-current-hash-verified-Android-visual-proof",
            data["globalAcceptanceBlockers"],
        )
        self.assertIn(
            "no-current-hash-verified-visual-proof",
            data["candidates"][0]["authorizationBlockers"],
        )

    def test_empty_visual_result_set_is_not_accepted(self) -> None:
        self.write_json(self.build_path, self.build_document([self.texture_row(self.ASSET)]))
        document = self.visual_document()
        document["candidateResults"] = []
        self.write_json(self.visual_path, document)

        data = self.inventory()

        self.assertFalse(data["evidence"]["visualProof"]["acceptedForCurrentRevision"])
        self.assertIn(
            "candidate-results-empty",
            data["evidence"]["visualProof"]["validationErrors"],
        )

    def test_candidate_inventory_is_sorted_and_distinguishes_4k_8k_and_blind_spots(self) -> None:
        eight_k = "Assets/Textures/A-EightK.png"
        unknown = "Assets/Textures/Unknown.exr"
        self.write_texture(eight_k, 8192, 4096, self.meta_text(default_limit=8192))
        unknown_meta = self.root / f"{unknown}.meta"
        unknown_meta.parent.mkdir(parents=True, exist_ok=True)
        unknown_meta.write_text(self.meta_text(default_limit=8192), encoding="utf-8")
        (self.root / unknown).write_bytes(b"not-an-exr")

        data = self.inventory(meta_paths=[self.meta_path, f"{eight_k}.meta", f"{unknown}.meta"])

        self.assertEqual(
            [eight_k, self.ASSET],
            [row["assetPath"] for row in data["candidates"]],
        )
        self.assertEqual(
            {"4K": 1, "8K": 1},
            data["summary"]["oversizedCandidateCountsByLimit"],
        )
        self.assertEqual(1, data["summary"]["unknownSourceDimensionsWithOversizedLimitCount"])
        self.assertIn(
            "oversized-limit-importers-with-unreadable-source-dimensions",
            data["globalAcceptanceBlockers"],
        )

    def test_dirty_worktree_rejects_otherwise_valid_evidence(self) -> None:
        self.write_json(self.build_path, self.build_document([self.texture_row(self.ASSET)]))
        self.write_visual_evidence()

        data = self.inventory(tracked_worktree_changes=[" M Assets/Other.asset"])

        self.assertFalse(data["limitReductionAuthorized"])
        self.assertIn("tracked-worktree-dirty", data["globalAcceptanceBlockers"])
        self.assertFalse(
            data["evidence"]["buildReports"][0]["acceptedForCurrentRevision"]
        )
        self.assertFalse(data["evidence"]["visualProof"]["acceptedForCurrentRevision"])

    def test_generated_reports_are_byte_deterministic_and_staleness_is_detected(self) -> None:
        data = self.inventory()
        report_root = self.root / "generated"

        audit.write_reports(report_root, data)
        first_json = (report_root / audit.JSON_REPORT_PATH).read_bytes()
        first_markdown = (report_root / audit.MARKDOWN_REPORT_PATH).read_bytes()
        audit.write_reports(report_root, data)

        self.assertEqual(first_json, (report_root / audit.JSON_REPORT_PATH).read_bytes())
        self.assertEqual(first_markdown, (report_root / audit.MARKDOWN_REPORT_PATH).read_bytes())
        self.assertEqual([], audit.report_check_errors(report_root, data))
        markdown = report_root / audit.MARKDOWN_REPORT_PATH
        markdown.write_text(markdown.read_text(encoding="utf-8") + "stale\n", encoding="utf-8")
        self.assertIn(
            f"generated-report-stale:{audit.MARKDOWN_REPORT_PATH.as_posix()}",
            audit.report_check_errors(report_root, data),
        )

    def inventory(
        self,
        *,
        meta_paths: list[str] | None = None,
        tracked_worktree_changes: list[str] | None = None,
    ) -> dict[str, object]:
        return audit.inventory(
            self.root,
            head=self.HEAD,
            tracked_worktree_changes=(
                [] if tracked_worktree_changes is None else tracked_worktree_changes
            ),
            all_tracked_paths=[],
            meta_paths=meta_paths or [self.meta_path],
            content_residency_path=self.content_path,
            build_report_paths=[self.build_path],
            visual_evidence_path=self.visual_path,
            require_tracked_evidence=False,
        )

    def write_texture(self, asset_path: str, width: int, height: int, meta: str) -> None:
        absolute = self.root / asset_path
        absolute.parent.mkdir(parents=True, exist_ok=True)
        absolute.write_bytes(self.png_bytes(width, height))
        (self.root / f"{asset_path}.meta").write_text(meta, encoding="utf-8")

    def write_visual_evidence(self) -> None:
        self.write_json(self.visual_path, self.visual_document())

    def visual_document(self) -> dict[str, object]:
        pairs = []
        for view in audit.VISUAL_VIEWS:
            before_path = f"Design/AgentReports/Captures/{view}_before.png"
            after_path = f"Design/AgentReports/Captures/{view}_after.png"
            before = self.root / before_path
            after = self.root / after_path
            before.parent.mkdir(parents=True, exist_ok=True)
            before.write_bytes(self.png_bytes(1920, 1080, payload=view.encode("ascii") + b"before"))
            after.write_bytes(self.png_bytes(1920, 1080, payload=view.encode("ascii") + b"after"))
            pairs.append(
                {
                    "view": view,
                    "cameraStateSha256": hashlib.sha256(view.encode("ascii")).hexdigest(),
                    "before": {"path": before_path, "sha256": self.file_sha(before)},
                    "after": {"path": after_path, "sha256": self.file_sha(after)},
                }
            )
        return {
            "schema": audit.VISUAL_SCHEMA,
            "taskId": audit.TASK_ID,
            "status": "complete",
            "exactCommit": self.HEAD,
            "dirty": False,
            "buildTarget": "Android",
            "deviceModel": "Pinned Device",
            "graphicsApi": "Vulkan",
            "candidateResults": [
                {
                    "assetPath": self.ASSET,
                    "result": "pass",
                    "beforeMaxTextureSize": 4096,
                    "afterMaxTextureSize": 2048,
                    "beforeAstcFormat": "RGBA_ASTC6X6_SRGB",
                    "afterAstcFormat": "RGBA_ASTC6X6_SRGB",
                    "rejectionChecks": {
                        name: False for name in audit.VISUAL_REJECTION_CHECKS
                    },
                    "capturePairs": pairs,
                }
            ],
        }

    def build_document(self, rows: list[dict[str, object]]) -> dict[str, object]:
        return {
            "schemaVersion": 1,
            "taskId": "APH-500",
            "status": "complete",
            "exactCommit": self.HEAD,
            "dirty": False,
            "releaseBuildType": "release",
            "packageType": "AAB",
            "buildTarget": "Android",
            "detailedBuildReport": True,
            "allIncludedTexturePathsExported": True,
            "buildReportIncludedTextures": rows,
            "buildReportIncludedAssets": rows,
        }

    @staticmethod
    def texture_row(asset_path: str) -> dict[str, object]:
        return {
            "sourceAssetPath": asset_path,
            "packedBytes": 1024,
            "objectTypes": ["UnityEngine.Texture2D"],
        }

    @staticmethod
    def content_row(asset_path: str, width: int, height: int) -> dict[str, object]:
        return {
            "assetPath": asset_path,
            "assetType": "Texture2D",
            "textureWidth": width,
            "textureHeight": height,
            "textureFormat": "RGBA_ASTC6X6_SRGB",
            "importedSizeBytes": 4096,
        }

    @staticmethod
    def meta_text(
        *,
        default_limit: int = 4096,
        android_limit: int | None = None,
        android_override: int = 0,
        android_format: int = -1,
    ) -> str:
        android = ""
        if android_limit is not None:
            android = (
                "  - serializedVersion: 4\n"
                "    buildTarget: Android\n"
                f"    maxTextureSize: {android_limit}\n"
                "    resizeAlgorithm: 0\n"
                f"    textureFormat: {android_format}\n"
                "    textureCompression: 2\n"
                "    compressionQuality: 50\n"
                "    crunchedCompression: 0\n"
                f"    overridden: {android_override}\n"
            )
        return (
            "fileFormatVersion: 2\n"
            "TextureImporter:\n"
            "  mipmaps:\n"
            "    enableMipMap: 1\n"
            "  textureType: 0\n"
            "  platformSettings:\n"
            "  - serializedVersion: 4\n"
            "    buildTarget: DefaultTexturePlatform\n"
            f"    maxTextureSize: {default_limit}\n"
            "    resizeAlgorithm: 0\n"
            "    textureFormat: -1\n"
            "    textureCompression: 2\n"
            "    compressionQuality: 50\n"
            "    crunchedCompression: 0\n"
            "    overridden: 0\n"
            f"{android}"
            "  spriteSheet:\n"
            "    serializedVersion: 2\n"
        )

    @staticmethod
    def png_bytes(width: int, height: int, payload: bytes = b"") -> bytes:
        def chunk(chunk_type: bytes, data: bytes) -> bytes:
            crc = binascii.crc32(chunk_type)
            crc = binascii.crc32(data, crc) & 0xFFFFFFFF
            return struct.pack(">I", len(data)) + chunk_type + data + struct.pack(">I", crc)

        ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
        return (
            b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", ihdr)
            + chunk(b"IDAT", zlib.compress(payload or b"\x00"))
            + chunk(b"IEND", b"")
        )

    @staticmethod
    def file_sha(path: Path) -> str:
        return hashlib.sha256(path.read_bytes()).hexdigest()

    @staticmethod
    def write_json(path: Path, payload: dict[str, object]) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
