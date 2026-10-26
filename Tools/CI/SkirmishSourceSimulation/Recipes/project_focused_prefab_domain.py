"""Fresh exact 16-prefab typed domain from qualified native bytes; no World.

Reconstructed producer, qualified by reproducing BOTH historical immutable
slice and locator-bound manifest digests. No claim that an old script was found.
"""
from pathlib import Path
import json,hashlib,collections,argparse
SOURCE_SHA='21e7474adfe2682a638bc6c23708f48b9611c526051871c0d93b4cb18a6ae4aa'
OLD_SLICE_SHA='a154f18e84a083399d07324cfe4f22dfdeb944d4247268324090855e2f7f8dbc'
OLD_MANIFEST_SHA='56195ca97e3a33154a08919ce5b6a495f1eaa8a6295cce8a095686e009900082'
FIELDS=['schema_version','kind','authority','source_scenes','unity_version','template_namespace_id','requested_prefab_keys','requested_prefab_keys_sha256','opening_prefabs','entities','unsupported_values','replay_ready','fresh_world_allocator_exported','full_mission_initialized','native_visual_readiness','geometry_exported','consumer_rule']
SCOPE='Logical gameplay template codec/closed entity namespace; three named presentation-only components not consumed; required numeric launcher/missile fields and anchors retained'
def sha(raw):return hashlib.sha256(raw).hexdigest()
def pairs(rows):
 d={}
 for k,v in rows:
  if k in d:raise ValueError('Duplicate native key '+k)
  d[k]=v
 return d
def encode(value):return(json.dumps(value,indent=2)+'\n').encode()
def produce(source_path,raw,slice_path):
 if sha(raw)!=SOURCE_SHA:raise ValueError('Exact qualified original native source required')
 doc=json.loads(raw,object_pairs_hook=pairs)
 if doc['kind']!='AuthoredSkirmishPrefabReferenceData'or len(doc['opening_prefabs'])!=16 or len(doc['entities'])!=528 or doc['replay_ready']is not False or doc['full_mission_initialized']is not False or doc['geometry_exported']is not False:raise ValueError('Actual finite native scope changed')
 derived={k:doc[k]for k in FIELDS};derived['source_reference_sha256']=SOURCE_SHA;derived['derived_read_domain']='ExactTypedPrefabClosureOnly';body=encode(derived)
 counts=dict(collections.Counter(x['path'].split('/')[1]for x in doc['unsupported_values']))
 manifest={'schema_version':1,'kind':'PinnedExactTemplateDomainProjection','source_path':source_path,'source_sha256':SOURCE_SHA,'source_bytes':len(raw),'slice_path':slice_path,'slice_sha256':sha(body),'slice_bytes':len(body),'selected_fields':FIELDS,'root_count':len(doc['opening_prefabs']),'entity_count':len(doc['entities']),'template_namespace_id':doc['template_namespace_id'],'full_Unity_replay_ready':doc['replay_ready'],'unsupported_values_preserved':len(doc['unsupported_values']),'unsupported_by_type':counts,'consumer_read_domain':SCOPE,'world_created':False}
 return body,encode(manifest)
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--source',required=True);p.add_argument('--output',required=True);p.add_argument('--historical-source-locator',required=True);p.add_argument('--historical-slice-locator',required=True);a=p.parse_args();source=Path(a.source)
 if not source.is_absolute()or not source.is_file()or source.is_symlink()or source.stat().st_size>8*1024*1024:raise ValueError('Bounded absolute regular native reference required')
 raw=source.read_bytes();oldbody,oldmanifest=produce(a.historical_source_locator,raw,a.historical_slice_locator)
 if sha(oldbody)!=OLD_SLICE_SHA or sha(oldmanifest)!=OLD_MANIFEST_SHA:raise ValueError('Historical producer reproduction failed')
 out=Path(a.output).absolute()
 if out.exists():raise ValueError('Fresh projection output required')
 out.mkdir(parents=True);slice_path=out/'templates16.exact-slice.json';body,manifest=produce(str(source),raw,str(slice_path));slice_path.write_bytes(body);mp=out/'template-slice-manifest.json';mp.write_bytes(manifest);descriptor={'path':str(slice_path),'sha256':sha(body),'source_reference_path':str(source),'source_reference_sha256':SOURCE_SHA,'projection_manifest_path':str(mp),'projection_manifest_sha256':sha(manifest)};(out/'descriptor.json').write_bytes(encode(descriptor));receipt={'kind':'QualifiedReconstructedExactPrefabProjection','historical_slice_reproduced_sha256':sha(oldbody),'historical_manifest_reproduced_sha256':sha(oldmanifest),'new_projection_manifest_sha256':sha(manifest),'new_slice_sha256':sha(body),'producer_sha256':sha(Path(__file__).read_bytes()),'all17native_fields_preserved':True,'full_native_replay_ready':False,'world_created':False,'descriptor':descriptor};(out/'producer-receipt.json').write_bytes(encode(receipt));print(json.dumps(receipt))
if __name__=='__main__':main()
