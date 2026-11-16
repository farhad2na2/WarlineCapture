#!/usr/bin/env python3
"""Read-only identity preflight for checked-in expanded definitions; never admits execution."""
from pathlib import Path
import hashlib
import re

class SourceMissionPreflightError(ValueError):
    pass

def _read(path):
    data = path.read_bytes()
    return data.decode('utf-8'), hashlib.sha256(data).hexdigest()

def _scalar(text, field):
    matches = re.findall(r'^  ' + re.escape(field) + r': ([^\r\n]+)$', text, re.MULTILINE)
    if len(matches) != 1:
        raise SourceMissionPreflightError('Expected one ' + field)
    return matches[0].strip()

def _guid_ref(text, field):
    match = re.fullmatch(r'\{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}', _scalar(text, field))
    if not match:
        raise SourceMissionPreflightError('Missing authored reference: ' + field)
    return match.group(1)

def inspect_s003(repo):
    root = Path(repo).resolve(strict=True)
    files = {
        'definition': 'Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset',
        'objective': 'Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishObjective_BA.asset',
        'army': 'Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishArmy_AirMobile.asset',
        'start': 'Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishStart_Field.asset',
        'layout': 'Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishLayout_DB_BA.asset',
    }
    values = {}; pins = {}
    for key, relative in files.items():
        path = root / relative
        values[key], pins[relative] = _read(path)
        meta, pins[relative + '.meta'] = _read(Path(str(path) + '.meta'))
        guid = re.findall(r'^guid: ([0-9a-f]{32})$', meta, re.MULTILINE)
        if len(guid) != 1:
            raise SourceMissionPreflightError('Invalid asset GUID: ' + relative)
        values[key + '_guid'] = guid[0]
    definition = values['definition']
    expected = {'catalogId': 'S003', 'definitionId': 'skirmish.s003', 'scenarioSetupId': 'scenario.skirmish.s003', 'contentVersion': '2'}
    for field, value in expected.items():
        if _scalar(definition, field) != value:
            raise SourceMissionPreflightError('Definition identity mismatch: ' + field)
    for field, key in [('objectiveConfig', 'objective'), ('armyProfileConfig', 'army'), ('startPackageConfig', 'start'), ('mapLayout', 'layout')]:
        if _guid_ref(definition, field) != values[key + '_guid']:
            raise SourceMissionPreflightError('Definition reference mismatch: ' + field)
    if _scalar(values['objective'], 'objectiveId') != 'BA' or _scalar(values['army'], 'profileId') != 'A':
        raise SourceMissionPreflightError('Objective or army identity mismatch')
    return {'kind': 'CheckedInExpandedMissionIdentityPreflight', 'mission': 'S003', 'definition_id': 'skirmish.s003',
            'content_version': 2, 'objective': 'BA', 'army_profile': 'A', 'source_pins': pins,
            'execution_admitted': False, 'world_allocated': False, 'mission_ready': False,
            'pending': ['source-bound Campaign map and layout qualification', 'aircraft prefab and native source metadata',
                        'air movement, service infrastructure, and production owner qualification', 'source runtime execution profile']}
