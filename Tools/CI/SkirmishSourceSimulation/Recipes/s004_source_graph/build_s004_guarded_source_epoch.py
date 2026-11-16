#!/usr/bin/env python3
"""Assemble the post-guard S004 source-only diagnostic input epoch."""
import argparse
import json
from pathlib import Path
import build_s004_merged_source_epoch as previous

ROOT=previous.ROOT

def configure():
    previous.configure()
    roles=previous.roles; bundles=previous.bundles; airports=previous.airports
    roles.BUILD=ROOT/'T Logs/SkirmishProduction/s002-current-merged-guard-build-20261105-T1/build.json'
    bundles.S002=roles.BUILD
    bundles.S026=ROOT/'T Logs/SkirmishProduction/s026-current-merged-guard-build-20261105-T1/build.json'
    bundles.CURRENT_SETUP=ROOT/'T Logs/SkirmishProduction/s004-current-merged-guard-setup-export-20261105-T1/compiled-setup.json'
    bundles.CURRENT_SETUP_RECEIPT=ROOT/'T Logs/SkirmishProduction/s004-current-merged-guard-setup-export-20261105-T1/receipt.json'
    bundles.ROLE=ROOT/'T Logs/SkirmishProduction/s004-current-guard-role-rebind-20261105-T2/role-catalog.json'
    bundles.AIRPORT=ROOT/'T Logs/SkirmishProduction/s004-current-guard-airport-rebind-20261105-T1/airport.json'
    airports.ROLE=bundles.ROLE

def build(output):
    configure()
    return previous.bundles.build(Path(output).resolve())

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',required=True,type=Path)
    print(json.dumps(build(parser.parse_args().output)))
