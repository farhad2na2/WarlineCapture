#!/usr/bin/env python3
"""Prepare a reviewable original-owner S005 production factory capture source."""
import argparse, hashlib, json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / 'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S005FullFactory.cs'
def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def once(text, old, new):
    if text.count(old) != 1: raise ValueError('Expected one original source anchor: ' + old[:100])
    return text.replace(old, new, 1)
def prepare(output):
    source = SOURCE.read_text()
    source = once(source, 'RunS005FullFactoryReferenceData()', 'RunS005ProductionFactoryReferenceData()')
    source = once(source, 'Arg("-skirmishS005FullFactoryData")', 'Arg("-skirmishS005ProductionFactoryData")')
    anchor = 'if (keys.Length != 7 || keys.Any(string.IsNullOrEmpty)) throw new InvalidDataException("Exact seven setup building keys required");'
    extra = '''
            var profile = SkirmishArmyProfileConfig.ResolveCached(setup.ArmyProfileId);
            if (profile == null) throw new InvalidDataException("Actual setup army profile required");
            keys = keys.Concat(setup.RoleOverlays.Where(role => profile.Allows(role.RoleId))
                .Select(role => SkirmishProductionCatalog.ProducerPrefabKey(role.Producer)))
                .Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray();
            if (keys.Any(string.IsNullOrEmpty)) throw new InvalidDataException("Every authored role requires a producer key");'''
    source = once(source, anchor, anchor + extra)
    source = once(source, '"S005OriginalFullFactoryRunwayDefinitions"', '"S005OriginalProductionFactoryRunwayDefinitions"')
    source = once(source, '"full-factory-runway-definitions.json"', '"production-factory-runway-definitions.json"')
    source = once(source, '[S005FullFactoryReference]', '[S005ProductionFactoryReference]')
    output.mkdir(parents=True, exist_ok=False)
    path = output / 'S005ProductionFactory.candidate.cs.txt'; path.write_text(source)
    receipt = {'status':'PreparedNotCompiledOrExecuted','source':str(SOURCE),'source_sha256':digest(SOURCE),'candidate':str(path),'candidate_sha256':digest(path),'generator_sha256':digest(Path(__file__)),'scope':'Original compiler/catalog/definition factory; adds actual allowed role producers to starting keys. No capture, mission or readiness asserted.','mission_ready':False}
    (output / 'preparation.json').write_text(json.dumps(receipt, indent=2) + '\n')
if __name__ == '__main__':
    p=argparse.ArgumentParser(); p.add_argument('output', type=Path); prepare(p.parse_args().output.resolve())
