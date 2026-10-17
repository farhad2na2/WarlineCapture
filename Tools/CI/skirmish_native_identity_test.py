import copy
import unittest

from skirmish_native_identity import canonical, content_identity, from_native_census, sha256, validate_setup_identity, verify_job


class NativeIdentityTests(unittest.TestCase):
    def setUp(self):
        self.identity = from_native_census(catalog_id="S005", definition_id="skirmish.s005", definition_version=2,
            difficulty="Regular", size="Standard", seed=155926, native_locale="fa-IR",
            native_setup_hash_uint32=0x79ED2299, native_content_identity='SYNTHETIC ONLY: \u0641\u0627\u0631\u0633\u06cc  " \\ \U0001f680')
        self.job = {key: self.identity[key] for key in ("catalog_id", "definition_id", "definition_version", "difficulty", "size", "seed", "locale")}
        self.job.update(setup_sha256=sha256(self.identity), content_sha256=sha256(content_identity(self.identity)))

    def test_actual_uint32_is_numeric_and_versioned_unicode_bytes_are_stable(self):
        verify_job(self.job, self.identity)
        self.assertIn(b'"native_setup_hash_uint32":2045584025', canonical(self.identity))
        self.assertIn(b"\\ud83d\\ude80", canonical(self.identity))
        self.assertNotEqual(self.job["setup_sha256"], "0" * 56 + "79ed2299")
        self.assertNotEqual(self.job["setup_sha256"], self.job["content_sha256"])

    def test_every_configuration_content_or_setup_mutation_rejected(self):
        for key, value in (("catalog_id", "S004"), ("definition_id", "skirmish.s004"), ("definition_version", 3),
            ("difficulty", "Veteran"), ("size", "War"), ("seed", 104734), ("locale", "EN"),
            ("native_setup_hash_uint32", 1), ("native_content_identity", "OTHER CONTENT")):
            with self.subTest(key=key), self.assertRaises(ValueError):
                verify_job(self.job, {**self.identity, key: value})
        for key in ("setup_sha256", "content_sha256"):
            with self.subTest(key=key), self.assertRaises(ValueError):
                verify_job({**self.job, key: "a" * 64}, self.identity)

    def test_unknown_fields_types_ranges_and_native_locales_fail_closed(self):
        for key, value in (("schema_version", True), ("schema_version", 2), ("identity_kind", "caller_declared"),
            ("native_setup_hash_uint32", True), ("native_setup_hash_uint32", "79ED2299"),
            ("native_setup_hash_uint32", -1), ("native_setup_hash_uint32", 4294967296), ("seed", True),
            ("locale", "fa"), ("native_content_identity", "")):
            with self.subTest(key=key), self.assertRaises(ValueError):
                validate_setup_identity({**self.identity, key: value})
        with self.assertRaises(ValueError): validate_setup_identity({**self.identity, "passed": True})
        missing = copy.deepcopy(self.identity); missing.pop("seed")
        with self.assertRaises(ValueError): validate_setup_identity(missing)


if __name__ == "__main__":
    unittest.main(verbosity=2)
