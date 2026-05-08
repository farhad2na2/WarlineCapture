#!/usr/bin/env python3
"""Local browser voice page for launching Codex tasks.

This version avoids browser speech recognition and cloud text-to-speech.
Chrome records audio, this local server transcribes it with local whisper.cpp,
runs Codex locally, then returns locally generated speech for playback.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import signal
import subprocess
import time
import uuid
from dataclasses import dataclass
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parent
PROJECT_ROOT = ROOT.parent.parent
DEFAULT_ENV = ROOT / ".env.local"
PROJECT_ENV = PROJECT_ROOT / ".env.local"
DEFAULT_WHISPER_MODEL = PROJECT_ROOT / ".local" / "whisper-models" / "ggml-small.bin"


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


@dataclass
class MultipartPart:
    name: str
    data: bytes
    filename: str = ""
    content_type: str = "application/octet-stream"


def parse_multipart(content_type: str, body: bytes) -> dict[str, MultipartPart]:
    match = re.search(r"boundary=(?P<boundary>[^;]+)", content_type)
    if not match:
        raise ValueError("Missing multipart boundary.")

    boundary = match.group("boundary").strip('"')
    marker = f"--{boundary}".encode("utf-8")
    parts: dict[str, MultipartPart] = {}

    for raw_part in body.split(marker):
        raw_part = raw_part.strip()
        if not raw_part or raw_part == b"--":
            continue
        if raw_part.endswith(b"--"):
            raw_part = raw_part[:-2].strip()
        if b"\r\n\r\n" not in raw_part:
            continue
        raw_headers, data = raw_part.split(b"\r\n\r\n", 1)
        if data.endswith(b"\r\n"):
            data = data[:-2]

        headers: dict[str, str] = {}
        for raw_header in raw_headers.decode("utf-8", errors="replace").split("\r\n"):
            if ":" not in raw_header:
                continue
            key, value = raw_header.split(":", 1)
            headers[key.lower().strip()] = value.strip()

        disposition = headers.get("content-disposition", "")
        name_match = re.search(r'name="([^"]+)"', disposition)
        if not name_match:
            continue
        filename_match = re.search(r'filename="([^"]*)"', disposition)
        name = name_match.group(1)
        parts[name] = MultipartPart(
            name=name,
            data=data,
            filename=filename_match.group(1) if filename_match else "",
            content_type=headers.get("content-type", "application/octet-stream"),
        )

    return parts


def build_multipart(fields: dict[str, str], files: dict[str, tuple[str, str, bytes]]) -> tuple[str, bytes]:
    boundary = f"codexvoice-{uuid.uuid4().hex}"
    chunks: list[bytes] = []

    for name, value in fields.items():
        chunks.extend(
            [
                f"--{boundary}\r\n".encode("utf-8"),
                f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode("utf-8"),
                value.encode("utf-8"),
                b"\r\n",
            ]
        )

    for name, (filename, content_type, data) in files.items():
        chunks.extend(
            [
                f"--{boundary}\r\n".encode("utf-8"),
                f'Content-Disposition: form-data; name="{name}"; filename="{filename}"\r\n'.encode("utf-8"),
                f"Content-Type: {content_type}\r\n\r\n".encode("utf-8"),
                data,
                b"\r\n",
            ]
        )

    chunks.append(f"--{boundary}--\r\n".encode("utf-8"))
    return f"multipart/form-data; boundary={boundary}", b"".join(chunks)


def selected_language_to_transcription_code(language: str) -> str:
    if language == "fa-IR":
        return "fa"
    if language == "de-DE":
        return "de"
    if language == "en-US":
        return "en"
    return "auto"


def transcribe_audio(audio: bytes, filename: str, content_type: str, language: str) -> str:
    state_dir = Path(os.environ.get("STATE_DIR", "/private/tmp/codex-local-relay"))
    state_dir.mkdir(parents=True, exist_ok=True)
    job_id = uuid.uuid4().hex
    suffix = ".mp4" if "mp4" in content_type or filename.endswith(".mp4") else ".webm"
    input_file = state_dir / f"voice-input-{job_id}{suffix}"
    wav_file = state_dir / f"voice-input-{job_id}.wav"
    input_file.write_bytes(audio)

    ffmpeg = os.environ.get("FFMPEG_BIN", "/opt/homebrew/bin/ffmpeg")
    whisper = os.environ.get("WHISPER_BIN", "/opt/homebrew/bin/whisper-cli")
    model = Path(os.environ.get("WHISPER_MODEL", str(DEFAULT_WHISPER_MODEL)))
    if not model.exists():
        raise RuntimeError(f"Local Whisper model not found: {model}")

    convert = subprocess.run(
        [
            ffmpeg,
            "-y",
            "-i",
            str(input_file),
            "-ar",
            "16000",
            "-ac",
            "1",
            "-c:a",
            "pcm_s16le",
            str(wav_file),
        ],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        timeout=60,
    )
    if convert.returncode != 0:
        raise RuntimeError(f"ffmpeg could not read the microphone recording: {convert.stderr.strip()}")

    prompt = (
        "WarlineCapture Codex Unity Git Slack HUD prefab tests branch. "
        "The speaker may mix Persian and English technical words."
    )
    transcription = subprocess.run(
        [
            whisper,
            "-m",
            str(model),
            "-f",
            str(wav_file),
            "-l",
            selected_language_to_transcription_code(language),
            "-nt",
            "-np",
            "--prompt",
            prompt,
        ],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        timeout=180,
    )
    if transcription.returncode != 0:
        raise RuntimeError(f"Local Whisper transcription failed: {transcription.stderr.strip()}")

    text = transcription.stdout.strip()
    if not text:
        raise RuntimeError("Local Whisper returned empty text.")
    return text


def synthesize_speech(text: str, language: str) -> tuple[str, bytes]:
    state_dir = Path(os.environ.get("STATE_DIR", "/private/tmp/codex-local-relay"))
    state_dir.mkdir(parents=True, exist_ok=True)
    job_id = uuid.uuid4().hex
    spoken_text = text[:1800]

    if language == "fa-IR":
        wav_file = state_dir / f"voice-reply-{job_id}.wav"
        espeak = os.environ.get("ESPEAK_BIN", "/opt/homebrew/bin/espeak-ng")
        result = subprocess.run(
            [espeak, "-v", "fa", "-s", "145", "-w", str(wav_file), spoken_text],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=60,
        )
        if result.returncode != 0:
            raise RuntimeError(f"Local Persian speech failed: {result.stderr.strip()}")
        return "audio/wav", wav_file.read_bytes()

    aiff_file = state_dir / f"voice-reply-{job_id}.aiff"
    voice = "Markus" if language == "de-DE" else "Samantha"
    result = subprocess.run(
        ["say", "-v", voice, "-o", str(aiff_file), spoken_text],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        timeout=60,
    )
    if result.returncode != 0 and language == "de-DE":
        result = subprocess.run(
            ["say", "-o", str(aiff_file), spoken_text],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=60,
        )
    if result.returncode != 0:
        raise RuntimeError(f"Local speech failed: {result.stderr.strip()}")
    return "audio/aiff", aiff_file.read_bytes()


@dataclass
class VoiceConfig:
    codex_worktree: Path
    codex_bin: Path
    state_dir: Path
    codex_model: str
    host: str
    port: int

    @classmethod
    def from_env(cls, host: str, port: int) -> "VoiceConfig":
        return cls(
            codex_worktree=Path(os.environ.get("CODEX_WORKTREE", "/Users/farhad/Projects/WarlineCapture-SlackAgent")),
            codex_bin=Path(os.environ.get("CODEX_BIN", "/Applications/Codex.app/Contents/Resources/codex")),
            state_dir=Path(os.environ.get("STATE_DIR", "/private/tmp/codex-local-relay")),
            codex_model=os.environ.get("CODEX_MODEL", "").strip(),
            host=host,
            port=port,
        )


@dataclass
class VoiceTask:
    process: subprocess.Popen[Any]
    output_file: Path
    log_file: Path
    started_at: float
    user_text: str
    language: str
    final_text: str = ""
    return_code: int | None = None


class VoiceState:
    def __init__(self, config: VoiceConfig) -> None:
        self.config = config
        self.config.state_dir.mkdir(parents=True, exist_ok=True)
        self.active: VoiceTask | None = None
        self.completed: VoiceTask | None = None

    def validate(self) -> None:
        if not self.config.codex_bin.exists():
            raise SystemExit(f"Codex binary not found: {self.config.codex_bin}")
        if not self.config.codex_worktree.exists():
            raise SystemExit(f"Codex worktree not found: {self.config.codex_worktree}")
        if not (self.config.codex_worktree / ".git").exists():
            raise SystemExit(f"Codex worktree is not a Git checkout: {self.config.codex_worktree}")

    def start_task(self, user_text: str, language: str = "") -> dict[str, Any]:
        self.refresh()
        if self.active and self.active.process.poll() is None:
            return {
                "ok": False,
                "error": "A Codex task is already running. Wait for it to finish, or press Cancel.",
            }

        job_id = str(int(time.time()))
        output_file = self.config.state_dir / f"voice-codex-last-message-{job_id}.txt"
        log_file = self.config.state_dir / f"voice-codex-{job_id}.log"
        prompt = self.build_prompt(user_text, language)
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

        self.active = VoiceTask(
            process=process,
            output_file=output_file,
            log_file=log_file,
            started_at=time.time(),
            user_text=user_text,
            language=language,
        )
        return {"ok": True, "task": self.task_payload(self.active)}

    def refresh(self) -> None:
        if not self.active:
            return

        return_code = self.active.process.poll()
        if return_code is None:
            return

        self.active.return_code = return_code
        if self.active.output_file.exists():
            self.active.final_text = self.active.output_file.read_text(encoding="utf-8").strip()
        if not self.active.final_text:
            self.active.final_text = f"Codex exited with code {return_code}. See log: {self.active.log_file}"

        self.completed = self.active
        self.active = None

    def status(self) -> dict[str, Any]:
        self.refresh()
        return {
            "ok": True,
            "worktree": str(self.config.codex_worktree),
            "localVoiceConfigured": DEFAULT_WHISPER_MODEL.exists(),
            "active": self.task_payload(self.active) if self.active else None,
            "completed": self.task_payload(self.completed) if self.completed else None,
        }

    def cancel(self) -> dict[str, Any]:
        self.refresh()
        if not self.active or self.active.process.poll() is not None:
            return {"ok": False, "error": "No active Codex task to cancel."}
        os.killpg(self.active.process.pid, signal.SIGTERM)
        return {"ok": True, "message": "Cancel signal sent."}

    def task_payload(self, task: VoiceTask | None) -> dict[str, Any] | None:
        if task is None:
            return None
        elapsed = int(time.time() - task.started_at)
        return {
            "userText": task.user_text,
            "language": task.language,
            "startedAt": task.started_at,
            "elapsed": elapsed,
            "running": task.return_code is None and task.process.poll() is None,
            "returnCode": task.return_code,
            "finalText": task.final_text,
            "logFile": str(task.log_file),
        }

    def build_prompt(self, user_text: str, language: str = "") -> str:
        language_note = f"Selected voice language: {language or 'auto'}.\n"
        reply_language_note = (
            "Reply in Persian/Farsi. Keep file paths, code symbols, class names, commands, and error text in their original English.\n"
            if language == "fa-IR"
            else "Reply in the same language the user used when it is clear; otherwise reply in English.\n"
        )
        return (
            "You are a local Codex agent controlled by a natural voice page for WarlineCapture.\n"
            f"Work only inside this checkout: {self.config.codex_worktree}\n"
            f"{language_note}"
            f"{reply_language_note}"
            "The user may speak casually with dictation mistakes. Infer the intent when it is clear; ask for clarification in the final response when it is not.\n"
            "Do not edit /Users/farhad/Projects/WarlineCapture unless explicitly asked.\n"
            "Keep changes scoped. Run relevant tests or Unity batchmode validation when practical.\n"
            "Use focused searches first. Prefer rg. Do not inventory the full Unity project.\n"
            "Never scan Assets/Synty, Assets/Game/Art, Assets/Game/Prefabs/Generated, or image/model/audio assets unless explicitly asked.\n"
            "For tactical HUD work, start only with these targets: "
            "Assets/Game/Scripts/UI, Assets/Game/Scripts/TacticalMaps, Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs, "
            "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab, Assets/Game/Prefabs/UI/Popups, and Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs.\n"
            "When finished, summarize what you did, changed files, tests run, and remaining risk. Keep the final answer easy to read aloud.\n\n"
            f"User voice request:\n{user_text}"
        )

    def codex_environment(self) -> dict[str, str]:
        env = os.environ.copy()
        extra_path = "/Applications/Codex.app/Contents/Resources:/opt/homebrew/bin:/usr/local/bin"
        env["PATH"] = f"{extra_path}:{env.get('PATH', '')}"
        return env


HTML = r"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>WarlineCapture Voice Codex</title>
  <style>
    :root {
      color-scheme: dark;
      --bg: #101314;
      --panel: #171c1d;
      --line: #2b3436;
      --text: #f2f2ee;
      --muted: #aeb8b9;
      --accent: #58d0a9;
      --accent-2: #f0b45b;
      --danger: #ff6b6b;
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      letter-spacing: 0;
    }
    main {
      min-height: 100vh;
      display: grid;
      grid-template-rows: auto 1fr;
      gap: 16px;
      padding: 22px;
      max-width: 980px;
      margin: 0 auto;
    }
    header {
      display: flex;
      justify-content: space-between;
      gap: 16px;
      align-items: flex-start;
      border-bottom: 1px solid var(--line);
      padding-bottom: 16px;
    }
    h1 {
      margin: 0;
      font-size: 24px;
      line-height: 1.15;
      font-weight: 720;
    }
    .subtitle {
      color: var(--muted);
      margin-top: 6px;
      font-size: 14px;
    }
    .controls {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 10px;
    }
    button, select {
      min-height: 42px;
      border-radius: 8px;
      border: 1px solid var(--line);
      color: var(--text);
      background: #202728;
      font: inherit;
      font-size: 14px;
    }
    button {
      padding: 0 16px;
      cursor: pointer;
      font-weight: 650;
    }
    select { padding: 0 10px; }
    button.primary {
      background: var(--accent);
      color: #062119;
      border-color: transparent;
    }
    button.danger {
      background: #3a2021;
      border-color: #693033;
      color: #ffd8d8;
    }
    button:disabled {
      cursor: not-allowed;
      opacity: 0.55;
    }
    .status {
      min-height: 44px;
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 12px 14px;
      color: var(--muted);
      background: var(--panel);
    }
    .status strong { color: var(--accent); }
    .grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 14px;
    }
    section {
      border-top: 1px solid var(--line);
      padding-top: 14px;
      min-width: 0;
    }
    h2 {
      margin: 0 0 8px;
      color: var(--muted);
      font-size: 13px;
      text-transform: uppercase;
      letter-spacing: 0;
    }
    pre {
      margin: 0;
      white-space: pre-wrap;
      overflow-wrap: anywhere;
      line-height: 1.45;
      font-size: 15px;
      min-height: 130px;
      color: var(--text);
    }
    .meter {
      width: 100%;
      height: 8px;
      background: #222a2b;
      border-radius: 999px;
      overflow: hidden;
      margin-top: 12px;
    }
    .bar {
      width: 0%;
      height: 100%;
      background: var(--accent-2);
      transition: width 80ms linear;
    }
    .small {
      color: var(--muted);
      font-size: 13px;
      margin-top: 8px;
    }
    @media (max-width: 760px) {
      main { padding: 16px; }
      header { display: grid; }
      .grid { grid-template-columns: 1fr; }
      .controls { align-items: stretch; }
      button, select { width: 100%; }
    }
  </style>
</head>
<body>
  <main>
    <header>
      <div>
        <h1>WarlineCapture Voice Codex</h1>
        <div class="subtitle">Local Whisper transcription + local Codex + local voice reply</div>
      </div>
      <div class="controls">
        <select id="language">
          <option value="">Auto language</option>
          <option value="fa-IR">Farsi / Persian</option>
          <option value="en-US">English</option>
          <option value="de-DE">German</option>
        </select>
        <button id="listen" class="primary">Start Listening</button>
        <button id="stop" disabled>Stop</button>
        <button id="cancel" class="danger">Cancel Codex</button>
      </div>
    </header>

    <div>
      <div id="status" class="status">Checking server...</div>
      <div class="meter"><div id="meter" class="bar"></div></div>
      <div class="small">Everything runs locally on this Mac. While Codex or the reply audio is running, the mic pauses so it does not hear itself.</div>
    </div>

    <div class="grid">
      <section>
        <h2>You said</h2>
        <pre id="transcript">Waiting.</pre>
      </section>
      <section>
        <h2>Codex replied</h2>
        <pre id="reply">No reply yet.</pre>
      </section>
    </div>
  </main>

  <script>
    const listenButton = document.getElementById('listen');
    const stopButton = document.getElementById('stop');
    const cancelButton = document.getElementById('cancel');
    const languageSelect = document.getElementById('language');
    const statusEl = document.getElementById('status');
    const transcriptEl = document.getElementById('transcript');
    const replyEl = document.getElementById('reply');
    const meterEl = document.getElementById('meter');

    let keepListening = false;
    let stream = null;
    let recorder = null;
    let chunks = [];
    let audioContext = null;
    let analyser = null;
    let analyserData = null;
    let animationId = null;
    let hasSpeech = false;
    let utteranceStartedAt = 0;
    let lastSpeechAt = 0;
    let statusTimer = null;
    let lastCompletedStartedAt = 0;
    let currentAudio = null;

    function setStatus(message, strong = '') {
      statusEl.innerHTML = strong ? `<strong>${strong}</strong> ${message}` : message;
    }

    function setControls() {
      listenButton.disabled = keepListening;
      stopButton.disabled = !keepListening;
    }

    function chooseMimeType() {
      const candidates = [
        'audio/webm;codecs=opus',
        'audio/webm',
        'audio/mp4',
        ''
      ];
      return candidates.find(type => !type || MediaRecorder.isTypeSupported(type)) || '';
    }

    async function ensureStream() {
      if (stream) return;
      stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        }
      });
      audioContext = new AudioContext();
      const source = audioContext.createMediaStreamSource(stream);
      analyser = audioContext.createAnalyser();
      analyser.fftSize = 1024;
      source.connect(analyser);
      analyserData = new Uint8Array(analyser.fftSize);
    }

    async function startListening() {
      if (!navigator.mediaDevices || !window.MediaRecorder) {
        setStatus('This browser cannot record audio. Use Chrome on the Mac.');
        return;
      }
      keepListening = true;
      setControls();
      await ensureStream();
      startUtterance();
    }

    function stopListening() {
      keepListening = false;
      setControls();
      stopRecorder();
      setStatus('Stopped.');
    }

    function stopRecorder() {
      if (animationId) {
        cancelAnimationFrame(animationId);
        animationId = null;
      }
      if (recorder && recorder.state === 'recording') {
        recorder.stop();
      }
      meterEl.style.width = '0%';
    }

    function startUtterance() {
      if (!keepListening || recorder?.state === 'recording') return;

      chunks = [];
      hasSpeech = false;
      utteranceStartedAt = Date.now();
      lastSpeechAt = 0;
      const mimeType = chooseMimeType();
      recorder = new MediaRecorder(stream, mimeType ? { mimeType } : undefined);

      recorder.ondataavailable = event => {
        if (event.data && event.data.size > 0) chunks.push(event.data);
      };
      recorder.onstop = () => {
        if (animationId) {
          cancelAnimationFrame(animationId);
          animationId = null;
        }
        meterEl.style.width = '0%';
        const blob = new Blob(chunks, { type: recorder.mimeType || 'audio/webm' });
        if (!hasSpeech || blob.size < 1200) {
          if (keepListening) setTimeout(startUtterance, 350);
          return;
        }
        sendVoice(blob);
      };

      recorder.start();
      setStatus('Listening. Speak naturally, then pause.');
      monitorVoice();
    }

    function monitorVoice() {
      if (!analyser || !recorder || recorder.state !== 'recording') return;
      analyser.getByteTimeDomainData(analyserData);
      let sum = 0;
      for (const value of analyserData) {
        const normalized = (value - 128) / 128;
        sum += normalized * normalized;
      }
      const rms = Math.sqrt(sum / analyserData.length);
      meterEl.style.width = `${Math.min(100, Math.round(rms * 420))}%`;

      const now = Date.now();
      if (rms > 0.035) {
        hasSpeech = true;
        lastSpeechAt = now;
      }
      if (hasSpeech && now - lastSpeechAt > 1250) {
        stopRecorder();
        return;
      }
      if (!hasSpeech && now - utteranceStartedAt > 12000) {
        stopRecorder();
        return;
      }
      if (now - utteranceStartedAt > 45000) {
        stopRecorder();
        return;
      }
      animationId = requestAnimationFrame(monitorVoice);
    }

    async function sendVoice(blob) {
      const shouldResume = keepListening;
      setStatus('Transcribing voice locally with Whisper...', 'Working');
      const form = new FormData();
      form.append('language', languageSelect.value);
      form.append('audio', blob, blob.type.includes('mp4') ? 'voice.mp4' : 'voice.webm');

      try {
        const response = await fetch('/api/voice-task', { method: 'POST', body: form });
        const data = await response.json();
        if (!data.ok) throw new Error(data.error || 'Voice request failed.');
        transcriptEl.textContent = data.transcript || '';
        replyEl.textContent = 'Codex is working...';
        setStatus('Codex is working on the transcribed request.', 'Running');
        pollUntilDone(shouldResume);
      } catch (error) {
        setStatus(error.message || String(error));
        if (shouldResume) setTimeout(startUtterance, 800);
      }
    }

    async function pollUntilDone(shouldResume) {
      clearInterval(statusTimer);
      statusTimer = setInterval(async () => {
        const response = await fetch('/api/status');
        const data = await response.json();
        if (data.active) {
          setStatus(`${data.active.elapsed}s elapsed.`, 'Codex running');
          return;
        }
        if (data.completed && data.completed.startedAt !== lastCompletedStartedAt) {
          clearInterval(statusTimer);
          lastCompletedStartedAt = data.completed.startedAt;
          replyEl.textContent = data.completed.finalText || 'No final text.';
          await playReply(data.completed.finalText || 'Codex finished without a readable reply.', shouldResume);
        }
      }, 1400);
    }

    async function playReply(text, shouldResume) {
      setStatus('Creating local voice reply...', 'Speaking');
      try {
        const response = await fetch('/api/speak', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ text, language: languageSelect.value })
        });
        if (!response.ok) {
          const data = await response.json().catch(() => ({}));
          throw new Error(data.error || 'Could not create speech.');
        }
        const blob = await response.blob();
        const url = URL.createObjectURL(blob);
        currentAudio = new Audio(url);
        currentAudio.onended = () => {
          URL.revokeObjectURL(url);
          setStatus(shouldResume ? 'Listening again.' : 'Done.');
          if (shouldResume && keepListening) setTimeout(startUtterance, 500);
        };
        currentAudio.onerror = () => {
          URL.revokeObjectURL(url);
          setStatus('Audio playback failed.');
          if (shouldResume && keepListening) setTimeout(startUtterance, 500);
        };
        await currentAudio.play();
      } catch (error) {
        setStatus(error.message || String(error));
        if (shouldResume && keepListening) setTimeout(startUtterance, 800);
      }
    }

    async function cancelCodex() {
      await fetch('/api/cancel', { method: 'POST' });
      if (currentAudio) currentAudio.pause();
      setStatus('Cancel requested.');
      if (keepListening) setTimeout(startUtterance, 600);
    }

    async function refreshStatus() {
      try {
        const response = await fetch('/api/status');
        const data = await response.json();
        const key = data.localVoiceConfigured ? 'Local voice ready.' : 'Whisper model missing.';
        setStatus(`Worktree: ${data.worktree}`, key);
      } catch (error) {
        setStatus(error.message || String(error));
      }
    }

    listenButton.addEventListener('click', () => startListening().catch(error => setStatus(error.message || String(error))));
    stopButton.addEventListener('click', stopListening);
    cancelButton.addEventListener('click', () => cancelCodex().catch(error => setStatus(error.message || String(error))));
    refreshStatus();
    setControls();
  </script>
</body>
</html>
"""


class VoiceHandler(BaseHTTPRequestHandler):
    state: VoiceState

    def log_message(self, fmt: str, *args: Any) -> None:
        print(f"[{self.log_date_time_string()}] {fmt % args}")

    def do_GET(self) -> None:
        if self.path == "/" or self.path.startswith("/?"):
            self.respond_bytes(HTTPStatus.OK, HTML.encode("utf-8"), "text/html; charset=utf-8")
            return
        if self.path == "/api/status":
            self.respond_json(self.state.status())
            return
        self.respond_json({"ok": False, "error": "Not found."}, HTTPStatus.NOT_FOUND)

    def do_POST(self) -> None:
        if self.path == "/api/voice-task":
            self.handle_voice_task()
            return
        if self.path == "/api/speak":
            self.handle_speak()
            return
        if self.path == "/api/cancel":
            self.respond_json(self.state.cancel())
            return
        self.respond_json({"ok": False, "error": "Not found."}, HTTPStatus.NOT_FOUND)

    def handle_voice_task(self) -> None:
        try:
            length = int(self.headers.get("Content-Length", "0"))
            body = self.rfile.read(length)
            parts = parse_multipart(self.headers.get("Content-Type", ""), body)
            audio_part = parts.get("audio")
            if not audio_part or not audio_part.data:
                raise ValueError("No audio was sent.")
            language = parts.get("language").data.decode("utf-8").strip() if parts.get("language") else ""
            transcript = transcribe_audio(
                audio_part.data,
                audio_part.filename,
                audio_part.content_type,
                language,
            )
            result = self.state.start_task(transcript, language)
            result["transcript"] = transcript
            self.respond_json(result)
        except Exception as exc:
            self.respond_json({"ok": False, "error": str(exc)}, HTTPStatus.BAD_REQUEST)

    def handle_speak(self) -> None:
        try:
            payload = self.read_json()
            text = str(payload.get("text", "")).strip()
            language = str(payload.get("language", "")).strip()
            if not text:
                raise ValueError("No text was sent.")
            content_type, audio = synthesize_speech(text, language)
            self.respond_bytes(HTTPStatus.OK, audio, content_type)
        except Exception as exc:
            self.respond_json({"ok": False, "error": str(exc)}, HTTPStatus.BAD_REQUEST)

    def read_json(self) -> dict[str, Any]:
        length = int(self.headers.get("Content-Length", "0"))
        body = self.rfile.read(length)
        if not body:
            return {}
        return json.loads(body.decode("utf-8"))

    def respond_json(self, payload: dict[str, Any], status: HTTPStatus = HTTPStatus.OK) -> None:
        self.respond_bytes(status, json.dumps(payload).encode("utf-8"), "application/json")

    def respond_bytes(self, status: HTTPStatus, body: bytes, content_type: str) -> None:
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)


def main() -> None:
    parser = argparse.ArgumentParser(description="Run the local Codex voice browser server.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", default=8765, type=int)
    parser.add_argument("--env", default=str(DEFAULT_ENV), help="Relay .env file to load.")
    args = parser.parse_args()

    load_env(PROJECT_ENV)
    load_env(Path(args.env))
    config = VoiceConfig.from_env(args.host, args.port)
    state = VoiceState(config)
    state.validate()

    VoiceHandler.state = state
    server = ThreadingHTTPServer((config.host, config.port), VoiceHandler)
    print(f"Codex voice server online: http://{config.host}:{config.port}")
    print(f"Worktree: {config.codex_worktree}")
    print(f"Local Whisper model: {DEFAULT_WHISPER_MODEL}")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("Shutting down.")


if __name__ == "__main__":
    main()
