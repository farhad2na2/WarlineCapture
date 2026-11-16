"""Create the exclusive pre-wrapper receipt for an S073 launch-input capture."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path


WRAPPER = "Tools/CI/invoke_unity_macos.sh"
REQUIRED_SOURCE_PINS = {
    "Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset",
    "Assets/Game/Resources/SkirmishCanonicalAircraftGeometry_V1.asset",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishAircraftRotorGeometryAuthoring.cs",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishCanonicalAircraftGeometryAuthoring.cs",
    "Assets/Game/Scenes/Match.unity",
    "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset",
    "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
    "Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md",
    "Assets/Game/Scripts/Components/OperationMapComponents.cs",
    "Assets/Game/Scripts/Components/SceneLifecycleComponents.cs",
    "Assets/Game/Scripts/Systems/S073NativeProductionDeliveryObserver.cs",
    "Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTickCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs",
    "Assets/Game/Scripts/Editor/Skirmish/S073NativeProductionDeliveryWindow.cs",
    WRAPPER,
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def _output_path(value, label):
    path = Path(value)
    if not path.is_absolute():
        raise ValueError(label + " must be absolute")
    resolved = path.resolve(strict=False)
    if resolved.parent != Path("/private/tmp"):
        raise ValueError(label + " must be a direct child of /private/tmp")
    if resolved.exists():
        raise FileExistsError(label + " must be fresh before wrapper launch: " + str(resolved))
    return resolved


def create_receipt(repo, receipt_path, window_output_path, delivery_output_path,
                   wrapper_command=WRAPPER):
    repo = Path(repo).resolve(strict=True)
    expected_wrapper = (repo / WRAPPER).resolve(strict=True)
    supplied_wrapper = Path(wrapper_command)
    if not supplied_wrapper.is_absolute():
        supplied_wrapper = repo / supplied_wrapper
    if supplied_wrapper.resolve(strict=False) != expected_wrapper:
        raise ValueError("Only the checked repository macOS Unity wrapper is admitted")
    if wrapper_command not in (WRAPPER, str(expected_wrapper)):
        raise ValueError("Use the canonical checked wrapper path in the receipt")

    raw_window_output = Path(window_output_path)
    raw_delivery_output = Path(delivery_output_path)
    if raw_window_output.is_absolute() and raw_delivery_output.is_absolute() and raw_window_output == raw_delivery_output:
        raise ValueError("Window and delivery output paths must be distinct")
    window_output = _output_path(window_output_path, "window output")
    delivery_output = _output_path(delivery_output_path, "delivery output")
    if window_output == delivery_output:
        raise ValueError("Window and delivery output paths must be distinct")

    pins = {}
    for relative in sorted(REQUIRED_SOURCE_PINS):
        source = (repo / relative).resolve(strict=True)
        if not source.is_relative_to(repo) or not source.is_file():
            raise ValueError("Required regular source file unavailable: " + relative)
        pins[relative] = sha(source.read_bytes())

    receipt = {
        "kind": "S073NativeDeliveryLaunchPreRunReceipt",
        "schema_version": 1,
        "captured_before_wrapper": True,
        "captured_at_utc": datetime.now(timezone.utc).isoformat(),
        "wrapper_command": WRAPPER,
        "output_paths_absent_at_capture": True,
        "window_output_path": str(window_output),
        "delivery_output_path": str(delivery_output),
        "source_pins_sha256": pins,
    }
    target = Path(os.path.abspath(receipt_path))
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("x", encoding="utf-8") as output:
        json.dump(receipt, output, indent=2, allow_nan=False)
        output.write("\n")
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--receipt", required=True)
    parser.add_argument("--window-output", required=True)
    parser.add_argument("--delivery-output", required=True)
    parser.add_argument("--wrapper-command", default=WRAPPER)
    args = parser.parse_args()
    receipt = create_receipt(args.repo, args.receipt, args.window_output,
                             args.delivery_output, args.wrapper_command)
    print(json.dumps(receipt, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
