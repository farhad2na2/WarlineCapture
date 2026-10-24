import copy
import unittest
from skirmish_aria_trace import analyze


def trace(count=8, progress=False, receipts=True, execute=True, alternating=False):
    frames = []
    actions = 0
    for i in range(count):
        target = 91 + i % 2 if alternating else 91
        if execute and i:
            actions += 1
        session = dict(Actions=actions, LastCompletedTargetId=target)
        frames.append(dict(time=i*4, frame=i, intent="ServiceOriginalTruck", target_id=target,
                           observation=dict(Time=i*4, Frame=i, EnemyHealth=800-i if progress else 800,
                                            Infantry=0, OwnStructureAttackUnits=0),
                           before=dict(session=copy.deepcopy(session)), after=dict(session=copy.deepcopy(session)),
                           feedback=dict(receipt=dict(id=i, status="accepted", target_id=target))
                           if receipts and execute else {}))
    return dict(schema_version=1, frames=frames)


class Controls(unittest.TestCase):
    def test_successful_clicks_do_not_hide_support_loop(self):
        result = analyze(trace())
        self.assertEqual(result["status"], "failed")
        self.assertIn("repeated_action_without_progress", [x["kind"] for x in result["issues"]])
        self.assertEqual(result["accepted_receipts"], 8)

    def test_real_progress_allows_repeated_actions(self):
        self.assertEqual(analyze(trace(progress=True))["status"], "passed")

    def test_duplicate_receipts_are_not_new_actions(self):
        value = trace(execute=False)
        for row in value["frames"]:
            row["feedback"] = dict(receipt=dict(id="same", status="accepted", target_id=91))
        result = analyze(value)
        self.assertEqual(result["executed_actions"], 1)
        self.assertEqual(result["accepted_receipts"], 1)

    def test_receipt_status_transition_is_counted_once(self):
        value = trace(count=2, execute=False)
        value["frames"][0]["feedback"] = dict(receipt=dict(id=1, status="accepted"))
        value["frames"][1]["feedback"] = dict(receipt=dict(id=1, status="completed"))
        self.assertEqual(analyze(value)["executed_actions"], 1)

    def test_rejected_controls_fail_without_successful_actions(self):
        value = trace(execute=False)
        for i, row in enumerate(value["frames"]):
            row["feedback"] = dict(receipt=dict(id=i, status="rejected", target_id=91))
        result = analyze(value)
        self.assertEqual(result["status"], "failed")
        self.assertIn("repeated_rejected", [x["kind"] for x in result["issues"]])

    def test_alternating_control_loop_is_detected(self):
        result = analyze(trace(count=16, alternating=True))
        self.assertIn("action_cycle_without_progress", [x["kind"] for x in result["issues"]])

    def test_intent_changes_do_not_hide_the_same_control_loop(self):
        value = trace()
        for i, row in enumerate(value["frames"]):
            row["intent"] = "SupportStage" + str(i)
        self.assertEqual(analyze(value)["status"], "failed")

    def test_world_progress_does_not_hide_rejected_actions(self):
        value = trace(progress=True, execute=False)
        for i, row in enumerate(value["frames"]):
            row["feedback"] = dict(receipt=dict(id=i, status="rejected", target_id=91))
        self.assertEqual(analyze(value)["status"], "failed")

    def test_successful_execution_resets_consecutive_rejections(self):
        value = trace(count=7, progress=True, execute=False)
        for i, row in enumerate(value["frames"]):
            row["feedback"] = dict(receipt=dict(id=i, status="accepted" if i==3 else "rejected", target_id=91))
        self.assertEqual(analyze(value)["status"], "passed")

    def test_missing_execution_feedback_is_inconclusive(self):
        result = analyze(trace(count=16, execute=False))
        self.assertEqual(result["status"], "inconclusive")
        self.assertEqual(result["executed_actions"], 0)

    def test_session_counter_can_show_clicks_without_receipts(self):
        self.assertEqual(analyze(trace(receipts=False))["status"], "failed")

    def test_ui_and_clock_changes_are_not_progress(self):
        value = trace()
        for i, row in enumerate(value["frames"]):
            row["observation"].update(DrawerOpen=bool(i%2), SelectedSlot=i)
        self.assertEqual(analyze(value)["status"], "failed")

    def test_completion_target_has_priority_over_next_proposal(self):
        value = trace(receipts=False)
        for i, row in enumerate(value["frames"]):
            row["target_id"] = i + 500
        result = analyze(value)
        self.assertTrue(any(x["target_id"] == 91 for x in result["issues"]))

    def test_missing_progress_and_backwards_time_rejected(self):
        value = trace()
        value["frames"][0]["observation"] = dict(Time=0, Frame=0)
        with self.assertRaisesRegex(ValueError, "No recognized"):
            analyze(value)
        value = trace()
        value["frames"][-1]["time"] = 1
        with self.assertRaisesRegex(ValueError, "backwards"):
            analyze(value)

    def test_unknown_execution_target_is_not_inferred_from_proposal(self):
        value = trace()
        for i, row in enumerate(value["frames"]):
            row["before"]["session"]["LastCompletedTargetId"] = 0
            row["feedback"]["receipt"].pop("target_id")
            row["feedback"]["receipt"]["status"] = "no_effect"
        result = analyze(value)
        self.assertEqual(result["issues"][0]["kind"], "repeated_no_effect_target_unknown")
        self.assertEqual(result["issues"][0]["target_id"], 0)

    def test_observed_watchdog_stop_is_reported(self):
        value = trace(count=2, execute=False)
        value["frames"][-1]["after"]["session"]["Phase"] = "Blocked"
        result = analyze(value)
        self.assertEqual(result["issues"][0]["kind"], "planner_blocked_before_completion")


if __name__ == "__main__":
    unittest.main(verbosity=2)
