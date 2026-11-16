import tempfile
import unittest
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import regenerate_opening


ROOT = Path(__file__).resolve().parents[5]


class S050OpeningTraceGenerationTests(unittest.TestCase):
    def generate_actor(self, root):
        out = root / "generated"
        regenerate_opening.generate(ROOT, out)
        return (out / "GeneratedTypedOpeningActorSource.cs").read_text()

    def test_trace_is_s050_scoped_and_reports_actual_owner_branches(self):
        with tempfile.TemporaryDirectory() as tmp:
            actor = self.generate_actor(Path(tmp))
        self.assertIn('setup.CatalogId=="S050"', actor)
        for branch in (
            "missing_prefab",
            "typed_pad_resolution",
            "grid_cardinality",
            "incomplete_allocator",
            "typed_occupancy_mask",
            "spawn_cell_search_failed",
            "ensure_traversable_failed",
            '"placed"',
        ):
            self.assertIn(branch, actor)
        self.assertIn("pad_center_world=", actor)
        self.assertIn("requested_cell=", actor)
        self.assertIn("center_cell=", actor)
        self.assertIn("footprint=", actor)
        self.assertIn("seed=", actor)

    def test_failure_census_is_read_only_and_keeps_existing_allocator_call(self):
        with tempfile.TemporaryDirectory() as tmp:
            actor = self.generate_actor(Path(tmp))
        self.assertIn("SpawnCellUtility.TryFind(ref rng, grid, walkable,", actor)
        self.assertIn("if (!SpawnCellUtility.TryFind(", actor)
        self.assertIn("branch=spawn_cell_search_failed radius=12", actor)
        self.assertIn("census_scope=all_grid_candidate_origins", actor)
        self.assertIn("census_available=", actor)
        # The census receives snapshots of the actual owner buffers after the
        # original search failed; it performs no writes and is not part of the
        # allocator decision path.
        census = actor[actor.index("private static string CandidateCensus"):actor.index("internal static Entity CreateForceMember")]
        self.assertNotIn(".Set(", census)
        self.assertNotIn("TryFind(", census)


if __name__ == "__main__":
    unittest.main()
