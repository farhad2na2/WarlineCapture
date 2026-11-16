#!/usr/bin/env python3
"""Assemble the independently audited November 6 S004 diagnostic inputs."""
import argparse
import json
from pathlib import Path

import build_s004_current_authoritative_epoch as previous

ROOT = previous.ROOT


def configure():
    previous.configure()
    merged = previous.previous
    roles, bundles, airports = merged.roles, merged.bundles, merged.airports
    evidence = ROOT / 'T Logs/SkirmishProduction/s003-current-metadata-refresh-20261106-T3'
    projection = ROOT / 'T Logs/SkirmishProduction/s003-current-factory-projection-20261106-T3'
    inputs = ROOT / 'T Logs/SkirmishProduction/s004-current-input-refresh-20261106-T1'
    roles.BUILD = ROOT / 'T Logs/SkirmishProduction/s004-current-role-refresh-build-20261106-T1/build.json'
    roles.PREFAB = ROOT / 'Logs/SkirmishProduction/s003-current-prefabs-refresh-20261106-r3/data/prefab-reference-data.json'
    roles.PREFAB_AUDIT = evidence / 'prefab-audit.json'
    roles.PROJECTION = projection / 'projection-receipt.json'
    roles.METADATA = projection / 'unit-production-metadata.json'
    bundles.S002 = roles.BUILD
    bundles.S026 = ROOT / 'T Logs/SkirmishProduction/s004-current-s026-build-20261106-T1/build.json'
    bundles.CURRENT_SETUP = ROOT / 'T Logs/SkirmishProduction/s004-current-setup-refresh-20261106-T1/compiled-setup.json'
    bundles.CURRENT_SETUP_RECEIPT = bundles.CURRENT_SETUP.parent / 'receipt.json'
    bundles.ROLE = evidence / 's004-role-catalog-r2.json'
    bundles.AIRPORT = evidence / 's004-airport-r2.json'
    airports.FACTORY = ROOT / 'Logs/SkirmishProduction/s004-current-factory-refresh-20261106-r1/data/full-factory-runway-definitions.json'
    airports.FACTORY_AUDIT = ROOT / 'T Logs/SkirmishProduction/s004-current-factory-refresh-20261106-T1/factory-audit.json'
    airports.S003 = projection / 'building-definitions.json'
    airports.PROJECTION, airports.ROLE = roles.PROJECTION, bundles.ROLE
    bundles.REPLACEMENTS = {
        'prefab_shared': roles.PREFAB,
        'prefab_s004': ROOT / 'Logs/SkirmishProduction/s004-current-input-prefabs-20261106-r1/data/prefab-reference-data.json',
        'building_definitions': airports.FACTORY,
        'configured_building_readmodel': ROOT / 'Logs/SkirmishProduction/s004-current-input-readmodel-20261106-r1/data/configured-spawnable-readmodel.json',
        'static_blockers': ROOT / 'Logs/SkirmishProduction/s004-current-input-blockers-20261106-r1/data/static-blockers.json',
    }
    bundles.AUDITS = {
        'prefab_shared': roles.PREFAB_AUDIT,
        'prefab_s004': inputs / 'prefabs-receipt.json',
        'building_definitions': airports.FACTORY_AUDIT,
        'configured_building_readmodel': inputs / 'readmodel-audit.json',
        'static_blockers': inputs / 'blockers-receipt.json',
    }


def build(output):
    configure()
    return previous.previous.bundles.build(Path(output).resolve())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(build(args.output)))


if __name__ == '__main__':
    main()
