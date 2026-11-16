"""Persist source-run pin evidence independently of outcome classification."""
import hashlib
import json
from pathlib import Path


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def write_new(path, value):
    with Path(path).open('x',encoding='utf-8') as stream:
        json.dump(value,stream,indent=2,sort_keys=True)
        stream.write('\n')


def before(output, input_pins, build_pins, arguments, executable):
    output=Path(output).resolve(strict=True)
    generated={}
    for argument in arguments:
        if argument.startswith('-') or not argument.endswith('.cs'): continue
        path=Path(argument).resolve(strict=True)
        if path.parent==output:
            generated[str(path)]=sha(path)
    if not generated: raise ValueError('Generated source projection evidence required')
    manifest={'scope':'PreWorkerSourceAndGeneratedPins_NotMissionOrReadiness',
              'input_sha256':input_pins,'build_sha256':build_pins,'generated_source_sha256':generated,
              'compile_rsp_sha256':sha(output/'compile.rsp'),'compiled_executable_path':str(Path(executable).resolve(strict=True)),
              'compiled_executable_sha256':sha(executable),
              'native_parity':False,'mission_ready':False}
    write_new(output/'source-pins-before.json',manifest)
    return manifest


def after(output, manifest):
    output=Path(output).resolve(strict=True)
    changed=[path for path,digest in manifest['generated_source_sha256'].items()
             if not Path(path).is_file() or sha(path)!=digest]
    if changed: raise ValueError('Generated source projection changed during worker: '+changed[0])
    if sha(output/'compile.rsp')!=manifest['compile_rsp_sha256']:
        raise ValueError('Compile response changed during worker')
    if sha(manifest['compiled_executable_path'])!=manifest['compiled_executable_sha256']:
        raise ValueError('Compiled executable changed during worker')
    result={'scope':'PostWorkerPinChecksOnly_NotRunnerOrMissionPass',
            'before_manifest_sha256':sha(output/'source-pins-before.json'),
            'generated_sources_rechecked':len(manifest['generated_source_sha256']),
            'input_pins_rechecked':len(manifest['input_sha256']),
            'build_pins_rechecked':len(manifest['build_sha256']),
            'native_parity':False,'mission_ready':False}
    write_new(output/'source-pins-after.json',result)
    return result
