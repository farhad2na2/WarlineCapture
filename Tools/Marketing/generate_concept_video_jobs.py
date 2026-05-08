#!/usr/bin/env python3
"""Submit, poll, download, and verify WarlineCapture generative trailer clips.

Default mode is a dry run that validates the concept package and writes a job
plan without calling any external API. `--provider openai-sora` can submit real
jobs when `OPENAI_API_KEY` is set and network access is available.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
SHOT_PACKAGE = ROOT / "Design" / "Marketing" / "GenerativeVideoConcepts" / "WarlineCapture_Generative_Cinematic_Shots.json"
OUTPUT_DIR = ROOT / "Design" / "Marketing" / "GenerativeVideoConcepts" / "Outputs"
JOB_PLAN_PATH = OUTPUT_DIR / "WarlineCapture_Generative_Cinematic_JobPlan.json"
QA_REPORT_PATH = OUTPUT_DIR / "WarlineCapture_Generative_Cinematic_QA_Report.md"
STORYBOARD_PATH = OUTPUT_DIR / "WarlineCapture_Generative_Cinematic_Storyboard.png"

OPENAI_VIDEO_ENDPOINT = "https://api.openai.com/v1/videos"
BLOCKED_ECONOMY_TERMS = ("Token", "Command Tokens", "Intel Keys", "loot box", "pay to win")


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def read_package() -> dict[str, Any]:
    return json.loads(SHOT_PACKAGE.read_text(encoding="utf-8"))


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/SFNS.ttf",
    ]
    for candidate in candidates:
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def wrap(draw: ImageDraw.ImageDraw, text: str, text_font: ImageFont.ImageFont, max_width: int) -> list[str]:
    words = text.split()
    lines: list[str] = []
    current: list[str] = []
    for word in words:
        candidate = " ".join(current + [word])
        if draw.textbbox((0, 0), candidate, font=text_font)[2] <= max_width:
            current.append(word)
            continue
        if current:
            lines.append(" ".join(current))
        current = [word]
    if current:
        lines.append(" ".join(current))
    return lines


def build_prompt(package: dict[str, Any], shot: dict[str, Any]) -> str:
    style = ", ".join(package["globalStyle"])
    negative = package["globalNegativePrompt"]
    return (
        f"{shot['prompt']}\n\n"
        f"Global style: {style}.\n"
        f"Camera direction: {shot['camera']}.\n"
        f"Hard constraints: {negative}"
    )


def prompt_scan(package: dict[str, Any]) -> dict[str, Any]:
    blocked = package.get("blockedTerms", BLOCKED_ECONOMY_TERMS)
    found: dict[str, list[str]] = {}
    for shot in package["shots"]:
        scan_text = "\n".join(
            [
                shot.get("prompt", ""),
                shot.get("camera", ""),
                shot.get("gameConcept", ""),
                " ".join(shot.get("qaTags", [])),
            ]
        )
        for term in blocked:
            if term.lower() in scan_text.lower():
                found.setdefault(shot["id"], []).append(term)
    return {
        "blockedTermCount": len(blocked),
        "blockedTermsFoundInPrompts": found,
    }


def make_job_plan(package: dict[str, Any], provider: str, model: str) -> dict[str, Any]:
    target = package["target"]
    scan = prompt_scan(package)
    jobs = []
    for shot in package["shots"]:
        jobs.append(
            {
                "shotId": shot["id"],
                "name": shot["name"],
                "provider": provider,
                "model": model,
                "status": "planned",
                "seconds": shot.get("durationSeconds", target["secondsPerClip"]),
                "size": target["size"],
                "prompt": build_prompt(package, shot),
                "sourceMode": "text-to-video",
                "usesWarlineCaptureScreenshotSource": False,
                "outputPath": str((OUTPUT_DIR / f"{shot['id']}_{slug(shot['name'])}.mp4").relative_to(ROOT)),
            }
        )
    return {
        "project": package["project"],
        "createdAt": utc_now(),
        "provider": provider,
        "model": model,
        "shotPackage": str(SHOT_PACKAGE.relative_to(ROOT)),
        "target": target,
        "promptScan": scan,
        "jobs": jobs,
    }


def slug(value: str) -> str:
    return "".join(ch.lower() if ch.isalnum() else "_" for ch in value).strip("_")


def write_storyboard(plan: dict[str, Any]) -> None:
    tile_w = 500
    tile_h = 300
    cols = len(plan["jobs"])
    image = Image.new("RGB", (tile_w * cols, tile_h), (8, 13, 15))
    draw = ImageDraw.Draw(image)
    title_font = font(26, True)
    body_font = font(18)
    colors = [(68, 202, 216), (222, 169, 65), (90, 148, 229), (191, 75, 58), (120, 190, 125)]
    for idx, job in enumerate(plan["jobs"]):
        x = idx * tile_w
        accent = colors[idx % len(colors)]
        draw.rectangle((x, 0, x + tile_w, tile_h), fill=(10, 18, 21), outline=accent, width=3)
        draw.text((x + 24, 24), job["shotId"], font=title_font, fill=accent)
        draw.text((x + 24, 62), job["name"], font=title_font, fill=(238, 242, 235))
        y = 112
        concept = job["prompt"].split("\n\n", 1)[0]
        for line in wrap(draw, concept, body_font, tile_w - 48)[:7]:
            draw.text((x + 24, y), line, font=body_font, fill=(195, 206, 198))
            y += 24
    image.save(STORYBOARD_PATH)


def request_json(url: str, method: str, token: str, payload: dict[str, Any] | None = None) -> dict[str, Any]:
    data = None
    headers = {"Authorization": f"Bearer {token}"}
    if payload is not None:
        data = json.dumps(payload).encode("utf-8")
        headers["Content-Type"] = "application/json"
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=120) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"{method} {url} failed with HTTP {exc.code}: {body}") from exc


def download_binary(url: str, token: str, output_path: Path) -> None:
    request = urllib.request.Request(url, headers={"Authorization": f"Bearer {token}"}, method="GET")
    with urllib.request.urlopen(request, timeout=300) as response:
        output_path.write_bytes(response.read())


def submit_openai(plan: dict[str, Any], model: str) -> None:
    token = os.environ.get("OPENAI_API_KEY")
    if not token:
        raise RuntimeError("OPENAI_API_KEY is required for --provider openai-sora.")
    for job in plan["jobs"]:
        if job.get("providerVideoId"):
            continue
        payload = {
            "model": model,
            "prompt": job["prompt"],
            "seconds": str(job["seconds"]),
            "size": job["size"],
        }
        response = request_json(OPENAI_VIDEO_ENDPOINT, "POST", token, payload)
        job["providerVideoId"] = response.get("id")
        job["status"] = response.get("status", "submitted")
        job["providerResponse"] = response
        job["submittedAt"] = utc_now()
        save_plan(plan)


def poll_openai(plan: dict[str, Any], poll_interval: int, timeout_minutes: int) -> None:
    token = os.environ.get("OPENAI_API_KEY")
    if not token:
        raise RuntimeError("OPENAI_API_KEY is required for --poll with openai-sora.")
    deadline = time.time() + timeout_minutes * 60
    while time.time() < deadline:
        remaining = [job for job in plan["jobs"] if job.get("status") not in ("completed", "failed", "cancelled")]
        if not remaining:
            return
        for job in remaining:
            video_id = job.get("providerVideoId")
            if not video_id:
                continue
            response = request_json(f"{OPENAI_VIDEO_ENDPOINT}/{video_id}", "GET", token)
            job["status"] = response.get("status", job["status"])
            job["progress"] = response.get("progress")
            job["providerResponse"] = response
            job["polledAt"] = utc_now()
            save_plan(plan)
        time.sleep(poll_interval)
    raise TimeoutError(f"Timed out waiting for video jobs after {timeout_minutes} minutes.")


def download_openai(plan: dict[str, Any]) -> None:
    token = os.environ.get("OPENAI_API_KEY")
    if not token:
        raise RuntimeError("OPENAI_API_KEY is required for --download with openai-sora.")
    for job in plan["jobs"]:
        if job.get("status") != "completed":
            continue
        output_path = ROOT / job["outputPath"]
        output_path.parent.mkdir(parents=True, exist_ok=True)
        if output_path.exists() and output_path.stat().st_size > 0:
            continue
        download_binary(f"{OPENAI_VIDEO_ENDPOINT}/{job['providerVideoId']}/content", token, output_path)
        job["downloadedAt"] = utc_now()
        save_plan(plan)


def verify_video(path: Path) -> dict[str, Any]:
    result: dict[str, Any] = {
        "path": str(path.relative_to(ROOT)),
        "exists": path.exists(),
        "fileSizeBytes": path.stat().st_size if path.exists() else 0,
        "opens": False,
        "width": 0,
        "height": 0,
        "fps": 0.0,
        "frameCount": 0,
        "durationSeconds": 0.0,
        "blankSampleCount": 0,
    }
    if not path.exists():
        return result
    capture = cv2.VideoCapture(str(path))
    result["opens"] = capture.isOpened()
    if not capture.isOpened():
        return result
    width = int(capture.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT))
    fps = float(capture.get(cv2.CAP_PROP_FPS) or 0)
    frame_count = int(capture.get(cv2.CAP_PROP_FRAME_COUNT))
    result.update(
        {
            "width": width,
            "height": height,
            "fps": round(fps, 2),
            "frameCount": frame_count,
            "durationSeconds": round(frame_count / fps, 2) if fps > 0 else 0.0,
        }
    )
    sample_indices = sorted(set([0, frame_count // 2, max(0, frame_count - 1)]))
    blank_count = 0
    for index in sample_indices:
        capture.set(cv2.CAP_PROP_POS_FRAMES, index)
        ok, frame = capture.read()
        if not ok:
            continue
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        if float(np.mean(gray)) < 8 or float(np.std(gray)) < 4:
            blank_count += 1
    capture.release()
    result["blankSampleCount"] = blank_count
    return result


def write_qa(plan: dict[str, Any]) -> dict[str, Any]:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    video_checks = [verify_video(ROOT / job["outputPath"]) for job in plan["jobs"]]
    prompt_scan_result = plan["promptScan"]
    completed = [job for job in plan["jobs"] if job.get("status") == "completed"]
    downloaded = [check for check in video_checks if check["exists"] and check["fileSizeBytes"] > 0]
    failures = []
    if prompt_scan_result["blockedTermsFoundInPrompts"]:
        failures.append("Blocked economy terms found in prompt text.")
    for job in plan["jobs"]:
        if job["usesWarlineCaptureScreenshotSource"]:
            failures.append(f"{job['shotId']} uses a WarlineCapture screenshot source.")
    for check in video_checks:
        if check["exists"] and (not check["opens"] or check["blankSampleCount"] > 0):
            failures.append(f"{check['path']} failed video verification.")

    report = {
        "writtenAt": utc_now(),
        "plannedJobs": len(plan["jobs"]),
        "completedJobs": len(completed),
        "downloadedVideos": len(downloaded),
        "storyboard": str(STORYBOARD_PATH.relative_to(ROOT)),
        "promptScan": prompt_scan_result,
        "videoChecks": video_checks,
        "failures": failures,
    }

    lines = [
        "# WarlineCapture Generative Cinematic QA Report",
        "",
        f"- Written: {report['writtenAt']}",
        f"- Job plan: `{JOB_PLAN_PATH.relative_to(ROOT)}`",
        f"- Storyboard: `{STORYBOARD_PATH.relative_to(ROOT)}`",
        f"- Planned jobs: {report['plannedJobs']}",
        f"- Completed jobs: {report['completedJobs']}",
        f"- Downloaded videos: {report['downloadedVideos']}",
        f"- Blocked economy terms configured: {prompt_scan_result['blockedTermCount']}",
        "",
        "## Checks",
        "",
        f"- [{'x' if not prompt_scan_result['blockedTermsFoundInPrompts'] else ' '}] No blocked economy terms in prompt text.",
        "- [x] Job plan uses text-to-video concept prompts, not WarlineCapture UI screenshots.",
        f"- [{'x' if STORYBOARD_PATH.exists() else ' '}] Storyboard contact sheet exists.",
        "",
        "## Video Outputs",
        "",
    ]
    for check in video_checks:
        state = "missing"
        if check["exists"] and check["opens"] and check["blankSampleCount"] == 0:
            state = f"{check['width']}x{check['height']} {check['durationSeconds']}s"
        elif check["exists"]:
            state = "exists but failed verification"
        lines.append(f"- `{check['path']}` - {state}")
    lines.extend(["", "## Failures", ""])
    if failures:
        lines.extend([f"- {failure}" for failure in failures])
    else:
        lines.append("- None.")
    lines.append("")
    QA_REPORT_PATH.write_text("\n".join(lines), encoding="utf-8")
    return report


def save_plan(plan: dict[str, Any]) -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    JOB_PLAN_PATH.write_text(json.dumps(plan, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--provider", choices=["dry-run", "openai-sora"], default="dry-run")
    parser.add_argument("--model", default="sora-2")
    parser.add_argument("--submit", action="store_true")
    parser.add_argument("--poll", action="store_true")
    parser.add_argument("--download", action="store_true")
    parser.add_argument("--poll-interval-seconds", type=int, default=15)
    parser.add_argument("--timeout-minutes", type=int, default=30)
    args = parser.parse_args()

    package = read_package()
    plan = make_job_plan(package, args.provider, args.model)
    save_plan(plan)
    write_storyboard(plan)

    if args.provider == "openai-sora":
        if args.submit:
            submit_openai(plan, args.model)
        if args.poll:
            poll_openai(plan, args.poll_interval_seconds, args.timeout_minutes)
        if args.download:
            download_openai(plan)
    elif args.submit or args.poll or args.download:
        raise RuntimeError("--submit, --poll, and --download require a real provider.")

    report = write_qa(plan)
    print(json.dumps({"jobPlan": str(JOB_PLAN_PATH), "qaReport": str(QA_REPORT_PATH), "storyboard": str(STORYBOARD_PATH), "failures": report["failures"]}, indent=2))
    return 1 if report["failures"] else 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(2)
