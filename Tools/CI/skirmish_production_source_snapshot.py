"""Complete Windows candidate snapshots and exact source mutation attribution.

Reuses the S005 complete source enumeration/hash contract. No exclusions, Unity,
asset writes, caches, build identity claims, or mission qualification.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import re

from s005_windows_source_snapshot import candidate_hash, snapshot


def write_record(project, output, record):
    project, output = Path(project).resolve(), Path(output).resolve()
    for name in ("Assets", "Packages", "ProjectSettings"):
        if output.is_relative_to(project / name):
            raise ValueError("Snapshot output must remain outside hashed source trees")
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(record, stream, indent=2)
        stream.write("\n")


def capture(project, output):
    project = Path(project).resolve()
    # Reject source-tree output before reading the census.
    for name in ("Assets", "Packages", "ProjectSettings"):
        if Path(output).resolve().is_relative_to(project / name):
            raise ValueError("Snapshot output must remain outside hashed source trees")
    manifest = snapshot(project)
    if not manifest:
        raise ValueError("Complete candidate contains no source files")
    record = dict(schema_version=1, project=str(project), captured_utc=datetime.now(timezone.utc).isoformat(),
                  source_manifest=manifest, candidate_sha256=candidate_hash(manifest),
                  source_file_count=len(manifest), scope="CompleteAssetsPackagesProjectSettings")
    write_record(project, output, record)
    return record


def checked(record):
    if not isinstance(record, dict) or type(record.get("schema_version")) is not int or record.get("schema_version") != 1 or record.get("scope") != "CompleteAssetsPackagesProjectSettings":
        raise ValueError("Unsupported or incomplete snapshot shape")
    manifest = record.get("source_manifest")
    if not isinstance(manifest, list) or not manifest or record.get("source_file_count") != len(manifest):
        raise ValueError("Missing complete nonempty manifest/count")
    seen = set()
    for row in manifest:
        if not isinstance(row, dict) or set(row) != {"path", "sha256"}:
            raise ValueError("Invalid manifest row")
        path, digest = row["path"], row["sha256"]
        if not isinstance(path, str) or path in seen or "\\" in path or ":" in path or ".." in Path(path).parts or path.startswith("/"):
            raise ValueError("Unsafe/duplicate source path")
        if path.split("/", 1)[0] not in ("Assets", "Packages", "ProjectSettings") or not isinstance(digest, str) or not re.fullmatch("[a-f0-9]{64}", digest):
            raise ValueError("Invalid source tree/digest")
        seen.add(path)
    if record.get("candidate_sha256") != candidate_hash(manifest):
        raise ValueError("Snapshot aggregate digest does not match exact Windows manifest")
    if not isinstance(record.get("project"), str) or not record["project"]:
        raise ValueError("Snapshot project missing")
    return {row["path"]: row["sha256"] for row in manifest}


def compare(before, after):
    first, second = checked(before), checked(after)
    if str(Path(before["project"]).resolve()).casefold() != str(Path(after["project"]).resolve()).casefold():
        raise ValueError("Snapshots belong to different projects")
    added = [{"path": path, "after_sha256": second[path]} for path in sorted(second.keys() - first.keys())]
    removed = [{"path": path, "before_sha256": first[path]} for path in sorted(first.keys() - second.keys())]
    changed = [{"path": path, "before_sha256": first[path], "after_sha256": second[path]} for path in sorted(first.keys() & second.keys()) if first[path] != second[path]]
    return dict(result="Unchanged" if not added and not removed and not changed else "SourceChanged",
                before_candidate_sha256=before["candidate_sha256"], after_candidate_sha256=after["candidate_sha256"],
                before_file_count=len(first), after_file_count=len(second), added=added, removed=removed, changed=changed,
                mission_qualification="not_assessed")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("capture", "compare"))
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path)
    parser.add_argument("--before", type=Path)
    parser.add_argument("--after", type=Path)
    args = parser.parse_args()
    try:
        if args.command == "capture":
            if args.output is None:
                raise ValueError("--output is required")
            record = capture(args.project, args.output)
            print(json.dumps({key: value for key, value in record.items() if key != "source_manifest"}, indent=2))
            return 0
        if args.before is None or args.after is None:
            raise ValueError("--before and --after are required")
        before = json.loads(args.before.read_text(encoding="utf-8-sig"))
        result = compare(before, json.loads(args.after.read_text(encoding="utf-8-sig")))
        if args.output is not None:
            write_record(before["project"], args.output, result)
        print(json.dumps(result, indent=2))
        return 0 if result["result"] == "Unchanged" else 2
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(json.dumps(dict(result="Rejected", error=str(error), mission_qualification="not_assessed")))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
