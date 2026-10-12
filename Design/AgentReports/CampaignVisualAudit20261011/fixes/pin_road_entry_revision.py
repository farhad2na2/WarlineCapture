from pathlib import Path
import hashlib, json, re
report=Path(__file__).resolve().parent
root=report.parents[3]
ledger=dict(line.split("=",1) for line in (report/"render-reconciliation.txt").read_text().splitlines())
previous=dict(line.split("=",1) for line in (report/"render-reconciliation-before-road-entries.txt").read_text().splitlines())
assert {k:v for k,v in ledger.items() if k!="sourceSha256"}=={k:v for k,v in previous.items() if k!="sourceSha256"}, "Render row changes require separate reconciliation"
scene=root/"Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
assert hashlib.sha256(scene.read_bytes()).hexdigest()==ledger["sourceSha256"]
log=(report/"validation/road-entry-grade-01.log").read_text(errors="replace")
assert "[CampaignRoadEntries] result=Passed" in log
assert "[ExistingEditorValidation] result=Passed" in (report/"validation/road-entry-grade-02.log").read_text(errors="replace")
assert "[CampaignRoadMeshReuse] result=Passed" in (report/"validation/road-mesh-reuse-01.log").read_text(errors="replace")
receipts=list((report/"road-entry-grade-01").glob("*.txt"));assert len(receipts)==11
counts={p.stem:int(p.read_text().splitlines()[0].split("=")[1]) for p in receipts}
assert counts["DenseCity"]>0
code=root/"Assets/Game/Scripts/Editor/OperationMapRenderEligibilityInventoryProbe.cs"
s=code.read_text();assert s.count(previous["sourceSha256"])==1
code.write_text(s.replace(previous["sourceSha256"],ledger["sourceSha256"]))
(report/"road-entry-reconciliation.json").write_text(json.dumps(dict(previousSourceSha256=previous["sourceSha256"],sourceSha256=ledger["sourceSha256"],sourceRows=124305,renderRowsAdded=0,renderRowsRemoved=0,gradedMeshes=counts,packedParity="pending"),indent=2)+"\n")
print(json.dumps(dict(pinnedSource=ledger["sourceSha256"],sourceRows=124305,gradedMeshes=counts)))
