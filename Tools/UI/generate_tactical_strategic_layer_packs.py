#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
VISUAL_LOCK = ROOT / "Design" / "VisualLock"
LAYERED = ROOT / "Design" / "VisualLockLayered"
FONT_DIR = ROOT / "Assets" / "Synty" / "InterfaceMilitaryCombatHUD" / "Fonts" / "Oxanium"
W, H = 1672, 941


COLORS = {
    "panel": (15, 24, 30, 218),
    "panel2": (20, 32, 40, 232),
    "cyan": (80, 214, 232, 235),
    "cyan_dim": (42, 112, 128, 180),
    "orange": (242, 154, 52, 235),
    "green": (98, 202, 123, 220),
    "red": (232, 72, 68, 230),
    "text": (229, 241, 242, 245),
    "muted": (147, 174, 180, 230),
}


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    path = FONT_DIR / name
    if path.exists():
        return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


FONT_LIGHT_18 = font("Oxanium-Light.ttf", 18)
FONT_LIGHT_22 = font("Oxanium-Light.ttf", 22)
FONT_LIGHT_28 = font("Oxanium-Light.ttf", 28)
FONT_BOLD_24 = font("Oxanium-Bold.ttf", 24)
FONT_BOLD_30 = font("Oxanium-Bold.ttf", 30)
FONT_BOLD_38 = font("Oxanium-Bold.ttf", 38)


def cut_poly(rect: tuple[int, int, int, int], cut: int) -> list[tuple[int, int]]:
    x1, y1, x2, y2 = rect
    return [
        (x1 + cut, y1),
        (x2 - cut, y1),
        (x2, y1 + cut),
        (x2, y2 - cut),
        (x2 - cut, y2),
        (x1 + cut, y2),
        (x1, y2 - cut),
        (x1, y1 + cut),
    ]


def panel_layer(rect, accent=COLORS["cyan"], fill=COLORS["panel"], cut=16, border=2) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon(cut_poly(rect, cut), fill=fill, outline=accent)
    for i in range(1, border):
        x1, y1, x2, y2 = rect
        d.line(cut_poly((x1 + i, y1 + i, x2 - i, y2 - i), max(1, cut - i)), fill=accent, width=1)
    return im


def line_layer(points, color, width=5) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(im).line(points, fill=color, width=width)
    return im


def text_layer(xy, text, fill=COLORS["text"], font_obj=FONT_LIGHT_22) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(im).text(xy, text, font=font_obj, fill=fill)
    return im


def ellipse_layer(rect, fill, outline=None, width=1) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse(rect, fill=fill, outline=outline, width=width)
    return im


def ring_layer(cx, cy, rx, ry, color, width=4) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for i in range(width):
        d.ellipse((cx - rx - i, cy - ry - i, cx + rx + i, cy + ry + i), outline=color)
    return im


def arrow_layer(x, y, color=COLORS["cyan"]) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(im).polygon([(x, y), (x + 18, y + 10), (x, y + 20), (x + 5, y + 10)], fill=color)
    return im


def target_crop_layer(source: Image.Image, box: tuple[int, int, int, int], cut: int = 0) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    crop = source.crop(box).convert("RGBA")
    if cut > 0:
        mask = Image.new("L", crop.size, 0)
        d = ImageDraw.Draw(mask)
        d.polygon(cut_poly((0, 0, crop.width - 1, crop.height - 1), cut), fill=255)
        crop.putalpha(mask)
    im.alpha_composite(crop, box[:2])
    return im


def panel_layer_local(width: int, height: int, accent=COLORS["cyan"], fill=COLORS["panel"], cut=14, border=2) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    local = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(local)
    polygon = cut_poly((0, 0, width - 1, height - 1), cut)
    d.polygon(polygon, fill=fill, outline=accent)
    if border > 1:
        d.line(polygon + [polygon[0]], fill=accent, width=border)
    inner = cut_poly((5, 5, width - 6, height - 6), max(1, cut - 5))
    d.line(inner + [inner[0]], fill=(accent[0], accent[1], accent[2], 95), width=1)
    im.alpha_composite(local, (0, 0))
    return im


def local_ring_layer(width: int, height: int, color, rx: int, ry: int, line_width: int = 4) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    local = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(local)
    cx, cy = width // 2, height // 2
    for i in range(line_width):
        d.ellipse((cx - rx - i, cy - ry - i, cx + rx + i, cy + ry + i), outline=color)
    im.alpha_composite(local, (0, 0))
    return im


def local_move_marker_layer(width: int, height: int) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    local = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(local)
    cx, cy = width // 2, height // 2
    for radius in (48, 32):
        d.ellipse((cx - radius, cy - radius * 0.55, cx + radius, cy + radius * 0.55), outline=(255, 164, 35, 235), width=3)
    d.line((cx - 56, cy, cx + 56, cy), fill=(255, 164, 35, 190), width=2)
    d.line((cx, cy - 34, cx, cy + 34), fill=(255, 164, 35, 190), width=2)
    d.polygon([(cx + 12, cy - 44), (cx + 48, cy - 70), (cx + 35, cy - 25)], fill=(255, 164, 35, 245))
    im.alpha_composite(local, (0, 0))
    return im


def local_arrow_icon_layer(width: int, height: int, color=(255, 164, 35, 245)) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    local = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(local)
    d.polygon([(8, height // 2 - 10), (width - 8, height // 2), (8, height // 2 + 10), (18, height // 2)], fill=color)
    im.alpha_composite(local, (0, 0))
    return im


def local_unit_marker_layer(width: int, height: int, color) -> Image.Image:
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    local = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    d = ImageDraw.Draw(local)
    cx, cy = width // 2, height // 2
    d.ellipse((cx - 28, cy - 26, cx + 28, cy + 26), fill=(color[0], color[1], color[2], 145), outline=(color[0], color[1], color[2], 230), width=3)
    im.alpha_composite(local, (0, 0))
    return im


def crop_and_save(layer: Image.Image, out_path: Path) -> dict | None:
    bbox = layer.getbbox()
    if not bbox:
        return None
    cropped = layer.crop(bbox)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    cropped.save(out_path)
    return {"bounds": list(bbox), "size": list(cropped.size)}


def write_pack(surface: str, target_rel: str, layers: list[dict], readme_extra: str) -> None:
    root = LAYERED / surface
    layers_dir = root / "layers"
    reference_dir = root / "reference"
    generated_dir = root / "generated_one_go"
    if root.exists():
        shutil.rmtree(root)
    layers_dir.mkdir(parents=True, exist_ok=True)
    reference_dir.mkdir(parents=True, exist_ok=True)
    generated_dir.mkdir(parents=True, exist_ok=True)

    target_src = ROOT / target_rel
    target_dst = reference_dir / f"{surface}_Landscape_Target.png"
    shutil.copy2(target_src, target_dst)

    manifest_layers = []
    thumbs = []
    for spec in layers:
        image = spec.pop("image")
        rel = Path(spec["file"])
        info = crop_and_save(image, root / rel)
        if not info:
            continue
        spec.update(info)
        manifest_layers.append(spec)
        thumbs.append((spec["id"], root / rel))

    contact_sheet(thumbs, generated_dir / "layers_contact_sheet.png")
    manifest = {
        "schema": "warlinecapture.ui.layerPack.v2",
        "surface": surface,
        "referenceResolution": {"width": W, "height": H},
        "target": target_rel,
        "reviewSheet": str((generated_dir / "layers_contact_sheet.png").relative_to(ROOT)),
        "layersRoot": str(layers_dir.relative_to(ROOT)),
        "rules": [
            "This pack is the implementation gate for the refreshed tactical/strategic state target.",
            "Do not use the flattened target as a Unity UI image.",
            "TMP text remains live text in Unity; text layers here are reference-only unless explicitly marked as icon/symbol.",
            "Runtime units, minimap viewport, command markers, build footprints, and ARIA highlights must remain separate runtime layers.",
        ],
        "layers": manifest_layers,
    }
    (root / "layer_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    (root / "README.md").write_text(
        f"# {surface} Layer Pack\n\n"
        f"Reference target: `{target_rel}`\n\n"
        f"{readme_extra}\n\n"
        "This folder satisfies the layer-pack gate for target review. Unity implementation must still create a target-to-canvas mapping before prefab edits.\n",
        encoding="utf-8",
    )


def contact_sheet(items: list[tuple[str, Path]], out_path: Path) -> None:
    cell_w, cell_h = 260, 184
    cols = 4
    rows = max(1, (len(items) + cols - 1) // cols)
    sheet = Image.new("RGBA", (cols * cell_w, rows * cell_h), (8, 13, 17, 255))
    d = ImageDraw.Draw(sheet)
    for idx, (label, path) in enumerate(items):
        im = Image.open(path).convert("RGBA")
        im.thumbnail((cell_w - 24, cell_h - 48), Image.Resampling.LANCZOS)
        x = (idx % cols) * cell_w
        y = (idx // cols) * cell_h
        sheet.alpha_composite(im, (x + (cell_w - im.width) // 2, y + 12))
        d.text((x + 12, y + cell_h - 30), label[:32], font=FONT_LIGHT_18, fill=COLORS["text"])
    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(out_path, quality=94)


def scn08_layers() -> list[dict]:
    target = Image.open(VISUAL_LOCK / "SCN-08_RTSBattleHUD_M01_TacticalFeedback" / "SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png").convert("RGBA")
    layers = []
    def add(id_, file_, image, role, object_path, layer_type, alpha_rule, target_rect=None):
        layers.append({
            "id": id_,
            "file": f"layers/{file_}",
            "role": role,
            "targetLayerType": layer_type,
            "alphaRule": alpha_rule,
            "bindings": [{"objectPath": object_path, "targetRect": target_rect or []}],
            "image": image,
        })
    add("tactical_context_overlay", "Content/tactical_context_overlay.png",
        target_crop_layer(target, (405, 104, 1298, 626)), "content", "Screen_MatchOverlay/TacticalContextPreview", "content image", "reference-only map context crop; not a Unity HUD chrome source", [405, 104, 1298, 626])
    add("command_path_line", "Markers/command_path_line.png", line_layer((803, 456, 938, 535, 1115, 384, 1204, 300), (255, 155, 35, 235), 6), "marker", "Screen_MatchOverlay/WorldCommandMarkerLayer/PathPreview", "runtime marker", "separate runtime overlay")
    add("move_destination_ring", "Markers/move_destination_ring.png", local_move_marker_layer(146, 116), "marker", "Screen_MatchOverlay/WorldCommandMarkerLayer/MoveDestinationMarker", "runtime marker", "transparent runtime marker; no map pixels")
    add("move_arrow_icon", "Icons/move_arrow_icon.png", local_arrow_icon_layer(50, 52), "icon", "Screen_MatchOverlay/WorldCommandMarkerLayer/MoveDestinationMarker/Icon", "dynamic icon", "transparent icon sprite")
    add("selection_ring", "Markers/selection_ring.png", local_ring_layer(192, 100, (41, 213, 255, 235), 88, 42, 4), "marker", "Screen_MatchOverlay/WorldCommandMarkerLayer/SelectionRing", "runtime marker", "transparent runtime marker; no map pixels")
    add("attack_target_ring", "Markers/attack_target_ring.png", local_ring_layer(164, 96, (245, 70, 48, 235), 72, 38, 4), "marker", "Screen_MatchOverlay/WorldCommandMarkerLayer/AttackTargetMarker", "runtime marker", "transparent runtime marker; no map pixels")
    add("friendly_unit_placeholder", "Content/friendly_unit_placeholder.png", local_unit_marker_layer(92, 76, (42, 190, 255)), "content", "Screen_MatchOverlay/RuntimeEntityLayer/FriendlySquadPreview", "content image", "temporary transparent runtime unit proxy")
    add("enemy_unit_placeholder", "Content/enemy_unit_placeholder.png", local_unit_marker_layer(90, 72, (238, 69, 56)), "content", "Screen_MatchOverlay/RuntimeEntityLayer/EnemyPatrolPreview", "content image", "temporary transparent runtime unit proxy")
    add("command_mode_banner_frame", "Frames/command_mode_banner_frame.png", panel_layer_local(385, 71, COLORS["orange"], (10, 18, 22, 238), 12, 2), "frame", "Screen_MatchOverlay/CommandModeBanner/Frame", "solid cut-corner backplate", "transparent outside; no baked text or icons")
    add("selected_entity_panel_frame", "Frames/selected_entity_panel_frame.png", panel_layer_local(332, 244, COLORS["cyan"], (10, 22, 28, 238), 14, 2), "frame", "Screen_MatchOverlay/SelectedEntityPanel/Frame", "solid cut-corner backplate", "transparent outside; no baked text or icons")
    add("invalid_command_toast_frame", "Frames/invalid_command_toast_frame.png", panel_layer_local(328, 44, COLORS["red"], (52, 14, 16, 236), 8, 2), "frame", "Screen_MatchOverlay/InvalidCommandToast/Frame", "solid cut-corner backplate", "transparent outside; no baked text or icons")
    add("minimap_bridge_frame", "Frames/minimap_bridge_frame.png", panel_layer_local(322, 302, COLORS["cyan"], (4, 14, 18, 225), 16, 2), "frame", "Screen_MatchOverlay/MiniMapPanel/MinimapCameraBridge/Frame", "solid cut-corner backplate", "transparent outside; no baked minimap content")
    viewport = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(viewport).rectangle((1408, 720, 1560, 812), outline=COLORS["orange"], width=3)
    add("minimap_viewport_rect", "Markers/minimap_viewport_rect.png", viewport, "marker", "Screen_MatchOverlay/MiniMapPanel/MinimapCameraBridge/ViewportRect", "runtime marker", "transparent marker overlay")
    return layers


def generic_state_layers(surface: str) -> list[dict]:
    target = Image.open(VISUAL_LOCK / surface / f"{surface}_Landscape_Target.png").convert("RGBA")
    # A minimal but valid review pack for state targets: a target reference plate and separate callout overlay.
    # Canvas work must still decompose the target-to-canvas mapping before prefab edits.
    callout = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(callout)
    d.rectangle((74, 58, 728, 132), outline=COLORS["orange"], width=3)
    d.text((96, 78), surface.replace("_", " "), font=FONT_BOLD_24, fill=COLORS["orange"])
    return [
        {
            "id": "state_reference_plate",
            "file": "layers/Content/state_reference_plate.png",
            "role": "reference",
            "targetLayerType": "content image",
            "alphaRule": "review-only flattened state reference; not a Unity chrome source",
            "bindings": [{"objectPath": f"{surface}/ReferenceOnly", "targetRect": [0, 0, W, H]}],
            "image": target,
        },
        {
            "id": "state_title_callout",
            "file": "layers/Overlays/state_title_callout.png",
            "role": "annotation",
            "targetLayerType": "dynamic label/reference",
            "alphaRule": "transparent outside callout",
            "bindings": [{"objectPath": f"{surface}/StateTitle", "targetRect": [74, 58, 728, 132]}],
            "image": callout,
        },
    ]


def main() -> None:
    write_pack(
        "SCN-08_RTSBattleHUD_M01_TacticalFeedback",
        "Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png",
        scn08_layers(),
        "M01 tactical HUD state pack. Includes separated runtime marker, command banner, selected entity, invalid toast, minimap viewport, and tactical context layers.",
    )
    for surface in [
        "SCN-09_BuildDrawer_M01DisabledState",
        "SCN-10_UnitCommandWheel_TargetingState",
        "POP-01_ThreatAlert_RoutePreviewState",
        "POP-03_BuildPlacement_MetadataValidityState",
        "POP-05_MissionResult_M01ContractState",
        "PREFAB-04_AssistantButton",
        "PREFAB-05_AssistantPanel",
        "PREFAB-06_TutorialCard",
        "PREFAB-07_TutorialHighlight",
        "POP-10_AssistantTakeover",
        "POP-11_CommanderIdentity",
    ]:
        write_pack(
            surface,
            f"Design/VisualLock/{surface}/{surface}_Landscape_Target.png",
            generic_state_layers(surface),
            "Initial state-target layer pack. Before Unity Canvas implementation, expand this pack into object-level layers from the target-to-canvas mapping.",
        )


if __name__ == "__main__":
    main()
