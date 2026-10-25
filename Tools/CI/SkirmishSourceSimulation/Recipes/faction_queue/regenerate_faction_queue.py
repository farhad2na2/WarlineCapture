"""CURRENT timeline/reservation/faction request policies over same paid owner."""
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
 code=mask(text);pattern=r'\b(?:public|private|internal|protected)\s+(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*[^\n{};=]+?\b'+re.escape(name)+r'(?:<[^{};()]*>)?\s*\('
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
 code=mask(text);matches=list(re.finditer(r'\bpublic\s+(?:readonly\s+)?'+re.escape(kind)+r'\s+'+re.escape(name)+r'\b',code))
 if len(matches)!=1:raise ValueError('Unique source declaration required: '+name)
 start=matches[0].start();brace=code.index('{',matches[0].end());end=brace+1;depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]



PATHS={'queue':'Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs','native':'Assets/Game/Scripts/Runtime/Skirmish/SkirmishNativeProduction.cs','request':'Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs','processing':'Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs'}
METHODS={'queue':['InitializePendingProduction','GetProgress','IsReady','IsReadyWithin','GetTransportLaunchAt','ShouldLaunchTransport','RebuildPendingProductionTimeline','DelayPendingProduction'],'native':['TrySession','Readiness','ResearchBlocks','CanQueue','TryReserve'],'request':['QueueFactionUnitProductionRequest','GetCachedUnitIdString','TryFindFirstFactionProducerBuilding','TryUseFactionProducerBuilding','TryResolveConfiguredUnit','ConfiguredUnitMatchesId','TryBuildConfiguredUnit','ToFixedString128']}
FILES=['BuildingProductionQueueCompositionSystemHelper.timeline.cs','SkirmishNativeProduction.typed.cs','FactionRequestKernel.source.cs','SourceFactionProductionDispatcher.cs']
def once(text,before,after):
 if text.count(before)!=1:raise ValueError('Finite faction policy binding changed: '+before[:120])
 return text.replace(before,after,1)
def block(text,marker):
 code=mask(text)
 if code.count(marker)!=1:raise ValueError('Unique guarded faction branch required')
 start=code.index(marker);brace=code.index('{',start);end=brace+1;depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end],code[start:brace]
def source_declaration(text,name,pattern):
 matches=list(re.finditer(pattern,mask(text)))
 if len(matches)!=1:raise ValueError('Unique original class declaration required: '+name)
 return text[matches[0].start():matches[0].end()].strip()
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as f:f.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);here=Path(__file__).resolve().parent;paths={k:repo/v for k,v in PATHS.items()};raw={k:v.read_bytes()for k,v in paths.items()};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};templates={name:(here/'templates'/(name+'.txt')).read_bytes()for name in FILES};bodies={};proof=[]
 for group,names in METHODS.items():
  for name in names:
   value=member(text[group],name);proof.append({'source':PATHS[group],'member':name,'native_member_sha256':hashlib.sha256(value.encode()).hexdigest()});bodies[name]=value
 for kind,name in [('enum','ProductionTransportMode'),('interface','IPendingProduction'),('struct','PendingProductionProgress')]:
  value=declaration(text['queue'],kind,name);bodies[name]=value;proof.append({'source':PATHS['queue'],'declaration':name,'native_declaration_sha256':hashlib.sha256(value.encode()).hexdigest()})
 request=bodies['QueueFactionUnitProductionRequest'];branch,condition=block(request,'if (request.FactionId == 2 &&');condition=re.sub(r'\s+','',condition)
 required='&&entityManager.HasComponent<SkirmishEnemyProductionIntentV1Component>(request.RequestOwner)&&'
 if required not in condition or '||'in condition or '?'in condition.replace('?.','').replace('??',''):raise ValueError('Native post-payment correlation not proven V1-only')
 request=once(request,branch,'/* Active V1 intent rejected before payment by same-world dispatcher; original branch unreachable in S002 profile. */');bodies['QueueFactionUnitProductionRequest']=request.replace('SkirmishPopulationPolicy.','SourceCampPopulationPolicy.').replace('SkirmishNativeProduction.','GeneratedEnemyProductionRequests.')
 bodies['GetCachedUnitIdString']=once(bodies['GetCachedUnitIdString'],'unitId.Length == 0','unitId.ToString().Length == 0')
 bodies['UnitIdCache']=source_declaration(text['request'],'unit ID cache',r'(?m)^\s*private readonly Dictionary<FixedString128Bytes, string> _unitIdStringCache\s*=\s*[^;\n]+;')
 bodies['IdleProbeConstant']=source_declaration(text['processing'],'idle request probe',r'(?m)^\s*private const float IdleRequestProbeIntervalSeconds\s*=\s*[^;\n]+;')
 process=member(text['processing'],'ProcessProductionRequests');proof.append({'source':PATHS['processing'],'member':'ProcessProductionRequests','native_member_sha256':hashlib.sha256(process.encode()).hexdigest()});start=process.index('{')+1;end=process.index('DynamicBuffer<BuildingFactionUnitProductionRequest>',start);guard=process[start:end].strip();code=mask(guard)
 if not re.fullmatch(r'if\s*\([^{};]+\)\s*return;',code):raise ValueError('Native production probe prefix needs explicit mutation/order review')
 bodies['CurrentProbeGuard']=guard
 files={}
 for name,template in templates.items():
  value=template.decode()
  for key,body in bodies.items():value=value.replace('@@'+key+'@@',body)
  if '@@'in value:raise ValueError('Unresolved owned faction/queue context placeholder')
  files[name]=value
 def unchanged():
  if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Current queue/faction source byte drift')
  if any((here/'templates'/(k+'.txt')).read_bytes()!=v for k,v in templates.items()):raise ValueError('Owned faction/queue context byte drift')
 unchanged()
 for name,value in files.items():write_lf(output/name,value)
 result={'kind':'CurrentNativeQueueTimelineReservationFactionPolicy','source_inputs':{str(paths[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'members':proof,'native_idle_probe_constant':bodies['IdleProbeConstant'],'native_probe_guard':guard,'outputs':{k:hashlib.sha256(v.encode()).hexdigest()for k,v in files.items()},'bindings':['Complete current queue timeline8methods+original enum/interface/progress data; SourceUnitPrefab is existing identity token','Complete current NativeProduction TrySession/Readiness/ResearchBlocks/CanQueue/TryReserve; same real paid/reservation/refund/native binding owner','Complete current faction request8methods and original ID-cache declaration; live configured entries/faction producer/order/current paid owner preserved','Only original V1-only postpayment correlation block omitted, with native conjunction proof and existing dispatcher V1-presence preflight BEFORE payment; no receipt inference/fake state','Native FixedString128.Length==0 maps existing empty-string test only; no nonempty policy conversion','Owned same-world dispatcher retains finite/poison/readscope gates; original current native idle constant and probe guard regenerated, no hardcoded .1 cadence','Exact raw bytes read once/rechecked; CRLF normalization; unknown prefix/correlation/method shapes reject; no Quote/default/frozen fallback']};write_lf(output/'source-extraction.json',json.dumps(result,indent=2));unchanged();return result
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
