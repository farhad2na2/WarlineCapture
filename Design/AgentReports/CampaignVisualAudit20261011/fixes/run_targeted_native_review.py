"""Sequential native visual captures through the required GUI wrapper."""
from pathlib import Path
import argparse, json, subprocess, sys
parser=argparse.ArgumentParser()
parser.add_argument("--run",default="visual-fix-03")
parser.add_argument("--indices",default="22,23,24,0,8")
args=parser.parse_args()
root=Path(__file__).resolve().parents[4]
audit=root/"Design/AgentReports/CampaignVisualAudit20261011"
inventory=json.loads((audit/"inventory.json").read_text())["missions"]
failures=[]
for index in map(int,args.indices.split(",")):
    mission=next(m for m in inventory if m["index"]==index)
    run=args.run
    receipt=audit/"captures"/mission["missionId"]/("run-"+run)/"receipt.json"
    if receipt.parent.exists(): raise RuntimeError("Preserve prior evidence: "+str(receipt.parent))
    (audit/"request.json").write_text(json.dumps(dict(index=index,run=run,warmupSeconds=8,supplemental=False,reimportSource=False,inspectPlacement=True)))
    log=audit/"fixes/validation"/("targeted-"+run+"-"+str(index)+".log")
    result=subprocess.run(["rtk","proxy","Tools/CI/invoke_unity_macos.sh","--reuse","--project",str(root),"--timeout","600","--log",str(log),"--","-executeMethod","Game.Editor.CampaignVisualAudit20261011.CaptureMission"],cwd=root)
    if result.returncode or not receipt.exists():sys.exit(result.returncode or 1)
    if "[CampaignVisualAudit] result=Passed" not in log.read_text(errors="replace"):raise RuntimeError("Missing capture pass marker: "+str(log))
    d=json.loads(receipt.read_text())
    rejected=[r for r in d["spawnRequests"] if "status=2" in r and ("authoredStartingGrant=1" in r or "requirePreferred=1" in r)]
    if d["result"]!="Passed" or d["missionOutcome"]!="None" or rejected:failures.append(mission["missionId"])
    print(json.dumps(dict(mission=mission["missionId"],capture=d["result"],phase=d["missionPhase"],outcome=d["missionOutcome"],requiredFailures=rejected)),flush=True)
print(json.dumps(dict(result="Failed" if failures else "Passed",failedMissions=failures,scope="Native opening visual/readiness checks; no normal-input completion claimed")),flush=True)
sys.exit(1 if failures else 0)
