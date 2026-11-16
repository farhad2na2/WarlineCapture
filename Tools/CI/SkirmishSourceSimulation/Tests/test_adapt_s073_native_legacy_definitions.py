"""Controls for native scope-to-consumer flag projection."""
import importlib.util
from pathlib import Path
import unittest

SCRIPT = Path(__file__).with_name("adapt_rebind_s073_native_legacy_definitions.py")
spec = importlib.util.spec_from_file_location("adapt_s073_native_legacy_definitions", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class S073LegacyDefinitionAdapterScopeTests(unittest.TestCase):
    def setUp(self):
        self.native = {
            "mission_world_loaded": False,
            "mission_initialized": False,
            "native_readiness": False,
        }
        self.names = {
            "mission_world_allocated": False,
            "mission_initialized": False,
            "native_visual_readiness": False,
        }

    def test_false_native_flags_remain_false_in_consumer_projection(self):
        self.assertEqual(module.consumer_scope_flags(self.native, self.names), {
            "mission_world_allocated": False,
            "full_mission_initialized": False,
            "native_visual_readiness": False,
        })

    def test_true_native_flags_are_rejected(self):
        for source, key in ((self.native, "mission_world_loaded"),
                            (self.native, "mission_initialized"),
                            (self.native, "native_readiness"),
                            (self.names, "mission_world_allocated"),
                            (self.names, "mission_initialized"),
                            (self.names, "native_visual_readiness")):
            with self.subTest(key=key):
                source[key] = True
                with self.assertRaisesRegex(ValueError, "source flags must all be false"):
                    module.consumer_scope_flags(self.native, self.names)
                source[key] = False


if __name__ == "__main__":
    unittest.main()
