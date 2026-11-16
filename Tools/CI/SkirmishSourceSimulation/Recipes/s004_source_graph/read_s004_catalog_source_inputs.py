#!/usr/bin/env python3
"""Read source inputs for missing S004 catalog rows; no Unity getter claim."""
import argparse
import hashlib
import json
import math
import re
import subprocess
from pathlib import Path

import read_s004_unit_scalar_inputs as scalar

ROOT = scalar.ROOT
FIELDS = ('allowIdleWander', 'speed', 'resourceHaulerBarrelCapacity', 'canAttack',
          'attackRange', 'attackDamage', 'maxHealth', 'soldierTransportCapacity',
          'isProductionTransportUnit')
SPRITES = ('portraitSprite', 'portraitCardSprite', 'portraitActionSprite')
BOOLS = {'allowIdleWander', 'canAttack', 'isProductionTransportUnit'}
INTS = {'resourceHaulerBarrelCapacity', 'attackDamage', 'maxHealth', 'soldierTransportCapacity'}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def validate_scalar(name, value):
    if name in BOOLS and value not in ('0', '1'):
        raise ValueError('Exact serialized boolean required: ' + name)
    if name in INTS and (not value.isascii() or not value.isdecimal()):
        raise ValueError('Nonnegative serialized integer required: ' + name)
    if name in ('speed', 'attackRange'):
        parsed = float(value)
        if not math.isfinite(parsed) or parsed < 0:
            raise ValueError('Finite nonnegative serialized float required: ' + name)


def sprite_reference(reference):
    match = re.fullmatch(r'\{fileID: (\d+), guid: ([0-9a-f]{32}), type: 3\}', reference)
    if not match or int(match.group(1)) != 21300000:
        raise ValueError('Exact serialized single-sprite reference required')
    return {'guid': match.group(2), 'local_id': int(match.group(1))}


def read():
    scalar_inputs = scalar.read()  # Recheck the exact prefab/config inheritance first.
    source = (ROOT / scalar.AUTHORING).read_text(encoding='utf-8-sig')
    required = ('public bool IsProductionTransportUnit => isProductionTransportUnit;',
                'public int SoldierTransportCapacity => Mathf.Max(0, config != null ? config.SoldierTransportCapacity : soldierTransportCapacity);',
                'public bool ConfiguredAllowIdleWander => config != null ? config.AllowIdleWander : allowIdleWander;',
                'public float ConfiguredSpeed => Mathf.Max(0f, config != null ? config.Speed : speed);',
                'public int ConfiguredResourceHaulerBarrelCapacity => Mathf.Max(0, config != null ? config.ResourceHaulerBarrelCapacity : resourceHaulerBarrelCapacity);',
                'public bool ConfiguredCanAttack => config != null ? config.CanAttack : canAttack;',
                'public float ConfiguredAttackRange => Mathf.Max(0f, config != null ? config.AttackRange : attackRange);',
                'public int ConfiguredAttackDamage => Mathf.Max(0, config != null ? config.AttackDamage : attackDamage);',
                'public int ConfiguredMaxHealth => Mathf.Max(1, config != null ? config.MaxHealth : maxHealth);')
    for expression in required:
        if source.count(expression) != 1:
            raise ValueError('Exact original catalog getter required: ' + expression)
    for name in ('PortraitSprite', 'PortraitCardSprite', 'PortraitActionSprite'):
        expression = f'public Sprite {name} => config != null ? config.{name} : {name[0].lower() + name[1:]};'
        if source.count(expression) != 1:
            raise ValueError('Exact original portrait getter required: ' + name)
    base = (ROOT / scalar.BASE).read_text(encoding='utf-8-sig')
    block = re.search(r'(?ms)^--- !u!114 &106347591433935587\n(.*?)(?=^--- !u!|\Z)', base)
    if block is None:
        raise ValueError('Original inherited authoring block required')
    vehicle = (ROOT / scalar.VEHICLE).read_text(encoding='utf-8-sig')
    if re.search(r'(?m)^\s*propertyPath: isProductionTransportUnit\s*$', vehicle):
        raise ValueError('Intermediate production transport override requires resolution')
    pins = dict(scalar_inputs['source_pins'])
    pins[str(Path(__file__).resolve())] = digest(Path(__file__).read_bytes())
    rows = []
    guids = set()
    for row in scalar_inputs['rows']:
        cfg = (ROOT / row['config']).read_text(encoding='utf-8-sig')
        prefab = (ROOT / row['prefab']).read_text(encoding='utf-8-sig')
        values = {}
        for name in FIELDS:
            value = scalar.field(cfg, name)
            validate_scalar(name, value)
            values[name] = value
        # This getter reads the component field, not the referenced config.
        matches = re.findall(r'- target: \{fileID: 391336776749024018, guid: 9ce6998c034284d98a89b5badc6e2f24, type: 3\}\s+propertyPath: isProductionTransportUnit\s+value: ?([^\r\n]*)\s+objectReference: [^\r\n]+', prefab)
        if len(matches) > 1:
            raise ValueError('Ambiguous inherited production transport override')
        component_value = matches[0].strip() if matches else scalar.field(block.group(1), 'isProductionTransportUnit')
        if component_value not in ('0', '1'):
            raise ValueError('Exact component production transport flag required')
        sprites = {}
        for name in SPRITES:
            reference = scalar.field(cfg, name)
            sprites[name] = sprite_reference(reference)
            guids.add(sprites[name]['guid'])
        rows.append({'key': row['key'], 'config': row['config'], 'serialized_values': values, 'scalar_source': row['scalars'],
                     'component_is_production_transport_unit': component_value, 'sprite_references': sprites})
    if len(guids) != 9:
        raise ValueError('Nine distinct source sprite identities required')
    expression = r'^guid: (' + '|'.join(sorted(guids)) + r')$'
    found = subprocess.run(['rg', '-uu', '-l', '-g', '*.meta', expression, 'Assets'], cwd=ROOT,
                           text=True, capture_output=True, check=True).stdout.splitlines()
    resolved = {}
    for relative in found:
        meta = ROOT / relative
        data = meta.read_bytes()
        guid = scalar.guid(data.decode('utf-8-sig'))
        if guid not in guids or guid in resolved:
            raise ValueError('Ambiguous source sprite GUID: ' + guid)
        text = data.decode('utf-8-sig')
        if len(re.findall(r'(?m)^  spriteMode: 1\s*$', text)) != 1:
            raise ValueError('Single sprite importer required: ' + relative)
        asset = Path(str(meta)[:-5])
        asset_bytes = asset.read_bytes()
        pins[str(meta)] = digest(data)
        pins[str(asset)] = digest(asset_bytes)
        resolved[guid] = str(asset.relative_to(ROOT))
    if set(resolved) != guids:
        raise ValueError('Incomplete sprite metadata identity domain')
    for row in rows:
        for ref in row['sprite_references'].values():
            ref['source_asset_path'] = resolved[ref['guid']]
    if any(digest(Path(path).read_bytes()) != expected for path, expected in pins.items()):
        raise ValueError('Source changed during catalog input read')
    return {'kind': 'S004SerializedCatalogGetterInputs', 'status': 'CollectedSourceOnly',
            'rows': rows, 'source_pins': pins,
            'scope': 'Serialized inputs and unique sprite source references only; no Unity getter, sprite-name or runtime catalog claim',
            'native_prefab_resolution': False, 'runtime_admission': False, 'mission_ready': False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = read()
    with args.output.open('x') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps({'status': result['status'], 'rows': len(result['rows']), 'source_pins': len(result['source_pins'])}))


if __name__ == '__main__':
    main()
