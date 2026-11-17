#!/usr/bin/env python3
"""Make ONE authored mission visible in (or hide it again from) the real Skirmish library, on a missions/* branch.

Authored missions stay Planned (hidden) in Assets/Game/Resources/SkirmishBattleCatalog.asset, so the real menu path
cannot launch them while they are under acceptance. The owner approved (internal only; nothing ships from missions/*
branches) showing a mission under acceptance in the real catalog. This tool edits exactly that mission's row, nothing else:

  Visible  Status: 1, PlayableScenarioIndex: 1000+N, DefinitionId: skirmish.sNNN, ContentVersion: <authored version>,
           ReadinessManifestId: validation.visible.skirmish.sNNN   (the marker that keeps it out of the published count,
           shows a VALIDATION CANDIDATE card, and lets SkirmishMenuJourney / the lane find it)
  Planned  Status: 0, PlayableScenarioIndex: -1, DefinitionId/ReadinessManifestId empty, ContentVersion: 0

It never publishes, never grants readiness, never touches another row, and refuses coded/legacy missions and any row that
is playable without the validation marker (a published row is never hidden). The lane commits the asset change so the
visibility commit is recorded. Prints one line: [SkirmishLibraryVisibility] catalog=<id> state=<..> result=Passed|Failed.
Environment fallbacks: WARLINE_SKIRMISH_CATALOG, WARLINE_LIBRARY_STATE.
"""
import argparse
import os
import re
import sys
from pathlib import Path

TAG = '[SkirmishLibraryVisibility]'
LIBRARY = Path('Assets/Game/Resources/SkirmishBattleCatalog.asset')
AUTHORED_CATALOG = Path('Assets/Game/Resources/SkirmishExpansionCatalog.asset')
SCENARIOS = Path('Assets/Game/Configs/SkirmishExpansion/Scenarios')
INDEX_BASE = 1000  # transport token; same base as SkirmishValidationLibraryPlan / SkirmishGenericMissionAvailabilityScope
MARKER_PREFIX = 'validation.visible.'  # SkirmishBattleCatalogEntry.ValidationVisiblePrefix
# Coded/legacy-index missions keep their own binding (SkirmishMissionDispatch.TryGetLegacyIndex / IsCodedExpanded).
PROTECTED = {'S001', 'S002', 'S003', 'S004', 'S005', 'S025', 'S073'}
ROW_START = re.compile(r'^  - ScenarioId: (\S+)\s*$')
FIELD = re.compile(r'^    ([A-Za-z]+): ?(.*?)\r?$')
EDITED = ('Status', 'PlayableScenarioIndex', 'DefinitionId', 'ContentVersion', 'ReadinessManifestId')


class Refused(Exception):
    pass


def canonical(catalog):
    return bool(re.fullmatch(r'S(\d{3})', catalog or '')) and 1 <= int(catalog[1:]) <= 120


def split_rows(lines):
    """Return {id: (start, end)} line ranges (end exclusive) of every library row, plus the list of duplicate ids."""
    starts = [(i, ROW_START.match(line).group(1)) for i, line in enumerate(lines) if ROW_START.match(line)]
    rows, duplicates = {}, []
    for n, (start, catalog) in enumerate(starts):
        end = start + 1
        while end < len(lines) and lines[end].startswith('    '):
            end += 1
        if catalog in rows:
            duplicates.append(catalog)
        rows[catalog] = (start, end)
    return rows, duplicates


def read_row(lines, start, end):
    fields = {}
    for i in range(start + 1, end):
        match = FIELD.match(lines[i])
        if match and match.group(1) in EDITED:
            fields[match.group(1)] = (i, match.group(2))
    missing = [name for name in EDITED if name not in fields]
    if missing:
        raise Refused('rowFieldsMissing:' + ','.join(missing))
    return fields


def guid_of(meta_text):
    match = re.search(r'^guid: ([0-9a-f]{32})\s*$', meta_text, re.MULTILINE)
    return match.group(1) if match else None


def authored_definition(repo, catalog):
    """Read the authored definition the library row must point at; refuse anything unbound or unregistered."""
    path = repo / SCENARIOS / catalog / f'SkirmishScenario_{catalog}.asset'
    if not path.is_file():
        raise Refused('authoredDefinitionMissing:' + str(path.relative_to(repo)))
    text = path.read_text(encoding='utf-8')

    def scalar(name):
        match = re.search(r'^  ' + name + r': ?(.*?)\s*$', text, re.MULTILINE)
        return match.group(1) if match else ''

    definition_id, version = scalar('definitionId'), scalar('contentVersion')
    if scalar('catalogId') != catalog:
        raise Refused('definitionCatalogIdMismatch:' + scalar('catalogId'))
    if definition_id != 'skirmish.s' + catalog[1:]:
        raise Refused('definitionIdMismatch:' + definition_id)
    if not version.isdigit() or int(version) <= 0:
        raise Refused('contentVersionNotPositive:' + version)
    for field in ('campaignMap', 'mapLayout'):
        if not re.search(r'^  ' + field + r': \{fileID: \d+, guid: [0-9a-f]{32}, type: \d\}', text, re.MULTILINE):
            raise Refused('mapOrLayoutNotBound:' + field)
    meta = Path(str(path) + '.meta')
    guid = guid_of(meta.read_text(encoding='utf-8')) if meta.is_file() else None
    registry = repo / AUTHORED_CATALOG
    if not guid or not registry.is_file() or guid not in registry.read_text(encoding='utf-8'):
        raise Refused('definitionNotRegisteredInExpansionCatalog')
    return definition_id, int(version)


def plan_edit(repo, lines, catalog, state):
    """Return (new_lines, details). Raises Refused. Pure over the given lines plus read-only authored assets."""
    if not canonical(catalog):
        raise Refused('catalogNotCanonical')
    if state not in ('Visible', 'Planned'):
        raise Refused('stateMustBeVisibleOrPlanned')
    if catalog in PROTECTED:
        raise Refused('codedOrLegacyMissionKeepsItsBinding')
    rows, duplicates = split_rows(lines)
    if catalog in duplicates:
        raise Refused('duplicateLibraryRow')
    if catalog not in rows:
        raise Refused('libraryRowMissing')
    start, end = rows[catalog]
    fields = read_row(lines, start, end)
    status, index = fields['Status'][1], fields['PlayableScenarioIndex'][1]
    marker = fields['ReadinessManifestId'][1]
    playable = status == '1' and index.lstrip('-').isdigit() and int(index) >= 0
    validation_visible = playable and marker.startswith(MARKER_PREFIX)
    if state == 'Visible':
        definition_id, version = authored_definition(repo, catalog)
        wanted = {'Status': '1', 'PlayableScenarioIndex': str(INDEX_BASE + int(catalog[1:])), 'DefinitionId': definition_id,
                  'ContentVersion': str(version), 'ReadinessManifestId': MARKER_PREFIX + definition_id}
        if playable and not validation_visible:
            raise Refused('rowAlreadyPlayableAndNotValidationVisible')
        if not playable:
            if status != '0' or index != '-1':
                raise Refused('plannedRowMalformed:status=' + status + ',index=' + index)
            for taken in rows:
                if taken == catalog:
                    continue
                other = read_row(lines, *rows[taken])
                if other['Status'][1] == '1' and other['PlayableScenarioIndex'][1] == wanted['PlayableScenarioIndex']:
                    raise Refused('transportIndexTakenBy:' + taken)
    else:
        if playable and not validation_visible:
            raise Refused('refusingToHideAPublishedRow')
        wanted = {'Status': '0', 'PlayableScenarioIndex': '-1', 'DefinitionId': '', 'ContentVersion': '0', 'ReadinessManifestId': ''}
    new_lines = list(lines)
    changed = []
    for name, value in wanted.items():
        line_number, current = fields[name]
        if current != value:
            ending = '\r\n' if lines[line_number].endswith('\r\n') else '\n' if lines[line_number].endswith('\n') else ''
            new_lines[line_number] = '    ' + name + ': ' + value + ending  # Unity writes an empty value as "Key: "
            changed.append(name)
    # Whole-file guard: only lines inside this row, only the five fields.
    differing = [i for i, (a, b) in enumerate(zip(lines, new_lines)) if a != b]
    if len(new_lines) != len(lines) or any(not (start < i < end) for i in differing) or len(differing) > len(EDITED):
        raise Refused('editWouldTouchSomethingElse')
    return new_lines, {'changed': changed, 'wanted': wanted}


def run(repo, catalog, state, dry_run=False):
    path = repo / LIBRARY
    if not path.is_file():
        raise Refused('libraryAssetMissing:' + str(LIBRARY))
    with path.open(encoding='utf-8', newline='') as stream:
        lines = stream.read().splitlines(keepends=True)
    new_lines, details = plan_edit(repo, lines, catalog, state)
    if details['changed'] and not dry_run:
        temp = path.with_name(path.name + '.tmp')
        with temp.open('w', encoding='utf-8', newline='') as stream:
            stream.write(''.join(new_lines))
        os.replace(temp, path)
        with path.open(encoding='utf-8', newline='') as stream:  # read back: the row must now say exactly what was asked
            again = stream.read().splitlines(keepends=True)
        start, end = split_rows(again)[0][catalog]
        for name, value in details['wanted'].items():
            if read_row(again, start, end)[name][1] != value:
                raise Refused('readBackMismatch:' + name)
    return details


def format_line(catalog, state, result, **fields):
    extra = ''.join(f' {key}={value}' for key, value in fields.items())
    return f'{TAG} catalog={catalog} state={state} result={result}{extra}'


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--catalog', default=os.environ.get('WARLINE_SKIRMISH_CATALOG'))
    parser.add_argument('--state', default=os.environ.get('WARLINE_LIBRARY_STATE'), choices=('Visible', 'Planned'))
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--dry-run', action='store_true', help='check and report; write nothing')
    args = parser.parse_args(argv)
    catalog, state = args.catalog or '?', args.state or '?'
    try:
        if not args.catalog or not args.state:
            raise Refused('catalogAndStateRequired')
        details = run(args.repo.resolve(), args.catalog, args.state, args.dry_run)
    except (Refused, OSError, UnicodeDecodeError) as error:
        print(format_line(catalog, state, 'Failed', reason=error))
        return 2
    wanted = details['wanted']
    print(format_line(catalog, state, 'Passed', changed=len(details['changed']),
                      touched=','.join(details['changed']) or 'none', index=wanted['PlayableScenarioIndex'],
                      definition=wanted['DefinitionId'] or 'none', version=wanted['ContentVersion'],
                      manifest=wanted['ReadinessManifestId'] or 'none', asset=LIBRARY, dryRun=int(args.dry_run),
                      published=0, readinessGranted=0))
    return 0


if __name__ == '__main__':
    sys.exit(main())
