"""Pin the exact reviewed shared preparation composition; no Assets writes or launch."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'Tools/CI/SkirmishProductionDrafts/SharedNativeProductionPreparationAria70'
INPUTS = (
    ('Design/AgentReports/SkirmishExpansion/S005/aircraft-framing-inventory/CAMERA_EXACT40_ADOPTION_20261015.json', 'dc5231186e48b73e9a9e1f696a8e60502c3fb009fbc66ed360172c52fa4abcc4'),
    ('Tools/CI/SkirmishProductionDrafts/TerminalPresentationProof/manifest.json', '7695828b5f8cb2f46b5d6971fe14af32fd14bcd8afb2b538b6a3244b0a963760'),
    ('Tools/CI/SkirmishProductionDrafts/AircraftCameraTerminalPreparation/manifest.json', '89ff2395822dfd865592c8dac1e49b100a8af7b26cf54deb31de80fb5ffd36a7'),
    ('Tools/CI/SkirmishProductionDrafts/AriaBoardingFootprintSuccessor/manifest.json', '39069f18b963b76881d019f0d31434c093471831d74cd8f9aa70b2e29ac9301f'),
    ('Tools/CI/SkirmishProductionDrafts/Ground30SimultaneousRefinement/manifest.json', 'e118f7b1644adb658ff9edf645865f902953efd9ba5b44de08f09347984bbd94'),
    ('Tools/CI/SkirmishProductionDrafts/SharedNativeProductionPreparationAria70/manifest.json', 'f20cad0d911e994193b18e2246d58432be4126ed5ae7a1e6bf773f31fe044deb'),
)
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()


def main():
    rows=[]; seen=set(); inputs=[]
    camera15=json.loads((ROOT/'Tools/CI/SkirmishProductionDrafts/MeasuredRotorAircraftFramingLodOwnership/manifest.json').read_text())
    camera15_before={r['target']:r['before_sha256'] for r in camera15['files']}
    for name,digest in INPUTS:
        path=ROOT/name
        if sha(path)!=digest: raise ValueError('Frozen input changed: '+name)
        inputs.append(dict(path=name,sha256=digest))
        data=json.loads(path.read_text()); camera='targets' in data
        for item in data['targets'] if camera else data['files']:
            target=item['target']; current=ROOT/target
            before=sha(current) if current.is_file() else None
            if target in seen: raise ValueError('Unexpected cross-map source collision: '+target)
            seen.add(target)
            if camera:
                expected_before=camera15_before.get(target,before)
            else: expected_before=item['before_sha256']
            if before!=expected_before or sha(ROOT/item['source'])!=item['sha256']:
                raise ValueError('Current before/source pin changed: '+target)
            rows.append(dict(target=target,source=item['source'],before_sha256=before,
                sha256=item['sha256'],origin_manifest=name))
    if len(rows)!=129 or sum(r['target'].endswith('.cs') for r in rows)!=127:
        raise ValueError('Exact camera40+Terminal4+cameraPrep1+ARIA58+Q25+shared1 required')
    enemy_path='Tools/CI/SkirmishProductionDrafts/EnemyAirCombined13/manifest.json'
    enemy_digest='deb0689a63810b749d5d0f1e8c625bf64df3694167933c92b1f4790cb34f510e'
    if sha(ROOT/enemy_path)!=enemy_digest: raise ValueError('Compiler predecessor changed')
    old=next(r for r in json.loads((ROOT/enemy_path).read_text())['files'] if r['target'].endswith('/SkirmishSetupCompiler.cs'))
    new=next(r for r in rows if r['target'].endswith('/SkirmishSetupCompiler.cs'))
    if old['sha256']!='ccaddb274a722793042524d1bd847d1a0381baa2122caab154084b763dff0fb5' or new['sha256']!='cee4f1f9d8da693b6052c827173b590ff0b499dfae2d4efabe575b00c16d7b3f':
        raise ValueError('Only reviewed Enemy13 to tower-composed compiler lineage accepted')
    inputs.append(dict(path=enemy_path,sha256=enemy_digest))
    result=dict(schema_version=1,baseline='f733f9763',target_count=len(rows),source_count=127,
        files=rows,inputs=inputs,unexpected_target_collisions=[],
        compiler_lineage=dict(target=new['target'],superseded_sha256=old['sha256'],selected_sha256=new['sha256']),
        scope='Strict isolated stable-main source closure probe; no implicit current WIP Assets fallback',
        native_execution=False,queue_success_enabled=False,stable_main_closure='Pending strict compile')
    (OUT/'composition.manifest.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(manifest=str(OUT/'composition.manifest.json'),sha256=sha(OUT/'composition.manifest.json'),targets=len(rows),sources=127)))


if __name__=='__main__': main()
