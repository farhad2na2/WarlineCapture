#!/usr/bin/env python3
"""Run the finite source CLI protocol tests through its qualified raw bootstrap."""
from pathlib import Path
import argparse
import os
import runpy
import sys
import unittest

sys.dont_write_bytecode = True
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--repo');args=p.parse_args()
if args.repo:os.environ['SKIRMISH_SOURCE_REPO']=str(Path(args.repo).resolve(strict=True))
ci=Path(__file__).resolve().parent
runpy.run_path(str(ci/'run_skirmish_source_simulation.py'),run_name='qualified_test_bootstrap')
patterns={'test_skirmish_source_build.py':25,'test_skirmish_source_manifest.py':19,
          'test_skirmish_source_syntax.py':4,'test_skirmish_navigation_identity_execution.py':3,
          'test_skirmish_source_pin_batch.py':5,'test_skirmish_owned_source.py':18,'test_skirmish_source_authoring.py':8,'test_skirmish_source_smoke.py':30}
total=0
for pattern,expected in patterns.items():
    suite=unittest.defaultTestLoader.discover(str(ci/'tests'),pattern=pattern)
    result=unittest.TextTestRunner(verbosity=1).run(suite);total+=result.testsRun
    if not result.wasSuccessful()or result.testsRun!=expected:raise SystemExit(1)
print('[SkirmishSourceProtocolTests] result=Passed tests='+str(total))
