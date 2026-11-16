import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class S002PathScheduleTraceContractTests(unittest.TestCase):
    def test_trace_is_opt_in_bounded_and_projection_only(self):
        owner = (ROOT / "Adapters/117_OriginalGroundOwners.cs").read_text()
        self.assertIn('WARLINE_SOURCE_PATH_SCHEDULE_TRACE', owner)
        self.assertIn('PathTraceFirstTick=240,PathTraceLastTick=320,PathTraceCapacity=96', owner)
        self.assertIn('Stopwatch.GetTimestamp()', owner)
        self.assertIn('ReadPathProjection()', owner)
        self.assertIn('inferred_admission_from_pending_projection', owner)
        self.assertIn('pending_cleared_unclassified', owner)
        self.assertIn('pending_projection_only', owner)
        self.assertNotIn('inferred_apply_then_schedule', owner)
        self.assertIn('pathTraceOverflow=true', owner)
        self.assertNotIn('_pendingPathHandle.IsCompleted', owner)

    def test_result_is_independent_of_aggregate_counter_flag(self):
        owner = (ROOT / "Adapters/312_PaidSourceOwnerStep.retry.cs").read_text()
        self.assertIn('["path_schedule_trace"]=ground.SourcePathScheduleTrace', owner)
        self.assertIn('ground.PathPhase(Tick)', owner)


if __name__ == "__main__":
    unittest.main()
