"""Finite source execution admission; never changes catalog playability."""
import json,hashlib
import skirmish_owned_source as owned
from pathlib import Path
PATH=Path(__file__).resolve().parent/'SkirmishSourceSimulation/admitted_profiles.json'
MODULE_SHA=owned.module_pin(__name__,__file__)
PROFILE_RAW=PATH.read_bytes()
PROFILE_SHA=hashlib.sha256(PROFILE_RAW).hexdigest()
def _pairs(rows):
 d={}
 for k,v in rows:
  if k in d:raise ValueError('Duplicate profile key: '+k)
  d[k]=v
 return d
def load_profiles():
 owned.pins();raw=PATH.read_bytes()
 if raw!=PROFILE_RAW:raise ValueError('Admission JSON changed since owned module execution')
 d=json.loads(raw,object_pairs_hook=_pairs)
 if set(d)!={'schema_version','numeric_runtime','profiles'} or type(d['schema_version'])is not int or d['schema_version']!=1 or d['numeric_runtime']!='InstalledMono':raise ValueError('Unsupported admission schema/runtime')
 if type(d['profiles'])is not list or not 0<len(d['profiles'])<=16:raise ValueError('Finite profiles required')
 seen=set()
 for r in d['profiles']:
  if type(r)is not dict or set(r)!={'mission_id','difficulty_id','size_id','seeds'}:raise ValueError('Exact profile keys required')
  if r['mission_id']not in ('S002','S026') or type(r['difficulty_id'])is not int or r['difficulty_id']!=2 or type(r['size_id'])is not int or r['size_id']not in ((1,)if r['mission_id']=='S002'else(1,2,3)):raise ValueError('Unqualified mission selection')
  if type(r['seeds'])is not list or not r['seeds'] or any(type(x)is not int or x not in ((104731,1)if r['mission_id']=='S002'else(104755,)) for x in r['seeds']) or len(set(r['seeds']))!=len(r['seeds']):raise ValueError('Unqualified seed')
  key=(r['mission_id'],r['difficulty_id'],r['size_id'])
  if key in seen:raise ValueError('Duplicate profile')
  seen.add(key)
 return d,{str(PATH):PROFILE_SHA,str(Path(__file__).resolve()):MODULE_SHA}
def require_profile(mission,seed,difficulty_id=2,size_id=1):
 d,pins=load_profiles()
 if type(seed)is not int or type(difficulty_id)is not int or type(size_id)is not int or not any((mission,difficulty_id,size_id)==(r['mission_id'],r['difficulty_id'],r['size_id']) and seed in r['seeds'] for r in d['profiles']):raise ValueError('Unqualified source execution profile')
 return pins
def generate_guard(compiled_mission="S002"):
 if compiled_mission not in ("S002","S026"):raise ValueError("Unknown compiled source backend")
 d,pins=load_profiles();clauses=[]
 for r in d['profiles']:
  if r['mission_id']!=compiled_mission:continue
  clauses.append('(mission=='+json.dumps(r['mission_id'])+' && difficulty=='+str(r['difficulty_id'])+' && size=='+str(r['size_id'])+' && ('+' || '.join('seed=='+str(s) for s in r['seeds'])+'))')
 if PATH.read_bytes()!=PROFILE_RAW:raise ValueError('Admission JSON changed during guard generation')
 owned.pins()
 backend='TypedGroundS026V1'if compiled_mission=='S026'else'LegacyGroundS002V1'
 preflight='internal static Newtonsoft.Json.Linq.JObject Preflight(Newtonsoft.Json.Linq.JObject initial,Newtonsoft.Json.Linq.JObject run){'
 if compiled_mission=='S026':
  preflight+='using var roads=new Game.Runtime.SourceFreshInitialRoadOwner(run);roads.Preflight();var compiled=Game.Runtime.AutonomousWorld.ReadCompiledInitialization(initial);var compatibility=Game.Runtime.SourceMetadataCompatibility.Load(initial,compiled.setup,(string)run["source_repository_root"],(string)run["repository_root"]);compatibility.VerifyUnchanged();string binaryDirectory;var templateDomain=Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory);var units=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(Game.Runtime.AutonomousWorld.Descriptor(initial["unit_production_metadata"],2*1024*1024)));var keys=new System.Collections.Generic.List<string>();foreach(var unit in(Newtonsoft.Json.Linq.JArray)units["units"])keys.Add((string)unit["key"]);var templatePreflight=Game.Runtime.GeneratedReferenceTemplateImporter.Preflight(templateDomain,binaryDirectory,keys);if((int)templatePreflight["roots"]!=16||(int)templatePreflight["entities"]!=528||(int)templatePreflight["decoded_components"]!=3250||(bool)templatePreflight["world_allocated"]||(int)templatePreflight["entity_identities_allocated"]!=0)throw new System.IO.InvalidDataException("Qualified paired template read domain changed");Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));Game.Configs.SourceEnglishCommandText.Bind(Game.Runtime.AutonomousWorld.Descriptor(initial["command_text_data"],4*1024*1024));return new Newtonsoft.Json.Linq.JObject{["status"]="Passed",["world_allocated"]=false,["entity_ids_allocated"]=0,["compiler_hash"]=Game.Configs.SkirmishSetupCompiler.ComputeHash(compiled.setup,compiled.setup.Seed),["mission"]=compiled.setup.CatalogId,["metadata_source_and_current_origins_checked"]=true,["paired_template_preflight"]=templatePreflight};}'
 else:preflight+='return new Newtonsoft.Json.Linq.JObject{["status"]="Passed",["world_allocated"]=false,["entity_ids_allocated"]=0,["scope"]="S002 profile/descriptor boundary; existing source-smoke-data owns native input qualification"};}'

 return 'using System; internal static class SourceAdmittedProfiles { internal static int JsonInteger(Newtonsoft.Json.Linq.JToken value,string name){if(value==null||value.Type!=Newtonsoft.Json.Linq.JTokenType.Integer)throw new System.IO.InvalidDataException("Integer JSON field required: "+name);long n=(long)value;if(n<int.MinValue||n>int.MaxValue)throw new System.IO.InvalidDataException("Int32 JSON field required: "+name);return(int)n;} internal static void RequireInitialization(Newtonsoft.Json.Linq.JObject initial){if(initial==null||JsonInteger(initial["schema_version"],"initialization.schema_version")!=1||initial["kind"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||(string)initial["kind"]!="GeneratedMissionInitialization"||initial["authority"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||(string)initial["authority"]!="AuthoredConfigAndBakedCampaignMap"||initial["mission_id"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String)throw new System.IO.InvalidDataException("Exact authored initialization envelope required");var selection=initial["selection"]as Newtonsoft.Json.Linq.JObject;if(selection==null||selection.Count!=3||selection["seed"]==null||selection["size_id"]==null||selection["difficulty_id"]==null)throw new System.IO.InvalidDataException("Exact finite selection required");Require((string)initial["mission_id"],JsonInteger(selection["seed"],"selection.seed"),JsonInteger(selection["difficulty_id"],"selection.difficulty_id"),JsonInteger(selection["size_id"],"selection.size_id"));} '+preflight+' internal const string Backend='+json.dumps(backend)+'; internal static void RequireBackend(string value){if(value!=Backend)throw new NotSupportedException("Compiled runtime backend mismatch");} internal static void Require(string mission,int seed,int difficulty=2,int size=1) { if(!('+' || '.join(clauses)+')) throw new NotSupportedException("Unqualified source execution profile"); } }\n',pins
