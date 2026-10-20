"""Read-only receipt/raw-source verification. Does not load Unity or evaluate a runtime cause."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
ROOT = next(p for p in HERE.parents if (p / 'Assets').is_dir())
pins = json.loads((HERE / 'source-pins.json').read_text())
cache = Path(pins['packageRoot'])
for group, root in [('packageFiles', cache), ('projectFiles', ROOT)]:
    for row in pins[group]:
        data = (root / row['path']).read_bytes()
        assert len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], row['path']
        if row.get('windowsExpected'):
            assert row['sha256'] == row['windowsExpected'] and row['windowsExactMatch'] is True
print('PASS raw pins unchanged:14 local package files,5 exact-base project files;2 supplied Windows files exact')
loader = (cache / 'Unity.Scenes.Editor/EntitySceneBuildUtility.cs').read_text()
assert 'Scene dstScene = CompanionGameObjectUtility.GetCompanionScene(false);' in loader
assert 'LoadSerializedFileAndForgetAsync(loadPath, 1, 0UL, -1L, dstScene)' in loader
assert 'forceLoadedObjects = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(loadPath);' in loader
assert 'in the editor, it is best to not unload anything' in loader
assert 'var ls = new LoadState(handle.ObjectId);' in loader
utility = (cache / 'Unity.Scenes/Hybrid/CompanionSceneUtility.cs').read_text()
assert 'CreateCompanionScenes();' in utility and 'beforeAssemblyReload += ' in utility
batch = (ROOT / 'Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.ProductionBatch.cs').read_text()
assert 'PrepareNativeSceneBaseline(jobs.Select(' in batch
assert 'Flags=SceneLoadFlags.BlockOnImport|SceneLoadFlags.BlockOnStreamIn' in batch
normal = (ROOT / 'Assets/Game/Scripts/Composition/OperationMapPackedEntitySceneOwnership.cs').read_text()
assert 'sceneEntity = SceneSystem.LoadSceneAsync(world.Unmanaged, sceneGuid);' in normal
observations = json.loads((HERE / 'observation-invariants.json').read_text())
assert all(value is None for value in observations['actualWitness'].values())
assert json.loads((HERE / 'source-review.json').read_text())['WindowsEvidenceAttribution']['localReproduction'] is False
print('PASS reviewed source anchors and pending-witness receipt consistency ONLY')
print('Compiler=NOT_RUN; Unity/native=NOT_RUN; causal root attribution=PENDING; repair=PENDING; acceptance=false')
