import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT = (Path(__file__).resolve().parents[1] / "Recipes" /
          "operation_map_publication" / "regenerate_full_read_models.py")
SPEC = importlib.util.spec_from_file_location("full_read_models", SCRIPT)
GENERATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GENERATOR)
REPO = Path(__file__).resolve().parents[4]


class OperationMapPublicationLexerCacheTests(unittest.TestCase):
    def test_cached_and_uncached_generation_match_for_current_sources(self):
        with tempfile.TemporaryDirectory() as temp:
            uncached = Path(temp) / "uncached"
            cached = Path(temp) / "cached"
            real_lru_cache = GENERATOR.functools.lru_cache

            with patch.object(GENERATOR.functools, "lru_cache", side_effect=lambda **_: lambda fn: fn):
                GENERATOR.generate(REPO, uncached)
            with patch.object(GENERATOR.functools, "lru_cache", real_lru_cache):
                GENERATOR.generate(REPO, cached)

            uncached_files = {p.name: p.read_bytes() for p in uncached.iterdir()}
            cached_files = {p.name: p.read_bytes() for p in cached.iterdir()}
            self.assertEqual(uncached_files.keys(), cached_files.keys())
            for name in uncached_files:
                with self.subTest(output=name):
                    self.assertEqual(uncached_files[name], cached_files[name])

    def test_source_change_on_later_read_is_rejected(self):
        source = REPO / GENERATOR.INPUTS["owner"]
        original_read_bytes = Path.read_bytes
        reads = 0

        def read_with_late_mutation(path):
            nonlocal reads
            data = original_read_bytes(path)
            if path == source:
                reads += 1
                if reads == 2:
                    return data + b"\n// simulated concurrent source change\n"
            return data

        with tempfile.TemporaryDirectory() as temp:
            with patch.object(Path, "read_bytes", read_with_late_mutation):
                with self.assertRaisesRegex(ValueError, "Native/helper drift before emission"):
                    GENERATOR.generate(REPO, Path(temp) / "out")
        self.assertEqual(2, reads)


if __name__ == "__main__":
    unittest.main(verbosity=2)
