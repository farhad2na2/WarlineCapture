"""CURRENT complete S002 enemy owner with explicit unsupported commander/direct-birth gates."""
from pathlib import Path
import argparse,hashlib,json,re
SOURCE='Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyStrategySystem.cs'
def once(text,old,new):
 if text.count(old)!=1:raise ValueError('Unique current enemy owner binding required: '+old)
 return text.replace(old,new,1)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);path=repo/SOURCE;raw=path.read_bytes();text=raw.decode('utf-8').replace('\r\n','\n');text=re.sub(r'(?m)^    \[Update(?:InGroup|Before|After)\([^\n]*\]$','',text)
 text=once(text,'            if(setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault)\n                return SkirmishEnemyObjectiveCommanderService.Evaluate(em,session,setup,army);\n            if (setup.EnemyPolicy != null && setup.ObjectiveKind == SkirmishObjectiveKind.BaseAssault)\n                return SkirmishEnemyCommanderService.Evaluate(em, session, setup, army);','            if (setup.CatalogId != "S002" || setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault || setup.EnemyPolicy != null)\n                throw new System.NotSupportedException("Source S002 BaseAssault/null-policy enemy owner only");\n            if (!SkirmishNativeProduction.TrySession(em, out var boundSession) || boundSession != session)\n                throw new System.InvalidOperationException("Real expanded production catalog required; no editor direct-birth fallback");')
 text=once(text,'result = SkirmishNativeProduction.RequestEnemyCounter(em, session,','result = GeneratedEnemyProductionRequests.RequestEnemyCounter(em, session,')
 text=once(text,'                    result = UnityEngine.Application.isPlaying ? 0 :\n                        SkirmishProductionService.TryProduce(em, session, roleId, 1, army, 2, out _) ? 1 : -1;','                    throw new System.InvalidOperationException("Expanded production binding lost during enemy evaluation");')
 text=once(text,'SkirmishExpandedEngagementService.DomainAllows(overlay.TargetDomains, true, SkirmishPopulationCategory.None)','GeneratedEnemyTargetDomain.DomainAllows(overlay.TargetDomains, true, SkirmishPopulationCategory.None)')
 if path.read_bytes()!=raw:raise ValueError('Native enemy source drift during extraction')
 with(output/'SkirmishEnemyStrategySystem.source.cs').open('w',encoding='utf-8',newline='')as file:file.write(text)
 proof={'kind':'CurrentNativeEnemyOwnerBinding','source_inputs':{str(path):hashlib.sha256(raw).hexdigest()},'output_sha256':hashlib.sha256(text.encode()).hexdigest(),'bindings':['Complete native OnCreate/OnUpdate/private queries/perception/strategy/order bodies retained','Only system ordering attributes removed here; finite current-header scheduler admission separately enforces them','S002 BaseAssault/null enemy-policy profile explicit; other commander branches reject, no constant policy substitute','Real expanded catalog required; lost binding rejects instead of Editor direct entity birth','RequestEnemyCounter and DomainAllows route to exact CURRENT native helper projections already materialized separately','Raw read bytes pinned/rechecked; working text/output LF'],'fake_accepted_commands':False};(output/'source-extraction.json').write_text(json.dumps(proof,indent=2))
 if path.read_bytes()!=raw:raise ValueError('Native enemy source drift during output')
 return proof
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
