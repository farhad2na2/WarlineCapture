#!/usr/bin/env python3
"""Slack polling relay that launches local Codex tasks."""

from __future__ import annotations

import argparse
import json
import os
import signal
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parent
DEFAULT_ENV = ROOT / ".env.local"


def load_env(path: Path) -> None:
    if not path.exists():
        return

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue

        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        if key and key not in os.environ:
            os.environ[key] = value


def require_env(name: str) -> str:
    value = os.environ.get(name, "").strip()
    if not value:
        raise SystemExit(f"Missing required environment variable: {name}")
    return value


@dataclass
class Config:
    slack_bot_token: str
    slack_channel_id: str
    codex_worktree: Path
    codex_bin: Path
    codex_trigger: str
    poll_seconds: float
    state_dir: Path
    task_dir: Path | None
    codex_model: str

    @classmethod
    def from_env(cls) -> "Config":
        task_dir = os.environ.get("CODEX_TASK_DIR", "").strip()
        return cls(
            slack_bot_token=require_env("SLACK_BOT_TOKEN"),
            slack_channel_id=require_env("SLACK_CHANNEL_ID"),
            codex_worktree=Path(os.environ.get("CODEX_WORKTREE", "/Users/farhad/Projects/WarlineCapture-SlackAgent")),
            codex_bin=Path(os.environ.get("CODEX_BIN", "/Applications/Codex.app/Contents/Resources/codex")),
            codex_trigger=os.environ.get("CODEX_TRIGGER", "codex local").strip().lower(),
            poll_seconds=float(os.environ.get("POLL_SECONDS", "8")),
            state_dir=Path(os.environ.get("STATE_DIR", "/private/tmp/codex-local-relay")),
            task_dir=Path(task_dir).expanduser() if task_dir else None,
            codex_model=os.environ.get("CODEX_MODEL", "").strip(),
        )


class SlackClient:
    def __init__(self, token: str) -> None:
        self.token = token

    def api(self, method: str, payload: dict[str, Any]) -> dict[str, Any]:
        body = urllib.parse.urlencode(payload).encode("utf-8")
        request = urllib.request.Request(
            f"https://slack.com/api/{method}",
            data=body,
            headers={
                "Authorization": f"Bearer {self.token}",
                "Content-Type": "application/x-www-form-urlencoded",
            },
            method="POST",
        )

        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                data = json.loads(response.read().decode("utf-8"))
        except urllib.error.URLError as exc:
            raise RuntimeError(f"Slack API request failed: {exc}") from exc

        if not data.get("ok"):
            raise RuntimeError(f"Slack API {method} failed: {data.get('error', data)}")

        return data

    def post(self, channel: str, text: str, thread_ts: str | None = None) -> None:
        payload: dict[str, Any] = {"channel": channel, "text": text}
        if thread_ts:
            payload["thread_ts"] = thread_ts
        self.api("chat.postMessage", payload)

    def history(self, channel: str, oldest: str | None) -> list[dict[str, Any]]:
        payload: dict[str, Any] = {"channel": channel, "limit": "20"}
        if oldest:
            payload["oldest"] = oldest
            payload["inclusive"] = "false"

        messages = self.api("conversations.history", payload).get("messages", [])
        return sorted(messages, key=lambda item: float(item["ts"]))


@dataclass
class ActiveTask:
    process: subprocess.Popen[Any]
    thread_ts: str
    output_file: Path
    log_file: Path
    started_at: float


class Relay:
    def __init__(self, config: Config, slack: SlackClient) -> None:
        self.config = config
        self.slack = slack
        self.active: ActiveTask | None = None
        self.config.state_dir.mkdir(parents=True, exist_ok=True)
        self.last_ts_file = self.config.state_dir / "last_ts"

    def run(self) -> None:
        self.validate_local_setup()
        last_ts = self.load_last_ts()
        if not last_ts:
            last_ts = self.seed_last_ts()

        self.slack.post(
            self.config.slack_channel_id,
            f"Codex local relay online. Worktree: `{self.config.codex_worktree}`. Trigger: `{self.config.codex_trigger}`.",
        )

        while True:
            try:
                self.check_active_task()
                self.poll_task_queue()
                last_ts = self.poll_once(last_ts)
                self.save_last_ts(last_ts)
            except KeyboardInterrupt:
                raise
            except Exception as exc:
                print(f"relay error: {exc}", file=sys.stderr)
                time.sleep(min(self.config.poll_seconds * 2, 30))

            time.sleep(self.config.poll_seconds)

    def validate_local_setup(self) -> None:
        if not self.config.codex_bin.exists():
            raise SystemExit(f"Codex binary not found: {self.config.codex_bin}")
        if not self.config.codex_worktree.exists():
            raise SystemExit(f"Codex worktree not found: {self.config.codex_worktree}")
        if not (self.config.codex_worktree / ".git").exists():
            raise SystemExit(f"Codex worktree is not a Git checkout: {self.config.codex_worktree}")
        if self.config.task_dir:
            self.config.task_dir.mkdir(parents=True, exist_ok=True)
            (self.config.task_dir / "processed").mkdir(parents=True, exist_ok=True)
            (self.config.task_dir / "failed").mkdir(parents=True, exist_ok=True)

    def load_last_ts(self) -> str | None:
        if self.last_ts_file.exists():
            return self.last_ts_file.read_text(encoding="utf-8").strip() or None
        return None

    def save_last_ts(self, ts: str) -> None:
        self.last_ts_file.write_text(ts, encoding="utf-8")

    def seed_last_ts(self) -> str:
        messages = self.slack.history(self.config.slack_channel_id, None)
        if messages:
            return messages[-1]["ts"]
        return f"{time.time():.6f}"

    def poll_once(self, last_ts: str) -> str:
        messages = self.slack.history(self.config.slack_channel_id, last_ts)
        for message in messages:
            last_ts = message["ts"]
            self.handle_message(message)
        return last_ts

    def poll_task_queue(self) -> None:
        task_dir = self.config.task_dir
        if not task_dir or (self.active and self.active.process.poll() is None):
            return

        queued_files = sorted(task_dir.glob("*.json"), key=lambda item: item.stat().st_mtime)
        if not queued_files:
            return

        queued_file = queued_files[0]
        try:
            payload = json.loads(queued_file.read_text(encoding="utf-8-sig"))
            task = str(payload.get("task", "")).strip()
            if not task:
                raise ValueError("queued task JSON is missing non-empty 'task'")
            bundle_dir_name = str(payload.get("bundle_dir_name", "")).strip()
            if bundle_dir_name:
                task += (
                    f"\n\nQueued task file: {queued_file}\n"
                    f"Failure bundle directory: {task_dir / bundle_dir_name}\n"
                    "Read the copied Jenkins logs from that bundle before editing code."
                )

            thread_ts = str(payload.get("thread_ts", "")).strip() or None
            self.start_task(task, thread_ts)
            destination = task_dir / "processed" / queued_file.name
        except Exception as exc:
            self.slack.post(
                self.config.slack_channel_id,
                f"Could not start queued Codex task from `{queued_file}`: {exc}",
            )
            destination = task_dir / "failed" / queued_file.name

        shutil.move(str(queued_file), str(destination))

    def handle_message(self, message: dict[str, Any]) -> None:
        if message.get("bot_id") or message.get("subtype"):
            return

        text = str(message.get("text", "")).strip()
        if not text.lower().startswith(self.config.codex_trigger):
            return

        command = text[len(self.config.codex_trigger) :].strip(" ,:\n\t")
        thread_ts = message.get("thread_ts") or message["ts"]
        lower = command.lower()

        if not command or lower == "help":
            self.reply_help(thread_ts)
        elif lower == "status":
            self.reply_status(thread_ts)
        elif lower in {"cancel", "stop"}:
            self.cancel(thread_ts)
        else:
            task = self.extract_task(command)
            self.start_task(task, thread_ts)

    def extract_task(self, command: str) -> str:
        lowered = command.lower()
        for prefix in ("start task:", "run task:", "task:", "start:", "run:"):
            if lowered.startswith(prefix):
                return command[len(prefix) :].strip()
        return command.strip()

    def reply_help(self, thread_ts: str) -> None:
        self.slack.post(
            self.config.slack_channel_id,
            "Commands:\n"
            "`codex local status`\n"
            "`codex local start task: <instructions>`\n"
            "`codex local cancel`",
            thread_ts,
        )

    def reply_status(self, thread_ts: str) -> None:
        branch = self.git(["branch", "--show-current"])
        commit = self.git(["rev-parse", "--short", "HEAD"])
        dirty = self.git(["status", "--short"])
        dirty_count = len([line for line in dirty.splitlines() if line.strip()])
        active = "yes" if self.active and self.active.process.poll() is None else "no"
        task_dir = str(self.config.task_dir) if self.config.task_dir else "disabled"
        self.slack.post(
            self.config.slack_channel_id,
            f"Local Codex status:\n"
            f"- worktree: `{self.config.codex_worktree}`\n"
            f"- branch: `{branch}`\n"
            f"- commit: `{commit}`\n"
            f"- dirty files: `{dirty_count}`\n"
            f"- active task: `{active}`\n"
            f"- queued task dir: `{task_dir}`",
            thread_ts,
        )

    def git(self, args: list[str]) -> str:
        result = subprocess.run(
            ["git", *args],
            cwd=self.config.codex_worktree,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
        )
        return result.stdout.strip() or result.stderr.strip()

    def start_task(self, task: str, thread_ts: str | None = None) -> None:
        if not task:
            if thread_ts:
                self.reply_help(thread_ts)
            return

        if self.active and self.active.process.poll() is None:
            self.slack.post(self.config.slack_channel_id, "A local Codex task is already running.", thread_ts)
            return

        job_id = str(int(time.time()))
        output_file = self.config.state_dir / f"codex-last-message-{job_id}.txt"
        log_file = self.config.state_dir / f"codex-{job_id}.log"
        prompt = self.build_prompt(task)

        command = [
            str(self.config.codex_bin),
            "exec",
            "-C",
            str(self.config.codex_worktree),
            "--sandbox",
            "workspace-write",
            "--add-dir",
            "/private/tmp",
            "-o",
            str(output_file),
        ]
        if self.config.task_dir:
            command.extend(["--add-dir", str(self.config.task_dir)])
        if self.config.codex_model:
            command.extend(["-m", self.config.codex_model])
        command.append(prompt)

        log_handle = log_file.open("w", encoding="utf-8")
        process = subprocess.Popen(
            command,
            cwd=self.config.codex_worktree,
            stdout=log_handle,
            stderr=subprocess.STDOUT,
            text=True,
            env=self.codex_environment(),
            start_new_session=True,
        )
        log_handle.close()

        self.active = ActiveTask(
            process=process,
            thread_ts=thread_ts or "",
            output_file=output_file,
            log_file=log_file,
            started_at=time.time(),
        )
        self.slack.post(
            self.config.slack_channel_id,
            f"Started local Codex task in `{self.config.codex_worktree}`. Log: `{log_file}`",
            thread_ts,
        )

    def build_prompt(self, task: str) -> str:
        return (
            "You are a local Codex agent triggered from Slack for WarlineCapture.\n"
            f"Work only inside this checkout: {self.config.codex_worktree}\n"
            "Do not edit /Users/farhad/Projects/WarlineCapture unless explicitly asked.\n"
            "Keep changes scoped. Run relevant tests or Unity validation when practical. Never invoke the Unity binary directly: use Tools/CI/invoke_unity_macos.sh only. On macOS it uses GUI licensing and never passes -batchmode; do not add -batchmode, do not reset Unity IPC, and do not treat licensing as blocked unless Hub is closed.\n"
            "Use focused searches first. Prefer rg. Do not inventory the full Unity project.\n"
            "Never scan Assets/Synty, Assets/Game/Art, Assets/Game/Prefabs/Generated, or image/model/audio assets unless explicitly asked.\n"
            "For tactical HUD work, start only with these targets: "
            "Assets/Game/Scripts/UI, Assets/Game/Scripts/TacticalMaps, Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs, "
            "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab, Assets/Game/Prefabs/UI/Popups, and Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs.\n"
            "If the task needs more context, explain the need in the final summary instead of doing a broad scan.\n"
            "When finished, summarize files changed, tests run, and any remaining risk.\n\n"
            f"Task:\n{task}"
        )

    def codex_environment(self) -> dict[str, str]:
        env = os.environ.copy()
        extra_path = "/Applications/Codex.app/Contents/Resources:/opt/homebrew/bin:/usr/local/bin"
        env["PATH"] = f"{extra_path}:{env.get('PATH', '')}"
        return env

    def check_active_task(self) -> None:
        if not self.active:
            return

        return_code = self.active.process.poll()
        if return_code is None:
            return

        elapsed = int(time.time() - self.active.started_at)
        final_text = ""
        if self.active.output_file.exists():
            final_text = self.active.output_file.read_text(encoding="utf-8").strip()

        if not final_text:
            final_text = f"Codex exited with code {return_code}. See log: `{self.active.log_file}`"

        prefix = f"Local Codex finished in {elapsed}s with exit code `{return_code}`.\n\n"
        self.slack.post(self.config.slack_channel_id, truncate(prefix + final_text), self.active.thread_ts)
        self.active = None

    def cancel(self, thread_ts: str) -> None:
        if not self.active or self.active.process.poll() is not None:
            self.slack.post(self.config.slack_channel_id, "No active local Codex task.", thread_ts)
            return

        os.killpg(self.active.process.pid, signal.SIGTERM)
        self.slack.post(self.config.slack_channel_id, "Sent cancel signal to the active local Codex task.", thread_ts)


def truncate(text: str, limit: int = 3500) -> str:
    if len(text) <= limit:
        return text
    return text[: limit - 80].rstrip() + "\n\n[truncated; see local relay log for full output]"


def main() -> None:
    parser = argparse.ArgumentParser(description="Poll Slack and run local Codex tasks.")
    parser.add_argument("--env", type=Path, default=DEFAULT_ENV, help="Path to .env.local config")
    args = parser.parse_args()

    load_env(args.env)
    config = Config.from_env()
    slack = SlackClient(config.slack_bot_token)
    Relay(config, slack).run()


if __name__ == "__main__":
    main()
