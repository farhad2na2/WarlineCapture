#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import subprocess
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ROOT = ROOT / "Assets/Game/Scripts"
DEFAULT_OUTPUT = ROOT / "Design/Architecture/phase7_monobehaviour_loop_baseline.md"

TYPE_DECLARATION_RE = re.compile(
    r"^[ \t]*(?:(?:\[[^\]\r\n]*(?:\r?\n[ \t]*\[[^\]\r\n]*)*\][ \t]*)\r?\n[ \t]*)*"
    r"(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|unsafe)\s+)*"
    r"(?P<kind>class)\s+"
    r"(?P<name>[A-Za-z_]\w*)"
    r"(?:\s*<[^>{};\r\n]+>)?"
    r"\s*(?P<bases>:[^{;]+)?",
    re.MULTILINE,
)

LOOP_METHOD_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|static|virtual|override|sealed|async)\s+)*"
    r"(?:(?:void\s+(?P<update>Update|LateUpdate|FixedUpdate))|(?:IEnumerator\s+(?P<coroutine>[A-Za-z_]\w*)))\s*\(",
    re.MULTILINE,
)


@dataclass(frozen=True)
class LoopEntry:
    path: str
    type_name: str
    method: str
    line: int
    scope: str

    @property
    def key(self) -> str:
        return f"{self.path}|{self.type_name}|{self.method}"


def normalize(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def strip_comments_and_strings(text: str) -> str:
    result: list[str] = []
    i = 0
    length = len(text)
    in_line_comment = False
    in_block_comment = False
    in_string = False
    in_verbatim_string = False
    in_char = False
    while i < length:
        ch = text[i]
        nxt = text[i + 1] if i + 1 < length else ""

        if in_line_comment:
            if ch == "\n":
                in_line_comment = False
                result.append(ch)
            else:
                result.append(" ")
            i += 1
            continue

        if in_block_comment:
            if ch == "*" and nxt == "/":
                result.extend((" ", " "))
                in_block_comment = False
                i += 2
            else:
                result.append("\n" if ch == "\n" else " ")
                i += 1
            continue

        if in_string:
            if ch == "\\" and not in_verbatim_string:
                result.append(" ")
                if i + 1 < length:
                    result.append(" ")
                    i += 2
                else:
                    i += 1
                continue
            if in_verbatim_string and ch == '"' and nxt == '"':
                result.extend((" ", " "))
                i += 2
                continue
            if ch == '"':
                in_string = False
                in_verbatim_string = False
            result.append("\n" if ch == "\n" else " ")
            i += 1
            continue

        if in_char:
            if ch == "\\":
                result.append(" ")
                if i + 1 < length:
                    result.append(" ")
                    i += 2
                else:
                    i += 1
                continue
            if ch == "'":
                in_char = False
            result.append("\n" if ch == "\n" else " ")
            i += 1
            continue

        if ch == "/" and nxt == "/":
            result.extend((" ", " "))
            in_line_comment = True
            i += 2
            continue

        if ch == "/" and nxt == "*":
            result.extend((" ", " "))
            in_block_comment = True
            i += 2
            continue

        if ch == "@" and nxt == '"':
            result.extend((" ", " "))
            in_string = True
            in_verbatim_string = True
            i += 2
            continue

        if ch == '"':
            result.append(" ")
            in_string = True
            i += 1
            continue

        if ch == "'":
            result.append(" ")
            in_char = True
            i += 1
            continue

        result.append(ch)
        i += 1

    return "".join(result)


def find_body_end(text: str, declaration_match: re.Match[str]) -> int:
    open_brace = text.find("{", declaration_match.end())
    if open_brace < 0:
        return declaration_match.end()

    depth = 0
    for index in range(open_brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return index + 1
    return len(text)


def scope_for(path: str) -> str:
    if "/Editor/" in path or path.startswith("Assets/Game/Scripts/Editor/"):
        return "Editor"
    if path.startswith("Assets/Game/Scripts/UI/"):
        return "ProductionUI"
    return "ProductionNonUI"


def enumerate_loop_entries(source_root: Path) -> list[LoopEntry]:
    entries: list[LoopEntry] = []
    for path in sorted(source_root.rglob("*.cs")):
        rel_path = normalize(path)
        text = path.read_text(encoding="utf-8")
        clean_text = strip_comments_and_strings(text)
        for declaration in TYPE_DECLARATION_RE.finditer(clean_text):
            bases = (declaration.group("bases") or "").lstrip(":")
            if not re.search(r"\b(?:MonoBehaviour|UnityEngine\.MonoBehaviour)\b", bases):
                continue

            body_end = find_body_end(clean_text, declaration)
            body = clean_text[declaration.end():body_end]
            for loop_match in LOOP_METHOD_RE.finditer(body):
                method = loop_match.group("update") or f"Coroutine:{loop_match.group('coroutine')}"
                line = clean_text.count("\n", 0, declaration.end() + loop_match.start()) + 1
                entries.append(LoopEntry(rel_path, declaration.group("name"), method, line, scope_for(rel_path)))

    return sorted(entries, key=lambda entry: (entry.scope, entry.path, entry.type_name, entry.method))


def git_value(args: list[str], fallback: str) -> str:
    try:
        result = subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True, text=True)
    except (OSError, subprocess.CalledProcessError):
        return fallback
    value = result.stdout.strip()
    return value if value else fallback


def markdown_escape(value: object) -> str:
    return str(value).replace("\n", " ").replace("|", "\\|")


def markdown_code(value: object) -> str:
    return f"`{markdown_escape(value)}`"


def write_markdown(entries: list[LoopEntry], output_path: Path, command: str, source_root: Path) -> None:
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    commit = git_value(["rev-parse", "--short", "HEAD"], "unknown")
    lines = [
        "# Phase 7 MonoBehaviour Loop Baseline",
        "",
        "Purpose:",
        "Capture the existing MonoBehaviour runtime loop surface before Phase 7 domain conversions. The Phase 7 architecture guard fails if a new loop key appears outside this baseline.",
        "",
        f"Generated: `{timestamp}`.",
        f"Command: `{command}`.",
        f"Source root: `{normalize_or_abs(source_root)}`.",
        f"Source commit: `{commit}`.",
        f"Rows: `{len(entries)}`.",
        "",
        "## Baseline",
        "",
        "| Key | Path | Type | Method | Line | Scope |",
        "| --- | --- | --- | --- | ---: | --- |",
    ]
    for entry in entries:
        lines.append(
            "| "
            + " | ".join(
                [
                    markdown_code(entry.key),
                    markdown_code(entry.path),
                    markdown_code(entry.type_name),
                    markdown_code(entry.method),
                    str(entry.line),
                    markdown_code(entry.scope),
                ]
            )
            + " |"
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def normalize_or_abs(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def shell_quote_command(root_arg: Path, output_arg: Path) -> str:
    return (
        "python3 Tools/Architecture/generate_phase7_monobehaviour_loop_baseline.py "
        f"--root {normalize_or_abs(root_arg)} --output {normalize_or_abs(output_arg)}"
    )


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate the Phase 7 MonoBehaviour loop baseline.")
    parser.add_argument("--root", default=DEFAULT_ROOT.as_posix(), help="Source root to scan.")
    parser.add_argument("--output", default=DEFAULT_OUTPUT.as_posix(), help="Markdown baseline output path.")
    args = parser.parse_args()

    source_root = (ROOT / args.root).resolve() if not Path(args.root).is_absolute() else Path(args.root)
    output_path = (ROOT / args.output).resolve() if not Path(args.output).is_absolute() else Path(args.output)
    entries = enumerate_loop_entries(source_root)
    write_markdown(entries, output_path, shell_quote_command(source_root, output_path), source_root)


if __name__ == "__main__":
    main()
