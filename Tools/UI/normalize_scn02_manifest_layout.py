#!/usr/bin/env python3
"""Normalize SCN-02 one-go manifest slots from parent panel geometry."""

from __future__ import annotations

import json
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[2]
LAYOUT = PROJECT / "Design/VisualLockLayered/SCN-02_MainMenu/scn02_main_menu_layout.json"


def center_rect(parent: list[int], width: int, height: int, dx: int = 0, dy: int = 0) -> list[int]:
    return [
        round(parent[0] + (parent[2] - width) * 0.5 + dx),
        round(parent[1] + (parent[3] - height) * 0.5 + dy),
        width,
        height,
    ]


def set_image(data: dict[str, object], name: str, rect: list[int], fit: str | None = None, z: int | None = None) -> None:
    for item in data["images"]:
        if item["name"] == name:
            item["rect"] = rect
            if fit is not None:
                item["fit"] = fit
            if z is not None:
                item["z"] = z
            return
    raise SystemExit(f"missing image {name}")


def set_text(data: dict[str, object], name: str, rect: list[int], align: str | None = None) -> None:
    for item in data["texts"]:
        if item["name"] == name:
            item["rect"] = rect
            if align is not None:
                item["alignment"] = align
            return
    raise SystemExit(f"missing text {name}")


def main() -> None:
    data = json.loads(LAYOUT.read_text(encoding="utf-8"))

    # Keep resource icons optically centered in the top bar slots, away from the chrome rim.
    set_image(data, "CreditsIcon", [1114, 82, 82, 82], "contain", 500)
    set_image(data, "MaterialsIcon", [1906, 82, 82, 82], "contain", 500)
    set_image(data, "AuthorityIcon", [2729, 76, 88, 88], "contain", 500)

    # Settings uses the explicit target slot, centered in the header's octagonal button cell.
    set_image(data, "SettingsGearIcon", [3664, 100, 72, 72], "contain", 500)

    # Content art sits in inner content windows. Frames remain above it and clip the edges.
    set_image(data, "CommanderProfilePortrait", [92, 395, 643, 438], "cover", 200)
    set_image(data, "ProfileDataStatusStrip", [86, 840, 662, 94], "stretch", 300)
    set_text(data, "CommanderProfilePending", [150, 914, 520, 50], "center")

    set_image(data, "ModeCardArt_Saga", [980, 550, 710, 790], "cover", 200)
    set_image(data, "ModeCardArt_Operation", [1880, 550, 710, 610], "cover", 200)
    set_image(data, "ModeCardArt_QuickCustom", [2826, 550, 710, 790], "cover", 200)

    # Warning row content belongs above row backing but inside the operation card.
    set_image(data, "OperationWarningRowPressure", [1840, 1180, 806, 126], "stretch", 300)
    set_image(data, "OperationWarningRowRisk", [1840, 1348, 806, 126], "stretch", 300)
    set_image(data, "OperationWarningIconPressure", [1868, 1196, 82, 82], "contain", 500)
    set_image(data, "OperationWarningIconRisk", [1868, 1364, 82, 82], "contain", 500)
    set_image(data, "OperationPressureMeter", [1998, 1258, 520, 42], "stretch", 500)
    set_image(data, "OperationRiskMeter", [1998, 1426, 520, 42], "stretch", 500)
    set_text(data, "OperationPressureText", [1970, 1194, 430, 42], "left")
    set_text(data, "OperationPressureHigh", [2470, 1260, 110, 42], "right")
    set_text(data, "OperationRiskText", [1970, 1362, 430, 42], "left")
    set_text(data, "OperationRiskElevated", [2432, 1428, 150, 42], "right")

    # CTA label is centered inside the deploy frame, independent of the generated button art.
    deploy_frame = [2594, 1786, 1204, 260]
    set_text(data, "DeployCommandLabel", center_rect(deploy_frame, 690, 88, dx=-82, dy=-2), "center")
    set_image(data, "DeployCommandChevrons", [3460, 1855, 188, 78], "contain", 500)

    LAYOUT.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    print(f"normalized {LAYOUT}")


if __name__ == "__main__":
    main()
