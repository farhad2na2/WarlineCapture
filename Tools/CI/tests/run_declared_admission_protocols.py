from pathlib import Path
import argparse,runpy,sys,unittest
p=argparse.ArgumentParser();p.add_argument('--ci-root',required=True);p.add_argument('--mode',choices=['header','rotation'],required=True);p.add_argument('--reference-set');a=p.parse_args();root=Path(a.ci_root).resolve()
# Execute the EXISTING direct-entry exact-byte bootstrap; __main__ CLI dispatch does not run.
runpy.run_path(str(root/'run_skirmish_source_simulation.py'),run_name='qualified_test_bootstrap')
if a.mode=='header':
 suite=unittest.defaultTestLoader.discover(str(root/'tests'),pattern='test_derived_projection_header_precheck.py');result=unittest.TextTestRunner(verbosity=2).run(suite)
 if not result.wasSuccessful():raise SystemExit(1)
 print('[PortableHeaderPrecheck] result=Passed checks='+str(result.testsRun)+' World=False')
else:
 sys.argv=[str(Path(__file__).parent/'test_rotation_reference_pin_collection.py'),'--ci-root',str(root),'--reference-set',a.reference_set];runpy.run_path(sys.argv[0],run_name='__main__')
