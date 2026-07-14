from __future__ import annotations

import json
import struct
import unittest

from Tools.CI.aph504_texture_streaming_pilot_selector import (
    CANDIDATE_ASSET_PATHS,
    EXPECTED_MOBILE_BUDGET_MIB,
    PILOT_LIMIT,
    PNG_SIGNATURE,
    ROOT,
    ValidationError,
    classify_world_texture,
    collect,
    parse_build_evidence,
    parse_mobile_quality,
    parse_png_dimensions,
    parse_texture_meta,
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


def build_report(paths: tuple[str, ...] = CANDIDATE_ASSET_PATHS) -> bytes:
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


def selection_row(path: str, family: str, packed_bytes: int) -> dict[str, object]:
    return {
        "assetPath": path,
        "textureFamily": family,
        "historicalAabPackedBytes": packed_bytes,
        "aph502Category": "world albedo",
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
        second = collect(ROOT)

        self.assertTrue(first["selectorValid"])
        self.assertFalse(first["pilotReadyForMutation"])
        self.assertFalse(first["mutationAuthorized"])
        self.assertFalse(first["expansionAuthorized"])
        self.assertEqual(
            [
                "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png",
                "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png",
            ],
            first["proposedCandidatePaths"],
        )
        self.assertEqual(13, len(first["candidates"]))
        self.assertTrue(first["scopedTrackedInputsClean"])
        self.assertTrue(first["controlInputHashesUnchangedDuringRead"])
        self.assertIn(
            "full-source-near-mips-not-preserved:globalTextureMipmapLimit=1",
            first["unresolvedEvidence"],
        )
        self.assertEqual(render_json(first), render_json(second))
        self.assertEqual(render_markdown(first), render_markdown(second))
        self.assertIn("result=Passed selector_valid=true pilot_ready=false", render_check(first))


if __name__ == "__main__":
    unittest.main()
