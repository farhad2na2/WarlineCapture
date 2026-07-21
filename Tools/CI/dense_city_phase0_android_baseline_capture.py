#!/usr/bin/env python3
"""Capture Phase 0 dense-city Android baseline evidence for the wired reference device.

This is characterization evidence for the current static-presentation map revision.
It does not accept Phase 9 gates.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import time
from pathlib import Path
from typing import Any


PACKAGE = "com.warlinecapture.game"
ACTIVITY = "com.unity3d.player.UnityPlayerGameActivity"
DEVICE_SERIAL_DEFAULT = "R4M7PZEQZ58T59ZH"


def run(adb: list[str], *args: str, timeout: float = 120.0) -> str:
    completed = subprocess.run(
        [*adb, *args],
        capture_output=True,
        text=True,
        timeout=timeout,
        check=False,
    )
    if completed.returncode != 0:
        raise RuntimeError(
            f"adb {' '.join(args)} failed ({completed.returncode}): {completed.stderr.strip()}"
        )
    return completed.stdout


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parse_meminfo_kb(text: str, label: str) -> int:
    patterns = (
        rf"^\s*{re.escape(label)}:\s+(\d+)\s*$",
        rf"^\s*{re.escape(label)}\s+(\d+)\s*$",
        rf"^\s*{re.escape(label)}:\s+(\d+)\s+",
        rf"^\s*{re.escape(label)}\s+(\d+)\s+",
    )
    for pattern in patterns:
        match = re.search(pattern, text, re.MULTILINE)
        if match is not None:
            return int(match.group(1))
    # TOTAL PSS/RSS often appear near the summary footer.
    if label == "TOTAL PSS":
        match = re.search(r"TOTAL PSS:\s+(\d+)", text)
        if match:
            return int(match.group(1))
        match = re.search(r"^TOTAL\s+(\d+)\s+", text, re.MULTILINE)
        if match:
            return int(match.group(1))
    if label == "TOTAL RSS":
        match = re.search(r"TOTAL RSS:\s+(\d+)", text)
        if match:
            return int(match.group(1))
    raise RuntimeError(f"Missing meminfo label: {label}")


def launch_match(adb: list[str]) -> None:
    run(
        adb,
        "shell",
        "am",
        "start",
        "-W",
        "-S",
        "-a",
        "android.intent.action.MAIN",
        "-c",
        "android.intent.category.LAUNCHER",
        "-n",
        f"{PACKAGE}/{ACTIVITY}",
        "--es",
        "unity",
        "-warlineAutoStartMatch",
        timeout=180.0,
    )


def launch_menu(adb: list[str]) -> None:
    run(
        adb,
        "shell",
        "am",
        "start",
        "-W",
        "-S",
        "-a",
        "android.intent.action.MAIN",
        "-c",
        "android.intent.category.LAUNCHER",
        "-n",
        f"{PACKAGE}/{ACTIVITY}",
        timeout=180.0,
    )


def parse_package_code_path(text: str) -> str:
    match = re.search(r"^\s*codePath=(\S+)\s*$", text, re.MULTILINE)
    if match is None:
        raise RuntimeError("Missing package codePath")
    return match.group(1)


def parse_du_bytes(output: str, expected_path: str) -> int:
    rows = [line.strip() for line in output.splitlines() if line.strip()]
    if len(rows) != 1:
        raise RuntimeError(f"du must return exactly one row for {expected_path}")
    match = re.fullmatch(r"(\d+)\s+(.+)", rows[0])
    if match is None or match.group(2) != expected_path:
        raise RuntimeError(f"du path or byte count is malformed for {expected_path}: {rows[0]!r}")
    size = int(match.group(1))
    if size <= 0:
        raise RuntimeError(f"du size must be positive for {expected_path}")
    return size


def measure_installed_artifact_bytes(adb: list[str], code_path: str) -> dict[str, int]:
    """MIUI/Android 16 dumpsys package omits codeSize/dataSize/cacheSize; use du instead."""
    normalized = code_path.rstrip("/")
    base_apk_path = f"{normalized}/base.apk"
    native_library_path = f"{normalized}/lib"
    base_apk_bytes = parse_du_bytes(
        run(adb, "shell", "du", "-sb", base_apk_path, timeout=60.0),
        base_apk_path,
    )
    native_library_bytes = parse_du_bytes(
        run(adb, "shell", "du", "-sb", native_library_path, timeout=60.0),
        native_library_path,
    )
    return {
        "baseApkBytes": base_apk_bytes,
        "nativeLibraryBytes": native_library_bytes,
        "installedApproximateBytes": base_apk_bytes + native_library_bytes,
    }


def find_unity_blast_layer(adb: list[str]) -> str | None:
    listing = run(adb, "shell", "dumpsys", "SurfaceFlinger", "--list", timeout=30.0)
    needle = f"SurfaceView[{PACKAGE}/{ACTIVITY}](BLAST)"
    for line in listing.splitlines():
        if needle not in line:
            continue
        start = line.find("{")
        if start < 0:
            continue
        # RequestedLayerState{HEX SurfaceView[...](BLAST)#id parentId=...}
        rest = line[start + 1 :]
        match = re.match(
            rf"^(?P<name>\S+\s+{re.escape(needle)}#\d+)\s",
            rest,
        )
        if match:
            return match.group("name")
    return None


def parse_surfaceflinger_latency_fps(text: str) -> tuple[float | None, float | None]:
    """Return (fps, max_queue_ms) from SurfaceFlinger --latency output.

    Filters Long.MAX_VALUE pending-frame sentinels and non-monotonic present times.
    """
    presents: list[int] = []
    for line in text.splitlines():
        parts = line.strip().replace("\t", " ").split()
        if len(parts) != 3:
            continue
        try:
            actual = int(parts[1])
        except ValueError:
            continue
        # 0 = empty slot; Long.MAX_VALUE = pending/unset present time on some devices.
        if actual <= 0 or actual >= 2**63 - 1:
            continue
        presents.append(actual)
    if len(presents) < 3:
        return None, None
    deltas_ms = [
        (presents[i] - presents[i - 1]) / 1_000_000.0
        for i in range(1, len(presents))
        if presents[i] > presents[i - 1]
    ]
    if not deltas_ms:
        return None, None
    # Prefer median inter-frame interval; ignores occasional large hitch outliers for FPS.
    deltas_sorted = sorted(deltas_ms)
    median_ms = deltas_sorted[len(deltas_sorted) // 2]
    if median_ms <= 0:
        return None, None
    return 1000.0 / median_ms, max(deltas_ms)


def capture_surfaceflinger_fps(adb: list[str], seconds: int) -> dict[str, Any]:
    samples: list[float] = []
    max_queue_ms: list[float] = []
    layer = find_unity_blast_layer(adb)
    last: dict[str, Any] = {
        "layer": layer,
        "totalFrames": None,
        "jankyFrames": None,
        "p50FrameMs": None,
        "p90FrameMs": None,
        "p95FrameMs": None,
        "p99FrameMs": None,
        "rawSurfaceFlingerBytes": 0,
        "latencyRowCount": 0,
        "averageMaxQueueMs": None,
        "maximumQueueMs": None,
    }
    for _ in range(seconds):
        if layer is None:
            layer = find_unity_blast_layer(adb)
            last["layer"] = layer
        text = ""
        if layer is not None:
            # Clear then sample one second of presents, matching prior characterization cadence.
            run(adb, "shell", "dumpsys SurfaceFlinger --latency-clear " + repr(layer), timeout=30.0)
            time.sleep(1.0)
            # Remote sh needs a quoted layer name (spaces + brackets + parens).
            remote = "dumpsys SurfaceFlinger --latency " + repr(layer)
            text = run(adb, "shell", remote, timeout=30.0)
            fps, queue_ms = parse_surfaceflinger_latency_fps(text)
            if fps is not None:
                samples.append(fps)
            if queue_ms is not None:
                max_queue_ms.append(queue_ms)
            presents = 0
            for line in text.splitlines():
                parts = line.strip().replace("\t", " ").split()
                if len(parts) != 3:
                    continue
                try:
                    actual = int(parts[1])
                except ValueError:
                    continue
                if 0 < actual < 2**63 - 1:
                    presents += 1
            last["rawSurfaceFlingerBytes"] = len(text)
            last["latencyRowCount"] = presents
            # Already slept 1s for the latency window; skip extra sleep below.
            gfx = run(adb, "shell", "dumpsys", "gfxinfo", PACKAGE, "framestats", timeout=30.0)
            total = re.search(r"Total frames rendered:\s+(\d+)", gfx)
            janky = re.search(r"Janky frames:\s+(\d+)", gfx)
            p50 = re.search(r"50th percentile:\s+(\d+)ms", gfx)
            p90 = re.search(r"90th percentile:\s+(\d+)ms", gfx)
            p95 = re.search(r"95th percentile:\s+(\d+)ms", gfx)
            p99 = re.search(r"99th percentile:\s+(\d+)ms", gfx)
            last.update(
                {
                    "totalFrames": int(total.group(1)) if total else None,
                    "jankyFrames": int(janky.group(1)) if janky else None,
                    "p50FrameMs": float(p50.group(1)) if p50 else None,
                    "p90FrameMs": float(p90.group(1)) if p90 else None,
                    "p95FrameMs": float(p95.group(1)) if p95 else None,
                    "p99FrameMs": float(p99.group(1)) if p99 else None,
                }
            )
            if not samples and p50 and float(p50.group(1)) > 0:
                samples.append(1000.0 / float(p50.group(1)))
            continue
        gfx = run(adb, "shell", "dumpsys", "gfxinfo", PACKAGE, "framestats", timeout=30.0)
        total = re.search(r"Total frames rendered:\s+(\d+)", gfx)
        janky = re.search(r"Janky frames:\s+(\d+)", gfx)
        p50 = re.search(r"50th percentile:\s+(\d+)ms", gfx)
        p90 = re.search(r"90th percentile:\s+(\d+)ms", gfx)
        p95 = re.search(r"95th percentile:\s+(\d+)ms", gfx)
        p99 = re.search(r"99th percentile:\s+(\d+)ms", gfx)
        last.update(
            {
                "totalFrames": int(total.group(1)) if total else None,
                "jankyFrames": int(janky.group(1)) if janky else None,
                "p50FrameMs": float(p50.group(1)) if p50 else None,
                "p90FrameMs": float(p90.group(1)) if p90 else None,
                "p95FrameMs": float(p95.group(1)) if p95 else None,
                "p99FrameMs": float(p99.group(1)) if p99 else None,
            }
        )
        if not samples and p50 and float(p50.group(1)) > 0:
            samples.append(1000.0 / float(p50.group(1)))
        time.sleep(1.0)
    samples_sorted = sorted(samples)
    if max_queue_ms:
        last["averageMaxQueueMs"] = sum(max_queue_ms) / len(max_queue_ms)
        last["maximumQueueMs"] = max(max_queue_ms)
    return {
        "sampleWindows": seconds,
        "source": "SurfaceFlingerLatency" if layer and samples_sorted else "GfxinfoFallback",
        "averageFps": sum(samples_sorted) / len(samples_sorted) if samples_sorted else None,
        "minimumFps": samples_sorted[0] if samples_sorted else None,
        "maximumFps": samples_sorted[-1] if samples_sorted else None,
        "p10Fps": samples_sorted[max(0, int(len(samples_sorted) * 0.10) - 1)] if samples_sorted else None,
        "p90Fps": samples_sorted[min(len(samples_sorted) - 1, int(len(samples_sorted) * 0.90))] if samples_sorted else None,
        "lastGfxinfo": last,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--apk", required=True, type=Path)
    parser.add_argument("--serial", default=DEVICE_SERIAL_DEFAULT)
    parser.add_argument("--git-commit", required=True)
    parser.add_argument("--dirty", action="store_true")
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--warmup-seconds", type=int, default=45)
    parser.add_argument("--steady-seconds", type=int, default=60)
    parser.add_argument("--match-ready-upper-bound-seconds", type=int, default=40)
    parser.add_argument(
        "--skip-install",
        action="store_true",
        help="Reuse the already-installed package (same APK already on device).",
    )
    args = parser.parse_args()

    apk = args.apk.resolve()
    if not apk.is_file():
        raise SystemExit(f"APK missing: {apk}")

    out = args.output_dir.resolve()
    out.mkdir(parents=True, exist_ok=True)
    adb = ["adb", "-s", args.serial]

    devices = run(["adb"], "devices", "-l")
    if args.serial not in devices or "device" not in devices:
        raise SystemExit(f"Required device not online: {args.serial}")

    apk_sha = sha256_file(apk)
    apk_bytes = apk.stat().st_size

    if not args.skip_install:
        run(adb, "install", "-r", "-d", str(apk), timeout=600.0)
    run(adb, "shell", "am", "force-stop", PACKAGE)
    run(adb, "logcat", "-c")

    launch_start = time.monotonic()
    launch_match(adb)

    # Wait for match-ready upper bound used by prior characterization.
    time.sleep(args.match_ready_upper_bound_seconds)
    load_upper_bound_s = time.monotonic() - launch_start
    (out / "match-40s-preview.png").write_bytes(
        subprocess.check_output([*adb, "exec-out", "screencap", "-p"])
    )

    menu_or_match = "match"
    meminfo = run(adb, "shell", "dumpsys", "meminfo", PACKAGE)
    (out / f"{menu_or_match}-meminfo.txt").write_text(meminfo, encoding="utf-8")

    time.sleep(args.warmup_seconds)
    run(adb, "shell", "dumpsys", "gfxinfo", PACKAGE, "reset")
    fps = capture_surfaceflinger_fps(adb, args.steady_seconds)
    steady_meminfo = run(adb, "shell", "dumpsys", "meminfo", PACKAGE)
    (out / "steady-meminfo.txt").write_text(steady_meminfo, encoding="utf-8")
    gfxinfo = run(adb, "shell", "dumpsys", "gfxinfo", PACKAGE)
    (out / "steady-gfxinfo.txt").write_text(gfxinfo, encoding="utf-8")
    logcat = run(adb, "logcat", "-d", "-t", "400")
    (out / "steady-logcat.txt").write_text(logcat, encoding="utf-8")

    package_dump = run(adb, "shell", "dumpsys", "package", PACKAGE)
    (out / "package-dump.txt").write_text(package_dump, encoding="utf-8")
    code_path = parse_package_code_path(package_dump)
    sizes = measure_installed_artifact_bytes(adb, code_path)

    # Unload / return-to-menu approximation: force-stop and relaunch without auto-match.
    unload_start = time.monotonic()
    run(adb, "shell", "am", "force-stop", PACKAGE)
    launch_menu(adb)
    time.sleep(15.0)
    unload_s = time.monotonic() - unload_start
    menu_meminfo = run(adb, "shell", "dumpsys", "meminfo", PACKAGE)
    (out / "menu-after-unload-meminfo.txt").write_text(menu_meminfo, encoding="utf-8")

    draw_calls = None
    triangles = None
    gc_hint = None
    update_ms = None
    main_thread_ms = None
    gpu_ms = None
    for pattern, target in (
        (r"drawCalls?=(\d+)", "draw"),
        (r"triangles?=(\d+)", "tri"),
        (r"avgUpdateMs=([0-9.]+)", "update"),
        (r"mainThreadMs=([0-9.]+)", "main"),
        (r"gpuMs=([0-9.]+)", "gpu"),
        (r"gcAlloc(?:Bytes)?=(\d+)", "gc"),
    ):
        match = re.search(pattern, logcat, re.IGNORECASE)
        if match is None:
            continue
        if target == "draw":
            draw_calls = int(match.group(1))
        elif target == "tri":
            triangles = int(match.group(1))
        elif target == "update":
            update_ms = float(match.group(1))
        elif target == "main":
            main_thread_ms = float(match.group(1))
        elif target == "gpu":
            gpu_ms = float(match.group(1))
        elif target == "gc":
            gc_hint = int(match.group(1))

    report = {
        "reportSchema": "warline.dense-city.phase0-android-baseline",
        "reportSchemaVersion": 1,
        "result": "CharacterizationCaptured",
        "gitCommit": args.git_commit,
        "dirtyWorktree": bool(args.dirty),
        "deviceSerial": args.serial,
        "packageName": PACKAGE,
        "apk": {
            "path": str(apk),
            "bytes": apk_bytes,
            "sha256": apk_sha,
        },
        "installed": {
            "codePath": code_path,
            **sizes,
        },
        "timing": {
            "matchReadyUpperBoundSeconds": load_upper_bound_s,
            "unloadForceStopToMenuSeconds": unload_s,
            "warmupSeconds": args.warmup_seconds,
            "steadySeconds": args.steady_seconds,
        },
        "memoryKiB": {
            "matchTotalPss": parse_meminfo_kb(meminfo, "TOTAL PSS"),
            "matchTotalRss": parse_meminfo_kb(meminfo, "TOTAL RSS"),
            "matchGraphics": parse_meminfo_kb(meminfo, "Graphics"),
            "matchNativeHeap": parse_meminfo_kb(meminfo, "Native Heap"),
            "steadyTotalPss": parse_meminfo_kb(steady_meminfo, "TOTAL PSS"),
            "menuAfterUnloadTotalPss": parse_meminfo_kb(menu_meminfo, "TOTAL PSS"),
        },
        "frame": fps,
        "diagnosticsFromLogcat": {
            "drawCalls": draw_calls,
            "triangles": triangles,
            "averageUpdateMs": update_ms,
            "mainThreadMs": main_thread_ms,
            "gpuMs": gpu_ms,
            "gcAllocBytesHint": gc_hint,
        },
        "notes": [
            "Match-ready timing is an upper bound from a fixed wait, matching prior characterization method.",
            "Installed size uses du -sb on base.apk + lib because MIUI dumpsys package omits codeSize fields.",
            "FPS samples prefer SurfaceFlinger BLAST-layer --latency; gfxinfo is Unity-empty on this device.",
            "Draw/GC values are best-effort logcat parses and may be null if diagnostics markers are absent.",
            "This report is Phase 0 baseline characterization, not Phase 9 acceptance.",
        ],
    }
    report_path = out / "dense-city-phase0-android-baseline.json"
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"result": "ok", "report": str(report_path), "apkSha256": apk_sha}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
