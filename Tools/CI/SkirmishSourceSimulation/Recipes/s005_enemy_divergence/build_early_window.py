#!/usr/bin/env python3
"""Compile opt-in source actor sampling and original path-budget observation."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's005-enemy-window-build-20261106-T1'
OUT = LOGS / 's005-early-path-window-build-20261106-T2'
OLD_ACTOR_TRACE = Path(__file__).with_name('EnemyPhaseTrace.cs')
PATH_TRACE = Path(__file__).with_name('PathBudgetTrace.cs')
BUDGET = ROOT / 'Assets/Game/Scripts/Systems/UnitPathfindingBudget.cs'

def sha(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        for part in iter(lambda:f.read(1024*1024),b''):h.update(part)
    return h.hexdigest()

def main():
    OUT.mkdir(exist_ok=False);old=json.loads((BASE/'receipt.json').read_bytes())
    if old.get('status')!='PassedCompileProbe' or old.get('mission_ready') is not False:raise ValueError('Passed unqualified diagnostic build required')
    pins=dict(old['input_sha256'])
    for name,digest in pins.items():
        if not Path(name).is_file() or sha(name)!=digest:raise ValueError('Source pin changed: '+name)
    options=[json.loads(x) for x in (BASE/'compile.rsp').read_text().splitlines() if x.strip()]
    helper=BASE/'S005EnemyWindowHelper.cs'
    for path in (OLD_ACTOR_TRACE,PATH_TRACE,BUDGET,helper,BASE/'compile.rsp',BASE/'receipt.json',Path(__file__)):
        pins[str(path)]=sha(path)
    if options.count(str(OLD_ACTOR_TRACE))!=1 or options.count(str(BUDGET))!=1 or options.count(str(helper))!=1:
        raise ValueError('Exact original source seams required')
    actor=OLD_ACTOR_TRACE.read_text(); anchor='if (tick < 1080 || tick > 1180) return;'
    if actor.count(anchor)!=1:raise ValueError('Exact actor sampling seam required')
    actor_path=OUT/'EarlyEnemyPhaseTrace.cs';actor_path.write_text(actor.replace(anchor,'if (tick > 1180 || (tick < 1080 && tick % 30 != 0)) return;',1))
    budget=BUDGET.read_text()
    if budget.count('#if UNITY_EDITOR')!=3:raise ValueError('Exact original budget observation guards required')
    if budget.count('        [Unity.Burst.BurstDiscard]\n')!=1:raise ValueError('Exact native-only Burst attribute required')
    budget_path=OUT/'ObservedUnitPathfindingBudget.cs';budget_path.write_text(budget.replace('#if UNITY_EDITOR','#if true').replace('        [Unity.Burst.BurstDiscard]\n','',1))
    helper_text=helper.read_text();seam='                var seenRequests=new HashSet<string>(StringComparer.Ordinal);'
    if helper_text.count(seam)!=1:raise ValueError('Exact bounded loop seam required')
    helper_path=OUT/'S005EarlyPathWindowHelper.cs';helper_path.write_text(helper_text.replace(seam,'                using var pathBudgetTrace=S005PathBudgetTrace.Attach();\n'+seam,1))
    replace={str(OLD_ACTOR_TRACE):str(actor_path),str(BUDGET):str(budget_path),str(helper):str(helper_path)}
    options=[replace.get(x,x) for x in options];options=[('-out:'+str(OUT/'source-force-ledger.exe')) if x.startswith('-out:') else x for x in options];options.append(str(PATH_TRACE))
    for p in (actor_path,budget_path,helper_path):pins[str(p)]=sha(p)
    (OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in options))
    compiler=Path('/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll');pins[str(compiler)]=sha(compiler)
    result=subprocess.run(['dotnet',str(compiler),'@'+str(OUT/'compile.rsp')],capture_output=True,text=True,timeout=300)
    (OUT/'compile.log').write_text(result.stdout+result.stderr)
    drift=[p for p,h in pins.items() if not Path(p).is_file() or sha(p)!=h]
    receipt=dict(old);receipt.update({'status':'PassedCompileProbe' if result.returncode==0 and not drift else 'FailedCompileProbe','compile_exit':result.returncode,'input_sha256':pins,'compile_log_sha256':sha(OUT/'compile.log'),'changed_during_compile':drift,'scope':'Original path-budget post-decision observation and coarse source actor samples; source host omits native-only BurstDiscard attribute; no policy change, native parity or readiness','runtime_admission':False,'mission_ready':False})
    (OUT/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'status':receipt['status'],'compile_exit':result.returncode,'drift_count':len(drift)}))
    if receipt['status']!='PassedCompileProbe':raise SystemExit(1)

if __name__=='__main__':main()
