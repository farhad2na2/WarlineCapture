"""Opt-in source-only original aircraft fuel safety owner and ordering."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[5]


def apply(arguments, output):
    source = ROOT / 'Assets/Game/Scripts/Systems/AircraftFuelSafetyReturnSystem.cs'
    control = ROOT / 'T Logs/SkirmishProduction/s004-aircraft-fuel-safety-control-20261105-T1'
    proof = json.loads((control / 'receipt.json').read_text())
    digest = hashlib.sha256(source.read_bytes()).hexdigest()
    marker = '[S004AircraftFuelSafetySourceControl] result=Passed cases=4 fixtureOnly=1 nativeParity=Unknown missionReady=0'
    if (proof.get('compile_exit') != 0 or proof.get('run_exit') != 0 or proof.get('pass_marker') is not True
            or proof.get('input_sha256_after', {}).get(str(source)) != digest
            or marker not in (control / 'run.log').read_text()):
        raise ValueError('Current original aircraft fuel owner control required')
    source_text = source.read_text()
    if '[UpdateInGroup(typeof(SimulationSystemGroup))]' not in source_text or '[UpdateBefore(typeof(UnitAirMovementSystem))]' not in source_text:
        raise ValueError('Original fuel safety owner ordering changed')

    def replace_file(suffix, name, replacements):
        matches = [i for i, value in enumerate(arguments) if value.endswith(suffix)]
        if len(matches) != 1: raise ValueError('Exact source fuel safety seam required: ' + suffix)
        text = Path(arguments[matches[0]]).read_text()
        for old, new in replacements:
            if text.count(old) != 1: raise ValueError('Changed source fuel safety seam: ' + old)
            text = text.replace(old, new)
        target = output / name
        target.write_text(text)
        arguments[matches[0]] = str(target)

    replace_file('/533_SourceOwnerOrderMetadata.cs', 'S004AirFuelSafetyOrderMetadata.cs', [
        ('namespace Game.Runtime.NativeOrderMetadata {', 'namespace Game.Runtime.NativeOrderMetadata {\n[Unity.Entities.UpdateInGroup(typeof(SimulationSystemGroup))]\n[Unity.Entities.UpdateBefore(typeof(UnitAirMovementSystem))]\ninternal struct AircraftFuelSafetyReturnSystem {}')])
    replace_file('/S004ScanRopeSourceOwnerOrder.cs', 'S004AirFuelSafetySourceOwnerOrder.cs', [
        ('"UnitManualMoveRetrySystem","UnitAirMovementSystem"', '"UnitManualMoveRetrySystem","AircraftFuelSafetyReturnSystem","UnitAirMovementSystem"'),
        ('{"s004_air",new[]{"UnitAirMovementSystem"}},', '{"s004_air_fuel_safety",new[]{"AircraftFuelSafetyReturnSystem"}},\n {"s004_air",new[]{"UnitAirMovementSystem"}},')])
    replace_file('/S004ScanRopeOwnerStep.cs', 'S004AirFuelSafetyOwnerStep.cs', [
        ('Phase("s004_air",()=>s004SupplementalTickOwners.Air());', 'Phase("s004_air_fuel_safety",()=>s004SupplementalTickOwners.AirFuelSafety());Phase("s004_air",()=>s004SupplementalTickOwners.Air());')])
    replace_file('/S004SupplementalTickOwners.cs', 'S004AirFuelSafetySupplementalOwners.cs', [
        ('private UnitAirMovementSystem air; private SystemState airState;', 'private UnitAirMovementSystem air; private SystemState airState;\n    private AircraftFuelSafetyReturnSystem airFuelSafety; private SystemState airFuelSafetyState;'),
        ('airState = State(); airState.Begin(); air.OnCreate(ref airState); airState.End();', 'airState = State(); airState.Begin(); air.OnCreate(ref airState); airState.End();\n        airFuelSafetyState = State(); airFuelSafetyState.Begin(); airFuelSafety.OnCreate(ref airFuelSafetyState); airFuelSafetyState.End();'),
        ('internal void Air() {', 'internal void AirFuelSafety() { airFuelSafetyState.Begin(); try { if (airFuelSafetyState.Enabled && airFuelSafetyState.RequiredQueriesPresent) airFuelSafety.OnUpdate(ref airFuelSafetyState); } finally { airFuelSafetyState.End(); } }\n    internal void Air() {')])
    defines = [i for i, value in enumerate(arguments) if value.startswith('-define:')]
    if len(defines) != 1: raise ValueError('One source define set required')
    arguments[defines[0]] += ',WARLINE_S004_AIR_FUEL_SAFETY'
    arguments.append(str(source))
