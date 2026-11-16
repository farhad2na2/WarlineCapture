"""Ordered damage, retained movement, admission rejection and deterministic bulk replay."""
import argparse
import copy
import json
from pathlib import Path
import sys
import unittest
sys.dont_write_bytecode = True
import skirmish_combat_replay as driver
from skirmish_combat_replay_test import candidate


def target(name='t', health=100, **patch):
    return dict(dict(Id=name, Health=health, Exists=True, HasHealth=True, CoverEligible=True, CoverPermille=500), **patch)


def shot(index, damage=120, **patch):
    attack = candidate(); attack['Damage'] = damage
    attack.update(patch)
    return dict(Target='t', Source=dict(Index=index, Version=1, CellX=index, CellZ=0, X=0, Y=0, Z=0),
                AttackerExists=True, Input=attack)


def damage_row(name='overkill'):
    return dict(id=name, kind='damage_sequence', input=dict(DeltaTime=.125, CellSize=1,
        Targets=[target()], Candidates=[shot(i) for i in range(1, 4)]), expect=dict(
            Shots=[dict(Decision='Shot', Damage=60, State=dict(ShotCounter=1)),
                   dict(Decision='Shot', Damage=60, State=dict(ShotCounter=1)),
                   dict(Decision='ClearTarget', Damage=0, State=dict(ShotCounter=0, CooldownRemaining=-.125))],
            Targets=[dict(Health=0, PredictedHealth=0, Aggregate=dict(TotalDamage=120,
                LastSource=dict(Index=2), ObservationSource=dict(Index=1), HasMixedObservationSources=1, ShowImpactVfx=1))]))


def movement_row(name='retained-motion'):
    frame = dict(DeltaTime=1, PathAdmissionKnown=True, PathAdmitted=True,
                 SettleCellKnown=False, SettleCellX=0, SettleCellZ=0)
    return dict(id=name, kind='infantry_trace', input=dict(
        InitialState=dict(X=0, Y=2, Z=0, RemainingDistance=0, CellX=0, CellZ=0, PathIndex=0),
        Speed=1, ArriveDistance=0, CellSize=1, GroupedManualMove=False,
        Path=[dict(CellX=i, CellZ=0, X=i, Z=0) for i in range(1, 4)], Frames=[copy.deepcopy(frame) for _ in range(3)]),
        expect=dict(State=dict(X=3, Y=2, Z=0, PathIndex=3, CellX=3), Frames=[
            dict(State=dict(X=i, PathIndex=i, CellX=i)) for i in range(1, 4)], RouteLegalityProven=False))


def gate_row(name='occupied-wait'):
    return dict(id=name, kind='movement_gate', input=dict(Start=0, Length=2, PathIndex=0, PoolLength=2,
        FactsKnown=True, CampaignGuidedMove=False, CellBlocked=False, OccupantBlocks=True,
        DeltaTime=.125, CurrentSpeed=3, StallSeconds=0),
        expect=dict(Range='Ready', Action='Wait', CurrentSpeed=0, StallSeconds=.125))


def fixtures():
    rows = [damage_row(), movement_row(), gate_row()]
    row=damage_row('windup-after-predicted-death'); row['input']['Candidates'][2]['Input'].update(HasAnimationOrder=1)
    row['input']['Candidates'][2]['Input']['State']['AnimationTimeRemaining']=0
    row['expect']['Shots'][2]=dict(Decision='Windup', Damage=0, State=dict(ShotCounter=0, CooldownRemaining=.18))
    rows.append(row)
    row=damage_row('missing-attacker'); row['input']['Candidates'][2]['AttackerExists']=False
    row['expect']['Shots'][2]=dict(Decision='MissingAttacker', State=dict(ShotCounter=0, CooldownRemaining=0)); rows.append(row)
    row=damage_row('zero-damage-consumes-shot'); row['input']['Candidates']=[shot(1, 0)]
    row['expect']=dict(Shots=[dict(Decision='Shot', Damage=0, State=dict(ShotCounter=1))], Targets=[dict(Health=100, HasAggregate=False)]);rows.append(row)
    row=damage_row('one-point-minimum'); row['input']['Targets'][0]['CoverPermille']=1;row['input']['Candidates']=[shot(1, 1)]
    row['expect']=dict(Shots=[dict(Damage=1)],Targets=[dict(Health=99, Aggregate=dict(TotalDamage=1,HasMixedObservationSources=0))]);rows.append(row)
    row=damage_row('cover-bypass'); row['input']['Targets'][0]['CoverEligible']=False;row['input']['Candidates']=[shot(1, 35)]
    row['expect']=dict(Targets=[dict(Health=65, Aggregate=dict(TotalDamage=35))]);rows.append(row)
    row=damage_row('identity-version');row['input']['Candidates'][1]['Source'].update(Index=1,Version=2)
    row['expect']['Targets'][0]['Aggregate']['LastSource']=dict(Index=1,Version=2);rows.append(row)
    row=damage_row('mutual-death-snapshot');row['input']['Targets']=[target('a'),target('b')]
    row['input']['Candidates']=[shot(1,200),shot(2,200)]
    row['input']['Candidates'][0]['Target']='b';row['input']['Candidates'][1]['Target']='a'
    row['expect']=dict(Shots=[dict(Decision='Shot'),dict(Decision='Shot')],Targets=[dict(Id='a',Health=0),dict(Id='b',Health=0)]);rows.append(row)
    row=movement_row('partial-step');row['input']['Speed']=.5;row['input']['Frames']=row['input']['Frames'][:1]
    row['expect']=dict(State=dict(X=.5,Y=2,PathIndex=0,CellX=0),Frames=[dict(Steps=[dict(Action='PartialMove',WriteFacing=True)])]);rows.append(row)
    row=movement_row('grouped-arrival');row['input'].update(GroupedManualMove=True,Path=[dict(CellX=1,CellZ=0,X=1,Z=0)])
    row['input']['Frames']=row['input']['Frames'][:1];row['input']['Frames'][0].update(SettleCellKnown=True,SettleCellX=0)
    row['expect']=dict(State=dict(X=0,Y=2,PathIndex=1,CellX=0),Frames=[dict(Steps=[dict(Action='GroupedStop',SettleCurrentCell=True)])]);rows.append(row)
    row=movement_row('eight-waypoint-cap');row['input'].update(Speed=100,Path=[dict(CellX=i,CellZ=0,X=i,Z=0) for i in range(1,13)])
    row['input']['Frames']=row['input']['Frames'][:1];row['expect']=dict(State=dict(X=8,PathIndex=8));rows.append(row)
    row=movement_row('zero-distance');row['input']['Speed']=0;row['expect']=dict(State=dict(X=0,PathIndex=0));rows.append(row)
    row=gate_row('occupied-repath');row['input']['DeltaTime']=.35;row['expect']=dict(Action='Repath',StallSeconds=0,CurrentSpeed=0);rows.append(row)
    row=gate_row('stale-path');row['input']['Length']=3;row['expect']=dict(Range='InvalidRange',Action='Repath',StallSeconds=0,CurrentSpeed=0);rows.append(row)
    row=gate_row('blocked-grid');row['input']['CellBlocked']=True;row['expect']=dict(Action='Repath',StallSeconds=0);rows.append(row)
    row=gate_row('guided-bypass');row['input'].update(CellBlocked=True,CampaignGuidedMove=True);row['expect']=dict(Action='AdmitPathFollow',CurrentSpeed=3,StallSeconds=0);rows.append(row)
    row=gate_row('finished-path');row['input']['PathIndex']=2;row['expect']=dict(Range='NoWork',Action='NoWork',CurrentSpeed=3);rows.append(row)
    return dict(schema_version=1,cases=rows)


class SequenceReplayTests(unittest.TestCase):
    def replay(self, data, tag):
        path=OUTPUT/(tag+'.json');path.write_text(json.dumps(data))
        return driver.run(BUILD,path,OUTPUT/tag)

    def test_shared_boundaries(self):
        result=self.replay(fixtures(),'boundaries')
        self.assertEqual(result['failed'],0,result)
        self.assertEqual(result['passed'],19)
        self.assertEqual(result['process_exit'],0)
        self.assertFalse(result['full_battle_simulated']);self.assertFalse(result['mission_qualified'])

    def test_missing_facts_and_inconsistent_snapshot_fail(self):
        rows=[]
        def add(row): row['id']=str(len(rows));rows.append(row);return row
        add(movement_row())['input']['Frames'][0]['PathAdmissionKnown']=False
        add(movement_row())['input']['Frames'][0]['PathAdmitted']=False
        r=add(movement_row());r['input'].update(GroupedManualMove=True,Path=[dict(CellX=1,CellZ=0,X=1,Z=0)])
        add(gate_row())['input']['FactsKnown']=False
        add(damage_row())['input']['Candidates'][0]['Input']['TargetHealth']=99
        add(damage_row())['input']['Candidates'][0]['Input']['TargetExists']=0
        add(damage_row())['input']['Candidates'][1]['Source']['Index']=1
        add(damage_row())['input']['Candidates'][0]['Source']['X']=999
        add(damage_row())['input']['Targets'].append(target())
        add(movement_row())['input']['Speed']=-1
        add(movement_row())['input']['Frames'][0]['DeltaTime']=-1
        del add(movement_row())['input']['Frames'][0]['PathAdmitted']
        add(damage_row())['input']['Candidates'][0]['Input']['HasTarget']=2
        add(movement_row())['input']['Path'][0]['X']=1e50
        add(movement_row())['input']['Frames']=[]
        add(gate_row())['expect']={}
        r=self.replay(dict(schema_version=1,cases=rows),'invalid')
        self.assertEqual(r['failed'],len(rows),r);self.assertEqual(r['process_exit'],1)

    def test_bulk_120_configs_three_sizes_repeat_deterministically(self):
        # Synthetic arithmetic/route fixtures, not catalog qualification or complete missions.
        rows=[]
        for mission in range(120):
            for size in (16,32,64):
                d=damage_row(f'synthetic-{mission}-{size}-damage')
                d['input']['Targets'][0]['Health']=100000
                d['input']['Candidates']=[shot(i,10,TargetHealth=100000) for i in range(size)]
                d['expect']=dict(Targets=[dict(Health=100000-size*5,Aggregate=dict(TotalDamage=size*5))]);rows.append(d)
                m=movement_row(f'synthetic-{mission}-{size}-movement');rows.append(m)
        data=dict(schema_version=1,cases=rows)
        a=self.replay(data,'bulk-a');b=self.replay(data,'bulk-b')
        self.assertEqual(a['failed'],0,a);self.assertEqual(a['passed'],720)
        self.assertEqual(a['cases'],b['cases'])
        print(json.dumps(dict(benchmark_cases=720,ordered_attack_candidates=13440,
            first_seconds=a['elapsed_seconds'],repeat_seconds=b['elapsed_seconds'])))


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--build',required=True,type=Path);p.add_argument('--output',required=True,type=Path)
    args=p.parse_args();BUILD=args.build.resolve();OUTPUT=args.output.resolve();OUTPUT.mkdir(parents=True,exist_ok=False)
    (OUTPUT/'examples.json').write_text(json.dumps(fixtures(),indent=2)+'\n')
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(SequenceReplayTests))
    print(json.dumps(dict(result='passed' if result.wasSuccessful() else 'failed',tests=result.testsRun,
        failures=len(result.failures),errors=len(result.errors),skipped=len(result.skipped))))
    raise SystemExit(0 if result.wasSuccessful() else 1)
