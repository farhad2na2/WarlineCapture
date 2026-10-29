#!/usr/bin/env python3
"""Run pinned-file controls against a fresh successful source build, without a World."""
import argparse, hashlib, json, os, pathlib, shutil, subprocess
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--build',required=True);p.add_argument('--output',required=True);a=p.parse_args()
build=pathlib.Path(a.build).resolve(strict=True);root=pathlib.Path(__file__).resolve().parents[2];requested=pathlib.Path(a.output)
if requested.is_symlink():raise SystemExit('Fresh output required')
out=requested.resolve()
if out.exists():raise SystemExit('Fresh output required')
r=json.loads((build/'build.json').read_text());sha=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
if r.get('status')!='Passed' or pathlib.Path(r['repository_root']).resolve()!=root:raise SystemExit('Successful build from this repo required')
pins=r['source_and_reference_pins'];response=pathlib.Path(r['compiler_response']['path']);test=root/'Tools/CI/SkirmishSourceSimulation/Tests/SourcePinnedFileOwnerControls.cs'
def check():
 for path,expected in pins.items():
  if sha(path)!=expected:raise RuntimeError('Build input changed: '+path)
 if sha(response)!=r['compiler_response']['sha256']:raise RuntimeError('Build response changed')
check();test_hash=sha(test);out.mkdir(parents=True);exe=out/'controls.exe';lines=response.read_text().splitlines();main=output=0
for i,line in enumerate(lines):
 if line.strip('"').startswith('-main:'):lines[i]='"-main:SourcePinnedFileOwnerControls"';main+=1
 if line.strip('"').startswith('-out:'):lines[i]='"-out:'+str(exe)+'"';output+=1
if main!=1 or output!=1:raise SystemExit('Unexpected compiler response')
lines.append('"'+str(test)+'"');rsp=out/'controls.rsp';rsp.write_text('\n'.join(lines)+'\n')
dotnet=pathlib.Path(shutil.which('dotnet') or '').resolve(strict=True)
if str(dotnet) not in pins:raise SystemExit('Build-pinned dotnet required')
compilers=[pathlib.Path(path) for path in pins if path.endswith('/Roslyn/bincore/csc.dll')]
if not compilers:raise SystemExit('Build-pinned Roslyn compiler required')
# The build selects the highest installed SDK; select from the pinned build set,
# not the current SDK inventory, using the same version ordering.
def sdk_key(path):return tuple(int(x) for x in path.parents[2].name.split('.') if x.isdigit())
csc=sorted(compilers,key=sdk_key)[-1]
env=os.environ.copy()
for key in ('WARLINE_PINNED_ROOT_MAP','WARLINE_PINNED_ROOT_ORIGINAL','WARLINE_PINNED_ROOT_LOCAL'):env.pop(key,None)
compile=subprocess.run([dotnet,str(csc),'@'+str(rsp)],cwd=root,env=env,capture_output=True,text=True);(out/'compile.log').write_text(compile.stdout+compile.stderr)
if compile.returncode:raise SystemExit('Controls compilation failed; see compile.log')
runtime=r['runtime_selection'];env['MONO_PATH']=os.pathsep.join([str(build/'placement'),str(pathlib.Path(runtime['newtonsoft']).parent),str(root/'Library/ScriptAssemblies'),runtime['engine']]);run=subprocess.run([runtime['mono'],'--optimize=-float32',str(exe)],cwd=root,env=env,capture_output=True,text=True);(out/'run.log').write_text(run.stdout+run.stderr);check()
if sha(test)!=test_hash:raise SystemExit('Test source changed')
if run.returncode or '[SourcePinnedFileOwnerControls] result=Passed' not in run.stdout:raise SystemExit('Controls failed; see run.log')
print('[PinnedFileControlRunner] result=Passed backend='+r['runtime_backend'])
