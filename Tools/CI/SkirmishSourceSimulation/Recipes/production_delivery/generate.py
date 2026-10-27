from pathlib import Path
import argparse,contextlib,hashlib,io,json,re
INPUTS=('Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs', 'Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs', 'Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs', 'Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs', 'Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs', 'Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs', 'Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs')
HELPERS={'complete_heli_fragment.py': '48599854a46a6d5459d2e06eba616bfa04cc6b3768a4c3f1e6b84655c38f764d', 'engine-binding-contract.json': 'bb80ea86cb1d8d351eeecce56eebcd69eec48cc7d2fb2f8673907c908d8395fe', 'ground-bridge.cs.template': '10c5bd32318370c5508c262ab18e72285c5d4892d639cbefa1e667362bc2f3bd', 'helicopter-bridge.cs.template': 'd0820b9132c27e201028e7546aa0c7a4f32210b0b70b09cc220afb0ed3bae6e2', 'native-method-contract.json': 'b836e9a08feebfbd35f00cbcfdc3d681fbed5651fc19a0b73069af54b9a47d3d', 'partial_generate.py': '3c365c5096cbcde0fc2e930af99a9af1402dbb0808c1fbf27f159787b034cc65', 'prepare_ground.py': '81d748228ca3a88d70b557bf749babedeb253fcd72c30104701628ebb0a931a6', 'prepare_heli.py': '5e6cb234d04a79f1edaad18bf2b2d8264ed2ed0a92a29613fad640978d308971', 'prepare_queue.py': '5634b7e4ebd9641548f6374d0a6ea32ec9706d8fd62a563aebc6d537e256d586', 'queue-bridge.cs.template': '283bd2788be952db99cb4d78101d5de8d4d2bc57c46578cbaa07eb4b773ace0e', 'source_extract.py': '7995379fabf146f5a2047513151409767700cfa30103d82bb26efe5e4014fa10'}
OUTPUTS=('SourceHelicopterDelivery.cs','SourceProductionDeliveryOwner.cs','SourceProductionGroundWorld.cs','SourceProductionSpawnReadModels.cs')
SCRIPTS=('partial_generate.py','prepare_ground.py','prepare_queue.py','prepare_heli.py')
def sha(b):return hashlib.sha256(b).hexdigest()
def read_current(p):
 if p.stat().st_size>2*1024*1024:raise ValueError('Input exceeds2MiB bound: '+str(p))
 raw=p.read_bytes()
 if len(raw)>2*1024*1024 or b'\r' in raw.replace(b'\r\n',b''):raise ValueError('Unsupported input bytes: '+str(p))
 norm=raw.replace(b'\r\n',b'\n')
 return norm.decode('utf-8'),raw,{'path':str(p),'raw_sha256':sha(raw),'lf_sha256':sha(norm),'representation':'CRLF_TO_LF_IN_MEMORY' if raw!=norm else 'LF_IDENTITY'}
def unchanged(pins):
 for p in pins:
  if sha(Path(p['path']).read_bytes())!=p['raw_sha256']:raise ValueError('Input changed during generation: '+p['path'])
def generate(repo:Path,output:Path):
 repo=Path(repo);output=Path(output);base=Path(__file__).resolve().parent;source={};native_raw={};helper={};pins=[]
 if any((output/n).exists() for n in OUTPUTS):raise FileExistsError('Generated output exists')
 for n in INPUTS:
  text,raw,p=read_current(repo/n);source[n]=text;native_raw[n]=raw;pins.append(p)
 for n,pin in HELPERS.items():
  text,raw,p=read_current(base/'helpers'/n)
  if p['lf_sha256']!=pin:raise ValueError('Unreviewed helper drift: '+n)
  helper[n]=text;pins.append(p)
 utilities={};exec(helper['source_extract.py'],utilities);original=utilities['method'];byText={v:k for k,v in source.items()};contract=json.loads(helper['native-method-contract.json']);calls=[]
 def method(text,name,occurrence=0):
  t,p=original(text,name,occurrence);key=byText[text]+'|'+name+'|'+str(occurrence);signature=re.sub(r'\s+','',t[:t.index('{')])
  if contract.get(key)!=signature:raise ValueError('Unsupported current native member/header: '+key)
  calls.append({'key':key,'header_tokens':signature,'span':p,'method_lf_sha256':sha(t.encode())});return t,p
 # Validate the finite member/header domain before emitting any output.
 for key,expected in contract.items():
  path,name,occurrence=key.split('|');t,_=original(source[path],name,int(occurrence))
  if re.sub(r'\s+','',t[:t.index('{')])!=expected:raise ValueError('Unsupported current native member/header: '+key)
 unchanged(pins);output.mkdir(parents=True,exist_ok=True)
 env={'Path':Path,'re':re,'json':json,'hashlib':hashlib,'P':output,'repo':repo,'source':source,'native_raw':native_raw,'helper':helper,'method':method,'block':utilities['block']}
 with contextlib.redirect_stdout(io.StringIO()) as captured:
  for n in SCRIPTS:exec(helper[n],env)
 for n in OUTPUTS:
  text=(output/n).read_text()
  if 'UNGENERATED_NATIVE' in text or '@@' in text:raise ValueError('Unresolved native hook: '+n)
 unchanged(pins)
 return {'schema_version':1,'kind':'CURRENT_NATIVE_TYPED_GROUND_PRODUCTION_DELIVERY_PROJECTION','inputs':pins,'outputs':{n:sha((output/n).read_bytes()) for n in OUTPUTS},'native_method_visits':calls,'unique_native_methods':len(set(c['key'] for c in calls)),'projection_log':captured.getvalue(),'historical_native_hash_guard':False,'boundaries':['one canonical building registry/pending queue/bank','original paid reserve/adopt and private pending pool reset','same CURRENT SharedProductionQueue/Delivery/TransportDispatch/Placement kernels linked by host','typed source asset/prefab/producer/grid/camera inputs','finite reviewed static-mesh pose/bounds/door/spin/pool view; no native visual claim','live no-provider/no-record profile before spawn mutation','unsupported aircraft helipad resolver and nonoverride infantry connectivity reject ONLYat original consumed branches','template recipe identity/publiccatalog queue guards are explicit existing profile admissions','source current changes still require owning compile/behavior qualification']}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True,type=Path);p.add_argument('--output',required=True,type=Path);a=p.parse_args();print(json.dumps(generate(a.repo,a.output),indent=2))
