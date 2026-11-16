"""Prepare a bounded compile probe of the original Materials recovery owner.

This does not admit startup or supply scenario inputs.  The source host models
an absent DynamicBuffer as null, whereas Unity's default buffer is a struct.
"""
from pathlib import Path
import hashlib
import types


def generate(repo, output):
    root = Path(repo)
    paths = [
        root / "Assets/Game/Scripts/Systems/MaterialsScenarioRecoveryStartupSystemHelper.cs",
        root / "Assets/Game/Scripts/Systems/ResourceExchangeStartupProjectionSystemHelper.cs",
        root / "Assets/Game/Scripts/Components/AssistantComponents.cs",
        Path(__file__).parent / "operation_map_publication/regenerate_config_metadata.py",
        Path(__file__).parent / "MaterialsRecoveryReadOnlyBuffers.cs.txt",
    ]
    raw = [path.read_bytes() for path in paths]
    owner, exchange, assistant = [item.decode("utf-8").replace("\r\n", "\n") for item in raw[:3]]
    for original, replacement in (("!buildings.IsCreated", "buildings == null"),
                                  ("!units.IsCreated", "units == null")):
        if owner.count(original) != 1:
            raise ValueError("Materials recovery buffer check changed: " + original)
        owner = owner.replace(original, replacement, 1)
    lex = types.ModuleType("materials_recovery_lexical")
    lex.__file__ = str(paths[3])
    exec(compile(raw[3], str(paths[3]), "exec"), lex.__dict__)
    gate = lex.member(exchange, "TryResolveGate")
    declaration = "public enum AssistantMessagePriority : byte"
    if assistant.count(declaration) != 1:
        raise ValueError("Assistant priority enum changed")
    start = assistant.index(declaration)
    end = assistant.index("}", assistant.index("{", start)) + 1
    priority = assistant[start:end]
    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)
    (out / "MaterialsScenarioRecoveryStartupSystemHelper.cs").write_text(owner)
    (out / "ResourceExchangeGateProbe.cs").write_text(
        "using Game.Configs;using Unity.Collections;namespace Game.Runtime{"
        "public sealed class ResourceExchangeStartupProjectionSystemHelper{\n" + gate + "\n}}\n")
    (out / "AssistantMessagePriority.cs").write_text("namespace Game.Components{\n" + priority + "\n}\n")
    (out / "MaterialsRecoveryReadOnlyBuffers.cs").write_bytes(raw[4])
    if any(path.read_bytes() != contents for path, contents in zip(paths, raw)):
        raise ValueError("Recovery probe source changed during extraction")
    return {"kind": "MaterialsRecoveryBoundedSourceProjection", "source_sha256": [hashlib.sha256(x).hexdigest() for x in raw],
            "scope": "Original recovery owner with bounded read-only buffers; no startup admission or scenario readiness"}
