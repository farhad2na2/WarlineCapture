"""Extract the exact source AI controller class and role enums for the S073 owner closure."""
from pathlib import Path
import hashlib
import json
import re


INPUTS = {
    "models": "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
    "values": "Assets/Game/Scripts/Configs/GameplayConfigValueTypes.cs",
    "startup": "Assets/Game/Scripts/Systems/AIStartupSystem.cs",
}


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
            if c == "@" and n == '"':
                state = "verbatim"
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c in ('"', "'"):
                state = "string" if c == '"' else "char"
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
        elif state == "verbatim":
            if c == '"' and n == '"':
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c == '"':
                state = "code"
            if c != "\n":
                out[i] = " "
        else:
            if c == "\\":
                out[i] = " "
                i += 1
                if i < len(text) and text[i] != "\n":
                    out[i] = " "
                continue
            if c == ('"' if state == "string" else "'"):
                state = "code"
            if c != "\n":
                out[i] = " "
        i += 1
    if state not in ("code", "line"):
        raise ValueError("Unterminated C# lexical value")
    return "".join(out)


def span(text, pattern, label, preceding_attribute=False):
    code = mask(text)
    matches = list(re.finditer(pattern, code, re.MULTILINE))
    if len(matches) != 1:
        raise ValueError("Unique original declaration required: " + label)
    match = matches[0]
    brace = code.find("{", match.end())
    if brace < 0:
        raise ValueError("Original declaration body missing: " + label)
    depth = 1
    end = brace + 1
    while depth:
        if end >= len(code):
            raise ValueError("Original declaration body is unclosed: " + label)
        if code[end] == "{":
            depth += 1
        elif code[end] == "}":
            depth -= 1
        end += 1
    start = match.start()
    if preceding_attribute:
        line_start = text.rfind("\n", 0, start) + 1
        prior_end = line_start - 1
        prior_start = text.rfind("\n", 0, max(prior_end, 0)) + 1
        prior_line = text[prior_start:prior_end].strip()
        if prior_line.startswith("[CreateAssetMenu("):
            start = prior_start
    return text[start:end]


def method(text, signature):
    code = mask(text)
    matches = list(re.finditer(re.escape(signature), code))
    if len(matches) != 1:
        raise ValueError("Unique original AI startup method required: " + signature)
    start = matches[0].start()
    cursor = code.find("(", matches[0].start())
    depth = 0
    while cursor < len(code):
        if code[cursor] == "(":
            depth += 1
        elif code[cursor] == ")":
            depth -= 1
            if depth == 0:
                cursor += 1
                break
        cursor += 1
    brace = code.find("{", cursor)
    if depth != 0 or brace < 0:
        raise ValueError("Original AI startup method body missing: " + signature)
    depth = 1
    end = brace + 1
    while depth:
        if end >= len(code):
            raise ValueError("Original AI startup method is unclosed: " + signature)
        if code[end] == "{":
            depth += 1
        elif code[end] == "}":
            depth -= 1
        end += 1
    return text[start:end]


def generate(repo, output):
    repo = Path(repo)
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    paths = {key: repo / value for key, value in INPUTS.items()}
    raw = {key: path.read_bytes() for key, path in paths.items()}
    text = {key: data.decode("utf-8").replace("\r\n", "\n") for key, data in raw.items()}
    controller = span(text["models"], r"(?m)^    public class AIControllerConfig\s*:\s*ScriptableObject\s*", "AIControllerConfig", True)
    role = span(text["values"], r"(?m)^    public enum AIControllerRole\s*:\s*byte\s*", "AIControllerRole")
    difficulty = span(text["values"], r"(?m)^    public enum AIControllerDifficulty\s*:\s*byte\s*", "AIControllerDifficulty")
    startup = text["startup"]
    delegate_pattern = r"(?m)^        public delegate bool TryResolveFactionSpawnCell\(byte factionId, out int2 spawnCell\);$"
    delegates = list(re.finditer(delegate_pattern, mask(startup)))
    if len(delegates) != 1:
        raise ValueError("Unique original AI spawn-cell resolver delegate required")
    delegate = startup[delegates[0].start():delegates[0].end()]
    result_type = span(startup, r"(?m)^        public readonly struct Result\s*", "AIStartupSystem.Result")
    methods = [
        method(startup, "        public Result Initialize(\n            EntityManager em,"),
        method(startup, "        private static void BuildFactionEconomyStartupEntries("),
        method(startup, "        private static void BuildFactionControlStartupEntries("),
        method(startup, "        private void EnsureAIBuildPlansInitialized("),
        method(startup, "        private void EnsureAIProductionPlansInitialized("),
        method(startup, "        private void EnsureAISquadPlansInitialized("),
        method(startup, "        private void EnsureAITargetPrioritySettingsInitialized("),
        method(startup, "        private static bool ShouldIncludeAIConfig("),
    ]
    ai_startup = (
        "using System.Collections.Generic;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;"
        "using UnityEngine;using Game.Components;using Game.Configs;namespace Game.Runtime{public partial struct AIStartupSystem{\n"
        + delegate + "\n" + result_type + "\n" + "\n".join(methods) + "\n}}"
    )
    files = {
        "S073_AIControllerConfig.source.cs": "using System;using System.Collections.Generic;using UnityEngine;namespace Game.Configs{\n" + controller + "\n}",
        "S073_AIControllerEnums.source.cs": "namespace Game.Configs{\n" + role + "\n" + difficulty + "\n}",
        "S073_AIStartupSystem.source.cs": ai_startup,
    }
    if any(paths[key].read_bytes() != value for key, value in raw.items()):
        raise ValueError("Original AI config source changed during extraction")
    for name, value in files.items():
        with (output / name).open("w", encoding="utf-8", newline="\n") as stream:
            stream.write(value)
    proof = {
        "kind": "S073OriginalAIConfigTypeExtraction",
        "scope": "Exact AI controller config types and only the original AIStartupSystem Initialize dependency closure; diagnostic/presentation members excluded from this selected source slice",
        "source_pins": {str(paths[key]): hashlib.sha256(value).hexdigest() for key, value in raw.items()},
        "outputs": {name: hashlib.sha256(value.encode()).hexdigest() for name, value in files.items()},
        "ai_startup_members": ["Initialize(EntityManager,...)" ,"BuildFactionEconomyStartupEntries", "BuildFactionControlStartupEntries",
                               "EnsureAIBuildPlansInitialized", "EnsureAIProductionPlansInitialized", "EnsureAISquadPlansInitialized",
                               "EnsureAITargetPrioritySettingsInitialized", "ShouldIncludeAIConfig"],
        "native_world_allocated": False,
        "mission_state_created": False,
    }
    return proof
