"""Pinned standalone Mono production worker for the existing WorldReplay host.

The host owns continuous-state protocol V2 and native assertion admission.
This module only builds, pins and launches the worker; it never reseeds state.
"""
from pathlib import Path
import hashlib
import json
import os
import shutil
import stat
import struct
import subprocess

DEFAULT_MATH = Path("/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.MathematicsModule.dll")
MAX_FILE = 256 * 1024 * 1024
MAX_BATCH = 64 * 1024 * 1024
OPTIONS = ["--optimize=-float32"]
PACKET = ("PhysicalProductionBindings.cs", "PhysicalProductionInputChecks.cs", "ProductionMonoWorker.cs")
ORIGINALS = ("Assets/Game/Scripts/Systems/BuildingResourceProductionEcsSystem.cs", "Assets/Game/Scripts/Systems/BuildingResourceProductionSystemHelper.cs", "Assets/Game/Scripts/Components/BuildingResourceProductionProfileComponent.cs")
CALLERS = ("Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs", "Assets/Game/Scripts/Systems/BuildingProductionTickCompositionSystemHelper.cs", "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs", "Assets/Game/Scripts/Systems/WorldScopedComponentQueryCache.cs")


def bounded_read(path, limit=MAX_FILE):
    """Reject special files/links before open; fixed read plus EOF, no growth."""
    path = Path(os.path.abspath(path))
    for item in (path, *path.parents):
        info = item.lstat()
        if stat.S_ISLNK(info.st_mode):
            raise ValueError("Symlink path rejected: " + str(item))
    info = path.stat()
    if not stat.S_ISREG(info.st_mode) or not 0 < info.st_size <= limit:
        raise ValueError("Positive bounded regular file required: " + str(path))
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    try:
        actual = os.fstat(fd)
        if not stat.S_ISREG(actual.st_mode) or (actual.st_dev, actual.st_ino, actual.st_size) != (info.st_dev, info.st_ino, info.st_size):
            raise ValueError("File identity/size changed before read: " + str(path))
        data = bytearray(info.st_size)
        with os.fdopen(fd, "rb", closefd=False) as stream:
            view = memoryview(data)
            offset = 0
            while offset < len(data):
                count = stream.readinto(view[offset:])
                if not count:
                    raise ValueError("Truncated file: " + str(path))
                offset += count
            if stream.read(1):
                raise ValueError("File grew: " + str(path))
        end = os.fstat(fd)
        now = path.stat()
        if (end.st_dev, end.st_ino, end.st_size, end.st_mtime_ns) != (actual.st_dev, actual.st_ino, actual.st_size, actual.st_mtime_ns) or (now.st_dev, now.st_ino, now.st_size, now.st_mtime_ns) != (end.st_dev, end.st_ino, end.st_size, end.st_mtime_ns):
            raise ValueError("File changed during read: " + str(path))
        return bytes(data)
    finally:
        os.close(fd)


def digest(path):
    return hashlib.sha256(bounded_read(path)).hexdigest()


def verify_config(path):
    config = json.loads(bounded_read(path, 1024 * 1024))
    if config.get("schema_version") != 1 or config.get("protocol_version") != 2 or config.get("mono_options") != OPTIONS or config.get("no_dotnet_fallback") is not True or config.get("aggregate_totals_supported") is not False:
        raise ValueError("Unsupported production worker configuration")
    pins = config.get("pins")
    if not isinstance(pins, dict) or not 12 <= len(pins) <= 64:
        raise ValueError("Complete bounded worker source/runtime pin closure required")
    for name, expected in pins.items():
        if not Path(name).is_absolute() or len(expected) != 64 or digest(name) != expected:
            raise ValueError("Production worker pin mismatch: " + name)
    for key in ("mono_path", "worker_path"):
        if config.get(key) not in pins:
            raise ValueError("Pinned executable missing: " + key)
    for name in ("UnityEngine.MathematicsModule.dll", "UnityEngine.CoreModule.dll", "UnityEngine.SharedInternalsModule.dll"):
        if str(Path(config["engine_path"]) / name) not in pins:
            raise ValueError("Pinned installed engine module missing: " + name)
    return config


def build_worker(output, project, source_root=None, math_module=DEFAULT_MATH):
    """Build beside host; return absolute config path for production_worker_config.

    source_root overrides ONLY the three reviewed packet files.
    Installed modules remain in place; the original production sources are linked.
    """
    project = Path(project).resolve(strict=True)
    output = Path(output).absolute()
    output.mkdir(parents=True, exist_ok=False)
    source_root = Path(source_root) if source_root else project / "Tools/CI/SkirmishOfflineSim/PhysicalProduction"
    math_module = Path(math_module).resolve(strict=True)
    engine = math_module.parent
    scripting = math_module.parents[2]
    runtime = scripting / "MonoBleedingEdge"
    mono = runtime / "bin/mono"
    if not mono.exists():
        raise ValueError("Required Editor-distribution standalone Mono is missing: " + str(mono) + "; install the pinned Editor distribution; no fallback")
    mono = mono.resolve(strict=True)
    dotnet_name = shutil.which("dotnet")
    if not dotnet_name:
        raise ValueError("dotnet SDK compiler is required to build the pinned Mono worker")
    dotnet = Path(dotnet_name).resolve(strict=True)
    candidates = list((dotnet.parent / "sdk").glob("*/Roslyn/bincore/csc.dll"))
    if not candidates:
        raise ValueError("Roslyn csc.dll is missing from installed dotnet SDK")
    compiler = max(candidates, key=lambda p: tuple(int(v) for v in p.parents[2].name.split(".") if v.isdigit()))
    refs = [scripting / "NetStandard/ref/2.1.0/netstandard.dll", scripting / "NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll", math_module, engine / "UnityEngine.SharedInternalsModule.dll"]
    deps = [runtime / "lib/mono/net_4_x-macos/mscorlib.dll", runtime / "lib/mono/net_4_x-macos/Facades/netstandard.dll", runtime / "lib/mono/gac/Mono.Security/4.0.0.0__0738eb9f132ed756/Mono.Security.dll", runtime / "lib/libmono-native.dylib", runtime / "etc/mono/config", engine / "UnityEngine.CoreModule.dll"]
    packet = [source_root / name for name in PACKET]
    sources = [source_root / name for name in ("ProductionMonoWorker.cs", "PhysicalProductionInputChecks.cs", "PhysicalProductionBindings.cs")] + [project / "Tools/CI/SkirmishOfflineSim/BodyTransitionData.cs"] + [project / name for name in ORIGINALS]
    pinned = list(dict.fromkeys([Path(__file__).absolute(), mono, dotnet, compiler] + refs + deps + packet + sources + [project / name for name in CALLERS]))
    pins = {str(path): digest(path) for path in pinned}
    exe = output / "ProductionMonoWorker.exe"
    args = ["-nologo", "-target:exe", "-nostdlib+", "-langversion:latest", "-out:" + str(exe)] + ["-r:" + str(path) for path in refs] + ["-r:ActualCore=" + str(engine / "UnityEngine.CoreModule.dll")] + list(map(str, sources))
    if any(any(c in arg for c in '\r\n"') for arg in args):
        raise ValueError("Unsafe compiler response argument")
    rsp = output / "compile.rsp"
    rsp.write_text("\n".join('"' + arg + '"' for arg in args) + "\n")
    result = subprocess.run(["rtk", "proxy", str(dotnet), str(compiler), "@" + str(rsp)], capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(result.stdout + result.stderr)
    if result.returncode:
        raise ValueError("Production Mono compile failed: " + str(output / "compile.log"))
    if pins != {str(path): digest(path) for path in pinned}:
        raise ValueError("Production build source/runtime drift")
    pins[str(exe)] = digest(exe)
    pins[str(rsp)] = digest(rsp)
    config = dict(schema_version=1, protocol_version=2, scope="OriginalPhysicalProduction_MonoWidenedFloat_ContinuousBatchV2", mono_path=str(mono), worker_path=str(exe), engine_path=str(engine), mono_options=OPTIONS, pins=pins, aggregate_totals_supported=False, no_dotnet_fallback=True, runtime_provenance="Editor-distribution bundled standalone Mono; no Editor launch or DLL copies", copied_dlls=[], native_execution=False, full_mission_supported=False)
    config["native_source_pins"] = {name: pins[str(project / name)] for name in ORIGINALS + CALLERS}
    path = output / "production-worker.json"
    path.write_text(json.dumps(config, indent=2) + "\n")
    verify_config(path)
    return path


def run_batch(config_path, input_path, output_path):
    """Optional protocol smoke runner; WorldReplay C# normally launches this batch.

    No interpretation of state/scalars, native trace or expected assertions here.
    """
    config_raw = bounded_read(config_path, 1024 * 1024)
    config = verify_config(config_path)
    raw = bounded_read(input_path, MAX_BATCH)
    if len(raw) < 12:
        raise ValueError("Truncated production batch header")
    magic, version, ticks = struct.unpack_from("<iii", raw)
    if magic != 0x50524431 or version != 2 or not 1 <= ticks <= 256:
        raise ValueError("Continuous production V2 request required")
    output_path = Path(output_path).absolute()
    if output_path.exists() or output_path.is_symlink():
        raise ValueError("Fresh worker output required")
    command = [config["mono_path"], *config["mono_options"], config["worker_path"], config["engine_path"], str(Path(input_path).absolute()), str(output_path), hashlib.sha256(raw).hexdigest()]
    result = subprocess.run(["rtk", "proxy", *command], capture_output=True, text=True, timeout=120)
    output_path.with_suffix(".stdout.log").write_text(result.stdout)
    output_path.with_suffix(".stderr.log").write_text(result.stderr)
    verify_config(config_path)
    if bounded_read(config_path, 1024 * 1024) != config_raw or bounded_read(input_path, MAX_BATCH) != raw:
        raise ValueError("Production config/request drift during worker execution")
    if result.returncode:
        raise ValueError("Production worker failed: " + str(output_path.with_suffix(".stderr.log")))
    reply = bounded_read(output_path, MAX_BATCH)
    if len(reply) < 12 or struct.unpack_from("<iii", reply) != (0x50524432, 2, ticks):
        raise ValueError("Production worker reply protocol differs")
    return reply
