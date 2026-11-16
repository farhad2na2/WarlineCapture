"""Compile the existing transform bridge's pure matrix method for S004 readback."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BRIDGE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-614-SourceMissionTransformBridge.cs"
S026_ECS = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs"
PROBE = Path(__file__).with_name("probe_s004_spawn_owners_compile.py")
FIXTURE = Path(__file__).with_name("S004TransformProjectionReadback.cs")

def apply(arguments, output):
    source = BRIDGE.read_text()
    guard = 'static void Need(bool condition,string reason){if(!condition)throw new NotSupportedException(reason);}'
    if source.count(guard) != 1:
        raise ValueError("Exact source bridge validation required")
    spec = importlib.util.spec_from_file_location("s004_transform_method_probe", PROBE)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    matrix = module.extract_method(source, "ComputeMatrices")
    original = 'public Unity.Mathematics.float4x4 ToMatrix()=>Unity.Mathematics.float4x4.TRS(Position,Rotation,Scale);'
    if S026_ECS.read_text().count(original) != 1:
        raise ValueError("Exact current source transform seam required")
    generated = output / "S004ExistingTransformProjection.cs"
    generated.write_text('using System;using System.Collections.Generic;using Unity.Entities;using Unity.Transforms;using Unity.Mathematics;\n'
                         'namespace Game.Runtime { internal static class S004ExistingTransformProjection {\n' + guard + '\n' + matrix + '\n} }\n'
                         'namespace Unity.Transforms { internal static class S004TransformMatrixExtension { '
                         'internal static Unity.Mathematics.float4x4 ToMatrix(this LocalTransform value)=>Unity.Mathematics.float4x4.TRS(value.Position,value.Rotation,value.Scale); } }\n')
    defines = [i for i, value in enumerate(arguments) if value.startswith("-define:")]
    if len(defines) != 1:
        raise ValueError("Exact source define set required")
    arguments[defines[0]] += ",WARLINE_S004_TRANSFORM_PROJECTION"
    arguments.extend((str(generated), str(FIXTURE)))
