"""Independent boundary and repeat controls for shared Skirmish squad targeting."""
import argparse
import copy
import json
from pathlib import Path
import sys
import unittest
sys.dont_write_bytecode = True
import skirmish_world_replay as sim

BUILD = OUTPUT = None
PROJECT = Path(__file__).resolve().parents[2]


def candidate(index, x, z=0, faction=1, health=100, damage=10, version=1):
    return dict(Entity=dict(Index=index,Version=version),Faction=faction,Health=health,Damage=damage,X=x,Z=z)


def case(id="squad"):
    return dict(id=id,kind="squad_target",input=dict(Faction=2,OwnBaseX=0,OwnBaseZ=0,DefenseRadius=10,
        ElapsedSeconds=0,FirstAttackSeconds=5,OpposingBase=dict(Index=90,Version=7)),
        candidates=[],members=[],expect=dict(Target=dict(Index=0,Version=0),Threatened=False))


class SquadTargetingTests(unittest.TestCase):
    def setUp(self):
        self.output=OUTPUT/self._testMethodName; self.output.mkdir(); self.index=0
    def replay(self,cases):
        self.index+=1; path=self.output/f"input-{self.index}.json"; path.write_text(json.dumps(dict(schema_version=1,cases=cases)))
        return sim.run(BUILD,path,self.output/f"run-{self.index}")
    def test_defense_precedes_closer_field_enemy(self):
        c=case(); c["candidates"]=[candidate(1,9),candidate(2,100)]
        c["members"]=[dict(Eligible=True,X=100,Z=0,Range=1)]
        c["expect"]=dict(Target=dict(Index=1,Version=1),Threatened=True)
        self.assertEqual(self.replay([c])["failed"],0)
    def test_radius_boundary_opening_and_missing_members(self):
        a=case("before"); a["candidates"]=[candidate(1,10)]
        b=copy.deepcopy(a); b["id"]="opening"; b["input"]["ElapsedSeconds"]=5
        b["expect"]=dict(Target=dict(Index=90,Version=7),Threatened=False)
        self.assertEqual(self.replay([a,b])["failed"],0)
    def test_equal_distance_preserves_supplied_order_and_entity_version(self):
        a=case("first"); a["candidates"]=[candidate(4,5,version=2),candidate(4,-5,version=3)]
        a["expect"]=dict(Target=dict(Index=4,Version=2),Threatened=True)
        b=copy.deepcopy(a); b["id"]="second"; b["candidates"].reverse(); b["expect"]["Target"]["Version"]=3
        self.assertEqual(self.replay([a,b])["failed"],0)
    def test_neutral_ally_dead_and_unarmed_candidates_are_skipped(self):
        c=case(); c["candidates"]=[candidate(1,1,faction=0),candidate(2,2,faction=2),candidate(3,3,health=0),candidate(4,4,damage=0),candidate(5,5)]
        c["expect"]=dict(Target=dict(Index=5,Version=1),Threatened=True)
        self.assertEqual(self.replay([c])["failed"],0)
    def test_alive_forward_member_detection_is_not_squad_center(self):
        c=case(); c["candidates"]=[candidate(1,108)]
        c["members"]=[dict(Eligible=False,X=108,Z=0,Range=100),dict(Eligible=True,X=0,Z=0,Range=1),dict(Eligible=True,X=100,Z=0,Range=1)]
        c["expect"]=dict(Target=dict(Index=1,Version=1),Threatened=True)
        self.assertEqual(self.replay([c])["failed"],0)
    def test_360_synthetic_queries_repeat_identically(self):
        cases=[]
        for i in range(360):
            c=case(str(i)); c["candidates"]=[candidate(i+1,3,4,version=i%5+1)]
            c["expect"]=dict(Target=dict(Index=i+1,Version=i%5+1),Threatened=True); cases.append(c)
        timings=[0.0,0.0]
        for start in (0,180):
            a,b=self.replay(cases[start:start+180]),self.replay(cases[start:start+180])
            self.assertEqual((a["passed"],a["failed"]),(180,0)); self.assertEqual(a["cases"],b["cases"])
            self.assertFalse(a["full_battle_simulated"]); self.assertFalse(a["mission_qualified"])
            timings[0]+=a["elapsed_seconds"]; timings[1]+=b["elapsed_seconds"]
        (self.output/"benchmark.json").write_text(json.dumps(dict(cases=360,first_seconds=timings[0],repeat_seconds=timings[1],identical=True),indent=2))
    def test_missing_unknown_duplicate_and_vacuous_inputs_fail(self):
        cases=[]
        for i in range(5):
            c=case(str(i)); c["candidates"]=[candidate(1,1)]
            if i==0: del c["candidates"][0]["Health"]
            if i==1: c["input"]["Undeclared"]=1
            if i==2: c["candidates"].append(copy.deepcopy(c["candidates"][0]))
            if i==3: c["expect"]={"Target":{}}
            if i==4: c["candidates"][0]["Entity"]["Index"]=0
            cases.append(c)
        self.assertEqual(self.replay(cases)["failed"],5)


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument("--build",type=Path,required=True); parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args(); BUILD=args.build.resolve(); OUTPUT=sim.common.new_output(PROJECT,args.output)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(SquadTargetingTests))
    print(f"[SkirmishSquadTargetingReplayTests] result={'Passed' if result.wasSuccessful() else 'Failed'} tests={result.testsRun} skipped={len(result.skipped)} native_runs=0")
    raise SystemExit(0 if result.wasSuccessful() else 1)
