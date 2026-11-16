"""Extract both original Resource Exchange startup projection owners.

The source host represents FixedString storage with managed strings and omits
the read-only EntityQuery buffer overload. The exact local substitutions below
keep owner bodies intact while matching those two surrogate API differences.
"""
from pathlib import Path
import hashlib


def _replace_exact(text, original, replacement, expected, label):
    count = text.count(original)
    if count != expected:
        raise ValueError(f"{label}: expected {expected} occurrences, found {count}: {original}")
    return text.replace(original, replacement)


def generate(repo, output):
    root = Path(repo)
    owner_paths = {
        "ResourceExchangeStartupProjectionSystemHelper.cs":
            root / "Assets/Game/Scripts/Systems/ResourceExchangeStartupProjectionSystemHelper.cs",
        "ResourceExchangeAIStartupProjectionSystemHelper.cs":
            root / "Assets/Game/Scripts/Systems/ResourceExchangeAIStartupProjectionSystemHelper.cs",
    }
    raw = {name: path.read_bytes() for name, path in owner_paths.items()}
    texts = {name: data.decode("utf-8").replace("\r\n", "\n") for name, data in raw.items()}

    projection_name = "ResourceExchangeStartupProjectionSystemHelper.cs"
    projection = texts[projection_name]
    projection = _replace_exact(
        projection,
        "where T : unmanaged, IComponentData",
        "where T : struct, IComponentData",
        1,
        "projection component constraint",
    )
    projection = _replace_exact(
        projection,
        "where T : unmanaged, IBufferElementData",
        "where T : struct, IBufferElementData",
        1,
        "projection buffer constraint",
    )

    ai_name = "ResourceExchangeAIStartupProjectionSystemHelper.cs"
    ai = texts[ai_name]
    ai = _replace_exact(
        ai,
        "controlQuery.GetSingletonBuffer<FactionControlEntry>(true)",
        "entityManager.GetBuffer<FactionControlEntry>(controlQuery.GetSingletonEntity())",
        1,
        "read-only singleton buffer access",
    )
    ai = _replace_exact(
        ai,
        "where T : unmanaged, IBufferElementData",
        "where T : struct, IBufferElementData",
        1,
        "AI buffer constraint",
    )

    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)
    generated = {
        projection_name: projection,
        ai_name: ai,
    }
    for name, text in generated.items():
        (out / name).write_text(text)

    if any(path.read_bytes() != raw[name] for name, path in owner_paths.items()):
        raise ValueError("Resource Exchange owner source changed during extraction")
    return {
        "kind": "ResourceExchangeOriginalStartupProjectionOwners",
        "source_sha256": {
            str(owner_paths[name]): hashlib.sha256(data).hexdigest()
            for name, data in raw.items()
        },
        "generated_sha256": {
            name: hashlib.sha256(text.encode("utf-8")).hexdigest()
            for name, text in generated.items()
        },
        "substitutions": [
            "managed-string FixedString surrogate: unmanaged constraints become struct",
            "surrogate EntityQuery: read-only GetSingletonBuffer becomes GetBuffer on the singleton entity",
        ],
        "scope": "Original projection and AI helper bodies; finite source ECS only, no Unity runtime readiness claim",
    }
