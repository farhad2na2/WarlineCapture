#!/usr/bin/env python3
"""Read actual S005 role producer requirements from the retained source world."""
import argparse, hashlib, importlib.util, json
from pathlib import Path
HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("metadata",HERE/"prepare_s005_same_world_unit_metadata.py")
metadata=importlib.util.module_from_spec(spec);spec.loader.exec_module(metadata)
def prepare(out):
    metadata.prepare(out)
    helper=out/"helper.cs"; text=helper.read_text()
    anchor="            var strategySubset=retainedWorld.RunExpandedRecurringOwnerDiagnostic("
    block='''            var producerCensus = new JArray();
            foreach (var role in setup.RoleOverlays.Where(role => scenarioDefinition.ArmyProfileConfig.Allows(role.RoleId)))
            {
                string unitKey = SkirmishRoleCatalogConfig.RuntimePrefabKey(role.RoleKind);
                string producerKey = GeneratedProductionRecipeSource.ProducerPrefabKey(role.Producer);
                producerCensus.Add(new JObject {
                    ["unit_key"] = unitKey, ["producer_key"] = producerKey,
                    ["producer_asset_bound"] = definitions.Assets.ContainsKey(producerKey),
                    ["producer_definition_bound"] = definitions.Definitions.Any(d => d.Prefab != null && d.Prefab.name == producerKey),
                    ["unit_metadata_bound"] = retainedWorld.ActualUnitMetadata.Ordered.Any(row => row.Prefab.name == unitKey)
                });
            }
'''
    if text.count(anchor)!=1: raise ValueError("Unique recurring owner anchor required")
    text=text.replace(anchor,block+anchor,1)
    anchor2='sourceHost=new JObject { ["unit_metadata_binding"]=unitBinding,'
    if text.count(anchor2)!=1: raise ValueError("Unique receipt anchor required")
    helper.write_text(text.replace(anchor2,'sourceHost=new JObject { ["producer_census"]=producerCensus,["unit_metadata_binding"]=unitBinding,',1))
    path=out/"preparation.json"; doc=json.loads(path.read_text())
    for item in (helper,Path(__file__).resolve()): doc["source_pins"][str(item)]=hashlib.sha256(item.read_bytes()).hexdigest()
    doc["producer_census_scope"]="Read-only original role mapping and captured definitions; no catalog, queue or readiness assertion"
    path.write_text(json.dumps(doc,indent=2)+"\n")
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())
