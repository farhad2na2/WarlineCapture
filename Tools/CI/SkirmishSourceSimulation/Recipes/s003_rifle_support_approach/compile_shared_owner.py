"""Compile the changed shared owner in the retained S003 source closure, without admission."""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--prior-build', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[5]
    prior = args.prior_build.resolve(strict=True)
    response = prior / 'compile.rsp'
    generated = prior / 'source/generated/S003RifleSupportApproach.cs'
    source = repo / 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs'
    compiler = Path('/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll')
    dotnet = Path('/usr/local/share/dotnet/dotnet')
    inputs = [response, generated, source, compiler, dotnet]
    pins = {str(path): sha(path) for path in inputs}
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    text = response.read_text()
    before_output = '-out:' + str(prior / 'SourceSimulation.exe')
    after_output = '-out:' + str(output / 'SourceSimulation.exe')
    if text.count(before_output) != 1 or text.count(str(generated)) != 1:
        raise ValueError('Exact retained compiler response required')
    text = text.replace(before_output, after_output).replace(str(generated), str(source))
    rsp = output / 'compile.rsp'
    rsp.write_text(text)
    result = subprocess.run([str(dotnet), str(compiler), '@' + str(rsp)],
                            cwd=repo, capture_output=True, text=True, check=False)
    (output / 'compile.log').write_text(result.stdout + result.stderr)
    stable = all(sha(Path(name)) == value for name, value in pins.items())
    receipt = {'kind': 'S003SharedOwnerCompilation', 'input_sha256': pins,
               'compiler_exit': result.returncode, 'pinned_inputs_unchanged': stable,
               'scope': 'Compile-only retained generated closure; no runtime admission, native parity or readiness',
               'mission_ready': False}
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))
    if result.returncode or not stable:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
