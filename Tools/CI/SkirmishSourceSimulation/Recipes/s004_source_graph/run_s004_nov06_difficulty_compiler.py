#!/usr/bin/env python3
"""Run the original S004 difficulty compiler on the current source closure."""
import argparse
import json
from pathlib import Path

import run_s004_current_difficulty_compiler as original

ROOT = original.ROOT
TERMINAL = ROOT / 'T Logs/SkirmishProduction/s004-nov06-cached-terminal-20261106-T1'
BUILD = ROOT / 'T Logs/SkirmishProduction/s004-current-role-refresh-build-20261106-T1/build.json'
PROBE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/SourceS004DifficultyCompilerProbe.cs'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--response-directory', type=Path, required=True)
    args = parser.parse_args()
    receipt = json.loads((TERMINAL / 'receipt.json').read_text())
    if (receipt.get('mission_ready') is not False or receipt.get('runtime_admission') is not False or
        original.sha(TERMINAL / 'compile.log') != receipt.get('compile_log_sha256')):
        raise ValueError('Exact successful source-only terminal compile evidence required')
    lines = [json.loads(x) for x in (TERMINAL / 'compile.rsp').read_text().splitlines() if x.strip()]
    outputs = [i for i, value in enumerate(lines) if value.startswith('-out:')]
    mains = [i for i, value in enumerate(lines) if value.startswith('-main:')]
    if len(outputs) != 1 or len(mains) != 1 or lines[mains[0]] != '-main:S004BundleDrivenEntry':
        raise ValueError('Exact source host compiler response required')
    response = args.response_directory.resolve()
    response.mkdir(parents=True, exist_ok=False)
    lines[outputs[0]] = '-out:' + str(response / 'fixture.exe')
    lines[mains[0]] = '-main:SourceS004DifficultyCompilerProbe'
    if str(PROBE) in lines:
        raise ValueError('Difficulty probe must not already be present')
    lines.append(str(PROBE))
    (response / 'compile.rsp').write_text(''.join(json.dumps(x) + '\n' for x in lines))
    original.PRIOR, original.BUILD, original.OUT = response, BUILD, args.output.resolve()
    original.main()


if __name__ == '__main__':
    main()
