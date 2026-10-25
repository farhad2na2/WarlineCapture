"""Portable CURRENT source initial placement, legal rects and canonical dictionary registry adapter."""
from pathlib import Path
import argparse,hashlib,json,re
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

def member(text,name):
 code=mask(text);pattern=r'(?m)^[ \t]*(?:public|private|internal|protected)\s+(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*[^\n{};=]+?\b'+re.escape(name)+r'\s*\('
 matches=list(re.finditer(pattern,code))
 if len(matches)!=1:raise ValueError('Unique native member required: '+name)
 start=matches[0].start();cursor=matches[0].end();depth=1
 while depth:
  if cursor>=len(code):raise ValueError('Unclosed native method signature')
  if code[cursor]=='(':depth+=1
  elif code[cursor]==')':depth-=1
  cursor+=1
 expr=code.find('=>',cursor);brace=code.find('{',cursor)
 if brace<0:
  if expr>=0:raise ValueError('Unsupported scalar expression-bodied native method: '+name)
  raise ValueError('Native member body required: '+name)
 if 0<=expr<brace and not re.fullmatch(r'\s*[\w.]+\s+switch\s*',code[expr+2:brace]):raise ValueError('Unsupported scalar/non-switch expression-bodied native method: '+name)
 end=brace+1;depth=1
 while depth:
  if end>=len(code):raise ValueError('Unclosed native member: '+name)
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 if 0<=expr<brace:
  while end<len(code)and code[end].isspace():end+=1
  if end>=len(code)or code[end]!=';':raise ValueError('Expected expression-switch terminator')
  end+=1
 return text[start:end]


PATHS={'initial':'Assets/Game/Scripts/Systems/BuildingRuntimeSpawnCompositionSystemHelper.InitialPlacement.cs','validation':'Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.cs','roads':'Assets/Game/Scripts/Systems/BuildingPlacementValidationUtilitySystemHelper.RoadValidation.cs','registry':'Assets/Game/Scripts/Systems/RuntimeBuildingSystem.cs'}
METHODS={'validation':['IsFootprintInsideGrid','HasCachedInvalidCellInFootprint','IsRuntimeBlockerCell'],'roads':['IsPlacementRectValid','HasBlockedCell']}
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as file:file.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);paths={k:repo/v for k,v in PATHS.items()};raw={k:v.read_bytes()for k,v in paths.items()};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};templates={name:Path(__file__).with_name(name).read_bytes()for name in['InitialPlacementContext.cs.txt','PlacementValidationContext.cs.txt']}
 initial=member(text['initial'],'TryFindValidInitialBuildingOrigin');selected={name:member(text[group],name)for group,names in METHODS.items()for name in names};registry=text['registry'];anchor='        private readonly Dictionary<int, TBuilding> _buildings = new();'
 if registry.count(anchor)!=1:raise ValueError('Current registry dictionary initialization shape changed')
 if re.search(r'\bRuntimeBuildingCollection\s*\(',mask(registry)):raise ValueError('New native registry constructor requires explicit alias binding review')
 registry=registry.replace(anchor,"""        private readonly Dictionary<int, TBuilding> _buildings;
        public RuntimeBuildingCollection() { _buildings = new(); }
        public RuntimeBuildingCollection(Dictionary<int,TBuilding> canonicalFreshRegistry)
        {
            if (canonicalFreshRegistry == null || canonicalFreshRegistry.Count != 0)
                throw new System.NotSupportedException("Fresh empty canonical registry required; no later counter reseed");
            _buildings = canonicalFreshRegistry;
        }""",1)
 files={'BuildingInitialPlacement.source.cs':templates['InitialPlacementContext.cs.txt'].decode()+initial.lstrip(' ')+'\n}}\n','BuildingPlacementValidation.source.cs':templates['PlacementValidationContext.cs.txt'].decode()+'\n\n'.join(selected[name].lstrip(' ')for name in['IsFootprintInsideGrid','HasCachedInvalidCellInFootprint','IsRuntimeBlockerCell','IsPlacementRectValid','HasBlockedCell'])+'\n}}\n','RuntimeBuildingCollection.source.cs':registry}
 def unchanged():
  if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Native placement/registry source drift during generation')
  if any(Path(__file__).with_name(k).read_bytes()!=v for k,v in templates.items()):raise ValueError('Owned context adapter drift during generation')
 unchanged()
 for name,value in files.items():write_lf(output/name,value)
 proof={'kind':'CurrentNativePlacementRegistryRecipe','source_inputs':{str(paths[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'owned_context_inputs':{k:hashlib.sha256(v).hexdigest()for k,v in templates.items()},'method_inputs':{**{k:hashlib.sha256(v.encode()).hexdigest()for k,v in selected.items()},'TryFindValidInitialBuildingOrigin':hashlib.sha256(initial.encode()).hexdigest()},'outputs':{k:hashlib.sha256(v.encode()).hexdigest()for k,v in files.items()},'bindings':['Complete CURRENT initial relocation method, five complete CURRENT legal placement helpers; only original owner wrapper/context types bound','Same original radius/ring/tie order, rectangle, prefix, road, runtime blocker and overlap callbacks; no absent/zero geometry substitution','Active Campaign preferred-origin diagnostic source paths explicitly reject in existing host adapter; generated S002 relocation uses requirePreferredOrigin=false','CURRENT full registry source retained; only native inline dictionary initializer moved into equivalent default constructor and explicit fresh EMPTY canonical dictionary alias constructor','Original _nextBuildingId initializer/AllocateId/Clear/selection/removal bodies untouched; no second registry/counter/reseed','Exact raw bytes read once/rechecked before and after outputs; CRLF native text normalizes LF'],'native_search_rewritten':False};write_lf(output/'source-extraction.json',json.dumps(proof,indent=2));unchanged();return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
