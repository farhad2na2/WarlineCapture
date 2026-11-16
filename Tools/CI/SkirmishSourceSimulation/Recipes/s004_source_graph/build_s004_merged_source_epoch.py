#!/usr/bin/env python3
"""Assemble the explicitly captured S004 merged-source diagnostic epoch."""
import argparse
import json
from pathlib import Path

import build_s004_current_epoch_bundle as bundles
import build_s004_current_role_catalog_metadata as roles
import build_s004_current_airport_source_definition as airports

ROOT=roles.ROOT


def configure():
    roles.BUILD=ROOT/'T Logs/SkirmishProduction/s002-refreshed-merged-source-build-20261105-T4/build.json'
    roles.PREFAB=ROOT/'Logs/SkirmishProduction/s003-shared-rifle-prefabs-capture-20261105-T1/data/prefab-reference-data.json'
    roles.PREFAB_AUDIT=ROOT/'T Logs/SkirmishProduction/s003-shared-rifle-prefabs-capture-20261105-T1/independent-audit.json'
    roles.PROJECTION=ROOT/'T Logs/SkirmishProduction/s003-shared-rifle-factory-projection-20261105-T1/projection-receipt.json'
    roles.METADATA=roles.PROJECTION.parent/'unit-production-metadata.json'
    bundles.S002=roles.BUILD
    bundles.S026=ROOT/'T Logs/SkirmishProduction/s026-merged-source-host-build-20261105-T1/build.json'
    bundles.CURRENT_SETUP=ROOT/'T Logs/SkirmishProduction/s004-merged-standard-setup-export-20261105-T1/compiled-setup.json'
    bundles.CURRENT_SETUP_RECEIPT=ROOT/'T Logs/SkirmishProduction/s004-merged-standard-setup-export-20261105-T1/receipt.json'
    bundles.ROLE=ROOT/'T Logs/SkirmishProduction/s004-current-role-catalog-source-20261105-T2.json'
    bundles.AIRPORT=ROOT/'T Logs/SkirmishProduction/s004-current-merged-airport-source-20261105-T1.json'
    airports.FACTORY=ROOT/'Logs/SkirmishProduction/s004-current-merged-factory-20261105-T2/full-factory-runway-definitions.json'
    airports.FACTORY_AUDIT=ROOT/'T Logs/SkirmishProduction/s004-current-merged-factory-20261105-T2/factory-audit.json'
    airports.S003=roles.PROJECTION.parent/'building-definitions.json'
    airports.PROJECTION=roles.PROJECTION
    airports.ROLE=bundles.ROLE
    bundles.REPLACEMENTS={
        'prefab_shared':roles.PREFAB,
        'prefab_s004':ROOT/'T Logs/SkirmishProduction/s004-merged-capture-links-20261105-T1/prefabs/data/prefab-reference-data.json',
        'building_definitions':airports.FACTORY,
        'configured_building_readmodel':ROOT/'Logs/SkirmishProduction/s004-current-merged-readmodel-20261105-T1/configured-spawnable-readmodel.json',
        'static_blockers':ROOT/'T Logs/SkirmishProduction/s004-merged-capture-links-20261105-T1/blockers/data/static-blockers.json',
    }
    bundles.AUDITS={
        'prefab_shared':roles.PREFAB_AUDIT,
        'prefab_s004':ROOT/'T Logs/SkirmishProduction/s004-merged-capture-links-20261105-T1/prefabs/receipt.json',
        'building_definitions':airports.FACTORY_AUDIT,
        'configured_building_readmodel':ROOT/'T Logs/SkirmishProduction/s004-current-merged-readmodel-20261105-T1/independent-audit.json',
        'static_blockers':ROOT/'T Logs/SkirmishProduction/s004-merged-capture-links-20261105-T1/blockers/receipt.json',
    }


def build(output):
    configure()
    return bundles.build(Path(output).resolve())


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',required=True,type=Path)
    print(json.dumps(build(parser.parse_args().output)))
