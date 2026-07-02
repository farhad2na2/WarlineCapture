#!/usr/bin/env python3
from __future__ import annotations

import argparse
import difflib
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
GAME_SCRIPTS_ROOT = ROOT / "Assets/Game/Scripts"

NAMESPACE_LINE_RE = re.compile(
    r"^(?P<prefix>[ \t]*namespace[ \t]+)"
    r"(?P<name>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)"
    r"(?P<suffix>[ \t]*(?:[;{]|$).*)$"
)
NAMESPACE_RE = re.compile(
    r"^[ \t]*namespace[ \t]+(?P<name>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)[ \t]*(?:[;{]|$)",
    re.MULTILINE,
)
FILE_SCOPED_NAMESPACE_RE = re.compile(
    r"^[ \t]*namespace[ \t]+(?P<name>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)[ \t]*;",
    re.MULTILINE,
)


@dataclass(frozen=True)
class AssemblyOwner:
    name: str
    root_namespace: str
    directory: Path


@dataclass(frozen=True)
class ScriptTarget:
    path: Path
    assembly: AssemblyOwner
    current_namespace: str
    has_file_scoped_namespace: bool

    @property
    def target_namespace(self) -> str:
        return self.assembly.root_namespace

    @property
    def needs_change(self) -> bool:
        return self.current_namespace != self.target_namespace or self.has_file_scoped_namespace


def normalize(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def load_assembly_owner(path: Path) -> AssemblyOwner:
    data = json.loads(path.read_text(encoding="utf-8"))
    name = data["name"]
    root_namespace = data.get("rootNamespace") or name
    return AssemblyOwner(name=name, root_namespace=root_namespace, directory=path.parent)


def assembly_owners() -> list[AssemblyOwner]:
    owners = [load_assembly_owner(path) for path in sorted(GAME_SCRIPTS_ROOT.rglob("*.asmdef"))]
    return sorted(owners, key=lambda owner: len(owner.directory.parts), reverse=True)


def find_owner(path: Path, owners: list[AssemblyOwner]) -> AssemblyOwner:
    for owner in owners:
        if path.is_relative_to(owner.directory):
            return owner

    raise ValueError(f"No assembly owner found for {normalize(path)}")


def current_namespace(text: str) -> str:
    match = NAMESPACE_RE.search(text)
    return match.group("name") if match else ""


def has_file_scoped_namespace(text: str) -> bool:
    return bool(FILE_SCOPED_NAMESPACE_RE.search(text))


def script_targets(assembly_filter: str | None) -> list[ScriptTarget]:
    owners = assembly_owners()
    targets: list[ScriptTarget] = []
    for path in sorted(GAME_SCRIPTS_ROOT.rglob("*.cs")):
        owner = find_owner(path, owners)
        if assembly_filter and owner.name != assembly_filter:
            continue

        text = path.read_text(encoding="utf-8-sig")
        targets.append(
            ScriptTarget(
                path=path,
                assembly=owner,
                current_namespace=current_namespace(text),
                has_file_scoped_namespace=has_file_scoped_namespace(text),
            )
        )

    return targets


def rewrite_existing_block_namespace(lines: list[str], target_namespace: str) -> list[str]:
    rewritten = list(lines)
    for index, line in enumerate(rewritten):
        match = NAMESPACE_LINE_RE.match(line.rstrip("\n\r"))
        if not match:
            continue

        rewritten[index] = f"{match.group('prefix')}{target_namespace}{match.group('suffix')}{line[len(line.rstrip(chr(10)).rstrip(chr(13))):]}"
        return rewritten

    raise ValueError("Namespace detected but namespace line could not be rewritten.")


def rewrite_existing_file_scoped_namespace(lines: list[str], target_namespace: str) -> list[str]:
    for index, line in enumerate(lines):
        stripped_line = line.rstrip("\n\r")
        match = NAMESPACE_LINE_RE.match(stripped_line)
        if not match:
            continue
        if not match.group("suffix").lstrip().startswith(";"):
            continue

        newline = "\r\n" if line.endswith("\r\n") else "\n"
        rewritten = list(lines[:index])
        if rewritten and rewritten[-1].strip():
            rewritten.append(newline)
        rewritten.append(f"namespace {target_namespace}{newline}")
        rewritten.append(f"{{{newline}")
        body = lines[index + 1 :]
        rewritten.extend(("    " + body_line if body_line.strip() else body_line) for body_line in body)
        if rewritten and rewritten[-1].strip():
            rewritten.append(newline)
        rewritten.append(f"}}{newline}")
        return rewritten

    raise ValueError("File-scoped namespace detected but namespace line could not be rewritten.")


def is_top_directive_line(stripped: str) -> bool:
    return (
        not stripped
        or stripped.startswith("//")
        or stripped.startswith("/*")
        or stripped.startswith("*")
        or stripped.startswith("*/")
        or stripped.startswith("using ")
        or stripped.startswith("using\t")
        or stripped.startswith("extern alias ")
        or stripped.startswith("[assembly:")
        or stripped.startswith("#nullable")
        or stripped.startswith("#pragma")
    )


KNOWN_NAMESPACE_ALIAS_ROOTS = {
    "System",
    "Unity",
    "UnityEngine",
    "UnityEditor",
    "Game",
    "TMPro",
    "SnivelerCode",
    "AOT",
    "JetBrains",
    "NUnit",
}

USING_ALIAS_RE = re.compile(
    r"^[ \t]*using[ \t]+[A-Za-z_]\w*[ \t]*=[ \t]*(?P<root>[A-Za-z_]\w*)(?:[.;])"
)


def should_keep_using_alias_outside_namespace(line: str) -> bool:
    match = USING_ALIAS_RE.match(line.strip())
    if not match:
        return True

    return match.group("root") in KNOWN_NAMESPACE_ALIAS_ROOTS


def insertion_index_for_namespace(lines: list[str]) -> int:
    index = 0
    while (
        index < len(lines)
        and is_top_directive_line(lines[index].strip())
        and should_keep_using_alias_outside_namespace(lines[index])
    ):
        index += 1

    return index


def add_block_scoped_namespace(lines: list[str], target_namespace: str) -> list[str]:
    index = insertion_index_for_namespace(lines)
    newline = "\n"
    for line in lines:
        if line.endswith("\r\n"):
            newline = "\r\n"
            break

    namespace_lines = [f"namespace {target_namespace}{newline}", f"{{{newline}"]
    if index > 0 and lines[index - 1].strip():
        namespace_lines.insert(0, newline)

    rewritten = list(lines)
    body = rewritten[index:]
    rewritten[index:] = namespace_lines
    rewritten.extend(("    " + body_line if body_line.strip() else body_line) for body_line in body)
    if rewritten and rewritten[-1].strip():
        rewritten.append(newline)
    rewritten.append(f"}}{newline}")
    return rewritten


def rewrite_text(text: str, target_namespace: str) -> str:
    lines = text.splitlines(keepends=True)
    if current_namespace(text):
        if has_file_scoped_namespace(text):
            rewritten = rewrite_existing_file_scoped_namespace(lines, target_namespace)
        else:
            rewritten = rewrite_existing_block_namespace(lines, target_namespace)
    else:
        rewritten = add_block_scoped_namespace(lines, target_namespace)

    return "".join(rewritten)


def print_diff(path: Path, before: str, after: str) -> None:
    before_lines = before.splitlines(keepends=True)
    after_lines = after.splitlines(keepends=True)
    sys.stdout.writelines(
        difflib.unified_diff(
            before_lines,
            after_lines,
            fromfile=f"a/{normalize(path)}",
            tofile=f"b/{normalize(path)}",
        )
    )


def run(args: argparse.Namespace) -> int:
    targets = script_targets(args.assembly)
    changed = 0
    for target in targets:
        if not target.needs_change:
            continue

        before = target.path.read_text(encoding="utf-8-sig")
        after = rewrite_text(before, target.target_namespace)
        if before == after:
            continue

        changed += 1
        if args.report:
            print(f"{normalize(target.path)} :: {target.current_namespace or '(none)'} -> {target.target_namespace}")
        if args.diff:
            print_diff(target.path, before, after)
        if args.apply:
            target.path.write_text(after, encoding="utf-8")

    print(
        f"assembly={args.assembly or '(all)'} files={len(targets)} changed={changed} "
        f"mode={'apply' if args.apply else 'dry-run'}"
    )
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Apply assembly-root namespaces to Assets/Game/Scripts C# files.")
    parser.add_argument("--assembly", help="Limit to one assembly name, such as Game.Catalog.Contracts.")
    parser.add_argument("--apply", action="store_true", help="Write changes. Omit for dry-run mode.")
    parser.add_argument("--diff", action="store_true", help="Print unified diffs for proposed changes.")
    parser.add_argument("--report", action="store_true", help="Print one-line proposed changes.")
    return parser.parse_args()


if __name__ == "__main__":
    raise SystemExit(run(parse_args()))
