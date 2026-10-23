"""Boundary, rejection, repeatability and integrity checks for the shared attack planner."""
import argparse
import copy
import json
from pathlib import Path
import shutil
import sys
import unittest

sys.dont_write_bytecode = True
import skirmish_combat_replay as driver


def candidate():
    return dict(State=dict(CooldownRemaining=0, TraceTimeRemaining=0, TracePhase=0,
                           ShotCounter=0, AnimationTimeRemaining=1),
                SelfHealth=100, TargetHealth=100, HasTarget=1, TargetExists=1,
                TargetHasHealth=1, TargetHasTransform=1, TargetIsStaticGridBlocker=0,
                IsDebugFireTarget=0, HasAnimationOrder=0, AircraftAttackWindowBlocked=0,
                AttackerX=0, AttackerZ=0, TargetX=1, TargetZ=0, SelfCombatRadius=0,
                TargetCombatRadius=0, Range=1, CooldownSeconds=1, Damage=10,
                TraceVisibleSeconds=.125, TracerEveryNthShot=1, AttackAnimationSeconds=.25)


def fixtures():
    rows = []
    def add(name, expect, state=None, dt=.125, **patch):
        data = candidate(); data.update(patch)
        if state: data['State'].update(state)
        rows.append(dict(id=name, input=data, delta_time=dt, cell_size=1, expect=expect))
    add('shot', dict(Kind='Shot', Damage=10, ShowTracer=1,
        BaseState=dict(CooldownRemaining=-.125, ShotCounter=0, AnimationTimeRemaining=1),
        State=dict(CooldownRemaining=1, TraceTimeRemaining=.125, TracePhase=.371, ShotCounter=1, AnimationTimeRemaining=1.15)))
    add('range-equal', dict(Kind='Shot'), TargetX=1.25)
    add('range-outside', dict(Kind='None'), TargetX=1.250001)
    add('xz-range', dict(Kind='None'), TargetX=1, TargetZ=1)
    add('radii-extend-range', dict(Kind='Shot'), TargetX=3.25, SelfCombatRadius=1, TargetCombatRadius=1)
    add('cooldown-active', dict(Kind='None', BaseState=dict(CooldownRemaining=.125)), state=dict(CooldownRemaining=.25))
    add('cooldown-equal-crossing-skips-windup', dict(Kind='Shot'), state=dict(CooldownRemaining=.125, AnimationTimeRemaining=0), HasAnimationOrder=1)
    add('windup', dict(Kind='Windup', State=dict(CooldownRemaining=.18, AnimationTimeRemaining=.25, ShotCounter=0)), state=dict(AnimationTimeRemaining=0), HasAnimationOrder=1)
    add('windup-minimum', dict(Kind='Windup', State=dict(CooldownRemaining=.05, AnimationTimeRemaining=.01)), state=dict(AnimationTimeRemaining=0), HasAnimationOrder=1, CooldownSeconds=0, AttackAnimationSeconds=0)
    add('animation-live-shoots', dict(Kind='Shot'), HasAnimationOrder=1)
    add('no-tracer', dict(Kind='Shot', ShowTracer=0, State=dict(ShotCounter=1, TraceTimeRemaining=0, TracePhase=0)), TracerEveryNthShot=3)
    add('third-tracer', dict(Kind='Shot', ShowTracer=1, State=dict(ShotCounter=3)), state=dict(ShotCounter=2), TracerEveryNthShot=3)
    add('counter-overflow', dict(Kind='Shot', ShowTracer=1, State=dict(ShotCounter=-2147483648)), state=dict(ShotCounter=2147483647), TracerEveryNthShot=2)
    add('trace-phase-wrap', dict(Kind='Shot', State=dict(TracePhase=.12100005)), state=dict(TracePhase=.75))
    add('nonpositive-tracer', dict(Kind='Shot', ShowTracer=1), TracerEveryNthShot=-1)
    add('zero-damage-still-shot', dict(Kind='Shot', Damage=0), Damage=0)
    add('dead-preserves-base-tick', dict(Kind='None', BaseState=dict(CooldownRemaining=-.125, TraceTimeRemaining=.125)), SelfHealth=0, state=dict(TraceTimeRemaining=.25))
    add('no-target-before-invalid', dict(Kind='None'), HasTarget=0, TargetExists=0)
    for flag in ('TargetExists', 'TargetHasHealth', 'TargetHasTransform'):
        add('missing-' + flag, dict(Kind='ClearTarget'), **{flag: 0})
    add('dead-target', dict(Kind='ClearTarget'), TargetHealth=0)
    add('static-target', dict(Kind='ClearTarget'), TargetIsStaticGridBlocker=1)
    add('debug-static-target', dict(Kind='Shot'), TargetIsStaticGridBlocker=1, IsDebugFireTarget=1)
    add('air-window', dict(Kind='None'), AircraftAttackWindowBlocked=1)
    add('invalid-before-air-window', dict(Kind='ClearTarget'), TargetExists=0, AircraftAttackWindowBlocked=1)
    add('zero-range', dict(Kind='None'), Range=0)
    add('negative-range', dict(Kind='None'), Range=-1)
    add('zero-dt-ready-host-owns-pause', dict(Kind='Shot'), dt=0)
    add('minimum-cooldown', dict(Kind='Shot', State=dict(CooldownRemaining=.01)), CooldownSeconds=0)
    return dict(schema_version=1, cases=rows)


class CombatReplayTests(unittest.TestCase):
    def replay(self, data, tag):
        path = OUTPUT / (tag + '.json')
        path.write_text(data if isinstance(data, str) else json.dumps(data))
        return driver.run(BUILD, path, OUTPUT / tag)

    def test_boundaries(self):
        report = self.replay(fixtures(), 'boundaries')
        self.assertEqual(report['failed'], 0, report)
        self.assertEqual(report['passed'], 30)
        self.assertEqual(report['process_exit'], 0)
        self.assertFalse(report['full_battle_simulated'])
        self.assertFalse(report['mission_qualified'])

    def test_equivalent_numeric_notation(self):
        data = fixtures(); data['cases'] = data['cases'][:1]
        data['cases'][0]['expect']['State']['CooldownRemaining'] = 1.0
        data['cases'][0]['expect']['Damage'] = 10.0
        self.assertEqual(self.replay(data, 'number-notation')['failed'], 0)

    def test_deterministic_and_bulk(self):
        rows = [dict(copy.deepcopy(fixtures()['cases'][i % 30]), id=str(i)) for i in range(10000)]
        data = dict(schema_version=1, cases=rows)
        a = self.replay(data, 'bulk-a'); b = self.replay(data, 'bulk-b')
        self.assertEqual(a['failed'], 0, a)
        self.assertEqual(a['cases'], b['cases'])

    def test_reject_invalid_candidates_and_expectations(self):
        rows = []
        for i in range(10):
            row = copy.deepcopy(fixtures()['cases'][0]); row['id'] = str(i); rows.append(row)
        del rows[0]['input']['State']['CooldownRemaining']
        rows[1]['input']['surprise'] = 1
        rows[2]['input']['HasTarget'] = 2
        rows[3]['delta_time'] = -1
        rows[4]['cell_size'] = 0
        rows[5]['expect'] = {}
        rows[6]['expect'] = dict(Kind='Windup')
        rows[7]['input']['Range'] = 1e50
        rows[8]['expect'] = dict(State={})
        rows[9]['input']['State']['ShotCounter'] = 2147483648
        report = self.replay(dict(schema_version=1, cases=rows), 'invalid')
        self.assertEqual(report['failed'], 10, report)
        self.assertEqual(report['process_exit'], 1)

    def test_duplicate_keys_ids_and_empty_cases(self):
        data = fixtures(); data['cases'][1]['id'] = data['cases'][0]['id']
        self.assertEqual(self.replay(data, 'duplicate-id')['failed'], 1)
        for name, data in [('duplicate-key', '{"schema_version":1,"schema_version":1,"cases":[]}'),
                           ('empty', '{"schema_version":1,"cases":[]}')]:
            self.assertEqual(self.replay(data, name)['result'], 'failed')

    def test_stale_binary_and_source_refused(self):
        clone = OUTPUT/'cloned-build'; shutil.copytree(BUILD, clone)
        manifest = json.loads((clone/'build.json').read_text())
        source = OUTPUT/'cloned-source'; source.mkdir()
        original = Path(manifest['project'])
        for rel in manifest['source_pins']:
            dst = source/rel; dst.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(original/rel, dst)
        manifest['project'] = str(source)
        (clone/'build.json').write_text(json.dumps(manifest))
        data = OUTPUT/'stale-input.json'; data.write_text(json.dumps(fixtures()))
        kernel = source/driver.KERNEL; kernel.write_text(kernel.read_text() + '// changed\n')
        with self.assertRaisesRegex(ValueError, 'Production source changed'):
            driver.run(clone, data, OUTPUT/'stale-source-run')
        shutil.copyfile(original/driver.KERNEL, kernel)
        dll = clone/'bin/CombatReplay.dll'; dll.write_bytes(dll.read_bytes() + b'changed')
        with self.assertRaisesRegex(ValueError, 'Built artifact changed'):
            driver.run(clone, data, OUTPUT/'stale-binary-run')

    def test_output_isolation(self):
        manifest = json.loads((BUILD/'build.json').read_text()); project = Path(manifest['project'])
        with self.assertRaises(ValueError): driver.new_output(project, project/'Assets'/'forbidden-combat-output')
        with self.assertRaises(FileExistsError): driver.new_output(project, OUTPUT)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--build', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    args = p.parse_args(); BUILD = args.build.resolve(); OUTPUT = args.output.resolve(); OUTPUT.mkdir(parents=True, exist_ok=False)
    (OUTPUT/'examples.json').write_text(json.dumps(fixtures(), indent=2) + '\n')
    result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(CombatReplayTests))
    print(json.dumps(dict(result='passed' if result.wasSuccessful() else 'failed', tests=result.testsRun,
                         failures=len(result.failures), errors=len(result.errors), skipped=len(result.skipped), output=str(OUTPUT))))
    raise SystemExit(0 if result.wasSuccessful() else 1)
