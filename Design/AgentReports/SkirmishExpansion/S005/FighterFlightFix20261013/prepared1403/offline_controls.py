"""Bounded synthetic contract checks; no native execution or mission qualification."""
import ast
import copy
import importlib.util
import json
import math
from pathlib import Path

P = Path(__file__).parent
spec = importlib.util.spec_from_file_location('intercept1403', P / 'intercept_qualifier.py')
q = importlib.util.module_from_spec(spec)
spec.loader.exec_module(q)

# Reuse previously reviewed native-shaped contact/receipt/trajectory data builders.
tree = ast.parse((P / 'synthetic_shape_builders.py').read_text())
builders = [n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name in ('make', 'expand', 'logs')]
stages = next(n for n in tree.body if isinstance(n, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'stages' for t in n.targets))
exec(compile(ast.fix_missing_locations(ast.Module(body=[stages] + builders, type_ignores=[])), '<reviewed-builders>', 'exec'))
source = dict(attemptId='same', actorIndex=10, actorVersion=2, reservationId=1003)
airports = [dict(phase='EnemyNativeCompletionCaptured', faction=2, runtimeId=15, originX=99, originZ=0, footprintX=1, footprintZ=1)]
base, touch = make()
base = expand(base)


def preframe(rows):
    a = dict(stage='PublicRouteZoomRequested', attempt='same', actorIndex=10, actorVersion=2,
             reservation=1003, frame=1, acceptedBefore=2, acceptedNow=2,
             hiddenTargetPoseRead=False, dispatched=3, completed=3)
    result = [a, dict(a, stage='PublicRouteZoomSettled', frame=2, zoomElapsed=3.5, zoomStableFrames=6)]
    for row in rows:
        if row['stage'] not in ('PublicApproachPrepared', 'PublicIntermediateLegPrepared', 'PublicAlignedLegPrepared'):
            continue
        r = dict(a, stage='PublicRouteLegFocusRequested', frame=row['frame'], cycle=row['cycle'], leg=row['leg'],
                 acceptedBefore=2 + row['leg'], acceptedNow=2 + row['leg'],
                 selected=True, ready=False, dispatched=row['completed'], completed=row['completed'],
                 actualCameraChanged=True, goalExposed=True, goalExposedBefore=False,
                 beforeX=0., beforeY=24., beforeZ=0., beforeFov=36., beforeOrtho=0.,
                 afterX=1., afterY=45., afterZ=1., afterFov=36., afterOrtho=0.,
                 safeX=0., safeY=0., safeWidth=1920., safeHeight=1080.,
                 goalScreenX=300., goalScreenY=300., goalScreenZ=200.)
        for label in ('rear', 'middle', 'final'):
            r.update({label + 'ScreenX': 300., label + 'ScreenY': 300., label + 'ScreenZ': 200.})
        result += [r, dict(r, stage='PublicRouteLegFramed', ready=True, frame=row['frame'] + 1)]
    return result


checks = []


def check(name, rows, accepted, contacts=None):
    try:
        q.qualify(rows, source, touch if contacts is None else contacts, airports, preframe(rows), logs(base))
        errors = []
    except (KeyError, ValueError, TypeError, IndexError) as error:
        errors = [str(error)]
    assert (not errors) == accepted, (name, errors)
    checks.append(dict(name=name, expectedAccepted=accepted, errors=errors))


check('Complete original aligned route still qualifies', base, True)
actual = copy.deepcopy(base)
middle = next(r for r in actual if r['stage'] == 'NativeApproachArrived')
middle.update(forwardX=.6347746849, forwardZ=math.sqrt(1 - .6347746849 ** 2), forwardDot=.6347746849, aligned=False)
check('Intermediate point arrival accepts truthful non-axis heading', actual, True)

for name, values in [
    ('Wrong intermediate goal', dict(nativeGoalMatches=False)),
    ('Missing intermediate native target', dict(nativeGoalPresent=False, nativeGoalMatches=False)),
    ('Returning before intermediate admission', dict(returningHome=1)),
    ('Foreign intermediate Move receipt', dict(moveReceipt=999)),
    ('Reused rear receipt', dict(moveReceipt=10)),
    ('Intermediate outside actual proximity', dict(actorX=-140., signedAlong=-140., commandDistance=20., alertDistance=140., nearCommandGoal=False)),
    ('Fabricated intermediate aligned flag', dict(aligned=True)),
]:
    rows = copy.deepcopy(actual)
    next(r for r in rows if r['stage'] == 'NativeApproachArrived').update(values)
    check(name, rows, False)

for stage in ('NativeAlignedApproachArrived', 'PublicAlignedHoldTouch', 'HeldAlignedObserved', 'PublicCameraPreparedAligned', 'TargetFlightDepartureAligned'):
    rows = copy.deepcopy(actual)
    next(r for r in rows if r['stage'] == stage).update(forwardX=.6347746849, forwardZ=math.sqrt(1 - .6347746849 ** 2), forwardDot=.6347746849, aligned=False)
    check('Final heading still required: ' + stage, rows, False)

for name, stage, values in [
    ('Final native goal required', 'NativeAlignedApproachArrived', dict(nativeGoalPresent=False, nativeGoalMatches=False)),
    ('Final 32m alert band required', 'NativeAlignedApproachArrived', dict(actorX=33., signedAlong=33., alertDistance=33., commandDistance=27., nearAlertGoal=False)),
    ('Final distinct receipt required', 'NativeAlignedApproachArrived', dict(moveReceipt=11)),
    ('Final airborne state required', 'NativeAlignedApproachArrived', dict(airborne=False)),
    ('Actual Hold tag required', 'HeldAlignedObserved', dict(hold=False)),
    ('Actual Hold contact required', 'HeldAlignedObserved', dict(holdTouch=False)),
]:
    rows = copy.deepcopy(actual)
    next(r for r in rows if r['stage'] == stage).update(values)
    check(name, rows, False)

rows = copy.deepcopy(actual)
samples = [r for r in rows if r['stage'] == 'NativeFlyThroughSample']
samples[0].update(actorX=4., signedAlong=4., alertDistance=4., commandDistance=56.)
check('Final trajectory must advance', rows, False)
check('Missing paired Touch completion rejected', actual, False, touch[:-1])
check('Incomplete 1392-style route cannot qualify final combat preparation', actual[:8], False)

result = dict(result='PassedSyntheticControlsOnly', controls=len(checks), cases=checks, nativeExecuted=False)
(P / 'offline-controls.json').write_text(json.dumps(result, indent=2) + '\n')
print('Passed', len(checks), 'bounded intermediate/final/receipt controls; native validation pending.')
