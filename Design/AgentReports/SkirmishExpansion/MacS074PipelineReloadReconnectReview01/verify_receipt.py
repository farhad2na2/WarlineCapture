"""Read-only package-source/receipt checks; never executes a recovery/query."""
from pathlib import Path
import hashlib
import json
import re

HERE = Path(__file__).resolve().parent
pins = json.loads((HERE / 'source-pins.json').read_text())
root = Path(pins['packageRoot'])
assert len(pins['files']) == 22
for row in pins['files']:
    data = (root / row['path']).read_bytes()
    assert len(data) == row['bytes'] and hashlib.sha256(data).hexdigest() == row['sha256'], row['path']
startup = (root / 'Editor/EditorPipelineStartup.cs').read_text()
for name in ['OnBeforeAssemblyReload', 'OnAfterAssemblyReload']:
    body = re.search(r'private static void ' + name + r'\(\)\s*\{([^}]*)\}', startup).group(1)
    assert not re.sub(r'//[^\n]*', '', body).strip()
assert startup.index('m_Server.Start(cfg?.Port ?? 0)') < startup.index('Commands.AutoTickCommand.RestoreFromSession')
assert '[MenuItem("Window/Pipeline/Start Server")]' in startup
assert '[MenuItem("Window/Pipeline/Stop Server")]' in startup
base = (root / 'Runtime/Common/BasePipelineServer.cs').read_text()
start = base[base.index('public void Start(int port = 0)'):base.index('private void OpenListener()')]
assert start.index('m_Dispatcher.Initialize();') < start.index('OpenListener();') < start.index('CreateInstanceDescriptor();') < start.index('ArmWatchdog();')
catch = start[start.index('catch (Exception)'):]
assert 'm_HttpListener?.Stop();' in catch and '.Close()' not in catch and 'Shutdown()' not in catch
assert 'if (!m_IsRunning)\n                return;' in base
probe = base[base.index('private bool IsPortAvailable(int port)'):]
assert 'new HttpListener()' in probe and 'AddLoopbackPrefixes(listener, port);' in probe
editor = (root / 'Editor/EditorPipelineServer.cs').read_text()
assert 'protected override object GetServerStatus()' in editor and 'UpdateHeartBeat();' in editor
assert 'class EditorPipelineManager : ScriptableObject' in (root / 'Editor/EditorPipelineManager.cs').read_text()
receipt = json.loads((HERE / 'review-receipt.json').read_text())
assert receipt['WindowsEvidence']['unityLog']['bindFailureLine'] == 9390
assert receipt['causalWitness'] is None and receipt['recoveryWitness'] is None
routes = json.loads((HERE / 'supported-routes-and-limits.json').read_text())
assert all(value is None for value in routes['actualResults'].values())
print('PASS22 exact package source pins unchanged, including14 prior Windows-matched pins;8 new Windows comparisons pending')
print('PASS reload/startup/failed-start/port/query source anchors and pending-witness consistency ONLY')
print('NoCLI/network/eval/Editor/compiler/native/process/recovery actions; cause and reconnect=PENDING; acceptance=false')
