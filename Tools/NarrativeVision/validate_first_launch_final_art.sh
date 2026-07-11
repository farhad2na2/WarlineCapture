#!/usr/bin/env bash

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/../.." && pwd)

if ! command -v python3 >/dev/null 2>&1; then
    echo "Missing required command: python3" >&2
    exit 69
fi

exec python3 - "$repo_root" <<'PY'
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile


REPO_ROOT = Path(sys.argv[1]).resolve()
FINAL_ART_ROOT = REPO_ROOT / "Design/NarrativeVision/FirstLaunch/ArtReview/FinalArt"
SOURCE_ROOT = FINAL_ART_ROOT / "SourceMasters"
PREVIEW_ROOT = FINAL_ART_ROOT / "Previews"
EVIDENCE_ROOT = FINAL_ART_ROOT / "Evidence"
REPORT_PATH = EVIDENCE_ROOT / "FINAL_ART_VALIDATION.json"
ASSETS_ROOT = REPO_ROOT / "Assets"

SOURCE_PATTERN = re.compile(
    r"^(FL-P(?P<number>\d{2}))_(?P<kind>FinalCandidate|BackgroundCandidate)_R(?P<revision>[1-9]\d*)\.png$"
)
PREVIEW_PATTERN = re.compile(
    r"^(FL-P(?P<number>\d{2}))_(?P<aspect>16x9|20x9)_R(?P<revision>[1-9]\d*)\.png$"
)
PANEL_PREFIX_PATTERN = re.compile(r"^(FL-P(?P<number>\d{2}))_")
EXPECTED_SOURCE_SIZE = (1672, 941)
EXPECTED_PREVIEW_SIZES = {"16x9": (1920, 1080), "20x9": (2400, 1080)}
EXPECTED_ASPECTS = {"source": 16 / 9, "16x9": 16 / 9, "20x9": 20 / 9}
ASPECT_TOLERANCE = 0.002


errors = []
unexpected_files = []


def relative(path):
    try:
        return path.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def add_error(message):
    errors.append(message)


def regular_files(root):
    if not root.is_dir():
        return []
    return sorted(
        (path for path in root.iterdir() if path.is_file()),
        key=lambda path: path.name.encode("utf-8"),
    )


def inspect_png(path, expected_size, expected_aspect):
    result = {
        "dimensions": None,
        "expectedDimensions": f"{expected_size[0]}x{expected_size[1]}",
        "dimensionsValid": False,
        "aspectValid": False,
        "pngIntegrityValid": False,
    }

    if shutil.which("magick") is None:
        return result, "ImageMagick command 'magick' is unavailable"

    try:
        identified = subprocess.run(
            ["magick", "identify", "-quiet", "-format", "%m\t%w\t%h\n", str(path)],
            check=False,
            capture_output=True,
            text=True,
        )
    except OSError as exc:
        return result, f"could not run ImageMagick: {exc}"

    rows = [row for row in identified.stdout.splitlines() if row]
    if identified.returncode != 0 or len(rows) != 1:
        detail = identified.stderr.strip() or "ImageMagick could not identify exactly one PNG frame"
        return result, detail

    fields = rows[0].split("\t")
    if len(fields) != 3 or fields[0] != "PNG":
        return result, f"file format is {fields[0] if fields else 'unknown'}, expected PNG"

    try:
        width, height = int(fields[1]), int(fields[2])
    except (ValueError, IndexError):
        return result, "ImageMagick returned invalid dimensions"

    result["dimensions"] = f"{width}x{height}"
    result["dimensionsValid"] = (width, height) == expected_size
    result["aspectValid"] = height > 0 and abs((width / height) - expected_aspect) <= ASPECT_TOLERANCE

    decoded = subprocess.run(
        ["magick", str(path), "null:"],
        check=False,
        capture_output=True,
        text=True,
    )
    result["pngIntegrityValid"] = decoded.returncode == 0
    if not result["pngIntegrityValid"]:
        return result, decoded.stderr.strip() or "ImageMagick failed to decode the PNG"
    return result, None


def source_entry(path, match):
    return {
        "file": relative(path),
        "candidateType": match.group("kind"),
        "revision": int(match.group("revision")),
    }


def preview_entry(path, match):
    return {
        "file": relative(path),
        "aspect": match.group("aspect"),
        "revision": int(match.group("revision")),
    }


if not SOURCE_ROOT.is_dir():
    add_error(f"Missing source-master directory: {relative(SOURCE_ROOT)}")
if not PREVIEW_ROOT.is_dir():
    add_error(f"Missing preview directory: {relative(PREVIEW_ROOT)}")

source_by_panel = {number: [] for number in range(1, 23)}
preview_by_panel = {
    number: {"16x9": [], "20x9": []} for number in range(1, 23)
}

for path in regular_files(SOURCE_ROOT):
    match = SOURCE_PATTERN.fullmatch(path.name)
    if match is None:
        unexpected_files.append(relative(path))
        add_error(f"Invalid source-master filename: {relative(path)}")
        continue

    number = int(match.group("number"))
    if number not in source_by_panel:
        unexpected_files.append(relative(path))
        add_error(f"Out-of-range source-master panel ID FL-P{number:02d}: {relative(path)}")
        continue
    if match.group("kind") == "BackgroundCandidate" and number != 8:
        unexpected_files.append(relative(path))
        add_error(
            f"BackgroundCandidate naming is accepted only for FL-P08: {relative(path)}"
        )
        continue
    source_by_panel[number].append((path, match))

for path in regular_files(PREVIEW_ROOT):
    match = PREVIEW_PATTERN.fullmatch(path.name)
    if match is None:
        unexpected_files.append(relative(path))
        add_error(f"Invalid preview filename: {relative(path)}")
        continue

    number = int(match.group("number"))
    if number not in preview_by_panel:
        unexpected_files.append(relative(path))
        add_error(f"Out-of-range preview panel ID FL-P{number:02d}: {relative(path)}")
        continue
    preview_by_panel[number][match.group("aspect")].append((path, match))

panels = []
for number in range(1, 23):
    panel_id = f"FL-P{number:02d}"
    sources = source_by_panel[number]
    previews = preview_by_panel[number]

    if not sources:
        add_error(f"Missing source master for panel ID {panel_id}")
    elif len(sources) > 1:
        names = ", ".join(path.name for path, _ in sources)
        add_error(f"Duplicate source-master panel ID {panel_id}: {names}")

    source_records = []
    for path, match in sources:
        record = source_entry(path, match)
        image_result, image_error = inspect_png(
            path, EXPECTED_SOURCE_SIZE, EXPECTED_ASPECTS["source"]
        )
        record.update(image_result)
        source_records.append(record)
        if image_error:
            add_error(f"Invalid PNG source master {relative(path)}: {image_error}")
        if not image_result["dimensionsValid"]:
            add_error(
                f"Wrong source-master dimensions for {relative(path)}: "
                f"{image_result['dimensions'] or 'unknown'}; expected 1672x941"
            )
        if not image_result["aspectValid"]:
            add_error(f"Wrong source-master aspect for {relative(path)}: expected 16:9")

    preview_records = {"16x9": [], "20x9": []}
    for aspect in ("16x9", "20x9"):
        candidates = previews[aspect]
        if not candidates:
            add_error(f"Missing {aspect} preview for panel ID {panel_id}")
        elif len(candidates) > 1:
            names = ", ".join(path.name for path, _ in candidates)
            add_error(f"Duplicate {aspect} preview for panel ID {panel_id}: {names}")

        for path, match in candidates:
            record = preview_entry(path, match)
            image_result, image_error = inspect_png(
                path, EXPECTED_PREVIEW_SIZES[aspect], EXPECTED_ASPECTS[aspect]
            )
            record.update(image_result)
            preview_records[aspect].append(record)
            if image_error:
                add_error(f"Invalid PNG preview {relative(path)}: {image_error}")
            if not image_result["dimensionsValid"]:
                expected = image_result["expectedDimensions"]
                add_error(
                    f"Wrong preview dimensions for {relative(path)}: "
                    f"{image_result['dimensions'] or 'unknown'}; expected {expected}"
                )
            if not image_result["aspectValid"]:
                add_error(f"Wrong preview aspect for {relative(path)}: expected {aspect}")

    if len(sources) == 1:
        source_revision = int(sources[0][1].group("revision"))
        for aspect in ("16x9", "20x9"):
            for path, match in previews[aspect]:
                preview_revision = int(match.group("revision"))
                if preview_revision != source_revision:
                    add_error(
                        f"Revision mismatch for {panel_id}: source is R{source_revision}, "
                        f"but {path.name} is R{preview_revision}"
                    )

    if len(previews["16x9"]) == 1 and len(previews["20x9"]) == 1:
        revision_16 = int(previews["16x9"][0][1].group("revision"))
        revision_20 = int(previews["20x9"][0][1].group("revision"))
        if revision_16 != revision_20:
            add_error(
                f"Preview pair revision mismatch for {panel_id}: "
                f"16x9 is R{revision_16}, 20x9 is R{revision_20}"
            )

    panels.append(
        {
            "panelId": panel_id,
            "sourceMasters": source_records,
            "previews": preview_records,
        }
    )

asset_leaks = []
if ASSETS_ROOT.is_dir():
    for path in sorted(ASSETS_ROOT.rglob("*"), key=lambda item: item.as_posix().encode("utf-8")):
        if not path.is_file():
            continue
        match = PANEL_PREFIX_PATTERN.match(path.name)
        if match and 1 <= int(match.group("number")) <= 22:
            asset_leaks.append(relative(path))

for leak in asset_leaks:
    add_error(f"Gate 6 final-art file must not be under Assets/: {leak}")

errors = sorted(set(errors), key=lambda value: value.encode("utf-8"))
unexpected_files = sorted(set(unexpected_files), key=lambda value: value.encode("utf-8"))

report = {
    "schemaVersion": 1,
    "gate": "Gate 6",
    "status": "pass" if not errors else "fail",
    "workspace": relative(FINAL_ART_ROOT),
    "expectedPanelCount": 22,
    "requirements": {
        "sourceMasterDimensions": "1672x941",
        "sourceMasterAspect": "16:9",
        "previewDimensions": {"16x9": "1920x1080", "20x9": "2400x1080"},
        "acceptedSourceNaming": "FL-P##_FinalCandidate_R#.png; FL-P08_BackgroundCandidate_R#.png is also accepted",
        "pngIntegrity": "single-frame PNG fully decodable by ImageMagick",
        "runtimeAssetFilesAllowed": False,
    },
    "panels": panels,
    "assetLeaks": asset_leaks,
    "unexpectedFiles": unexpected_files,
    "errors": errors,
}

EVIDENCE_ROOT.mkdir(parents=True, exist_ok=True)
with tempfile.NamedTemporaryFile(
    mode="w", encoding="utf-8", dir=EVIDENCE_ROOT, prefix=".final-art-validation.", delete=False
) as handle:
    json.dump(report, handle, indent=2, ensure_ascii=True)
    handle.write("\n")
    temporary_report = Path(handle.name)
os.replace(temporary_report, REPORT_PATH)

if errors:
    print(f"Gate 6 final-art validation FAILED with {len(errors)} error(s):", file=sys.stderr)
    for error in errors:
        print(f"  - {error}", file=sys.stderr)
    print(f"Validation report: {relative(REPORT_PATH)}", file=sys.stderr)
    raise SystemExit(1)

print("Gate 6 final-art validation PASSED for 22 panel IDs.")
print(f"Validation report: {relative(REPORT_PATH)}")
PY
