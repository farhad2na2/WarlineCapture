from __future__ import annotations

import json
import struct
import unittest

from Tools.CI.aph504_texture_streaming_pilot_selector import (
    AAB_REPORT_PATH,
    EXPECTED_MOBILE_BUDGET_MIB,
    PILOT_LIMIT,
    PNG_SIGNATURE,
    ROOT,
    ValidationError,
    classify_inventory_texture,
    classify_world_texture,
    collect,
    collect_repository_inventory,
    parse_build_evidence,
    parse_current_build_gate,
    parse_current_residency_gate,
    parse_mobile_quality,
    parse_performance_evidence_gate,
    parse_png_dimensions,
    parse_texture_meta,
    parse_visual_evidence_gate,
    render_check,
    render_json,
    render_markdown,
    select_pilot,
    validate_mobile_quality,
)


def texture_meta(
    *,
    serialized_version: int = 13,
    texture_type: int = 0,
    sprite_mode: int = 0,
    streaming: str | None = "0",
    ignore_limit: str | None = "0",
    readable: int = 1,
) -> str:
    optional = ""
    if streaming is not None:
        optional += f"  streamingMipmaps: {streaming}\n"
    if ignore_limit is not None:
        optional += f"  ignoreMipmapLimit: {ignore_limit}\n"
    return f"""fileFormatVersion: 2
TextureImporter:
  serializedVersion: {serialized_version}
  mipmaps:
    enableMipMap: 1
  isReadable: {readable}
{optional}  spriteMode: {sprite_mode}
  textureType: {texture_type}
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    overridden: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 4096
    overridden: 0
  userData:
"""


def mobile_quality(*, budget: int = EXPECTED_MOBILE_BUDGET_MIB, global_limit: int = 1) -> str:
    return f"""QualitySettings:
  m_QualitySettings:
  - serializedVersion: 5
    name: Mobile
    globalTextureMipmapLimit: {global_limit}
    streamingMipmapsActive: 1
    streamingMipmapsAddAllCameras: 1
    streamingMipmapsMemoryBudget: {budget}
    streamingMipmapsMaxLevelReduction: 2
    streamingMipmapsMaxFileIORequests: 1024
"""


def build_report(paths: tuple[str, ...]) -> bytes:
    rows = [
        {
            "sourceAssetPath": path,
            "packedBytes": 10_000_000 + index,
            "objectTypes": ["UnityEngine.Texture2D"],
        }
        for index, path in enumerate(paths)
    ]
    return json.dumps(
        {
            "status": "complete",
            "exactCommit": "a" * 40,
            "dirty": False,
            "reportedIncludedAssetCount": len(rows),
            "totalIncludedAssetCount": len(rows) + 5,
            "buildReportIncludedAssets": rows,
        }
    ).encode("utf-8")


def complete_build_report(head: str, paths: list[str]) -> bytes:
    rows = [
        {"sourceAssetPath": path, "objectTypes": ["UnityEngine.Texture2D"]}
        for path in sorted(paths)
    ]
    return json.dumps(
        {
            "schemaVersion": 1,
            "taskId": "APH-500",
            "status": "complete",
            "exactCommit": head,
            "dirty": False,
            "releaseBuildType": "release",
            "buildTarget": "Android",
            "detailedBuildReport": True,
            "allIncludedTexturePathsExported": True,
            "buildReportIncludedTextures": rows,
        }
    ).encode("utf-8")


def visual_evidence(head: str, paths: list[str]) -> bytes:
    return json.dumps(
        {
            "schemaVersion": 1,
            "taskId": "APH-505",
            "status": "complete",
            "exactCommit": head,
            "dirty": False,
            "candidatePaths": paths,
            "capturedViews": ["near", "medium", "far"],
            "beforeAfterPairsComplete": True,
            "visualRegressions": {
                "blur": False,
                "latePop": False,
                "terrainSeams": False,
                "missingVegetationDetail": False,
            },
            "accepted": True,
        }
    ).encode("utf-8")


def complete_residency(head: str, paths: list[str]) -> bytes:
    return json.dumps(
        {
            "status": "complete",
            "baselineCommit": head,
            "assets": [
                {"assetPath": path, "assetType": "Texture2D"}
                for path in paths
            ],
        }
    ).encode("utf-8")


def performance_evidence(head: str, paths: list[str]) -> bytes:
    return json.dumps(
        {
            "schemaVersion": 1,
            "taskId": "APH-506",
            "status": "complete",
            "exactCommit": head,
            "dirty": False,
            "candidatePaths": paths,
            "durationSeconds": 600,
            "memoryMeasured": True,
            "ioMeasured": True,
            "memoryRegressionAccepted": True,
            "ioRegressionAccepted": True,
            "accepted": True,
        }
    ).encode("utf-8")


def selection_row(path: str, family: str, packed_bytes: int) -> dict[str, object]:
    return {
        "assetPath": path,
        "textureFamily": family,
        "historicalAabPackedBytes": packed_bytes,
        "currentCategory": "world albedo",
        "proposedForPilot": False,
        "selectionReasons": [],
        "exclusionReasons": [],
    }


class Aph504TextureStreamingPilotSelectorTests(unittest.TestCase):
    def test_parses_explicit_modern_world_albedo_importer(self) -> None:
        meta = parse_texture_meta(texture_meta())

        self.assertEqual(13, meta.serialized_version)
        self.assertEqual(1, meta.enable_mip_map)
        self.assertEqual(0, meta.streaming_mipmaps)
        self.assertEqual(0, meta.ignore_mipmap_limit)
        self.assertEqual(4096, meta.default_max_texture_size)
        self.assertEqual(("world albedo", ()), classify_world_texture("Assets/World/Texture_01_A.png", meta))

    def test_absent_streaming_fields_are_not_inferred_from_old_yaml(self) -> None:
        meta = parse_texture_meta(
            texture_meta(serialized_version=4, streaming=None, ignore_limit=None, readable=0)
        )

        self.assertIsNone(meta.streaming_mipmaps)
        self.assertIsNone(meta.ignore_mipmap_limit)

    def test_duplicate_streaming_scalar_fails_closed(self) -> None:
        source = texture_meta().replace(
            "  streamingMipmaps: 0\n",
            "  streamingMipmaps: 0\n  streamingMipmaps: 0\n",
        )

        with self.assertRaises(ValidationError) as raised:
            parse_texture_meta(source)

        self.assertEqual("streamingMipmaps-count-invalid", raised.exception.code)

    def test_protected_or_sprite_path_is_not_world_eligible(self) -> None:
        meta = parse_texture_meta(texture_meta(sprite_mode=1))

        category, exclusions = classify_world_texture("Assets/Game/UI/Texture_01_A.png", meta)

        self.assertIsNone(category)
        self.assertEqual(("protected-path-class", "sprite-importer"), exclusions)

    def test_inventory_semantics_use_current_aph502_precedence_without_strict_yaml(self) -> None:
        old_ui_meta = texture_meta(serialized_version=4, streaming=None, ignore_limit=None).replace(
            "  spriteMode: 0", "  spriteMode: 1"
        )
        normal_meta = texture_meta(texture_type=1)

        self.assertEqual("UI", classify_inventory_texture("Assets/Game/UI/Panel_Normal.png", old_ui_meta))
        self.assertEqual(
            "world normal/mask",
            classify_inventory_texture("Assets/World/Ground_Normal.png", normal_meta),
        )

    def test_png_dimensions_are_read_from_ihdr(self) -> None:
        header = PNG_SIGNATURE + struct.pack(">I", 13) + b"IHDR" + struct.pack(">II", 4096, 2048)

        self.assertEqual((4096, 2048), parse_png_dimensions(header))
        with self.assertRaises(ValidationError):
            parse_png_dimensions(b"not-png")

    def test_clean_positive_build_rows_are_usable_but_incomplete(self) -> None:
        paths = ("Assets/Texture_01_A.png", "Assets/Texture_02_A.png")

        evidence = parse_build_evidence(build_report(paths), paths)

        self.assertEqual((), evidence.errors)
        self.assertFalse(evidence.export_complete)
        self.assertEqual(10_000_000, evidence.candidates[paths[0]].packed_bytes)
        self.assertEqual((), evidence.candidates[paths[0]].errors)

    def test_duplicate_json_key_and_duplicate_candidate_fail_closed(self) -> None:
        path = "Assets/Texture_01_A.png"
        duplicate_key = build_report((path,)).decode("utf-8").replace(
            '"status": "complete",',
            '"status": "complete", "status": "complete",',
            1,
        )
        malformed = parse_build_evidence(duplicate_key.encode("utf-8"), (path,))
        self.assertTrue(malformed.errors[0].startswith("aab-report-malformed:DuplicateJsonKeyError"))

        payload = json.loads(build_report((path,)).decode("utf-8"))
        payload["buildReportIncludedAssets"].append(payload["buildReportIncludedAssets"][0])
        payload["reportedIncludedAssetCount"] += 1
        duplicate = parse_build_evidence(json.dumps(payload).encode("utf-8"), (path,))
        self.assertEqual(("aab-included-row-count:2",), duplicate.candidates[path].errors)

    def test_mobile_quality_requires_bounded_256_mib_budget(self) -> None:
        quality = parse_mobile_quality(mobile_quality())

        self.assertEqual(EXPECTED_MOBILE_BUDGET_MIB, quality.memory_budget_mib)
        self.assertEqual([], validate_mobile_quality(quality))
        self.assertEqual(
            ["mobile-memory-budget-not-256:512"],
            validate_mobile_quality(parse_mobile_quality(mobile_quality(budget=512))),
        )

    def test_current_build_gate_requires_same_revision_complete_texture_export(self) -> None:
        head = "a" * 40
        paths = ["Assets/A.png", "Assets/B.png"]

        self.assertTrue(parse_current_build_gate(complete_build_report(head, paths), head, paths).accepted)
        rejected = parse_current_build_gate(build_report(tuple(paths)), head)

        self.assertFalse(rejected.accepted)
        self.assertIn("schema-version-not-1", rejected.errors)
        self.assertIn("complete-texture-export-marker-not-true", rejected.errors)

    def test_current_build_and_residency_gates_require_selected_texture_paths(self) -> None:
        head = "a" * 40
        available = ["Assets/A.png"]
        required = ["Assets/A.png", "Assets/B.png"]

        build = parse_current_build_gate(complete_build_report(head, available), head, required)
        residency = parse_current_residency_gate(complete_residency(head, available), head, required)

        self.assertIn(
            "selected-texture-absent-from-complete-build-export:Assets/B.png",
            build.errors,
        )
        self.assertIn("selected-texture-absent-from-residency:Assets/B.png", residency.errors)

    def test_aph505_and_aph506_gates_require_exact_revision_and_candidate_set(self) -> None:
        head = "a" * 40
        paths = ["Assets/A.png", "Assets/B.png"]

        self.assertTrue(parse_visual_evidence_gate(visual_evidence(head, paths), head, paths).accepted)
        self.assertTrue(parse_performance_evidence_gate(performance_evidence(head, paths), head, paths).accepted)
        wrong_paths = ["Assets/Other.png"]

        self.assertIn(
            "aph505-candidate-paths-mismatch",
            parse_visual_evidence_gate(visual_evidence(head, paths), head, wrong_paths).errors,
        )
        self.assertIn(
            "aph506-candidate-paths-mismatch",
            parse_performance_evidence_gate(performance_evidence(head, paths), head, wrong_paths).errors,
        )

    def test_selector_is_deterministic_and_uses_distinct_texture_families(self) -> None:
        rows = [
            selection_row("Assets/Texture_01_A.png", "01", 30),
            selection_row("Assets/Texture_01_B.png", "01", 20),
            selection_row("Assets/Texture_02_A.png", "02", 20),
            selection_row("Assets/Texture_03_A.png", "03", 10),
        ]

        selected = select_pilot(rows, [])

        self.assertEqual(PILOT_LIMIT, len(selected))
        self.assertEqual(["Assets/Texture_01_A.png", "Assets/Texture_02_A.png"], selected)
        self.assertEqual(["texture-family-quota-filled:01"], rows[1]["exclusionReasons"])
        self.assertEqual(["pilot-cap-reached:2"], rows[3]["exclusionReasons"])

    def test_selector_global_error_returns_no_candidates(self) -> None:
        rows = [selection_row("Assets/Texture_01_A.png", "01", 30)]

        selected = select_pilot(rows, ["aab-report-malformed"])

        self.assertEqual([], selected)
        self.assertEqual(["selector-global-gate-failed"], rows[0]["exclusionReasons"])

    def test_live_repository_plan_is_deterministic_and_rollout_blocked(self) -> None:
        first = collect(ROOT)
        if not first["selectorValid"]:
            first = collect(ROOT)
        inventory = collect_repository_inventory(ROOT)
        historical = json.loads((ROOT / AAB_REPORT_PATH).read_text(encoding="utf-8"))
        historical_texture_paths = {
            row["sourceAssetPath"]
            for row in historical["buildReportIncludedAssets"]
            if "UnityEngine.Texture2D" in row.get("objectTypes", [])
        }

        self.assertTrue(first["selectorValid"])
        self.assertFalse(first["pilotReadyForMutation"])
        self.assertFalse(first["mutationAuthorized"])
        self.assertFalse(first["expansionAuthorized"])
        self.assertEqual(PILOT_LIMIT, len(first["proposedCandidatePaths"]))
        self.assertTrue(set(first["proposedCandidatePaths"]).issubset(historical_texture_paths))
        self.assertGreater(len(first["candidates"]), PILOT_LIMIT)
        self.assertEqual(
            len(inventory.importer_meta_paths),
            first["currentRepositoryEvidence"]["trackedTextureImporterCount"],
        )
        self.assertEqual(
            inventory.manifest_package_count,
            first["currentRepositoryEvidence"]["manifestPackageCount"],
        )
        self.assertEqual(
            inventory.locked_package_count,
            first["currentRepositoryEvidence"]["lockedPackageCount"],
        )
        self.assertEqual(
            first["mutationAuthorized"],
            all(first["mutationPreconditions"].values()),
        )
        self.assertIn("aph505-evidence-unavailable", first["unresolvedEvidence"])
        self.assertIn("aph506-evidence-unavailable", first["unresolvedEvidence"])
        self.assertEqual(
            first["scopedTrackedInputsClean"],
            first["mutationPreconditions"]["scopedTrackedInputsClean"],
        )
        self.assertTrue(first["controlInputHashesUnchangedDuringRead"])
        self.assertFalse(first["mutationPreconditions"]["fullResolutionNearbyTexturesPreserved"])
        self.assertEqual(first, json.loads(render_json(first)))
        self.assertIn(
            f"Tracked TextureImporter count: `{len(inventory.importer_meta_paths)}`",
            render_markdown(first),
        )
        self.assertIn("result=Passed selector_valid=true pilot_ready=false", render_check(first))


if __name__ == "__main__":
    unittest.main()
