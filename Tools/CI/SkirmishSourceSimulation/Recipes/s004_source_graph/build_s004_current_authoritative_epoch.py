#!/usr/bin/env python3
"""Assemble the current S004 source-data epoch; no gameplay admission."""
import argparse
import json
from pathlib import Path
import build_s004_merged_source_epoch as previous

ROOT=previous.ROOT

def configure():
    previous.configure()
    roles=previous.roles;bundles=previous.bundles;airports=previous.airports
    roles.BUILD=ROOT/'T Logs/SkirmishProduction/s004-unqualified-source-build-20261105-T1/build.json'
    roles.PREFAB=ROOT/'Logs/SkirmishProduction/s004-current-shared-prefabs-20261105-r1/data/prefab-reference-data.json'
    roles.PREFAB_AUDIT=ROOT/'T Logs/SkirmishProduction/s004-current-shared-prefabs-20261105-T1/independent-audit.json'
    roles.PROJECTION=ROOT/'T Logs/SkirmishProduction/s004-current-shared-factory-projection-20261105-T1/projection-receipt.json'
    roles.METADATA=roles.PROJECTION.parent/'unit-production-metadata.json'
    bundles.S002=roles.BUILD
    bundles.S026=ROOT/'T Logs/SkirmishProduction/s026-four-difficulty-reader-build-20261105-T1/build.json'
    bundles.CURRENT_SETUP=ROOT/'T Logs/SkirmishProduction/s004-current-scope-setup-export-20261105-T1/compiled-setup.json'
    bundles.CURRENT_SETUP_RECEIPT=bundles.CURRENT_SETUP.parent/'receipt.json'
    bundles.ROLE=ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-role-catalog-20261105-T1.json'
    bundles.AIRPORT=ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-airport-definition-20261105-T1.json'
    airports.FACTORY=ROOT/'Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-r1/data/full-factory-runway-definitions.json'
    airports.FACTORY_AUDIT=ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-T1/factory-audit.json'
    airports.S003=roles.PROJECTION.parent/'building-definitions.json'
    airports.PROJECTION=roles.PROJECTION;airports.ROLE=bundles.ROLE
    bundles.REPLACEMENTS={
        'prefab_shared':roles.PREFAB,
        'prefab_s004':ROOT/'Logs/SkirmishProduction/s004-current-authoritative-prefabs-20261105-r1/data/prefab-reference-data.json',
        'building_definitions':airports.FACTORY,
        'configured_building_readmodel':ROOT/'Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-r3/data/configured-spawnable-readmodel.json',
        'static_blockers':ROOT/'Logs/SkirmishProduction/s004-current-authoritative-blockers-20261105-r1/data/static-blockers.json'}
    bundles.AUDITS={
        'prefab_shared':roles.PREFAB_AUDIT,
        'prefab_s004':ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-prefabs-20261105-T1/receipt.json',
        'building_definitions':airports.FACTORY_AUDIT,
        'configured_building_readmodel':ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-T3/readmodel-audit.json',
        'static_blockers':ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-blockers-20261105-T1/receipt.json'}

def build(output):
    configure();return previous.bundles.build(Path(output).resolve())

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True)
    print(json.dumps(build(parser.parse_args().output)))
