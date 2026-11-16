"""Opt-in source Support closure; no native presentation or readiness claim."""
from pathlib import Path
import hashlib
import re
import s004_transport_profile as transport

ROOT = transport.ROOT

def apply(arguments, output):
    pins = transport.verify()["source_and_reference_pins"]
    def pinned(path):
        path = Path(path)
        if pins.get(str(path)) != hashlib.sha256(path.read_bytes()).hexdigest():
            raise ValueError("Exact Support source pin required: " + str(path))
        return str(path)
    def remove(suffix):
        matches = [i for i, value in enumerate(arguments) if value.endswith(suffix)]
        if len(matches) != 1: raise ValueError("Exact Support projection required: " + suffix)
        return arguments.pop(matches[0])
    remove("/S004AirdropProjection.cs")
    remove("/SupportFlightOrderingType.cs")
    remove("/OriginalSupportAirdropComponents.cs")
    remove("/174_SupportEffectComponents.cs")
    remove("/386_SupportEffectComponents.cs")
    remove("/213_UnsupportedSupportCollection.cs")
    ecs = Path(remove("/S004AirdropAutonomousEcs.cs"))
    text = ecs.read_text(); old = "public struct ComponentType{"; new = "public partial struct ComponentType{"
    if text.count(old) != 1: raise ValueError("Exact ComponentType source seam required")
    projection = output / "S004SupportAutonomousEcs.cs"
    projection.write_text(text.replace(old, new)); arguments.append(str(projection))
    support = ROOT / "Assets/Game/Scripts/Systems/Support"
    paths = sorted(support.glob("*.cs"))
    paths += [ROOT / path for path in (
        "Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs",
        "Assets/Game/Scripts/Components/SupportEffectComponents.cs",
        "Assets/Game/Scripts/Components/SupportMissionContextComponents.cs",
        "Assets/Game/Scripts/Components/CampaignMissionCitywideAlertComponents.cs",
        "Assets/Game/Scripts/Runtime/Missions/CampaignCitywideFuelScope.cs")]
    for path in paths:
        value = pinned(path)
        if value not in arguments: arguments.append(value)
    defense = Path(pinned(ROOT / "Assets/Game/Scripts/Components/CampaignMissionDefenseComponents.cs"))
    marker = "    public struct CampaignMissionDormantMapUnitTag : IComponentData { }"
    if defense.read_text().count(marker) != 1: raise ValueError("Original dormant unit tag changed")
    tag = output / "OriginalCampaignDormantUnitTag.cs"
    tag.write_text("using Unity.Entities; namespace Game.Components {\n" + marker + "\n}\n")
    utility = Path(pinned(ROOT / "Assets/Game/Scripts/Runtime/Missions/CampaignMissionCitywideAlertRuleUtility.cs"))
    match = re.search(r"public static bool Matches\(in CampaignMissionCitywideAlertState mission, in CampaignMissionRuntimeComponent runtime\)\s*=>[^;]+;", utility.read_text())
    if match is None: raise ValueError("Original Campaign fuel identity method changed")
    extracted = output / "OriginalCitywideFuelMatch.cs"
    extracted.write_text("using Game.Components;namespace Game.Runtime { public static class CampaignMissionCitywideAlertRuleUtility {\n" + match.group() + "\n} }\n")
    engine_type = output / "GameObjectTypeOnly.cs"
    engine_type.write_text("namespace UnityEngine { public class GameObject {} }\n")
    recipe = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
    arguments.extend(str(path) for path in (tag, extracted, engine_type, recipe / "S004SupportEntityManagerBoundary.cs", recipe / "S004SupportOwnerDiagnostic.cs"))
