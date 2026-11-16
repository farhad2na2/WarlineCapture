#!/usr/bin/env python3
"""Build a source-only S004 metadata candidate; this is not runtime admission."""
import argparse
import hashlib
import json
import struct
from pathlib import Path
import audit_s004_unit_metadata_coverage as coverage
import read_s004_unit_scalar_inputs as scalar_inputs
import read_s004_catalog_source_inputs as catalog_inputs
import run_s004_catalog_getter_control as getter_control

ROOT = Path(__file__).resolve().parents[5]
COVERAGE = ROOT / "T Logs/SkirmishProduction/s004-unit-metadata-coverage-20261104-T1.json"
INPUT = ROOT / "T Logs/SkirmishProduction/s004-unit-scalar-inputs-20261104-T3.json"
PROJECTION = ROOT / "T Logs/SkirmishProduction/s004-unit-scalar-policy-20261104-T3/projection.json"
RECEIPT = PROJECTION.parent / "receipt.json"
GETTER_INPUT = ROOT / "T Logs/SkirmishProduction/s004-catalog-getter-source-inputs-20261104-T4.json"
GETTER_DIR = ROOT / "T Logs/SkirmishProduction/s004-catalog-getter-control-20261104-T1"
GETTER_PROJECTION = GETTER_DIR / "projection.json"
GETTER_RECEIPT = GETTER_DIR / "receipt.json"

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def number(value): return {"codec":"integer-v1", "type":"System.Int32", "value":str(value)}
def boolean(value): return {"codec":"bool-v1", "value":value}
def text(value): return {"codec":"utf16-v1", "value":value}
def f32(value): return {"codec":"f32-bits-v1", "bits":struct.pack(">f",value).hex()}
def footprint(size):
    return {"codec":"struct-fields-v1", "type":"UnityEngine.Vector2Int", "fields":{
        "UnityEngine.Vector2Int::m_X":number(size[0]),"UnityEngine.Vector2Int::m_Y":number(size[1])}}

def build():
    paths = [COVERAGE, INPUT, PROJECTION, RECEIPT, GETTER_INPUT, GETTER_PROJECTION, GETTER_RECEIPT,
             GETTER_DIR / 'S004OriginalGetterProjection.cs', GETTER_DIR / 'compile.log', GETTER_DIR / 'run.log',
             getter_control.HARNESS, Path(getter_control.__file__).resolve(), *(ROOT / p for p in coverage.INPUTS.values()), Path(__file__).resolve()]
    hashes = {str(p):sha(p) for p in paths}
    audit = json.loads(COVERAGE.read_bytes())
    if coverage.audit() != audit: raise ValueError("Current S004 identity coverage must match retained audit")
    source = json.loads(INPUT.read_bytes())
    if scalar_inputs.read() != source: raise ValueError("Current serialized scalar inputs changed")
    projection = json.loads(PROJECTION.read_bytes()); receipt = json.loads(RECEIPT.read_bytes())
    if receipt.get("status") != "PassedSourceOnlyScalarPolicy" or receipt.get("input_sha256") != sha(INPUT) or receipt.get("projection_sha256") != sha(PROJECTION) or receipt.get("native_prefab_resolution") is not False:
        raise ValueError("Exact source scalar policy receipt required")
    if projection.get("kind") != "S004OriginalUnitScalarPolicyProjection" or any(projection.get(flag) is not False for flag in ("native_parity","runtime_admission","mission_ready")):
        raise ValueError("Exact unqualified scalar projection required")
    candidates = {row["key"]:row for row in projection["rows"]}
    if len(candidates) != 3 or set(candidates) != set(audit["missing_metadata_keys"]): raise ValueError("Exact missing scalar candidate domain required")
    getter_input = json.loads(GETTER_INPUT.read_bytes())
    if catalog_inputs.read() != getter_input: raise ValueError("Current serialized catalog inputs changed")
    getter_projection = json.loads(GETTER_PROJECTION.read_bytes()); getter_receipt = json.loads(GETTER_RECEIPT.read_bytes())
    if getter_receipt.get('status') != 'PassedSourceOnlyOriginalGetterProjection' or getter_receipt.get('input_sha256') != sha(GETTER_INPUT) or getter_receipt.get('projection_sha256') != sha(GETTER_PROJECTION) or any(getter_receipt.get(flag) is not False for flag in ('native_prefab_resolution','runtime_admission','mission_ready')):
        raise ValueError('Exact unqualified getter projection receipt required')
    if (GETTER_DIR / 'S004OriginalGetterProjection.cs').read_text() != getter_control.generate(getter_control.AUTHORING.read_text()) or getter_control.MARKER not in (GETTER_DIR / 'run.log').read_text():
        raise ValueError('Current original getter bodies and actual control marker required')
    getter_rows = {row['key']: row for row in getter_projection['rows']}
    if len(getter_rows) != 3 or set(getter_rows) != set(candidates): raise ValueError('Exact source getter unit domain required')
    retained = {}
    data = {}
    for name in ("s003","s002"):
        data[name] = json.loads((ROOT/coverage.INPUTS[name]).read_bytes())
        for row in data[name]["units"]:
            key = row["key"]
            if not isinstance(row.get("authoring_catalog_properties"), dict):
                raise ValueError("Retained original catalog authoring data required")
            if key in retained and retained[key][0]["authoring_catalog_properties"] != row["authoring_catalog_properties"]:
                raise ValueError("Retained catalog authoring captures disagree: " + key)
            if key not in retained: retained[key] = (row,name)
    if data["s003"]["transports"] != data["s002"]["transports"]: raise ValueError("Retained transport facts differ")
    identities = {}
    for row in audit["slots"]:
        old = identities.setdefault(row["key"],row["prefab_identity"])
        if old != row["prefab_identity"]: raise ValueError("Conflicting actual factory identity")
    rows = []
    # This order is deterministic source-slot order, not a claimed Unity registry order.
    for slot in audit["slots"]:
        key = slot["key"]
        if any(row["key"] == key for row in rows): continue
        if key in retained:
            row,name = retained[key]
            if coverage.identity(row["prefab"]) != coverage.identity(identities[key]): raise ValueError("Retained prefab identity mismatch")
            definition = row["definition_metadata"]; production = row["production_metadata"]
            catalog_authoring = row["authoring_catalog_properties"]
            provenance = {"kind":"RetainedOriginalFactoryMetadata", "capture":name, "source_index":row["index"]}
        else:
            candidate = candidates[key]; d = candidate["definition_projection"]; size = candidate["footprint"]
            getter = getter_rows[key]
            if getter['IsAirUnit'] != candidate['is_air_unit']: raise ValueError('Original scalar/getter air projection differs: '+key)
            def source_sprite(name):
                ref = getter[name]
                return {'codec':'source-sprite-reference-v1','type':'UnityEngine.Sprite','path':ref['source_asset_path'],
                        'guid':ref['guid'],'local_id':ref['local_id']}
            catalog_fields = {'ConfiguredDisplayName':text(d['display_name']),'ConfiguredDescription':text(d['description']),
                'CanRequest':boolean(d['can_request']),'MaterialsCost':number(d['materials_cost']),'Price':number(d['credits_cost']),
                'ProductionDurationSeconds':f32(candidate['production_duration_seconds']),
                'PortraitSprite':source_sprite('portraitSprite'),'PortraitCardSprite':source_sprite('portraitCardSprite'),
                'PortraitActionSprite':source_sprite('portraitActionSprite')}
            for name in ('IsAirUnit','IsProductionTransportUnit','ConfiguredAllowIdleWander','ConfiguredCanAttack'):
                catalog_fields[name] = boolean(getter[name])
            for name in ('SoldierTransportCapacity','ConfiguredResourceHaulerBarrelCapacity','ConfiguredAttackDamage','ConfiguredMaxHealth'):
                catalog_fields[name] = number(getter[name])
            for name in ('ConfiguredAttackRange','ConfiguredSpeed'):
                catalog_fields[name] = {'codec':'f32-bits-v1','bits':getter[name+'Bits']}
            catalog_authoring = {'source_type':'Game.Authoring.UnitGridAuthoring','fields':catalog_fields,
                'runtime_catalog_admission_and_role_overlay_applied':False,
                'scope':'Source-only getter projection; sprite names and Unity object resolution unobserved'}
            if candidate["serialized_transport_null"] is not True: raise ValueError("Exact null serialized transport required")
            definition = {"type":"Game.Runtime.BuildingDefinitionPrefabSystemHelper+UnitDefinitionMetadata", "fields":{
                "DisplayName":text(d["display_name"]),"Description":text(d["description"]),"FootprintCells":footprint(size),
                "CanRequest":boolean(d["can_request"]),"Price":number(d["materials_cost"]),"CreditsCost":number(d["credits_cost"])}}
            production = {"type":"Game.Runtime.BuildingProductionQueueCompositionSystemHelper+UnitProductionMetadata", "fields":{
                "ProductionDurationSeconds":f32(candidate["production_duration_seconds"]),"ProductionTransportPrefab":{"codec":"null-v1"},
                "IsAirUnit":boolean(candidate["is_air_unit"]),"ProductionTransportArrivalSeconds":f32(candidate["transport_arrival_seconds"]),
                "ProductionTransportHoldForNextReadySeconds":f32(candidate["transport_hold_seconds"]),"ProductionTransportMaxConcurrent":number(candidate["transport_max_concurrent"]),
                "ProductionTransportRequiresAirportRunway":boolean(candidate["requires_runway"]),"ProductionTransportUsesRunwayLanding":boolean(candidate["uses_runway_landing"]),"FootprintCells":footprint(size)}}
            provenance = {"kind":"SerializedSourceAndOriginalScalarPolicies", "native_getters_observed":False, "input_sha256":sha(INPUT),"projection_sha256":sha(PROJECTION),
                          'catalog_getter_input_sha256':sha(GETTER_INPUT),'catalog_getter_projection_sha256':sha(GETTER_PROJECTION),
                          'sprite_names_observed':False}
        rows.append({"index":len(rows),"key":key,"prefab":identities[key],"has_definition_metadata":True,"has_production_metadata":True,
                     "definition_metadata":definition,"production_metadata":production,"authoring_catalog_properties":catalog_authoring,"source_provenance":provenance})
    if len(rows)!=20 or {x["key"] for x in rows} != set(audit["retained_metadata_keys"]+audit["missing_metadata_keys"]): raise ValueError("Exact S004 production key domain required")
    if {str(p):sha(p) for p in paths} != hashes: raise ValueError("Candidate input changed during join")
    metadata_sources = ("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
        "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs",
        "Assets/Game/Scripts/Composition/BuildingDefinitionUnitAuthoringMetadata.cs",
        "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs")
    source_pins = {name:sha(ROOT/name) for name in metadata_sources}
    return {"schema_version":1,"kind":"S004SourceDerivedUnitMetadataCandidate","authority":"RetainedFactoryAndSerializedSourcePolicies_DiagnosticOnly",
            "catalog_id":"S004","setup_hash":1130726651,"seed":104733,"order_scope":"First occurrence in captured S004 production slots; not native registry order",
            "source_inputs":hashes,"source_pins":source_pins,"units":rows,"transports":data["s003"]["transports"],
            "unsupported_values":[],"unsupported_values_scope":"Candidate metadata fields only; native prefab resolution remains unverified",
            "baked_actor_footprint_substituted":False,"runtime_camera_basis_exported":False,
            "ui_catalog_runtime_policy_sampled":False,
            "catalog_authoring_incomplete_keys":[row["key"] for row in rows if row["authoring_catalog_properties"] is None],
            "source_only_sprite_name_unobserved_keys":[row["key"] for row in rows if row["source_provenance"]["kind"] == "SerializedSourceAndOriginalScalarPolicies"],
            "native_prefab_resolution":False,"runtime_admission":False,"mission_ready":False}

def main():
    p=argparse.ArgumentParser();p.add_argument("--output",type=Path,required=True);a=p.parse_args();result=build()
    with a.output.open("x") as f:json.dump(result,f,indent=2);f.write("\n")
    print(json.dumps({"kind":result["kind"],"rows":len(result["units"]),"runtime_admission":False}))
if __name__=="__main__":main()
