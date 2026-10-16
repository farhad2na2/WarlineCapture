"""One Editor overlay on the exact four-owner UI-storage authority; no Unity."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[4]
folder = Path(__file__).resolve().parent
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
parent = root / 'Logs/SkirmishProduction/Offline/AriaUiOwnedStorage/owning-142sui8j'
authority = parent / 'result.json'
assert sha(authority) == 'aa4175bd18278194117ae1cfb394b1626f597b44715ad5586ba322c478c2b8d6'
prior = json.loads(authority.read_text())
assert prior['result'] == 'PassedFourOwningAssemblies'
record = next(r for r in prior['records'] if r['owner'] == 'Game.Editor')
manifest = folder / 'manifest.json'
rows = json.loads(manifest.read_text())['files']
assert len(rows) == 1
row = rows[0]
assert sha(root / row['target']) == row['before_sha256']
assert sha(root / row['source']) == row['sha256']
pins = {str(authority): sha(authority), str(manifest): sha(manifest), str(Path(__file__)): sha(Path(__file__)),
        str(root / row['target']): row['before_sha256'], str(root / row['source']): row['sha256']}
for source in record['sources'].values():
    path = Path(source['path'])
    assert sha(path) == source['sha256']
    pins[str(path)] = source['sha256']
for path, expected in record['references'].items():
    assert sha(path) == expected
    pins[path] = expected
output = Path(tempfile.mkdtemp(prefix='measured-opening-', dir=parent.parent))
source = output / Path(row['target']).name
shutil.copyfile(root / row['source'], source)
response = parent / 'Game.Editor.rsp'
pins[str(response)] = sha(response)
lines = response.read_text().splitlines()
old = '"' + record['sources'][row['target']]['path'] + '"'
assert lines.count(old) == 1
lines = ['"' + str(source) + '"' if line == old else line for line in lines
         if not line.lower().startswith(('-out:', '-refout:'))]
lines.extend(['-out:"' + str(output / 'Game.Editor.dll') + '"',
              '-refout:"' + str(output / 'Game.Editor.ref.dll') + '"'])
rsp = output / 'Game.Editor.rsp'
rsp.write_text('\n'.join(lines) + '\n', encoding='utf-8')
dotnet = shutil.which('dotnet')
sdk = subprocess.run(['rtk', 'proxy', dotnet, '--list-sdks'], capture_output=True, text=True, check=True).stdout.splitlines()[0]
version, location = sdk.split(' ', 1)
csc = Path(location.strip('[]')) / version / 'Roslyn/bincore/csc.dll'
pins[str(csc)] = sha(csc)
result = subprocess.run(['rtk', 'proxy', dotnet, str(csc), '@' + str(rsp)], cwd=root, capture_output=True, text=True)
(output / 'Game.Editor.log').write_text(result.stdout + result.stderr, encoding='utf-8')
mismatches = [path for path, expected in pins.items() if not Path(path).exists() or sha(path) != expected]
assert sha(source) == row['sha256']
passed = result.returncode == 0 and not mismatches
receipt = dict(result='PassedFocusedEditorMeasuredOpening' if passed else 'Failed',
               parent_authority=str(authority), parent_sha256=sha(authority), manifest_sha256=sha(manifest),
               explicit_target=row['target'], explicit_source_sha256=row['sha256'],
               source_count=len(record['sources']), reference_count=len(record['references']),
               input_sha256=pins, stability_mismatches=mismatches, exit_code=result.returncode,
               native_executed=False, tests_executed=False, fallback_used=False,
               scope='Exact parent Editor sources and built references, one new QA source overlay only')
(output / 'result.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(result=receipt['result'], output=str(output), mismatches=mismatches)))
raise SystemExit(0 if passed else 1)
