"""Compile the ordinary source airdrop owner for a no-Support diagnostic."""
from pathlib import Path
import hashlib
import subprocess
import sys

import s004_transport_profile as transport

ROOT = transport.ROOT

def apply(arguments, output):
    build = transport.verify()
    pins = build["source_and_reference_pins"]
    def pinned(path):
        if pins.get(str(path)) != hashlib.sha256(path.read_bytes()).hexdigest():
            raise ValueError("Exact airdrop source pin required: " + str(path))
        return path
    source = pinned(ROOT / "Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs")
    effect = pinned(ROOT / "Assets/Game/Scripts/Components/SupportEffectComponents.cs")
    ability = pinned(ROOT / "Assets/Game/Scripts/Components/SupportAbilityComponents.cs")
    generator = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_airdrop_projection.py"
    projected = output / "S004AirdropProjection.cs"
    result = subprocess.run([sys.executable, str(generator), "--source-sha256", pins[str(source)], "--output", str(projected)], capture_output=True, text=True, timeout=60)
    (output / "airdrop-generation.log").write_text(result.stdout + result.stderr)
    if result.returncode != 0 or "[S004AirdropProjection] result=Generated" not in result.stdout:
        raise ValueError("Airdrop source projection failed")
    def replace_suffix(suffix, path):
        matches = [i for i, value in enumerate(arguments) if value.endswith(suffix)]
        if len(matches) != 1: raise ValueError("Exact airdrop projection required: " + suffix)
        arguments[matches[0]] = str(path)
    headers = output / "TransportHeadersWithoutFixedList.cs"
    lines = headers.read_text().splitlines(keepends=True)
    removed = [line for line in lines if "struct UnitTransportAirdropSystem:" in line]
    if len(removed) != 1: raise ValueError("Exact airdrop compile boundary required")
    new_headers = output / "TransportHeadersWithoutAirdrop.cs"
    new_headers.write_text("using System;\n" + "".join(line for line in lines if line not in removed))
    replace_suffix("/TransportHeadersWithoutFixedList.cs", new_headers)
    ordering = output / "SupportFlightOrderingType.cs"
    ordering.write_text("namespace Game.Runtime { internal struct SupportFlightSystem {} }\n")
    effect_text = effect.read_text()
    structs = []
    for name in ("SupportFlightComponent", "SupportPassengerCleanupComponent"):
        start = effect_text.index("    public struct " + name)
        pos = effect_text.index("{", start); depth = 1; end = pos + 1
        while depth:
            depth += (effect_text[end] == "{") - (effect_text[end] == "}"); end += 1
        structs.append(effect_text[start:end])
    extracted = output / "OriginalSupportAirdropComponents.cs"
    extracted.write_text("using Unity.Entities; using Unity.Mathematics; namespace Game.Components {\n" + "\n".join(structs) + "\n}\n")
    ecs = output / "S004TransportAutonomousEcs.cs"; text = ecs.read_text()
    old = "public struct LocalTransform : Unity.Entities.IComponentData{public Unity.Mathematics.float3 Position;"
    new = "public struct LocalTransform : Unity.Entities.IComponentData{public static LocalTransform Identity=>new LocalTransform{Rotation=Unity.Mathematics.quaternion.identity,Scale=1};public Unity.Mathematics.float3 Position;"
    if text.count(old) != 1: raise ValueError("Exact source LocalTransform required")
    replacement = output / "S004AirdropAutonomousEcs.cs"; replacement.write_text(text.replace(old, new))
    replace_suffix("/S004TransportAutonomousEcs.cs", replacement)
    host = output / "S004TransportOwnedEntityJobs.cs"; text = host.read_text(); marker = "var admitted=new[]{"
    names = ("UpdateSettleMoveTransformJob", "UpdateParachuteDropTransformJob", "UpdateCargoDropTransformJob")
    addition = "".join('"Game.Runtime.UnitTransportAirdropSystem+' + name + '",' for name in names)
    if text.count(marker) != 1: raise ValueError("Exact source job admission required")
    replacement = output / "S004AirdropOwnedEntityJobs.cs"; replacement.write_text(text.replace(marker, marker + addition))
    replace_suffix("/S004TransportOwnedEntityJobs.cs", replacement)
    arguments.extend(str(path) for path in (projected, ordering, extracted, ability))
