#!/usr/bin/env python3
"""Run exact-source S049 reader controls with caller-supplied captured inputs."""
import argparse, hashlib, json, os, pathlib, shutil, subprocess, sys
P=pathlib.Path

def require(condition,message):
 if not condition:raise ValueError(message)

def digest(path):
 h=hashlib.sha256()
 with P(path).open('rb') as f:
  for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
 return h.hexdigest()
def pinned(d):
 p=P(d['path']);require(p.is_absolute() and p.is_file() and digest(p)==d['sha256'],'Pinned input changed: '+str(p));return p
def main():
 a=argparse.ArgumentParser();a.add_argument('--build-receipt',required=True);a.add_argument('--initialization',required=True);a.add_argument('--source-root',required=True);a.add_argument('--data-root',required=True);a.add_argument('--mono-config',required=True);a.add_argument('--output',required=True);v=a.parse_args()
 build=json.loads(P(v.build_receipt).read_text());require(build['status']=='Passed','Build receipt not passed');root=P(build['repository_root']).resolve();require(root==P(v.source_root).resolve(),'Source root differs from build');out=P(v.output);out.mkdir(parents=True,exist_ok=False)
 for path,sha in build['source_and_reference_pins'].items():require(digest(path)==sha,'Build pin changed: '+path)
 rsp=pinned(build['compiler_response']);initial=json.loads(P(v.initialization).read_text());source= P(__file__).resolve().parent;dot=shutil.which('dotnet');require(dot,'dotnet required');sdk=P(dot).resolve().parent/'sdk';compilers=sorted(sdk.glob('*/Roslyn/bincore/csc.dll'));require(compilers,'Roslyn compiler required');runtime=build['runtime_selection'];mono=P(runtime['mono']);engine=P(runtime['engine']);newton=P(runtime['newtonsoft']).parent;assemblies=root/'Library/ScriptAssemblies';env=os.environ.copy();env['MONO_PATH']=os.pathsep.join(map(str,(engine,assemblies,newton)));config=P(v.mono_config).resolve();require(config.is_file(),"Mono config required")
 descriptors=['metadata_compiler_context','configuration_qualification','navigation_reference','operation_map_rotation_reference','death_mechanical_inputs'];paths={name:str(pinned(initial[name])) for name in descriptors}
 cases=[('DirectContextControls',[paths['metadata_compiler_context'],paths['configuration_qualification'],str(out/'mutations')]),('FixedMapControls',[paths['navigation_reference']]),('ArtifactControls',[paths['operation_map_rotation_reference'],paths['death_mechanical_inputs']]),('FactoryControls',[str(P(v.initialization).resolve()),str(root),str(P(v.data_root).resolve())]),('ClassifierControls',[])]
 base=rsp.read_text().splitlines()
 for name,args in cases:
  control=source/(name+'.cs');exe=out/(name+'.exe');lines=[]
  for line in base:
   if line.startswith('"-main:'):line='"-main:Controls"'
   elif line.startswith('"-out:'):line='"-out:'+str(exe.resolve())+'"'
   lines.append(line)
  lines.append('"'+str(control)+'"');response=out/(name+'.rsp');response.write_text('\n'.join(lines)+'\n')
  compiled=subprocess.run([dot,str(compilers[-1]),'@'+str(response)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);(out/(name+'.compile.log')).write_text(compiled.stdout);require(compiled.returncode==0,'Compile failed: '+name)
  result=subprocess.run([str(mono),'--optimize=-float32','--config',str(config),str(exe),*args],env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);(out/(name+'.log')).write_text(result.stdout);require(result.returncode==0 and 'result=Passed' in result.stdout,'Control failed: '+name)
 print('[S049SourceControls] result=Passed controls='+str(len(cases)))
if __name__=='__main__':main()
