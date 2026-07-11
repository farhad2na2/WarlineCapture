from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from Tools.CI.aph508_animation_texture_audit import (
    SetEvidence,
    TextureEvidence,
    parse_texture,
    render_report,
)


class Aph508AnimationTextureAuditTests(unittest.TestCase):
    def test_parse_texture_validates_serialized_payload(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "Texture.asset"
            path.write_text(
                "m_Width: 2\n"
                "m_Height: 1\n"
                "m_CompleteImageSize: 2\n"
                "m_TextureFormat: 17\n"
                "m_MipCount: 1\n"
                "m_IsReadable: 1\n"
                "m_StreamingMipmaps: 0\n"
                "m_ColorSpace: 0\n"
                "m_FilterMode: 0\n"
                "image data: 2\n_typelessdata: 0011\n",
                encoding="utf-8",
            )
            parsed = parse_texture(root, path)
            self.assertEqual(2, parsed["payload_bytes"])
            self.assertEqual(17, parsed["texture_format"])
            self.assertEqual(64, len(parsed["payload_sha256"]))

    def test_parse_texture_rejects_truncated_payload(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "Texture.asset"
            path.write_text(
                "m_Width: 2\nm_Height: 1\nm_CompleteImageSize: 2\n"
                "m_TextureFormat: 17\nm_MipCount: 1\nm_IsReadable: 1\n"
                "m_StreamingMipmaps: 0\nm_ColorSpace: 0\nm_FilterMode: 0\n"
                "image data: 2\n_typelessdata: 00\n",
                encoding="utf-8",
            )
            with self.assertRaisesRegex(ValueError, "payload length mismatch"):
                parse_texture(root, path)

    def test_report_is_deterministic_and_separates_build_inclusion(self) -> None:
        def texture(path: str, digest: str, included: bool) -> TextureEvidence:
            return TextureEvidence(
                path=path,
                set_name=path.split("/")[-3],
                index=0,
                guid="0" * 32,
                file_bytes=10,
                file_sha256="f" * 64,
                payload_sha256=digest,
                width=2,
                height=1,
                payload_bytes=2,
                texture_format=17,
                mip_count=1,
                readable=1,
                streaming=0,
                color_space=0,
                filter_mode=0,
                reference_files=("material.mat",),
                packed_apk_bytes=2 if included else None,
                packed_aab_bytes=2 if included else None,
                imported_bytes=4 if included else None,
            )

        included = texture("Assets/Generated/Current/ModelResources/AnimationTexture0.asset", "a" * 64, True)
        excluded = texture("Assets/Generated/Legacy/ModelResources/AnimationTexture0.asset", "b" * 64, False)
        coverage = SetEvidence("Current", 1, 2, 1, 50, 50, 100, 200)
        first = render_report([included, excluded], [coverage], [])
        second = render_report([included, excluded], [coverage], [])
        self.assertEqual(first, second)
        self.assertIn("Project-only set", first)
        self.assertIn("50.00%", first)
        self.assertIn("named device-runtime residency", first)

    def test_report_exposes_exact_payload_duplicates(self) -> None:
        base = dict(
            set_name="Set", index=0, guid="0" * 32, file_bytes=10,
            file_sha256="f" * 64, payload_sha256="a" * 64, width=2,
            height=1, payload_bytes=2, texture_format=17, mip_count=1,
            readable=1, streaming=0, color_space=0, filter_mode=0,
            reference_files=(), packed_apk_bytes=None, packed_aab_bytes=None,
            imported_bytes=None,
        )
        first = TextureEvidence(path="first.asset", **base)
        second = TextureEvidence(path="second.asset", **base)
        coverage = SetEvidence("Set", 1, 0, 1, 1, 1, 1, 2)
        report = render_report([first, second], [coverage], [])
        self.assertIn("Exact payload duplicate", report)


if __name__ == "__main__":
    unittest.main()
