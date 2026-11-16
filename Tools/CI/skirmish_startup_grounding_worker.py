"""Build the narrowly scoped original-source Mono grounding batch worker, without Unity or DLL copies."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys
sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[2]
SOURCES = [
    'Tools/CI/SkirmishOfflineSim/StartupGroundingWorker.cs',
    'Tools/CI/SkirmishOfflineSim/StartupGroundingShims.cs',
    'Assets/Game/Scripts/Components/MapSurfaceComponents.cs',
    'Assets/Game/Scripts/Components/MapSurfaceMeshGeometryComponents.cs',
    'Assets/Game/Scripts/Components/MapSurfaceBlobAccess.cs',
    'Assets/Game/Scripts/Components/MapSurfaceSceneOverlaySampling.cs',
    'Assets/Game/Scripts/Systems/SpawnGroundingKernel.cs',
]
DEFAULT_MATH = Path('/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.MathematicsModule.dll')


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def build_worker(output, project=ROOT, math_module=DEFAULT_MATH):
    output, project, math_module = Path(output).resolve(), Path(project).resolve(), Path(math_module).resolve()
    output.mkdir(parents=True, exist_ok=False)
    scripting = math_module.parents[2]
    mono = scripting / 'MonoBleedingEdge/bin/mono'
    if not mono.is_file():
        raise ValueError('Editor-distribution standalone Mono is missing; install the matching Editor runtime (no fallback)')
    runtime = scripting / 'MonoBleedingEdge'
    dependencies = [runtime / 'lib/mono/net_4_x-macos/mscorlib.dll',
                    runtime / 'lib/mono/net_4_x-macos/Facades/netstandard.dll',
                    runtime / 'lib/mono/gac/Mono.Security/4.0.0.0__0738eb9f132ed756/Mono.Security.dll',
                    runtime / 'lib/libmono-native.dylib', runtime / 'etc/mono/config',
                    math_module.parent / 'UnityEngine.SharedInternalsModule.dll']
    for dependency in dependencies:
        if not dependency.is_file():
            raise ValueError('Required bundled Mono runtime dependency is missing: ' + str(dependency))
    refs = [scripting / 'NetStandard/ref/2.1.0/netstandard.dll', scripting / 'NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll', math_module]
    dotnet = Path(shutil.which('dotnet')).resolve()
    sdk = dotnet.parent / 'sdk'
    def version(path):
        return tuple(int(p) for p in path.name.split('.') if p.isdigit())
    candidates = sorted(sdk.glob('*/Roslyn/bincore/csc.dll'), key=lambda p: version(p.parents[2]))
    if not candidates:
        raise ValueError('Installed Roslyn compiler required')
    compiler = candidates[-1]
    pins = [project / p for p in SOURCES] + [Path(__file__).resolve(), mono.resolve(), dotnet, compiler] + refs + dependencies
    before = {str(p): sha(p) for p in pins}
    executable = output / 'StartupGroundingWorker.exe'
    args = ['-nologo', '-target:exe', '-nostdlib+', '-langversion:latest', '-out:' + str(executable)]
    args += ['-r:' + str(p) for p in refs] + [str(project / p) for p in SOURCES]
    if any('"' in a or '\n' in a or '\r' in a for a in args):
        raise ValueError('Unsafe compiler response-file argument')
    response = output / 'compile.rsp'
    response.write_text('\n'.join('"' + a + '"' for a in args) + '\n')
    proc = subprocess.run(['rtk', 'proxy', str(dotnet), str(compiler), '@' + str(response)], capture_output=True, text=True, timeout=120)
    (output / 'compile.log').write_text(proc.stdout + proc.stderr)
    if proc.returncode:
        raise ValueError('Grounding worker compilation failed: ' + str(output / 'compile.log'))
    after = {str(p): sha(p) for p in pins}
    if before != after:
        raise ValueError('Grounding worker build source/runtime drift')
    pinned = before | {str(executable): sha(executable), str(response): sha(response)}
    config = dict(schema_version=1, scope='OriginalCompactAndGeometry_MonoWidenedFloat_BatchV1', mono_path=str(mono.resolve()),
                  worker_path=str(executable), math_path=str(math_module), evidence_root=str(output / 'runs'),
                  runtime_provenance='Editor-distribution bundled standalone Mono; JIT statically linked in executable; managed corelib/facade/security and bundled native/config dependencies pinned',
                  runtime_dependencies=[str(p) for p in dependencies], pins=pinned, native_execution=False, full_mission_initialized=False, player_ready=False)
    path = output / 'worker-config.json'
    path.write_text(json.dumps(config, indent=2) + '\n')
    (output / 'build-result.json').write_text(json.dumps(dict(result='Passed', sources=pinned, copied_dlls=[], native_runs=0), indent=2) + '\n')
    return path
