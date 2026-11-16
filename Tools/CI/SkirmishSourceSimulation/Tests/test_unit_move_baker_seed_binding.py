"""Guard the original Baker's footprint-dependent idle seed after projection."""
import pathlib
import re
import unittest


class UnitMoveBakerSeedBindingTests(unittest.TestCase):
    def test_seed_uses_the_resolved_shared_footprint(self):
        root = pathlib.Path(__file__).resolve().parents[4]
        source = (root / "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs").read_text()
        bake = source.split("public override void Bake(UnitGridAuthoring authoring)", 1)[1]
        self.assertEqual(len(re.findall(r"\bint2\s+footprint\s*=\s*movement\.Footprint\.Size\s*;", bake)), 1)
        self.assertEqual(len(re.findall(r"new\s+int4\s*\(\s*footprint\.x\s*,\s*footprint\.y\s*,", bake)), 1)


if __name__ == "__main__":
    unittest.main()
