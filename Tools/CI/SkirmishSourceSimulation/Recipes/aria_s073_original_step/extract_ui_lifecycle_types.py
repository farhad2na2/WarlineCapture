#!/usr/bin/env python3
"""Extract exact current UI lifecycle enum/state declarations for the derived probe."""
import hashlib
import re
from pathlib import Path


INPUTS = (
    "Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs",
    "Assets/Game/Scripts/UI/Contracts/UIRoute.cs",
    "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs",
)
OUTPUT = "SourceS073OriginalAriaUiLifecycleTypes.cs"
DECLARATIONS = (
    (INPUTS[0], "UiShellMode", "enum"),
    (INPUTS[0], "UiShellTransitionPhase", "enum"),
    (INPUTS[1], "UIRoute", "enum"),
    (INPUTS[2], "UiShellStateComponent", "struct"),
)


def mask(text):
    out = list(text)
    i = 0
    state = "code"
    while i < len(text):
        c = text[i]
        n = text[i + 1] if i + 1 < len(text) else ""
        if state == "code":
            if c == "/" and n in ("/", "*"):
                state = "line" if n == "/" else "block"
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c == '"':
                state = "string"
                out[i] = " "
                i += 1
                continue
            if c == "'":
                state = "char"
                out[i] = " "
                i += 1
                continue
        elif state == "line":
            if c == "\n":
                state = "code"
            else:
                out[i] = " "
        elif state == "block":
            if c == "*" and n == "/":
                out[i] = out[i + 1] = " "
                i += 2
                state = "code"
                continue
            if c != "\n":
                out[i] = " "
        else:
            if c == "\\":
                out[i] = " "
                i += 1
                if i < len(text):
                    out[i] = " "
            elif c == ('"' if state == "string" else "'"):
                state = "code"
            if i < len(text) and text[i] != "\n":
                out[i] = " "
        i += 1
    if state not in ("code", "line"):
        raise ValueError("Unterminated C# comment/string while extracting UI lifecycle declarations")
    return "".join(out)


def declaration(text, name, kind):
    code = mask(text)
    pattern = re.compile(r"(?m)^\s*public\s+" + re.escape(kind) + r"\s+" + re.escape(name) + r"\b")
    matches = list(pattern.finditer(code))
    if len(matches) != 1:
        raise ValueError("Expected exactly one current public " + kind + " " + name)
    start = matches[0].start()
    brace = code.find("{", matches[0].end())
    if brace < 0:
        raise ValueError("Missing declaration body for " + name)
    depth = 1
    end = brace + 1
    while depth:
        if end >= len(code):
            raise ValueError("Unclosed declaration body for " + name)
        if code[end] == "{":
            depth += 1
        elif code[end] == "}":
            depth -= 1
        end += 1
    return text[start:end].strip()


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    source_text = {}
    source_pins = {}
    for relative in INPUTS:
        source = (repo / relative).resolve(strict=True)
        raw = source.read_bytes()
        source_text[relative] = raw.decode("utf-8").replace("\r\n", "\n")
        source_pins[str(source)] = hashlib.sha256(raw).hexdigest()

    extracted = [declaration(source_text[path], name, kind) for path, name, kind in DECLARATIONS]
    result = (
        "using Unity.Entities;\nusing Game.UI.Contracts;\n"
        "namespace Game.UI.Contracts {\n" + extracted[0] + "\n" + extracted[1] + "\n" + extracted[2] + "\n}\n"
        "namespace Game.UI.Shell.Contracts.Ecs {\n" + extracted[3] + "\n}\n"
    )
    if "UiShellRootComponent" in result:
        raise ValueError("UI lifecycle extraction must not duplicate UiShellRootComponent")
    target = output / OUTPUT
    with target.open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(result)
    generator = Path(__file__).resolve(strict=True)
    return {
        "source_pins": {**source_pins, str(generator): hashlib.sha256(generator.read_bytes()).hexdigest()},
        "outputs": {OUTPUT: hashlib.sha256(result.encode("utf-8")).hexdigest()},
        "experimental_only": True,
        "scope": "Exact current UiShellMode, UiShellTransitionPhase, UIRoute and UiShellStateComponent declarations only; no stubs and no UiShellRootComponent duplicate.",
        "declarations": [name for _, name, _ in DECLARATIONS],
    }
