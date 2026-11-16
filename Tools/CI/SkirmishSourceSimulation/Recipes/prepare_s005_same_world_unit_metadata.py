#!/usr/bin/env python3
"""Join current S005 unit metadata and source templates to retained startup."""
import argparse,hashlib,importlib.util,json
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
spec=importlib.util.spec_from_file_location("recurring",HERE/"prepare_s005_recurring_source_owners.py")
recurring=importlib.util.module_from_spec(spec);spec.loader.exec_module(recurring)
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def prepare(out):
    epoch=recurring.storage.strategy.ground.host.start.roads.groups.epoch
    epoch.CURRENT_BUILD=ROOT/"T Logs/SkirmishProduction/source-unit-domain-s002-build-20261105-T4/build.json"
    epoch.APPROVED_EXTRA_CHANGES={str(ROOT/x) for x in (
        "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.UnitData.cs",
        "Tools/CI/SkirmishSourceSimulation/Adapters/413_GeneratedUnitProductionMetadata.cs",
        "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json")}
    epoch.INPUT_OVERRIDES={
        "T Logs/SkirmishProduction/s005-building-factory-capture-20261105-T2/full-factory-runway-definitions.json":"Logs/SkirmishProduction/s005-current-factory-capture-20261105-T3/full-factory-runway-definitions.json",
        "T Logs/SkirmishProduction/s005-configured-readmodel-capture-20261105-T1/configured-spawnable-readmodel.json":"Logs/SkirmishProduction/s005-current-readmodel-capture-20261105-T2/configured-spawnable-readmodel.json",
        "T Logs/SkirmishProduction/s005-prefab-object-reference-capture-20261105-T2/s005-prefab-object-references.json":"Logs/SkirmishProduction/s005-combined-prefab-capture-20261105-T1/selected-unit-prefab-object-references.json",
        "T Logs/SkirmishProduction/s005-prefab-object-projection-20261105-T2/projection.json":"T Logs/SkirmishProduction/s005-combined-prefab-projection-20261105-T1/projection.json"}
    recurring.prepare(out)
    once=epoch.roster.base.once
    helper=out/"helper.cs";text=helper.read_text()
    text=once(text,'templates.Prefabs.Count==16','templates.Prefabs.Count==23')
    data=ROOT/"Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4/unit-production-metadata.json"
    receipt=data.with_name("receipt.json");identity=data.with_name("unit-prefab-identity-readback.json")
    log=ROOT/"T Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4-editor.log"
    anchor='            var strategySubset=retainedWorld.RunExpandedRecurringOwnerDiagnostic('
    block='''            string unitData=Path.GetFullPath("DATA_PATH"),unitReceipt=Path.GetFullPath("RECEIPT_PATH"),unitLog=Path.GetFullPath("LOG_PATH"),unitIdentity=Path.GetFullPath("IDENTITY_PATH");
            var unitAuthority=SourceAuthoredUnitMetadataAuthority.FromCapture(setup,unitData,"DATA_SHA",unitReceipt,"RECEIPT_SHA",unitLog,Path.GetFullPath("."));
            bool wrongIdentityRejected=false;
            try { unitAuthority.ReadIndependentIdentities(unitData); } catch(InvalidOperationException) { wrongIdentityRejected=true; }
            Need(wrongIdentityRejected && retainedWorld.ActualUnitMetadata==null,"Wrong independent identity must fail before metadata binding");
            var unitBinding=retainedWorld.BindAuthoredSourceUnitMetadata(unitAuthority,unitData,"DATA_SHA",unitIdentity,Path.GetFullPath("."),templates.Prefabs);
            bool duplicateUnitBindingRejected=false;
            try { retainedWorld.BindAuthoredSourceUnitMetadata(unitAuthority,unitData,"DATA_SHA",unitIdentity,Path.GetFullPath("."),templates.Prefabs); }
            catch(InvalidOperationException) { duplicateUnitBindingRejected=true; }
            Need(duplicateUnitBindingRejected,"Duplicate unit metadata binding must reject");
'''
    for key,value in {"DATA_PATH":str(data.relative_to(ROOT)),"RECEIPT_PATH":str(receipt.relative_to(ROOT)),"LOG_PATH":str(log.relative_to(ROOT)),
                      "IDENTITY_PATH":str(identity.relative_to(ROOT)),"DATA_SHA":sha(data),"RECEIPT_SHA":sha(receipt)}.items():block=block.replace(key,value)
    text=once(text,anchor,block+anchor)
    text=once(text,'sourceHost=new JObject { ["storage_binding"]=storageBinding,',
              'sourceHost=new JObject { ["unit_metadata_binding"]=unitBinding,["wrong_unit_identity_rejected"]=wrongIdentityRejected,["duplicate_unit_binding_rejected"]=duplicateUnitBindingRejected,["storage_binding"]=storageBinding,')
    helper.write_text(text)
    rsp=out/"compile.rsp";args=[json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    oldLoader=ROOT/"T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004DiagnosticUnitMetadataMaterializer.cs"
    loader=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/413_GeneratedUnitProductionMetadata.cs"
    oldIdentity=HERE/"S005IdentityImportExperiment.cs.txt"
    identitySource=ROOT/"T Logs/SkirmishProduction/s005-combined-prefab-control-20261105-T1/SourceIdentityHandles.cs"
    authority=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/SourceAuthoredUnitMetadataAuthority.cs"
    for old,new in ((oldLoader,loader),(oldIdentity,identitySource)):
        if args.count(str(old))!=1 or str(new) in args:raise ValueError("Exact same-world source binding required: "+str(old))
        args[args.index(str(old))]=str(new)
    if str(authority) in args:raise ValueError("Duplicate authority source")
    args.append(str(authority));rsp.write_text("".join(json.dumps(arg)+"\n" for arg in args))
    prep=out/"preparation.json";doc=json.loads(prep.read_text())
    capture=ROOT/"Logs/SkirmishProduction/s005-combined-prefab-capture-20261105-T1/selected-unit-prefab-object-references.json"
    raw=json.loads(capture.read_text())
    for row in ("capture_assembly","entities_assembly"):
        assembly=Path(raw[row]["path"])
        if sha(assembly)!=raw[row]["sha256"]:raise ValueError("Capture assembly changed: "+str(assembly))
        doc["source_pins"][str(assembly)]=sha(assembly)
    for path in (helper,rsp,loader,authority,identitySource,data,receipt,identity,log,capture,Path(__file__).resolve()):
        doc["source_pins"][str(path)]=sha(path)
    doc["unit_binding_scope"]="Selected source metadata and imported templates in retained world; no production delivery or native readiness"
    prep.write_text(json.dumps(doc,indent=2)+"\n")
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())
