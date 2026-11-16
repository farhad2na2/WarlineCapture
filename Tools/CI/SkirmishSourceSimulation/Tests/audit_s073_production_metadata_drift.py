#!/usr/bin/env python3
"""Inspect the exact retained S073 production metadata's current source pins."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
METADATA = ROOT / "Design/AgentReports/SkirmishProduction/S073/TransportMetadataReadback20261101/s073-unit-production-metadata.json"
RECIPE = ROOT / "Design/AgentReports/SkirmishProduction/S073/NativeProductionRecipeObservation20261101/recipe-observation-receipt.json"
SOURCES = (
    "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
    "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
    "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset",
    "Assets/Game/Configs/Skirmish/UnitRegistry.asset",
    "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab",
)
RECIPE_SOURCES = (
    "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    if digest(METADATA) != "1372d72063491572eeaa11ffc78a8bbf790f10d1ef9f673fe5627af4447c3e8e":
        raise ValueError("Retained production metadata changed")
    if digest(RECIPE) != "43e8e923810ae82d04dbdf0ca18a2e35d5b44122b4337b7b23fdaa53739fbffb":
        raise ValueError("Retained production recipe receipt changed")
    metadata, recipe = json.loads(METADATA.read_bytes()), json.loads(RECIPE.read_bytes())
    entries = []
    for category, names, pins in (("metadata", SOURCES, metadata["source_pins"]),
                                  ("recipe", RECIPE_SOURCES, recipe["observed_source_pins_sha256"])):
        for relative in names:
            path = ROOT / relative
            expected = pins.get(relative)
            if not isinstance(expected, str) or len(expected) != 64 or not path.is_file() or path.is_symlink():
                raise ValueError("Pinned current source file required: " + relative)
            actual = digest(path)
            entries.append({"category": category, "path": relative, "captured_sha256": expected,
                            "current_sha256": actual, "matches": actual == expected})
    result = {"status": "ProductionSourcePinAuditCompleted", "metadata_sha256": digest(METADATA),
              "recipe_receipt_sha256": digest(RECIPE), "sources": entries,
              "changed_source_count": sum(not e["matches"] for e in entries),
              "source_epoch_stability_verified": False, "metadata_equivalence_proven": False,
              "runtime_admission": False, "mission_ready": False}
    output = Path(args.output).absolute()
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": result["status"], "changed_source_count": result["changed_source_count"],
                      "metadata_equivalence_proven": False, "mission_ready": False}))


if __name__ == "__main__":
    main()
