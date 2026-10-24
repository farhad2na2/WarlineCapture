"""Reproducible retained causal-prefix Mono host; no trace admission or simulation inputs.

Original statements are extracted from pinned current game source projections.
Fixed templates provide the already reviewed portable ECS bindings/admission joins.
"""
from pathlib import Path
import hashlib
import json
import os
import re
import shutil
import stat
import subprocess
import skirmish_production_worker as runtime

DEFAULT_MATH = runtime.DEFAULT_MATH
OPTIONS = ["--optimize=-float32"]
MAX_SOURCE = 16 * 1024 * 1024


def _need(value, reason):
    if not value:
        raise ValueError(reason)


def _unique(pairs):
    result = {}
    for key, value in pairs:
        _need(key not in result, "Duplicate source-manifest key: " + key)
        result[key] = value
    return result


def _sha(raw):
    return hashlib.sha256(raw).hexdigest()


def _relative(root, name):
    _need(isinstance(name, str) and name and "\\" not in name, "Relative source name required")
    path = Path(name)
    _need(not path.is_absolute() and all(x not in (".", "..") for x in path.parts), "Unsafe source-relative name")
    return root / path


def _directory(path):
    path = Path(os.path.abspath(path))
    for item in (path, *path.parents):
        _need(not stat.S_ISLNK(item.lstat().st_mode), "Symlink directory rejected: " + str(item))
    _need(path.is_dir(), "Existing real directory required: " + str(path))
    return path


def _shipping(text):
    """Disable only UNITY_EDITOR; preserve all other directives and source bytes."""
    stack, out, active = [], [], True
    for line in text.splitlines(True):
        directive = line.strip()
        if directive == "#if UNITY_EDITOR":
            stack.append((active, True, False))
            active = False
        elif directive.startswith("#if "):
            stack.append((active, False, True))
            if active:
                out.append(line)
        elif directive == "#else" and stack:
            parent, editor, first = stack[-1]
            if editor:
                active = parent and not first
                stack[-1] = (parent, editor, not first)
            elif active:
                out.append(line)
        elif directive == "#endif" and stack:
            parent, editor, _ = stack.pop()
            if not editor and active:
                out.append(line)
            active = parent
        elif active:
            out.append(line)
    _need(not stack, "Unbalanced source directives")
    return "".join(out)


def _generate(project, source_root, output):
    manifest_path = source_root / "manifest.json"
    manifest_raw = runtime.bounded_read(manifest_path, MAX_SOURCE)
    manifest = json.loads(manifest_raw, object_pairs_hook=_unique)
    _need(manifest.get("schema_version") == 1 and manifest.get("source_input_count") == 44, "Exact retained44-source manifest required")
    originals, outputs, linked = manifest["originals"], manifest["outputs"], manifest["linked"]
    _need(isinstance(originals, dict) and 8 <= len(originals) <= 64 and len(outputs) == 41 and len(linked) == 3, "Bounded reviewed source union required")
    pins = {str(manifest_path): _sha(manifest_raw)}
    projected = {}
    for name, spec in originals.items():
        path = _relative(project, name)
        raw = runtime.bounded_read(path, MAX_SOURCE)
        text = _shipping(raw.decode("utf-8"))
        _need(_sha(text.encode("utf-8")) == spec["projected_sha256"], "Original source shipping projection changed: " + name)
        projected[name] = text
        pins[str(path)] = _sha(raw)
    generated, records, used = [], [], set()
    for row in outputs:
        name = row["output"]
        _need(name.endswith(".cs") and name not in used, "Unique generated C# filename required")
        used.add(name)
        template = _relative(source_root, row["template"])
        raw = runtime.bounded_read(template, MAX_SOURCE)
        _need(_sha(raw) == row["template_sha256"], "Portable template changed: " + name)
        pins[str(template)] = _sha(raw)
        text = raw.decode("utf-8")
        for fragment in row["fragments"]:
            start, end, token = fragment["start"], fragment["end"], fragment["token"]
            source = projected[fragment["source"]]
            _need(type(start) is int and type(end) is int and 0 <= start < end <= len(source), "Bounded exact source span required")
            part = source[start:end]
            _need(_sha(part.encode("utf-8")) == fragment["sha256"] and text.count(token) == 1, "Original source slice/token changed: " + name)
            for old, new in fragment.get("portable_substitutions", []):
                _need(part.count(old) == 1, "Exact declared portable binding changed: " + name)
                part = part.replace(old, new, 1)
            if fragment.get("compact_whitespace", False):
                part = re.sub(r"\s+", "", part)
            text = text.replace(token, part, 1)
        raw = text.encode("utf-8")
        _need("@@SOURCE_" not in text and _sha(raw) == row["expected_output_sha256"], "Generated source differs from reviewed retained host: " + name)
        path = _relative(output / "generated", name)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(raw)
        generated.append(path)
        pins[str(path)] = _sha(raw)
        records.append(dict(input=name, sha256=_sha(raw), source_fragments=len(row["fragments"]), reviewed_output_equal=True))
    for row in linked:
        path = _relative(project, row["path"])
        raw = runtime.bounded_read(path, MAX_SOURCE)
        _need(_sha(raw) == row["raw_sha256"], "Directly linked original source changed: " + row["path"])
        generated.append(path)
        pins[str(path)] = _sha(raw)
        records.append(dict(input=row["path"], sha256=_sha(raw), linked_original=True, reviewed_output_equal=True))
    _need(len(generated) == 44, "Exact44 compiler inputs required")
    receipt = output / "source-equivalence.json"
    receipt.write_text(json.dumps(dict(result="Passed", input_count=44, inputs=records), indent=2) + "\n")
    pins[str(receipt)] = runtime.digest(receipt)
    return generated, pins


def verify_config(path):
    config = json.loads(runtime.bounded_read(path, 1024 * 1024), object_pairs_hook=_unique)
    _need(config.get("schema_version") == 1 and config.get("mono_options") == OPTIONS and config.get("no_dotnet_fallback") is True and config.get("copied_dlls") == [], "Exact installed-Mono causal host config required")
    pins = config.get("pins")
    _need(isinstance(pins, dict) and 32 <= len(pins) <= 256, "Complete bounded causal source/runtime pin closure required")
    for name, expected in pins.items():
        _need(Path(name).is_absolute() and isinstance(expected, str) and re.fullmatch("[0-9a-f]{64}", expected) and runtime.digest(name) == expected, "Causal worker pin mismatch: " + name)
    for key in ("worker_path", "mono_path", "newtonsoft_path"):
        _need(config.get(key) in pins, "Pinned executable/dependency required: " + key)
    for name in ("UnityEngine.CoreModule.dll", "UnityEngine.MathematicsModule.dll", "UnityEngine.SharedInternalsModule.dll"):
        _need(str(Path(config["engine_path"]) / name) in pins, "Pinned installed engine module required: " + name)
    return config


def build_worker(output, project, source_root=None, math_module=DEFAULT_MATH):
    """Build the SAME retained host, returning an absolute runtime-config Path.

    No private fixture, previous build, Git, Editor, DLL copy or native execution.
    The external CLI owns strict evidence/source admission and the run interface.
    """
    project = _directory(project)
    source_root = _directory(source_root if source_root else project / "Tools/CI/SkirmishOfflineSim/RetainedCausal")
    output = Path(os.path.abspath(output))
    _need(not output.exists() and not output.is_symlink(), "Fresh worker output directory required")
    for forbidden in ("Assets", "Packages", "ProjectSettings", ".git"):
        _need(not output.is_relative_to(project / forbidden), "Worker output cannot modify project " + forbidden)
    _directory(output.parent)
    output.mkdir()
    sources, pins = _generate(project, source_root, output)
    math_module = Path(math_module if math_module is not None else DEFAULT_MATH).resolve(strict=True)
    engine, scripting = math_module.parent, math_module.parents[2]
    distribution = scripting / "MonoBleedingEdge"
    mono = distribution / "bin/mono"
    _need(mono.is_file(), "Required Editor-distribution standalone Mono missing; install matching Editor distribution; no fallback: " + str(mono))
    dotnet_name = shutil.which("dotnet")
    _need(dotnet_name, "Installed dotnet SDK compiler required")
    dotnet = Path(dotnet_name).resolve(strict=True)
    compilers = list((dotnet.parent / "sdk").glob("*/Roslyn/bincore/csc.dll"))
    _need(compilers, "Installed Roslyn compiler missing")
    compiler = max(compilers, key=lambda p: tuple(int(v) for v in p.parents[2].name.split(".") if v.isdigit()))
    newtonsoft = list((project / "Library/PackageCache").glob("com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll"))
    _need(len(newtonsoft) == 1, "Exactly one installed package Newtonsoft.Json module required")
    newtonsoft = newtonsoft[0]
    refs = [scripting / "NetStandard/ref/2.1.0/netstandard.dll", scripting / "NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll", math_module, engine / "UnityEngine.SharedInternalsModule.dll"]
    deps = [engine / "UnityEngine.CoreModule.dll", distribution / "lib/libmono-native.dylib", distribution / "etc/mono/config", distribution / "lib/mono/net_4_x-macos/mscorlib.dll", distribution / "lib/mono/net_4_x-macos/Facades/netstandard.dll"]
    for name in ("System", "System.Core", "System.Numerics", "System.Xml", "System.Runtime.Serialization"):
        deps.append(distribution / ("lib/mono/gac/" + name + "/4.0.0.0__b77a5c561934e089/" + name + ".dll"))
    inputs = [Path(__file__).absolute(), Path(runtime.__file__).absolute(), mono, dotnet, compiler, newtonsoft, *refs, *deps]
    pins.update({str(path): runtime.digest(path) for path in inputs})
    executable = output / "RetainedCausalWorker.exe"
    args = ["-nologo", "-target:exe", "-nostdlib+", "-langversion:latest", "-out:" + str(executable)] + ["-r:" + str(path) for path in refs] + ["-r:" + str(newtonsoft), "-r:ActualCore=" + str(engine / "UnityEngine.CoreModule.dll")] + list(map(str, sources))
    _need(not any(any(c in arg for c in '\r\n"') for arg in args), "Unsafe compiler response argument")
    response = output / "compile.rsp"
    response.write_text("\n".join('"' + arg + '"' for arg in args) + "\n")
    command = ["rtk", "proxy", str(dotnet), str(compiler), "@" + str(response)]
    log = output / "compile.log"
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=120)
    except subprocess.TimeoutExpired as error:
        def partial_text(value):
            return value.decode("utf-8", errors="replace") if isinstance(value, bytes) else (value or "")
        stdout, stderr = partial_text(error.stdout), partial_text(error.stderr)
        (output / "compile.stdout.log").write_bytes(error.stdout if isinstance(error.stdout, bytes) else stdout.encode("utf-8"))
        (output / "compile.stderr.log").write_bytes(error.stderr if isinstance(error.stderr, bytes) else stderr.encode("utf-8"))
        log.write_text(stdout + stderr)
        (output / "compile-status.json").write_text(json.dumps(dict(status="TimedOut", command=command, timeout_seconds=error.timeout, process_exit=None, partial_stdout_file="compile.stdout.log", partial_stderr_file="compile.stderr.log"), indent=2) + "\n")
        raise RuntimeError("Retained causal Mono compile timed out; partial output preserved: " + str(log)) from error
    log.write_text(result.stdout + result.stderr)
    _need(result.returncode == 0, "Retained causal Mono compile failed: " + str(log))
    _need(all(runtime.digest(name) == expected for name, expected in pins.items()), "Causal worker source/runtime changed during build")
    for path in (executable, response, log):
        pins[str(path)] = runtime.digest(path)
    config = dict(schema_version=1, scope="OriginalRetainedCausalPrefix_OneCanonicalBank", mono_path=str(mono), engine_path=str(engine), newtonsoft_path=str(newtonsoft), worker_path=str(executable), mono_options=OPTIONS, pins=pins, no_dotnet_fallback=True, copied_dlls=[], native_execution=False, full_frame_supported=False, full_mission_supported=False)
    path = output / "runtime-config.json"
    path.write_text(json.dumps(config, indent=2) + "\n")
    verify_config(path)
    return path
