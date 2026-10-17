"""Plan by default. Root-only --apply imports exactly six reviewed targets plus one new script meta."""
from pathlib import Path
import argparse,hashlib,json,os,re,shutil,subprocess,uuid
ROOT=Path(__file__).resolve().parents[4]
FOLDER=Path(__file__).resolve().parent
MAPS=[('Tools/CI/SkirmishProductionDrafts/LegacyFallbackWitnessDiagnostic/manifest.json','c3588da14e009f5ecc6e6f736d939252cc9ef6b7ce865cdd4eff5ee330f53285'),('Tools/CI/SkirmishProductionDrafts/JointLogicalPadAllocation/manifest.json','57b074178528d02dc7750541edca827a2e7507a8445e2d9ba8bcf9464c8c103a')]
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
def exact(relative):
    path=(ROOT/relative).resolve()
    if not path.is_relative_to(ROOT) or str(relative).startswith(('/', '\\')):raise ValueError('Path outside exact workspace')
    return path
def preflight():
    rows=[]
    for name,expected in MAPS:
        path=exact(name)
        if digest(path)!=expected:raise ValueError('Authority changed: '+name)
        rows.extend(json.loads(path.read_text())['files'])
    if len(rows)!=6 or len({r['target'] for r in rows})!=6:raise ValueError('Exactly six unique reviewed targets required')
    if digest(FOLDER/'META_SUPPLEMENT.json')!='9feea9dd154358b6acd356749d65ebdaf4e3567b4593008d23108f616c98600b':raise ValueError('Meta authority changed')
    meta=json.loads((FOLDER/'META_SUPPLEMENT.json').read_text())
    if len(meta['files'])!=1 or meta['files'][0]['target']!='Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.JointLogicalPadAllocation.cs.meta':raise ValueError('Exact single new meta required')
    rows+=meta['files']
    for row in rows:
        if digest(exact(row['source']))!=row['sha256']:raise ValueError('Source changed: '+row['source'])
        if digest(exact(row['target']))!=row['before_sha256']:raise ValueError('Current-before differs: '+row['target'])
    guid=meta['guid']
    if not re.fullmatch('[a-f0-9]{32}',guid):raise ValueError('Malformed meta GUID')
    scan=subprocess.run(['rtk','proxy','rg','--files-with-matches','--glob','*.meta',r'(?m)^guid:\s*'+guid+r'\s*$',str(ROOT/'Assets')],capture_output=True,text=True)
    if scan.returncode not in (0,1):raise ValueError('Actual meta GUID scan failed: '+scan.stderr)
    if scan.returncode==0:raise ValueError('New meta GUID already exists: '+scan.stdout)
    return rows
def idle():
    script="Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'Unity.exe' -and $_.ExecutablePath -match '[\\\\/]Editor[\\\\/]Unity.exe$' } | Select-Object ProcessId,ExecutablePath,CommandLine | ConvertTo-Json -Compress"
    p=subprocess.run(['powershell.exe','-NoProfile','-Command',script],capture_output=True,text=True,check=True)
    if p.stdout.strip():raise ValueError('Editor active; preserve lane: '+p.stdout.strip())
def apply(rows,backup):
    idle()
    backup=backup.resolve()
    if not backup.is_relative_to(ROOT/'Logs/SkirmishProduction') or backup.exists():raise ValueError('Fresh backup directory beneath Logs/SkirmishProduction required')
    backup.mkdir(parents=True,exist_ok=False)
    originals={};payloads={};written=[]
    for row in rows:
        target=exact(row['target']);originals[row['target']]=target.read_bytes() if target.exists() else None
        payloads[row['target']]=exact(row['source']).read_bytes()
        if originals[row['target']] is not None:
            dest=backup/'before'/row['target'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(originals[row['target']])
        dest=backup/'prepared'/row['target'];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(payloads[row['target']])
    receipt={'status':'Prepared','reviewed_targets':6,'new_meta_targets':1,'maps':[{'path':n,'sha256':h} for n,h in MAPS],'files':rows,'writes':[],'native_executed':False}
    (backup/'IMPORT_RECEIPT.json').write_text(json.dumps(receipt,indent=2)+'\n')
    try:
        # Every source and target is rechecked together immediately before the bounded transaction.
        preflight();idle()
        for row in rows:
            target=exact(row['target'])
            if digest(target)!=row['before_sha256']:raise ValueError('Concurrent target change: '+row['target'])
            target.parent.mkdir(parents=True,exist_ok=True)
            os.replace(backup/'prepared'/row['target'],target);written.append(row)
            if digest(target)!=row['sha256']:raise ValueError('Post-write byte mismatch: '+row['target'])
        receipt['status']='ImportedExactReviewedSixPlusMeta';receipt['writes']=[r['target'] for r in written]
    except BaseException as error:
        rollback=[]
        for row in reversed(written):
            target=exact(row['target'])
            try:
                if digest(target)!=row['sha256']:raise ValueError('Foreign edit after import; refusing overwrite')
                old=originals[row['target']]
                if old is None:target.unlink()
                else:
                    temp=backup/('rollback-'+uuid.uuid4().hex);temp.write_bytes(old);os.replace(temp,target)
                rollback.append({'target':row['target'],'restored':True})
            except BaseException as failure:rollback.append({'target':row['target'],'restored':False,'error':repr(failure)})
        receipt.update(status='Failed_RollbackAttempted',error=repr(error),rollback=rollback)
        raise
    finally:(backup/'IMPORT_RECEIPT.json').write_text(json.dumps(receipt,indent=2)+'\n')
    return receipt
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--apply',action='store_true');p.add_argument('--backup',type=Path);a=p.parse_args()
    rows=preflight()
    if a.apply:
        if a.backup is None:p.error('--apply requires --backup')
        result=apply(rows,a.backup)
    else:result={'status':'PlanOnly_NoAssetsOrToolsWrites','reviewed_targets':6,'new_meta_targets':1,'files':rows}
    print(json.dumps(result,indent=2))
