"""Exercise author finally-revalidation against a real failing child and a changed temporary input."""
import argparse,os,runpy,sys,tempfile,json,subprocess
from pathlib import Path
from unittest.mock import patch
p=argparse.ArgumentParser();p.add_argument('--ci-root',required=True);p.add_argument('--draft',required=True);p.add_argument('--build',required=True);p.add_argument('--data-repo',required=True);a=p.parse_args();ci=Path(a.ci_root).resolve();repo=ci.parents[1]
runpy.run_path(str(ci/'run_skirmish_source_simulation.py'),run_name='qualified_test_bootstrap')
import skirmish_owned_source as owned
author=owned.require('skirmish_source_authoring');real_popen=subprocess.Popen
with tempfile.TemporaryDirectory(prefix='s049-author-revalidation-',dir='/private/tmp') as tmp:
 for name,child,limit in [('failed',"import pathlib,sys;pathlib.Path(sys.argv[1]).write_text('changed');sys.exit(7)",60),('timeout',"import pathlib,sys,time;pathlib.Path(sys.argv[1]).write_text('changed');time.sleep(30)",.3)]:
  draft=Path(tmp)/(name+'.json');draft.write_bytes(Path(a.draft).read_bytes());output=Path(tmp)/(name+'-out')
  def spawn(command,**kw):return real_popen([sys.executable,'-c',child,str(draft)],stdout=kw['stdout'],stderr=kw['stderr'])
  try:
   with patch.object(author.subprocess,'Popen',side_effect=spawn):author.author(repo,repo,repo/'Tools/CI/SkirmishSourceSimulation/manifest.json',output,'S049',104735,draft,a.build,a.data_repo,limit)
  except Exception as e:
   if 'Input changed' not in str(e):raise
  else:raise AssertionError(name+' child input mutation accepted')
  receipt=json.loads((output/'authoring-receipt.json').read_text())
  if name=='failed':assert receipt['process_exit']==7 and receipt['status']=='Failed'
  else:assert receipt['timed_out'] is True and receipt['status']=='Failed'
print('[S049AuthorRevalidationControls] Passed failed and timed-out subprocess changed-input rejection')
