"""Prepare a one-step S004 source bridge probe with a sealed compile readset."""
import hashlib
import json
import os
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BRIDGE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-614-SourceMissionTransformBridge.cs"
CLEANUP = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/SourceFiniteParentCleanup.cs"
CLOCK = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/SourceTransformPhaseClock.cs"
HEADLESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Templates/S026/SourceHeadlessLeafBinding.cs.template"
READSET = ROOT / "Tools/CI/SkirmishSourceSimulation/Templates/S026/SourceTransformConsumedReadset.cs.template"
FAMILY = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceGroundMissionFamily.cs"
ECS = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs"
SETUP = ROOT / "T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json"

def sha(data): return hashlib.sha256(data).hexdigest()
def once(text, old, new):
    if text.count(old) != 1: raise ValueError("Exact S004 bridge source seam required: " + old[:100])
    return text.replace(old, new, 1)

def apply(arguments, output, executable):
    bridge = BRIDGE.read_text()
    bridge = once(bridge, 'if(sourceKeys.Count!=16)', 'if(sourceKeys.Count!=23)')
    family = FAMILY.read_text()
    insertion = 'if(setup.CatalogId=="S004"){SourceS004CompiledSetupInput.RequireExact(setup,' + json.dumps(str(SETUP)) + ',' + json.dumps(sha(SETUP.read_bytes())) + ');return;}\n  '
    family = once(family, 'if(setup.CatalogId=="S002")return;', insertion + 'if(setup.CatalogId=="S002")return;')
    indices = [i for i, value in enumerate(arguments) if value == str(FAMILY)]
    if len(indices) != 1: raise ValueError("Exact source family adapter required")
    family_path = output / "S004TransformSourceFamily.cs"; family_path.write_text(family); arguments[indices[0]] = str(family_path)
    bridge_path = output / "S004SourceMissionTransformBridge.cs"; bridge_path.write_text(bridge)
    leaf_lines = HEADLESS.read_text().splitlines()
    if len(leaf_lines) < 2 or "class SourceHeadlessLeafBinding" not in leaf_lines[1]: raise ValueError("Exact headless guard required")
    leaf = output / "S004SourceHeadlessLeafBinding.cs"; leaf.write_text("\n".join(leaf_lines[:2]) + "\n")
    if ECS.read_text().count('public Unity.Mathematics.float4x4 ToMatrix()=>Unity.Mathematics.float4x4.TRS(Position,Rotation,Scale);') != 1: raise ValueError("Exact source transform seam required")
    matrix = output / "S004ExistingTransformProjection.cs"
    if str(matrix) not in arguments or matrix.read_text().count("class S004TransformMatrixExtension") != 1:
        raise ValueError("Exact previously generated S004 transform extension required")
    arguments.extend(str(p) for p in (bridge_path, CLEANUP, CLOCK, leaf))
    defines = [i for i, v in enumerate(arguments) if v.startswith("-define:")]
    if len(defines) != 1: raise ValueError("Exact source define set required")
    arguments[defines[0]] += ",WARLINE_S004_TRANSFORM_BRIDGE"
    if os.environ.get("WARLINE_S004_TRANSFORM_SCHEDULE") == "1":
        order = output / "S004ScanRopeSourceOwnerOrder.cs"
        step = output / "S004ScanRopeOwnerStep.cs"
        if str(order) not in arguments or str(step) not in arguments: raise ValueError("Exact scan/rope owner order required")
        text = order.read_text()
        text = once(text, '"UnitTransportRopeDisperseSystem","EngageTargetValidateSystem"', '"UnitTransportRopeDisperseSystem","ParentSystem","LocalToWorldSystem","EngageTargetValidateSystem"')
        text = once(text, '{"s004_rope_disperse",new[]{"UnitTransportRopeDisperseSystem"}},', '{"s004_rope_disperse",new[]{"UnitTransportRopeDisperseSystem"}},\n {"s004_transforms",new[]{"ParentSystem","LocalToWorldSystem"}},')
        order.write_text(text)
        text = step.read_text()
        text = once(text, 'Phase("s004_rope_disperse",()=>s004ScanRopeTickOwners.RopeDisperse());', 'Phase("s004_rope_disperse",()=>s004ScanRopeTickOwners.RopeDisperse());Phase("s004_transforms",RunS004TransformScheduledPhase);')
        step.write_text(text)
        arguments[defines[0]] += ",WARLINE_S004_TRANSFORM_SCHEDULE"
    sources = [Path(x).resolve(strict=True) for x in arguments if x.endswith(".cs")]
    if len(sources) != len(set(sources)): raise ValueError("Duplicate source paths cannot be sealed")
    pins = {str(p): sha(p.read_bytes()) for p in sources}
    patterns = (r"WithChangeFilter\s*<[^>]*(?:LocalTransform|LocalToWorld|Parent|Child|PostTransformMatrix)",
                r"DidChange\s*<[^>]*(?:LocalTransform|LocalToWorld|Parent|Child|PostTransformMatrix)")
    for p in sources:
        text = p.read_text(errors="strict")
        if any(re.search(pattern, text) for pattern in patterns): raise ValueError("Explicit transform changed-filter requires review: " + str(p))
    readset = json.dumps({"kind":"S004DiagnosticCompiledSourceReadset","source_pins":pins,
        "scope":"One S004 source bridge probe; explicit generic transform changed filters scanned; no native parity or full scheduler claim"}, indent=2) + "\n"
    sidecar = Path(str(executable) + ".transform-readset.json"); sidecar.write_text(readset)
    template = READSET.read_text()
    if template.count("READSET_SHA256") != 1: raise ValueError("Exact source readset seal required")
    readset_source = output / "S004TransformConsumedReadset.cs"; readset_source.write_text(template.replace("READSET_SHA256", sha(readset.encode())))
    arguments.append(str(readset_source))
