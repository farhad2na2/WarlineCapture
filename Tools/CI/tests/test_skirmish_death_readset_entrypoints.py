import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.DeathReadset.cs"


class DeathReadsetEntrypointTests(unittest.TestCase):
    def test_legacy_entrypoint_keeps_fixed_selected16_contract(self):
        text = SOURCE.read_text()
        self.assertIn("RunDeathMechanicalReadset()=>RunDeathMechanicalReadsetCore(false)", text)
        self.assertIn('keySha!="2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7"||keys.Length!=16', text)
        self.assertIn('selectedSet?"ExplicitHashPinnedGenericSet":"LegacyFixedSelected16"', text)

    def test_generic_entrypoint_uses_same_owner_and_stays_diagnostic(self):
        text = SOURCE.read_text()
        self.assertIn("RunSelectedDeathMechanicalReadset()=>RunDeathMechanicalReadsetCore(true)", text)
        self.assertIn("if(selectedSet){if(keys.Length<1||keys.Length>64)", text)
        self.assertIn('["full_mission_initialized"]=false', text)
        self.assertIn('["renderer_qualified"]=false', text)
        self.assertIn('["runtime_admitted"]=false', text)
        self.assertIn('" originalPrepare="+rows.Count+" childPermutation="+rows.Count', text)


if __name__ == "__main__":
    unittest.main()
