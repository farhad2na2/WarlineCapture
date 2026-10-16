"""Offline verification of prepared source backup; never invokes Unity or applies Assets."""
import argparse,hashlib,json,pathlib,re
ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--index-sha256',required=True);args=ap.parse_args()
root=pathlib.Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
raw=(root/'index.json').read_bytes()
if sha(raw)!=args.index_sha256:raise RuntimeError('Pinned index SHA mismatch')
index=json.loads(raw);objects={};overlay={};entries=0
if index.get('predecessorIndex') and sha((root/index['predecessorIndex']).read_bytes())!=index['predecessorIndexSha256']:raise RuntimeError('Preserved predecessor index SHA mismatch')

def verify(row):
 p=root/row['object'];expected=row['sha256']
 if row['object']!='objects/'+expected+'.blob' or not re.fullmatch('[0-9a-f]{64}',expected) or p.is_symlink():raise RuntimeError('Unsafe object reference')
 data=p.read_bytes()
 if len(data)!=row['bytes'] or sha(data)!=expected:raise RuntimeError('Exact object hash/size mismatch')
 objects[expected]=len(data);return data

for bundle in index['bundles']:
 manifest=json.loads(verify(bundle['manifest']))
 records={r['path']:r for r in manifest['files']}
 if len(records)!=len(manifest['files']) or set(records)!=set(r['path'] for r in bundle['sources']):raise RuntimeError('Original manifest paths differ/duplicate')
 for row in bundle['sources']:
  record=records[row['path']];expected=record.get('stagedSha256',record.get('staged_sha256'))
  if not row['path'].startswith('Assets/') or '..' in pathlib.PurePosixPath(row['path']).parts or expected!=row['stagedSha256'] or expected!=row['sha256']:raise RuntimeError('Source manifest mapping mismatch')
  verify(row);entries+=1
  if row['path'] in overlay and row['baseSha256'] is not None and overlay[row['path']]['stagedSha256']!=row['baseSha256']:raise RuntimeError('Ordered overlay base mismatch')
  overlay[row['path']]={'bundle':bundle['bundle'],**row}
 patch=verify(bundle['patch'])
 if sha(patch)!=manifest.get('patchSha256',manifest.get('patch_sha256')):raise RuntimeError('Original patch SHA mismatch')
 for row in bundle['metadata']:verify(row)
if index['layerOrder']!=[b['bundle'] for b in index['bundles']] or index['finalOverlay']!=[overlay[p] for p in sorted(overlay)]:raise RuntimeError('Overlay ordering/mapping differs')
actual={p.name for p in (root/'objects').iterdir()}
if actual!={h+'.blob' for h in objects}:raise RuntimeError('Object inventory missing/extra')
if len(objects)!=index['objectCount'] or sum(objects.values())!=index['objectBytes'] or entries!=index['sourceEntryCount'] or len(overlay)!=index['finalOverlayFiles']:raise RuntimeError('Count/size mismatch')
print(json.dumps({'result':'PassedExactPreparedSourceBackup','scope':'Offline exact bytes/manifest/source/patch/overlay verification only; no native test or gameplay integration','indexSha256':sha(raw),'objects':len(objects),'objectBytes':sum(objects.values()),'sourceEntries':entries,'finalOverlayFiles':len(overlay),'unityCalls':0,'assetsApplied':0},indent=2))
