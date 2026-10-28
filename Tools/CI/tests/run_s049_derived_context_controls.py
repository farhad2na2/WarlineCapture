"""Data-only regression controls against one actual S049-derived compiler context."""
import argparse,copy,json,os,runpy,sys,tempfile,shutil
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--ci-root',required=True);p.add_argument('--context',required=True);a=p.parse_args();root=Path(a.ci_root).resolve()
runpy.run_path(str(root/'run_skirmish_source_simulation.py'),run_name='qualified_test_bootstrap')
import skirmish_owned_source as owned
profiles=owned.require('skirmish_source_profiles');build=owned.require('skirmish_source_build');context=json.loads(Path(a.context).read_text())
assert profiles._validated_context_target(context)['catalog_id']=='S049'
def reject(name,change):
 c=copy.deepcopy(context);change(c)
 try:profiles._validated_context_target(c)
 except (ValueError,OSError,KeyError,TypeError):return
 raise AssertionError('accepted '+name)
reject('wrong capture hash',lambda c:c['native_factory_reference'].__setitem__('sha256','0'*64))
reject('wrong catalog',lambda c:c['target_context'].__setitem__('catalog_id','S050'))
reject('malformed schema',lambda c:c.__setitem__('schema_version',1))
for flag in ('world_allocated','source_fit_certified','active_mission_water_qualified','mission_ready','runtime_loader_admitted'):
 reject(flag,lambda c,f=flag:c.__setitem__(f,True))
# Existing schema v1/v2 target protocol remains accepted, with the old explicit checks.
for version in (1,2):
 c=copy.deepcopy(context);c['kind']='OriginalCompilerDerivedNativeFactoryContext';c['authority']='ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050';c['schema_version']=version
 c['checks'].pop('factory_keyed_coverage_and_order_projected');c['checks']['factory_sections_unchanged' if version==1 else 'factory_keyed_coverage_and_order_projected']=True
 assert profiles._validated_context_target(c)==c['target_context']
with tempfile.TemporaryDirectory(prefix='s049-derived-pin-') as tmp:
 f=Path(tmp)/'input';f.write_text('original');d=build.pin(f);pins={d['path']:d['sha256']};build.verify(pins);f.write_text('changed')
 try:build.verify(pins)
 except Exception:pass
 else:raise AssertionError('changed input pin accepted')
# Verify the actual historical root maps to an equivalent capture, then reject a changed mapped file.
original_map=os.environ.get('WARLINE_PINNED_ROOT_MAP')
if not original_map:raise ValueError('Actual multi-root mapping required')
rows=json.loads(original_map);native=json.loads(Path(profiles.relocated_path(context['native_factory_reference']['path'])).read_text());historical=native['source_root'];idx=next(i for i,row in enumerate(rows) if row['original']==historical);oldlocal=Path(rows[idx]['local']);projection=json.loads(Path(profiles.relocated_path(native['metadata_projection']['path'])).read_text())
with tempfile.TemporaryDirectory(prefix='s049-derived-root-',dir='/private/tmp') as tmp:
 target=Path(tmp)/'root';target.mkdir()
 capture_rel=str(Path(native['compiler_evidence']['path']).relative_to(historical))
 for rel in list(projection['source_pins'])+[capture_rel]:
  src=oldlocal/rel;dst=target/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
 mapped=copy.deepcopy(rows);mapped[idx]['local']=str(target);os.environ['WARLINE_PINNED_ROOT_MAP']=json.dumps(mapped)
 assert profiles._validated_context_target(context)['catalog_id']=='S049'
 first=next(iter(projection['source_pins']));(target/first).write_bytes(b'changed')
 try:profiles._validated_context_target(context)
 except (ValueError,OSError):pass
 else:raise AssertionError('changed mapped capture input accepted')
 os.environ['WARLINE_PINNED_ROOT_MAP']=original_map
print('[S049DerivedContextControls] Passed actual captured context, wrong hash/identity/schema, false flags, legacy schema 1/2 changed pin and mapped root tampering')
