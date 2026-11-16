"""Extract the original breach approach kernel and three geometry helpers unchanged."""
from pathlib import Path
import hashlib

SOURCE = "Assets/Game/Scripts/Systems/BuildingBarrierUtilitySystemHelper.cs"
SIGNATURES = (
    "public static bool TryFindBreachApproachCell(",
    "private static int2 ResolvePerimeterOutsideDirection(",
    "private static void TryScoreBreachApproachCandidate(",
    "private static bool IsOutsidePerimeterOnSide(",
)

def sha(raw):
    return hashlib.sha256(raw).hexdigest()

def extract_method(text, signature):
    if text.count(signature) != 1:
        raise ValueError("Expected unique original method: " + signature)
    start = text.index(signature)
    brace = text.index("{", start)
    depth = 0
    in_string = in_char = escape = False
    for index in range(brace, len(text)):
        char = text[index]
        if escape:
            escape = False
            continue
        if (in_string or in_char) and char == "\\":
            escape = True
            continue
        if not in_char and char == '"':
            in_string = not in_string
            continue
        if not in_string and char == "'":
            in_char = not in_char
            continue
        if in_string or in_char:
            continue
        if char == "{": depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    raise ValueError("Unterminated original method: " + signature)

def generate(repo, output):
    source_path = Path(repo) / SOURCE
    original = source_path.read_bytes()
    text = original.decode("utf-8").replace("\r\n", "\n")
    methods = [extract_method(text, signature) for signature in SIGNATURES]
    body = "using Game.Components;\nusing Unity.Collections;\nusing Unity.Mathematics;\nusing UnityEngine;\n\nnamespace Game.Runtime\n{\n    internal static partial class BuildingBarrierUtilitySystemHelper\n    {\n"
    body += "\n\n".join("        " + method.replace("\n", "\n        ") for method in methods)
    body += "\n    }\n}\n"
    if source_path.read_bytes() != original:
        raise ValueError("BuildingBarrierUtilitySystemHelper changed during extraction")
    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)
    with (out / "S073BreachApproachGeometry.cs").open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(body)
    return {"kind":"S073OriginalBreachApproachExtraction", "source_pins":{str(source_path.resolve()):sha(original)}, "source_sha256":sha(original), "extracted_sha256":sha(body.encode()), "extracted_signatures":list(SIGNATURES), "scope":"Four original methods copied without gameplay edits into the same partial helper type."}
