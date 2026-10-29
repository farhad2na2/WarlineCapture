"""Isolated source-only admission helper; it does not establish compiler equivalence."""
import hashlib
import json
import pathlib
import re
import stat

ORIGIN = 'd2c9714d8f8a49cab62de59fe57dceff9cf6ab24b7d176608850a2bfcfd20383'
FIELDS = {'schema_version','kind','mission','seed','target_setup_hash','origin_seed','origin_setup_hash','native_factory_sha256','semantic_inputs_sha256','diagnostic_only','native_context_qualified','mission_ready'}

def pairs(rows):
    result = {}
    for key, value in rows:
        if key in result: raise ValueError('duplicate JSON field')
        result[key] = value
    return result

def load(path):
    p = pathlib.Path(path)
    if not p.is_absolute() or any(x.is_symlink() for x in [p, *p.parents]): raise ValueError('absolute regular descriptor required')
    if not stat.S_ISREG(p.stat().st_mode) or not 0 < p.stat().st_size <= 65536: raise ValueError('bounded regular descriptor required')
    raw = p.read_bytes()
    if len(raw)>65536: raise ValueError('descriptor grew beyond limit')
    value = json.loads(raw, object_pairs_hook=pairs, parse_constant=lambda _: (_ for _ in ()).throw(ValueError('nonfinite JSON')))
    if type(value) is not dict or set(value) != FIELDS: raise ValueError('exact fields required')
    if (type(value['schema_version']) is not int or value['schema_version'] != 1 or value['kind'] != 'S002SourceOnlyDerivedSeed' or
        value['mission'] != 'S002' or type(value['seed']) is not int or not -2147483648 <= value['seed'] <= 2147483647 or
        value['seed'] in (1,104731) or type(value['target_setup_hash']) is not int or not 0 <= value['target_setup_hash'] <= 4294967295 or
        type(value['origin_seed']) is not int or value['origin_seed'] != 104731 or type(value['origin_setup_hash']) is not int or
        value['origin_setup_hash'] != 3657809016 or value['native_factory_sha256'] != ORIGIN or value['semantic_inputs_sha256'] != '5f5f805b12440d38e45b65067bf216b9350d14026568cee565c9d39515386cdf' or value['diagnostic_only'] is not True or
        value['native_context_qualified'] is not False or value['mission_ready'] is not False): raise ValueError('invalid S002 derived descriptor')
    return value, {'path':str(p),'sha256':hashlib.sha256(raw).hexdigest()}

def admit_initial(initial, value, descriptor):
    if initial.get('mission_id') != 'S002' or initial.get('s002_derived_seed') != descriptor or initial.get('selection') != {'seed':value['seed'],'size_id':1,'difficulty_id':2}: raise ValueError('derived initialization mismatch')
    if initial.get('metadata_compatibility',{}).get('sha256') != ORIGIN: raise ValueError('wrong native factory artifact')
    return True

def classify(result):
    result = dict(result)
    result.update(diagnostic_only=True,validation_passed=False,camera_qualified=False,active_mission_water_qualified=False,runtime_presentation_qualified=False,source_fit_certified=False,native_acceptance=False,mission_ready=False)
    if result.get('status') == 'SourceOutcomeObserved': result['status']='DiagnosticSourceOutcomeObserved'
    result['scope']='S002 source-only derived-seed diagnostic; native context and mission readiness unqualified.'
    return result

def generated_guard(value, descriptor):
    # Runtime descriptor is validated by the metadata and fresh-storage owners; never embed one seed/path.
    return ('if((string)initial["mission_id"]=="S002" && initial["s002_derived_seed"]==null && JsonInteger(selection["seed"],"selection.seed")!=1 && JsonInteger(selection["seed"],"selection.seed")!=104731) throw new System.IO.InvalidDataException("Derived descriptor required");'
      'if((string)initial["mission_id"]=="S002" && initial["s002_derived_seed"]!=null) {'
      'var d=initial["s002_derived_seed"] as Newtonsoft.Json.Linq.JObject;'
      'if(d==null||d.Count!=2||d["path"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||d["sha256"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||'
      'JsonInteger(selection["size_id"],"selection.size_id")!=1||JsonInteger(selection["difficulty_id"],"selection.difficulty_id")!=2||'
      '(string)initial["metadata_compatibility"]?["sha256"]!="d2c9714d8f8a49cab62de59fe57dceff9cf6ab24b7d176608850a2bfcfd20383")'
      'throw new System.IO.InvalidDataException("Exact S002 derived seed descriptor required");'
      'var q=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(Game.Runtime.AutonomousWorld.PinnedFile((string)d[\"path\"],(string)d[\"sha256\"],1024*1024)),new Newtonsoft.Json.Linq.JsonLoadSettings{DuplicatePropertyNameHandling=Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Error});if(q.Count!=12||JsonInteger(q[\"schema_version\"],\"schema_version\")!=1||(string)q[\"kind\"]!=\"S002SourceOnlyDerivedSeed\"||(string)q[\"mission\"]!=\"S002\"||JsonInteger(q[\"seed\"],\"seed\")!=JsonInteger(selection[\"seed\"],\"selection.seed\")||q[\"target_setup_hash\"]?.Type!=Newtonsoft.Json.Linq.JTokenType.Integer||JsonInteger(q[\"origin_seed\"],\"origin_seed\")!=104731||(long?)q[\"origin_setup_hash\"]!=3657809016L||(string)q[\"native_factory_sha256\"]!=\"d2c9714d8f8a49cab62de59fe57dceff9cf6ab24b7d176608850a2bfcfd20383\"||(string)q[\"semantic_inputs_sha256\"]!=\"5f5f805b12440d38e45b65067bf216b9350d14026568cee565c9d39515386cdf\"||q[\"diagnostic_only\"]?.Type!=Newtonsoft.Json.Linq.JTokenType.Boolean||(bool?)q[\"diagnostic_only\"]!=true||q[\"native_context_qualified\"]?.Type!=Newtonsoft.Json.Linq.JTokenType.Boolean||(bool?)q[\"native_context_qualified\"]!=false||q[\"mission_ready\"]?.Type!=Newtonsoft.Json.Linq.JTokenType.Boolean||(bool?)q[\"mission_ready\"]!=false)throw new System.IO.InvalidDataException(\"S002 derived descriptor mismatch\");return;}')
