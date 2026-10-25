"""CURRENT original building requests and explicit source-profile adoption specialization."""
from pathlib import Path
import argparse,hashlib,json,re
import re
def mask(text):
 out=list(text);i=0;state='code'
 while i<len(text):
  c=text[i];n=text[i+1]if i+1<len(text)else''
  if state=='code':
   if c=='/'and n in['/','*']:state='line'if n=='/'else'block';out[i]=out[i+1]=' ';i+=2;continue
   if c=='@'and n=='"':state='verbatim';out[i]=out[i+1]=' ';i+=2;continue
   if c in['"',"'"]:state='string'if c=='"'else'char';out[i]=' ';i+=1;continue
  elif state=='line':
   if c=='\n':state='code'
   else:out[i]=' '
  elif state=='block':
   if c=='*'and n=='/':out[i]=out[i+1]=' ';i+=2;state='code';continue
   if c!='\n':out[i]=' '
  elif state=='verbatim':
   if c=='"'and n=='"':out[i]=out[i+1]=' ';i+=2;continue
   if c=='"':state='code'
   if c!='\n':out[i]=' '
  else:
   if c=='\\':out[i]=' ';i+=1
   elif c==('"'if state=='string'else"'"):state='code'
   if i<len(text)and text[i]!='\n':out[i]=' '
  i+=1
 if state not in['code','line']:raise ValueError('Unclosed native C# lexical value')
 return ''.join(out)


def member(text,name,signature=None,expression=False):
 code=mask(text);pattern=r'\b(?:public|private|internal|protected)\s+(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*[^\n{};=]+?\b'+re.escape(name)+r'\s*\('
 matches=list(re.finditer(pattern,code))
 if signature:matches=[x for x in matches if signature in x.group()]
 if len(matches)!=1:raise ValueError('Unique native member required: '+name+' count='+str(len(matches)))
 start=matches[0].start();cursor=matches[0].end();depth=1
 while depth:
  if cursor>=len(code):raise ValueError('Unclosed source signature')
  if code[cursor]=='(':depth+=1
  elif code[cursor]==')':depth-=1
  cursor+=1
 rest=code[cursor:];arrow=rest.find('=>');brace=rest.find('{')
 if arrow>=0 and(brace<0 or arrow<brace):
  if expression:
   semi=rest.find(';',arrow)
   if semi<0 or any(x in rest[arrow+2:semi]for x in '{}'):raise ValueError('Unsupported finite expression shape: '+name)
   if name!='CanInspectUnavailable' or not rest[arrow+2:semi].strip().startswith('failure is'):raise ValueError('Unadmitted expression member: '+name)
   return text[start:cursor+semi+1]
  if brace<0 or not re.fullmatch(r'\s*[\w.]+\s+switch\s*',rest[arrow+2:brace]):raise ValueError('Unsupported scalar expression-bodied native method: '+name)
 if brace<0:raise ValueError('Native block body required: '+name)
 brace+=cursor;end=brace+1;depth=1
 while depth:
  if end>=len(code):raise ValueError('Unclosed native body: '+name)
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 if arrow>=0 and arrow+cursor<brace:
  while end<len(code)and code[end].isspace():end+=1
  if end>=len(code)or code[end]!=';':raise ValueError('Expected switch expression terminator')
  end+=1
 return text[start:end]
def declaration(text,kind,name):
 code=mask(text);matches=list(re.finditer(r'\bpublic\s+'+re.escape(kind)+r'\s+'+re.escape(name)+r'\b',code))
 if len(matches)!=1:raise ValueError('Unique source declaration required: '+name)
 start=matches[0].start();brace=code.index('{',matches[0].end());end=brace+1;depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]

PATHS={'requests':'Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs','faction':'Assets/Game/Scripts/Systems/BuildingRuntimeOwnershipCompositionSystemHelper.cs','birth':'Assets/Game/Scripts/Runtime/Skirmish/RuntimeActorBirthContext.cs','identity':'Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs','barrier':'Assets/Game/Scripts/Systems/BuildingBarrierUtilitySystemHelper.cs'}
FILES=['RuntimeSpawnRequestProcessor.source.cs','BuildingOwnerFaction.source.cs','NativeBirthDiagnosticProfile.building-adoption.cs','BuildingDefinitionIdentity.source.cs']
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);here=Path(__file__).resolve().parent;paths={k:repo/v for k,v in PATHS.items()};raw={k:v.read_bytes()for k,v in paths.items()};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};templates={k:(here/'templates'/(k+'.txt')).read_bytes()for k in FILES};bodies={};proof=[]
 def selected(group,name,signature=None):
  value=member(text[group],name,signature);proof.append({'member':name,'source':PATHS[group],'native_member_sha256':hashlib.sha256(value.encode()).hexdigest()});return value
 for name in ['ProcessRuntimeSpawnRequests','ResolveOwnerFaction','WriteRuntimeSpawnRequest','TryResolveConfiguredBuildingDefinition']:bodies[name]=selected('requests',name).replace('BuildingDefinitionPrefabSystemHelper','GeneratedConfiguredDefinitionSource')
 constants=[]
 for type_name,name in [('float','IdleRequestProbeIntervalSeconds'),('int','MaxRuntimeSpawnRequestsPerUpdate')]:
  found=list(re.finditer(r'(?m)^\s*private const '+type_name+' '+name+r' = [^;\n]+;',mask(text['requests'])))
  if len(found)!=1:raise ValueError('Unique current request constant required: '+name)
  constants.append(text['requests'][found[0].start():found[0].end()].strip())
 bodies['RequestConstants']='\n'.join(constants)
 owner=selected('faction','SetRuntimeBuildingOwnerFaction').replace('public void SetRuntimeBuildingOwnerFaction','internal void Set',1)
 gate='UpdateRuntimeGateFriendlyPassFaction(context, building, ownerFactionId);';visual='ApplyRuntimeBuildingFactionVisual(context, building);'
 if owner.count(gate)!=1 or owner.count(visual)!=1:raise ValueError('Native faction collaborator shape changed')
 visual_body=selected('faction','ApplyRuntimeBuildingFactionVisual');code=re.sub(r'\s+','',mask(visual_body).split('{',1)[1]);expected='if(context.BuildingFactionVisualSystem==null)return;'
 if not code.startswith(expected):raise ValueError('Native non-rendered faction early-return domain changed')
 owner=owner.replace(gate,'if (building.Definition?.IsWall == true || building.Definition?.HasRunway == true)\n                throw new System.NotSupportedException("Active wall/runway ownership outside starting ground scope");',1).replace(visual,'/* NonRendered scope: original null BuildingFactionVisualSystem early-return, no presentation instance. */',1);bodies['Set']=owner;bodies['UpdateRuntimeCombatFaction']=selected('faction','UpdateRuntimeCombatFaction')
 for name in ['VerifyNativeAdoption','VerifyBuildingRecord']:
  source=selected('birth',name);code=re.sub(r'\s+','',mask(source).split('{',1)[1]);expected='if(!em.HasComponent<RuntimeActorBirthRecord>(actor))return;'
  if not code.startswith(expected):raise ValueError('Native no-record adoption branch changed: '+name)
 birth=selected('birth','VerifyBuildingAdoption')
 for actor in ['building.BlockerEntity','actor']:
  call='VerifyBuildingRecord(em, '+actor+', request);'
  if birth.count(call)!=1:raise ValueError('Native building adoption traversal changed: '+actor)
  birth=birth.replace(call,'RequireNoRecordProfile(em, '+actor+');',1)
 bodies['VerifyBuildingAdoption']=birth;bodies['RuntimeDefinitionMatchesId']=selected('identity','RuntimeDefinitionMatchesId');bodies['IsWallGateDefinition']=selected('barrier','IsWallGateDefinition','public static')
 files={}
 for name,template in templates.items():
  value=template.decode()
  for key,body in bodies.items():value=value.replace('@@'+key+'@@',body)
  if '@@'in value:raise ValueError('Unresolved owned adapter placeholder')
  files[name]=value
 def unchanged():
  if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Current building request/adoption source drift')
  if any((here/'templates'/(k+'.txt')).read_bytes()!=v for k,v in templates.items()):raise ValueError('Owned request context/profile drift')
 unchanged()
 for name,value in files.items():write_lf(output/name,value)
 result={'kind':'CurrentOriginalBuildingRequestAdoptionRecipes','source_inputs':{str(paths[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'members':proof,'native_request_constants':constants,'outputs':{k:hashlib.sha256(v.encode()).hexdigest()for k,v in files.items()},'bindings':['CURRENT complete ProcessRuntimeSpawnRequests +3 definition resolution/write/faction helpers; only definition type/name binding','CURRENT source request constants emitted class-level; remove ONLY two duplicate constants from owned wrapper','CURRENT complete native SetRuntimeBuildingOwnerFaction numeric statements; explicit existing wall/runway gate, original null presentation early return verified','CURRENT complete UpdateRuntimeCombatFaction/definition identity/barrier methods; same actor/storage/fabrication and canonical registry','CURRENT full VerifyBuildingAdoption checkpoint Building/blocker/actor traversal; no-record specialization requires original first-return source guards. Ordinary checkpoint link is NOT a certificate','Existing host no-record profile/census/live-read plumbing; native active records reject, no fabricated certificates/native stream flags','Exact source bytes read once/rechecked, CRLF explicit; no prior body hash gate or policy fallback'],'required_adapter_removals':['private const float IdleRequestProbeIntervalSeconds = 0.1f;','private const int MaxRuntimeSpawnRequestsPerUpdate = 16;']};write_lf(output/'source-extraction.json',json.dumps(result,indent=2));unchanged();return result
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
