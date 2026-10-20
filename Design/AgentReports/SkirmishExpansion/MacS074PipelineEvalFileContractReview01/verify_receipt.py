"""Read-only source/receipt checks. Does not invoke CLI, eval, compiler or Unity."""
from pathlib import Path
import hashlib
import json

HERE = Path(__file__).resolve().parent
pins = json.loads((HERE / 'source-pins.json').read_text())
root = Path(pins['packageRoot'])
for row in pins['files']:
    data = (root / row['path']).read_bytes()
    assert len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], row['path']
for row in pins['skillReferences']:
    data = Path(row['path']).read_bytes()
    assert len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256']
cli = pins['cli']
hash_value = hashlib.sha256()
with Path(cli['resolvedPath']).open('rb') as stream:
    for block in iter(lambda: stream.read(1024 * 1024), b''):
        hash_value.update(block)
assert hash_value.hexdigest() == cli['sha256'] and cli['invoked'] is False
command = (root / 'Runtime/Commands/CodeEvalCommand.cs').read_text()
assert 'System.IO.File.ReadAllText(file)' in command
assert 'return EvaluateSource(code, timeout, "eval_file")' in command
compiler = (root / 'Runtime/Compilation/EvalCodeCompiler.cs').read_text()
assert 'Guid.NewGuid().ToString("N").Substring(0, 8)' in compiler
assert 'AssemblyName = $"PipelineEval_{id}"' in compiler
assert 'public static object Execute()' in compiler and 'var result = method.Invoke(null, null)' in compiler
assert 'Output =' not in compiler
server = (root / 'Runtime/Common/BasePipelineServer.cs').read_text()
assert 'CommandExecutionResponse.CmdSuccess(commandRequest.Command, result)' in server
assert 'workItem.TryCancel();' in (root / 'Runtime/Common/Dispatcher.cs').read_text()
receipt = json.loads((HERE / 'review-receipt.json').read_text())
observed = receipt['windowsObservedEnvelope']
assert observed['evalSuccessPath'] == 'data.result.success'
assert observed['userReturnPath'] == 'data.result.result' and observed['extraResultLayer'] is False
assert receipt['windowsObserverOwnership']['sourceSha256'] == '496621762e723e9eda6875e03d7ad4d0a2f0645f4038ea7fe73cedc15605d73c'
states = json.loads((HERE / 'check-arm-disarm-contract.json').read_text())
assert all(value is None for value in states['actualReceipts'].values())
print('PASS14 package files,2 skill references and exact installed CLI binary raw pins unchanged')
print('PASS source anchors and attributed Windows DTO/stored-original-Action receipt consistency ONLY')
print('MacCLI/eval/compiler/Unity/native=NOT_RUN; callback/timeout/reload controls=PENDING; acceptance=false')
