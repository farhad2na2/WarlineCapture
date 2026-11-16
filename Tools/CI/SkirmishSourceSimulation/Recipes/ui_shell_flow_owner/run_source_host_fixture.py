from __future__ import annotations

import hashlib
import argparse
import json
import pathlib
import re
import shutil
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[5]
HERE = pathlib.Path(__file__).resolve().parent
FIXTURE_INPUTS = HERE / 'fixture'
OWNER = ROOT / 'Assets/Game/Scripts/UI/Shell/Ecs'


def declaration(path: pathlib.Path, name: str) -> str:
    text = path.read_text()
    match = re.search(r'\b(?:struct|enum)\s+' + re.escape(name) + r'\b', text)
    if not match:
        raise RuntimeError(f'missing {name} in {path}')
    start = text.rfind('\n', 0, match.start()) + 1
    brace = text.find('{', match.end())
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == '{':
            depth += 1
        elif text[index] == '}':
            depth -= 1
            if depth == 0:
                end = index + 1
                while end < len(text) and text[end] in '\r\n':
                    end += 1
                return text[start:end].rstrip()
    raise RuntimeError(f'unbalanced declaration {name} in {path}')


def main(args: list[str]) -> int:
    parser = argparse.ArgumentParser(description='Compile and run the original UiShellFlowSystem in a source-host fixture.')
    parser.add_argument('output_dir', type=pathlib.Path, help='New evidence directory; existing directories are rejected.')
    options = parser.parse_args(args)
    fixture = options.output_dir.resolve()
    basis_build = ROOT / 'Logs/SkirmishProduction/s073-original-aria-probe-build-20261103-r3/build.json'
    if not basis_build.is_file():
        raise RuntimeError(f'Pinned basis build is missing: {basis_build}')
    if fixture.exists():
        raise RuntimeError(f'Fresh output directory required; preserving existing evidence: {fixture}')
    fixture.mkdir(parents=True)
    owner_files = [OWNER / 'UiShellFlowSystem.cs', OWNER / 'UiShellFlowSystem.Requests.cs', OWNER / 'UiShellStartupFlow.cs']
    for source in owner_files:
        shutil.copyfile(source, fixture / source.name)
    audio_gateway = ROOT / 'Assets/Game/Scripts/UI/Components/UIAudioEventGateway.cs'
    shutil.copyfile(audio_gateway, fixture / audio_gateway.name)
    audio_ids = ROOT / 'Assets/Game/Scripts/Configs/Audio/AudioEventIds.Constants0.cs'
    shutil.copyfile(audio_ids, fixture / audio_ids.name)
    for source in sorted(FIXTURE_INPUTS.glob('*.cs')):
        shutil.copyfile(source, fixture / source.name)

    wanted = {
        ROOT / 'Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs': [
            'UiShellMode', 'UiShellTransitionPhase', 'MatchIntroTransitionStateKind', 'UiShellRouteIntent',
            'UiShellCommandKind', 'UiShellRegionId',
        ],
        ROOT / 'Assets/Game/Scripts/UI/Contracts/UIRoute.cs': ['UIRoute'],
        ROOT / 'Assets/Game/Scripts/UI/Contracts/UiShellPopupContracts.cs': ['UiShellPopupKind', 'UiShellPopupIntent'],
        OWNER / 'Contracts/UiShellEcsComponents.cs': [
            'UiShellStartupDisposition', 'UiShellStartupDispositionComponent', 'UiShellStateComponent',
            'UiShellLoadingProgressComponent', 'UiShellLoadingProgressRequestComponent',
            'MatchIntroTransitionComponent', 'UiShellActivePopupComponent', 'UiShellRouteRequestComponent',
            'UiShellRouteHistoryComponent', 'UiShellPopupRequestComponent',
            'UiShellPresentationCommandComponent', 'UiShellTransitionCompleteComponent',
        ],
    }
    parts = [
        'using Unity.Collections;\nusing Unity.Entities;\nnamespace Game.UI.Contracts {',
        '\n'.join(declaration(path, name) for path, names in wanted.items() if 'UI/Contracts/' in str(path) for name in names),
        '}\nnamespace Game.UI.Shell.Contracts.Ecs { using Game.UI.Contracts;',
        '\n'.join(declaration(path, name) for path, names in wanted.items() if 'Shell/Ecs/Contracts/' in str(path) for name in names),
        '}',
    ]
    (fixture / 'ExtractedContracts.cs').write_text('\n\n'.join(parts) + '\n')

    sources = [
        fixture / 'ExtractedContracts.cs',
        fixture / 'EcsShim.cs',
        fixture / 'SideEffectBoundaries.cs',
        *[fixture / p.name for p in owner_files],
        fixture / audio_gateway.name,
        fixture / audio_ids.name,
        fixture / 'FixtureProgram.cs',
    ]
    out = fixture / 'UiShellFlowSourceHost.dll'
    rsp = fixture / 'compile.rsp'
    framework_refs = sorted(pathlib.Path('/usr/local/share/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.5/ref/net10.0').glob('*.dll'))
    rsp.write_text('\n'.join(['-nologo', '-langversion:latest', '-nostdlib+', '-target:exe', '-main:FixtureProgram', f'-out:{out}', *[f'-r:{p}' for p in framework_refs], *[str(p) for p in sources]]) + '\n')
    attempt = 1
    log = fixture / f'compile-attempt-{attempt}.log'
    compiler = pathlib.Path('/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll')
    result = subprocess.run(['/usr/local/share/dotnet/dotnet', str(compiler), '@' + str(rsp)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    log.write_text(result.stdout)
    manifest = {
        'fixture_kind': 'source-host controlled fixture; no Unity or gameplay execution',
        'recipe': str(pathlib.Path(__file__).resolve()),
        'recipe_sha256': hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest(),
        'basis_build': str(basis_build),
        'basis_build_sha256': hashlib.sha256(basis_build.read_bytes()).hexdigest(),
        'owner_sources': [{'path': str(p), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in owner_files],
        'extracted_contract_sources': {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in wanted},
        'side_effect_sources': [{'path': str(p), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in [audio_gateway, audio_ids]],
        'checked_in_fixture_inputs': [{'path': str(p), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(FIXTURE_INPUTS.glob('*.cs'))],
        'fixture_sources': [{'path': str(p), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in sources],
        'compiler': str(compiler),
        'compiler_sha256': hashlib.sha256(compiler.read_bytes()).hexdigest(),
        'framework_reference_pins': [{'path': str(p), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest(), 'bytes': p.stat().st_size} for p in framework_refs],
        'script_assembly_references': 0,
        'compiled_game_assembly_references': 0,
        'compile_response_sha256': hashlib.sha256(rsp.read_bytes()).hexdigest(),
        'compile_exit': result.returncode,
        'compile_attempt': attempt,
        'compile_log': str(log),
        'compile_log_sha256': hashlib.sha256(log.read_bytes()).hexdigest(),
        'compiled_output': str(out) if out.exists() else None,
        'fixture_result': None,
        'boundaries': ['UiShellEcsGateway.RegisterAsRuntimeGateway observed only', 'music enqueue captured only', 'UI audio bridge enqueue captured only'],
        'completion_policy': 'No completion is synthesized. Test injects a completion only as an explicit named fixture input.',
    }
    (fixture / 'provenance.json').write_text(json.dumps(manifest, indent=2) + '\n')
    if result.returncode:
        print(result.stdout, end='')
        return result.returncode
    runtime_config = fixture / 'UiShellFlowSourceHost.runtimeconfig.json'
    runtime_config.write_text(json.dumps({'runtimeOptions': {'tfm': 'net10.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '10.0.5'}}}) + '\n')
    run = subprocess.run(['/usr/local/share/dotnet/dotnet', str(out)], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (fixture / 'run.log').write_text(run.stdout)
    manifest['fixture_result'] = {'exit_code': run.returncode, 'log': str(fixture / 'run.log'), 'output': run.stdout.splitlines()}
    (fixture / 'provenance.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(run.stdout, end='')
    return run.returncode


if __name__ == '__main__':
    try:
        raise SystemExit(main(sys.argv[1:]))
    except Exception as exc:
        print(f'[UiShellFlowSourceHost] result=Failed error={exc}', file=sys.stderr)
        raise SystemExit(1)
