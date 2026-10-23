"""Mission orchestration integration, repeat and negative controls (world facts supplied)."""
import argparse
import copy
import json
from pathlib import Path
import shutil
import sys
import unittest
sys.dont_write_bytecode = True
import skirmish_mission_replay as sim
import skirmish_offline_sim_cases as probes

BUILD = OUTPUT = None
PROJECT = Path(__file__).resolve().parents[2]
from skirmish_mission_replay_cases import convert, generate


def fixture(family="CE", mode="win", sid="fixture"):
    deadline = 1200 if family == "FC" else 1080
    return convert(probes.case(sid, family, "Standard", deadline, mode), deadline)


class MissionReplayTests(unittest.TestCase):
    def setUp(self):
        self.output = OUTPUT/self._testMethodName; self.output.mkdir(); self.index = 0

    def replay(self, cases, raw=None, build=None):
        self.index += 1
        path = self.output/f"input-{self.index}.json"
        path.write_text(raw if raw is not None else json.dumps(dict(schema_version=1, cases=cases)))
        return sim.run(build or BUILD, path, self.output/f"run-{self.index}")

    def test_1800_catalog_synthetic_mission_traces_repeat_identically(self):
        suite, manifest = generate(PROJECT)
        cases = suite["cases"]
        a,b = self.replay(cases),self.replay(cases)
        self.assertEqual((manifest["catalog_entries"],len(cases),a["passed"],a["failed"],a["process_exit"]),(120,1800,1800,0,0))
        self.assertEqual(a["cases"],b["cases"])
        self.assertFalse(a["full_battle_simulated"]); self.assertFalse(a["mission_qualified"])
        self.assertFalse(a["persistence_executed"])
        (self.output/"benchmark.json").write_text(json.dumps(dict(cases=1800,first_seconds=a["elapsed_seconds"],repeat_seconds=b["elapsed_seconds"],identical=True,world_facts_supplied=True,mission_qualified=False),indent=2))

    def test_pause_resume_and_fractional_clock_preserve_dwell(self):
        c=fixture(); row=c["steps"][0]; row.update(delta_seconds=4.5,repeat=1,expect={"ElapsedSeconds":4.5,"Outcome":"None"})
        pause=copy.deepcopy(row); pause.update(paused=True,delta_seconds=1000,expect={"ElapsedSeconds":4.5,"Outcome":"None"})
        finish=copy.deepcopy(row); finish.update(delta_seconds=.5,expect={"ElapsedSeconds":5,"Outcome":"Victory","FreezeCount":1})
        c["steps"]=[row,pause,finish]
        r=self.replay([c]); self.assertEqual(r["failed"],0)
        self.assertEqual(len(r["cases"][0]["replay"]["snapshots"][-1]["departures"]),2)
        tiny=fixture("FC","pause","tiny"); tiny["steps"][0].update(paused=False,delta_seconds=.00000003,repeat=17)
        r=self.replay([tiny]); self.assertEqual(r["failed"],0)
        state=r["cases"][0]["replay"]["state"]
        self.assertEqual(state["Objective"]["ElapsedTicks"],5)
        self.assertGreater(state["CollectorFractionalTicks"],0)
        self.assertLess(state["CollectorFractionalTicks"],1)

    def test_base_projection_precedes_late_surrender_and_terminal_facts_stay_frozen(self):
        c=fixture("BA"); row=c["steps"][0]; row.update(surrender=True,delta_seconds=1)
        row["world_facts"]["PlayerDesignatedAlive"]=False
        later=copy.deepcopy(row); later.update(surrender=False,delta_seconds=1000,repeat=5)
        later["world_facts"].update(PlayerDesignatedAlive=True,EnemyDesignatedAlive=True)
        c["steps"]=[row,later]; c["expect"].update(Outcome="Draw",Reason="BothBasesDestroyed",ElapsedSeconds=1,Playing=0,Paused=1,Objective={"PlayerDesignatedAlive":False,"EnemyDesignatedAlive":False})
        self.assertEqual(self.replay([c])["failed"],0)

    def test_nonbase_surrender_bypasses_invalid_world_collection_and_paused_time(self):
        c=fixture("FC","surrender"); c["steps"][0].update(paused=True)
        c["steps"][0]["world_facts"]["OriginalBaseFactsPresent"]=False
        c["expect"].update(ElapsedSeconds=0,CollectorLastSeconds=0,Reason="Surrender")
        self.assertEqual(self.replay([c])["failed"],0)

    def test_initializing_then_playing_and_legacy_skip(self):
        c=fixture("BA"); c["initial_phase"]="Initializing"
        row=c["steps"][0]; row.update(delta_seconds=10,expect={"ElapsedSeconds":0,"Outcome":"None","Frozen":False})
        later=copy.deepcopy(row); later.update(phase="Playing",expect={"ElapsedSeconds":10,"Outcome":"Victory"})
        c["steps"]=[row,later]
        legacy=fixture("BA",sid="legacy"); legacy["legacy"]=True; legacy["expect"]=dict(Outcome="None",Frozen=False,ElapsedSeconds=0,DeadlineSeconds=0)
        self.assertEqual(self.replay([c,legacy])["failed"],0)

    def test_repeat_missions_have_distinct_results_no_clock_or_departure_leak(self):
        first=fixture("CE",sid="first"); second=fixture("CE","lose","second")
        r=self.replay([first,second]); self.assertEqual(r["failed"],0)
        a,b=[v["replay"]["state"] for v in r["cases"]]
        self.assertEqual((a["FreezeCount"],b["FreezeCount"]),(1,1)); self.assertNotEqual(a["Result"]["SessionId"],b["Result"]["SessionId"])
        self.assertEqual(len(a["Departed"]),2); self.assertEqual(b["Departed"],[])
        self.assertEqual((a["ElapsedSeconds"],b["ElapsedSeconds"]),(5,1))

    def test_missing_derived_unknown_facts_and_illegal_clock_rejected(self):
        cases=[]
        for i in range(6):
            c=fixture(sid=str(i)); row=c["steps"][0]
            if i==0: del row["world_facts"]["OriginalTrucks"]
            if i==1: row["world_facts"]["SimulationDeltaTicks"]=50000000
            if i==2: row["delta_seconds"]=-1
            if i==3: row["repeat"]=100001
            if i==4: row["world_facts"]["OriginalTrucks"][0]["Alive"]=1
            if i==5: row["world_facts"]["OriginalTrucks"][0]["StableOriginalId"]="wrong"
            cases.append(c)
        r=self.replay(cases); self.assertEqual((r["failed"],r["process_exit"]),(6,1))

    def test_false_win_expectation_and_frozen_phase_override_rejected(self):
        c=fixture("CE","lose"); c["expect"]["Outcome"]="Victory"
        d=fixture("BA",sid="phase"); extra=copy.deepcopy(d["steps"][0]); extra["phase"]="Playing"; d["steps"].append(extra)
        self.assertEqual(self.replay([c,d])["failed"],2)

    def test_duplicate_keys_ids_and_unknown_family_are_rejected(self):
        r=self.replay([],raw='{"schema_version":1,"cases":[],"cases":[]}'); self.assertEqual(r["process_exit"],1)
        a=fixture(); b=copy.deepcopy(a); c=fixture(sid="bad"); c["family"]="OTHER"
        r=self.replay([a,b,c]); self.assertEqual((r["passed"],r["failed"]),(1,2))

    def test_unknown_numeric_composite_phases_and_vacuous_expectations_rejected(self):
        cases=[]
        for i,value in enumerate(("255","Playing, Cleaning","Playing, Queued","playing")):
            c=fixture(sid=f"phase-{i}"); c["initial_phase"]=value; c["steps"][0]["phase"]="Playing"; cases.append(c)
            c=fixture(sid=f"step-{i}"); c["steps"][0]["phase"]=value; cases.append(c)
        c=fixture(sid="empty"); c["expect"]={"Objective":{}}; cases.append(c)
        c=fixture(sid="empty-row"); c["steps"][0]["expect"]={}; cases.append(c)
        self.assertEqual(self.replay(cases)["failed"],10)

    def clone(self):
        clone=self.output/"build"; shutil.copytree(BUILD,clone,ignore=shutil.ignore_patterns("dotnet-home","obj","source"))
        path=clone/"build.json"; manifest=json.loads(path.read_text()); root=self.output/"project"
        for name in manifest["source_pins"]:
            p=root/name; p.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(PROJECT/name,p)
        manifest["project"]=str(root); path.write_text(json.dumps(manifest)); return clone,root

    def test_source_binary_and_manifest_drift_fail_closed(self):
        clone,root=self.clone(); source=root/sim.KERNEL; original=source.read_bytes(); source.write_bytes(original+b"\n// changed")
        with self.assertRaisesRegex(ValueError,"Production source changed"): self.replay([fixture()],build=clone)
        source.write_bytes(original); binary=clone/"bin/MissionReplay.dll"; data=binary.read_bytes(); binary.write_bytes(data+b"x")
        with self.assertRaisesRegex(ValueError,"Built artifact changed"): self.replay([fixture()],build=clone)
        binary.write_bytes(data); path=clone/"build.json"; manifest=json.loads(path.read_text()); manifest["source_pins"].pop(sim.KERNEL); path.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError,"Incomplete/unsupported mission source"): self.replay([fixture()],build=clone)

    def test_outputs_cannot_overwrite_evidence_or_import_assets(self):
        with self.assertRaises(FileExistsError): sim.common.new_output(PROJECT,self.output)
        with self.assertRaises(ValueError): sim.common.new_output(PROJECT,PROJECT/"Assets/forbidden-mission-test")


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument("--build",type=Path,required=True); parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args(); BUILD=args.build.resolve(); OUTPUT=sim.common.new_output(PROJECT,args.output)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(MissionReplayTests))
    print(f"[SkirmishMissionReplayTests] result={'Passed' if result.wasSuccessful() else 'Failed'} tests={result.testsRun} skipped={len(result.skipped)} native_runs=0")
    raise SystemExit(0 if result.wasSuccessful() else 1)
