import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]


class S073SourceDerivedInitialPairGateTests(unittest.TestCase):
    def test_current_road_mask_capture_is_opt_in_and_uses_live_source_rows(self):
        source = (ROOT / "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs").read_text()
        start = source.index('string currentMaskPrefix = System.Environment.GetEnvironmentVariable("WARLINE_S073_CURRENT_ROAD_MASK_PREFIX")')
        end = source.index('if (roadOverlap)', start)
        hook = source[start:end]

        self.assertIn("!string.IsNullOrEmpty(currentMaskPrefix)", hook)
        self.assertIn("SkirmishPresetConfig.IndustrialBasinScenarioIndex", hook)
        self.assertIn("TryResolveS073InitialSourcePair", hook)
        self.assertIn("request.PreferredOrigin.x == maskBarrackSource.Preferred.x", hook)
        self.assertIn("request.PreferredOrigin.y == maskAmmoSource.Preferred.y", hook)
        self.assertNotIn("new Vector2Int(793, 545)", hook)
        self.assertNotIn("new Vector2Int(828, 462)", hook)

    def test_pair_survey_requires_unique_rows_but_not_historical_origins(self):
        source = (ROOT / "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.S073PreparingSurvey.cs").read_text()
        start = source.index("static bool TryResolveS073InitialSourcePair(")
        end = source.index("        S073LogicalReauthoringSurvey SurveyS073LogicalReauthoring(", start)
        resolver = source[start:end]

        self.assertIn("barracksMatches != 1 || ammoMatches != 1", resolver)
        self.assertIn("row.FactionId != 2", resolver)
        self.assertIn("row.OriginOffset", resolver)
        self.assertIn("factionSpawn", resolver)
        self.assertNotIn("new Vector2Int(828, 462)", resolver)
        self.assertNotIn("new Vector2Int(867, 462)", resolver)


if __name__ == "__main__":
    unittest.main()
