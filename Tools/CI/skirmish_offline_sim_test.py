"""Negative and boundary controls for the real-rule offline simulator."""
import argparse
import copy
import json
from pathlib import Path
import shutil
import sys
import unittest
sys.dont_write_bytecode = True
import skirmish_offline_sim as sim
import skirmish_offline_sim_cases as probes

BUILD = None
OUTPUT = None
PROJECT = Path(__file__).resolve().parents[2]


class OfflineSimTests(unittest.TestCase):
    def setUp(self):
        self.output = OUTPUT/self._testMethodName
        self.output.mkdir()
        self.index = 0

    def replay(self, cases, raw=None, build=None):
        self.index += 1
        path = self.output/f"input-{self.index}.json"
        path.write_text(raw if raw is not None else json.dumps(dict(schema_version=1, cases=cases)))
        return sim.run(build or BUILD, path, self.output/f"run-{self.index}")

    def fixture(self, family="CE", mode="win"):
        return probes.case("fixture", family, "Standard", 1200 if family == "FC" else 1080, mode)

    def test_actual_120_catalog_360_vectors_1800_synthetic_probes(self):
        suite, manifest = probes.generate(PROJECT)
        self.assertEqual((manifest["catalog_entries"], manifest["size_vectors"], manifest["cases"]), (120, 360, 1800))
        result = self.replay(suite["cases"])
        self.assertEqual((result["passed"], result["failed"], result["process_exit"]), (1800, 0, 0))
        self.assertFalse(result["full_battle_simulated"])
        self.assertFalse(result["mission_qualified"])

    def test_missing_unknown_boolean_and_negative_clock_facts_rejected(self):
        cases = []
        for i in range(4):
            case = self.fixture()
            case["id"] = f"bad-{i}"
            facts = case["steps"][0]["facts"]
            if i == 0: del facts["Playing"]
            if i == 1: facts["Undeclared"] = 1
            if i == 2: facts["Playing"] = 1
            if i == 3: facts["SimulationDeltaTicks"] = -1
            cases.append(case)
        result = self.replay(cases)
        self.assertEqual(result["failed"], 4)
        self.assertEqual(result["process_exit"], 1)

    def test_wrong_original_identity_and_missing_duplicate_facts_rejected(self):
        cases = []
        for i in range(3):
            case = self.fixture()
            case["id"] = f"identity-{i}"
            facts = case["steps"][0]["facts"]["OriginalTrucks"]
            if i == 0: facts[0]["StableOriginalId"] = "replacement"
            if i == 1: facts.pop()
            if i == 2: facts[1]["StableOriginalId"] = facts[0]["StableOriginalId"]
            cases.append(case)
        self.assertEqual(self.replay(cases)["failed"], 3)

    def test_false_expected_win_is_failure_and_other_cases_continue(self):
        bad = self.fixture("CE", "lose"); bad["expect"]["Outcome"] = "Victory"
        good = self.fixture("BA"); good["id"] = "after-failure"
        result = self.replay([bad, good])
        self.assertEqual((result["failed"], result["passed"]), (1, 1))
        self.assertIn("expected Victory, actual Defeat", result["cases"][0]["error"])

    def test_duplicate_json_key_rejected(self):
        result = self.replay([], raw='{"schema_version":1,"cases":[],"cases":[]}')
        self.assertEqual(result["process_exit"], 1)
        self.assertIn("Duplicate JSON key", result["error"])

    def test_duplicate_case_ids_invalid_repeat_and_unknown_family(self):
        a = self.fixture(); b = self.fixture()
        c = self.fixture(); c["id"] = "repeat"; c["steps"][0]["repeat"] = 0
        d = self.fixture(); d["id"] = "family"; d["family"] = "INVENTED"
        result = self.replay([a, b, c, d])
        self.assertEqual((result["passed"], result["failed"]), (1, 3))

    def test_convoy_pause_preserves_four_point_nine_second_dwell(self):
        case = self.fixture()
        facts = case["steps"][0]["facts"]
        facts["SimulationDeltaTicks"] = 49*probes.T//10
        pause = copy.deepcopy(facts); pause["Paused"] = True; pause["SimulationDeltaTicks"] = 2000*probes.T
        finish = copy.deepcopy(facts); finish["SimulationDeltaTicks"] = probes.T//10
        case["steps"] = [
            dict(facts=facts, expect=dict(Outcome="None", ElapsedTicks=49*probes.T//10)),
            dict(facts=pause, expect=dict(Outcome="None", ElapsedTicks=49*probes.T//10)),
            dict(facts=finish)]
        result = self.replay([case])
        self.assertEqual(result["failed"], 0)
        self.assertEqual(len(result["cases"][0]["replay"]["snapshots"][-1]["transitions"]), 2)

    def test_breakthrough_unlock_tick_never_adds_retroactive_exit_dwell(self):
        case = self.fixture("BT")
        open_facts = case["steps"][0]["facts"]
        open_facts["SimulationDeltaTicks"] = 20*probes.T
        for item in open_facts["OriginalSubjects"][:8]: item["InsideExit"] = True
        almost = copy.deepcopy(open_facts); almost["SimulationDeltaTicks"] = 29*probes.T//10
        finish = copy.deepcopy(almost); finish["SimulationDeltaTicks"] = probes.T//10
        case["steps"] = [
            dict(facts=open_facts, expect=dict(ExitOpen=True, Outcome="None")),
            dict(facts=almost, expect=dict(Outcome="None")),
            dict(facts=finish)]
        self.assertEqual(self.replay([case])["failed"], 0)

    def test_frontline_contest_freezes_last_tenth_capture(self):
        case = self.fixture("FC")
        start = case["steps"][0]["facts"]
        start["SimulationDeltaTicks"] = 79*probes.T//10
        contest = copy.deepcopy(start)
        contest["SimulationDeltaTicks"] = probes.T//10
        contest["ZoneA"]["EnemyEligibleInfantry"] = 1
        finish = copy.deepcopy(start); finish["SimulationDeltaTicks"] = probes.T//10
        case["steps"] = [
            dict(facts=start, expect={"ZoneA": {"ProgressTicks": 79*probes.T//10, "Owner": "Neutral"}}),
            dict(facts=contest, expect={"ZoneA": {"ProgressTicks": 79*probes.T//10, "Contested": True}}),
            dict(facts=finish)]
        case["expect"] = {"Outcome": "None", "ZoneA": {"Owner": "Player", "ProgressTicks": 0}}
        self.assertEqual(self.replay([case])["failed"], 0)

    def test_base_assault_surrender_precedes_both_base_deaths(self):
        case = self.fixture("BA")
        case["steps"][0]["facts"].update(PlayerDesignatedAlive=False, EnemyDesignatedAlive=False, Surrender=True)
        case["expect"] = dict(Outcome="Defeat", Reason="Surrender", Terminal=True)
        self.assertEqual(self.replay([case])["failed"], 0)

    def test_deterministic_replay_has_identical_states_and_transitions(self):
        cases = [self.fixture(family) for family in ("BA", "FC", "BT", "CE")]
        a, b = self.replay(cases), self.replay(cases)
        self.assertEqual(a["cases"], b["cases"])

    def clone_build(self):
        clone = self.output/"build"
        shutil.copytree(BUILD, clone, ignore=shutil.ignore_patterns("dotnet-home", "obj", "source"))
        manifest = json.loads((clone/"build.json").read_text())
        root = self.output/"project"
        for name in manifest["source_pins"]:
            p = root/name; p.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(PROJECT/name, p)
        manifest["project"] = str(root)
        (clone/"build.json").write_text(json.dumps(manifest))
        return clone, root

    def test_stale_source_and_changed_binary_are_refused_before_replay(self):
        clone, root = self.clone_build()
        source = root/sim.SOURCES[0]; original = source.read_bytes(); source.write_bytes(original+b"\n//changed")
        with self.assertRaisesRegex(ValueError, "Production source changed"):
            self.replay([self.fixture()], build=clone)
        source.write_bytes(original)
        binary = clone/"bin/ObjectiveReplay.dll"; binary.write_bytes(binary.read_bytes()+b"changed")
        with self.assertRaisesRegex(ValueError, "Built artifact changed"):
            self.replay([self.fixture()], build=clone)

    def test_incomplete_manifest_cannot_skip_source_validation(self):
        clone, _ = self.clone_build()
        path = clone/"build.json"; manifest = json.loads(path.read_text()); manifest["source_pins"].pop(sim.BASE)
        path.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError, "Incomplete/unsupported production source"):
            self.replay([self.fixture()], build=clone)

    def test_base_extraction_is_verbatim_and_ambiguous_source_refused(self):
        text = (PROJECT/sim.BASE).read_text()
        generated, meta = sim.extract_base(text)
        self.assertIn("public static bool TryEvaluate", generated)
        self.assertGreater(meta["start_line"], 1)
        with self.assertRaises(ValueError): sim.extract_base(text+text)
        with self.assertRaises(ValueError): sim.extract_base("")

    def test_outputs_cannot_overwrite_prior_run_or_import_assets(self):
        with self.assertRaises(FileExistsError): sim.new_output(PROJECT, self.output)
        with self.assertRaises(ValueError): sim.new_output(PROJECT, PROJECT/"Assets/forbidden-offline-test")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    BUILD = args.build.resolve()
    OUTPUT = sim.new_output(PROJECT, args.output)
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(OfflineSimTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    print(f"[SkirmishOfflineSimTests] result={'Passed' if result.wasSuccessful() else 'Failed'} tests={result.testsRun} skipped={len(result.skipped)} native_runs=0")
    raise SystemExit(0 if result.wasSuccessful() else 1)
